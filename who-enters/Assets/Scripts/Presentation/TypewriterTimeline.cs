using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WhoEnters.Presentation
{
    /// <summary>
    /// Pure, grapheme-aware caption clock. It deliberately accepts a delta so headless tests and
    /// reduced-motion accessibility do not depend on Unity's global clock.
    /// </summary>
    public sealed class TypewriterTimeline
    {
        public const float DefaultCharactersPerSecond = 35f;
        public const float CommaPauseSeconds = 0.08f;
        public const float SentencePauseSeconds = 0.16f;

        private readonly List<string> elements = new List<string>();
        private readonly float charactersPerSecond;
        private readonly bool instant;
        private float budget;
        private float pauseRemaining;
        private int visibleCount;

        public string FullText { get; }
        public int VisibleCount => visibleCount;
        public int ElementCount => elements.Count;
        public bool IsComplete => visibleCount >= elements.Count;
        public string VisibleText => JoinVisible(visibleCount);

        public TypewriterTimeline(string text, float charactersPerSecond = DefaultCharactersPerSecond, bool instant = false)
        {
            FullText = text ?? string.Empty;
            this.charactersPerSecond = Math.Max(1f, charactersPerSecond);
            this.instant = instant;
            var enumerator = StringInfo.GetTextElementEnumerator(FullText);
            while (enumerator.MoveNext()) elements.Add((string)enumerator.Current);
            if (instant) visibleCount = elements.Count;
        }

        public bool Advance(float deltaSeconds)
        {
            if (IsComplete) return false;
            if (instant)
            {
                visibleCount = elements.Count;
                return true;
            }

            var remaining = Math.Max(0f, deltaSeconds);
            var changed = false;
            while (remaining > 0f && !IsComplete)
            {
                if (pauseRemaining > 0f)
                {
                    var used = Math.Min(pauseRemaining, remaining);
                    pauseRemaining -= used;
                    remaining -= used;
                    if (pauseRemaining > 0f) break;
                }

                var needed = Math.Max(0f, 1f - budget) / charactersPerSecond;
                if (remaining + 0.00001f < needed)
                {
                    budget += remaining * charactersPerSecond;
                    break;
                }

                remaining -= needed;
                budget = 0f;
                visibleCount++;
                changed = true;
                pauseRemaining = PunctuationPause(elements[visibleCount - 1]);
            }
            return changed;
        }

        public bool CompleteImmediately()
        {
            if (IsComplete) return false;
            visibleCount = elements.Count;
            budget = 0f;
            pauseRemaining = 0f;
            return true;
        }

        private string JoinVisible(int count)
        {
            if (count <= 0) return string.Empty;
            var builder = new StringBuilder();
            for (var index = 0; index < count; index++) builder.Append(elements[index]);
            return builder.ToString();
        }

        private static float PunctuationPause(string element)
        {
            if (string.IsNullOrEmpty(element)) return 0f;
            switch (element[element.Length - 1])
            {
                case '.': case '!': case '?': case '…': return SentencePauseSeconds;
                case ',': case ';': case ':': return CommaPauseSeconds;
                default: return 0f;
            }
        }
    }
}
