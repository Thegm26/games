# Story Content Map

The gate watch contains five decrees and eight resolved encounters per night. The card displays every rule-relevant document, trait, and visible cue, so a player can explain the official verdict from the current decree.

## Returning stories

| Chain | Nights | Early admit route | Early deny / mercy route | Persistent final consequence and epilogue input |
| --- | --- | --- | --- | --- |
| Mira Mothwitch | 1, 3, 5 | `mira_welcomed` selects oathbound Mira, with raven feather and moth lantern; a lawful day-three admit yields `mira_oath`, then the gate-memory route and `secret_gate_remembers`. | `mira_turned_away` selects rainbound Mira with drowned lantern/wet wings. Day-three mercy admit yields `mira_second_chance`; lawful denial yields `mira_rain_silenced`. Each has its own night-five card and terminal flag. | Oath route may unlock the secret ending. Refused oath, second-chance, and rain-silenced routes each have separate cards, visible evidence, and terminal epilogue inputs. |
| Pip Sootwhistle | 1, 2, 4 | `pip_trusted` selects the dry-letter card with dry royal wax. Day-two admit produces `pip_letter_kept`, which selects the dry Ember Ledger card. | `pip_rejected` selects a rain-letter with smudged wax. Day-two admit rescues it (`pip_letter_rescued`) for a patched-letter ledger card; denial loses it for the soggy-letter card. | Each ledger card yields distinct terminal warning flags: dry, recovered-from-confiscation, patched, or soggy warning outcomes are separate epilogue inputs. |
| Nella Nightsoup | 2, 3, 5 | `nella_helped` selects the full-pot healer-writ route. Honoring the writ selects Nella’s full final pot. | `nella_refused` selects a cold-pot writ route. Day-three admit offers delayed care; denial withholds it; each selects a different night-five card. | Full-pot, waiting-list, late-caravan, and thin-pot finales have unique evidence and patient-focused terminal epilogue inputs. |
| Sir Rowan Ashcloak | 2, 4, 5 | `rowan_armed` selects the sheathed-sword confession with an Ember Ledger page. Its admit outcome selects the final-watch card. | `rowan_disarmed` selects empty-scabbard Rowan with a tower receipt. Day-four admit makes him a witness; denial abandons his oath; each selects a different final card. | Confessor, exile, witness, and abandoned-oath finales use distinct proof/evidence and terminal watch epilogue inputs. |

Every returning slot retains a no-flag fallback so the eight-slot schedule cannot fail. Conditional variants have higher priority and use only flags produced on an earlier day. The fallback is a safety route, not a way to erase a played branch: each normal prior choice produces a distinct later card, visible evidence, and terminal flag.

## Portrait art contract

All forty slots resolve to one of exactly sixteen reusable art archetypes: `guard`, `courier`, `witch`, `goblin-merchant`, `commoner`, `shepherd`, `knight`, `noble`, `cleric`, `healer`, `child`, `giant`, `animal`, `bard`, `traveler`, and `masked-cultist`. Branch state is expressed through the card’s clues and documents, not by requesting unbounded new portrait files. Every archetype is used at least once.

## Explicit moral outcomes

The core contracts provide `FlagOnAdmit` and `FlagOnDeny`, rather than an outcome-text field. This package uses both fields for every significant returning choice, and a later alternative consumes each nonterminal flag. `StoryContent.TerminalOutcomeFlags` and `StoryContent.EpilogueInputs` explicitly document the final flags consumed by ending/epilogue presentation. An optional mercy decision is explicit: the player can select a non-decree verdict, lose a seal, and still produce an authored branch flag without changing what the rule evaluator says was lawful.

## Content safety and tone

Characters are whimsical, diverse fantasy citizens. “Danger” is represented by forged papers, suspicious symbols, weather, and off-screen moat denial; there is no hateful material, targeted degradation, or graphic gore.
