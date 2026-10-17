# PSYCHOPOMP (Hell): the UI and HUD pass

Branch `hell-ui`, 2026-10-18 (night). The creator asked: "what does our UI currently look like? The same as Skyline? The
plating will probably have to change, and we'll need an ability wheel or something." This file is the answer (the
inventory), the redesign, and what was built. **Nothing here has run in Unity yet**: the pure tests (`HellHudTests`, 40)
and the Unity-side compile check are green; the stills to check it by eye are in "Checking it" at the end.

## 1. The inventory (before this pass)

Yes: in play it was Skyline's HUD with Hell bits bolted on, each lane placing its own pieces by hand. Fonts were Skyline's
three (Anton, Luckiest Guy, Bangers) plus the project default.

| State | Element | Where | Shows | Font | Skyline-specific / wrong for Hell |
|---|---|---|---|---|---|
| **Title** | Title word | centre | "HELL DESCENT" | Anton 168 | old working title; not the creator's face |
| | Menu | centre | START / CHAPTERS / SETTINGS / QUIT, chapter list, the errands question | Anton | |
| | Hint line | bottom | "arrows / W S ..." | project | |
| **Opening** | Text cards | centre | speaker + 6 placeholder cards over the ember gradient | Anton speaker, project body | |
| **Floor intro** | Floor card | centre | FLOOR ONE / THE EMBER GATE / tagline | Anton | |
| **Play** | Crosshair, hit marker, hit-from wedge | centre | dot, coloured ticks, red wedge at 120 px | | |
| | Health bar | bottom left | Skyline's chassis (orange) + **plating (light blue slab)**, shards on a break | | **plating is mall-robot armour; light blue sits next to the enemies' soulfire cyan** |
| | Ward gained / broken flashes | screen edges, full screen | light-blue edge flash, pale blue veil | | cyan-ish again |
| | Ammo | bottom right | digits, gun silhouette, **"WRIT" / "BREACHER"** name row | Luckiest Guy | gun names are Skyline's (the guns still are; creator's call); upper-cased the name every frame (garbage) |
| | Hell cooldowns | above the ammo | Rewind cyan bar + chevron, Step violet pips, Nightfall bar; no keys shown | | Rewind in **cyan** (the enemies' colour); `GetComponent` in every draw; repainted every frame |
| | Objective strip | top centre (26 px) | in Hold the Brand: **an arcade-cabinet glyph** in a job-sheet slot; the same glyph on the ring | | **a Skyline mall asset glyph in Hell** |
| | Brand ring | round the crosshair | violet meter, rate halo | | glyph above |
| | Objective line | top centre, 104 px | "SIGIL A  1 / 6     1:23" (clock glued on) | Luckiest Guy | built a new string every frame (the clock) |
| | Banner | 30% | floor names, AGAIN | Luckiest Guy | |
| | Wave clock | top right | (never bound in Hell) | | Skyline only |
| | Comic lettering | near enemies | POW!, KZZT!, OBJECTION! | Luckiest Guy + Bangers | **robot words on Hell enemies** (an imp dies "KZZT!", the scythe demon "FZZT-POP!"): see "Not done" |
| | Hurt vignette, low red edge | screen | | | fine |
| | Subtitles | bottom centre, 70% wide | speaker prefix + line | project | BAJA / VESPER had no colour (white) |
| **Job** | Tracker | left, 130 px | "FAVOUR: The Viscount ||||.... 2/3 DONE" (a text bar) | Luckiest Guy | built strings every frame |
| | Briefing / result card | left, 170 px | BAJA, her line, the task | Luckiest Guy + project | |
| | Door marker | in the world | "JOB 23 m" | Luckiest Guy | a new string every frame |
| **Beat** | Letterbox | top/bottom 11% | | | |
| **Tutorial** | Hint card | 60% down | "HOLD [E] TO AIM ..." | Luckiest Guy | **collided with the decision prompt (58%)** |
| **Skill pick** | Overlay | full | FLOOR N CLEARED, three cards | Luckiest Guy / project | Time cards in cyan |
| **Boss** | Vesper's bar | top, 150 px | name, ember bar, notches, hint | Luckiest Guy | **her bar in ember (your colour)**; under the old objective line |
| | Banners | 32% | VESPER, UNDO, NIGHTFALL, SHE KNEELS | Luckiest Guy | a different height from the other banners |
| **Decision** | Prompt | 58% | HOLD [F] OFFER YOUR HAND ... | Luckiest Guy | over the hint card |
| | Hold ring | crosshair, r 46 | | | |
| | Escape line | top, 96 px | | Luckiest Guy | **8 px from the run's objective line, which was never cleared after the shaft ("DOWN")** |
| **Ending cards / credits / stats** | screens | centre | cards, credits, big numbers | Anton, Luckiest Guy, project | old title; small print in Anton/project |
| **Pause menu** | panel | centre | PAUSED, Audio / Controls / Look / Subtitles | project bold | Skyline's sample subtitle "Clamped! Hold still-ish." (DOT); no tutorial row; no wheel binding |

### The top problems

1. **The health bar was Skyline's plating bar**, in a light blue that reads as the enemies' soulfire cyan.
2. **Cyan was everywhere**: the Rewind bar, the Time skill cards, the plating flashes. The language says cyan = enemies only.
3. **Skyline leftovers on screen**: an arcade-cabinet glyph for the Brand, robot comic words on imps and demons, DOT's
   sample subtitle in the pause menu.
4. **Collisions**: the hint card (60%) under the decision prompt (58%); the ending's line (96 px) on the run's objective
   line (104 px), which also kept saying "DOWN" through the boss and the ending; the boss bar under the old line.
5. **No way to see or bind the abilities** (no wheel), no key labels, nothing for Baja's hour.
6. **Garbage every frame** from the HUD's texts (the objective line with its clock, the job tracker and marker, the ammo
   name).

