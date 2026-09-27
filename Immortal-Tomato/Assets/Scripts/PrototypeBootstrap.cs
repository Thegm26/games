using UnityEngine;
using System.Collections.Generic;

public static class PrototypeBootstrap
{
    // Called by the editor scene authoring command. Kept outside Editor so the
    // saved scene uses exactly the same simple setup in every Unity install.
    public static void CreateSceneContent(Sprite platformSprite, Sprite[] kitchenSections, Dictionary<string, Sprite[]> ambientFrames, Material ambientMaterial, Sprite[] fireMaskSprites = null)
    {
        if (Object.FindFirstObjectByType<TomatoGame>() != null) return;
        var camera = new GameObject("Main Camera", typeof(Camera));
        camera.tag = "MainCamera";
        var cam = camera.GetComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 5f; cam.backgroundColor = new Color(.055f, .09f, .12f); camera.transform.position = new Vector3(-41.61f, 0, -10);
        const float sectionWidth = 33.666f;
        float[] sections = { -sectionWidth, 0f, sectionWidth };
        for (int i = 0; i < 3; i++) MakeBackground($"Kitchen Section {i + 1:00}", kitchenSections[i], sections[i]);
        if (fireMaskSprites != null)
            for (int i = 0; i < GameplayArtLayout.Fires.Count; i++)
                if (fireMaskSprites[i] != null) MakeFireMask(GameplayArtLayout.Fires[i], fireMaskSprites[i]);
        MakeKitchenSeamCover("Kitchen Seam 01", -sectionWidth * .5f, platformSprite);
        MakeKitchenSeamCover("Kitchen Seam 02", sectionWidth * .5f, platformSprite);
        MakeAmbient("S1 Pot Steam", ambientFrames["Steam"], 8f, .0f, new Vector2(sections[0] - 5.2f, -1.3f), .52f, ambientMaterial);
        MakeAmbient("S1 Hanging Pan", ambientFrames["Pan"], 7f, .45f, new Vector2(-40f, 1.7f), .43f, ambientMaterial);
        MakeAmbient("S2 Pot Steam", ambientFrames["Steam"], 8f, .34f, new Vector2(sections[1] + 2.7f, -1.15f), .48f, ambientMaterial);
        MakeAmbient("S2 Hanging Pan", ambientFrames["Pan"], 7f, .7f, new Vector2(sections[1] + .9f, 1.5f), .40f, ambientMaterial);
        MakeAmbient("S3 Cold Vent Mist", ambientFrames["ColdMist"], 8f, .19f, new Vector2(sections[2] - 1.5f, 1.65f), .58f, ambientMaterial);
        MakeAmbient("S3 Fan Overlay", ambientFrames["Fan"], 11f, .0f, new Vector2(sections[2] - 1.4f, 1.95f), .52f, ambientMaterial);
        MakeAmbient("S3 Exit Lamp", ambientFrames["ExitLamp"], 7f, .37f, new Vector2(sections[2] + 14.1f, 2.05f), .42f, ambientMaterial);
        MakeAmbient("S3 Sausage Swing A", ambientFrames["Sausage"], 7f, .22f, new Vector2(sections[2] + 8.5f, 2.35f), .42f, ambientMaterial);
        MakeAmbient("S3 Sausage Swing B", ambientFrames["Sausage"], 7f, .68f, new Vector2(sections[2] + 10.1f, 2.35f), .42f, ambientMaterial);
        // Every solid is a measured painted counter/ice/shelf top.  The
        // playable route and the small high drawers live in separate catalogs
        // for QA, but share this single scene-authoring path.  No global floor
        // is added beneath the background.
        foreach (GameplayArtLayout.SurfaceSpec surface in GameplayArtLayout.AllSolidSurfaces)
            MakePlatform(surface, platformSprite);
        var tomato = new GameObject("Immortal Tomato", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(TomatoGame));
        // Spawn 37 source pixels in front of the S1 green-door jamb, on its
        // measured service-counter top rather than the old arbitrary table.
        Vector2 doorSpawn = GameplayArtLayout.DoorSpawn;
        tomato.transform.position = new Vector3(doorSpawn.x, doorSpawn.y, 0);
        tomato.transform.localScale = Vector3.one * .54f;
        var rigidbody = tomato.GetComponent<Rigidbody2D>(); rigidbody.gravityScale = 2.4f; rigidbody.freezeRotation = true;
        var collider = tomato.GetComponent<CapsuleCollider2D>(); collider.size = new Vector2(2.55f, 3.55f); collider.offset = new Vector2(0, 1.775f);
        // Enemy source art is authored facing left: tomato progress is to the
        // right, so both enemies watch and attack from their right-hand posts.
        // Their colliders are trigger-only; they never add an invisible wall
        // to the painted counter route.
        foreach (GameplayArtLayout.EnemyAnchorSpec enemy in GameplayArtLayout.EnemyAnchors)
        {
            if (enemy.Name == "Chef Enemy") MakeChef(GameplayArtLayout.EnemyRoot(enemy));
            else if (enemy.Name == "Waiter Enemy") MakeWaiter(GameplayArtLayout.EnemyRoot(enemy));
        }
        foreach (GameplayArtLayout.CheckpointSpec checkpoint in GameplayArtLayout.Checkpoints)
            MakeCheckpoint(checkpoint);
        foreach (GameplayArtLayout.TriggerSpec hazard in GameplayArtLayout.Hazards)
            MakeHazard(hazard);
        foreach (GameplayArtLayout.FireSpec fire in GameplayArtLayout.Fires)
            MakeFireGroup(fire, ambientFrames["Flame"], ambientMaterial);
        MakeFallHazard(GameplayArtLayout.FallHazard);
        MakeLeftWorldBoundary(GameplayArtLayout.LeftWorldBoundary);
        camera.AddComponent<HorizontalCameraFollow>().Configure(tomato.transform, GameplayArtLayout.DefaultCameraMinX, GameplayArtLayout.DefaultCameraMaxX);
    }

