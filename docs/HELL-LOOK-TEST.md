# Hell: the look test (branch `hell-look`)

Real frames of the game for choosing the art direction (docs/HELL-ART-PASS.md section 4), 2026-10-17.

## Switches (player or editor play, command line)

| Switch | Values | Default |
|---|---|---|
| `-hellLook` | `A` Inked Inferno, `B` Molten Grit, `C` Black and Ember (one for every floor) | the creator's pick: floor 1 A, floors 2 and 3 B |
| `-hellOutline` | `soulfire-pulse`, `lime`, `soulfire`, `whitehot`, `pulse` (lime), `kit`, `off` | `soulfire-pulse` (the creator's pick, 2026-10-17) |
| `-hellStuds` | `bone`, `stone`, `blue` | `bone` (blue clashed with the cyan-white outline) |
| `-hellLookFloors` | `B,A,C` (one direction per floor; a floor left out takes `-hellLook`) | `A,B,B` |
| `-hellLookCapture <dir>` | the stills (13 per direction), then quit | |

Round 2 (2026-10-18): the lighting engine was fixed and every direction rebuilt on it: see docs/HELL-LIGHTING.md.

**The default (2026-10-18, the creator's pick):** with no switches the game runs floor 1 A (Inked Inferno), floors 2 and 3 B
(Molten Grit), with the soulfire pulse outline and bone studs. `HellLookSwitch.DefaultFloorLooks`; baked into the scene by
`HellLookBuild.BakeLook` (called by `SkylineBuild.BuildHellFromCommandLine` and `HellLookBuild.BuildScene`), and applied at run time
to a scene built without a switch. The pick came with one change: B's floor 2 floor (iron diamond plate, "too industrially
metallic, not tarnished enough for Hell, slightly off the palette") is now `scorch` (Poly Haven `cracked_red_ground`, scorched cracked
ground) and its walkways, covers and trims `corrode` (Poly Haven `rust_coarse_01`, corroded ember-rust iron) instead of the
grey painted `rust` sheet. `-hellOutline off` now only turns the outline off. A drop-in imp model in `Resources/HellModels/`
wins over the look's Quaternius stand-in.

- **Soulfire pulse:** cold pale cyan-white `#C8F4FF`. Idle it breathes at 0.6 Hz, shallow. Alert it sits at 0.3 wind-up.
  Through an attack's telegraph the wind-up rises from 0.5 to 1, at which point it beats at 6 Hz, deep, peaking as it fires.
- **How it works:** `HellLookSwitch` sets the global `_SkHellHostile`. The InkLit mask pass writes each hostile renderer's
  wind-up into the mask's blue channel, from `1 - _InkWidth` set per renderer with a property block. The ink composite
  replaces the hostile hue and pulses it. With the global at 0, the kit's look is unchanged.

## Files

- `Packages/com.vibejam.jamkit/Runtime/Skyline/Hell/HellLookOptions.cs` (pure, tested): the switches, hues, pulse numbers.
- `Scripts/Hell/Look/HellLookSwitch.cs`: applies the look at run time (by the builder's material names), the outline,
  the studs and the wind-up feed.
- `Scripts/Hell/Look/HellLookCapture.cs`: the stills.
- `Editor/HellLookBuild.cs`: normal-map import, the Lit templates, the imp prefab, and the scene and player builds.
- `Look/Ink/InkLit.shader`, `InkComposite.shader`: the override (a few guarded lines each).
- `Resources/HellLook/Textures/`: the CC0 textures for B (Poly Haven, ambientCG; `CREDITS.txt`).
- `Look/HellLook/Models/fire-imp-quaternius.obj`: the Quaternius "Demon" (CC0), converted from GLB.
- `HellBodies.Build` asks `HellLookSwitch.ModelOverride` first, so the Demon is the imp only under a look.
