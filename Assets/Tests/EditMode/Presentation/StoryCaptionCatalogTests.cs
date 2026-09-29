using System.Linq;
using NUnit.Framework;
using WhoEnters.Content;
using WhoEnters.Core;
using WhoEnters.Presentation;

namespace WhoEnters.Tests.Presentation
{
    public sealed class StoryCaptionCatalogTests
    {
        [Test]
        public void EveryCurrentAuthoredVisitorHasAnExplicitCaption()
        {
            var content = StoryContent.Create();
            var trace = new MemoryPresentationTraceSink();

            var missing = StoryCaptionCatalog.MissingVisitorMappings(content, trace);

            Assert.That(missing, Is.Empty, "A missing visitor caption is a presentation/content integration blocker.");
            Assert.That(trace.Events, Is.Empty);
        }

        [Test]
        public void StoryFlagConsequencesAndEveryEndingHaveCaptionCopy()
        {
            var content = StoryContent.Create();
            var flags = content.Visitors.SelectMany(visitor => new[] { visitor.FlagOnAdmit, visitor.FlagOnDeny })
                .Where(flag => !string.IsNullOrWhiteSpace(flag)).Distinct().ToArray();

            foreach (var flag in flags)
                Assert.That(StoryCaptionCatalog.HasConsequenceForFlag(flag), Is.True, "Missing consequence caption: " + flag);
            foreach (var terminalFlag in StoryContent.TerminalOutcomeFlags)
                Assert.That(StoryCaptionCatalog.HasConsequenceForFlag(terminalFlag), Is.True, "Missing terminal epilogue caption: " + terminalFlag);
            foreach (EndingKind ending in System.Enum.GetValues(typeof(EndingKind)))
                Assert.That(StoryCaptionCatalog.Ending(ending).Lines.Single().Text, Is.Not.Empty);
        }

        [Test]
        public void VerdictChoosesOutcomeFlagCaptionBeforeGenericCorrectness()
        {
            var visitor = StoryContent.Create().Visitors.Single(item => item.Id == "d1-02-mira-mothwitch");
            var sequence = StoryCaptionCatalog.Verdict(visitor, new VerdictRecord { Chosen = Decision.Admit, Correct = true }, new GameState());

            Assert.That(sequence.Lines.Single().Text, Does.Contain("lantern"));
        }

        [Test]
        public void EndingCarriesEveryReachedTerminalChainOutcomeInStableStoryOrder()
        {
            var state = new GameState();
            state.StoryFlags.Add("mira_oath_final_refused");
            state.StoryFlags.Add("nella_safe");
            state.StoryFlags.Add("pip_warning_heard_dry");
            state.StoryFlags.Add("rowan_watch_held");

            var sequence = StoryCaptionCatalog.Ending(EndingKind.GateHeld, state);
            var text = sequence.Lines.Select(line => line.Text).ToArray();

            CollectionAssert.AreEqual(new[]
            {
                StoryContent.EpilogueInputs["mira_oath_final_refused"],
                StoryContent.EpilogueInputs["nella_safe"],
                StoryContent.EpilogueInputs["pip_warning_heard_dry"],
                StoryContent.EpilogueInputs["rowan_watch_held"],
            }, text.Skip(1).ToArray());
        }

        [Test]
        public void EveryTerminalOutcomeCanAppearInAnEndingSequence()
        {
            foreach (var terminalFlag in StoryContent.TerminalOutcomeFlags)
            {
                var state = new GameState();
                state.StoryFlags.Add(terminalFlag);
                var text = StoryCaptionCatalog.Ending(EndingKind.GateHeld, state).Lines.Select(line => line.Text).ToArray();
                CollectionAssert.Contains(text, StoryContent.EpilogueInputs[terminalFlag]);
            }
        }
    }
}
