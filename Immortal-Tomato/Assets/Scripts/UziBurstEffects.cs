using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uzi round and brass simulation. Tracers use a swept query so their visual
/// speed cannot skip over a waiter or chef between rendered frames.
/// </summary>
public sealed class UziBurstEffects : MonoBehaviour
{
    const int RoundsPerMuzzlePulse = 3;
    const float RoundSpacing = .027f;
    const float TracerSpeed = 23f;
    const float TracerLifetime = .62f;
    const float Gravity = 19f;
    const float CasingLifetime = 2.15f;
    // Every visible tracer represents one actual round, so it removes exactly
    // one health point. The dense burst remains dangerous through volume,
    // rather than invisible per-round damage multiplication.
    const int DamagePerRound = 1;
    const string VolumeKey = "ImmortalTomato.MenuVolume";
    const string MutedKey = "ImmortalTomato.MenuMuted";

    readonly List<FlyingTracer> tracers = new();
    readonly List<SpentCasing> casings = new();
    readonly List<PendingRound> pendingRounds = new();
    Sprite[] tracerFrames;
    Sprite[] casingFrames;
    AudioClip burstClip;
    AudioSource audioSource;
    int sortingLayerId;
    int sortingOrder;
    int totalRoundsSpawned;
    int lastBurstDirection;

    public int ActiveTracerCount => tracers.Count;
    public int ActiveCasingCount => casings.Count;
    public int TotalRoundsSpawned => totalRoundsSpawned;
    public int LastBurstDirection => lastBurstDirection;
    public int RoundsPerBurst => RoundsPerMuzzlePulse;
    public int RoundDamage => DamagePerRound;
    public bool AssetsReady => tracerFrames != null && tracerFrames.Length >= 3 && casingFrames != null && casingFrames.Length >= 4 && burstClip != null;

    sealed class FlyingTracer
    {
        public GameObject gameObject;
        public SpriteRenderer renderer;
        public Vector2 velocity;
        public float expiresAt;
    }

    sealed class SpentCasing
    {
        public GameObject gameObject;
        public SpriteRenderer renderer;
        public Vector2 velocity;
        public float landedAt = -1f;
        public int bounces;
        public float expiresAt;
    }

    struct PendingRound
    {
        public Vector2 muzzle;
        public int direction;
        public bool upperMuzzle;
        public int index;
        public float dueAt;
    }

    public void Initialize(SpriteRenderer sourceRenderer)
    {
        sortingLayerId = sourceRenderer != null ? sourceRenderer.sortingLayerID : 0;
        sortingOrder = sourceRenderer != null ? sourceRenderer.sortingOrder + 3 : 3;
        tracerFrames = LoadFrames("Frames/UziFx", "Bullet_");
        casingFrames = LoadFrames("Frames/UziFx", "Casing_");
        burstClip = Resources.Load<AudioClip>("Audio/UziBurst");
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
        if (!AssetsReady)
            Debug.LogError("Uzi burst effects require Frames/UziFx Bullet_01..03, Casing_01..04, and Audio/UziBurst.", this);
    }

    static Sprite[] LoadFrames(string resourcePath, string namePrefix)
    {
        Texture2D[] textures = Resources.LoadAll<Texture2D>(resourcePath);
        textures = System.Array.FindAll(textures, texture => texture.name.StartsWith(namePrefix, System.StringComparison.Ordinal));
        System.Array.Sort(textures, (left, right) => string.CompareOrdinal(left.name, right.name));
        var sprites = new Sprite[textures.Length];
        for (int i = 0; i < textures.Length; i++)
        {
            Texture2D texture = textures[i];
            sprites[i] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
        }
        return sprites;
    }

    /// <summary>Starts a dense three-round burst from an exactly measured muzzle.</summary>
    public int FireMuzzleBurst(Vector2 muzzleWorld, int direction, bool upperMuzzle, bool playSound)
    {
        return FireMuzzleBurst(muzzleWorld, direction, upperMuzzle, RoundsPerMuzzlePulse, playSound);
    }

