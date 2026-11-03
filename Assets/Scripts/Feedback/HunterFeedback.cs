using UnityEngine;

// Presentation only: state label, log lines and debug gizmos. The A* path (PathNavigator) and the
// wolf lines (VisionSensorGizmos) draw themselves.
[RequireComponent(typeof(FSMAgentHunter), typeof(VisionSensor))]
public class HunterFeedback : MonoBehaviour
{
    [Header("Label")]
    [SerializeField] private bool _showLabel = true;
    [SerializeField] private float _labelHeight = 2.5f;
    [SerializeField] private Vector2 _labelSize = new Vector2(220f, 40f);
    [SerializeField] private int _fontSize = 14;
    [SerializeField] private Color _labelColor = Color.white;

    [Header("Logs")]
    [SerializeField] private bool _logEvents = true;

    [Header("Gizmos")]
    [SerializeField] private Color _visionColor = new Color(1f, 0.9f, 0.2f, 0.9f);
    [SerializeField] private Color _meleeColor = new Color(1f, 0.2f, 0.2f, 0.9f);
    [SerializeField] private Color _rangeColor = new Color(1f, 0.55f, 0.1f, 0.9f);
    [SerializeField] private Color _focusColor = Color.cyan;
    [SerializeField] private Color _safeZoneColor = new Color(0.65f, 0.4f, 0.95f);
    [SerializeField] private float _focusGizmoRadius = 0.4f;

    private FSMAgentHunter _hunter;
    private VisionSensor _sensor;
    private GUIStyle _style;
    private string _label = string.Empty;
    private HunterStates _labelState;
    private int _labelGathers = -1;
    private bool _labelBuilt;

    private void Awake()
    {
        _hunter = GetComponent<FSMAgentHunter>();
        _sensor = GetComponent<VisionSensor>();
    }

    private void OnEnable()
    {
        _hunter.StateChanged += OnStateChanged;
        _hunter.SafeZoneChosen += OnSafeZoneChosen;
        _sensor.TargetSpotted += OnWolfSpotted;
    }

    private void OnDisable()
    {
        _hunter.StateChanged -= OnStateChanged;
        _hunter.SafeZoneChosen -= OnSafeZoneChosen;
        _sensor.TargetSpotted -= OnWolfSpotted;
    }

    private void OnStateChanged(HunterStates from, HunterStates to)
    {
        if (from == to) Log($"started in {to}");
        else Log($"state {from} -> {to}");
    }

    private void OnSafeZoneChosen(Zone zone, float pathLength)
    {
        Log($"fleeing to safe zone '{(zone != null ? zone.name : "none")}' (path length {pathLength:F1})");
    }

    private void OnWolfSpotted(Transform wolf)
    {
        Log($"wolf detected: {(wolf != null ? wolf.name : "unknown")}");
    }

    private void Log(string message)
    {
        if (_logEvents) Debug.Log($"[Hunter] {message}", this);
    }

    private void OnGUI()
    {
        if (!_showLabel || Event.current.type != EventType.Repaint || !_hunter.IsPresent) return;

        Camera view = Camera.main;
        if (view == null) return;

        Vector3 screen = view.WorldToScreenPoint(transform.position + Vector3.up * _labelHeight);
        if (screen.z <= 0f) return;

        RefreshLabel();
        EnsureStyle();

        Rect area = new Rect(
            screen.x - _labelSize.x * 0.5f,
            Screen.height - screen.y - _labelSize.y * 0.5f,
            _labelSize.x,
            _labelSize.y);
        GUI.Label(area, _label, _style);
    }

    // The text is rebuilt only when something shown in it changed, so OnGUI stays allocation-free.
    private void RefreshLabel()
    {
        HunterStates state = _hunter.CurrentState;
        int gathers = _hunter.GatherCount;
        if (_labelBuilt && state == _labelState && gathers == _labelGathers) return;

        _labelBuilt = true;
        _labelState = state;
        _labelGathers = gathers;
        _label = $"{state}  Gathered {gathers}/{_hunter.RequiredGathers}";
    }

    private void EnsureStyle()
    {
        if (_style != null) return;

        _style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = _fontSize,
            fontStyle = FontStyle.Bold
        };
        _style.normal.textColor = _labelColor;
    }

    private void OnDrawGizmos()
    {
        if (_hunter == null) _hunter = GetComponent<FSMAgentHunter>();
        if (_hunter == null) return;

        Vector3 center = transform.position;
        VisionSensorGizmos.DrawCircle(center, _hunter.VisionRadius, _visionColor);
        VisionSensorGizmos.DrawCircle(center, _hunter.RangeAttackRadius, _rangeColor);
        VisionSensorGizmos.DrawCircle(center, _hunter.MeleeAttackRadius, _meleeColor);

        if (!Application.isPlaying || !_hunter.IsPresent) return;

        if (_hunter.CurrentState == HunterStates.Flee && _hunter.ChosenSafeZone != null)
        {
            Gizmos.color = _safeZoneColor;
            Gizmos.DrawLine(center, _hunter.ChosenSafeZone.Center);
        }

        if (_hunter.TryGetFocus(out Vector3 focus))
        {
            Gizmos.color = _focusColor;
            Gizmos.DrawLine(center, focus);
            Gizmos.DrawWireSphere(focus, _focusGizmoRadius);
        }
    }
}
