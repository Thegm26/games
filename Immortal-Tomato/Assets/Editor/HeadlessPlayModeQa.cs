using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Batch-mode Play Mode verification for the real TomatoGame controller.
/// Invoke with: Unity -batchmode -quit -projectPath ... -executeMethod HeadlessPlayModeQa.Run
/// It never touches the desktop input stream: queued QA input is consumed by
/// TomatoGame.Update, then normal physics, contacts, clip playback and respawn
/// coroutines produce the sampled telemetry below.
/// </summary>
[InitializeOnLoad]
public static class HeadlessPlayModeQa
{
    const string PendingKey = "ImmortalTomato.HeadlessQa.Pending";
    const string ExitKey = "ImmortalTomato.HeadlessQa.ExitPending";
    const string ResultKey = "ImmortalTomato.HeadlessQa.Result";
    const string ScenePath = "Assets/Main.unity";
    const double SampleInterval = .05;

    enum Phase { Warmup, Idle, Run, Jump, Recovery, ShootStanding, AmmoReload, ShootSameFrameMove, ShootMoving, ShootRepeat, CancelSplat, CancelHazard, PreviewInterrupt, Splat, RegenWait, LeftBoundary, VisibleFireHazard, Traverse, Complete }

    static readonly HashSet<string> SeenClips = new();
    static readonly HashSet<string> TraversalPlatforms = new();
    static readonly List<string> TraversalSupportSequence = new();
    // These are the support tops a full run must physically touch. The S1
    // pre-fire jump lands safely on the measured Stove Worktop after clearing
    // the left flame and knife cluster. In S2, the safe route deliberately
    // uses the Upper Oven Ingredient/Right Plate shelves to clear the floor
    // flames, so the fire-obstructed Burner Counter remains source-aligned
    // but is not required as a safe-route landing.
    // Remaining measured route colliders are validated by
    // ColliderAlignmentAudit and in the consecutive jump-reachability table.
    static readonly string[] RequiredTraversalSupports =
    {
        "S1 Door Service Counter", "S1 Stove Worktop", "S1 Prep Island",
        "S1 Right Service Counter", "S1 Exit Counter",
        "S2 Knife Gap Ledge", "S2 Oven Counter", "S2 Upper Oven Ingredient Shelf",
        "S2 Upper Right Plate Drawer", "S2 Mixer Counter",
        "S3 Freezer Ledge", "S3 Crate Shelf", "S3 Sausage Counter", "S3 Exit Counter"
    };
    static readonly string[] OptionalBypassSupports =
    {
        "S2 Entry Counter", "S3 Entry Counter", "S3 Frozen Bridge"
    };
    const string ActionSupportName = "S1 Door Service Counter";
    // The door counter now ends at a real, lethal S1 stove flame. Keep the
    // movement/Shoot fixture on the measured service counter to its right so
    // its requested D input tests Shoot's return to Run instead of entering
    // a newly-correct fire trigger.
    const string MovingActionSupportName = "S1 Right Service Counter";
    static TomatoGame tomato;
    static Phase phase;
    static double startedAt;
    static double phaseAt;
    static double nextSampleAt;
    static double nextTraversalJumpAt;
    static bool actionQueued;
    static bool sawGrounded;
    static bool sawLocked;
    static bool alphaReadable = true;
    static float firstX;
    static float maxX;
    static float maxFootDrift;
    static float maxGroundSurfaceDrift;
    static float minimumTraversalY;
    static float maxTraversalAirborneSeconds;
    static int traversalJumps;
    static int traversalLandings;
    static double awaitingLandingSince = -1d;
    static double probeMissSince = -1d;
    static TomatoRuntimeTelemetry lastTraversalTelemetry;
    static int idleSamples;
    static bool idleStateStable;
    static Vector2 idleRootMin, idleRootMax;
    static Vector2 idleVisualOffsetMin, idleVisualOffsetMax;
    static Vector2 idleFootMin, idleFootMax;
    static readonly Dictionary<string, Vector2> StationaryClipOrigins = new();
    static readonly Dictionary<string, float> StationaryClipRootDrift = new();
    sealed class ClipRuntimeMetrics
    {
        public int samples;
        public readonly HashSet<string> frames = new();
        public Vector2 coreDeltaMin = new(float.PositiveInfinity, float.PositiveInfinity);
        public Vector2 coreDeltaMax = new(float.NegativeInfinity, float.NegativeInfinity);
        public Vector2 anchorDeltaMin = new(float.PositiveInfinity, float.PositiveInfinity);
        public Vector2 anchorDeltaMax = new(float.NegativeInfinity, float.NegativeInfinity);
        public Vector2 scaleMin = new(float.PositiveInfinity, float.PositiveInfinity);
        public Vector2 scaleMax = new(float.NegativeInfinity, float.NegativeInfinity);
        public Vector2 maxAdjacentScaleStep;
        public Vector2 previousScale;
        public bool hasPreviousScale;
        public float minCoreContainment = float.PositiveInfinity;
        public float minAlphaOverlap = float.PositiveInfinity;
        public string minAlphaOverlapFrame = "NONE";
        public float minCoreWidthRatio = float.PositiveInfinity;
        public float maxCoreWidthRatio = float.NegativeInfinity;
        public float minCoreHeightRatio = float.PositiveInfinity;
        public float maxCoreHeightRatio = float.NegativeInfinity;
        public float minJumpBootOffset = float.PositiveInfinity;
        public float maxJumpBootOffset = float.NegativeInfinity;
        public bool nativeTexture = true;
    }
    static readonly Dictionary<string, ClipRuntimeMetrics> ClipMetrics = new();
    static readonly Dictionary<string, TomatoClipGeometryAudit> ClipAudits = new();
    static bool sawShoot;
    static bool shootReturnedToGroundLocomotion;
    static bool shootEndedAsGroundedJump;
    static bool hasPreviousStateSample;
    static string previousStateClip;
    static Vector2 previousStateScale;
    static Vector2 previousStateAnchorDelta;
    static float maxStateTransitionScaleStep;
    static float maxStateTransitionAnchorStep;
    static int samples;
    static bool running;
    static int observedTransitionSerial;
    static bool standingShotQueued;
    static bool standingSawShoot;
    static bool standingReturnedIdle;
    static int standingIdleFramesAfterReturn;
    static readonly HashSet<string> StandingIdleFrames = new();
    static int standingUziRoundsBaseline;
    static bool standingUziBurstVerified;
    static bool standingUziDirectionVerified;
    static bool reloadSawEmpty;
    static bool reloadSawProgress;
    static bool reloadLockoutQueued;
    static bool reloadLockoutVerified;
    static int reloadLockoutStarts;
    static bool reloadRefilled;
    static bool reloadPostShotQueued;
    static bool reloadPostShotVerified;
    static int reloadPostShotRoundsBaseline;
    static bool reloadPostShotCompleted;
    static bool movingShotQueued;
    static bool movingPrimedRun;
    static bool movingSawShoot;
    static bool movingReturnedRun;
    static bool sameFrameShotQueued;
    static bool sameFrameSawShoot;
    static bool sameFrameReturnedRun;
    static bool sameFramePreservedIdleBody;
    static float sameFrameEntryIou;
    static bool repeatFirstQueued;
    static bool repeatSecondQueued;
    static int repeatShootStarts;
    static int repeatShootStartBaseline;
    static bool repeatedReturnedIdle;
    static bool shootTransitionGroundJump;
    static float maxShootTransitionBodyStep;
    static float maxShootTransitionScaleStep;
    static readonly List<string> ShootTransitionRecords = new();
    static bool cancelSplatQueued;
    static bool cancelSplatRecovered;
    static bool cancelHazardShootQueued;
    static bool cancelHazardTriggered;
    static bool cancelHazardRecovered;
    static int previewStep;
    static bool previewShootQueued;
    static bool previewInterruptIssued;
    static double previewStepAt;
    static readonly HashSet<string> PreviewValidated = new();
    static int maxTransitionGhostRenderers;
    static bool ghostLeakObserved;
    static bool settledMainAlphaWrong;
    static bool leftBoundaryVerified;
    static float leftBoundaryMinimumRootX;
    static bool visibleFirePlaced;
    static bool visibleFireTriggered;
    static bool visibleFireRecovered;
    static int visibleFireIndex;
    static int visibleFireLivesBefore;
    static int visibleFireVerifiedCount;
    static int traversalLivesAtStart;
    static bool cancelHazardLifeVerified;
    static readonly string[] RepresentativeAnimatedFires = { "S1 Right Stove Fire", "S2 Floor Flame Columns" };

    static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    static string QaDirectory => Path.Combine(ProjectRoot, "QA", "Runtime");
    static string ReportPath => Path.Combine(QaDirectory, "headless_report.log");
    static string TracePath => Path.Combine(QaDirectory, "latest_trace.log");

    static HeadlessPlayModeQa()
    {
        EditorApplication.update += Tick;
        EditorApplication.update += RuntimeUpdateBridge;
    }

    public static void Run()
    {
        try
        {
            Directory.CreateDirectory(QaDirectory);
            SessionState.EraseBool(ExitKey);
            SessionState.SetBool(PendingKey, true);
            File.WriteAllText(ReportPath, $"Immortal Tomato headless Play Mode QA started {DateTime.UtcNow:O}\n", new System.Text.UTF8Encoding(false));
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            WebGLBuilder.ValidateScene();
            WriteReport("[SETUP] serialized scene validation passed; entering Play Mode.");
            EditorApplication.isPlaying = true;
        }
        catch (Exception exception)
        {
            Fail("Setup failed: " + exception);
        }
    }

    static void Tick()
    {
        if (SessionState.GetBool(ExitKey, false) && !EditorApplication.isPlaying)
        {
            int code = SessionState.GetInt(ResultKey, 1);
            SessionState.EraseBool(ExitKey);
            SessionState.EraseBool(PendingKey);
            EditorApplication.Exit(code);
            return;
        }
        if (!SessionState.GetBool(PendingKey, false) || !EditorApplication.isPlaying || running) return;
        BeginRuntime();
    }

