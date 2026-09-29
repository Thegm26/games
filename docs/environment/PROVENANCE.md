# Environment Provenance

## Creation method

- Tool: built-in OpenAI image generation tool, `stylized-concept` use case.
- Human visual inputs: none.
- External art, textures, reference images, or downloaded assets: none.
- Reference policy: broad traits only — fairy-tale castle gate, warm/cool decision contrast, portrait mobile composition. No character, writing, symbol, layout, or trade dress was copied from reference games.
- All final layer files are original AI-generated artwork, resampled with FFmpeg/Lanczos to their contract dimensions and losslessly stored as PNG. The Unity runtime copies are under `Assets/Art/Environment/Runtime/`; matching non-runtime source copies are under `public/assets/environment/`.

## Prompt records

1. **Preflight** — original storybook-cartoon dark-fantasy gatehouse viewed outward, warm torch-lit inward path on left, cold moat on right, uncluttered central card space, no UI/text/characters.
2. **Castle vista** — distant warm castle and cobbled admit path on left, cold blue-black moat gorge on right, dusk sky, central card-safe region, no foreground gate or UI.
3. **Gate frame** — isolated chunky stone arch, open wooden doors, top portcullis, transparent central opening; no backdrop or glow.
4. **Fog and rain** — sparse transparent teal fog at the right/lower moat edge with faint edge rain; center nearly clear.
5. **Torches and glow** — two isolated warm torch sprites and feathered glows with transparent surroundings.
6. **Foreground silhouettes** — isolated lower-edge welcoming parapet/pines left and hostile rocks/chain/reeds right; transparent card-safe center.

## Inspection outcome

The preflight was directly inspected before final-layer generation. It establishes a readable warm-left/admit versus cold-right/deny split, gate-arch focal framing, sufficient central negative space, and a cute-dark storybook tone. Final-layer verification is deliberately data-only: dimensions, pixel formats, alpha extrema, file sizes, sorting contract, and SHA-256 checksums are recorded in `asset-manifest.json`.

## Known generation constraint

The generator returned `941 x 1672` source images rather than the requested source resolution. Each final layer was resampled to the exact `1440 x 2560` Unity contract. This is a source-detail limitation, not a runtime dimension mismatch; do not treat the resampled layer as a substitute for a future higher-native-resolution marketing illustration.
