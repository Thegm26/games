# Art Provenance

## Origin and rights

All final character, UI and evidence PNGs are original project-bound outputs generated with OpenAI's built-in image generation on 2026-09-29, then losslessly resampled/padded with FFmpeg only where needed for the documented Unity size contract. No external character art, game screenshots, trademarks, or reference images were used as source material.

The direction used broad, non-copying traits only: polished storybook-cartoon faces, cute readable silhouettes, dark-fantasy castle mood, midnight-blue parchment, antique gold, emerald admission and crimson denial. No character, composition, logo, text or trade dress was copied from any reference game.

## Final character prompt family

> Use case: illustration-story. Asset type: Unity mobile-game visitor portrait. Primary request: an original chest-up medieval castle visitor with a readable silhouette and expressive face. Style: premium hand-painted storybook dark fantasy, cute but not childish, polished game-ready 2D illustration. Composition: one centred subject, full forehead and upper torso visible, generous outer margins. Constraints: no text, watermark, logo or copied design.

The sixteen final character files were individually generated 1024×1536 RGBA portraits—not contact-sheet crops. Direct inspection confirmed consistent bust framing, intact anatomy, clear faces, transparent outer margins, and purposeful visual differentiation across the archetypes. The alpha channel is not a clean silhouette-only cutout: each portrait intentionally retains a substantial near-opaque warm/cool vignette field behind the subject. The runtime contract is therefore an explicit framed portrait window with a shared vignette backdrop, bottom-centre anchor, and upper-torso visual baseline—not transparent-background compositing.

## Final landscape visitor-card frame

`Assets/Art/UI/visitor-card.png` was replaced on 2026-09-29 with an original horizontal `1536×1024` RGBA card frame generated using the **OpenAI built-in image generation** tool. Final generated artifact before project copy: `/home/gm26/.codex-profiles/account-03/generated_images/01a0eeff-2776-7c20-bd59-e4f956a7e4b1/exec-f2ffc517-4371-4d1c-ae6d-a11fc9864cad.png`. It was copied without post-processing to the runtime path and the byte-identical public mirror.

> Use case: illustration-story. Asset type: Unity mobile-game landscape visitor-card frame, used in a 640×430 logical allocation. Primary request: one original polished horizontal 3:2 fantasy castle gatekeeper visitor-card frame with an explicit tall framed portrait window on the left and a separate open parchment information field on the right. Scene/backdrop: ornate medieval castle gatehouse UI, not a physical scene. Subject: border/frame only; left portrait window is a dark midnight-blue-to-warm-amber vignette backdrop in a gold-and-iron arched frame, cleanly readable for an opaque chest-up visitor portrait. Right side has a large quiet warm parchment field for live visitor name, dialogue and evidence, with a subtle lower evidence divider/medallion zone and no text. Style/medium: premium hand-painted storybook dark fantasy UI illustration, cute but sophisticated, cohesive antique wood, iron, parchment, dark indigo, aged gold and restrained plum accents. Composition/framing: landscape 3:2 source; important ornament inside a 96px outer safe margin; portrait window about 34% width and 78% height, visually bottom-aligned; quiet right field about 56% width. The finished full composition is intentionally displayed Simple/aspect-preserved at 640×427, not 9-sliced. Constraints: no words, letters, numbers, logos, trademarks, watermark, characters, faces, hands, weapons or copied game trade dress. Avoid: busy reading field or generic empty frame.

Direct visual inspection confirmed a polished gatehouse construction, a distinct framed vignette window on the left, quiet high-contrast live-text zones on the right, no raster text/watermark/character, and readable detail at intended scale. Technical inspection confirmed `1536×1024`, RGBA, meaningful RGB variation (channel standard deviations `83.9/71.8/52.8`), and non-flat alpha edge treatment. The runtime sprite keeps its existing stable GUID and imports at `1024×683` under the existing ASTC 4×4 policy.

## Redistributable typography

Two unmodified, redistributable font binaries were downloaded directly from their authoritative upstream repositories on 2026-09-29. Both are SIL Open Font License 1.1 (full license text is bundled beside the assets). They are source-identical to their public mirrors and are not system-font references.

| Runtime asset | Upstream source (pinned commit) | License text | SHA-256 | Modification |
| --- | --- | --- | --- | --- |
| `Assets/Fonts/GrenzeGotisch-SemiBold.ttf` | `https://raw.githubusercontent.com/Omnibus-Type/Grenze-Gotisch/7b5eac166bc3b2a519f98b5c124cb7a11670cc7b/fonts/ttf/GrenzeGotisch-SemiBold.ttf` | `Assets/Fonts/Licenses/Grenze-Gotisch-OFL-1.1.txt` | `ef529138dddb80c2269cc2e46f400f43320e73694f28646dd617753a98dd16f4` | None |
| `Assets/Fonts/AtkinsonHyperlegible-Regular.otf` | `https://raw.githubusercontent.com/googlefonts/atkinson-hyperlegible/1cb311624b2ddf88e9e37873999d165a8cd28b46/fonts/otf/AtkinsonHyperlegible-Regular.otf` | `Assets/Fonts/Licenses/Atkinson-Hyperlegible-OFL-1.1.txt` | `4a0397a3709c5fc99e38d05469dcfbf1b3481196e89a01b7377f3163b188258e` | None |

Grenze Gotisch is the agreed ornate display direction. Atkinson Hyperlegible was selected for body/UI because its intentionally differentiated letterforms target low-vision readability and mobile scanning. Authoritative source pages: `https://github.com/Omnibus-Type/Grenze-Gotisch` and `https://github.com/googlefonts/atkinson-hyperlegible`.

## Final UI and evidence prompt family

> Use case: illustration-story. Asset type: Unity mobile-game UI/evidence overlay. Primary request: one isolated ornate dark-fantasy interface asset with midnight blue, antique gold and parchment. Constraints: one complete asset, transparent where appropriate, no text, labels, watermark, logo, characters or copied design.

The six evidence files were generated in separate calls and padded onto transparent 512×512 runtime canvases: `moon-seal`, `forged-seal`, `royal-counterseal`, `healer-writ-kit`, `red-lantern`, and `traitor-mark`. Direct inspection covered unwanted text, watermarking, alpha contamination, semantic readability, palette continuity and anatomy. No serious visual defect remained after inspection.

## Tool credit

- OpenAI image generation: original project-bound character, UI and evidence raster art.
- FFmpeg: transparent-canvas sizing only for the six evidence overlays.
- Human/team credit: Thegm26, game direction.
