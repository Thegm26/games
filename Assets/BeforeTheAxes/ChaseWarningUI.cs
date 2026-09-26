using UnityEngine;

namespace BeforeTheAxes
{
    /// <summary>
    /// Top-centre warning shown only while at least one woodcutter is actively chasing with
    /// a direct, unobstructed line of sight. Search and patrol are deliberately silent.
    /// </summary>
    public sealed class ChaseWarningUI : MonoBehaviour
    {
        private GUIStyle warningStyle;
        private GUIStyle countStyle;
        private int activeChaserCount;

        public bool IsWarningVisible => activeChaserCount > 0;
        public int ActiveChaserCount => activeChaserCount;

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
        }

        private void OnGUI()
        {
            if (!IsWarningVisible) return;
            EnsureStyles();

            const float width = 390f;
            const float height = 50f;
            Rect panel = new Rect((Screen.width - width) * .5f, 22f, width, height);
            GUI.color = new Color(.16f, .025f, .015f, .92f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(1f, .24f, .08f, 1f);
            GUI.DrawTexture(new Rect(panel.x, panel.y, 5f, panel.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(panel, WarningTextForCount(activeChaserCount), warningStyle);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 30f, panel.width - 24f, 16f), "BREAK LINE OF SIGHT", countStyle);
        }

        public static string WarningTextForCount(int count)
        {
            return count <= 1 ? "YOU ARE BEING CHASED" : $"YOU ARE BEING CHASED — {count} WOODCUTTERS";
        }

        private void EnsureStyles()
        {
            if (warningStyle != null) return;
            warningStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(1f, .78f, .65f, 1f) }
            };
            countStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(1f, .7f, .45f, 1f) }
            };
        }
    }
}