    static void MakePlatform(GameplayArtLayout.SurfaceSpec surface, Sprite platformSprite)
    {
        var platform = new GameObject(surface.Name, typeof(SpriteRenderer));
        platform.transform.position = surface.CenterWorld;
        var renderer = platform.GetComponent<SpriteRenderer>();
        renderer.sprite = platformSprite;
        // The root owns the collider only.  Rendering the full 0.12-unit
        // collision box made a row of flat, opaque blocks sit on top of the
        // painted kitchen.  Keep that renderer for the serialized scene
        // reference, but do not draw it.
        renderer.color = Color.clear;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = -20;
        renderer.enabled = false;
        platform.transform.localScale = new Vector3(surface.ColliderSize.x, surface.ColliderSize.y, 1);
        var visual = platform.AddComponent<GameplaySurfaceVisual>();
        visual.SurfaceName = surface.Name;
        visual.PaintedReference = surface.PaintedReference;
        visual.MaterialCategory = SurfaceCategory(surface);
        visual.BodyRenderer = renderer;

        // These two slim strips are a visual cue for the exact playable edge,
        // not replacement counter artwork.  Their shared top sits exactly on
        // the measured collider top: a 0.03 highlight followed by a soft
        // 0.08 fascia below it.  That lets the original painted counter,
        // shelf, ice, or door remain the dominant material.
        const float highlightHeight = .03f;
        const float fasciaHeight = .08f;
        var fasciaObject = new GameObject("Surface Fascia");
        fasciaObject.transform.SetParent(platform.transform, false);
        fasciaObject.transform.localPosition = new Vector3(0f, (GameplayArtLayout.PlatformThickness * .5f - highlightHeight - fasciaHeight * .5f) / GameplayArtLayout.PlatformThickness, 0f);
        fasciaObject.transform.localScale = new Vector3(1f, fasciaHeight / GameplayArtLayout.PlatformThickness, 1f);
        var fascia = fasciaObject.AddComponent<SpriteRenderer>();
        fascia.sprite = platformSprite;
        fascia.color = SurfaceColor(surface, false);
        fascia.sortingLayerName = "Default";
        fascia.sortingOrder = -20;
        visual.FasciaRenderer = fascia;

        var highlightObject = new GameObject("Surface Lip Highlight");
        highlightObject.transform.SetParent(platform.transform, false);
        highlightObject.transform.localPosition = new Vector3(0f, (GameplayArtLayout.PlatformThickness * .5f - highlightHeight * .5f) / GameplayArtLayout.PlatformThickness, 0f);
        highlightObject.transform.localScale = new Vector3(1f, highlightHeight / GameplayArtLayout.PlatformThickness, 1f);
        var highlight = highlightObject.AddComponent<SpriteRenderer>();
        highlight.sprite = platformSprite;
        highlight.color = SurfaceColor(surface, true);
        highlight.sortingLayerName = "Default";
        highlight.sortingOrder = -19;
        visual.HighlightRenderer = highlight;
        var collider = platform.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        // Wall drawers are still real standable colliders, but one-way so a
        // normal route jump can pass beneath the artwork instead of hitting
        // an invisible underside.  Landing on their painted top remains
        // fully solid.
        if (surface.OneWay)
        {
            var effector = platform.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useOneWayGrouping = true;
            // These wall drawers are landing surfaces, not invisible side
            // walls. A 90-degree surface arc accepts contact from above while
            // letting the tomato move past the painted shelf ends/bottom.
            effector.surfaceArc = 90f;
            collider.usedByEffector = true;
        }
    }

