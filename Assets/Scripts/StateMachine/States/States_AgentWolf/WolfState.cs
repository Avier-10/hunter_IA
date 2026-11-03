using UnityEngine;

// Shared movement/perception helpers and the signal hooks the agent forwards to the active state.
public abstract class WolfState : SteeringState
{
    protected readonly FSMAgentWolf Wolf;

    protected WolfState(FSMAgentWolf agent, StateMachine stateMachine) : base(agent, stateMachine)
    {
        Wolf = agent;
    }

    public virtual void OnAlert(Vector3 lastKnownPosition) { }

    public virtual void OnTargetDropped()
    {
        Wolf.ClearTarget();
        Wolf.ClearLastKnownPosition();
        Wolf.ChangeState(WolfStates.Patrol);
    }

    // Howls only on a genuinely new sighting; the agent's howl cooldown stays as a second guard.
    protected bool TryEngageTarget()
    {
        if (!Wolf.TryPerceiveTarget()) return false;

        if (Wolf.IsNewSighting) Wolf.Howl(Wolf.LastKnownPosition);
        Wolf.ChangeState(WolfStates.Chase);
        return true;
    }

    // Wolves must never step into a safe zone, so every move goes through FSMAgentWolf.Move.
    protected override void Drive(Vector3 steering, float maxSpeed)
    {
        Wolf.Move(steering, maxSpeed);
    }

    protected void MoveToward(Vector3 point, float maxSpeed)
    {
        MoveToward(point, maxSpeed, Wolf.MaxSteering, Wolf.SlowingDistance);
    }

    protected void MoveAlongPath(float maxSpeed)
    {
        MoveAlongPath(Wolf.Navigator, maxSpeed, Wolf.MaxSteering, Wolf.SlowingDistance);
    }
}
