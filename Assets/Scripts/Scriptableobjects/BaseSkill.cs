using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New BaseSkill", menuName = "Skills/BaseSkill")]
public class BaseSkill : ScriptableObject
{
    public AnimationClip animation;
    public float totalFrame;
    public List<AudioClip> audioClips;
    public AudioClip GetAudioClip(int index)
    {
        if (index < 0 || index >= audioClips.Count)
        {
            Debug.LogWarning("Audio clip index out of range. Returning null.");
            return null;
        }
        return audioClips[index];
    }
}