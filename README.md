# Chamber Logic — Level 1

An original, Buckshot-inspired 3D probability duel for Unity. Every shell count is public; the tension comes from deciding with changing conditional odds.

## Run

Open this folder in Unity 6, open `Assets/Scenes/Chamber.unity`, and press Play. The editable scene is also the configured build scene.

The public source repository intentionally excludes raw Unity Asset Store files. For the fully authored local scene, acquire and import these packages through Package Manager > My Assets before opening the scene: `Voodoo Doll – Stylized 3D Prop` by DarkDrop Studio, `Low Poly ShotGun Weapon Pack 1` by CASTLE BRAVO, `Horror Game Essentials` by The Artifact, and `Stylized - Simple Hands`. The scene uses VoodooDoll, ShotGun_C, the black rigged hand prefab, GhostChild_Pro_1, and Fall_Bones_2 from those packages.

The saved scene is directly authored: DarkDrop Studio's textured Voodoo Doll sits fixed in the dealer chair while two black rigged hands remain hidden below the table until they reach for the weapon. A skeletal bearer glides in beside the player and presents six approximately 69 mm shells on a metal tray. After the camera reveals the complete live/blank mix, the bearer withdraws. Both hands then grip and lift the shotgun, rack its action exactly once for each of the six shown shells, and lower it flat onto a clean oxidized-steel table. The hands later hold the ShotGun C model low at the doll's side when aiming at the player and place the muzzle beside the doll's head for a self-shot. A live hit makes the doll collapse mostly straight down, remain down for about 2.5 seconds, and recover if it still has health. Recoil, muzzle flash, camera impact, a spatial ghost-child vocal, a bone-impact fall, layered gun audio, dark ambience, and a quiet spooky music box support the reactions.

Use `1` to aim at the dealer, `2` to aim at yourself, and `R` to restart. Play Mode tests validate the clean mechanical table, skeleton tray presentation, six shotgun charging motions, grip points, muzzle-to-face aim, and doll reactions.

## Lesson

`P(live next | revealed shells) = live shells remaining / shells remaining`.

The player gets counts during the challenge. Once the round ends, the after-action report walks through every probability update. A blank fired at yourself retains your turn; every other result hands the turn to the dealer.

## Art license

The industrial environment and shell-crate assets come from Kenney's CC0 packs. The skeletal bearer comes from Quaternius's [Animated Monster Pack](https://quaternius.com/packs/animatedmonster.html), licensed CC0; its license is included beside the model. DarkDrop Studio's Voodoo Doll, the ShotGun C model, Stylized - Simple Hands, and the selected ghost-child/fall sounds are Unity Asset Store content covered by the Standard Unity Asset Store EULA. The coffin, body bag, and bottles come from loafbrr's [Halloween Props](https://opengameart.org/content/halloween-props), and the matching duel table/chairs come from [Wooden Furnitures](https://opengameart.org/content/wooden-furnitures). The CC0 music layers are [Creepy Ambient Loop](https://opengameart.org/content/creepy-ambient-loop), [Abandoned Passages](https://opengameart.org/content/abandoned-passages-horror-ambience-loop), and the spooky waltz from [4 Music Box Tracks](https://opengameart.org/content/4-music-box-tracks).
