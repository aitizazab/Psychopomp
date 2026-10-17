# Hell: the game's flow, title to title (branch `hell-flow`)

Built 2026-10-17 night (sprint lane 1, `HELL-SPRINT.md`). The game no longer starts the run on load: it opens on a title
screen and goes title, opening, three floors, boss, ending, credits, stats, and back to the title. Not yet run in Unity:
the pure tests and the Unity-side compile check pass. The play checks are at the end.

## The flow

| Stage | What shows | Moves on when |
|---|---|---|
| Title | "HELL DESCENT" (working name), START / SETTINGS / QUIT | Start: the opening. Settings opens the kit's pause menu (Audio, Controls, Look, Subtitles; Resume comes back). Quit closes the game |
| Opening | The God of Death's premise: 6 text cards over black and ember | The last card, or a skip |
| FloorIntro | The floor's card: "FLOOR ONE / THE EMBER GATE" and a tagline, 2.8 s | Its time, or any press |
| Playing | The rooms, the shafts, the skill picks | The last floor's shaft |
| Boss | The boss lane's fight | `HellRunEvents.BossDefeated()` |
| Ending | The ending lane's decision scene and escape | `HellRunEvents.RaiseEnding(id)` |
| Credits | A scrolling list (hold Enter / A to speed it up) | Its end, or a skip |
| Stats | Big numbers that count up, the ending's name, CONTINUE | Enter / A / a click: the scene reloads on the title |

With no boss listener the Boss stage passes at once; with no ending listener the credits roll at once. The fallback
endings are `bottom` (no boss) and `boss-defeated` (a boss, no ending scene).

While a screen shows, the game holds still: the time scale is 0, the player's input is empty and the cursor is free.

**Controls on the screens:**
- Keyboard and mouse: arrows or W/S to move, Enter or Space to pick, click an item, Escape or Backspace to skip.
- Pad: D-pad or stick to move, A to pick, B or Start to skip.
- On the title, Escape opens the pause menu too.

**Scripted runs** (`-skylineOut`, `-skyline-perf`, `-skyline-demo`) and `-hellSkipTitle` skip every screen. They drop
straight into the first room, show the floor names as HUD banners, and stop on the stats, which are written to the log
(`HELL_STATS ...`).

## Where things are

