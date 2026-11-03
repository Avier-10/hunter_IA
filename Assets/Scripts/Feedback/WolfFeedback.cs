using UnityEngine;

// Presentation only: state tint, log lines and debug gizmos. The FoV cone (VisionSensorGizmos)
// and the A* path (PathNavigator) draw themselves.
[RequireComponent(typeof(FSMAgentWolf))]
public class WolfFeedback : MonoBehaviour
{
    [Header("Tint")]
    [Tooltip("Leave empty to tint every Renderer found in the children (multi-cube bodies).")]
    [SerializeField] private Renderer[] _renderers;
    [SerializeField] private Color _patrolColor = new Color(0.45f, 0.75f, 0.45f);
    [SerializeField] private Color _chaseColor = new Color(0.9f, 0.15f, 0.15f);
    [SerializeField] private Color _goToLastKnownColor = new Color(1f, 0.55f, 0.1f);
    [SerializeField] private Color _searchColor = new Color(1f, 0.9f, 0.2f);

    [Header("Logs")]
    [SerializeField] private bool _logEvents = true;

    [Header("Gizmos")]
    [SerializeField] private Color _lastKnownColor = Color.magenta;
    [SerializeField] private float _lastKnownGizmoRadius = 0.5f;
    [SerializeField] private Color _howlColor = new Color(1f, 0.4f, 0.8f);
    [SerializeField] private float _howlRingRadius = 6f;
    [SerializeField] private float _howlRingDuration = 1f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private const float RingFlatten = 0.02f;

    private FSMAgentWolf _wolf;
    private MaterialPropertyBlock _block;
    private float _howlTime = float.NegativeInfinity;
    private Vector3 _howlPosition;

    private void Awake()
    {
        _wolf = GetComponent<FSMAgentWolf>();
        _block = new MaterialPropertyBlock();
        if (_renderers == null || _renderers.Length == 0) _renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnEnable()
    {
        _wolf.StateChanged += OnStateChanged;
        _wolf.Howled += OnHowled;
        _wolf.AlertReceived += OnAlertReceived;
        _wolf.TargetAcquired += OnTargetAcquired;
        ApplyTint(_wolf.CurrentState);
    }

    private void OnDisable()
    {
        _wolf.StateChanged -= OnStateChanged;
        _wolf.Howled -= OnHowled;
        _wolf.AlertReceived -= OnAlertReceived;
        _wolf.TargetAcquired -= OnTargetAcquired;
    }

    private void OnStateChanged(WolfStates from, WolfStates to)
    {
        ApplyTint(to);
        if (from != to) Log($"state {from} -> {to}");
    }

    private void OnHowled(Vector3 position)
    {
        _howlTime = Time.time;
        _howlPosition = transform.position;
        Log($"howled, last known {position}");
    }

    private void OnAlertReceived(FSMAgentWolf source, Vector3 position)
    {
        Log($"received alert from [{(source != null ? source.name : "unknown")}], last known {position}");
    }

    private void OnTargetAcquired(Transform target)
    {
        Log($"spotted hunter '{target.name}'");
    }

    private void ApplyTint(WolfStates state)
    {
        if (_renderers == null) return;

        Color color = ColorFor(state);
        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer part = _renderers[i];
            if (part == null) continue;

            part.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColorId, color);
            part.SetPropertyBlock(_block);
        }
    }

    private Color ColorFor(WolfStates state)
    {
        switch (state)
        {
            case WolfStates.Chase: return _chaseColor;
            case WolfStates.GoToLastKnown: return _goToLastKnownColor;
            case WolfStates.Search: return _searchColor;
            default: return _patrolColor;
        }
    }

    private void Log(string message)
    {
        if (_logEvents) Debug.Log($"[{name}] {message}", this);
    }

    private void OnDrawGizmos()
    {
        if (_wolf == null) _wolf = GetComponent<FSMAgentWolf>();
        if (_wolf == null || !Application.isPlaying) return;

        if (_wolf.HasLastKnownPosition)
        {
            Gizmos.color = _lastKnownColor;
            Gizmos.DrawWireSphere(_wolf.LastKnownPosition, _lastKnownGizmoRadius);
            Gizmos.DrawLine(transform.position, _wolf.LastKnownPosition);
        }

        DrawHowlRing();
    }

    private void DrawHowlRing()
    {
        float progress = (Time.time - _howlTime) / Mathf.Max(_howlRingDuration, Mathf.Epsilon);
        if (progress < 0f || progress >= 1f) return;

        Color color = _howlColor;
        color.a = 1f - progress;
        Gizmos.color = color;

        Matrix4x4 previous = Gizmos.matrix;
        // A heavily flattened sphere reads as a ring on the floor.
        Gizmos.matrix = Matrix4x4.TRS(_howlPosition, Quaternion.identity, new Vector3(1f, RingFlatten, 1f));
        Gizmos.DrawWireSphere(Vector3.zero, _howlRingRadius * progress);
        Gizmos.matrix = previous;
    }
}
