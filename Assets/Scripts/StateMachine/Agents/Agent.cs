using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour, IVelocityProvider
{
    public Vector3 Velocity => _velocity;
    protected Vector3 _velocity;
    public static List<Agent> AllAgents { get; private set; } = new List<Agent>();

    public void ApplyVelocity(Vector3 steering, float maxSpeed)
    {
        _velocity += steering;
        
        _velocity = Vector3.ClampMagnitude(_velocity,maxSpeed); 

        transform.position += _velocity * Time.deltaTime;

        if (_velocity != Vector3.zero)
            transform.forward = _velocity;

        transform.position = Bounds.Instance.OutOfBounds(transform.position);
    }
    protected virtual void Awake()
    {
        AllAgents.Add(this);
    }

    protected virtual void OnDestroy()
    {
        AllAgents.Remove(this);
    }
}