| File | What it holds |
|---|---|
| `Runtime/Skyline/Hell/HellFlow.cs` | `HellFlow`, the state machine (pure); `HellFlowStage`; `HellEndings` (ids and draft names) |
| `Runtime/Skyline/Hell/HellRunStats.cs` | `HellRunStats` (the counters, pure) and `HellFastPathTracker` (did the player take the room's fast path?) |
| `Runtime/Skyline/Hell/HellFlowText.cs` | **Every word**: the title, the opening's cards, the floor cards, the credits, the menu words |
| `Scripts/Hell/HellScreens.cs` | The screens (UI Toolkit). The director makes them at run time on the HUD's panel settings, so the scene builder needs no change |
| `Scripts/Hell/HellRunDirector.cs` | Drives the flow and the rooms. Feeds the stats. Rewindable for the stats' world counters |
| `Scripts/Hell/HellRunEvents.cs` | The contracts (below) |
| `Tests/Skyline/Hell/HellFlowTests.cs` | 29 tests: the flow, the hooks, deaths and time, the Rewind boundary, the stats, fast paths in all 18 rooms, the words |

## The words: placeholders for the creator

The creator writes all dialogue, so every story line in `HellFlowText` is marked `placeholder: true`:
- the 6 opening cards;
- the floor taglines;
- the title (`TitlePlaceholder`);
- the credits' names and the CC BY lines.

The opening's draft beats are:
1. Your sister is dead.
2. You go, furious, to the God of Death.
3. She is about to kill you.
4. She gets "the funniest idea".
5. Kill Vesper, Lucifer's living bride, to teach him a lesson, and do a few favours on the way.
6. She gives you shadow, time and a door into Hell (she has none). Your sister is down there. "Don't take too long."

**The credits** take their CC BY lines from `HELL-ART-PASS.md` section 5. Keep only the assets that actually ship.
Sound credits are a `[to fill]` line.

## Contracts for the other lanes (all in `HellRunEvents` unless named)

**Jobs (lane 2):**
- `HellRunEvents.Stats` is the same object as `HellRunStats.Current`. It is null before the first Start, so guard with `?.`.
- Record a job with `RecordJobOffered()` and `RecordJobDone()`.
- Read the counts with `JobsOffered`, `JobsDone` and `JobsShare` (0..1).
- Other recorders: `RecordKill()`, `RecordShadowStep()`, `RecordRewind()`, `RecordDeath()`, `RecordFastPath(bool taken)`.
  The director already records kills (`SkylineBot.Wrecked`), Shadow Steps (`HellAbilities.Stepped`), Rewinds
  (`HellRewind.Rewound`), deaths and fast paths (at each room's drop). Don't record those twice.
- **Rewind-safe:** kills, jobs offered and jobs done are saved and restored with the Rewind (the director is an
  `IHellRewindable`). A job done and then rewound away is undone; done again, it counts once. Deaths, Rewinds, Shadow
  Steps, fast paths and the times are never rewound.
- Other events to hook: `RoomEntered(floor, room, kind)` and `RoomRestarted`.

**Boss (lane 3):**
- Subscribe to `HellRunEvents.FinalBossRequested`; unsubscribe in `OnDestroy`, because the scene reloads at the title.
- It fires after floor 3's shaft. Its handler moves the player into the arena and starts the fight, i.e.
  `Begin(onDefeated: HellRunEvents.BossDefeated)`.
- Call `HellRunEvents.RaiseRewindBoundary()` after moving the player.
- A player death during the Boss or Ending stage is yours to handle (`player.Wrecked`). The director only counts it.
- With no subscriber, the run goes straight to the ending.

**Ending (lane 4):**
- Subscribe to `HellRunEvents.EndingRequested`. It fires after `BossDefeated()`.
- Run the decision scene and the escape, then call `HellRunEvents.RaiseEnding(HellEndings.Kill / SpareKept / SpareCost)`.
  That rolls the credits, then the stats, then the title.
- The spare's outcome reads `HellRunEvents.Stats.JobsDone` and `JobsOffered`.
- Rename the endings in `HellEndings.Title`.

**Anyone:**
- `HellRunEvents.StageChanged(from, to)` and `HellRunEvents.Stage`.
- `SkylinePauseMenu.Blocked` (the pause binding doesn't open the menu) and `SkylinePauseMenu.OpenMenu()`.

## The Rewind across rooms: decided

**The history clears at every boundary**, so a Rewind clamps to the room's start. `HellRunEvents.RewindBoundary`
fires on:
- a room entered or restarted (a death, or a sink in The Collapse);
- a shaft;
- every stage change past the title.

`HellRewind` clears its ring on it (it used to clear only on `RoomRestarted`). A Rewind asked for 4 s back one second
into a room restores the room's first recorded frame, never the previous room (tested with the real `SnapshotRing`).

**Two more fixes:**
- **The room's spawn counter rewinds.** `HellRoom` snapshots `_spawned`, so a bot re-spawned after a Rewind gets the
  same seed.
- **Snapshots allocate less.** `HellRoom`, `HellLava` and `HellCollapsePlatform` now fill pooled snapshot objects
  (`HellSnapBox<T>` in `HellRewind.cs`) instead of boxing a struct every fixed step.

**Still allocates:** the objectives' own `Snapshot()` (`HellObjectives.cs`, the rooms lanes' file) boxes small values
every step, and the Ooze Wells clone two arrays. That is about 1-3 small objects per active room per step.

## Fast paths: how "taken" is measured

`HellFastPathTracker` works like this:
- It samples the room's `fastPath` every 2 m.
- It keeps the samples more than 5 m from every point of the `safePath` (the stretches that are the fast path's own).
- It marks a sample visited when the feet pass within 3.5 m.
- The fast path counts as taken when at least half of its own samples were visited.

It resets on a death, so the attempt that finished the room is the one that counts. Every room offers one fast path,
so the stats show taken out of rooms cleared.

Tested in all 18 rooms: walking the fast path counts as taken; walking the safe path visits none of the samples.

## What needs checking in Unity (main session)

1. **Compile.** The scene needs no rebuild for the flow: the screens are made at run time.
2. **The title:**
   - it shows on Play;
   - the gradient and the embers draw (the gradient is a custom mesh in `generateVisualContent`; check its winding if
     the background is missing);
   - it works with the mouse (hover and click), the keyboard and a pad;
   - Settings opens the pause menu and Resume returns;
   - Quit stops play mode.
3. **The pause menu now exists in `HellDescent`.** It used to create itself only in scenes named `Skyline*`.
   Escape opens it in play.
4. **Opening cards:** they fade in and out, Enter advances, Escape skips. Then the Ember Gate card, then the first room.
5. **Floor cards** after each skill pick.
6. **After floor 3's shaft** (with no boss lane merged): the credits roll at once, then the stats. Check the numbers
   look right: the time per floor, deaths, Rewinds, Shadow Steps, fast paths x/12, kills. CONTINUE reloads to the title.
7. **The Rewind:** within 4 s of entering a room, a Rewind keeps you in the room (`HELL_REWIND back` with no teleport
   to the last room).
8. **The cursor:** free on the title and the stats, locked again in play.
9. **The log:** `HELL_FLOW <from> -> <to>`, `HELL_RUN fast path taken|not taken (n/m)`, `HELL_STATS ...`.

## Not done

- The title uses Anton (in the kit), not Pirata One (the art pass's pick, not downloaded).
- There is no title music or menu sounds yet: that is the sound lane's job. Note that `SkylineMixer` and
  `SkylineQuality` still act only in `Skyline*` scenes.
- There is no "quit to title" in the pause menu.
- The art pass's "slow camera across the Sigils room" behind the title is not done: the title is on its own gradient.
