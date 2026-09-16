using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour
{
    [Header("Trap")]
    [SerializeField] private float _trapDuration = 2f;
    [SerializeField] private float _trapDamage = 999f;

    private FSMAgentEscapist _currentEscapist;
    private float _trapTimer;

    public bool IsOccupied => _currentEscapist != null;
    public FSMAgentEscapist CurrentEscapist => _currentEscapist;

    public static List<Trap> AllObjects { get; private set; }
        = new List<Trap>();

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

        Debug.Log(
            $"{escapist.name} quedó atrapada durante {_trapDuration} segundos."
        );

        return true;
    }

    public void Release(FSMAgentEscapist escapist)
    {
        if (_currentEscapist != escapist)
            return;

        Debug.Log($"{escapist.name} liberó la trampa.");

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

            Debug.Log($"{escapist.name} fue atrapada por la trampa.");

            escapist.TakeDamage(_trapDamage);
        }

        Debug.Log($"{name} fue consumida.");

        Destroy(gameObject);
    }
}