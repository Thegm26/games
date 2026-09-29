# Who Enters? — Session Handoff

**Last updated:** 2026-09-29 (Europe/Paris)  
**Immediate next action:** Presentation must consume the branch-specific visitor IDs and terminal epilogue flags, then an integration owner must run the connected content+runtime/UI review. Every future agent must read this file before any action and may not relax these constraints.

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
- **Content implementation checkpoint:** `0ae159fe7e1bb4fe90864084ca60d04bcfb1b351` contains the current story-branch correction; it was published non-destructively after fetch confirmed no divergent remote commits. Push every later checkpoint after fetch/divergence inspection.

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
- Character/UI art isolated milestone is **PASS**: 16 canonical portrait keys, 9 core UI sprites, and 6 individual evidence/cue overlays (31 runtime PNGs total) live under `Assets/Art/Characters/` and `Assets/Art/UI/`, with exact public mirrors. `docs/art/ASSET_CONTRACT.md`, `PROVENANCE.md`, and schema-v2 `asset-manifest.json` are the source of truth for paths, dimensions, hashes, alpha, memory, import settings, GUIDs, provenance, button ColorTint state behavior, and evidence binding. Fresh technical and art-direction isolated reviews passed after correcting Unity's rewritten mobile importer records: 31 Android and 31 iPhone ASTC 6×6 overrides at texture format 54; zero iOS/48 overrides; 31 unique GUIDs; exact mirrors and zero manifest mismatches. Exact source RGBA sum is 163,575,376 bytes; auditable ASTC 6×6 estimate is 7,576,000 bytes (7.225 MiB). Independent Unity import/full EditMode rerun passed **43/43** at `TestResults/editmode-art-monitor-rereview.xml` with no import, compile, or fatal errors. Connected content/runtime and aesthetic review remains mandatory for card binding, mobile density, portrait differentiation, and evidence silhouette compactness.
- Active/expected roles: character/UI-art implementation active; cards/narrative implementation active; technical monitor active. Later: audio implementation; QA/integration; Sol aesthetic reviews for art direction, mobile visual UX, and cohesion/polish.
- **Serious review correction in progress:** the first content draft exposed roughly 43 portrait keys, conflicting with the approved sixteen-archetype art pipeline. Content now owns the fixed canonical set (`guard`, `courier`, `witch`, `goblin-merchant`, `commoner`, `shepherd`, `knight`, `noble`, `cleric`, `healer`, `child`, `giant`, `animal`, `bard`, `traveler`, `masked-cultist`), validates every key, and requires every archetype to be used. Fresh art/content review is mandatory after the headless test re-run.
- Story-branch correction checkpoint: each Mira, Pip, Nella, and Rowan early outcome now resolves to a distinct later card with distinct visible evidence, dialogue, terminal epilogue input, and no silent reconvergence. `StoryContent.TerminalOutcomeFlags` plus `EpilogueInputs` make every outcome either consumed by a later conditional card or explicitly terminal. The fresh isolated Sol content review **PASSed with no serious or minor findings**.
- Published checkpoint: `c2b3fb3 feat: add procedural gatehouse audio` is on `origin/main`; it follows the published content and presentation checkpoints and adds only audio-owned runtime/tests/docs plus the narrow audio hooks in `GameDirector`. Concurrent art, environment, public, and test-result work remains unstaged and untouched.
- Headless branch verification: the filtered content suite is **10/10 passing** in `TestResults/editmode-content-branches.xml`; matching diagnostics are in `Logs/editmode-content-branches.log`, including `content.branch_diagnostic` for all four chains. A previous full suite was **24/27**, with all three failures owned by in-progress Presentation work: seven newly added fallback visitor captions, a `GameDirector.SetReducedMotion` null guard, and a typewriter punctuation expectation. Re-run the full suite after the presentation owner completes its correction.
- Pending reviews: presentation must map all current authored visitor IDs and all `TerminalOutcomeFlags`; content connected runtime/UI review; fresh art/content review after the canonical-key correction; art-direction/mobile-UX/cohesion reviews after assets are connected; integration review after all components connect.
- Narrative-presentation candidate is implemented under `Assets/Scripts/Presentation/` with a pure grapheme-aware 35 cps typewriter, punctuation pauses, first-action-completes/second-action-continues safety, caption catalog, story/outcome/ending copy, and raster-art binding hooks. The first isolated review found four serious defects (overlay keyboard leakage, three-action visitor verdicts, non-immediate reduced motion, and incomplete terminal epilogues); all were corrected. `GameDirector` now calls it for title, tutorial, daily decree, visitor, verdict, summary, and endings, blocks every hidden-modal decision route, and aggregates reached terminal epilogues in stable story order. Full headless EditMode verification passed **35/35** at `TestResults/editmode-presentation-rereview.xml` with `Logs/editmode-presentation-rereview-pass.log`. The mandatory fresh isolated review independently reran 35/35 and **PASSed with no findings**; the scoped checkpoint may be committed. Connected runtime/content/mobile-UX/cohesion review remains required after the approved content provider is wired.
- Audio milestone complete: `Assets/Scripts/Audio/` provides an original deterministic 22,050 Hz mono procedural cue palette (castle wind/drone, bell, gate, tactile card cues, admit/deny, verdict/feedback, four endings, UI) with no downloaded samples. `GameAudio` creates clips/sources only after a deliberate interaction, persists mute, applies per-cue cooldown/polyphony, and emits removable `audio.*` diagnostics. A serious first-swipe parity defect was found in the first isolated Sol review: `input.drag_start` routed card pickup while still audio-locked. It was corrected so the synchronous primary pointer-down unlocks before pickup routing; first noncommitting swipes now provide ambient, pickup, rate-limited drag, and snapback, while committed swipes share that prefix and leave verdict sounds to `GameDirector`. HOW TO PLAY now follows the same activation/UI-click path. Focused EditMode verification is **8/8 passing** at `TestResults/editmode-audio.xml`; full EditMode verification is **43/43 passing** at `TestResults/editmode-all-after-audio.xml`. The mandatory fresh isolated Sol re-review independently reran **8/8** and **PASSed with no findings**. Connected runtime/content/art review remains mandatory after integration.
- Minor known gap: the current runtime is still wired to `DevelopmentContent`; content integration is pending and must be performed by the gameplay/integration owner, not through an unreviewed cross-ownership edit.

