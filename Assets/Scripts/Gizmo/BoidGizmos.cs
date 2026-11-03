using UnityEngine;

// Draws the Boid radii: separation, alignment, cohesion and the vision radii that trigger/stop evading the hunter.
// Always drawn by default; untick Draw Always to draw only while selected so a big flock stays readable.
[RequireComponent(typeof(FSMAgentEscapist))]
public class BoidGizmos : MonoBehaviour
{
    [SerializeField] private bool _drawAlways = true;
    [SerializeField] private Color _separationColor = new Color(1f, 0.35f, 0.35f);
    [SerializeField] private Color _alignmentColor = new Color(0.35f, 0.8f, 1f);
    [SerializeField] private Color _cohesionColor = new Color(0.4f, 1f, 0.5f);
    [SerializeField] private Color _visionEnterColor = new Color(1f, 0.9f, 0.2f);
    [SerializeField] private Color _visionExitColor = new Color(1f, 0.6f, 0.1f);

    private FSMAgentEscapist _boid;

    private void OnDrawGizmos()
    {
        if (_drawAlways) Draw();
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawAlways) Draw();
    }

    private void Draw()
    {
        if (_boid == null) _boid = GetComponent<FSMAgentEscapist>();

        FlockingData flocking = _boid.Flocking;
        if (flocking == null) return;

        Vector3 center = transform.position;
        VisionSensorGizmos.DrawCircle(center, flocking.separationRadius, _separationColor);
        VisionSensorGizmos.DrawCircle(center, flocking.alignmentRadius, _alignmentColor);
        VisionSensorGizmos.DrawCircle(center, flocking.cohesionRadius, _cohesionColor);
        VisionSensorGizmos.DrawCircle(center, _boid.VisionRadiusEnter, _visionEnterColor);
        VisionSensorGizmos.DrawCircle(center, _boid.VisionRadiusExit, _visionExitColor);
    }
}
