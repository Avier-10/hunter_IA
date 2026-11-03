using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour, IVelocityProvider
{
    public Vector3 Velocity => _velocity;
    protected Vector3 _velocity;
    public static List<Agent> AllAgents { get; } = new List<Agent>();

    // Domain reload may be disabled, so statics must be cleared explicitly.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        AllAgents.Clear();
    }

    public void ApplyVelocity(Vector3 steering, float maxSpeed)
    {
        _velocity += steering;
        
        _velocity = Vector3.ClampMagnitude(_velocity,maxSpeed); 

        transform.position += _velocity * Time.deltaTime;

        if (_velocity != Vector3.zero)
            transform.forward = _velocity;

        if (UsesBounds)
            transform.position = Bounds.Instance.OutOfBounds(transform.position);
    }

    // The wrap-around Bounds is meant for the Boids; navigating agents (wolves, hunter) opt out.
    protected virtual bool UsesBounds => true;
    protected virtual void Awake()
    {
        AllAgents.Add(this);
    }

    protected virtual void OnDestroy()
    {
        AllAgents.Remove(this);
    }
}
