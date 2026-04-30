using UnityEngine;
public class DieState_Enemy : IState
{
    private EnemyController enemy;
    private EnemyStateManager stateManager;
    public DieState_Enemy(EnemyController enemy, EnemyStateManager stateManager)
    {
        this.enemy = enemy;
        this.stateManager = stateManager;
    }
    public void Enter()
    {
        stateManager.StopAnimation();
        stateManager.SetAnimation(AnimationType.Die);
    }

    public void Exit()
    {

    }

    public void Update()
    {
    }
}