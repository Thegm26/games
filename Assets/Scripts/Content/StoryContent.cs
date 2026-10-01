using System;
using System.Collections.Generic;
using System.Linq;
using WhoEnters.Core;

namespace WhoEnters.Content
{
    /// <summary>
    /// Authored five-night gate watch. Every verdict is computed from the active decree and the
    /// words listed on the visitor card; flags make moral choices and returning stories explicit.
    /// </summary>
    public static class StoryContent
    {
        /// <summary>Approved shared art contract: exactly these sixteen reusable portrait archetypes.</summary>
        public static readonly IReadOnlyList<string> CanonicalPortraitKeys = new[]
        {
            "guard", "courier", "witch", "goblin-merchant", "commoner", "shepherd", "knight", "noble",
            "cleric", "healer", "child", "giant", "animal", "bard", "traveler", "masked-cultist",
        };

        /// <summary>
        /// A terminal flag is deliberately read by the ending/epilogue presentation rather than by another
        /// encounter alternative. Keeping this list here lets data validation reject decorative, orphaned outcomes.
        /// </summary>
        public static readonly IReadOnlyList<string> TerminalOutcomeFlags = new[]
        {
            "secret_gate_remembers", "mira_oath_final_refused", "mira_oath_released", "mira_oath_exiled",
            "mira_second_chance_held", "mira_second_chance_denied", "mira_rain_silenced_held", "mira_rain_silenced_denied",
            "mira_final_resolved", "mira_final_refused", "mira_lantern_lost", "mira_last_lantern_delayed",
            "pip_warning_heard_dry", "pip_warning_ignored_dry", "pip_warning_recovered", "pip_warning_buried",
            "pip_warning_heard_rescued", "pip_warning_ignored_rescued", "pip_warning_heard_soggy", "pip_warning_ignored_soggy", "pip_address_washed_out", "pip_ledger_delayed",
            "nella_safe", "nella_left_out", "nella_patients_waiting", "nella_patients_barred",
            "nella_patients_saved_late", "nella_late_caravan_turned", "nella_homefire_found", "nella_last_cup_left", "nella_courier_delayed", "nella_patients_delayed",
            "rowan_watch_held", "rowan_watch_broken", "rowan_exile_watch", "rowan_exile_broken",
            "rowan_witness_watch", "rowan_witness_broken", "rowan_oath_rekindled", "rowan_oath_lost", "rowan_witness_delayed", "rowan_watch_delayed",
        };

        /// <summary>Ending-card inputs for every terminal outcome. Presentation may phrase these, but may not discard them.</summary>
        public static readonly IReadOnlyDictionary<string, string> EpilogueInputs = new Dictionary<string, string>
        {
            { "secret_gate_remembers", "Mira names the gate. Dawn comes." },
            { "mira_oath_final_refused", "Mira's final oath was denied." },
            { "mira_oath_released", "Mira leaves safe with her royal pass." },
            { "mira_oath_exiled", "Mira is sent back into the rain." },
            { "mira_second_chance_held", "Mira and her lantern find shelter." },
            { "mira_second_chance_denied", "Mira's second chance is denied." },
            { "mira_rain_silenced_held", "Mira's wet lantern reaches the warm hall." },
            { "mira_rain_silenced_denied", "Mira and her lantern stay outside." },
            { "mira_final_resolved", "Mira's last request is heard." },
            { "mira_final_refused", "Mira's last request is denied." },
            { "mira_lantern_lost", "Mira's broken lantern could not find the gate again." },
            { "mira_last_lantern_delayed", "Mira's last lantern was delayed beyond the wall." },
            { "pip_warning_heard_dry", "Pip's dry letter warns the castle." },
            { "pip_warning_ignored_dry", "Pip's warning comes too late." },
            { "pip_warning_recovered", "Pip finds the saved warning." },
            { "pip_warning_buried", "Pip's warning stays lost in the records." },
            { "pip_warning_heard_rescued", "Pip's saved letter warns the castle." },
            { "pip_warning_ignored_rescued", "Pip's saved warning is ignored." },
            { "pip_warning_heard_soggy", "Pip's wet warning reaches the watch." },
            { "pip_warning_ignored_soggy", "Pip's wet warning is lost." },
            { "pip_address_washed_out", "Pip's address washed away before the return route opened." },
            { "pip_ledger_delayed", "Pip's ledger warning arrived after the watch changed." },
            { "nella_safe", "Nella and her patients find shelter." },
            { "nella_left_out", "Nella leaves soup at the gate." },
            { "nella_patients_waiting", "Nella's patients are still waiting." },
            { "nella_patients_barred", "Nella's patients stay outside." },
            { "nella_patients_saved_late", "Nella's medicine cart gets through." },
            { "nella_late_caravan_turned", "Nella's medicine cart turns back." },
            { "nella_homefire_found", "Nella finds a warm hearth." },
            { "nella_last_cup_left", "Nella leaves her last cup outside." },
            { "nella_courier_delayed", "Nella's courier could not carry word through the storm." },
            { "nella_patients_delayed", "Nella's patient list reached the wall too late." },
            { "rowan_watch_held", "Rowan takes the final watch." },
            { "rowan_watch_broken", "Rowan's watch ends at the wall." },
            { "rowan_exile_watch", "Rowan watches from outside." },
            { "rowan_exile_broken", "Rowan's exile watch ends." },
            { "rowan_witness_watch", "Rowan watches without his sword." },
            { "rowan_witness_broken", "Rowan is sent back into the storm." },
            { "rowan_oath_rekindled", "Rowan keeps watch again." },
            { "rowan_oath_lost", "Rowan's last watch is lost." },
            { "rowan_witness_delayed", "Rowan's witness did not reach the gate in time." },
            { "rowan_watch_delayed", "Rowan's final watch was never called to the wall." },
        };

