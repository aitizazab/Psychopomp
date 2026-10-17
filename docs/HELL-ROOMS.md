# Hell: rooms, hazards and the run (branch `hell-rooms`)

Day 1 of the event (2026-10-17). The design is `event/HELL-DESIGN.md` in the FableAnimationTests repo. This branch builds the
rooms, their hazards and objectives, and the run that strings them together. Every room is built for Hell from scratch: no
M1 arena, Food Court or Relay map is reused (the creator, 2026-10-17). The systems are reused: the builder pattern,
`ArenaNav`, the tow circle (`Repossession`), the HUD and `SkylineBot.Spawn`.

The branch has `hell-enemies` (197aa893) merged in, because it uses `HellBeamLane`, `HellRewindLedger` and the shared rewind
contract files from there. The abilities branch is not merged: its types are stubbed (see "Merging").

## What is built

### Pure code (`Packages/com.vibejam.jamkit/Runtime/Skyline/Hell/`, tests in `Tests/Skyline/Hell/`)

| File | What it holds |
|---|---|
| `HellRoomTypes.cs` | The room as data (`HellRoomDef`): solids (one collider each, `FloorSolid`), hazards (`HellHazard`: lava, collapse, beam), enemy spawns by role and wave, objective stops, dark zones, fire lights, the exit gate, the drop zone, and the safe and fast paths. `HellRoomTransform` handles mirroring, quarter turns and offsets, ramps included. |
| `HellRoomLayouts.cs` | The six layouts (below). |
| `HellRoomVariants.cs` | The per-floor variants (3 x 6 = 18 rooms). |
| `HellRoomLint.cs` | The room rule as a check (below). |
| `HellRun.cs` | `HellDeck.Deal(seed)` draws 4 of the 6 rooms per floor, seeded. `HellRun` is the run's state machine: rooms, drops, shaft, pick, finish, deaths. |
| `HellObjectives.cs` | The six objectives (below). |
| `HellHazards.cs` | `LavaContact` (damage over time, then a sink), `CollapseCycle` (solid, shaking, fallen, returns), `BeamCycle`, `SafeSpot`. |
| `HellWorld.cs` | Where things sit in the one scene: a slot per room (floor row x kind column, 320 m / 420 m apart) and a shaft per floor. |

### The six rooms

Units are metres. Ground is y 0. Lava sits in 1.2 m-deep basins (surface y -1.2): you can wade out with a jump and a mantle,
but standing in it for 1 s sinks you.

1. **The Six Sigils** (72 x 182 m, a lava cavern). A stone ring walkway runs round the walls, with cover on its lava edge.
   The sigils are 30-47 m apart, at heights 0, 3, 6, 0, -2 and 4. The safe way is stone: the ring, plus walkways
   through the lava kept off the fast lines (2026-10-17, to cut the safe path from 931 m to 414 m): a bridge from the
   start terrace to A, a south walkway to B's low landing and a ramp up onto B, B's north ramp and a walkway to a
   crossing in front of C (C has an east ramp), an east spine past D's bridge, and a bridge and ramp into the pit for E.
   Each leg tests a different skill, every one simulated (below):
   - **A to B:** jump and double jump onto a 9 m wall standing in the lava, run it, run off its end onto B's low
     landing (0.5 m), then up onto B (a ramp, or a hop and a mantle).
   - **B to C:** a rising chain of 5 collapsing platforms (4 m), a double jump up onto C.
   - **C to D:** run off C and jump down onto a dark perch, one Shadow Step to a second dark perch (you keep your speed),
     a hop onto D. One charge: the old two-step chain needed two.
   - **D to E:** a 2 m jump onto the beam platform, slide under the beam, jump the lava into the walled pit.
   - **E to F:** into the corridor between the pit's west wall and a tall wall, wall-run its west face north, wall-jump
     west over the pit wall onto a low block in the lava (0.5 m), then a jump, a double jump and a mantle onto F.

   The exit is past F.
2. **The Soul Lantern** (44 x 60). Take the lantern by the entry and carry it to the altar across a lava chasm. While you
   carry it you are slower (0.8x) and can't Shadow Step. Fast: a bridge of 9 collapsing segments. Safe: a ramp up to the
   west wall ledge, along it behind low cover, and down again.
3. **Break the Chains** (40 x 64). Three anchors hold the exit gate: on the west wall at 4.5 m, the east wall at 7.5 m and
   over the exit at 12 m. Each is warded, so a shot only bites from within 12 m. Fast: wall-run the walls that stand in
   the lava and shoot in mid-air; jump off the north deck for the third. Safe: ramps up to each anchor's balcony and to
   the gallery. Dark pillars in the lava make a Shadow Step line; the middle one (5 x 6 m) is where the west wall's wall
   jump lands, the stepping stone between the two walls.
