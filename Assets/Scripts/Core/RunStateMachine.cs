using System;
using System.Collections.Generic;
using System.Linq;

namespace WhoEnters.Core
{
    public sealed class RunStateMachine
    {
        public GameState State { get; }
        public GameContent Content { get; }

        public RunStateMachine(GameContent content, int seed)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            State = new GameState { Seed = seed };
            ValidateContent(content);
            DebugTrace.Log("state.run_created", $"seed={seed};days={content.TotalDays};visitors={content.Visitors.Count}");
        }

        public VisitorDefinition CurrentVisitor()
        {
            var list = QueueForDay(State.Day);
            return State.EncounterIndex < list.Length ? list[State.EncounterIndex] : null;
        }

        public DecreeDefinition CurrentDecree() => Content.Decrees.FirstOrDefault(decree => decree.Day == State.Day);

        public VerdictRecord Resolve(Decision chosen)
        {
            if (State.Phase != RunPhase.Encounter) throw new InvalidOperationException("Verdicts may only be resolved during an encounter.");
            var visitor = CurrentVisitor();
            if (visitor == null)
            {
                DebugTrace.Error("state.missing_visitor", $"day={State.Day};index={State.EncounterIndex}");
                throw new InvalidOperationException("No active visitor.");
            }
            var decree = CurrentDecree();
            if (decree == null)
            {
                DebugTrace.Error("state.missing_decree", $"day={State.Day};visitor={visitor.Id}");
                throw new InvalidOperationException("No decree for active day.");
            }
            var evaluation = RuleEvaluator.Evaluate(visitor, decree);
            var correct = evaluation.Expected == chosen;
            var scoreDelta = correct ? 100 + Math.Min(State.Streak, 4) * 25 : 0;
            var integrityDelta = correct ? 0 : -1;
            State.Score += scoreDelta;
            State.Streak = correct ? State.Streak + 1 : 0;
            State.Integrity += integrityDelta;
            AddFlag(chosen == Decision.Admit ? visitor.FlagOnAdmit : visitor.FlagOnDeny);
            var record = new VerdictRecord { VisitorId = visitor.Id, Chosen = chosen, Expected = evaluation.Expected, Correct = correct, ScoreDelta = scoreDelta, IntegrityDelta = integrityDelta };
            State.Verdicts.Add(record);
            DebugTrace.Log("verdict.resolved", $"visitor={visitor.Id};choice={chosen};expected={evaluation.Expected};rule={evaluation.RuleId};correct={correct};scoreDelta={scoreDelta};integrityDelta={integrityDelta};score={State.Score};streak={State.Streak};integrity={State.Integrity}");
            Advance();
            return record;
        }

        public void StartRun()
        {
            FreezeDayQueue(State.Day);
            State.Phase = RunPhase.Encounter;
            DebugTrace.Log("state.phase", "from=Title;to=Encounter");
        }

        public void AdvanceDay()
        {
            if (State.Phase != RunPhase.DaySummary) return;
            State.Day++;
            State.EncounterIndex = 0;
            FreezeDayQueue(State.Day);
            State.Phase = RunPhase.Encounter;
            DebugTrace.Log("state.day_started", $"day={State.Day}");
        }

        public IReadOnlyList<VisitorDefinition> FrozenQueueForDay(int day) => QueueForDay(day);

        public EndingKind Ending()
        {
            if (State.Integrity <= 0) return EndingKind.CastleFallen;
            if (State.StoryFlags.Contains("secret_gate_remembers")) return EndingKind.TheGateRemembers;
            return State.Integrity <= 2 ? EndingKind.HollowVictory : EndingKind.GateHeld;
        }

        private void Advance()
        {
            if (State.Integrity <= 0)
            {
                State.Phase = RunPhase.Ending;
                DebugTrace.Log("state.phase", "to=Ending;reason=integrity_depleted");
                return;
            }
            State.EncounterIndex++;
            if (State.EncounterIndex < QueueForDay(State.Day).Length) return;
            if (State.Day >= Content.TotalDays)
            {
                State.Phase = RunPhase.Ending;
                DebugTrace.Log("state.phase", "to=Ending;reason=run_completed");
                return;
            }
            State.Phase = RunPhase.DaySummary;
            DebugTrace.Log("state.phase", $"to=DaySummary;day={State.Day}");
        }

