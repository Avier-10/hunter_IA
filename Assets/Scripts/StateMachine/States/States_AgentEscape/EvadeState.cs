using UnityEngine;

public class EvadeState : SteeringState
{
    private EvadeData _data;

    public EvadeState(FSMAgentEscapist agent, EvadeData data, StateMachine stateMachine)
        : base(agent, stateMachine)
    {
        _data = data;
    }

    public override void Update()
    {
        Vector3 steering = Evade(_data.Hunter, _data.maxSpeed, _data.maxSteering);

        _agent.ApplyVelocity(steering, _data.maxSpeed);
    }
}

[System.Serializable]
public class EvadeData
{
    [Header("Steering")]
    public float maxSpeed;
    public float maxSteering;
    
    [Header("Hunter")]
    public Agent Hunter; 
}
