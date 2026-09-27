using UnityEngine;

/// <summary>Compact, detached world-space health bar for a 2D enemy.</summary>
public sealed class EnemyHealthBar : MonoBehaviour
{
    SpriteRenderer ownerRenderer;
    SpriteRenderer background;
    SpriteRenderer fill;
    Transform target;
    int maxHealth;
    int currentHealth;

    public float NormalizedFill => maxHealth <= 0 ? 0f : currentHealth / (float)maxHealth;
    public SpriteRenderer FillRenderer => fill;

    public static EnemyHealthBar Create(Component owner, SpriteRenderer renderer, int maximum)
    {
        var root = new GameObject(owner.name + " Health Bar", typeof(EnemyHealthBar));
        EnemyHealthBar bar = root.GetComponent<EnemyHealthBar>();
        bar.target = owner.transform;
        bar.ownerRenderer = renderer;
        bar.maxHealth = Mathf.Max(1, maximum);
        bar.currentHealth = bar.maxHealth;
        Sprite pixel = CreatePixel();
        bar.background = CreatePart("Background", root.transform, pixel, new Color(.055f, .01f, .01f, .92f));
        bar.fill = CreatePart("Fill", root.transform, pixel, new Color(.92f, .16f, .1f, 1f));
        bar.background.sortingLayerID = renderer.sortingLayerID;
        bar.fill.sortingLayerID = renderer.sortingLayerID;
        bar.background.sortingOrder = renderer.sortingOrder + 8;
        bar.fill.sortingOrder = renderer.sortingOrder + 9;
        bar.RefreshPosition();
        bar.RefreshFill();
        return bar;
    }

    static SpriteRenderer CreatePart(string name, Transform parent, Sprite sprite, Color color)
    {
        SpriteRenderer renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        renderer.transform.SetParent(parent, false);
        renderer.sprite = sprite;
        renderer.color = color;
        return renderer;
    }

    static Sprite CreatePixel()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "EnemyHealthBarPixel", filterMode = FilterMode.Point };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
    }

    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        RefreshFill();
    }

    public void HideAndDispose()
    {
        if (gameObject != null) Destroy(gameObject);
    }

    void LateUpdate()
    {
        if (target == null || ownerRenderer == null || !ownerRenderer.enabled)
        {
            Destroy(gameObject);
            return;
        }
        RefreshPosition();
    }

    void RefreshPosition()
    {
        Bounds bounds = ownerRenderer.bounds;
        // Detached from the animated/scaled enemy hierarchy: bar dimensions
        // stay screen-readable while only its world anchor follows the actor.
        transform.position = new Vector3(bounds.center.x, bounds.max.y + .2f, 0f);
        background.transform.localScale = new Vector3(1.15f, .105f, 1f);
    }

    void RefreshFill()
    {
        if (fill == null || background == null) return;
        float ratio = NormalizedFill;
        float width = 1.05f * ratio;
        fill.transform.localScale = new Vector3(width, .062f, 1f);
        fill.transform.localPosition = new Vector3(-.525f + width * .5f, 0f, -.01f);
    }
}
