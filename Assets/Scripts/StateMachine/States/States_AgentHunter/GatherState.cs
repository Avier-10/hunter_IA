using UnityEngine;

public class GatherState : SteeringState
{
    private FSMAgentHunter _hunter;
    private GatherData _data;
    private FSMAgentEscapist _target;
    
    
    private float _gatherTimer;
    private bool _isCollecting;

    public GatherState(FSMAgentHunter agent, GatherData data, StateMachine stateMachine)
        : base(agent, stateMachine)
    {
        _hunter = agent;
        _data = data;
    }

    public override void Enter()
    {
        _target = _hunter.FindNearestEliminatedBoid(
            _data.perceptionRadius
        );

        _isCollecting = false;
        _gatherTimer = 0f;
        Debug.Log("Hunter: ENTRE GATHER");
    }

    public override void Exit() => Debug.Log("Hunter: SALGO GATHER");

    public override void Update()
    {
        if (_target == null || !_target.IsEliminated)
        {
            _hunter.ChangeState(HunterStates.Patrol);
            return;
        }

        if (!_isCollecting)
            MoveToTarget();
        else
            RunCollection();
       
    }

    private void MoveToTarget()
    {
        float distance = Vector3.Distance(_agent.transform.position, _target.transform.position);

        if (distance <= _data.arriveThreshold)
        {
            _isCollecting = true;
            _gatherTimer = _data.gatherDuration;

            _agent.ApplyVelocity(Vector3.zero, 0f);
            return;
        }

        Vector3 steering = Seek(_target.transform.position, _data.maxSpeed, _data.maxSteering);
       
        _agent.ApplyVelocity(steering, _data.maxSpeed);
    }

    private void RunCollection()
    {
        _gatherTimer -= Time.deltaTime;

        if (_gatherTimer <= 0f)
        {
            _target.Collectible();

            Debug.Log("Hunter: Boid recolectado");

            _hunter.ChangeState(HunterStates.Patrol);
        }
    }

    private Vector3 Seek(Vector3 target, float maxSpeed, float maxSteering)
    {
        var desired = DesiredVector(target, maxSpeed);

        return CalculateSteering(desired, maxSteering);
    }
}

[System.Serializable]
public class GatherData
{
    public float maxSpeed = 3f;
    public float maxSteering = 4f;
    public float gatherDuration = 1.5f;
    public float arriveThreshold = 0.5f;
    public float perceptionRadius = 8f;
}