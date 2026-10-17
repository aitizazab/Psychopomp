# Hell enemies (the event game, day 1)

The design is in `FableAnimationTests/event/HELL-DESIGN.md`, section "Enemies". There are three enemies. Each one keeps
the type id, the spawn calls and the functions of the Skyline bot it replaces, so the rooms branch spawns them by the old
ids:

| Id (unchanged) | Hell enemy | Replaces | What it does |
|---|---|---|---|
| `mall-cop-drone` | **Fire imp** | the mall-cop drone | Hovers and patrols like the drone. Fires ember bolts 3x as fast, aimed where you were 0.2 s ago. |
| `spotlight` | **Scythe demon** | the spotlight | A big ground demon. It strides in, raises its scythe and calls demon scythes. Each scythe projects its path, then beams along it. |
| `floor-care-unit` | **Lava ooze** | the floor-care unit | A throbbing blob that crawls at you and throbs faster as it winds up. It slams a lava pool that burns, then cools and fades. |

The gondola (`window-washer-gondola`) has no Hell stand-in and stays the Skyline one.

**The switch.** `SkylineBot.HellRoster` is `true` by default (it is reset to true on play). It makes `SkylineBot.Spawn(id, ...)`
look the def up with `HellEnemies.Find(id)`. Set it to `false` to get the Skyline bots back, for example for the old Skyline
scenes and their play-mode checks.

**What stays Skyline.** The pure Skyline defs (`SkylineEnemies.*`) are untouched, so the headless sims (FightSim, CombatSim,
RepossessSim, EncounterDodge) still balance the old bots and their tests are unchanged. No existing test number was changed.

## Code

**Pure code** (`Packages/com.vibejam.jamkit/Runtime/Skyline/`, tests in `Tests/Skyline/HellEnemiesTests.cs` and `StateCopyTests.cs`):
- **`HellEnemies.cs`**: `HellKind`, plus the three defs `FireImp()`, `ScytheDemon()`, `LavaOoze()`, and `Find(id)`.
- **`Enemies.cs`**: `EnemyDef` gains `hell`, the scythe fields (`scythe*`, `walkSpeed`, `walkKeepDistance`), the pool fields
  (`pool*`) and the pulse fields (`pulse*`).
- **`HellAttacks.cs`**:
  - **`HellImpAim`**: the fire imp's miss distance in numbers.
  - **`HellGeometry`**: segment distances.
  - **`ScytheBeam`**: one scythe's timeline: telegraph, sweep, hold, fade, done; a hit lands once.
  - **`ScytheVolley.Plan`**: where a volley's scythes go.
  - **`BeamLane`**: a room lane's clock.
  - **`LavaPool`**: spread, burn, cool, gone, with damage ticks.
  - **`OozePulse`**: the throb.
- **`StateCopy.cs`**: the Rewind's snapshot of a pure object. It covers all instance fields, the `Rng` captured deeply, and
  lists and arrays of values copied. It is used for the brain, the patrol, the body, the damage stages, the aim trail, the
  arrival, the idle work and the Hell timelines.

**Unity code** (`Assets/JamKit/Templates/SkylineRepo/Scripts/`):
- **`Common/HellRewindable.cs` and `Common/HellRewindRegistry.cs`**: the shared contract, byte for byte as specified, with
  their fixed GUIDs.
- **`Common/HellRewindLedger.cs`**: `IHellRewindSpawned` and the ledger (see "Rewind" below).
- **`Bots/SkylineBotHell.cs`** (a partial of `SkylineBot`): the Hell behaviour, the death and the rewind snapshot. Small
  hooks in `SkylineBot.cs`, `SkylineBotDamage.cs` (Hell damage points and colours), `SkylineBotBodies.cs` (Hell defs go to
  `HellBodies`) and `SkylineBodyPool.cs` (no ahead-built model for Hell defs).
