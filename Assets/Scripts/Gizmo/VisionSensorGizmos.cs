using UnityEngine;

// Draws what a VisionSensor perceives. Also hosts the shared circle/arc helpers for other radii gizmos.
[RequireComponent(typeof(VisionSensor))]
public class VisionSensorGizmos : MonoBehaviour
{
    [SerializeField] private Color _rangeColor = new Color(1f, 0.9f, 0.2f, 0.8f);
    [SerializeField] private Color _seenColor = Color.red;

    private const int DefaultSegments = 48;

    private VisionSensor _sensor;

    public static void DrawCircle(Vector3 center, float radius, Color color)
    {
        DrawArc(center, Vector3.forward, 360f, radius, color, false);
    }

    // Arc centered on `forward` (flattened to XZ). Edges join the arc ends to the center when requested.
    public static void DrawArc(Vector3 center, Vector3 forward, float angle, float radius, Color color, bool drawEdges = true, int segments = DefaultSegments)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude < Mathf.Epsilon) forward = Vector3.forward;
        forward.Normalize();

        Color previousColor = Gizmos.color;
        Gizmos.color = color;

        float start = -angle * 0.5f;
        float step = angle / Mathf.Max(1, segments);
        Vector3 previous = center + Quaternion.AngleAxis(start, Vector3.up) * forward * radius;

        if (drawEdges && angle < 360f) Gizmos.DrawLine(center, previous);

        for (int i = 1; i <= segments; i++)
        {
            Vector3 next = center + Quaternion.AngleAxis(start + step * i, Vector3.up) * forward * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }

        if (drawEdges && angle < 360f) Gizmos.DrawLine(center, previous);

        Gizmos.color = previousColor;
    }

    private void OnDrawGizmos()
    {
        if (_sensor == null) _sensor = GetComponent<VisionSensor>();
        if (_sensor == null) return;

        Vector3 eye = _sensor.EyePosition;
        DrawArc(eye, transform.forward, _sensor.ViewAngle, _sensor.ViewRadius, _rangeColor);
        if (_sensor.ViewAngle < 360f) DrawCircle(eye, _sensor.ViewRadius, new Color(_rangeColor.r, _rangeColor.g, _rangeColor.b, _rangeColor.a * 0.3f));

        Gizmos.color = _seenColor;
        var visible = _sensor.VisibleTargets;
        for (int i = 0; i < visible.Count; i++)
            if (visible[i] != null) Gizmos.DrawLine(eye, visible[i].position + Vector3.up * (eye.y - transform.position.y));
    }
}