        public static GameContent Create()
        {
            var content = new GameContent { TotalDays = 5, VisitorsPerDay = 8 };
            AddDecrees(content);
            AddDayOne(content);
            AddDayTwo(content);
            AddDayThree(content);
            AddDayFour(content);
            AddDayFive(content);
            ContentDiagnostics.ValidateAuthoredContent(content);
            DebugTrace.Log("content.story_loaded", "decrees=5;authoredVisitors=" + content.Visitors.Count + ";resolvedSlots=40;chains=4");
            return content;
        }

        private static void AddDecrees(GameContent content)
        {
            content.Decrees.Add(Decree(1, "Moon Seals",
                "FIRST MATCH WINS\n1. FORGED SEAL — DENY\n2. MOON SEAL — ADMIT\nDEFAULT: DENY", Decision.Deny,
                Rule("deny-forged-moon-seal", "A cracked or forged seal is false.", 30, Decision.Deny, cues: new[] { "forged seal" }),
                Rule("admit-moon-seal", "A true moon seal grants entry.", 10, Decision.Admit, docs: new[] { "moon seal" })));

            content.Decrees.Add(Decree(2, "Storm Shelter",
                "FIRST MATCH WINS\n1. PLAGUE MARK — DENY\n2. STORM PASS — ADMIT\n3. SEAL + BANDAGE — ADMIT\nDEFAULT: DENY", Decision.Deny,
                Rule("deny-plague-mark", "Plague marks outrank every plea.", 40, Decision.Deny, cues: new[] { "plague mark" }),
                Rule("admit-storm-pass", "A storm pass grants shelter.", 25, Decision.Admit, docs: new[] { "storm pass" }),
                Rule("admit-injured-moon-seal", "An injured bearer of the moon seal may enter.", 15, Decision.Admit, docs: new[] { "moon seal" }, cues: new[] { "bandaged arm" })));

            content.Decrees.Add(Decree(3, "Raven Rule",
                "FIRST MATCH WINS\n1. CULT SIGIL — DENY\n2. FORGED SEAL — DENY\n3. WRIT + KIT — ADMIT\n4. SEAL + OATH — ADMIT\nDEFAULT: DENY", Decision.Deny,
                Rule("deny-cult-sigil", "The black cult sign bars the gate.", 50, Decision.Deny, cues: new[] { "cult sigil" }),
                Rule("deny-forged-raven-seal", "A forged seal cannot carry an oath.", 40, Decision.Deny, cues: new[] { "forged seal" }),
                Rule("admit-healer-writ", "A healer’s writ and kit serve the castle.", 25, Decision.Admit, docs: new[] { "healer writ" }, cues: new[] { "healer kit" }),
                Rule("admit-raven-oath", "A moon seal with a raven oath is recognized.", 15, Decision.Admit, traits: new[] { "raven oath" }, docs: new[] { "moon seal" })));

            content.Decrees.Add(Decree(4, "Red Lantern",
                "FIRST MATCH WINS\n1. COUNTERSEAL — ADMIT\n2. RED LANTERN — DENY\n3. CHILD + BREAD — ADMIT\nDEFAULT: DENY", Decision.Deny,
                Rule("deny-red-lantern", "The red lantern signals the siege spy.", 50, Decision.Deny, cues: new[] { "red lantern" }),
                Rule("admit-royal-counterseal", "The royal counterseal overrides the ledger.", 60, Decision.Admit, docs: new[] { "royal counterseal" }),
                Rule("admit-bread-child", "A child’s bread token earns refuge.", 15, Decision.Admit, traits: new[] { "child" }, docs: new[] { "bread token" })));

            content.Decrees.Add(Decree(5, "Last Gate",
                "FIRST MATCH WINS\n1. OATH + WRIT — ADMIT\n2. COUNTERSEAL — ADMIT\n3. TRAITOR MARK — DENY\nDEFAULT: DENY", Decision.Deny,
                Rule("deny-traitor-mark", "The traitor mark seals the outer gate.", 60, Decision.Deny, cues: new[] { "traitor mark" }),
                Rule("admit-gate-memory", "Mira’s oath and the gate-memory writ may pass.", 80, Decision.Admit, traits: new[] { "mira oath" }, docs: new[] { "gate-memory writ" }),
                Rule("admit-final-counterseal", "A royal counterseal remains lawful.", 70, Decision.Admit, docs: new[] { "royal counterseal" })));
        }

