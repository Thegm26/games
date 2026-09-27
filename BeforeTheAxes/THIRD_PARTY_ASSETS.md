# Third-party assets

## SunnyLand Woods

- Source: `Assets/Art/SunnyLand/Woods/Source` (including the preserved original
  `public-license.txt` and `sunny-land-woods-files.zip`).
- Artwork: Luis Zuno / @ansimuz.
- License: public domain; free for personal or commercial use. Credit is not
  required, but is appreciated. The source license also identifies Pascal
  Belisle as the music author; this project uses no SunnyLand music.

## SunnyLand Tall Forest

- Source: `Assets/Art/SunnyLand/TallForest/Source` (including the preserved
  original `public-license.txt` and `tall_forest_files.zip`).
- Artwork: Luis Zuno / @ansimuz.
- License: permitted for personal or commercial projects, modification, and
  redistribution; credit is not required, but is appreciated.

The two scenes in `Assets/Scenes` use only the original raster artwork above.

## Village arrival foliage

- Reused visual: `forestpack_tree_1_leaf_1` mesh and
  `forestpack_tree_leaf_low` material from the installed `Supercyan Free Forest
  Sample` package.
- Source location: `Assets/Supercyan Free Forest Sample/Models/Tree/Leaf` and
  `Assets/Supercyan Free Forest Sample/Materials/Mobile/Tree`.
- Package metadata identifies the source as `Environment Pack: Free Forest
  Sample` (version 2.0.1, Unity Asset Store product 168396). The package's
  original readme PDFs remain alongside the source; this entry intentionally
  does not make a license claim beyond those supplied materials.
- Use in game: the village-arrival burst renders the original low-poly leaf
  mesh/material directly; it does not generate a mesh, texture, or material at
  runtime.

## Adaptive forest music

- Track: `Stealth in the Woods` by Lisboa.
- Source: https://opengameart.org/content/stealth-music
- License: CC0 1.0 (public domain); the source explicitly describes it as a loopable
  track free to use in games. No attribution is required.
- Downloaded file: `Assets/Audio/Music/StealthInTheWoods_CC0.mp3` (6.1 MB source,
  44.1 kHz stereo MP3). This is a 2017 human-made OpenGameArt submission, not
  generative-AI content.
- Use in game: the calm loop plays normally; a second, pitch-raised instance of the
  same composition crossfades in only during direct pursuit, preserving the woodland
  musical language while making the chase feel urgent.

## Gameplay sound effects

- Mushroom file: `Assets/BeforeTheAxes/Audio/mushroom_pickup_confirmation_004.ogg`.
- Source: [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds),
  Kenney Vleugels / Kenney.nl; downloaded from the original asset archive.
- License: [Creative Commons Zero 1.0 (CC0)](https://creativecommons.org/publicdomain/zero/1.0/).
- Selection: `confirmation_004.ogg` is the short, gentle objective-pickup cue.
  It is a human-made Kenney asset, not generative-AI content.

## Woodcutter caught sound

- File: `Assets/BeforeTheAxes/Audio/caught_wood_thud_000.ogg`.
- Source: [Kenney Impact Sounds](https://kenney.nl/assets/impact-sounds),
  `Audio/impactWood_medium_000.ogg` from the original archive
  `kenney_impact-sounds.zip`; Kenney Vleugels / Kenney.nl.
- License: [Creative Commons Zero 1.0 (CC0)](https://creativecommons.org/publicdomain/zero/1.0/).
- Use in game: a 0.333-second, low-mid wooden thud used only for the caught state.
  It replaces the generic UI error beep to fit the stylized woodland woodcutters.
  The source clip is unmodified and human-made, not generative-AI content.

## Tree transformation sound

- File: `Assets/BeforeTheAxes/Audio/tree_transform_leaf_rustle_003.ogg`.
- Source: [Kenney RPG Audio](https://kenney.nl/assets/rpg-audio),
  `Audio/cloth3.ogg` from the original archive `kenney_rpg-audio.zip`; Kenney Vleugels / Kenney.nl.
- License: [Creative Commons Zero 1.0 (CC0)](https://creativecommons.org/publicdomain/zero/1.0/).
- Use in game: the unmodified 0.477-second, soft cloth-and-leaf-like rustle plays once when the
  guardian successfully enters tree form. It is a low-intensity physical woodland cue, not a UI
  beep or an AI-generated asset.
