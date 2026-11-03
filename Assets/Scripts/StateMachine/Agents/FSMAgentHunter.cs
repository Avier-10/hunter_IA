using System;
using System.Collections.Generic;
using UnityEngine;

public enum HunterStates
{
    Search,
    Attack,
    Gather,
    Flee,
    Exit,
    Dead
}

// Holds components, shared tuning, perception caches and gather bookkeeping.
// Every transition between states is decided inside the states (see HunterState).
[RequireComponent(typeof(VisionSensor), typeof(PathNavigator))]
public class FSMAgentHunter : Agent, ICapturable
{
    [Header("Data")]
    [SerializeField] private SearchData dataSearch;
    [SerializeField] private AttackData dataAttack;
    [SerializeField] private GatherData dataGather;
    [SerializeField] private FleeData dataFlee;
    [SerializeField] private ExitData dataExit;

    [Header("Rules")]
    [SerializeField] private int _requiredGathers = 3;
    [SerializeField] private float _respawnTime = 5f;
    [SerializeField] private float _safeZoneWaitTime = 3f;

    [Header("Perception")]
    [SerializeField] private float _visionRadius = 8f;
    [Tooltip("Seconds between boid scans.")]
    [SerializeField] private float _boidScanInterval = 0.2f;
    [Tooltip("Seconds between line-of-sight checks while moving toward a boid.")]
    [SerializeField] private float _directLineInterval = 0.15f;

    [Header("Combat")]
    [SerializeField] private float _meleeAttackRadius = 1f;
    [SerializeField] private float _rangeAttackRadius = 6f;
    [Tooltip("Time between attacks (seconds).")]
    [SerializeField] private float _tba = 5f;

    [Header("Movement")]
    [SerializeField] private float _maxSteering = 10f;
    [SerializeField] private float _slowingDistance = 1f;

    [Header("Weapon")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _bulletPrefab;

    private StateMachine _stateMachine;
    private VisionSensor _sensor;
    private PathNavigator _navigator;
    private HunterBoidScanner _boids;
    private Renderer[] _renderers;
    private Collider[] _colliders;

    private HunterStates _current;
    private bool _hasState;
    private float _tbaTimer;
    private int _gatherCount;

    private bool _hasFocus;
    private bool _focusIsTransform;
    private Transform _focusTransform;
    private Vector3 _focusPoint;

    // Interest objects (traps) this hunter spawned and that still exist.
    private readonly List<Trap> _activeObjects = new List<Trap>();

    protected override bool UsesBounds => false;

    public HunterStates CurrentState => _current;
    public VisionSensor Sensor => _sensor;
    public PathNavigator Navigator => _navigator;
    public HunterBoidScanner Boids => _boids;

    public int RequiredGathers => _requiredGathers;
    public int GatherCount => _gatherCount;
    public int ActiveObjectCount => _activeObjects.Count;
    public bool HasEnoughGathers => _gatherCount >= _requiredGathers;
    public float RespawnTime => _respawnTime;
    public float SafeZoneWaitTime => _safeZoneWaitTime;
    public float VisionRadius => _visionRadius;
    public float DirectLineInterval => _directLineInterval;
    public float MeleeAttackRadius => _meleeAttackRadius;
    public float RangeAttackRadius => _rangeAttackRadius;
    public float TBA => _tba;
    public float MaxSteering => _maxSteering;
    public float SlowingDistance => _slowingDistance;

    public bool IsPresent { get; private set; } = true;
    public bool WolfPerceived => _sensor != null && _sensor.HasTarget;
    public Zone ChosenSafeZone { get; private set; }

    // Read by HunterAmmoUI. HasBullet is simply "time between attacks has elapsed".
    public bool HasBullet => _tbaTimer <= 0f;

    public event Action<HunterStates, HunterStates> StateChanged;
    public event Action<Zone, float> SafeZoneChosen;

    protected override void Awake()
    {
        base.Awake();

        _sensor = GetComponent<VisionSensor>();
        _navigator = GetComponent<PathNavigator>();
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
        _boids = new HunterBoidScanner(_boidScanInterval);

        ValidateSetup();

        _stateMachine = new StateMachine();
        _stateMachine.RegisterState(HunterStates.Search, new SearchState(this, dataSearch, _stateMachine));
        _stateMachine.RegisterState(HunterStates.Attack, new AttackState(this, dataAttack, _stateMachine));
        _stateMachine.RegisterState(HunterStates.Gather, new GatherState(this, dataGather, _stateMachine));
        _stateMachine.RegisterState(HunterStates.Flee, new FleeState(this, dataFlee, _stateMachine));
        _stateMachine.RegisterState(HunterStates.Exit, new ExitState(this, dataExit, _stateMachine));
        _stateMachine.RegisterState(HunterStates.Dead, new DeadState(this, _stateMachine));
    }

    // Initial state is entered here so listeners subscribed in OnEnable see it and the Graph singleton exists.
    private void Start()
    {
        ChangeState(HunterStates.Search);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        UpdateReload(deltaTime);

        if (IsPresent)
            _boids.Tick(transform.position, _visionRadius, deltaTime);

        _stateMachine.Update();
    }

    private void OnValidate()
    {
        ValidateSetup();
    }

    public void ChangeState(HunterStates next)
    {
        if (_stateMachine == null || (_hasState && _current == next)) return;

        HunterStates previous = _current;
        bool hadState = _hasState;
        _current = next;
        _hasState = true;

        _stateMachine.ChangeState(next);

        StateChanged?.Invoke(hadState ? previous : next, next);
    }

    // The only external entry into the FSM: a wolf's capture is a stimulus, not a decision the hunter makes,
    // so it forces Dead (top priority) from whatever state is running. Everything else is decided in the states.
    public void Capture()
    {
        if (_current == HunterStates.Dead) return;

        ChangeState(HunterStates.Dead);
        HunterSignals.RaiseHunterDied();
    }

    // Hides the hunter from wolves and cameras without deactivating the object, so the FSM keeps ticking.
    public void SetPresent(bool present)
    {
        IsPresent = present;

        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].enabled = present;

        for (int i = 0; i < _colliders.Length; i++)
            if (_colliders[i] != null) _colliders[i].enabled = present;
    }

