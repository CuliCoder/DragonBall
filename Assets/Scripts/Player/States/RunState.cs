using UnityEngine;
public class RunState : IState
{
    private PlayerController player;
    private PlayerStateManager stateManager;
    public RunState(PlayerController player, PlayerStateManager stateManager)
    {
        this.player = player;
        this.stateManager = stateManager;
    }
    public void Enter()
    {
        stateManager.SetAnimation(AnimationType.Run);
    }

    public void Exit()
    {

    }

    public void Update()
    {
        if (InputManager.Instance.MoveInput == Vector2.zero)
        {
            stateManager.ChangeState(stateManager.idleState);
        }
    }
}