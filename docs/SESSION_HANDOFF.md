# Who Enters? — Session Handoff

**Last updated:** 2026-09-29 (Europe/Paris)  
**Immediate next action:** Re-run headless content tests after the portrait-archetype correction, then obtain the mandatory fresh connected art/content review. Every future agent must read this file before any action and may not relax these constraints.

## NON-NEGOTIABLE REQUIREMENTS

- [ ] Quality over speed: avoid downstream refinements through deliberate preflight, data validation, review, correction, and re-review.
- [ ] Use dedicated **Terra-high** implementation agents: gameplay/core, environment, character-and-UI art, cards-and-narrative, audio, and QA/integration.
- [ ] Use a **Sol-medium** technical monitor plus three additional aesthetic reviewers: art direction, mobile visual UX, and cohesion/polish.
- [ ] Each reviewer must perform one isolated-component review and one review of that component connected to its dependencies.
- [ ] A serious review finding requires a Terra correction followed by a fresh review; do not waive this for schedule reasons.
- [ ] Development and QA are headless/data/state/event based only. Never automate the mouse, control the GUI/editor, capture screenshots, or use screenshot tests.
- [ ] Keep structured, removable debug events and the debug overlay throughout development; remove only in the explicit later cleanup pass.
- [ ] First version must be playable in Unity **6000.6.0f1**. Do **not** build, export, package, or create an itch archive. Final handoff opens the Unity Editor only so the user can press Play.
- [ ] Preserve `/home/gm26/game` and `/home/gm26/blender-agent-pipeline`; they are unrelated, untouched old work.
- [ ] Art must be original, coherent, polished storybook dark fantasy—not primitive placeholders. Record every external asset’s licence, source, checksum, modification status, and AI provenance.
- [ ] Update this handoff at every completed milestone before moving to a dependent task.

## Product and Repository

- **Title/theme:** *Who Enters?* — a cute-but-dark storybook-fantasy castle gatekeeper game for the Slapjam “Castles” theme.
- **Remote:** public `https://github.com/Thegm26/who-enters.git`
- **Local project:** `/home/gm26/games/who-enters`
- **Current branch:** `feature/gameplay-core` (coordinate integration rather than overwriting other shared-workspace changes).
- **Unity editor:** `/home/gm26/Unity/Hub/Editor/6000.6.0f1/Editor/Unity`
- **Git identity:** `Thegm26 <georgios.michalakis26@gmail.com>` for author and committer.
- **Remote checkpoint:** `eebdcbd56b67a2b72a0b27727b59e3662ede678b` was pushed non-destructively to `origin/main` after fetch confirmed the remote had no divergent branch. Push every later checkpoint after fetch/divergence inspection.

## Approved Game Specification

- Portrait, mobile-first Unity game at logical `720×1280`; touch swipe is primary, visible buttons and keyboard are fallback controls.
- Five days, exactly eight frozen encounter slots per day, for forty resolved encounters on a completed run.
- Swipe left admits to the castle; swipe right denies to the moat. Cards snap back below threshold.
- Start with five integrity seals. A wrong official verdict costs one seal. Correct verdicts score `100 + 25 × min(streak, 4)`; an incorrect verdict resets streak.
- Endings: `CastleFallen` at zero integrity; `HollowVictory` at 1–2 seals; `GateHeld` at 3–5; `TheGateRemembers` when the secret story flag is earned.
- Seeded queues are deterministic (`-seed=<int>` command-line support). Story anchors retain authored encounter slots; independent visitors are seeded.
- Four returning story chains have three appearances each. Required flags must be produced on an earlier day; each conditional slot needs a no-flag fallback. Mercy may knowingly violate a decree, cost integrity, and alter later branches only when represented explicitly by flag/outcome data.
- Decrees, documents, traits, and visible cues must fully explain every expected verdict. No hidden knowledge can decide correctness.

## Shared Contracts and Architecture

