using System;
using UnityEngine;

/// <summary>
/// A stationary, left-facing kitchen enemy.  The resource sprites are loaded
/// at runtime so scene authoring is safe while art is being produced.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D))]
public sealed class ChefEnemy : MonoBehaviour
{
    const string IdleResource = "Enemies/ChefIdle";
    const string IdleBResource = "Enemies/ChefIdleB";
    const string WindupResource = "Enemies/ChefWindup";
    const string ThrowResource = "Enemies/ChefThrow";
    const string CleaverResource = "Enemies/Cleaver";
    const string IdleFramesResource = "EnemyFrames/Chef/Idle";
    const string ThrowFramesResource = "EnemyFrames/Chef/Throw";
    const string CleaverFramesResource = "EnemyFrames/Chef/Cleaver";

    [SerializeField] float detectionDistance = 9f;
    [SerializeField, Min(0f)] float awarenessSeconds = .85f;
    [SerializeField, Min(1)] int maxHealth = 24;
    [SerializeField] float telegraphSeconds = .55f;
    [SerializeField] float idleFrameSeconds = .42f;
    [SerializeField] float fullFrameIdleFrameSeconds = .12f;
    [SerializeField] float throwFrameSeconds = .14f;
    [SerializeField, Range(0, EnemySpriteLoader.ClipFrameCount - 1)] int cleaverReleaseFrame = 4;
    [SerializeField] float throwPoseSeconds = .18f;
    [SerializeField] float cooldownSeconds = 2.4f;
    [SerializeField] float projectileSpeed = 10f;
    [SerializeField] Vector2 throwOffset = new(-.72f, .7f);

    SpriteRenderer spriteRenderer;
    BoxCollider2D hitbox;
    Sprite idleSprite;
    Sprite idleBSprite;
    Sprite windupSprite;
    Sprite throwSprite;
    Sprite cleaverSprite;
    Sprite[] idleFrames = Array.Empty<Sprite>();
    Sprite[] throwFrames = Array.Empty<Sprite>();
    Vector3 restingPosition;
    float nextThrowAt;
    float awarenessStartedAt = -1f;
    float throwAt = -1f;
    float throwPoseUntil = -1f;
    float throwClipStartedAt = -1f;
    bool throwClipActive;
    bool cleaverReleased;
    bool warnedMissingIdle;
    bool warnedMissingCleaver;
    bool dying;
    float fadeStartedAt = -1f;
    int currentHealth;
    EnemyHealthBar healthBar;

