using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Focused live-Play-Mode proof for the authored Chef and Waiter behaviour.
/// Invoke in batch mode with -executeMethod EnemyRuntimeQa.Run.
/// </summary>
[InitializeOnLoad]
public static class EnemyRuntimeQa
{
    const string PendingKey = "ImmortalTomato.EnemyRuntimeQa.Pending";
    const string ExitKey = "ImmortalTomato.EnemyRuntimeQa.Exit";
    const string ResultKey = "ImmortalTomato.EnemyRuntimeQa.Result";
    const string ScenePath = "Assets/Main.unity";
    const double SampleInterval = .035d;

    enum Phase { WaiterIdle, WaiterCharge, WaiterReset, WaiterReacquire, WaiterOffscreen, WaiterHit, ChefThrow, ChefHit }

    static readonly HashSet<string> waiterIdleSprites = new();
    static readonly HashSet<int> waiterRunFrames = new();
    static readonly HashSet<int> chefThrowFrames = new();
    static readonly HashSet<int> cleaverFrames = new();
    static readonly List<string> exceptions = new();

    static TomatoGame tomato;
    static WaiterEnemy waiter;
    static ChefEnemy chef;
    static Vector3 waiterHome;
    static Phase phase;
    static bool running;
    static double phaseAt;
    static double nextSampleAt;
    static float waiterStartX;
    static float waiterMinimumX;
    static bool waiterCharged;
    static bool waiterRunObserved;
    static float waiterRunMinY;
    static float waiterRunMaxY;
    static Vector2 waiterRunColliderSize;
    static CleaverProjectile cleaver;
    static bool cleaverSpawned;
    static bool cleaverConfiguredLeft;
    static int cleaverSamples;
    static float cleaverLastX;
    static bool cleaverMovesLeft;
    static UziBurstEffects uzi;
    static Camera runtimeCamera;
    static HorizontalCameraFollow cameraFollow;
    static bool cameraFollowWasEnabled;
    static bool waiterShot;
    static bool waiterOffscreenVerified;
    static bool waiterNonlethalVerified;
    static bool waiterFadeObserved;
    static bool waiterBurstCleared;
    static float waiterAlphaAtHit;
    static Vector2 waiterShotMuzzle;
    static double waiterImpactAt = -1d;
    static bool chefShot;
    static bool chefNonlethalVerified;
    static bool chefTargetHidden;
    static double chefTargetReappearedAt = -1d;
    static bool chefFadeObserved;
    static float chefAlphaAtHit;
    static Vector2 chefShotMuzzle;
    static double chefImpactAt = -1d;
    static bool cleaverNeutralized;

    static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    static string ReportPath => Path.Combine(ProjectRoot, "QA", "Runtime", "enemy_runtime_report.log");

