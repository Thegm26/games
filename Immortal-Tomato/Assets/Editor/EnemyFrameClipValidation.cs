using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Explicit asset validation for the authored full-canvas enemy clips. This is
/// intentionally separate from Main-scene validation: gameplay can continue
/// to use the original Enemies sprites while frame art is being produced.
/// </summary>
public static class EnemyFrameClipValidation
{
    readonly struct Clip
    {
        public readonly string resourceFolder;
        public readonly string prefix;

        public Clip(string resourceFolder, string prefix)
        {
            this.resourceFolder = resourceFolder;
            this.prefix = prefix;
        }
    }

    static readonly Clip[] Clips =
    {
        new("EnemyFrames/Chef/Idle", "Idle"),
        new("EnemyFrames/Chef/Throw", "Throw"),
        new("EnemyFrames/Chef/Cleaver", "Cleaver"),
        new("EnemyFrames/Waiter/Idle", "Idle"),
        new("EnemyFrames/Waiter/Run", "Run")
    };

    [MenuItem("Immortal Tomato/Validate Enemy Frame Clips")]
    public static void ValidateEnemyFrames()
    {
        var errors = new List<string>();
        foreach (Clip clip in Clips)
        {
            Texture2D[] frames = Resources.LoadAll<Texture2D>(clip.resourceFolder);
            Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
            if (!EnemySpriteLoader.IsCompleteClip(frames, clip.prefix, out string reason))
                errors.Add($"Resources/{clip.resourceFolder}: {reason} (required names: {clip.prefix}_01 through {clip.prefix}_08).");
        }

        if (errors.Count > 0)
            throw new InvalidOperationException("Enemy full-frame validation failed:\n" + string.Join("\n", errors));

        Debug.Log("Enemy full-frame validation passed: five complete, ordered eight-frame clips loaded from Resources.");
    }
}