    public bool FacesLeft => transform.localScale.x >= 0f && !spriteRenderer.flipX;
    public bool IsDying => dying;
    public bool IsHazardActive => !dying && hitbox != null && hitbox.enabled;
    public float VisualAlpha => spriteRenderer != null ? spriteRenderer.color.a : 0f;
    public float DetectionDistance => detectionDistance;
    public float AwarenessSeconds => awarenessSeconds;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public EnemyHealthBar HealthBar => healthBar;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        restingPosition = transform.position;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        spriteRenderer.flipX = false; // Enemy source art is authored facing left.
        spriteRenderer.sortingOrder = 20;
        var body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        hitbox = GetComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        currentHealth = Mathf.Max(1, maxHealth);
        idleSprite = EnemySpriteLoader.Load(IdleResource, new Vector2(.5f, 0f));
        idleBSprite = EnemySpriteLoader.Load(IdleBResource, new Vector2(.5f, 0f));
        windupSprite = EnemySpriteLoader.Load(WindupResource, new Vector2(.5f, 0f));
        throwSprite = EnemySpriteLoader.Load(ThrowResource, new Vector2(.5f, 0f));
        cleaverSprite = EnemySpriteLoader.Load(CleaverResource, new Vector2(.5f, .5f));
        idleFrames = EnemySpriteLoader.LoadFrames(IdleFramesResource, "Idle", new Vector2(.5f, 0f));
        throwFrames = EnemySpriteLoader.LoadFrames(ThrowFramesResource, "Throw", new Vector2(.5f, 0f));
        ApplySprite(CurrentIdleSprite(), IdleResource, ref warnedMissingIdle);
        healthBar = EnemyHealthBar.Create(this, spriteRenderer, currentHealth);
    }

    void Update()
    {
        if (dying)
        {
            FadeAndRemove();
            return;
        }
        // Full-canvas sprites share a feet-baseline pivot, so the visual can
        // animate without moving this trigger/collider root.
        transform.position = restingPosition;
        // Keep awareness current even while a previous throw pose is finishing.
        // If the tomato leaves detection, the next sighting starts a fresh delay.
        TomatoGame tomato = FindTomatoToLeft();
        bool targetAware = HasBecomeAware(tomato);
        if (throwClipActive)
        {
            UpdateThrowClip();
            return;
        }

        // Preserve the shipped pose sequence while the new full-frame art is
        // absent or only partially imported.
        if (throwAt > 0f)
        {
            ApplySprite(windupSprite ?? throwSprite ?? CurrentIdleSprite(), IdleResource, ref warnedMissingIdle);
            if (Time.time >= throwAt)
            {
                ThrowCleaver();
                throwAt = -1f;
                throwPoseUntil = Time.time + throwPoseSeconds;
                nextThrowAt = Time.time + cooldownSeconds;
            }
            return;
        }

        if (Time.time < throwPoseUntil)
        {
            ApplySprite(throwSprite ?? windupSprite ?? CurrentIdleSprite(), IdleResource, ref warnedMissingIdle);
            return;
        }

        ApplySprite(CurrentIdleSprite(), IdleResource, ref warnedMissingIdle);
        if (targetAware && Time.time >= nextThrowAt)
        {
            if (throwFrames.Length == EnemySpriteLoader.ClipFrameCount)
                StartThrowClip();
            else
                throwAt = Time.time + telegraphSeconds;
        }
    }

    /// <summary>Called by a swept, on-camera Uzi tracer. Returns false after death has begun.</summary>
    public bool TakeBulletHit(int damage)
    {
        if (dying) return false;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, damage));
        if (healthBar != null) healthBar.SetHealth(currentHealth);
        if (currentHealth > 0) return true;
        dying = true;
        fadeStartedAt = Time.time;
        // Stop every branch which could release a knife on a later frame.
        throwAt = throwPoseUntil = throwClipStartedAt = -1f;
        throwClipActive = false;
        cleaverReleased = true;
        hitbox.enabled = false;
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }
        foreach (CleaverProjectile projectile in FindObjectsByType<CleaverProjectile>(FindObjectsSortMode.None))
            if (projectile.IsOwnedBy(gameObject)) projectile.StopThreat();
        if (healthBar != null) healthBar.HideAndDispose();
        return true;
    }

    bool HasBecomeAware(TomatoGame tomato)
    {
        if (tomato == null)
        {
            awarenessStartedAt = -1f;
            return false;
        }
        if (awarenessStartedAt < 0f) awarenessStartedAt = Time.time;
        return Time.time - awarenessStartedAt >= awarenessSeconds;
    }

    public bool IsHitByBulletSegment(Vector2 origin, Vector2 direction, float distance)
    {
        return TryGetBulletHitDistance(origin, direction, distance, out _);
    }

    public bool TryGetBulletHitDistance(Vector2 origin, Vector2 direction, float distance, out float hitDistance)
    {
        hitDistance = 0f;
        if (dying || hitbox == null || !hitbox.enabled) return false;
        Bounds bounds = hitbox.bounds;
        return TrySegmentBoundsDistance(origin, direction, distance, bounds, out hitDistance);
    }

    static bool TrySegmentBoundsDistance(Vector2 origin, Vector2 direction, float distance, Bounds bounds, out float hitDistance)
    {
        hitDistance = 0f;
        float enter = 0f;
        float exit = distance;
        if (!ClipAxis(origin.x, direction.x, bounds.min.x, bounds.max.x, ref enter, ref exit) ||
            !ClipAxis(origin.y, direction.y, bounds.min.y, bounds.max.y, ref enter, ref exit)) return false;
        hitDistance = Mathf.Clamp(enter, 0f, distance);
        return enter <= exit && enter <= distance && exit >= 0f;
    }

    static bool ClipAxis(float origin, float direction, float minimum, float maximum, ref float enter, ref float exit)
    {
        if (Mathf.Abs(direction) <= Mathf.Epsilon) return origin >= minimum && origin <= maximum;
        float first = (minimum - origin) / direction;
        float last = (maximum - origin) / direction;
        if (first > last) (first, last) = (last, first);
        enter = Mathf.Max(enter, first);
        exit = Mathf.Min(exit, last);
        return enter <= exit;
    }

    void FadeAndRemove()
    {
        const float fadeSeconds = .42f;
        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01(1f - (Time.time - fadeStartedAt) / fadeSeconds);
        spriteRenderer.color = color;
        if (Time.time - fadeStartedAt >= fadeSeconds) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (healthBar != null) healthBar.HideAndDispose();
    }

    Sprite CurrentIdleSprite()
    {
        if (idleFrames.Length == EnemySpriteLoader.ClipFrameCount)
            return LoopingFrame(idleFrames, fullFrameIdleFrameSeconds);
        if (idleSprite == null) return idleBSprite;
        if (idleBSprite == null) return idleSprite;
        return Mathf.FloorToInt(Time.time / Mathf.Max(.05f, idleFrameSeconds)) % 2 == 0 ? idleSprite : idleBSprite;
    }

    Sprite LoopingFrame(Sprite[] frames, float secondsPerFrame)
    {
        int index = Mathf.FloorToInt(Time.time / Mathf.Max(.05f, secondsPerFrame)) % frames.Length;
        return frames[index];
    }

    void StartThrowClip()
    {
        throwClipActive = true;
        cleaverReleased = false;
        throwClipStartedAt = Time.time;
        ApplySprite(throwFrames[0], ThrowFramesResource, ref warnedMissingIdle);
    }

    void UpdateThrowClip()
    {
        float elapsed = Time.time - throwClipStartedAt;
        float secondsPerFrame = Mathf.Max(.05f, throwFrameSeconds);
        int frame = Mathf.Clamp(Mathf.FloorToInt(elapsed / secondsPerFrame), 0, throwFrames.Length - 1);
        ApplySprite(throwFrames[frame], ThrowFramesResource, ref warnedMissingIdle);

        if (!cleaverReleased && frame >= Mathf.Clamp(cleaverReleaseFrame, 0, throwFrames.Length - 1))
        {
            ThrowCleaver();
            cleaverReleased = true;
            nextThrowAt = Time.time + cooldownSeconds;
        }

        if (elapsed >= throwFrames.Length * secondsPerFrame)
            throwClipActive = false;
    }

    TomatoGame FindTomatoToLeft()
    {
        TomatoGame tomato = UnityEngine.Object.FindFirstObjectByType<TomatoGame>();
        if (tomato == null || tomato.IsLocked || tomato.transform.position.x >= transform.position.x) return null;
        return transform.position.x - tomato.transform.position.x <= detectionDistance ? tomato : null;
    }

    void ThrowCleaver()
    {
        if (cleaverSprite == null && !EnemySpriteLoader.HasCompleteClip(CleaverFramesResource, "Cleaver"))
        {
            WarnOnce(ref warnedMissingCleaver, CleaverResource);
            return;
        }
        var projectile = new GameObject("Chef Cleaver Projectile", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(CleaverProjectile));
        projectile.transform.position = transform.position + (Vector3)throwOffset;
        projectile.transform.localScale = Vector3.one * .45f;
        var renderer = projectile.GetComponent<SpriteRenderer>();
        renderer.sprite = cleaverSprite;
        renderer.flipX = false;
        renderer.sortingOrder = 22;
        var collider = projectile.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(4.5f, 1.35f);
        var body = projectile.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        projectile.GetComponent<CleaverProjectile>().Configure(Vector2.left * projectileSpeed, gameObject, cleaverSprite);
    }

    void ApplySprite(Sprite sprite, string resource, ref bool warned)
    {
        if (dying) return;
        if (sprite == null)
        {
            spriteRenderer.enabled = false;
            hitbox.enabled = false;
            WarnOnce(ref warned, resource);
            return;
        }
        hitbox.enabled = true;
        spriteRenderer.enabled = true;
        spriteRenderer.sprite = sprite;
    }

    void WarnOnce(ref bool warned, string resource)
    {
        if (warned) return;
        warned = true;
        Debug.LogWarning($"{name} is waiting for Resources/{resource}.png; its trigger remains disabled until visible art is available.", this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (dying) return;
        var tomato = other.GetComponent<TomatoGame>();
        if (tomato != null) tomato.HazardRespawn(tomato.CurrentCheckpoint);
    }
}
