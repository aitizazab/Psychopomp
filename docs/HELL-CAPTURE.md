# Hell capture: clean picture, record and replay, camera shots, frame times

The trailer tooling for HellDescent (night sprint lane 9, 2026-10-17, branch `hell-capture`). The 120 fps recorder was
ported from Skyline's `skyline-capture` branch (VibeJamKit-capture, `docs/SKYLINE-TRAILER-CAPTURE.md`); only what Hell
needs came across. Skyline's light presets, set-piece sequences and route pilot stayed behind. The items the Skyline
audit (`docs/SKYLINE-CAPTURE-AUDIT.md` there) found broken are fixed here (see "Audit items" below).

`$U` below is `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe`. Commands run from the HellDescent
folder. `$exe` is `out\hell\build\HellDescent.exe`.

## 1. The clean picture

| Switch | What it does |
|---|---|
| `-hell-clean` | No HUD and no voice lines (same as `-hell-hud clean -hell-voices off`) |
| `-hell-hud clean` / `full` | Clean hides every HUD element: crosshair, hit marker, health bar, ammo, Hell cooldown bars, objective strip and markers, the Hell objective line and banners, comic lettering, hurt vignette, subtitles. The skill pick still shows while it is open, so a run can go on. |
| `-hell-voices off` / `on` / `only` | `off` mutes the spoken lines and barks (the bed under the narrator). `only` mutes everything except them (a voices stem: replay the same take a second time and it lines up). |

The same switches are in the pause menu: **Look > HUD** (Full / Clean) and **Audio > Voice lines** (On / Off / Only
the voices). They last for the session and are not saved.

How it works: `Scripts/Capture/HellCleanPicture.cs`. `SkylineHud.Update` hides the HUD's root, and
`SkylineMix.UserGain` multiplies in the voice mode, so every sound played through `SkylineSounds`, the barks and the
teaching voice obey it. **A new sound or voice line (lanes 1, 3, 4 and 7) must take `SkylineMix.UserGain`, with
category `Voices` for spoken lines, or play through `SkylineSounds`. Otherwise this switch can't mute it.** The same goes
for HUD: a new HUD element must be built under `SkylineHud.Instance.Root` or `Safe` to be hidden. Lane 1's title,
opening cards and floor cards are screens rather than HUD, so they are left alone.

## 2. Recording the creator's play

```
& $exe -hell-rec run1                         # in the built game (best: it runs at full speed)
& $exe -hell-rec run1 -hellSeed 1234          # with a chosen run seed
```
Or in the editor: **JamKit > Hell > Capture**, then "Play and record".

- The file is `out/hell/replays/run1.hellrec`. A bare name is resolved against the working directory, so launch the
  exe from the HellDescent folder or give a full path. It is saved every 30 s and on exit, about 4 MB per 10 minutes.
- Recording starts on the frame the run starts. Each frame stores the input (stick, every button including Shadow Step,
  Rewind and the skill button), the view after the frame's look, and the feet after the frame. Skill picks and the run
  seed are stored too. Frames where the pause menu or the pick screen hold the game are left out.
- START and the title's **CHAPTERS** both start it (a chapter skips the opening, so it starts on the chapter's start).
  The file also stores how the run began: the chapter, the difficulty, the Vesper chapter's errands answer and the
  skills picked on the title before a chapter. `-hell-replay` restores them: it starts the same chapter, grants those
  skills up front and sets the difficulty and the errands answer (in memory; the saved options are untouched). Files
  recorded before this (version 1) replay as START runs.
