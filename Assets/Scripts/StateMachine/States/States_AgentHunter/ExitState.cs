using UnityEngine;

// Leaves the map once enough boids were gathered. Only Flee (and the external Dead) can interrupt it.
public class ExitState : HunterState
{
    private readonly ExitData _data;

    private Vector3 _goal;
    private bool _hasGoal;

    public ExitState(FSMAgentHunter agent, ExitData data, StateMachine stateMachine)
        : base(agent, stateMachine)
    {
        _data = data;
    }

    protected override void OnEnter()
    {
        _hasGoal = TryPickGoal(out _goal);
        if (_hasGoal) Hunter.SetFocus(_goal);
        else Debug.LogWarning("[Hunter] No Exit zone found.", Hunter);
    }

    public override void Update()
    {
        if (TryGlobalTransitions(HunterTransitions.Flee)) return;

        if (Zone.IsInside(ZoneKind.Exit, _agent.transform.position))
        {
            // Leaves and re-enters the map in the Start zone with a clean slate.
            Hunter.ReturnToStart();
            Hunter.ChangeState(HunterStates.Search);
            return;
        }

        if (!_hasGoal)
        {
            Stop();
            return;
        }

        TravelTo(_goal, _data.maxSpeed);
    }

    // Prefers a graph node inside the zone so A* can reach it; falls back to the zone center.
    private bool TryPickGoal(out Vector3 goal)
    {
        goal = default;

        if (ZoneNodes.TryGetNearest(ZoneKind.Exit, _agent.transform.position, Hunter.Navigator.Profile, out Node node))
        {
            goal = node.Position;
            return true;
        }

        if (!Zone.TryGet(ZoneKind.Exit, out Zone zone)) return false;

        goal = zone.Center;
        return true;
    }
}

[System.Serializable]
public class ExitData
{
    public float maxSpeed = 6f;
}
