# Character and UI Asset Contract

## Runtime inventory

The Unity runtime imports **31 PNG sprites** from `Assets/Art/Characters/` and `Assets/Art/UI/`: sixteen portrait archetypes, nine core UI assets, and six reusable evidence/cue overlays. It also has two explicit redistributable UI fonts in `Assets/Fonts/`. Source-identical mirrors live in `public/assets/characters/`, `public/assets/ui/`, and `public/assets/fonts/`; they are provenance mirrors, not Unity runtime locations.

Canonical portrait file names exactly match `StoryContent.CanonicalPortraitKeys`: `animal`, `bard`, `child`, `cleric`, `commoner`, `courier`, `giant`, `goblin-merchant`, `guard`, `healer`, `knight`, `masked-cultist`, `noble`, `shepherd`, `traveler`, and `witch`.

All portraits are original 1024×1536 RGBA, chest-up art with transparent outer margins **and an intentionally retained in-image orange/black (warm/cool) vignette field behind the visitor**. They are not clean cutouts and must not be composited as though their interiors are transparent. Place one at a time in the card's explicit portrait window, preserve aspect, anchor bottom-centre, and preserve the visible upper-torso baseline; never crop above the forehead or below the upper torso. The card window provides the shared dark-to-amber backdrop so the portrait's retained vignette reads as an intentional, cohesive nested treatment.

| Asset | Purpose | Integration rule |
| --- | --- | --- |
| `visitor-card.png` | Landscape visitor frame | Native `1536×1024` (3:2); `Image.type = Simple`, `preserveAspect = true`, no sprite border/9-slice. Present at `640×427` inside the 640×430 card allocation to avoid distortion. Use the left framed `210×330` portrait window with bottom-centre alignment. Name, dialogue, and evidence stay live text in the quiet right parchment field. |
| `decree-parchment.png` | Decree/tutorial panel | `Image.type = Sliced`; live title and decree copy only. |
| `day-ending-panel.png` | Day summary/ending panel | `Image.type = Sliced`; live score, integrity and epilogue copy only. |
| `admit-button.png`, `deny-button.png` | Decision buttons | `Image.type = Sliced`; labels are live text, not raster text. |
| `verdict-admit.png`, `verdict-deny.png` | Verdict feedback stamps | Native aspect; do not stretch into a button. |
| `integrity-intact.png`, `integrity-broken.png` | Castle integrity seals | Native aspect; five small instances maximum. |
| `Evidence/*.png` | Declarative visual evidence | Always pair with live accessible evidence text. |

## Exact Button state contract

There are intentionally no duplicate pressed/disabled raster buttons: Unity's built-in `Selectable` ColorTint preserves a single polished 9-sliced frame per decision and avoids mismatched generated state art.

- Both Button target graphics use their matching `Image` in `Sliced` mode.
- `transition = ColorTint`, `fadeDuration = 0.08`, `colorMultiplier = 1.0`.
- Admit: normal `#FFFFFFFF`, highlighted `#FFF1C9FF`, pressed `#C2E1C4FF`, selected `#E2F2D7FF`, disabled `#5A6359A6`.
- Deny: normal `#FFFFFFFF`, highlighted `#FFE0D6FF`, pressed `#E8A8A8FF`, selected `#F4CCC7FF`, disabled `#66565AA6`.
- Apply the same disabled alpha to the label; disable raycasts through the parent Button. Do not tint an enabled deny button green or an enabled admit button red.
- Press feedback scales the parent to `0.97` for at most `0.08s`; it never swaps the sprite.

## Evidence/cue binding contract

- `moon seal` → `moon-seal.png`
- `forged seal` → `forged-seal.png`
- `healer writ` or `healer kit` → `healer-writ-kit.png`
- `royal counterseal` → `royal-counterseal.png`
- `red lantern` → `red-lantern.png`
- `cult sigil` or `traitor mark` → `traitor-mark.png`

All other authored cues stay as high-contrast live evidence text until a dedicated overlay is commissioned; integration must not silently substitute a visually unrelated asset. Icons are supplementary, so screen-reader and low-vision players retain the same rule information.

## Importer contract

Every runtime PNG has an explicit Unity importer `.meta`: Sprite, 100 PPU, bilinear filtering, clamp wrapping, sRGB, alpha transparency, mipmaps off, Read/Write off, fallback physics shape off. Android and iPhone use explicit ASTC 4×4 overrides (Unity texture format 54).

| Group | Mobile max size | Sprite border |
| --- | ---: | ---|
| Portraits | 1024 | 0 |
| Visitor card | 1024 | 0 (Simple/aspect-preserved; 9-slice is intentionally unsuitable for this full landscape composition) |
| Decree parchment | 1024 | L/R 64, B/T 96 |
| Day/ending panel | 1024 | L/R 72, B/T 96 |
| Admit/Deny buttons | 1024 | L/R 110, B/T 190 |
| Seals, stamps, evidence | 512 | 0 |

## Visual acceptance constraints

- Portrait silhouettes remain readable at 180–280 logical pixels wide on the 720×1280 canvas.
- UI uses the established midnight blue, antique gold, parchment, emerald-admit and crimson-deny palette.
- No raster asset contains text, logo, watermark, copied game trade dress, or an unlicensed external element.
- Do not use anything in `docs/art/rejected/` or either preflight contact sheet at runtime.

## Typography asset contract

| Role | Stable Unity path | Face | Use |
| --- | --- | --- | --- |
| Display | `Assets/Fonts/GrenzeGotisch-SemiBold.ttf` | Grenze Gotisch SemiBold | Titles, decree headings, visitor names, short stamped feedback only. Keep mixed case and do not use it for long body copy. |
| Body/UI | `Assets/Fonts/AtkinsonHyperlegible-Regular.otf` | Atkinson Hyperlegible Regular | Dialogue, decrees, evidence, buttons, captions, status, and accessibility-critical UI. This is the required default for mobile readability. |

Both font files are direct Unity-imported `Font` assets with generated `.meta` import records. Their full SIL Open Font License 1.1 texts are retained in `Assets/Fonts/Licenses/`, and identical public provenance mirrors retain the same files under `public/assets/fonts/`. Runtime integration must assign these assets directly; `LegacyRuntime.ttf`, operating-system fonts, and implicit built-in fallbacks are forbidden for production UI.
