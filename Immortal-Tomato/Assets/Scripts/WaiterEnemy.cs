using System;
using UnityEngine;

/// <summary>
/// A left-facing tray-first charge enemy. It always returns to its authored
/// post, including after the tomato respawns or the charge misses.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D))]
public sealed class WaiterEnemy : MonoBehaviour
{
    const string IdleResource = "Enemies/WaiterIdle";
    const string IdleBResource = "Enemies/WaiterIdleB";
    const string RunAResource = "Enemies/WaiterRunA";
    const string RunBResource = "Enemies/WaiterRunB";
    const string IdleFramesResource = "EnemyFrames/Waiter/Idle";
    const string RunFramesResource = "EnemyFrames/Waiter/Run";

    [SerializeField] float detectionDistance = 8.5f;
    [SerializeField, Min(0f)] float awarenessSeconds = .85f;
    [SerializeField, Min(1)] int maxHealth = 18;
    [SerializeField] float chargeSpeed = 6.2f;
    [SerializeField] float maximumChargeDistance = 7.5f;
    [SerializeField] float pauseBeforeReturn = .35f;
    [SerializeField] float returnSpeed = 5.4f;
    [SerializeField] float idleFrameSeconds = .42f;
    [SerializeField] float fullFrameIdleFrameSeconds = .12f;
    [SerializeField] float runFrameSeconds = .10f;

    SpriteRenderer spriteRenderer;
    Rigidbody2D body;
    BoxCollider2D hitbox;
    Sprite idleSprite;
    Sprite idleBSprite;
    Sprite runASprite;
    Sprite runBSprite;
    Sprite[] idleFrames = Array.Empty<Sprite>();
    Sprite[] runFrames = Array.Empty<Sprite>();
    Vector3 home;
    float chargeStartX;
    float returnAt;
    float awarenessStartedAt = -1f;
    float fadeStartedAt = -1f;
    int currentHealth;
    EnemyHealthBar healthBar;
    State state;
    bool warnedMissingIdle;

    enum State { Idle, Charging, Returning, Dying }

