using System.Collections.Generic;
using UnityEngine;

public class WolfPatrolState : WolfState
{
    private readonly WolfPatrolData _data;

    private int _index;
    private int _direction = 1;
    private bool _usingPath;
    // Cached line-of-sight to the current waypoint; invalidated whenever the waypoint changes.
    private ThrottledBool _directLine;

    public WolfPatrolState(FSMAgentWolf agent, WolfPatrolData data, StateMachine stateMachine)
        : base(agent, stateMachine)
    {
        _data = data;
    }

    public override void Enter()
    {
        ResetPathMode();
        _directLine.Invalidate();
        // Coming back from a search, resume at the closest waypoint instead of the stale index.
        _index = NearestWaypointIndex();
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

        List<Node> nodes = _data.nodes;
        if (nodes == null || nodes.Count == 0)
        {
            Stop();
            return;
        }

        Node waypoint = nodes[_index];
        if (waypoint == null)
        {
            Advance();
            return;
        }

        Vector3 waypointPosition = waypoint.Position;
        if (IsWithin(waypointPosition, _data.arrivalDistance))
        {
            Advance();
            return;
        }

        if (HasDirectLine(waypointPosition))
        {
            if (_usingPath) ResetPathMode();
            MoveToward(waypointPosition, _data.maxSpeed);
            return;
        }

        if (!_usingPath)
        {
            Wolf.Navigator.SetDestination(waypointPosition);
            _usingPath = true;
        }
        MoveAlongPath(_data.maxSpeed);
    }

    private bool HasDirectLine(Vector3 point)
    {
        if (!_directLine.IsDue()) return _directLine.Value;

        Graph graph = Graph.Instance;
        bool clear = graph == null || graph.HasLineOfSight(Wolf.transform.position, point);
        return _directLine.Store(clear, _data.lineOfSightInterval);
    }

    private void Advance()
    {
        ResetPathMode();
        _directLine.Invalidate();

        int count = _data.nodes.Count;
        if (count <= 1) return;

        if (!_data.pingPong)
        {
            _index = (_index + 1) % count;
            return;
        }

        int next = _index + _direction;
        if (next < 0 || next >= count)
        {
            _direction = -_direction;
            next = _index + _direction;
        }
        _index = next;
    }

    private void ResetPathMode()
    {
        Wolf.Navigator.Clear();
        _usingPath = false;
    }

    private int NearestWaypointIndex()
    {
        if (_data.nodes == null) return 0;

        int best = 0;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < _data.nodes.Count; i++)
        {
            Node node = _data.nodes[i];
            if (node == null) continue;

            float sqr = FlatSqrDistance(node.Position);
            if (sqr >= bestSqr) continue;

            bestSqr = sqr;
            best = i;
        }
        return best;
    }
}

[System.Serializable]
public class WolfPatrolData
{
    [Header("Route")]
    public List<Node> nodes = new List<Node>();
    [Tooltip("True: walk back in reverse at the ends. False: loop to the first node.")]
    public bool pingPong;
    public float arrivalDistance = 0.75f;
    [Tooltip("Seconds between line-of-sight checks toward the current waypoint.")]
    public float lineOfSightInterval = 0.15f;

    [Header("Steering")]
    public float maxSpeed = 2.5f;
}
