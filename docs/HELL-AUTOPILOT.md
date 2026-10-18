# Hell: the autopilot and the stability sweep (branch `hell-autopilot`)

Built 2026-10-18 at night (lane B of `HELL-GAPS.md`, gaps #1, #15 and #26). The autopilot plays the built game like a
person, from the title (or the first room) to the end of the run. It does each room's objective along the room's own safe
or fast path, using the real player's inputs. It writes a report per room. It also runs a script of Rewinds at the edge
cases and checks what each one left behind.

**Not yet run in Unity.** The pure tests and the Unity-side compile check pass. The first sweep on a player build is the
first real run (see "Known limits").

## Files

| File | What it holds |
|---|---|
| `Packages/com.vibejam.jamkit/Runtime/Skyline/Hell/HellAutopilotPlan.cs` | The pure half. `HellAutopilotPlan.Build(room, route)` makes the plan. `ResumeIndex` says where to pick the plan up again. It also holds `HellProgressWatch` (the stuck watch), `HellFrameStats` (frame times), `HellRewindChecks` (the Rewind edge-case rules) and `HellAutoReport` (the JSON and the summary line). |
| `Assets/JamKit/Templates/SkylineRepo/Scripts/Capture/HellAutopilot.cs` | The Unity half: the player's hands, the fight, the tasks, the watch, the Rewind script and the log. It adds itself after the scene loads when `-hellAutopilot` is on the command line. No scene or builder change is needed. |
| `Packages/com.vibejam.jamkit/Tests/Skyline/Hell/HellAutopilotTests.cs` | 15 tests (below). |

## The flags

| Flag | What it does | Default |
|---|---|---|
| `-hellAutopilot safe` / `fast` | Turns it on and picks the route. `fast` takes each room's fast path (jumps, slides, Shadow Steps, wall-runs, the brand's laps). It falls back to the safe path in that room after two sinks or two failed Shadow Steps, when it gets stuck, or when the fast plan ends with the objective undone. | off |
| `-hellSeed N` | The run's deck (the director's own flag). Seeds **1 and 2 together deal all 18 rooms.** | random |
| `-hellAutopilotLog <path>` | Where the JSON report goes (relative to the working folder). | `out/hell/autopilot.json` |
| `-hellAutopilotRewind` | Runs the Rewind edge-case script (below). Turns off the autopilot's own reflex Rewinds, so the cooldown is free for the cases. | off |
| `-hellAutopilotStuck S` | No progress for S seconds counts as stuck. Progress is either moving more than 1.5 m or the plan or objective moving on. | 10 |
| `-hellAutopilotSkipAfter S` | After S seconds stuck in one room, it teleports into the room's drop hole and marks the room `skipped`. | 120 |
| `-hellAutopilotNoSkip` | Never skip a room: a softlock then runs until the time limit. | off |
| `-hellAutopilotMinutes M` | Time limit for the whole run. It ends with `result=timeout`. | 75 |
| `-hellAutopilotToCredits` | Plays on after the rooms: it fights the boss on the spot and presses Enter on screens. It ends on the flow lane's `HELL_STATS` log line. **Use it only after the flow lane is merged.** Without flow, the end banner's Enter reloads the scene. | off (ends at `RunFinished`) |
| `-hellChapter 1\|2\|3\|4` | Starts that chapter instead of the full run (as the title's CHAPTERS menu counts them; 4 is the Vesper fight). The skipped floors' skill picks are a seeded choice (from `-hellSeed`). With 4 the autopilot plays on to the credits by itself (`-hellAutopilotToCredits` implied): it strafes and shoots in the fight and the decision takes the scripted choice, `-hellEnding spare\|kill` (default kill). `-hellErrands yes\|no` answers the Vesper errands question (default yes); `-hellDifficulty easy\|normal\|hard` (default normal). | full run |
| `-hellSkipTitle` | The flow lane's switch to skip the title. Not needed with the autopilot: it takes the player's input before the director starts, and a scripted player skips the screens by itself. Harmless to add. | |

With the capture lane's switches:
- `-hell-clean` gives a clean picture.
- `-hell-rec <name>` records the autopilot's run as a replay. Its Shadow Step and Rewind presses go through the
  bindings, so the recorder sees them.
- **If you add any `-skylineOut`, `-skyline-perf` or `-skyline-demo`, also add `-hell-abilities`.** Otherwise the abilities
  don't attach in those scripted modes.

## What it does in a room

**The plan** (`HellAutopilotPlan.Build`) starts from the room's safe or fast polyline, sampled every 0.5 m:
- **Ground under each sample:** a solid, a collapsing platform or a timeline tile within 1 m below to 0.6 m above.
  Climbs and short drops on the path count as ground.
- **A stretch with no ground is a gap.** It is cut at its authored corners, and each leg becomes one of:
  - a **Shadow Step**, if the leg ends on the gap's dark landing within 17 m of the eye;
  - a **wall-run**, if most of the leg runs beside a runnable wall;
  - otherwise a **jump** (sprint, jump at the edge, double jump at the apex).
- **On ground,** a leg is a run, a **climb** (more than 0.6 m up within 4 m: jump, double jump, hold forward to
  mantle), a **slide** (a beam crosses it) or a **drop**.
- **The tasks sit on their best samples:**
  - each sigil, the lantern and the altar: in order, the first sample in reach;
  - each anchor: the first sample on ground at least 1 m inside its 12 m ward, measured from eye height;
  - each well: the nearest sample on ground within its 3.5 m;
  - the brand: on the safe route, the sample in the ring next to a stone (hold); on the fast route, the first sample in
    the ring (lap the racing line).
- The plan ends at the middle of the drop hole.

**The hands** are the real player's. The autopilot drives `SkylinePlayer.ScriptedInput` with movement, sprint, jump,
crouch/slide, fire, reload and swap.
- **The view** turns like a wrist (`HumanAim`, as the Skyline demo does):
  - a reaction beat of 0.16-0.3 s on a new target;
  - a turn capped at 650 degrees a second in a fight and 280 when calm;
  - a small overshoot that settles.

  It never snaps. When not fighting it looks about 4 m ahead along the plan. A turned view strafes, as a player does.
- **Shadow Step, Rewind and the skill-pick cards** are key presses on a virtual keyboard added to the Input System. They
  go through the game's bindings and screens exactly as a player's keys would.
  - **The Shadow Step:** it stops at the edge, turns to the landing, holds the step key for at least 0.35 s until the
    aim has settled and the game's own preview is valid, then releases.
  - **If a key isn't read,** the log says `HELL_AUTO keys fallback` once, and the same game methods are called directly.

**The fight:**
- **Target:** the nearest live bot whose centre the gun's own ray reaches. The aim point has a spread of 0.45 of the bot's
  radius, re-rolled every 0.3-0.6 s.
- **Firing:** only while the crosshair's ray lands on the target, at 0.11-0.24 s between presses.
- **Reloading:** when dry. It swaps guns when out of ammo.
- **When it doesn't fight:**
  - in The Collapse;
  - on a gap's leg, or within 8 m of its takeoff;
  - in the air;
  - on the fast route, except at task points.
- **The Rewind reflex:** 45 damage within about 1.5 s with the Rewind ready makes it press Rewind (`HELL_AUTO rewind
  reflex`).

**Tasks:**
- **Stops** (sigils, lantern, altar): it waits up to 2.5 s for the objective to count the touch, then walks onto the
  stop's exact spot.
- **Anchors:** it shoots from the spot, strafing.
- **Wells:** it strafes inside the seal radius, shooting oozes.
- **The brand:** on the safe route it holds behind the stone. On the fast route it sprints the racing line, with an
  occasional slide, until the brand is set.

**Getting back on track:**
- **After a sink, a Rewind or a route switch,** it carries on from the plan point nearest the feet. That point lies
  between the last task done and the next one still to do, and is never on the far side of a gap.
- **After a death,** it starts the room's plan again.

## What it logs

| Line | When |
|---|---|
| `HELL_AUTO start mode safe seed 1 ...` | at load |
| `HELL_AUTO room 1.2 OozeWells entered` | each room (floor.room) |
| `HELL_AUTO task Seal 1 done [1.2 OozeWells] after 3.1 s` | each task |
| `HELL_AUTO room [1.2 OozeWells] stuck <action> 10 s at (x, y, z) point 7/18 route Safe` | no progress for the stuck time: the position, the room, the last action |
| `HELL_AUTO route safe [..]: <why>` | a fall back from the fast route |
| `HELL_AUTO skip [..]: teleport to the drop hole after 120 s stuck` | a skipped room (the sweep goes on) |
| `HELL_AUTO room 1.2 OozeWells ok 41.3 s deaths 0 sinks 1 route safe fps 144 (1% low 97)` | leaving a room |
| `HELL_AUTO rewind <case> [..] ok (...)` or `... PROBLEMS: ...` | each Rewind case |
| `HELL_AUTOPILOT done rooms=12/12 stuck=0 skipped=0 deaths=1 errors=0 rewindProblems=0 result=finished mode=safe seed=1` | the last line; then the player quits (exit 0 finished, 2 timeout or skips) |

**The JSON** (`-hellAutopilotLog`) has a record per room:
- floor, room, kind and route;
- entered, objective time and seconds;
- whether the objective was done and whether the room was skipped;
- deaths, sinks, Rewinds, Shadow Steps and route switches;
- mean fps, 1% low fps and the worst frame;
- the stuck entries, the errors, and notes (task timeouts, refused steps, plan notes).

At the run level it also has every Rewind case's line, the Rewind problems, and up to 200 errors and exceptions from
the Unity log, each tagged with its room. Errors raised by the autopilot itself are not counted. They are logged once as
`HELL_AUTO internal ...` and noted in the room.

## The Rewind edge cases (`-hellAutopilotRewind`, gap #15)

Each case runs once, at the first chance in the run, with the cooldown ready:

| Case | When | Expected |
|---|---|---|
| `room-first-second` | 0.6-1.2 s into the second room or later (the history still holds the previous room's corridor) | It goes off. The room clock goes back by the history, and the player stays in this room. |
| `lantern-carry` | 1.5 s after taking the lantern (so 4 s back is before the pickup) | Objective and player agree: `HellRoomFlags.CarryingLantern` matches the objective's state |
| `during-collapse` | The Collapse, 5 s in | Clock back 4 s, the player not inside a solid |
| `mid-shadow-step` | Holding the step key 0.45 s, then Rewind while still aiming | The aim is cancelled by the Rewind (`aimingAfter` false). It notes whether a step went off on the release afterwards. |
| `across-death` | The script ends a life (a 100000 damage `TakeAttack`, labelled `autopilot-rewind-case`), waits 0.6 s after the restart, then Rewinds | It goes back only the 0.6 s since the restart (the history is dropped on `RoomRestarted`). Never back past the death. |
| `during-pick` | The skill pick open, 0.8 s in | It does **not** go off. After the pick, time runs again and no late Rewind fires. |

**After each case** (0.15 s later) it records these, and `HellRewindChecks.Problems` lists anything inconsistent:
- the room clock before and after, against the expected rollback (the Rewind's seconds, or the history if shorter);
- the objective's progress (a Rewind never moves it forward);
- the lantern flag against the objective;
- the player inside a solid (a capsule overlap against the world);
- the player outside the room's bounds;
- doubled bots (two of a kind within 0.3 m);
- more live bots than at any time in the last 6.5 s;
- the time scale stuck at 0 with no screen open.

## Command lines (from the HellDescent folder, on the player build)

`$exe` is `out\hell\build\HellDescent.exe`. `-batchmode` keeps the window off the screen (the creator's no-focus-stealing
rule) and still renders with graphics. Frame times in batch mode are indicative only. Measure fps in a windowed run
after bedtime: replace `-batchmode` with `-screen-fullscreen 0 -screen-width 1920 -screen-height 1080`.

```
# safe sweep: seeds 1 and 2 together deal all 18 rooms
& $exe -batchmode -logFile out/hell/auto-safe-1.log -hellAutopilot safe -hellSeed 1 -hellAutopilotLog out/hell/auto-safe-1.json
& $exe -batchmode -logFile out/hell/auto-safe-2.log -hellAutopilot safe -hellSeed 2 -hellAutopilotLog out/hell/auto-safe-2.json

# fast sweep (falls back to safe per room where the fast line fails; the JSON says where)
& $exe -batchmode -logFile out/hell/auto-fast-1.log -hellAutopilot fast -hellSeed 1 -hellAutopilotLog out/hell/auto-fast-1.json
& $exe -batchmode -logFile out/hell/auto-fast-2.log -hellAutopilot fast -hellSeed 2 -hellAutopilotLog out/hell/auto-fast-2.json

# Rewind edge cases (seed 2 deals The Collapse and the Lantern on floor 1, so every case comes early)
& $exe -batchmode -logFile out/hell/auto-rewind.log -hellAutopilot safe -hellSeed 2 -hellAutopilotRewind -hellAutopilotLog out/hell/auto-rewind.json

# one chapter: floor 2 alone, or the Vesper fight to the credits (spare at the decision, recorded as a replay)
& $exe -batchmode -logFile out/hell/auto-ch2.log -hellAutopilot safe -hellSeed 1 -hellChapter 2 -hellAutopilotLog out/hell/auto-ch2.json
& $exe -batchmode -logFile out/hell/auto-vesper.log -hellAutopilot safe -hellSeed 1 -hellChapter 4 -hellErrands no -hellEnding spare -hell-rec auto-vesper
```

**Reading a run:**
- `Select-String out/hell/auto-safe-1.log -Pattern 'HELL_AUTOPILOT done|HELL_AUTO room .* (stuck|skip)|HELL_AUTO rewind|HELL_AUTO keys'`
- then the JSON's `rooms[].stuck`, `errors`, `notes` and the fps fields.

A safe run of 12 rooms takes roughly 15-25 minutes; the Six Sigils' safe path alone is about 100 s of running.

## For judge agents: the autopilot as the demo player

- **Record a run once, then shoot it.** Add `-hell-rec auto-s1` to a safe or fast run, then replay with the capture lane's
  cameras:

  ```
  & $exe -hell-replay auto-s1 -hell-replay-from 95 -hell-replay-to 110 -hell-shot chase-high -hell-clean
  ```

  See `HELL-CAPTURE.md`. The replay is deterministic, so a shot can be retaken.
- **Find the moments** from the log:
  - the time of each room (`HELL_AUTO room ... entered` and the JSON's `entered`);
  - each Shadow Step (`HELL_STEP`) and Rewind (`HELL_REWIND`);
  - the fights (`HELL_ROOM spawn`).
- **What judges should know it is.** It is a competent, human-paced player, not a showcase:
  - it walks the authored lines;
  - it shoots at a human cadence with misses;
  - on the safe route it holds still behind the brand's stone for the slow 30 s charge.

  For B-roll of the fast lines, use `fast`. Its stuck and route-switch entries show where a fast line didn't work for it.
  Those clips are not representative, so cut around them.
- **Never judge a skipped room's footage.** The skip is a teleport, and the JSON marks the room `skipped: true`.

## Tests (`core-tests/Skyline`, `--filter FullyQualifiedName~HellAutopilot`)

**The plan, over all 18 rooms and both routes:**
- each plan starts at the entry and ends in the drop hole;
- the safe plans have no Shadow Step, no wall-run and no jump wider than the jump gap;
- every stop gets its task, in order where the room says so;
- every task point is where the objective counts it;
- Shadow Steps land in the dark within reach, and only on the fast route;
- every fast route except the Brand's and the Lantern's has a gap, beam or wall move;
- no room has a gap the planner can't cross;
- The Collapse's fast lane (rooms3) jumps the bridge's lava gap, while its safe lane is a plain run. The autopilot doesn't
  wall-kick the run-up hall's stalactites, so on the fast route it reaches the bridge at a sprint, finds it already falling,
  sinks and falls back to the safe lane (expect `route safe` for The Collapse in the fast sweep: by design, not a bug).

**Resuming:**
- it picks the nearest point between the last task done and the next one;
- it never resumes on the far side of a gap.

**The rest:**
- the stuck watch fires once and resets on progress;
- the frame stats give the mean, the percentiles and the 1% low;
- a consistent Rewind has no problems, and each kind of inconsistency is caught;
- the report counts rooms, stuck entries, skips and deaths, and writes the summary line and valid JSON.

The whole Skyline suite passed with them: 1098 passed, 3 skipped. Movement: 215 passed. The Unity-side compile check and
the per-assembly check are green.

## Known limits

- **Nothing has run in Unity yet.** The first sweep will find things: thresholds for jumps, arrival radii, the
  climb timing. The stuck lines are designed to say where.
- **The virtual keyboard is the one untested link.** It is added with `InputSystem.AddDevice<Keyboard>`, and focus is
  ignored (`backgroundBehavior = IgnoreFocus`, `runInBackground`). If its presses aren't read, the autopilot notices
  (the step aim doesn't start, or the Rewind doesn't fire), logs `HELL_AUTO keys fallback` and calls the game methods
  directly. In that mode the capture lane's recorder won't see the step and Rewind buttons.
- **Fast lines are best effort:**
  - Wall-runs are a jump toward the wall with the stick along it, leaving the catch to the movement engine; the kick-off
    is a jump the moment the next leg begins.
  - The Chains' wall-run anchor shots happen only if the view gets there in the air. Otherwise the safe route picks the
    anchors up.
  - Expect `route safe` entries in the fast sweep. They are data for the rooms lane, not bugs in the rooms.
- **The Collapse:**
  - It doesn't dodge the debris; it takes the hits.
  - On the fast lane it has no stalactite kicks (rooms3), so it loses the bridge and takes the safe lane.
- **The fight:**
  - It has no cover cycle: it fights from the path, walking.
  - It doesn't use Time Dilation, Nightfall or the other floor skills' buttons.
  - The skill pick is a random card (seeded).
- **The boss** (with `-hellAutopilotToCredits`): it stands, strafes and shoots, but doesn't move round the arena. The
  decision is the ending director's scripted choice (`-hellEnding`); it holds fire while she kneels, then plays the escape like a room and lets the ending's comic turn its own pages (docs/HELL-CAPTURE.md section 7, with `-hellInvulnerable` and `-hellBossHealth` for footage).
- **Skips are teleports.** They keep a sweep going past a softlock. Use `-hellAutopilotNoSkip` when a softlock should
  stop the run.
- **Frame times** in `-batchmode` aren't the windowed game's. Use a windowed run for the 120 fps check.
- **The flow lane's version of `HellRunDirector`:**
  - The autopilot listens only to `RoomEntered`, `RoomRestarted` and `RunFinished`, and reads `Run`, `Current` and
    `player`.
  - If flow renames those, the autopilot's `Bind` and `Update` need the new names. Nothing else depends on them.
