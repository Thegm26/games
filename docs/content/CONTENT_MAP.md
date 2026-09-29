# Story Content Map

The gate watch contains five decrees and eight resolved encounters per night. The card displays every rule-relevant document, trait, and visible cue, so a player can explain the official verdict from the current decree.

## Returning stories

| Chain | Nights | Early choice | Final consequence |
| --- | --- | --- | --- |
| Mira Mothwitch | 1, 3, 5 | Welcoming Mira enables her raven-oath route. | Admit oathful Mira with the gate-memory writ on night five to earn `secret_gate_remembers`. |
| Pip Sootwhistle | 1, 2, 4 | Trusting Pip protects the royal letter. | Pip’s counterseal carries the red-lantern warning. |
| Nella Nightsoup | 2, 3, 5 | Helping Nella supports the healer’s route. | Nella’s counterseal brings her patients safely through. |
| Sir Rowan Ashcloak | 2, 4, 5 | Letting Rowan in starts the confession route. | Rowan supplies countersealed proof against the lantern spy. |

The fallback version of every returning slot remains lawful and readable if the earlier flag was not earned. It keeps the fixed encounter schedule while reflecting the prior choice in the dialogue and portrait key.

## Portrait art contract

All forty slots resolve to one of exactly sixteen reusable art archetypes: `guard`, `courier`, `witch`, `goblin-merchant`, `commoner`, `shepherd`, `knight`, `noble`, `cleric`, `healer`, `child`, `giant`, `animal`, `bard`, `traveler`, and `masked-cultist`. Branch state is expressed through the card’s clues and documents, not by requesting unbounded new portrait files. Every archetype is used at least once.

## Explicit moral outcomes

The core contracts provide `FlagOnAdmit` and `FlagOnDeny`, rather than an outcome-text field. This package uses both fields for every significant returning choice, and the selected alternative reads those flags on a later day. An optional mercy decision is therefore explicit: the player can select a non-decree verdict, lose a seal, and still produce an authored branch flag. Runtime outcome copy can be added by the UI/integration owner without changing rule correctness.

## Content safety and tone

Characters are whimsical, diverse fantasy citizens. “Danger” is represented by forged papers, suspicious symbols, weather, and off-screen moat denial; there is no hateful material, targeted degradation, or graphic gore.