    static string SurfaceCategory(GameplayArtLayout.SurfaceSpec surface)
    {
        string n = surface.Name.ToLowerInvariant();
        if (n.Contains("ice") || n.Contains("freezer") || n.Contains("frozen")) return "cold-ice";
        if (n.Contains("shelf") || n.Contains("drawer") || n.Contains("plate") || n.Contains("produce")) return "wall-shelf";
        if (n.Contains("exit") || n.Contains("door")) return "door-trim";
        if (n.Contains("crate") || n.Contains("sausage") || n.Contains("bridge")) return "wood-prep";
        return "steel-counter";
    }

    static Color SurfaceColor(GameplayArtLayout.SurfaceSpec surface, bool highlight)
    {
        Color body;
        switch (SurfaceCategory(surface))
        {
            // Low-alpha, desaturated tones borrow the light from the source
            // panorama instead of covering it with a synthetic rectangle.
            case "cold-ice": body = new Color(.26f, .52f, .58f, .48f); break;
            case "wall-shelf": body = new Color(.20f, .29f, .30f, .52f); break;
            case "door-trim": body = new Color(.40f, .16f, .12f, .50f); break;
            case "wood-prep": body = new Color(.45f, .27f, .12f, .52f); break;
            default: body = new Color(.18f, .31f, .33f, .56f); break;
        }
        if (!highlight) return body;
        Color edge = Color.Lerp(new Color(body.r, body.g, body.b, 1f), Color.white, .42f);
        edge.a = Mathf.Min(.72f, body.a + .16f);
        return edge;
    }

    static void MakeBackground(string label, Sprite sprite, float x)
    {
        var backdrop = new GameObject(label, typeof(SpriteRenderer));
        backdrop.transform.position = new Vector3(x, 0, 5);
        backdrop.transform.localScale = Vector3.one * 1.55f;
        var renderer = backdrop.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingLayerName = "Background"; renderer.sortingOrder = 0;
    }

    static void MakeFireMask(GameplayArtLayout.FireSpec fire, Sprite sprite)
    {
        Bounds bounds = fire.TargetBounds;
        var mask = new GameObject($"{fire.Name} Background Fire Mask", typeof(SpriteRenderer), typeof(BackgroundFireMask));
        mask.transform.position = new Vector3(bounds.center.x, bounds.center.y, 4.5f);
        mask.transform.localScale = new Vector3(1.55f, 1.55f, 1f);
        var renderer = mask.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = 5;
        var marker = mask.GetComponent<BackgroundFireMask>();
        marker.FireName = fire.Name;
        marker.SourcePixels = new Rect(fire.LeftPixel, fire.TopPixel, fire.RightPixel - fire.LeftPixel, fire.BottomPixel - fire.TopPixel);
        marker.Renderer = renderer;
    }

    // The supplied panorama slices meet at visible art boundaries. These
    // non-physical steel posts make those joins read as deliberate framing.
    static void MakeKitchenSeamCover(string label, float x, Sprite sprite)
    {
        MakeBackdropDetail(label + " Post", sprite, new Vector3(x, 0f, 4f), new Vector2(.84f, 11.4f), new Color(.055f, .105f, .125f), 1);
        MakeBackdropDetail(label + " Face", sprite, new Vector3(x + .06f, 0f, 3.9f), new Vector2(.54f, 11.12f), new Color(.13f, .215f, .235f), 2);
        MakeBackdropDetail(label + " Edge", sprite, new Vector3(x - .27f, 0f, 3.8f), new Vector2(.065f, 11.05f), new Color(.48f, .64f, .62f), 3);
        MakeBackdropDetail(label + " Top Bracket", sprite, new Vector3(x, 5.29f, 3.7f), new Vector2(1.08f, .18f), new Color(.29f, .41f, .42f), 3);
        MakeBackdropDetail(label + " Bottom Bracket", sprite, new Vector3(x, -5.29f, 3.7f), new Vector2(1.08f, .18f), new Color(.29f, .41f, .42f), 3);
    }

