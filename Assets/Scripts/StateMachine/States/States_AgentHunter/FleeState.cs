using System.Collections.Generic;
using UnityEngine;

// Runs to the safe zone with the shortest A* path, waits there, then resumes. Only Dead can interrupt it.
public class FleeState : HunterState
{
    private readonly FleeData _data;
    private readonly List<Node> _scratchPath = new List<Node>();

    private Node _goal;
    private bool _isSafe;
    private float _waitTimer;

    public FleeState(FSMAgentHunter agent, FleeData data, StateMachine stateMachine)
        : base(agent, stateMachine)
    {
        _data = data;
    }

    protected override void OnEnter()
    {
        _isSafe = false;
        _goal = null;
        _waitTimer = 0f;

        ChooseSafeZone();
    }

    public override void Update()
    {
        if (_isSafe)
        {
            Wait();
            return;
        }

        if (_goal == null)
        {
            BeginWaiting();
            return;
        }

        if (IsWithin(_goal.Position, _data.arrivalDistance))
        {
            BeginWaiting();
            return;
        }

        TravelTo(_goal.Position, _data.maxSpeed, _data.maxSteering);
    }

    // Without a reachable safe zone the hunter just holds still for the wait time instead of thrashing states.
    private void BeginWaiting()
    {
        _isSafe = true;
        _waitTimer = Hunter.SafeZoneWaitTime;
        Stop();

        if (!Zone.IsInSafeZone(_agent.transform.position))
            Debug.LogWarning("[Hunter] No reachable safe zone to flee to.", Hunter);
    }

    private void Wait()
    {
        Stop();

        _waitTimer -= Time.deltaTime;
        if (_waitTimer > 0f) return;

        ResumeAfterTask();
    }

    // A zone is contested when the wolf would get there first, comparing arrival times (distance / speed) so a zone
    // the hunter is about to reach, even with the wolf standing next to it, is not discarded.
    // The wolf distance is a straight line: it only ranks the zones, A* still builds the real route.
    private bool IsContested(Transform threat, Vector3 safePoint, float hunterPathLength)
    {
        if (threat == null) return false;

        Vector3 toThreat = threat.position - safePoint;
        toThreat.y = 0f;

        float hunterTime = hunterPathLength / Mathf.Max(_data.maxSpeed, Mathf.Epsilon);
        float wolfTime = toThreat.magnitude / Mathf.Max(_data.assumedWolfSpeed, Mathf.Epsilon);

        return wolfTime < hunterTime;
    }

    private void ChooseSafeZone()
    {
        Graph graph = Graph.Instance;
        if (graph == null) return;

        Vector3 position = _agent.transform.position;
        NavigationProfile profile = Hunter.Navigator.Profile;
        Node start = graph.GetClosestNode(position, profile);

        // The wolf that triggered the flee: a zone it can reach before the hunter would be a trap.
        Transform threat = Hunter.Sensor.ClosestTarget;

        Zone bestZone = null;
        float bestScore = float.MaxValue;
        float bestLength = 0f;

        var zones = Zone.All;
        for (int i = 0; i < zones.Count; i++)
        {
            Zone zone = zones[i];
            if (zone.Kind != ZoneKind.Safe) continue;
            if (!ZoneNodes.TryGetNearest(zone, position, profile, true, out Node safeNode)) continue;
            if (!Pathfinding.TryFindPath(start, safeNode, profile, _scratchPath)) continue;

            float length = Pathfinding.PathLength(_scratchPath);
            float score = IsContested(threat, safeNode.Position, length) ? length + _data.contestedPenalty : length;
            if (score >= bestScore) continue;

            bestScore = score;
            bestLength = length;
            bestZone = zone;
            _goal = safeNode;
        }

        if (bestZone == null)
        {
            _goal = null;
            return;
        }

        Hunter.SetFocus(_goal.Position);
        Hunter.ReportSafeZoneChosen(bestZone, bestLength);
    }
}

[System.Serializable]
public class FleeData
{
    public float maxSpeed = 6.5f;
    [Tooltip("Steering limit while fleeing. Higher than the normal one so the hunter turns around right away instead of drifting on.")]
    public float maxSteering = 30f;
    [Tooltip("Rough wolf speed used to judge who reaches a safe zone first. Keep it near the wolves' Chase speed.")]
    public float assumedWolfSpeed = 4f;
    [Tooltip("Extra path length added to a safe zone the perceived wolf would reach before the hunter, so the hunter avoids running toward it.")]
    public float contestedPenalty = 100f;
    [Tooltip("Distance to the safe node at which the hunter starts waiting.")]
    public float arrivalDistance = 0.5f;
}
