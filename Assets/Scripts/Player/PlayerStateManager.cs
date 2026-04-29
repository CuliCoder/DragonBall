using UnityEngine;
public class PlayerStateManager : MonoBehaviour
{
    private PlayerController player;
    private PlayerAnimationManager animationManager;
    public IState currentState { get; private set; }
    public IState idleState { get; private set; }
    public IState runState { get; private set; }
    public void Init(PlayerController playerController, PlayerAnimationManager animManager)
    {
        player = playerController;
        animationManager = animManager;
        idleState = new IdleState(player, this);
        runState = new RunState(player, this);
        currentState = idleState;
        currentState.Enter();
    }
    public void ChangeState(IState newState)
    {
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
}