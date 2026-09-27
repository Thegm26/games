using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>Measured actor state exposed to explicit QA tooling.</summary>
public struct TomatoRuntimeTelemetry
{
    public Vector3 root;
    public Vector3 visual;
    public Bounds colliderBounds;
    public Bounds spriteBounds;
    public Bounds alphaBounds;
    public Bounds coreBounds;
    public Vector2 foot;
    public Vector2 physicsFoot;
    // Registration anchor is the weighted red-body point used to place the
    // renderer. Body-centre/ratio fields below are deliberately independent
    // bounds measurements used to validate the gameplay capsule.
    public Vector2 registrationAnchor;
    public Vector2 registrationAnchorToColliderCenter;
    public Vector2 coreCenter;
    public Vector2 coreToColliderCenter;
    public Vector2 probeOrigin;
    public Vector2 probePoint;
    public Vector2 probeNormal;
    public float probeDistance;
    public string probeCollider;
    public Vector2 velocity;
    public bool grounded;
    public bool locked;
    public bool alphaReadable;
    public string clip;
    public int frame;
    public int frameCount;
    public Vector2 spritePivotPixels;
    public Vector2 texturePixels;
    public Vector2 visualScale;
    public string frameName;
    public int opaquePixels;
    public int bodyPixels;
    public float coreContainment;
    public float alphaColliderOverlap;
    public float coreWidthToColliderWidth;
    public float coreHeightToColliderHeight;
    public float footToColliderBottom;
    public float footToSurface;
    public int clipTransitionSerial;
    public string clipTransitionFrom;
    public string clipTransitionTo;
    public float clipTransitionIou;
    public Vector2 clipTransitionBodyDelta;
    public int shootActionStarts;
    public bool shootActionActive;
    public bool transitionGhostEnabled;
    public bool transitionFadeActive;
    public float mainVisualAlpha;
    public float transitionGhostAlpha;
    public int uziRoundsSpawned;
    public int uziActiveTracers;
    public int uziActiveCasings;
    public int uziLastDirection;
    public int ammoInMagazine;
    public int magazineSize;
    public bool isReloading;
    public float reloadProgress;
    public int lives;
    public int maxLives;
    public bool gameOver;
}

