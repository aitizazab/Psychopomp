# Hell descent: the signature effects and feel (lane D)

Built 2026-10-18 (event night 1) on branch `hell-fx`. **Nothing here has been run in Unity yet**: the pure tests and the
Unity-side compile checks pass; the by-eye checks are at the end. Visual only: no sound (the sound lane owns audio), no
outlines or room materials (the look lane owns them).

## How it hangs together

- `Scripts/Fx/HellFx.cs` boots a **rig** on every scene with a player (`HellFxRig`: `HellRewindFx`, `HellStepFx`,
  `HellFeelFx`, `HellWorldFx`) and owns **the screen layer**: one URP `Volume` (priority 50) with a runtime profile
  (`HellFxProfile`: ColorAdjustments, ChromaticAberration, LensDistortion, Vignette). All four run inside URP's one uber
  post pass, so the effects add **no full-screen pass**; every one of them is also in `Assets/Settings/DefaultVolumeProfile`,
  so a build's post-processing variant stripping keeps them. Channels (Time Dilation, the Rewind, its snap, the Shadow
  Step, a kill) are mixed by `ScreenGradeMix`: the volume's weight is the strongest channel, each parameter the
  strength-weighted average of the channels' targets. On top, UI Toolkit overlays on the HUD root (they show with
  post-processing off too): a violet edge, an ink edge, radial speed streaks (a 256 px texture made once, re-dealt
  30 times a second), three tape bars, a flash veil. And the camera's **field-of-view kick**, applied last
  (`DefaultExecutionOrder(32000)`) on top of what the movement camera and the viewmodel wrote, never compounding.
- `Scripts/Fx/HellMotes.cs`: **every world effect is a pooled mote**: 320 spheres + 320 cubes made once at scene start,
  the oldest taken over when full. Opaque primitives with the kit's shared lit/glow materials (the comic-ink way: no
  particle shaders, nothing a build can strip). Shapes: Blob (swells, shrinks), Streak (stretched along its velocity),
  Splat (flat on the ground). **`HellMotes.ClearAll` runs on every `HellRewind.Rewound`.**
- `Scripts/Fx/HellFxPalette.cs`: **every colour and the strengths in one place** (static fields, so the look lane's
  art-direction switch can set them at runtime): `Ink`, `Shadow`, `Rewind`, `Dilation`, `Fire`, `Ember`, `LavaGlow`,
  `Dust`, `Crack`, the glow strengths, `ScreenStrength` (scales every screen treatment) and `Density` (scales mote counts).
  `HellFxPalette.Use(HellFxLook.InkedInferno | MoltenGrit | BlackAndEmber)` sets a direction (A the defaults; B warmer,
  softer grade; C brighter glows and a stronger grade because the stone carries no colour). Hostile lime is absent on
  purpose: it belongs to the enemy outlines.
- Pure parts: `Packages/com.vibejam.jamkit/Runtime/Skyline/HellFxMath.cs` (`GhostTrack`, `HellFxCurves`,
  `EmitAccumulator`, `ScreenGradeMix`).

## The effects

### 1. The Rewind (the Time shot): `HellRewindFx`

