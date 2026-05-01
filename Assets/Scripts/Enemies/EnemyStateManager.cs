using UnityEngine;
public class EnemyStateManager : MonoBehaviour
{
    private EnemyController enemy;
    private PlayerAnimationManager animationManager;
    [SerializeField]
    public Skills skills;
    public Animator animator { get; private set; }
    public IState currentState { get; private set; }
    public IState idleState { get; private set; }
    public IState runState { get; private set; }
    public IState dieState { get; private set; }
    public void Init(EnemyController enemyController, PlayerAnimationManager animManager)
    {
        enemy = enemyController;
        animationManager = animManager;
        animator = GetComponent<Animator>();
        idleState = new IdleState_Enemy(enemy, this);
        runState = new RunState_Enemy(enemy, this);
        dieState = new DieState_Enemy(enemy, this);
        // boomState = new BoomState_Enemy(enemy, this);
        currentState = idleState;
        currentState.Enter();
    }
    public void ChangeState(IState newState)
    {
        if(newState == currentState)
        {
            return;
        }
        if (currentState != null)
        {
            currentState.Exit();
        }
        currentState = newState;
        currentState.Enter();
    }

    public void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }
    public void SetAnimation(AnimationType type)
    {
        animationManager.PlayAnimation(type);
    }
    public void playAnimation(AnimationClip animation)
    {
        animator.Play(animation.name);
    }
    public void StopAnimation()
    {
        animator.Play("noneenemy");
    }
}