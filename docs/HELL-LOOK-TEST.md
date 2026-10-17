# Hell: the look test (branch `hell-look`)

Real frames of the game for choosing the art direction (docs/HELL-ART-PASS.md section 4), 2026-10-17.

## Switches (player or editor play, command line)

| Switch | Values | Default |
|---|---|---|
| `-hellLook` | `A` Inked Inferno, `B` Molten Grit, `C` Black and Ember | none: the scene's own grey-box materials |
| `-hellOutline` | `soulfire-pulse`, `lime`, `soulfire`, `whitehot`, `pulse` (lime), `kit`, `off` | `soulfire-pulse` (the creator's pick, 2026-10-17) |
| `-hellStuds` | `bone`, `stone`, `blue` | `bone` (blue clashed with the cyan-white outline) |
| `-hellLookCapture <dir>` | the stills, then quit | |

With no switches the Hell scene still gets the soulfire pulse outline (an outlines-only ink pass, nothing else restyled) and
bone-white studs; `-hellOutline off` turns that off.

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
