using UnityEngine;

/// <summary>
/// Fits one keyed Flame animation into one horizontal slot of a measured
/// painted fire rectangle. Flame source frames include a large magenta canvas;
/// their visible pixels are therefore positioned from the keyed bounds rather
/// than from the sprite's full canvas bounds.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(AmbientSpriteAnimator))]
public sealed class AnimatedFirePlacement : MonoBehaviour
{
    // Union of all non-magenta Flame_01..08 pixels, measured in their native
    // 272 x 724 frame canvas. The coordinates are image pixels (top-left
    // origin) and deliberately include the tallest/widest animation frame.
    public const float KeyLeftPixel = 55f;
    public const float KeyRightPixel = 230f;
    public const float KeyTopPixel = 175f;
    public const float KeyBottomPixel = 518f;
    public const float FrameWidthPixels = 272f;
    public const float FrameHeightPixels = 724f;
    public const float FramePixelsPerUnit = 100f;

    [SerializeField] string fireName;
    [SerializeField] Bounds targetBounds;
    [SerializeField] int slotIndex;
    [SerializeField] int slotCount;

    public string FireName => fireName;
    public Bounds TargetBounds => targetBounds;
    public int SlotIndex => slotIndex;
    public int SlotCount => slotCount;

    public Bounds KeyedVisibleBounds
    {
        get
        {
            float scale = Mathf.Abs(transform.lossyScale.x);
            float localLeft = (KeyLeftPixel - FrameWidthPixels * .5f) / FramePixelsPerUnit * scale;
            float localRight = (KeyRightPixel - FrameWidthPixels * .5f) / FramePixelsPerUnit * scale;
            float localBottom = (FrameHeightPixels - KeyBottomPixel) / FramePixelsPerUnit * scale;
            float localTop = (FrameHeightPixels - KeyTopPixel) / FramePixelsPerUnit * scale;
            Vector3 root = transform.position;
            return new Bounds(
                new Vector3(root.x + (localLeft + localRight) * .5f, root.y + (localBottom + localTop) * .5f, root.z),
                new Vector3(localRight - localLeft, localTop - localBottom, 0f));
        }
    }

    public void Configure(GameplayArtLayout.FireSpec fire, int index, int count)
    {
        fireName = fire.Name;
        targetBounds = fire.TargetBounds;
        slotIndex = index;
        slotCount = count;

        // Scale by the measured firebox height, never stretching the source
        // art. Slots are then spread across a wide burner row, keeping every
        // visible keyed pixel inside its corresponding painted rectangle.
        float scale = targetBounds.size.y / ((KeyBottomPixel - KeyTopPixel) / FramePixelsPerUnit);
        float visibleWidth = (KeyRightPixel - KeyLeftPixel) / FramePixelsPerUnit * scale;
        float localLeft = (KeyLeftPixel - FrameWidthPixels * .5f) / FramePixelsPerUnit * scale;
        float localBottom = (FrameHeightPixels - KeyBottomPixel) / FramePixelsPerUnit * scale;
        float visibleLeft = count == 1
            ? targetBounds.center.x - visibleWidth * .5f
            : Mathf.Lerp(targetBounds.min.x, targetBounds.max.x - visibleWidth, index / (float)(count - 1));
        transform.position = new Vector3(visibleLeft - localLeft, targetBounds.min.y - localBottom, 1f);
        transform.localScale = Vector3.one * scale;
    }

    public static int SlotsFor(GameplayArtLayout.FireSpec fire)
    {
        Bounds target = fire.TargetBounds;
        float flameAspect = (KeyRightPixel - KeyLeftPixel) / (KeyBottomPixel - KeyTopPixel);
        return Mathf.Max(1, Mathf.FloorToInt(target.size.x / Mathf.Max(.0001f, target.size.y * flameAspect)));
    }
}
