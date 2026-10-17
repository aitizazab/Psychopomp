# Hell integration: how the Rewind works now

The enemies branch and the abilities branch each made the kit's bots and bolts rewindable on their own. This page is
the one design they were merged into (day 1, 2026-10-17). Where it disagrees with the "Rewind" parts of
`HELL-ENEMIES.md` or `HELL-ABILITIES.md`, this page wins.

## The recorder: `HellRewind` (Player/HellRewind.cs)

- **One recorder.** `HellRewind` lives on the player; `HellAbilities` adds it. Every fixed step it snapshots every
  `IHellRewindable` in `HellRewindRegistry.All`. Each frame is kept as (object, snapshot) pairs in a ring sized for 6 s.
- **Using it** (`TryRewind`) does four things:
  1. It notes what is listed right now.
  2. It restores the frame from `seconds` ago (4, or 6 with Deeper Rewind). It calls `Restore` on that frame's own
     objects, listed or not, so a dead enemy (inactive, unlisted) still gets its snapshot back.
  3. Anything that was listed in step 1 but is missing from the frame appeared after it. It is told so through
     `IHellRewindSpawnAware.RewoundBeforeSpawn()` and switches itself off.
  4. It clears the ring.
- **Why step 1 comes first:** a restore can list things, such as a dead enemy reactivated or a Skyline bot revived as a
  fresh one. Those must not be mistaken for things that appeared after the snapshot.
- **No more ledger.** The enemies branch's `HellRewindLedger` and `IHellRewindSpawned` are gone. Step 3 does the same
  job, so nothing else needs to be made at a room's start.
- **Room restarts** (`HellRunEvents.RoomRestarted`) drop the history (`HellRewind.ClearHistory`). A rewind can never
  reach back past a death.

## Snapshots make (almost) no garbage

- **Pooled snapshot objects.** Every rewindable takes its snapshot object from a `HellSnapPool<T>`. The pool turns over
  more slowly than the ring, so an object is reused only after its frame has left the ring.
- **Pure parts copied in place.** The pure parts (`EnemyBody`, `BotBrain`, `HoverPatrol`, the Hell attack timelines and
  so on) are copied by `StateCopy.Capture(obj, reuse)`. This refills the earlier copy in place.
- **What still allocates:** reflection boxes each value field it reads. The fix, if the profiler ever shows it: write
  typed copies for `BotBrain` and `HoverPatrol` (see `HELL-ENEMIES.md`, "Snapshot cost").

## Who is rewindable, and how

| Thing | File | Listed | Snapshot / restore | Appeared after the snapshot |
|---|---|---|---|---|
| Player | `HellAbilities` + `SkylinePlayer.SaveForRewind` | while `HellAbilities` is enabled | movement, look, health, guns, ammo (see HELL-ABILITIES.md) | n/a |
| Every bot, Skyline or Hell | `Bots/SkylineBot.HellRewind.cs` (the ONLY bot path) | while active and initialised (Init / OnEnable / OnDisable) | full state, see below | **retired** |
| Bolts (taser and ember) | `Bots/SkylineBolt.HellRewind.cs` | the pool as ONE rewindable (`SkylineBolt.Rewindable`, listed by HellRewind) | every bolt in flight; restore stops them all and flies the snapshot's again (embers as embers) | gone with the restore |
| Demon scythes | `Bots/HellScythe.cs` | while active | beam timeline (`StateCopy`), target, spin | switched off |
| Lava pools (ooze) | `Bots/HellLavaPool.cs` | while active | `LavaPool` state, target, position | switched off |
| Beam lanes | `Bots/HellBeamLane.cs` | while active | its clock | n/a (placed by rooms) |
| Arena / Relay / repossession directors | `*.HellRewind.cs` partials | found by HellRewind every 2 s | phases, wave clock, floor clock | n/a |
| Hell rooms, lava, collapse platforms | `Scripts/Hell/*` (rooms branch) | while active | room clock and objective; lava sink clock; platform state | n/a |

## Bots in detail

- **The snapshot.** One snapshot path covers every bot. It is the enemies branch's full state: position, facing,
  velocity, health and plates, alive, brain, patrol, search, arrival, idle work, aim trail, random stream and damage
  stage. It also holds the Hell attack state: the ooze's throb and its pools, the demon's arm and stride, and the imp's
  charge.
- **An enemy killed since the snapshot comes back.**
  - **A Hell enemy** dies by deactivating, without a wreck (`HellDie`). Its restore reactivates it with the snapshot's
    health, and logs `HELL_ENEMY <id> restored alive by the Rewind`.
  - **A Skyline bot** (a non-Hell def) leaves a wreck that took its body. It is **revived**: a fresh bot of the same
    kind, seed, group and target is spawned and given the snapshot. The old bot and its wreck are retired.
    `SkylineBot.HellRevived(old, new)` tells the directors.
- **A bot spawned since the snapshot is retired.** It is switched off for good: not live, not aimable, not listed, and
  `Alive` is false. `SkylineBot.HellRetired(bot)` fires so directors and rooms can drop it. The object stays (inactive),
  so references don't break.
- **Not rewound:** the drone's hover offset, a Skyline beam or acid sweep in progress (they restart from the brain's
  state), the gondola's rail and mines, Writ spots, and burns (`HellBurn`).

## Rules for anything new

- **Implement `IHellRewindable`.** List it in `OnEnable` and unlist it in `OnDisable`. Never `Destroy` it during play:
  deactivate it instead.
- **If it can appear during play,** also implement `IHellRewindSpawnAware` and switch it off quietly in
  `RewoundBeforeSpawn`.
- **Take snapshot objects from a `HellSnapPool<T>`.** For pure objects, use `StateCopy.Capture(obj, previous)`.
- **Run on scaled time,** so Time Dilation slows it. All time-scale changes go through `HellTime`.
- **Lists of bots:** count `bot.Alive` rather than removing dead bots. A rewind can bring a dead one back (the same
  object), and a retired one reports `Alive == false`.

## Other merge notes

- **Effects class renamed.** The enemies' static effects class is now `HellEnemyFx` (`Bots/HellEnemyFx.cs`). The
  abilities' `HellFx` (`Fx/HellFx.cs`) is the component that draws the ability effects.
- **The enemy test bed's R key** now uses the real `HellRewind` for free, with no cooldown.
