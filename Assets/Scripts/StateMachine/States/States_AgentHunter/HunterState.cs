using UnityEngine;

// Transitions a state is allowed to take through HunterState.TryGlobalTransitions.
// The check itself always runs in the fixed priority order Flee > Exit > Gather > Attack.
[System.Flags]
public enum HunterTransitions
{
    None = 0,
    Flee = 1,
    Exit = 2,
    Gather = 4,
    Attack = 8,
    All = Flee | Exit | Gather | Attack
}

// Common base of the hunter states: agent reference, the shared ordered transition check
// and movement helpers (steering when the target is in direct sight, A* otherwise).
public abstract class HunterState : SteeringState
{
    protected readonly FSMAgentHunter Hunter;

    private bool _usingPath;
    // Cached line-of-sight to the state's single movement target; refreshed at FSMAgentHunter.DirectLineInterval.
    private ThrottledBool _directLine;

    protected HunterState(FSMAgentHunter agent, StateMachine stateMachine) : base(agent, stateMachine)
    {
        Hunter = agent;
    }

    public sealed override void Enter()
    {
        ResetPathMode();
        _directLine.Invalidate();
        OnEnter();
    }

    public sealed override void Exit()
    {
        ResetPathMode();
        Hunter.ClearFocus();
        OnExit();
    }

    protected virtual void OnEnter() { }
    protected virtual void OnExit() { }

    // Evaluates the transitions in priority order (Dead is external, see FSMAgentHunter.Capture).
    // Each state passes only the ones that make sense for it.
    protected bool TryGlobalTransitions(HunterTransitions allowed)
    {
        if (Allows(allowed, HunterTransitions.Flee) && Hunter.WolfPerceived)
            return SwitchTo(HunterStates.Flee);

        if (Allows(allowed, HunterTransitions.Exit) && Hunter.HasEnoughGathers)
            return SwitchTo(HunterStates.Exit);

        if (Allows(allowed, HunterTransitions.Gather) && Hunter.Boids.NearestEliminated != null)
            return SwitchTo(HunterStates.Gather);

        if (Allows(allowed, HunterTransitions.Attack) && Hunter.HasBullet && Hunter.Boids.NearestAlive != null)
            return SwitchTo(HunterStates.Attack);

        return false;
    }

    // Where a state hands over once its own work is done: Exit if enough boids were gathered, else Search.
    protected void ResumeAfterTask()
    {
        Hunter.ChangeState(Hunter.HasEnoughGathers ? HunterStates.Exit : HunterStates.Search);
    }

    // Throttled: the cast runs at most once per interval, so call it with the same target throughout a state.
    protected bool HasDirectLine(Vector3 point)
    {
        if (!_directLine.IsDue()) return _directLine.Value;

        Graph graph = Graph.Instance;
        bool clear = graph == null || graph.HasLineOfSight(_agent.transform.position, point);
        return _directLine.Store(clear, Hunter.DirectLineInterval);
    }

    // Trips to the Coto, a safe zone or the exit: always A*, never a straight-line shortcut.
    protected void TravelTo(Vector3 point, float maxSpeed)
    {
        TravelTo(point, maxSpeed, Hunter.MaxSteering);
    }

    // The steering limit is explicit so urgent trips (fleeing) can turn around faster than a calm wander.
    protected void TravelTo(Vector3 point, float maxSpeed, float maxSteering)
    {
        _usingPath = true;
        Hunter.Navigator.SetDestination(point);
        MoveAlongPath(Hunter.Navigator, maxSpeed, maxSteering, Hunter.SlowingDistance);
    }

    // Only for boid targets: steering when the point is in direct sight, A* over the graph otherwise.
    protected void MoveTo(Vector3 point, float maxSpeed)
    {
        if (HasDirectLine(point))
        {
            ResetPathMode();
            SteerTo(point, maxSpeed);
            return;
        }

        _usingPath = true;
        Hunter.Navigator.SetDestination(point);
        FollowPath(maxSpeed);
    }

    protected void SteerTo(Vector3 point, float maxSpeed)
    {
        MoveToward(point, maxSpeed, Hunter.MaxSteering, Hunter.SlowingDistance);
    }

    protected void FollowPath(float maxSpeed)
    {
        MoveAlongPath(Hunter.Navigator, maxSpeed, Hunter.MaxSteering, Hunter.SlowingDistance);
    }

    protected void ResetPathMode()
    {
        if (_usingPath || Hunter.Navigator.HasPath) Hunter.Navigator.Clear();
        _usingPath = false;
    }

    private bool SwitchTo(HunterStates state)
    {
        Hunter.ChangeState(state);
        return true;
    }

    private static bool Allows(HunterTransitions allowed, HunterTransitions transition)
    {
        return (allowed & transition) != 0;
    }
}
