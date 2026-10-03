# Story Content Map

The gate watch contains five decrees and eight resolved encounters per night. The card displays every rule-relevant document, trait, and visible cue, so a player can explain the official verdict from the current decree.

## Mobile copy contract

The game says the action plainly: **“Should they enter? Swipe right to Admit. Swipe left to Deny.”** A first swipe on a fresh card only reveals unfinished text; the next deliberate swipe makes the choice. Buttons and arrow keys are fallback controls.

The optional Rulebook is the only player-facing surface that shows a decree: it begins once with `FIRST MATCH WINS`, lists short numbered rules in evaluation order, and ends in `DEFAULT: DENY`. A later matching rule is diagnostic context, never a tie-break. Decree text is capped at 140 characters, visitor speech at 100 characters, and every shown clue at 35 characters. Every visitor dossier has one to four unique facts. Normal-flow dialogue and captions are deliberately non-evidentiary; the card and Rulebook are available before a choice. After a choice, one concise human-readable result identifies the deciding label, then any story consequence follows.

Terms kept for the card art—such as a **royal counterseal** or **healer writ**—are treated as named passes. The decree always says exactly which named clue allows or blocks entry; no lore knowledge is needed.

## Conflict and near-miss contract

Every reachable route has exactly the same number of cards where an ADMIT rule and a DENY rule both match: day one through day five use `1, 2, 3, 4, 5` respectively. Alternative cards in a shared slot keep the same conflict status, so a story choice cannot quietly make a day easier. The evaluator records every matched rule, then uses the first by priority (and authored order for equal priorities).

Each day also includes harmless visual distractors and an incomplete compound-rule dossier that reaches the explicit default. These make the full card worth reading without requiring hidden lore or allowing a partial exception to pass.

## Returning stories

| Chain | Nights | Early admit route | Early deny / mercy route | Persistent final consequence and epilogue input |
| --- | --- | --- | --- | --- |
| Mira Mothwitch | 1, 3, 5 | `mira_welcomed` plus the explicit day-two `mira_lantern_mended` outcome selects oathbound Mira; a lawful day-three admit yields `mira_oath`, then the guided-last-lantern route and `secret_gate_remembers`. | `mira_turned_away` selects rainbound Mira when the lantern is mended. A day-two `mira_lantern_lost` outcome instead selects `d3-03-mira-lost-light`; a delayed last-lantern outcome likewise selects `d5-07-mira-last-lantern`. | Oath, refused-oath, second-chance, rain-silenced, and both no-flag fallback cards are reachable, with each delay recorded as an ending input rather than erasing Mira’s earlier flag. |
| Pip Sootwhistle | 1, 2, 4 | `pip_trusted` plus `pip_address_forwarded` selects the dry letter; its kept letter plus `pip_ledger_forwarded` selects the dry Ember Ledger card. | `pip_rejected` plus the forwarded address selects the rain letter. A washed-out address selects `d2-02-pip-lost-address`; a delayed ledger selects `d4-05-pip-ledger-wayfarer`. | All four ledger branches and both fallback cards preserve their earlier letter state; washed-out/delayed routes are ending inputs. |
| Nella Nightsoup | 2, 3, 5 | `nella_helped` plus `nella_courier_sent` selects the full-pot writ route. Its outcome plus `nella_patients_notified` selects the matching final pot. | `nella_refused` selects the cold-pot writ route when the courier arrives. A delayed courier selects `d3-07-nella-wayfarer-soup`; a delayed patient list selects `d5-02-nella-roadside-cup`. | Full-pot, waiting-list, late-caravan, thin-pot, and both fallback routes keep their stated patient consequence and ending input. |
| Sir Rowan Ashcloak | 2, 4, 5 | `rowan_armed` plus `rowan_witness_sent` selects the confession. Its outcome plus `rowan_watch_called` selects the matching final watch. | `rowan_disarmed` plus the witness selects the unarmed route. A delayed witness selects `d4-03-rowan-road-witness`; a delayed watch selects `d5-08-rowan-roadside-oath`. | Confessor, exile, witness, abandoned-oath, and both fallback cards remain distinct, with delayed logistics preserved at the ending. |

Every returning slot retains a no-flag fallback so the eight-slot schedule cannot fail. Conditional variants use an AND of the legacy story flag and an explicit earlier route-control outcome. A delayed/lost control selects the no-flag card while retaining the earlier story flag in state; its ending input documents why the return changed. The focused test derives and plays a fresh legal prerequisite path for every visitor ID and every outcome flag across fixed seeds, so fallbacks cannot become unreachable again.

## Portrait art contract

All forty slots resolve to one of exactly sixteen reusable art archetypes: `guard`, `courier`, `witch`, `goblin-merchant`, `commoner`, `shepherd`, `knight`, `noble`, `cleric`, `healer`, `child`, `giant`, `animal`, `bard`, `traveler`, and `masked-cultist`. Branch state is expressed through the card’s clues and documents, not by requesting unbounded new portrait files. Every archetype is used at least once.

## Explicit moral outcomes

The core contracts provide `FlagOnAdmit` and `FlagOnDeny`, rather than an outcome-text field. This package uses both fields for every significant returning choice, and a later alternative consumes each nonterminal flag. `StoryContent.TerminalOutcomeFlags` and `StoryContent.EpilogueInputs` explicitly document the final flags consumed by ending/epilogue presentation. An optional mercy decision is explicit: the player can select a non-decree verdict, lose a seal, and still produce an authored branch flag without changing what the rule evaluator says was lawful.

## Content safety and tone

Characters are whimsical, diverse fantasy citizens. “Danger” is represented by forged papers, suspicious symbols, weather, and off-screen moat denial; there is no hateful material, targeted degradation, or graphic gore.
