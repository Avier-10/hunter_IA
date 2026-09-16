using UnityEngine;
using System.Collections;
public class DeathState : State
{
    private FSMAgentEscapist _agent;
    private DeathData _data;

    public DeathState(FSMAgentEscapist agent, DeathData data, StateMachine stateMachine)
        : base(stateMachine)
    {
        _agent = agent;
        _data = data;
    }

    public override void Enter()
    {
        _agent.SetDeathColor(_data.deathColor);

        _agent.SetVisible(false);

        _agent.StartCoroutine(RespawnDelay());

        Debug.Log("Escapist: Entre Death");
    }

    public override void Exit()
    {
        _agent.RestoreColor();
        _agent.SetVisible(true);

        Debug.Log("Escapist: Sali Death");
    }

    public override void Update() {}

    private IEnumerator RespawnDelay()
    {
        yield return new WaitForSeconds(_data.respawnDelay);

        Vector2 randomCircle = Random.insideUnitCircle * _data.spawnAreaRadius;

        _agent.transform.position = new Vector3(randomCircle.x,_agent.transform.position.y,randomCircle.y);

        _agent.ResetHealth();

        _agent.InitializeMovement();

        _agent.ChangeState(EscapistStates.Flocking);
    }
}

[System.Serializable]
public class DeathData
{
    [Header("Respawn")]
    public float respawnDelay = 3f;
    public float spawnAreaRadius = 20f;

    [Header("Feedback")]
    public Color deathColor = Color.red;
}
