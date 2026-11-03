using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public enum EscapistStates
{
    Flocking,
    Evade,
    Attracted,
    Death
}

public class FSMAgentEscapist : Agent
{
    // Damage that kills any boid in one hit (bullet, melee, trap).
    public const float LethalDamage = 999f;

    [Header("Data")]
    [SerializeField] private FlockingData dataFlocking;
    [SerializeField] private EvadeData dataEvade;
    [SerializeField] private AttractedData dataAttracted;
    [SerializeField] private DeathData dataDeath;

    [Header("Vision")]
    [SerializeField] private float _visionRadiusEnter = 5f;
    [SerializeField] private float _visionRadiusExit = 7f;

    [Header("Health")]
    [FormerlySerializedAs("_vidaMaxima")]
    [SerializeField] private float _maxHealth = 3f;

    [Header("Traps")]
    [Tooltip("Radius within which a flocking boid is attracted to a free trap.")]
    [SerializeField] private float _trapDetectionRadius = 8f;

    [Header("Respawn")]
    [Tooltip("Seconds a collected boid stays hidden before respawning in the Coto.")]
    [SerializeField] private float _respawnDelay = 3f;

    private float _health;
    private bool _isCollected;

    private StateMachine _stateMachine;
    private AttractedState _attractedState;

    private GameObject _remains;

    public bool IsEliminated => _health <= 0f && !_isCollected;
    public bool IsCollected => _isCollected;
    public bool IsAlive => _health > 0f && !_isCollected;

    // Read by BoidGizmos to draw the perception radii.
    public FlockingData Flocking => dataFlocking;
    public float VisionRadiusEnter => _visionRadiusEnter;
    public float VisionRadiusExit => _visionRadiusExit;


    protected override void Awake()
    {
        base.Awake();

        _health = _maxHealth;
        _stateMachine = new StateMachine();

        InitializeMovement();

        FlockingState flockingState = new FlockingState(this, dataFlocking, _stateMachine);
        EvadeState evadeState = new EvadeState(this, dataEvade, _stateMachine);
        DeathState deathState = new DeathState(this, dataDeath, _stateMachine);
        _attractedState = new AttractedState(this, dataAttracted, _stateMachine);

        _stateMachine.RegisterState(EscapistStates.Flocking,flockingState);
        _stateMachine.RegisterState(EscapistStates.Evade,evadeState);
        _stateMachine.RegisterState(EscapistStates.Attracted,_attractedState);
        _stateMachine.RegisterState(EscapistStates.Death,deathState);

        _stateMachine.ChangeState(EscapistStates.Flocking);
    }

    private void Update()
    {
        if (_isCollected)
            return;

        if (!IsEliminated) CheckEnvironment();

        _stateMachine.Update();
    }


    public void ChangeState(EscapistStates state)
    {
        _stateMachine.ChangeState(state);
    }


    private void CheckEnvironment()
    {
        float hunterDistance = Vector3.Distance( transform.position, dataEvade.Hunter.transform.position);

        bool inEvade = _stateMachine.CurrentStateKey.Equals(EscapistStates.Evade);
        bool inFlocking = _stateMachine.CurrentStateKey.Equals(EscapistStates.Flocking);

        if (!inEvade && hunterDistance <= _visionRadiusEnter)
        {
            _stateMachine.ChangeState(EscapistStates.Evade);
            return;
        }

        if (inEvade)
        {
            if (hunterDistance > _visionRadiusExit)
                _stateMachine.ChangeState(EscapistStates.Flocking);

            return;
        }

        if (inFlocking)
        {
            Trap nearestInterest = FindNearestObject(_trapDetectionRadius);

            if (nearestInterest != null)
                ChangeToAttracted(nearestInterest);
        }
    }

    public void ChangeToAttracted(Trap target)
    {
        _attractedState.SetTarget(target);

        _stateMachine.ChangeState(EscapistStates.Attracted);
    }

    private Trap FindNearestObject(float radius)
    {
        Trap nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Trap interest in Trap.AllObjects)
        {
            if (interest == null)
                continue;

            if (interest.IsOccupied)
                continue;

            float distance = Vector3.Distance(transform.position, interest.transform.position
            );

            if (distance <= radius && distance < nearestDistance)
            {
                nearest = interest;
                nearestDistance = distance;
            }
        }

        return nearest;
    }


    public void TakeDamage(float amount)
    {
        if (IsEliminated)
            return;

        _health -= amount;

        if (_health <= 0f)
        {
            _health = 0f;
            _velocity = Vector3.zero;

            _stateMachine.ChangeState(EscapistStates.Death);
        }
    }

    public void ResetHealth()
    {
        _health = _maxHealth;
    }


    public void Collect()
    {
        _isCollected = true;

        ClearRemains();
        SetVisible(false);

        StartCoroutine(RespawnAfterCollection());
    }

    private IEnumerator RespawnAfterCollection()
    {
        yield return new WaitForSeconds(_respawnDelay);

        RespawnInCoto();

        _isCollected = false;

        SetVisible(true);

        ChangeState(EscapistStates.Flocking);
    }

    // Boids only live in the Coto, so every respawn lands on a random point inside that zone.
    public void RespawnInCoto()
    {
        if (Zone.TryGet(ZoneKind.HuntingGround, out Zone coto))
        {
            Vector3 point = coto.RandomPoint();
            point.y = transform.position.y;
            transform.position = point;
        }

        ResetHealth();
        InitializeMovement();
    }


    public void InitializeMovement()
    {
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

        _velocity = randomDirection.normalized * dataFlocking.maxSpeed;
    }

    public void SetVisible(bool visible)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = visible;

        foreach (Collider collider in GetComponentsInChildren<Collider>())
            collider.enabled = visible;
    }

    // What is left where the boid fell; the hunter still collects the (hidden) boid at this spot.
    public void SpawnRemains(GameObject prefab)
    {
        ClearRemains();

        if (prefab == null) return;

        _remains = Instantiate(prefab, transform.position, Quaternion.identity);
    }

    public void ClearRemains()
    {
        if (_remains != null) Destroy(_remains);

        _remains = null;
    }
}