    static void BeginRuntime()
    {
        tomato = UnityEngine.Object.FindFirstObjectByType<TomatoGame>();
        if (tomato == null)
        {
            Fail("Play Mode began without TomatoGame.");
            return;
        }
        if (!tomato.UziFxAssetsReady)
        {
            Fail("Uzi FX assets failed to load from Resources.");
            return;
        }
        if (tomato.Lives != 3 || tomato.MaxLives != 3 || tomato.IsGameOver)
        {
            Fail($"Tomato must begin gameplay at 3/3 lives: {tomato.Lives}/{tomato.MaxLives} gameOver={tomato.IsGameOver}.");
            return;
        }
        running = true;
        tomato.WriteFrameAlignmentAudit(Path.Combine(QaDirectory, "frame_alignment_runtime.log"));
        WriteReport("[SETUP] wrote exact per-frame source/registered-world alignment table to QA/Runtime/frame_alignment_runtime.log.");
        // This suite verifies tomato controller, terrain and authored hazard
        // invariants. Enemy behavior is covered separately; disabling their
        // live projectiles here prevents a random combat hit from changing a
        // traversal timing test into a respawn test.
        DisableDynamicEnemyInterference();
        SeenClips.Clear();
        TraversalPlatforms.Clear();
        TraversalSupportSequence.Clear();
        startedAt = phaseAt = EditorApplication.timeSinceStartup;
        nextSampleAt = startedAt;
        firstX = maxX = tomato.transform.position.x;
        maxFootDrift = maxGroundSurfaceDrift = 0f;
        minimumTraversalY = float.PositiveInfinity;
        maxTraversalAirborneSeconds = 0f;
        traversalJumps = traversalLandings = 0;
        awaitingLandingSince = probeMissSince = -1d;
        idleSamples = 0;
        cancelHazardLifeVerified = false;
        idleStateStable = true;
        idleRootMin = idleVisualOffsetMin = idleFootMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        idleRootMax = idleVisualOffsetMax = idleFootMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        StationaryClipOrigins.Clear();
        StationaryClipRootDrift.Clear();
        ClipMetrics.Clear();
        ClipAudits.Clear();
        foreach (string clip in new[] { "Idle", "Run", "Jump", "Shoot", "Hit", "Regen" })
        {
            TomatoClipGeometryAudit audit = tomato.GetClipGeometryAudit(clip);
            ClipAudits[clip] = audit;
            WriteReport($"[GEOMETRY] clip={clip} frames={audit.frameCount} selected={tomato.CuratedFrameNames(clip)} native={audit.identityPass} legacyCoreRange={F(audit.legacyFootAnchoredCoreRangePixels)} registeredCoreRange={F(audit.coreAnchoredCoreRangePixels)} maxSourceAnchorStep={audit.maxAnchorStepPixels:0.###} maxBboxScaleStep={audit.maxBboxScaleStep:0.###} maxAreaScaleStep={audit.maxAreaScaleStep:0.###} minRegisteredIoU={audit.minimumRegisteredAlphaIou:0.###}");
        }
        sawShoot = shootReturnedToGroundLocomotion = shootEndedAsGroundedJump = false;
        observedTransitionSerial = 0;
        standingShotQueued = standingSawShoot = standingReturnedIdle = false;
        standingIdleFramesAfterReturn = 0;
        StandingIdleFrames.Clear();
        standingUziRoundsBaseline = 0;
        standingUziBurstVerified = standingUziDirectionVerified = false;
        movingShotQueued = movingSawShoot = movingReturnedRun = false;
        sameFrameShotQueued = sameFrameSawShoot = sameFrameReturnedRun = sameFramePreservedIdleBody = false;
        sameFrameEntryIou = 0f;
        repeatFirstQueued = repeatSecondQueued = false;
        repeatShootStarts = repeatShootStartBaseline = 0;
        repeatedReturnedIdle = false;
        shootTransitionGroundJump = false;
        maxShootTransitionBodyStep = maxShootTransitionScaleStep = 0f;
        ShootTransitionRecords.Clear();
        cancelSplatQueued = cancelSplatRecovered = false;
        cancelHazardShootQueued = cancelHazardTriggered = cancelHazardRecovered = false;
        previewStep = 0;
        previewShootQueued = previewInterruptIssued = false;
        previewStepAt = 0d;
        PreviewValidated.Clear();
        maxTransitionGhostRenderers = 0;
        ghostLeakObserved = settledMainAlphaWrong = false;
        leftBoundaryVerified = false;
        leftBoundaryMinimumRootX = float.PositiveInfinity;
        visibleFirePlaced = visibleFireTriggered = visibleFireRecovered = false;
        visibleFireIndex = visibleFireLivesBefore = visibleFireVerifiedCount = 0;
        traversalLivesAtStart = 0;
        hasPreviousStateSample = false;
        previousStateClip = string.Empty;
        previousStateScale = previousStateAnchorDelta = Vector2.zero;
        maxStateTransitionScaleStep = maxStateTransitionAnchorStep = 0f;
        samples = 0;
        sawGrounded = sawLocked = false;
        alphaReadable = true;
        tomato.ClearHeadlessQaInput();
        SetPhase(Phase.Warmup);
        WriteReport("[PLAY] actual Play Mode controller found; running scripted physics/input trace.");
    }

    static void DisableDynamicEnemyInterference()
    {
        int disabled = 0;
        foreach (ChefEnemy enemy in UnityEngine.Object.FindObjectsByType<ChefEnemy>(FindObjectsSortMode.None))
        {
            enemy.gameObject.SetActive(false);
            disabled++;
        }
        foreach (WaiterEnemy enemy in UnityEngine.Object.FindObjectsByType<WaiterEnemy>(FindObjectsSortMode.None))
        {
            enemy.gameObject.SetActive(false);
            disabled++;
        }
        foreach (CleaverProjectile projectile in UnityEngine.Object.FindObjectsByType<CleaverProjectile>(FindObjectsSortMode.None))
            projectile.gameObject.SetActive(false);
        WriteReport($"[SETUP] disabled {disabled} dynamic enemies for deterministic controller/terrain QA.");
    }

    static void SetPhase(Phase next)
    {
        phase = next;
        phaseAt = EditorApplication.timeSinceStartup;
        actionQueued = false;
        if ((next == Phase.ShootStanding || next == Phase.AmmoReload || next == Phase.ShootRepeat || next == Phase.CancelSplat || next == Phase.CancelHazard || next == Phase.PreviewInterrupt) && tomato != null)
            PlaceActorOnMidCounter();
        // Earlier placement fixtures can legitimately touch hazards while
        // setting up their own checks. This phase verifies one isolated
        // explicit hazard, so start from the authored three-life baseline.
        if (next == Phase.CancelHazard && tomato != null)
        {
            tomato.ResetLivesForQa();
            WriteReport("[SETUP] reset lives to 3 for isolated hazard-interruption fixture.");
        }
        if (next == Phase.VisibleFireHazard && tomato != null)
        {
            tomato.ResetLivesForQa();
            WriteReport("[SETUP] reset lives to 3 for isolated S1/S2 animated-fire trigger fixtures.");
        }
        if (next == Phase.ShootRepeat && tomato != null)
            repeatShootStartBaseline = tomato.GetTelemetry().shootActionStarts;
        if ((next == Phase.ShootSameFrameMove || next == Phase.ShootMoving) && tomato != null)
            PlaceActorForMovingShootCase();
        if (next == Phase.Traverse && tomato != null)
        {
            // Representative fire contacts intentionally consume lives. The
            // route test is a separate fixture, so begin it at the real 3/3
            // baseline rather than accidentally converting the next hazard
            // into Game Over.
            tomato.ResetLivesForQa();
            traversalLivesAtStart = tomato.Lives;
            WriteReport("[SETUP] reset lives to 3 for isolated full traversal fixture.");
            PlaceTraversalStart(fullRoute: true);
        }
        if (next == Phase.LeftBoundary && tomato != null)
            PlaceTraversalStart();
        if (next == Phase.Traverse && tomato != null)
        {
            TomatoRuntimeTelemetry t = tomato.GetTelemetry();
            RecordTraversalSupport(t.probeCollider);
        }
        WriteReport($"[PHASE] {phase} t={Elapsed():0.000}s");
        WriteTrace($"[HEADLESS-QA:phase] {phase} t={Elapsed():0.000}s");
    }

    static void PlaceActorForMovingShootCase()
    {
        // This is a real painted counter beyond the S1 left-stove flames. It
        // leaves enough measured runway for the one Shoot action without
        // making the action test depend on a fire bypass.
        Collider2D platform = GameObject.Find(MovingActionSupportName).GetComponent<Collider2D>();
        Vector2 position = new(platform.bounds.min.x + .5f, platform.bounds.max.y);
        tomato.Body.position = position;
        tomato.transform.position = position;
        tomato.Body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        WriteReport($"[SETUP] placed moving Shoot action case on {MovingActionSupportName} at {F(position)}.");
    }

    static void PlaceActorOnMidCounter()
    {
        Collider2D platform = GameObject.Find(ActionSupportName).GetComponent<Collider2D>();
        // The art-aligned green-door spawn is the only safe stationary pad on
        // this counter: its centre overlaps the now-correct reachable portion
        // of the S1 left-stove flame. Use the documented spawn coordinate so
        // static controller fixtures cannot spend a life before their input.
        Vector2 position = new(GameplayArtLayout.DoorSpawn.x, platform.bounds.max.y);
        tomato.Body.simulated = true;
        tomato.Body.position = position;
        tomato.transform.position = position;
        tomato.Body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        WriteReport($"[SETUP] placed action case on {ActionSupportName} at {F(position)}; waiting for grounded probe confirmation.");
    }

