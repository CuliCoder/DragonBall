using UnityEngine;
public class PlayerStateManager : MonoBehaviour
{
    private PlayerController player;
    private PlayerAnimationManager animationManager;
    public IState currentState { get; private set; }
    public IState idleState { get; private set; }
    public IState runState { get; private set; }
    private void Awake()
    {
        if (player == null)
        {
            player = GetComponent<PlayerController>();
            animationManager = GetComponent<PlayerAnimationManager>();
        }
        idleState = new IdleState(player, this);
        runState = new RunState(player, this);
    }
    private void Start()
    {
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