| | |
|---|---|
| Trigger | `HellRewind.Rewound` (listens only; `HellRewind.cs` untouched). |
| Recording | 30 times a game second: the player (chest), every live bot (`Center`, 16 slots) and every bolt in flight (24 slots, by pool index) into a `GhostTrack` ring each (6.6 s). Fixed slots, no allocation. Cleared on `HellRunEvents.RoomRestarted` and after each Rewind (the history is gone, as the Rewind's own ring is). |
| The scrub | Each track's last `seconds` (4, 6 with Deeper Rewind) resampled into a 48-point path; the world is **held at 0.06x** (`HellTime` key `rewindfx`) for 0.5 s while cyan ghosts race backwards along the paths with a tapering line behind them and echo blobs every 45 ms: yours runs back to where you now stand, each enemy's back to its restored spot, each bolt flies back into the muzzle that fired it. Screen: saturation -72, hue -12, contrast +28, chroma 1, lens pulled in (-0.45), cyan vignette, a cyan colour filter, speed streaks, rolling tape bars, the view narrowing 8%. A pale cyan flash as it starts. |
| The snap | A bright pop (filter 1.25, lens +0.3, chroma), a cyan-white veil, FOV +7% out and back, shake 0.3, and an afterimage ring on every restored enemy. |
| Rewind safety | Clears the motes, and the kit's own short-lived effects from the abandoned future (death flames, smoke puffs, sparks: switched off, which is how their pools free them; one `FindObjectsByType` sweep per Rewind, never per frame). |
| Tunables | `sampleHz` 30, `scrubSeconds` 0.5, `freeze` on, `freezeScale` 0.06, `tailShare` 0.45, `snapTrauma` 0.3 (inspector on `HellFxRig`); colour `HellFxPalette.Rewind` / `RewindGlow`. |
| Cost | Recording: ~40 vector pushes per 33 ms. Scrub (0.5 s only): up to 41 LineRenderers (48 points) + 41 spheres + ~170 echo motes. Screen: the uber pass already runs; 5 UI elements. |

### 2. The Shadow Step (the Shadow shot): `HellStepFx`, `HellStepMarkerFx`

| | |
|---|---|
| Aim preview | The marker (`HellFx.MakeMarker`, still owned and placed by `HellAbilities`): an ink disc ringed by 10 violet shards that turn (90°/s) and breathe, a thin violet pillar, and **a dark tendril** from the player's hand (eye + right 0.22, down 0.28, forward 0.35) to the landing: a quadratic arc rising 18% of the distance, writhing (two sines, zero at the ends), an ink line (0.09 m) with a violet core (0.025 m). Refused: dull red, slower, calmer. No per-frame allocation (the old `TintMarker` called `GetComponentsInChildren` every aimed frame; now materials change only when validity does). |
| Departure | Trigger `HellAbilities.Stepped(from, to)`. 16 ink shards lift off where you stood (rising), 6 violet sparks. |
| The smear | 6-36 ink blobs along the jump at body height, the start fading first (0.14 s) to the landing last (0.46 s), cut by 4 long violet streaks flying the jump's way. |
| Arrival | 14 flat ink splats thrown out across the ground, 14 ink droplets flung up, a ring of 10 violet sparks. Screen: an ink edge that snaps shut and opens (0.38 s), saturation -35, chroma 0.75, lens +0.22, ink vignette; **FOV +12% in 0.05 s, back over 0.32 s**; shake 0.12. (The abilities' own two smoke bursts still play.) |
| Tunables | `fovKick` 0.12, `fovPunch` 0.05, `fovBack` 0.32, `arrivalTrauma` 0.12; `HellFxPalette.Ink` / `Shadow` / `ShadowGlow`. |
| Cost | ~80 motes for under half a second; 2 LineRenderers (20 points) while aiming. |

### 3. Time Dilation: `HellFx.SetDilation` (called by `HellAbilities` every frame) and `HellFeelFx`

| | |
|---|---|
| Look | Blue-violet: saturation -45, contrast +12, chroma 0.25, a violet-tinted colour filter, a violet vignette (0.4), the violet screen edge (UI), all by the dilation's strength (its ease in and out). |
| Movers | Every 35 ms (real time) each bolt in flight drops a blue-violet afterimage blob (0.32 s): bolts draw dotted trails. Violet speed streaks at the screen edge, by your speed (from 6 to 14 m/s) times the strength. Embers, flames and motes run on game time, so they visibly slow. |
| Tunables | `afterimageEvery` 0.035, `afterimageLife` 0.32, `speedLinesFrom` 6, `speedLinesFull` 14; `HellFxPalette.Dilation` / `DilationGlow`. |
| Cost | One blob per bolt per 35 ms (a fight has under 10 bolts). |

### 4. Game feel: `HellFeelFx`, `HellWorldFx`, `HellFx`

| Effect | Trigger | What | Tunables |
|---|---|---|---|
| Hit feedback | the player's `HitLanded` | a spit of 4 ember streaks off the hit point along the normal (the bots' white flash and the guns' hit stop are the kit's already) | palette `Ember` |
| Kill burst | `SkylineBot.Killed` | a ring of 12 fire blobs, a fountain of 14 embers, a warm screen pop (gun kills), shake 0.14 by distance (the gun's kill hit stop, 0.08-0.1 s, is the kit's) | `killTrauma` |
| Camera shake | landings, kills, Combustion, falling platforms, the Rewind's snap, the step's arrival | through the player's own `ScreenShake`; hard landings from 9 m/s of fall (0.38 at 22 m/s) with a dust ring; `ShakeAt` falls off with distance | `landingFrom/Full/Max`; **the Look page's "Screen shake" slider** (0-100%, saved as `shake` in the options) scales `maxOffset` / `maxRoll` |
| Combustion | `HellFx.FireBlast` | the glow ball plus a fire ring, 18 embers, smoke, shake 0.35 within 4 m to 0 at 22 m | |
| Nightfall | `HellFx.NightfallSphere` | the ink rim now has a violet ember every fifth blob and pops in with an overshoot; an ink ring thrown out at the cast. Its sphere is **not** cleared by a Rewind, because the darkness itself isn't rewound (`HellDarkness`). | |
| Lava | rooms' lava basins (found on room entry / run start) | embers rising (0.5 per m² per s, capped 40/s per basin, within 45 m of the camera) and **glow pulses**: hot blobs heaving out of the surface and sinking back (0.06 per m² per s, capped 4/s) | `embersPerSquareMetre`, `maxEmbersPerSecond`, `pulsesPerSquareMetre`, `maxPulsesPerSecond`, `lavaRange`; `Ember`, `LavaGlow` |
| Collapse | each platform's position leaving home while solid (shaking), `Solid` going false (falling) | while shaking: up to 7 cracks open on its top (flat dark splats with a hot spark) and grit trickles from under it; falling: a dust ring, a spray of grit, shake 0.28 by distance | `crackEvery`, `gritEvery`, `fallTrauma`; `Dust`, `Crack` |
| Skill-pick reveal | `HellSkillPick.Open` turning on (finds the `hell-skill-pick` layer by name; `HellSkillPick.cs` untouched) | a warm flash; the layer fades in (0.15 s); the title drops in with its letter spacing closing; the three cards rise 120 px with an overshoot, 0.09 s apart; unscaled (the game is stopped) | `revealSeconds`, `cardDelay`, `cardSeconds` |

## Frame budget

Target 120+ fps on an RTX 3050 laptop. **No heat shimmer, no blur chains, no extra full-screen pass, no opaque-texture
read**: the grade, chroma, lens and vignette ride URP's uber post pass (already running for the ink look's bloom and
tonemapping). Overlays are 8 UI elements, most hidden (`display: none`) when idle. World effects are pooled opaque
primitives sharing a handful of materials (SRP batcher friendly), no shadows. Steady-state cost in a lava room: tens of
motes (embers, pulses). Peak: the Rewind's 0.5 s scrub (about 250 motes + 41 lines). Per-frame allocation: none (rooms are
found on room entry; the kit's effects are swept once per Rewind; the skill pick's layer is found once per pick).

