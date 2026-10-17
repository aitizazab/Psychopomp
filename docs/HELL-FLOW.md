# Hell: the game's flow, title to title (branch `hell-flow`)

Built 2026-10-17 night (sprint lane 1, `HELL-SPRINT.md`). The game no longer starts the run on load: it opens on a title
screen and goes title, opening, three floors, boss, ending, credits, stats, and back to the title. Not yet run in Unity:
the pure tests and the Unity-side compile check pass. The play checks are at the end.

## The flow

| Stage | What shows | Moves on when |
|---|---|---|
| Title | "HELL DESCENT" (working name), START / CHAPTERS / SETTINGS / QUIT | Start: the difficulty (BAJA'S HOUR: Easy / Normal / Hard, saved; docs/HELL-HOUR.md), then the opening. Chapters: a chapter start (below). Settings opens the kit's pause menu (Audio, Controls, Look, Subtitles, Difficulty; Resume comes back). Quit closes the game |
| Opening | The God of Death's premise: 6 text cards over black and ember | The last card, or a skip |
| FloorIntro | The floor's card: "FLOOR ONE / THE EMBER GATE" and a tagline, 2.8 s | Its time, or any press |
| Playing | The rooms, the shafts, the skill picks | The last floor's shaft |
| Boss | The boss lane's fight | `HellRunEvents.BossDefeated()` |
| Ending | The ending lane's decision scene and escape | `HellRunEvents.RaiseEnding(id)` |
| Credits | A scrolling list (hold Enter / A to speed it up) | Its end, or a skip |
| Stats | Big numbers that count up, the ending's name, Baja's hour left (or TIME OWED), CONTINUE | Enter / A / a click: the scene reloads on the title |

With no boss listener the Boss stage passes at once; with no ending listener the credits roll at once. The fallback
endings are `bottom` (no boss) and `boss-defeated` (a boss, no ending scene).

While a screen shows, the game holds still: the time scale is 0, the player's input is empty and the cursor is free.

**Controls on the screens:**
- Keyboard and mouse: arrows or W/S to move, Enter or Space to pick, click an item, Escape or Backspace to skip.
- Pad: D-pad or stick to move, A to pick, B or Start to skip.
- On the title's main list, Escape or the pad's Start / Menu opens the pause menu too (the screen opens it itself:
  `SkylinePauseMenu.Blocked` stays on over every screen). In a sub-list (the difficulty question after START, the chapters,
  the errands question) Escape / Backspace, B and Start go back a list instead, and the difficulty question has a BACK row
  for the mouse (the creator clicked START by accident and couldn't get back, 2026-10-18).
- Fixed 2026-10-18: Settings picked with A (or Enter) opened the pause menu and the same press then picked its first row,
  Resume, closing it at once (the screens run before the menu in a frame). The menu now ignores input on the frame it
  opens.

**Scripted runs** (`-skylineOut`, `-skyline-perf`, `-skyline-demo`) and `-hellSkipTitle` skip every screen. They drop
straight into the first room, show the floor names as HUD banners, and stop on the stats, which are written to the log
(`HELL_STATS ...`).

## Chapters and the opening deck (for the judges, who play a few minutes)

**CHAPTERS** on the title lists FLOOR 1, FLOOR 2, FLOOR 3, VESPER and BACK. All are open from the start.
- **A chapter skips the opening.** Floor N starts at floor N's first room, with its card.
- **Baja's hour is part spent** (docs/HELL-HOUR.md): Floor 2 a third, Floor 3 two thirds, Vesper 0.85, of the difficulty's
  budget. Chapters use the saved difficulty (only the full game's START asks).
- **Vesper** first asks whether you ran the God of Death's errands on the way down (the chapter offers no jobs; the answer
  decides the spare ending, docs/HELL-ENDING.md; Escape / B goes back to the list), then goes straight to the boss hook.
- **Skills first:** a chapter first opens the normal skill pick once per floor it skips (Floor 2: one pick; Floor 3 and
  Vesper: two). This goes through `HellRunEvents.FloorCleared(0..)`, so a `FloorCleared` listener sees those floors
  "cleared" before a chapter starts. `HellRunEvents.ChapterStarted` tells a listener which chapter it was (-1 means the
  full game).

**Floor 1 is a fixed deck** (`HellDeck.Opening`), whatever the seed: Hold the Brand, the Soul Lantern (Vesper's floor-1
beat lives in it), Break the Chains, the Collapse. The Six Sigils (long) waits for floors 2 and 3, which are still
dealt from the seed. With `-hellRandomDeck`, floor 1 is dealt too, with the Soul Lantern pinned into it.

## Frame rate

The pause menu's **Look** page has **Frame rate cap** (Uncapped, the default; 240, 165, 144, 120, 60) and **VSync**
(off by default). They are saved with the other options (`SkylineOptions.frameCap`, `vsync`) and applied at start and on
every change (`SkylineSettings.ApplyDisplay`). Scripted runs and batch mode are left alone.

## Where things are

| File | What it holds |
|---|---|
| `Runtime/Skyline/Hell/HellFlow.cs` | `HellFlow`, the state machine (pure); `HellFlowStage`; `HellEndings` (ids and draft names) |
| `Runtime/Skyline/Hell/HellRunStats.cs` | `HellRunStats` (the counters, pure) and `HellFastPathTracker` (did the player take the room's fast path?) |
| `Runtime/Skyline/Hell/HellFlowText.cs` | **Every word**: the title, the opening's cards, the floor cards, the credits, the menu words |
| `Scripts/Hell/HellScreens.cs` | The screens (UI Toolkit). The director makes them at run time on the HUD's panel settings, so the scene builder needs no change |
| `Scripts/Hell/HellRunDirector.cs` | Drives the flow and the rooms. Feeds the stats. Rewindable for the stats' world counters |
| `Scripts/Hell/HellRunEvents.cs` | The contracts (below) |
| `Tests/Skyline/Hell/HellFlowTests.cs` | 32 tests: the flow, the hooks, chapters, the opening deck, the frame cap, deaths and time, the Rewind boundary, the stats, fast paths in all 18 rooms, the words |

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

All the events below landed together in commit `6c3ba2b0`, so the other lanes only listen.

| Event | Arguments | When |
|---|---|---|
| `FloorStarted` | `int floor` | A floor's card shows |
| `RoomEntered` | `floor, room, kind` | A room is entered |
| `RoomObjectiveDone` | `floor, kind, seconds` | A room's objective is done (a Rewind can undo it; it fires again when redone) |
| `PlayerDied` | `floor, where` | A death, in any stage. `where` is the room's kind, `shaft`, `boss` or `ending` |
| `RoomRestarted` | | A room restarts |
| `RewindUsed` | | A Rewind is used |
| `ShadowStepUsed` | `from, to` | A Shadow Step is taken |
| `JobOffered`, `JobDone`, `JobFailed` | `string id` | A job is offered, done or failed |
| `ChapterStarted` | `int chapter` | A run starts from the title (-1 is the full game) |
| `StageChanged` | `from, to` | The flow's stage changes |
| `RewindBoundary` | | The Rewind's history clears |
| `FinalBossRequested` | `Action onDefeated` | After floor 3's shaft (see Boss) |
| `EndingRequested` | | After the boss is beaten (see Ending) |
| `EndingReached` | `string id` | The ending is reached |
| `RunStarted`, `RunFinished`, `FloorCleared` | | As before |

**Jobs (lane 2):**
- `HellRunEvents.Stats` is the same object as `HellRunStats.Current`. It is null before the first Start, so guard with `?.`.
- Call `HellRunEvents.RaiseJobOffered(id)`, `RaiseJobDone(id)` and `RaiseJobFailed(id)`. They count into the stats and
  fire the job events. In `HellJobs.RaiseOffered`, `RaiseDone` and `RaiseFailed` these are the one-line TODOs.
- Job counts are **not** rewound: the jobs ledger settles each job once.
- Read the counts with `JobsOffered`, `JobsDone`, `JobsFailed` and `JobsShare` (0..1).
- Other recorders: `RecordKill()`, `RecordShadowStep()`, `RecordRewind()`, `RecordDeath()`, `RecordFastPath(bool taken)`.
  The director already records kills (`SkylineBot.Wrecked`), Shadow Steps (`HellAbilities.Stepped`), Rewinds
  (`HellRewind.Rewound`), deaths and fast paths (at each room's drop). Don't record those twice.
- **Rewind-safe:** kills are saved and restored with the Rewind (the director is an `IHellRewindable`). Nothing else in
  the stats is rewound.
- Other events to hook: `RoomEntered(floor, room, kind)` and `RoomRestarted`.

**Boss (lane 3):**
- `HellRunEvents.FinalBossRequested` is an `Action<Action>`. It fires after floor 3's shaft, or at the Vesper chapter,
  with `onDefeated` = `HellRunEvents.BossDefeated`.
- `HellVesperFight`'s reflection hook already accepts that signature: `RequestFinalBoss(onDefeated)`.
- It subscribes once, statically, so it survives the reload at the title.
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
- The pause menu has **Return to title** in a run (press again within the confirm window, like Quit): it clears what
  outlives a scene load (the time scale and its requests, the Rewind's history, the jobs' ledger and the Vesper chapter's
  assumed answer, the music director, which a fresh scene boots again) and reloads the scene, as the end of a run does
  (`HellRunDirector.ReturnToTitle`, logs `HELL_FLOW return to title (pause menu)`). And **Restart room** while a room is
  being played (docs/HELL-ROOMS.md).
- The art pass's "slow camera across the Sigils room" behind the title is not done: the title is on its own gradient.
