using UnityEngine;

// Wrap-around limits from Parcial 1. With followHuntingGround on, the limits are the Coto zone, so the Boids
// (the agents that still use it) never leave their hunting ground. The width/height fields are the fallback.
public class Bounds : MonoBehaviour
{
    public static Bounds Instance { get; private set; }

    [SerializeField] private bool followHuntingGround = true;
    [SerializeField] private float height = 30f;
    [SerializeField] private float width = 60f;
    [SerializeField] private bool drawGizmos;

    private Vector3 _center;
    private float _width;
    private float _height;

    // Domain reload may be disabled, so statics must be cleared explicitly.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(this);

        _width = width;
        _height = height;
    }

    // Zones register in OnEnable, so every one of them is known by the time Start runs.
    private void Start()
    {
        if (!followHuntingGround || !Zone.TryGet(ZoneKind.HuntingGround, out Zone coto)) return;

        _center = coto.Center;
        _width = coto.Size.x;
        _height = coto.Size.z;
    }

    public Vector3 OutOfBounds(Vector3 position)
    {
        Vector3 newPosition = position;
        Vector3 offset = position - _center;
        float halfWidth = _width / 2;
        float halfHeight = _height / 2;

        if (offset.x > halfWidth) newPosition.x = _center.x - halfWidth;
        if (offset.x < -halfWidth) newPosition.x = _center.x + halfWidth;
        if (offset.z > halfHeight) newPosition.z = _center.z - halfHeight;
        if (offset.z < -halfHeight) newPosition.z = _center.z + halfHeight;
        return newPosition;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        bool running = Application.isPlaying;
        Vector3 center = running ? _center : Vector3.zero;
        float drawWidth = running ? _width : width;
        float drawHeight = running ? _height : height;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(drawWidth, 0, drawHeight));
    }
}
