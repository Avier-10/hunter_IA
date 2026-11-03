using System;
using UnityEngine;

public enum WolfStates
{
    Patrol,
    Chase,
    GoToLastKnown,
    Search
}

// Holds components, shared tuning and perceived-target memory. Transitions live in the states.
[RequireComponent(typeof(VisionSensor), typeof(PathNavigator))]
public class FSMAgentWolf : Agent
{
    [Header("Data")]
    [SerializeField] private WolfPatrolData dataPatrol;
    [SerializeField] private WolfChaseData dataChase;
    [SerializeField] private WolfGoToLastKnownData dataGoToLastKnown;
    [SerializeField] private WolfSearchData dataSearch;

    [Header("Movement")]
    [SerializeField] private float _maxSteering = 8f;
    [SerializeField] private float _slowingDistance = 1f;

    [Header("Combat")]
    [SerializeField] private float _captureRadius = 1f;
    [SerializeField] private float _howlCooldown = 3f;
    [Tooltip("Seconds after losing a target before re-seeing it raises TargetAcquired again.")]
    [SerializeField] private float _reacquireCooldown = 2f;

    private StateMachine _stateMachine;
    private VisionSensor _sensor;
    private PathNavigator _navigator;
    private WolfStates _current;
    private bool _hasState;
    private float _nextHowlTime;

    private Transform _target;
    private Agent _targetAgent;
    private ICapturable _targetCapturable;

    private Transform _announcedTarget;
    private float _targetLostTime = float.NegativeInfinity;

    private Vector3 _lastKnownPosition;

    protected override bool UsesBounds => false;

    public VisionSensor Sensor => _sensor;
    public PathNavigator Navigator => _navigator;
    public WolfStates CurrentState => _current;
    public float CaptureRadius => _captureRadius;
    public float MaxSteering => _maxSteering;
    public float SlowingDistance => _slowingDistance;

    // Set by the last TryPerceiveTarget call: true only for a genuinely new sighting (not a quick re-acquire).
    public bool IsNewSighting { get; private set; }

    public bool HasLastKnownPosition { get; private set; }
    public Vector3 LastKnownPosition => _lastKnownPosition;

    // Null while nothing is perceived; callers must not assume it survives a lost-sight transition.
    // The ternary normalizes a destroyed Unity object (fake-null) to a real null.
    public Transform Target => _target != null ? _target : null;
    public Agent TargetAgent => _targetAgent;
    public ICapturable TargetCapturable => _targetCapturable;

    public event Action<WolfStates, WolfStates> StateChanged;
    public event Action<Vector3> Howled;
    public event Action<Transform> TargetAcquired;
    public event Action<FSMAgentWolf, Vector3> AlertReceived;
    public event Action TargetDropped;

    protected override void Awake()
    {
        base.Awake();

        _sensor = GetComponent<VisionSensor>();
        _navigator = GetComponent<PathNavigator>();

        ValidateSetup();

        _stateMachine = new StateMachine();
        _stateMachine.RegisterState(WolfStates.Patrol, new WolfPatrolState(this, dataPatrol, _stateMachine));
        _stateMachine.RegisterState(WolfStates.Chase, new WolfChaseState(this, dataChase, _stateMachine));
        _stateMachine.RegisterState(WolfStates.GoToLastKnown, new WolfGoToLastKnownState(this, dataGoToLastKnown, _stateMachine));
        _stateMachine.RegisterState(WolfStates.Search, new WolfSearchState(this, dataSearch, _stateMachine));
    }

    // Initial state is entered here so listeners subscribed in OnEnable see the first transition
    // and the Graph singleton is already available.
    private void Start()
    {
        ChangeState(WolfStates.Patrol);
    }

    private void OnEnable()
    {
        WolfAlertBus.AlertRaised += HandleAlert;
        // No reset when the hunter enters a safe zone: wolves that see it enter lose it through perception (safe-zone
        // targets are invisible), while the ones already heading to a last known position keep going and can ambush the exit.
        HunterSignals.HunterDied += HandleTargetDropped;
    }

    private void OnDisable()
    {
        WolfAlertBus.AlertRaised -= HandleAlert;
        HunterSignals.HunterDied -= HandleTargetDropped;
    }

    private void Update()
    {
        _stateMachine.Update();
    }

