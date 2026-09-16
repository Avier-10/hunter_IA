using System.Collections.Generic;
using UnityEngine;

public enum HunterStates
{
    Patrol,
    Attack,
    Gather
}

public class FSMAgentHunter : Agent
{
    [Header("Data")]
    [SerializeField] private PatrolData dataPatrol;
    [SerializeField] private AttackData dataAttack;
    [SerializeField] private GatherData dataGather;

    [Header("Weapon")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _bulletPrefab;

    private StateMachine _stateMachine;
    private float _tbaTimer;
    private bool _hasBullet = true;

    public List<Trap> ActiveObjects = new List<Trap>();

    public HunterStates CurrentState => (HunterStates)_stateMachine.CurrentStateKey;

    public bool HasBullet => _hasBullet;
    public Transform FirePoint => _firePoint;
    public GameObject BulletPrefab => _bulletPrefab;

    public float ReloadProgress
    {
        get
        {
            if (HasBullet)
                return 1f;

            if (dataAttack.TBA <= 0f)
                return 1f;

            return 1f - (_tbaTimer / dataAttack.TBA);
        }
    }

    public float ReloadTimeRemaining => _tbaTimer;

    protected override void Awake()
    {
        base.Awake();

        _stateMachine = new StateMachine();

        PatrolState patrolState = new PatrolState(this, dataPatrol, _stateMachine);
        AttackState attackState = new AttackState(this, dataAttack, _stateMachine);
        GatherState gatherState = new GatherState(this, dataGather, _stateMachine);

        _stateMachine.RegisterState(HunterStates.Patrol, patrolState);
        _stateMachine.RegisterState(HunterStates.Attack, attackState);
        _stateMachine.RegisterState(HunterStates.Gather, gatherState);

        _stateMachine.ChangeState(HunterStates.Patrol);
    }


    private void Update()
    {
        UpdateReload();

        StateEntry();

        _stateMachine.Update();
    }

    private void StateEntry()
    {
        HunterStates currentState = CurrentState;

        FSMAgentEscapist eliminated = FindNearestEliminatedBoid(dataGather.perceptionRadius);

        if (eliminated != null && currentState != HunterStates.Gather)
        {
            _stateMachine.ChangeState(HunterStates.Gather);

            return;
        }

        if (currentState == HunterStates.Patrol)
        {
            FSMAgentEscapist target = FindNearestAliveBoid(dataAttack.perceptionRadius);

            if (target != null) _stateMachine.ChangeState(HunterStates.Attack);
        }
    }

    public void ChangeState(HunterStates state) => _stateMachine.ChangeState(state);

    private void UpdateReload()
    {
        _tbaTimer = Mathf.Max(0f, _tbaTimer - Time.deltaTime);

        if (_tbaTimer <= 0f)
            _hasBullet = true;
    }

    public void ResetTBA()
    {
        _tbaTimer = dataAttack.TBA;
    }

    public void Shoot(FSMAgentEscapist target)
    {
        if (!_hasBullet)
            return;

        _hasBullet = false;

        GameObject bulletObject = Instantiate(_bulletPrefab, _firePoint.position, _firePoint.rotation);

        Bullet bullet = bulletObject.GetComponent<Bullet>();

        bullet.Initialize(target);
    }


    public FSMAgentEscapist FindNearestAliveBoid(float radius)
    {
        FSMAgentEscapist nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Agent agent in AllAgents)
        {
            if (agent is FSMAgentEscapist boid && boid.IsAlive)
            {
                float dist =
                    Vector3.Distance(transform.position, boid.transform.position);

                if (dist <= radius && dist < nearestDist)
                {
                    nearest = boid;
                    nearestDist = dist;
                }
            }
        }

        return nearest;
    }

    public FSMAgentEscapist FindNearestEliminatedBoid(float radius)
    {
        FSMAgentEscapist nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Agent agent in AllAgents)
        {
            if (agent is FSMAgentEscapist boid && boid.IsEliminated)
            {
                float dist =
                    Vector3.Distance(transform.position, boid.transform.position);

                if (dist <= radius && dist < nearestDist)
                {
                    nearest = boid;
                    nearestDist = dist;
                }
            }
        }

        return nearest;
    }
}