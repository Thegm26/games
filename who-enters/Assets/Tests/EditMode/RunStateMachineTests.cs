using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WhoEnters.Core;
using WhoEnters.Gameplay;
using WhoEnters.UI;

namespace WhoEnters.Tests.EditMode
{
    public sealed class RunStateMachineTests
    {
        [Test]
        public void HigherPriorityRuleWinsAndEmitsPredicateTrace()
        {
            DebugTrace.Clear();
            var visitor = new VisitorDefinition { Id = "test", Traits = { "cursed" }, Documents = { "moon seal" } };
            var decree = new DecreeDefinition
            {
                DefaultVerdict = Decision.Deny,
                Rules =
                {
                    new RuleDefinition { Id = "admit", Priority = 10, Verdict = Decision.Admit, RequiredDocuments = { "moon seal" } },
                    new RuleDefinition { Id = "deny", Priority = 20, Verdict = Decision.Deny, RequiredTraits = { "cursed" } },
                },
            };

            var result = RuleEvaluator.Evaluate(visitor, decree);

            Assert.That(result.Expected, Is.EqualTo(Decision.Deny));
            Assert.That(result.RuleId, Is.EqualTo("deny"));
            Assert.That(DebugTrace.Recent.Any(entry => entry.EventId == "rule.predicate"), Is.True);
        }

        [Test]
        public void ConflictingMatchedRulesUseHighestPriorityAndReportOpposingRules()
        {
            var visitor = new VisitorDefinition { Id = "conflict", Traits = { "cursed", "licensed" } };
            var decree = new DecreeDefinition
            {
                Rules =
                {
                    new RuleDefinition { Id = "admit-license", Priority = 10, Verdict = Decision.Admit, RequiredTraits = { "licensed" } },
                    new RuleDefinition { Id = "deny-curse", Priority = 20, Verdict = Decision.Deny, RequiredTraits = { "cursed" } },
                    new RuleDefinition { Id = "admit-appeal", Priority = 5, Verdict = Decision.Admit, RequiredTraits = { "cursed" } },
                },
            };

            var result = RuleEvaluator.Evaluate(visitor, decree);

            Assert.That(result.Expected, Is.EqualTo(Decision.Deny));
            Assert.That(result.UsedDefault, Is.False);
            Assert.That(result.MatchedRules.Select(rule => rule.Id), Is.EqualTo(new[] { "deny-curse", "admit-license", "admit-appeal" }));
            Assert.That(result.OpposingVerdictRules.Select(rule => rule.Id), Is.EqualTo(new[] { "admit-license", "admit-appeal" }));
            Assert.That(result.MatchedRules[0].Verdict, Is.EqualTo(Decision.Deny));
            Assert.That(result.MatchedRules[0].Priority, Is.EqualTo(20));
            Assert.That(result.MatchedRules[0].Explanation, Is.Empty);
        }

        [Test]
        public void EqualPriorityRulesUseTheirAuthoredOrder()
        {
            var visitor = new VisitorDefinition { Id = "tie", Traits = { "first", "second" } };
            var decree = new DecreeDefinition
            {
                Rules =
                {
                    new RuleDefinition { Id = "z-authored-first", Priority = 10, Verdict = Decision.Admit, RequiredTraits = { "first" } },
                    new RuleDefinition { Id = "a-authored-second", Priority = 10, Verdict = Decision.Deny, RequiredTraits = { "second" } },
                },
            };

            var result = RuleEvaluator.Evaluate(visitor, decree);

            Assert.That(result.Expected, Is.EqualTo(Decision.Admit));
            Assert.That(result.RuleId, Is.EqualTo("z-authored-first"));
            Assert.That(result.MatchedRules.Select(rule => rule.Id), Is.EqualTo(new[] { "z-authored-first", "a-authored-second" }));
        }

        [Test]
        public void IncompleteCompoundRuleDoesNotMatch()
        {
            var visitor = new VisitorDefinition { Id = "near-miss", Traits = { "cursed" } };
            var decree = new DecreeDefinition
            {
                DefaultVerdict = Decision.Admit,
                Rules = { new RuleDefinition { Id = "deny-cursed-with-seal", Priority = 10, Verdict = Decision.Deny, RequiredTraits = { "cursed" }, RequiredDocuments = { "moon seal" } } },
            };

            var result = RuleEvaluator.Evaluate(visitor, decree);

            Assert.That(result.UsedDefault, Is.True);
            Assert.That(result.MatchedRules, Is.Empty);
            Assert.That(result.Expected, Is.EqualTo(Decision.Admit));
        }