    public bool FacesLeft => transform.localScale.x >= 0f && !spriteRenderer.flipX;
    public bool IsCharging => state == State.Charging;
    public bool IsDying => state == State.Dying;
    public bool IsHazardActive => !IsDying && hitbox != null && hitbox.enabled;
    public float VisualAlpha => spriteRenderer != null ? spriteRenderer.color.a : 0f;
    public float DetectionDistance => detectionDistance;
    public float AwarenessSeconds => awarenessSeconds;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public EnemyHealthBar HealthBar => healthBar;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        home = transform.position;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        spriteRenderer.flipX = false; // Waiter/tray artwork is authored left-facing.
        spriteRenderer.sortingOrder = 20;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        hitbox = GetComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        currentHealth = Mathf.Max(1, maxHealth);
        idleSprite = EnemySpriteLoader.Load(IdleResource, new Vector2(.5f, 0f));
        idleBSprite = EnemySpriteLoader.Load(IdleBResource, new Vector2(.5f, 0f));
        runASprite = EnemySpriteLoader.Load(RunAResource, new Vector2(.5f, 0f));
        runBSprite = EnemySpriteLoader.Load(RunBResource, new Vector2(.5f, 0f));
        idleFrames = EnemySpriteLoader.LoadFrames(IdleFramesResource, "Idle", new Vector2(.5f, 0f));
        runFrames = EnemySpriteLoader.LoadFrames(RunFramesResource, "Run", new Vector2(.5f, 0f));
        ApplyIdleSprite();
        healthBar = EnemyHealthBar.Create(this, spriteRenderer, currentHealth);
    }

    void Update()
    {
        if (state == State.Dying)
        {
            FadeAndRemove();
            return;
        }
        switch (state)
        {
            case State.Idle:
                ApplyIdleSprite();
                body.MovePosition(home);
                transform.rotation = Quaternion.identity;
                if (HasBecomeAware(FindTomatoToLeft()))
                {
                    state = State.Charging;
                    chargeStartX = transform.position.x;
                }
                break;
            case State.Charging:
                ApplyRunSprite();
                // Full-canvas run frames provide the motion. Keep the root and
                // trigger upright so frame padding can never alter the body.
                body.MovePosition(transform.position + Vector3.left * (chargeSpeed * Time.deltaTime));
                transform.rotation = Quaternion.identity;
                TomatoGame chargingTomato = FindTomatoToLeft();
                // Losing sight resets the acquisition timer. A target which
                // returns later must be watched for the full awareness delay.
                if (chargingTomato == null) awarenessStartedAt = -1f;
                if (chargeStartX - transform.position.x >= maximumChargeDistance || chargingTomato == null)
                {
                    state = State.Returning;
                    returnAt = Time.time + pauseBeforeReturn;
                }
                break;
            case State.Returning:
                if (Time.time < returnAt)
                {
                    ApplyIdleSprite();
                    return;
                }
                ApplyRunSprite();
                body.MovePosition(Vector3.MoveTowards(transform.position, home, returnSpeed * Time.deltaTime));
                transform.rotation = Quaternion.identity;
                if (Vector3.Distance(transform.position, home) < .03f)
                {
                    body.position = home;
                    state = State.Idle;
                }
                break;
        }
    }

    /// <summary>Called by a swept, on-camera Uzi tracer. Returns false after death has begun.</summary>
    public bool TakeBulletHit(int damage)
    {
        if (state == State.Dying) return false;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, damage));
        if (healthBar != null) healthBar.SetHealth(currentHealth);
        if (currentHealth > 0) return true;
        state = State.Dying;
        fadeStartedAt = Time.time;
        hitbox.enabled = false;
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
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
        if (IsDying || hitbox == null || !hitbox.enabled) return false;
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

    TomatoGame FindTomatoToLeft()
    {
        TomatoGame tomato = UnityEngine.Object.FindFirstObjectByType<TomatoGame>();
        if (tomato == null || tomato.IsLocked || tomato.transform.position.x >= transform.position.x) return null;
        return transform.position.x - tomato.transform.position.x <= detectionDistance ? tomato : null;
    }

    void ApplyIdleSprite()
    {
        ApplySprite(CurrentIdleSprite());
    }

    void ApplyRunSprite()
    {
        ApplySprite(CurrentRunSprite());
    }

    Sprite CurrentIdleSprite()
    {
        if (idleFrames.Length == EnemySpriteLoader.ClipFrameCount)
            return LoopingFrame(idleFrames, fullFrameIdleFrameSeconds);
        if (idleSprite == null) return idleBSprite;
        if (idleBSprite == null) return idleSprite;
        return Mathf.FloorToInt(Time.time / Mathf.Max(.05f, idleFrameSeconds)) % 2 == 0 ? idleSprite : idleBSprite;
    }

    Sprite CurrentRunSprite()
    {
        if (runFrames.Length == EnemySpriteLoader.ClipFrameCount)
            return LoopingFrame(runFrames, runFrameSeconds);
        if (runASprite == null) return runBSprite ?? CurrentIdleSprite();
        if (runBSprite == null) return runASprite;
        return Mathf.FloorToInt(Time.time / Mathf.Max(.05f, runFrameSeconds)) % 2 == 0 ? runASprite : runBSprite;
    }

    Sprite LoopingFrame(Sprite[] frames, float secondsPerFrame)
    {
        int index = Mathf.FloorToInt(Time.time / Mathf.Max(.05f, secondsPerFrame)) % frames.Length;
        return frames[index];
    }

    void ApplySprite(Sprite sprite)
    {
        if (IsDying) return;
        if (sprite != null)
        {
            hitbox.enabled = true;
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = sprite;
            return;
        }
        spriteRenderer.enabled = false;
        hitbox.enabled = false;
        if (warnedMissingIdle) return;
        warnedMissingIdle = true;
        Debug.LogWarning($"{name} is waiting for Resources/{IdleResource}.png; its trigger remains disabled until the art is available.", this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsDying) return;
        TomatoGame tomato = other.GetComponent<TomatoGame>();
        if (tomato == null) return;
        tomato.HazardRespawn(tomato.CurrentCheckpoint);
        // The return state prevents a waiter left off-route after the hit.
        state = State.Returning;
        returnAt = Time.time + pauseBeforeReturn;
    }
}