    static EnemyRuntimeQa()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("Immortal Tomato/QA/Run Enemy Runtime QA")]
    public static void RunFromMenu() => Start();

    public static void Run() => Start();

    static void Start()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[ENEMY-RUNTIME-QA:FAIL] Exit Play Mode before starting enemy runtime QA.");
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, $"Immortal Tomato enemy runtime QA started {DateTime.UtcNow:O}{Environment.NewLine}");
            SessionState.EraseBool(ExitKey);
            SessionState.SetBool(PendingKey, true);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
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
            int result = SessionState.GetInt(ResultKey, 1);
            SessionState.EraseBool(ExitKey);
            SessionState.EraseBool(PendingKey);
            if (Application.isBatchMode) EditorApplication.Exit(result);
            return;
        }

        if (!SessionState.GetBool(PendingKey, false) || !EditorApplication.isPlaying) return;
        try
        {
            if (!running) BeginRuntime();
            else TickRuntime();
        }
        catch (Exception exception)
        {
            Fail("Runtime assertion failed: " + exception.Message);
        }
    }

    static void BeginRuntime()
    {
        tomato = UnityEngine.Object.FindFirstObjectByType<TomatoGame>();
        waiter = UnityEngine.Object.FindFirstObjectByType<WaiterEnemy>();
        chef = UnityEngine.Object.FindFirstObjectByType<ChefEnemy>();
        if (tomato == null || waiter == null || chef == null)
        {
            Fail("Main Play Mode did not create TomatoGame, WaiterEnemy and ChefEnemy.");
            return;
        }

        exceptions.Clear();
        Application.logMessageReceived += CaptureException;
        try
        {
            VerifyAuthoredEnemy(chef, GameplayArtLayout.ChefScale, "S2 Mixer Counter");
            VerifyAuthoredEnemy(waiter, GameplayArtLayout.WaiterScale, "S2 Burner Counter");
            waiterHome = waiter.transform.position;
            waiterIdleSprites.Clear();
            waiterRunFrames.Clear();
            chefThrowFrames.Clear();
            cleaverFrames.Clear();
            waiterRunObserved = false;
            waiterRunMinY = float.PositiveInfinity;
            waiterRunMaxY = float.NegativeInfinity;
            cleaver = null;
            cleaverSpawned = false;
            cleaverConfiguredLeft = false;
            cleaverSamples = 0;
            cleaverMovesLeft = false;
            uzi = tomato.GetComponentInChildren<UziBurstEffects>();
            Require(uzi != null && uzi.AssetsReady, "Tomato Uzi effects were unavailable for hit QA.");
            runtimeCamera = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            Require(runtimeCamera != null && runtimeCamera.isActiveAndEnabled, "Main Play Mode did not provide an active camera for viewport hit QA.");
            cameraFollow = runtimeCamera.GetComponent<HorizontalCameraFollow>();
            cameraFollowWasEnabled = cameraFollow != null && cameraFollow.enabled;
            if (cameraFollow != null) cameraFollow.enabled = false;
            waiterShot = waiterOffscreenVerified = waiterNonlethalVerified = waiterFadeObserved = waiterBurstCleared = chefShot = chefNonlethalVerified = chefFadeObserved = cleaverNeutralized = chefTargetHidden = false;
            waiterImpactAt = chefImpactAt = -1d;
            chefTargetReappearedAt = -1d;
            running = true;
            SetPhase(Phase.WaiterIdle);
            Write("[SETUP] Main Play Mode loaded; scales, left-facing setup, and support grounding passed.");
        }
        catch (Exception exception)
        {
            Fail("Initial enemy setup assertion failed: " + exception.Message);
        }
    }

    static void VerifyAuthoredEnemy(Component enemy, float expectedScale, string supportName)
    {
        SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
        BoxCollider2D hitbox = enemy.GetComponent<BoxCollider2D>();
        Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
        GameplayArtLayout.EnemyAnchorSpec anchor = default;
        bool foundAnchor = false;
        foreach (GameplayArtLayout.EnemyAnchorSpec candidate in GameplayArtLayout.EnemyAnchors)
            if (candidate.Name == enemy.name) { anchor = candidate; foundAnchor = true; break; }
        if (!foundAnchor || renderer == null || hitbox == null || body == null)
            throw new InvalidOperationException(enemy.name + " is missing authored enemy dependencies.");
        Vector2 expectedRoot = GameplayArtLayout.EnemyRoot(anchor);
        if (Mathf.Abs(enemy.transform.localScale.x - expectedScale) > .001f ||
            Mathf.Abs(enemy.transform.localScale.y - expectedScale) > .001f ||
            renderer.flipX || hitbox.bounds.min.y < expectedRoot.y - .02f ||
            Mathf.Abs(hitbox.bounds.min.y - expectedRoot.y) > .02f ||
            Vector2.Distance(enemy.transform.position, expectedRoot) > .02f ||
            body.bodyType != RigidbodyType2D.Kinematic || body.gravityScale != 0f)
            throw new InvalidOperationException($"{enemy.name} failed scale/facing/grounded setup at {supportName}.");
        Write($"[AUTHORED] {enemy.name} scale={enemy.transform.localScale.x:0.###} facesLeft={!renderer.flipX} grounded={hitbox.bounds.min.y:0.###}/{expectedRoot.y:0.###} support={supportName}.");
    }

    static void SetPhase(Phase next)
    {
        phase = next;
        phaseAt = EditorApplication.timeSinceStartup;
        nextSampleAt = phaseAt;
        Write($"[PHASE] {phase}");
    }

    static void TickRuntime()
    {
        if (exceptions.Count > 0)
        {
            Fail("Unexpected runtime exception: " + string.Join(" | ", exceptions));
            return;
        }
        if (tomato == null)
        {
            Fail("TomatoGame was destroyed during QA.");
            return;
        }
        if (phase <= Phase.WaiterReset && waiter == null) { Fail("Waiter was destroyed before hit QA."); return; }
        if (phase <= Phase.ChefThrow && chef == null) { Fail("Chef was destroyed before hit QA."); return; }

        double now = EditorApplication.timeSinceStartup;
        if (now >= nextSampleAt)
        {
            Sample();
            nextSampleAt = now + SampleInterval;
        }

        double age = now - phaseAt;
        switch (phase)
        {
            case Phase.WaiterIdle:
                if (age >= .38d)
                {
                    Require(waiterIdleSprites.Count >= 2, "Waiter Idle did not advance through two sprite names: " + Names(waiterIdleSprites));
                    Require(IsUpright(waiter), "Waiter Idle root was not upright.");
                    // Detection checks X only. Keeping the inert QA tomato
                    // overhead avoids turning this animation/motion trace
                    // into a collision/respawn test before both Run halves.
                    ParkTomato(waiter.transform.position + Vector3.left * 6.5f + Vector3.up * 10f);
                    waiterStartX = waiter.transform.position.x;
                    waiterMinimumX = waiterStartX;
                    waiterCharged = false;
                    SetPhase(Phase.WaiterCharge);
                }
                break;
            case Phase.WaiterCharge:
                // Batch editor callbacks can outpace Game View frames; leave
                // two complete run cycles for the real Update/MovePosition
                // path rather than judging the first editor-second alone.
                if (age >= 2.2d)
                {
                    Require(waiterCharged && waiterMinimumX < waiterStartX - .05f,
                        $"Waiter did not charge left after tomato detection: start={waiterStartX:0.###} min={waiterMinimumX:0.###} current={waiter.transform.position.x:0.###} charging={waiter.IsCharging} run={Numbers(waiterRunFrames)} tomato={tomato.transform.position} locked={tomato.IsLocked}.");
                    Require(HasBothHalves(waiterRunFrames), "Waiter Run did not advance through both frame halves: " + Numbers(waiterRunFrames));
                    Require(waiterRunObserved && waiterRunMaxY - waiterRunMinY <= .01f,
                        $"Waiter Run root/collider was not stable: rootY=[{waiterRunMinY:0.###},{waiterRunMaxY:0.###}].");
                    ParkTomato(waiterHome + Vector3.right);
                    SetPhase(Phase.WaiterReset);
                }
                break;
            case Phase.WaiterReset:
                if (age >= 2.15d)
                {
                    Require(Vector2.Distance(waiter.transform.position, waiterHome) <= .06f && !waiter.IsCharging,
                        $"Waiter did not reset home before disable: pos={waiter.transform.position} home={waiterHome}.");
                    // The target was absent for the return trip. Reacquiring it
                    // must wait for awareness again instead of charging at once.
                    ParkTomato(waiter.transform.position + Vector3.left * 6.5f + Vector3.up * 10f);
                    SetPhase(Phase.WaiterReacquire);
                }
                break;
            case Phase.WaiterReacquire:
                if (age < Mathf.Max(.05f, waiter.AwarenessSeconds - .12f))
                {
                    Require(!waiter.IsCharging, "Waiter charged before its reacquisition awareness delay elapsed.");
                }
                else if (age >= waiter.AwarenessSeconds + .24f)
                {
                    Require(waiter.IsCharging, "Waiter did not charge after its reacquisition awareness delay.");
                    FireOffscreenTracerAt(waiter.GetComponent<BoxCollider2D>().bounds, "Waiter");
                    SetPhase(Phase.WaiterOffscreen);
                }
                break;
            case Phase.WaiterOffscreen:
                if (age >= .32d)
                {
                    Require(waiter.CurrentHealth == waiter.MaxHealth && !waiter.IsDying,
                        $"An offscreen waiter tracer changed health: {waiter.CurrentHealth}/{waiter.MaxHealth}.");
                    waiterOffscreenVerified = true;
                    waiterShotMuzzle = FireRealTracerAt(waiter.GetComponent<BoxCollider2D>().bounds, "Waiter");
                    waiterAlphaAtHit = waiter.VisualAlpha;
                    SetPhase(Phase.WaiterHit);
                }
                break;
            case Phase.WaiterHit:
                if (!waiterNonlethalVerified && age >= .34d)
                {
                    int expectedHealth = waiter.MaxHealth - uzi.RoundsPerBurst * uzi.RoundDamage;
                    Require(waiter.CurrentHealth == expectedHealth && !waiter.IsDying && Mathf.Abs(waiter.VisualAlpha - waiterAlphaAtHit) < .01f,
                        $"Visible waiter tracers must remove one HP each without fading: health={waiter.CurrentHealth}/{waiter.MaxHealth} damage={uzi.RoundDamage}.");
                    Require(waiter.HealthBar != null && Mathf.Abs(waiter.HealthBar.NormalizedFill - waiter.CurrentHealth / (float)waiter.MaxHealth) < .01f,
                        "Waiter health bar did not track nonlethal Uzi damage.");
                    waiterNonlethalVerified = true;
                    FireVisibleBurstsAt(waiter.GetComponent<BoxCollider2D>().bounds, Mathf.CeilToInt(waiter.CurrentHealth / (float)uzi.RoundsPerBurst), "Waiter");
                    waiterShot = true;
                    Write($"[HEALTH] waiter nonlethal burst dealt {uzi.RoundsPerBurst * uzi.RoundDamage} across {uzi.RoundsPerBurst} visible rounds.");
                }
                if (waiter != null && waiter.IsDying)
                {
                    if (waiterImpactAt < 0d)
                    {
                        waiterImpactAt = now;
                        // The test proves one real tracer reaches this target;
                        // clearing the rest of its dense production burst keeps
                        // the later chef lifecycle independent and deterministic.
                        uzi.CancelAndClear();
                        waiterBurstCleared = true;
                        Write($"[IMPACT] Waiter production tracer consumed; rounds={uzi.TotalRoundsSpawned} active={uzi.ActiveTracerCount}.");
                    }
                    Require(waiter != null && waiterShot && waiter.IsDying && !waiter.IsCharging && !waiter.IsHazardActive &&
                            !waiter.GetComponent<BoxCollider2D>().enabled && !waiter.GetComponent<Rigidbody2D>().simulated,
                        "Waiter bullet impact did not immediately disable AI, collider, and hazard.");
                    if (waiter.VisualAlpha < waiterAlphaAtHit - .03f) waiterFadeObserved = true;
                }
                else if (waiterNonlethalVerified && waiterImpactAt < 0d && age >= 1d)
                {
                    float hitDistance = -1f;
                    bool query = waiter != null && waiter.TryGetBulletHitDistance(waiterShotMuzzle, Vector2.right, 1f, out hitDistance);
                    Fail($"Waiter did not receive a production tracer hit within 1s: rounds={uzi.TotalRoundsSpawned} active={uzi.ActiveTracerCount} directSegment={query}@{hitDistance:0.###}.");
                    return;
                }
                if (waiterImpactAt >= 0d && now - waiterImpactAt >= .72d)
                {
                    Require(waiterFadeObserved && waiterBurstCleared, $"Waiter did not visibly fade after Uzi hit (initial alpha={waiterAlphaAtHit:0.###}).");
                    Require(waiter == null, "Waiter was not destroyed after its fade.");
                    Require(UnityEngine.Object.FindObjectsByType<EnemyHealthBar>(FindObjectsSortMode.None).Length == 1, "Waiter health bar survived death.");
                    ParkTomato(chef.transform.position + Vector3.left * 8.4f + Vector3.up * 10f);
                    SetPhase(Phase.ChefThrow);
                }
                break;
            case Phase.ChefThrow:
                if (!chefTargetHidden && age >= .35d)
                {
                    ParkTomato(chef.transform.position + Vector3.right * 2f);
                    chefTargetHidden = true;
                }
                else if (chefTargetHidden && chefTargetReappearedAt < 0d && age >= .62d)
                {
                    ParkTomato(chef.transform.position + Vector3.left * 8.4f + Vector3.up * 10f);
                    chefTargetReappearedAt = now;
                }
                if (chefTargetReappearedAt > 0d && now - chefTargetReappearedAt < Mathf.Max(.05f, chef.AwarenessSeconds - .12f))
                    Require(!cleaverSpawned && chefThrowFrames.Count == 0, "Chef threw before its reset awareness delay elapsed.");
                if (cleaverSpawned && cleaverSamples >= 3 && !chefShot)
                {
                    Require(HasBothHalves(chefThrowFrames), "Chef Throw did not advance through both frame halves: " + Numbers(chefThrowFrames));
                    Require(cleaverSpawned && cleaverSamples >= 3 && cleaverMovesLeft && cleaverConfiguredLeft && cleaverFrames.Count >= 2,
                        $"Chef cleaver did not move left across multiple frames: spawned={cleaverSpawned} samples={cleaverSamples} frames={Numbers(cleaverFrames)} movesLeft={cleaverMovesLeft}.");
                    Require(cleaver != null && cleaver.GetComponent<Collider2D>().enabled, "Chef did not have a live cleaver threat before hit QA.");
                    chefShotMuzzle = FireRealTracerAt(chef.GetComponent<BoxCollider2D>().bounds, "Chef");
                    chefAlphaAtHit = chef.VisualAlpha;
                    SetPhase(Phase.ChefHit);
                }
                else if (age >= 5d) Fail("Chef did not reach a live-cleaver state for bullet hit QA.");
                break;
            case Phase.ChefHit:
                if (!chefNonlethalVerified && age >= .34d)
                {
                    int expectedHealth = chef.MaxHealth - uzi.RoundsPerBurst * uzi.RoundDamage;
                    Require(chef.CurrentHealth == expectedHealth && !chef.IsDying && Mathf.Abs(chef.VisualAlpha - chefAlphaAtHit) < .01f,
                        $"Visible chef tracers must remove one HP each without fading: health={chef.CurrentHealth}/{chef.MaxHealth} damage={uzi.RoundDamage}.");
                    Require(chef.HealthBar != null && Mathf.Abs(chef.HealthBar.NormalizedFill - chef.CurrentHealth / (float)chef.MaxHealth) < .01f,
                        "Chef health bar did not track nonlethal Uzi damage.");
                    chefNonlethalVerified = true;
                    FireVisibleBurstsAt(chef.GetComponent<BoxCollider2D>().bounds, Mathf.CeilToInt(chef.CurrentHealth / (float)uzi.RoundsPerBurst), "Chef");
                    chefShot = true;
                    Write($"[HEALTH] chef nonlethal burst dealt {uzi.RoundsPerBurst * uzi.RoundDamage} across {uzi.RoundsPerBurst} visible rounds.");
                }
                if (chef != null && chef.IsDying)
                {
                    if (chefImpactAt < 0d) { chefImpactAt = now; Write($"[IMPACT] Chef production tracer consumed; rounds={uzi.TotalRoundsSpawned} active={uzi.ActiveTracerCount}."); }
                    Require(chef != null && chefShot && chef.IsDying && !chef.IsHazardActive &&
                            !chef.GetComponent<BoxCollider2D>().enabled && !chef.GetComponent<Rigidbody2D>().simulated,
                        "Chef bullet impact did not immediately disable AI, collider, and hazard.");
                    if (chef.VisualAlpha < chefAlphaAtHit - .03f) chefFadeObserved = true;
                    cleaverNeutralized |= !HasLiveChefCleaver();
                }
                else if (chefNonlethalVerified && chefImpactAt < 0d && age >= 1d)
                {
                    float hitDistance = -1f;
                    bool query = chef != null && chef.TryGetBulletHitDistance(chefShotMuzzle, Vector2.right, 1f, out hitDistance);
                    Fail($"Chef did not receive a production tracer hit within 1s: rounds={uzi.TotalRoundsSpawned} active={uzi.ActiveTracerCount} directSegment={query}@{hitDistance:0.###}.");
                    return;
                }
                if (chefImpactAt >= 0d && now - chefImpactAt >= .72d)
                {
                    Require(chefFadeObserved, $"Chef did not visibly fade after Uzi hit (initial alpha={chefAlphaAtHit:0.###}).");
                    Require(cleaverNeutralized && !HasLiveChefCleaver(), "Chef death left a live cleaver threat.");
                    Require(chef == null, "Chef was not destroyed after its fade.");
                    Require(UnityEngine.Object.FindObjectsByType<EnemyHealthBar>(FindObjectsSortMode.None).Length == 0, "Enemy health bars survived final enemy removal.");
                    Pass();
                }
                break;
        }
    }

    static void Sample()
    {
        if (phase == Phase.WaiterIdle)
        {
            string name = TextureName(waiter.GetComponent<SpriteRenderer>());
            Require(name.StartsWith("Idle_", StringComparison.Ordinal), "Waiter Idle used unexpected sprite " + name + ".");
            waiterIdleSprites.Add(name);
            Require(IsUpright(waiter), "Waiter Idle became non-upright.");
        }
        else if (phase == Phase.WaiterCharge)
        {
            waiterCharged |= waiter.IsCharging;
            waiterMinimumX = Mathf.Min(waiterMinimumX, waiter.transform.position.x);
            string name = TextureName(waiter.GetComponent<SpriteRenderer>());
            // Charging is assigned at the end of WaiterEnemy's Idle branch;
            // its first Charging visual is applied in the following Update.
            // Accept that single state/visual boundary, but no Idle sample
            // after the run clip has begun.
            if (waiterRunFrames.Count == 0 && name.StartsWith("Idle_", StringComparison.Ordinal)) return;
            int frame = FrameNumber(name, "Run_");
            Require(frame > 0, "Waiter charge used non-Run sprite " + name + ".");
            waiterRunFrames.Add(frame);
            BoxCollider2D hitbox = waiter.GetComponent<BoxCollider2D>();
            if (!waiterRunObserved)
            {
                waiterRunObserved = true;
                waiterRunColliderSize = hitbox.bounds.size;
            }
            else
                Require(Vector2.Distance(hitbox.bounds.size, waiterRunColliderSize) <= .001f,
                    "Waiter Run collider size changed between frames.");
            waiterRunMinY = Mathf.Min(waiterRunMinY, waiter.transform.position.y);
            waiterRunMaxY = Mathf.Max(waiterRunMaxY, waiter.transform.position.y);
            Require(IsUpright(waiter), "Waiter Run became non-upright.");
        }
        else if (phase == Phase.ChefThrow)
        {
            string name = TextureName(chef.GetComponent<SpriteRenderer>());
            int frame = FrameNumber(name, "Throw_");
            if (frame > 0) chefThrowFrames.Add(frame);
            CleaverProjectile current = UnityEngine.Object.FindFirstObjectByType<CleaverProjectile>();
            if (current != null)
            {
                int cleaverFrame = FrameNumber(TextureName(current.GetComponent<SpriteRenderer>()), "Cleaver_");
                if (cleaverFrame > 0) cleaverFrames.Add(cleaverFrame);
                if (cleaver != current)
                {
                    cleaver = current;
                    cleaverSpawned = true;
                    cleaverConfiguredLeft = current.MovesLeft;
                    cleaverLastX = current.transform.position.x;
                    Write($"[CLEAVER] spawned x={cleaverLastX:0.###} velocity={current.Velocity}.");
                }
                else
                {
                    cleaverSamples++;
                    cleaverMovesLeft |= current.transform.position.x < cleaverLastX - .01f;
                    cleaverLastX = current.transform.position.x;
                }
            }
        }
    }

    static void ParkTomato(Vector3 position)
    {
        tomato.ClearHeadlessQaInput();
        tomato.Body.simulated = false;
        tomato.Body.position = position;
        tomato.transform.position = position;
        tomato.Body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        Write($"[ACTION] parked tomato at ({position.x:0.###},{position.y:0.###}) for {phase}.");
    }

    static Vector2 FireRealTracerAt(Bounds target, string targetName)
    {
        // This calls the production Uzi effect. Its normal Update advances the
        // spawned tracer and performs the same swept collision used in play.
        // Start outside the collider: all three real tracers must travel into
        // the body rather than receive a synthetic direct health call.
        Vector2 muzzle = new(target.min.x - .25f, target.center.y);
        FocusCameraAt(target.center);
        uzi.FireMuzzleBurst(muzzle, 1, false, false);
        Write($"[ACTION] fired visible production Uzi burst at {targetName} from ({muzzle.x:0.###},{muzzle.y:0.###}).");
        return muzzle;
    }

    static void FireVisibleBurstsAt(Bounds target, int burstCount, string targetName)
    {
        Vector2 muzzle = new(target.min.x - .25f, target.center.y);
        FocusCameraAt(target.center);
        for (int i = 0; i < burstCount; i++) uzi.FireMuzzleBurst(muzzle, 1, false, false);
        Write($"[ACTION] fired {burstCount} visible production Uzi bursts at {targetName}.");
    }

    static void FireOffscreenTracerAt(Bounds target, string targetName)
    {
        // The spawned visual begins on screen, but the collider entry point is
        // just beyond the right viewport edge. That proves a swept tracer does
        // not deal damage after it has left the player's view.
        Vector2 muzzle = new(target.min.x - .25f, target.center.y);
        float rightEdge = target.min.x - .01f;
        float halfWidth = runtimeCamera.orthographicSize * runtimeCamera.aspect;
        Vector3 position = runtimeCamera.transform.position;
        runtimeCamera.transform.position = new Vector3(rightEdge - halfWidth, target.center.y, position.z);
        uzi.FireMuzzleBurst(muzzle, 1, false, false);
        Write($"[ACTION] fired viewport-edge production Uzi burst toward offscreen {targetName} from ({muzzle.x:0.###},{muzzle.y:0.###}).");
    }

    static void FocusCameraAt(Vector2 target)
    {
        Vector3 position = runtimeCamera.transform.position;
        runtimeCamera.transform.position = new Vector3(target.x, target.y, position.z);
    }

    static bool HasLiveChefCleaver()
    {
        if (chef == null) return false;
        foreach (CleaverProjectile projectile in UnityEngine.Object.FindObjectsByType<CleaverProjectile>(FindObjectsSortMode.None))
            if (projectile.IsOwnedBy(chef.gameObject) && projectile.GetComponent<Collider2D>().enabled) return true;
        return false;
    }

    static bool IsUpright(Component enemy) =>
        Mathf.Abs(enemy.transform.rotation.eulerAngles.z) < .01f || Mathf.Abs(enemy.transform.rotation.eulerAngles.z - 360f) < .01f;

    static string TextureName(SpriteRenderer renderer) => renderer != null && renderer.sprite != null && renderer.sprite.texture != null
        ? renderer.sprite.texture.name : "NONE";

    static int FrameNumber(string name, string prefix)
    {
        if (string.IsNullOrEmpty(name) || !name.StartsWith(prefix, StringComparison.Ordinal)) return 0;
        return int.TryParse(name.Substring(prefix.Length), out int frame) ? frame : 0;
    }

    static bool HasBothHalves(HashSet<int> frames)
    {
        bool firstHalf = false, secondHalf = false;
        foreach (int frame in frames)
        {
            firstHalf |= frame is >= 1 and <= 4;
            secondHalf |= frame is >= 5 and <= 8;
        }
        return firstHalf && secondHalf;
    }

    static string Names(HashSet<string> names) => string.Join(",", names);
    static string Numbers(HashSet<int> frames) => string.Join(",", frames);

    static void CaptureException(string condition, string stackTrace, LogType type)
    {
        if ((type is LogType.Error or LogType.Exception or LogType.Assert) &&
            (stackTrace.Contains("Assets/Scripts/", StringComparison.Ordinal) ||
             stackTrace.Contains("Assets\\\\Scripts\\\\", StringComparison.Ordinal)))
            exceptions.Add(condition);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static void Pass()
    {
        Write($"[RESULT] waiterIdle={Names(waiterIdleSprites)} waiterRun={Numbers(waiterRunFrames)} waiterRunRootY=[{waiterRunMinY:0.###},{waiterRunMaxY:0.###}] waiterRunCollider={waiterRunColliderSize} chefThrow={Numbers(chefThrowFrames)} cleaverFrames={Numbers(cleaverFrames)} cleaverSamples={cleaverSamples} realUziWaiterFade={waiterFadeObserved} realUziChefFade={chefFadeObserved} cleaverNeutralized={cleaverNeutralized} exceptions={exceptions.Count}.");
        Write("[PASS] Main Play Mode enemy runtime QA passed.");
        End(0);
    }

    static void Fail(string message)
    {
        Debug.LogError("[ENEMY-RUNTIME-QA:FAIL] " + message);
        try { Write("[FAIL] " + message); } catch { }
        End(1);
    }

    static void End(int result)
    {
        if (!running && !SessionState.GetBool(PendingKey, false)) return;
        running = false;
        Application.logMessageReceived -= CaptureException;
        if (cameraFollow != null) cameraFollow.enabled = cameraFollowWasEnabled;
        SessionState.SetInt(ResultKey, result);
        SessionState.SetBool(ExitKey, true);
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
    }

    static void Write(string line) => File.AppendAllText(ReportPath, line + Environment.NewLine);
}
