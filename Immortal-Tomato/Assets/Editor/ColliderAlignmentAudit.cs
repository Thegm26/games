using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Scene-side proof that physics follows the hand-measured kitchen artwork.
/// The expected references come from GameplayArtLayout source pixels, so this
/// audit cannot pass merely because a collider was copied into its own oracle.
/// </summary>
public static class ColliderAlignmentAudit
{
    const float AlignmentTolerance = .025f; // 1.61 source px at 1.55/100 scale.
    const float TomatoCapsuleHalfWidth = 2.55f * .54f * .5f;
    const float TomatoJumpSpeed = 12f;
    const float TomatoGravity = 9.81f * 2.4f;
    const float TomatoRunSpeed = 5.5f;

    static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    static string ReportPath => Path.Combine(ProjectRoot, "QA", "Runtime", "collider_alignment_report.log");

    readonly struct LegacyRow
    {
        public readonly string Name;
        public readonly Bounds Bounds;
        public readonly string IntendedReference;
        public LegacyRow(string name, Vector2 center, Vector2 size, string intendedReference)
        {
            Name = name;
            Bounds = new Bounds(center, size);
            IntendedReference = intendedReference;
        }
    }

    // Exact pre-audit values from the former PrototypeBootstrap code.  Kept in
    // the report so the displacement from the guessed route is reviewable.
    static readonly LegacyRow[] LegacyRoute =
    {
        new("S1 Prep Counter", new(-40.666f, -.82f), new(2.3f, .15f), "S1 Stove Worktop"),
        new("S1 Central Counter", new(-35.766f, -1f), new(4.5f, .15f), "S1 Prep Island"),
        new("S1 Mid Service Counter", new(-29.5f, -1.36f), new(5f, .15f), "S1 Flame Bypass Shelf"),
        new("S1 Right Service Counter", new(-21.1f, -.92f), new(8.4f, .15f), "S1 Right Service Counter"),
        new("S2 Left Ledge", new(-12.7f, -1.09f), new(2.2f, .15f), "S2 Left Stove Counter"),
        new("S2 Left Stove Counter", new(-9.5f, -1.36f), new(2.5f, .15f), "S2 Knife Gap Ledge"),
        new("S2 Oven Ledge", new(-2.8f, -.92f), new(3.6f, .15f), "S2 Oven Counter"),
        new("S2 Center Stove Counter", new(2.6f, -.92f), new(5.4f, .15f), "S2 Burner Counter"),
        new("S2 Mixer Counter", new(13f, -.92f), new(7.4f, .15f), "S2 Mixer Counter"),
        new("S3 Entry Counter", new(19.25f, -1.2f), new(4.7f, .15f), "S3 Entry Counter"),
        new("S3 Cold Ledge", new(25.966f, -1.32f), new(3.5f, .15f), "S3 Freezer Ledge"),
        new("S3 Freezer Ledge", new(31.266f, -1.09f), new(2.7f, .15f), "S3 Produce Counter"),
        new("S3 Freezer Bridge", new(35.5f, -1.1f), new(4.7f, .15f), "S3 Crate Shelf"),
        new("S3 Icy Counter", new(40.866f, -.89f), new(2.8f, .15f), "S3 Sausage Counter"),
        new("S3 Exit Counter", new(47f, -.9f), new(6.5f, .15f), "S3 Exit Counter"),
    };

