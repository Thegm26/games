using NUnit.Framework;
using WhoEnters.Presentation;

namespace WhoEnters.Tests.Presentation
{
    public sealed class TypewriterTimelineTests
    {
        [Test]
        public void AdvancesAtConfiguredRateAndHonoursPunctuationPause()
        {
            var timeline = new TypewriterTimeline("A, B.");

            // Two glyphs take roughly 0.057s; the remaining comma pause must absorb the next tick.
            timeline.Advance(0.06f);
            Assert.That(timeline.VisibleText, Is.EqualTo("A,"));
            timeline.Advance(0.07f);
            Assert.That(timeline.VisibleText, Is.EqualTo("A,"));
            timeline.Advance(0.06f);
            Assert.That(timeline.VisibleText, Is.EqualTo("A, "));
            timeline.Advance(0.01f);
            Assert.That(timeline.VisibleText, Is.EqualTo("A, B"));
            timeline.Advance(1f);
            Assert.That(timeline.VisibleText, Is.EqualTo("A, B."));
            Assert.That(timeline.IsComplete, Is.True);
        }

        [Test]
        public void PreservesQuotesAccentsAndUnicodeGraphemes()
        {
            const string text = "“Míra 🦋”";
            var timeline = new TypewriterTimeline(text);

            Assert.That(timeline.ElementCount, Is.LessThan(text.Length));
            timeline.Advance(2f);

            Assert.That(timeline.VisibleText, Is.EqualTo(text));
            Assert.That(timeline.IsComplete, Is.True);
        }

        [Test]
        public void InstantModeCompletesWithoutClockProgress()
        {
            var timeline = new TypewriterTimeline("No waiting.", instant: true);

            Assert.That(timeline.IsComplete, Is.True);
            Assert.That(timeline.VisibleText, Is.EqualTo("No waiting."));
            Assert.That(timeline.Advance(0f), Is.False);
        }
    }
}
