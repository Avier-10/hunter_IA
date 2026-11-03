using UnityEngine;

// Entered only through FSMAgentHunter.Capture. Invisible and inert for RespawnTime, then back at Start.
public class DeadState : HunterState
{
    private float _timer;

    public DeadState(FSMAgentHunter agent, StateMachine stateMachine) : base(agent, stateMachine) { }

    protected override void OnEnter()
    {
        Hunter.SetPresent(false);
        Hunter.StopMoving();
        _timer = Hunter.RespawnTime;
    }

    protected override void OnExit()
    {
        Hunter.SetPresent(true);
    }

    // Dead yields to nothing: no global transitions are evaluated here.
    public override void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        Hunter.ReturnToStart();
        Hunter.ChangeState(HunterStates.Search);
    }
}
