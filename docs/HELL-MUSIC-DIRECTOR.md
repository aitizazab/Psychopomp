# HellDescent music director (built 2026-10-18)

The music plan is `HELL-MUSIC.md`; this is the code that plays it.

| Part | File |
|---|---|
| The pure rules (manifest reader, the cell with hysteresis, bar-line maths, fades, modifiers, stage to cue) | `Packages/com.vibejam.jamkit/Runtime/Skyline/Hell/HellMusic.cs` |
| Their tests (13) | `Packages/com.vibejam.jamkit/Tests/Skyline/Hell/HellMusicTests.cs` |
| The player (MonoBehaviour, boots itself) | `Assets/JamKit/Templates/SkylineRepo/Scripts/Audio/HellMusicDirector.cs` |
| The manifest (the composer agent's) | `Assets/JamKit/Templates/SkylineRepo/Resources/SkylineAudio/hell-music/hell-music-manifest.json`, loaded as `Resources.Load<TextAsset>("SkylineAudio/hell-music/hell-music-manifest")` |

`HellMusicDirector` makes itself in any scene with a `HellRunDirector`, `HellVesperTestBed` or `HellEndingTestBed` (like
`HellSoundBinder`), survives scene loads, and adds no code to the other lanes' scripts: it watches public state and listens
to `HellRunEvents`.

## The manifest

The composer's schema (v1): `sample_rate`, then `cues.<id>` with `kind` (`layered_loop`, `loop`, `parts`, `stinger`,
`oneshot`), `status` (`ready` / `pending`), `resource` (a Resources path, no extension), `bpm`, `beats_per_bar`, `bars`,
`loop_samples`, `duck_db`, and either `stems[]` (`id`, `mood`, `speed`, `role` = bed / drums / drone, `resource`) or
`parts{}` (`kind`, `role`, `phase`, `resource`). **A cue whose status isn't `ready` is skipped** (it falls back, below); a
stem whose file is missing is left out of the set. The manifest is read at boot and again on each return to the title.

## In-run (the 9-cell set)

- All stems of `inrun` (19: 3 moods x 3 speeds x bed + drums, and `dark_drone`) are scheduled on one `dspTime`
  (`AudioSource.PlayScheduled`, `clip.loop`): the files are exactly `loop_samples` long, so they stay sample-locked. Mixing is
  gain only, never a restart.
- **Mood** (`HellMusicCellRules.Wanted`): **Danger** when health (plating + chassis) < 35%, the Brand or the Collapse is at
  85%+ progress, the hour has < 60 s left, or the escape / boss fallback forces it; **Normal** when an enemy is awake (alert,
  telegraph, recover) or a room objective is running; else **Quiet**. Up at once; down only after 2 calm bars.
- **Speed**: the player's horizontal speed smoothed over 0.5 s; Fast from 11 m/s, Flying from 18 m/s, each dropping only
  2 m/s under its threshold; falling (a drop hole, a shaft) is Flying. Speed is additive (Fast = still + fast stems).
- **Switching**: stems whose mood changes fade on the **next bar line** (into Danger 1 s, out of Danger 3 s, Quiet/Normal
  2 s, equal power); speed stems fade on the **next beat** (in 0.5 s, out 1.5 s). Never mid-bar.

## Modifiers

| Modifier | Source | Effect |
|---|---|---|
| Dark | `HellShadowAudio.Depth` | drums stems x `DarkHush.MusicDrums`, drone = `DarkHush.MusicDrone`, low-pass `DarkHush.MusicCutoff` |
| Time Dilation | `HellAbilityAudio.Pitch` | pitch 1 - 0.15 x the world's slow-down (0.35x world = 0.90), low-pass toward 700 Hz |
| Pause menu | `SkylinePauseMenu.IsOpen` | `baja_hold` from its start, the game music on under it at -14 dB and 600 Hz (sources ignore the listener pause) |
| Skill pick | `HellSkillPick.Open` | the in-run set out over 0.5 s (its clock keeps running), `baja_gift` in; reversed after |
| Stingers | `duck_db` | the loops duck by it for the stinger's length |

The lowest cutoff wins. Each music source has its own `AudioLowPassFilter` (there is no AudioMixer asset).

## By stage (`HellMusicStageRules`)

