using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AmbientSpriteAnimator : MonoBehaviour
{
    [SerializeField] Sprite[] frames;
    [SerializeField] float fps = 8f;
    [SerializeField] bool loop = true;
    [SerializeField] float startOffset;
    SpriteRenderer spriteRenderer;

    public Sprite[] Frames => frames;
    public float FramesPerSecond => fps;

    public void Configure(Sprite[] newFrames, float newFps, bool shouldLoop, float offset)
    {
        frames = newFrames;
        fps = newFps;
        loop = shouldLoop;
        startOffset = offset;
    }

    void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        int frame = Mathf.FloorToInt((Time.time + startOffset) * fps);
        if (loop) frame %= frames.Length;
        else frame = Mathf.Min(frame, frames.Length - 1);
        spriteRenderer.sprite = frames[frame];
    }
}
