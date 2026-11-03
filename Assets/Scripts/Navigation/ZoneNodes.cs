using UnityEngine;

// Allocation-free queries that bridge map zones and graph nodes (single pass over the shared graph).
public static class ZoneNodes
{
    // Nearest usable node whose position lies inside any zone of `kind`.
    public static bool TryGetNearest(ZoneKind kind, Vector3 from, NavigationProfile profile, out Node nearest)
    {
        nearest = null;
        Graph graph = Graph.Instance;
        if (graph == null) return false;

        float bestSqr = float.MaxValue;
        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            Node node = graph.Nodes[i];
            if (!profile.CanUse(node) || !Zone.IsInside(kind, node.Position)) continue;

            float sqr = (node.Position - from).sqrMagnitude;
            if (sqr >= bestSqr) continue;

            bestSqr = sqr;
            nearest = node;
        }

        return nearest != null;
    }

    // Nearest usable node inside one specific zone; `safeNodesOnly` keeps only nodes flagged as safe-zone.
    public static bool TryGetNearest(Zone zone, Vector3 from, NavigationProfile profile, bool safeNodesOnly, out Node nearest)
    {
        nearest = null;
        Graph graph = Graph.Instance;
        if (graph == null || zone == null) return false;

        float bestSqr = float.MaxValue;
        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            Node node = graph.Nodes[i];
            if (!profile.CanUse(node) || (safeNodesOnly && !node.IsSafeZone)) continue;
            if (!zone.Contains(node.Position)) continue;

            float sqr = (node.Position - from).sqrMagnitude;
            if (sqr >= bestSqr) continue;

            bestSqr = sqr;
            nearest = node;
        }

        return nearest != null;
    }

    // Single-pass reservoir sampling; nodes closer than `minDistance` (flat) to `from` are skipped.
    public static bool TryGetRandom(ZoneKind kind, Vector3 from, float minDistance, NavigationProfile profile, out Node picked)
    {
        picked = null;
        Graph graph = Graph.Instance;
        if (graph == null) return false;

        float minSqr = minDistance * minDistance;
        int candidates = 0;
        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            Node node = graph.Nodes[i];
            if (!profile.CanUse(node) || !Zone.IsInside(kind, node.Position)) continue;

            Vector3 flat = node.Position - from;
            flat.y = 0f;
            if (flat.sqrMagnitude < minSqr) continue;

            candidates++;
            if (Random.Range(0, candidates) == 0) picked = node;
        }

        return picked != null;
    }
}
