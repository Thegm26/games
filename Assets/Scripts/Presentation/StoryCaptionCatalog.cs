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
        private static readonly Dictionary<string, string> VisitorCaptions = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "d1-01-lantern-miller", "A warm lantern and a true seal: the miller waits beneath the rain." },
            { "d1-02-mira-mothwitch", "Mira’s moths circle a true moon seal. The gate remembers small kindnesses." },
            { "d1-03-cracked-merchant", "Gold cannot mend a cracked seal. Look closely at what the merchant carries." },
            { "d1-04-sleepy-shepherd", "The shepherd has no seal, only wet wool and a tired promise." },
            { "d1-05-puddle-knight", "A dented helm is not a document. The moon seal is what matters tonight." },
            { "d1-06-quiet-tinker", "Lock picks glint in the tinker’s coat; no moon seal follows." },
            { "d1-07-pip-sootwhistle", "Pip guards a sealed letter from the storm. Your choice will travel with it." },
            { "d1-08-rain-crow", "Aunt Corva brings crow feathers, not a seal. The decree is plain." },
            { "d2-01-storm-pass-baker", "Moss brings warm bread and a storm pass. Shelter has its paperwork." },
            { "d2-02-pip-dry-letter", "Pip returns with dry royal wax. A saved letter opens later doors." },
            { "d2-02-pip-lost-address", "Pip’s letter lost its address in the rain, but its storm pass remains true." },
            { "d2-02-pip-rain-letter", "Pip’s damp letter survived the rain. The storm pass still speaks clearly." },
            { "d2-03-plague-juggler", "Berry jam or plague? The mark outranks every cheerful excuse." },
            { "d2-04-bandaged-scribe", "The scribe bears both moon seal and bandage: the clause makes room for duty." },
            { "d2-05-wet-woodcutter", "A wet axe is honest-looking, but the shelter clause asks for proof." },
            { "d2-06-nella-nightsoup", "Nella’s soup could warm the sick. Remember who stood outside the gate." },
            { "d2-07-tiny-giant", "Gub’s boots are enormous, but a storm pass is a storm pass." },
            { "d2-08-sir-rowan", "Sir Rowan’s sword stays sheathed. His oath may return before the last night." },
            { "d3-01-doctor-thimble", "A healer’s writ and kit: Doctor Thimble brings both proof and purpose." },
            { "d3-02-false-feather", "A raven feather cannot sanctify a forged seal." },
            { "d3-03-mira-oathbound", "Mira returns under raven oath. The lantern’s small moths know your earlier choice." },
            { "d3-03-mira-lost-light", "Mira’s moth-lantern is dark, but her moon seal still asks for a careful reading." },
            { "d3-03-mira-rainbound", "Mira has a moon seal but no oath. Rain can wash away more than flame." },
            { "d3-04-cult-puppeteer", "The puppeteer’s strings hide a cult sign. The decree puts that sign first." },
            { "d3-05-quiet-apothecary", "A healer’s writ, a kit, and a rude frog. The rules recognize the first two." },
            { "d3-06-moonless-minstrel", "A clever song does not replace a recognized writ or oath." },
            { "d3-07-nella-soup-route", "Nella’s broth reached the guards. Her healer’s writ is now in your hands." },
            { "d3-07-nella-wayfarer-soup", "Nella keeps the soup warm for travelers, carrying a healer’s writ through the storm." },
            { "d3-07-nella-cold-route", "Nella still carries a healer’s writ, even if her broth went cold." },
            { "d3-08-raven-orphan", "Mimi’s raven button is sweet, but it is not the oath the gate recognizes." },
            { "d4-01-royal-librarian", "The librarian’s counterseal is ugly, official, and exactly what the decree names." },
            { "d4-02-red-lantern-fisher", "A royal seal cannot outshine the red lantern. Read the priority carefully." },
            { "d4-03-rowan-confessor", "Rowan’s counterseal and sheathed sword tell a quieter kind of truth." },
            { "d4-03-rowan-road-witness", "Rowan arrives as a road witness, with proof instead of a blade." },
            { "d4-03-rowan-unarmed", "Rowan left steel behind and carries the royal counterseal instead." },
            { "d4-04-crumb-princess", "Princess Crumb has a tiny crown, a bread token, and the child’s exception." },
            { "d4-05-pip-ledger-dry", "Pip’s dry letter warns of a lantern spy. Your earlier trust carried this far." },
            { "d4-05-pip-ledger-confiscated", "Pip found the counterseal in a tower ledger. Even confiscated truth leaves a trail." },
            { "d4-05-pip-ledger-patched", "Pip patched the rescued pages. The red-lantern warning survived the storm." },
            { "d4-05-pip-ledger-soggy", "The paper is damp, but Pip’s counterseal and warning remain legible." },
            { "d4-05-pip-ledger-wayfarer", "Pip carries a wayfarer’s copy of the warning, worn but still readable." },
            { "d4-06-mouse-ambassador", "The mouse embassy has charm, not a counterseal or bread token." },
            { "d4-07-red-cap-singer", "A bread token helps a child; the red lantern still outranks it." },
            { "d4-08-bread-runner", "Tibby runs with supper and a bread token. The child clause was made for nights like this." },
            { "d5-01-counterseal-cobbler", "The cobbler’s thread is humble; the royal counterseal is decisive." },
            { "d5-02-nella-last-soup", "Nella’s final pot reaches the gate with the authority you helped preserve." },
            { "d5-02-nella-waiting-list", "Nella brings the names of patients still waiting beyond your wall." },
            { "d5-02-nella-late-caravan", "A late medicine cart reaches the gate with one narrow road left." },
            { "d5-02-nella-roadside-cup", "Nella brings one roadside cup and a counterseal that may yet find it a hearth." },
            { "d5-02-nella-thin-soup", "A thin pot, a royal counterseal, and one last chance to make shelter matter." },
            { "d5-03-traitor-pigeon", "A guilty hat is funny. A traitor mark is not." },
            { "d5-04-lost-page", "Page Juniper’s counterseal is too large for one hand, but it is still royal." },
            { "d5-05-ember-scout", "News from the hill road is useful, but the Last Gate recognizes only its written exceptions." },
            { "d5-06-marked-duke", "The duke’s counterseal cannot erase the traitor mark’s higher command." },
            { "d5-07-mira-gate-remembers", "Mira brings the gate-memory writ. Kindness has become evidence." },
            { "d5-07-mira-refused-oath", "Mira returns with a counterseal where an oath once stood." },
            { "d5-07-mira-second-chance", "Mira’s mercy-lit lantern asks for one final kind decision." },
            { "d5-07-mira-rain-silenced", "Mira’s drowned lantern carries a counterseal through the rain." },
            { "d5-07-mira-last-lantern", "Mira brings a wet writ and asks for one honest listening." },
            { "d5-08-rowan-final-watch", "Rowan’s proof reaches the final watch with a sheathed sword and royal authority." },
            { "d5-08-rowan-exile-watch", "Rowan keeps watch from the road, carrying proof without a place at the fire." },
            { "d5-08-rowan-witness-watch", "Disarmed Rowan brings a witness seal instead of a blade." },
            { "d5-08-rowan-last-oath", "Rowan returns without steel, carrying only proof and the last oath." },
            { "d5-08-rowan-roadside-oath", "Rowan’s roadside oath reaches the last gate with rain on every word." },
        };

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
        };

        public static CaptionSequence Intro() => Single("intro", PresentationRole.Intro,
            "A storm seals the kingdom in. For five nights, decide who may cross the castle gate.");

        public static CaptionSequence Tutorial() => Single("tutorial", PresentationRole.Tutorial,
            "Read the decree, then the visitor’s words and signs. Swipe left to ADMIT; swipe right to DENY. A first swipe finishes any caption. A second, deliberate swipe makes the verdict.");

        public static CaptionSequence DayAndDecree(int day, DecreeDefinition decree)
        {
            var title = decree == null ? "The watch resumes." : decree.Title;
            var rules = decree == null ? "Read the gate closely." : decree.DisplayText;
            return new CaptionSequence("day-" + day + "-decree",
                new CaptionLine("day-" + day, PresentationRole.DayTitle, "DAY " + day + " — " + title),
                new CaptionLine("decree-" + day, PresentationRole.Decree, rules));
        }

        public static CaptionSequence Visitor(VisitorDefinition visitor)
        {
            if (visitor == null) return Single("visitor-fallback", PresentationRole.Visitor, "Someone waits at the gate.");
            var text = VisitorCaptions.TryGetValue(visitor.Id, out var authored)
                ? authored
                : "Study the decree and the signs before you decide.";
            return Single("visitor-" + visitor.Id, PresentationRole.Visitor, text);
        }

        public static CaptionSequence Verdict(VisitorDefinition visitor, VerdictRecord verdict, GameState state)
        {
            var flag = FindLatestFlag(visitor, verdict);
            if (!string.IsNullOrWhiteSpace(flag) && FlagConsequences.TryGetValue(flag, out var consequence))
                return Single("verdict-" + (visitor == null ? "unknown" : visitor.Id), PresentationRole.Verdict, consequence);
            var text = verdict != null && verdict.Correct
                ? "The decree holds. The gate answers with a slow, certain groan."
                : "A seal fractures. The storm notices every uncertain judgment.";
            return Single("verdict-" + (visitor == null ? "unknown" : visitor.Id), PresentationRole.Verdict, text);
        }

        public static CaptionSequence DaySummary(GameState state) => Single("day-summary-" + state.Day, PresentationRole.DaySummary,
            "DAY " + state.Day + " COMPLETE\n\nScore: " + state.Score + "\nSeals remaining: " + state.Integrity + "\n\nThe gate survives another night. Count what the storm has taken, then face the next decree.");

        public static CaptionSequence Ending(EndingKind ending)
        {
            switch (ending)
            {
                case EndingKind.CastleFallen: return Single("ending-fallen", PresentationRole.Ending, "The final seal breaks. The storm enters, but your verdicts are written in its rain.");
                case EndingKind.HollowVictory: return Single("ending-hollow", PresentationRole.Ending, "The gate remains standing, though its halls are quiet with the cost of keeping it.");
                case EndingKind.TheGateRemembers: return Single("ending-remembers", PresentationRole.Ending, "Mira speaks the gate’s true name. Stone remembers mercy, and the castle opens into dawn.");
                default: return Single("ending-held", PresentationRole.Ending, "The gate holds. Inside, lanterns burn for everyone whose story crossed the storm.");
            }
        }

        public static CaptionSequence Ending(EndingKind ending, GameState state)
        {
            var baseSequence = Ending(ending);
            var text = baseSequence.Lines[0].Text;
            if (state != null)
            {
                text += "\n\nFinal score: " + state.Score + "\nSeals remaining: " + state.Integrity + "\nVerdicts: " + state.Verdicts.Count;
            }
            var lines = new List<CaptionLine> { new CaptionLine(baseSequence.Id, PresentationRole.Ending, text) };
            if (state != null)
            {
                foreach (var terminalFlag in ReachedTerminalFlagsInStoryOrder(state))
                    lines.Add(new CaptionLine("ending-" + terminalFlag, PresentationRole.Ending, StoryContent.EpilogueInputs[terminalFlag]));
            }
            return new CaptionSequence(baseSequence.Id, lines.ToArray());
        }

        public static bool HasCaptionForVisitor(string visitorId) => VisitorCaptions.ContainsKey(visitorId);
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