        [Test]
        public void DefaultEvaluationUsesExplicitNoExceptionContract()
        {
            var result = RuleEvaluator.Evaluate(new VisitorDefinition { Id = "default" }, new DecreeDefinition { DefaultVerdict = Decision.Deny });

            Assert.That(result.RuleId, Is.EqualTo("default.no_exception"));
            Assert.That(result.Explanation, Is.EqualTo("No exception applies."));
            Assert.That(result.UsedDefault, Is.True);
            Assert.That(result.MatchedRules, Is.Empty);
            Assert.That(result.OpposingVerdictRules, Is.Empty);
        }

        [Test]
        public void RuleEvaluationSnapshotsCallerSuppliedDiagnosticLists()
        {
            var matchedRules = new List<RuleMatchDiagnostic>
            {
                new RuleMatchDiagnostic("matched-before", Decision.Deny, 20, "matched explanation"),
            };
            var opposingRules = new List<RuleMatchDiagnostic>
            {
                new RuleMatchDiagnostic("opposing-before", Decision.Admit, 10, "opposing explanation"),
            };
            var evaluation = new RuleEvaluation(Decision.Deny, "selected", "selected explanation", false, matchedRules, opposingRules);

            matchedRules[0] = new RuleMatchDiagnostic("matched-after", Decision.Admit, 0, "changed");
            matchedRules.Add(new RuleMatchDiagnostic("matched-added", Decision.Admit, 0, "added"));
            opposingRules.Clear();
            opposingRules.Add(new RuleMatchDiagnostic("opposing-after", Decision.Deny, 0, "changed"));

            Assert.That(evaluation.MatchedRules.Select(rule => rule.Id), Is.EqualTo(new[] { "matched-before" }));
            Assert.That(evaluation.OpposingVerdictRules.Select(rule => rule.Id), Is.EqualTo(new[] { "opposing-before" }));
            Assert.That(((ICollection<RuleMatchDiagnostic>)evaluation.MatchedRules).IsReadOnly, Is.True);
            Assert.That(((ICollection<RuleMatchDiagnostic>)evaluation.OpposingVerdictRules).IsReadOnly, Is.True);
        }

        [Test]
        public void ResolveCopiesVerdictContractWithoutChangingScoringOrState()
        {
            var machine = new RunStateMachine(DevelopmentContent.Create(), 44);
            machine.StartRun();
            var evaluation = RuleEvaluator.Evaluate(machine.CurrentVisitor(), machine.CurrentDecree());

            var record = machine.Resolve(evaluation.Expected);

            Assert.That(record.RuleId, Is.EqualTo(evaluation.RuleId));
            Assert.That(record.RuleExplanation, Is.EqualTo(evaluation.Explanation));
            Assert.That(record.Correct, Is.True);
            Assert.That(record.ScoreDelta, Is.EqualTo(100));
            Assert.That(record.IntegrityDelta, Is.EqualTo(0));
            Assert.That(machine.State.Score, Is.EqualTo(100));
            Assert.That(machine.State.Streak, Is.EqualTo(1));
            Assert.That(machine.State.Integrity, Is.EqualTo(5));
            Assert.That(machine.State.EncounterIndex, Is.EqualTo(1));
            Assert.That(machine.State.Phase, Is.EqualTo(RunPhase.Encounter));
        }

        [Test]
        public void FrozenQueuesStayStableAndFullRunReachesGateHeld()
        {
            DebugTrace.Clear();
            var machine = new RunStateMachine(DevelopmentContent.Create(), 260928);
            machine.StartRun();
            var initialQueue = machine.FrozenQueueForDay(1).Select(visitor => visitor.Id).ToArray();
            var first = machine.CurrentVisitor();
            var expected = RuleEvaluator.Evaluate(first, machine.CurrentDecree()).Expected;
            machine.Resolve(expected);
            Assert.That(machine.FrozenQueueForDay(1).Select(visitor => visitor.Id), Is.EqualTo(initialQueue));

            while (machine.State.Phase != RunPhase.Ending)
            {
                if (machine.State.Phase == RunPhase.DaySummary)
                {
                    machine.AdvanceDay();
                    continue;
                }
                var visitor = machine.CurrentVisitor();
                machine.Resolve(RuleEvaluator.Evaluate(visitor, machine.CurrentDecree()).Expected);
            }

            Assert.That(machine.State.Verdicts.Count, Is.EqualTo(40));
            Assert.That(machine.State.Integrity, Is.EqualTo(5));
            Assert.That(machine.Ending(), Is.EqualTo(EndingKind.TheGateRemembers));
            Assert.That(DebugTrace.Recent.Any(entry => entry.EventId == "verdict.resolved"), Is.True);
        }

