using System;
using System.Collections.Generic;
using System.Linq;
using WhoEnters.Content;
using WhoEnters.Core;

namespace WhoEnters.Presentation
{
    /// <summary>Concise authored presentation copy. Rules remain in Content/Core; this only explains them.</summary>
    public static class StoryCaptionCatalog
    {
        // Visitor text is intentionally a non-evidentiary action reminder. IDs remain auditable so
        // a new card cannot silently bypass presentation coverage, without pre-solving its verdict.
        private static readonly HashSet<string> VisitorCaptionIds = new HashSet<string>(
            StoryContent.Create().Visitors.Select(visitor => visitor.Id), StringComparer.Ordinal);

        private static readonly Dictionary<string, string> FlagConsequences = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "mira_welcomed", "Mira’s lantern warms; moths lift toward the battlements." },
            { "mira_oath", "Mira speaks the raven oath. The old gate listens." },
            { "mira_oath_refused", "Mira folds her lantern shut. The rain keeps its secrets." },
            { "mira_oath_final_refused", "Mira’s final oath is refused; the gate remembers the sound." },
            { "mira_oath_released", "A counterseal lets Mira leave the storm behind." },
            { "mira_oath_exiled", "Mira’s counterseal goes unanswered beyond the outer wall." },
            { "mira_second_chance", "Mira leaves a small light beneath the arch." },
            { "mira_second_chance_held", "Mira’s relit lantern finds shelter at last." },
            { "mira_second_chance_denied", "The relit lantern dims on the far side of the gate." },
            { "mira_rain_silenced", "Rain drowns Mira’s lantern, but not the choice before you." },
            { "mira_rain_silenced_held", "A drowned lantern is carried gently into the warm hall." },
            { "mira_rain_silenced_denied", "Mira’s drowned lantern stays with the rain." },
            { "mira_turned_away", "Mira turns from the gate, moths lost in the storm." },
            { "mira_final_resolved", "Mira bows, and the gate’s memory settles into quiet stone." },
            { "mira_final_refused", "Mira’s moths turn away from the warm side of the wall." },
            { "mira_lantern_mended", "A baker mends Mira’s lantern for the return road." },
            { "mira_lantern_lost", "Mira’s broken lantern cannot find the gate again." },
            { "mira_last_lantern_guided", "A safe lantern route is marked before the last watch." },
            { "mira_last_lantern_delayed", "The last lantern route is lost in the storm." },
            { "secret_gate_remembers", "The gate remembers your kindness. Its locks answer Mira’s true name." },
            { "pip_trusted", "Pip tucks the letter safely under a dry cloak." },
            { "pip_rejected", "Pip shelters the letter beneath a cart, watching the tower lights." },
            { "pip_letter_kept", "The royal wax stays bright enough to warn the castle." },
            { "pip_letter_confiscated", "The guards log Pip’s letter; its warning waits in the ledger." },
            { "pip_letter_rescued", "Pip saves the letter’s pages by a baker’s oven." },
            { "pip_letter_lost", "Rain blurs the letter, but Pip still remembers the warning." },
            { "pip_warning_heard_dry", "The dry royal letter exposes the lantern route." },
            { "pip_warning_ignored_dry", "Pip’s dry warning is filed too late." },
            { "pip_warning_recovered", "Pip recovers the confiscated warning from watch records." },
            { "pip_warning_buried", "The confiscated letter remains buried in the tower ledger." },
            { "pip_warning_heard_rescued", "The patched letter reaches the Ember Ledger." },
            { "pip_warning_ignored_rescued", "The patched warning is dismissed beneath the storm." },
            { "pip_warning_heard_soggy", "The rain-blurred warning still reaches the watch." },
            { "pip_warning_ignored_soggy", "The soggy warning is lost to the storm." },
            { "pip_address_forwarded", "Aunt Corva carries Pip’s address toward the tower." },
            { "pip_address_washed_out", "Pip’s address washes away before the return road opens." },
            { "pip_ledger_forwarded", "Mimi carries Pip’s ledger warning to the watch." },
            { "pip_ledger_delayed", "Pip’s ledger warning arrives after the watch changes." },
            { "nella_helped", "Nella’s soup crosses the threshold toward the sick." },
            { "nella_refused", "Nella holds the soup close while the steam thins in rain." },
            { "nella_writ_honored", "Nella’s writ becomes a promise the castle can keep." },
            { "nella_writ_denied", "Nella’s healer’s kit stays outside the walls." },
            { "nella_writ_offered_after_refusal", "Nella returns with a medicine cart and a narrower hope." },
            { "nella_writ_withheld", "Nella’s writ remains withheld while the soup cools." },
            { "nella_safe", "Nella’s last pot finds a warm hearth." },
            { "nella_left_out", "Nella leaves one cup of soup beneath the gate." },
            { "nella_patients_waiting", "Nella’s patient list crosses the threshold before the storm deepens." },
            { "nella_patients_barred", "The patient list stays outside with the rain." },
            { "nella_patients_saved_late", "The late medicine cart reaches the infirmary." },
            { "nella_late_caravan_turned", "Nella’s medicine cart turns back into the dark." },
            { "nella_homefire_found", "Nella finds a hearth for one last cup of soup." },
            { "nella_last_cup_left", "Nella leaves the final cup beneath the closed gate." },
            { "nella_courier_sent", "Gub carries word of Nella’s medicine through the rain." },
            { "nella_courier_delayed", "Nella’s medicine courier cannot cross the storm." },
            { "nella_patients_notified", "The royal librarian sends Nella’s patient list ahead." },
            { "nella_patients_delayed", "Nella’s patient list reaches the wall too late." },
            { "rowan_armed", "Rowan’s sheathed blade is permitted through the storm." },
            { "rowan_disarmed", "Rowan lowers his eyes and leaves his oath in the rain." },
            { "rowan_confessed", "Rowan’s proof is entered into the Ember Ledger." },
            { "rowan_cast_out", "Rowan turns from the gate without drawing steel." },
            { "rowan_proof_offered", "Rowan trades his blade for a witness seal and proof." },
            { "rowan_oath_abandoned", "Rowan leaves his abandoned oath in the rain." },
            { "rowan_watch_held", "Rowan takes the final watch, facing the storm beside the gate." },
            { "rowan_watch_broken", "Rowan’s final oath fades beneath the outer wall." },
            { "rowan_exile_watch", "Rowan watches from exile, keeping proof close to his cloak." },
            { "rowan_exile_broken", "Rowan’s exile oath breaks beneath the outer wall." },
            { "rowan_witness_watch", "Rowan serves as the gate’s witness without a sword." },
            { "rowan_witness_broken", "Rowan’s disarmed testimony turns back into the storm." },
            { "rowan_oath_rekindled", "Rowan rekindles the last oath beside the gate." },
            { "rowan_oath_lost", "Rowan’s last oath goes out with the rain." },
            { "rowan_witness_sent", "Doctor Thimble sends Rowan’s witness through the storm." },
            { "rowan_witness_delayed", "Rowan’s witness does not reach the gate in time." },
            { "rowan_watch_called", "Princess Crumb calls Rowan’s final watch to the wall." },
            { "rowan_watch_delayed", "Rowan’s final watch is never called to the wall." },
        };

        public static CaptionSequence Intro() => Single("intro", PresentationRole.Intro,
            "Keep the castle safe for five nights.");

        public static CaptionSequence Tutorial() => Single("tutorial", PresentationRole.Tutorial,
            "Swipe right to admit.\nSwipe left to deny.\nTap Rulebook if you need it.");

        public static CaptionSequence DayAndDecree(int day, DecreeDefinition decree)
        {
            return Single("day-" + day + "-decree", PresentationRole.Decree, "The gate opens. Make your call.");
        }

        public static CaptionSequence Visitor(VisitorDefinition visitor)
        {
            if (visitor == null) return Single("visitor-fallback", PresentationRole.Visitor, "Check the dossier, then choose.");
            var text = "Swipe right: Admit\nSwipe left: Deny";
            return Single("visitor-" + visitor.Id, PresentationRole.Visitor, text);
        }

        public static CaptionSequence Verdict(VisitorDefinition visitor, VerdictRecord verdict, GameState state)
        {
            var flag = FindLatestFlag(visitor, verdict);
            var lines = new List<CaptionLine>
            {
                new CaptionLine("verdict-result-" + (visitor == null ? "unknown" : visitor.Id), PresentationRole.Verdict, VerdictSummary(verdict)),
            };
            if (!string.IsNullOrWhiteSpace(flag) && FlagConsequences.TryGetValue(flag, out var consequence))
                lines.Add(new CaptionLine("verdict-story-" + flag, PresentationRole.Verdict, consequence));
            return new CaptionSequence("verdict-" + (visitor == null ? "unknown" : visitor.Id), lines.ToArray());
        }

        public static CaptionSequence DaySummary(GameState state) => Single("day-summary-" + state.Day, PresentationRole.DaySummary,
            "Score: " + state.Score + "\nSeals left: " + state.Integrity + "\n\nThe gate held tonight. Prepare for tomorrow.");

        public static CaptionSequence Ending(EndingKind ending)
        {
            switch (ending)
            {
                case EndingKind.CastleFallen: return Single("ending-fallen", PresentationRole.Ending, "The last seal broke. The storm got in.");
                case EndingKind.HollowVictory: return Single("ending-hollow", PresentationRole.Ending, "You held the gate, but only just.");
                case EndingKind.TheGateRemembers: return Single("ending-remembers", PresentationRole.Ending, "Mira names the gate. It opens to dawn.");
                default: return Single("ending-held", PresentationRole.Ending, "You held the gate. The castle is safe tonight.");
            }
        }

        public static CaptionSequence Ending(EndingKind ending, GameState state)
        {
            var baseSequence = Ending(ending);
            var text = baseSequence.Lines[0].Text;
            if (state != null)
            {
                text += "\n\nScore: " + state.Score + "\nSeals left: " + state.Integrity + "\nChoices: " + state.Verdicts.Count;
            }
            var lines = new List<CaptionLine> { new CaptionLine(baseSequence.Id, PresentationRole.Ending, text) };
            if (state != null)
            {
                foreach (var terminalFlag in ReachedTerminalFlagsInStoryOrder(state))
                    lines.Add(new CaptionLine("ending-" + terminalFlag, PresentationRole.Ending, StoryContent.EpilogueInputs[terminalFlag]));
            }
            return new CaptionSequence(baseSequence.Id, lines.ToArray());
        }

        public static bool HasCaptionForVisitor(string visitorId) => VisitorCaptionIds.Contains(visitorId);
        public static bool HasConsequenceForFlag(string flag) => FlagConsequences.ContainsKey(flag);

        /// <summary>
        /// Kept separate from authored content so new conditional visitors never silently lose a
        /// narrative line. The runtime remains playable with a deterministic fallback; QA treats
        /// any returned id as a blocking presentation/content integration gap.
        /// </summary>
        public static IReadOnlyList<string> MissingVisitorMappings(GameContent content, IPresentationTraceSink trace = null)
        {
            var missing = new List<string>();
            if (content == null) return missing;
            foreach (var visitor in content.Visitors)
            {
                if (visitor == null || string.IsNullOrWhiteSpace(visitor.Id) || HasCaptionForVisitor(visitor.Id)) continue;
                missing.Add(visitor.Id);
                trace?.Record("presentation.error/fallback", "reason=missing-visitor-caption;id=" + visitor.Id);
            }
            missing.Sort(StringComparer.Ordinal);
            return missing;
        }

        private static CaptionSequence Single(string id, PresentationRole role, string text) => new CaptionSequence(id, new CaptionLine(id, role, text));

        private static string VerdictSummary(VerdictRecord verdict)
        {
            var label = VerdictLabel(verdict == null ? null : verdict.RuleId);
            if (verdict != null && verdict.Correct) return "RIGHT. " + label + " decided it. +" + verdict.ScoreDelta;
            return "WRONG. " + label + " decided it. Lost 1 seal.";
        }

        private static string VerdictLabel(string ruleId)
        {
            switch (ruleId)
            {
                case "deny-forged-moon-seal": return "Forged seal";
                case "admit-moon-seal": return "Moon seal";
                case "deny-plague-mark": return "Plague mark";
                case "admit-storm-pass": return "Storm pass";
                case "admit-injured-moon-seal": return "Seal and bandage";
                case "deny-cult-sigil": return "Cult sigil";
                case "deny-forged-raven-seal": return "Forged seal";
                case "admit-healer-writ": return "Writ and kit";
                case "admit-raven-oath": return "Seal and oath";
                case "deny-red-lantern": return "Red lantern";
                case "admit-royal-counterseal":
                case "admit-final-counterseal": return "Counterseal";
                case "admit-bread-child": return "Child and bread";
                case "deny-traitor-mark": return "Traitor mark";
                case "admit-gate-memory": return "Oath and writ";
                default: return "No exception";
            }
        }

        private static string FindLatestFlag(VisitorDefinition visitor, VerdictRecord verdict)
        {
            if (visitor == null || verdict == null) return string.Empty;
            return verdict.Chosen == Decision.Admit ? visitor.FlagOnAdmit : visitor.FlagOnDeny;
        }

        private static IEnumerable<string> ReachedTerminalFlagsInStoryOrder(GameState state)
        {
            var chainOrder = new[] { "mira_", "nella_", "pip_", "rowan_" };
            foreach (var chainPrefix in chainOrder)
                foreach (var terminalFlag in StoryContent.TerminalOutcomeFlags)
                    if (BelongsToChain(terminalFlag, chainPrefix) && state.StoryFlags.Contains(terminalFlag))
                        yield return terminalFlag;
        }

        private static bool BelongsToChain(string terminalFlag, string chainPrefix)
            => terminalFlag.StartsWith(chainPrefix, StringComparison.Ordinal)
                || (chainPrefix == "mira_" && terminalFlag == "secret_gate_remembers");
    }
}