        private void AddFlag(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) return;
            if (State.StoryFlags.Add(flag)) DebugTrace.Log("story.flag_added", $"flag={flag}");
        }

        private void FreezeDayQueue(int day)
        {
            if (State.FrozenDayQueues.ContainsKey(day)) return;
            var selectedBySlot = Content.Visitors.Where(visitor => visitor.Day == day)
                .GroupBy(visitor => visitor.EncounterSlot)
                .OrderBy(group => group.Key)
                .Select(group => SelectAlternative(group, day))
                .ToArray();
            var queue = OrderFrozenQueue(selectedBySlot);
            State.FrozenDayQueues.Add(day, queue);
            DebugTrace.Log("visitor.queue_frozen", $"day={day};ids={string.Join(",", queue.Select(visitor => visitor.Id))}");
        }

        private VisitorDefinition SelectAlternative(IEnumerable<VisitorDefinition> candidates, int day)
        {
            foreach (var candidate in candidates.OrderByDescending(visitor => visitor.AlternativePriority).ThenBy(visitor => StableOrder(visitor.Id, State.Seed)))
            {
                var eligible = string.IsNullOrWhiteSpace(candidate.RequiredFlag) || State.StoryFlags.Contains(candidate.RequiredFlag);
                DebugTrace.Log("visitor.alternative_evaluated", $"day={day};slot={candidate.EncounterSlot};visitor={candidate.Id};requiredFlag={candidate.RequiredFlag};eligible={eligible}");
                if (eligible) return candidate;
            }
            DebugTrace.Error("visitor.alternative_missing", $"day={day};slot={candidates.First().EncounterSlot}");
            throw new InvalidOperationException($"No eligible visitor alternative for day {day}.");
        }

        private VisitorDefinition[] OrderFrozenQueue(VisitorDefinition[] selectedBySlot)
        {
            // Story-chain encounters retain their authored slot; independent encounters are seeded and fill the remaining slots.
            var queue = new VisitorDefinition[selectedBySlot.Length];
            foreach (var anchor in selectedBySlot.Where(visitor => !string.IsNullOrWhiteSpace(visitor.StoryChainId)))
            {
                queue[anchor.EncounterSlot - 1] = anchor;
            }
            var fillers = selectedBySlot.Where(visitor => string.IsNullOrWhiteSpace(visitor.StoryChainId))
                .OrderBy(visitor => StableOrder(visitor.Id, State.Seed ^ State.Day)).ToArray();
            var fillerIndex = 0;
            for (var index = 0; index < queue.Length; index++)
            {
                if (queue[index] == null) queue[index] = fillers[fillerIndex++];
            }
            return queue;
        }

        private VisitorDefinition[] QueueForDay(int day)
        {
            if (!State.FrozenDayQueues.TryGetValue(day, out var queue))
                throw new InvalidOperationException($"Day {day} queue has not been frozen.");
            return queue;
        }

        private static int StableOrder(string id, int seed)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var character in id) { hash ^= character; hash *= 16777619; }
                hash ^= (uint)seed + 0x9e3779b9u + (hash << 6) + (hash >> 2);
                hash ^= hash >> 16;
                hash *= 0x7feb352du;
                hash ^= hash >> 15;
                return (int)hash;
            }
        }

        private static void ValidateContent(GameContent content)
        {
            Assert(content.TotalDays == 5, "content.invalid_total_days", $"expected=5;actual={content.TotalDays}");
            Assert(content.VisitorsPerDay == 8, "content.invalid_visitors_per_day", $"expected=8;actual={content.VisitorsPerDay}");
            var expectedDays = Enumerable.Range(1, content.TotalDays).ToArray();
            var decreeDays = content.Decrees.Select(decree => decree.Day).OrderBy(day => day).ToArray();
            Assert(decreeDays.SequenceEqual(expectedDays), "content.invalid_decrees", $"expected={string.Join(",", expectedDays)};actual={string.Join(",", decreeDays)}");
            Assert(content.Decrees.All(decree => decree != null && !string.IsNullOrWhiteSpace(decree.Title) && !string.IsNullOrWhiteSpace(decree.DisplayText)), "content.invalid_decree_fields", "title/displayText required");
            Assert(content.Visitors.Count >= content.TotalDays * content.VisitorsPerDay, "content.invalid_visitor_count", $"expectedAtLeast={content.TotalDays * content.VisitorsPerDay};actual={content.Visitors.Count}");
            Assert(content.Visitors.All(visitor => visitor != null && visitor.Day >= 1 && visitor.Day <= content.TotalDays && visitor.EncounterSlot >= 1 && visitor.EncounterSlot <= content.VisitorsPerDay && !string.IsNullOrWhiteSpace(visitor.Id)), "content.invalid_visitor_fields", "id/day/encounterSlot required");
            Assert(content.Visitors.Select(visitor => visitor.Id).Distinct().Count() == content.Visitors.Count, "content.duplicate_visitor_id", "visitor ids must be unique");
            foreach (var day in expectedDays)
            {
                var slots = content.Visitors.Where(visitor => visitor.Day == day).Select(visitor => visitor.EncounterSlot).Distinct().OrderBy(slot => slot).ToArray();
                Assert(slots.SequenceEqual(Enumerable.Range(1, content.VisitorsPerDay)), "content.invalid_day_slots", $"day={day};expected=1..{content.VisitorsPerDay};actual={string.Join(",", slots)}");
                foreach (var slot in slots)
                {
                    var alternatives = content.Visitors.Where(visitor => visitor.Day == day && visitor.EncounterSlot == slot).ToArray();
                    Assert(alternatives.Any(visitor => string.IsNullOrWhiteSpace(visitor.RequiredFlag)), "content.missing_slot_fallback", $"day={day};slot={slot}");
                }
            }
            var flagsByProducerDay = new Dictionary<string, int>();
            foreach (var visitor in content.Visitors)
            {
                RegisterProducedFlag(flagsByProducerDay, visitor.FlagOnAdmit, visitor.Day);
                RegisterProducedFlag(flagsByProducerDay, visitor.FlagOnDeny, visitor.Day);
            }
            foreach (var visitor in content.Visitors.Where(visitor => !string.IsNullOrWhiteSpace(visitor.RequiredFlag)))
            {
                Assert(flagsByProducerDay.TryGetValue(visitor.RequiredFlag, out var producerDay) && producerDay < visitor.Day,
                    "content.invalid_flag_reference", $"visitor={visitor.Id};required={visitor.RequiredFlag}");
            }
            DebugTrace.Log("content.validated", $"decrees={content.Decrees.Count};visitors={content.Visitors.Count};days={content.TotalDays}");
        }

        private static void RegisterProducedFlag(Dictionary<string, int> flagsByProducerDay, string flag, int day)
        {
            if (string.IsNullOrWhiteSpace(flag)) return;
            if (!flagsByProducerDay.TryGetValue(flag, out var existingDay) || day < existingDay) flagsByProducerDay[flag] = day;
        }

        private static void Assert(bool condition, string eventId, string payload)
        {
            if (condition) return;
            DebugTrace.Error(eventId, payload);
            throw new ArgumentException(payload);
        }
    }
}
