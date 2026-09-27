using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public static class WebGLBuilder
{
    const string ScenePath = "Assets/Main.unity";
    const string PlatformTexturePath = "Assets/Art/PlatformPixel.asset";
    static readonly string[] KitchenSectionPaths = { "Assets/Art/KitchenSection01.png", "Assets/Art/KitchenSection02.png", "Assets/Art/KitchenSection03.png" };
    const string AmbientFramesRoot = "Assets/Art/Ambient/Frames";
    const string AmbientMaterialPath = "Assets/Art/Ambient/AmbientChromaKey.mat";
    const string AmbientShaderPath = "Assets/Shaders/AmbientChromaKey.shader";
    const string CombatHudUziPath = "Assets/Resources/UI/UziHud.png";
    const string FireMaskRoot = "Assets/Art/FireMasks";
    static readonly string[] TomatoFrameRoots =
    {
        "Assets/Resources/Frames/Idle", "Assets/Resources/Frames/Run",
        "Assets/Resources/Frames/Jump", "Assets/Resources/Frames/Shoot",
        "Assets/Resources/Frames/Hit", "Assets/Resources/Frames/Regen"
    };

    public static void CreateScene()
    {
        EnsureTomatoFrameImports();
        EnsureCombatHudUziImport();
        Sprite[] fireMasks = EnsureBackgroundFireMasks();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PrototypeBootstrap.CreateSceneContent(GetPlatformSprite(), GetKitchenSectionSprites(), GetAmbientFrameLibrary(), GetAmbientMaterial(), fireMasks);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Authored scene saved: " + ScenePath);
    }

    /// <summary>
    /// Character PNGs are generated at 420x724.  Unity's old nearest-power-
    /// of-two setting silently imported them as 512x1024, changing the aspect
    /// ratio and making every manually-created sprite pivot wrong.  These are
    /// runtime-readable Texture2D frames rather than imported sprite assets,
    /// so keep their exact native pixel canvas and uncompressed alpha data.
    /// </summary>
    public static void EnsureTomatoFrameImports()
    {
        bool changed = false;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", TomatoFrameRoots))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new System.Exception("Tomato frame importer missing: " + path);
            bool needsImport = importer.npotScale != TextureImporterNPOTScale.None ||
                               !importer.isReadable || importer.mipmapEnabled ||
                               importer.textureCompression != TextureImporterCompression.Uncompressed ||
                               importer.filterMode != FilterMode.Bilinear;
            if (!needsImport) continue;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            changed = true;
        }
        if (changed) AssetDatabase.SaveAssets();
    }

    /// <summary>Keeps the top-left Uzi asset readable for transparency validation and a crisp HUD fill mask.</summary>
    public static void EnsureCombatHudUziImport()
    {
        var importer = AssetImporter.GetAtPath(CombatHudUziPath) as TextureImporter;
        if (importer == null) throw new System.Exception("Combat HUD Uzi is missing: " + CombatHudUziPath);
        bool needsImport = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single ||
                           importer.spritePixelsPerUnit != 100 || importer.npotScale != TextureImporterNPOTScale.None ||
                           !importer.isReadable || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                           importer.filterMode != FilterMode.Point || !importer.alphaIsTransparency;
        if (!needsImport) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.isReadable = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Point;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
    }

    static Sprite GetPlatformSprite()
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PlatformTexturePath);
        if (texture == null)
        {
            texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "PlatformPixel" };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, PlatformTexturePath);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            sprite.name = "PlatformPixelSprite";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.ImportAsset(PlatformTexturePath);
        }
        var sprites = AssetDatabase.LoadAllAssetsAtPath(PlatformTexturePath);
        foreach (var asset in sprites)
            if (asset is Sprite sprite) return sprite;
        throw new System.Exception("Platform sprite could not be created.");
    }

    static Sprite[] GetKitchenSectionSprites()
    {
        var sprites = new Sprite[KitchenSectionPaths.Length];
        for (int i = 0; i < KitchenSectionPaths.Length; i++)
        {
            var importer = AssetImporter.GetAtPath(KitchenSectionPaths[i]) as TextureImporter;
            if (importer == null) throw new System.Exception("Kitchen section is missing: " + KitchenSectionPaths[i]);
            if (importer.textureType != TextureImporterType.Sprite || importer.spritePixelsPerUnit != 100)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(KitchenSectionPaths[i]);
            if (sprites[i] == null) throw new System.Exception("Kitchen section sprite import failed: " + KitchenSectionPaths[i]);
        }
        return sprites;
    }

    static Sprite[] EnsureBackgroundFireMasks()
    {
        Directory.CreateDirectory(FireMaskRoot);
        var result = new Sprite[GameplayArtLayout.Fires.Count];
        for (int i = 0; i < GameplayArtLayout.Fires.Count; i++)
        {
            GameplayArtLayout.FireSpec fire = GameplayArtLayout.Fires[i];
            string sourcePath = KitchenSectionPaths[fire.Section];
            var importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
            if (importer == null) throw new System.Exception("Kitchen section importer missing: " + sourcePath);
            if (!importer.isReadable || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.isReadable = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
            if (source == null) throw new System.Exception("Kitchen source texture missing: " + sourcePath);
            int left = Mathf.RoundToInt(fire.LeftPixel);
            int right = Mathf.RoundToInt(fire.RightPixel);
            int top = Mathf.RoundToInt(fire.TopPixel);
            int bottom = Mathf.RoundToInt(fire.BottomPixel);
            int width = Mathf.Max(1, right - left);
            int height = Mathf.Max(1, bottom - top);
            var patch = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = fire.Name + " Background Fire Mask" };
            var pixels = new Color32[width * height];
            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    int sourceX = Mathf.Clamp(left + px, 0, source.width - 1);
                    // GameplayArtLayout uses source-image top-left pixels;
                    // Texture2D.GetPixel uses bottom-left coordinates.
                    int sourceY = Mathf.Clamp(source.height - 1 - (top + py), 0, source.height - 1);
                    Color32 sourcePixel = source.GetPixel(sourceX, sourceY);
                    int textureRow = py;
                    if (!IsPaintedFlame(sourcePixel)) { pixels[textureRow * width + px] = new Color32(0, 0, 0, 0); continue; }
                    Color replacement = FindSafeBackgroundPixel(source, sourceX, sourceY);
                    pixels[textureRow * width + px] = replacement;
                }
            }
            patch.SetPixels32(pixels);
            patch.Apply(false, false);
            string path = $"{FireMaskRoot}/{Sanitize(fire.Name)}.png";
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            File.WriteAllBytes(Path.Combine(projectRoot, path), patch.EncodeToPNG());
            Object.DestroyImmediate(patch);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var maskImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (maskImporter == null) throw new System.Exception("Fire mask importer missing: " + path);
            maskImporter.textureType = TextureImporterType.Sprite;
            maskImporter.spriteImportMode = SpriteImportMode.Single;
            maskImporter.spritePixelsPerUnit = 100;
            maskImporter.npotScale = TextureImporterNPOTScale.None;
            maskImporter.isReadable = true;
            maskImporter.mipmapEnabled = false;
            maskImporter.textureCompression = TextureImporterCompression.Uncompressed;
            maskImporter.filterMode = FilterMode.Bilinear;
            maskImporter.SaveAndReimport();
            result[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        AssetDatabase.SaveAssets();
        return result;
    }

    static bool IsPaintedFlame(Color32 p)
    {
        if (p.a < 32) return false;
        // The panorama has warm cream lighting, so hue alone also selects
        // wall tiles and stainless highlights. Restrict the patch to the
        // saturated orange/red fire pixels that the replacement animation
        // actually covers.
        // GetPixel returns linear colours for sRGB imports; after the
        // Color32 conversion saturated red flames can have single-digit G.
        return p.r > 150 && p.g > 8 && p.b < 100 && p.r > p.g + 50 && p.g > p.b + 10;
    }

    static Color FindSafeBackgroundPixel(Texture2D source, int x, int y)
    {
        int[] offsets = { 14, -14, 24, -24, 8, -8, 32, -32 };
        foreach (int dy in offsets)
        {
            int yy = Mathf.Clamp(y + dy, 0, source.height - 1);
            Color32 candidate = source.GetPixel(x, yy);
            if (!IsPaintedFlame(candidate))
            {
                float luminance = candidate.r * .2126f + candidate.g * .7152f + candidate.b * .0722f;
                // Prefer the nearest same-column surface colour. Very bright
                // candidates are usually wall glow/pizza highlights rather
                // than the dark stove/cabinet that sits behind the flame.
                if (luminance < .62f) return candidate;
            }
        }
        for (int dx = 1; dx <= 18; dx++)
        {
            int lx = Mathf.Clamp(x - dx, 0, source.width - 1);
            int rx = Mathf.Clamp(x + dx, 0, source.width - 1);
            Color32 left = source.GetPixel(lx, y);
            if (!IsPaintedFlame(left))
            {
                float luminance = left.r * .2126f + left.g * .7152f + left.b * .0722f;
                if (luminance < .62f) return left;
            }
            Color32 right = source.GetPixel(rx, y);
            if (!IsPaintedFlame(right))
            {
                float luminance = right.r * .2126f + right.g * .7152f + right.b * .0722f;
                if (luminance < .62f) return right;
            }
        }
        // A saturated flame can fill every probe around its brightest core;
        // keep that pixel covered with a restrained stove-dark fallback so
        // the painted fire cannot leak back through the mask.
        return new Color(.075f, .065f, .06f, 1f);
    }

    static string Sanitize(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c.ToString(), string.Empty);
        return value.Replace(' ', '_');
    }

    static Dictionary<string, Sprite[]> GetAmbientFrameLibrary()
    {
        var pivots = new Dictionary<string, Vector2> {
            { "Flame", new Vector2(.5f, 0f) }, { "Steam", new Vector2(.5f, 0f) }, { "ColdMist", new Vector2(.5f, 0f) },
            { "Fan", new Vector2(.5f, .5f) }, { "ExitLamp", new Vector2(.5f, .5f) },
            { "Sausage", new Vector2(.5f, 1f) }, { "Pan", new Vector2(.5f, 1f) }
        };
        var library = new Dictionary<string, Sprite[]>();
        foreach (var pair in pivots)
        {
            var frames = new Sprite[8];
            for (int i = 0; i < frames.Length; i++)
            {
                string path = $"{AmbientFramesRoot}/{pair.Key}/{pair.Key}_{i + 1:00}.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new System.Exception("Ambient frame missing: " + path);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (importer.textureType != TextureImporterType.Sprite || importer.spritePixelsPerUnit != 100 || settings.spriteAlignment != (int)SpriteAlignment.Custom || settings.spritePivot != pair.Value)
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = pair.Value;
                    importer.SetTextureSettings(settings);
                    // Set type after texture settings: SetTextureSettings can
                    // otherwise restore the source's default texture mode.
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is Sprite sprite) { frames[i] = sprite; break; }
                if (frames[i] == null) throw new System.Exception("Ambient sprite import failed: " + path);
            }
            library[pair.Key] = frames;
        }
        return library;
    }

    static Material GetAmbientMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(AmbientMaterialPath);
        if (material != null) return material;
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(AmbientShaderPath);
        if (shader == null) throw new System.Exception("Ambient chroma-key shader is missing.");
        material = new Material(shader) { name = "AmbientChromaKey" };
        AssetDatabase.CreateAsset(material, AmbientMaterialPath);
        return material;
    }

    public static void ValidateScene()
    {
        EnsureTomatoFrameImports();
        EnsureCombatHudUziImport();
        UziFxAssetValidation.ValidateHudSprite();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var required = new List<string> { "Main Camera", "Kitchen Section 01", "Kitchen Section 02", "Kitchen Section 03", "Immortal Tomato", "Chef Enemy", "Waiter Enemy" };
        foreach (string seam in new[] { "Kitchen Seam 01", "Kitchen Seam 02" })
            required.AddRange(new[] { seam + " Post", seam + " Face", seam + " Edge", seam + " Top Bracket", seam + " Bottom Bracket" });
        foreach (GameplayArtLayout.SurfaceSpec surface in GameplayArtLayout.AllSolidSurfaces) required.Add(surface.Name);
        foreach (GameplayArtLayout.FireSpec fire in GameplayArtLayout.Fires) required.Add(fire.Name + " Background Fire Mask");
        foreach (GameplayArtLayout.TriggerSpec hazard in GameplayArtLayout.Hazards) required.Add(hazard.Name);
        required.Add(GameplayArtLayout.FallHazard.Name);
        foreach (GameplayArtLayout.CheckpointSpec checkpoint in GameplayArtLayout.Checkpoints) required.Add(checkpoint.Name);
        required.Add(GameplayArtLayout.LeftWorldBoundary.Name);
        foreach (var item in required)
            if (GameObject.Find(item) == null) throw new System.Exception("Missing authored scene object: " + item);
        var tomato = Object.FindFirstObjectByType<TomatoGame>();
        if (tomato == null) throw new System.Exception("TomatoGame is absent from Main scene.");
        var chef = Object.FindFirstObjectByType<ChefEnemy>();
        var waiter = Object.FindFirstObjectByType<WaiterEnemy>();
        if (chef == null || waiter == null) throw new System.Exception("Chef and waiter enemies must be authored into Main scene.");
        foreach (var enemy in new Component[] { chef, waiter })
        {
            var hitbox = enemy.GetComponent<BoxCollider2D>();
            var body = enemy.GetComponent<Rigidbody2D>();
            var renderer = enemy.GetComponent<SpriteRenderer>();
            float expectedScale = enemy == chef ? GameplayArtLayout.ChefScale : GameplayArtLayout.WaiterScale;
            if (hitbox == null || !hitbox.isTrigger || body == null || body.bodyType != RigidbodyType2D.Kinematic || renderer == null || renderer.flipX || Mathf.Abs(enemy.transform.localScale.x - expectedScale) > .001f)
                throw new System.Exception("Enemy setup must be left-facing, kinematic and trigger-only: " + enemy.name);
        }
        if (chef.GetComponent<BoxCollider2D>().bounds.Intersects(waiter.GetComponent<BoxCollider2D>().bounds))
            throw new System.Exception("Chef and waiter trigger boxes must not overlap at their authored posts.");
        foreach (GameplayArtLayout.EnemyAnchorSpec enemySpec in GameplayArtLayout.EnemyAnchors)
        {
            GameObject enemy = GameObject.Find(enemySpec.Name);
            if (Vector2.Distance(enemy.transform.position, GameplayArtLayout.EnemyRoot(enemySpec)) > .01f)
                throw new System.Exception("Enemy post is not on its measured painted support: " + enemySpec.Name);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", TomatoFrameRoots))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (importer == null || importer.npotScale != TextureImporterNPOTScale.None || !importer.isReadable || importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed || texture == null ||
                texture.width != 420 || texture.height != 724 || Mathf.Abs(texture.width / (float)texture.height - 420f / 724f) > .0001f)
                throw new System.Exception("Tomato frame importer must be native 420x724, non-POT, readable, uncompressed, and mipmap-free: " + path);
        }
        var tomatoCollider = tomato.GetComponent<CapsuleCollider2D>();
        if (tomatoCollider == null || tomatoCollider.direction != CapsuleDirection2D.Vertical)
            throw new System.Exception("Tomato physics capsule is absent or not vertical.");
        // Root, physics foot, and spawn counter surface must share one baseline.
        // TomatoGame aligns each alpha-foot to that physics foot at runtime.
        float localPhysicsFoot = tomatoCollider.offset.y - tomatoCollider.size.y * .5f;
        if (Mathf.Abs(localPhysicsFoot) > .0001f)
            throw new System.Exception("Tomato capsule foot must be local y=0.");
        var spawnCounter = GameObject.Find(GameplayArtLayout.DoorSupportName).GetComponent<Collider2D>();
        if (Vector2.Distance(tomato.transform.position, GameplayArtLayout.DoorSpawn) > .01f || Mathf.Abs(tomato.transform.position.y - spawnCounter.bounds.max.y) > .01f)
            throw new System.Exception("Tomato spawn is not aligned to the measured green-door counter surface.");
        if (Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Any(component => component.GetType().Name == "RuntimeQaOverlay"))
            throw new System.Exception("Main scene must not include the removed runtime QA overlay.");
        foreach (string removedDebugObject in new[] { "EventSystem", "UI", "Instructions", "Idle", "Run", "Jump", "Shoot", "Splat / Regen" })
            if (Resources.FindObjectsOfTypeAll<GameObject>().Any(gameObject => gameObject.scene == SceneManager.GetActiveScene() && gameObject.name == removedDebugObject))
                throw new System.Exception("Main scene must not include runtime QA UI: " + removedDebugObject);
        foreach (var pair in new[] { ("Kitchen Section 01", -33.666f), ("Kitchen Section 02", 0f), ("Kitchen Section 03", 33.666f) })
        {
            var sectionObject = GameObject.Find(pair.Item1);
            var section = sectionObject.transform;
            if (Mathf.Abs(section.position.x - pair.Item2) > .01f || Mathf.Abs(section.localScale.x - 1.55f) > .001f)
                throw new System.Exception("Incorrect section transform: " + pair.Item1);
            var sectionRenderer = sectionObject.GetComponent<SpriteRenderer>();
            if (sectionRenderer.sortingLayerName != "Background" || sectionRenderer.sortingOrder != 0)
                throw new System.Exception("Incorrect background render setup: " + pair.Item1);
        }
        foreach (string seam in new[] { "Kitchen Seam 01", "Kitchen Seam 02" })
        {
            foreach (string part in new[] { " Post", " Face", " Edge", " Top Bracket", " Bottom Bracket" })
            {
                var cover = GameObject.Find(seam + part);
                var renderer = cover.GetComponent<SpriteRenderer>();
                if (renderer == null || cover.GetComponent<Collider2D>() != null || renderer.sortingLayerName != "Background" || renderer.sortingOrder <= 0)
                    throw new System.Exception("Kitchen seam cover is not a foreground-only background detail: " + seam + part);
            }
        }
        UziFxAssetValidation.Validate();
        // Gameplay collision belongs only to source-measured counter/ice/shelf
        // tops plus the one explicit map-edge wall, never a safety floor.
        foreach (GameplayArtLayout.SurfaceSpec route in GameplayArtLayout.AllSolidSurfaces)
        {
            var platformObject = GameObject.Find(route.Name);
            var platform = platformObject.GetComponent<Collider2D>();
            var surfaceVisual = platformObject.GetComponent<GameplaySurfaceVisual>();
            if (platform == null || platform.isTrigger) throw new System.Exception("Visible route collider is invalid: " + route.Name);
            if (surfaceVisual == null || !surfaceVisual.IsConfigured || surfaceVisual.SurfaceName != route.Name || string.IsNullOrEmpty(surfaceVisual.MaterialCategory))
                throw new System.Exception("Measured route is missing its aligned foreground surface visual: " + route.Name);
            // Only narrow, translucent lip renderers may represent physics.
            // A collider-sized opaque body would visually detach from the
            // source panorama and make the characters appear to stand on
            // overlay blocks.
            Bounds colliderBounds = platform.bounds;
            Bounds fasciaBounds = surfaceVisual.FasciaRenderer.bounds;
            Bounds highlightBounds = surfaceVisual.HighlightRenderer.bounds;
            if (surfaceVisual.BodyRenderer.enabled ||
                Mathf.Abs(fasciaBounds.size.x - colliderBounds.size.x) > .01f ||
                Mathf.Abs(highlightBounds.size.x - colliderBounds.size.x) > .01f ||
                fasciaBounds.size.y < .075f || fasciaBounds.size.y > .09f ||
                highlightBounds.size.y < .025f || highlightBounds.size.y > .04f ||
                Mathf.Abs(highlightBounds.max.y - colliderBounds.max.y) > .01f ||
                Mathf.Abs(fasciaBounds.max.y - highlightBounds.min.y) > .01f ||
                surfaceVisual.FasciaRenderer.color.a < .35f || surfaceVisual.FasciaRenderer.color.a > .7f ||
                surfaceVisual.HighlightRenderer.color.a < .45f || surfaceVisual.HighlightRenderer.color.a > .8f ||
                surfaceVisual.FasciaRenderer.sortingOrder >= 20 || surfaceVisual.HighlightRenderer.sortingOrder >= 20)
                throw new System.Exception("Measured route surface visual must be a thin translucent lip behind characters: " + route.Name);
            var effector = platform.GetComponent<PlatformEffector2D>();
            if (route.OneWay != (platform.usedByEffector && effector != null && effector.useOneWay) ||
                (route.OneWay && Mathf.Abs(effector.surfaceArc - 90f) > .01f))
                throw new System.Exception("Shelf one-way collider setup is invalid: " + route.Name);
            float top = platform.bounds.max.y;
            if (Mathf.Abs(top - route.TopWorld) > .01f) throw new System.Exception("Route top is not aligned to measured art: " + route.Name);
        }
        foreach (GameplayArtLayout.FireSpec fire in GameplayArtLayout.Fires)
        {
            var maskObject = GameObject.Find(fire.Name + " Background Fire Mask");
            var mask = maskObject != null ? maskObject.GetComponent<BackgroundFireMask>() : null;
            var renderer = maskObject != null ? maskObject.GetComponent<SpriteRenderer>() : null;
            if (mask == null || renderer == null || !renderer.enabled || renderer.sortingLayerName != "Background" || renderer.sortingOrder <= 0 || mask.Renderer != renderer || mask.FireName != fire.Name)
                throw new System.Exception("Background fire pixels are not masked for: " + fire.Name);
        }
        var leftBoundary = GameObject.Find(GameplayArtLayout.LeftWorldBoundary.Name)?.GetComponent<BoxCollider2D>();
        GameplayArtLayout.BoundarySpec boundarySpec = GameplayArtLayout.LeftWorldBoundary;
        if (leftBoundary == null || leftBoundary.isTrigger ||
            Mathf.Abs(leftBoundary.bounds.max.x - boundarySpec.InnerFaceWorldX) > .01f ||
            Mathf.Abs(leftBoundary.bounds.min.y - boundarySpec.BottomWorld) > .01f ||
            Mathf.Abs(leftBoundary.bounds.max.y - boundarySpec.TopWorld) > .01f)
            throw new System.Exception("Left map boundary is absent or does not match its authored world edge.");
        int hazardLayer = LayerMask.NameToLayer("Hazard");
        if (hazardLayer < 0) throw new System.Exception("Hazard layer is missing.");
        foreach (var hazard in Object.FindObjectsByType<HazardZone>(FindObjectsSortMode.None))
        {
            var collider = hazard.GetComponent<BoxCollider2D>();
            if (hazard.gameObject.layer != hazardLayer || collider == null || !collider.isTrigger)
                throw new System.Exception("Hazard physics setup is invalid: " + hazard.name);
        }
        // Trigger dimensions are measured from individual sharp/flame source
        // rectangles, not inferred from the deleted continuous floor.
        foreach (GameplayArtLayout.TriggerSpec hazardSpec in GameplayArtLayout.Hazards)
        {
            var hazard = GameObject.Find(hazardSpec.Name).GetComponent<BoxCollider2D>();
            float width = hazard.size.x * hazard.transform.lossyScale.x;
            if (width > 3.25f)
                throw new System.Exception("Hazard trigger is too wide to jump: " + hazardSpec.Name);
        }
        var fallHazard = GameObject.Find(GameplayArtLayout.FallHazard.Name)?.GetComponent<BoxCollider2D>();
        if (fallHazard == null || !fallHazard.isTrigger ||
            fallHazard.gameObject.layer != hazardLayer ||
            !BoundsClose(fallHazard.bounds, GameplayArtLayout.FallHazard.ExpectedBounds))
            throw new System.Exception("Out-of-map fall trigger is absent or misaligned.");
        const float FallEdgeMargin = 15f;
        if (fallHazard.bounds.min.x > GameplayArtLayout.PixelToWorldX(0, 0f) - FallEdgeMargin ||
            fallHazard.bounds.max.x < GameplayArtLayout.PixelToWorldX(2, GameplayArtLayout.NativeWidthPixels) + FallEdgeMargin ||
            fallHazard.bounds.max.y >= GameplayArtLayout.PixelToWorldY(GameplayArtLayout.NativeHeightPixels))
            throw new System.Exception("Out-of-map fall trigger must cover both map edges below the painted artwork.");
        float lastCheckpointX = float.NegativeInfinity;
        foreach (GameplayArtLayout.CheckpointSpec checkpoint in GameplayArtLayout.Checkpoints)
        {
            float checkpointX = GameObject.Find(checkpoint.Name).transform.position.x;
            if (checkpointX <= lastCheckpointX) throw new System.Exception("Door checkpoint order is invalid: " + checkpoint.Name);
            lastCheckpointX = checkpointX;
        }
        var follow = Object.FindFirstObjectByType<HorizontalCameraFollow>();
        if (follow == null || follow.Target != tomato.transform || Mathf.Abs(follow.MinX - GameplayArtLayout.DefaultCameraMinX) > .01f || Mathf.Abs(follow.MaxX - GameplayArtLayout.DefaultCameraMaxX) > .01f)
            throw new System.Exception("Camera follow configuration is invalid.");
        var camera = Object.FindFirstObjectByType<Camera>();
        // Batchmode reports a transient 1:1 camera aspect.  The shipped game
        // view is explicitly 1280x720, so validate against that authored
        // aspect rather than making the door visibility check batch-dependent.
        float halfWidth = GameplayArtLayout.DefaultCameraHalfWidth;
        float spawnViewLeft = camera.transform.position.x - halfWidth;
        float spawnViewRight = camera.transform.position.x + halfWidth;
        if (tomatoCollider.bounds.min.x < spawnViewLeft + .1f || tomatoCollider.bounds.max.x > spawnViewRight - .1f)
            throw new System.Exception("Green-door tomato spawn is not fully visible in the initial camera view.");
        var ambients = Object.FindObjectsByType<AmbientSpriteAnimator>(FindObjectsSortMode.None);
        int fireAnimatorCount = 0;
        foreach (var ambient in ambients)
        {
            var renderer = ambient.GetComponent<SpriteRenderer>();
            if (ambient.Frames == null || ambient.Frames.Length != 8 || ambient.GetComponent<Collider2D>() != null ||
                renderer.sortingLayerName != "Ambient" || renderer.sortingOrder != 10 || renderer.sharedMaterial == null ||
                renderer.sharedMaterial.shader == null || renderer.sharedMaterial.shader.name != "ImmortalTomato/AmbientChromaKey")
                throw new System.Exception("Ambient object is invalid: " + ambient.name);
            var firePlacement = ambient.GetComponent<AnimatedFirePlacement>();
            if (firePlacement == null) continue;
            fireAnimatorCount++;
            foreach (Sprite frame in ambient.Frames)
                if (frame == null || !frame.name.StartsWith("Flame_"))
                    throw new System.Exception("Animated fire does not use all eight Flame frames: " + ambient.name);
            Bounds target = firePlacement.TargetBounds;
            Bounds keyed = firePlacement.KeyedVisibleBounds;
            if (keyed.min.x < target.min.x - .025f || keyed.max.x > target.max.x + .025f ||
                keyed.min.y < target.min.y - .025f || keyed.max.y > target.max.y + .025f)
                throw new System.Exception("Animated fire keyed pixels are outside their painted firebox: " + ambient.name);
        }
        int expectedFireAnimators = 0;
        foreach (GameplayArtLayout.FireSpec fire in GameplayArtLayout.Fires)
        {
            int expectedSlots = AnimatedFirePlacement.SlotsFor(fire);
            expectedFireAnimators += expectedSlots;
            GameObject trigger = GameObject.Find(fire.Name);
            var collider = trigger != null ? trigger.GetComponent<BoxCollider2D>() : null;
            var placements = trigger != null ? trigger.GetComponentsInChildren<AnimatedFirePlacement>(true) : null;
            if (collider == null || !collider.isTrigger || collider.gameObject.layer != hazardLayer ||
                collider.GetComponent<HazardZone>() == null || !BoundsClose(collider.bounds, fire.TriggerBounds) ||
                placements == null || placements.Length != expectedSlots)
                throw new System.Exception("Measured animated fire or matching kill trigger is missing: " + fire.Name);
            if (fire.TriggerBounds.min.x < fire.TargetBounds.min.x - .025f || fire.TriggerBounds.max.x > fire.TargetBounds.max.x + .025f ||
                fire.TriggerBounds.min.y < fire.TargetBounds.min.y - .025f || fire.TriggerBounds.max.y > fire.TargetBounds.max.y + .025f)
                throw new System.Exception("Fire kill trigger escapes the painted flame region: " + fire.Name);
            if (Physics2D.Distance(tomatoCollider, collider).distance <= .01f)
                throw new System.Exception("Tomato spawn begins inside an animated fire trigger: " + fire.Name);
        }
        if (fireAnimatorCount != expectedFireAnimators)
            throw new System.Exception($"Animated fire renderer count is wrong: {fireAnimatorCount}/{expectedFireAnimators}");
        int backgroundOrder = SortingLayer.GetLayerValueFromName("Background");
        int ambientOrder = SortingLayer.GetLayerValueFromName("Ambient");
        int gameplayOrder = SortingLayer.GetLayerValueFromName("Default");
        if (!(backgroundOrder < ambientOrder && ambientOrder < gameplayOrder))
            throw new System.Exception("Sorting layers must be Background < Ambient < Default.");
        if (GameObject.Find("S1 Hanging Pan").transform.position.x < -50.49f || GameObject.Find("S1 Hanging Pan").transform.position.x > -16.83f)
            throw new System.Exception("S1 hanging pan is outside its panorama.");
        ColliderAlignmentAudit.WriteAndValidate();
        Debug.Log("Main scene validation passed.");
    }

    public static void Build()
    {
        if (!File.Exists(ScenePath)) CreateScene();
        EnsureCombatHudUziImport();
        UziFxAssetValidation.ValidateHudSprite();
        const string menuScenePath = "Assets/MainMenu.unity";
        if (!File.Exists(menuScenePath)) throw new System.Exception("Main menu scene is missing: " + menuScenePath);
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene(menuScenePath, true),
            new EditorBuildSettingsScene(ScenePath, true)
        };
        PlayerSettings.productName = "Immortal Tomato Animation Playground";
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        var output = Path.GetFullPath("Build/WebGL");
        Directory.CreateDirectory(output);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { menuScenePath, ScenePath }, locationPathName = output,
            target = BuildTarget.WebGL, options = BuildOptions.None
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("WebGL build failed: " + report.summary.result);
        Debug.Log($"WebGL build complete: {output} ({report.summary.totalSize} bytes)");
    }

    static bool BoundsClose(Bounds actual, Bounds expected) =>
        Mathf.Abs(actual.min.x - expected.min.x) <= .01f &&
        Mathf.Abs(actual.max.x - expected.max.x) <= .01f &&
        Mathf.Abs(actual.min.y - expected.min.y) <= .01f &&
        Mathf.Abs(actual.max.y - expected.max.y) <= .01f;
}
