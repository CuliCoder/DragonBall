using UnityEngine;
public class PlayerAnimationManager : MonoBehaviour
{
    [SerializeField]
    private SpriteAnimator HeadAnimator;
    [SerializeField]
    private SpriteAnimator BodyAnimator;
    [SerializeField]
    private SpriteAnimator LegsAnimator;
    [SerializeField]
    private SkinData skinData;
    public void PlayAnimation(AnimationType animationType)
    {
        HeadAnimator.PlayAnimation(skinData.GetSpriteAnimation(BodyPart.Head, animationType));
        BodyAnimator.PlayAnimation(skinData.GetSpriteAnimation(BodyPart.Body, animationType));
        LegsAnimator.PlayAnimation(skinData.GetSpriteAnimation(BodyPart.Legs, animationType));
    }
}