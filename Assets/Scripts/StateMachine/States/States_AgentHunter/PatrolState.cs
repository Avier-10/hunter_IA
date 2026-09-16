using System.Collections.Generic;
using UnityEngine;

public class PatrolState : SteeringState
{
    private FSMAgentHunter _hunter;
    private PatrolData _data;
    
    private int _currentNode;
    private float _spawnTimer;

    public PatrolState(FSMAgentHunter agent, PatrolData data, StateMachine stateMachine) 
        : base(agent, stateMachine)
    {
        _hunter = agent;
        _data = data;
    }

    public override void Enter()
    {
        _spawnTimer = _data.spawnInterval;
        Debug.Log("Hunter: ENTRE PATROL");
    }

    public override void Exit() => Debug.Log("Hunter: SALGO PATROL");

    public override void Update()
    {
        PatrolLoop();
        HandleSpawning();
    }
    
    private void PatrolLoop()
    {
        var nextWaypoint = _data.wayPoints[_currentNode];

        Vector3 direction = nextWaypoint.position - _hunter.transform.position;

        Debug.DrawLine(
            _hunter.transform.position,
            nextWaypoint.position,
            Color.red
        );

        float distance = direction.magnitude;

        if (distance <= _data.waypointCheckDistance)
        {
            _currentNode =
                _currentNode + 1 < _data.wayPoints.Count
                ? _currentNode + 1
                : 0;

            nextWaypoint = _data.wayPoints[_currentNode];
        }

        Vector3 steering = Arrive(
            nextWaypoint.position,
            _data.maxSpeed,
            _data.maxSteering,
            _data.slowingDistance
        );

        _hunter.ApplyVelocity(steering, _data.maxSpeed);
    }
    private Vector3 Arrive(Vector3 target, float maxSpeed, float maxSteering, float slowingDistance)
    {
        Vector3 direction = target - _hunter.transform.position;

        float distance = direction.magnitude;

        if (distance < slowingDistance)
            return Vector3.zero;

        float targetSpeed = maxSpeed * (distance / slowingDistance);
        float desiredSpeed = Mathf.Min(targetSpeed, maxSpeed);

        Vector3 desired = direction.normalized * desiredSpeed;

        return CalculateSteering(desired, maxSteering);
    }
    
    private void HandleSpawning()
    {
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f) return;

        _spawnTimer = _data.spawnInterval;
        _hunter.ActiveObjects.RemoveAll(o => o == null);

        if (_hunter.ActiveObjects.Count < _data.maxActiveObjects && _data.trapPrefab != null)
        {
            SpawnTrap();
        }
    }    
    private void SpawnTrap()
    {
        Vector2 randomCircle = Random.insideUnitCircle * _data.spawnAreaRadius;
        Vector3 spawnPos = _hunter.transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        GameObject obj = Object.Instantiate(_data.trapPrefab, spawnPos, Quaternion.identity);
        
        var interest = obj.GetComponent<Trap>();

        Debug.Log(
        $"Hunter: Spawn trap {_hunter.ActiveObjects.Count + 1}"
    );
        if (interest != null)
            _hunter.ActiveObjects.Add(interest);
    }
}

[System.Serializable]
public class PatrolData
{
    [Header("Waypoints")]
    public List<Transform> wayPoints;
    public float waypointCheckDistance;

    [Header("Steering")]
    public float maxSpeed = 3f;
    public float maxSteering = 3f;
    public float slowingDistance = 1f;

    [Header("Traps")]
    public GameObject trapPrefab;
    public float spawnInterval = 5f;
    public int maxActiveObjects = 5;
    public float spawnAreaRadius = 1f; 
}