using UnityEngine;

// Default state: get into the Coto, wander between its graph nodes and keep it stocked with interest objects.
public class SearchState : HunterState
{
    private readonly SearchData _data;

    private Node _cotoNode;
    private Node _wanderNode;
    private float _spawnTimer;

    public SearchState(FSMAgentHunter agent, SearchData data, StateMachine stateMachine)
        : base(agent, stateMachine)
    {
        _data = data;
        // Not reset on Enter: frequent state changes must not postpone the spawns forever.
        _spawnTimer = data.spawnInterval;
    }

    protected override void OnEnter()
    {
        _cotoNode = null;
        _wanderNode = null;
    }

    public override void Update()
    {
        if (TryGlobalTransitions(HunterTransitions.All)) return;

        if (!Zone.IsInside(ZoneKind.HuntingGround, _agent.transform.position))
        {
            ReachCoto();
            return;
        }

        _cotoNode = null;
        HandleSpawning();
        Wander();
    }

    private void ReachCoto()
    {
        NavigationProfile profile = Hunter.Navigator.Profile;
        if (_cotoNode == null
            && !ZoneNodes.TryGetNearest(ZoneKind.HuntingGround, _agent.transform.position, profile, out _cotoNode))
        {
            Stop();
            return;
        }

        Hunter.SetFocus(_cotoNode.Position);
        TravelTo(_cotoNode.Position, _data.maxSpeed);
    }

    private void Wander()
    {
        if (_wanderNode == null || IsWithin(_wanderNode.Position, _data.arrivalDistance))
        {
            NavigationProfile profile = Hunter.Navigator.Profile;
            if (!ZoneNodes.TryGetRandom(ZoneKind.HuntingGround, _agent.transform.position, _data.minWanderDistance, profile, out _wanderNode))
            {
                Stop();
                return;
            }

            ResetPathMode();
        }

        Hunter.SetFocus(_wanderNode.Position);
        TravelTo(_wanderNode.Position, _data.maxSpeed);
    }

    private void HandleSpawning()
    {
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f) return;

        _spawnTimer = _data.spawnInterval;

        Hunter.PruneActiveObjects();

        if (Hunter.ActiveObjectCount >= _data.maxActiveObjects || _data.trapPrefab == null) return;
        if (!Zone.TryGet(ZoneKind.HuntingGround, out Zone coto)) return;

        GameObject spawned = Object.Instantiate(_data.trapPrefab, coto.RandomPoint(), Quaternion.identity);
        Trap trap = spawned.GetComponent<Trap>();
        if (trap == null)
        {
            Debug.LogWarning("[Hunter] The interest object prefab has no Trap component.", Hunter);
            Object.Destroy(spawned);
            return;
        }

        Hunter.RegisterActiveObject(trap);
    }
}

[System.Serializable]
public class SearchData
{
    [Header("Steering")]
    public float maxSpeed = 5f;

    [Header("Wander")]
    public float arrivalDistance = 0.75f;
    [Tooltip("Random wander nodes closer than this to the hunter are skipped.")]
    public float minWanderDistance = 3f;

    [Header("Interest objects")]
    public GameObject trapPrefab;
    public float spawnInterval = 5f;
    public int maxActiveObjects = 5;
}
