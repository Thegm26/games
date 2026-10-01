using System;
using System.Collections.Generic;
using System.Linq;
using WhoEnters.Core;

namespace WhoEnters.Gameplay
{
    /// <summary>
    /// A complete, stable, player-facing dossier. The card deliberately reports every authored
    /// document, visible sign, and trait for the visitor; it must never filter the evidence down
    /// to the rule that happened to win. Rule evaluation remains available for diagnostics and
    /// proof, but is not rendered into this pre-choice surface.
    /// </summary>
    public readonly struct DecisionEvidence
    {
        public readonly RuleEvaluation Evaluation;
        public readonly string[] Documents;
        public readonly string[] VisibleSigns;
        public readonly string[] Traits;
        public readonly string[] Facts;
        public readonly string Text;

        private DecisionEvidence(RuleEvaluation evaluation, VisitorDefinition visitor)
        {
            Evaluation = evaluation;
            Documents = Snapshot(visitor.Documents);
            VisibleSigns = Snapshot(visitor.VisibleCues);
            Traits = Snapshot(visitor.Traits);
            Facts = Documents.Concat(VisibleSigns).Concat(Traits).Distinct(StringComparer.Ordinal).ToArray();
            Text = BuildText(Documents, VisibleSigns, Traits);
        }

        public static DecisionEvidence Resolve(VisitorDefinition visitor, DecreeDefinition decree)
        {
            if (visitor == null) throw new ArgumentNullException(nameof(visitor));
            if (decree == null) throw new ArgumentNullException(nameof(decree));
            var evaluation = RuleEvaluator.Evaluate(visitor, decree);
            var dossier = new DecisionEvidence(evaluation, visitor);
            if (dossier.Facts.Length < 1 || dossier.Facts.Length > 4)
                throw new InvalidOperationException("A mobile dossier must contain one to four unique authored facts: " + visitor.Id);
            if (dossier.Facts.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("A mobile dossier contains an empty authored fact: " + visitor.Id);
            return dossier;
        }

        public VisitorDefinition ProjectedVisitor(string id = "decision-evidence")
        {
            var projected = new VisitorDefinition { Id = id };
            // Full category snapshots, rather than a selected rule subset, prove the visible
            // dossier reproduces the evaluator without exposing its conclusion on the card.
            projected.Traits.AddRange(Traits);
            projected.Documents.AddRange(Documents);
            projected.VisibleCues.AddRange(VisibleSigns);
            return projected;
        }

        private static string[] Snapshot(IEnumerable<string> values)
            => values == null ? Array.Empty<string>() : values.ToArray();

        private static string BuildText(IEnumerable<string> documents, IEnumerable<string> visibleSigns, IEnumerable<string> traits)
        {
            var lines = new[]
            {
                // Compact category labels preserve their meaning without stealing a whole
                // 32px row from the facts themselves on a narrow portrait card.
                CategoryLine("Docs", documents),
                CategoryLine("Signs", visibleSigns),
                CategoryLine("Traits", traits),
            }.Where(line => !string.IsNullOrEmpty(line));
            return string.Join("\n", lines);
        }

        private static string CategoryLine(string category, IEnumerable<string> facts)
        {
            var values = facts == null ? Array.Empty<string>() : facts.ToArray();
            return values.Length == 0 ? string.Empty : category + ": " + string.Join("; ", values);
        }
    }
}
