using UnityEngine;
public class BoomState : IState
{
    private PlayerController player;
    private PlayerStateManager stateManager;
    private BaseSkill skill;
    private float currentFrame = 0f;
    public BoomState(PlayerController player, PlayerStateManager stateManager)
    {
        skill = stateManager.skills.skills[0];
        this.player = player;
        this.stateManager = stateManager;
    }
    public void Enter()
    {
        stateManager.SetAnimation(AnimationType.Boom);
        currentFrame = 0f;
        stateManager.playAnimation(skill.animation);
        SoundManager.Instance.PlaySfx(skill.GetAudioClip(0));
    }

    public void Exit()
    {

    }

    public void Update()
    {
        if (currentFrame > skill.totalFrame)
        {
            SoundManager.Instance.PlaySfx(skill.GetAudioClip(1));

            if (player is LocalPlayerController localPlayer)
            {
                localPlayer.isBoom = false;
            }
            stateManager.ChangeState(stateManager.idleState);
        }
        currentFrame++;
    }
}