        private static void AddDayOne(GameContent c)
        {
            Add(c, V("d1-01-lantern-miller", 1, 1, "Bramble Miller", "The mill wheel groans before dawn.", "commoner", docs: A("moon seal"), cues: A("flour sack")));
            Add(c, V("d1-02-mira-mothwitch", 1, 2, "Mira Mothwitch", "The moths found their way through rain.", "witch", story: "mira", docs: A("moon seal"), cues: A("moth lantern"), admit: "mira_welcomed", deny: "mira_turned_away"));
            Add(c, V("d1-03-cracked-merchant", 1, 3, "Orlo Crackedcup", "A cracked cup makes a long road longer.", "goblin-merchant", docs: A("moon seal"), cues: A("forged seal")));
            Add(c, V("d1-04-sleepy-shepherd", 1, 4, "Tansy Woolcap", "My flock chose the ditch at sunset.", "shepherd", cues: A("wet cloak")));
            Add(c, V("d1-05-puddle-knight", 1, 5, "Sir Puddleby", "Puddles greet better than courtiers.", "guard", docs: A("moon seal"), cues: A("dented helm")));
            Add(c, V("d1-06-quiet-tinker", 1, 6, "Nix Needleknock", "The wall coughs when storms gather.", "commoner", cues: A("lock picks")));
            Add(c, V("d1-07-pip-sootwhistle", 1, 7, "Pip Sootwhistle", "One tune survived the storm.", "courier", story: "pip", docs: A("moon seal"), cues: A("sealed letter"), admit: "pip_trusted", deny: "pip_rejected"));
            // Aunt Corva either carries Pip's address onward or loses it in the storm. This explicit
            // gate decision determines whether Pip can return by the ordinary letter route.
            Add(c, V("d1-08-rain-crow", 1, 8, "Aunt Corva", "Chimney smoke called me across the hills.", "animal", cues: A("crow feather"), admit: "pip_address_washed_out", deny: "pip_address_forwarded"));
        }

        private static void AddDayTwo(GameContent c)
        {
            Add(c, V("d2-01-storm-pass-baker", 2, 1, "Moss Bunbaker", "A silent oven makes me nervous.", "commoner", docs: A("storm pass"), cues: A("bread basket"), admit: "mira_lantern_mended", deny: "mira_lantern_lost"));
            Add(c, V("d2-02-pip-dry-letter", 2, 2, "Pip Sootwhistle", "The storm missed one pocket.", "courier", story: "pip", required: "pip_trusted", requiredFlags: A("pip_address_forwarded"), priority: 10, docs: A("storm pass"), cues: A("sealed letter", "dry royal wax"), admit: "pip_letter_kept", deny: "pip_letter_confiscated"));
            Add(c, V("d2-02-pip-rain-letter", 2, 2, "Pip Sootwhistle", "The sky soaked my coat, not my courage.", "courier", story: "pip", required: "pip_rejected", requiredFlags: A("pip_address_forwarded"), priority: 10, docs: A("storm pass"), cues: A("damp letter", "smudged royal wax"), admit: "pip_letter_rescued", deny: "pip_letter_lost"));
            Add(c, V("d2-02-pip-lost-address", 2, 2, "Pip Sootwhistle", "The road forgot my name; chimneys did not.", "courier", story: "pip", docs: A("storm pass"), cues: A("unaddressed letter", "smudged royal wax"), admit: "pip_letter_rescued", deny: "pip_letter_lost"));
            Add(c, V("d2-03-plague-juggler", 2, 3, "Jory Jugglepins", "My juggling pins miss a dry stage.", "bard", docs: A("storm pass"), cues: A("plague mark")));
            Add(c, V("d2-04-bandaged-scribe", 2, 4, "Edda Inkfingers", "The archives grumble in the rain.", "cleric", docs: A("moon seal"), cues: A("bandaged arm", "plague mark")));
            Add(c, V("d2-05-wet-woodcutter", 2, 5, "Hollis Woodwren", "The forest kept its secrets.", "commoner", docs: A("moon seal"), cues: A("wet axe")));
            Add(c, V("d2-06-nella-nightsoup", 2, 6, "Nella Nightsoup", "I count lonely windows in the rain.", "healer", story: "nella", docs: A("storm pass"), cues: A("healer kit"), admit: "nella_helped", deny: "nella_refused"));
            Add(c, V("d2-07-tiny-giant", 2, 7, "Gub the Tiny Giant", "Clouds keep standing on my shoulders.", "giant", docs: A("storm pass"), cues: A("muddy boots"), admit: "nella_courier_sent", deny: "nella_courier_delayed"));
            Add(c, V("d2-08-sir-rowan", 2, 8, "Sir Rowan Ashcloak", "I have slept in kinder storms.", "knight", story: "rowan", docs: A("moon seal"), cues: A("bandaged arm", "sheathed sword"), admit: "rowan_armed", deny: "rowan_disarmed"));
        }