    public static void WriteAndValidate()
    {
        // This entry point is also called directly from batchmode. Ensure it
        // always examines the authored gameplay scene rather than Unity's
        // otherwise-empty default scene.
        EditorSceneManager.OpenScene("Assets/Main.unity", OpenSceneMode.Single);
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        var rows = new List<string>
        {
            "Immortal Tomato collider/source-art alignment audit",
            "Source mapping: worldX = sectionCenter + (sourceX - 1086) * 0.0155; worldY = (362 - sourceY) * 0.0155.",
            "Background: 2172x724 px, 100 PPU, transform scale 1.55. Tolerance: 0.025 world (1.61 source px).",
            "Expected references below are manually measured source-art pixels from GameplayArtLayout, independent of scene collider values.",
            string.Empty,
            "PRE-AUDIT ROUTE (legacy guessed colliders -> measured intended reference)",
            "object | old L,R,B,T | intended painted top | horizontal centre delta | vertical top delta"
        };
        var errors = new List<string>();

        foreach (LegacyRow legacy in LegacyRoute)
        {
            GameplayArtLayout.SurfaceSpec target = GameplayArtLayout.Surface(legacy.IntendedReference);
            float horizontal = legacy.Bounds.center.x - (target.LeftWorld + target.RightWorld) * .5f;
            float vertical = legacy.Bounds.max.y - target.TopWorld;
            rows.Add($"{legacy.Name} | {BoundsText(legacy.Bounds)} | {target.PaintedReference} px[{target.LeftPixel:0}-{target.RightPixel:0}] topY={target.TopPixel:0} | dx={horizontal:+0.###;-0.###;0} | dTop={vertical:+0.###;-0.###;0}");
        }

        rows.Add(string.Empty);
        rows.Add("POST-AUDIT ROUTE (serialized collider -> independently measured painted top)");
        rows.Add("object | collider L,R,B,T | painted source reference | horizontal L/R delta | vertical top delta | result");
        foreach (GameplayArtLayout.SurfaceSpec spec in GameplayArtLayout.RouteSurfaces)
        {
            Collider2D collider = RequireCollider(spec.Name, errors);
            Bounds expected = new(spec.CenterWorld, new Vector3(spec.ColliderSize.x, spec.ColliderSize.y, 0f));
            Bounds actual = collider != null ? collider.bounds : default;
            float leftDelta = actual.min.x - expected.min.x;
            float rightDelta = actual.max.x - expected.max.x;
            float topDelta = actual.max.y - spec.TopWorld;
            bool pass = collider is BoxCollider2D && !collider.isTrigger && !collider.usedByEffector && collider.GetComponent<PlatformEffector2D>() == null &&
                        Mathf.Abs(leftDelta) <= AlignmentTolerance && Mathf.Abs(rightDelta) <= AlignmentTolerance && Mathf.Abs(topDelta) <= AlignmentTolerance;
            if (!pass) errors.Add($"route collider mismatch: {spec.Name}");
            rows.Add($"{spec.Name} | {BoundsText(actual)} | {spec.PaintedReference} px[{spec.LeftPixel:0}-{spec.RightPixel:0}] topY={spec.TopPixel:0} | dx=({leftDelta:+0.###;-0.###;0},{rightDelta:+0.###;-0.###;0}) | dTop={topDelta:+0.###;-0.###;0} | {(pass ? "PASS" : "FAIL")}");
        }

        rows.Add(string.Empty);
        rows.Add("UPPER DRAWER / SHELF SUPPORTS (serialized collider -> measured painted shelf top)");
        rows.Add("object | collider L,R,B,T | painted source reference | horizontal L/R delta | vertical top delta | result");
        foreach (GameplayArtLayout.SurfaceSpec spec in GameplayArtLayout.UpperShelfSurfaces)
        {
            Collider2D collider = RequireCollider(spec.Name, errors);
            Bounds expected = new(spec.CenterWorld, new Vector3(spec.ColliderSize.x, spec.ColliderSize.y, 0f));
            Bounds actual = collider != null ? collider.bounds : default;
            float leftDelta = actual.min.x - expected.min.x;
            float rightDelta = actual.max.x - expected.max.x;
            float topDelta = actual.max.y - spec.TopWorld;
            PlatformEffector2D effector = collider != null ? collider.GetComponent<PlatformEffector2D>() : null;
            bool pass = collider is BoxCollider2D && !collider.isTrigger && collider.usedByEffector && effector != null && effector.useOneWay &&
                        Mathf.Abs(effector.surfaceArc - 90f) <= .01f &&
                        Mathf.Abs(leftDelta) <= AlignmentTolerance && Mathf.Abs(rightDelta) <= AlignmentTolerance && Mathf.Abs(topDelta) <= AlignmentTolerance;
            if (!pass) errors.Add($"upper drawer/shelf collider mismatch: {spec.Name}");
            rows.Add($"{spec.Name} | {BoundsText(actual)} | {spec.PaintedReference} px[{spec.LeftPixel:0}-{spec.RightPixel:0}] topY={spec.TopPixel:0} | dx=({leftDelta:+0.###;-0.###;0},{rightDelta:+0.###;-0.###;0}) | dTop={topDelta:+0.###;-0.###;0} | {(pass ? "PASS" : "FAIL")}");
        }

        rows.Add(string.Empty);
        rows.Add("HAZARD TRIGGERS (serialized trigger -> blade/flame/spike source rectangle)");
        rows.Add("object | collider L,R,B,T | painted source rectangle | L/R/T/B delta | result");
        int hazardLayer = LayerMask.NameToLayer("Hazard");
        foreach (GameplayArtLayout.TriggerSpec spec in GameplayArtLayout.Hazards)
        {
            Collider2D collider = RequireCollider(spec.Name, errors);
            Bounds expected = spec.ExpectedBounds;
            Bounds actual = collider != null ? collider.bounds : default;
            bool pass = collider is BoxCollider2D && collider.isTrigger && collider.gameObject.layer == hazardLayer && BoundsClose(actual, expected);
            if (!pass) errors.Add($"hazard trigger mismatch: {spec.Name}");
            rows.Add($"{spec.Name} | {BoundsText(actual)} | {spec.PaintedReference} px[{spec.LeftPixel:0}-{spec.RightPixel:0},{spec.TopPixel:0}-{spec.BottomPixel:0}] | d=({actual.min.x - expected.min.x:+0.###;-0.###;0},{actual.max.x - expected.max.x:+0.###;-0.###;0},{actual.max.y - expected.max.y:+0.###;-0.###;0},{actual.min.y - expected.min.y:+0.###;-0.###;0}) | {(pass ? "PASS" : "FAIL")}");
        }

        GameplayArtLayout.WorldTriggerSpec fallSpec = GameplayArtLayout.FallHazard;
        Collider2D fallCollider = RequireCollider(fallSpec.Name, errors);
        Bounds fallActual = fallCollider != null ? fallCollider.bounds : default;
        const float FallEdgeMargin = 15f;
        bool fallPass = fallCollider is BoxCollider2D && fallCollider.isTrigger && fallCollider.gameObject.layer == hazardLayer &&
                        BoundsClose(fallActual, fallSpec.ExpectedBounds) &&
                        fallActual.min.x <= GameplayArtLayout.PixelToWorldX(0, 0f) - FallEdgeMargin &&
                        fallActual.max.x >= GameplayArtLayout.PixelToWorldX(2, GameplayArtLayout.NativeWidthPixels) + FallEdgeMargin &&
                        fallActual.max.y < GameplayArtLayout.PixelToWorldY(GameplayArtLayout.NativeHeightPixels);
        if (!fallPass) errors.Add("out-of-map fall trigger mismatch");
        rows.Add($"{fallSpec.Name} | {BoundsText(fallActual)} | {fallSpec.Reference}; top below art bottom y={GameplayArtLayout.PixelToWorldY(GameplayArtLayout.NativeHeightPixels):0.###} | {(fallPass ? "PASS" : "FAIL")}");

        rows.Add(string.Empty);
        AuditLeftWorldBoundary(rows, errors);
        AuditAnimatedFires(rows, errors, hazardLayer);

        rows.Add(string.Empty);
        rows.Add("CHECKPOINT TRIGGERS (support is an independently measured painted route top)");
        rows.Add("object | trigger L,R,B,T | support/reference | stored respawn root | support delta | result");
        foreach (GameplayArtLayout.CheckpointSpec spec in GameplayArtLayout.Checkpoints)
        {
            Collider2D collider = RequireCollider(spec.Name, errors);
            Vector2 expectedCenter = GameplayArtLayout.CheckpointCenter(spec);
            Vector2 expectedRespawn = GameplayArtLayout.SupportPoint(spec.SupportName, spec.SourcePixelX);
            Vector2 actualRespawn = collider != null ? (Vector2)collider.transform.position + Vector2.up * GameplayArtLayout.CheckpointRespawnOffsetY : Vector2.zero;
            float delta = Vector2.Distance(actualRespawn, expectedRespawn);
            bool pass = collider is BoxCollider2D && collider.isTrigger && Mathf.Abs(collider.bounds.center.x - expectedCenter.x) <= AlignmentTolerance && delta <= AlignmentTolerance;
            if (!pass) errors.Add($"checkpoint mismatch: {spec.Name}");
            rows.Add($"{spec.Name} | {BoundsText(collider != null ? collider.bounds : default)} | {spec.PaintedReference}; {spec.SupportName} pxX={spec.SourcePixelX:0} | {VectorText(actualRespawn)} | delta={delta:0.###} | {(pass ? "PASS" : "FAIL")}");
        }

        rows.Add(string.Empty);
        AuditTomatoAndEnemies(rows, errors);
        AuditInitialCamera(rows, errors);
        AuditS2LipTransitions(rows, errors);
        AuditRouteReachability(rows, errors);
        AuditEverySerializedCollider(rows, errors);

        rows.Add(string.Empty);
        rows.Add(errors.Count == 0 ? "[PASS] Every serialized gameplay collider is source-art aligned and the door spawn has measured support." : "[FAIL] " + string.Join("; ", errors));
        File.WriteAllLines(ReportPath, rows);
        if (errors.Count > 0) throw new InvalidOperationException("Collider alignment audit failed. See " + ReportPath + ": " + string.Join("; ", errors));
        Debug.Log("Collider alignment audit passed: " + ReportPath);
    }

