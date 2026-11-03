using UnityEngine;

// Caches a bool query (line of sight, visibility) so it runs at most once per interval instead of every frame.
// Allocation-free value type: keep it in a non-readonly field and call IsDue -> Store -> Value.
public struct ThrottledBool
{
    private float _nextRefreshTime;
    private bool _value;

    public bool Value => _value;

    // True when the cached value is stale (or was never computed) and the query should run again.
    public bool IsDue()
    {
        return Time.time >= _nextRefreshTime;
    }

    public bool Store(bool value, float interval)
    {
        _value = value;
        _nextRefreshTime = Time.time + Mathf.Max(0f, interval);
        return value;
    }

    // Forces the next IsDue to return true (new target, new path, state change).
    public void Invalidate()
    {
        _nextRefreshTime = 0f;
    }
}