        private static void AddDayThree(GameContent c)
        {
            Add(c, V("d3-01-doctor-thimble", 3, 1, "Doctor Thimble", "The sick cannot wait for clear skies.", "healer", docs: A("healer writ"), cues: A("healer kit"), admit: "rowan_witness_sent", deny: "rowan_witness_delayed"));
            Add(c, V("d3-02-false-feather", 3, 2, "Velvet Vane", "The storm merely gave me an audience.", "traveler", traits: A("raven oath"), docs: A("moon seal"), cues: A("forged seal")));
            Add(c, V("d3-03-mira-oathbound", 3, 3, "Mira Mothwitch", "An old song brought me home.", "witch", story: "mira", required: "mira_welcomed", requiredFlags: A("mira_lantern_mended"), priority: 10, traits: A("raven oath"), docs: A("moon seal"), cues: A("moth lantern", "raven feather"), admit: "mira_oath", deny: "mira_oath_refused"));
            Add(c, V("d3-03-mira-rainbound", 3, 3, "Mira Mothwitch", "The storm took warmth, not my way home.", "witch", story: "mira", required: "mira_turned_away", requiredFlags: A("mira_lantern_mended"), priority: 10, docs: A("moon seal"), cues: A("drowned lantern", "wet moth wings"), admit: "mira_second_chance", deny: "mira_rain_silenced"));
            Add(c, V("d3-03-mira-lost-light", 3, 3, "Mira Mothwitch", "Something precious broke, yet I walked on.", "witch", story: "mira", docs: A("moon seal"), cues: A("torn lantern", "wet moth wings"), admit: "mira_second_chance", deny: "mira_rain_silenced"));
            Add(c, V("d3-04-cult-puppeteer", 3, 4, "Master Stringbean", "My puppets prefer a weary audience.", "masked-cultist", docs: A("healer writ"), cues: A("cult sigil", "healer kit")));
            Add(c, V("d3-05-quiet-apothecary", 3, 5, "Fenn Littlejar", "I arrived before the rain found its voice.", "healer", docs: A("healer writ"), cues: A("healer kit", "cult sigil")));
            Add(c, V("d3-06-moonless-minstrel", 3, 6, "Lute Larkspur", "The wind may share my song tonight.", "bard", docs: A("healer writ"), cues: A("tiny lute")));
            Add(c, V("d3-07-nella-soup-route", 3, 7, "Nella Nightsoup", "People beyond these walls still need warmth.", "healer", story: "nella", required: "nella_helped", requiredFlags: A("nella_courier_sent"), priority: 10, docs: A("healer writ"), cues: A("healer kit", "full soup pot"), admit: "nella_writ_honored", deny: "nella_writ_denied"));
            Add(c, V("d3-07-nella-cold-route", 3, 7, "Nella Nightsoup", "The road was hard; my thoughts stayed warm.", "healer", story: "nella", required: "nella_refused", requiredFlags: A("nella_courier_sent"), priority: 10, docs: A("healer writ"), cues: A("healer kit", "cold soup pot"), admit: "nella_writ_offered_after_refusal", deny: "nella_writ_withheld"));
            Add(c, V("d3-07-nella-wayfarer-soup", 3, 7, "Nella Nightsoup", "Coughing led me toward your wall.", "healer", story: "nella", docs: A("healer writ"), cues: A("healer kit", "travel-stained pot"), admit: "nella_writ_offered_after_refusal", deny: "nella_writ_withheld"));
            Add(c, V("d3-08-raven-orphan", 3, 8, "Mimi Feathercap", "Something shining seemed lonely on the road.", "child", traits: A("child"), docs: A("moon seal"), cues: A("raven feather"), admit: "pip_ledger_delayed", deny: "pip_ledger_forwarded"));
        }

        private static void AddDayFour(GameContent c)
        {
            Add(c, V("d4-01-royal-librarian", 4, 1, "Archivist Plum", "The library alphabetized itself. An omen.", "cleric", docs: A("royal counterseal"), cues: A("book tower"), admit: "nella_patients_notified", deny: "nella_patients_delayed"));
            Add(c, V("d4-02-red-lantern-fisher", 4, 2, "Bix Brineboots", "The river owes me three eels.", "traveler", docs: A("royal counterseal"), cues: A("red lantern"), admit: "mira_last_lantern_guided", deny: "mira_last_lantern_delayed"));
            Add(c, V("d4-03-rowan-confessor", 4, 3, "Sir Rowan Ashcloak", "I should have spoken before the storm.", "knight", story: "rowan", required: "rowan_armed", requiredFlags: A("rowan_witness_sent"), priority: 10, docs: A("royal counterseal"), cues: A("sheathed sword", "red lantern"), admit: "rowan_confessed", deny: "rowan_cast_out"));
            Add(c, V("d4-03-rowan-unarmed", 4, 3, "Sir Rowan Ashcloak", "I returned with nothing more to hide.", "knight", story: "rowan", required: "rowan_disarmed", requiredFlags: A("rowan_witness_sent"), priority: 10, docs: A("royal counterseal"), cues: A("empty scabbard", "red lantern"), admit: "rowan_proof_offered", deny: "rowan_oath_abandoned"));
            Add(c, V("d4-03-rowan-road-witness", 4, 3, "Sir Rowan Ashcloak", "Truth travels farther than weather.", "knight", story: "rowan", docs: A("royal counterseal"), cues: A("witness seal", "red lantern"), admit: "rowan_proof_offered", deny: "rowan_oath_abandoned"));
            Add(c, V("d4-04-crumb-princess", 4, 4, "Princess Crumb", "The moon looked worried at bedtime.", "child", traits: A("child"), docs: A("bread token"), cues: A("red lantern"), admit: "rowan_watch_delayed", deny: "rowan_watch_called"));
            Add(c, V("d4-05-pip-ledger-dry", 4, 5, "Pip Sootwhistle", "I outran the storm.", "courier", story: "pip", required: "pip_letter_kept", requiredFlags: A("pip_ledger_forwarded"), priority: 40, docs: A("royal counterseal"), cues: A("sealed letter", "dry royal wax"), admit: "pip_warning_heard_dry", deny: "pip_warning_ignored_dry"));
            Add(c, V("d4-05-pip-ledger-confiscated", 4, 5, "Pip Sootwhistle", "Empty hands remember the road.", "courier", story: "pip", required: "pip_letter_confiscated", requiredFlags: A("pip_ledger_forwarded"), priority: 30, docs: A("royal counterseal"), cues: A("empty satchel", "tower receipt"), admit: "pip_warning_recovered", deny: "pip_warning_buried"));
            Add(c, V("d4-05-pip-ledger-patched", 4, 5, "Pip Sootwhistle", "I mended what rain tore.", "courier", story: "pip", required: "pip_letter_rescued", requiredFlags: A("pip_ledger_forwarded"), priority: 20, docs: A("royal counterseal"), cues: A("patched letter", "red-lantern warning"), admit: "pip_warning_heard_rescued", deny: "pip_warning_ignored_rescued"));
            Add(c, V("d4-05-pip-ledger-soggy", 4, 5, "Pip Sootwhistle", "Rain blurs ink, not purpose.", "courier", story: "pip", required: "pip_letter_lost", requiredFlags: A("pip_ledger_forwarded"), priority: 10, docs: A("royal counterseal"), cues: A("damp letter", "smudged warning"), admit: "pip_warning_heard_soggy", deny: "pip_warning_ignored_soggy"));
            Add(c, V("d4-05-pip-ledger-wayfarer", 4, 5, "Pip Sootwhistle", "The road gave me a riddle.", "courier", story: "pip", docs: A("royal counterseal"), cues: A("road-worn note", "red-lantern warning"), admit: "pip_warning_heard_soggy", deny: "pip_warning_ignored_soggy"));
            Add(c, V("d4-06-mouse-ambassador", 4, 6, "Ambassador Squeak", "My embassy requests dry crumbs.", "animal", docs: A("bread token"), cues: A("tiny banner")));
            Add(c, V("d4-07-red-cap-singer", 4, 7, "Rollo Redcap", "Even rain taps its feet to my chorus.", "bard", traits: A("child"), docs: A("bread token"), cues: A("red lantern")));
            Add(c, V("d4-08-bread-runner", 4, 8, "Tibby Toast", "Crows asked me for directions.", "child", traits: A("child"), docs: A("bread token"), cues: A("bread basket")));
        }