    /// <summary>
    /// Starts up to three physical rounds from a measured muzzle. The player
    /// controller supplies the remaining magazine capacity; direct runtime QA
    /// deliberately keeps using the full-burst overload above.
    /// </summary>
    public int FireMuzzleBurst(Vector2 muzzleWorld, int direction, bool upperMuzzle, int roundCount, bool playSound)
    {
        roundCount = Mathf.Clamp(roundCount, 0, RoundsPerMuzzlePulse);
        if (!AssetsReady || roundCount == 0) return 0;
        direction = direction < 0 ? -1 : 1;
        lastBurstDirection = direction;
        if (playSound) PlayBurstSound();
        for (int i = 0; i < roundCount; i++)
        {
            pendingRounds.Add(new PendingRound {
                muzzle = muzzleWorld, direction = direction, upperMuzzle = upperMuzzle,
                index = i, dueAt = Time.time + i * RoundSpacing
            });
        }
        return roundCount;
    }

    void SpawnTracer(Vector2 muzzleWorld, int direction, int roundIndex)
    {
        var visual = new GameObject("Uzi Tracer", typeof(SpriteRenderer));
        visual.transform.position = muzzleWorld + new Vector2(direction * .08f, (roundIndex - 1) * .018f);
        var renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sprite = tracerFrames[roundIndex % tracerFrames.Length];
        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = sortingOrder;
        renderer.flipX = direction < 0;
        tracers.Add(new FlyingTracer {
            gameObject = visual, renderer = renderer,
            velocity = new Vector2(direction * (TracerSpeed + roundIndex * .75f), (roundIndex - 1) * .13f),
            expiresAt = Time.time + TracerLifetime
        });
        totalRoundsSpawned++;
    }

    void SpawnCasing(Vector2 muzzleWorld, int direction, bool upperMuzzle, int roundIndex)
    {
        var visual = new GameObject("Spent Uzi Casing", typeof(SpriteRenderer));
        visual.transform.position = muzzleWorld + new Vector2(-direction * .06f, upperMuzzle ? .03f : -.03f);
        var renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sprite = casingFrames[(totalRoundsSpawned + roundIndex) % casingFrames.Length];
        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = sortingOrder - 1;
        renderer.flipX = direction > 0;
        casings.Add(new SpentCasing {
            gameObject = visual, renderer = renderer,
            velocity = new Vector2(-direction * (1.65f + roundIndex * .22f), 2.4f + (upperMuzzle ? .25f : -.15f) + roundIndex * .14f),
            expiresAt = Time.time + CasingLifetime
        });
    }

    void PlayBurstSound()
    {
        // The deterministic batch suite intentionally runs without an audio
        // listener. Its visual/cleanup assertions still exercise the burst,
        // while this suppresses meaningless headless audio warnings.
        if (Application.isBatchMode || burstClip == null || audioSource == null) return;
        audioSource.mute = PlayerPrefs.GetInt(MutedKey, 0) != 0;
        audioSource.volume = PlayerPrefs.GetFloat(VolumeKey, .72f) * .48f;
        if (!audioSource.mute) audioSource.PlayOneShot(burstClip);
    }

    void Update()
    {
        UpdateTracers();
        UpdateCasings();
        UpdatePendingRounds();
    }

    void UpdatePendingRounds()
    {
        for (int i = pendingRounds.Count - 1; i >= 0; i--)
        {
            PendingRound round = pendingRounds[i];
            if (Time.time < round.dueAt) continue;
            SpawnTracer(round.muzzle, round.direction, round.index);
            SpawnCasing(round.muzzle, round.direction, round.upperMuzzle, round.index);
            pendingRounds.RemoveAt(i);
        }
    }

    void UpdateTracers()
    {
        for (int i = tracers.Count - 1; i >= 0; i--)
        {
            FlyingTracer tracer = tracers[i];
            if (tracer.gameObject == null || Time.time >= tracer.expiresAt)
            {
                DisposeTracer(i);
                continue;
            }
            Vector2 previousPosition = tracer.gameObject.transform.position;
            // A round exists only while the player can see it. This prevents
            // unseen screen-edge shots from damaging an enemy out of view.
            if (!IsInActiveCameraView(previousPosition))
            {
                DisposeTracer(i);
                continue;
            }
            Vector2 nextPosition = previousPosition + tracer.velocity * Time.deltaTime;
            if (TryHitEnemy(previousPosition, nextPosition))
            {
                DisposeTracer(i);
                continue;
            }
            tracer.gameObject.transform.position = nextPosition;
            if (!IsInActiveCameraView(nextPosition))
            {
                DisposeTracer(i);
                continue;
            }
            Color color = tracer.renderer.color;
            color.a = Mathf.Clamp01((tracer.expiresAt - Time.time) / .14f);
            tracer.renderer.color = color;
        }
    }

