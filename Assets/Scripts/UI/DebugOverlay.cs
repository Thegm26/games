using System.Text;
using UnityEngine;
using UnityEngine.UI;
using WhoEnters.Core;

namespace WhoEnters.UI
{
    public sealed class DebugOverlay : MonoBehaviour
    {
        private Text label;
        private bool visible;
        private readonly StringBuilder builder = new StringBuilder();

        public void Configure(Text target)
        {
            label = target;
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
            if (label != null) label.gameObject.SetActive(value && DebugTrace.Enabled);
        }

        private void Update()
        {
            if (!visible || label == null || !DebugTrace.Enabled) return;
            builder.Clear();
            builder.AppendLine("DEVELOPMENT TRACE");
            foreach (var entry in DebugTrace.Recent) builder.AppendLine(entry.ToString());
            label.text = builder.ToString();
        }
    }
}
