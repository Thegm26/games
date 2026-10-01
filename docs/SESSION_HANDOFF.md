# Who Enters? — Session Handoff

**Last updated:** 2026-10-01 (Europe/Paris)
**Current state:** The harder-deduction default experience is implemented, headlessly verified, and has passed a fresh final documentation/cohesion re-review with no serious or minor findings. The next step is fetch/divergence and Git-identity inspection before a scoped commit and push; exclude transient `TestResults/`, `Logs/`, and `TestArtifacts/`. Do not build, export, package, or create an itch archive.

## Non-negotiable continuation rules

- Read this handoff, `/home/gm26/AGENTS.md`, and inspect `git status` before acting. Preserve all concurrent dirty work and respect bounded ownership.
- Every implementation edit, including tests, content, UI, audio, assets, and corrections, belongs to a dedicated **gpt-5.6-terra** agent. **gpt-5.6-sol** is planning, monitoring, and review only.
- Every reviewer inspects its isolated component and its behavior connected to dependencies. A serious finding requires a Terra correction and a fresh review; never waive or reuse a stale pass.
- QA is headless and evidence-based: data, state, structured events, diagnostics, and tests only. Any earlier screenshot, video, or rendered-capture acceptance gate is superseded and removed. Do **not** automate Unity GUI/mouse input, capture screenshots or video, use screenshot tests, render-capture workflows, build, export, package, or archive.
- Unity **6000.6.0f1** may be opened only when the user explicitly asks to press Play. Do not control an already-open editor.
- Keep the structured debug stream and debug overlay until an explicit later cleanup pass. Debug output must remain useful without unbounded console logging.
- Preserve `/home/gm26/game` and `/home/gm26/blender-agent-pipeline`; neither belongs to this repository task.
- Treat `TestResults/` and `Logs/` as transient evidence: never include them in a commit.

## Product and repository

- **Game:** *Who Enters?*, a portrait, cute-but-dark storybook-fantasy castle gatekeeper game for the Slapjam “Castles” theme.
- **Repository:** `/home/gm26/games/who-enters`, branch `feature/gameplay-core`; remote `https://github.com/Thegm26/who-enters.git`.
- **Unity:** `/home/gm26/Unity/Hub/Editor/6000.6.0f1/Editor/Unity`.
- **Git identity:** `Thegm26 <georgios.michalakis26@gmail.com>` for both author and committer.
- **Controls:** portrait logical 720×1280; swipe right = Admit and swipe left = Deny. Visible buttons plus arrow/D-A keyboard fallbacks have parity. Reduced motion remains supported.
- **Run:** five days × eight frozen slots = forty decisions. Five integrity seals, existing scoring/streak behavior, deterministic seeded queues, four story chains, all four endings, and replay remain intact.

## Authoritative harder-deduction contract

- Every visitor shows a complete categorized dossier of all authored **Documents**, **Visible Signs**, and **Traits**: one to four concise, unique facts, deterministically ordered and sufficient to reproduce evaluation. No pre-choice verdict, winning rule, or hidden evidence is shown.
- Rules are priority ordered: first matching rule wins, with an explicit default-deny/no-exception outcome. Opposite-verdict priority conflicts are route-independent and exactly **D1=1, D2=2, D3=3, D4=4, D5=5** (Day 1=1, Day 2=2, Day 3=3, Day 4=4, Day 5=5). Remaining cases use harmless distractors or incomplete compound-rule near misses.
- All 64 authored visitor IDs and story routes, every flag/branch/slot, and all four endings remain reachable. Conditional alternatives in the same slot preserve equivalent deduction difficulty.
- `VerdictRecord` retains `RuleId` and full `RuleExplanation`; deterministic evaluation and structured diagnostics retain provenance, conflict, dossier-size, duplicate-fact, priority-copy, and answer-leak checks.
- The optional Rulebook is the sole player-facing full decree/rule surface. Ordinary flow does not repeat rules. Pre-decision captions/dialogue are atmospheric or instructional only and cannot recommend a verdict or reveal a winning rule. After a choice, one concise human-readable deciding reason (or default explanation) precedes the existing story consequence; the full `RuleExplanation` remains available in the record and diagnostics, not repeatedly narrated to the player.

## Presentation, input, diagnostics, and audio

