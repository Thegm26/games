using System;
using UnityEngine;
using UnityEngine.EventSystems;
using WhoEnters.Core;

namespace WhoEnters.UI
{
    public sealed class SwipeCard : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public const float DistanceThreshold = 158f;
        public const float VelocityThreshold = 1100f;
        public event Action<Decision> Decided;
        private RectTransform card;
        private Vector2 home;
        private float lastTime;
        private float lastVelocity;
        private float lastLoggedDistance;
        private bool dragging;

        public void Configure(RectTransform target)
        {
            card = target;
            home = target.anchoredPosition;
        }

        public void ResetCard()
        {
            if (card == null) return;
            card.anchoredPosition = home;
            card.localRotation = Quaternion.identity;
            dragging = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            dragging = true;
            lastTime = Time.unscaledTime;
            lastVelocity = 0f;
            lastLoggedDistance = float.NaN;
            DebugTrace.Log("input.drag_start", $"x={eventData.position.x:0.0};y={eventData.position.y:0.0}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || card == null) return;
            var factor = card.root.GetComponent<Canvas>().scaleFactor;
            var delta = eventData.delta.x / factor;
            card.anchoredPosition += new Vector2(delta, 0f);
            card.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(card.anchoredPosition.x / 34f, -16f, 16f));
            var now = Time.unscaledTime;
            lastVelocity = delta / Mathf.Max(0.001f, now - lastTime);
            lastTime = now;
            if (float.IsNaN(lastLoggedDistance) || Mathf.Abs(card.anchoredPosition.x - lastLoggedDistance) >= 48f)
            {
                lastLoggedDistance = card.anchoredPosition.x;
                DebugTrace.Log("input.drag_move", $"distance={card.anchoredPosition.x:0.0};velocity={lastVelocity:0.0};sampled=true");
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!dragging || card == null) return;
            dragging = false;
            var distance = card.anchoredPosition.x;
            var resolution = SwipeDecisionResolver.Resolve(distance, lastVelocity);
            if (resolution.Commit) Decided?.Invoke(resolution.Decision);
            else ResetCard();
        }
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
            var decision = distance < 0f ? Decision.Admit : Decision.Deny;
            var commits = Mathf.Abs(distance) >= SwipeCard.DistanceThreshold || Mathf.Abs(velocity) >= SwipeCard.VelocityThreshold;
            DebugTrace.Log("input.swipe_evaluated", $"distance={distance:0.0};velocity={velocity:0.0};distanceThreshold={SwipeCard.DistanceThreshold};velocityThreshold={SwipeCard.VelocityThreshold};commit={commits};decision={decision}");
            return new SwipeResolution(commits, decision);
        }
    }
}
