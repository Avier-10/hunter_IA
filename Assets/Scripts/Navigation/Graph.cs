using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    public static Graph Instance { get; private set; }

    [SerializeField] private List<Node> _nodes = new List<Node>();
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private float _agentRadius = 0.5f;

    public IReadOnlyList<Node> Nodes => _nodes;

    // Domain reload may be disabled, so statics must be cleared explicitly.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        // Only the duplicate component goes: the GameObject owns the node children.
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    public void SetNodes(IEnumerable<Node> nodes)
    {
        _nodes.Clear();
        _nodes.AddRange(nodes);
    }

    public bool HasLineOfSight(Vector3 a, Vector3 b)
    {
        b.y = a.y;
        Vector3 delta = b - a;
        float distance = delta.magnitude;
        if (distance <= Mathf.Epsilon) return true;

        if (_agentRadius <= 0f) return !Physics.Linecast(a, b, _obstacleMask);
        return !Physics.SphereCast(a, _agentRadius, delta / distance, out _, distance, _obstacleMask);
    }

    public Node GetClosestNode(Vector3 position, NavigationProfile profile)
    {
        Node closestVisible = null;
        Node closestAny = null;
        float bestVisibleSqr = float.MaxValue;
        float bestAnySqr = float.MaxValue;

        for (int i = 0; i < _nodes.Count; i++)
        {
            Node node = _nodes[i];
            if (!profile.CanUse(node)) continue;

            float sqr = (node.Position - position).sqrMagnitude;
            if (sqr < bestAnySqr)
            {
                bestAnySqr = sqr;
                closestAny = node;
            }

            if (sqr < bestVisibleSqr && HasLineOfSight(position, node.Position))
            {
                bestVisibleSqr = sqr;
                closestVisible = node;
            }
        }

        return closestVisible != null ? closestVisible : closestAny;
    }

    public bool TryGetPath(Vector3 from, Vector3 to, NavigationProfile profile, List<Node> result)
    {
        result.Clear();

        Node start = GetClosestNode(from, profile);
        Node goal = GetClosestNode(to, profile);
        if (start == null || goal == null) return false;

        return Pathfinding.TryFindPath(start, goal, profile, result);
    }
}
