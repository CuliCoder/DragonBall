using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "SkinData", menuName = "ScriptableObjects/SkinData")]
public class SkinData : ScriptableObject
{
    public SkinName skinName;
    public List<SpriteAnimation> HeadAnimations;

    public List<SpriteAnimation> BodyAnimations;

    public List<SpriteAnimation> LegsAnimations;
    public SpriteAnimation GetSpriteAnimation(BodyPart bodyPart, AnimationType animationType)
    {
        List<SpriteAnimation> animations = null;
        switch (bodyPart)
        {
            case BodyPart.Head:
                animations = HeadAnimations;
                break;
            case BodyPart.Body:
                animations = BodyAnimations;
                break;
            case BodyPart.Legs:
                animations = LegsAnimations;
                break;
        }
        if (animations != null)
        {
            foreach (var skinAnimation in animations)
            {
                if (skinAnimation.animationType == animationType)
                {
                    return skinAnimation;
                }
            }
        }
        return null;
    }
}