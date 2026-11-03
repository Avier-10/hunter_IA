using UnityEngine;

// Always A*: the professor's rule is that the last known position is reached via the graph, even if visible.
public class WolfGoToLastKnownState : WolfState
{
    private readonly WolfGoToLastKnownData _data;
    private float _elapsed;

    public WolfGoToLastKnownState(FSMAgentWolf agent, WolfGoToLastKnownData data, StateMachine stateMachine)
        : base(agent, stateMachine)
    {
        _data = data;
    }

    public override void Enter()
    {
        _elapsed = 0f;
        Wolf.Navigator.Clear();
        PlanToLastKnown();
    }

    public override void Exit() => Wolf.Navigator.Clear();

    public override void OnAlert(Vector3 lastKnownPosition)
    {
        // Repeated alerts about roughly the same spot must not keep postponing the timeout.
        if (Wolf.HasLastKnownPosition)
        {
            Vector3 delta = lastKnownPosition - Wolf.LastKnownPosition;
            delta.y = 0f;
            float minShift = Wolf.Navigator.RepathDistance;
            if (delta.sqrMagnitude <= minShift * minShift) return;
        }

        Wolf.SetLastKnownPosition(lastKnownPosition);
        _elapsed = 0f;
        PlanToLastKnown();
    }

    public override void Update()
    {
        if (TryEngageTarget()) return;

        if (!Wolf.HasLastKnownPosition)
        {
            Wolf.ChangeState(WolfStates.Patrol);
            return;
        }

        if (IsWithin(Wolf.LastKnownPosition, _data.arrivalRadius) || Wolf.Navigator.HasArrived)
        {
            Wolf.ChangeState(WolfStates.Search);
            return;
        }

        // Unreachable or too slow: give up the walk and look around from here (never go direct).
        _elapsed += Time.deltaTime;
        if (_elapsed >= _data.timeout || Wolf.Navigator.FailedPlanCount >= _data.maxPlanFailures)
        {
            Wolf.ChangeState(WolfStates.Search);
            return;
        }

        MoveAlongPath(_data.maxSpeed);
    }

    private void PlanToLastKnown()
    {
        if (Wolf.HasLastKnownPosition)
            Wolf.Navigator.SetDestination(Wolf.LastKnownPosition);
    }
}

[System.Serializable]
public class WolfGoToLastKnownData
{
    [Header("Steering")]
    public float maxSpeed = 3.5f;
    public float arrivalRadius = 1f;

    [Header("Failure protection")]
    [Tooltip("Seconds before giving up on reaching the last known position and switching to Search.")]
    public float timeout = 15f;
    [Tooltip("Consecutive failed A* plans before switching to Search.")]
    public int maxPlanFailures = 3;
}
