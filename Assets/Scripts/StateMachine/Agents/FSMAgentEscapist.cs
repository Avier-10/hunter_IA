using System.Collections;
using UnityEngine;

public enum EscapistStates
{
    Flocking,
    Evade,
    Attracted,
    Death
}

public class FSMAgentEscapist : Agent
{
    [Header("Data")]
    [SerializeField] private FlockingData dataFlocking;
    [SerializeField] private EvadeData dataEvade;
    [SerializeField] private AttractedData dataAttracted;
    [SerializeField] private DeathData dataDeath;

    [Header("Vision")]
    [SerializeField] private float _visionRadiusEnter = 5f;
    [SerializeField] private float _visionRadiusExit = 7f;

    [Header("Vida")]
    [SerializeField] private float _vidaMaxima = 3f;

    private float _vida;
    private bool _isCollected;

    private StateMachine _stateMachine;
    private AttractedState _attractedState;

    private Renderer[] _renderers;
    private Color[] _originalColors;

    public bool IsEliminated => _vida <= 0f && !_isCollected;
    public bool IsCollected => _isCollected;
    public bool IsAlive => _vida > 0f && !_isCollected;


    protected override void Awake()
    {
        base.Awake();

        InitializeVisuals();

        _vida = _vidaMaxima;
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
            Trap nearestInterest = FindNearestObject(8f);

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

        _vida -= amount;

        if (_vida <= 0f)
        {
            _vida = 0f;
            _velocity = Vector3.zero;

            _stateMachine.ChangeState(EscapistStates.Death);
        }
    }

    public void ResetHealth()
    {
        _vida = _vidaMaxima;
    }


    public void Collectible()
    {
        _isCollected = true;

        SetVisible(false);

        StartCoroutine(RespawnAfterCollection());
    }

    private IEnumerator RespawnAfterCollection()
    {
        yield return new WaitForSeconds(3f);

        Vector2 randomCircle = Random.insideUnitCircle * 20f;

        transform.position = new Vector3(randomCircle.x, transform.position.y, randomCircle.y);

        ResetHealth();
        InitializeMovement();

        _isCollected = false;

        SetVisible(true);
        RestoreColor();

        ChangeState(EscapistStates.Flocking);
    }


    public void InitializeMovement()
    {
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

        _velocity = randomDirection.normalized * dataFlocking.maxSpeed;
    }

    private void InitializeVisuals()
    {
        _renderers = GetComponentsInChildren<Renderer>();

        _originalColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalColors[i] = _renderers[i].material.color;
        }
    }

    public void SetVisible(bool visible)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = visible;

        foreach (Collider collider in GetComponentsInChildren<Collider>())
            collider.enabled = visible;
    }

    public void SetDeathColor(Color color)
    {
        foreach (Renderer renderer in _renderers)
        {
            renderer.material.color = color;
        }
    }

    public void RestoreColor()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].material.color = _originalColors[i];
        }
    }
}
