# Castle Environment Asset Contract

`Assets/Art/Environment/Runtime/` contains the Unity-imported PNG layers for the `Who Enters?` gate scene. `public/assets/environment/` retains matching source copies and preflight material only; it is not a Unity runtime location. All runtime layers are exact `1440 x 2560` 9:16 portrait assets.

## Unity import defaults

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Pixels Per Unit: `100`
- Mesh Type: `Full Rect`
- Generate Mip Maps: disabled
- Wrap Mode: `Clamp`
- Filter Mode: `Bilinear`
- sRGB: enabled
- Alpha Is Transparency: enabled for RGBA layers; disabled/irrelevant for the opaque vista
- Android and iPhone overrides: enabled, Max Size `2048`, ASTC 6x6, quality `50`. The opaque RGB vista uses Unity `TextureImporterFormat` `50` (`ASTC_RGB_6x6`); RGBA overlays use `54` (`ASTC_RGBA_6x6`).
- Generate Fallback Physics Shape: disabled; these sprites are decorative and must not create 2D colliders.
- Max Size: 2048 for all final layers; the preflight is limited to 1024

## Sorting and composition

Use one `Sorting Layer` named `Environment` and this order, from back to front:

| Order | File | Role | Runtime motion |
| ---: | --- | --- | --- |
| 0 | `Assets/Art/Environment/Runtime/01-castle-vista.png` | Opaque distant castle, warm approach, cold moat | slow 0.02x parallax |
| 10 | `Assets/Art/Environment/Runtime/02-gate-frame.png` | Gatehouse stone, open doors, portcullis | static |
| 20 | `Assets/Art/Environment/Runtime/03-fog-rain.png` | Low-contrast weather overlay | fog 0.08x horizontal drift; rain UV/position loop |
| 30 | `Assets/Art/Environment/Runtime/04-torches-glow.png` | Torches and warm glow | 0.95–1.05 alpha/intensity flicker |
| 40 | `Assets/Art/Environment/Runtime/05-foreground-silhouettes.png` | Parapet, pines, moat rocks and chain | 0.12x parallax |

Place visitor cards and decree UI above order 40, preferably on their own `GameplayUI` sorting layer. The card-safe region is approximately `x 20%–80%` and `y 24%–84%` on every layer. Do not mirror the scene: the warm approach remains left and the moat remains right, while the explicit interaction mapping is right/admit and left/deny.

## Runtime texture budget

At import resolution, uncompressed backing memory is about 56.25 MiB for the four RGBA layers plus 10.55 MiB for the RGB vista. Platform ASTC settings above reduce this substantially; retain the PNGs as editable source assets and let Unity create platform-compressed textures. Avoid duplicating these full-size textures in memory.

## Preflight

`public/assets/environment/preflight/gatehouse-preflight.png` is the true `720 x 1280` validation export. The adjacent `gatehouse-preflight-source-941x1672.png` is retained solely as the unscaled generated source; consumers must not reference it at runtime.
