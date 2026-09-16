using UnityEngine;

public class EvadeState : SteeringState
{
    private EvadeData _data;

    public EvadeState(FSMAgentEscapist agent, EvadeData data, StateMachine stateMachine)
        : base(agent, stateMachine)
    {
        _data = data;
    }

    public override void Enter() {} /*Debug.Log("Entre Evade");*/
    public override void Exit()  {}/*Debug.Log("Sali Evade");*/

    public override void Update()
    {
        Vector3 steering = Evade(_data.Hunter, _data.maxSpeed);

        _agent.ApplyVelocity(steering, _data.maxSpeed);
    }

    
    private Vector3 Evade(Agent target, float maxSpeed)
    {
        var futurePosition = CalculateFuture(target, maxSpeed);

        return Flee(futurePosition, _data.maxSpeed, _data.maxSteering);
    }

    private Vector3 Flee(Vector3 target, float maxSpeed, float maxSteering)
    {
        var desired = DesiredVector(target, maxSpeed);

        return CalculateSteering(-desired, maxSteering);
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
