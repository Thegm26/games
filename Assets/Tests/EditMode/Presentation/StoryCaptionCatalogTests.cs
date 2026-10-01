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
            var sequence = StoryCaptionCatalog.Verdict(visitor, new VerdictRecord
            {
                Chosen = Decision.Admit, Correct = true, RuleId = "admit-moon-seal", RuleExplanation = "A true moon seal grants entry.",
            }, new GameState());

            Assert.That(sequence.Lines.First().Text, Is.EqualTo("RIGHT. Moon seal decided it. +0"));
            Assert.That(sequence.Lines.First().Text, Does.Not.Contain("admit-moon-seal").And.Not.Contain("A true moon seal grants entry."));
            Assert.That(sequence.Lines.Last().Text, Is.Not.Empty);
        }

        [Test]
        public void TutorialAndVisitorCaptionsUsePlainSwipeWords()
        {
            var tutorial = StoryCaptionCatalog.Tutorial().Lines.Single().Text;
            Assert.That(tutorial, Is.EqualTo("Swipe right to admit.\nSwipe left to deny.\nTap Rulebook if you need it."));

            var visitor = StoryContent.Create().Visitors.First();
            var caption = StoryCaptionCatalog.Visitor(visitor).Lines.Single().Text;
            Assert.That(caption, Is.EqualTo("Swipe right: Admit\nSwipe left: Deny"));
            Assert.That(caption.Length, Is.LessThanOrEqualTo(90));
        }

        [Test]
        public void VerdictCaptionsSayWhetherTheChoiceWasRightAndWhetherASealWasLost()
        {
            var visitor = StoryContent.Create().Visitors.First(item => string.IsNullOrEmpty(item.FlagOnAdmit));
            var right = StoryCaptionCatalog.Verdict(visitor, new VerdictRecord { Chosen = Decision.Admit, Correct = true, RuleId = "admit-moon-seal", RuleExplanation = "A true moon seal grants entry." }, new GameState());
            var wrong = StoryCaptionCatalog.Verdict(visitor, new VerdictRecord { Chosen = Decision.Deny, Correct = false, RuleId = "admit-moon-seal", RuleExplanation = "A true moon seal grants entry." }, new GameState());

            Assert.That(right.Lines.First().Text, Does.StartWith("RIGHT. Moon seal decided it."));
            Assert.That(wrong.Lines.First().Text, Does.Contain("WRONG."));
            Assert.That(wrong.Lines.First().Text, Does.Contain("Lost 1 seal."));
            Assert.That(right.Lines, Has.Length.EqualTo(1));
        }

        [Test]
        public void PredecisionCaptionIsAnActionReminderAndDoesNotLeakTheVisitorsDossier()
        {
            var content = StoryContent.Create();
            foreach (var visitor in content.Visitors)
            {
                var text = StoryCaptionCatalog.Visitor(visitor).Lines.Single().Text;
                foreach (var fact in visitor.Traits.Concat(visitor.Documents).Concat(visitor.VisibleCues))
                    Assert.That(text.IndexOf(fact, System.StringComparison.OrdinalIgnoreCase), Is.EqualTo(-1), visitor.Id + ";fact=" + fact);
            }
        }

        [Test]
        public void RulebookIsTheOnlyCaptionSurfaceThatMayCarryTheCompleteDecree()
        {
            var content = StoryContent.Create();
            foreach (var decree in content.Decrees)
            {
                var day = StoryCaptionCatalog.DayAndDecree(decree.Day, decree).Lines.Single().Text;
                Assert.That(day, Is.Not.EqualTo(decree.DisplayText));
                Assert.That(day, Does.Not.Contain("FIRST MATCH WINS").And.Not.Contain("DEFAULT:"));
            }
            foreach (var visitor in content.Visitors)
            {
                var summary = StoryCaptionCatalog.Verdict(visitor, new VerdictRecord
                {
                    Chosen = Decision.Admit, Correct = true, RuleId = "admit-moon-seal", RuleExplanation = "A true moon seal grants entry.",
                }, new GameState());
                Assert.That(summary.Lines.Count, Is.LessThanOrEqualTo(string.IsNullOrWhiteSpace(visitor.FlagOnAdmit) ? 1 : 2));
                Assert.That(summary.Lines.First().Text, Does.Not.Contain("admit-moon-seal").And.Not.Contain("A true moon seal grants entry."));
            }
        }

        [Test]
        public void NormalFlowCopyDoesNotRepeatRulebookOnlyTermsOrOrderedDecreeCopy()
        {
            var content = StoryContent.Create();
            var normalFlow = new System.Collections.Generic.List<string>
            {
                StoryCaptionCatalog.Intro().Lines.Single().Text,
                StoryCaptionCatalog.Visitor(null).Lines.Single().Text,
                StoryCaptionCatalog.DaySummary(new GameState()).Lines.Single().Text,
            };
            foreach (EndingKind ending in System.Enum.GetValues(typeof(EndingKind)))
                normalFlow.AddRange(StoryCaptionCatalog.Ending(ending, new GameState()).Lines.Select(line => line.Text));
            foreach (var decree in content.Decrees)
                normalFlow.Add(StoryCaptionCatalog.DayAndDecree(decree.Day, decree).Lines.Single().Text);
            foreach (var visitor in content.Visitors)
            {
                normalFlow.Add(StoryCaptionCatalog.Visitor(visitor).Lines.Single().Text);
                normalFlow.AddRange(StoryCaptionCatalog.Verdict(visitor,
                    new VerdictRecord { Chosen = Decision.Admit, Correct = true }, new GameState()).Lines.Select(line => line.Text));
                normalFlow.AddRange(StoryCaptionCatalog.Verdict(visitor,
                    new VerdictRecord { Chosen = Decision.Deny, Correct = false }, new GameState()).Lines.Select(line => line.Text));
            }

            foreach (var text in normalFlow)
            {
                Assert.That(text, Does.Not.Contain("rule").IgnoreCase);
                Assert.That(text, Does.Not.Contain("FIRST MATCH").IgnoreCase);
                Assert.That(text, Does.Not.Contain("DEFAULT").IgnoreCase);
                foreach (var decree in content.Decrees)
                {
                    Assert.That(text, Is.Not.EqualTo(decree.Title));
                    Assert.That(text, Is.Not.EqualTo(decree.DisplayText));
                }
            }
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
