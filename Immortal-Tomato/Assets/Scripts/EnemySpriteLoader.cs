using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy art may arrive as either a Sprite import or a plain transparent
/// Texture2D.  Creating the sprite here keeps the gameplay code independent
/// of importer timing and gives character sheets a stable feet-on-root pivot.
/// </summary>
public static class EnemySpriteLoader
{
    public const int ClipFrameCount = 8;

    static readonly HashSet<string> WarnedIncompleteClips = new();

    public static Sprite Load(string resourcePath, Vector2 pivot)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Sprite importedSprite = Resources.Load<Sprite>(resourcePath);
            texture = importedSprite != null ? importedSprite.texture : null;
        }
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), pivot, 100f);
    }

    /// <summary>
    /// Loads a complete, ordered full-canvas clip. A clip is deliberately
    /// rejected as a whole when it is incomplete: mixing a few new full frames
    /// with the legacy pose art creates a much more visible pop than the safe
    /// legacy fallback does.
    /// </summary>
    public static Sprite[] LoadFrames(string resourceFolder, string framePrefix, Vector2 pivot)
    {
        Texture2D[] textures = Resources.LoadAll<Texture2D>(resourceFolder);
        Array.Sort(textures, (a, b) => string.CompareOrdinal(a.name, b.name));
        if (!IsCompleteClip(textures, framePrefix, out string reason))
        {
            WarnIncompleteClip(resourceFolder, reason);
            return Array.Empty<Sprite>();
        }

        var frames = new Sprite[ClipFrameCount];
        for (int i = 0; i < frames.Length; i++)
        {
            Texture2D texture = textures[i];
            // Every source image uses the same full canvas and feet-baseline
            // pivot. The renderer can therefore change frames without ever
            // shifting the trigger/collider root to compensate for padding.
            frames[i] = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), pivot, 100f);
        }
        return frames;
    }

    public static bool HasCompleteClip(string resourceFolder, string framePrefix)
    {
        Texture2D[] textures = Resources.LoadAll<Texture2D>(resourceFolder);
        Array.Sort(textures, (a, b) => string.CompareOrdinal(a.name, b.name));
        return IsCompleteClip(textures, framePrefix, out _);
    }

    public static bool IsCompleteClip(Texture2D[] textures, string framePrefix, out string reason)
    {
        if (textures == null || textures.Length != ClipFrameCount)
        {
            reason = $"expected {ClipFrameCount} frames, found {textures?.Length ?? 0}";
            return false;
        }

        int width = textures[0] != null ? textures[0].width : 0;
        int height = textures[0] != null ? textures[0].height : 0;
        for (int i = 0; i < ClipFrameCount; i++)
        {
            Texture2D texture = textures[i];
            string expectedName = $"{framePrefix}_{i + 1:00}";
            if (texture == null || texture.name != expectedName)
            {
                reason = $"expected ordered frame {expectedName}";
                return false;
            }
            if (texture.width != width || texture.height != height)
            {
                reason = $"{texture.name} is {texture.width}x{texture.height}, not the common {width}x{height} full canvas";
                return false;
            }
        }

        reason = null;
        return true;
    }

    static void WarnIncompleteClip(string resourceFolder, string reason)
    {
        if (!WarnedIncompleteClips.Add(resourceFolder)) return;
        Debug.LogWarning($"Enemy frame clip Resources/{resourceFolder} is incomplete ({reason}); using legacy Enemies sprites when available.");
    }
}