        [Test]
        public void InvalidContentFailsBeforeRunStarts()
        {
            var invalid = DevelopmentContent.Create();
            invalid.Visitors.RemoveAt(0);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("content.invalid_visitor_count"));
            Assert.Throws<System.ArgumentException>(() => new RunStateMachine(invalid, 1));
        }

        [Test]
        public void ListOnlyPrerequisiteCannotMasqueradeAsASlotFallback()
        {
            var invalid = DevelopmentContent.Create();
            invalid.Visitors.Single(visitor => visitor.Id == "dev-d1-v8").FlagOnAdmit = "courier-approved";
            var onlyDayTwoSlotOneAlternative = invalid.Visitors.Single(visitor => visitor.Day == 2 && visitor.EncounterSlot == 1);
            onlyDayTwoSlotOneAlternative.RequiredFlags.Add("courier-approved");

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("content.missing_slot_fallback"));
            Assert.Throws<System.ArgumentException>(() => new RunStateMachine(invalid, 1));
        }

        [Test]
        public void ConditionalAlternativeIsChosenOnlyFromFlagsFrozenBeforeTheDay()
        {
            var content = DevelopmentContent.Create();
            var flagProducer = content.Visitors.Single(visitor => visitor.Id == "dev-d1-v8");
            flagProducer.FlagOnAdmit = "saved-bellringer";
            content.Visitors.Add(new VisitorDefinition
            {
                Id = "day2-bellringer-returned", Day = 2, EncounterSlot = 1, AlternativePriority = 10,
                DisplayName = "Bellringer Returned", Dialogue = "The gate remembers.", RequiredFlag = "saved-bellringer",
            });
            var machine = new RunStateMachine(content, 77);
            machine.StartRun();
            while (machine.State.Phase == RunPhase.Encounter)
            {
                var visitor = machine.CurrentVisitor();
                machine.Resolve(RuleEvaluator.Evaluate(visitor, machine.CurrentDecree()).Expected);
            }
            machine.AdvanceDay();
            Assert.That(machine.FrozenQueueForDay(2).Any(visitor => visitor.Id == "day2-bellringer-returned"), Is.True);

            var unflagged = DevelopmentContent.Create();
            unflagged.Visitors.Single(visitor => visitor.Id == "dev-d1-v8").FlagOnDeny = "saved-bellringer";
            unflagged.Visitors.Add(new VisitorDefinition
            {
                Id = "day2-bellringer-returned", Day = 2, EncounterSlot = 1, AlternativePriority = 10,
                DisplayName = "Bellringer Returned", Dialogue = "The gate remembers.", RequiredFlag = "saved-bellringer",
            });
            var unflaggedMachine = new RunStateMachine(unflagged, 77);
            unflaggedMachine.StartRun();
            while (unflaggedMachine.State.Phase == RunPhase.Encounter)
            {
                var visitor = unflaggedMachine.CurrentVisitor();
                unflaggedMachine.Resolve(RuleEvaluator.Evaluate(visitor, unflaggedMachine.CurrentDecree()).Expected);
            }
            unflaggedMachine.AdvanceDay();
            Assert.That(unflaggedMachine.FrozenQueueForDay(2).Any(visitor => visitor.Id == "day2-bellringer-returned"), Is.False);
        }

        [Test]
        public void ScoreIntegrityAndEndingBandsFollowThePublishedRules()
        {
            var machine = new RunStateMachine(DevelopmentContent.Create(), 22);
            machine.StartRun();
            var expectedFirst = RuleEvaluator.Evaluate(machine.CurrentVisitor(), machine.CurrentDecree()).Expected;
            machine.Resolve(expectedFirst == Decision.Admit ? Decision.Deny : Decision.Admit);
            Assert.That(machine.State.Score, Is.EqualTo(0));
            Assert.That(machine.State.Streak, Is.EqualTo(0));
            Assert.That(machine.State.Integrity, Is.EqualTo(4));
            var correct = RuleEvaluator.Evaluate(machine.CurrentVisitor(), machine.CurrentDecree()).Expected;
            machine.Resolve(correct);
            Assert.That(machine.State.Score, Is.EqualTo(100));
            Assert.That(machine.State.Streak, Is.EqualTo(1));

            machine.State.Integrity = 2;
            Assert.That(machine.Ending(), Is.EqualTo(EndingKind.HollowVictory));
            machine.State.StoryFlags.Add("secret_gate_remembers");
            Assert.That(machine.Ending(), Is.EqualTo(EndingKind.TheGateRemembers));
            machine.State.Integrity = 0;
            Assert.That(machine.Ending(), Is.EqualTo(EndingKind.CastleFallen));
        }

        [Test]
        public void SeedsAndSwipeThresholdsAreDeterministicAndTraceable()
        {
            var first = new RunStateMachine(DevelopmentContent.Create(), 99);
            var second = new RunStateMachine(DevelopmentContent.Create(), 99);
            first.StartRun();
            second.StartRun();
            Assert.That(first.FrozenQueueForDay(1).Select(visitor => visitor.Id), Is.EqualTo(second.FrozenQueueForDay(1).Select(visitor => visitor.Id)));
            var changedSeed = new RunStateMachine(DevelopmentContent.Create(), 100);
            changedSeed.StartRun();
            Assert.That(first.FrozenQueueForDay(1).Select(visitor => visitor.Id), Is.Not.EqualTo(changedSeed.FrozenQueueForDay(1).Select(visitor => visitor.Id)));

            var anchoredContent = DevelopmentContent.Create();
            anchoredContent.Visitors.Single(visitor => visitor.Id == "dev-d1-v2").StoryChainId = "bellringer";
            var anchored = new RunStateMachine(anchoredContent, 101);
            anchored.StartRun();
            Assert.That(anchored.FrozenQueueForDay(1)[1].Id, Is.EqualTo("dev-d1-v2"));

            DebugTrace.Clear();
            var snapback = SwipeDecisionResolver.Resolve(20f, 10f);
            var leftDistanceCommit = SwipeDecisionResolver.Resolve(-SwipeCard.DistanceThreshold, 0f);
            var rightDistanceCommit = SwipeDecisionResolver.Resolve(SwipeCard.DistanceThreshold, 0f);
            var velocityCommit = SwipeDecisionResolver.Resolve(2f, SwipeCard.VelocityThreshold);
            Assert.That(snapback.Commit, Is.False);
            Assert.That(leftDistanceCommit.Commit, Is.True);
            Assert.That(leftDistanceCommit.Decision, Is.EqualTo(Decision.Deny));
            Assert.That(rightDistanceCommit.Commit, Is.True);
            Assert.That(rightDistanceCommit.Decision, Is.EqualTo(Decision.Admit));
            Assert.That(velocityCommit.Commit, Is.True);
            Assert.That(GameDirector.TryKeyboardDecision(KeyCode.LeftArrow, out var leftKey), Is.True);
            Assert.That(leftKey, Is.EqualTo(Decision.Deny));
            Assert.That(GameDirector.TryKeyboardDecision(KeyCode.RightArrow, out var rightKey), Is.True);
            Assert.That(rightKey, Is.EqualTo(Decision.Admit));
            Assert.That(DebugTrace.Recent.Any(entry => entry.EventId == "input.swipe_evaluated"), Is.True);
        }

        [Test]
        public void RuntimeDirectorBuildsItsCanvasWithTheUnitySixFont()
        {
            var host = new GameObject("test-runtime-host");
            host.AddComponent<GameAudio>();
            var director = host.AddComponent<GameDirector>();
            if (Object.FindAnyObjectByType<Canvas>() == null)
                typeof(GameDirector).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(director, null);
            Assert.That(Object.FindAnyObjectByType<Canvas>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<SwipeCard>(), Is.Not.Null);
            Object.DestroyImmediate(host);
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) Object.DestroyImmediate(canvas.gameObject);
            foreach (var eventSystem in Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None)) Object.DestroyImmediate(eventSystem.gameObject);
        }
    }
}
