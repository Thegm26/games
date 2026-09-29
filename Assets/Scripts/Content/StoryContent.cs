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
            Add(c, V("d2-02-pip-grateful", 2, 2, "Pip Sootwhistle", "Your warmth kept my letter dry. The royal wax still shines.", "courier", story: "pip", required: "pip_trusted", priority: 10, docs: A("storm pass"), cues: A("sealed letter"), admit: "pip_letter_kept", deny: "pip_letter_lost"));
            Add(c, V("d2-02-pip-wary", 2, 2, "Pip Sootwhistle", "I slept beneath a cart. The letter is damp, but still mine.", "courier", story: "pip", docs: A("storm pass"), cues: A("damp letter"), admit: "pip_letter_kept", deny: "pip_letter_lost"));
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
            Add(c, V("d3-03-mira-oathbound", 3, 3, "Mira Mothwitch", "Your fire gave my moths courage. I return under raven oath.", "witch", story: "mira", required: "mira_welcomed", priority: 10, traits: A("raven oath"), docs: A("moon seal"), cues: A("moth lantern"), admit: "mira_oath", deny: "mira_oath_refused"));
            Add(c, V("d3-03-mira-rainbound", 3, 3, "Mira Mothwitch", "The rain took my lantern’s flame, not my manners.", "witch", story: "mira", docs: A("moon seal"), cues: A("moth lantern"), admit: "mira_second_chance", deny: "mira_oath_refused"));
            Add(c, V("d3-04-cult-puppeteer", 3, 4, "Master Stringbean", "My puppets have no secrets; their strings do.", "masked-cultist", docs: A("healer writ"), cues: A("cult sigil")));
            Add(c, V("d3-05-quiet-apothecary", 3, 5, "Fenn Littlejar", "I carry mint, willow bark, and one very rude frog.", "healer", docs: A("healer writ"), cues: A("healer kit", "frog jar")));
            Add(c, V("d3-06-moonless-minstrel", 3, 6, "Lute Larkspur", "No seal, but I can play a song that sounds official.", "bard", cues: A("tiny lute")));
            Add(c, V("d3-07-nella-soup-route", 3, 7, "Nella Nightsoup", "The broth reached the guards. I brought the castle’s healer writ.", "healer", story: "nella", required: "nella_helped", priority: 10, docs: A("healer writ"), cues: A("healer kit"), admit: "nella_writ_honored", deny: "nella_writ_denied"));
            Add(c, V("d3-07-nella-cold-route", 3, 7, "Nella Nightsoup", "The broth cooled outside. I still have hands that can help.", "healer", story: "nella", docs: A("healer writ"), cues: A("healer kit"), admit: "nella_writ_honored", deny: "nella_writ_denied"));
            Add(c, V("d3-08-raven-orphan", 3, 8, "Mimi Feathercap", "The raven gave me this button. It is not a seal.", "child", traits: A("child"), cues: A("raven feather")));
        }

        private static void AddDayFour(GameContent c)
        {
            Add(c, V("d4-01-royal-librarian", 4, 1, "Archivist Plum", "The counterseal is ugly, but it is very official.", "cleric", docs: A("royal counterseal"), cues: A("book tower")));
            Add(c, V("d4-02-red-lantern-fisher", 4, 2, "Bix Brineboots", "This lantern is red because I like red. Nothing else.", "traveler", docs: A("royal counterseal"), cues: A("red lantern")));
            Add(c, V("d4-03-rowan-confessor", 4, 3, "Sir Rowan Ashcloak", "I kept my blade sheathed. The counterseal proves my errand.", "knight", story: "rowan", required: "rowan_armed", priority: 10, docs: A("royal counterseal"), cues: A("sheathed sword"), admit: "rowan_confessed", deny: "rowan_cast_out"));
            Add(c, V("d4-03-rowan-unarmed", 4, 3, "Sir Rowan Ashcloak", "I left my sword with your guards. I come with a seal, not steel.", "knight", story: "rowan", docs: A("royal counterseal"), cues: A("empty scabbard"), admit: "rowan_confessed", deny: "rowan_cast_out"));
            Add(c, V("d4-04-crumb-princess", 4, 4, "Princess Crumb", "My bread token has sesame on it. The kingdom insists that counts.", "child", traits: A("child"), docs: A("bread token"), cues: A("tiny crown")));
            Add(c, V("d4-05-pip-ledger", 4, 5, "Pip Sootwhistle", "The letter named a lantern spy. I brought the royal counterseal.", "courier", story: "pip", required: "pip_letter_kept", priority: 10, docs: A("royal counterseal"), cues: A("sealed letter"), admit: "pip_warning_heard", deny: "pip_warning_ignored"));
            Add(c, V("d4-05-pip-soggy-ledger", 4, 5, "Pip Sootwhistle", "The rain blurred the letter, but not the red-lantern warning.", "courier", story: "pip", docs: A("royal counterseal"), cues: A("damp letter"), admit: "pip_warning_heard", deny: "pip_warning_ignored"));
            Add(c, V("d4-06-mouse-ambassador", 4, 6, "Ambassador Squeak", "My embassy is under the pantry. Please do not tell the cats.", "animal", cues: A("tiny banner")));
            Add(c, V("d4-07-red-cap-singer", 4, 7, "Rollo Redcap", "I sing to storms. The lantern only keeps time.", "bard", traits: A("child"), docs: A("bread token"), cues: A("red lantern")));
            Add(c, V("d4-08-bread-runner", 4, 8, "Tibby Toast", "I ran from the village with supper and a token.", "child", traits: A("child"), docs: A("bread token"), cues: A("bread basket")));
        }

        private static void AddDayFive(GameContent c)
        {
            Add(c, V("d5-01-counterseal-cobbler", 5, 1, "Cobbler Cinder", "The royal boots split. I bring their counterseal and my thread.", "commoner", docs: A("royal counterseal"), cues: A("gold thread")));
            Add(c, V("d5-02-nella-last-soup", 5, 2, "Nella Nightsoup", "The sick are safe. This counterseal buys one last trip through the gate.", "healer", story: "nella", required: "nella_writ_honored", priority: 10, docs: A("royal counterseal"), cues: A("healer kit"), admit: "nella_safe", deny: "nella_left_out"));
            Add(c, V("d5-02-nella-thin-soup", 5, 2, "Nella Nightsoup", "I have no writ left, only a counterseal and a pot that misses home.", "healer", story: "nella", docs: A("royal counterseal"), cues: A("empty soup pot"), admit: "nella_safe", deny: "nella_left_out"));
            Add(c, V("d5-03-traitor-pigeon", 5, 3, "Baron Beak", "I am merely a pigeon in a very guilty hat.", "animal", docs: A("royal counterseal"), cues: A("traitor mark")));
            Add(c, V("d5-04-lost-page", 5, 4, "Page Juniper", "The counterseal is bigger than my whole hand.", "child", traits: A("child"), docs: A("royal counterseal"), cues: A("ink-stained sleeve")));
            Add(c, V("d5-05-ember-scout", 5, 5, "Scout Flicker", "No mark, no seal—only news of the hill road.", "traveler", cues: A("muddy boots")));
            Add(c, V("d5-06-marked-duke", 5, 6, "Duke Dovetail", "A birthmark is not treason. The red wax is quite tasteful.", "noble", docs: A("royal counterseal"), cues: A("traitor mark")));
            Add(c, V("d5-07-mira-gate-remembers", 5, 7, "Mira Mothwitch", "The gate remembers your kindness. My oath carries its true name.", "witch", story: "mira", required: "mira_oath", priority: 10, traits: A("mira oath"), docs: A("gate-memory writ"), cues: A("moth lantern"), admit: "secret_gate_remembers", deny: "mira_final_refused"));
            Add(c, V("d5-07-mira-last-lantern", 5, 7, "Mira Mothwitch", "I found a writ in the rain. It asks only that you listen.", "witch", story: "mira", docs: A("gate-memory writ"), cues: A("moth lantern"), admit: "mira_final_resolved", deny: "mira_final_refused"));
            Add(c, V("d5-08-rowan-final-watch", 5, 8, "Sir Rowan Ashcloak", "The traitor used a red lantern; my counterseal brings the proof.", "knight", story: "rowan", required: "rowan_confessed", priority: 10, docs: A("royal counterseal"), cues: A("sheathed sword"), admit: "rowan_watch_held", deny: "rowan_watch_broken"));
            Add(c, V("d5-08-rowan-last-oath", 5, 8, "Sir Rowan Ashcloak", "I have proof, a counterseal, and no sword to frighten you with.", "knight", story: "rowan", docs: A("royal counterseal"), cues: A("empty scabbard"), admit: "rowan_watch_held", deny: "rowan_watch_broken"));
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

        private static void Require(bool condition, string eventId, string payload)
        {
            if (condition) return;
            DebugTrace.Error(eventId, payload);
            throw new ArgumentException(payload);
        }
    }
}
