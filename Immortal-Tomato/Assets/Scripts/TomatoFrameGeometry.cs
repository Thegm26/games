using System;
using UnityEngine;

/// <summary>
/// Alpha-derived geometry for one source animation frame.  The fields named
/// "core" intentionally ignore a frame's extremities as much as possible:
/// guns, hands, one raised boot, and anti-aliased single pixels must not be
/// allowed to steer the entire character from frame to frame.
/// </summary>
public readonly struct TomatoFrameGeometry
{
    public readonly bool readable;
    public readonly Vector2 min;
    public readonly Vector2 max;
    public readonly Vector2 centroid;
    public readonly Vector2 coreCenter;
    public readonly Vector2 coreMin;
    public readonly Vector2 coreMax;
    public readonly Vector2 footCenter;
    public readonly float footY;
    public readonly int opaquePixels;
    public readonly int bodyPixels;

    public TomatoFrameGeometry(
        bool readable,
        Vector2 min,
        Vector2 max,
        Vector2 centroid,
        Vector2 coreCenter,
        Vector2 coreMin,
        Vector2 coreMax,
        Vector2 footCenter,
        float footY,
        int opaquePixels,
        int bodyPixels)
    {
        this.readable = readable;
        this.min = min;
        this.max = max;
        this.centroid = centroid;
        this.coreCenter = coreCenter;
        this.coreMin = coreMin;
        this.coreMax = coreMax;
        this.footCenter = footCenter;
        this.footY = footY;
        this.opaquePixels = opaquePixels;
        this.bodyPixels = bodyPixels;
    }

    public Vector2 size => max - min;
    public Vector2 coreSize => coreMax - coreMin;
    public Vector2 coreToFoot => coreCenter - footCenter;

    /// <summary>
    /// Binary alpha-mask IoU after translating <paramref name="next"/> so its
    /// measured body anchor sits on <paramref name="previous"/>'s anchor.
    /// This measures the actual silhouette, not merely two rectangles.
    /// </summary>
    public static float RegisteredAlphaIou(Texture2D previousTexture, TomatoFrameGeometry previous, Texture2D nextTexture, TomatoFrameGeometry next)
    {
        if (!previous.readable || !next.readable || previousTexture == null || nextTexture == null) return 0f;
        try
        {
            Color[] a = previousTexture.GetPixels();
            Color[] b = nextTexture.GetPixels();
            int aWidth = previousTexture.width, aHeight = previousTexture.height;
            int bWidth = nextTexture.width, bHeight = nextTexture.height;
            int shiftX = Mathf.RoundToInt(previous.coreCenter.x - next.coreCenter.x);
            int shiftY = Mathf.RoundToInt(previous.coreCenter.y - next.coreCenter.y);
            int minX = Mathf.Min(0, shiftX);
            int minY = Mathf.Min(0, shiftY);
            int maxX = Mathf.Max(aWidth, shiftX + bWidth);
            int maxY = Mathf.Max(aHeight, shiftY + bHeight);
            int intersection = 0, union = 0;
            const float threshold = .08f;
            for (int y = minY; y < maxY; y++)
            for (int x = minX; x < maxX; x++)
            {
                bool aOpaque = x >= 0 && x < aWidth && y >= 0 && y < aHeight && a[y * aWidth + x].a >= threshold;
                int bx = x - shiftX, by = y - shiftY;
                bool bOpaque = bx >= 0 && bx < bWidth && by >= 0 && by < bHeight && b[by * bWidth + bx].a >= threshold;
                if (aOpaque || bOpaque) union++;
                if (aOpaque && bOpaque) intersection++;
            }
            return union == 0 ? 0f : intersection / (float)union;
        }
        catch (UnityException)
        {
            return 0f;
        }
    }

    public static TomatoFrameGeometry Measure(Texture2D texture)
    {
        if (texture == null) return default;
        try
        {
            int width = texture.width;
            int height = texture.height;
            Color[] pixels = texture.GetPixels();
            const float threshold = .08f;
            int minX = width, minY = height, maxX = -1, maxY = -1;
            double sumX = 0d, sumY = 0d, sumWeight = 0d;
            int count = 0;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float alpha = pixels[y * width + x].a;
                if (alpha < threshold) continue;
                minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                sumX += x * alpha; sumY += y * alpha; sumWeight += alpha;
                count++;
            }
            if (count == 0) return new TomatoFrameGeometry(false, Vector2.zero, new Vector2(width, height), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, 0f, 0, 0);

            // A 6%-tall foot band is deliberately robust against a lone
            // anti-aliased toe at the lowest row.  Its weighted centre makes
            // a two-boot stance stable without anchoring it to either boot.
            int visibleHeight = maxY - minY + 1;
            int footBandTop = minY + Mathf.Max(2, Mathf.RoundToInt(visibleHeight * .06f));
            double footX = 0d, footY = 0d, footWeight = 0d;
            for (int y = minY; y <= Mathf.Min(maxY, footBandTop); y++)
            for (int x = minX; x <= maxX; x++)
            {
                float alpha = pixels[y * width + x].a;
                if (alpha < threshold) continue;
                footX += x * alpha; footY += y * alpha; footWeight += alpha;
            }
            Vector2 robustFoot = footWeight > 0d
                ? new Vector2((float)(footX / footWeight), (float)(footY / footWeight))
                : new Vector2((minX + maxX) * .5f, minY);

            // Register by the red tomato body whenever the source contains a
            // usable body mask.  Alpha-only registration sees hands, guns and
            // a raised boot as part of the character and caused exactly the
            // large "character switches" visible in the old Shoot clip.
            // The fallback uses a dense middle band for frames whose red body
            // is deliberately obscured (Hit / Regen effects).
            int coreLow = minY + Mathf.RoundToInt(visibleHeight * .31f);
            int coreHigh = minY + Mathf.RoundToInt(visibleHeight * .73f);
            int[] rowCounts = new int[Mathf.Max(1, coreHigh - coreLow + 1)];
            var coreXs = new System.Collections.Generic.List<float>();
            var coreYs = new System.Collections.Generic.List<float>();
            for (int y = coreLow; y <= coreHigh; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Color color = pixels[y * width + x];
                float alpha = color.a;
                if (alpha < threshold) continue;
                // Tomato red/orange is both saturated and red-dominant.  Do
                // not let dark Uzi pixels, the white chef effects, or green
                // leaves move the core anchor.  A little orange is allowed so
                // highlights remain part of the fruit.
                bool tomatoBody = color.r >= .24f &&
                                  color.r >= color.g * 1.16f &&
                                  color.r >= color.b * 1.30f &&
                                  (color.r - Mathf.Min(color.g, color.b)) >= .12f;
                if (!tomatoBody) continue;
                // Duplicate proportional to opacity only in a light-weight
                // way.  It preserves meaningful opaque body pixels without
                // letting semi-transparent smoke dominate the result.
                int repetitions = alpha >= .66f ? 3 : alpha >= .33f ? 2 : 1;
                for (int n = 0; n < repetitions; n++)
                {
                    coreXs.Add(x);
                    coreYs.Add(y);
                }
            }
            int bodyPixelCount = coreXs.Count;
            if (coreXs.Count < 48)
            {
                coreXs.Clear(); coreYs.Clear();
                for (int y = coreLow; y <= coreHigh; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    float alpha = pixels[y * width + x].a;
                    if (alpha < threshold) continue;
                    int repetitions = alpha >= .66f ? 3 : alpha >= .33f ? 2 : 1;
                    for (int n = 0; n < repetitions; n++)
                    {
                        coreXs.Add(x);
                        coreYs.Add(y);
                    }
                }
            }
            if (coreXs.Count == 0)
            {
                coreXs.Add((minX + maxX) * .5f);
                coreYs.Add((minY + maxY) * .5f);
            }
            coreXs.Sort(); coreYs.Sort();
            int trim = Mathf.FloorToInt(coreXs.Count * .14f);
            int begin = Mathf.Clamp(trim, 0, coreXs.Count - 1);
            int end = Mathf.Clamp(coreXs.Count - trim, begin + 1, coreXs.Count);
            double coreX = 0d, coreY = 0d;
            for (int i = begin; i < end; i++) { coreX += coreXs[i]; coreY += coreYs[i]; }
            int trimmedCount = end - begin;
            Vector2 core = new Vector2((float)(coreX / trimmedCount), (float)(coreY / trimmedCount));

            // A compact core bound around the same 14–86% trimmed samples is
            // the geometry compared with the fixed gameplay capsule.
            Vector2 coreMin = new(coreXs[begin], coreYs[begin]);
            Vector2 coreMax = new(coreXs[end - 1] + 1f, coreYs[end - 1] + 1f);
            return new TomatoFrameGeometry(
                true,
                new Vector2(minX, minY), new Vector2(maxX + 1, maxY + 1),
                new Vector2((float)(sumX / sumWeight), (float)(sumY / sumWeight)),
                core, coreMin, coreMax, robustFoot, robustFoot.y, count, bodyPixelCount);
        }
        catch (UnityException)
        {
            return default;
        }
    }
}