    static bool IsStableOnMidCounter(out TomatoRuntimeTelemetry t)
    {
        t = tomato.GetTelemetry();
        Collider2D platform = GameObject.Find(ActionSupportName).GetComponent<Collider2D>();
        return t.grounded && !t.locked && t.probeCollider == ActionSupportName &&
               Mathf.Abs(t.velocity.y) <= .2f && Mathf.Abs(t.root.y - platform.bounds.max.y) <= .06f;
    }

    static bool IsStableOnRightServiceCounter(out TomatoRuntimeTelemetry t)
    {
        t = tomato.GetTelemetry();
        Collider2D platform = GameObject.Find(MovingActionSupportName).GetComponent<Collider2D>();
        return t.grounded && !t.locked && t.probeCollider == MovingActionSupportName &&
               Mathf.Abs(t.velocity.y) <= .2f && Mathf.Abs(t.root.y - platform.bounds.max.y) <= .06f;
    }

    static bool HasLoadedMagazine() => tomato != null && !tomato.IsReloading && tomato.AmmoInMagazine == tomato.MagazineSize;

    static string State(TomatoRuntimeTelemetry t) =>
        $"clip={t.clip} frame={t.frameName} root=({t.root.x:0.###},{t.root.y:0.###}) vel=({t.velocity.x:0.###},{t.velocity.y:0.###}) grounded={t.grounded} probe={t.probeCollider} locked={t.locked} shootActive={t.shootActionActive} ammo={t.ammoInMagazine}/{t.magazineSize} reload={t.isReloading}@{t.reloadProgress:0.##} ghost={t.transitionGhostEnabled}/{t.transitionFadeActive} alpha={t.mainVisualAlpha:0.###}";

    static void PlaceTraversalStart(bool fullRoute = false)
    {
        // The actual green-door spawn remains at x180 and is separately
        // audited as safe. The full-route fixture starts farther left on that
        // same measured counter so a real running jump can establish height
        // before its capsule reaches the adjacent fire at x238.
        Vector2 position = fullRoute
            ? GameplayArtLayout.SupportPoint(GameplayArtLayout.DoorSupportName, 90f)
            : GameplayArtLayout.DoorSpawn;
        tomato.Body.position = position;
        tomato.transform.position = position;
        tomato.Body.linearVelocity = Vector2.zero;
        tomato.SetCheckpoint(position);
        Physics2D.SyncTransforms();
        firstX = maxX = position.x;
        WriteReport($"[SETUP] restored measured {(fullRoute ? "full-route" : "green-door")} traversal start on {GameplayArtLayout.DoorSupportName} at {F(position)}.");
    }

    static void RunPreviewInterruptionCase(double now)
    {
        if (previewStep >= 4) return;
        if (!previewShootQueued)
        {
            if (!HasLoadedMagazine() || !IsStableOnMidCounter(out _)) return;
            tomato.QueueHeadlessQaShoot();
            previewShootQueued = true;
            WriteReport($"[ACTION] queued Shoot before preview interruption #{previewStep + 1}.");
            return;
        }
        TomatoRuntimeTelemetry t = tomato.GetTelemetry();
        if (!previewInterruptIssued && t.clip == "Shoot")
        {
            string preview = previewStep switch { 0 => "Idle", 1 => "Run", 2 => "Jump", _ => "Shoot" };
            switch (preview)
            {
                case "Idle": tomato.PreviewIdle(); break;
                case "Run": tomato.PreviewRun(); break;
                case "Jump": tomato.PreviewJump(); break;
                default: tomato.PreviewShoot(); break;
            }
            previewInterruptIssued = true;
            previewStepAt = now;
            WriteReport($"[ACTION] issued Preview{preview} during live Shoot.");
            return;
        }
        // The preview button duration is at most 1.8 seconds. Wait beyond it,
        // then verify it has restored a genuinely advancing grounded Idle.
        if (previewInterruptIssued && now - previewStepAt >= 2.02d && t.grounded && t.clip == "Idle" && !t.shootActionActive && !t.transitionGhostEnabled && !t.transitionFadeActive && t.mainVisualAlpha >= .999f)
        {
            string preview = previewStep switch { 0 => "Idle", 1 => "Run", 2 => "Jump", _ => "Shoot" };
            PreviewValidated.Add(preview);
            WriteReport($"[PREVIEW] {preview} interruption restored Idle frame={t.frameName} ghost={t.transitionGhostEnabled} alpha={t.mainVisualAlpha:0.###}.");
            previewStep++;
            previewShootQueued = previewInterruptIssued = false;
        }
    }

