using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WhoEnters.Content;
using WhoEnters.Core;

namespace WhoEnters.Tests.EditMode.Content
{
    public sealed class StoryContentTests
    {
        private static readonly int[] Seeds = { -99, 0, 1, 260928, 777777 };

        [Test]
        public void AuthoringHasFiveDecreesFortySlotsAndValidAlternatives()
        {
            var content = StoryContent.Create();

            Assert.That(content.TotalDays, Is.EqualTo(5));
            Assert.That(content.VisitorsPerDay, Is.EqualTo(8));
            Assert.That(content.Decrees.Select(decree => decree.Day).OrderBy(day => day), Is.EqualTo(Enumerable.Range(1, 5)));
            Assert.That(content.Visitors.Select(visitor => visitor.Id).Distinct().Count(), Is.EqualTo(content.Visitors.Count));
            Assert.That(content.Visitors.Count, Is.GreaterThanOrEqualTo(40));
            for (var day = 1; day <= 5; day++)
            {
                var slots = content.Visitors.Where(visitor => visitor.Day == day).GroupBy(visitor => visitor.EncounterSlot).OrderBy(group => group.Key).ToArray();
                Assert.That(slots.Select(group => group.Key), Is.EqualTo(Enumerable.Range(1, 8)), "day=" + day);
                Assert.That(slots.All(group => group.Any(visitor => string.IsNullOrEmpty(visitor.RequiredFlag))), Is.True, "fallback day=" + day);
            }
        }

        [Test]
        public void AllRuleReferencesAndExpectedVerdictsAreVisibleAndDeterministic()
        {
            var content = StoryContent.Create();
            var allTraits = new HashSet<string>(content.Visitors.SelectMany(visitor => visitor.Traits));
            var allDocuments = new HashSet<string>(content.Visitors.SelectMany(visitor => visitor.Documents));
            var allCues = new HashSet<string>(content.Visitors.SelectMany(visitor => visitor.VisibleCues));
            foreach (var decree in content.Decrees)
            {
                foreach (var rule in decree.Rules)
                {
                    Assert.That(rule.RequiredTraits.All(allTraits.Contains), Is.True, "missing trait rule=" + rule.Id);
                    Assert.That(rule.RequiredDocuments.All(allDocuments.Contains), Is.True, "missing document rule=" + rule.Id);
                    Assert.That(rule.RequiredVisibleCues.All(allCues.Contains), Is.True, "missing cue rule=" + rule.Id);
                }
            }
            foreach (var visitor in content.Visitors)
            {
                var decree = content.Decrees.Single(item => item.Day == visitor.Day);
                var first = RuleEvaluator.Evaluate(visitor, decree);
                var second = RuleEvaluator.Evaluate(visitor, decree);
                Assert.That(first.Expected, Is.EqualTo(second.Expected), visitor.Id);
                Assert.That(first.RuleId, Is.EqualTo(second.RuleId), visitor.Id);
                Assert.That(visitor.Traits.Count + visitor.Documents.Count + visitor.VisibleCues.Count, Is.GreaterThan(0), visitor.Id);
            }
        }

        [Test]
        public void PortraitKeysUseAllAndOnlyTheSixteenApprovedArchetypes()
        {
            var content = StoryContent.Create();
            var used = content.Visitors.Select(visitor => visitor.PortraitKey).Distinct().OrderBy(key => key).ToArray();
            var approved = StoryContent.CanonicalPortraitKeys.OrderBy(key => key).ToArray();
            Assert.That(used, Is.EqualTo(approved));
            Assert.That(StoryContent.CanonicalPortraitKeys.Count, Is.EqualTo(16));
        }

