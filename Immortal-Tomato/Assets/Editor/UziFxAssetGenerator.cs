using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Deterministic tiny pixel frames are more legible and reproducible than a
/// generative-image pass. This produces the source-controlled Uzi FX assets.
/// </summary>
public static class UziFxAssetGenerator
{
    const string FrameDirectory = "Assets/Resources/Frames/UziFx";
    const string AudioDirectory = "Assets/Resources/Audio";
    const string AudioPath = AudioDirectory + "/UziBurst.wav";

    [MenuItem("Immortal Tomato/Generate Uzi FX Assets")]
    public static void Generate()
    {
        Directory.CreateDirectory(FrameDirectory);
        Directory.CreateDirectory(AudioDirectory);
        for (int i = 0; i < 3; i++)
            WritePng(FrameDirectory + "/Bullet_" + (i + 1).ToString("00") + ".png", MakeBullet(i));
        for (int i = 0; i < 4; i++)
            WritePng(FrameDirectory + "/Casing_" + (i + 1).ToString("00") + ".png", MakeCasing(i));
        WriteBurstWav(AudioPath);
        AssetDatabase.Refresh();
        ConfigureImports();
        Debug.Log("Generated deterministic Uzi bullet/casing PNG frames and UziBurst.wav.");
    }

    static Texture2D MakeBullet(int frame)
    {
        var texture = NewTexture(48, 14);
        int trim = frame * 5;
        // Warm trail -> white core -> brass point, all on transparent alpha.
        Fill(texture, 2 + trim, 5, 35 - trim, 8, new Color(1f, .22f, .02f, .38f));
        Fill(texture, 10 + trim, 4, 43 - trim, 9, new Color(1f, .62f, .03f, .88f));
        Fill(texture, 18 + trim, 5, 44 - trim, 8, new Color(1f, .96f, .7f, 1f));
        Fill(texture, 43 - trim, 4, 47 - trim, 9, new Color(.95f, .62f, .16f, 1f));
        texture.Apply(false);
        return texture;
    }

    static Texture2D MakeCasing(int frame)
    {
        var texture = NewTexture(20, 20);
        int shift = frame % 2;
        Fill(texture, 6 + shift, 4, 14 + shift, 15, new Color(.62f, .28f, .04f, 1f));
        Fill(texture, 8 + shift, 4, 15 + shift, 14, new Color(.97f, .64f, .14f, 1f));
        Fill(texture, 9 + shift, 6, 12 + shift, 13, new Color(1f, .85f, .38f, 1f));
        Fill(texture, 5 + shift, 3, 15 + shift, 5, new Color(.48f, .19f, .03f, 1f));
        Fill(texture, 6 + shift, 15, 15 + shift, 17, new Color(1f, .77f, .22f, 1f));
        texture.Apply(false);
        return texture;
    }

    static Texture2D NewTexture(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) {
            filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels(new Color[width * height]);
        return texture;
    }

    static void Fill(Texture2D texture, int xMin, int yMin, int xMax, int yMax, Color color)
    {
        for (int y = Mathf.Max(0, yMin); y <= Mathf.Min(texture.height - 1, yMax); y++)
        for (int x = Mathf.Max(0, xMin); x <= Mathf.Min(texture.width - 1, xMax); x++)
            texture.SetPixel(x, y, color);
    }

    static void WritePng(string path, Texture2D texture)
    {
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    // Original synthesized four-shot rat-tat burst: filtered noise/transient
    // only, with no external recording or copyright dependency.
    static void WriteBurstWav(string path)
    {
        const int sampleRate = 22050;
        const float duration = .28f;
        int count = Mathf.CeilToInt(sampleRate * duration);
        var samples = new short[count];
        uint state = 0x42D00D11u;
        for (int i = 0; i < count; i++)
        {
            float time = i / (float)sampleRate;
            float value = 0f;
            for (int shot = 0; shot < 4; shot++)
            {
                float local = time - shot * .055f;
                if (local < 0f || local > .07f) continue;
                state = state * 1664525u + 1013904223u;
                float noise = ((state >> 8) & 0xffff) / 32767.5f - 1f;
                float envelope = Mathf.Exp(-local * 58f);
                value += (noise * .72f + Mathf.Sin(local * 840f) * .28f) * envelope;
            }
            samples[i] = (short)Mathf.Clamp(value * 13000f, short.MinValue, short.MaxValue);
        }
        using (var stream = File.Create(path))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16); writer.Write((short)1); writer.Write((short)1);
            writer.Write(sampleRate); writer.Write(sampleRate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
            foreach (short sample in samples) writer.Write(sample);
        }
    }

    static void ConfigureImports()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { FrameDirectory }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
    }
}
