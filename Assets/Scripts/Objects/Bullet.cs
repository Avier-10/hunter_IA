using UnityEngine;

public class Bullet : MonoBehaviour
{
    private FSMAgentEscapist _target;

    [SerializeField] private float _speed = 15f;
    [Tooltip("Distance to the target at which the bullet counts as a hit.")]
    [SerializeField] private float _hitDistance = 0.2f;

    public void Initialize(FSMAgentEscapist target)
    {
        _target = target;
    }

    private void Update()
    {
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direction =
            _target.transform.position - transform.position;

        float distance = direction.magnitude;

        if (distance <= _hitDistance)
        {
            HitTarget();
            return;
        }

        transform.position +=
            direction.normalized * _speed * Time.deltaTime;

        transform.forward = direction.normalized;
    }

    private void HitTarget()
    {
        _target.TakeDamage(FSMAgentEscapist.LethalDamage);

        Destroy(gameObject);
    }
}
