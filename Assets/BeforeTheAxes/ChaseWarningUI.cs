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

        private int activeChaserCount;

        public bool IsWarningVisible => activeChaserCount > 0;
        public int ActiveChaserCount => activeChaserCount;
        public float CurrentOutlineAlpha { get; private set; }
        public float CurrentOutlineThickness { get; private set; }

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
        }

        private void OnGUI()
        {
            if (!IsWarningVisible || Event.current.type != EventType.Repaint)
                return;

            float thickness = CurrentOutlineThickness;
            float cornerLength = Mathf.Max(thickness * 3.5f, 26f);
            float width = Screen.width;
            float height = Screen.height;

            // Four restrained corner brackets sell danger without masking the low-poly world.
            Color previousColor = GUI.color;
            GUI.color = new Color(.93f, .075f, .035f, CurrentOutlineAlpha);

            DrawCorner(0f, 0f, 1f, 1f, cornerLength, thickness);
            DrawCorner(width, 0f, -1f, 1f, cornerLength, thickness);
            DrawCorner(0f, height, 1f, -1f, cornerLength, thickness);
            DrawCorner(width, height, -1f, -1f, cornerLength, thickness);

            GUI.color = previousColor;
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
    }
}
