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
        /// <summary>Higher alternatives are selected first when their prerequisite is satisfied; a no-prerequisite fallback is required.</summary>
        public int AlternativePriority;
        public string DisplayName = "";
        public string Dialogue = "";
        public string PortraitKey = "placeholder";
        public List<string> Traits = new List<string>();
        public List<string> Documents = new List<string>();
        public List<string> VisibleCues = new List<string>();
        public string StoryChainId = "";
        public string RequiredFlag = "";
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
        public readonly Decision Expected;
        public readonly string RuleId;
        public readonly string Explanation;
        public RuleEvaluation(Decision expected, string ruleId, string explanation)
        {
            Expected = expected;
            RuleId = ruleId;
            Explanation = explanation;
        }
    }
}
