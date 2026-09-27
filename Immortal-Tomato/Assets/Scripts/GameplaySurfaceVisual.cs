using UnityEngine;

/// <summary>
/// Authored foreground lip for a measured gameplay surface.  Physics remains
/// on the parent BoxCollider2D; this component only records the source-art
/// category and the renderers used to make that support visible in front of
/// the panorama and behind characters.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameplaySurfaceVisual : MonoBehaviour
{
    public string SurfaceName;
    public string PaintedReference;
    public string MaterialCategory;
    // Retained root SpriteRenderer. It deliberately stays disabled because
    // its bounds equal the physics box and must never become visible art.
    public SpriteRenderer BodyRenderer;
    public SpriteRenderer FasciaRenderer;
    public SpriteRenderer HighlightRenderer;

    public bool IsConfigured => BodyRenderer != null && FasciaRenderer != null && HighlightRenderer != null &&
                                !BodyRenderer.enabled && FasciaRenderer.enabled && HighlightRenderer.enabled &&
                                FasciaRenderer.sortingLayerName == "Default" &&
                                HighlightRenderer.sortingLayerName == "Default";
}
