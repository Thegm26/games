using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WhoEnters.Core;

namespace WhoEnters.Integration
{
    /// <summary>Matches the single-sprite button contract: a brief scale response, never a sprite swap.</summary>
    public sealed class DecisionButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public const float PressScale = .97f;
        public const float ReturnDurationSeconds = .08f;
        private Button button;
        private Text label;
        private float releaseElapsed = ReturnDurationSeconds;
        private bool pressed;
        private bool reducedMotion;

        public void Configure(Button target, Text targetLabel)
        {
            button = target;
            label = targetLabel;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null || !button.interactable || reducedMotion) return;
            pressed = true;
            releaseElapsed = 0f;
            transform.localScale = Vector3.one * PressScale;
            DebugTrace.Log("animation.button_press", "scale=1.00->.97;duration=.08");
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Update()
        {
            if (label != null && button != null)
            {
                var color = label.color;
                color.a = button.interactable ? 1f : button.colors.disabledColor.a;
                label.color = color;
            }
            if (!pressed) return;
            releaseElapsed += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one * PressScale, Vector3.one, Mathf.Clamp01(releaseElapsed / ReturnDurationSeconds));
            if (releaseElapsed < ReturnDurationSeconds) return;
            transform.localScale = Vector3.one;
            pressed = false;
            DebugTrace.Log("animation.button_release", "scale=.97->1.00;duration=.08");
        }

        private void Release()
        {
            if (!pressed) return;
            releaseElapsed = 0f;
        }

        public void SetReducedMotion(bool enabled)
        {
            reducedMotion = enabled;
            pressed = false;
            transform.localScale = Vector3.one;
            DebugTrace.Log("animation.button_reduced_motion", "enabled=" + enabled + ";scale=1");
        }
    }
}
