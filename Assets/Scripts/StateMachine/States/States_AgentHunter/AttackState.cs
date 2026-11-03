using UnityEngine;

// Only entered with the weapon ready and a live boid in sight radius. Melee, range or pursue, by distance.
public class AttackState : HunterState
{
    private readonly AttackData _data;

    private FSMAgentEscapist _target;
    private bool _awaitingImpact;
    private bool _rescanRequested;
    private float _impactTimer;

    public AttackState(FSMAgentHunter agent, AttackData data, StateMachine stateMachine)
        : base(agent, stateMachine)
    {
        _data = data;
    }

    protected override void OnEnter()
    {
        _target = Hunter.Boids.NearestAlive;
        _awaitingImpact = false;
        _rescanRequested = false;
        Hunter.SetFocus(_target != null ? _target.transform : null);
    }

    protected override void OnExit()
    {
        _target = null;
    }

    public override void Update()
    {
        // Attack is the lowest-priority transition, so it is never offered from inside Attack itself.
        if (TryGlobalTransitions(HunterTransitions.Flee | HunterTransitions.Exit | HunterTransitions.Gather)) return;

        if (_awaitingImpact)
        {
            WaitForImpact();
            return;
        }

        // Leaving the vision radius abandons the chase but keeps the weapon cooldown untouched.
        if (_target == null || !_target.IsAlive || !IsWithin(_target.transform.position, Hunter.VisionRadius))
        {
            Hunter.ChangeState(HunterStates.Search);
            return;
        }

        Vector3 targetPosition = _target.transform.position;
        float sqrDistance = FlatSqrDistance(targetPosition);

        if (sqrDistance <= Square(Hunter.MeleeAttackRadius))
        {
            PerformMeleeAttack();
            return;
        }

        bool inSight = HasDirectLine(targetPosition);
        if (inSight && sqrDistance <= Square(Hunter.RangeAttackRadius) && PerformRangeAttack())
            return;

        if (inSight)
        {
            ResetPathMode();
            Drive(Pursuit(_target, _data.maxSpeed, Hunter.MaxSteering), _data.maxSpeed);
            return;
        }

        MoveTo(targetPosition, _data.maxSpeed);
    }

    private void PerformMeleeAttack()
    {
        Stop();

        _target.TakeDamage(_data.damage);

        AttackSuccessful();
    }

    // False when the weapon is not set up; the hunter then keeps closing in for a melee attack.
    private bool PerformRangeAttack()
    {
        if (!Hunter.Shoot(_target)) return false;

        // The bullet is still flying: stay put so the Gather transition fires as soon as the boid drops.
        Stop();
        Hunter.ResetTBA();
        _awaitingImpact = true;
        _rescanRequested = false;
        _impactTimer = _data.impactWait;
        return true;
    }

    // Gather (checked first in Update) takes over once the scanner sees the eliminated boid;
    // if the bullet never lands (target gone, bullet destroyed) the wait times out back to Search.
    private void WaitForImpact()
    {
        Stop();

        if (!_rescanRequested && (_target == null || _target.IsEliminated))
        {
            // The scanner cache is a few frames old; ask for a fresh scan so Gather sees the kill right away.
            _rescanRequested = true;
            Hunter.Boids.Invalidate();
        }

        _impactTimer -= Time.deltaTime;
        if (_impactTimer <= 0f) Hunter.ChangeState(HunterStates.Search);
    }

    private void AttackSuccessful()
    {
        Hunter.ResetTBA();
        Hunter.ChangeState(HunterStates.Search);
    }

    private static float Square(float value) => value * value;
}

[System.Serializable]
public class AttackData
{
    public float maxSpeed = 6f;
    public float damage = FSMAgentEscapist.LethalDamage;
    [Tooltip("Seconds the hunter waits for its bullet to land before giving up and searching again.")]
    public float impactWait = 2f;
}