/// <summary>
/// Source-space gun measurements used only for the human-readable alignment
/// audit.  They deliberately describe the dark/metal right-hand weapon area,
/// not the tomato body registration anchor.
/// </summary>
public readonly struct TomatoGunMuzzleGeometry
{
    public readonly bool readable;
    public readonly Vector2 min;
    public readonly Vector2 max;
    public readonly Vector2 upperMuzzle;
    public readonly Vector2 lowerMuzzle;

    public TomatoGunMuzzleGeometry(bool readable, Vector2 min, Vector2 max, Vector2 upperMuzzle, Vector2 lowerMuzzle)
    {
        this.readable = readable;
        this.min = min;
        this.max = max;
        this.upperMuzzle = upperMuzzle;
        this.lowerMuzzle = lowerMuzzle;
    }
}

public static class TomatoFrameVisualMeasurements
{
    /// <summary>
    /// Finds the two far-right dark Uzi tips.  The result is an audit aid, so
    /// it is conservative: smoke and red tomato pixels are excluded and the
    /// rightmost weapon rows choose the muzzle bands.
    /// </summary>
    public static TomatoGunMuzzleGeometry MeasureGunMuzzles(Texture2D texture, TomatoFrameGeometry body)
    {
        if (texture == null || !body.readable) return default;
        try
        {
            int width = texture.width, height = texture.height;
            Color[] pixels = texture.GetPixels();
            int startX = Mathf.Clamp(Mathf.FloorToInt(body.coreCenter.x + body.coreSize.x * .38f), 0, width - 1);
            int[] rightByRow = new int[height];
            int minX = width, minY = height, maxX = -1, maxY = -1;
            for (int y = 0; y < height; y++)
            {
                rightByRow[y] = -1;
                for (int x = startX; x < width; x++)
                {
                    Color color = pixels[y * width + x];
                    float hi = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
                    float lo = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
                    bool metal = color.a >= .25f && hi <= .78f && hi - lo <= .22f;
                    if (!metal) continue;
                    rightByRow[y] = x;
                    minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                }
            }
            if (maxX < 0) return default;
            // The two guns overlap through the hands in some frames, so a
            // connected-component approach can merge them.  Their muzzle tips
            // remain cleanly separated above/below the tomato core instead.
            int splitY = Mathf.Clamp(Mathf.RoundToInt(body.coreCenter.y), 1, height - 1);
            Vector2 lower = MeasureMuzzleBand(pixels, width, startX, rightByRow, 0, splitY, new Vector2(maxX, minY));
            Vector2 upper = MeasureMuzzleBand(pixels, width, startX, rightByRow, splitY, height, new Vector2(maxX, maxY));
            return new TomatoGunMuzzleGeometry(true, new Vector2(minX, minY), new Vector2(maxX + 1, maxY + 1), upper, lower);
        }
        catch (UnityException)
        {
            return default;
        }
    }