    bool TryHitEnemy(Vector2 from, Vector2 to)
    {
        Vector2 offset = to - from;
        float distance = offset.magnitude;
        if (distance <= Mathf.Epsilon) return false;
        Vector2 direction = offset / distance;
        // Enemy hitboxes are triggers. Query their bounds directly instead of
        // relying on the project-wide Physics2D.queriesHitTriggers setting.
        // A fast tracer can cross more than one character in a rendered
        // frame, so it must consume itself on the *nearest* eligible target.
        WaiterEnemy nearestWaiter = null;
        ChefEnemy nearestChef = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (WaiterEnemy waiter in FindObjectsByType<WaiterEnemy>(FindObjectsSortMode.None))
        {
            if (waiter.TryGetBulletHitDistance(from, direction, distance, out float hitDistance) && hitDistance < nearestDistance)
            {
                nearestWaiter = waiter;
                nearestChef = null;
                nearestDistance = hitDistance;
            }
        }
        foreach (ChefEnemy chef in FindObjectsByType<ChefEnemy>(FindObjectsSortMode.None))
        {
            if (chef.TryGetBulletHitDistance(from, direction, distance, out float hitDistance) && hitDistance < nearestDistance)
            {
                nearestChef = chef;
                nearestWaiter = null;
                nearestDistance = hitDistance;
            }
        }
        if (nearestDistance == float.PositiveInfinity || !IsInActiveCameraView(from + direction * nearestDistance)) return false;
        return nearestWaiter != null ? nearestWaiter.TakeBulletHit(DamagePerRound) : nearestChef != null && nearestChef.TakeBulletHit(DamagePerRound);
    }

    static bool IsInActiveCameraView(Vector2 worldPosition)
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.isActiveAndEnabled) camera = FindFirstObjectByType<Camera>();
        // Keep gameplay functional in deliberately camera-free test scenes;
        // the shipped scene always has an active Main Camera.
        if (camera == null || !camera.isActiveAndEnabled) return true;
        Vector3 viewport = camera.WorldToViewportPoint(worldPosition);
        return viewport.z >= camera.nearClipPlane && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
    }

    void UpdateCasings()
    {
        for (int i = casings.Count - 1; i >= 0; i--)
        {
            SpentCasing casing = casings[i];
            if (casing.gameObject == null || Time.time >= casing.expiresAt)
            {
                DisposeCasing(i);
                continue;
            }
            if (casing.landedAt < 0f)
            {
                Vector2 position = casing.gameObject.transform.position;
                casing.velocity.y -= Gravity * Time.deltaTime;
                float downwardDistance = Mathf.Max(.02f, -casing.velocity.y * Time.deltaTime + .025f);
                RaycastHit2D ground = FindGroundBelow(position, downwardDistance);
                if (ground.collider != null && casing.velocity.y <= 0f)
                {
                    position.y = ground.point.y + .035f;
                    if (casing.bounces++ == 0)
                        casing.velocity = new Vector2(casing.velocity.x * .38f, 1.35f);
                    else
                    {
                        casing.velocity = Vector2.zero;
                        casing.landedAt = Time.time;
                    }
                }
                else
                    position += casing.velocity * Time.deltaTime;
                casing.gameObject.transform.position = position;
                casing.gameObject.transform.Rotate(0f, 0f, -casing.velocity.x * 560f * Time.deltaTime);
            }
            else
            {
                Color color = casing.renderer.color;
                color.a = Mathf.Clamp01(1f - (Time.time - casing.landedAt) / .72f);
                casing.renderer.color = color;
            }
        }
    }

    RaycastHit2D FindGroundBelow(Vector2 position, float distance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(position, Vector2.down, distance);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            if (hit.collider.transform.root == transform.root) continue;
            return hit;
        }
        return default;
    }

    void DisposeTracer(int index)
    {
        if (tracers[index].gameObject != null) Destroy(tracers[index].gameObject);
        tracers.RemoveAt(index);
    }

    void DisposeCasing(int index)
    {
        if (casings[index].gameObject != null) Destroy(casings[index].gameObject);
        casings.RemoveAt(index);
    }

    /// <summary>Used by Shoot interruption/respawn paths and runtime QA.</summary>
    public void CancelAndClear()
    {
        pendingRounds.Clear();
        for (int i = tracers.Count - 1; i >= 0; i--) DisposeTracer(i);
        for (int i = casings.Count - 1; i >= 0; i--) DisposeCasing(i);
    }

    void OnDestroy() => CancelAndClear();
}
