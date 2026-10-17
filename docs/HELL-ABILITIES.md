# Hell descent: the player's abilities

Built 2026-10-17 (event day 1) on branch `hell-abilities`, from `FableAnimationTests/event/HELL-DESIGN.md` ("The player's
abilities") and the creator's skill picks. Nothing here has been run in Unity yet: pure tests and the Unity-side compile
check pass; the play checks are listed at the end.

## What is built

| Piece | Where | What it does |
|---|---|---|
| Shadow Step | `Scripts/Player/HellAbilities.cs`, planner in `Runtime/Skyline/HellAbilities.cs` (`BlinkPlanner`, `BlinkRules`) | Hold to aim: a marker shows the landing (violet = valid, red = not). Release: you arrive at once with your velocity kept. Landing = ground hit, a ledge top when you aim at a wall (up to 2.2 m above the hit), the foot of a taller wall, or the ground under the end of the aim (up to 6 m down); within 18 m, in sight, the body must fit. **It must be dark** (`HellDarkness`), or lava with Ashwalker. Charges: 1 (2 with Ambush), 6 s each. Dark smoke at both ends. Blocked while `HellAbilities.ShadowStepBlocked`. |
| Rewind | `Scripts/Player/HellRewind.cs` | Every fixed step, a snapshot of every `IHellRewindable` in `HellRewindRegistry.All`, stored as (object, snapshot) pairs in a ring sized for 6 s. Use: restores the snapshot from 4 s ago (6 with Deeper Rewind), calling `Restore` on that frame's own objects (registered or not), then clears the ring. Cooldown 20 s. A 0.3 s desaturated flash. |
| Time Dilation | `HellAbilities.TickDilation`, `TimeDilationState`, `DilationCurve` | Pressing aim down sights slows time for 3 s after the press, by your speed at the press: none below 2.5 m/s, 0.35x from 10.5 m/s (walk 5 = 0.80x, sprint 9.5 = 0.43x, slide 13 = 0.35x). Ease 0.15 s in and out (smoothstep), on real time. The look stays real-time (the player reads the mouse per frame and the stick on unscaled time). A violet screen edge, a pitch hook. |
| One time-scale owner | `Scripts/Player/HellTime.cs` | `HellTime.Request(key, scale)`: the smallest request wins. The player's hit stop now goes through it (`hitstop`), Time Dilation is `dilation`, the pick screen `skillpick`. The pause menu still holds 0 while open. Physics keeps its fixed step (the game logic runs in Update on `deltaTime`; the ring records in game time). |
| Darkness | `Scripts/Player/HellDarkness.cs`, `DarknessField` | `IsDark(p)`: inside an authored zone (`RegisterZone(Bounds)` / `UnregisterZone`) or a dynamic sphere (Nightfall), and not lit (`RegisterLight(pos, radius)`: lava, fire). **A scene with no zones at all counts as dark everywhere unlit** (`FallbackAllDarkWithoutZones`, so the old arena can be played); the first zone registered makes the rule strict. |
| Burn | `Scripts/Bots/HellBurn.cs`, `BurnStatus` | `HellBurn.Apply(bot, seconds, dps)` (default 3 s at 8/s): damage through the bot's own TakeHit in 0.25 s ticks, embers. `KilledWhileBurning` fires when a burning bot is wrecked. For lava and the ooze's lava pools to call. |
| Bot targeting | `Scripts/Player/HellTargeting.cs` | Before the bots think: a live Shade draws them; a bot inside Nightfall's sphere, or looking through it, gets a never-live stand-in target (sees nothing, attacks nothing). |
| Floor skills | `Scripts/Player/HellSkills.cs` | Registry (data + component) and the nine picked skills (below). |
| Pick screen | `Scripts/Hud/HellSkillPick.cs` | On `HellRunEvents.FloorCleared`: time stops, three cards (flavour colour, icon text, name, description) of skills you don't own. Mouse, 1/2/3, A/D or arrows + Enter, pad D-pad + A. Grants the pick, then `HellRunEvents.ContinueAfterPick()`. **Editor: F9 opens a pick.** |
| HUD | `Scripts/Hud/HellHud.cs` | Above the ammo counter, slanted bars with ink keylines, no text: Rewind (cyan bar with a back-chevron), Shadow Step charges (violet pips, the recharging one fills), Nightfall when owned. |
| Effects and sound hooks | `Scripts/Fx/HellFx.cs` | Smoke blobs (opaque, ink style), the marker, Nightfall's dark rim, Combustion's blast, the screen layer (UI veil + a URP colour-adjustments volume). `HellAbilityAudio.Cue(id, pos)` and `PitchChanged(p)`; each cue also plays `hell/<id>` from SkylineSounds when such a sound exists. |

## Inputs (binding table: `BindAction.ShadowStep`, `Rewind`, `Skill`; on the Controls page)

| Action | Keyboard / mouse | Pad |
|---|---|---|
| Shadow Step (hold to aim, release) | E | RB / R1 |
| Rewind | V | LB / L1 |
| Floor skill (Nightfall) | G | D-pad down |
| Time Dilation | the Aim binding (right mouse) | LT / L2 |

## The floor skills (the creator's picks)

| Skill | Flavour | What it does | Component |
|---|---|---|---|
| Umbral Chain | Shadow | After a step, one more step within 1 s is free (it doesn't chain again). | flag (`HellSkillPassive`) |
| Ambush | Shadow | +1 charge; the first shot after a step does 3x damage (`SkylinePlayer.ShotDamageScale`). | `HellSkillAmbush` |
| Shade | Shadow | A step leaves your shadow at the start; bots target it for 4 s; their bolts strike it. | `HellSkillShade` |
| Nightfall | Shadow | Skill button: a dark sphere (6 m radius, 8 s, cooldown 22 s) where you aim, up to 30 m. Bots inside or looking through lose you; you can Shadow Step into it. | `HellSkillNightfall` |
| Deeper Rewind | Time | Rewind goes back 6 s. | `HellSkillDeeperRewind` |
| Momentum Bank | Time | Dilation holds 3 s standing up to 6 s at 13 m/s (`MomentumBank`). | flag |
| Borrowed Time | Time | Each kill: -1.5 s on the Shadow Step recharge, -3 s on the Rewind. | `HellSkillBorrowedTime` |
| Ashwalker | Fire | Step onto lava (needs `HellAbilities.LavaQuery`); then `LavaImmune` is true for 6 s. Passive `LavaHop`: `TryLavaBounce()` bounces you 1.4 m with no damage when jump was pressed within 0.15 s. | flag |
| Combustion | Fire | A bot that dies burning explodes (5 m, 45 damage, sets the others burning: chains, 0.12 s apart). | `HellSkillCombustion` |

## How to add a skill

1. Add a `HellSkillDef` to `HellSkills.All` (id, name, flavour Shadow/Time/Fire, description, icon text).
2. Write its component: `public sealed class HellSkillX : HellSkill` with `OnGranted()` (and `Update` / `OnEnable` as needed).
   A pure flag skill can use `HellSkillPassive` and be read with `abilities.Has(id)`.
3. Pair them in `HellSkills.Components`: `{ "x", typeof(HellSkillX) }`.

Hooks a skill can use: `HellAbilities.Stepped(from, to)`, `StepCharges` (charges, `Shave`, `SetMax`), `blink` (range via
`rangeScale`), `Dilation` / `HellTime.Request`, `HellRewind.Rewound`, `Rewind.TryRewind(free: true)`, `Rewind.seconds` /
`Rewind.Cooldown`, the player's `CueRaised`, `HitLanded`, `AttackTaken`, `WreckIntercept` and `ShotDamageScale`,
`SkylineBot.Killed`, `HellBurn.Apply` / `KilledWhileBurning`, `HellDarkness`, `HellAbilities.SkillPressed()`.

## What rewinds

> **Merged with the enemies branch (2026-10-17):** bots, bolts and the Hell attacks now follow
> `docs/HELL-INTEGRATION.md`; it wins where this list differs.

- **The player:** the whole movement state (`MoveSim.SaveState`: position, velocity, mode, every timer, the capsule
  height), the look, plating and chassis, the boost charge, which gun is out, both guns' magazine and reserve (a reload in
  progress is cancelled), the special-ammo reserve. Ability charges and cooldowns do not rewind.
- **The kit's bots** (`SkylineBot.HellRewind.cs`, a partial file): alive/wrecked, position, facing, awake, the brain's state
  and timers, chassis and plates (shed plates come back on). A bot wrecked since the snapshot is **revived** as a fresh bot of
  the same kind, seed, group and target; the old one and its wreck are deactivated, never destroyed
  (`SkylineBot.HellRevived(old, new)` lets directors swap references). A bot spawned after the snapshot is deactivated
  (`IHellRewindSpawnAware`; the enemies branch's `HellRewindLedger` does this for its own things). Not restored: the drone's
  hover offset, a beam or acid sweep in progress, the gondola's mines.
- **Bolts:** the pool is one rewindable; restore = the snapshot's bolts fly again from where they were.
- **The M1 arena** (encounter phases, markers, the lift), **the Relay** (wave clock, wave count, run clock) and **the
  repossession** (floor clock, unfinished tow meters). Burns don't rewind.
- **Snapshots make no garbage:** each rewindable fills objects from a `HellSnapPool<T>` that turns over more slowly than
  the ring (`MoveSim.SaveState(reuse)`, `BotBrain.SaveState(reuse)`).

## Contracts with the other branches

- `Common/HellRewindable.cs` and `Common/HellRewindRegistry.cs`: exactly as agreed (guids `4be1c9d2...`, `9d3a7e61...`).
- `Common/HellRunEventsStub.cs`: a stub `HellRunEvents` inside `#if !HELL_ROOMS`. **Deleted at the rooms merge (2026-10-17)** (the rooms
  branch owns the real one).
- For the rooms branch: `HellDarkness.RegisterZone/UnregisterZone/RegisterLight`, `HellAbilities.LavaQuery` (set it: is this
  point lava?), `HellAbilities.LavaImmune`, `HellAbilities.LavaHop` and `HellAbilities.TryLavaBounce()` (call it every frame
  the feet touch lava; true = bounced, no damage), `HellBurn.Apply(bot)` for lava touching enemies, `ShadowStepBlocked`.

## Tuning (inspector fields on `HellAbilities` / `HellRewind`, or the pure defaults)

Shadow Step: range 18 m, ledge reach 2.2 m, drop 6 m, recharge 6 s (`blink`). Rewind: 4 s, cooldown 20 s. Dilation:
`dilationCurve` (noneBelow 2.5, fullAt 10.5, minScale 0.35, exponent 1), hold 3 s, ease 0.15 s; `momentumBank` (3 to 6 s
from 2.5 to 13 m/s). Umbral window 1 s; Ashwalker 6 s; lava hop window 0.15 s, height 1.4 m. Nightfall radius 6, 8 s,
cooldown 22 s. Combustion radius 5, damage 45, burn 3 s at 10/s.

## Setup

None: after every scene load `HellAbilities` adds itself (with `HellRewind`, `HellTargeting`, `HellSkillPick`, `HellHud`)
to every `SkylinePlayer` (`HellAbilities.AutoAttach`). The scripted runs (capture, demo, perf probe) are left alone unless
`-hell-abilities` is on the command line.

## Tests

- `core-tests/Skyline` (`HellAbilityTests`, 24): blink landings (ground, past range, void refused, ledge, wall foot, low
  ceiling, range skill), the darkness rule, cooldowns and charges (Shave, SetMax), the dilation curve and envelope (ease,
  hold, re-press), time-scale requests, the ring (4 s ago, short history), gun/armour/ledger restore, the wave clock's
  restore, darkness zones/spheres/lights, burn ticks, Momentum Bank, the skill offer (3 distinct, none owned, shrinks).
- `core-tests/Movement` (`MoveRewindTests`, 4): a restore replays the same path mid-slide and jump; the save is a copy;
  saving into an earlier save reuses it; the driver resync drops interpolation.

## What's left

- Nothing has been played. The Unity checks below are the first run.
- No EditMode/PlayMode tests on the Unity side yet.
- Sounds are hooks only (no `hell/*` clips made); the pitch hook isn't wired to the mixer.
- The Nightfall sphere is a rim of dark blobs (no transparent shader: builds strip variants nothing uses).
- Bot rewind: hover offsets, beams/sweeps in progress and gondola mines don't rewind; burns don't rewind.
- The repossession's towed assets and the Relay's finished steps stay done.
