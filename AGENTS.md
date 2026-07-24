# Conditional Chamber — Codex Handoff

## Project

- Unity 6 project (`6000.4.8f1`) for a horror probability duel inspired by Buckshot Roulette.
- Open `Assets/Scenes/Chamber.unity`; this saved scene is the source of truth and the configured build scene.
- Core runtime code is in `Assets/Scripts/ChamberLogicGame.cs` and `Assets/Scripts/ChamberRound.cs`.
- The current level teaches conditional probability from the known live/blank shell counts.

## Working Rules

- Edit the authored scene and serialized references directly. Do not leave scene/world generation, placement, migration, or audit scripts in the project.
- Keep the experience focused: one seated voodoo-doll opponent, two rigged hands, the clean mechanical duel table, shotgun, skeletal tray bearer, and horror environment.
- Validate gun, hand, shell, muzzle, face, table, and doll geometry numerically from transforms and bounds. Do not use screenshots as the primary alignment check.
- The hands must visibly reach, grip, lift, rack, aim, recoil, and return the shotgun. They never carry individual shells. The gun must rotate naturally and lie flat on the table at rest.
- Live hits make the doll drop mostly downward, hold for roughly 2.5 seconds, then recover if still alive. Avoid a pendulum-like sideways swing.
- Keep UI minimal and pacing slow enough to read the shell reveal and probability explanation.
- Do not redistribute imported Asset Store source files. Their directories are intentionally ignored by Git.
- Track Unity `.meta` files for committed project assets because serialized scene references depend on their GUIDs. Ignore only generated/editor-local artifacts already covered by `.gitignore`.
- Preserve unrelated user changes. In particular, do not stage or revert `Assets/Materials/HeroTable/Table_Walnut.mat` unless the user explicitly asks.
- Use Git author `GeorgesMichalakis <georgios.michalakis26@gmail.com>`. Commit coherent completed changes and push `main` to `origin`.

## Current Authored State

- The doll remains seated until a live hit; only the two hands animate the weapon workflow.
- A CC0 Quaternius skeleton glides beside the player with six approximately 69 mm shells on a carried tray. It withdraws after the reveal, then both hands lift the shotgun and rack it exactly six times.
- The shotgun uses authored boom, blank-click, and action sounds, with dark ambience, a music-box layer, recoil, muzzle flash, and camera impact.
- The environment and runtime were cleaned of obsolete markers, unused colliders, duplicate procedural audio, and unused imported assets in commit `3572e38`.
- Local Asset Store imports required for the full scene are documented in `README.md` and remain outside the public repository.

## Verification Before Saying Done

1. Ensure Unity has compiled without errors and `Assets/Scenes/Chamber.unity` has no missing serialized references.
2. Run Edit Mode tests in `Assets/Tests/EditMode/ChamberRoundTests.cs`.
3. Run Play Mode tests in `Assets/Tests/PlayMode/ChamberScenePlayTests.cs`; these check the tray-bearer presentation, six charging motions, mechanical table/gun geometry, grip poses, aim rotation, lighting/audio, and doll collapse/recovery.
4. Enter Play Mode and exercise dealer aim, self aim, blank, live hit, turn changes, and restart. Inspect transform traces in the Console when diagnosing alignment.
5. Check `git status` before committing; leave the user-owned walnut material change unstaged.

## Resume Prompt

When returning in a fresh Codex chat, open this repository and say:

> Continue Conditional Chamber. Read AGENTS.md and README.md, inspect the current Git status and scene/test state, then continue from the latest commit. Preserve the unstaged walnut material change and verify in Unity Play Mode before committing and pushing.