- **A chapter from the command line** (capture and testing): `-hellChapter 1|2|3|4` skips the title and starts that
  chapter (as the CHAPTERS menu counts them; 4 is the Vesper fight), once per launch. The skipped floors' skill picks
  are a seeded choice (from `-hellSeed`, or the recording's seed). `-hellErrands yes|no` answers the Vesper errands
  question (default yes); `-hellDifficulty easy|normal|hard` (default normal; in memory only). With `-hell-rec` the
  recording starts with the chapter and stores all of it, so `-hell-replay` replays it without these switches. Works
  with `-hellAutopilot` too (docs/HELL-AUTOPILOT.md). The log shows `HELL_FLOW chapter N from the command line`.
- **Game time runs on a fixed step** (`-hell-rec-fps 60|90|120`, default 120) with the frame rate capped at that step.
  If the machine draws slower than the step, play slows down slightly. Record at the rate you will capture at: a replay
  captures only at its own rate.
- The log shows `HELL_REC armed|start|grant|saved`.

## 3. Replaying it (re-takes, other cameras, jump to a time)

```
& $exe -hell-replay run1                                  # the whole run again, then you take over
& $exe -hell-replay run1 -hell-replay-from 95 -hell-replay-to 110 -hell-shot chase-high -hell-clean
```
Or in the editor: **JamKit > Hell > Capture**, then "Play the replay" (from, to, shot, sound, clean).

- Same seed, same fixed step, same input. The effects' random numbers are also seeded (`Random.InitState`) when the
  run starts, in both recording and replay. Skill picks are made on the pick screen as they were. The view is set to the
  recorded one before each frame, so shots go where they went. If the feet drift more than 0.25 m from the recording,
  they are put back (`HELL_REPLAY drift put back`). The end-of-replay line reports the worst drift. A recording made in
  the editor and replayed in the editor should never drift. One made in a build may drift slightly.
- **Jump to a time:** with `-hell-replay-from`, the frames before that time run with every camera off and the sound
  silent. This is simulation only and much faster than real time. Then the picture comes on.
- When the replay ends, or reaches `-hell-replay-to`, a preview hands the controls back to the player, who can play on
  from there.

## 4. Camera shots

Shots are JSON files in `Assets/JamKit/Templates/SkylineRepo/HellCapture/Shots/<name>.json` (or `-hell-shot path.json`).
A shot's camera takes over the picture. The player camera, and with it the gun overlay, is switched off, and the
listener stays on the player. The shot's camera copies the player camera's renderer and post-processing. **Shot times are
take seconds:** 0 is the take's first recorded frame. They run on the take's own clock, so Time Dilation and hit stops
never stretch them.

| Field | Meaning |
|---|---|
| `follow` | `none`, `player` (their feet), `nearest-enemy` (picked when the shot starts and kept; its centre) |
| `space` | `world`, `target` (offsets around the target, turned with its facing), `targetWorld` (around it, not turned) |
| `anchorAtStart` | take the target's place once when the shot starts: a locked-off camera set where the action is |
| `followLag` | seconds of smoothing on the target (0: locked on) |
| `lookAtTarget`, `lookOffset` | aim at the target plus an offset, instead of the keys' look-at |
| `spline` | Catmull-Rom through the key positions (default) or straight lines |
| `keys[]` | `t`, `position` [x,y,z], `lookAt` [x,y,z] or `yaw`/`pitch` (degrees, pitch positive down), `fov` (vertical degrees) or `lens` (mm, full frame: 24 wide, 50 normal, 85 tight), `roll` (dutch, degrees), `ease` (`linear`, `smooth`, `in`, `out`, `hold`) for the stretch to the next key |
| `shake` | `amplitude` (degrees), `frequency` (Hz), `position` (metres), `seed`: handheld, repeatable |
| `dof` | `on`, `focusTarget` (else `distance` m), `aperture` (f-stop), `focalLength` (0: from the FOV). URP Bokeh depth of field on a top-priority volume |

Unknown keys are reported; keys starting with `//` are comments. The shipped shots are `enemy-reveal` (an enemy-card
arc round the nearest enemy, 50 to 85 mm, background soft), `chase-high` (above and behind, looking down the line
ahead), `locked-wide` (a locked-off wide with a slow push in) and `dutch-low` (a low dutch angle levelling out).

**The player has no body.** The game is first person, so outside shots show the world, the enemies and the effects,
but not the player. A third-person stand-in model is needed before chase shots can show the player moving.

## 5. Recording takes at 120 fps 1440p (the Recorder, editor only, main session)

```
& $U -projectPath . -logFile out/hell/trailer/t1/trailer.log -executeMethod JamKit.SkylineRepo.EditorTools.Recording.HellTrailerCapture.RunFromCommandLine `
   -hell-take "run1@95-110:chase-high,run1@95-110,run1@95-110::voices-only" -hell-capture-out out/hell/trailer/t1 `
   [-hell-capture-fps 120] [-hell-capture-res 1440p] [-hell-capture-codec h264|prores|prores4444] [-hell-capture-mbps N] [-hell-capture-hud full]
```
- A take is `replay[@from[-to]][:shot[:voices]]`, with times in replay seconds and voices `all`, `no-voices` or
  `voices-only`. Takes are separated by commas, and `-hell-shot` sets the shot of every take that names none.
- Each take is written as `take-<replay>[-<from x10>ds][-<shot>][-novoices|-voices].mp4` (`.mov` for ProRes). A
  `trailer.json` holds `{ok, takes, fps, width, height, codec, error}`. Exit code 0 means OK.
- Every take is the clean picture by default (`-hell-capture-hud full` keeps the HUD). The fps must be the replay's own.
- It needs a windowed editor (batch mode can't record). Run it uncapped, one GPU job at a time, after the creator's
  bedtime: the editor window takes focus. The Hell scene is built first only if it is missing.
- How a take starts exactly on its frame: the replay jumps (undrawn) to the start, then pauses the editor
  (`Debug.Break`). The capture starts the Recorder and resumes. The take is counted in replay steps, one per recorded
  frame. A take from 0 s starts at step 1, because step 0 runs before the pause.
- H.264 runs at about 155 Mbit/s at 1440p120, with a key frame each second. ProRes is near-lossless. Audio is kept as one
  mixed track. For stems, record the same take with `no-voices` and with `voices-only`: they line up.

## 6. The frame-time check

```
& $exe -hell-perf out/hell/perf [-hell-perf-target 120] [-hell-perf-speed 8] [-hell-perf-rooms all|2|SixSigils|3.Collapse,...] [-hell-perf-enemies on|off] -screen-width 2560 -screen-height 1440 -screen-fullscreen 1
```
- The run director is held back. The eye flies through each room in turn along the room's safe path (the lint's route
  from the entry through every objective stop to the exit), at eye height and 8 m/s. Each room is started as in play,
  with its enemies spawned and fighting. If the player is wrecked, they are put back up.
- It runs uncapped (v-sync off). The first 0.5 s of each room, while the room switches on and its enemies spawn, is
  reported apart as `enterWorstMs`.
- It writes `out/hell/perf/perf.json` and quits. The file has `targetFps`, `targetMs`, `quality`, `resolution`, `note`,
  `all` (frames, avg ms/fps, 1% low ms/fps, worst ms, meetsTarget), `worstRoom`, `allRoomsMeetTarget`, and `rooms[]`
  with the same numbers per room (`1.SixSigils` etc.) plus `framesOverTarget` and `enterWorstMs`. A room meets the target
  when its 1% low (the mean of its slowest 1% of frames) fits in 8.33 ms at 120 fps.
- Log lines: `HELL_PERF start|room <id>: ...|wrote|FAILED`. It also works in the editor (`-hell-perf` on the editor's
  command line, then press Play), but only a build's numbers count.

## Audit items fixed on the way

- **The shot setting only named the file.** Shots are now real cameras (section 4).
- **Lines started before recording.** Nothing runs ahead of the take: the replay starts with the run, and a capture take
  begins on its exact frame after an undrawn jump (section 5).
- **Slow motion stretched scripted waits.** Nothing scripted in a Hell take runs on game time. Shots run on the take
  clock, and the replay steps one recorded frame per frame. Slow motion in the footage is the creator's own Time
  Dilation, replayed. Extra slow motion belongs in the edit (120 fps footage plays at 0.5x as 60 fps).
- **Unseeded effects.** `UnityEngine.Random` is seeded from the run seed when the run starts, in both recording and
  replay, and the run's deck comes from the stored seed. Still unseeded: the skill pick's offer order (the replay picks by
  id, so it doesn't matter) and physics debris on the 50 Hz step.

## Code

- **Pure (core-tests/Skyline, `HellCaptureTests`, 29 tests):** `Packages/com.vibejam.jamkit/Runtime/Skyline/Hell/Capture/`
  holds `HellCaptureArgs.cs` (switches, takes, rate, size, bitrate, take clock), `HellReplay.cs` (the file, drift),
  `HellShots.cs` (shot data, sampling, lens, shake, follow, loader), `HellPerfRoute.cs` (fly-through route, report JSON)
  and `HellCaptureJson.cs` (the JSON reader, ported).
- **Unity:** `Scripts/Capture/` holds `HellCleanPicture`, `HellCaptureBoot` (the switches and the editor's hand-over file
  `Temp/HellCapture/current.json`), `HellPlayRecorder`, `HellReplayer`, `HellShotCamera` and `HellPerfFlight`.
  `Editor/Recording/HellTrailerCapture.cs` is the Recorder driver, and `Editor/HellCaptureWindow.cs` the window.
- **Hooks in existing code (small):** `SkylineHud.Update` (hide), `SkylineMix.UserGain` (voice mode), the
  `SkylinePauseMenu` rows, `HellAbilities.ScriptedButtons` (the replay's Shadow Step, Rewind and skill buttons) and
  `HellSkillPick.PickById`. The Recording asmdef gained its missing `JamKit.Skyline` reference.
- Room geometry, `HellRunDirector` and the enemies are untouched. The tools only set `director.seed` before its `Start`
  and, for the frame-time check, disable it.

## What needs checking in Unity (nothing here has run in Unity yet)

1. The import makes `.meta` files for the new files and folders. Commit them; don't hand-write them.
2. The clean picture: `-hell-clean` or the pause menu row hides everything, the pick screen still shows, and voices
   stay muted.
3. Record about 60 s in the editor window, replay it, and compare. `HELL_REPLAY done ... worst drift` should be about 0
   with no corrections, and the enemies should die the same way. Then record in the built exe and replay in the editor
   to see the drift.
4. Jump to a time: the jump runs fast, and the picture comes on at the right moment.
5. Each shipped shot over a replay, in particular the depth of field (the Ink renderer's post may differ) and whether
   copying the renderer index keeps the ink look.
6. One capture take at 1440p120: the file length matches the take and the audio is in sync (the ears skill can check).
7. `-hell-perf` on the exe at 1440p full screen: `perf.json` per room.

## What's left

- A third-person stand-in body for the player, for outside shots.
- Enemy direction for the cards: posing, the enemy's signature move on cue, the name card. Today a shot can only follow
  an enemy the replay brings on.
- Several angles from one run (today: re-run the replay per angle, which is deterministic).
- Lane 1's title screen: if it holds the run back until "Start", recording and replay still start with the run
  (`HellRunEvents.RunStarted`). The replay can't click Start, though, so the title needs to start itself when the
  `HellReplayer` is present (or under `-hell-replay`).
