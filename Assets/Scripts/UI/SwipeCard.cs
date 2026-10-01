using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WhoEnters.Core;
using WhoEnters.Integration;

namespace WhoEnters.UI
{
    public enum SwipePreviewDirection { None, Deny, Admit }

    public sealed class SwipeCard : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public const float DistanceThreshold = 158f;
        public const float VelocityThreshold = 1100f;
        public const float SnapbackDurationSeconds = .18f;
        public const float CommitDurationSeconds = .26f;
        public const float CommitEndpointX = 860f;
        public const float CaptionSwipeThreshold = 24f;
        public event Action<Decision> Decided;
        private RectTransform card;
        private Canvas canvas;
        private Vector2 home;
        private float lastTime;
        private float lastVelocity;
        private float lastLoggedDistance;
        private bool dragging;
        private bool animating;
        private Vector2 animationStart;
        private Vector2 animationEnd;
        private float rotationStart;
        private float rotationEnd;
        private float animationElapsed;
        private float animationDuration;
        private bool reducedMotion;
        private bool decisionInputEnabled = true;
        private bool captionGesture;
        private float captionGestureDistance;
        private Action captionSwipeAction;
        private DecisionTargetGlow denyPreview;
        private DecisionTargetGlow admitPreview;
        private bool clearPreviewAfterAnimation;

        public SwipePreviewDirection PreviewDirection { get; private set; }
        public float PreviewIntensity { get; private set; }
        public bool PreviewVisualsAreOutsideMovingCard => card != null
            && (denyPreview == null || !denyPreview.transform.IsChildOf(card))
            && (admitPreview == null || !admitPreview.transform.IsChildOf(card));