    static void AuditTomatoAndEnemies(List<string> rows, List<string> errors)
    {
        rows.Add("TOMATO / ENEMY HITBOXES");
        TomatoGame tomato = UnityEngine.Object.FindFirstObjectByType<TomatoGame>();
        CapsuleCollider2D capsule = tomato != null ? tomato.GetComponent<CapsuleCollider2D>() : null;
        Vector2 expectedDoorSpawn = GameplayArtLayout.DoorSpawn;
        Bounds capsuleBounds = capsule != null ? capsule.bounds : default;
        Collider2D support = FindSupportingRoute(capsuleBounds.min.x + TomatoCapsuleHalfWidth, capsuleBounds.min.y);
        float spawnDelta = tomato != null ? Vector2.Distance(tomato.transform.position, expectedDoorSpawn) : float.PositiveInfinity;
        float supportDelta = support != null ? capsuleBounds.min.y - support.bounds.max.y : float.PositiveInfinity;
        bool tomatoPass = tomato != null && capsule != null && capsule.direction == CapsuleDirection2D.Vertical &&
                          spawnDelta <= AlignmentTolerance && support != null && support.name == GameplayArtLayout.DoorSupportName && Mathf.Abs(supportDelta) <= AlignmentTolerance;
        if (!tomatoPass) errors.Add("tomato is not grounded at measured green-door spawn");
        rows.Add($"Immortal Tomato capsule | {BoundsText(capsuleBounds)} | green door jamb px143; spawn px{GameplayArtLayout.DoorSpawnPixelX:0}; {GameplayArtLayout.DoorSupportName} top | root={VectorText(tomato != null ? tomato.transform.position : Vector3.zero)} support={(support != null ? support.name : "MISS")} supportDelta={supportDelta:0.###} spawnDelta={spawnDelta:0.###} | {(tomatoPass ? "PASS" : "FAIL")}");

        foreach (GameplayArtLayout.EnemyAnchorSpec spec in GameplayArtLayout.EnemyAnchors)
        {
            GameObject enemy = GameObject.Find(spec.Name);
            BoxCollider2D hitbox = enemy != null ? enemy.GetComponent<BoxCollider2D>() : null;
            Vector2 expectedRoot = GameplayArtLayout.EnemyRoot(spec);
            float expectedScale = spec.Name == "Chef Enemy" ? GameplayArtLayout.ChefScale : GameplayArtLayout.WaiterScale;
            float rootDelta = enemy != null ? Vector2.Distance(enemy.transform.position, expectedRoot) : float.PositiveInfinity;
            float footDelta = hitbox != null ? hitbox.bounds.min.y - GameplayArtLayout.Surface(spec.SupportName).TopWorld : float.PositiveInfinity;
            float scaleDelta = enemy != null ? Mathf.Abs(enemy.transform.localScale.x - expectedScale) : float.PositiveInfinity;
            bool pass = enemy != null && hitbox != null && hitbox.isTrigger && rootDelta <= AlignmentTolerance && Mathf.Abs(footDelta) <= AlignmentTolerance && scaleDelta <= AlignmentTolerance;
            if (!pass) errors.Add("enemy collider mismatch: " + spec.Name);
            rows.Add($"{spec.Name} hitbox | {BoundsText(hitbox != null ? hitbox.bounds : default)} | {spec.PaintedReference}; {spec.SupportName} pxX={spec.SourcePixelX:0} | root={VectorText(enemy != null ? enemy.transform.position : Vector3.zero)} scale={expectedScale:0.##} scaleDelta={scaleDelta:0.###} rootDelta={rootDelta:0.###} footDelta={footDelta:0.###} | {(pass ? "PASS" : "FAIL")}");
        }
        rows.Add("Chef Cleaver Projectile | runtime-created, not serialized | trigger local size=(4.5,1.35), visual scale=.45 => world size=(2.025,0.608); spawned from chef throw hand | audited by ChefEnemy/CleaverProjectile definition");
    }

