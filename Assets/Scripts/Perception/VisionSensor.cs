using System;
using System.Collections.Generic;
using UnityEngine;

// Throttled FoV / line-of-sight perception. Targets are discovered only through a physics query,
// so the owner never needs a reference to what it is looking for.
public class VisionSensor : MonoBehaviour
{
    [SerializeField] private float _viewRadius = 10f;
    [Range(0f, 360f)]
    [SerializeField] private float _viewAngle = 90f;
    [SerializeField] private float _eyeHeight = 1f;
    [SerializeField] private LayerMask _targetMask;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private float _scanInterval = 0.15f;
    [SerializeField] private bool _ignoreTargetsInSafeZone;
    [SerializeField] private int _maxTargets = 8;

    private const float FullCircle = 360f;

    private readonly List<Transform> _visible = new List<Transform>();
    private readonly List<Transform> _previous = new List<Transform>();
    private Collider[] _buffer;
    private float _timer;

    public IReadOnlyList<Transform> VisibleTargets => _visible;
    public bool HasTarget => _visible.Count > 0;
    public Transform ClosestTarget { get; private set; }
    public float ViewRadius => _viewRadius;
    public float ViewAngle => _viewAngle;
    public Vector3 EyePosition => transform.position + Vector3.up * _eyeHeight;

    public event Action<Transform> TargetSpotted;
    public event Action<Transform> TargetLost;

    private void Awake()
    {
        EnsureBuffer();
    }

    private void OnEnable()
    {
        // Random offset keeps many sensors from scanning on the same frame.
        _timer = UnityEngine.Random.Range(0f, _scanInterval);
    }

    private void OnDisable()
    {
        _visible.Clear();
        _previous.Clear();
        ClosestTarget = null;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        _timer = _scanInterval;
        Scan();
    }

    // Immediate, un-throttled check for a single target (e.g. "still perceiving while chasing").
    public bool CanSee(Transform target)
    {
        if (target == null) return false;
        return IsVisible(target, EyePosition);
    }

    // Forgets what was seen from the old position (after a teleport) so states do not react to stale targets.
    public void ResetScan()
    {
        _visible.Clear();
        _previous.Clear();
        ClosestTarget = null;
        _timer = _scanInterval;
    }

    private void Scan()
    {
        EnsureBuffer();

        _previous.Clear();
        _previous.AddRange(_visible);
        _visible.Clear();
        ClosestTarget = null;

        Vector3 eye = EyePosition;
        int count = Physics.OverlapSphereNonAlloc(eye, _viewRadius, _buffer, _targetMask, QueryTriggerInteraction.Ignore);

        float closestSqr = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Transform target = ResolveRoot(_buffer[i]);
            if (target == transform || target.IsChildOf(transform)) continue;
            if (_visible.Contains(target)) continue;
            if (!IsVisible(target, eye)) continue;

            _visible.Add(target);

            float sqr = FlatSqrDistance(transform.position, target.position);
            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                ClosestTarget = target;
            }
        }

        RaiseTransitions();
    }

    private void RaiseTransitions()
    {
        for (int i = 0; i < _visible.Count; i++)
            if (!_previous.Contains(_visible[i])) TargetSpotted?.Invoke(_visible[i]);

        for (int i = 0; i < _previous.Count; i++)
            if (!_visible.Contains(_previous[i])) TargetLost?.Invoke(_previous[i]);
    }

    private bool IsVisible(Transform target, Vector3 eye)
    {
        Vector3 targetPosition = target.position;

        if (_ignoreTargetsInSafeZone && Zone.IsInSafeZone(targetPosition)) return false;

        float sqrDistance = FlatSqrDistance(transform.position, targetPosition);
        if (sqrDistance > _viewRadius * _viewRadius) return false;

        if (_viewAngle < FullCircle && !IsInsideCone(targetPosition)) return false;

        // Same height offset on both ends so low ground geometry does not block the ray.
        Vector3 targetEye = targetPosition + Vector3.up * _eyeHeight;
        return !Physics.Linecast(eye, targetEye, _obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private bool IsInsideCone(Vector3 targetPosition)
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;

        // Degenerate cases: target on top of the sensor counts as inside.
        if (forward.sqrMagnitude < Mathf.Epsilon || toTarget.sqrMagnitude < Mathf.Epsilon) return true;

        return Vector3.Angle(forward, toTarget) <= _viewAngle * 0.5f;
    }

    private static Transform ResolveRoot(Collider collider)
    {
        // Bodies made of several child cubes must report one root, not one target per cube.
        Agent agent = collider.GetComponentInParent<Agent>();
        if (agent != null) return agent.transform;

        Rigidbody body = collider.attachedRigidbody;
        return body != null ? body.transform : collider.transform;
    }

    private static float FlatSqrDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y;
        return (a - b).sqrMagnitude;
    }

    private void EnsureBuffer()
    {
        int size = Mathf.Max(1, _maxTargets);
        if (_buffer == null || _buffer.Length != size) _buffer = new Collider[size];
    }
}
