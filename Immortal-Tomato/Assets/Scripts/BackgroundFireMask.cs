using UnityEngine;

/// <summary>Marks a deterministic source-art patch that hides painted fire pixels.</summary>
[DisallowMultipleComponent]
public sealed class BackgroundFireMask : MonoBehaviour
{
    public string FireName;
    public Rect SourcePixels;
    public SpriteRenderer Renderer;
}
