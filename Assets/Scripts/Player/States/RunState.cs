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
        stateManager.StopAnimation();
        stateManager.SetAnimation(AnimationType.Run);
    }

    public void Exit()
    {

    }

    public void Update()
    {
        if (player is LocalPlayerController localPlayer)
        {
            if (localPlayer.Velocity.x == 0)
            {
                stateManager.ChangeState(stateManager.idleState);
            }
            else if (localPlayer.isBoom)
            {
                localPlayer.isBoom = true;
                stateManager.ChangeState(stateManager.boomState);
            }
        }
        else
        {

        }

    }
}