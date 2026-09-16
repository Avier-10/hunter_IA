using UnityEngine;

public abstract class SteeringState : State
{
    protected Agent _agent;

    protected SteeringState(Agent agent, StateMachine stateMachine) : base(stateMachine)
    {
        _agent = agent;
    }

    protected Vector3 DesiredVector(Vector3 target, float maxSpeed)
    {
        Vector3 desired = (target - _agent.transform.position).normalized;
        desired *= maxSpeed;
        return desired;
    }

    protected Vector3 CalculateSteering(Vector3 desired, float maxSteering)
    {
        Vector3 steering = desired - _agent.Velocity;
        steering = Vector3.ClampMagnitude(steering, maxSteering * Time.deltaTime);
        return steering;
    }
    protected Vector3 CalculateFuture(Agent target, float maxSpeed)
    {
        Vector3 direction = target.transform.position - _agent.transform.position;
        float distance = direction.magnitude;

        float prediction = distance / (maxSpeed + target.Velocity.magnitude);

        return target.transform.position + target.Velocity * prediction;
    }

    protected bool InRange(Vector3 pos, float radius)
        => (pos - _agent.transform.position).sqrMagnitude <= radius * radius;

        
}