    static void TickRuntime()
    {
        if (!running) return;
        if (tomato == null)
        {
            Fail("TomatoGame was destroyed during Play Mode.");
            return;
        }
        double now = EditorApplication.timeSinceStartup;
        if (now >= nextSampleAt)
        {
            Sample();
            nextSampleAt = now + SampleInterval;
            if (!running) return;
        }

        double age = now - phaseAt;
        switch (phase)
        {
            case Phase.Warmup:
                tomato.SetHeadlessQaMovement(0f);
                if (age >= 1.0) SetPhase(Phase.Idle);
                break;
            case Phase.Idle:
                tomato.SetHeadlessQaMovement(0f);
                if (age >= .65) SetPhase(Phase.Run);
                break;
            case Phase.Run:
                tomato.SetHeadlessQaMovement(1f);
                if (age >= 1.0) SetPhase(Phase.Jump);
                break;
            case Phase.Jump:
                tomato.SetHeadlessQaMovement(1f);
                if (!actionQueued && tomato.IsGrounded)
                {
                    tomato.QueueHeadlessQaJump();
                    actionQueued = true;
                    WriteReport("[ACTION] queued normal jump input.");
                }
                if (age >= 1.25) SetPhase(Phase.Recovery);
                break;
            case Phase.Recovery:
                tomato.SetHeadlessQaMovement(0f);
                if (age >= 1.1) SetPhase(Phase.ShootStanding);
                break;
            case Phase.ShootStanding:
                tomato.SetHeadlessQaMovement(0f);
                if (!standingShotQueued && IsStableOnMidCounter(out _))
                {
                    tomato.QueueHeadlessQaShoot();
                    standingShotQueued = true;
                    standingUziRoundsBaseline = tomato.GetTelemetry().uziRoundsSpawned;
                    WriteReport("[ACTION] queued standing Idle->Shoot input.");
                }
                // Shoot is .727 s; leave more than two full Idle cycles after
                // it returns, proving that the return clip is looping rather
                // than a stopped non-looping idle frame.
                if (standingReturnedIdle && standingUziBurstVerified && standingUziDirectionVerified && standingIdleFramesAfterReturn >= 14 && age >= 1.65) SetPhase(Phase.AmmoReload);
                else if (age >= 4.0) Fail("standing Shoot did not settle on Mid Counter: " + State(tomato.GetTelemetry()));
                break;
            case Phase.AmmoReload:
                tomato.SetHeadlessQaMovement(0f);
                TomatoRuntimeTelemetry reloadTelemetry = tomato.GetTelemetry();
                reloadSawEmpty |= reloadTelemetry.isReloading && reloadTelemetry.ammoInMagazine == 0;
                reloadSawProgress |= reloadTelemetry.isReloading && reloadTelemetry.reloadProgress > .05f && reloadTelemetry.reloadProgress < .98f;
                if (!reloadLockoutQueued && reloadTelemetry.isReloading)
                {
                    reloadLockoutStarts = reloadTelemetry.shootActionStarts;
                    tomato.QueueHeadlessQaShoot();
                    reloadLockoutQueued = true;
                    WriteReport("[ACTION] queued Shoot during reload; it must be ignored.");
                }
                else if (reloadLockoutQueued && reloadTelemetry.isReloading && reloadTelemetry.shootActionStarts == reloadLockoutStarts)
                {
                    reloadLockoutVerified = true;
                }
                reloadRefilled |= !reloadTelemetry.isReloading && reloadTelemetry.ammoInMagazine == reloadTelemetry.magazineSize;
                if (reloadRefilled && !reloadPostShotQueued && IsStableOnMidCounter(out reloadTelemetry))
                {
                    reloadPostShotRoundsBaseline = reloadTelemetry.uziRoundsSpawned;
                    tomato.QueueHeadlessQaShoot();
                    reloadPostShotQueued = true;
                    WriteReport("[ACTION] queued post-reload Shoot input.");
                }
                if (reloadPostShotQueued)
                {
                    reloadPostShotVerified |= reloadTelemetry.uziRoundsSpawned - reloadPostShotRoundsBaseline >= 3 && reloadTelemetry.ammoInMagazine < reloadTelemetry.magazineSize;
                    // Let this post-reload action expend its full magazine,
                    // then wait through that second real reload. The next
                    // independent movement fixture must begin loaded rather
                    // than inheriting hidden test-only ammunition.
                    reloadPostShotCompleted |= reloadPostShotVerified && !reloadTelemetry.shootActionActive && !reloadTelemetry.isReloading && reloadTelemetry.ammoInMagazine == reloadTelemetry.magazineSize;
                }
                if (reloadPostShotCompleted) SetPhase(Phase.ShootSameFrameMove);
                else if (age >= 6.0) Fail("ammo/reload contract did not complete: " + State(reloadTelemetry));
                break;
            case Phase.ShootSameFrameMove:
                // Exact regression: D and J are consumed by TomatoGame.Update
                // in the same frame while the visible body is planted Idle.
                // The first Shoot frame must remain that Idle body, not Run.
                tomato.SetHeadlessQaMovement(1f);
                if (!sameFrameShotQueued && HasLoadedMagazine() && IsStableOnRightServiceCounter(out _))
                {
                    tomato.QueueHeadlessQaShoot();
                    sameFrameShotQueued = true;
                    WriteReport("[ACTION] queued same-frame Idle + D + Shoot input.");
                }
                sameFrameSawShoot |= tomato.ActiveClip == "Shoot";
                sameFrameReturnedRun |= sameFrameSawShoot && tomato.IsGrounded && tomato.ActiveClip == "Run";
                if (sameFramePreservedIdleBody && sameFrameReturnedRun) SetPhase(Phase.ShootMoving);
                else if (age >= 3.0) Fail($"same-frame Idle+D+Shoot did not preserve Idle body then return Run: iou={sameFrameEntryIou:0.###} body={sameFramePreservedIdleBody} {State(tomato.GetTelemetry())}");
                break;
            case Phase.ShootMoving:
                // The preceding ten-round action may have started a reload.
                // Keep this precise fixture planted until the full magazine is
                // available, then exercise the same real moving Shoot path.
                tomato.SetHeadlessQaMovement((movingShotQueued || HasLoadedMagazine()) ? 1f : 0f);
                movingPrimedRun |= HasLoadedMagazine() && tomato.ActiveClip == "Run";
                if (!movingShotQueued && movingPrimedRun && IsStableOnRightServiceCounter(out _))
                {
                    tomato.QueueHeadlessQaShoot();
                    movingShotQueued = true;
                    WriteReport("[ACTION] queued moving Run->Shoot input.");
                }
                if (movingReturnedRun) SetPhase(Phase.ShootRepeat);
                else if (age >= 3.0) Fail("moving Shoot did not return to grounded Run: " + State(tomato.GetTelemetry()));
                break;
            case Phase.ShootRepeat:
                tomato.SetHeadlessQaMovement(0f);
                bool repeatReady = IsStableOnMidCounter(out TomatoRuntimeTelemetry repeatTelemetry);
                if (!repeatFirstQueued && HasLoadedMagazine() && repeatReady)
                {
                    tomato.QueueHeadlessQaShoot();
                    repeatFirstQueued = true;
                    WriteReport("[ACTION] queued repeated-shot first input.");
                }
                else if (repeatFirstQueued && !repeatSecondQueued && repeatReady && age >= .08)
                {
                    tomato.QueueHeadlessQaShoot();
                    repeatSecondQueued = true;
                    WriteReport("[ACTION] queued repeated-shot restart input.");
                }
                if (repeatedReturnedIdle && repeatShootStarts >= 2 && age >= 1.6) SetPhase(Phase.CancelSplat);
                else if (age >= 4.5) Fail("repeated Shoot did not restore grounded Idle: " + State(repeatTelemetry));
                break;
            case Phase.CancelSplat:
                tomato.SetHeadlessQaMovement(0f);
                if (!cancelSplatQueued && HasLoadedMagazine() && IsStableOnMidCounter(out _))
                {
                    // Both requests are consumed by TomatoGame.Update in the
                    // same frame: Shoot starts, then K interrupts it.
                    tomato.QueueHeadlessQaShoot();
                    tomato.QueueHeadlessQaSplat();
                    cancelSplatQueued = true;
                    WriteReport("[ACTION] queued same-frame Shoot + Splat interruption.");
                }
                if (cancelSplatRecovered) SetPhase(Phase.CancelHazard);
                else if (age >= 4.5) Fail("same-frame Shoot+Splat did not settle after Regen: " + State(tomato.GetTelemetry()));
                break;
            case Phase.CancelHazard:
                tomato.SetHeadlessQaMovement(0f);
                if (!cancelHazardShootQueued && HasLoadedMagazine() && IsStableOnMidCounter(out _))
                {
                    tomato.QueueHeadlessQaShoot();
                    cancelHazardShootQueued = true;
                    WriteReport("[ACTION] queued Shoot for hazard interruption.");
                }
                if (cancelHazardRecovered) SetPhase(Phase.PreviewInterrupt);
                else if (age >= 4.5) Fail("hazard Shoot interruption did not settle after Regen: " + State(tomato.GetTelemetry()));
                break;
            case Phase.PreviewInterrupt:
                tomato.SetHeadlessQaMovement(0f);
                RunPreviewInterruptionCase(now);
                if (previewStep >= 4 && age >= 9.4) SetPhase(Phase.Splat);
                break;
            case Phase.Splat:
                tomato.SetHeadlessQaMovement(0f);
                if (!actionQueued)
                {
                    tomato.QueueHeadlessQaSplat();
                    actionQueued = true;
                    WriteReport("[ACTION] queued normal splat/regenerate input.");
                }
                if (age >= .25) SetPhase(Phase.RegenWait);
                break;
            case Phase.RegenWait:
                tomato.SetHeadlessQaMovement(0f);
                if (age >= 2.35) SetPhase(Phase.LeftBoundary);
                break;
            case Phase.LeftBoundary:
                // Drive into the edge under real Rigidbody2D physics.  The
                // wall's inside face is flush with source pixel x=0, so the
                // tomato capsule must stop before it can leave the panorama.
                tomato.SetHeadlessQaMovement(-1f);
                leftBoundaryMinimumRootX = Mathf.Min(leftBoundaryMinimumRootX, tomato.transform.position.x);
                if (age >= 1.1)
                {
                    GameplayArtLayout.BoundarySpec boundary = GameplayArtLayout.LeftWorldBoundary;
                    float minimumLegalRoot = boundary.InnerFaceWorldX + tomato.GetComponent<CapsuleCollider2D>().bounds.extents.x - .04f;
                    leftBoundaryVerified = tomato.IsGrounded && tomato.transform.position.x >= minimumLegalRoot;
                    if (!leftBoundaryVerified)
                    {
                        Fail($"left boundary failed containment: rootX={tomato.transform.position.x:0.###} legalMin={minimumLegalRoot:0.###} minSeen={leftBoundaryMinimumRootX:0.###}");
                        break;
                    }
                    WriteReport($"[BOUNDARY] actual leftward run stopped at rootX={tomato.transform.position.x:0.###}; legalMin={minimumLegalRoot:0.###}; minSeen={leftBoundaryMinimumRootX:0.###}.");
                    SetPhase(Phase.VisibleFireHazard);
                }
                break;
            case Phase.VisibleFireHazard:
                tomato.SetHeadlessQaMovement(0f);
                if (!visibleFirePlaced)
                {
                    // These are real animated fire groups, not a direct
                    // HazardRespawn call: place the tomato capsule in the
                    // matching source-measured trigger and wait for physics.
                    string fireName = RepresentativeAnimatedFires[visibleFireIndex];
                    Collider2D fire = GameObject.Find(fireName).GetComponent<Collider2D>();
                    tomato.Body.simulated = true;
                    tomato.Body.position = fire.bounds.center;
                    tomato.transform.position = fire.bounds.center;
                    tomato.Body.linearVelocity = Vector2.zero;
                    tomato.SetCheckpoint(GameplayArtLayout.DoorSpawn);
                    Physics2D.SyncTransforms();
                    visibleFireLivesBefore = tomato.Lives;
                    visibleFirePlaced = true;
                    WriteReport($"[HAZARD] placed real tomato capsule in {fireName} at {F(fire.bounds.center)}; awaiting trigger regeneration.");
                }
                TomatoRuntimeTelemetry fireTelemetry = tomato.GetTelemetry();
                visibleFireTriggered |= visibleFirePlaced && fireTelemetry.locked;
                visibleFireRecovered |= visibleFireTriggered && !fireTelemetry.locked && fireTelemetry.grounded && fireTelemetry.clip == "Idle" &&
                                       Vector2.Distance(fireTelemetry.root, GameplayArtLayout.DoorSpawn) <= .06f;
                if (visibleFireRecovered)
                {
                    if (fireTelemetry.lives != visibleFireLivesBefore - 1 || fireTelemetry.gameOver)
                    {
                        Fail($"Each real animated fire must consume exactly one life: before={visibleFireLivesBefore} after={fireTelemetry.lives}/{fireTelemetry.maxLives} gameOver={fireTelemetry.gameOver}.");
                        break;
                    }
                    WriteReport($"[HAZARD] {RepresentativeAnimatedFires[visibleFireIndex]} trigger caused real Hit/Regen, consumed exactly one life, and returned to the measured door checkpoint.");
                    visibleFireVerifiedCount++;
                    if (visibleFireVerifiedCount == RepresentativeAnimatedFires.Length)
                        SetPhase(Phase.Traverse);
                    else
                    {
                        visibleFireIndex++;
                        visibleFirePlaced = visibleFireTriggered = visibleFireRecovered = false;
                        phaseAt = now;
                    }
                }
                else if (age >= 4.5) Fail("animated fire trigger did not cause a complete physical hazard respawn: " + State(fireTelemetry));
                break;
            case Phase.Traverse:
                tomato.SetHeadlessQaMovement(1f);
                TomatoRuntimeTelemetry traversalTelemetry = tomato.GetTelemetry();
                if (traversalTelemetry.gameOver || traversalTelemetry.lives != traversalLivesAtStart)
                {
                    Fail($"Traversal entered a lethal fire instead of bypassing it: lives={traversalTelemetry.lives}/{traversalTelemetry.maxLives} gameOver={traversalTelemetry.gameOver} root={F(traversalTelemetry.root)} probe={traversalTelemetry.probeCollider}.");
                    break;
                }
                // A landing can occur between 50 ms samples. Credit it before
                // another jump is queued, otherwise the new take-off would
                // overwrite the outstanding landing timer.
                if (tomato.IsGrounded)
                    RecordTraversalSupport(tomato.GetTelemetry().probeCollider);
                if (tomato.IsGrounded && awaitingLandingSince >= 0d)
                    RecordTraversalLanding(now);
                if (age >= 12.0 && tomato.IsGrounded && tomato.transform.position.x >= GameplayArtLayout.Surface("S3 Exit Counter").LeftWorld + 1f && tomato.GetTelemetry().probeCollider == "S3 Exit Counter")
                {
                    Sample(); // Final result must use this physics step, not the prior sample.
                    Complete();
                    break;
                }
                if (tomato.IsGrounded && now >= nextTraversalJumpAt && !tomato.IsLocked && ShouldTraversalJump(tomato))
                {
                    tomato.QueueHeadlessQaJump();
                    nextTraversalJumpAt = now + .62;
                    traversalJumps++;
                    awaitingLandingSince = now;
                    WriteTrace($"[HEADLESS-QA:action] traversal-jump x={tomato.transform.position.x:0.###} t={Elapsed():0.000}s");
                }
                if (age >= 28.0) Complete();
                break;
        }
    }