    static void MakeBackdropDetail(string label, Sprite sprite, Vector3 position, Vector2 size, Color color, int sortingOrder)
    {
        var detail = new GameObject(label, typeof(SpriteRenderer));
        detail.transform.position = position;
        detail.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = detail.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = sortingOrder;
    }

    static void MakeAmbient(string label, Sprite[] frames, float fps, float offset, Vector2 position, float scale, Material material)
    {
        var ambient = new GameObject(label, typeof(SpriteRenderer), typeof(AmbientSpriteAnimator));
        ambient.transform.position = new Vector3(position.x, position.y, 1f);
        ambient.transform.localScale = Vector3.one * scale;
        var renderer = ambient.GetComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Ambient";
        renderer.sortingOrder = 10;
        renderer.sharedMaterial = material;
        ambient.GetComponent<AmbientSpriteAnimator>().Configure(frames, fps, true, offset);
    }

    static void MakeFireGroup(GameplayArtLayout.FireSpec fire, Sprite[] frames, Material material)
    {
        // Wide painted burner rows get several normally-proportioned flames;
        // narrow/tall fireboxes get only as many as their measured rectangle
        // can contain. Each child is attached to its matching kill trigger.
        Transform trigger = GameObject.Find(fire.Name).transform;
        int slots = AnimatedFirePlacement.SlotsFor(fire);
        for (int index = 0; index < slots; index++)
        {
            var flame = new GameObject($"{fire.Name} Animated Flame {index + 1:00}", typeof(SpriteRenderer), typeof(AmbientSpriteAnimator), typeof(AnimatedFirePlacement));
            flame.transform.SetParent(trigger, true);
            var renderer = flame.GetComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Ambient";
            renderer.sortingOrder = 10;
            renderer.sharedMaterial = material;
            flame.GetComponent<AmbientSpriteAnimator>().Configure(frames, 11f, true, index / (float)slots);
            flame.GetComponent<AnimatedFirePlacement>().Configure(fire, index, slots);
        }
    }

    static void MakeHazard(GameplayArtLayout.TriggerSpec spec)
    {
        var hazard = new GameObject(spec.Name, typeof(BoxCollider2D), typeof(HazardZone));
        hazard.layer = LayerMask.NameToLayer("Hazard");
        Bounds bounds = spec.ExpectedBounds;
        hazard.transform.position = bounds.center;
        var collider = hazard.GetComponent<BoxCollider2D>(); collider.size = bounds.size; collider.isTrigger = true;
    }

    static void MakeFallHazard(GameplayArtLayout.WorldTriggerSpec spec)
    {
        var hazard = new GameObject(spec.Name, typeof(BoxCollider2D), typeof(HazardZone));
        hazard.layer = LayerMask.NameToLayer("Hazard");
        hazard.transform.position = spec.ExpectedBounds.center;
        var collider = hazard.GetComponent<BoxCollider2D>(); collider.size = spec.ExpectedBounds.size; collider.isTrigger = true;
    }

    static void MakeLeftWorldBoundary(GameplayArtLayout.BoundarySpec spec)
    {
        var boundary = new GameObject(spec.Name, typeof(BoxCollider2D));
        boundary.transform.position = spec.CenterWorld;
        var collider = boundary.GetComponent<BoxCollider2D>();
        collider.size = spec.ColliderSize;
        collider.isTrigger = false;
    }

    static void MakeChef(Vector2 position)
    {
        var chef = new GameObject("Chef Enemy", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(ChefEnemy));
        chef.transform.position = position;
        chef.transform.localScale = Vector3.one * GameplayArtLayout.ChefScale;
        var collider = chef.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(6.5f, 8f);
        collider.offset = new Vector2(0f, 4f);
        collider.isTrigger = true;
        var body = chef.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
    }

    static void MakeWaiter(Vector2 position)
    {
        var waiter = new GameObject("Waiter Enemy", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(WaiterEnemy));
        waiter.transform.position = position;
        waiter.transform.localScale = Vector3.one * GameplayArtLayout.WaiterScale;
        var collider = waiter.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(6.5f, 8f);
        collider.offset = new Vector2(0f, 4f);
        collider.isTrigger = true;
        var body = waiter.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
    }

    static void MakeCheckpoint(GameplayArtLayout.CheckpointSpec spec)
    {
        var checkpoint = new GameObject(spec.Name, typeof(BoxCollider2D), typeof(CheckpointTrigger));
        checkpoint.transform.position = GameplayArtLayout.CheckpointCenter(spec);
        var collider = checkpoint.GetComponent<BoxCollider2D>(); collider.size = new Vector2(.55f, 2.2f); collider.isTrigger = true;
    }

}
