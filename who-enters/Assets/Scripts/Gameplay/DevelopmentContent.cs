using System.Collections.Generic;
using WhoEnters.Core;

namespace WhoEnters.Gameplay
{
    /// <summary>Replace this provider with authored data in Assets/Scripts/Content without changing the runtime contracts.</summary>
    public static class DevelopmentContent
    {
        public static GameContent Create()
        {
            var content = new GameContent { TotalDays = 5, VisitorsPerDay = 8 };
            for (var day = 1; day <= content.TotalDays; day++)
            {
                content.Decrees.Add(new DecreeDefinition
                {
                    Day = day,
                    Title = $"Day {day} Decree",
                    DisplayText = day == 1
                        ? "Admit those bearing the moon seal. Deny all others."
                        : "Deny cursed travellers. Admit those bearing the moon seal. All others remain outside.",
                    DefaultVerdict = Decision.Deny,
                    Rules = new List<RuleDefinition>
                    {
                        new RuleDefinition { Id = "deny-cursed", Label = "Cursed travellers may not pass.", Priority = 20, Verdict = Decision.Deny, RequiredTraits = new List<string> { "cursed" } },
                        new RuleDefinition { Id = "admit-moon-seal", Label = "A valid moon seal grants entry.", Priority = 10, Verdict = Decision.Admit, RequiredDocuments = new List<string> { "moon seal" } },
                    },
                });

                for (var slot = 1; slot <= content.VisitorsPerDay; slot++)
                {
                    var admitted = slot % 2 == 0;
                    var visitor = new VisitorDefinition
                    {
                        Id = $"dev-d{day}-v{slot}",
                        Day = day,
                        EncounterSlot = slot,
                        DisplayName = admitted ? "Moonlit Traveller" : "Shrouded Stranger",
                        Dialogue = admitted ? "My seal is freshly stamped. Will the gate open?" : "The rain has followed me from the old road.",
                        PortraitKey = admitted ? "traveller" : "stranger",
                        Traits = new List<string>(),
                        Documents = admitted ? new List<string> { "moon seal" } : new List<string>(),
                        VisibleCues = admitted ? new List<string> { "silver wax" } : new List<string> { "wet cloak" },
                    };
                    if (!admitted && slot == 7 && day > 1) visitor.Traits.Add("cursed");
                    if (day == 5 && slot == 8)
                    {
                        visitor.DisplayName = "The Last Bellringer";
                        visitor.Dialogue = "One mercy can decide what this gate remembers.";
                        visitor.Documents = new List<string> { "moon seal" };
                        visitor.FlagOnAdmit = "secret_gate_remembers";
                    }
                    content.Visitors.Add(visitor);
                }
            }
            return content;
        }
    }
}
