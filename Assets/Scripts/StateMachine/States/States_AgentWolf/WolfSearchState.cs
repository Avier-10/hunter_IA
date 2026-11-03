using UnityEngine;

public class WolfSearchState : WolfState
{
    private readonly WolfSearchData _data;

    private float _timer;
    private float _scanTimer;
    private bool _scanning;
    private Vector3 _stopPosition;

    public WolfSearchState(FSMAgentWolf agent, WolfSearchData data, StateMachine stateMachine)
        : base(agent, stateMachine)
    {
        _data = data;
    }

    public override void Enter()
    {
        _timer = _data.searchDuration;
        Wolf.Navigator.Clear();
        BeginMove();
    }

    public override void Exit() => Wolf.Navigator.Clear();

    public override void OnAlert(Vector3 lastKnownPosition)
    {
        Wolf.SetLastKnownPosition(lastKnownPosition);
        Wolf.ChangeState(WolfStates.GoToLastKnown);
    }

    public override void Update()
    {
        if (TryEngageTarget()) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            // Nothing found: forget the position and walk back to the route.
            Wolf.ClearLastKnownPosition();
            Wolf.ClearTarget();
            Wolf.ChangeState(WolfStates.Patrol);
            return;
        }

        if (_scanning)
        {
            Scan();
            return;
        }

        if (IsWithin(_stopPosition, _data.arrivalDistance))
        {
            BeginScan();
            return;
        }

        MoveAlongPath(_data.maxSpeed);
    }

    private void Scan()
    {
        Stop();
        Wolf.transform.Rotate(0f, _data.scanTurnSpeed * Time.deltaTime, 0f);

        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0f) BeginMove();
    }

    private void BeginScan()
    {
        _scanning = true;
        _scanTimer = _data.scanDuration;
        Wolf.Navigator.Clear();
    }

    private void BeginMove()
    {
        _scanning = false;

        // An unreachable stop is replaced by another one; the cap keeps this from looping.
        int attempts = Mathf.Max(1, _data.maxStopAttempts);
        for (int i = 0; i < attempts; i++)
        {
            if (!TryPickStop(out _stopPosition)) break;

            // Clear first: SetDestination would ignore a nearby destination while one is already set.
            Wolf.Navigator.Clear();
            Wolf.Navigator.SetDestination(_stopPosition);
            if (!Wolf.Navigator.IsPlanFailed) return;
        }

        // No reachable stop: look around from here and fall through to the normal end-of-search flow.
        BeginScan();
    }

    // Single-pass reservoir sampling keeps node selection allocation-free.
    private bool TryPickStop(out Vector3 position)
    {
        position = default;

        Graph graph = Graph.Instance;
        if (graph == null || !Wolf.HasLastKnownPosition) return false;

        NavigationProfile profile = Wolf.Navigator.Profile;
        Vector3 center = Wolf.LastKnownPosition;
        float radiusSqr = _data.searchRadius * _data.searchRadius;
        int candidates = 0;

        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            Node node = graph.Nodes[i];
            if (!profile.CanUse(node) || Zone.IsInSafeZone(node.Position)) continue;

            Vector3 flat = node.Position - center;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSqr) continue;
            if (IsWithin(node.Position, _data.arrivalDistance)) continue;

            candidates++;
            if (Random.Range(0, candidates) != 0) continue;

            position = node.Position;
        }

        return candidates > 0;
    }
}

[System.Serializable]
public class WolfSearchData
{
    [Header("Search")]
    public float searchDuration = 8f;
    public float searchRadius = 10f;
    public float scanDuration = 1.5f;
    [Tooltip("Degrees per second while looking around in place.")]
    public float scanTurnSpeed = 120f;
    public float arrivalDistance = 0.75f;
    [Tooltip("Stops tried per move before giving up and scanning in place when the picked stops are unreachable.")]
    public int maxStopAttempts = 3;

    [Header("Steering")]
    public float maxSpeed = 3.5f;
}
