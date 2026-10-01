using System.Text;
using UnityEngine;
using UnityEngine.UI;
using WhoEnters.Core;

namespace WhoEnters.UI
{
    public sealed class DebugOverlay : MonoBehaviour
    {
        // The panel is intentionally diagnostic-only. Keeping a bounded newest-first tail means
        // a late failure never disappears below older startup noise on a phone-height panel.
        public const int MaximumVisibleEvents = 10;
        private Text label;
        private GameObject panel;
        private bool visible;
        private readonly StringBuilder builder = new StringBuilder();

        public void Configure(Text target, GameObject panelRoot = null)
        {
            label = target;
            panel = panelRoot;
            SetVisible(false);
        }

        public void Toggle()
        {
            SetVisible(!visible);
            DebugTrace.Log("debug.overlay", $"visible={visible}");
        }

        public void SetVisible(bool value)
        {
            visible = value;
            if (panel != null) panel.SetActive(value && DebugTrace.Enabled);
            if (label != null) label.gameObject.SetActive(value && DebugTrace.Enabled);
        }

        private void Update()
        {
            Refresh();
        }

        /// <summary>Refreshes the developer-only trace surface with the newest bounded event tail.</summary>
        public void Refresh()
        {
            if (!visible || label == null || !DebugTrace.Enabled) return;
            builder.Clear();
            builder.AppendLine("DEVELOPMENT TRACE");
            var skipped = Mathf.Max(0, DebugTrace.Recent.Count - MaximumVisibleEvents);
            var index = 0;
            foreach (var entry in DebugTrace.Recent)
            {
                if (index++ < skipped) continue;
                builder.AppendLine(entry.ToString());
            }
            label.text = builder.ToString();
        }
    }
}
