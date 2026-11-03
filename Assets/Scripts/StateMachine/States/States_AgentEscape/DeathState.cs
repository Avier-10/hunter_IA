using UnityEngine;
using System.Collections;
public class DeathState : State
{
    private FSMAgentEscapist _agent;
    private DeathData _data;
    private Coroutine _expiry;

    public DeathState(FSMAgentEscapist agent, DeathData data, StateMachine stateMachine)
        : base(stateMachine)
    {
        _agent = agent;
        _data = data;
    }

    public override void Enter()
    {
        _agent.SetVisible(false);
        _agent.SpawnRemains(_data.remainsPrefab);

        _expiry = _agent.StartCoroutine(ExpireIfUncollected());
    }

    public override void Exit()
    {
        if (_expiry != null) _agent.StopCoroutine(_expiry);
        _expiry = null;

        _agent.ClearRemains();
        _agent.SetVisible(true);
    }

    public override void Update() {}

    // The hunter has to collect it, so it stays dead until then; a boid nobody collects still comes back.
    private IEnumerator ExpireIfUncollected()
    {
        yield return new WaitForSeconds(_data.uncollectedTimeout);

        // Collected boids respawn through FSMAgentEscapist.Collect instead.
        if (_agent.IsCollected) yield break;

        _agent.RespawnInCoto();
        _agent.ChangeState(EscapistStates.Flocking);
    }
}

[System.Serializable]
public class DeathData
{
    [Header("Respawn")]
    [Tooltip("Seconds an eliminated boid waits to be collected before coming back on its own.")]
    public float uncollectedTimeout = 30f;

    [Header("Feedback")]
    [Tooltip("Left where the boid fell (e.g. a red cube) until it is collected or respawns.")]
    public GameObject remainsPrefab;
}