- The dossier card uses the established mobile-safe text/card zones at a 32px logical readable floor, with code-native readable backing; all facts are textual. Existing evidence art is supplementary only, non-raycast, capped at two icons in authored order, and tested for containment and >=44px physical silhouette across 360×640, 390×844, and 720×1280.
- Rulebook modal gating is correct through caption completion. It masks decision routes while open, then recomputes readiness on close. Swipe, visible buttons, and keyboard respect the same gating.
- HUD seals follow the five-seal policy. `DebugTrace.Recorded` remains exhaustive for test/data inspection; the overlay/console mirror is bounded, console mirroring defaults off, and errors remain visible.
- All 64 direct visitor dialogue lines are distinct, atmospheric, evidence-safe, and <=44 characters; they do not reveal the verdict or repeat rulebook copy.
- The old harsh arrival sound is replaced by a restrained procedural arrival cue. A serious cadence review finding was corrected: encounter slots are consumed only after actual playback, yielding 40 actual plays per run and 40 on replay; duplicate requests do not consume slots or create duplicate plays. Existing standard cue cooldown and polyphony behavior remains in force.

## Current implementation ownership and important paths

- Core/contracts: `Assets/Scripts/Core/GameContracts.cs`, `RuleEvaluator.cs`, `RunStateMachine.cs`, `DebugTrace.cs`.
- Authored content/dossiers: `Assets/Scripts/Content/**` and content EditMode tests.
- Runtime presentation/input: `Assets/Scripts/Gameplay/GameDirector.cs`, presentation/UI tests, and existing art bindings.
- Audio: `Assets/Scripts/Audio/**` and audio tests.
- Numeric mobile/layout contract: `docs/integration/GATEHOUSE_INTEGRATION.md`.
- Keep existing original art/audio provenance and asset contracts intact. Do not add raster assets unless a newly reviewed blocking defect requires it.

## Authoritative verification evidence

All verification was headless in Unity 6000.6.0f1, using data/state/events/tests. The final full result supersedes earlier full-suite counts; no source or test is newer than it.

- `TestResults/editmode-arrival-audio-focused-final.xml`: **15/15 PASS**.
- `TestResults/editmode-arrival-connected-final.xml`: **1/1 PASS**.
- `TestResults/editmode-arrival-full-final.xml`: **119/119 PASS** (authoritative full suite).
- Supporting final evidence: `TestResults/editmode-rulebook-readability-final4.xml` **110/110 PASS**; `TestResults/editmode-rulebook-interleaving-full.xml` **115/115 PASS**; dialogue focused suite **19/19 PASS** and earlier full suite **118/118 PASS** (superseded by 119/119); HUD/diagnostics full suite **117/117 PASS**.
- `git diff --check` passed after the final arrival/audio correction. Re-run it after any later edit.

## Fresh review record

- Core technical review: **PASS**.
- Content/fairness review: **PASS**.
- Mobile dossier/icon containment review: **PASS**.
- Rulebook isolated and connected review: **PASS**.
- Dialogue/content isolated and connected review: **PASS**.
- HUD/audio re-review, including arrival cadence/replay: **PASS**.
- Connected runtime/data-state review across gameplay, content, presentation, art bindings, audio cadence, and input routes: **PASS**.
- Fresh final documentation/cohesion re-review: **PASS** — no serious or minor findings.

## Headless test policy

Run Unity only with `-batchmode -nographics`, preferably in a fresh `/tmp` project mirror while the user may have the original project open. Inspect XML/log evidence after a relevant change. Do not run player builds, web exports, GUI automation, screenshot/video capture, or screenshot tests.

```sh
/home/gm26/Unity/Hub/Editor/6000.6.0f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath <fresh-temp-mirror> \
  -runTests -testPlatform EditMode \
  -testResults <mirror>/TestResults/editmode-results.xml \
  -logFile <mirror>/Logs/editmode-tests.log
```

## Ordered resume checklist

1. Fetch and inspect remote divergence plus effective Git identity; preserve unrelated dirty changes.
2. Make a scoped commit and push only the intended source/docs/assets, explicitly excluding transient `TestResults/`, `Logs/`, and `TestArtifacts/`. Do not claim a commit or push has occurred before then.
3. Open Unity 6000.6.0f1 only if the user explicitly asks to press Play; never build, export, or package as part of this handoff.