4. **Hold the Brand** (52 x 70). Charge from speed, not from standing (the creator, 2026-10-17). The brand is a glowing
   ring 20 m across on a 24 m island, round an 8 m lava pool: the track between them is a racetrack, with a wall-run
   wall on each long side and three stones (one on the ring's south edge, two at the pool's corners). Inside the ring
   (feet within 10 m across and 4.5 m up or down, so a wall-run counts) the brand charges at a rate that grows with your
   horizontal speed: 30 s standing or crouching behind a stone, 3x that rate from 8.5 m/s up (sprint, slide, wall-run:
   about 10 s, measured 10.5 s in the movement engine from a standing start, about 1.7 laps of the 56 m racing line).
   Stepping out pauses it; it never drains. Waves come every 5 s from the first touch whatever you do, so a slow charge
   costs more fights (7 waves standing, 2-3 running). The ring's glow and the HUD ring's halo pulse faster the faster
   it charges, and the objective line shows the rate ("x3.0"). Fast in: 3 collapsing stepping stones over the lake, or
   two dark pillars for the Shadow Step. Safe in: the west walkway and the open bridge, then the stone at the pool's
   south-west corner.
5. **The Collapse** (40 x 120; redesigned on `hell-rooms3`, 2026-10-18, the creator: "that section should be only half of
   the room"). **The first half (z 0-56) is a stone run-up hall** that never falls, with two ways through it, the same
   length: two staggered rows of hanging **stalactites** over its middle (rock slabs from a rock roof at 12 m down to
   2.1 m, faces 3 m apart, z 30-49.5), which you can **wall-kick** between (jump up into the first, then kick across to
   the other row and back: each kick within the wall-kick window keeps your speed and adds along the wall; four kicks take
   a sprint's 9.5 m/s to about 20 m/s), or simply run below them (a 1.7 m runner passes under; no speed gained). The hall
   is dark under the roof (the room's dark zone). **The second half (z 56-112) is the collapsing bridge**, its tiles
   falling on the room clock row by row from the bridge's start. The central lane is straight (one 4 m lava gap); its front
   sets off from the bridge's start at 5.0 s and comes at 13 m/s, so it is already falling when a sprinter arrives (about
   6.5 s), and only the kick route's speed outruns it. The west lane winds between 6 stone fins (on the lava bed: they
   stand when their tiles fall), 9 m apart with 9 m of sideways weave; its front sets off at 7.3 s at 5.5 m/s along the
   room: survivable at a sprint, with no room for a fumble at the start. Debris falls on both lanes on a seeded timer, on
   the running line 7-24 m ahead of the front: its shadow glows on the floor 1 s before, it hits for 30 within 1.5 m, and a
   sidestep clears it. Imps come at the side walls every 3 s over the bridge (3 waves, 2 each). **The bridge's lava has no
   floor** (`HellRoomDef.lavaFloor` false): a fallen tile drops you into it and you sink at once, which restarts the room
   (the floor behind you is gone).
6. **The Ooze Wells** (52 x 64). Three wells in a lava lake. Stand within 3.5 m of a well for 3 s to seal it, in any order.
   An open well spawns an ooze every 7 s, at most 2 alive per well. Fast: 4.5 m collapsing stepping stones a sprint hop
   (6 m) apart between the wells; the piers of wells 1 and 3 run out to the row of stones, well 2's pier out to its well.
   Safe: the ring walkway and the piers.

Each room has an entry vestibule and an exit corridor with a drop hole. Every room except The Collapse has an exit gate
that opens when the objective is done.

### Open edges (`hell-rooms3`, 2026-10-18)

The creator: "random walls in an odd box shape that prevent the player from seeing outside". The rooms are no longer boxed
in. `Kit.Shell` keeps only the north wall tall (the exit's gatehouse, with the gate, the lintel and Baja's hour gate in
its doorway); the west, east and south sides are an **open rim**, built by `Kit.Rim()` once the room's paths are known,
metre by metre along each side:
- a **tall wall** section only where something needs one: the job pocket's door (24 m round it, `PocketWall`, so
  `HellJobPockets` can still cut its door), a chain anchor on the wall (8 m behind it, `AnchorWall`), and wherever a
  raised stretch of the safe path (a ramp or balcony along the side, above 0.15 m) comes within 3.5 m;
- a **parapet** (1 m, `parapet-*` solids, `ParapetHeight`) wherever the safe path comes within 3.5 m (`RimGuard`): low
  cover that keeps the safe way off the drop;
- elsewhere an **open cliff edge**: the floor or the lava simply ends, the sky and spires beyond, and 40 m down the
  builder's "Abyss Sea" glows (no collider). A fall off the edge is a sink: past the room's bounds (`bounds.min.y - 3`)
  `HellRoom` puts you back at the last safe spot, as lava does (in The Collapse it restarts the room).