        private static void AddDayFive(GameContent c)
        {
            Add(c, V("d5-01-counterseal-cobbler", 5, 1, "Cobbler Cinder", "These boots outlast most nobles.", "commoner", docs: A("royal counterseal"), cues: A("gold thread")));
            Add(c, V("d5-02-nella-last-soup", 5, 2, "Nella Nightsoup", "I promised the watch a kind dawn.", "healer", story: "nella", required: "nella_writ_honored", requiredFlags: A("nella_patients_notified"), priority: 40, docs: A("royal counterseal"), cues: A("healer kit", "full soup pot", "traitor mark"), admit: "nella_safe", deny: "nella_left_out"));
            Add(c, V("d5-02-nella-waiting-list", 5, 2, "Nella Nightsoup", "Too many names wait through storms.", "healer", story: "nella", required: "nella_writ_denied", requiredFlags: A("nella_patients_notified"), priority: 30, docs: A("royal counterseal"), cues: A("closed healer kit", "patient list", "traitor mark"), admit: "nella_patients_waiting", deny: "nella_patients_barred"));
            Add(c, V("d5-02-nella-late-caravan", 5, 2, "Nella Nightsoup", "Dawn found us still on the road.", "healer", story: "nella", required: "nella_writ_offered_after_refusal", requiredFlags: A("nella_patients_notified"), priority: 20, docs: A("royal counterseal"), cues: A("medicine cart token", "full soup pot", "traitor mark"), admit: "nella_patients_saved_late", deny: "nella_late_caravan_turned"));
            Add(c, V("d5-02-nella-thin-soup", 5, 2, "Nella Nightsoup", "I held the hearth in my thoughts.", "healer", story: "nella", required: "nella_writ_withheld", requiredFlags: A("nella_patients_notified"), priority: 10, docs: A("royal counterseal"), cues: A("empty soup pot", "cold ladle", "traitor mark"), admit: "nella_homefire_found", deny: "nella_last_cup_left"));
            Add(c, V("d5-02-nella-roadside-cup", 5, 2, "Nella Nightsoup", "Someone here still needs a warm room.", "healer", story: "nella", docs: A("royal counterseal"), cues: A("travel-stained pot", "cold ladle", "traitor mark"), admit: "nella_homefire_found", deny: "nella_last_cup_left"));
            Add(c, V("d5-03-traitor-pigeon", 5, 3, "Baron Beak", "A pigeon may still dress well.", "animal", docs: A("royal counterseal"), cues: A("traitor mark")));
            Add(c, V("d5-04-lost-page", 5, 4, "Page Juniper", "Fog whispered beside the road.", "child", traits: A("child"), docs: A("royal counterseal"), cues: A("traitor mark")));
            Add(c, V("d5-05-ember-scout", 5, 5, "Scout Flicker", "The hill road is breathing smoke.", "traveler", docs: A("gate-memory writ"), cues: A("muddy boots")));
            Add(c, V("d5-06-marked-duke", 5, 6, "Duke Dovetail", "The storm taught me humility.", "noble", docs: A("royal counterseal"), cues: A("traitor mark")));
            Add(c, V("d5-07-mira-gate-remembers", 5, 7, "Mira Mothwitch", "The last bell called my name.", "witch", story: "mira", required: "mira_oath", requiredFlags: A("mira_last_lantern_guided"), priority: 40, traits: A("mira oath"), docs: A("gate-memory writ"), cues: A("traitor mark"), admit: "secret_gate_remembers", deny: "mira_oath_final_refused"));
            Add(c, V("d5-07-mira-refused-oath", 5, 7, "Mira Mothwitch", "Tonight I will speak plainly.", "witch", story: "mira", required: "mira_oath_refused", requiredFlags: A("mira_last_lantern_guided"), priority: 30, docs: A("royal counterseal"), cues: A("closed moth lantern", "traitor mark"), admit: "mira_oath_released", deny: "mira_oath_exiled"));
            Add(c, V("d5-07-mira-second-chance", 5, 7, "Mira Mothwitch", "A small glow kept pace with me.", "witch", story: "mira", required: "mira_second_chance", requiredFlags: A("mira_last_lantern_guided"), priority: 20, docs: A("royal counterseal"), cues: A("relit moth lantern", "traitor mark"), admit: "mira_second_chance_held", deny: "mira_second_chance_denied"));
            Add(c, V("d5-07-mira-rain-silenced", 5, 7, "Mira Mothwitch", "The storm stole my voice once.", "witch", story: "mira", required: "mira_rain_silenced", requiredFlags: A("mira_last_lantern_guided"), priority: 10, docs: A("royal counterseal"), cues: A("drowned lantern", "traitor mark"), admit: "mira_rain_silenced_held", deny: "mira_rain_silenced_denied"));
            Add(c, V("d5-07-mira-last-lantern", 5, 7, "Mira Mothwitch", "Rain brought me back to this place.", "witch", story: "mira", docs: A("gate-memory writ", "royal counterseal"), cues: A("traitor mark"), admit: "mira_final_resolved", deny: "mira_final_refused"));
            Add(c, V("d5-08-rowan-final-watch", 5, 8, "Sir Rowan Ashcloak", "No night has asked this much of me.", "knight", story: "rowan", required: "rowan_confessed", requiredFlags: A("rowan_watch_called"), priority: 40, docs: A("royal counterseal"), cues: A("sheathed sword", "ember ledger page"), admit: "rowan_watch_held", deny: "rowan_watch_broken"));
            Add(c, V("d5-08-rowan-exile-watch", 5, 8, "Sir Rowan Ashcloak", "The outer wall is cold at night.", "knight", story: "rowan", required: "rowan_cast_out", requiredFlags: A("rowan_watch_called"), priority: 30, docs: A("royal counterseal"), cues: A("rain cloak", "outer-wall pass"), admit: "rowan_exile_watch", deny: "rowan_exile_broken"));
            Add(c, V("d5-08-rowan-witness-watch", 5, 8, "Sir Rowan Ashcloak", "Someone trusted me with the truth.", "knight", story: "rowan", required: "rowan_proof_offered", requiredFlags: A("rowan_watch_called"), priority: 20, docs: A("royal counterseal"), cues: A("empty scabbard", "witness seal"), admit: "rowan_witness_watch", deny: "rowan_witness_broken"));
            Add(c, V("d5-08-rowan-last-oath", 5, 8, "Sir Rowan Ashcloak", "I still owe this wall a promise.", "knight", story: "rowan", required: "rowan_oath_abandoned", requiredFlags: A("rowan_watch_called"), priority: 10, docs: A("royal counterseal"), cues: A("empty scabbard", "frayed oath cord"), admit: "rowan_oath_rekindled", deny: "rowan_oath_lost"));
            Add(c, V("d5-08-rowan-roadside-oath", 5, 8, "Sir Rowan Ashcloak", "The road did not take my promise.", "knight", story: "rowan", docs: A("royal counterseal"), cues: A("rain cloak", "frayed oath cord"), admit: "rowan_oath_rekindled", deny: "rowan_oath_lost"));
        }