    // Shared by Dead (after the respawn wait) and Exit (after leaving the map): back to Start with a clean slate.
    public void ReturnToStart()
    {
        Vector3 position = transform.position;
        if (Zone.TryGet(ZoneKind.Start, out Zone start))
        {
            Vector3 center = start.Center;
            position = new Vector3(center.x, position.y, center.z);
        }
        else
        {
            Debug.LogWarning($"[{name}] No Start zone found; respawning in place.", this);
        }

        transform.position = position;
        StopMoving();
        _navigator.Clear();
        _boids.Invalidate();
        _sensor.ResetScan();
        PruneActiveObjects();

        _gatherCount = 0;
        _tbaTimer = 0f;
    }

    public void StopMoving()
    {
        _velocity = Vector3.zero;
    }

    public void AddGather()
    {
        _gatherCount++;
    }

    public void RegisterActiveObject(Trap trap)
    {
        _activeObjects.Add(trap);
    }

    // Drops the interest objects that were consumed (destroyed) since the last call.
    public void PruneActiveObjects()
    {
        for (int i = _activeObjects.Count - 1; i >= 0; i--)
            if (_activeObjects[i] == null) _activeObjects.RemoveAt(i);
    }

    public void ReportSafeZoneChosen(Zone zone, float pathLength)
    {
        ChosenSafeZone = zone;
        SafeZoneChosen?.Invoke(zone, pathLength);
    }

    // Debug-only: what the hunter is currently heading for (drawn by HunterFeedback).
    public void SetFocus(Transform target)
    {
        _focusTransform = target;
        _focusIsTransform = true;
        _hasFocus = target != null;
    }

    public void SetFocus(Vector3 point)
    {
        _focusTransform = null;
        _focusIsTransform = false;
        _focusPoint = point;
        _hasFocus = true;
    }

    public void ClearFocus()
    {
        _focusTransform = null;
        _focusIsTransform = false;
        _hasFocus = false;
    }

    public bool TryGetFocus(out Vector3 point)
    {
        point = _focusPoint;
        if (!_hasFocus) return false;
        if (!_focusIsTransform) return true;

        // A tracked transform can be destroyed meanwhile (collected boid): nothing to draw then.
        if (_focusTransform == null) return false;

        point = _focusTransform.position;
        return true;
    }

    public void ResetTBA()
    {
        _tbaTimer = _tba;
    }

    // Returns false when the weapon is not ready or not set up, so the caller can fall back to melee.
    public bool Shoot(FSMAgentEscapist target)
    {
        if (!HasBullet || target == null || _bulletPrefab == null || _firePoint == null)
            return false;

        GameObject bulletObject = Instantiate(_bulletPrefab, _firePoint.position, _firePoint.rotation);

        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet == null)
        {
            Destroy(bulletObject);
            return false;
        }

        bullet.Initialize(target);
        return true;
    }

    private void UpdateReload(float deltaTime)
    {
        if (_tbaTimer > 0f)
            _tbaTimer = Mathf.Max(0f, _tbaTimer - deltaTime);
    }

    private void ValidateSetup()
    {
        PathNavigator navigator = _navigator != null ? _navigator : GetComponent<PathNavigator>();
        if (navigator != null && navigator.Profile != NavigationProfile.Hunter)
            Debug.LogWarning($"[{name}] PathNavigator profile must be Hunter so A* may use safe-zone nodes.", this);
    }
}
