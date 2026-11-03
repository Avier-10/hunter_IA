using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour
{
    [Header("Trap")]
    [SerializeField] private float _trapDuration = 2f;
    [SerializeField] private float _trapDamage = FSMAgentEscapist.LethalDamage;

    private FSMAgentEscapist _currentEscapist;
    private float _trapTimer;

    public bool IsOccupied => _currentEscapist != null;
    public FSMAgentEscapist CurrentEscapist => _currentEscapist;

    public static List<Trap> AllObjects { get; }
        = new List<Trap>();

    // Domain reload may be disabled, so statics must be cleared explicitly.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        AllObjects.Clear();
    }

    private void Awake()
    {
        AllObjects.Add(this);
    }

    private void OnDestroy()
    {
        AllObjects.Remove(this);
    }

    public bool TryOccupy(FSMAgentEscapist escapist)
    {
        if (IsOccupied)
            return false;

        _currentEscapist = escapist;
        _trapTimer = _trapDuration;

        return true;
    }

    public void Release(FSMAgentEscapist escapist)
    {
        if (_currentEscapist != escapist)
            return;

        _currentEscapist = null;
        _trapTimer = 0f;
    }

    private void Update()
    {
        if (!IsOccupied)
            return;

        _trapTimer -= Time.deltaTime;

        if (_trapTimer <= 0f)
        {
            Consume();
        }
    }

    public void Consume()
    {
        if (_currentEscapist != null)
        {
            FSMAgentEscapist escapist = _currentEscapist;

            _currentEscapist = null;

            escapist.TakeDamage(_trapDamage);
        }

        Destroy(gameObject);
    }
}