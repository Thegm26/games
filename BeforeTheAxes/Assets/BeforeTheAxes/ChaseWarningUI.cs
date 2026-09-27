using UnityEngine;

namespace BeforeTheAxes
{
    /// <summary>
    /// A warm urgency tint shown only while a woodcutter has direct sight and is chasing.
    /// It intentionally has no text, icons, brackets, or edge geometry.
    /// </summary>
    public sealed class ChaseWarningUI : MonoBehaviour
    {
        private const float PulseSpeed = 4.25f;
        // A single transparent wash is stronger than the old edge treatment while
        // still leaving the low-poly forest readable during a chase.
        private const float MinimumAlpha = .19f;
        private const float MaximumAlpha = .32f;
        private static readonly Color ChaseTint = new Color(1f, .165f, .055f, 1f);

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
            CurrentOutlineThickness = 0f;
            CurrentGlowDepth = 0f;
        }

        private void OnGUI()
        {
            if (!IsWarningVisible || Event.current.type != EventType.Repaint)
                return;

            Color previousColor = GUI.color;
            GUI.color = new Color(ChaseTint.r, ChaseTint.g, ChaseTint.b, CurrentOutlineAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }
    }
}
