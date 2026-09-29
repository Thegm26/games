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
            var distanceCommit = SwipeDecisionResolver.Resolve(-SwipeCard.DistanceThreshold, 0f);
            var velocityCommit = SwipeDecisionResolver.Resolve(2f, SwipeCard.VelocityThreshold);
            Assert.That(snapback.Commit, Is.False);
            Assert.That(distanceCommit.Commit, Is.True);
            Assert.That(distanceCommit.Decision, Is.EqualTo(Decision.Admit));
            Assert.That(velocityCommit.Commit, Is.True);
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
