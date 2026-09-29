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
            "mira_final_resolved", "mira_final_refused",
            "pip_warning_heard_dry", "pip_warning_ignored_dry", "pip_warning_recovered", "pip_warning_buried",
            "pip_warning_heard_rescued", "pip_warning_ignored_rescued", "pip_warning_heard_soggy", "pip_warning_ignored_soggy",
            "nella_safe", "nella_left_out", "nella_patients_waiting", "nella_patients_barred",
            "nella_patients_saved_late", "nella_late_caravan_turned", "nella_homefire_found", "nella_last_cup_left",
            "rowan_watch_held", "rowan_watch_broken", "rowan_exile_watch", "rowan_exile_broken",
            "rowan_witness_watch", "rowan_witness_broken", "rowan_oath_rekindled", "rowan_oath_lost",
        };

        /// <summary>Ending-card inputs for every terminal outcome. Presentation may phrase these, but may not discard them.</summary>
        public static readonly IReadOnlyDictionary<string, string> EpilogueInputs = new Dictionary<string, string>
        {
            { "secret_gate_remembers", "Mira names the gate; mercy becomes the dawn ending." },
            { "mira_oath_final_refused", "Mira's oath was lawfully refused at the final gate." },
            { "mira_oath_released", "Mira leaves safely under the counterseal after her oath was refused." },
            { "mira_oath_exiled", "Mira's refused oath ends beyond the outer wall." },
            { "mira_second_chance_held", "Mira's mercy-lit lantern is held at the last gate." },
            { "mira_second_chance_denied", "Mira's second chance is denied despite its counterseal." },
            { "mira_rain_silenced_held", "Mira's drowned lantern is still given lawful shelter." },
            { "mira_rain_silenced_denied", "Mira's drowned lantern is turned away for the last time." },
            { "mira_final_resolved", "Mira's unwritten fallback plea is heard at the final gate." },
            { "mira_final_refused", "Mira's unwritten fallback plea is refused at the final gate." },
            { "pip_warning_heard_dry", "Pip's dry royal letter exposes the lantern route." },
            { "pip_warning_ignored_dry", "Pip's dry warning is filed too late." },
            { "pip_warning_recovered", "Pip recovers a confiscated letter from the watch records." },
            { "pip_warning_buried", "Pip's confiscated letter remains buried in the tower ledger." },
            { "pip_warning_heard_rescued", "Pip's patched letter reaches the Ember Ledger." },
            { "pip_warning_ignored_rescued", "Pip's patched letter is dismissed beneath the storm." },
            { "pip_warning_heard_soggy", "Pip's rain-blurred warning still reaches the watch." },
            { "pip_warning_ignored_soggy", "Pip's rain-blurred warning is lost to the storm." },
            { "nella_safe", "Nella's patients receive shelter and broth." },
            { "nella_left_out", "Nella leaves a final cup at the gate." },
            { "nella_patients_waiting", "Nella returns to patients still waiting beyond the wall." },
            { "nella_patients_barred", "Nella's patients remain barred from the castle infirmary." },
            { "nella_patients_saved_late", "Nella's late caravan reaches the infirmary." },
            { "nella_late_caravan_turned", "Nella's late caravan turns back into the rain." },
            { "nella_homefire_found", "Nella finds a hearth after her writ was withheld." },
            { "nella_last_cup_left", "Nella leaves her last cup beneath the gate." },
            { "rowan_watch_held", "Rowan keeps the final watch beside the gate." },
            { "rowan_watch_broken", "Rowan's confessed watch breaks at the wall." },
            { "rowan_exile_watch", "Rowan watches from exile after being cast out." },
            { "rowan_exile_broken", "Rowan's exile oath breaks in the rain." },
            { "rowan_witness_watch", "Disarmed Rowan serves as the gate's witness." },
            { "rowan_witness_broken", "Rowan's disarmed testimony is turned away." },
            { "rowan_oath_rekindled", "Rowan rekindles an abandoned oath without steel." },
            { "rowan_oath_lost", "Rowan loses his last oath beyond the wall." },
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
            content.Decrees.Add(Decree(1, "The Moon-Seal Watch",
                "Admit a true moon seal. Deny a forged seal and all without one.", Decision.Deny,
                Rule("deny-forged-moon-seal", "A cracked or forged seal is false.", 30, Decision.Deny, cues: new[] { "forged seal" }),
                Rule("admit-moon-seal", "A true moon seal grants entry.", 10, Decision.Admit, docs: new[] { "moon seal" })));

            content.Decrees.Add(Decree(2, "The Storm-Shelter Clause",
                "Deny plague marks. Admit a storm pass, or an injured moon-seal bearer.", Decision.Deny,
                Rule("deny-plague-mark", "Plague marks outrank every plea.", 40, Decision.Deny, cues: new[] { "plague mark" }),
                Rule("admit-storm-pass", "A storm pass grants shelter.", 25, Decision.Admit, docs: new[] { "storm pass" }),
                Rule("admit-injured-moon-seal", "An injured bearer of the moon seal may enter.", 15, Decision.Admit, docs: new[] { "moon seal" }, cues: new[] { "bandaged arm" })));

            content.Decrees.Add(Decree(3, "The Raven Exception",
                "Deny cult signs and forged seals. Admit a healer’s writ, or a moon seal sworn by raven oath.", Decision.Deny,
                Rule("deny-cult-sigil", "The black cult sign bars the gate.", 50, Decision.Deny, cues: new[] { "cult sigil" }),
                Rule("deny-forged-raven-seal", "A forged seal cannot carry an oath.", 40, Decision.Deny, cues: new[] { "forged seal" }),
                Rule("admit-healer-writ", "A healer’s writ and kit serve the castle.", 25, Decision.Admit, docs: new[] { "healer writ" }, cues: new[] { "healer kit" }),
                Rule("admit-raven-oath", "A moon seal with a raven oath is recognized.", 15, Decision.Admit, traits: new[] { "raven oath" }, docs: new[] { "moon seal" })));

            content.Decrees.Add(Decree(4, "The Ember Ledger",
                "Deny the red lantern. Admit a royal counterseal, or a child carrying a bread token.", Decision.Deny,
                Rule("deny-red-lantern", "The red lantern signals the siege spy.", 50, Decision.Deny, cues: new[] { "red lantern" }),
                Rule("admit-royal-counterseal", "The royal counterseal overrides the ledger.", 25, Decision.Admit, docs: new[] { "royal counterseal" }),
                Rule("admit-bread-child", "A child’s bread token earns refuge.", 15, Decision.Admit, traits: new[] { "child" }, docs: new[] { "bread token" })));

            content.Decrees.Add(Decree(5, "The Last Gate",
                "Deny the traitor mark. Admit a royal counterseal, or Mira’s oath with the gate-memory writ.", Decision.Deny,
                Rule("deny-traitor-mark", "The traitor mark seals the outer gate.", 60, Decision.Deny, cues: new[] { "traitor mark" }),
                Rule("admit-gate-memory", "Mira’s oath and the gate-memory writ may pass.", 35, Decision.Admit, traits: new[] { "mira oath" }, docs: new[] { "gate-memory writ" }),
                Rule("admit-final-counterseal", "A royal counterseal remains lawful.", 20, Decision.Admit, docs: new[] { "royal counterseal" })));
        }

        private static void AddDayOne(GameContent c)
        {
            Add(c, V("d1-01-lantern-miller", 1, 1, "Bramble Miller", "The flour will spoil in this rain.", "commoner", docs: A("moon seal"), cues: A("flour sack")));
            Add(c, V("d1-02-mira-mothwitch", 1, 2, "Mira Mothwitch", "My lantern knows the road home. May I wait by your fire?", "witch", story: "mira", docs: A("moon seal"), cues: A("moth lantern"), admit: "mira_welcomed", deny: "mira_turned_away"));
            Add(c, V("d1-03-cracked-merchant", 1, 3, "Orlo Crackedcup", "A little crack in wax proves nothing, does it?", "goblin-merchant", docs: A("moon seal"), cues: A("forged seal")));
            Add(c, V("d1-04-sleepy-shepherd", 1, 4, "Tansy Woolcap", "My sheep are smaller than wolves, I promise.", "shepherd", cues: A("wet cloak")));
            Add(c, V("d1-05-puddle-knight", 1, 5, "Sir Puddleby", "I polished my helm through the whole storm.", "guard", docs: A("moon seal"), cues: A("dented helm")));
            Add(c, V("d1-06-quiet-tinker", 1, 6, "Nix Needleknock", "I mend locks; I do not pick them. Usually.", "commoner", cues: A("lock picks")));
            Add(c, V("d1-07-pip-sootwhistle", 1, 7, "Pip Sootwhistle", "A letter for the castle, and a song for the rain.", "courier", story: "pip", docs: A("moon seal"), cues: A("sealed letter"), admit: "pip_trusted", deny: "pip_rejected"));
            Add(c, V("d1-08-rain-crow", 1, 8, "Aunt Corva", "The crows said the towers were lonely.", "animal", cues: A("crow feather")));
        }

        private static void AddDayTwo(GameContent c)
        {
            Add(c, V("d2-01-storm-pass-baker", 2, 1, "Moss Bunbaker", "Eight rolls, one storm pass, no crumbs left behind.", "commoner", docs: A("storm pass"), cues: A("bread basket")));
            Add(c, V("d2-02-pip-dry-letter", 2, 2, "Pip Sootwhistle", "Your warmth kept my letter dry. The royal wax still shines.", "courier", story: "pip", required: "pip_trusted", priority: 10, docs: A("storm pass"), cues: A("sealed letter", "dry royal wax"), admit: "pip_letter_kept", deny: "pip_letter_confiscated"));
            Add(c, V("d2-02-pip-rain-letter", 2, 2, "Pip Sootwhistle", "I slept beneath a cart. The letter is damp, but still mine.", "courier", story: "pip", required: "pip_rejected", priority: 10, docs: A("storm pass"), cues: A("damp letter", "smudged royal wax"), admit: "pip_letter_rescued", deny: "pip_letter_lost"));
            Add(c, V("d2-02-pip-lost-address", 2, 2, "Pip Sootwhistle", "The rain took the address, but I kept the wax and the storm pass.", "courier", story: "pip", docs: A("storm pass"), cues: A("unaddressed letter", "smudged royal wax"), admit: "pip_letter_rescued", deny: "pip_letter_lost"));
            Add(c, V("d2-03-plague-juggler", 2, 3, "Jory Jugglepins", "My spots are berry jam. Mostly berry jam.", "bard", docs: A("storm pass"), cues: A("plague mark")));
            Add(c, V("d2-04-bandaged-scribe", 2, 4, "Edda Inkfingers", "The archive roof bit me, but I saved the records.", "cleric", docs: A("moon seal"), cues: A("bandaged arm", "ink-stained sleeve")));
            Add(c, V("d2-05-wet-woodcutter", 2, 5, "Hollis Woodwren", "My axe is for fallen trees, not castle doors.", "commoner", cues: A("wet axe")));
            Add(c, V("d2-06-nella-nightsoup", 2, 6, "Nella Nightsoup", "I brought broth for whoever is sick beyond your walls.", "healer", story: "nella", docs: A("storm pass"), cues: A("healer kit"), admit: "nella_helped", deny: "nella_refused"));
            Add(c, V("d2-07-tiny-giant", 2, 7, "Gub the Tiny Giant", "My boots are wet. The rest of me is too.", "giant", docs: A("storm pass"), cues: A("muddy boots")));
            Add(c, V("d2-08-sir-rowan", 2, 8, "Sir Rowan Ashcloak", "My oath is old; my blade stays sheathed.", "knight", story: "rowan", docs: A("moon seal"), cues: A("bandaged arm", "sheathed sword"), admit: "rowan_armed", deny: "rowan_disarmed"));
        }

        private static void AddDayThree(GameContent c)
        {
            Add(c, V("d3-01-doctor-thimble", 3, 1, "Doctor Thimble", "My kit is small because my patients are brave.", "healer", docs: A("healer writ"), cues: A("healer kit")));
            Add(c, V("d3-02-false-feather", 3, 2, "Velvet Vane", "A raven whispered this seal into my pocket.", "traveler", docs: A("moon seal"), cues: A("forged seal", "raven feather")));
            Add(c, V("d3-03-mira-oathbound", 3, 3, "Mira Mothwitch", "Your fire gave my moths courage. I return under raven oath.", "witch", story: "mira", required: "mira_welcomed", priority: 10, traits: A("raven oath"), docs: A("moon seal"), cues: A("moth lantern", "raven feather"), admit: "mira_oath", deny: "mira_oath_refused"));
            Add(c, V("d3-03-mira-rainbound", 3, 3, "Mira Mothwitch", "The rain took my lantern’s flame, not my manners.", "witch", story: "mira", required: "mira_turned_away", priority: 10, docs: A("moon seal"), cues: A("drowned lantern", "wet moth wings"), admit: "mira_second_chance", deny: "mira_rain_silenced"));
            Add(c, V("d3-03-mira-lost-light", 3, 3, "Mira Mothwitch", "A roadside fire relit one moth, though no earlier promise led me here.", "witch", story: "mira", docs: A("moon seal"), cues: A("torn lantern", "wet moth wings"), admit: "mira_second_chance", deny: "mira_rain_silenced"));
            Add(c, V("d3-04-cult-puppeteer", 3, 4, "Master Stringbean", "My puppets have no secrets; their strings do.", "masked-cultist", docs: A("healer writ"), cues: A("cult sigil")));
            Add(c, V("d3-05-quiet-apothecary", 3, 5, "Fenn Littlejar", "I carry mint, willow bark, and one very rude frog.", "healer", docs: A("healer writ"), cues: A("healer kit", "frog jar")));
            Add(c, V("d3-06-moonless-minstrel", 3, 6, "Lute Larkspur", "No seal, but I can play a song that sounds official.", "bard", cues: A("tiny lute")));
            Add(c, V("d3-07-nella-soup-route", 3, 7, "Nella Nightsoup", "The broth reached the guards. I brought the castle’s healer writ.", "healer", story: "nella", required: "nella_helped", priority: 10, docs: A("healer writ"), cues: A("healer kit", "full soup pot"), admit: "nella_writ_honored", deny: "nella_writ_denied"));
            Add(c, V("d3-07-nella-cold-route", 3, 7, "Nella Nightsoup", "The broth cooled outside. I still have hands that can help.", "healer", story: "nella", required: "nella_refused", priority: 10, docs: A("healer writ"), cues: A("healer kit", "cold soup pot"), admit: "nella_writ_offered_after_refusal", deny: "nella_writ_withheld"));
            Add(c, V("d3-07-nella-wayfarer-soup", 3, 7, "Nella Nightsoup", "No promise sent me back, only a cold pot and a healer’s hands.", "healer", story: "nella", docs: A("healer writ"), cues: A("healer kit", "travel-stained pot"), admit: "nella_writ_offered_after_refusal", deny: "nella_writ_withheld"));
            Add(c, V("d3-08-raven-orphan", 3, 8, "Mimi Feathercap", "The raven gave me this button. It is not a seal.", "child", traits: A("child"), cues: A("raven feather")));
        }

        private static void AddDayFour(GameContent c)
        {
            Add(c, V("d4-01-royal-librarian", 4, 1, "Archivist Plum", "The counterseal is ugly, but it is very official.", "cleric", docs: A("royal counterseal"), cues: A("book tower")));
            Add(c, V("d4-02-red-lantern-fisher", 4, 2, "Bix Brineboots", "This lantern is red because I like red. Nothing else.", "traveler", docs: A("royal counterseal"), cues: A("red lantern")));
            Add(c, V("d4-03-rowan-confessor", 4, 3, "Sir Rowan Ashcloak", "I kept my blade sheathed. The counterseal proves my errand.", "knight", story: "rowan", required: "rowan_armed", priority: 10, docs: A("royal counterseal"), cues: A("sheathed sword", "ember ledger page"), admit: "rowan_confessed", deny: "rowan_cast_out"));
            Add(c, V("d4-03-rowan-unarmed", 4, 3, "Sir Rowan Ashcloak", "I left my sword with your guards. I come with a seal, not steel.", "knight", story: "rowan", required: "rowan_disarmed", priority: 10, docs: A("royal counterseal"), cues: A("empty scabbard", "tower receipt"), admit: "rowan_proof_offered", deny: "rowan_oath_abandoned"));
            Add(c, V("d4-03-rowan-road-witness", 4, 3, "Sir Rowan Ashcloak", "No earlier order brought me here; this witness seal is all I carry.", "knight", story: "rowan", docs: A("royal counterseal"), cues: A("rain cloak", "witness seal"), admit: "rowan_proof_offered", deny: "rowan_oath_abandoned"));
            Add(c, V("d4-04-crumb-princess", 4, 4, "Princess Crumb", "My bread token has sesame on it. The kingdom insists that counts.", "child", traits: A("child"), docs: A("bread token"), cues: A("tiny crown")));
            Add(c, V("d4-05-pip-ledger-dry", 4, 5, "Pip Sootwhistle", "The dry letter named a lantern spy. Its royal wax still bites.", "courier", story: "pip", required: "pip_letter_kept", priority: 40, docs: A("royal counterseal"), cues: A("sealed letter", "dry royal wax"), admit: "pip_warning_heard_dry", deny: "pip_warning_ignored_dry"));
            Add(c, V("d4-05-pip-ledger-confiscated", 4, 5, "Pip Sootwhistle", "Your guards logged my letter. I found its counterseal in the tower ledger.", "courier", story: "pip", required: "pip_letter_confiscated", priority: 30, docs: A("royal counterseal"), cues: A("empty satchel", "tower receipt"), admit: "pip_warning_recovered", deny: "pip_warning_buried"));
            Add(c, V("d4-05-pip-ledger-patched", 4, 5, "Pip Sootwhistle", "I dried the rescued pages by a baker’s oven. The warning survives.", "courier", story: "pip", required: "pip_letter_rescued", priority: 20, docs: A("royal counterseal"), cues: A("patched letter", "red-lantern warning"), admit: "pip_warning_heard_rescued", deny: "pip_warning_ignored_rescued"));
            Add(c, V("d4-05-pip-ledger-soggy", 4, 5, "Pip Sootwhistle", "The rain blurred the letter, but not the red-lantern warning.", "courier", story: "pip", required: "pip_letter_lost", priority: 10, docs: A("royal counterseal"), cues: A("damp letter", "smudged warning"), admit: "pip_warning_heard_soggy", deny: "pip_warning_ignored_soggy"));
            Add(c, V("d4-05-pip-ledger-wayfarer", 4, 5, "Pip Sootwhistle", "No earlier letter reached you, but I found this counterseal and warning on the road.", "courier", story: "pip", docs: A("royal counterseal"), cues: A("road-worn note", "red-lantern warning"), admit: "pip_warning_heard_soggy", deny: "pip_warning_ignored_soggy"));
            Add(c, V("d4-06-mouse-ambassador", 4, 6, "Ambassador Squeak", "My embassy is under the pantry. Please do not tell the cats.", "animal", cues: A("tiny banner")));
            Add(c, V("d4-07-red-cap-singer", 4, 7, "Rollo Redcap", "I sing to storms. The lantern only keeps time.", "bard", traits: A("child"), docs: A("bread token"), cues: A("red lantern")));
            Add(c, V("d4-08-bread-runner", 4, 8, "Tibby Toast", "I ran from the village with supper and a token.", "child", traits: A("child"), docs: A("bread token"), cues: A("bread basket")));
        }

        private static void AddDayFive(GameContent c)
        {
            Add(c, V("d5-01-counterseal-cobbler", 5, 1, "Cobbler Cinder", "The royal boots split. I bring their counterseal and my thread.", "commoner", docs: A("royal counterseal"), cues: A("gold thread")));
            Add(c, V("d5-02-nella-last-soup", 5, 2, "Nella Nightsoup", "The sick are safe. This counterseal buys one last trip through the gate.", "healer", story: "nella", required: "nella_writ_honored", priority: 40, docs: A("royal counterseal"), cues: A("healer kit", "full soup pot"), admit: "nella_safe", deny: "nella_left_out"));
            Add(c, V("d5-02-nella-waiting-list", 5, 2, "Nella Nightsoup", "You denied my writ. My patients still wait; this counterseal names them.", "healer", story: "nella", required: "nella_writ_denied", priority: 30, docs: A("royal counterseal"), cues: A("closed healer kit", "patient list"), admit: "nella_patients_waiting", deny: "nella_patients_barred"));
            Add(c, V("d5-02-nella-late-caravan", 5, 2, "Nella Nightsoup", "The village sent a late medicine cart. The counterseal grants one road.", "healer", story: "nella", required: "nella_writ_offered_after_refusal", priority: 20, docs: A("royal counterseal"), cues: A("medicine cart token", "full soup pot"), admit: "nella_patients_saved_late", deny: "nella_late_caravan_turned"));
            Add(c, V("d5-02-nella-thin-soup", 5, 2, "Nella Nightsoup", "The writ was withheld. I have a counterseal and one last cup for home.", "healer", story: "nella", required: "nella_writ_withheld", priority: 10, docs: A("royal counterseal"), cues: A("empty soup pot", "cold ladle"), admit: "nella_homefire_found", deny: "nella_last_cup_left"));
            Add(c, V("d5-02-nella-roadside-cup", 5, 2, "Nella Nightsoup", "No earlier writ survived, only a counterseal and a roadside cup.", "healer", story: "nella", docs: A("royal counterseal"), cues: A("travel-stained pot", "cold ladle"), admit: "nella_homefire_found", deny: "nella_last_cup_left"));
            Add(c, V("d5-03-traitor-pigeon", 5, 3, "Baron Beak", "I am merely a pigeon in a very guilty hat.", "animal", docs: A("royal counterseal"), cues: A("traitor mark")));
            Add(c, V("d5-04-lost-page", 5, 4, "Page Juniper", "The counterseal is bigger than my whole hand.", "child", traits: A("child"), docs: A("royal counterseal"), cues: A("ink-stained sleeve")));
            Add(c, V("d5-05-ember-scout", 5, 5, "Scout Flicker", "No mark, no seal—only news of the hill road.", "traveler", cues: A("muddy boots")));
            Add(c, V("d5-06-marked-duke", 5, 6, "Duke Dovetail", "A birthmark is not treason. The red wax is quite tasteful.", "noble", docs: A("royal counterseal"), cues: A("traitor mark")));
            Add(c, V("d5-07-mira-gate-remembers", 5, 7, "Mira Mothwitch", "The gate remembers your kindness. My oath carries its true name.", "witch", story: "mira", required: "mira_oath", priority: 40, traits: A("mira oath"), docs: A("gate-memory writ"), cues: A("moth lantern", "raven feather"), admit: "secret_gate_remembers", deny: "mira_oath_final_refused"));
            Add(c, V("d5-07-mira-refused-oath", 5, 7, "Mira Mothwitch", "My oath was refused, so I bring a counterseal instead of a true name.", "witch", story: "mira", required: "mira_oath_refused", priority: 30, docs: A("royal counterseal"), cues: A("closed moth lantern", "raven feather"), admit: "mira_oath_released", deny: "mira_oath_exiled"));
            Add(c, V("d5-07-mira-second-chance", 5, 7, "Mira Mothwitch", "Your mercy relit this lantern. A counterseal asks you to finish it kindly.", "witch", story: "mira", required: "mira_second_chance", priority: 20, docs: A("royal counterseal"), cues: A("relit moth lantern", "wet moth wings"), admit: "mira_second_chance_held", deny: "mira_second_chance_denied"));
            Add(c, V("d5-07-mira-rain-silenced", 5, 7, "Mira Mothwitch", "The rain took my oath. I carry only a counterseal and a drowned lantern.", "witch", story: "mira", required: "mira_rain_silenced", priority: 10, docs: A("royal counterseal"), cues: A("drowned lantern", "fallen moth wings"), admit: "mira_rain_silenced_held", deny: "mira_rain_silenced_denied"));
            Add(c, V("d5-07-mira-last-lantern", 5, 7, "Mira Mothwitch", "I found a writ in the rain. It asks only that you listen.", "witch", story: "mira", docs: A("gate-memory writ"), cues: A("moth lantern"), admit: "mira_final_resolved", deny: "mira_final_refused"));
            Add(c, V("d5-08-rowan-final-watch", 5, 8, "Sir Rowan Ashcloak", "The traitor used a red lantern; my counterseal brings the proof.", "knight", story: "rowan", required: "rowan_confessed", priority: 40, docs: A("royal counterseal"), cues: A("sheathed sword", "ember ledger page"), admit: "rowan_watch_held", deny: "rowan_watch_broken"));
            Add(c, V("d5-08-rowan-exile-watch", 5, 8, "Sir Rowan Ashcloak", "Cast out once, I watch from the road. My counterseal still proves the spy.", "knight", story: "rowan", required: "rowan_cast_out", priority: 30, docs: A("royal counterseal"), cues: A("rain cloak", "outer-wall pass"), admit: "rowan_exile_watch", deny: "rowan_exile_broken"));
            Add(c, V("d5-08-rowan-witness-watch", 5, 8, "Sir Rowan Ashcloak", "Disarmed, I bring a witness seal instead of a blade.", "knight", story: "rowan", required: "rowan_proof_offered", priority: 20, docs: A("royal counterseal"), cues: A("empty scabbard", "witness seal"), admit: "rowan_witness_watch", deny: "rowan_witness_broken"));
            Add(c, V("d5-08-rowan-last-oath", 5, 8, "Sir Rowan Ashcloak", "My oath was abandoned, yet this counterseal asks for one final watch.", "knight", story: "rowan", required: "rowan_oath_abandoned", priority: 10, docs: A("royal counterseal"), cues: A("empty scabbard", "frayed oath cord"), admit: "rowan_oath_rekindled", deny: "rowan_oath_lost"));
            Add(c, V("d5-08-rowan-roadside-oath", 5, 8, "Sir Rowan Ashcloak", "No earlier confession reached you. I bring a counterseal and a rain-worn oath.", "knight", story: "rowan", docs: A("royal counterseal"), cues: A("rain cloak", "frayed oath cord"), admit: "rowan_oath_rekindled", deny: "rowan_oath_lost"));
        }

        private static void Add(GameContent content, VisitorDefinition visitor) => content.Visitors.Add(visitor);

        private static VisitorDefinition V(string id, int day, int slot, string name, string dialogue, string portrait,
            string story = "", string required = "", int priority = 0, string[] traits = null, string[] docs = null,
            string[] cues = null, string admit = "", string deny = "")
        {
            return new VisitorDefinition
            {
                Id = id, Day = day, EncounterSlot = slot, AlternativePriority = priority, DisplayName = name,
                Dialogue = dialogue, PortraitKey = portrait, StoryChainId = story, RequiredFlag = required,
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
            var consumedFlags = content.Visitors.Where(visitor => !string.IsNullOrWhiteSpace(visitor.RequiredFlag))
                .Select(visitor => visitor.RequiredFlag).Distinct().ToHashSet();
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
                Require(!string.IsNullOrWhiteSpace(visitor.DisplayName) && visitor.Dialogue.Length <= 120 && !string.IsNullOrWhiteSpace(visitor.PortraitKey),
                    "content.mobile_copy_invalid", "visitor=" + visitor.Id);
                Require(visitor.Traits.Count + visitor.Documents.Count + visitor.VisibleCues.Count > 0,
                    "content.evidence_missing", "visitor=" + visitor.Id);
                Require(StoryContent.CanonicalPortraitKeys.Contains(visitor.PortraitKey),
                    "content.portrait_key_invalid", "visitor=" + visitor.Id + ";key=" + visitor.PortraitKey);
                var decree = content.Decrees.Single(decreeItem => decreeItem.Day == visitor.Day);
                var result = RuleEvaluator.Evaluate(visitor, decree);
                Require(!string.IsNullOrWhiteSpace(result.RuleId), "content.verdict_missing", "visitor=" + visitor.Id);
                DebugTrace.Log("content.visitor_diagnostic", "id=" + visitor.Id + ";day=" + visitor.Day + ";slot=" + visitor.EncounterSlot + ";expected=" + result.Expected + ";rule=" + result.RuleId);
            }
            Require(StoryContent.CanonicalPortraitKeys.All(key => content.Visitors.Any(visitor => visitor.PortraitKey == key)),
                "content.portrait_key_unused", "one or more canonical portrait keys have no visitor");
        }

        private static IEnumerable<string> OutcomeFlags(VisitorDefinition visitor)
        {
            if (!string.IsNullOrWhiteSpace(visitor.FlagOnAdmit)) yield return visitor.FlagOnAdmit;
            if (!string.IsNullOrWhiteSpace(visitor.FlagOnDeny)) yield return visitor.FlagOnDeny;
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
