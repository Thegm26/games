using UnityEngine;
using UnityEngine.UI;
using WhoEnters.Core;

namespace WhoEnters.Integration
{
    /// <summary>Explicit verdict and damage endpoints; no screenshot-derived animation assumptions.</summary>
    public sealed class FeedbackMotion : MonoBehaviour
    {
        public const float StampDurationSeconds = .38f;
        public const float StampStartScale = 1.45f;
        public const float StampEndScale = 1f;
        public const float DamageShakeDurationSeconds = .22f;
        public const float DamageShakeAmplitude = 13f;
        private Image stamp;
        private RectTransform shakeTarget;
        private float stampElapsed;
        private float shakeElapsed;
        private Vector2 shakeHome;
        private bool activeStamp;
        private bool activeShake;
        private bool reducedMotion;

        public void Configure(Image stampImage, RectTransform target)
        {
            stamp = stampImage;
            shakeTarget = target;
            if (shakeTarget != null) shakeHome = shakeTarget.anchoredPosition;
            if (stamp != null) stamp.gameObject.SetActive(false);
        }

        public void PlayVerdict(bool correct, Sprite sprite)
        {
            if (stamp == null) return;
            stamp.sprite = sprite;
            stamp.color = new Color(1f, 1f, 1f, 0f);
            stamp.rectTransform.localScale = Vector3.one * StampStartScale;
            stamp.gameObject.SetActive(true);
            stampElapsed = 0f;
            activeStamp = !reducedMotion;
            if (reducedMotion)
            {
                stamp.color = Color.white;
                stamp.rectTransform.localScale = Vector3.one * StampEndScale;
            }
            DebugTrace.Log("animation.stamp_start", "correct=" + correct + ";duration=.38;scale=1.45->1.00");
            if (!correct)
            {
                shakeElapsed = 0f;
                activeShake = !reducedMotion && shakeTarget != null;
                if (shakeTarget != null) shakeHome = shakeTarget.anchoredPosition;
                DebugTrace.Log("animation.damage_shake_start", "duration=.22;amplitude=13");
            }
        }

        public void ClearVerdict()
        {
            activeStamp = false;
            activeShake = false;
            if (stamp != null) stamp.gameObject.SetActive(false);
            if (shakeTarget != null) shakeTarget.anchoredPosition = shakeHome;
        }

        public void SetReducedMotion(bool enabled)
        {
            reducedMotion = enabled;
            if (!enabled)
            {
                DebugTrace.Log("animation.reduced_motion", "enabled=false;stamp=normal;shake=home");
                return;
            }
            activeStamp = false;
            activeShake = false;
            if (stamp != null && stamp.gameObject.activeSelf)
            {
                stamp.color = Color.white;
                stamp.rectTransform.localScale = Vector3.one * StampEndScale;
            }
            if (shakeTarget != null) shakeTarget.anchoredPosition = shakeHome;
            DebugTrace.Log("animation.reduced_motion", "enabled=true;stamp=endpoint;shake=home");
        }

        private void Update()
        {
            if (activeStamp && stamp != null)
            {
                stampElapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(stampElapsed / StampDurationSeconds);
                stamp.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t * 4f));
                stamp.rectTransform.localScale = Vector3.one * Mathf.Lerp(StampStartScale, StampEndScale, t);
                if (t >= 1f)
                {
                    activeStamp = false;
                    DebugTrace.Log("animation.stamp_end", "duration=.38;scale=1.00");
                }
            }
            if (activeShake && shakeTarget != null)
            {
                shakeElapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(shakeElapsed / DamageShakeDurationSeconds);
                shakeTarget.anchoredPosition = shakeHome + Vector2.right * Mathf.Sin(t * Mathf.PI * 5f) * DamageShakeAmplitude * (1f - t);
                if (t >= 1f)
                {
                    shakeTarget.anchoredPosition = shakeHome;
                    activeShake = false;
                    DebugTrace.Log("animation.damage_shake_end", "duration=.22;endpoint=home");
                }
            }
        }
    }
}
