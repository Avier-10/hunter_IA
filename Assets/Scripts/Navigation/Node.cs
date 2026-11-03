using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    [SerializeField] private List<Node> _neighbors = new List<Node>();
    [SerializeField] private bool _isSafeZone;
    [SerializeField] private bool _blocked;
    [SerializeField] private float _gizmoRadius = 0.3f;
    [SerializeField] private Color _nodeColor = Color.white;
    [SerializeField] private Color _safeZoneColor = Color.green;
    [SerializeField] private Color _linkColor = Color.gray;

    public Vector3 Position => transform.position;
    public IReadOnlyList<Node> Neighbors => _neighbors;
    public bool IsSafeZone => _isSafeZone;
    public bool IsBlocked => _blocked;

    public void Connect(Node other)
    {
        if (other == null || other == this) return;

        if (!_neighbors.Contains(other)) _neighbors.Add(other);
        if (!other._neighbors.Contains(this)) other._neighbors.Add(this);
    }

    public void Disconnect(Node other)
    {
        if (other == null) return;

        _neighbors.Remove(other);
        other._neighbors.Remove(this);
    }

    public void ClearConnections()
    {
        for (int i = _neighbors.Count - 1; i >= 0; i--)
        {
            Node other = _neighbors[i];
            if (other != null) other._neighbors.Remove(this);
        }
        _neighbors.Clear();
    }

    public float DistanceTo(Node other)
    {
        return Vector3.Distance(Position, other.Position);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _isSafeZone ? _safeZoneColor : _nodeColor;
        Gizmos.DrawSphere(Position, _gizmoRadius);

        Gizmos.color = _linkColor;
        for (int i = 0; i < _neighbors.Count; i++)
        {
            Node other = _neighbors[i];
            if (other == null) continue;

            // Linked nodes both draw the edge; only the lower id does, unless the link is one-way.
            bool reciprocal = other._neighbors.Contains(this);
            if (reciprocal && GetInstanceID() > other.GetInstanceID()) continue;

            Gizmos.DrawLine(Position, other.Position);
        }
    }
}