| Stage | Cue | While pending |
|---|---|---|
| Title | `title` | silence |
| Opening | `baja_opening`: its `sister` loop under the cards; on the hand-over to play, its `fall` part (below) | silence |
| FloorIntro / Playing | `inrun`; floor cards `floor_card_1..3` on `FloorStarted` | silence |
| Boss | `vesper_boss`: the phase's bed + drums on one clock, switched on the next bar (1 s) at Vesper's phase change; `kneel` when she's defeated | `inrun`, Danger |
| Ending: Decision / AfterChoice | `vesper_decision` `hold`; `kill` (the loop stops dead) or `spare` when decided | silence |
| Ending: ToEscape / Escape | `inrun`, Danger at least, speed from the player | |
| Ending: Out / Cards | `ending_kill` / `ending_spare_kept` / `ending_spare_cost` by the ending id (`EndingReached`) | `baja_stats`, then `title` |
| Credits | `credits` | `title` |
| Stats | `baja_stats` | `title` |
| Jobs | `baja_job_brief` / `_done` / `_failed` on `HellRunEvents.JobOffered/JobDone/JobFailed` | skipped |

Not wired yet (no cue or no hook): the death sting (the SFX covers it), Vesper's history cutscene parts and the opening's
page parts (they need page-change hooks from the cutscene player), the boss Rewind-tell effect, `baja_cliffhanger`.

## The opening's fall (round 3)

`baja_opening.parts.fall` is a `stinger_into_cue` (`into_cue: inrun`, `downbeat_sample: 170667` at 48 kHz). When the stage
goes from the opening to play, the director schedules the stinger at dspTime T (`PlayScheduled`), stops the opening's loop at
T, and starts ALL in-run stems at T + 170667/48000 (3.56 s) at full gain on the cell the rules give (Quiet or Normal, Still,
after `rules.Reset()`): the stinger's hit lands on the set's first downbeat and rings over it. Floor 1's card stinger is
skipped (the fall is that arrival). Without the part (or a pending opening) the set starts as before.

The opening's music does NOT follow the cards yet (sister -> hall -> wait -> idea): `HellScreens` keeps its card index
private and raises no per-card event, and the screens are another lane's file. The hook it needs: a
`public static event Action<int> OpeningCard` (or a public `CardIndex`) on `HellScreens`; the director would then map card
numbers to parts and switch loops on their bar lines (`bar_seconds` per part is in the manifest).

## Rules kept

- **Volume**: `SkylineMix.CategoryGain[Music] x SkylineMix.UserGain(Music)`: the pause menu's Music slider, and the
  clean-picture switch (`HellCleanPicture.Gain`, `HELL-CAPTURE.md`) for the trailer stems.
- **Rewind**: the music does not rewind. A Rewind is game time; the music runs on the audio clock, and its state (mood,
  speed) just follows the restored game the next frame. Room restarts don't restart it either.
- **No per-frame allocations**: voices are made at boot and on a manifest reload; the frame only sets volume, pitch and
  cutoff (written only when they change). The bar-line maths assume pitch 1; under Time Dilation the fades land slightly
  off the bar (the sources stay locked to each other).
- **Memory**: 19 stems x 28.4 s stereo. Set the stems' import **Load Type** to *Compressed In Memory* (not *Streaming*: 19
  streams can't be scheduled together reliably) when Unity makes their metas.

## Checks in Unity (not run here)

1. Open the game scene: a `HellMusicDirector` object appears with `music-*` children (one per ready stem + beds, overlay, 2
   stingers). Console clean.
2. Title -> Start -> floor 1: the floor card stinger over the in-run set; standing still in an empty room is Quiet Still;
   sprint/slide past 11 m/s adds the fast stems on a beat; an enemy alerting moves to Normal on the next bar; dropping
   below 35% health goes to Danger on the next bar and stays 2 bars after healing.
3. Hide in the dark: the drums drop, the drone comes up, the music darkens. Time Dilation: a slight pitch drop and muffle.
4. Esc: Baja's hold loop from its start with the game music faint underneath; close: back. Skill pick: Baja's gift loop.
5. Vesper (chapter 3 from the title): phase changes switch on a bar line; her kneel stops the loops. Decision: shoot =
   the loop cuts and the kill sting; offer a hand = the spare sting. The escape plays the Danger set.
6. Pause menu Music slider at 0: silence. Clean-picture "voices only": no music.