    static void AuditRouteReachability(List<string> rows, List<string> errors)
    {
        rows.Add(string.Empty);
        rows.Add("ROUTE CONTINUITY / JUMP REACHABILITY");
        rows.Add("from -> to | gap | rise | conservative reachable horizontal distance | result");
        IReadOnlyList<GameplayArtLayout.SurfaceSpec> route = GameplayArtLayout.RouteSurfaces;
        for (int i = 0; i < route.Count - 1; i++)
        {
            GameplayArtLayout.SurfaceSpec from = route[i];
            GameplayArtLayout.SurfaceSpec to = route[i + 1];
            float gap = Mathf.Max(0f, to.LeftWorld - from.RightWorld);
            float rise = to.TopWorld - from.TopWorld;
            float discriminant = TomatoJumpSpeed * TomatoJumpSpeed - 2f * TomatoGravity * Mathf.Max(0f, rise);
            float airTime = discriminant >= 0f ? (TomatoJumpSpeed + Mathf.Sqrt(discriminant)) / TomatoGravity : 0f;
            float reach = TomatoRunSpeed * airTime + TomatoCapsuleHalfWidth * 2f;
            bool pass = rise <= TomatoJumpSpeed * TomatoJumpSpeed / (2f * TomatoGravity) - .12f && gap <= reach - .15f;
            if (!pass) errors.Add($"route transition is unreachable: {from.Name} -> {to.Name}");
            rows.Add($"{from.Name} -> {to.Name} | {gap:0.###} | {rise:+0.###;-0.###;0} | {reach:0.###} | {(pass ? "PASS" : "FAIL")}");
        }
    }