        private static void Add(GameContent content, VisitorDefinition visitor) => content.Visitors.Add(visitor);

        private static VisitorDefinition V(string id, int day, int slot, string name, string dialogue, string portrait,
            string story = "", string required = "", string[] requiredFlags = null, int priority = 0, string[] traits = null, string[] docs = null,
            string[] cues = null, string admit = "", string deny = "")
        {
            return new VisitorDefinition
            {
                Id = id, Day = day, EncounterSlot = slot, AlternativePriority = priority, DisplayName = name,
                Dialogue = dialogue, PortraitKey = portrait, StoryChainId = story, RequiredFlag = required, RequiredFlags = L(requiredFlags),
                FlagOnAdmit = admit, FlagOnDeny = deny,
                Traits = L(traits), Documents = L(docs), VisibleCues = L(cues),
            };
        }

        private static DecreeDefinition Decree(int day, string title, string text, Decision defaultVerdict, params RuleDefinition[] rules)
            => new DecreeDefinition { Day = day, Title = title, DisplayText = text, DefaultVerdict = defaultVerdict, Rules = rules.ToList() };

        private static RuleDefinition Rule(string id, string label, int priority, Decision verdict, string[] traits = null, string[] docs = null, string[] cues = null)
            => new RuleDefinition { Id = id, Label = label, Priority = priority, Verdict = verdict, RequiredTraits = L(traits), RequiredDocuments = L(docs), RequiredVisibleCues = L(cues) };

        private static List<string> L(string[] values) => values == null ? new List<string>() : values.ToList();
        private static string[] A(params string[] values) => values;

    }

    /// <summary>Pure authored-data checks supplement the generic Core validator with useful diagnostics.</summary>
    public static class ContentDiagnostics
    {
        private static readonly IReadOnlyDictionary<int, int> OpposingVerdictQuotaByDay = new Dictionary<int, int>
        {
            { 1, 1 }, { 2, 2 }, { 3, 3 }, { 4, 4 }, { 5, 5 },
        };