- **`Bots/HellBodies.cs`**: one primitive builder per enemy, or a prefab override (see "Bodies").
- **`Bots/HellLook.cs`**: the colours.
- **`Bots/HellFx.cs`**: the death burst, the bolt flare and pooled flames. Its `HellFx.Prewarm()` is called from `SkylinePrewarm.Pools()`.
- **`Bots/HellScythe.cs`**: a demon scythe in the world.
- **`Bots/HellBeamLane.cs`**: a room's beam lane.
- **`Bots/HellLavaPool.cs`**: a lava pool in the world.
- **`Bots/SkylineBolt.cs`**: gains `MakeEmber()` and is rewindable.
- **`Bots/HellEnemyTestBed.cs`**: a drop-in check scene component.

## Fire imp

**Movement.** The drone's code: `HoverPatrol` strafes across your line within a 3 m leash, bobs, and dashes when hit. It
also uses the drone's search when it loses sight and its door arrival. Patrol speed is 3 m/s and the dash 5.5 m/s.

**Bolts.** 90 m/s (`HellEnemies.ImpBoltSpeedFactor` = 3x the drone's 30). A bolt aims where your chest was
`aimLagSeconds` = 0.2 s ago, with no lead and 0.5 degrees of spread. A target moving across the line is missed by
`speed x (0.2 s + flight)`, against a hit width of 0.56 m:

| Range | Flight | Miss at 4 m/s across | Miss at 9.5 m/s (sprint) |
|---|---|---|---|
| 8 m | 0.09 s | 1.16 m | 2.75 m |
| 16 m | 0.18 s | 1.51 m | 3.6 m |
| 30 m | 0.33 s | 2.13 m | 5.1 m |

At 3x speed the lag is now most of the miss, so the imp is dodgeable at every range by moving across its line. Standing still
is hit every time. Running straight at it or straight away from it is hit more than with the drone, because the flight time
no longer adds much. The tests pin this: `AWalkerAcrossTheLineIsMissedAtEveryRangeAndAStandingTargetIsNot`, and
`FlownBoltsMissAStrafeAndHitAStandingTarget` (90 m/s bolts flown at a 5 m/s strafe and at a standing target, 8 to 30 m).

**Other numbers.**

| Setting | Value |
|---|---|
| Damage | 18 a bolt (drone: 24) |
| Telegraph | 0.4 s: an ember swells in its mouth |
| Cycle | 1.1 s |
| Chassis | 60 hp |
| Plates | `horns`, 15 hp |

**Projectile.** An ember: a flickering orange-yellow HDR core with a short red-orange trail (0.045 s, about 4 m at 90 m/s)
and no taser arc. It ends in a small flare and embers where it strikes.

**Body.** About 1 m, horn tip to tail:
- a charred round body and head;
- bone horns (the plate);
- two bat wings that flap, faster in a dash and folding a little while it charges;
- flames that flicker on its head and tail;
- yellow eyes;
- the **core**, the weak point: a white-yellow sphere bulging out of its chest, ringed with cracked char.

## Scythe demon

**Body.** About 2.8 m to the horn tips:
- charred legs and torso;
- rust-and-bone pauldrons (two plates, 40 hp each), each with a flame;
- a horned head with burning eyes;
- a long scythe in its right hand;
- the **core**, the weak point: glowing out of a split in its chest, framed by two bone ribs.

Chassis 140 hp.

**Movement.** While awake and further than `walkKeepDistance` (16 m), it strides in at `walkSpeed` (1.6 m/s). It uses the
floor unit's detour-round-cover drive, with its legs swinging, and does not stride while winding up. Knocks only shake it:
it is too heavy to push.

**The attack.** Each brain cycle runs:
1. **Telegraph** (0.6 s): the scythe arm swings up over its head.
2. **Volley**, on the attack: `scytheCount` scythes appear (`ScytheVolley.Plan`):
   - **The demon's lane**: one lane out of the demon, running at you, 14 m long.
   - **Lanes across you**: the rest are parallel lanes across your position, at a seeded 35-90 degrees to the demon's line.
     The middle one passes within 0.3 m of where you stand, so standing still is hit. The others are `scytheSpacing`
     (3.6 m) either side, and the gaps between lanes are safe.
3. **Each scythe** (`ScytheBeam`, `HellScythe`):
   - **Telegraph**: it appears spinning at its path's start and **projects its path** as a thin red line for
     `scytheTelegraph` (0.9 s). The line widens as it gets closer to firing, and a bright mote runs down it every 0.32 s to
     show which way the sweep comes.
   - **Beam**: the blade **beams** along the line at `scytheBeamSpeed` (45 m/s), and a thick white-hot beam fills the path
     behind it.
   - **Hold and fade**: the full beam holds `scytheHold` (0.2 s), then fades over 0.3 s.
   - **Damage**: touching the lit beam (within `scytheHitRadius` 0.55 m of it, against the player's feet-to-head capsule)
     hits once for `scytheDamage` (30).

Every number is data on the def. The cycle is 3.4 s, so a volley comes every 3.4 s and lasts about 1.4 s.

**Beam lanes for rooms.** A room can cut "beam lanes" without a demon:
- **In code**: `HellBeamLane.Create(worldFrom, worldTo, period, offset, parent, target)`.
- **In a scene**: add a `HellBeamLane` component, with `from` and `to` in its local space at waist height (about 1 m), plus
  `period` and `offset`.

It fires a scythe on its clock (`BeamLane`: first at `offset`, then every `period`), with the demon's tuning by default. Its
target is the scene's `SkylinePlayer` unless the room sets one. It is rewindable.

## Lava ooze

**Body.** About 1.3 m across:
- an orange-glowing blob whose scale and glow throb on a beat (it swells sideways and squats);
- black cooled-crust plates on its back and sides (the plate `crust`, 35 hp);
- two dark hollows for eyes and glowing drips;
- the **core**, the weak point: white-hot, bulging out of its top and throbbing with the beat.

It no longer has the canister. Chassis 90 hp.

**Throb** (`OozePulse`). 1.1 beats a second asleep and 1.7 awake. Through its 0.8 s wind-up the beat speeds up to 5 a second
(`pulseTelegraphHz`), so the attack reads from the rhythm.

**Movement.** The floor-care unit's code, slower (3.2 m/s). It crawls round its post while asleep, drives at you round cover
when awake, and paths on the nav graph when it comes through a door. It winds up when your feet are within
`sweepRadius` (3.6 m) with a clear line, and stops driving at half that.

**Attack.** The floor-care unit's wind-up and area attack, with the molly-acid idea as lava. It slams (a hard squash that
springs back) and a **lava pool** spreads round it (`LavaPool`, `HellLavaPool`):

| Phase | What happens |
|---|---|
| Spread | Out to `poolRadius` (3.2 m) over 0.3 s. |
| Burn | Burns for `poolBurnSeconds` (3 s). It hurts while your feet are in it and within 0.35 m of its surface, so a jump clears it. It ticks every 0.25 s at 22 damage a second; the first touch ticks at once, and it never ticks faster than the tick time. |
| Cool | For `poolCoolSeconds` (1.5 s) it darkens to crust, shrinks to 85% and sinks away. It is harmless. |

Each ooze keeps at most `poolMax` (3) burning pools; laying another starts the oldest cooling. The cycle is 3.2 s. A pool
outlives the ooze that laid it.

## Bodies (placeholder art, easy to swap)

Each enemy's body is built by one method in `HellBodies`: `FireImp`, `ScytheDemon` or `LavaOoze`. They are made from
primitives:
- **Ink roles.** The whole rig is in the **Hostile** ink role and the core in the **bot weak-point** role (the pulsing
  outline).
- **Colours.** Charred black-red bodies, bone-orange horns and armour, and HDR glows that bloom (`HellLook`). The
  white-yellow is kept for the cores, so the weak point is the hottest thing on every body.

**To swap in final art,** put a prefab at `Resources/HellModels/fire-imp`, `scythe-demon` or `lava-ooze`, using these names
(the same contract as the Skyline bots' models):
- `Plate_<plateId>` for each plate;
- `WeakPoint`;
- for the imp: `Muzzle`, `WingL` and `WingR`;
- for the demon: `ArmR`, `LegL` and `LegR`;
- for the ooze: `Blob`;
- any `Flame*`.

Renderers without colliders get box colliders. The other rules:
- **Origins**: the imp's is its centre; the demon's and the ooze's are their feet.
- **Not pooled**: Hell bodies are never built ahead by `SkylineBodyPool`. Primitive bodies are cheap.

**Damage states** are kept. They are the same stages (70% and 30%), with the animation and the limp:
- Hell colours: the torn shards are char, bone and rust, and two glowing fire cracks replace the wires.
- The demon droops and twitches like the spotlight did.

## Death

On the killing hit:
- the brain is wrecked, and the `Wrecked` and `Killed` events fire as before;
- a **burst of fire and smoke** plays (`HellFx.DeathBurst`: a fireball, licks of flame, smoke puffs and embers);
- the plates still on are thrown off;
- the bot is **deactivated** (`gameObject.SetActive(false)`), never destroyed.

`WreckObject` stays null, so there is no strippable wreck in Hell. The log line is `HELL_ENEMY <id> died (deactivated, kept
for the Rewind)`.

## Rewind

Every Hell enemy, ember bolt, scythe and lava pool, and every beam lane, implements `IHellRewindable`. It registers with
`HellRewindRegistry` in `OnEnable` and unregisters in `OnDisable`. All of them run on scaled time (`Time.deltaTime`), so Time
Dilation and Stasis slow them.

**An enemy's snapshot** holds:
- **Movement**: position (its post and transform), facing and velocity (the knock and the patrol's).
- **Health**: chassis and plates (`EnemyBody`), and alive.
- **AI and attack state, timers and the random stream**: the brain, the patrol, the search, the door arrival, the idle
  crawl, the aim trail, the random stream, the damage stage, the throb, its pools, the demon's arm and stride, and the imp's
  charge.

**Restore** puts all of it back:
- it reactivates a bot that died since, logging `HELL_ENEMY <id> restored alive by the Rewind`;
- it sets the plates on or off and the wear;
- it sets the damage stage without its animation.

`StateCopyTests` show that a restored brain or patrol replays the same inputs exactly, random choices included.

**The one gap, and the ledger.** A rewindable registers only while active. So a Rewind to a moment *before* something
appeared (a wave spawned, a bolt fired, a pool laid in the last 4 s) has no snapshot of it, and it would stay.

`HellRewindLedger` fixes that. It is itself a rewindable and always registered:
- **Its snapshot** is the set of spawned things active at that step.
- **Its restore** quietly switches off every spawned thing active now that was not in that set (`IHellRewindSpawned.VanishForRewind`).
- **Order** does not matter relative to the other restores.

It is made on first use. A room should call `HellRewindLedger.Ensure()` when it starts, so the earliest frames hold it too.

**What HellRewind (abilities branch) must do for this to work:**
1. **Keep the references.** Keep each frame as (rewindable, snapshot) pairs and call `Restore` on the pairs of the frame it
   goes back to, including objects that are no longer registered. A dead enemy is inactive and unregistered; its `Restore`
   reactivates it.
2. **Iterate the frame, not the registry.** Iterate the frame's own array, not `HellRewindRegistry.All`, while restoring:
   a restore that reactivates something registers it, which changes `All` mid-loop.

## Sounds and voices

Hell's own sounds are looked up at `Resources/SkylineAudio/hell/<fire-imp|scythe-demon|lava-ooze>/<name>`, where `<name>` is
`telegraph`, `attack`, `plate_shed` or `wreck` (`alert` and `hurt` are asked for too). None exist yet. Until they do, only the
cues that warn or confirm (`telegraph`, `attack`, `plate_shed` and `wreck`) borrow the old bot's sound:
- the imp borrows the drone's taser zap;
- the demon borrows the spotlight's muffled rifle;
- the ooze borrows the floor-care unit's pump spit.

The robots' hums, chirps, cries and barks are off for Hell enemies.

## See them in Unity (for the main session)

The worktree has not been opened in Unity.

1. **Open a scene.** Open any Skyline scene that has a `SkylinePlayer` standing on flat open ground with about 25 m clear in
   front: the arena, the floor or the range.
2. **Add the test bed.** Add an empty GameObject with **Add Component > JamKit/Hell/Enemy Test Bed** (`HellEnemyTestBed`).
3. **Press Play.** In front of the player it spawns, each awake: a fire imp (14 m out, 4 m left, 3 m above the chest), a
   scythe demon (22 m out) and a lava ooze (9 m out, 3 m right). It also spawns a beam lane across the way 6 m ahead, firing
   every 3.5 s. The Console shows `HELL_TESTBED spawned ...`.
4. **Check:**
   - **The bodies read** against the scene: dark bodies, glowing white-yellow cores, pink hostile outlines, the core
     outlined in the pulsing weak-point role. Wings flap, flames flicker, and the ooze throbs.
   - **The imp**:
     - An ember swells in its mouth for 0.4 s, then a fast orange ember with a short trail fires.
     - Standing still, you are hit. Strafing across, it misses.
     - A hit gives a small flare.
   - **The demon**:
     - It strides in until about 16 m, raises its scythe for 0.6 s, then three scythes appear: one lane from the demon at you
       and two or more lanes across you.
     - Each shows a red line with a mote running along it for 0.9 s, then the blade sweeps it with a white-hot beam.
     - Standing still, you are hit once per scythe. Stepping into a gap between lanes, you are not.
   - **The ooze**:
     - Its throb speeds up for 0.8 s, then it squashes and a lava pool spreads 3.2 m round it.
     - Standing in the pool, you take ticks of damage; jumping clears it.
     - The pool cools dark and sinks after about 3.3 s.
   - **The beam lane**: a scythe on its line every 3.5 s.
   - **Damage states**: shoot an enemy to 70% and 30%. Sparks, torn shards in char, bone and rust, and glowing cracks appear.
     Plates (horns, pauldrons, crust) fly off when broken.
   - **Deaths**: a fireball with smoke and embers. The enemy's GameObject goes **inactive** (not destroyed) under the test
     bed, and `HELL_ENEMY ... died` is logged.
   - **Rewind**:
     - Press **R** (the test bed's stand-in Rewind: the last 4 s of every registered rewindable). Enemies killed in the last
       4 s come back alive where they were (`HELL_ENEMY ... restored alive`), with their health and plates.
     - Bolts, scythes and pools are back where they were.
     - Things fired or laid after that moment vanish.
     - The player is not rewound by the stand-in.
   - **Time scale**: with `Time.timeScale` at 0.35 in the inspector's Time settings, bolts, scythes, pools, throbs and
     wings all slow down.
5. **Respawn.** Press **T** to spawn another set.

In a real room, set `SkylineBot.SearchArea` to the room's flight area. The imp's patrol and search stay inside it, and the
default is the Skyline hall's rectangle. The test bed sets it to 60 m round the player.

## Tests

- **New**: `HellEnemiesTests` (14) and `StateCopyTests` (7).
- **Existing**: unchanged and passing.
- **Unity side**: the compile check (`core-tests/SkylineUnity`) and the per-assembly check are green.

To run the Unity-side compile check in this worktree, `Library/ScriptAssemblies` and the NUnit package were copied from the
main checkout's `Library` (ignored by git).

## Left to do

- **Sounds and voices** for the three enemies: say what each sound physically is first, then let the creator pick. Barks are
  off.
- **Final art**: drop prefabs at `Resources/HellModels/` following the names above.
- **Not seen in Unity yet**: the placeholder bodies, the weak points' reach from the front and the effects need a look. The
  likely tuning points are:
  - the scythe line widths;
  - the pool's HDR glow against real lava floors;
  - the imp's ember brightness.
- **The headless sims** (FightSim and the others) still run the Skyline defs. A Hell fight sim, to balance the volley and the
  pools against the player's abilities, is not built.
- **Melee**: the demon does not use its scythe in melee, and the ooze does not chase onto ledges (it works on one floor
  level, like the floor-care unit).
- **Gondola**: it has no Hell version and its rail and mines are not rewindable.
- **Writ spots** (the Skyline gun's cracked-casing mechanic) are not in the enemy snapshot.
- **Snapshot cost**: about 10 small allocations per enemy per fixed step, through reflection-based `StateCopy`. If the Rewind
  shows in the profiler, replace `StateCopy` for `BotBrain` and `HoverPatrol` with hand-written structs.