    static Vector2 MeasureMuzzleBand(Color[] pixels, int width, int startX, int[] rightByRow, int rowStart, int rowEnd, Vector2 fallback)
    {
        int right = -1;
        for (int y = rowStart; y < rowEnd; y++) right = Mathf.Max(right, rightByRow[y]);
        if (right < 0) return fallback;
        int threshold = right - 8;
        double sumX = 0d, sumY = 0d;
        int count = 0;
        for (int y = rowStart; y < rowEnd; y++)
        for (int x = Mathf.Max(startX, threshold); x <= right; x++)
        {
            if (rightByRow[y] < x) continue;
            Color color = pixels[y * width + x];
            float hi = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float lo = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            bool metal = color.a >= .25f && hi <= .78f && hi - lo <= .22f;
            if (!metal) continue;
            sumX += x;
            sumY += y;
            count++;
        }
        return count == 0 ? new Vector2(right, (rowStart + rowEnd) * .5f) : new Vector2((float)(sumX / count), (float)(sumY / count));
    }
}

/// <summary>Continuity evidence for one complete playable clip.</summary>
public readonly struct TomatoClipGeometryAudit
{
    public readonly string clip;
    public readonly int frameCount;
    public readonly Vector2 legacyFootAnchoredCoreRangePixels;
    public readonly Vector2 coreAnchoredCoreRangePixels;
    public readonly float maxAnchorStepPixels;
    public readonly float maxBboxScaleStep;
    public readonly float maxAreaScaleStep;
    public readonly float minimumRegisteredAlphaIou;
    public readonly bool identityPass;

    public TomatoClipGeometryAudit(
        string clip,
        int frameCount,
        Vector2 legacyFootAnchoredCoreRangePixels,
        Vector2 coreAnchoredCoreRangePixels,
        float maxAnchorStepPixels,
        float maxBboxScaleStep,
        float maxAreaScaleStep,
        float minimumRegisteredAlphaIou,
        bool identityPass)
    {
        this.clip = clip;
        this.frameCount = frameCount;
        this.legacyFootAnchoredCoreRangePixels = legacyFootAnchoredCoreRangePixels;
        this.coreAnchoredCoreRangePixels = coreAnchoredCoreRangePixels;
        this.maxAnchorStepPixels = maxAnchorStepPixels;
        this.maxBboxScaleStep = maxBboxScaleStep;
        this.maxAreaScaleStep = maxAreaScaleStep;
        this.minimumRegisteredAlphaIou = minimumRegisteredAlphaIou;
        this.identityPass = identityPass;
    }
}