## Headless Commands and Results

Run from `/home/gm26/games/who-enters`; commands write only transient `TestResults/` and `Logs/` artifacts:

```sh
mkdir -p TestResults
/home/gm26/Unity/Hub/Editor/6000.6.0f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /home/gm26/games/who-enters \
  -runTests -testPlatform EditMode \
  -testResults /home/gm26/games/who-enters/TestResults/editmode-results.xml \
  -logFile /home/gm26/games/who-enters/Logs/editmode-tests.log
```

Import/compile verification only (no build/export):

```sh
/home/gm26/Unity/Hub/Editor/6000.6.0f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /home/gm26/games/who-enters \
  -logFile /home/gm26/games/who-enters/Logs/headless-import.log
```

Inspect `TestResults/editmode-results.xml`, `Logs/editmode-tests.log`, and `Logs/headless-import.log` after every relevant milestone. Do not run player builds, web exports, editor GUI automation, or screenshot workflows.

## Ordered Resume Checklist

1. Read this handoff and check `git status`; preserve concurrent agents’ work.
2. Presentation owner maps current content visitor IDs and every `StoryContent.TerminalOutcomeFlags` consequence; rerun its missing-mapping check.
3. Run the full headless EditMode suite and record XML/log totals. Correct the Presentation-owned failures before treating the content branch checkpoint as connected.
4. Have gameplay/integration deliberately wire the approved content provider; do not patch foreign ownership silently.
5. Obtain connected content+runtime/UI review; correct/re-review serious findings.
6. Complete remaining art/audio work, then run all three aesthetic reviewers against isolated and connected states.
7. Run QA/integration headlessly with data/state/event tests and diagnostic logs.
8. When all acceptance gates pass, launch only the Unity Editor for the user to press Play. No build/export/package.