        public static void ValidateAuthoredContent(GameContent content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            var recurring = content.Visitors.Where(visitor => !string.IsNullOrWhiteSpace(visitor.StoryChainId))
                .GroupBy(visitor => visitor.StoryChainId).ToArray();
            var chains = recurring.Select(group => group.Key).OrderBy(key => key).ToArray();
            Require(chains.SequenceEqual(new[] { "mira", "nella", "pip", "rowan" }), "content.chain_set_invalid", string.Join(",", chains));
            foreach (var chain in recurring)
            {
                var appearanceDays = chain.Select(visitor => visitor.Day).Distinct().OrderBy(day => day).ToArray();
                Require(appearanceDays.Length == 3, "content.chain_appearance_invalid", "chain=" + chain.Key + ";days=" + string.Join(",", appearanceDays));
            }

            var allOutcomeFlags = content.Visitors.SelectMany(OutcomeFlags).Distinct().OrderBy(flag => flag).ToArray();
            var consumedFlags = content.Visitors.SelectMany(RequiredFlags).ToHashSet();
            var terminalFlags = StoryContent.TerminalOutcomeFlags.ToHashSet();
            Require(terminalFlags.SetEquals(StoryContent.EpilogueInputs.Keys), "content.epilogue_input_mismatch",
                "terminal=" + string.Join(",", terminalFlags.OrderBy(flag => flag)));
            foreach (var terminal in terminalFlags)
                Require(!string.IsNullOrWhiteSpace(StoryContent.EpilogueInputs[terminal]), "content.epilogue_input_missing", "flag=" + terminal);
            foreach (var outcome in allOutcomeFlags)
                Require(consumedFlags.Contains(outcome) || terminalFlags.Contains(outcome), "content.orphan_outcome_flag", "flag=" + outcome);
            Require(terminalFlags.All(allOutcomeFlags.Contains), "content.terminal_flag_unproduced",
                "terminal=" + string.Join(",", terminalFlags.Where(flag => !allOutcomeFlags.Contains(flag))));

            foreach (var chain in recurring)
            {
                var firstDay = chain.Min(visitor => visitor.Day);
                var earlyOutcomes = chain.Where(visitor => visitor.Day == firstDay).SelectMany(OutcomeFlags).Distinct().ToArray();
                var branchTargets = new List<VisitorDefinition>();
                foreach (var earlyOutcome in earlyOutcomes)
                {
                    var target = chain.Where(visitor => visitor.Day > firstDay && visitor.RequiredFlag == earlyOutcome).ToArray();
                    Require(target.Length > 0, "content.branch_early_outcome_unconsumed", "chain=" + chain.Key + ";flag=" + earlyOutcome);
                    branchTargets.AddRange(target);
                    Require(BranchReachesTerminal(chain.ToArray(), earlyOutcome, terminalFlags), "content.branch_nonterminal", "chain=" + chain.Key + ";flag=" + earlyOutcome);
                }
                Require(branchTargets.Select(visitor => visitor.Id).Distinct().Count() == branchTargets.Count,
                    "content.branch_reconverges", "chain=" + chain.Key + ";targets=" + string.Join(",", branchTargets.Select(visitor => visitor.Id)));
                Require(branchTargets.Select(VisibleFingerprint).Distinct().Count() == branchTargets.Count,
                    "content.branch_evidence_reconverges", "chain=" + chain.Key + ";targets=" + string.Join(",", branchTargets.Select(visitor => visitor.Id)));
                DebugTrace.Log("content.branch_diagnostic", "chain=" + chain.Key + ";early=" + string.Join(",", earlyOutcomes) + ";targets=" + string.Join(",", branchTargets.Select(visitor => visitor.Id)));
            }

            foreach (var visitor in content.Visitors)
            {
                Require(!string.IsNullOrWhiteSpace(visitor.DisplayName) && visitor.Dialogue.Length <= 100 && !string.IsNullOrWhiteSpace(visitor.PortraitKey),
                    "content.mobile_copy_invalid", "visitor=" + visitor.Id);
                var dossier = Dossier(visitor).ToArray();
                Require(dossier.Length >= 1 && dossier.Length <= 4,
                    "content.dossier_fact_count_invalid", "visitor=" + visitor.Id + ";count=" + dossier.Length);
                Require(dossier.Distinct(StringComparer.Ordinal).Count() == dossier.Length,
                    "content.dossier_duplicate_fact", "visitor=" + visitor.Id);
                Require(dossier.All(cue => cue.Length <= 35),
                    "content.evidence_copy_invalid", "visitor=" + visitor.Id);
                Require(StoryContent.CanonicalPortraitKeys.Contains(visitor.PortraitKey),
                    "content.portrait_key_invalid", "visitor=" + visitor.Id + ";key=" + visitor.PortraitKey);
                var decree = content.Decrees.Single(decreeItem => decreeItem.Day == visitor.Day);
                var result = RuleEvaluator.Evaluate(visitor, decree);
                Require(!string.IsNullOrWhiteSpace(result.RuleId), "content.verdict_missing", "visitor=" + visitor.Id);
                DebugTrace.Log("content.visitor_diagnostic", "id=" + visitor.Id + ";day=" + visitor.Day + ";slot=" + visitor.EncounterSlot + ";expected=" + result.Expected + ";rule=" + result.RuleId);
            }
            foreach (var decree in content.Decrees)
            {
                Require(!string.IsNullOrWhiteSpace(decree.Title) && decree.DisplayText.Length <= 140,
                    "content.decree_copy_invalid", "day=" + decree.Day);
                Require(decree.DisplayText.Split('\n').Count(line => line == "FIRST MATCH WINS") == 1
                    && decree.DisplayText.Split('\n').Count(line => line == "DEFAULT: " + decree.DefaultVerdict.ToString().ToUpperInvariant()) == 1,
                    "content.decree_order_or_default_missing", "day=" + decree.Day);
                Require(HasHarmlessDistractor(content, decree), "content.distractor_missing", "day=" + decree.Day);
                foreach (var compoundRule in decree.Rules.Where(rule => PredicateFactCount(rule) > 1))
                    Require(HasHarmlessNearMiss(content, decree, compoundRule), "content.compound_near_miss_missing", "day=" + decree.Day + ";rule=" + compoundRule.Id);
            }
            ValidateRouteInvariantConflictQuotas(content);
            Require(StoryContent.CanonicalPortraitKeys.All(key => content.Visitors.Any(visitor => visitor.PortraitKey == key)),
                "content.portrait_key_unused", "one or more canonical portrait keys have no visitor");
        }