        [Test]
        public void EveryFlagRequirementHasAPriorProducerAndEveryStoryHasThreeNights()
        {
            var content = StoryContent.Create();
            var flags = content.Visitors.SelectMany(visitor => new[]
            {
                new FlagProducer(visitor.FlagOnAdmit, visitor.Day), new FlagProducer(visitor.FlagOnDeny, visitor.Day),
            }).Where(flag => !string.IsNullOrEmpty(flag.Flag)).ToArray();
            foreach (var conditional in content.Visitors.Where(visitor => !string.IsNullOrEmpty(visitor.RequiredFlag)))
            {
                Assert.That(flags.Any(flag => flag.Flag == conditional.RequiredFlag && flag.Day < conditional.Day), Is.True,
                    "required=" + conditional.RequiredFlag + ";visitor=" + conditional.Id);
            }
            var chains = content.Visitors.Where(visitor => !string.IsNullOrEmpty(visitor.StoryChainId)).GroupBy(visitor => visitor.StoryChainId).ToArray();
            Assert.That(chains.Select(chain => chain.Key).OrderBy(key => key), Is.EqualTo(new[] { "mira", "nella", "pip", "rowan" }));
            foreach (var chain in chains)
                Assert.That(chain.Select(visitor => visitor.Day).Distinct().Count(), Is.EqualTo(3), chain.Key);
        }

        [Test]
        public void MultipleSeedsResolveExactlyEightUniqueVisitorsPerDayWithoutSkipOrRepeat()
        {
            foreach (var seed in Seeds)
            {
                var run = CompleteWithExpectedVerdicts(seed);
                Assert.That(run.State.Verdicts.Count, Is.EqualTo(40), "seed=" + seed);
                Assert.That(run.State.Verdicts.Select(record => record.VisitorId).Distinct().Count(), Is.EqualTo(40), "seed=" + seed);
                for (var day = 1; day <= 5; day++)
                    Assert.That(run.FrozenQueueForDay(day).Count, Is.EqualTo(8), "seed=" + seed + ";day=" + day);
            }
        }

        [Test]
        public void EveryOutcomeFlagIsConsumedOrHasADocumentedTerminalEpilogueInput()
        {
            var content = StoryContent.Create();
            var produced = content.Visitors.SelectMany(visitor => new[] { visitor.FlagOnAdmit, visitor.FlagOnDeny })
                .Where(flag => !string.IsNullOrWhiteSpace(flag)).Distinct().ToArray();
            var consumed = content.Visitors.Where(visitor => !string.IsNullOrWhiteSpace(visitor.RequiredFlag))
                .Select(visitor => visitor.RequiredFlag).ToHashSet();

            Assert.That(StoryContent.TerminalOutcomeFlags.OrderBy(flag => flag),
                Is.EqualTo(StoryContent.EpilogueInputs.Keys.OrderBy(flag => flag)));
            foreach (var outcome in produced)
            {
                Assert.That(consumed.Contains(outcome) || StoryContent.TerminalOutcomeFlags.Contains(outcome), Is.True,
                    "orphan outcome=" + outcome);
            }
            foreach (var terminal in StoryContent.TerminalOutcomeFlags)
            {
                Assert.That(produced.Contains(terminal), Is.True, "unproduced terminal=" + terminal);
                Assert.That(StoryContent.EpilogueInputs[terminal], Is.Not.Empty, "missing epilogue input=" + terminal);
            }
        }