    static void Sample()
    {
        TomatoRuntimeTelemetry t = tomato.GetTelemetry();
        samples++;
        SeenClips.Add(t.clip);
        sawGrounded |= t.grounded;
        sawLocked |= t.locked;
        alphaReadable &= t.alphaReadable;
        maxX = Mathf.Max(maxX, t.root.x);
        // Feet are meaningful only for locomotion.  Hit/Regen contain smoke,
        // splat and rebuild effects whose lowest alpha is intentionally not a
        // boot; their core/capsule containment is measured separately.
        bool groundedLocomotion = t.grounded && (t.clip == "Idle" || t.clip == "Run");
        if (groundedLocomotion)
            maxFootDrift = Mathf.Max(maxFootDrift, Mathf.Abs(t.footToColliderBottom));
        TrackClipGeometry(t);
        if (t.clip == "Shoot") sawShoot = true;
        bool normalShootPhase = phase == Phase.ShootStanding || phase == Phase.ShootMoving || phase == Phase.ShootRepeat;
        if (normalShootPhase && sawShoot && t.grounded && t.clip == "Jump") shootEndedAsGroundedJump = true;
        if (sawShoot && t.grounded && (t.clip == "Idle" || t.clip == "Run")) shootReturnedToGroundLocomotion = true;
        TrackInterruptionLifecycle(t);
        TrackShootTransition(t);
        TrackScriptedShootCases(t);
        TrackIdleStability(t);
        TrackStationaryActionStability(t);
        if (phase == Phase.Traverse)
        {
            double now = EditorApplication.timeSinceStartup;
            lastTraversalTelemetry = t;
            minimumTraversalY = Mathf.Min(minimumTraversalY, t.root.y);
            if (t.grounded) RecordTraversalSupport(t.probeCollider);
            if (awaitingLandingSince >= 0d)
            {
                float airborne = (float)(now - awaitingLandingSince);
                maxTraversalAirborneSeconds = Mathf.Max(maxTraversalAirborneSeconds, airborne);
                if (t.grounded && airborne > .08f)
                {
                    RecordTraversalLanding(now);
                }
                else if (airborne > 1.65f)
                {
                    Fail($"Traversal jump did not land within 1.65 seconds (x={t.root.x:0.###}, y={t.root.y:0.###}).");
                    return;
                }
            }
            if (t.probeCollider == "MISS")
            {
                if (probeMissSince < 0d) probeMissSince = now;
                else if (now - probeMissSince > 1.35d)
                {
                    Fail($"Traversal lost all authored ground probes for {now - probeMissSince:0.###} seconds.");
                    return;
                }
            }
            else probeMissSince = -1d;
            if (t.root.y < -6f || t.velocity.y < -24f)
            {
                Fail($"Traversal fell beyond safe bounds: root=({t.root.x:0.###},{t.root.y:0.###}) velocityY={t.velocity.y:0.###}.");
                return;
            }
        }
        // A probe can see a shelf while the capsule is passing it in mid-air,
        // and Unity can retain the previous grounded flag for that one
        // simulation step. Only audit a settled support contact here.
        if (groundedLocomotion && Mathf.Abs(t.velocity.y) <= .1f && !float.IsNaN(t.footToSurface) && t.probeCollider != "MISS" && t.probeNormal.y >= .95f && t.probeDistance <= .04f)
            maxGroundSurfaceDrift = Mathf.Max(maxGroundSurfaceDrift, Mathf.Abs(t.footToSurface));

        if (float.IsNaN(t.root.x) || float.IsNaN(t.root.y) || float.IsInfinity(t.root.x) || float.IsInfinity(t.root.y))
            Fail("Non-finite Tomato root position.");
        if (t.frameCount <= 0 || t.frame < 0 || t.frame >= t.frameCount)
            Fail($"Invalid animation frame state: {t.clip} {t.frame + 1}/{t.frameCount}.");

        string line = string.Format(CultureInfo.InvariantCulture,
            "[HEADLESS-QA:sample] t={0:0.000} phase={1} root=({2:0.###},{3:0.###}) visual=({4:0.###},{5:0.###}) vel=({6:0.###},{7:0.###}) clip={8} frame={9}/{10} name={11} tex=({12:0},{13:0}) scale=({14:0.###},{15:0.###}) grounded={16} locked={17} collider=L{18:0.###},R{19:0.###},B{20:0.###},T{21:0.###} core=({22:0.###},{23:0.###}) coreDelta=({24:0.###},{25:0.###}) coreContain={26:0.###} alphaOverlap={27:0.###} foot=({28:0.###},{29:0.###}) physFoot=({30:0.###},{31:0.###}) footVsCollider={32:0.###} footVsSurface={33} probe={34}",
            Elapsed(), phase, t.root.x, t.root.y, t.visual.x, t.visual.y, t.velocity.x, t.velocity.y,
            t.clip, t.frame + 1, t.frameCount, t.frameName, t.texturePixels.x, t.texturePixels.y, t.visualScale.x, t.visualScale.y, t.grounded, t.locked,
            t.colliderBounds.min.x, t.colliderBounds.max.x, t.colliderBounds.min.y, t.colliderBounds.max.y,
            t.coreCenter.x, t.coreCenter.y, t.coreToColliderCenter.x, t.coreToColliderCenter.y, t.coreContainment, t.alphaColliderOverlap,
            t.foot.x, t.foot.y, t.physicsFoot.x, t.physicsFoot.y, t.footToColliderBottom,
            float.IsNaN(t.footToSurface) ? "MISS" : t.footToSurface.ToString("0.###", CultureInfo.InvariantCulture), t.probeCollider);
        WriteTrace(line);
    }

