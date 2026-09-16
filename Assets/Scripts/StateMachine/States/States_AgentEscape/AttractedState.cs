using UnityEngine;

public class AttractedState : SteeringState
{
    private FSMAgentEscapist _escapist;
    private Trap _target;
    private AttractedData _data;

    private bool _isOccupied;

    public AttractedState(
        FSMAgentEscapist agent,
        AttractedData data,
        StateMachine stateMachine)
        : base(agent, stateMachine)
    {
        _escapist = agent;
        _data = data;
    }

    public void SetTarget(Trap target)
    {
        _target = target;
    }

    public override void Enter()
    {
        _isOccupied = false;

        Debug.Log("Escapist: Entre Attracted");
    }

    public override void Exit()
    {
        if (_isOccupied && _target != null)
        {
            _target.Release(_escapist);
            _isOccupied = false;
        }

        Debug.Log("Escapist: Sali Attracted");
    }

    public override void Update()
    {
        if (_target == null)
        {
            _escapist.ChangeState(EscapistStates.Flocking);
            return;
        }

        if (_isOccupied)
        {
            _escapist.ApplyVelocity(Vector3.zero, 0f);
            return;
        }

        Vector3 direction =
            _target.transform.position - _escapist.transform.position;

        float distance = direction.magnitude;

        if (distance <= _data.arrivalDistance)
        {
            _escapist.ApplyVelocity(Vector3.zero, 0f);

            if (_target.TryOccupy(_escapist))
            {
                _isOccupied = true;
            }
            else
            {
                Debug.Log(
                    $"{_escapist.name} intentó ocupar una trampa ocupada."
                );

                _escapist.ChangeState(EscapistStates.Flocking);
            }

            return;
        }

        Vector3 steering = Arrive(
            _target.transform.position,
            _data.maxSpeed,
            _data.maxSteering,
            _data.slowingDistance
        );

        _escapist.ApplyVelocity(steering, _data.maxSpeed);
    }

    private Vector3 Arrive(
        Vector3 target,
        float maxSpeed,
        float maxSteering,
        float slowingDistance)
    {
        Vector3 direction =
            target - _escapist.transform.position;

        float distance = direction.magnitude;

        if (distance < 0.01f)
            return -_escapist.Velocity;

        float targetSpeed = maxSpeed * Mathf.Clamp01(distance / slowingDistance);

        Vector3 desired = direction.normalized * targetSpeed;

        return CalculateSteering(
            desired,
            maxSteering
        );
    }
}

[System.Serializable]
public class AttractedData
{
    [Header("Steering")]
    public float maxSpeed = 3f;
    public float maxSteering = 3f;
    public float slowingDistance = 1f;

   
    public float arrivalDistance = 0.3f;
    

}