    static void AuditS2LipTransitions(List<string> rows, List<string> errors)
    {
        rows.Add(string.Empty);
        rows.Add("S2 PAINTED LIP / STEP CHECKS (no hidden bridge or floor)");
        rows.Add("transition | source-pixel seam | world seam | top delta | result");

        // These values use source-art endpoints, not collider values.  The
        // small downward left-stove -> knife-ledge seam is continuous in the
        // original pixels (383 then 384); a larger collider gap would make
        // the capsule fall through a visible counter lip.  The latter lip is
        // an intentional upward step before the mixer and must remain a real
        // jump rather than being filled by an invisible platform.
        AuditLip("S2 Left Stove Counter", "S2 Knife Gap Ledge", false, rows, errors);
        AuditLip("S2 Burner Exit Ledge", "S2 Mixer Counter", true, rows, errors);
    }

    static void AuditLip(string fromName, string toName, bool requiresJump, List<string> rows, List<string> errors)
    {
        GameplayArtLayout.SurfaceSpec from = GameplayArtLayout.Surface(fromName);
        GameplayArtLayout.SurfaceSpec to = GameplayArtLayout.Surface(toName);
        Collider2D fromCollider = RequireCollider(fromName, errors);
        Collider2D toCollider = RequireCollider(toName, errors);
        float sourceSeam = to.LeftPixel - from.RightPixel;
        float measuredWorldSeam = to.LeftWorld - from.RightWorld;
        float actualWorldSeam = toCollider.bounds.min.x - fromCollider.bounds.max.x;
        float topDelta = toCollider.bounds.max.y - fromCollider.bounds.max.y;
        bool adjacentPaint = sourceSeam >= 0f && sourceSeam <= 7f;
        bool actualMatchesPaint = Mathf.Abs(actualWorldSeam - measuredWorldSeam) <= AlignmentTolerance;
        bool shapeMatches = requiresJump ? topDelta > .1f : topDelta < -.1f;
        bool pass = adjacentPaint && actualMatchesPaint && shapeMatches;
        if (!pass) errors.Add($"S2 measured lip mismatch: {fromName} -> {toName}");
        string kind = requiresJump ? "intentional upward jump lip" : "continuous downward counter lip";
        rows.Add($"{fromName} -> {toName} ({kind}) | px={sourceSeam:0} | art={measuredWorldSeam:0.###}; collider={actualWorldSeam:0.###} | {topDelta:+0.###;-0.###;0} | {(pass ? "PASS" : "FAIL")}");
    }

