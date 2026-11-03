using System.Collections.Generic;
using UnityEngine;

// Plans and tracks a node path; it never moves the agent. States feed GetNextPoint() into steering.
public class PathNavigator : MonoBehaviour
{
    [SerializeField] private NavigationProfile _profile;
    [SerializeField] private float _nodeArrivalDistance = 0.75f;
    [SerializeField] private float _repathDistance = 2f;
    [SerializeField] private float _minRepathInterval = 0.5f;
    [Tooltip("Seconds before a failed A* plan is retried while a destination is set.")]
    [SerializeField] private float _planRetryInterval = 0.5f;
    [Tooltip("Cap for the retry interval, which doubles after each consecutive failed plan.")]
    [SerializeField] private float _planRetryMaxInterval = 4f;
    [Tooltip("Seconds between line-of-sight checks for the final leg to the destination.")]
    [SerializeField] private float _losCheckInterval = 0.15f;
    [SerializeField] private bool _drawGizmos = true;
    [SerializeField] private Color _pathColor = Color.cyan;

    // Lowest allowed retry interval, so a zero in the Inspector cannot replan every frame.
    private const float MinRetryInterval = 0.01f;

    private readonly List<Node> _path = new List<Node>();
    private ThrottledBool _finalLegClear;
    private Vector3 _destination;
    private bool _hasDestination;
    private int _index;
    private float _lastPlanTime;
    private bool _planFailed;
    private int _failedPlanCount;
    private float _currentRetryInterval;
    // True once the path is exhausted at its last node with no line of sight to the destination.
    private bool _endedAtLastNode;

    public NavigationProfile Profile => _profile;
    public IReadOnlyList<Node> Path => _path;
    public bool HasPath => _path.Count > 0;
    public bool IsPlanFailed => _planFailed;
    // Consecutive failed plans for the current destination; reset by a successful plan or Clear().
    public int FailedPlanCount => _failedPlanCount;
    public float RepathDistance => _repathDistance;
    private float BaseRetryInterval => Mathf.Max(MinRetryInterval, _planRetryInterval);
    // Arrived at the destination, or at the last node when the destination is not reachable in a straight line.
    public bool HasArrived => _hasDestination
        && (_endedAtLastNode || FlatSqrDistance(transform.position, _destination) <= _nodeArrivalDistance * _nodeArrivalDistance);

    public void SetDestination(Vector3 target)
    {
        if (_hasDestination)
        {
            bool movedEnough = FlatSqrDistance(target, _destination) > _repathDistance * _repathDistance;
            bool intervalElapsed = Time.time - _lastPlanTime >= _minRepathInterval;
            if (!movedEnough || !intervalElapsed) return;
        }

        _destination = target;
        _hasDestination = true;
        Plan();
    }

    // A failed plan would otherwise leave the agent idle until the destination moves; retry on a timer.
    private void Update()
    {
        if (!_hasDestination || !_planFailed) return;
        if (Time.time - _lastPlanTime < _currentRetryInterval) return;

        Plan();
    }

    private void Plan()
    {
        _lastPlanTime = Time.time;
        _index = 0;
        _endedAtLastNode = false;
        _finalLegClear.Invalidate();

        bool success = Graph.Instance != null
            && Graph.Instance.TryGetPath(transform.position, _destination, _profile, _path);
        if (!success) _path.Clear();
        else SkipBacktrackNodes();

        _planFailed = !success;
        _failedPlanCount = success ? 0 : _failedPlanCount + 1;

        // Exponential backoff between retries of a failing plan; a success restores the base interval.
        float baseInterval = BaseRetryInterval;
        _currentRetryInterval = success || _failedPlanCount <= 1
            ? baseInterval
            : Mathf.Min(_currentRetryInterval * 2f, Mathf.Max(baseInterval, _planRetryMaxInterval));
    }

    // A* starts from the node closest to the agent, which can lie behind it (even where a threat stands).
    // While the agent already sees the next node, the first one is dead weight: drop it so the agent never walks back to it.
    private void SkipBacktrackNodes()
    {
        Graph graph = Graph.Instance;
        if (graph == null) return;

        while (_path.Count >= 2 && graph.HasLineOfSight(transform.position, _path[1].Position))
            _path.RemoveAt(0);
    }

    public Vector3 GetNextPoint()
    {
        if (!_hasDestination) return transform.position;

        float arrivalSqr = _nodeArrivalDistance * _nodeArrivalDistance;
        while (_index < _path.Count && FlatSqrDistance(transform.position, _path[_index].Position) <= arrivalSqr)
            _index++;

        if (_index < _path.Count) return _path[_index].Position;

        // Final leg straight to the destination only with a clear line; otherwise stop at the last node.
        // The check is cached for _losCheckInterval so it is not a physics cast every frame.
        Graph graph = Graph.Instance;
        if (_path.Count > 0 && graph != null && !HasClearFinalLeg(graph))
        {
            _endedAtLastNode = true;
            return _path[_path.Count - 1].Position;
        }

        _endedAtLastNode = false;
        return _destination;
    }

    public void Clear()
    {
        _path.Clear();
        _index = 0;
        _hasDestination = false;
        _planFailed = false;
        _failedPlanCount = 0;
        _currentRetryInterval = BaseRetryInterval;
        _endedAtLastNode = false;
        _finalLegClear.Invalidate();
    }

    private bool HasClearFinalLeg(Graph graph)
    {
        if (!_finalLegClear.IsDue()) return _finalLegClear.Value;

        return _finalLegClear.Store(graph.HasLineOfSight(transform.position, _destination), _losCheckInterval);
    }

    private static float FlatSqrDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y;
        return (a - b).sqrMagnitude;
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos || !_hasDestination) return;

        Gizmos.color = _pathColor;
        Vector3 previous = transform.position;
        for (int i = _index; i < _path.Count; i++)
        {
            Gizmos.DrawLine(previous, _path[i].Position);
            previous = _path[i].Position;
        }
        Gizmos.DrawLine(previous, _destination);
        Gizmos.DrawWireSphere(_destination, _nodeArrivalDistance);
    }
}
