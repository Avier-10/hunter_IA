using UnityEngine;

public class WolfChaseState : WolfState
{
    private readonly WolfChaseData _data;
    private float _unseenTime;
    // Throttled Sensor.CanSee for the current target (reset on Enter).
    private ThrottledBool _canSee;

    public WolfChaseState(FSMAgentWolf agent, WolfChaseData data, StateMachine stateMachine)
        : base(agent, stateMachine)
    {
        _data = data;
    }

    public override void Enter()
    {
        _unseenTime = 0f;
        _canSee.Invalidate();
        Wolf.Navigator.Clear();
    }

    public override void Exit() => Wolf.Navigator.Clear();

    public override void Update()
    {
        Transform target = Wolf.Target;

        // A target inside a safe zone is lost at once (no grace): the last known position is where it vanished.
        bool inSafeZone = target != null && Zone.IsInSafeZone(target.position);

        if (target == null || inSafeZone || !CanSeeTarget(target))
        {
            // Last known position was refreshed every frame the target was visible.
            _unseenTime += Time.deltaTime;
            if (target == null || inSafeZone || _unseenTime > _data.lostSightGrace)
            {
                Wolf.ClearTarget();
                Wolf.ChangeState(WolfStates.GoToLastKnown);
                return;
            }

            // Within grace: head for the last seen position through A* (never direct movement to it).
            Wolf.Navigator.SetDestination(Wolf.LastKnownPosition);
            MoveAlongPath(_data.maxSpeed);
            return;
        }

        _unseenTime = 0f;
        Wolf.Navigator.Clear();
        Wolf.SetLastKnownPosition(target.position);

        if (TryCapture(target)) return;

        // Target is in sight, so steering (not A*) is allowed.
        Agent targetAgent = Wolf.TargetAgent;
        Vector3 aim = targetAgent != null
            ? CalculateFuture(targetAgent, _data.maxSpeed)
            : target.position;

        MoveToward(aim, _data.maxSpeed);
    }

    private bool CanSeeTarget(Transform target)
    {
        if (!_canSee.IsDue()) return _canSee.Value;

        return _canSee.Store(Wolf.Sensor.CanSee(target), _data.sightCheckInterval);
    }

    private bool TryCapture(Transform target)
    {
        ICapturable capturable = Wolf.TargetCapturable;
        if (capturable == null || !IsWithin(target.position, Wolf.CaptureRadius)) return false;

        capturable.Capture();
        Wolf.ClearTarget();
        Wolf.ClearLastKnownPosition();
        // The captured target must not be re-engaged from the stale scan list.
        Wolf.Sensor.ResetScan();
        Wolf.ChangeState(WolfStates.Patrol);
        return true;
    }
}

[System.Serializable]
public class WolfChaseData
{
    [Header("Steering")]
    public float maxSpeed = 4f;

    [Header("Perception")]
    [Tooltip("Seconds the target may be out of sight (FoV edge, brief occlusion) before the chase ends.")]
    public float lostSightGrace = 0.5f;
    [Tooltip("Seconds between visibility checks on the chased target (FoV and occlusion).")]
    public float sightCheckInterval = 0.1f;
}
