using UnityEngine;
public class SpriteAnimator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private SpriteAnimation currentAnimation;
    private int currentFrame;
    private float timer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (currentAnimation == null) return;

        timer += Time.deltaTime;
        if (timer >= currentAnimation.frameRate)
        {
            timer = 0f;
            currentFrame++;
            if (currentFrame >= currentAnimation.frames.Length)
            {
                if (currentAnimation.loop)
                {
                    currentFrame = 0;
                }
                else
                {
                    currentFrame = currentAnimation.frames.Length - 1; // Stay on last frame
                }
            }
            spriteRenderer.sprite = currentAnimation.frames[currentFrame];
        }
    }

    public void PlayAnimation(SpriteAnimation animation)
    {
        // if (currentAnimation == animation) return;
        currentAnimation = animation;
        currentFrame = 0;
        timer = 0f;
        transform.localPosition = (Vector3)currentAnimation.offset;
        spriteRenderer.sprite = currentAnimation.frames[currentFrame];
    }
}