        private static void ValidateRouteInvariantConflictQuotas(GameContent content)
        {
            foreach (var decree in content.Decrees)
            {
                var expected = OpposingVerdictQuotaByDay[decree.Day];
                var slotConflicts = new List<bool>();
                foreach (var slot in content.Visitors.Where(visitor => visitor.Day == decree.Day).GroupBy(visitor => visitor.EncounterSlot))
                {
                    var variants = slot.Select(visitor => RuleEvaluator.Evaluate(visitor, decree).OpposingVerdictRules.Count > 0).Distinct().ToArray();
                    Require(variants.Length == 1, "content.slot_conflict_not_equivalent",
                        "day=" + decree.Day + ";slot=" + slot.Key);
                    slotConflicts.Add(variants[0]);
                }
                var actual = slotConflicts.Count(conflict => conflict);
                Require(actual == expected, "content.opposing_verdict_quota_invalid",
                    "day=" + decree.Day + ";expected=" + expected + ";actual=" + actual);
                DebugTrace.Log("content.opposing_verdict_quota", "day=" + decree.Day + ";expected=" + expected + ";actual=" + actual);
            }
        }

        private static bool HasHarmlessDistractor(GameContent content, DecreeDefinition decree)
        {
            var predicateFacts = decree.Rules.SelectMany(RuleFacts).ToHashSet(StringComparer.Ordinal);
            return content.Visitors.Where(visitor => visitor.Day == decree.Day)
                .SelectMany(Dossier).Any(fact => !predicateFacts.Contains(fact));
        }

        private static bool HasHarmlessNearMiss(GameContent content, DecreeDefinition decree, RuleDefinition rule)
        {
            var required = RuleFacts(rule).ToHashSet(StringComparer.Ordinal);
            return content.Visitors.Where(visitor => visitor.Day == decree.Day).Any(visitor =>
            {
                var matched = Dossier(visitor).Count(required.Contains);
                return matched > 0 && matched < required.Count && RuleEvaluator.Evaluate(visitor, decree).UsedDefault;
            });
        }

        private static int PredicateFactCount(RuleDefinition rule) => RuleFacts(rule).Count();
        private static IEnumerable<string> RuleFacts(RuleDefinition rule)
            => rule.RequiredTraits.Concat(rule.RequiredDocuments).Concat(rule.RequiredVisibleCues);
        private static IEnumerable<string> Dossier(VisitorDefinition visitor)
            => visitor.Traits.Concat(visitor.Documents).Concat(visitor.VisibleCues);

        private static IEnumerable<string> OutcomeFlags(VisitorDefinition visitor)
        {
            if (!string.IsNullOrWhiteSpace(visitor.FlagOnAdmit)) yield return visitor.FlagOnAdmit;
            if (!string.IsNullOrWhiteSpace(visitor.FlagOnDeny)) yield return visitor.FlagOnDeny;
        }

        private static IEnumerable<string> RequiredFlags(VisitorDefinition visitor)
        {
            if (!string.IsNullOrWhiteSpace(visitor.RequiredFlag)) yield return visitor.RequiredFlag;
            if (visitor.RequiredFlags == null) yield break;
            foreach (var flag in visitor.RequiredFlags.Where(flag => !string.IsNullOrWhiteSpace(flag))) yield return flag;
        }

        private static bool BranchReachesTerminal(VisitorDefinition[] chain, string flag, HashSet<string> terminalFlags)
        {
            if (terminalFlags.Contains(flag)) return true;
            var consumers = chain.Where(visitor => visitor.RequiredFlag == flag).ToArray();
            return consumers.Length > 0 && consumers.All(visitor => OutcomeFlags(visitor).All(outcome => BranchReachesTerminal(chain, outcome, terminalFlags)));
        }

        private static string VisibleFingerprint(VisitorDefinition visitor)
            => visitor.Dialogue + "|" + string.Join(",", visitor.Traits) + "|" + string.Join(",", visitor.Documents) + "|" + string.Join(",", visitor.VisibleCues);

        private static void Require(bool condition, string eventId, string payload)
        {
            if (condition) return;
            DebugTrace.Error(eventId, payload);
            throw new ArgumentException(payload);
        }
    }
}