    static void Complete()
    {
        if (!running) return;
        tomato.ClearHeadlessQaInput();
        var errors = new List<string>();
        foreach (string clip in new[] { "Idle", "Run", "Jump", "Shoot", "Hit", "Regen" })
            if (!SeenClips.Contains(clip)) errors.Add("clip not observed: " + clip);
        if (!sawGrounded) errors.Add("never observed grounded contact");
        if (!sawLocked) errors.Add("splat/regeneration lock was never observed");
        if (!alphaReadable) errors.Add("a displayed animation frame had unreadable alpha bounds");
        // Core registration deliberately preserves the fruit torso while a
        // run-cycle boot alternates above/below the surface.  This robust
        // bottom-band allowance is tight enough to catch a detached sprite,
        // without requiring every painted boot to be nailed to a single row.
        if (maxFootDrift > .17f) errors.Add($"grounded locomotion boot-band/collider offset is {maxFootDrift:0.###} (limit .17)");
        if (maxGroundSurfaceDrift > .17f) errors.Add($"grounded locomotion boot-band/surface offset is {maxGroundSurfaceDrift:0.###} (limit .17)");
        Vector2 idleRootRange = Range(idleRootMin, idleRootMax);
        Vector2 idleVisualOffsetRange = Range(idleVisualOffsetMin, idleVisualOffsetMax);
        Vector2 idleFootRange = Range(idleFootMin, idleFootMax);
        if (idleSamples < 5) errors.Add($"only {idleSamples} settled idle samples were captured");
        if (!idleStateStable) errors.Add("settled idle was not continuously grounded Idle");
        if (idleRootRange.x > .01f || idleRootRange.y > .01f) errors.Add($"settled idle physics root drift is {F(idleRootRange)} (limit .01)");
        // Renderer-child offset is intentionally allowed to change: it is the
        // registration correction that keeps the tomato core fixed.  The
        // physical root and measured core/capsule range below are the actual
        // no-jitter invariant.  Boot-band movement remains bounded so a bad
        // source frame cannot appear detached from the counter.
        if (idleFootRange.x > .14f || idleFootRange.y > .14f) errors.Add($"settled idle robust-foot drift is {F(idleFootRange)} (limit .14)");
        foreach (var pair in StationaryClipRootDrift)
            if (pair.Value > .01f) errors.Add($"stationary {pair.Key} physics-root drift is {pair.Value:0.###} (limit .01)");
        foreach (string clip in new[] { "Idle", "Run", "Jump", "Shoot", "Hit", "Regen" })
        {
            if (!ClipAudits.TryGetValue(clip, out TomatoClipGeometryAudit audit) || !audit.identityPass)
                errors.Add($"{clip} did not use native readable 420x724 source frames");
            if (!ClipMetrics.TryGetValue(clip, out ClipRuntimeMetrics metrics) || metrics.samples == 0)
            {
                errors.Add($"no runtime geometry samples captured for {clip}");
                continue;
            }
            Vector2 anchorRange = Range(metrics.anchorDeltaMin, metrics.anchorDeltaMax);
            Vector2 scaleRange = Range(metrics.scaleMin, metrics.scaleMax);
            if (!metrics.nativeTexture) errors.Add($"{clip} runtime texture dimensions/aspect were not native 420x724");
            // Registration uses a weighted point; this separately measured
            // red-body bounding centre is intentionally not used by SetFrame.
            if (anchorRange.x > .001f || anchorRange.y > .001f)
                errors.Add($"{clip} registration-anchor/collider drift is {F(anchorRange)} (limit .001)");
            if (metrics.minCoreContainment < .75f)
                errors.Add($"{clip} independent red-body/capsule containment is {metrics.minCoreContainment:0.###} (limit .75)");
            // At least 65% of the complete visible alpha rectangle (including
            // guns and arms) must remain within the gameplay capsule.  This
            // catches a visual/collider separation rather than only logging it.
            if (metrics.minAlphaOverlap < .65f)
                errors.Add($"{clip} alpha/capsule overlap is {metrics.minAlphaOverlap:0.###} on {metrics.minAlphaOverlapFrame} (limit .65)");
            if (scaleRange.x > .00001f || scaleRange.y > .00001f || metrics.maxAdjacentScaleStep.x > .00001f || metrics.maxAdjacentScaleStep.y > .00001f)
                errors.Add($"{clip} renderer scale changed within clip: range={F(scaleRange)} adjacent={F(metrics.maxAdjacentScaleStep)} (must be 0)");
        }
        if (maxStateTransitionScaleStep > .00001f)
            errors.Add($"renderer scale changed across animation-state transition: {maxStateTransitionScaleStep:0.######} (must be 0)");
        if (maxStateTransitionAnchorStep > .001f)
            errors.Add($"registration anchor changed across animation-state transition: {maxStateTransitionAnchorStep:0.######} (limit .001)");
        if (!ClipMetrics.TryGetValue("Jump", out ClipRuntimeMetrics jumpMetrics))
        {
            errors.Add("Jump-specific collider/silhouette metrics were not captured");
        }
        else
        {
            // Independent body bounds must sit inside the fixed gameplay
            // capsule with a substantial body size—not a tiny accidental hit.
            float maxJumpCentreX = Mathf.Max(Mathf.Abs(jumpMetrics.coreDeltaMin.x), Mathf.Abs(jumpMetrics.coreDeltaMax.x));
            float maxJumpCentreY = Mathf.Max(Mathf.Abs(jumpMetrics.coreDeltaMin.y), Mathf.Abs(jumpMetrics.coreDeltaMax.y));
            if (maxJumpCentreX > .18f || maxJumpCentreY > .18f)
                errors.Add($"Jump independent body-centre/capsule delta is ({maxJumpCentreX:0.###},{maxJumpCentreY:0.###}) (limit .18)");
            if (jumpMetrics.minCoreWidthRatio < .28f || jumpMetrics.maxCoreWidthRatio > .62f || jumpMetrics.minCoreHeightRatio < .18f || jumpMetrics.maxCoreHeightRatio > .42f)
                errors.Add($"Jump core/capsule ratios are width [{jumpMetrics.minCoreWidthRatio:0.###},{jumpMetrics.maxCoreWidthRatio:0.###}] height [{jumpMetrics.minCoreHeightRatio:0.###},{jumpMetrics.maxCoreHeightRatio:0.###}] (required width .28-.62, height .18-.42)");
            if (jumpMetrics.minCoreContainment < .85f || jumpMetrics.minAlphaOverlap < .65f)
                errors.Add($"Jump collider/silhouette overlap insufficient: coreContain={jumpMetrics.minCoreContainment:0.###} alphaOverlap={jumpMetrics.minAlphaOverlap:0.###} on {jumpMetrics.minAlphaOverlapFrame}");
            if (jumpMetrics.minJumpBootOffset < -.28f || jumpMetrics.maxJumpBootOffset > .28f)
                errors.Add($"Jump airborne boot-band offset [{jumpMetrics.minJumpBootOffset:0.###},{jumpMetrics.maxJumpBootOffset:0.###}] exceeds -.28..+.28");
        }
        if (!sawShoot) errors.Add("Shoot action was never observed");
        if (!shootReturnedToGroundLocomotion) errors.Add("Shoot did not return to grounded Idle/Run");
        if (shootEndedAsGroundedJump) errors.Add("Shoot transitioned to Jump while physically grounded");
        if (!standingSawShoot || !standingReturnedIdle) errors.Add("standing Idle->Shoot did not return to grounded Idle");
        if (!standingUziBurstVerified || !standingUziDirectionVerified)
            errors.Add($"standing Shoot Uzi burst contract failed: rounds={standingUziBurstVerified} direction={standingUziDirectionVerified}");
        if (!reloadSawEmpty || !reloadSawProgress || !reloadLockoutVerified || !reloadRefilled || !reloadPostShotVerified || !reloadPostShotCompleted)
            errors.Add($"magazine/reload contract failed: empty={reloadSawEmpty} progress={reloadSawProgress} lockout={reloadLockoutVerified} refilled={reloadRefilled} postShot={reloadPostShotVerified}/{reloadPostShotCompleted}");
        if (standingIdleFramesAfterReturn < 14 || StandingIdleFrames.Count < 4)
            errors.Add($"standing Shoot return did not loop through two full Idle cycles (samples={standingIdleFramesAfterReturn}, frames={string.Join(",", StandingIdleFrames)})");
        if (!movingShotQueued || !movingSawShoot || !movingReturnedRun)
            errors.Add("moving Run->Shoot did not return directly to grounded Run while input remained held");
        if (!sameFrameShotQueued || !sameFrameSawShoot || !sameFrameReturnedRun || !sameFramePreservedIdleBody || sameFrameEntryIou < .95f)
            errors.Add($"same-frame Idle+D+Shoot replaced its Idle body or failed to resume Run (iou={sameFrameEntryIou:0.###}, preserved={sameFramePreservedIdleBody}, returned={sameFrameReturnedRun})");
        if (!repeatSecondQueued || repeatShootStarts < 2 || !repeatedReturnedIdle)
            errors.Add($"repeated Shoot did not restart and return predictably (starts={repeatShootStarts}, returnedIdle={repeatedReturnedIdle})");
        if (shootTransitionGroundJump) errors.Add("Shoot boundary selected Jump while physically grounded");
        if (maxShootTransitionBodyStep > .035f)
            errors.Add($"Shoot transition registered body-centre step is {maxShootTransitionBodyStep:0.###} (limit .035)");
        if (maxShootTransitionScaleStep > .00001f)
            errors.Add($"Shoot transition renderer scale step is {maxShootTransitionScaleStep:0.######} (must be 0)");
        if (!cancelSplatQueued || !cancelSplatRecovered)
            errors.Add("same-frame Shoot+Splat did not complete Regen and restore grounded Idle with Shoot cleared");
        if (!cancelHazardShootQueued || !cancelHazardTriggered || !cancelHazardRecovered)
            errors.Add("hazard respawn during Shoot did not complete Regen and restore grounded Idle with Shoot cleared");
        if (!leftBoundaryVerified)
            errors.Add($"left world boundary did not contain a real leftward run (minRoot={leftBoundaryMinimumRootX:0.###})");
        if (visibleFireVerifiedCount != RepresentativeAnimatedFires.Length)
            errors.Add($"representative animated S1/S2 fires did not each complete a real trigger-driven regeneration ({visibleFireVerifiedCount}/{RepresentativeAnimatedFires.Length})");
        if (PreviewValidated.Count != 4)
            errors.Add($"not all Preview interruptions restored normal Idle: {string.Join(",", PreviewValidated)}");
        if (maxTransitionGhostRenderers > 1 || ghostLeakObserved)
            errors.Add($"transition ghost lifecycle invalid: max={maxTransitionGhostRenderers} leak={ghostLeakObserved}");
        if (settledMainAlphaWrong)
            errors.Add("main renderer alpha was not restored after a settled transition/interruption");
        float advance = maxX - firstX;
        if (advance < 90f) errors.Add($"door-to-exit traversal advanced only {advance:0.###} units (limit 90)");
        if (minimumTraversalY < -6f) errors.Add($"traversal left the authored vertical route (minimum y {minimumTraversalY:0.###})");
        if (traversalJumps == 0 || traversalLandings < traversalJumps) errors.Add($"only {traversalLandings}/{traversalJumps} traversal jumps landed");
        if (maxTraversalAirborneSeconds > 1.65f) errors.Add($"traversal airborne duration was {maxTraversalAirborneSeconds:0.###} seconds");
        if (!lastTraversalTelemetry.grounded || lastTraversalTelemetry.root.x < GameplayArtLayout.Surface("S3 Exit Counter").LeftWorld + 1f || lastTraversalTelemetry.probeCollider != "S3 Exit Counter")
            errors.Add($"traversal did not finish grounded on the authored route (root=({lastTraversalTelemetry.root.x:0.###},{lastTraversalTelemetry.root.y:0.###}), grounded={lastTraversalTelemetry.grounded})");
        foreach (string platform in RequiredTraversalSupports)
        {
            if (!TraversalPlatforms.Contains(platform)) errors.Add("traversal never landed on " + platform);
        }

        WriteReport($"[RESULT] samples={samples} maxAdvance={advance:0.###} traversalMinY={minimumTraversalY:0.###} jumps={traversalJumps} landings={traversalLandings} maxAir={maxTraversalAirborneSeconds:0.###} landed={string.Join(",", TraversalPlatforms)} supportSequence={string.Join(" > ", TraversalSupportSequence)} requiredSupports={string.Join(",", RequiredTraversalSupports)} optionalBypasses={string.Join(",", OptionalBypassSupports)} final=({lastTraversalTelemetry.root.x:0.###},{lastTraversalTelemetry.root.y:0.###}) grounded={lastTraversalTelemetry.grounded} leftBoundary={leftBoundaryVerified}@minRoot={leftBoundaryMinimumRootX:0.###} animatedFire={visibleFireVerifiedCount}/{RepresentativeAnimatedFires.Length} maxFootDrift={maxFootDrift:0.###} maxGroundSurfaceDrift={maxGroundSurfaceDrift:0.###} idleSamples={idleSamples} idleRootRange={F(idleRootRange)} idleVisualOffsetRange={F(idleVisualOffsetRange)} idleFootRange={F(idleFootRange)} stationaryRootDrift={FormatClipDrift()} clipGeometry={FormatClipGeometry()} stateTransitionScaleStep={maxStateTransitionScaleStep:0.######} stateTransitionAnchorStep={maxStateTransitionAnchorStep:0.######} shootGroundReturn={shootReturnedToGroundLocomotion} shootGroundJump={shootEndedAsGroundedJump} shootBoundaryBodyStep={maxShootTransitionBodyStep:0.###} shootBoundaryScaleStep={maxShootTransitionScaleStep:0.######} sameFrameIdleToShootIou={sameFrameEntryIou:0.###} sameFrameIdleBody={sameFramePreservedIdleBody} sameFrameRunReturn={sameFrameReturnedRun} standingIdleFrames={standingIdleFramesAfterReturn}/{StandingIdleFrames.Count} reload={reloadSawEmpty}/{reloadSawProgress}/{reloadLockoutVerified}/{reloadRefilled}/{reloadPostShotVerified}/{reloadPostShotCompleted} movingRunReturn={movingReturnedRun} repeatedStarts={repeatShootStarts} repeatedIdleReturn={repeatedReturnedIdle} cancelSplat={cancelSplatRecovered} cancelHazard={cancelHazardRecovered} previews={string.Join(",", PreviewValidated)} ghostMax={maxTransitionGhostRenderers} ghostLeak={ghostLeakObserved} alphaRestored={!settledMainAlphaWrong} shootTransitions={string.Join(" | ", ShootTransitionRecords)} clips={string.Join(",", SeenClips)}");
        if (errors.Count == 0)
        {
            WriteReport("[PASS] Headless Play Mode physics, controller, animation and positional-invariant QA passed.");
            WriteTrace("[HEADLESS-QA:PASS] all runtime invariants passed.");
            End(0);
        }
        else Fail("Runtime QA failed: " + string.Join("; ", errors));
    }