        [Test]
        public void EveryRecurringChainHasPersistentBranchSpecificEvidenceAndTerminalEpilogueInput()
        {
            AssertPersistentBranch("mira", "d1-02-mira-mothwitch", Decision.Admit, Decision.Deny,
                new[] { "d1-02-mira-mothwitch", "d3-03-mira-oathbound", "d5-07-mira-gate-remembers" }, "secret_gate_remembers",
                new[] { "d1-02-mira-mothwitch", "d3-03-mira-rainbound", "d5-07-mira-rain-silenced" }, "mira_rain_silenced_held");
            AssertPersistentBranch("pip", "d1-07-pip-sootwhistle", Decision.Admit, Decision.Deny,
                new[] { "d1-07-pip-sootwhistle", "d2-02-pip-dry-letter", "d4-05-pip-ledger-dry" }, "pip_warning_heard_dry",
                new[] { "d1-07-pip-sootwhistle", "d2-02-pip-rain-letter", "d4-05-pip-ledger-patched" }, "pip_warning_heard_rescued");
            AssertPersistentBranch("nella", "d2-06-nella-nightsoup", Decision.Admit, Decision.Deny,
                new[] { "d2-06-nella-nightsoup", "d3-07-nella-soup-route", "d5-02-nella-last-soup" }, "nella_safe",
                new[] { "d2-06-nella-nightsoup", "d3-07-nella-cold-route", "d5-02-nella-late-caravan" }, "nella_patients_saved_late");
            AssertPersistentBranch("rowan", "d2-08-sir-rowan", Decision.Admit, Decision.Deny,
                new[] { "d2-08-sir-rowan", "d4-03-rowan-confessor", "d5-08-rowan-final-watch" }, "rowan_watch_held",
                new[] { "d2-08-sir-rowan", "d4-03-rowan-unarmed", "d5-08-rowan-witness-watch" }, "rowan_witness_watch");
        }

        [Test]
        public void ExplicitMercyChoicesPersistIntoMirasFinalCardAndCostIntegrity()
        {
            var oathRefused = CompleteWithPolicy(260928, (run, visitor, expected) => visitor.Id == "d3-03-mira-oathbound" ? Opposite(expected) : expected);
            var rainMercy = CompleteWithPolicy(260928, (run, visitor, expected) => visitor.Id == "d1-02-mira-mothwitch"
                ? Decision.Deny
                : visitor.Id == "d3-03-mira-rainbound" ? Opposite(expected) : expected);

            Assert.That(StoryIds(oathRefused, "mira").Last(), Is.EqualTo("d5-07-mira-refused-oath"));
            Assert.That(TerminalFlags(oathRefused), Does.Contain("mira_oath_released"));
            Assert.That(StoryIds(rainMercy, "mira").Last(), Is.EqualTo("d5-07-mira-second-chance"));
            Assert.That(TerminalFlags(rainMercy), Does.Contain("mira_second_chance_held"));
            Assert.That(oathRefused.State.Integrity, Is.EqualTo(4), "refusing the lawful raven oath costs one seal");
            Assert.That(rainMercy.State.Integrity, Is.EqualTo(3), "denying Mira, then granting unlawful mercy, costs two seals");
        }

        [Test]
        public void ContentDataCanReachEveryPublishedEnding()
        {
            var secret = CompleteWithExpectedVerdicts(260928);
            Assert.That(secret.Ending(), Is.EqualTo(EndingKind.TheGateRemembers));

            var gateHeld = CompleteWithPolicy(12, (run, visitor, expected) => visitor.Id == "d5-07-mira-gate-remembers" ? Opposite(expected) : expected);
            Assert.That(gateHeld.Ending(), Is.EqualTo(EndingKind.GateHeld));

            var hollow = CompleteWithPolicy(13, (run, visitor, expected) => run.State.Verdicts.Count < 3 || visitor.Id == "d5-07-mira-gate-remembers" ? Opposite(expected) : expected);
            Assert.That(hollow.Ending(), Is.EqualTo(EndingKind.HollowVictory));

            var fallen = CompleteWithPolicy(14, (run, visitor, expected) => Opposite(expected));
            Assert.That(fallen.Ending(), Is.EqualTo(EndingKind.CastleFallen));
        }

        [Test]
        public void SameSeedProducesTheSameResolvedQueues()
        {
            var first = CompleteWithExpectedVerdicts(260928);
            var second = CompleteWithExpectedVerdicts(260928);
            for (var day = 1; day <= 5; day++)
            {
                Assert.That(first.FrozenQueueForDay(day).Select(visitor => visitor.Id),
                    Is.EqualTo(second.FrozenQueueForDay(day).Select(visitor => visitor.Id)), "day=" + day);
            }
        }

