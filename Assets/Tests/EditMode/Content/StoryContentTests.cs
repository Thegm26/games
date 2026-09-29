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
        public void EachConditionalAlternativeAndItsFallbackAreReachable()
        {
            var content = StoryContent.Create();
            foreach (var conditional in content.Visitors.Where(visitor => !string.IsNullOrEmpty(visitor.RequiredFlag)))
            {
                var flagged = CompleteUntilDay(content, conditional.Day, 41, null);
                Assert.That(flagged.FrozenQueueForDay(conditional.Day).Any(visitor => visitor.Id == conditional.Id), Is.True,
                    "required alternative=" + conditional.Id);

                var unflagged = CompleteUntilDay(content, conditional.Day, 41, conditional.RequiredFlag);
                Assert.That(unflagged.FrozenQueueForDay(conditional.Day).Any(visitor => visitor.Id == conditional.Id), Is.False,
                    "fallback alternative=" + conditional.Id);
            }
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

        private static RunStateMachine CompleteUntilDay(GameContent content, int targetDay, int seed, string suppressFlag)
        {
            var run = new RunStateMachine(content, seed);
            run.StartRun();
            while (run.State.Day < targetDay)
            {
                if (run.State.Phase == RunPhase.DaySummary)
                {
                    run.AdvanceDay();
                    continue;
                }
                var visitor = run.CurrentVisitor();
                var expected = RuleEvaluator.Evaluate(visitor, run.CurrentDecree()).Expected;
                var choice = Produces(visitor, suppressFlag, expected) ? Opposite(expected) : expected;
                run.Resolve(choice);
            }
            return run;
        }

        private static bool Produces(VisitorDefinition visitor, string flag, Decision chosen)
            => !string.IsNullOrEmpty(flag) && ((chosen == Decision.Admit && visitor.FlagOnAdmit == flag) || (chosen == Decision.Deny && visitor.FlagOnDeny == flag));

        private static Decision Opposite(Decision value) => value == Decision.Admit ? Decision.Deny : Decision.Admit;

        private readonly struct FlagProducer
        {
            public readonly string Flag;
            public readonly int Day;
            public FlagProducer(string flag, int day) { Flag = flag; Day = day; }
        }
    }
}
