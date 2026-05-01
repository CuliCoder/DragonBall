using UnityEngine;
public class RunState_Enemy : IState
{
    private EnemyController enemy;
    private EnemyStateManager stateManager;
    public RunState_Enemy(EnemyController enemy, EnemyStateManager stateManager)
    {
        this.enemy = enemy;
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

    }
}