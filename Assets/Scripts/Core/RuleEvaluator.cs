using System.Collections.Generic;
using System.Linq;

namespace WhoEnters.Core
{
    public static class RuleEvaluator
    {
        public static RuleEvaluation Evaluate(VisitorDefinition visitor, DecreeDefinition decree)
        {
            var ordered = decree.Rules.OrderByDescending(rule => rule.Priority).ThenBy(rule => rule.Id);
            foreach (var rule in ordered)
            {
                var traitMatch = ContainsAll(visitor.Traits, rule.RequiredTraits);
                var documentMatch = ContainsAll(visitor.Documents, rule.RequiredDocuments);
                var cueMatch = ContainsAll(visitor.VisibleCues, rule.RequiredVisibleCues);
                var matches = traitMatch && documentMatch && cueMatch;
                DebugTrace.Log("rule.predicate", $"visitor={visitor.Id};rule={rule.Id};traits={traitMatch};docs={documentMatch};cues={cueMatch};match={matches}");
                if (matches)
                {
                    return new RuleEvaluation(rule.Verdict, rule.Id, rule.Label);
                }
            }

            DebugTrace.Log("rule.default", $"visitor={visitor.Id};decision={decree.DefaultVerdict}");
            return new RuleEvaluation(decree.DefaultVerdict, "default", "No exception applies.");
        }

        private static bool ContainsAll(List<string> source, List<string> required)
        {
            return required.Count == 0 || required.All(source.Contains);
        }
    }
}