    static void AuditInitialCamera(List<string> rows, List<string> errors)
    {
        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        TomatoGame tomato = UnityEngine.Object.FindFirstObjectByType<TomatoGame>();
        if (camera == null || tomato == null)
        {
            errors.Add("camera/tomato missing for door-spawn visibility audit");
            return;
        }
        // Unity batchmode can retain a 1:1 render target.  This audit concerns
        // the authored 16:9 game camera, whose dimensions are declared in the
        // source-art layout rather than inferred from the current editor view.
        float halfWidth = GameplayArtLayout.DefaultCameraHalfWidth;
        float left = camera.transform.position.x - halfWidth;
        float right = camera.transform.position.x + halfWidth;
        Bounds capsule = tomato.GetComponent<Collider2D>().bounds;
        bool pass = capsule.min.x >= left + .1f && capsule.max.x <= right - .1f;
        if (!pass) errors.Add("initial camera clips green-door tomato spawn");
        rows.Add($"INITIAL CAMERA (authored 16:9) | centreX={camera.transform.position.x:0.###} halfWidth={halfWidth:0.###} view=[{left:0.###},{right:0.###}] | door-spawn capsule=[{capsule.min.x:0.###},{capsule.max.x:0.###}] | left clearance={capsule.min.x - left:0.###} | {(pass ? "PASS" : "FAIL")}");
    }

    static void AuditEverySerializedCollider(List<string> rows, List<string> errors)
    {
        rows.Add(string.Empty);
        rows.Add("ALL SERIALIZED COLLIDERS (unknown entries are failures; no hidden global floor is permitted)");
        var known = new HashSet<string>();
        foreach (GameplayArtLayout.SurfaceSpec spec in GameplayArtLayout.AllSolidSurfaces) known.Add(spec.Name);
        foreach (GameplayArtLayout.TriggerSpec spec in GameplayArtLayout.Hazards) known.Add(spec.Name);
        known.Add(GameplayArtLayout.FallHazard.Name);
        foreach (GameplayArtLayout.CheckpointSpec spec in GameplayArtLayout.Checkpoints) known.Add(spec.Name);
        known.Add("Immortal Tomato");
        known.Add("Chef Enemy");
        known.Add("Waiter Enemy");
        known.Add(GameplayArtLayout.LeftWorldBoundary.Name);

        Collider2D[] colliders = UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        Array.Sort(colliders, (a, b) => string.CompareOrdinal(a.name, b.name));
        rows.Add($"count={colliders.Length}; expected={known.Count}");
        if (colliders.Length != known.Count) errors.Add($"serialized collider count {colliders.Length} != expected {known.Count}");
        foreach (Collider2D collider in colliders)
        {
            bool pass = known.Contains(collider.name);
            if (!pass) errors.Add("unknown collider: " + collider.name);
            rows.Add($"{collider.name} | {collider.GetType().Name} trigger={collider.isTrigger} layer={LayerMask.LayerToName(collider.gameObject.layer)} | {BoundsText(collider.bounds)} | {(pass ? "CATALOGUED" : "UNKNOWN/FAIL")}");
        }
    }