    static void Fail(string message)
    {
        Debug.LogError("[HEADLESS-QA:FAIL] " + message);
        try
        {
            Directory.CreateDirectory(QaDirectory);
            WriteReport("[FAIL] " + message);
            WriteTrace("[HEADLESS-QA:FAIL] " + message);
        }
        catch { }
        End(1);
    }

    static void End(int result)
    {
        running = false;
        SessionState.SetInt(ResultKey, result);
        SessionState.SetBool(ExitKey, true);
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
    }

    static double Elapsed() => EditorApplication.timeSinceStartup - startedAt;

    static void RecordTraversalLanding(double now)
    {
        if (awaitingLandingSince < 0d) return;
        maxTraversalAirborneSeconds = Mathf.Max(maxTraversalAirborneSeconds, (float)(now - awaitingLandingSince));
        traversalLandings++;
        awaitingLandingSince = -1d;
    }

    static void TrackIdleStability(TomatoRuntimeTelemetry t)
    {
        // Let the first settled frame establish the expected state after the
        // phase transition; every following frame must be planted Idle.
        if (phase != Phase.Idle || EditorApplication.timeSinceStartup - phaseAt < .12d) return;
        idleSamples++;
        idleStateStable &= t.grounded && !t.locked && t.clip == "Idle";
        Vector2 root = t.root;
        Vector2 visualOffset = (Vector2)t.visual - root;
        Include(ref idleRootMin, ref idleRootMax, root);
        Include(ref idleVisualOffsetMin, ref idleVisualOffsetMax, visualOffset);
        Include(ref idleFootMin, ref idleFootMax, t.foot);
    }

    static void TrackStationaryActionStability(TomatoRuntimeTelemetry t)
    {
        // The standing case is the root-continuity measurement. The repeated
        // case intentionally begins from a physical landing setup, so it is
        // asserted through restart/return state rather than mixing landing
        // settlement into an animation-root metric.
        bool zeroInputShoot = phase == Phase.ShootStanding && t.grounded && t.clip == "Shoot";
        // Only measure the deliberately stationary interruption fixtures.
        // Other phases teleport the tomato to inspect fire triggers or can
        // intentionally cover a long route segment, so folding those roots
        // into one clip metric would report a false visual-root drift.
        bool stationaryInterruption = phase == Phase.CancelSplat || phase == Phase.CancelHazard ||
                                      phase == Phase.Splat || phase == Phase.RegenWait;
        bool lockedAction = stationaryInterruption && t.locked && (t.clip == "Hit" || t.clip == "Regen");
        if (!zeroInputShoot && !lockedAction) return;
        string metricKey = phase + ":" + t.clip;
        Vector2 root = t.root;
        if (!StationaryClipOrigins.TryGetValue(metricKey, out Vector2 origin))
        {
            StationaryClipOrigins[metricKey] = root;
            StationaryClipRootDrift[metricKey] = 0f;
            return;
        }
        StationaryClipRootDrift[metricKey] = Mathf.Max(StationaryClipRootDrift[metricKey], Vector2.Distance(origin, root));
    }

    static void TrackShootTransition(TomatoRuntimeTelemetry t)
    {
        if (t.clipTransitionSerial == observedTransitionSerial) return;
        observedTransitionSerial = t.clipTransitionSerial;
        if (t.clipTransitionFrom != "Shoot" && t.clipTransitionTo != "Shoot") return;
        maxShootTransitionBodyStep = Mathf.Max(maxShootTransitionBodyStep, t.clipTransitionBodyDelta.magnitude);
        maxShootTransitionScaleStep = Mathf.Max(maxShootTransitionScaleStep, Vector2.Distance(t.visualScale, Vector2.one));
        string record = $"{t.clipTransitionFrom}->{t.clipTransitionTo} iou={t.clipTransitionIou:0.###} bodyDelta={F(t.clipTransitionBodyDelta)} scale={F(t.visualScale)} returned={t.clip}";
        ShootTransitionRecords.Add(record);
        WriteReport("[SHOOT-TRANSITION] " + record);
        WriteTrace("[HEADLESS-QA:shoot-transition] " + record);
        bool normalShootPhase = phase == Phase.ShootStanding || phase == Phase.ShootMoving || phase == Phase.ShootRepeat;
        if (normalShootPhase && t.clipTransitionFrom == "Shoot" && t.grounded && t.clipTransitionTo == "Jump") shootTransitionGroundJump = true;
        if (phase == Phase.ShootSameFrameMove && t.clipTransitionFrom == "Idle" && t.clipTransitionTo == "Shoot")
        {
            sameFrameEntryIou = t.clipTransitionIou;
            sameFramePreservedIdleBody = t.frameName.StartsWith("Idle_", StringComparison.Ordinal) && t.clipTransitionIou >= .95f;
            WriteReport($"[SAME-FRAME] Idle->Shoot entry source={t.frameName} iou={t.clipTransitionIou:0.###} preservedIdleBody={sameFramePreservedIdleBody}.");
        }
        if (phase == Phase.ShootRepeat)
            repeatShootStarts = Mathf.Max(repeatShootStarts, t.shootActionStarts - repeatShootStartBaseline);
    }

    static void TrackInterruptionLifecycle(TomatoRuntimeTelemetry t)
    {
        maxTransitionGhostRenderers = Mathf.Max(maxTransitionGhostRenderers, t.transitionGhostEnabled ? 1 : 0);
        if (!t.transitionFadeActive && t.transitionGhostEnabled) ghostLeakObserved = true;
        if (!t.transitionFadeActive && !t.transitionGhostEnabled && t.mainVisualAlpha < .999f)
            settledMainAlphaWrong = true;
        if (phase == Phase.CancelSplat && cancelSplatQueued && !t.locked && t.grounded && t.clip == "Idle" && !t.shootActionActive && !t.transitionGhostEnabled && t.uziActiveTracers == 0 && t.uziActiveCasings == 0)
            cancelSplatRecovered = true;
        if (phase == Phase.CancelHazard && cancelHazardShootQueued && t.clip == "Shoot" && !cancelHazardTriggered)
        {
            tomato.HazardRespawn(tomato.CurrentCheckpoint);
            cancelHazardTriggered = true;
            WriteReport("[ACTION] triggered hazard respawn during live Shoot.");
        }
        if (phase == Phase.CancelHazard && cancelHazardTriggered && !t.locked && t.grounded && t.clip == "Idle" && !t.shootActionActive && !t.transitionGhostEnabled && t.uziActiveTracers == 0 && t.uziActiveCasings == 0)
        {
            if (t.lives != 2 || t.gameOver)
            {
                Fail($"Hazard interruption must consume exactly one of three lives: lives={t.lives}/{t.maxLives} gameOver={t.gameOver}.");
                return;
            }
            cancelHazardLifeVerified = true;
            cancelHazardRecovered = true;
        }
    }

    static void TrackScriptedShootCases(TomatoRuntimeTelemetry t)
    {
        if (phase == Phase.ShootStanding) standingSawShoot |= t.clip == "Shoot";
        if (phase == Phase.ShootStanding && standingShotQueued && t.uziRoundsSpawned - standingUziRoundsBaseline == 10)
        {
            standingUziBurstVerified = true;
            // The standing fixture has already faced right through the real
            // Run phase, so this verifies the emitted tracer direction rather
            // than merely accepting a non-zero direction value.
            standingUziDirectionVerified |= t.uziLastDirection == 1;
        }
        if (phase == Phase.ShootStanding && standingSawShoot && t.grounded && t.clip == "Idle")
        {
            standingReturnedIdle = true;
            standingIdleFramesAfterReturn++;
            StandingIdleFrames.Add(t.frameName);
        }
        if (phase == Phase.ShootMoving)
        {
            movingSawShoot |= t.clip == "Shoot";
            movingReturnedRun |= movingSawShoot && t.grounded && t.clip == "Run";
        }
        if (phase == Phase.ShootRepeat && repeatSecondQueued && t.grounded && t.clip == "Idle")
            repeatedReturnedIdle = true;
        if (phase == Phase.ShootRepeat)
            repeatShootStarts = Mathf.Max(repeatShootStarts, t.shootActionStarts - repeatShootStartBaseline);
    }

