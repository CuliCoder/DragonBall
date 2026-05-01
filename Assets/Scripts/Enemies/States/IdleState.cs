public class IdleState_Enemy : IState
{
    private EnemyController enemy;
    private EnemyStateManager stateManager;
    public IdleState_Enemy(EnemyController enemy, EnemyStateManager stateManager)
    {
        this.enemy = enemy;
        this.stateManager = stateManager;
    }
    public void Enter()
    {
        stateManager.StopAnimation();
        stateManager.SetAnimation(AnimationType.Idle);
    }

    public void Exit()
    {

    }

    public void Update()    
    {

    }
}