    static void AuditLeftWorldBoundary(List<string> rows, List<string> errors)
    {
        GameplayArtLayout.BoundarySpec spec = GameplayArtLayout.LeftWorldBoundary;
        Collider2D collider = RequireCollider(spec.Name, errors);
        Bounds actual = collider != null ? collider.bounds : default;
        float faceDelta = actual.max.x - spec.InnerFaceWorldX;
        bool pass = collider is BoxCollider2D && !collider.isTrigger &&
                    Mathf.Abs(faceDelta) <= AlignmentTolerance &&
                    Mathf.Abs(actual.min.y - spec.BottomWorld) <= AlignmentTolerance &&
                    Mathf.Abs(actual.max.y - spec.TopWorld) <= AlignmentTolerance;
        if (!pass) errors.Add("left world boundary mismatch");
        rows.Add("LEFT MAP BOUNDARY (solid containment wall, not a floor or death trigger)");
        rows.Add($"{spec.Name} | {BoundsText(actual)} | inside face=source px{spec.InnerFacePixelX:0} / worldX={spec.InnerFaceWorldX:0.###}; vertical=[{spec.BottomWorld:0.###},{spec.TopWorld:0.###}] | faceDelta={faceDelta:+0.###;-0.###;0} | {(pass ? "PASS" : "FAIL")}");
    }

    static void AuditAnimatedFires(List<string> rows, List<string> errors, int hazardLayer)
    {
        rows.Add(string.Empty);
        rows.Add("ANIMATED FIRE COVERAGE (keyed Flame pixels fitted to measured painted fireboxes)");
        rows.Add("painted fire group | target source pixels | keyed visual union | matching trigger | result");
        foreach (GameplayArtLayout.FireSpec fire in GameplayArtLayout.Fires)
        {
            GameObject trigger = GameObject.Find(fire.Name);
            Collider2D collider = trigger != null ? trigger.GetComponent<Collider2D>() : null;
            Bounds target = fire.TargetBounds;
            AnimatedFirePlacement[] placements = trigger != null ? trigger.GetComponentsInChildren<AnimatedFirePlacement>(true) : Array.Empty<AnimatedFirePlacement>();
            int expectedSlots = AnimatedFirePlacement.SlotsFor(fire);
            Bounds triggerTarget = fire.TriggerBounds;
            bool triggerContained = BoundsWithin(triggerTarget, target);
            bool triggerPass = collider is BoxCollider2D && collider.isTrigger && collider.gameObject.layer == hazardLayer &&
                               collider.GetComponent<HazardZone>() != null && triggerContained && BoundsClose(collider.bounds, triggerTarget);
            bool framePass = placements.Length == expectedSlots;
            bool placementPass = true;
            bool first = true;
            Bounds groupedVisual = default;
            for (int i = 0; i < placements.Length; i++)
            {
                AnimatedFirePlacement placement = placements[i];
                AmbientSpriteAnimator animator = placement.GetComponent<AmbientSpriteAnimator>();
                SpriteRenderer renderer = placement.GetComponent<SpriteRenderer>();
                bool flameFrames = animator != null && animator.Frames != null && animator.Frames.Length == 8;
                if (flameFrames)
                    foreach (Sprite frame in animator.Frames)
                        flameFrames &= frame != null && frame.name.StartsWith("Flame_");
                bool chroma = renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null &&
                              renderer.sharedMaterial.shader.name == "ImmortalTomato/AmbientChromaKey";
                Bounds visual = placement.KeyedVisibleBounds;
                bool contained = visual.min.x >= target.min.x - AlignmentTolerance && visual.max.x <= target.max.x + AlignmentTolerance &&
                                 visual.min.y >= target.min.y - AlignmentTolerance && visual.max.y <= target.max.y + AlignmentTolerance;
                bool identity = placement.FireName == fire.Name && placement.SlotCount == expectedSlots &&
                                placement.SlotIndex >= 0 && placement.SlotIndex < expectedSlots &&
                                BoundsClose(placement.TargetBounds, target) && placement.transform.parent == trigger.transform;
                framePass &= flameFrames && chroma;
                placementPass &= contained && identity;
                if (first) { groupedVisual = visual; first = false; }
                else groupedVisual.Encapsulate(visual);
            }
            // For a multi-flame group, the outer keyed visual bounds must hug
            // the measured firebox on all sides. This catches both floating
            // flames and colliders placed below their actual animation.
            bool tight = !first && (expectedSlots == 1 || BoundsClose(groupedVisual, target));
            bool pass = triggerPass && framePass && placementPass && tight;
            if (!pass) errors.Add("animated fire mismatch: " + fire.Name);
            rows.Add($"{fire.Name} | visual x{fire.LeftPixel:0}-{fire.RightPixel:0} y{fire.TopPixel:0}-{fire.BottomPixel:0}; trigger x{fire.TriggerLeftPixel:0}-{fire.TriggerRightPixel:0} y{fire.TriggerTopPixel:0}-{fire.TriggerBottomPixel:0} | {BoundsText(groupedVisual)} slots={placements.Length}/{expectedSlots} | {(triggerPass ? "Hazard trigger" : "MISSING trigger")} | {(pass ? "PASS" : "FAIL")}");
        }

        TomatoGame tomato = UnityEngine.Object.FindFirstObjectByType<TomatoGame>();
        Collider2D tomatoCollider = tomato != null ? tomato.GetComponent<Collider2D>() : null;
        rows.Add("INITIAL SPAWN / FIRE SEPARATION (the green-door capsule must not begin inside a lethal trigger)");
        foreach (GameplayArtLayout.FireSpec fire in GameplayArtLayout.Fires)
        {
            Collider2D collider = GameObject.Find(fire.Name)?.GetComponent<Collider2D>();
            float separation = tomatoCollider != null && collider != null ? Physics2D.Distance(tomatoCollider, collider).distance : float.NegativeInfinity;
            bool safe = separation > .01f;
            if (!safe) errors.Add("initial tomato capsule overlaps fire trigger: " + fire.Name);
            rows.Add($"{fire.Name} | capsule separation={separation:0.###} | {(safe ? "PASS" : "FAIL")}");
        }
    }

