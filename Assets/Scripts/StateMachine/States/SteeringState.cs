using UnityEngine;

public abstract class SteeringState : State
{
    // Closer than this the target counts as reached; avoids normalising a near-zero vector.
    private const float ArriveStopDistance = 0.01f;

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

        float combinedSpeed = maxSpeed + target.Velocity.magnitude;
        // Both agents standing still: there is nothing to predict (and the division would be 0/0).
        if (combinedSpeed <= Mathf.Epsilon)
            return target.transform.position;

        float prediction = distance / combinedSpeed;

        return target.transform.position + target.Velocity * prediction;
    }

    protected bool InRange(Vector3 pos, float radius)
        => (pos - _agent.transform.position).sqrMagnitude <= radius * radius;

    // Steering primitives. They return a steering force; the caller decides how to apply it.

    protected Vector3 Seek(Vector3 target, float maxSpeed, float maxSteering)
    {
        var desired = DesiredVector(target, maxSpeed);

        return CalculateSteering(desired, maxSteering);
    }

    protected Vector3 Flee(Vector3 target, float maxSpeed, float maxSteering)
    {
        var desired = DesiredVector(target, maxSpeed);

        return CalculateSteering(-desired, maxSteering);
    }

    protected Vector3 Arrive(Vector3 target, float maxSpeed, float maxSteering, float slowingDistance)
    {
        Vector3 direction = target - _agent.transform.position;

        float distance = direction.magnitude;

        if (distance < ArriveStopDistance)
            return -_agent.Velocity;

        float slowing = Mathf.Max(slowingDistance, Mathf.Epsilon);
        float targetSpeed = maxSpeed * Mathf.Clamp01(distance / slowing);

        Vector3 desired = direction.normalized * targetSpeed;

        return CalculateSteering(desired, maxSteering);
    }

    protected Vector3 Pursuit(Agent target, float maxSpeed, float maxSteering)
    {
        var futurePosition = CalculateFuture(target, maxSpeed);

        return Seek(futurePosition, maxSpeed, maxSteering);
    }

    protected Vector3 Evade(Agent target, float maxSpeed, float maxSteering)
    {
        var futurePosition = CalculateFuture(target, maxSpeed);

        return Flee(futurePosition, maxSpeed, maxSteering);
    }

    // Helpers for agents that walk on the floor and follow graph paths (wolves, hunter).

    // Flat movement; WolfState overrides it to route through FSMAgentWolf.Move.
    protected virtual void Drive(Vector3 steering, float maxSpeed)
    {
        steering.y = 0f;
        _agent.ApplyVelocity(steering, maxSpeed);
    }

    protected void MoveToward(Vector3 point, float maxSpeed, float maxSteering, float slowingDistance)
    {
        if (FlatSqrDistance(point) <= Mathf.Epsilon)
        {
            Stop();
            return;
        }

        point.y = _agent.transform.position.y;
        Drive(Arrive(point, maxSpeed, maxSteering, slowingDistance), maxSpeed);
    }

    // A* only: holds still when the navigator has no path instead of cutting straight to the goal.
    protected void MoveAlongPath(PathNavigator navigator, float maxSpeed, float maxSteering, float slowingDistance)
    {
        if (navigator == null || !navigator.HasPath)
        {
            Stop();
            return;
        }

        MoveToward(navigator.GetNextPoint(), maxSpeed, maxSteering, slowingDistance);
    }

    protected void Stop()
    {
        Drive(Vector3.zero, 0f);
    }

    protected float FlatSqrDistance(Vector3 point)
    {
        Vector3 delta = point - _agent.transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude;
    }

    protected bool IsWithin(Vector3 point, float radius)
    {
        return FlatSqrDistance(point) <= radius * radius;
    }
}