## Hooks added to other lanes' files (one line each, plus the option)

- `Scripts/Bots/SkylineBolt.cs:36`: `public static IReadOnlyList<SkylineBolt> Pool => _pool;` (read-only, for the
  Rewind's bolt ghosts and Time Dilation's afterimages).
- `Scripts/Hud/SkylinePauseMenu.cs:411`: the Look page's "Screen shake" slider.
- `Runtime/Skyline/SkylineOptions.cs`: the `screenShake` option (field, clamp, save as `shake`, load): 5 lines.

## Tests

`core-tests/Skyline` `HellFxTests` (20): the ghost track (ring, interpolation and clamping, search, time going back,
path length), the scrub curve (0 to 1, never backtracks), the envelope, decay, FOV kick (out, back, negative),
landing trauma, falloff, stagger, back-out overshoot, breathe range, the emission accumulator (fractions kept, long frame
capped), the grade mix (one channel reaches its look, two blend by strength, none is off) and the screen-shake option
(round trip, clamp, old saves keep shake on).

## Check in Unity (by eye)

1. **Rewind** (V) in a fight: the world holds for half a second while cyan ghosts run back (yours toward the camera, the
   enemies' to where they now stand, bolts back to the guns), the grade goes cold and drained with streaks and tape bars,
   then the snap. Is 0.5 s at 0.06x too long a hold for play? (`scrubSeconds`, `freezeScale`, or `freeze` off.) Does a
   hit stop during the scrub fight it? (The scrub re-asserts its request every frame.)
2. **Shadow Step**: the tendril reads from the hand to the marker (not clipping the viewmodel); the marker turns; red
   when refused; the arrival's ink edge, chroma pop and FOV kick feel like a punch, not a stutter. Watch a step from a side
   camera for the smear.
3. **The FOV kick never drifts**: after many steps and rewinds the view must come back to the slider's field of view
   exactly (if it creeps, the movement camera isn't resetting the FOV each frame: report it).
4. **Time Dilation**: the blue-violet grade and edge, bolt afterimage trails, speed streaks only when moving fast.
5. **Post-processing on the Hell camera**: the volume needs the camera's Post Processing on (the overlays show anyway).
   If the grade doesn't appear, check `SkylineLook.SetupCameras` turned it on for the Hell scene.
6. **Lava**: embers and glow pulses over every basin; density looks right in all three art directions
   (`HellFxPalette.Use(...)`); frame time with two big basins on screen.
7. **Collapse**: cracks and grit while a platform shakes, dust and a shake when it drops; nothing fires after a Rewind
   restores a fallen platform.
8. **Kills**: fire ring and embers on top of the enemies' own death burst (not too much?); the Screen shake slider at 0
   removes all shake.
9. **Skill pick (F9 in the editor)**: the flash, the title drop, the cards rising one by one with an overshoot, and the
   keyboard focus scale still working afterwards.
10. **Rewind safety**: after a Rewind, no death flames, smoke, sparks or ink from the abandoned future remain.
11. **Frame time**: a perf probe pass in a lava room and during a Rewind (the 1% lows).
12. The new `.cs` files need their `.meta` files made by Unity on first open (none were written by hand).
