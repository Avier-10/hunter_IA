using UnityEngine;

// Walks to an eliminated boid, collects it for a fixed time and counts it. Abandoning counts nothing.
public class GatherState : HunterState
{
    private readonly GatherData _data;

    private FSMAgentEscapist _target;
    private float _gatherTimer;
    private bool _isCollecting;

    public GatherState(FSMAgentHunter agent, GatherData data, StateMachine stateMachine)
        : base(agent, stateMachine)
    {
        _data = data;
    }

    protected override void OnEnter()
    {
        _target = Hunter.Boids.NearestEliminated;
        _isCollecting = false;
        _gatherTimer = 0f;
        Hunter.SetFocus(_target != null ? _target.transform : null);
    }

    protected override void OnExit()
    {
        // Leaving mid-collection (Flee, Dead) simply drops the progress.
        _target = null;
        _isCollecting = false;
    }

    public override void Update()
    {
        if (TryGlobalTransitions(HunterTransitions.Flee)) return;

        if (_target == null || !_target.IsEliminated)
        {
            Hunter.ChangeState(HunterStates.Search);
            return;
        }

        if (_isCollecting)
            RunCollection();
        else
            MoveToTarget();
    }

    private void MoveToTarget()
    {
        Vector3 targetPosition = _target.transform.position;

        if (IsWithin(targetPosition, _data.arriveThreshold))
        {
            _isCollecting = true;
            _gatherTimer = _data.gatherDuration;
            Stop();
            return;
        }

        MoveTo(targetPosition, _data.maxSpeed);
    }

    private void RunCollection()
    {
        Stop();

        _gatherTimer -= Time.deltaTime;
        if (_gatherTimer > 0f) return;

        _target.Collect();
        Hunter.AddGather();

        ResumeAfterTask();
    }
}

[System.Serializable]
public class GatherData
{
    public float maxSpeed = 5f;
    public float gatherDuration = 1.5f;
    public float arriveThreshold = 0.5f;
}
