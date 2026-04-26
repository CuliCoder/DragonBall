using UnityEngine;
public class IdleState : IState
{
    private PlayerController player;
    private PlayerStateManager stateManager;
    public IdleState(PlayerController player, PlayerStateManager stateManager)
    {
        this.player = player;
        this.stateManager = stateManager;
    }
    public void Enter()
    {
        stateManager.SetAnimation(AnimationType.Idle);
    }

    public void Exit()
    {

    }

    public void Update()
    {
        if (InputManager.Instance.MoveInput != Vector2.zero)
        {
            stateManager.ChangeState(stateManager.runState);
        }
    }
}