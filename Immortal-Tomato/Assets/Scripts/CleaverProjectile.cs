using System;
using UnityEngine;

/// <summary>Left-moving, spinning chef projectile with bounded lifetime.</summary>
[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public sealed class CleaverProjectile : MonoBehaviour
{
    const string CleaverFramesResource = "EnemyFrames/Chef/Cleaver";

    [SerializeField] float lifetimeSeconds = 3f;
    [SerializeField] float spinDegreesPerSecond = 900f;
    [SerializeField] float spinFrameSeconds = .075f;

    Vector2 velocity;
    GameObject source;
    float expiresAt;
    bool spent;
    SpriteRenderer spriteRenderer;
    Sprite[] spinFrames = Array.Empty<Sprite>();
    float spawnedAt;

    public Vector2 Velocity => velocity;
    public bool MovesLeft => velocity.x < 0f;
    public bool IsOwnedBy(GameObject enemy) => source == enemy;

    /// <summary>Immediately neutralizes a knife when its owning chef dies.</summary>
    public void StopThreat()
    {
        if (spent) return;
        spent = true;
        Collider2D hitbox = GetComponent<Collider2D>();
        if (hitbox != null) hitbox.enabled = false;
        Destroy(gameObject);
    }

    public void Configure(Vector2 initialVelocity, GameObject sourceEnemy, Sprite fallbackSprite)
    {
        velocity = initialVelocity;
        source = sourceEnemy;
        spawnedAt = Time.time;
        expiresAt = Time.time + lifetimeSeconds;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spinFrames = EnemySpriteLoader.LoadFrames(CleaverFramesResource, "Cleaver", new Vector2(.5f, .5f));
        if (spinFrames.Length == EnemySpriteLoader.ClipFrameCount)
        {
            spriteRenderer.sprite = spinFrames[0];
            transform.rotation = Quaternion.identity;
        }
        else if (spriteRenderer != null)
            spriteRenderer.sprite = fallbackSprite;
    }

    void Update()
    {
        if (Time.time >= expiresAt)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(velocity * Time.deltaTime);
        if (spinFrames.Length == EnemySpriteLoader.ClipFrameCount)
        {
            int frame = Mathf.FloorToInt((Time.time - spawnedAt) / Mathf.Max(.025f, spinFrameSeconds)) % spinFrames.Length;
            spriteRenderer.sprite = spinFrames[frame];
        }
        else
            transform.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (spent || other == null || other.gameObject == source) return;
        TomatoGame tomato = other.GetComponent<TomatoGame>();
        if (tomato != null)
        {
            spent = true;
            tomato.HazardRespawn(tomato.CurrentCheckpoint);
            Destroy(gameObject);
            return;
        }
        // Do not leave knives alive after striking a painted route surface.
        if (!other.isTrigger) Destroy(gameObject);
    }
}
