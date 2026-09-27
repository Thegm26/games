using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Offline source-frame audit.  It is intentionally batch-safe and uses the
/// exact readable PNGs loaded by TomatoGame, so animation continuity is
/// measured before physics/gameplay can hide a bad source frame.
/// </summary>
public static class TomatoFrameGeometryAudit
{
    static readonly string[] Clips = { "Idle", "Run", "Jump", "Shoot", "Hit", "Regen" };
    static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    static string OutputPath => Path.Combine(ProjectRoot, "QA", "Runtime", "frame_geometry_audit.log");

    public static void Run()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath)!);
        var lines = new List<string> { "Immortal Tomato source-frame geometry audit " + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) };
        var clipFrames = new Dictionary<string, Texture2D[]>();
        foreach (string clip in Clips)
        {
            var textures = Resources.LoadAll<Texture2D>($"Frames/{clip}");
            Array.Sort(textures, (a, b) => string.CompareOrdinal(a.name, b.name));
            if (clip == "Idle") textures = Array.FindAll(textures, t => t.name is "Idle_05" or "Idle_06" or "Idle_07" or "Idle_08");
            clipFrames[clip] = textures;
            lines.Add($"[CLIP] {clip} frames={textures.Length}");
            TomatoFrameGeometry? previous = null;
            for (int i = 0; i < textures.Length; i++)
            {
                TomatoFrameGeometry g = TomatoFrameGeometry.Measure(textures[i]);
                string continuity = "first";
                if (previous.HasValue)
                {
                    var p = previous.Value;
                    Vector2 anchorDelta = g.coreCenter - p.coreCenter;
                    Vector2 centroidDelta = g.centroid - p.centroid;
                    float widthRatio = g.size.x / Mathf.Max(.01f, p.size.x);
                    float heightRatio = g.size.y / Mathf.Max(.01f, p.size.y);
                    float areaRatio = g.opaquePixels / Mathf.Max(1f, p.opaquePixels);
                    continuity = $"fromPrev anchorΔ={F(anchorDelta)} centroidΔ={F(centroidDelta)} bboxScale=({widthRatio:0.###},{heightRatio:0.###}) areaScale={areaRatio:0.###}";
                }
                lines.Add($"  {textures[i].name} bbox={F(g.min)}..{F(g.max)} size={F(g.size)} centroid={F(g.centroid)} core={F(g.coreCenter)} coreSize={F(g.coreSize)} foot={F(g.footCenter)} opaque={g.opaquePixels} {continuity}");
                previous = g;
            }
        }
        // The action boundary is where a frame-sequence audit alone is blind:
        // score every planted Idle frame against every Shoot frame after their
        // tomato-body anchors are registered.  This is source evidence for
        // choosing an entry pose instead of guessing from file order.
        lines.Add("[BOUNDARY] Idle->Shoot registered-alpha continuity");
        foreach (Texture2D idle in clipFrames["Idle"])
        {
            TomatoFrameGeometry idleGeometry = TomatoFrameGeometry.Measure(idle);
            foreach (Texture2D shoot in clipFrames["Shoot"])
            {
                TomatoFrameGeometry shootGeometry = TomatoFrameGeometry.Measure(shoot);
                float iou = TomatoFrameGeometry.RegisteredAlphaIou(idle, idleGeometry, shoot, shootGeometry);
                Vector2 bodyDelta = (shootGeometry.coreCenter - idleGeometry.coreCenter) / 100f;
                Vector2 footDelta = (shootGeometry.footCenter - shootGeometry.coreCenter - (idleGeometry.footCenter - idleGeometry.coreCenter)) / 100f;
                lines.Add($"  {idle.name}->{shoot.name} iou={iou:0.###} sourceBodyDelta={F(bodyDelta)} sourceFootDelta={F(footDelta)}");
            }
        }
        File.WriteAllLines(OutputPath, lines);
        Debug.Log($"[TOMATO-GEOMETRY] wrote {OutputPath}");
    }

    static string F(Vector2 value) => $"({value.x:0.##},{value.y:0.##})";
}
