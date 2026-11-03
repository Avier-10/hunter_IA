using UnityEngine;

// Throttled boid perception for the hunter. States read the cached results and never iterate Agent.AllAgents.
public class HunterBoidScanner
{
    private readonly float _interval;

    private FSMAgentEscapist _alive;
    private FSMAgentEscapist _eliminated;
    private float _timer;

    public HunterBoidScanner(float interval)
    {
        _interval = Mathf.Max(interval, Mathf.Epsilon);
        // Random offset keeps the first scan from landing on the same frame as other throttled queries.
        _timer = Random.Range(0f, _interval);
    }

    // The cache can be a few frames old, so it is revalidated cheaply on every read.
    public FSMAgentEscapist NearestAlive => _alive != null && _alive.IsAlive ? _alive : null;
    public FSMAgentEscapist NearestEliminated => _eliminated != null && _eliminated.IsEliminated ? _eliminated : null;

    public void Tick(Vector3 origin, float radius, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer > 0f) return;

        _timer = _interval;
        Scan(origin, radius);
    }

    // Forces the next Tick to scan (after a teleport, for example).
    public void Invalidate()
    {
        _timer = 0f;
        _alive = null;
        _eliminated = null;
    }

    private void Scan(Vector3 origin, float radius)
    {
        _alive = null;
        _eliminated = null;

        float radiusSqr = radius * radius;
        float aliveSqr = float.MaxValue;
        float eliminatedSqr = float.MaxValue;

        var agents = Agent.AllAgents;
        for (int i = 0; i < agents.Count; i++)
        {
            if (agents[i] is not FSMAgentEscapist boid) continue;

            Vector3 delta = boid.transform.position - origin;
            delta.y = 0f;
            float sqr = delta.sqrMagnitude;
            if (sqr > radiusSqr) continue;

            if (boid.IsAlive)
            {
                if (sqr >= aliveSqr) continue;
                aliveSqr = sqr;
                _alive = boid;
            }
            else if (boid.IsEliminated)
            {
                if (sqr >= eliminatedSqr) continue;
                eliminatedSqr = sqr;
                _eliminated = boid;
            }
        }
    }
}