    private void OnValidate()
    {
        ValidateSetup();
    }

    public void ChangeState(WolfStates next)
    {
        if (_stateMachine == null || (_hasState && _current == next)) return;

        WolfStates previous = _current;
        bool hadState = _hasState;
        _current = next;
        _hasState = true;

        _stateMachine.ChangeState(next);

        StateChanged?.Invoke(hadState ? previous : next, next);
    }

    public void SetLastKnownPosition(Vector3 position)
    {
        _lastKnownPosition = position;
        HasLastKnownPosition = true;
    }

    public void ClearLastKnownPosition()
    {
        HasLastKnownPosition = false;
    }

    // Reads the throttled sensor and remembers the closest visible target. No static lookup of the hunter.
    public bool TryPerceiveTarget()
    {
        IsNewSighting = false;
        if (_sensor == null || !_sensor.HasTarget) return false;

        Transform seen = _sensor.ClosestTarget != null ? _sensor.ClosestTarget : _sensor.VisibleTargets[0];
        if (seen == null || Zone.IsInSafeZone(seen.position)) return false;

        if (seen != _target) IsNewSighting = CacheTarget(seen);
        SetLastKnownPosition(seen.position);
        return true;
    }

    public void ClearTarget()
    {
        if (_target != null) _targetLostTime = Time.time;

        _target = null;
        _targetAgent = null;
        _targetCapturable = null;
    }

    public void Howl(Vector3 position)
    {
        if (Time.time < _nextHowlTime) return;

        _nextHowlTime = Time.time + _howlCooldown;
        WolfAlertBus.Raise(this, position);
        Howled?.Invoke(position);
    }

    // Flat movement that refuses to enter a safe zone, even when seeking straight at a target.
    public void Move(Vector3 steering, float maxSpeed)
    {
        Vector3 before = transform.position;
        steering.y = 0f;

        ApplyVelocity(steering, maxSpeed);

        Vector3 after = transform.position;
        after.y = before.y;

        if (Zone.IsInSafeZone(after))
        {
            transform.position = before;
            _velocity = Vector3.zero;
            return;
        }

        transform.position = after;
    }

    // Returns true when this is a new sighting (TargetAcquired raised).
    private bool CacheTarget(Transform seen)
    {
        _target = seen;
        _targetAgent = seen.GetComponentInParent<Agent>();
        _targetCapturable = seen.GetComponentInParent<ICapturable>();

        // Re-seeing the same target right after losing it is not a new sighting: no repeated event/log.
        bool isNewSighting = seen != _announcedTarget || Time.time - _targetLostTime >= _reacquireCooldown;
        if (!isNewSighting) return false;

        _announcedTarget = seen;
        TargetAcquired?.Invoke(seen);
        return true;
    }

    private void HandleAlert(FSMAgentWolf source, Vector3 position)
    {
        if (source == this) return;

        AlertReceived?.Invoke(source, position);
        (_stateMachine?.CurrentState as WolfState)?.OnAlert(position);
    }

    private void HandleTargetDropped()
    {
        ClearTarget();
        _announcedTarget = null;
        // The cached scan list may still hold the dropped target for up to one scan interval.
        if (_sensor != null) _sensor.ResetScan();
        TargetDropped?.Invoke();
        (_stateMachine?.CurrentState as WolfState)?.OnTargetDropped();
    }

    private void ValidateSetup()
    {
        // Patrol nodes live in the scene, so a prefab asset cannot hold them: only check scene instances.
        if (gameObject.scene.IsValid() && dataPatrol != null && dataPatrol.nodes != null)
        {
            for (int i = 0; i < dataPatrol.nodes.Count; i++)
            {
                Node node = dataPatrol.nodes[i];
                if (node == null)
                    Debug.LogWarning($"[{name}] Patrol node at index {i} is missing.", this);
                else if (node.IsSafeZone)
                    Debug.LogWarning($"[{name}] Patrol node '{node.name}' is a safe-zone node; wolves must not patrol it.", this);
            }
        }

        PathNavigator navigator = _navigator != null ? _navigator : GetComponent<PathNavigator>();
        if (navigator != null && navigator.Profile != NavigationProfile.Wolf)
            Debug.LogWarning($"[{name}] PathNavigator profile must be Wolf so A* avoids safe-zone nodes.", this);
    }
}