- `Assets/Scripts/Core/GameContracts.cs`: `VisitorDefinition`, `RuleDefinition`, `DecreeDefinition`, `GameContent`, `GameState`, endings, verdicts. A visitor supports day/slot alternatives, priority, flags, dialogue, portrait key, traits, documents, and visible cues.
- `Assets/Scripts/Core/RuleEvaluator.cs`: priority-ordered deterministic rule matching. Keep content predicates compatible; do not silently redesign core contracts.
- `Assets/Scripts/Core/RunStateMachine.cs`: validates days, slots, fallbacks, flags, freezes queues, resolves verdicts, scores integrity, and selects ending.
- `Assets/Scripts/Core/DebugTrace.cs`: structured diagnostic event store. New systems should emit stable event identifiers with compact key/value payloads.
- `Assets/Scripts/Gameplay/DevelopmentContent.cs`: current placeholder provider. The content agent owns the new provider under `Assets/Scripts/Content`; integration must deliberately switch the runtime to it.
- Runtime UI/director: `Assets/Scripts/Gameplay/GameDirector.cs`; it currently renders the data-driven visitor text/evidence and debug overlay.
- Content ownership: `Assets/Scripts/Content/**`, `Assets/Tests/**/Content*`, `docs/content/**`, plus this handoff. Do not change Core, Gameplay, scenes, ProjectSettings, build files, environment/character/UI art, or audio from the content task.

## Current State and Reviews

- Baseline commit: `4cc7c48 feat: add Unity gatekeeper gameplay foundation`.
- Baseline Unity EditMode suite: **7/7 passing** (recorded before this checkpoint); preserve this as the regression baseline.
- Environment assets are in `Assets/Art/Environment/Runtime/`; provenance and asset contract are in `docs/environment/`. Public source mirrors live in `public/assets/environment/`.
- Environment completed three review cycles and ended **PASS**, including ASTC importer overrides. Treat it as a dependency, not content-agent ownership.
- Active/expected roles: character/UI-art implementation active; cards/narrative implementation active; technical monitor active. Later: audio implementation; QA/integration; Sol aesthetic reviews for art direction, mobile visual UX, and cohesion/polish.
- **Serious review correction in progress:** the first content draft exposed roughly 43 portrait keys, conflicting with the approved sixteen-archetype art pipeline. Content now owns the fixed canonical set (`guard`, `courier`, `witch`, `goblin-merchant`, `commoner`, `shepherd`, `knight`, `noble`, `cleric`, `healer`, `child`, `giant`, `animal`, `bard`, `traveler`, `masked-cultist`), validates every key, and requires every archetype to be used. Fresh art/content review is mandatory after the headless test re-run.
- Headless content correction verification: **14/14 EditMode tests passed** (7 new content tests and 7 baseline tests) in `TestResults/editmode-content-results.xml`; the matching log is `Logs/editmode-content-tests.log`.
- Pending reviews: content isolated technical/narrative review; content connected runtime/UI review; fresh art/content review after the canonical-key correction; art-direction/mobile-UX/cohesion reviews after assets are connected; integration review after all components connect.
- Minor known gap: the current runtime is still wired to `DevelopmentContent`; content integration is pending and must be performed by the gameplay/integration owner, not through an unreviewed cross-ownership edit.

## Headless Commands and Results

Run from `/home/gm26/games/who-enters`; commands write only transient `TestResults/` and `Logs/` artifacts:

```sh
mkdir -p TestResults
/home/gm26/Unity/Hub/Editor/6000.6.0f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath /home/gm26/games/who-enters \
  -runTests -testPlatform EditMode \
  -testResults /home/gm26/games/who-enters/TestResults/editmode-results.xml \
  -logFile /home/gm26/games/who-enters/Logs/editmode-tests.log
```

Import/compile verification only (no build/export):

```sh
/home/gm26/Unity/Hub/Editor/6000.6.0f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath /home/gm26/games/who-enters \
  -logFile /home/gm26/games/who-enters/Logs/headless-import.log
```

Inspect `TestResults/editmode-results.xml`, `Logs/editmode-tests.log`, and `Logs/headless-import.log` after every relevant milestone. Do not run player builds, web exports, editor GUI automation, or screenshot workflows.

## Ordered Resume Checklist

1. Read this handoff and check `git status`; preserve concurrent agents’ work.
2. Finish and commit the cards/decrees/story content only in its owned paths.
3. Run the headless EditMode data suite; record result paths and event diagnostics.
4. Obtain isolated content review, correct serious issues with Terra, and repeat that review if needed.
5. Have gameplay/integration deliberately wire the approved content provider; do not patch foreign ownership silently.
6. Obtain connected content+runtime/UI review; correct/re-review serious findings.
7. Complete remaining art/audio work, then run all three aesthetic reviewers against isolated and connected states.
8. Run QA/integration headlessly with data/state/event tests and diagnostic logs.
9. When all acceptance gates pass, launch only the Unity Editor for the user to press Play. No build/export/package.