        private static RunStateMachine CompleteWithExpectedVerdicts(int seed) => CompleteWithPolicy(seed, (run, visitor, expected) => expected);

        private static RunStateMachine CompleteWithPolicy(int seed, System.Func<RunStateMachine, VisitorDefinition, Decision, Decision> decision)
        {
            var run = new RunStateMachine(StoryContent.Create(), seed);
            run.StartRun();
            while (run.State.Phase != RunPhase.Ending)
            {
                if (run.State.Phase == RunPhase.DaySummary)
                {
                    run.AdvanceDay();
                    continue;
                }
                var visitor = run.CurrentVisitor();
                var expected = RuleEvaluator.Evaluate(visitor, run.CurrentDecree()).Expected;
                run.Resolve(decision(run, visitor, expected));
            }
            return run;
        }

        private static void AssertPersistentBranch(string chain, string earlyVisitorId, Decision firstChoice, Decision secondChoice,
            string[] firstExpectedIds, string firstTerminalFlag, string[] secondExpectedIds, string secondTerminalFlag)
        {
            var first = CompleteWithPolicy(260928, (run, visitor, expected) => visitor.Id == earlyVisitorId ? firstChoice : expected);
            var second = CompleteWithPolicy(260928, (run, visitor, expected) => visitor.Id == earlyVisitorId ? secondChoice : expected);
            var firstIds = StoryIds(first, chain);
            var secondIds = StoryIds(second, chain);

            Assert.That(firstIds, Is.EqualTo(firstExpectedIds), "first branch chain=" + chain);
            Assert.That(secondIds, Is.EqualTo(secondExpectedIds), "second branch chain=" + chain);
            Assert.That(firstIds.Skip(1).SequenceEqual(secondIds.Skip(1)), Is.False, "later ids reconverged chain=" + chain);
            Assert.That(TerminalFlags(first), Does.Contain(firstTerminalFlag), "first terminal chain=" + chain);
            Assert.That(TerminalFlags(second), Does.Contain(secondTerminalFlag), "second terminal chain=" + chain);
            Assert.That(firstTerminalFlag, Is.Not.EqualTo(secondTerminalFlag), "terminal reconverged chain=" + chain);
            Assert.That(StoryContent.EpilogueInputs.ContainsKey(firstTerminalFlag), Is.True, "first epilogue chain=" + chain);
            Assert.That(StoryContent.EpilogueInputs.ContainsKey(secondTerminalFlag), Is.True, "second epilogue chain=" + chain);

            var firstLater = first.Content.Visitors.Single(visitor => visitor.Id == firstIds.Last());
            var secondLater = second.Content.Visitors.Single(visitor => visitor.Id == secondIds.Last());
            Assert.That(VisibleFingerprint(firstLater), Is.Not.EqualTo(VisibleFingerprint(secondLater)), "later evidence reconverged chain=" + chain);
        }

        private static string[] StoryIds(RunStateMachine run, string chain)
            => run.State.Verdicts.Select(record => run.Content.Visitors.Single(visitor => visitor.Id == record.VisitorId))
                .Where(visitor => visitor.StoryChainId == chain).Select(visitor => visitor.Id).ToArray();

        private static string[] TerminalFlags(RunStateMachine run)
            => run.State.StoryFlags.Where(StoryContent.TerminalOutcomeFlags.Contains).ToArray();

        private static string VisibleFingerprint(VisitorDefinition visitor)
            => visitor.Dialogue + "|" + string.Join(",", visitor.Traits) + "|" + string.Join(",", visitor.Documents) + "|" + string.Join(",", visitor.VisibleCues);

        private static Decision Opposite(Decision value) => value == Decision.Admit ? Decision.Deny : Decision.Admit;

        private readonly struct FlagProducer
        {
            public readonly string Flag;
            public readonly int Day;
            public FlagProducer(string flag, int day) { Flag = flag; Day = day; }
        }
    }
}