Braziers stay on the tall walls and now also stand on the parapets (`HellLightRig`; docs/HELL-LIGHTING.md, "The open
rim"). The lint checks it: **open edges** below.

### Momentum through the drops (`hell-rooms3`, 2026-10-18)

The creator: "Floor/room transitions are a teleport, so you can't carry speed through them." Now they carry it.
`HellMomentum` (pure, in `HellWorld.cs`) keeps the peak of your horizontal speed while you play a room, fading 2 m/s each
second (so bumping the drop hole's far wall on the way down costs little). When the drop hole (or a floor's shaft, the
peak holds while you are out of a room) brings you into the next room, `HellRunDirector` still places you 4 m above its
vestibule, but now **moving at that speed along the room's entry direction** (your velocity rotated into the new room),
capped at 16 m/s so the landing stays on every room's entry floor (about 9 m ahead of the vestibule's middle: the
vestibule is open to the room, so you fly out of it and land running). A death, a restart and a new run carry nothing.
The Rewind boundary at room entry is unchanged (`RoomEntered` still clears the history). The console logs `HELL_RUN
carried N m/s into the room`. The Collapse's sims start from a standing drop (the worst case): speed carried in only helps.

### The lint: the creator's room rule, measured

`HellRoomLint.Check` runs on all 18 rooms in the tests and in the command-line build.

**Paths.** Both the safe and fast paths must:
- run from the entry to the exit;
- reach every stop (in order where the room says so). An anchor counts as reached within its ward, measured from eye
  height.

**The fast path** must:
- be at most 0.8x the safe path's length;
- be at most 1.3x the straight line through the stops;
- cross at least one hazard.

**The safe path** must be solid ground. Every 0.5 m sample sits on a top within 0.6 m: ramps are followed, and climbs of up
to 3 m within 4 m are allowed. No sample may be over lava or a platform, in a beam, or inside a solid. Every leg must also
be walkable on `ArenaNav` (1 m grid), unless it's a climb.

**Scale** (the creator: "the six points are all so close together"):
- a room is at least 40 x 60 m;
- objective legs are at least 20 m (30 m in the Six Sigils);
- every leg's straight line crosses a hazard or a solid more than 2.5 m above its higher end, so no leg is a plain walk.

**Darkness:**
- every room has at least one dark zone;
- no steady fire light reaches a dark zone; a lava **flare** may, for a moment (at most 2 s up, 25% of its cycle), never into
  a dark zone on the fast line, and one dark zone per room stays out of every flare (fire kills shadow, live:
  `docs/HELL-SHADOW.md`, 2026-10-18).

The lights are generated over the lava (one per 12 m, radius 9), and each one's radius is clamped clear of the dark zones.
A light held back by a dark zone off the fast line also flares out to 9 m for 1.2 s every 6-8 s (`HellLight.flare*`).

**Falling tiles:** no solid may stand on a timeline tile; anything over one must reach down to the lava bed (this caught
the old floating baffles).

**Open edges** (`hell-rooms3`): every safe path sample within 2.5 m (`OpenEdgeClear`) of the room's rim (`HellRoomDef.rim`,
its west, east, south and north wall lines) must have a parapet or wall on the rim beside it, at least 0.8 m above the
sample, so the safe way never runs along a drop. The vestibule (8 m round the entry) and the exit doorway (4 m) are the
room's own doors. `Kit.Rim()` guards 3.5 m, so the built rooms pass with a metre to spare.

**Hold rooms** (the Brand's ring, `HellHoldRing`): the "fast path" is the run in plus the circling line. The lint checks:
- the ring is at least 18 m across, has a lava pool in its middle and a runnable wall inside;
- its racing line is closed, at least 40 m a lap, and every sample is inside the ring on solid ground (not over a
  hazard, not inside a solid);
- the fast path enters the ring; the safe path stops inside it within 1.6 m of a stone at least 1 m tall;
- a full-speed charge is at least one lap;
- **as time:** each path's cost is its length plus its charge time run at sprint speed (safe 30 s, fast 10 s, at 9.5
  m/s), and the fast cost is at most 0.8x the safe cost. The plain length rule (0.8x) still applies to the paths too.

Measured path lengths, base layouts:

| Room | Safe (m) | Fast (m) | Notes |
|---|---|---|---|
| Sigils | 414 | 276 | was 931: the crossings (2026-10-17) |
| Lantern | 97 | 64 | |
| Chains | 251 | 90 | |
| Brand | 139 | 82 | with the charge at a sprint: safe 424, fast 177 (0.42x); racing line 55.8 m a lap, 1.7 laps |
| Collapse | 163 | 124 | 0.76x (rooms3): the safe lane weaves 9 m between the fins, plus the end detour |
| Wells | 276 | 112 | |

