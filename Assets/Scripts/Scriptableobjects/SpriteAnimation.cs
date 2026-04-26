using UnityEngine;
[CreateAssetMenu(fileName = "SpriteAnimation", menuName = "ScriptableObjects/SpriteAnimation")]
public class SpriteAnimation : ScriptableObject
{
    public AnimationType animationType;
    public Sprite[] frames;
    public float frameRate = 0.1f;
    public bool loop = true;
    public Vector2 offset;
}