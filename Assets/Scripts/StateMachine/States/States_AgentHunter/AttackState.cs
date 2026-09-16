using UnityEngine;
public class AttackState : SteeringState
{
    private FSMAgentHunter _hunter;
    private AttackData _data;
    private FSMAgentEscapist _target;

    public AttackState(FSMAgentHunter agent, AttackData data, StateMachine stateMachine) : base(agent, stateMachine)
    {
        _hunter = agent;
        _data = data;
    }

    public override void Enter()
    {
        _target = _hunter.FindNearestAliveBoid(_data.perceptionRadius);
        Debug.Log("Hunter: Entre Attack");
    }

    public override void Exit() => Debug.Log("Hunter: Sali Attack");

    public override void Update()
    {
        if (_target == null || !_target.IsAlive || Vector3.Distance(_hunter.transform.position, _target.transform.position) > _data.perceptionRadius)
        {
            _hunter.ChangeState(HunterStates.Patrol);
            return;
        }

        float distance = Vector3.Distance(_hunter.transform.position, _target.transform.position);


        // Tiene bala: busca atacar a distancia
        if (_hunter.HasBullet)
        {
            if (distance <= _data.rangeAttackRadius)
            {
                PerformRangeAttack();
                return;
            }

            Pursue();
            return;
        }

        // No tiene bala: persigue hasta poder atacar cuerpo a cuerpo
        if (distance <= _data.meleeAttackRadius)
        {
            PerformMeleeAttack();
            return;
        }

        Pursue();
    }

    private void Pursue()
    {
        Vector3 steering = Seek(_target.transform.position, _data.maxSpeed, _data.maxSteering);
        _hunter.ApplyVelocity(steering, _data.maxSpeed);
    }

    private void PerformMeleeAttack()
    {
        _hunter.ApplyVelocity(Vector3.zero, 0f);

        Debug.Log("Hunter: Ataque melee");

        _target.TakeDamage(999f);

        _hunter.ChangeState(HunterStates.Patrol);

        //AttackSuccessful();
    }

    private void PerformRangeAttack()
    {
        _hunter.ApplyVelocity(Vector3.zero, 0f);

        Debug.Log("Hunter: Ataque a distancia");

        _hunter.Shoot(_target);

        AttackSuccessful();
    }

    private void AttackSuccessful()
    {
        _hunter.ResetTBA();
        _hunter.ChangeState(HunterStates.Patrol);
    }
    private Vector3 Seek(Vector3 target, float maxSpeed, float maxSteering)
    {
        var desired = DesiredVector(target, maxSpeed);

        return CalculateSteering(desired, maxSteering);
    }
}

[System.Serializable]
public class AttackData
{
    public float maxSpeed = 5f;
    public float maxSteering = 6f;
    public float TBA = 5f;
    public float rangeAttackRadius = 6f;
    public float meleeAttackRadius = 1f;
    public float perceptionRadius = 8f; 
}