        public void Configure(RectTransform target, Action captionSwipe = null)
        {
            card = target;
            home = target.anchoredPosition;
            captionSwipeAction = captionSwipe;
            canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null) DebugTrace.Error("input.card_canvas_missing", "target=" + target.name);
        }

        public void SetDecisionInputEnabled(bool enabled) => decisionInputEnabled = enabled;

        /// <summary>
        /// Directional target halos are decorative only and belong outside the moving card; the
        /// card remains the full hit target. The fixed, labelled targets keep semantic feedback
        /// visible through a full drag without painting over visitor copy.
        /// </summary>
        public void ConfigureDragPreview(DecisionTargetGlow denyEdge, DecisionTargetGlow admitEdge)
        {
            denyPreview = denyEdge;
            admitPreview = admitEdge;
            if (!PreviewVisualsAreOutsideMovingCard)
                DebugTrace.Error("input.drag_preview_invalid_parent", "preview=child-of-moving-card");
            ClearPreview("configured");
        }

        public void ResetCard()
        {
            if (card == null) return;
            card.anchoredPosition = home;
            card.localRotation = Quaternion.identity;
            dragging = false;
            animating = false;
            ClearPreview("reset");
            DebugTrace.Log("animation.card_reset", "position=0,0;rotation=0");
        }

        public void PlayCommitted(Decision decision)
        {
            if (card == null) return;
            var sign = decision == Decision.Admit ? 1f : -1f;
            SetPreview(decision == Decision.Admit ? SwipePreviewDirection.Admit : SwipePreviewDirection.Deny,
                1f, "commit");
            if (reducedMotion)
            {
                card.anchoredPosition = new Vector2(sign * CommitEndpointX, home.y);
                card.localRotation = Quaternion.identity;
                DebugTrace.Log("animation.card_end", $"reduced=true;position={sign * CommitEndpointX:0.0},{home.y:0.0};rotation=0");
                ClearPreview("reduced-commit");
                return;
            }
            clearPreviewAfterAnimation = true;
            Animate(card.anchoredPosition, new Vector2(sign * CommitEndpointX, home.y), card.localEulerAngles.z, sign * 18f, CommitDurationSeconds, "commit;decision=" + decision);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!decisionInputEnabled)
            {
                captionGesture = captionSwipeAction != null;
                captionGestureDistance = 0f;
                if (captionGesture) DebugTrace.Log("input.caption_swipe_start", "card=stationary");
                return;
            }
            animating = false;
            dragging = true;
            ClearPreview("drag-start");
            lastTime = Time.unscaledTime;
            lastVelocity = 0f;
            lastLoggedDistance = float.NaN;
            DebugTrace.Log("input.drag_start", $"x={eventData.position.x:0.0};y={eventData.position.y:0.0}");
            DebugTrace.Log("animation.card_pickup", "scale=1.00;tilt=drag-controlled");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (captionGesture)
            {
                captionGestureDistance += Mathf.Abs(eventData.delta.x);
                return;
            }
            if (!dragging || card == null) return;
            if (canvas == null) canvas = card.GetComponentInParent<Canvas>();
            var factor = canvas != null ? Mathf.Max(.001f, canvas.scaleFactor) : 1f;
            var delta = eventData.delta.x / factor;
            card.anchoredPosition += new Vector2(delta, 0f);
            card.localRotation = reducedMotion ? Quaternion.identity : Quaternion.Euler(0f, 0f, Mathf.Clamp(card.anchoredPosition.x / 34f, -16f, 16f));
            var direction = card.anchoredPosition.x < 0f ? SwipePreviewDirection.Deny : SwipePreviewDirection.Admit;
            SetPreview(direction, Mathf.Clamp01(Mathf.Abs(card.anchoredPosition.x) / DistanceThreshold), "drag");
            var now = Time.unscaledTime;
            // A tiny same-frame drag is never a fling; this also makes public pointer tests
            // reproduce the same threshold behaviour as a 60 Hz phone.
            lastVelocity = delta / Mathf.Max(1f / 60f, now - lastTime);
            lastTime = now;
            if (float.IsNaN(lastLoggedDistance) || Mathf.Abs(card.anchoredPosition.x - lastLoggedDistance) >= 48f)
            {
                lastLoggedDistance = card.anchoredPosition.x;
                DebugTrace.Log("input.drag_move", $"distance={card.anchoredPosition.x:0.0};velocity={lastVelocity:0.0};sampled=true");
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (captionGesture)
            {
                captionGesture = false;
                if (captionGestureDistance >= CaptionSwipeThreshold)
                {
                    DebugTrace.Log("input.caption_swipe_consumed", $"distance={captionGestureDistance:0.0};card=stationary");
                    captionSwipeAction?.Invoke();
                }
                else DebugTrace.Log("input.caption_swipe_ignored", $"distance={captionGestureDistance:0.0};threshold={CaptionSwipeThreshold:0.0}");
                return;
            }
            if (!dragging || card == null) return;
            dragging = false;
            var distance = card.anchoredPosition.x;
            var resolution = SwipeDecisionResolver.Resolve(distance, lastVelocity);
            if (resolution.Commit)
            {
                SetPreview(resolution.Decision == Decision.Admit ? SwipePreviewDirection.Admit : SwipePreviewDirection.Deny, 1f, "commit-ready");
                Decided?.Invoke(resolution.Decision);
            }
            else if (reducedMotion) ResetCard();
            else
            {
                clearPreviewAfterAnimation = true;
                Animate(card.anchoredPosition, home, card.localEulerAngles.z, 0f, SnapbackDurationSeconds, "snapback");
            }
        }

        public void SetReducedMotion(bool enabled)
        {
            reducedMotion = enabled;
            if (enabled) ResetCard();
            DebugTrace.Log("animation.card_reduced_motion", "enabled=" + enabled + ";tilt=0;travel=" + (enabled ? "instant" : "animated"));
        }

        private void Update()
        {
            if (!animating || card == null) return;
            animationElapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(animationElapsed / animationDuration);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            card.anchoredPosition = Vector2.LerpUnclamped(animationStart, animationEnd, eased);
            card.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(rotationStart, rotationEnd, eased));
            if (t < 1f) return;
            animating = false;
            if (clearPreviewAfterAnimation)
            {
                clearPreviewAfterAnimation = false;
                ClearPreview("animation-end");
            }
            DebugTrace.Log("animation.card_end", $"position={animationEnd.x:0.0},{animationEnd.y:0.0};rotation={rotationEnd:0.0}");
        }

        private void Animate(Vector2 start, Vector2 end, float startRotation, float endRotation, float duration, string kind)
        {
            animationStart = start;
            animationEnd = end;
            rotationStart = NormalizeAngle(startRotation);
            rotationEnd = endRotation;
            animationDuration = Mathf.Max(.001f, duration);
            animationElapsed = 0f;
            animating = true;
            DebugTrace.Log("animation.card_start", $"kind={kind};duration={duration:0.00};from={start.x:0.0},{start.y:0.0};to={end.x:0.0},{end.y:0.0};rotation={rotationStart:0.0}->{endRotation:0.0}");
        }

        private void SetPreview(SwipePreviewDirection direction, float intensity, string reason)
        {
            PreviewDirection = direction;
            PreviewIntensity = Mathf.Clamp01(intensity);
            SetPreviewIntensity(denyPreview, direction == SwipePreviewDirection.Deny ? PreviewIntensity : 0f);
            SetPreviewIntensity(admitPreview, direction == SwipePreviewDirection.Admit ? PreviewIntensity : 0f);
            DebugTrace.Log("input.drag_preview", $"direction={direction};intensity={PreviewIntensity:0.00};reason={reason};reduced={reducedMotion}");
        }

        private void ClearPreview(string reason)
        {
            clearPreviewAfterAnimation = false;
            PreviewDirection = SwipePreviewDirection.None;
            PreviewIntensity = 0f;
            SetPreviewIntensity(denyPreview, 0f);
            SetPreviewIntensity(admitPreview, 0f);
            DebugTrace.Log("input.drag_preview_clear", "reason=" + reason);
        }

        private static void SetPreviewIntensity(DecisionTargetGlow glow, float intensity)
        {
            if (glow == null) return;
            glow.SetIntensity(intensity);
            // The halo never receives a tap; a later held drag restores its static cue without
            // adding motion in reduced-motion mode.
            glow.raycastTarget = false;
        }

        private static float NormalizeAngle(float angle) => angle > 180f ? angle - 360f : angle;
    }

    public readonly struct SwipeResolution
    {
        public readonly bool Commit;
        public readonly Decision Decision;
        public SwipeResolution(bool commit, Decision decision) { Commit = commit; Decision = decision; }
    }

    public static class SwipeDecisionResolver
    {
        public static SwipeResolution Resolve(float distance, float velocity)
        {
            var decision = distance < 0f ? Decision.Deny : Decision.Admit;
            var commits = Mathf.Abs(distance) >= SwipeCard.DistanceThreshold || Mathf.Abs(velocity) >= SwipeCard.VelocityThreshold;
            DebugTrace.Log("input.swipe_evaluated", $"distance={distance:0.0};velocity={velocity:0.0};distanceThreshold={SwipeCard.DistanceThreshold};velocityThreshold={SwipeCard.VelocityThreshold};commit={commits};decision={decision}");
            return new SwipeResolution(commits, decision);
        }
    }
}
