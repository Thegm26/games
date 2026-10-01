using System;
using System.Collections.Generic;
using System.Linq;

namespace WhoEnters.Core
{
    public static class RuleEvaluator
    {
        public static RuleEvaluation Evaluate(VisitorDefinition visitor, DecreeDefinition decree)
        {
            var matchedDiagnostics = new List<RuleMatchDiagnostic>();
            // LINQ's stable OrderBy preserves authored list order when priorities tie.
            var ordered = decree.Rules.OrderByDescending(rule => rule.Priority);
            foreach (var rule in ordered)
            {
                var traitMatch = ContainsAll(visitor.Traits, rule.RequiredTraits);
                var documentMatch = ContainsAll(visitor.Documents, rule.RequiredDocuments);
                var cueMatch = ContainsAll(visitor.VisibleCues, rule.RequiredVisibleCues);
                var isMatch = traitMatch && documentMatch && cueMatch;
                DebugTrace.Log("rule.predicate", $"visitor={visitor.Id};rule={rule.Id};traits={traitMatch};docs={documentMatch};cues={cueMatch};match={isMatch}");
                if (isMatch)
                {
                    matchedDiagnostics.Add(new RuleMatchDiagnostic(rule.Id, rule.Verdict, rule.Priority, rule.Label));
                }
            }

            if (matchedDiagnostics.Count > 0)
            {
                var selected = matchedDiagnostics[0];
                var matchedRules = Snapshot(matchedDiagnostics);
                var opposingRules = Snapshot(matchedDiagnostics.Where(rule => rule.Verdict != selected.Verdict));
                DebugTrace.Log("rule.evaluated", $"visitor={visitor.Id};expected={selected.Verdict};rule={selected.Id};usedDefault=false;matchedRules={RuleIds(matchedRules)};opposingVerdictRules={RuleIds(opposingRules)}");
                return new RuleEvaluation(selected.Verdict, selected.Id, selected.Explanation, false, matchedRules, opposingRules);
            }

            const string defaultRuleId = "default.no_exception";
            const string defaultExplanation = "No exception applies.";
            var noMatches = Snapshot(matchedDiagnostics);
            DebugTrace.Log("rule.default", $"visitor={visitor.Id};decision={decree.DefaultVerdict};rule={defaultRuleId};usedDefault=true");
            DebugTrace.Log("rule.evaluated", $"visitor={visitor.Id};expected={decree.DefaultVerdict};rule={defaultRuleId};usedDefault=true;matchedRules=;opposingVerdictRules=");
            return new RuleEvaluation(decree.DefaultVerdict, defaultRuleId, defaultExplanation, true, noMatches, noMatches);
        }

        private static IReadOnlyList<RuleMatchDiagnostic> Snapshot(IEnumerable<RuleMatchDiagnostic> diagnostics)
            => Array.AsReadOnly(diagnostics.ToArray());

        private static string RuleIds(IReadOnlyList<RuleMatchDiagnostic> diagnostics)
            => string.Join(",", diagnostics.Select(diagnostic => diagnostic.Id));

        private static bool ContainsAll(List<string> source, List<string> required)
        {
            return required.Count == 0 || required.All(source.Contains);
        }
    }
}