    static Collider2D RequireCollider(string name, List<string> errors)
    {
        GameObject gameObject = GameObject.Find(name);
        Collider2D collider = gameObject != null ? gameObject.GetComponent<Collider2D>() : null;
        if (collider == null) errors.Add("missing collider: " + name);
        return collider;
    }

    static Collider2D FindSupportingRoute(float x, float footY)
    {
        Collider2D closest = null;
        float closestDelta = float.PositiveInfinity;
        foreach (GameplayArtLayout.SurfaceSpec spec in GameplayArtLayout.RouteSurfaces)
        {
            Collider2D collider = GameObject.Find(spec.Name)?.GetComponent<Collider2D>();
            if (collider == null || x < collider.bounds.min.x - AlignmentTolerance || x > collider.bounds.max.x + AlignmentTolerance) continue;
            float delta = Mathf.Abs(footY - collider.bounds.max.y);
            if (delta < closestDelta) { closestDelta = delta; closest = collider; }
        }
        return closest;
    }

    static bool BoundsClose(Bounds actual, Bounds expected) =>
        Mathf.Abs(actual.min.x - expected.min.x) <= AlignmentTolerance &&
        Mathf.Abs(actual.max.x - expected.max.x) <= AlignmentTolerance &&
        Mathf.Abs(actual.min.y - expected.min.y) <= AlignmentTolerance &&
        Mathf.Abs(actual.max.y - expected.max.y) <= AlignmentTolerance;

    static bool BoundsWithin(Bounds inner, Bounds outer) =>
        inner.min.x >= outer.min.x - AlignmentTolerance && inner.max.x <= outer.max.x + AlignmentTolerance &&
        inner.min.y >= outer.min.y - AlignmentTolerance && inner.max.y <= outer.max.y + AlignmentTolerance;

    static string BoundsText(Bounds bounds) => string.Format(CultureInfo.InvariantCulture, "L{0:0.###},R{1:0.###},B{2:0.###},T{3:0.###}", bounds.min.x, bounds.max.x, bounds.min.y, bounds.max.y);
    static string VectorText(Vector3 value) => string.Format(CultureInfo.InvariantCulture, "({0:0.###},{1:0.###})", value.x, value.y);
}