    static void TrackClipGeometry(TomatoRuntimeTelemetry t)
    {
        if (!ClipMetrics.TryGetValue(t.clip, out ClipRuntimeMetrics metrics))
        {
            metrics = new ClipRuntimeMetrics();
            ClipMetrics[t.clip] = metrics;
        }
        metrics.samples++;
        metrics.frames.Add(t.frameName);
        metrics.coreDeltaMin = Vector2.Min(metrics.coreDeltaMin, t.coreToColliderCenter);
        metrics.coreDeltaMax = Vector2.Max(metrics.coreDeltaMax, t.coreToColliderCenter);
        metrics.anchorDeltaMin = Vector2.Min(metrics.anchorDeltaMin, t.registrationAnchorToColliderCenter);
        metrics.anchorDeltaMax = Vector2.Max(metrics.anchorDeltaMax, t.registrationAnchorToColliderCenter);
        metrics.scaleMin = Vector2.Min(metrics.scaleMin, t.visualScale);
        metrics.scaleMax = Vector2.Max(metrics.scaleMax, t.visualScale);
        if (metrics.hasPreviousScale)
        {
            Vector2 step = t.visualScale - metrics.previousScale;
            step = new Vector2(Mathf.Abs(step.x), Mathf.Abs(step.y));
            metrics.maxAdjacentScaleStep = Vector2.Max(metrics.maxAdjacentScaleStep, step);
        }
        metrics.previousScale = t.visualScale;
        metrics.hasPreviousScale = true;
        metrics.minCoreContainment = Mathf.Min(metrics.minCoreContainment, t.coreContainment);
        if (t.alphaColliderOverlap < metrics.minAlphaOverlap)
        {
            metrics.minAlphaOverlap = t.alphaColliderOverlap;
            metrics.minAlphaOverlapFrame = t.frameName;
        }
        metrics.minCoreWidthRatio = Mathf.Min(metrics.minCoreWidthRatio, t.coreWidthToColliderWidth);
        metrics.maxCoreWidthRatio = Mathf.Max(metrics.maxCoreWidthRatio, t.coreWidthToColliderWidth);
        metrics.minCoreHeightRatio = Mathf.Min(metrics.minCoreHeightRatio, t.coreHeightToColliderHeight);
        metrics.maxCoreHeightRatio = Mathf.Max(metrics.maxCoreHeightRatio, t.coreHeightToColliderHeight);
        if (t.clip == "Jump")
        {
            metrics.minJumpBootOffset = Mathf.Min(metrics.minJumpBootOffset, t.footToColliderBottom);
            metrics.maxJumpBootOffset = Mathf.Max(metrics.maxJumpBootOffset, t.footToColliderBottom);
        }
        metrics.nativeTexture &= Mathf.RoundToInt(t.texturePixels.x) == 420 && Mathf.RoundToInt(t.texturePixels.y) == 724 &&
                                 Mathf.Abs(t.texturePixels.x / Mathf.Max(1f, t.texturePixels.y) - 420f / 724f) < .0001f;

        if (hasPreviousStateSample && previousStateClip != t.clip)
        {
            maxStateTransitionScaleStep = Mathf.Max(maxStateTransitionScaleStep, Vector2.Distance(t.visualScale, previousStateScale));
            maxStateTransitionAnchorStep = Mathf.Max(maxStateTransitionAnchorStep, Vector2.Distance(t.registrationAnchorToColliderCenter, previousStateAnchorDelta));
        }
        hasPreviousStateSample = true;
        previousStateClip = t.clip;
        previousStateScale = t.visualScale;
        previousStateAnchorDelta = t.registrationAnchorToColliderCenter;
    }

    static void Include(ref Vector2 min, ref Vector2 max, Vector2 point)
    {
        min = Vector2.Min(min, point);
        max = Vector2.Max(max, point);
    }

    static Vector2 Range(Vector2 min, Vector2 max) =>
        float.IsInfinity(min.x) || float.IsInfinity(max.x) ? Vector2.positiveInfinity : max - min;

    static string FormatClipDrift()
    {
        var values = new List<string>();
        foreach (var pair in StationaryClipRootDrift) values.Add($"{pair.Key}:{pair.Value:0.###}");
        return values.Count == 0 ? "none" : string.Join(",", values);
    }

    static string FormatClipGeometry()
    {
        var values = new List<string>();
        foreach (string clip in new[] { "Idle", "Run", "Jump", "Shoot", "Hit", "Regen" })
        {
            if (!ClipMetrics.TryGetValue(clip, out ClipRuntimeMetrics metrics))
            {
                values.Add(clip + ":missing");
                continue;
            }
            string jumpBoot = clip == "Jump" ? $",boot=[{metrics.minJumpBootOffset:0.###},{metrics.maxJumpBootOffset:0.###}]" : string.Empty;
            values.Add($"{clip}[n={metrics.samples},frames={string.Join("/", metrics.frames)},bodyΔ={F(Range(metrics.coreDeltaMin, metrics.coreDeltaMax))},anchorΔ={F(Range(metrics.anchorDeltaMin, metrics.anchorDeltaMax))},contain={metrics.minCoreContainment:0.###},alpha={metrics.minAlphaOverlap:0.###}@{metrics.minAlphaOverlapFrame},ratioW={metrics.minCoreWidthRatio:0.###}-{metrics.maxCoreWidthRatio:0.###},ratioH={metrics.minCoreHeightRatio:0.###}-{metrics.maxCoreHeightRatio:0.###},scale={F(Range(metrics.scaleMin, metrics.scaleMax))},adjScale={F(metrics.maxAdjacentScaleStep)}{jumpBoot}]");
        }
        return string.Join(" ", values);
    }

    static string F(Vector2 value) => $"({value.x:0.###},{value.y:0.###})";

    static bool ShouldTraversalJump(TomatoGame game)
    {
        float x = game.transform.position.x;
        // Source-pixel run-up marks are separately authored against the
        // measured panorama route. They feed normal controller input only:
        // the tomato still takes off, collides, lands and can trigger hazards
        // under real Rigidbody2D simulation.
        return game.GetTelemetry().probeCollider switch
        {
            // Land on the left half of the next counter, rather than launching
            // every hop at maximum distance.  This preserves a real run-up
            // before the following measured gap/flame.
            // The reachable S1-left fire starts at source x238. The tomato
            // This full-route fixture begins at source x90. Its capsule is
            // ~44px wide either side of the root, so launch at x165: the
            // genuine run-up lifts it above the fire before root x194 would
            // make the capsule touch the trigger beginning at x238, and its
            // landing carries past the source x462-515 knife cluster.
            "S1 Door Service Counter" => x >= GameplayArtLayout.PixelToWorldX(0, 165),
            "S1 Stove Worktop" => x >= GameplayArtLayout.PixelToWorldX(0, 645),
            "S1 Prep Island" => x >= GameplayArtLayout.PixelToWorldX(0, 994),
            // The raised painted shelf gives a genuine run-up before the
            // full-height source flame rectangle.
            "S1 Flame Bypass Shelf" => x >= GameplayArtLayout.PixelToWorldX(0, 1320),
            "S1 Right Service Counter" => x >= GameplayArtLayout.PixelToWorldX(0, 1850),
            "S1 Exit Counter" => x >= GameplayArtLayout.PixelToWorldX(0, 2100),
            // Launch from the left half of the knife ledge so landing occurs
            // on the safe left side of the oven counter, leaving a genuine
            // run-up to clear the pizza-oven flame at x887-1009.
            "S2 Knife Gap Ledge" => x >= GameplayArtLayout.PixelToWorldX(1, 430),
            "S2 Oven Counter" => x >= GameplayArtLayout.PixelToWorldX(1, 750),
            // The oven route lands on this raised shelf, whose measured
            // right edge is source x1340.  Start at x1220 (well before that
            // edge and the floor flame starts at x1456) so normal controller
            // physics carries the capsule through the real safe upper route
            // instead of walking off the shelf into the painted flames.
            "S2 Upper Oven Ingredient Shelf" => x >= GameplayArtLayout.PixelToWorldX(1, 1220),
            // Launch before the counter flame (1304-1438). From x1210 the
            // normal jump arc lands on the real upper-right plate drawer,
            // letting the tomato pass above the floor-flame columns too.
            "S2 Burner Counter" => x >= GameplayArtLayout.PixelToWorldX(1, 1210),
            // The painted 7 px lip between these two different-height tops is
            // narrower than the tomato capsule; it is a step-up jump, not a
            // place to let the capsule wedge into two opposing side faces.
            "S2 Burner Exit Ledge" => x >= GameplayArtLayout.PixelToWorldX(1, 1700),
            "S2 Mixer Counter" => x >= GameplayArtLayout.PixelToWorldX(1, 2070),
            "S3 Left Counter" => x >= GameplayArtLayout.PixelToWorldX(2, 280),
            "S3 Freezer Ledge" => x >= GameplayArtLayout.PixelToWorldX(2, 600),
            "S3 Crate Shelf" => x >= GameplayArtLayout.PixelToWorldX(2, 1160),
            "S3 Frozen Bridge" => x >= GameplayArtLayout.PixelToWorldX(2, 1480),
            "S3 Sausage Counter" => x >= GameplayArtLayout.PixelToWorldX(2, 1700),
            _ => false,
        };
    }

    static void RecordTraversalSupport(string platform)
    {
        if (string.IsNullOrEmpty(platform) || platform == "MISS") return;
        TraversalPlatforms.Add(platform);
        if (TraversalSupportSequence.Count == 0 || TraversalSupportSequence[^1] != platform)
            TraversalSupportSequence.Add(platform);
    }
    static void WriteReport(string line) => File.AppendAllText(ReportPath, line + Environment.NewLine);
    static void WriteTrace(string line) => File.AppendAllText(TracePath, line + Environment.NewLine);

    // Kept at the end so the fast editor callback above stays easy to audit.
    static void RuntimeUpdateBridge()
    {
        if (running) TickRuntime();
    }
}
