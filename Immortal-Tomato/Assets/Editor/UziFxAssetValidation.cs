using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Focused build-time checks for the cosmetic burst asset contract.</summary>
public static class UziFxAssetValidation
{
    const string FrameRoot = "Assets/Resources/Frames/UziFx";
    const string AudioPath = "Assets/Resources/Audio/UziBurst.wav";
    const string HudPath = "Assets/Resources/UI/UziHud.png";

    [MenuItem("Immortal Tomato/Validate Uzi FX Assets")]
    public static void Validate()
    {
        ValidateFrames("Bullet", 3, new Vector2Int(48, 14));
        ValidateFrames("Casing", 4, new Vector2Int(20, 20));
        ValidateHudSprite();
        AudioClip audio = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
        if (audio == null || audio.length < .25f || audio.length > .31f)
            throw new System.Exception("Uzi burst WAV is missing or is not the expected short rat-tat asset.");
        Debug.Log("Uzi FX validation passed: combat frames, burst audio, and transparent HUD Uzi sprite are ready.");
    }

    /// <summary>Validates the authored Uzi extracted for the top-left combat HUD.</summary>
    public static void ValidateHudSprite()
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(HudPath);
        // Unity 6 may expose the Sprite as a subasset rather than the main
        // asset. Resolve both forms just as the runtime loader does.
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HudPath);
        if (sprite == null)
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(HudPath))
                if (asset is Sprite candidate) { sprite = candidate; break; }
        TextureImporter importer = AssetImporter.GetAtPath(HudPath) as TextureImporter;
        var failures = new List<string>();
        if (texture == null) failures.Add("Texture2D=null");
        else if (texture.width != 135 || texture.height != 170) failures.Add($"dimensions={texture.width}x{texture.height}, expected=135x170");
        if (importer == null) failures.Add("TextureImporter=null");
        else
        {
            if (importer.textureType != TextureImporterType.Sprite) failures.Add($"textureType={importer.textureType}, expected=Sprite");
            if (importer.spriteImportMode != SpriteImportMode.Single) failures.Add($"spriteImportMode={importer.spriteImportMode}, expected=Single");
            if (importer.npotScale != TextureImporterNPOTScale.None) failures.Add($"npotScale={importer.npotScale}, expected=None");
            if (!importer.isReadable) failures.Add("isReadable=false, expected=true");
            if (importer.mipmapEnabled) failures.Add("mipmapEnabled=true, expected=false");
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) failures.Add($"textureCompression={importer.textureCompression}, expected=Uncompressed");
            if (importer.filterMode != FilterMode.Point) failures.Add($"filterMode={importer.filterMode}, expected=Point");
            if (!importer.alphaIsTransparency) failures.Add("alphaIsTransparency=false, expected=true");
        }
        if (sprite == null) failures.Add("Sprite=null (main asset and subassets)");
        else if (texture != null && (Mathf.Abs(sprite.rect.width - texture.width) > .01f || Mathf.Abs(sprite.rect.height - texture.height) > .01f))
            failures.Add($"spriteRect={sprite.rect.width}x{sprite.rect.height}, expected={texture.width}x{texture.height}");
        if (failures.Count > 0)
            throw new System.Exception("Combat HUD Uzi validation failed (" + HudPath + "): " + string.Join("; ", failures));

        bool visiblePixel = false;
        bool transparentPixel = false;
        foreach (Color pixel in texture.GetPixels())
        {
            visiblePixel |= pixel.a > .01f;
            transparentPixel |= pixel.a < .01f;
            if (visiblePixel && transparentPixel) break;
        }
        if (!visiblePixel || !transparentPixel)
            throw new System.Exception("Combat HUD Uzi must retain both visible artwork and transparent background: " + HudPath);
    }

    static void ValidateFrames(string prefix, int expectedCount, Vector2Int expectedSize)
    {
        for (int index = 1; index <= expectedCount; index++)
        {
            string path = FrameRoot + "/" + prefix + "_" + index.ToString("00") + ".png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (texture == null || importer == null || texture.width != expectedSize.x || texture.height != expectedSize.y ||
                !importer.isReadable || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.filterMode != FilterMode.Point)
                throw new System.Exception("Invalid Uzi FX frame: " + path);
            bool opaque = false;
            foreach (Color pixel in texture.GetPixels())
                if (pixel.a > .01f) { opaque = true; break; }
            if (!opaque) throw new System.Exception("Uzi FX frame has no visible pixels: " + path);
        }
        if (Directory.GetFiles(FrameRoot, prefix + "_*.png").Length != expectedCount)
            throw new System.Exception("Unexpected " + prefix + " frame count in Uzi FX resources.");
    }
}
