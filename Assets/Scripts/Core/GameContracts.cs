using System;
using System.Collections.Generic;

namespace WhoEnters.Core
{
    public enum Decision { Admit, Deny }
    public enum RunPhase { Title, Tutorial, Encounter, DaySummary, Ending }
    public enum EndingKind { CastleFallen, HollowVictory, GateHeld, TheGateRemembers }

    [Serializable]
    public sealed class VisitorDefinition
    {
        public string Id = "";
        public int Day;
        /// <summary>Stable 1-based position in the day's eight encounter slots. Multiple visitors may share a slot as alternatives.</summary>
        public int EncounterSlot;
        /// <summary>Higher alternatives are selected first when their prerequisites are satisfied; a no-prerequisite fallback is required.</summary>
        public int AlternativePriority;
        public string DisplayName = "";
        public string Dialogue = "";
        public string PortraitKey = "placeholder";
        public List<string> Traits = new List<string>();
        public List<string> Documents = new List<string>();
        public List<string> VisibleCues = new List<string>();
        public string StoryChainId = "";
        /// <summary>Legacy single prerequisite, retained for authored content compatibility.</summary>
        public string RequiredFlag = "";
        /// <summary>Additional prerequisites combined with <see cref="RequiredFlag"/> using AND semantics.</summary>
        public List<string> RequiredFlags = new List<string>();
        public string FlagOnAdmit = "";
        public string FlagOnDeny = "";
    }

    [Serializable]
    public sealed class RuleDefinition
    {
        public string Id = "";
        public string Label = "";
        public int Priority;
        public Decision Verdict;
        public List<string> RequiredTraits = new List<string>();
        public List<string> RequiredDocuments = new List<string>();
        public List<string> RequiredVisibleCues = new List<string>();
    }

    [Serializable]
    public sealed class DecreeDefinition
    {
        public int Day;
        public string Title = "";
        public string DisplayText = "";
        public Decision DefaultVerdict = Decision.Deny;
        public List<RuleDefinition> Rules = new List<RuleDefinition>();
    }

    [Serializable]
    public sealed class GameContent
    {
        public List<VisitorDefinition> Visitors = new List<VisitorDefinition>();
        public List<DecreeDefinition> Decrees = new List<DecreeDefinition>();
        public int TotalDays = 5;
        public int VisitorsPerDay = 8;
    }

    public sealed class VerdictRecord
    {
        public string VisitorId = "";
        public Decision Chosen;
        public Decision Expected;
        public string RuleId = "";
        public string RuleExplanation = "";
        public bool Correct;
        public int ScoreDelta;
        public int IntegrityDelta;
    }

    public sealed class GameState
    {
        public RunPhase Phase = RunPhase.Title;
        public int Day = 1;
        public int EncounterIndex;
        public int Integrity = 5;
        public int Score;
        public int Streak;
        public int Seed;
        public readonly HashSet<string> StoryFlags = new HashSet<string>();
        public readonly List<VerdictRecord> Verdicts = new List<VerdictRecord>();
        internal readonly Dictionary<int, VisitorDefinition[]> FrozenDayQueues = new Dictionary<int, VisitorDefinition[]>();
    }

    public readonly struct RuleEvaluation
    {
        private static readonly IReadOnlyList<RuleMatchDiagnostic> EmptyDiagnostics = Array.AsReadOnly(Array.Empty<RuleMatchDiagnostic>());

        public readonly Decision Expected;
        public readonly string RuleId;
        public readonly string Explanation;
        public readonly bool UsedDefault;
        public readonly IReadOnlyList<RuleMatchDiagnostic> MatchedRules;
        public readonly IReadOnlyList<RuleMatchDiagnostic> OpposingVerdictRules;

        public RuleEvaluation(Decision expected, string ruleId, string explanation)
            : this(expected, ruleId, explanation, false, EmptyDiagnostics, EmptyDiagnostics)
        {
        }

        public RuleEvaluation(
            Decision expected,
            string ruleId,
            string explanation,
            bool usedDefault,
            IReadOnlyList<RuleMatchDiagnostic> matchedRules,
            IReadOnlyList<RuleMatchDiagnostic> opposingVerdictRules)
        {
            Expected = expected;
            RuleId = ruleId;
            Explanation = explanation;
            UsedDefault = usedDefault;
            MatchedRules = Snapshot(matchedRules);
            OpposingVerdictRules = Snapshot(opposingVerdictRules);
        }

        private static IReadOnlyList<RuleMatchDiagnostic> Snapshot(IReadOnlyList<RuleMatchDiagnostic> diagnostics)
        {
            if (diagnostics == null || diagnostics.Count == 0)
                return EmptyDiagnostics;

            var snapshot = new RuleMatchDiagnostic[diagnostics.Count];
            for (var index = 0; index < snapshot.Length; index++)
                snapshot[index] = diagnostics[index];

            return Array.AsReadOnly(snapshot);
        }
    }

    public readonly struct RuleMatchDiagnostic
    {
        public readonly string Id;
        public readonly Decision Verdict;
        public readonly int Priority;
        public readonly string Explanation;

        public RuleMatchDiagnostic(string id, Decision verdict, int priority, string explanation)
        {
            Id = id;
            Verdict = verdict;
            Priority = priority;
            Explanation = explanation;
        }
    }
}
