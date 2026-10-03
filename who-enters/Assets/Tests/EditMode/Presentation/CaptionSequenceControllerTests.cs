using System.Linq;
using NUnit.Framework;
using WhoEnters.Presentation;

namespace WhoEnters.Tests.Presentation
{
    public sealed class CaptionSequenceControllerTests
    {
        [Test]
        public void FirstActionCompletesTextAndSecondActionCompletesSequenceWithoutDoubleAction()
        {
            var trace = new MemoryPresentationTraceSink();
            var controller = new CaptionSequenceController(trace);
            controller.Begin(new CaptionSequence("one", new CaptionLine("line", PresentationRole.Visitor, "A sentence.")));

            Assert.That(controller.HandleAction(), Is.EqualTo(TypewriterAction.CompletedText));
            Assert.That(controller.IsActive, Is.True);
            Assert.That(controller.HandleAction(), Is.EqualTo(TypewriterAction.CompletedSequence));
            Assert.That(controller.IsActive, Is.False);
            Assert.That(controller.HandleAction(), Is.EqualTo(TypewriterAction.None));
            Assert.That(trace.Events.Count(item => item.EventId == "caption.dismissed"), Is.EqualTo(1));
        }

        [Test]
        public void AdvancesOrderedScreenRolesOneCaptionAtATime()
        {
            var trace = new MemoryPresentationTraceSink();
            var controller = new CaptionSequenceController(trace) { InstantMode = true };
            controller.Begin(new CaptionSequence("day", new CaptionLine("title", PresentationRole.DayTitle, "Day one"), new CaptionLine("decree", PresentationRole.Decree, "Read seals.")));

            Assert.That(controller.CurrentLine.Role, Is.EqualTo(PresentationRole.DayTitle));
            Assert.That(controller.HandleAction(), Is.EqualTo(TypewriterAction.AdvancedCaption));
            Assert.That(controller.CurrentLine.Role, Is.EqualTo(PresentationRole.Decree));
            Assert.That(controller.HandleAction(), Is.EqualTo(TypewriterAction.CompletedSequence));
            CollectionAssert.AreEqual(new[] { "presentation.sequence_started", "caption.started", "typewriter.completed", "caption.dismissed", "caption.started", "typewriter.completed", "caption.dismissed", "presentation.sequence_completed" }, trace.Events.Select(item => item.EventId));
        }

        [Test]
        public void EmitsDeterministicSampledEventsForSameDeltaSequence()
        {
            var first = RunTrace();
            var second = RunTrace();

            CollectionAssert.AreEqual(first, second);
            Assert.That(first.Any(item => item.StartsWith("typewriter.progress|")), Is.True);
            Assert.That(first.Any(item => item.StartsWith("typewriter.completed|")), Is.True);
        }

        [Test]
        public void EmptySequenceUsesFallbackDiagnostic()
        {
            var trace = new MemoryPresentationTraceSink();
            var controller = new CaptionSequenceController(trace);

            Assert.That(controller.Begin(new CaptionSequence("empty")), Is.False);
            Assert.That(trace.Events.Single().EventId, Is.EqualTo("presentation.error/fallback"));
        }

        [Test]
        public void InstantToggleCompletesActiveCaptionImmediatelyAndDeterministically()
        {
            var trace = new MemoryPresentationTraceSink();
            var controller = new CaptionSequenceController(trace);
            controller.Begin(new CaptionSequence("motion", new CaptionLine("motion-line", PresentationRole.Visitor, "A longer sentence.")));
            controller.Advance(0.03f);

            controller.InstantMode = true;

            Assert.That(controller.VisibleText, Is.EqualTo("A longer sentence."));
            Assert.That(controller.IsTextComplete, Is.True);
            Assert.That(trace.Events.Any(item => item.EventId == "typewriter.skipped" && item.Payload.Contains("source=instant-toggle")), Is.True);
            Assert.That(trace.Events.Any(item => item.EventId == "typewriter.completed" && item.Payload.Contains("source=instant-toggle")), Is.True);
        }

        [TestCase("button")]
        [TestCase("swipe")]
        [TestCase("keyboard")]
        public void VisitorCaptionConsumesFirstActionAndLetsSecondDecisionThrough(string source)
        {
            var controller = new CaptionSequenceController(new MemoryPresentationTraceSink());
            controller.Begin(new CaptionSequence("visitor-" + source, new CaptionLine("visitor", PresentationRole.Visitor, "Read this first.")));

            Assert.That(PresentationInputGate.ConsumeDecisionCaption(controller), Is.True, source + " must first complete text");
            Assert.That(controller.IsTextComplete, Is.True);
            Assert.That(PresentationInputGate.ConsumeDecisionCaption(controller), Is.False, source + " must resolve on the second deliberate action");
            Assert.That(controller.IsActive, Is.False);
        }

        private static string[] RunTrace()
        {
            var trace = new MemoryPresentationTraceSink();
            var controller = new CaptionSequenceController(trace);
            controller.Begin(new CaptionSequence("trace", new CaptionLine("trace-line", PresentationRole.Tutorial, "Hi, gate!")));
            controller.Advance(0.06f);
            controller.Advance(0.20f);
            controller.HandleAction();
            return trace.Events.Select(item => item.EventId + "|" + item.Payload).ToArray();
        }
    }
}
