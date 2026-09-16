using System.Collections.Generic;
using UnityEngine;

public class FlockingState : SteeringState
{
    private FlockingData _data;

    public FlockingState(FSMAgentEscapist agent, FlockingData data, StateMachine stateMachine)
        : base(agent, stateMachine)
    {
        _data = data;
    }

    public override void Enter() {}
    public override void Exit()  {}

    public override void Update()
    {
        Vector3 steering = CalculateSeparation() * _data.separationWeight
                          + CalculateAlignment() * _data.alignmentWeight
                          + CalculateCohesion() * _data.cohesionWeight;

       _agent.ApplyVelocity(steering, _data.maxSpeed);
    }

    private Vector3 CalculateSeparation()
    {
        Vector3 desired = default;
        int count = 0;

        foreach (var item in Agent.AllAgents)
        {
            if (item == _agent) continue;
            if (item is not FSMAgentEscapist) continue;

            if (InRange(item.transform.position, _data.separationRadius))
            {
                desired += (item.transform.position - _agent.transform.position);
                count++;
            }
        }

        if (count == 0) return Vector3.zero;
        desired /= count;

        return CalculateSteering(-desired.normalized * _data.maxSpeed, _data.maxSteering);
    }
    private Vector3 CalculateAlignment()
    {
        Vector3 desired = default;
        int count = 0;

        foreach (var item in Agent.AllAgents)
        {
            if (item == _agent) continue;
            if (item is not FSMAgentEscapist) continue;

            if (InRange(item.transform.position, _data.alignmentRadius))
            {
                desired += item.Velocity;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;
        desired /= count;

        return CalculateSteering(desired.normalized * _data.maxSpeed, _data.maxSteering);
    }
    private Vector3 CalculateCohesion()
    {
        Vector3 desired = default;
        int count = 0;

        foreach (var item in Agent.AllAgents)
        {
            if (item == _agent) continue;
            if (item is not FSMAgentEscapist) continue;

            if (InRange(item.transform.position, _data.cohesionRadius))
            {
                desired += item.transform.position;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;
        desired /= count;

        Vector3 target = DesiredVector(desired, _data.maxSpeed);
        return CalculateSteering(target, _data.maxSteering);
    }
}

[System.Serializable]
public class FlockingData
{
    [Header("Steering")]
    public float maxSpeed;
    public float maxSteering;

    [Header("Radio")]
    public float separationRadius = 1.5f; 
    public float cohesionRadius = 4f;
    public float alignmentRadius = 4f;

    [Header("Weights"), Range(0f, 1f)]
    public float separationWeight = 1f;
    [Range(0f, 1f)]
    public float cohesionWeight = 1f;
    [Range(0f, 1f)]
    public float alignmentWeight = 1f;
}
