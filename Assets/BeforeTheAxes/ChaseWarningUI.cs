using UnityEngine;

namespace BeforeTheAxes
{
    /// <summary>
    /// A quiet, screen-edge danger cue shown only while a woodcutter has direct sight and is chasing.
    /// It intentionally has no text or centre fill so the player can keep reading the forest.
    /// </summary>
    public sealed class ChaseWarningUI : MonoBehaviour
    {
        private const float MinimumEdgePixels = 5f;
        private const float MaximumEdgePixels = 15f;
        private const float PulseSpeed = 5.5f;
        private const float MinimumAlpha = .13f;
        private const float MaximumAlpha = .31f;
        private const float MinimumGlowDepthPixels = 32f;
        private const float MaximumGlowDepthPixels = 76f;
        private const int GlowBands = 6;

        private int activeChaserCount;

        public bool IsWarningVisible => activeChaserCount > 0;
        public int ActiveChaserCount => activeChaserCount;
        public float CurrentOutlineAlpha { get; private set; }
        public float CurrentOutlineThickness { get; private set; }
        public float CurrentGlowDepth { get; private set; }

        private void Update()
        {
            int count = 0;
            WoodcutterAI[] woodcutters = FindObjectsByType<WoodcutterAI>(FindObjectsSortMode.None);
            foreach (WoodcutterAI woodcutter in woodcutters)
            {
                if (woodcutter != null && woodcutter.isActiveAndEnabled &&
                    woodcutter.State == WoodcutterAI.AlertState.Chase && woodcutter.HasDirectSight)
                    count++;
            }
            activeChaserCount = count;
            float pulse = (Mathf.Sin(Time.unscaledTime * PulseSpeed) + 1f) * .5f;
            CurrentOutlineAlpha = Mathf.Lerp(MinimumAlpha, MaximumAlpha, pulse);
            CurrentOutlineThickness = CalculateOutlineThickness(Screen.width, Screen.height);
            CurrentGlowDepth = CalculateGlowDepth(Screen.width, Screen.height);
        }

        private void OnGUI()
        {
            if (!IsWarningVisible || Event.current.type != EventType.Repaint)
                return;

            float thickness = CurrentOutlineThickness;
            float cornerLength = Mathf.Max(thickness * 3.5f, 26f);
            float width = Screen.width;
            float height = Screen.height;

            Color previousColor = GUI.color;

            // A warm, transparent edge glow: the middle of the screen remains completely clear.
            // Stacked bands are deliberately texture-free, tiny, and WebGL-friendly.
            DrawEdgeGlow(width, height, CurrentGlowDepth, CurrentOutlineAlpha);

            // Four restrained corner brackets sell danger without masking the low-poly world.
            GUI.color = new Color(.93f, .075f, .035f, CurrentOutlineAlpha);

            DrawCorner(0f, 0f, 1f, 1f, cornerLength, thickness);
            DrawCorner(width, 0f, -1f, 1f, cornerLength, thickness);
            DrawCorner(0f, height, 1f, -1f, cornerLength, thickness);
            DrawCorner(width, height, -1f, -1f, cornerLength, thickness);

            GUI.color = previousColor;
        }

        private static void DrawEdgeGlow(float width, float height, float depth, float pulseAlpha)
        {
            for (int band = 0; band < GlowBands; band++)
            {
                float progress = band / (float)GlowBands;
                float bandDepth = depth / GlowBands + 1f;
                float inset = band * depth / GlowBands;
                // Bright at the outer edge, quickly fading toward the clear centre.
                float alpha = pulseAlpha * .24f * (1f - progress) * (1f - progress);
                GUI.color = new Color(1f, .16f, .045f, alpha);

                GUI.DrawTexture(new Rect(0f, inset, width, bandDepth), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, height - inset - bandDepth, width, bandDepth), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(inset, inset + bandDepth, bandDepth, height - 2f * (inset + bandDepth)), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(width - inset - bandDepth, inset + bandDepth, bandDepth, height - 2f * (inset + bandDepth)), Texture2D.whiteTexture);
            }
        }

        private static void DrawCorner(float x, float y, float horizontalDirection, float verticalDirection,
            float length, float thickness)
        {
            float horizontalX = horizontalDirection > 0f ? x : x - length;
            float verticalY = verticalDirection > 0f ? y : y - length;
            float horizontalY = verticalDirection > 0f ? y : y - thickness;
            float verticalX = horizontalDirection > 0f ? x : x - thickness;

            GUI.DrawTexture(new Rect(horizontalX, horizontalY, length, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(verticalX, verticalY, thickness, length), Texture2D.whiteTexture);
        }

        public static float CalculateOutlineThickness(float screenWidth, float screenHeight)
        {
            float shortestScreenEdge = Mathf.Max(1f, Mathf.Min(screenWidth, screenHeight));
            return Mathf.Clamp(shortestScreenEdge * .009f, MinimumEdgePixels, MaximumEdgePixels);
        }

        public static float CalculateGlowDepth(float screenWidth, float screenHeight)
        {
            float shortestScreenEdge = Mathf.Max(1f, Mathf.Min(screenWidth, screenHeight));
            return Mathf.Clamp(shortestScreenEdge * .047f, MinimumGlowDepthPixels, MaximumGlowDepthPixels);
        }
    }
}
