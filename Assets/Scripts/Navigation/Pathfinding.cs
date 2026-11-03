using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
    private struct QueueEntry
    {
        public Node Node;
        public float G;
    }

    private static readonly PriorityQueue<QueueEntry> _frontier = new PriorityQueue<QueueEntry>();
    private static readonly Dictionary<Node, Node> _cameFrom = new Dictionary<Node, Node>();
    private static readonly Dictionary<Node, float> _costSoFar = new Dictionary<Node, float>();

    public static bool TryFindPath(Node start, Node goal, NavigationProfile profile, List<Node> result)
    {
        result.Clear();
        if (!profile.CanUse(start) || !profile.CanUse(goal)) return false;

        _frontier.Clear();
        _cameFrom.Clear();
        _costSoFar.Clear();

        _cameFrom[start] = null;
        _costSoFar[start] = 0f;
        _frontier.Enqueue(new QueueEntry { Node = start, G = 0f }, Heuristic(start, goal));

        bool found = false;
        while (_frontier.Count > 0)
        {
            QueueEntry current = _frontier.Dequeue();

            // A cheaper route to this node was found after this entry was queued.
            if (current.G > _costSoFar[current.Node]) continue;

            if (current.Node == goal)
            {
                found = true;
                break;
            }

            IReadOnlyList<Node> neighbors = current.Node.Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                Node next = neighbors[i];
                if (!profile.CanUse(next)) continue;

                float newCost = current.G + current.Node.DistanceTo(next);
                if (_costSoFar.TryGetValue(next, out float knownCost) && newCost >= knownCost) continue;

                _costSoFar[next] = newCost;
                _cameFrom[next] = current.Node;
                _frontier.Enqueue(new QueueEntry { Node = next, G = newCost }, newCost + Heuristic(next, goal));
            }
        }

        if (!found) return false;

        for (Node node = goal; node != null; node = _cameFrom[node])
            result.Add(node);
        result.Reverse();
        return true;
    }

    public static float PathLength(IReadOnlyList<Node> path)
    {
        float length = 0f;
        if (path == null) return length;

        for (int i = 1; i < path.Count; i++)
            length += path[i - 1].DistanceTo(path[i]);
        return length;
    }

    // Edge costs are straight-line distances, so Euclidean distance never overestimates
    // (admissible) and obeys the triangle inequality (consistent).
    private static float Heuristic(Node from, Node goal)
    {
        return Vector3.Distance(from.Position, goal.Position);
    }
}