public class TomatoGame : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5.5f;
    [SerializeField] float jumpForce = 12f;
    [SerializeField] float frameRate = 11f;

    [Header("Uzi Magazine")]
    [SerializeField, Min(1)] int magazineSize = 10;
    [SerializeField, Min(.1f)] float reloadDuration = 1.45f;

    [Header("Player Lives")]
    [SerializeField, Min(1)] int startingLives = 3;

    SpriteRenderer spriteRenderer;
    SpriteRenderer transitionRenderer;
    // The generated Shoot sheets redraw the complete tomato in a different
    // pose.  Swapping those complete frames makes the fruit look as if it has
    // changed character, even when the registered body and collider are
    // mathematically stable.  Shooting therefore keeps a real planted
    // locomotion frame as the body and adds only the firing effect below.
    SpriteRenderer[] muzzleFlashRenderers;
    Sprite muzzleFlashSprite;
    Vector2 muzzleFlashCenterPixels;
    UziBurstEffects uziBurstEffects;
    GameObject combatHud;
    Button[] hudGunButtons;
    Image[] hudGunIcons;
    Image[] hudHeartIcons;
    Sprite gunSilhouetteSprite;
    Sprite heartSprite;
    Sprite emptyHeartSprite;
    int reloadGunIndex = -1;
    GameObject gameOverOverlay;
    CapsuleCollider2D capsule;
    Rigidbody2D body;
    readonly Dictionary<string, Sprite[]> clips = new();
    string activeClip = "Idle";
    Coroutine playback;
    readonly HashSet<Collider2D> groundContacts = new();
    bool grounded;
    bool locked;
    float previewUntil = -1f;
    Vector3 checkpoint;
    int activeFrame;
    Sprite displayedFrame;
    string displayedClip = string.Empty;
    int clipTransitionSerial;
    string clipTransitionFrom = "NONE";
    string clipTransitionTo = "NONE";
    float clipTransitionIou = 1f;
    Vector2 clipTransitionBodyDelta;
    Vector2 displayedCoreToCollider;
    bool hasDisplayedCoreToCollider;
    Coroutine transitionFade;
    Color visualBaseColor = Color.white;
    bool shootActionActive;
    int shootActionStarts;
    int ammoInMagazine;
    int lives;
    bool gameOver;
    bool isReloading;
    float reloadStartedAt = -1f;
    Coroutine reloadRoutine;
    float currentHorizontal;
    string shootBaseClip = "Idle";
    int shootBaseFrame;
    readonly Dictionary<int, AlphaBounds> alphaBounds = new();
    readonly Dictionary<int, TomatoFrameGeometry> frameGeometry = new();
    readonly Dictionary<string, string> curatedFrames = new();
    readonly RaycastHit2D[] groundProbeHits = new RaycastHit2D[12];
    ContactFilter2D groundProbeFilter;

    // Editor-only QA can feed this controller without synthesising desktop
    // keyboard events.  The override is deliberately consumed by Update's
    // normal movement/action code below, so it still exercises the real
    // Rigidbody, collision callbacks, animation state machine and coroutines.
    bool headlessQaInputEnabled;
    float headlessQaHorizontal;
    int headlessQaJumpRequests;
    int headlessQaShootRequests;
    int headlessQaSplatRequests;

    const float GroundNormalMinimum = .55f;
    const float GroundProbeDistance = .32f;
    static readonly Vector2 StableRendererScale = Vector2.one;
    const float TransitionFadeSeconds = .075f;

    struct AlphaBounds
    {
        public bool readable;
        public Vector2 min;
        public Vector2 max;
        public float footCenterX;
    }

    public Vector3 CurrentCheckpoint => checkpoint;
    public Rigidbody2D Body => body;
    public SpriteRenderer Renderer => spriteRenderer;
    public string ActiveClip => activeClip;
    public int ActiveFrame => activeFrame;
    public int ActiveFrameCount => clips.TryGetValue(activeClip, out var frames) ? frames.Length : 0;
    public float AnimationFps => frameRate;
    public bool IsGrounded => grounded;
    public bool IsLocked => locked;
    public IReadOnlyCollection<Collider2D> GroundContacts => groundContacts;
    public bool UziFxAssetsReady => uziBurstEffects != null && uziBurstEffects.AssetsReady;
    public int AmmoInMagazine => ammoInMagazine;
    public int MagazineSize => Mathf.Max(1, magazineSize);
    public bool IsReloading => isReloading;
    public float ReloadProgress => isReloading
        ? Mathf.Clamp01((Time.time - reloadStartedAt) / Mathf.Max(.1f, reloadDuration))
        : 0f;
    public int Lives => lives;
    public int MaxLives => Mathf.Max(1, startingLives);
    public bool IsGameOver => gameOver;
    public GameObject CombatHud => combatHud;
    public GameObject GameOverOverlay => gameOverOverlay;
    public static bool GameplaySuspended { get; private set; }

    public TomatoClipGeometryAudit GetClipGeometryAudit(string clip)
    {
        if (!clips.TryGetValue(clip, out Sprite[] frames) || frames.Length == 0) return default;
        Vector2 legacyMin = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 legacyMax = new(float.NegativeInfinity, float.NegativeInfinity);
        Vector2 registeredMin = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 registeredMax = new(float.NegativeInfinity, float.NegativeInfinity);
        float maxAnchorStep = 0f, maxBboxScaleStep = 0f, maxAreaScaleStep = 0f, minimumIou = 1f;
        bool identityPass = true;
        TomatoFrameGeometry previous = default;
        Sprite previousSprite = null;
        for (int i = 0; i < frames.Length; i++)
        {
            Sprite frame = frames[i];
            TomatoFrameGeometry current = GetFrameGeometry(frame);
            identityPass &= current.readable && frame.texture != null && frame.texture.width == 420 && frame.texture.height == 724;
            Vector2 legacy = current.coreToFoot;
            legacyMin = Vector2.Min(legacyMin, legacy);
            legacyMax = Vector2.Max(legacyMax, legacy);
            // SetFrame always maps this core to this exact local position.
            Vector2 registered = PhysicsCoreLocal();
            registeredMin = Vector2.Min(registeredMin, registered);
            registeredMax = Vector2.Max(registeredMax, registered);
            if (i > 0)
            {
                maxAnchorStep = Mathf.Max(maxAnchorStep, Vector2.Distance(current.coreCenter, previous.coreCenter));
                float widthRatio = current.size.x / Mathf.Max(1f, previous.size.x);
                float heightRatio = current.size.y / Mathf.Max(1f, previous.size.y);
                float areaRatio = current.opaquePixels / Mathf.Max(1f, previous.opaquePixels);
                maxBboxScaleStep = Mathf.Max(maxBboxScaleStep, Mathf.Max(Mathf.Abs(widthRatio - 1f), Mathf.Abs(heightRatio - 1f)));
                maxAreaScaleStep = Mathf.Max(maxAreaScaleStep, Mathf.Abs(areaRatio - 1f));
                minimumIou = Mathf.Min(minimumIou, TomatoFrameGeometry.RegisteredAlphaIou(previousSprite.texture, previous, frame.texture, current));
            }
            previous = current;
            previousSprite = frame;
        }
        if (frames.Length == 1) minimumIou = 1f;
        return new TomatoClipGeometryAudit(
            clip, frames.Length, legacyMax - legacyMin, registeredMax - registeredMin,
            maxAnchorStep, maxBboxScaleStep, maxAreaScaleStep, minimumIou, identityPass);
    }

    public string CuratedFrameNames(string clip)
    {
        if (curatedFrames.TryGetValue(clip, out string names)) return names;
        if (!clips.TryGetValue(clip, out Sprite[] frames)) return "none";
        var namesBuilder = new StringBuilder();
        foreach (Sprite frame in frames)
        {
            if (namesBuilder.Length > 0) namesBuilder.Append(",");
            namesBuilder.Append(frame.texture.name);
        }
        string value = namesBuilder.ToString();
        curatedFrames[clip] = value;
        return value;
    }

    /// <summary>
    /// Writes source and registered-runtime measurements for every frame.
    /// This is intentionally verbose: it is the inspection table used when a
    /// source pose looks wrong, rather than another aggregate pass/fail value.
    /// </summary>
    public void WriteFrameAlignmentAudit(string path)
    {
        var rows = new List<string> {
            $"Tomato frame alignment audit root={F(transform.position)} physicsCoreWorld={F(transform.TransformPoint(PhysicsCoreLocal()))} capsule={capsule.bounds} scale={F(transform.lossyScale)}",
            "Columns: source alpha bounds | tomato-red core | robust boot | dark Uzi bounds + upper/lower muzzle | registered Unity-world core/boot/core-bounds | deltas from previous source frame"
        };
        foreach (string clip in new[] { "Idle", "Run", "Jump", "Shoot", "Hit", "Regen" })
        {
            if (!clips.TryGetValue(clip, out Sprite[] frames)) continue;
            rows.Add($"[CLIP] {clip} frames={frames.Length}" + (clip == "Shoot" ? " (raw source only; runtime shooting preserves locomotion body and overlays flashes)" : string.Empty));
            TomatoFrameGeometry previous = default;
            TomatoGunMuzzleGeometry previousGun = default;
            bool hasPrevious = false;
            foreach (Sprite frame in frames)
            {
                TomatoFrameGeometry g = GetFrameGeometry(frame);
                TomatoGunMuzzleGeometry gun = TomatoFrameVisualMeasurements.MeasureGunMuzzles(frame.texture, g);
                Vector2 coreLocal = PhysicsCoreLocal();
                Vector2 registeredFootLocal = coreLocal + (g.footCenter - g.coreCenter) / frame.pixelsPerUnit;
                Vector2 registeredCoreMinLocal = coreLocal + (g.coreMin - g.coreCenter) / frame.pixelsPerUnit;
                Vector2 registeredCoreMaxLocal = coreLocal + (g.coreMax - g.coreCenter) / frame.pixelsPerUnit;
                Vector2 registeredCore = transform.TransformPoint(coreLocal);
                Vector2 registeredFoot = transform.TransformPoint(registeredFootLocal);
                Vector2 registeredBodyCenter = transform.TransformPoint((registeredCoreMinLocal + registeredCoreMaxLocal) * .5f);
                Vector2 registeredBodySize = Vector2.Scale(registeredCoreMaxLocal - registeredCoreMinLocal, Abs2(transform.lossyScale));
                string delta = "first";
                if (hasPrevious)
                    delta = $"alphaMinΔ={F(g.min - previous.min)} alphaMaxΔ={F(g.max - previous.max)} redCoreΔ={F(g.coreCenter - previous.coreCenter)} bootΔ={F(g.footCenter - previous.footCenter)} upperMuzzleΔ={F(gun.upperMuzzle - previousGun.upperMuzzle)} lowerMuzzleΔ={F(gun.lowerMuzzle - previousGun.lowerMuzzle)}";
                rows.Add($"  {frame.texture.name}: alpha={F(g.min)}..{F(g.max)} redCore={F(g.coreCenter)} coreBox={F(g.coreMin)}..{F(g.coreMax)} boot={F(g.footCenter)} gun={(gun.readable ? F(gun.min) + ".." + F(gun.max) : "none")} upperMuzzle={(gun.readable ? F(gun.upperMuzzle) : "none")} lowerMuzzle={(gun.readable ? F(gun.lowerMuzzle) : "none")} registeredWorld core={F(registeredCore)} boot={F(registeredFoot)} bodyCenter={F(registeredBodyCenter)} bodySize={F(registeredBodySize)} {delta}");
                previous = g;
                previousGun = gun;
                hasPrevious = true;
            }
        }
        rows.Add($"[SHOOT-OVERLAY] extracted orange/yellow flame centroid source={F(muzzleFlashCenterPixels)} from Shoot_04 at source x>=307. Pulse sequence lower on semantic Shoot frames 2/6, upper on 4/8. No full Shoot torso/boot/hand sprite is rendered during gameplay Shoot.");
        rows.Add("[GAMEPLAY-SHOOT-FLASH-ALIGNMENT] actual transformed flame centre is placed at the measured barrel tip; error must be 0.000 world units.");
        foreach (string baseClip in new[] { "Idle", "Run" })
        {
            if (!clips.TryGetValue(baseClip, out Sprite[] baseFrames)) continue;
            foreach (Sprite frame in baseFrames)
            {
                TomatoFrameGeometry g = GetFrameGeometry(frame);
                TomatoGunMuzzleGeometry gun = TomatoFrameVisualMeasurements.MeasureGunMuzzles(frame.texture, g);
                if (!g.readable || !gun.readable) continue;
                foreach (bool flipped in new[] { false, true })
                {
                    Vector3 visualPosition = (Vector3)PhysicsCoreLocal() - SpritePixelToLocal(frame, g.coreCenter, flipped);
                    Vector3 lowerTip = SpritePixelToLocal(frame, gun.lowerMuzzle, flipped);
                    Vector3 upperTip = SpritePixelToLocal(frame, gun.upperMuzzle, flipped);
                    // SpriteRenderer.flipX mirrors pixels around its own pivot;
                    // it does not mirror child transforms.  Use the same
                    // placement calculation as ConfigureMuzzleFlashPlacement
                    // so this audit measures the actual renderer hierarchy.
                    Vector3 lowerLocal = FlashChildPosition(muzzleFlashSprite, muzzleFlashCenterPixels, lowerTip, flipped);
                    Vector3 upperLocal = FlashChildPosition(muzzleFlashSprite, muzzleFlashCenterPixels, upperTip, flipped);
                    Vector3 lowerWorld = transform.TransformPoint(visualPosition + lowerLocal + SpritePixelToLocal(muzzleFlashSprite, muzzleFlashCenterPixels, flipped));
                    Vector3 upperWorld = transform.TransformPoint(visualPosition + upperLocal + SpritePixelToLocal(muzzleFlashSprite, muzzleFlashCenterPixels, flipped));
                    Vector3 lowerTarget = transform.TransformPoint(visualPosition + lowerTip);
                    Vector3 upperTarget = transform.TransformPoint(visualPosition + upperTip);
                    rows.Add($"  {baseClip}/{frame.texture.name} flipX={flipped}: lowerTipWorld={F(lowerTarget)} flashCenterWorld={F(lowerWorld)} err={Vector2.Distance(lowerTarget, lowerWorld):0.000000}; upperTipWorld={F(upperTarget)} flashCenterWorld={F(upperWorld)} err={Vector2.Distance(upperTarget, upperWorld):0.000000}; separation={Vector2.Distance(lowerTarget, upperTarget):0.000000}");
                }
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllLines(path, rows);
    }

    static Vector2 Abs2(Vector3 value) => new(Mathf.Abs(value.x), Mathf.Abs(value.y));

    public void SetHeadlessQaMovement(float horizontal)
    {
        headlessQaInputEnabled = true;
        headlessQaHorizontal = Mathf.Clamp(horizontal, -1f, 1f);
    }

    public void QueueHeadlessQaJump() { headlessQaInputEnabled = true; headlessQaJumpRequests++; }
    public void QueueHeadlessQaShoot() { headlessQaInputEnabled = true; headlessQaShootRequests++; }
    public void QueueHeadlessQaSplat() { headlessQaInputEnabled = true; headlessQaSplatRequests++; }
    public void ClearHeadlessQaInput()
    {
        headlessQaInputEnabled = false;
        headlessQaHorizontal = 0f;
        headlessQaJumpRequests = headlessQaShootRequests = headlessQaSplatRequests = 0;
    }

    void Awake()
    {
        magazineSize = Mathf.Max(1, magazineSize);
        reloadDuration = Mathf.Max(.1f, reloadDuration);
        startingLives = Mathf.Max(1, startingLives);
        lives = startingLives;
        GameplaySuspended = false;
        ammoInMagazine = magazineSize;
        capsule = GetComponent<CapsuleCollider2D>();
        body = GetComponent<Rigidbody2D>();
        // The physics root is the route surface.  Keeping the capsule's lower
        // edge at local y=0 makes spawn, respawn, collision, and visual-foot
        // telemetry use one unambiguous baseline.
        AlignPhysicsFootToRoot();
        groundProbeFilter = new ContactFilter2D();
        groundProbeFilter.NoFilter();
        groundProbeFilter.useTriggers = false;
        CreateVisualChild();
        checkpoint = transform.position;
        foreach (var clip in new[] { "Idle", "Run", "Jump", "Shoot", "Hit", "Regen" })
            clips[clip] = LoadFrames(clip);
        CreateMuzzleFlashOverlays();
        CreateUziBurstEffects();
        CreateCombatHud();
        Play("Idle", true);
    }

    // The source sheets have different transparent padding: their visible feet
    // range from 4 to roughly 160 pixels above the canvas bottom.  A child
    // renderer lets the physics root remain stable while every visible frame is
    // positioned with its actual alpha-foot on that root.
    void CreateVisualChild()
    {
        var oldRenderer = GetComponent<SpriteRenderer>();
        var visual = new GameObject("Tomato Visual", typeof(SpriteRenderer));
        visual.transform.SetParent(transform, false);
        spriteRenderer = visual.GetComponent<SpriteRenderer>();
        var transition = new GameObject("Tomato Transition Visual", typeof(SpriteRenderer));
        transition.transform.SetParent(transform, false);
        transitionRenderer = transition.GetComponent<SpriteRenderer>();
        if (oldRenderer != null)
        {
            spriteRenderer.sortingLayerID = oldRenderer.sortingLayerID;
            spriteRenderer.sortingOrder = oldRenderer.sortingOrder;
            spriteRenderer.color = oldRenderer.color;
            oldRenderer.enabled = false;
        }
        transitionRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        transitionRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        transitionRenderer.enabled = false;
        visualBaseColor = spriteRenderer.color;
    }

    void CreateMuzzleFlashOverlays()
    {
        if (!clips.TryGetValue("Shoot", out Sprite[] shootFrames) || shootFrames.Length < 4 || spriteRenderer == null)
            return;

        // Shoot_04 contains the only useful authored orange flash.  Extract
        // just its bright flame pixels from the far-right muzzle area; none of
        // the tomato, hands, boots, smoke or weapons is copied into the layer.
        Texture2D source = shootFrames[3].texture;
        if (source == null || !source.isReadable) return;
        Texture2D extracted = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
        {
            name = "TomatoMuzzleFlashOnly",
            filterMode = source.filterMode,
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] sourcePixels = source.GetPixels();
        Color[] pixels = new Color[sourcePixels.Length];
        int minMuzzleX = Mathf.RoundToInt(source.width * .73f);
        double flashX = 0d, flashY = 0d, flashWeight = 0d;
        for (int y = 0; y < source.height; y++)
        for (int x = minMuzzleX; x < source.width; x++)
        {
            Color color = sourcePixels[y * source.width + x];
            float brightest = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float darkest = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            // Orange/yellow flame only.  White glove and grey Uzi highlights
            // fail the saturation test, so the overlay cannot redraw a body.
            bool flame = color.a > .12f && color.r > .68f && color.g > .12f &&
                         color.r > color.b * 1.45f && brightest - darkest > .22f;
            if (flame)
            {
                pixels[y * source.width + x] = color;
                float weight = color.a * Mathf.Max(.1f, color.r + color.g - color.b);
                flashX += x * weight;
                flashY += y * weight;
                flashWeight += weight;
            }
        }
        extracted.SetPixels(pixels);
        extracted.Apply(false, true);
        muzzleFlashSprite = Sprite.Create(extracted, new Rect(0, 0, extracted.width, extracted.height), new Vector2(.5f, 0f), 100f);
        muzzleFlashCenterPixels = flashWeight > 0d
            ? new Vector2((float)(flashX / flashWeight), (float)(flashY / flashWeight))
            : new Vector2(source.width * .8f, source.height * .35f);

        muzzleFlashRenderers = new SpriteRenderer[2];
        for (int i = 0; i < muzzleFlashRenderers.Length; i++)
        {
            var flash = new GameObject(i == 0 ? "Lower Uzi Muzzle Flash" : "Upper Uzi Muzzle Flash", typeof(SpriteRenderer));
            flash.transform.SetParent(spriteRenderer.transform, false);
            var renderer = flash.GetComponent<SpriteRenderer>();
            renderer.sprite = muzzleFlashSprite;
            renderer.sortingLayerID = spriteRenderer.sortingLayerID;
            renderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            renderer.enabled = false;
            muzzleFlashRenderers[i] = renderer;
        }
    }

    void CreateUziBurstEffects()
    {
        var effectsObject = new GameObject("Uzi Burst Effects");
        effectsObject.transform.SetParent(transform, false);
        uziBurstEffects = effectsObject.AddComponent<UziBurstEffects>();
        uziBurstEffects.Initialize(spriteRenderer);
    }

    void CreateCombatHud()
    {
        EnsureEventSystem();
        combatHud = new GameObject("Combat HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = combatHud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler scaler = combatHud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;

        // Keep the combat HUD deliberately tiny: two clean gun silhouettes and
        // lives only. It is anchored to the screen corner, never to the player.
        gunSilhouetteSprite = CreateGunSilhouetteSprite();
        heartSprite = CreateHeartSprite(false);
        emptyHeartSprite = CreateHeartSprite(true);
        hudGunButtons = new Button[2];
        hudGunIcons = new Image[2];
        for (int i = 0; i < hudGunButtons.Length; i++)
        {
            Button gun = new GameObject($"Gun Button {i + 1}", typeof(Image), typeof(Button)).GetComponent<Button>();
            gun.transform.SetParent(combatHud.transform, false);
            RectTransform rect = gun.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f + i * 62f, -24f);
            rect.sizeDelta = new Vector2(54f, 42f);
            Image icon = gun.GetComponent<Image>();
            icon.sprite = gunSilhouetteSprite;
            icon.preserveAspect = true;
            icon.color = new Color(.9f, .92f, .95f, 1f);
            icon.raycastTarget = true;
            int gunIndex = i;
            gun.onClick.AddListener(() => ReloadFromHud(gunIndex));
            hudGunButtons[i] = gun;
            hudGunIcons[i] = icon;
        }
        hudHeartIcons = new Image[MaxLives];
        for (int i = 0; i < hudHeartIcons.Length; i++)
        {
            Image heart = CreateHudImage($"Heart {i + 1}", combatHud.transform, Color.white);
            RectTransform rect = heart.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(27f + i * 31f, -74f);
            rect.sizeDelta = new Vector2(25f, 23f);
            heart.sprite = heartSprite;
            heart.preserveAspect = true;
            hudHeartIcons[i] = heart;
        }
        CreateGameOverOverlay(canvas.transform);
        UpdateCombatHud();
    }

    // Main intentionally ships without authored debug UI. Create this only at
    // runtime so the Game Over buttons remain clickable in a clean scene.
    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("Runtime EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    Image CreateHudImage(string name, Transform parent, Color color)
    {
        Image image = new GameObject(name, typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    Text CreateHudText(string name, Transform parent, int fontSize, TextAnchor alignment)
    {
        Text text = new GameObject(name, typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    Sprite CreateGunSilhouetteSprite()
    {
        const int width = 96, height = 64;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "Clean Gun Silhouette" };
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            bool bodyPart = (x >= 20 && x < 70 && y >= 28 && y < 42) ||
                            (x >= 64 && x < 90 && y >= 32 && y < 38) ||
                            (x >= 34 && x < 48 && y >= 13 && y < 30);
            pixels[y * width + x] = bodyPart ? Color.white : Color.clear;
        }
        texture.SetPixels(pixels); texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(.5f, .5f), 100f);
    }

    Sprite CreateHeartSprite(bool empty)
    {
        const int width = 64, height = 56;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = empty ? "Empty Heart" : "Heart" };
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[width * height];
        Color body = empty ? new Color(.16f, .18f, .21f, .72f) : new Color(.95f, .18f, .22f, 1f);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            Vector2 p = new Vector2(x + .5f, y + .5f);
            Vector2 left = new Vector2(width * .33f, height * .62f);
            Vector2 right = new Vector2(width * .67f, height * .62f);
            bool lobes = Vector2.Distance(p, left) < 13f || Vector2.Distance(p, right) < 13f;
            bool point = p.y <= height * .62f && p.y >= 8f && Mathf.Abs(p.x - width * .5f) <= (p.y - 8f) * .82f;
            pixels[y * width + x] = lobes || point ? body : Color.clear;
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(.5f, .5f), 100f);
    }

    void CreateGameOverOverlay(Transform parent)
    {
        gameOverOverlay = new GameObject("Game Over Overlay", typeof(Image));
        gameOverOverlay.transform.SetParent(parent, false);
        Image background = gameOverOverlay.GetComponent<Image>();
        background.color = new Color(.02f, .01f, .01f, .88f);
        RectTransform root = background.rectTransform;
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
        Text title = CreateHudText("Game Over Title", root, 64, TextAnchor.MiddleCenter);
        title.text = "GAME OVER";
        title.rectTransform.anchorMin = new Vector2(.2f, .58f); title.rectTransform.anchorMax = new Vector2(.8f, .78f);
        title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
        CreateGameOverButton("Restart Button", root, "RESTART  [R]", new Vector2(.5f, .48f), RestartGame);
        CreateGameOverButton("Main Menu Button", root, "MAIN MENU  [M]", new Vector2(.5f, .37f), ReturnToMainMenu);
        gameOverOverlay.SetActive(false);
    }

    void CreateGameOverButton(string name, Transform parent, string label, Vector2 anchor, UnityEngine.Events.UnityAction callback)
    {
        Button button = new GameObject(name, typeof(Image), typeof(Button)).GetComponent<Button>();
        button.transform.SetParent(parent, false);
        Image image = button.GetComponent<Image>(); image.color = new Color(.75f, .12f, .08f, 1f);
        RectTransform rect = button.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = new Vector2(310f, 58f);
        button.onClick.AddListener(callback);
        Text text = CreateHudText("Label", rect, 23, TextAnchor.MiddleCenter); text.text = label;
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
    }

    void UpdateCombatHud()
    {
        if (hudGunIcons != null)
            for (int i = 0; i < hudGunIcons.Length; i++)
                hudGunIcons[i].color = isReloading && (reloadGunIndex < 0 || reloadGunIndex == i)
                    ? new Color(1f, .55f, .18f, 1f)
                    : new Color(.9f, .92f, .95f, 1f);
        if (hudHeartIcons != null)
            for (int i = 0; i < hudHeartIcons.Length; i++)
                hudHeartIcons[i].sprite = i < lives ? heartSprite : emptyHeartSprite;
    }

    void ConfigureMuzzleFlashPlacement(Sprite bodyFrame)
    {
        if (muzzleFlashRenderers == null || muzzleFlashSprite == null || bodyFrame == null ||
            !clips.TryGetValue("Shoot", out Sprite[] shootFrames) || shootFrames.Length < 4)
            return;
        TomatoFrameGeometry body = GetFrameGeometry(bodyFrame);
        TomatoGunMuzzleGeometry guns = TomatoFrameVisualMeasurements.MeasureGunMuzzles(bodyFrame.texture, body);
        if (!body.readable || !guns.readable) return;

        // Place the bright flame centroid directly on each measured Uzi tip,
        // not merely near it through a shared tomato core. This remains true
        // for every source Run frame and when the character is flipped.
        Vector3 lowerTip = SpritePixelToLocal(bodyFrame, guns.lowerMuzzle);
        Vector3 upperTip = SpritePixelToLocal(bodyFrame, guns.upperMuzzle);
        muzzleFlashRenderers[0].transform.localPosition = FlashChildPosition(muzzleFlashSprite, muzzleFlashCenterPixels, lowerTip, spriteRenderer.flipX);
        muzzleFlashRenderers[1].transform.localPosition = FlashChildPosition(muzzleFlashSprite, muzzleFlashCenterPixels, upperTip, spriteRenderer.flipX);
        muzzleFlashRenderers[0].flipX = spriteRenderer.flipX;
        muzzleFlashRenderers[1].flipX = spriteRenderer.flipX;
    }

    void SetMuzzleFlashPulse(int actionFrame, bool firedRound = true)
    {
        if (muzzleFlashRenderers == null) return;
        // Alternate the two visible Uzis at the source frame rate.  This is a
        // deliberate firing cadence, not a body-scale or position animation.
        bool lower = firedRound && (actionFrame == 1 || actionFrame == 5);
        bool upper = firedRound && (actionFrame == 3 || actionFrame == 7);
        for (int i = 0; i < muzzleFlashRenderers.Length; i++)
        {
            if (muzzleFlashRenderers[i] == null) continue;
            muzzleFlashRenderers[i].enabled = i == 0 ? lower : upper;
            muzzleFlashRenderers[i].color = Color.white;
        }
    }

    int FireUziBurst(Sprite bodyFrame, int actionFrame)
    {
        if (uziBurstEffects == null || bodyFrame == null) return 0;
        bool lower = actionFrame == 1 || actionFrame == 5;
        bool upper = actionFrame == 3 || actionFrame == 7;
        if (!lower && !upper) return 0;
        int rounds = Mathf.Min(uziBurstEffects.RoundsPerBurst, ammoInMagazine);
        if (rounds <= 0) return 0;
        TomatoFrameGeometry bodyGeometry = GetFrameGeometry(bodyFrame);
        TomatoGunMuzzleGeometry guns = TomatoFrameVisualMeasurements.MeasureGunMuzzles(bodyFrame.texture, bodyGeometry);
        if (!bodyGeometry.readable || !guns.readable) return 0;
        int direction = spriteRenderer != null && spriteRenderer.flipX ? -1 : 1;
        Vector2 muzzle = spriteRenderer.transform.TransformPoint(SpritePixelToLocal(bodyFrame, lower ? guns.lowerMuzzle : guns.upperMuzzle));
        // The first pulse owns the original synthesized four-shot sound;
        // subsequent alternating barrel pulses remain visual-only.
        int spawned = uziBurstEffects.FireMuzzleBurst(muzzle, direction, upper, rounds, actionFrame == 1);
        ammoInMagazine -= spawned;
        if (ammoInMagazine == 0) StartReload();
        return spawned;
    }

    Sprite[] LoadFrames(string clip)
    {
        var textures = Resources.LoadAll<Texture2D>($"Frames/{clip}");
        System.Array.Sort(textures, (a, b) => string.CompareOrdinal(a.name, b.name));
        // The first four source images are a take-off/landing sequence.  Idle
        // deliberately uses only the planted-boot frames, so it cannot read as
        // a jump while the body is stationary.
        if (clip == "Idle")
            textures = System.Array.FindAll(textures, texture =>
                texture.name is "Idle_05" or "Idle_06" or "Idle_07" or "Idle_08");
        var sprites = new Sprite[textures.Length];
        for (int i = 0; i < textures.Length; i++)
        {
            sprites[i] = Sprite.Create(textures[i], new Rect(0, 0, textures[i].width, textures[i].height), new Vector2(.5f, 0f), 100f);
            frameGeometry[sprites[i].GetInstanceID()] = TomatoFrameGeometry.Measure(textures[i]);
        }
        return sprites;
    }


    void Update()
    {
        UpdateCombatHud();
        if (gameOver)
        {
            if (Input.GetKeyDown(KeyCode.R)) RestartGame();
            else if (Input.GetKeyDown(KeyCode.M)) ReturnToMainMenu();
            return;
        }
        // Preview buttons own the animation briefly, so the normal grounded
        // idle/run state machine cannot replace the selected clip instantly.
        if (previewUntil > 0f)
        {
            if (Time.time < previewUntil)
            {
                return;
            }
            previewUntil = -1f;
            if (!locked) Play("Idle", true);
        }
        if (locked)
        {
            return;
        }
        float horizontal = headlessQaInputEnabled ? headlessQaHorizontal : Input.GetAxisRaw("Horizontal");
        currentHorizontal = horizontal;
        body.linearVelocity = new Vector2(horizontal * moveSpeed, body.linearVelocity.y);
        if (horizontal != 0) spriteRenderer.flipX = horizontal < 0;
        bool jumpPressed = headlessQaInputEnabled ? ConsumeHeadlessQaRequest(ref headlessQaJumpRequests) : Input.GetKeyDown(KeyCode.Space);
        if (grounded && jumpPressed)
        {
            grounded = false;
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
            Play("Jump", false);
        }
        bool pointerIsOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool shootPressed = headlessQaInputEnabled ? ConsumeHeadlessQaRequest(ref headlessQaShootRequests) :
            (Input.GetKeyDown(KeyCode.J) || (Input.GetMouseButtonDown(0) && !pointerIsOverUi));
        if (shootPressed)
        {
            StartShootAction();
        }
        bool splatPressed = headlessQaInputEnabled ? ConsumeHeadlessQaRequest(ref headlessQaSplatRequests) : Input.GetKeyDown(KeyCode.K);
        if (splatPressed)
        {
            // Starting Hit locks the actor. Do not let the airborne
            // locomotion branch below replace that clip in this same Update.
            StartCoroutine(SplatThenRegenerate());
            return;
        }
        UpdateLocomotionAnimation(horizontal);
    }

    void FixedUpdate()
    {
        // A contact is ground only when it pushes upward.  Walls, undersides,
        // and a stale collision-exit callback can no longer enable jumping.
        // Some short painted counter tops can replace a contact during their
        // physics step even though the capsule is visibly resting on them.
        // The same downward capsule probe used by QA is the stable fallback;
        // it is deliberately limited to skin-width proximity so corner hits
        // and nearby raised decoration cannot count as ground.
        ReconcileGrounded();
    }

    bool ReconcileGrounded()
    {
        // Contact callbacks and the downward cast cover complementary timing:
        // a resting capsule can report only a contact, while an edge landing
        // can have a short callback gap but a zero-distance cast.  Both must
        // agree with downward/non-rising motion before a jump is permitted.
        groundContacts.RemoveWhere(contact => contact == null || !contact.enabled || !contact.gameObject.activeInHierarchy);
        RaycastHit2D probe = FindGroundProbe();
        bool probeGrounded = probe.collider != null && probe.normal.y >= GroundNormalMinimum && probe.distance <= .055f;
        bool contactGrounded = groundContacts.Count > 0;
        // A resting Rigidbody can retain a tiny upward solver velocity for a
        // physics tick after a teleport/landing.  Accept only that skin-width
        // probe/contact case; a real take-off is orders of magnitude faster
        // (jumpForce is 12), so this cannot classify an airborne jump as ground.
        grounded = body != null && body.linearVelocity.y <= .2f && (contactGrounded || probeGrounded);
        return grounded;
    }

    static bool ConsumeHeadlessQaRequest(ref int requests)
    {
        if (requests <= 0) return false;
        requests--;
        return true;
    }

    void OnCollisionEnter2D(Collision2D collision) => TrackGroundContact(collision);
    void OnCollisionStay2D(Collision2D collision) => TrackGroundContact(collision);
    void OnCollisionExit2D(Collision2D collision) => groundContacts.Remove(collision.otherCollider);

    void TrackGroundContact(Collision2D collision)
    {
        bool hasGroundNormal = false;
        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y >= GroundNormalMinimum)
            {
                hasGroundNormal = true;
                break;
            }
        }
        if (hasGroundNormal) groundContacts.Add(collision.otherCollider);
        else groundContacts.Remove(collision.otherCollider);
    }

    void UpdateLocomotionAnimation(float horizontal)
    {
        // Shoot owns its full clip. Its completion callback chooses the next
        // state on the final-frame tick, so a completed non-looping frame
        // cannot be left displayed until a separately timed Update runs.
        if (shootActionActive) return;
        ResolveLocomotion(horizontal);
    }

    void ResolveLocomotion(float horizontal)
    {
        // Reconcile at every return point. This prevents a stale grounded flag
        // from flashing Jump while the capsule is visibly on a counter.
        ReconcileGrounded();

        if (!grounded)
        {
            if (activeClip != "Jump") Play("Jump", false);
            return;
        }

        string locomotionClip = Mathf.Abs(horizontal) > .01f ? "Run" : "Idle";
        if (activeClip != locomotionClip) Play(locomotionClip, true);
    }

    float ClipDuration(string clip) => clips[clip].Length / frameRate;

    public void PreviewIdle()
    {
        BeginPreview(-1f);
        Play("Idle", true);
    }

    public void PreviewRun()
    {
        BeginPreview(Time.time + 1.8f);
        Play("Run", true);
    }

    public void PreviewJump()
    {
        BeginPreview(Time.time + ClipDuration("Jump"));
        Play("Jump", false);
    }

    public void PreviewShoot()
    {
        BeginPreview(Time.time + ClipDuration("Shoot"));
        StartShootAction();
    }
    public void PreviewSplatRegen()
    {
        if (locked) return;
        CancelShootAction();
        StartCoroutine(SplatThenRegenerate());
    }

    void BeginPreview(float until)
    {
        previewUntil = until;
        CancelShootAction();
        CancelVisualTransition();
    }

    IEnumerator SplatThenRegenerate()
    {
        CancelShootAction();
        locked = true;
        body.simulated = false;
        yield return PlayOnce("Hit");
        yield return new WaitForSeconds(.3f);
        yield return PlayOnce("Regen");
        body.simulated = true;
        locked = false;
        Play("Idle", true);
    }

    public void HazardRespawn(Vector3 respawnPoint)
    {
        if (locked || gameOver) return;
        lives = Mathf.Max(0, lives - 1);
        CancelShootAction();
        UpdateCombatHud();
        if (lives == 0)
        {
            EnterGameOver();
            return;
        }
        StartCoroutine(SplatRegenerateAt(respawnPoint));
    }

    public void SetCheckpoint(Vector3 point) => checkpoint = point;

    IEnumerator SplatRegenerateAt(Vector3 respawnPoint)
    {
        CancelShootAction();
        locked = true;
        body.simulated = false;
        yield return PlayOnce("Hit");
        yield return new WaitForSeconds(.3f);
        body.position = respawnPoint;
        transform.position = respawnPoint;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        groundContacts.Clear();
        yield return PlayOnce("Regen");
        body.simulated = true;
        locked = false;
        Play("Idle", true);
    }

    void StartShootAction()
    {
        if (gameOver || isReloading || ammoInMagazine <= 0) return;
        // Keep the currently authored locomotion body.  The full Shoot source
        // frames redraw torso, hands, smoke and boots differently; blending
        // them only hid the snap for 75 ms.  A short alternating Uzi flash is
        // the actual action layer, so Idle -> Shoot has no body replacement.
        CancelVisualTransition();
        shootActionStarts++;
        shootActionActive = true;
        shootBaseClip = ChooseShootBaseClip();
        shootBaseFrame = FindFrameIndex(shootBaseClip, displayedFrame);
        activeClip = "Shoot";
        if (playback != null) StopCoroutine(playback);
        playback = StartCoroutine(AnimateShootOverlay());
    }

    void FinishShootAction()
    {
        if (!shootActionActive || locked || activeClip != "Shoot") return;
        shootActionActive = false;
        SetMuzzleFlashPulse(-1);
        // Resume from the next body frame rather than resetting to Idle_05.
        // That prevents the visible "stuck in the middle of Idle" return.
        ReconcileGrounded();
        if (!grounded)
        {
            Play("Jump", false);
            return;
        }
        string locomotionClip = Mathf.Abs(currentHorizontal) > .01f ? "Run" : "Idle";
        int resumeFrame = locomotionClip == shootBaseClip ? shootBaseFrame : 0;
        Play(locomotionClip, true, false, resumeFrame);
    }

    void CancelShootAction()
    {
        SetMuzzleFlashPulse(-1);
        if (uziBurstEffects != null) uziBurstEffects.CancelAndClear();
        if (!shootActionActive) return;
        shootActionActive = false;
        CancelVisualTransition();
    }

    // Public callback keeps the HUD interaction stable when Button events are
    // invoked by Unity's EventSystem (and by headless QA).
    public void ReloadFromHud(int gunIndex)
    {
        if (gameOver) return;
        StartReload(Mathf.Clamp(gunIndex, 0, 1), true);
    }

    void StartReload()
    {
        StartReload(-1, false);
    }

    void StartReload(int gunIndex, bool allowNonEmpty)
    {
        if (isReloading || (!allowNonEmpty && ammoInMagazine > 0)) return;
        isReloading = true;
        reloadGunIndex = gunIndex;
        reloadStartedAt = Time.time;
        UpdateCombatHud();
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = StartCoroutine(ReloadMagazine());
    }

    IEnumerator ReloadMagazine()
    {
        yield return new WaitForSeconds(reloadDuration);
        ammoInMagazine = MagazineSize;
        isReloading = false;
        reloadGunIndex = -1;
        reloadStartedAt = -1f;
        reloadRoutine = null;
        UpdateCombatHud();
    }

    void OnDestroy()
    {
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
    }

    void EnterGameOver()
    {
        gameOver = true;
        GameplaySuspended = true;
        locked = true;
        previewUntil = -1f;
        CancelShootAction();
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = null;
        isReloading = false;
        reloadGunIndex = -1;
        reloadStartedAt = -1f;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }
        foreach (WaiterEnemy waiter in FindObjectsByType<WaiterEnemy>(FindObjectsSortMode.None)) waiter.enabled = false;
        foreach (ChefEnemy chef in FindObjectsByType<ChefEnemy>(FindObjectsSortMode.None)) chef.enabled = false;
        foreach (CleaverProjectile cleaver in FindObjectsByType<CleaverProjectile>(FindObjectsSortMode.None)) cleaver.StopThreat();
        if (gameOverOverlay != null) gameOverOverlay.SetActive(true);
        UpdateCombatHud();
    }

    public void RestartGame()
    {
        GameplaySuspended = false;
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
    }

    public void ReturnToMainMenu()
    {
        GameplaySuspended = false;
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }

    /// <summary>Editor QA fixture only: restores a known positive life count before an unrelated traversal test.</summary>
    public void ResetLivesForQa(int value = 3)
    {
        if (!Application.isEditor) return;
        lives = Mathf.Clamp(value, 1, MaxLives);
        UpdateCombatHud();
    }

    void CancelVisualTransition()
    {
        if (transitionFade != null) StopCoroutine(transitionFade);
        transitionFade = null;
        if (spriteRenderer != null) spriteRenderer.color = visualBaseColor;
        if (transitionRenderer != null)
        {
            transitionRenderer.enabled = false;
            transitionRenderer.sprite = null;
        }
    }

    void Play(string clip, bool loop, bool finishShoot = false, int firstFrame = 0)
    {
        // Stopping a coroutine does not run the code after its final yield.
        // Clear a live Shoot action before any unrelated clip replaces it.
        // Shoot restarts intentionally keep the action state alive.
        if (clip != "Shoot" && shootActionActive) CancelShootAction();
        activeClip = clip;
        if (playback != null) StopCoroutine(playback);
        playback = StartCoroutine(Animate(clip, loop, finishShoot, firstFrame));
    }

    IEnumerator PlayOnce(string clip)
    {
        if (clip != "Shoot" && shootActionActive) CancelShootAction();
        activeClip = clip;
        if (playback != null) StopCoroutine(playback);
        playback = StartCoroutine(Animate(clip, false));
        yield return new WaitForSeconds(ClipDuration(clip));
    }

    IEnumerator Animate(string clip, bool loop, bool finishShoot = false, int firstFrame = 0)
    {
        var frames = clips[clip];
        if (frames.Length == 0) yield break;
        do
        {
            for (int offset = 0; offset < frames.Length; offset++)
            {
                int i = (firstFrame + offset) % frames.Length;
                activeFrame = i;
                SetFrame(frames[i]);
                yield return new WaitForSeconds(1f / frameRate);
            }
            firstFrame = 0;
        } while (loop && !locked);
        if (finishShoot) FinishShootAction();
    }

    string ChooseShootBaseClip()
    {
        // Input is sampled before Shoot in Update. Do not let a same-frame D
        // press replace the visible Idle body with a Run body: preserve the
        // actual displayed locomotion source for this shot, then return to Run
        // afterwards if movement remains held.
        if (activeClip == "Idle" || activeClip == "Run") return activeClip;
        if (ContainsFrame("Idle", displayedFrame)) return "Idle";
        if (ContainsFrame("Run", displayedFrame)) return "Run";
        if (Mathf.Abs(currentHorizontal) > .01f) return "Run";
        // A shot fired while airborne must not replace the jump silhouette.
        // Keep its displayed pose for the short action and only add flashes.
        if (!grounded && activeClip == "Jump") return "Jump";
        return "Idle";
    }

    int FindFrameIndex(string clip, Sprite frame)
    {
        if (!clips.TryGetValue(clip, out Sprite[] frames) || frames.Length == 0) return 0;
        for (int i = 0; i < frames.Length; i++)
            if (frames[i] == frame) return i;
        return 0;
    }

    bool ContainsFrame(string clip, Sprite frame)
    {
        if (frame == null || !clips.TryGetValue(clip, out Sprite[] frames)) return false;
        foreach (Sprite candidate in frames)
            if (candidate == frame) return true;
        return false;
    }

    IEnumerator AnimateShootOverlay()
    {
        if (!clips.TryGetValue(shootBaseClip, out Sprite[] baseFrames) || baseFrames.Length == 0)
        {
            FinishShootAction();
            yield break;
        }
        int actionFrames = clips.TryGetValue("Shoot", out Sprite[] shootFrames) ? shootFrames.Length : 8;
        for (int i = 0; i < actionFrames; i++)
        {
            activeFrame = i;
            int baseIndex = shootBaseFrame % baseFrames.Length;
            SetFrame(baseFrames[baseIndex]);
            ConfigureMuzzleFlashPlacement(baseFrames[baseIndex]);
            // Only a paid-for round may make light or sound. A ten-round
            // magazine therefore yields pulses 3+3+3+1, never a cosmetic
            // eleventh/twelfth shot after the magazine is empty.
            int spawned = FireUziBurst(baseFrames[baseIndex], i);
            SetMuzzleFlashPulse(i, spawned > 0);
            shootBaseFrame = (baseIndex + 1) % baseFrames.Length;
            yield return new WaitForSeconds(1f / frameRate);
        }
        FinishShootAction();
    }

    void SetFrame(Sprite frame)
    {
        TomatoFrameGeometry geometry = GetFrameGeometry(frame);
        TomatoFrameGeometry previousGeometry = GetFrameGeometry(displayedFrame);
        bool clipBoundary = displayedFrame != null && displayedClip != activeClip;
        if (clipBoundary)
        {
            clipTransitionSerial++;
            clipTransitionFrom = displayedClip;
            clipTransitionTo = activeClip;
            clipTransitionIou = TomatoFrameGeometry.RegisteredAlphaIou(displayedFrame.texture, previousGeometry, frame.texture, geometry);
        }
        spriteRenderer.sprite = frame;
        displayedFrame = frame;
        displayedClip = activeClip;
        if (!geometry.readable) return;

        // The Rigidbody root and capsule never move for a drawing frame.  We
        // register the fruit body (not a toe, gun or hand) to the capsule
        // centre and move only the renderer child.  That makes animation
        // source padding/pose changes incapable of changing collisions.
        // One renderer scale for the full character lifetime.  Per-frame
        // width/height compensation made the fruit visibly pop (especially
        // in Shoot); registration is position-only now.
        Vector2 scale = StableRendererScale;
        spriteRenderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        Vector3 coreLocal = SpritePixelToLocal(frame, geometry.coreCenter);
        Vector2 targetCore = PhysicsCoreLocal();
        spriteRenderer.transform.localPosition = new Vector3(
            targetCore.x - coreLocal.x * scale.x,
            targetCore.y - coreLocal.y * scale.y,
            0f);
        Vector2 currentCoreToCollider = (Vector2)GetPixelWorldBounds(frame, geometry.coreMin, geometry.coreMax).center - (Vector2)capsule.bounds.center;
        if (clipBoundary && hasDisplayedCoreToCollider)
            clipTransitionBodyDelta = currentCoreToCollider - displayedCoreToCollider;
        displayedCoreToCollider = currentCoreToCollider;
        hasDisplayedCoreToCollider = true;
    }

    void BeginVisualTransition()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null || transitionRenderer == null) return;
        CancelVisualTransition();
        transitionRenderer.sprite = spriteRenderer.sprite;
        transitionRenderer.transform.localPosition = spriteRenderer.transform.localPosition;
        transitionRenderer.transform.localScale = spriteRenderer.transform.localScale;
        transitionRenderer.flipX = spriteRenderer.flipX;
        transitionRenderer.flipY = spriteRenderer.flipY;
        transitionRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        transitionRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        transitionRenderer.color = spriteRenderer.color;
        transitionRenderer.enabled = true;
        transitionFade = StartCoroutine(FadeTransitionVisual());
    }

    IEnumerator FadeTransitionVisual()
    {
        Color mainStart = visualBaseColor;
        Color ghostStart = transitionRenderer.color;
        mainStart.a *= .58f;
        spriteRenderer.color = mainStart;
        float elapsed = 0f;
        while (elapsed < TransitionFadeSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / TransitionFadeSeconds);
            Color main = mainStart;
            main.a = Mathf.Lerp(mainStart.a, 1f, t);
            Color ghost = ghostStart;
            ghost.a = Mathf.Lerp(ghostStart.a, 0f, t);
            spriteRenderer.color = main;
            transitionRenderer.color = ghost;
            yield return null;
        }
        spriteRenderer.color = visualBaseColor;
        transitionRenderer.enabled = false;
        transitionFade = null;
    }

    void AlignPhysicsFootToRoot()
    {
        if (capsule == null || capsule.direction != CapsuleDirection2D.Vertical) return;
        capsule.offset = new Vector2(capsule.offset.x, capsule.size.y * .5f);
    }

    float PhysicsFootLocalY() => capsule == null ? 0f : capsule.offset.y - capsule.size.y * .5f;
    float PhysicsFootLocalX() => capsule == null ? 0f : capsule.offset.x;
    Vector2 PhysicsCoreLocal() => capsule == null ? Vector2.zero : capsule.offset;

    TomatoFrameGeometry GetFrameGeometry(Sprite sprite)
    {
        if (sprite == null) return default;
        int id = sprite.GetInstanceID();
        if (frameGeometry.TryGetValue(id, out TomatoFrameGeometry cached)) return cached;
        TomatoFrameGeometry measured = TomatoFrameGeometry.Measure(sprite.texture);
        frameGeometry[id] = measured;
        return measured;
    }

    AlphaBounds GetAlphaBounds(Sprite sprite)
    {
        if (sprite == null) return default;
        int id = sprite.GetInstanceID();
        if (alphaBounds.TryGetValue(id, out var cached)) return cached;
        TomatoFrameGeometry geometry = GetFrameGeometry(sprite);
        var result = new AlphaBounds {
            readable = geometry.readable,
            min = geometry.readable ? geometry.min : Vector2.zero,
            max = geometry.readable ? geometry.max : sprite.rect.size,
            // Use the weighted bottom band, never a one-pixel toe.
            footCenterX = geometry.readable ? geometry.footCenter.x : sprite.rect.width * .5f
        };
        alphaBounds[id] = result;
        return result;
    }

    Bounds GetAlphaWorldBounds(AlphaBounds alpha)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return default;
        var sprite = spriteRenderer.sprite;
        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, 0f);
        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, 0f);
        foreach (var pixel in new[] {
            new Vector2(alpha.min.x, alpha.min.y), new Vector2(alpha.min.x, alpha.max.y),
            new Vector2(alpha.max.x, alpha.min.y), new Vector2(alpha.max.x, alpha.max.y) })
        {
            Vector3 point = spriteRenderer.transform.TransformPoint(SpritePixelToLocal(sprite, pixel));
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }
        return new Bounds((min + max) * .5f, max - min);
    }

    Bounds GetPixelWorldBounds(Sprite sprite, Vector2 minPixels, Vector2 maxPixels)
    {
        if (spriteRenderer == null || sprite == null) return default;
        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, 0f);
        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, 0f);
        foreach (var pixel in new[] {
            new Vector2(minPixels.x, minPixels.y), new Vector2(minPixels.x, maxPixels.y),
            new Vector2(maxPixels.x, minPixels.y), new Vector2(maxPixels.x, maxPixels.y) })
        {
            Vector3 point = spriteRenderer.transform.TransformPoint(SpritePixelToLocal(sprite, pixel));
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }
        return new Bounds((min + max) * .5f, max - min);
    }

    static float AreaOverlap(Bounds subject, Bounds container)
    {
        float width = Mathf.Max(0f, Mathf.Min(subject.max.x, container.max.x) - Mathf.Max(subject.min.x, container.min.x));
        float height = Mathf.Max(0f, Mathf.Min(subject.max.y, container.max.y) - Mathf.Max(subject.min.y, container.min.y));
        float area = subject.size.x * subject.size.y;
        return area <= .00001f ? 0f : width * height / area;
    }

    static Vector3 FlashChildPosition(Sprite flash, Vector2 flashCenterPixels, Vector3 desiredBodyPoint, bool flipX)
    {
        // The flash renderer has the same flip state as the body.  Its flame
        // centroid therefore sits at +p unflipped and -p flipped, while the
        // desired barrel tip has already received the body's flip.  Solving
        // childPosition + renderedCentroid == desiredTip gives this exact
        // placement for either facing direction.
        Vector3 renderedFlashCenter = SpritePixelToLocal(flash, flashCenterPixels, flipX);
        return desiredBodyPoint - renderedFlashCenter;
    }

    Vector3 SpritePixelToLocal(Sprite sprite, Vector2 pixel) => SpritePixelToLocal(sprite, pixel, spriteRenderer != null && spriteRenderer.flipX, spriteRenderer != null && spriteRenderer.flipY);

    static Vector3 SpritePixelToLocal(Sprite sprite, Vector2 pixel, bool flipX, bool flipY = false)
    {
        Vector2 local = (pixel - sprite.pivot) / sprite.pixelsPerUnit;
        if (flipX) local.x = -local.x;
        if (flipY) local.y = -local.y;
        return local;
    }

    RaycastHit2D FindGroundProbe()
    {
        if (capsule == null) return default;
        int count = capsule.Cast(Vector2.down, groundProbeFilter, groundProbeHits, GroundProbeDistance);
        for (int i = 0; i < count; i++)
        {
            var hit = groundProbeHits[i];
            if (hit.collider != null && !hit.collider.isTrigger && hit.normal.y >= GroundNormalMinimum)
                return hit;
        }
        return default;
    }

    public TomatoRuntimeTelemetry GetTelemetry()
    {
        var c = capsule != null ? capsule.bounds : default;
        var sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        var alpha = GetAlphaBounds(sprite);
        var geometry = GetFrameGeometry(sprite);
        var alphaWorld = GetAlphaWorldBounds(alpha);
        var coreWorld = geometry.readable ? GetPixelWorldBounds(sprite, geometry.coreMin, geometry.coreMax) : default;
        Vector2 registeredCore = geometry.readable && spriteRenderer != null
            ? spriteRenderer.transform.TransformPoint(SpritePixelToLocal(sprite, geometry.coreCenter))
            : c.center;
        Vector2 foot = alpha.readable && spriteRenderer != null
            ? spriteRenderer.transform.TransformPoint(SpritePixelToLocal(sprite, geometry.footCenter))
            : new Vector2(c.center.x, c.min.y);
        Vector2 physicsFoot = new(c.center.x, c.min.y);
        Vector2 origin = capsule == null ? transform.position : new Vector2(c.center.x, c.min.y + .012f);
        RaycastHit2D hit = FindGroundProbe();
        return new TomatoRuntimeTelemetry
        {
            root = transform.position,
            visual = spriteRenderer != null ? spriteRenderer.transform.position : transform.position,
            colliderBounds = c,
            spriteBounds = spriteRenderer != null ? spriteRenderer.bounds : default,
            alphaBounds = alphaWorld,
            coreBounds = coreWorld,
            foot = foot,
            physicsFoot = physicsFoot,
            // The weighted point is used for registration, but all collider
            // fit checks use the independently transformed red-body bounds
            // centre.  Those are not algebraically guaranteed to coincide.
            registrationAnchor = registeredCore,
            registrationAnchorToColliderCenter = registeredCore - (Vector2)c.center,
            coreCenter = coreWorld.center,
            coreToColliderCenter = (Vector2)coreWorld.center - (Vector2)c.center,
            probeOrigin = origin,
            probePoint = hit.collider != null ? hit.point : origin + Vector2.down * GroundProbeDistance,
            probeNormal = hit.collider != null ? hit.normal : Vector2.zero,
            probeDistance = hit.collider != null ? hit.distance : -1f,
            probeCollider = hit.collider != null ? hit.collider.name : "MISS",
            velocity = body != null ? body.linearVelocity : Vector2.zero,
            grounded = grounded,
            locked = locked,
            alphaReadable = alpha.readable,
            clip = activeClip,
            frame = activeFrame,
            frameCount = ActiveFrameCount,
            spritePivotPixels = sprite != null ? sprite.pivot : Vector2.zero,
            texturePixels = sprite != null && sprite.texture != null ? new Vector2(sprite.texture.width, sprite.texture.height) : Vector2.zero,
            visualScale = spriteRenderer != null ? spriteRenderer.transform.localScale : Vector3.one,
            frameName = sprite != null && sprite.texture != null ? sprite.texture.name : "NONE",
            opaquePixels = geometry.opaquePixels,
            bodyPixels = geometry.bodyPixels,
            coreContainment = AreaOverlap(coreWorld, c),
            alphaColliderOverlap = AreaOverlap(alphaWorld, c),
            coreWidthToColliderWidth = c.size.x <= .00001f ? 0f : coreWorld.size.x / c.size.x,
            coreHeightToColliderHeight = c.size.y <= .00001f ? 0f : coreWorld.size.y / c.size.y,
            footToColliderBottom = foot.y - c.min.y,
            footToSurface = hit.collider != null ? foot.y - hit.point.y : float.NaN,
            clipTransitionSerial = clipTransitionSerial,
            clipTransitionFrom = clipTransitionFrom,
            clipTransitionTo = clipTransitionTo,
            clipTransitionIou = clipTransitionIou,
            clipTransitionBodyDelta = clipTransitionBodyDelta,
            shootActionStarts = shootActionStarts,
            shootActionActive = shootActionActive,
            transitionGhostEnabled = transitionRenderer != null && transitionRenderer.enabled,
            transitionFadeActive = transitionFade != null,
            mainVisualAlpha = spriteRenderer != null ? spriteRenderer.color.a : 0f,
            transitionGhostAlpha = transitionRenderer != null ? transitionRenderer.color.a : 0f,
            uziRoundsSpawned = uziBurstEffects != null ? uziBurstEffects.TotalRoundsSpawned : 0,
            uziActiveTracers = uziBurstEffects != null ? uziBurstEffects.ActiveTracerCount : 0,
            uziActiveCasings = uziBurstEffects != null ? uziBurstEffects.ActiveCasingCount : 0,
            uziLastDirection = uziBurstEffects != null ? uziBurstEffects.LastBurstDirection : 0,
            ammoInMagazine = AmmoInMagazine,
            magazineSize = MagazineSize,
            isReloading = IsReloading,
            reloadProgress = ReloadProgress,
            lives = Lives,
            maxLives = MaxLives,
            gameOver = IsGameOver,
        };
    }

    static string F(Vector2 value) => $"({value.x:0.###},{value.y:0.###})";
    static string F(Vector3 value) => $"({value.x:0.###},{value.y:0.###},{value.z:0.###})";
    static string SurfaceDelta(float value) => float.IsNaN(value) ? "MISS" : value.ToString("+0.###;-0.###;0.000");
}
