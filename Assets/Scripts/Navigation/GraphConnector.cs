using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Editor-time helper: bakes node links into the scene so Graph stays runtime-only.
public class GraphConnector : MonoBehaviour
{
    [SerializeField] private Graph _graph;
    [SerializeField] private float _maxLinkDistance = 8f;

    private void Reset()
    {
        _graph = GetComponent<Graph>();
    }

    [ContextMenu("Collect Child Nodes")]
    public void CollectChildNodes()
    {
        if (_graph == null) return;

        RecordUndo(_graph, "Collect Child Nodes");
        _graph.SetNodes(GetComponentsInChildren<Node>(true));
        MarkDirty(_graph);
    }

    [ContextMenu("Rebuild Connections")]
    public void RebuildConnections()
    {
        if (_graph == null) return;

        var nodes = _graph.Nodes;
        for (int i = 0; i < nodes.Count; i++)
            if (nodes[i] != null) RecordUndo(nodes[i], "Rebuild Connections");

        for (int i = 0; i < nodes.Count; i++)
            if (nodes[i] != null) nodes[i].ClearConnections();

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null) continue;

            for (int j = i + 1; j < nodes.Count; j++)
            {
                if (nodes[j] == null) continue;
                if (nodes[i].DistanceTo(nodes[j]) > _maxLinkDistance) continue;
                if (!_graph.HasLineOfSight(nodes[i].Position, nodes[j].Position)) continue;

                nodes[i].Connect(nodes[j]);
            }
        }

        for (int i = 0; i < nodes.Count; i++)
            if (nodes[i] != null) MarkDirty(nodes[i]);
    }

    private static void RecordUndo(Object target, string label)
    {
#if UNITY_EDITOR
        Undo.RecordObject(target, label);
#endif
    }

    private static void MarkDirty(Object target)
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(target);
#endif
    }
}