## 2. The redesign

### The language

Dark obsidian plates with ember rims and ink keylines (the Skyline slant kept). One hue per meaning, in
`HellHudStyle` (`Scripts/Hud/HellHudStyle.cs`):

| Hue | Means |
|---|---|
| **Ember** (#FF6A1F, hot core #FFD27A) | you: health, fire, the low-health throb |
| **Bone** (#ECDFC6) | the Ward (the outer health layer) |
| **Violet** (#9B6BFF) | Shadow (Shadow Step, Nightfall), objectives, favours |
| **Sand** (#F2C86B) | Time: Rewind, Time Dilation, Baja's hour, her subtitle name |
| **Soulfire cyan** (#5FE3F0) | **the enemies only**: outlines, Vesper's bar |

The Rewind's *world* effect (`HellFxPalette.Rewind`) is still cyan; that belongs to the FX lane. If it should follow the
HUD to sand, change `HellFxPalette.Rewind` (one line).

### The faces (the creator's picks)

| Role | Face | Used on |
|---|---|---|
| `HellFaces.Title` | **Cinzel Decorative Black** | the title screen word, the credits' first line. Nothing else |
| `HellFaces.Heading` | **Metal Mania** | the opening cards, banners, the objective line, job card titles and tasks, the tracker's job name, ability names (wheel, pick cards), the decision prompt and escape line, Vesper's name, menu items, screen headings, the pause menu's title |
| `HellFaces.Fine` | **Grenze Gotisch SemiBold (replaced Grenze Gotisch, 2026-10-18)** | settings rows and notes, credits small print, stat labels, screen hints, key labels under the ability tiles, the wheel's state and description, skill descriptions |
| `HellFaces.Numbers` | **Luckiest Guy** (kept) | ammo, clocks, stats, skill glyphs, the tutorial hint cards, Vesper's hint, the job marker and count |
| `HellFaces.Comic` | **Bangers** (kept, with Luckiest Guy) | the comic lettering, unchanged |
| (project face) | | story prose: subtitles, Baja's job line (read, not glanced at) |

The working title is **PSYCHOPOMP**, written once: `HellFlowText.Title`. Under it, at its lower right, a **Minecraft-style splash line**: tilted, yellow, Luckiest Guy, pulsing gently, picked at random each time the title shows from `HellFlowText.Splashes` (20 lines, ALL PLACEHOLDER for the creator to edit, e.g. "To Hell and Back Before Lunch!").

### Health: the ember vessel (the damage model kept)

The code's damage model is the kit's and stays: **plating over a chassis**, the plating takes hits first and comes back only
from fresh kills (walk over the wreck: `PlayerArmor.Strip`), the chassis never regenerates (Mended, the job boon, refills
both). So Hell keeps two layers, re-skinned (`HellHudVessel`, drawn into the same element; no text):

- **The vessel** (health): a slanted obsidian plate with an ember rim, full of molten ember. As it drains, **dark cracks
  open across the emptied part**, one per sixth lost (`HellVesselModel.Cracks`, up to five). A **heart-gem** at its left
  end glows with the ember and **throbs under 35%** with the screen's red edge.
- **The Ward** (the plating): a thin **bone** slab over the vessel; it **shatters** into shards when it breaks; the
  screen edges flash bone when a kill refills it.

### The abilities (bottom right, above the ammo)

`HellHud` (rewritten). A row of tiles, right to left, each with its **control for the device in use** under it (Grenze Gotisch, e.g.
"RIGHT MOUSE" / "LT", "V" / "LB", "E" / "RB", "G" / "D-PAD DOWN"; `HellInputDevice`):

- **Time Dilation**: a sand dial with a little hourglass; it **empties while the world is slowed** (`TimeDilationState.HoldLeft01`), full when ready (it has no cooldown).
- **Rewind**: a sand dial with a back-chevron, **filling as it cools down**.
- **Shadow Step**: **one violet diamond per charge**, the recharging one filling from the bottom; grey while blocked (the lantern).
- **Nightfall** (when owned): a violet dial.
- Above the tiles, **a chip per floor skill you own** (its glyph in its flavour's hue).

A tile coming ready flashes white. It repaints only when a value it draws changes.

### The ability wheel

`HellAbilityWheel` (`Scripts/Hud/HellAbilityWheel.cs`), added by `HellHud`.

- **Hold the Wheel binding: Tab / D-pad left** (a new `BindAction.Wheel`, on the Controls page; D-pad left was free, and
  the right stick is free to point while the left thumb holds it).
- Opens a radial: **the three gifts first (Shadow Step at twelve o'clock, Rewind, Time Dilation), then every floor skill you
  own**, each slot a dial of its readiness; the middle shows the pointed slot's **name** (Metal Mania), its **state**
  (`1 / 2`, `READY`, `8 s`, `ACTIVE`, `PASSIVE`, `ON THE SKILL BUTTON`) and **what it does** (Grenze Gotisch).
- **Time slows to 0.2x** while it is open (`HellTime` key `wheel`, the Time theme). The look holds still and the gun, Shadow
  Step and Rewind are quiet (`SkylineDeviceInput.LookHeld`); moving and jumping go on. It won't open while aiming a step,
  in the pause menu or the pick, or when wrecked.
- **Pointing:** the mouse moves a virtual pointer (clamped, with a dead zone in the middle) or the right stick points
  directly; Q / E step round. Moving back to the middle keeps the last slot, so a stick springing back still picks.
- **What it selects (decided):** the gifts and the passive skills have their own buttons or none, so for them the wheel
  **only shows**. Releasing on an **active** skill **binds it to the Skill button** (`HellAbilityWheel.Binding`,
  pure `HellSkillBinding`). Nightfall is today's only active skill and binds itself when granted, so in practice the wheel is
  the "what do I have and what does it do" screen; the binding is ready for the next active skill
  (`HellAbilityWheel.ActiveSkills`).
- **Replays** record the wheel held (`HellReplayButtons.WheelHeld`), because its slow-down changes the simulation; the
  pointer is not recorded (the binding only matters with two active skills).
- Banners, the hint card and the decision prompt step aside while it is open.

### The layout (one place, tested)

Every element's box is in `HellHudPlaces` (pure, `Runtime/Skyline/Hell/HellHudPlaces.cs`); the HUD code places itself from
it (`HellHudStyle.Place`). Reference 1920 x 1080 panel units, scaled with the screen (1080p and 1440p are the same panel):

```
 [FAVOURS 2/3]              [  OBJECTIVE LINE  ]                [hour]
 [job tracker ====]              [ 1:23 ]                       [glass]
 [briefing card   ]        [ VESPER, THE EVENING STAR ]         [ m:ss]
 [ ...            ]        [ ===========|=====|====== ]
                           [        boss hint         ]
                    [        FLOOR ONE (banner, 30%)       ]
                                 ( rings )
                                    +            <- crosshair, hit marker, Brand ring, decision ring
                    [ HOLD [F] OFFER YOUR HAND (prompt, 60%) ]
                    [   HOLD [E] TO AIM (hint card, 65.5%)   ]
                                                     [skill chips  ]
                    [ subtitles, 50% wide, 132 up ]  [ ~  <<  <>  ( )]
 [<> vessel ======/]                                 [ RMB V  E  G ]
 [  ward ~~~~~~~~~/]                                 [  12  WRIT   ]
```

The tests (`HellHudTests.NothingThatShowsTogetherOverlaps`) resolve every box in every state (play, job, beat hold, boss,
decision, escape, wheel) at 1080p, 1440p, 4K, 720p, 16:10 and two 21:9 sizes, and fail on any overlap or anything off
screen. The ending's escape line now uses the objective slot, and the run's line is cleared outside play, so they never
show together. The objective line and the room clock are separate (the clock's text comes from a cache).

### Baja's hour (for the run-clock lane)

The top-right box is reserved. Call, from anywhere, any time (before the HUD exists too):

```csharp
HellHud.SetHourglass(float remaining01, float secondsLeft = -1f, bool urgent = false);  // remaining01 < 0 hides it
HellHud.HideHourglass();
HellHud.HourglassSlot          // the VisualElement (null until built): add your own visual to it...
HellHud.DrawDefaultHourglass   // ...and set this false to drop the placeholder hourglass
```

Until then a plain hourglass draws there (bone glass, sand falling, "m:ss" in Luckiest Guy, ember throb when `urgent`).
It sits under the HUD's root, so the clean picture hides it.

### Smaller things

- The Brand's objective shows a **brand sigil** on the ring and markers; the Skyline job strip is hidden in Hell.
- **Vesper's bar in soulfire cyan** (an enemy), her name and banners in Metal Mania; all banners share one height.
- Subtitle colours: **BAJA sand, VESPER violet**.
- Skill pick: Metal Mania names, Grenze Gotisch descriptions, obsidian cards with ember rims, Time in sand.
- Pause menu (Hell scenes): Metal Mania title, Grenze Gotisch rows, an ember top edge; Subtitles page gains **Tutorial hints
  On/Off** and **Show the tutorial hints again**; the sample subtitle is neutral; Controls lists **Ability wheel (hold)**.
- No garbage a frame from the HUD's texts: clock strings cached (`HellClockText`), the job tracker/count/marker cached by
  value, the ammo name upper-cased once per gun, the objective line re-read 5 times a second.
- The Hell look is on only in play in scenes not named `Skyline*` (`HellHudStyle.Active`; `Override` forces it), so the
  kit's EditMode HUD tests keep checking Skyline's look.

## 3. Files

| File | What |
|---|---|
| `Packages/.../Runtime/Skyline/Hell/HellHudPlaces.cs` | `HellFaces`, `HellHudPlaces` (the boxes, the states, `Problems`), `HellClockText`, `HellVesselModel` (pure) |
| `Packages/.../Runtime/Skyline/Hell/HellWheel.cs` | `HellWheel` (pick maths), `HellWheelSelector`, `HellSkillBinding`, `HellWheelSlots`, `HellWheelText` (pure) |
| `Packages/.../Tests/Skyline/Hell/HellHudTests.cs` | 40 tests |
| `Scripts/Hud/HellHudStyle.cs` | the palette, `Active`, `Place`, painting helpers, the brand sigil |
| `Scripts/Hud/HellHudVessel.cs` | the ember vessel and the Ward |
| `Scripts/Hud/HellHud.cs` | the ability tiles, skill chips, the hourglass slot |
| `Scripts/Hud/HellAbilityWheel.cs` | the wheel |
| `Scripts/Hud/HellInputDevice.cs` | the device in use, short control labels (cached) |
| `Scripts/Hud/HellUiShots.cs` | `-hell-ui-shots <dir>`: the stills below |
| `ThirdParty/Fonts/Resources/SkylineFonts/{CinzelDecorative,MetalMania,GrenzeGotisch}/` | the fonts with their OFL.txt; `ThirdParty/Fonts/CREDITS.md` |
| Edited | `SkylineHudHealth` (the vessel hook), `SkylineHudObjective` (sigil, no strip), `SkylineHudSubtitles` (Hell width and height), `SkylineHudFlashes`, `SkylineHud` (bone flashes), `SkylineHudAmmo` (no garbage), `SkylineHudStyle` (faces), `SkylinePauseMenu`, `SkylineDeviceInput` (`LookHeld`), `HellAbilities` (no step/rewind under the wheel), `HellSkills` (Nightfall obeys the binding), `HellHudLine` (faces, places, the clock), `HellRunDirector` (line/clock split, cleared outside play), `HellJobDirector`, `HellLinePlayer`, `HellEndingHud`, `HellVesperHud`, `HellScreens`, `HellEndingCards`, `HellSkillPick`, `HellFlowText` (PSYCHOPOMP, font credits), `HudModels` (speaker colours), `BindingTable` (`Wheel`), the replay buttons |

Unity makes the `.meta` files for the new scripts, fonts and font folders on import (none were written by hand): commit them.

## 4. Checking it (the main session)

`$U` = `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe`, from the HellDescent folder, after the merge and
an import (fonts need their metas). A build (`out\hell\build\HellDescent.exe`) or the editor's Play both work.

```
# the HUD states (play, job card, low health, the wheel, boss, decision, beat hold, clean picture, skill pick)
& $exe -hellSkipTitle -hell-ui-shots out/hell/ui/hud -screen-width 2560 -screen-height 1440 -screen-fullscreen 0
# the screens (title, opening card, floor card, credits, stats, pause menu)
& $exe -hell-ui-shots out/hell/ui/screens -screen-width 2560 -screen-height 1440 -screen-fullscreen 0
# the same at 1080p, to compare sizes
& $exe -hellSkipTitle -hell-ui-shots out/hell/ui/hud-1080 -screen-width 1920 -screen-height 1080 -screen-fullscreen 0
```

Each writes `<name>.png` into the folder and logs `HELL_UI_SHOT <name> <path>`, then quits (about 15 s). What to look for:

1. **Fonts load** (no `SKYLINE_HUD font missing` in the log): PSYCHOPOMP in Cinzel Decorative on `title.png`; Metal Mania on
   the objective line, banners, job card title, wheel names, decision prompt; Grenze Gotisch on the key labels and pause menu rows.
2. `play.png`: the vessel (ember, Ward over it), the tiles with key labels, three skill chips, the hourglass top right,
   the objective line with the clock under it, nothing overlapping.
3. `low-health.png`: cracks across the drained vessel, the gem bright, no Ward.
4. `wheel.png`: six slots (three gifts, Nightfall, Ambush, Deeper Rewind), slot 3 (Nightfall) pointed at with its name,
   state and description in the middle; the world slowed.
5. `boss.png`: Vesper's cyan bar under the clock; `decision.png`: prompt under the ring, the hint card under the prompt.
6. In play, by hand: hold **Tab**, move the mouse round, release on Nightfall; on a pad hold **D-pad left** and point the
   right stick. `HELL_WHEEL open` in the log. Check time slows and the view doesn't turn.
7. The clean picture (`-hell-clean`) hides all of it, the wheel and the hourglass included.
8. The Profiler in play with the HUD up: no GC alloc from `HellHud`, `HellJobDirector`, `HellRunDirector`, `SkylineHudAmmo`.

## 5. Not done

- **Comic words** are Skyline's per bot kind (`ComicLettering`: KZZT! on a fire imp, FZZT-POP! on the scythe demon,
  OBJECTION! on the Writ's spot). A Hell set (imp: FWOOSH! / POP!, demon: SHRAK! / KRUNCH!, ooze: SPLORCH! kept) is a data
  change there and in `ComicLetteringTests`; left for the creator to approve since they are words on screen.
- **Gun names** "WRIT" / "BREACHER" under the ammo are the kit's guns' names: the creator's call.
- The opening cards are Metal Mania (the creator, 2026-10-18); subtitles and Baja's job line keep the project face (prose).
  it is one constant per screen.
- A tutorial hint for the wheel ("HOLD {Wheel} TO SEE YOUR POWERS") belongs in `HellLines` (the creator's words).
- The Rewind's world effect is still cyan (`HellFxPalette.Rewind`, FX lane).
- `HellTutorial` and `HellEndingDirector` still track the device themselves; they could use `HellInputDevice`.
