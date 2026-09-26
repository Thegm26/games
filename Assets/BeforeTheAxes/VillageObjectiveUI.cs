using UnityEngine;

namespace BeforeTheAxes
{
    /// <summary>Animated return-to-village instruction shown immediately after the mushroom is collected.</summary>
    public sealed class VillageObjectiveUI : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float displaySeconds = 4.5f;

        private RootMushroomPickup mushroom;
        private float shownAt = -100f;
        private GUIStyle headingStyle;
        private GUIStyle detailStyle;

        public bool IsReturnPromptVisible => Time.unscaledTime < shownAt + displaySeconds;

        private void OnEnable()
        {
            RootMushroomPickup.Collected += OnMushroomCollected;
            mushroom = FindFirstObjectByType<RootMushroomPickup>(FindObjectsInactive.Include);
            if (mushroom != null && mushroom.IsCollected) shownAt = Time.unscaledTime;
        }

        private void OnDisable()
        {
            RootMushroomPickup.Collected -= OnMushroomCollected;
        }

        private void OnMushroomCollected(RootMushroomPickup collectedMushroom)
        {
            mushroom = collectedMushroom;
            shownAt = Time.unscaledTime;
        }

        private void OnGUI()
        {
            if (!IsReturnPromptVisible) return;
            EnsureStyles();

            float elapsed = Time.unscaledTime - shownAt;
            float enter = Mathf.Clamp01(elapsed / .25f);
            float leave = Mathf.Clamp01((displaySeconds - elapsed) / .45f);
            float alpha = Mathf.Min(enter, leave);
            float scale = Mathf.Lerp(.94f, 1f, 1f - Mathf.Pow(1f - enter, 3f));
            float width = Mathf.Min(500f, Screen.width - 32f);
            Rect panel = new Rect((Screen.width - width) * .5f, Screen.height * .18f, width, 92f);

            Matrix4x4 previousMatrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), panel.center);
            GUI.color = new Color(.025f, .08f, .05f, .94f * alpha);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(.4f, .9f, .5f, alpha);
            GUI.DrawTexture(new Rect(panel.x, panel.y, 4f, panel.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 20f, panel.y + 13f, panel.width - 40f, 32f), "RETURN TO THE VILLAGE", headingStyle);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 49f, panel.width - 40f, 24f), "Mushroom secured — return to the village.", detailStyle);
            GUI.matrix = previousMatrix;
        }

        private void EnsureStyles()
        {
            if (headingStyle != null) return;
            headingStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            detailStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = new Color(.8f, .94f, .84f, 1f) }
            };
        }
    }
}
