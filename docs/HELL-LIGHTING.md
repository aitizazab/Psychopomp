# Hell: the lighting engine (investigation and revamp)

2026-10-18, after the creator's verdict on the first look-test stills ("the lighting is all wrong ... pathetic as it is").
Owner: the look lane. Files: `Scripts/Hell/Look/` (`HellLookSwitch`, `HellLightRig`, `HellBeyond`, `HellLookPalette`,
`HellLookTextures`, `HellLookCapture`), `Look/HellLook/Shaders/` (`HellSurface`, `HellSky`, `HellHaze`), `Look/Ink/InkLit.shader`
(one guarded block), `Editor/HellLookBuild.cs`.

## 1. Findings: what was wrong

Proof lines are from the player log (`HELL_LIGHT ...`), the code, and the stills of 2026-10-17
(`FableAnimationTests/art/review/2026-10-17/hell-look/`).

| # | Question | Finding | Proof |
|---|---|---|---|
| 1 | Which shaders do rooms and enemies use? | Builder: URP **Lit** (`SK_Hell_*`, colour only, no textures) and URP **Unlit** glows. Under A and C the switch swapped the scenery to **InkLit** in flat palette colours (no texture at all); under B to URP Lit with photo textures **box-UV'd per renderer, one texture per material** (walls and floors of a floor looked alike; the outer walls got the dark basalt and stayed black). Enemies: InkLit (hostile role) in flat colours. | `HellLookSwitch.InkScenery` (old), `SkylineInkRoles.Apply` in `HellBodies.Build` |
| 2 | Do realtime lights affect them? | Yes in principle: PC_Renderer is **Forward+** (`m_RenderingMode: 2`), InkLit has the cluster loop. But see 3 and 4: the lights reached almost nothing. | `PC_Renderer.asset`, `InkLit.shader` ForwardLit |
| 3 | Were the lava lights placed? | Yes, and **badly**: one per 12 m at `LavaSurface + 0.8 = -0.4 m`, i.e. **0.4 m below the walking floor (y 0), inside the basin**. Every floor's top face, every walkway and the player's view of them face *away* from the light (N.L = 0): the floors were never lit by the lava. The walls they could reach are the basin's own inner sides. | `HellRoomLayouts.Done()`: `V(..., LavaSurface + 0.8f, ...)` |
| 4 | How far did they reach? | Builder intensity 2.2 (A 4.5, B 3.4, C 3.2), range 9. URP point lights fall off with the inverse square: 4.5 / d^2 is 0.05 at 9.5 m but already **0.18 at 5 m and 0.07 at 8 m**: a faint glow 1-3 m round each light. Worse, InkLit's banded pools thresholded the *distance* attenuation alone (`a = atten * N.L`, bands at 0.03 and 0.25), **ignoring intensity**: a pool was at most 5.7 m (first band) and 2 m (second) across whatever the light's strength. | `InkLit.shader` `InkAdditionalLight` (old) |
| 5 | URP asset | `PC_RPAsset` (quality "PC", the player's level): HDR on, additional lights per pixel, per-object limit 4 (**ignored by Forward+**), additional-light shadows supported (atlas 2048, tiers 256/512/1024), main-light shadows 2048, shadow distance 50, 4 cascades, soft shadows, light cookies on, SSAO renderer feature present. Nothing in it was wrong; the scene used none of it (every light `shadows = None` except the 0.35 directional). | `HELL_LIGHT pipeline ...` log line; `PC_RPAsset.asset` |
| 6 | Post-processing on the camera | On (`PrepareCamera` sets `renderPostProcessing`, HDR). Fine. | `HellLookSwitch.PrepareCamera` |
| 7 | Ambient | **Flat**, one colour per floor (A 0.20/0.15/0.13 warm grey). Flat ambient lights every face the same, so wall and floor of the same flat colour came out the **same value**: "hard to distinguish between wall and floor". C's 0.05 flat ambient plus lights that reached 2 m = unnavigable. | `HellLookSwitch.Atmosphere` (old) |
| 8 | Sky / beyond the bounds | **None**: `RenderSettings.skybox = null`, cameras clear to a **solid colour** (the fog colour), rooms are open-topped 8 m walls. Above and beyond the walls there was a flat brown void. | `SkylineArenaBuilder.HellLighting`, `BuildHell` cameras |
| 9 | Fog | Linear 35-200 m (A), exp2 0.011 (B), linear 10-95 m black (C). C's fog ended at 95 m in near-black: with almost no light, the room was black past 20 m. | old `Atmosphere` |
| 10 | The purple beams and black discs | The **Six Sigils** objective markers: each sigil is a 1.2 m pad (`Sigil`) plus a 0.3 x 9 m violet post (`Sigil Beam`) so you can find it from afar. The pad's "not yet lit" colour was `(0.35, 0.12, 0.45)`, unlit, below 0.5: under fog and tonemapping it read as a **black disc**; the post was the same dim violet, solid, ending abruptly at 9 m ("not all the way" up). They're not studs. | `SkylineArenaBuilder.Hell` "Sigil Beam", `HellRoom` `_unlit` |
| 11 | Why no blue/white studs | They exist, every 8 m on the safe path, but each is a **24 cm square, 6 cm tall**: 1-3 pixels at 15 m in a 2560 px frame. The bone-white ones are visible in the old B floor-1 still as tiny white dots on the walkways. | builder `Box(guides, "Guide", p ± 0.12, height 0.06)` |

Proof lines from the rebuilt player (`out/hell-look/logs/capture-*.log`):

```
HELL_LIGHT pipeline PC_RPAsset: quality PC, HDR True, additional lights PerPixel (per object limit 4), additional shadows True, shadow distance 50, main shadows True
HELL_LIGHT rig: 634 lava lights (replacing 357 builder lights), 332 braziers, 58 heat hazes
HELL_LIGHT beyond: sky True, 468 spires, 72 chains
HELL_LIGHT ambient (floor 1): up (0.006, 0.005, 0.006) side (0.011, 0.004, 0.004) down (0.028, 0.006, 0.003)   (B; down > up: the lava glow from below)
```

## 2. The revamp (what changed)

All three directions now run on the same engine, so they compare fairly; the direction only changes the palette, the
lighting model (bands or PBR) and the post. Every number is in `HellLookPalette.cs` (per direction, per floor).

- **Light rig** (`HellLightRig`): the builder's lava lights are replaced by lights **1.6 m above every lava surface**, one per
  9 m, intensity 15-25, range 14 (cut a metre short of every dark zone's pall, so dark zones stay dark). **Braziers** on
  every tall outer wall's inner face every 15 m (iron bowl and bracket, a flame, a halo in the air, a light of intensity 9,
  range 13) graze the textured walls. Every fire **flickers**. The **3 braziers nearest the camera cast hard shadows**, the
  sky glow (directional) casts soft shadows. C adds a dim **carried ember** on the camera (Deep Rock's headlamp).
- **Ambient** as spherical harmonics: a cool-ish sky above, a warm equator, the **lava glow from below**. Proven by
  evaluating the probe (log line above).
- **InkLit** (enemies in every direction, nothing else): with `_SkHellLight.y = 1` a light's banded pool follows its
  intensity, not only its distance.
- **Hell/Surface** (all scenery): world triplanar CC0 textures, the TOP set on floors and ledges, the SIDE set on walls,
  whiteout-blended normal maps, an **edge trim** (a band along wall tops and a curb round every slab), darker towards the
  floor, and **heated veins** (the side texture's deepest crevices glow at the lava line). A: posterized "painted" textures
  with ink bands; B: URP PBR; C: desaturated near-black, PBR. Sets: floor 1 cliff walls / burned ash floors / volcanic tile
  walkways; floor 2 dark brick walls / scorched cracked floors / corroded walkways (was iron plate and painted rust until the creator's note); floor 3 black glassy rock / glossy basalt. New CC0
  downloads (Poly Haven 1k): cliff_side, dark_brick_wall, metal_plate, dark_rock_02 (`Resources/HellLook/Textures/CREDITS.txt`).
- **Beyond the bounds** (`HellBeyond`, `Hell/Sky`): a procedural skybox per floor (smoke ceiling lit from below, horizon
  fire glow, two ranges of jagged crags, eruption columns, embers), the cameras clear to it, and round each room 26 rock
  spires (20-110 m) and 4 giant chains slung across the sky, one combined mesh each per room.
- **Air**: a heat-haze curtain rising 4 m off every lava basin (`Hell/Haze`), halos round the braziers (`Hell/Halo`).
- **Enemies**: re-dressed on spawn: a charred hide texture, glowing ember cracks on the dark parts (procedural Voronoi,
  `HellLookTextures`), the ooze's body molten (scrolling cel lava). The imp model (Quaternius Demon) got UVs at prefab build.
- **Studs**: 60 cm bone diamonds, raised, glowing. **Sigils**: pads glow violet when pending (`HellRoom` and builder colour),
  the posts are light pillars fading upward over 14 m.
- **Per-floor directions**: `-hellLookFloors B,A,C` (tested in `HellLookOptionsTests.ADirectionPerFloor`); one post volume
  per direction, weighted by the camera's floor; fog, ambient, sun and sky switch per floor.

## 3. The reference checklist (each reference's strength, and our proof)

| Reference strength | Feature in our engine | Proof (still on the page) | State |
|---|---|---|---|
| DRG: dark rock lit by its sources, coloured pools on walls | lava lights above the lava, wall braziers, flicker | wall-floor-close, floorN-eye | done |
| DRG: depth from light falloff, dark between lights | low ambient, 10-14 m pools, palls clear of lights | floor1-eye, dark-zone | done |
| DRG / DOOM: fog catching the light | heat haze, brazier halos, exp2 fog, hazy far spires | floorN-wide, floorN-sky | partial (no volumetric shafts) |
| DOOM: textured rock; layered silhouettes beyond | triplanar CC0 + normals; crags, spires, chains | wall-floor-close, floorN-sky | done |
| Wall vs floor contrast (the creator's note) | different sets and tints, trims, darker wall foot | floorN-wide | done |
| Ultrakill: one colour flood per floor | per-floor fog, ambient, sky (orange, red, violet) | floor1/2/3-wide | done |
| Hades: banded lava with a bright shore | cel lava + shore rim (A, C) | A floor2-wide | done |
| Devil Daggers / DOOM: cores the hottest pixels, readable on fire | white-hot cores, soulfire outline, dark charred bodies | enemies-close, enemies-at-range | done |
| DOOM / DRG: enemies textured with glowing parts | hide + ember cracks, molten ooze | enemies-close | done (stand-in shapes remain) |
| Metal: Hellsinger: warm key against a cool fill | cool-ish sky ambient vs fire | floorN-eye | partial (subtle) |
| Shadows from key lights | sun soft shadows + nearest 3 braziers | wall-floor-close | done |

## 4. Frame times (1440p windowed, `-hell-perf` fly-through with enemies, 3 rooms, RTX 3050 laptop, other programs open)

| Direction | avg ms | 1% low ms | worst room |
|---|---|---|---|
| A | 6.43 (155 fps) | 27.9 | 1.SixSigils |
| B | 7.38 (135 fps) | 32.5 | 3.Collapse |
| C | 6.19 (162 fps) | 21.1 | 2.BreakChains |

Averages clear 120 fps; 1% lows do not (20-50 ms spikes, also at room entry). The 2026-10-15 ink measurement before this
work was 6.6 ms / 30 ms 1% lows, so the spikes are not new: hitches (spawns, shader warm-up, a lagging PC), not steady load.

## 5. Follow-ups

- Chase the 1% lows (shader variant warm-up via a ShaderVariantCollection, spawn allocations).
- The horizon band under the crags still reads as a stripe in some shots; the far crags could become geometry.
- No volumetric light shafts (URP has none built in); halos and haze stand in.
- C's carried ember is dim: the floor at your feet reads but is dark; tune `carryLight` after the pick.
- The scythe demon and ooze are still primitive stand-ins (now textured and cracked).
- Done 2026-10-18: the creator's pick (A, B, B) is the game's default (docs/HELL-LOOK-TEST.md); floor 2's floor re-textured.

## How to run

- Build: `Unity -batchmode -projectPath . -executeMethod JamKit.SkylineRepo.EditorTools.HellLookBuild.PlayerFromCommandLine -skylineOut out/hell-look`
- Stills: `out/hell-look/build/HellDescent.exe -hellLook B -hellLookCapture <dir>` (13 shots into `<dir>/B/`), or
  `-hellLookFloors B,A,C` (into `<dir>/floors-BAC/`).
- Frame times: add `-hell-perf out/hell-look/perf-B -hell-perf-rooms 1.SixSigils,2.BreakChains,3.Collapse`.
- Review page: `FableAnimationTests/art/review/2026-10-18/hell-look-v2.html`.