The Collapse in the movement engine (`HellRoomTests`, rooms3: the whole room in the traversal simulator on the room
clock, dropped in standing from 4 m as the run does; the bridge's tiles really fall and its lava has no floor):

| Floor | Kick route (stalactites, then the central lane) | Sprint below, central lane | Slide and hops below, central lane | Sprint, safe lane |
|---|---|---|---|---|
| 1 | made it in 8.95 s, 20.6 m/s out of the hall, smallest lead 0.49 s | loses at z 59.8 (lead -0.68 s), 9.5 m/s | loses (lead -0.01 s), 10.1 m/s | 14.6 s, smallest lead 0.80 s |
| 2 | 8.95 s, lead 0.39 s | loses (-0.78 s) | loses (-0.10 s) | 14.6 s, lead 0.70 s |
| 3 | 8.95 s, lead 0.22 s | loses (-0.93 s) | loses (-0.25 s) | 14.6 s, lead 0.55 s |

The base layout's safe lane: lead 0.70 s. A 2 s fumble at the start loses the safe lane on every floor. Both routes
through the hall are the same ground (`BothRoutesThroughTheHallAreTheSameLength`); the difference is speed only: the
kicks reach the bridge about 1.5 s sooner and twice as fast, and the central lane is already falling when a sprinter
gets there. The bug test `AFallenSafeTileDropsYouIntoTheLava` stands a player on a safe-lane tile and checks they sink
within 0.6 s of it falling (floors 1 and 3).

### Floors (`HellRoomVariants.Floors`)

| Floor | Lava DPS / sink | Shake | Platforms return | Beams on | Collapse speed | Layout | Enemies |
|---|---|---|---|---|---|---|---|
| 1 The Ember Gate | 10 / 30 | 0.9 s | x0.8 (sooner) | x0.8 | x1 (was x0.9: a sprint then kept up) | as built | imps only (other roles become imps, 3.5 m up) |
| 2 The Molten Works | 15 / 45 | 0.8 s | x1 | x1 | x1 | mirrored | imps + scythe demons |
| 3 The Black Glass | 22 / 60 | 0.65 s | x1.5 | x1.25 | x1.06 (was x1.12: the safe lane then outran a sprint) | turned a quarter (world orientation) | all three, plus the `minFloor 3` spawns |

Mirroring is the variation you can feel in play. A quarter turn only changes the room's world orientation, so floor 3
plays with floor 1's handedness. Its difference is in the numbers and the enemy mix.

### Unity (`Assets/JamKit/Templates/SkylineRepo/Scripts/Hell/`, editor `Editor/SkylineArenaBuilder.Hell.cs`)

- **`HellRoom`** is the room at run time. It rebuilds its data from `HellWorld.Room(floor, kind)`.
  - **`Begin`:**
    - starts the clock and spawns wave 0;
    - registers the dark zones and lights with `HellDarkness`;
    - sets `SkylineBot.SearchArea` and `Nav` to the room and calls `HellRewindLedger.Ensure()`;
    - makes `HellBeamLane`s from the beam hazards;
    - binds the HUD tow ring (the Brand: the kit's `Repossession` only mirrors `BrandHold`'s charge and "inside the
      ring"; `SkylineHudObjective.RingRate` drives the ring's pulsing rate halo).
  - **Each fixed step:**
    - ticks the objective (the Brand gets the player's horizontal speed from the movement sim);
    - spawns the brand's waves, the wells' oozes and The Collapse's wall imps;
    - The Collapse: deals each debris drop's hit on the step it lands (`HellDebris.Lands` / `Hits`, 30 damage);
  - **Each frame (visuals only, all from the room clock and the objective, so a Rewind shows them rewound):** the
    brand ring's segments pulse faster with the charge rate (`BrandPulseSlowHz` + `BrandPulseFastHz` x the rate share),
    dim outside the ring and burn steady when set; The Collapse's debris shadows (made at run time, one per drop) grow
    over their 1 s warning, the rock falls in its last 0.45 s, the rubble lies 0.6 s.
    - keeps the last safe spot;
    - handles a sink or a fall out of the room;
    - opens the exit gate when the objective is done.
  - **The lantern:** sets `HellRoomFlags.Carry(true)`, which sets `HellAbilities.ShadowStepBlocked`, and
    `player.MoveScale = 0.8`.
  - **Rewind:** implements `IHellRewindable` for the clock and the objective.
- **`HellLava`** is one per room, covering all its basins.
  - It deals damage over time and sinks you: a heavy hit, then back to the last safe spot.
  - It asks `HellAbilities.TryLavaBounce()` every step your feet touch lava, and reads `HellAbilities.LavaImmune`.
  - It sets `HellAbilities.LavaQuery` and burns bots in the lava (`HellBurn.Apply`).
  - Rewindable.
  - Floorless lava (`HellRoomDef.lavaFloor` false, The Collapse's bridge): the builder gives those basins no collider, and
    `HellRoom` sinks you as soon as your feet are 0.3 m under the surface (`LavaContact.SunkIn`): the creator saw the player
    stay up when the safe lane's tiles fell, because the old basins had a collider at the surface (lava to wade out of),
    so a fallen tile left you standing on the glowing lava for the 1 s sink time, and any hop reset that clock.
- **`HellCollapsePlatform`** is one per platform. It shakes for the floor's shake time, then falls: the collider goes at
  once and the block drops 6 m. It returns after the room's delay, unless your body is in its place. Timeline tiles
  trigger on the room clock instead. Rewindable, and never destroyed.
- **`HellAnchor`** is a shootable chain anchor (`ISkylineHittable` via `SkylineHitZone`). Deflected shots return
  `hit = false`, so they show no marker. Its look is `HellChainViews` (made in `HellRoom.Awake`, ticked in its Update;
  the builder's red box and box links are hidden at run time, the box's collider stays the hit zone): a massive iron
  chain from a riveted wall bracket to the exit gate, held by one straining link at the anchor (ember to white-hot as its
  health falls, cracks that widen, sparks that thicken, a pulse and swell on each hit) inside a faint rune ring that
  burns bright while you stand in the ward. Broken: the link shatters (glowing pieces, a spark burst, a light flash), the
  chain whips and falls into a heap at the gate's foot, the gate shudders (a visual twin; its collider never moves). It
  reads the `ChainAnchors` state every frame, so a Rewind or a restart makes an anchor whole again at once; no per-frame
  allocation.
- **`HellRunDirector`** runs the deck and the run:
  - it drops you into each room from 4 m above its vestibule, moving at the speed you carried out of the last one
    (`HellMomentum`, "Momentum through the drops");
  - a death restarts the room (`player.Wrecked`, then `Room.Restart`, `player.Restart` at the entry, and
    `HellRunEvents.RoomRestarted`);
  - a room's drop hole leads to the next room, and after the 4th room to the floor's shaft;
  - at the shaft's bottom it calls `HellRunEvents.RaiseFloorCleared(floor)` and waits for `ContinueAfterPick()`; with no
    listener it continues at once;
  - after floor 3's shaft the run ends and the flow moves on to the boss hook, the ending, the credits and the stats
    (`docs/HELL-FLOW.md`: since the `hell-flow` branch the game opens on a title screen and the director drives `HellFlow`);
  - `-hellSeed N` fixes the deck.
- **`HellRunEvents`** (the contract; extended by `hell-flow` with the boss, ending, stats and Rewind-boundary hooks,
  see `docs/HELL-FLOW.md`):
  - `FloorCleared(int)` takes a 0-based floor and is raised for floors 0 and 1 only;
  - `ContinueAfterPick()`, `WaitingForPick`;
  - `RoomEntered`, `RoomRestarted`, `RunStarted`, `RunFinished`.
- **`HellHudLine`** shows the objective line and the room clock under the HUD's objective strip, plus floor and run
  banners. The wording is a draft.
- **`HellAbilityStubs.cs`** stubs `HellAbilities`, `HellDarkness` and `HellBurn` under `#if !HELL_ABILITIES`, with the
  abilities branch's real signatures.
- **The builder:** `SkylineArenaBuilder.BuildHell` builds `Assets/JamKit/Templates/SkylineRepo/Generated/HellDescent.unity`
  and puts it first in the Build Settings. It builds:
  - all 18 rooms and 3 shafts;
  - the player, the HUD and the director;
  - dark lighting and fog.

  The materials make each kind of ground readable:

  | Material | Means |
  |---|---|
  | Stone, per floor (ash, iron-brown, obsidian) | Solid ground |
  | Emissive orange lava | Lava |
  | Collapse platforms with an ember rim | Collapses under you |
  | An ember stripe on wall-run walls | The fast line |
  | A black shadow pall and obsidian floor | A dark zone |
  | Cold blue guide studs every 8 m | The safe path |

## Tuning (where the numbers live)

| What | Where |
|---|---|
| Per-floor hazard numbers and enemy roles | `HellRoomVariants.Floors` |
| Per-room numbers | The layout's `k.r.*`: `collapseReturnSeconds`, `tuning.radius`, `tuning.seconds` (wells' seal time, the brand's wave interval), `tuning.spawnEvery` and `spawnCap` (oozes), `tuning.anchorHealth` and `wardRadius`, `tuning.carryMoveScale` |
| Lava | `HellRoomDef.lavaDps`, `lavaSinkDamage`, `lavaSinkSeconds` (set per floor) |
| Hold the Brand | `HellBrandTuning` (`HellObjectives.cs`), all in one place: `RingRadius` 10 (20 m across), `RingHalfHeight` 4.5, `SlowSeconds` 30 (standing), `FastRate` 3 (so 10 s), `SlowSpeed` 3 m/s (at or below: the slow rate), `FullSpeed` 8.5 m/s (at or above: the fast rate; linear between, a 5 m/s walk is about 1.7x), `WaveEvery` 5. Geometry: `HellRoomLayouts.BrandCentre`, `BrandLoopHalf` 8, `BrandLoopCut` 4.5 (the racing line). Glow: `HellRoom.BrandPulseSlowHz` 0.6, `BrandPulseFastHz` 3.4; HUD halo `SkylineHudObjective.RingGlowSlowHz` / `RingGlowFastHz` |
| The Collapse | `HellRoomLayouts`: the hall (`CollapseStartZ` 56: the bridge starts here; the stalactites `CollapseStalactiteFirstZ` 30, `CollapseStalactiteSpacing` 8, `CollapseStalactiteStagger` 2, `CollapseStalactiteLength` 7.5, `CollapseStalactiteCount` 2 per side, `CollapseStalactiteHalfGap` 1.5, `CollapseStalactiteBottom` 2.1, `CollapseRoofY` 12), the fronts (`CollapseFastLaneSpeed` 13, `CollapseFastDelay` 5.0, `CollapseSafeLaneSpeed` 5.5 along the room, `CollapseSafeDelay` 7.3, both from `CollapseFrontOriginZ` = the bridge start), `CollapseWaveEvery` 3 (wall imps), the fins (`CollapseBaffleFirstZ` 60, `CollapseBaffleSpacing` 9, `CollapseBaffleCount` 6, running lines x -9.5 / -18.5); floors scale both fronts (`collapseSpeedScale` 1 / 1 / 1.06). More stalactites = more kicks = more speed (each kick adds about 2.7 m/s) |
| The Collapse's debris | `HellDebris` (`HellHazards.cs`): `TelegraphSeconds` 1, `FallSeconds` 0.45, `RubbleSeconds` 0.6, `Damage` 30, `Radius` 1.5, `DropHeight` 12, `MinGap`/`MaxGap` 1.3-2.1 s per lane, `MinLead`/`MaxLead` 7-24 m ahead of the front, `SideJitter` 0.7 m off the running line; seeded per floor in `HellRoomVariants.Build` (`CollapseDebris(room, floor + 2, scale)`) |
| Geometry | Edit the layout in `HellRoomLayouts`, then run the tests: the lint says what broke |
| Lights | `HellRoomLayouts.LightSpacing`, `LightRadius` |
| Enemies | `k.Spawn(role, position, yaw, wave, minFloor)` in each layout |

## The traversal simulator (the fast lines, simulated)

`Tests/Skyline/Hell/`:

| File | What it holds |
|---|---|
| `HellTraversalSim.cs` | `HellTraversalWorld`: a room variant as the movement engine sees it. Every solid is a `BoxWorld` box or ramp, with gates open. Each lava basin is a solid box at the lava surface, as the Unity builder makes it. Each collapsing platform is a box with its own `CollapseCycle` at the floor's shake time; The Collapse's timeline tiles stay solid unless the leg runs `OnTheClock(watch)` (rooms3: the tiles then shake and fall on the room clock from the leg's start, the floorless lava under them ends the run, and `watch` sees every step); `HellRoomTests` races the whole room that way. Beams are always on, and a body in one fails the leg. The darkness is the room's dark zones and fire lights in a `DarknessField`. The Shadow Step uses `BlinkPlanner` and `BlinkRules` on rays against the same boxes, dark checked at the feet + 0.5 m like the Unity side; it has one charge (6 s), keeps your velocity and is refused while carrying. `HellLeg` is a scripted leg; `HellTraversal` runs it. |
| `HellFastLines.cs` | The legs, room by room: every fast-line leg that needs a jump, a wall-run, a slide or a step (15 legs, all six rooms; The Collapse's whole-room races are `CollapseSpeedRoute`, `CollapseNormalRouteFastLane` and `CollapseSlideHopFastLane`, run by `HellRoomTests`). |
| `HellFastLineTests.cs` | One test per leg. It runs on all three floors (floor 2 mirrored, floor 3 turned), at full speed and at 90% (sprint speed and start speed x 0.9). Every press is tried 0.1 s early and 0.1 s late one at a time, and all presses 0.1 s late together; every run must succeed. |

It drives the real `MoveSim`, the code the player runs, at 120 Hz. Its world is `BoxWorld`, not Unity's
CharacterController, so positions are close but not exact.

**A leg** starts standing at `from`, already moving toward `face` at a share of sprint speed (`StartSpeed(0)`: from
a standstill). Then its beats run in order. A beat looks at a point (the view's yaw turns to it; the stick is held
forward and sprint is held). It may press one thing as it starts, and it lasts until its condition (never before its
own press has fired). After the last beat the player runs at the goal. The leg succeeds when the feet stand on ground
within the goal's radius and half a metre of its height. It fails on lava, a beam, a fall or the timeout. Points and
conditions are in the base layout's frame; the floor variants run through their transform.

```csharp
yield return new HellLeg(K, "A-B wall-run onto B", V(-18, 0, 33), V(-12, 0, 41))
    .Run(V(-12, 0, 41), p => p.Pos.Z > 37.4f)                          // run to A's edge
    .Jump(V(-9, 1, 42.6f), p => p.Falling)                             // jump, until the apex
    .Jump(V(-6, 1, 42.6f), p => p.Wallrunning || p.BeatTime > 1.2f)    // double jump onto the wall
    .Run(V(4, 1, 42.6f), p => p.Pos.X > -2f && p.Grounded)             // run the wall, off its end
    .Run(V(15, 0.5f, 47.5f), p => p.Pos.Z > 46.3f)
    .Jump(V(16, 3, 52), p => p.Grounded)
    .Goal(V(16, 3, 52), 3.5f);
```

The beats are `Run`, `Jump`, `Hop`, `Slide` (presses crouch and holds it), `Step(at, look, until)` (a Shadow Step
aimed at a point) and `Carrying()` (the lantern's 0.8x stick, no step). A `Hop` is a jump on landing: its timing slop
is late only, because a press before the landing is a double jump. Early presses are timed from the on-time run; late
ones from when the beat is actually reached, as a player reacting late. The `HellProbe` a condition reads has `Pos`,
`Grounded`, `Falling`, `Wallrunning`, `BeatTime`, `HorizontalDistance(p)` and the `Sim`. `Stones(...)` scripts a run of
stepping stones the way a player crosses them: on each stone, run on and jump from its far half.

**Writing a leg that reads like a player:**
- Look past where you land: looking at the landing spot swings the view round as you reach it and kills your speed.
- Jump from position conditions (an edge, a distance) rather than from times, so a late press stays a late press.
- A failure prints the run's log: presses, mode changes, platforms falling and where it ended.

To add a leg, add a `yield return` to the room's method in `HellFastLines.cs`; the test picks it up. Run only these:
`dotnet test core-tests/Skyline/Skyline.Tests.csproj --filter FullyQualifiedName~HellFastLine` (about 10 s).

**What the simulation changed (2026-10-17):**

| Room | Leg | What failed | The fix |
|---|---|---|---|
| Sigils | A to B | The 22 m wall can't be run in one wall-run (1.75 s); a low catch sank into the lava by x -4, and the wall jump pushed away from B. | The wall is 9 m (x -12..-3), running off its end onto a low landing (0.5 m) south of B, then a ramp or a hop and mantle onto B. |
| Sigils | B to C | 3 m platforms: hops at 90% speed fell short, and the errors added up over five hops. | 4 m platforms, and a double jump onto C. |
| Sigils | C to D | Two Shadow Steps in a row: there is one charge (6 s recharge). | A jump down onto a dark perch, one step to a second, a hop to D (D moved 4 m west to keep the step in range). |
| Sigils | D to E | The double jump onto the beam platform landed in the beam. | The platform starts 2 m sooner (z 110): a plain jump, land, slide. |
| Sigils | E to F | Two walls whose wall jumps pushed away from F; not enough height out of a 6 m climb. | One tall wall in the pit, run on its west face; the wall jump west lands on a low block in the lava (0.5 m) beside F, then up onto F. |
| Chains | The two walls | The west wall's wall jump overshot the middle pillar (3 m) into the lava. | The pillar is 5 x 6 m, where the jump lands. |
| Wells | Every stone run | Stones 4.3-4.5 m apart and 3 m wide: every sprint hop (6 m) overshot. | 4.5 m stones a sprint hop apart; the piers run out to the stones. |
| Lantern, Brand, Collapse | All | Held as built. | None (the Brand and The Collapse are hell-rooms2's; simulated, not changed). |


## Checks

`powershell -NoProfile -File tools/skyline/test.ps1` runs the pure tests and the Unity-side compile check. To run the
compile check in a worktree, copy `Library/ScriptAssemblies` and the NUnit package (`Library/PackageCache/com.unity.ext.nunit@*`)
from the main checkout's `Library` (where Unity last built). `Library` is ignored by git.

Run only the Hell tests with `dotnet test core-tests/Skyline/Skyline.Tests.csproj --filter FullyQualifiedName~Hell`.

## Unity steps for the main session

1. **Build the scene.** Use the menu **JamKit > Hell > Build the Hell scene**, or run this command. It lints the 18 rooms,
   builds the scene and exits 0:

   ```
   Unity.exe -batchmode -projectPath . -executeMethod JamKit.SkylineRepo.EditorTools.HellScene.BuildFromCommandLine -logFile -
   ```

   Look for `HELL_LINT 0 problems` and `HELL_BUILD built ...`. The Build Settings now list `HellDescent.unity` first.
2. **Play `HellDescent.unity`.** The console logs `HELL_RUN start seed ... deck ...`, then `HELL_ROOM begin ...`.
3. **What to check:**
   - **The first room:**
     - you drop into the vestibule;
     - the objective line shows at the top;
     - the imps spawn (`HELL_ROOM spawn`).
   - **Hazards:**
     - lava burns, and 1 s in it sinks you back to solid ground;
     - collapse platforms shake, fall and return;
     - the beam lanes fire.
   - **Objectives:**
     - the objective done opens the red exit gate;
     - the corridor's hole drops you into the next room;
     - the Brand: the 20 m ring glows round the pool; the HUD tow ring shows inside it and fills slowly standing
       (30 s), about 3x as fast lapping at a sprint (the line shows "x3.0"); the ring's glow and the HUD halo pulse
       faster at speed;
     - The Collapse: the debris shadows glow on the floor 1 s before a rock lands (`HELL_ROOM debris hit` when it hits),
       the imps appear at the side walls at 3, 6 and 9 s, the fins stay standing when their tiles fall; kicking up the
       hall's stalactites takes you onto the bridge at about 20 m/s and the central lane holds; sprinting below them, the
       central lane is already falling when you get there; a tile falling under you drops you into the lava and the room
       restarts (`HELL_ROOM sank (lava)`);
     - between rooms: run into a drop hole and you land running in the next room (`HELL_RUN carried N m/s`);
     - the open rims: low parapets beside the safe walkways, open cliff edges elsewhere, the sky and spires beyond, the
       lava sea far below; jump off an edge and you are put back at the last safe spot;
     - the lantern rides in front of the camera, and you are slower;
     - anchors take damage only from up close.
   - **Deaths and floors:**
     - a death restarts the room (the "AGAIN" banner);
     - after 4 rooms you fall down the shaft;
     - with the abilities merged, the skill pick opens.
   - **The ink look:** this scene does not run the Skyline ink conversion; it has plain URP materials. Check that the
     lava's HDR glow reads without bloom.

## Merging

- **`hell-enemies`** is already merged here.
- **`hell-abilities`:**
  - delete `Scripts/Hell/HellAbilityStubs.cs` (or define `HELL_ABILITIES`);
  - delete its `Common/HellRunEventsStub.cs` (under `#if !HELL_ROOMS`): this branch's `HellRunEvents` is the real one.

  `HellRoomFlags.Carry` already sets `HellAbilities.ShadowStepBlocked`, so no extra wiring is needed. `HellRewind`
  should drop its history on `HellRunEvents.RoomRestarted`.

**Done in HellDescent (2026-10-17):** both stubs deleted; `HellRewind.ClearHistory` runs on `RoomRestarted`; `HellRoom`
no longer calls the removed `HellRewindLedger`; the ooze wells count `Alive` bots instead of dropping dead ones (a
Rewind can bring one back). See `docs/HELL-INTEGRATION.md`.

## What's left (honest list)

**Not checked in play:**
- Nothing has run in Unity yet: no play test, no captures.
- Every room's fast line is simulated leg by leg in the movement engine ("The traversal simulator" below), with
  margins: all three floors, full and 90% speed, each press 0.1 s early or late. That is `BoxWorld`, not Unity's
  CharacterController, and scripted input, not hands. Worth a human try first, the tightest legs:
  - the Sigils' C-D: landing on the first dark perch at a run and stepping straight away (you keep your speed, so a
    late step runs you off the 4.5 m perch);
  - the Sigils' E-F: the turn into the pit-wall corridor and the early wall kick (0.25 s after the catch) that clears
    the pit wall;
  - Break the Chains: the west wall's wall jump onto the middle pillar (jump off by about z 19.5);
  - the Brand's dark pillars: step onto the first at a run, then two hops on landing (3 m pillars).
- The Collapse (rooms3) and the Brand's racing line ARE simulated, in the pure movement engine on `BoxWorld` (not Unity's
  CharacterController), with scripted inputs. The Collapse's margins (see its table): the kick route keeps 0.22 s on
  floor 3 at the bridge's start; a slide-and-hop runner (no kicks) misses the fast lane by only 0.01 s on floor 1, so a
  better slide-hopper may make it there (lower `CollapseFastDelay` or raise the fast front to tighten; each 0.1 s off
  the delay costs the kick route 0.1 s of its lead). The kicks are the stiffest input in the game: each must be pressed
  within the wall-kick window of touching the next stalactite (a press 0.1 s early is a double jump, not a kick: the fast-
  line margin test only proves that a fumbled chain still lands you safely in the hall). If kicking in Unity feels too
  hard, widen the rows' gap (`CollapseStalactiteHalfGap`) or add a stalactite pair.
- The Collapse's race sims don't play the debris sidesteps; each costs a little.
- The Brand's wall-run walls are not run in the sims (the lap is a sprint on the ground); a wall-run there counts at
  8.64 m/s (full rate), and the ring reaches 4.5 m up.
- Debris only hurts the player (not bots), and hits whoever is under it on the landing step (no knockback).

**Layout:**
- The Sigils' safe path is now 414 m (about 44 s of sprinting) against a 276 m fast path (0.67x). The new walkways
  are plain stone floors and ramps (one material); check they read as the safe way and don't pull the eye off the
  fast line.
- The Brand's ring is 48 flat glowing segments on the island's track (the island is 24 m square, the ring 20 m across);
  the old 6 m disc is gone. Check it reads as one ring.
- Floor 3's "rotation" is only world orientation.
- The open rims (rooms3) are unplayed: check that scythe demons and oozes don't walk off a cliff edge (their nav is built
  from the room's solids, so an open edge is just the end of the walkable floor), that the autopilot's safe route never
  touches an edge (the lint keeps the safe path 2.5 m off any unguarded rim), and how a fall reads (about a second of
  falling toward the glowing sea before you are put back).

**Enemies:**
- The enemies spawn at fixed points (no door telegraphs).
- The ooze is the floor-care bot; walk graphs are built per room on `Begin`.

**Rewind and the run:**
- ~~A Rewind across a room boundary is not handled~~ and ~~there is no title screen~~: done on `hell-flow`
  (`docs/HELL-FLOW.md`: the Rewind's history clears at every room boundary; title to title flow).
- `HellHudLine` text is a draft.
