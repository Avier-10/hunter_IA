using System.Collections.Generic;
using UnityEngine;

// A rectangular map area. The trigger BoxCollider is only the editable volume; physics never reacts to it.
[RequireComponent(typeof(BoxCollider))]
public class Zone : MonoBehaviour
{
    [SerializeField] private ZoneKind _kind;
    [SerializeField] private float _gizmoFillAlpha = 0.15f;

    private static readonly List<Zone> _zones = new List<Zone>();

    private BoxCollider _collider;

    public static IReadOnlyList<Zone> All => _zones;

    public ZoneKind Kind => _kind;
    public Vector3 Center => Volume.bounds.center;
    public Vector3 Size => Volume.bounds.size;

    private BoxCollider Volume
    {
        get
        {
            if (_collider == null) _collider = GetComponent<BoxCollider>();
            return _collider;
        }
    }

    // Domain reload may be disabled, so statics must be cleared explicitly.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _zones.Clear();
    }

    private void OnEnable()
    {
        if (!_zones.Contains(this)) _zones.Add(this);
    }

    private void OnDisable()
    {
        _zones.Remove(this);
    }

    // XZ only: the vertical extent of the volume is irrelevant to agents walking on the floor.
    public bool Contains(Vector3 point)
    {
        var area = Volume.bounds;
        return point.x >= area.min.x && point.x <= area.max.x
            && point.z >= area.min.z && point.z <= area.max.z;
    }

    public Vector3 RandomPoint()
    {
        var area = Volume.bounds;
        return new Vector3(
            Random.Range(area.min.x, area.max.x),
            area.min.y,
            Random.Range(area.min.z, area.max.z));
    }

    public static bool IsInside(ZoneKind kind, Vector3 point)
    {
        for (int i = 0; i < _zones.Count; i++)
            if (_zones[i]._kind == kind && _zones[i].Contains(point)) return true;
        return false;
    }

    public static bool IsInSafeZone(Vector3 point)
    {
        return IsInside(ZoneKind.Safe, point);
    }

    public static bool TryGet(ZoneKind kind, out Zone zone)
    {
        for (int i = 0; i < _zones.Count; i++)
        {
            if (_zones[i]._kind != kind) continue;
            zone = _zones[i];
            return true;
        }

        zone = null;
        return false;
    }

    private static Color KindColor(ZoneKind kind)
    {
        switch (kind)
        {
            case ZoneKind.Start: return new Color(0.35f, 0.85f, 0.45f);
            case ZoneKind.HuntingGround: return new Color(0.95f, 0.65f, 0.15f);
            case ZoneKind.Safe: return new Color(0.65f, 0.40f, 0.95f);
            default: return new Color(0.30f, 0.65f, 1f);
        }
    }

    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;

        Color color = KindColor(_kind);
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = new Color(color.r, color.g, color.b, _gizmoFillAlpha);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = color;
        Gizmos.DrawWireCube(box.center, box.size);

        Gizmos.matrix = previousMatrix;

#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(transform.TransformPoint(box.center), _kind.ToString());
#endif
    }
}
