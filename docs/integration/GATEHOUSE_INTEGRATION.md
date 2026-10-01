# Gatehouse Integration Contract

The first playable Unity version is a runtime-created portrait `720×1280` gatehouse UI. It deliberately uses `StoryContent.Create()`; `DevelopmentContent` is never selected in production.

## Serialized visual catalog

`Assets/Resources/Integration/VisualAssetCatalog.asset` contains direct Unity Sprite references only. It has five environment layers, sixteen canonical portraits, nine core UI sprites, and six evidence overlays. No texture is copied or loaded from `public/` at runtime. Rebuild it headlessly after a legitimate art-path change:

```sh
/home/gm26/Unity/Hub/Editor/6000.6.0f1/Editor/Unity -batchmode -nographics \
  -projectPath /home/gm26/games/who-enters \
  -executeMethod WhoEnters.Editor.Integration.VisualAssetCatalogBuilder.Rebuild \
  -logFile /home/gm26/games/who-enters/Logs/integration-catalog-build.log
```

The process is intentionally headless. Do not automate editor clicks, mouse input, screenshots, screenshot tests, builds, exports, or packaging.

## Runtime layout and motion

- Environment layers use the authored order `0, 10, 20, 30, 40`; its warm-left/cold-right story contrast remains visual framing only. The explicit control contract is swipe/right-arrow/right button = admit and swipe/left-arrow/left button = deny. The environment and night tint stretch to full canvas, while every 9:16 sprite uses **cover** policy (preserved aspect with surplus cropped, never distorted).
- `SafeAreaLayoutRoot` places interactive UI inside `Screen.safeArea`; the numeric policy is validated at `360×640`, `390×844` with a notch inset, and `720×1280`. Both decision targets are `270×104` logical pixels; the `172×100` utility targets retain a complete one-line `Motion On/Off` state at the 32px body floor. At the narrow inset scale of `.45`, every player control is at least `44` physical pixels high. Body text has a `32` logical-pixel floor, or `14.4` physical pixels at that scale.
- The visitor card is the native-aspect `690×460` runtime presentation of the horizontal `1536×1024` asset. One base card raster remains behind a code-native, mask-only arched aperture; no opaque foreground card is redrawn. Portraits are bottom-aligned in the left-shifted aperture, and their rendered right edge has a measured five-physical-pixel gutter before the text column. The live dossier is a stable `Documents`, `Visible Signs`, `Traits` surface containing all and only the visitor’s one through four unique authored facts; it is separated from dialogue and the `220×116` icon zone by verified world gutters. It contains no rule id or verdict. Evidence icons are `116×116`, spaced `124` logical pixels when paired, fully card-contained (including every authored two-icon dossier), non-raycastable, and supplementary to the accessible dossier text. At most two authored cues become icons; any remaining cue remains fully visible in the text dossier.
- Sliced card/parchment/panels/buttons use the approved sprites. Evidence icons appear only when the authored cue has an exact approved overlay, while live text always remains.
- Card snapback: `0.18s` back to its authored home. Commit: `0.26s` to `x = +860` admit / `-860` deny. Stamp: `0.38s`, scale `1.45→1.00`, on the stable feedback HUD; damage shake (`0.22s`, amplitude `13`) targets that HUD and never writes the exiting card transform.
- Visitor and verdict typewriter lines use a dedicated non-decision `TAP TO REVEAL` / `CONTINUE` surface. Admit/Deny controls are dimmed, non-interactable, and non-raycastable until visitor text is dismissed. A consumed swipe resets the card before any later verdict gesture.
- The overlay primary CTA is caption-owned while narration is active: it begins as `TAP TO REVEAL`, changes to `CONTINUE` only when the active line is complete, and becomes `PLAY AGAIN` only after the ending sequence completes. Utility/button copy uses Atkinson Hyperlegible; Grenze Gotisch is reserved for display headings.
- Direct Play uses the legacy Input Manager (`activeInputHandler: 0`) because keyboard fallbacks and uGUI use `UnityEngine.Input` with `StandaloneInputModule`. `Gatehouse.unity` is build-enabled for editor direct Play and includes an enabled MainCamera; the gameplay canvas remains Screen Space Overlay for mobile UI.
- Reduced motion is a visible toggle and propagates to typewriter, environment, cards, stamps, damage shake, and button press response. It uses immediate deterministic endpoints, zero drift/tilt/travel/shake, and fixed torch alpha.
- `DebugTrace` emits `integration.*`, `environment.*`, `asset.visual_bound`, and `animation.*` events. Keep these until the explicit debug-removal pass. Every routine event remains in the structured `Recorded` stream and the bounded newest-event overlay tail; Unity-console mirroring is disabled by default and is an explicit opt-in capped at 64 routine entries per cleared diagnostic session. Errors still use `Debug.LogError` so failures remain visible.

## Validation

The integration tests assert direct catalog completeness, all canonical key references, visible binding of all sixteen portraits without an opaque foreground redraw, authored and normalized evidence resolution, environment cover/stretch geometry, actual RectTransform/world-corner card geometry, safe-area viewport matrix, touch/font/rect/text-capacity contracts, caption-owned CTAs, legacy input/EventSystem/camera/direct-Play configuration, StoryContent runtime selection, caption gating, consumed-swipe reset, reduced motion, stable stamp/shake hierarchy, full verdict continuation, integrity seals, and audio cue availability.
