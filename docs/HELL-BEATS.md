# HellDescent: story beats and tutorials (lane C)

Built 2026-10-18 (night sprint) on branch `hell-beats`. Nothing here has run in Unity yet: the pure tests and the Skyline
compile check pass; the Unity checks are at the end. **Every line is a PLACEHOLDER** written by an agent: the creator
writes all dialogue. All words live in one file, `Runtime/Skyline/Hell/HellLines.cs`, under stable keys.

## Files

| File | What |
|---|---|
| `Packages/.../Runtime/Skyline/Hell/HellLines.cs` | Every beat and tutorial line: key, speaker, text. The speakers' subtitle names (`DEATH`, `VESPER`). |
| `Packages/.../Runtime/Skyline/Hell/HellBeatSets.cs` | The story beats as data: named sets, the active one, the placeholder set. |
| `Packages/.../Runtime/Skyline/Hell/HellBeatRules.cs` | The beats' trigger rules (pure): triggers, filters, fallbacks, once per run, hold cap. |
| `Packages/.../Runtime/Skyline/Hell/HellTutorialRules.cs` | The prompts' definitions and the queue (pure): once only, priority, staleness, saved seen set, the dark-zone pick. |
| `Packages/.../Runtime/Skyline/Hell/HellPromptText.cs` | Input labels in hints, from the bindings, for the device in use (pure). |
| `Assets/.../Scripts/Hell/HellBeats.cs` | Reports what happens to the rules; plays the beat (lines, staging, hold). |
| `Assets/.../Scripts/Hell/HellTutorial.cs` | Watches the game for first times; shows the prompts. |
| `Assets/.../Scripts/Hell/HellLinePlayer.cs` | Says a line (subtitle + voice clip if present), the hint card, the letterbox bars. |
| `Packages/.../Tests/Skyline/Hell/HellBeatsTests.cs` | 29 tests. |

`HellBeats` and `HellTutorial` attach themselves to any scene with a `HellRunDirector` (no scene edit). All their UI is
built inside the HUD, so the capture lane's clean-picture switch hides it; voice clips take the Voices volume
(`SkylineMix.UserGain`), so its voices-off/only switch applies after the capture merge.

## Voice files

`Resources/SkylineAudio/hell-voice/<speaker>/<key>.wav`, speaker `death` or `vesper`, e.g.
`hell-voice/vesper/beat.vesper.f1.lantern.1.wav`. If Unity's `Resources.Load` balks at dots in a name, the player also
tries the key with `_` or `-` for the dots (`beat_vesper_f1_lantern_1.wav`). No file: the line shows as text only, for
reading time (about 15 characters a second, 2.2 to 7 s). With a file: the clip's length plus 0.35 s.

## Story beats (a generic system; the current three are placeholder data)

The creator didn't like Vesper's three moments (2026-10-18); a writing agent is drafting option sets. The system is
data-driven so a new set is a data edit.

**A beat** (`HellBeatDef`): an `id`; `when` = a list of triggers (any matches; later ones act as fallbacks); `lines` (keys
in `HellLines`, one to three); `after` (only once that beat has played); the staging; `letterbox`; `holdSeconds` and
`holdScale`. Each beat plays once per run (a new run resets). Beats queue: one speaks at a time.

**Triggers** (`HellBeatTrigger`), each with optional filters `floor` (0-based), `room` (0-3), `kind` (room kind name),
`unlessKindSeen` (no room of that kind entered on that floor yet):

| Trigger | Fires when | Extra |
|---|---|---|
| `RoomEntered` | a room is entered | |
| `ObjectiveDone` | the room's objective completes | |
| `ObjectiveProgress` | the objective's progress rises | `atLeast` 0..1 |
| `LanternTaken` | the Soul Lantern is picked up | |
| `RoomTimer` | time in the room (its clock) | `seconds` |
| `DarkZoneEntered` | the player stands in darkness | |
| `ShaftEntered` | a floor's shaft (after its 4th room) | |
| `JobDone` | a job is done | `count` (jobs done so far) |
| `BeforeBoss` | the boss is requested | |
| `RunFinished` | the run ends | |

**Presentation:** every beat says its lines (subtitles, voice if present). Staging `FigureAhead` puts a dark figure with
ember eyes on the ground about `figureDistance` m ahead of the player, facing them, for `figureSeconds` (a stand-in until
Vesper's body exists). A beat with `holdSeconds > 0` holds the player: letterbox, the player's input replaced by an
empty frame (only if nothing else drives the player, e.g. a replay), and the world slowed through `HellTime` (key
`beat`), for at most 4 s (`HellBeatRules.MaxHoldSeconds`). Beats without a hold never stop play.

**Swapping a set:** in `HellBeatSets.cs`, copy `Placeholder()` into a new method, add it to `Sets` under a name, put its
words in `HellLines.cs` under new keys, and set `ActiveName` (or launch with `-hell-beats <name>` to try one). Tests
check that every set's lines exist.

### The placeholder set (`placeholder`)

| Beat | Lines (keys) | Triggers, in order | Presentation |
|---|---|---|---|
| `vesper.f1.lantern` (floor 1: her voice in the Soul Lantern, hostile) | `beat.vesper.f1.lantern.1`, `.2` | lantern picked up on floor 1; 25 s in floor 1's lantern room; entering floor 1's last room if no lantern room came before; floor 1's shaft | voice only, play goes on |
| `vesper.f2.deal` (floor 2: her deal, her daughter, Lucifer's boredom; curious) | `beat.vesper.f2.deal.1`, `.2` | half the Sigils touched on floor 2; floor 2's shaft | figure ahead (30 m, 6 s), play goes on |
| `vesper.f3.mirror` (floor 3: she sees herself in you) | `beat.vesper.f3.mirror.1`, `.2` | floor 3's shaft (right before the boss); boss requested (a chapter start straight to Vesper); run finished (no boss) | letterbox, held 3.5 s at 0.15x |

## Tutorials (the God of Death teaches what she gave you)

Each prompt: her spoken line `tut.<id>.1` (subtitled, voiced if present), a hint card under the crosshair `tut.<id>.hint`
(short, with the control for the device in use), and for prompts that ask the player to do something, a reaction
`tut.<id>.done` when they do it (the card goes at once). Otherwise the card stays for its time. One prompt at a time,
1.5 s apart, best priority first; a request that can't show within its stale time is dropped (it may come back). None
starts while a beat speaks or the game is paused. Doing a thing before being asked marks its prompt seen (no teaching
what they know).

**Once only:** the seen set is saved in PlayerPrefs (`hell.tutorial.seen`) across runs. **Settings:**
`HellTutorial.SetEnabled(bool)` (PlayerPrefs `hell.tutorial.on`) and `HellTutorial.ReplayAll()`. Until the pause menu
has a row for them (flow lane's file): `-hell-tutorials off|on|replay` on the command line.

**Input labels:** `{ShadowStep}`, `{Rewind}`, `{Aim}` (any `BindAction` name) become the bound control in brackets from
`SkylineBindings.Table` ("[E]", "[RB / R1]", "[RIGHT MOUSE]", "[LT / L2]"); `{kbm:...|pad:...}` picks words per device.
The device is the one used last (a pad stick or button vs a key, mouse button or mouse movement), re-read every frame.

| Prompt | Keys | Trigger | Ends | Priority |
|---|---|---|---|---|
| Shadow Step | `tut.shadowstep.1/.hint/.done` | any room entry, 1.2 s in (the first room in practice). The dark zone nearest the entry (4-18 m, else the nearest) gets the Shadow Step's violet landing marker: "step into that shadow" | a Shadow Step (reaction), or 14 s | 3 |
| Rewind | `tut.rewind.1/.hint/.done` | a hit leaving plating + chassis under 45%, or the first death (1 s after the restart) | a Rewind (reaction), or 6 s | 0 |
| Time Dilation | `tut.dilation.1/.hint/.done` | 0.6 s above 8 m/s on the ground plane | dilation kicks in (reaction), or 8 s | 4 |
| Lava | `tut.lava.1/.hint` | lava underfoot or within 3.5 m | 6 s | 2 |
| Collapsing platforms | `tut.collapse.1/.hint` | standing on a collapse platform, or about to (0.4 s ahead) | 5 s | 2 |
| The two paths | `tut.paths.1/.hint` | any room entry, 4 s in (blue studs = safe, ember stripes = fast) | 8 s | 7 |
| Objective per room kind | `tut.obj.<Kind>.1/.hint` for SixSigils, SoulLantern (carrying: no Shadow Step), BreakChains, HoldBrand, Collapse, OozeWells | the first room of that kind, 2.5 s in | 8 s | 6 |
| Skill pick | `tut.skillpick.1/.hint` | the first floor cleared | 12 s | 1 |
| First job | `tut.job.1/.hint` | the first job offered | 7 s | 5 |

## Events listened to

- `HellRunEvents`: `RunStarted`, `RoomEntered`, `RoomRestarted`, `FloorCleared`, `RunFinished` (all on main).
- `HellAbilities.Stepped`, `HellAbilities.Dilation`, `HellRewind.Rewound`, `SkylinePlayer.AttackTaken`.
- Watched (read only): the run director's `Run` (phase, floor, room: the shaft), `Current` (clock, objective progress and
  done, collapse platforms, dark zones), `HellRoomFlags.CarryingLantern`, `HellLava.IsLava`, `HellDarkness.IsDark`.
- **TODO hooks waiting on other lanes** (bound by reflection, so this compiles and runs without them; replace with plain
  subscriptions after the merges):
  - `HellRunEvents.FinalBossRequested` (hell-flow) for `BeforeBoss`. Without it, floor 3's beat still plays at floor 3's
    shaft or the run's end.
  - `HellJobs.DoneCount` / `OfferedCount` (hell-jobs) for `JobDone` and the first-job prompt; polled. Better: subscribe to
    `HellJobs.Done` / `Offered`.
  - A pause-menu row "Tutorials: On / Off / Replay" (hell-flow owns `SkylinePauseMenu`) calling
    `HellTutorial.SetEnabled` / `ReplayAll`.
  - Optional: add `DEATH` and `VESPER` colours to `SpeakerColours.Of` (`HudModels.cs`); today their names are white.

## Check in Unity

1. Metas: Unity makes the `.meta` files for the 8 new `.cs` files on import (none were written by hand); commit them.
2. Log on play: `HELL_BEATS set placeholder (3 beats)`; then `HELL_TUTORIAL shadowstep` in the first room, the violet
   marker on a dark patch near the entry, the hint "HOLD [E] TO AIM ...", and after a step `tut.shadowstep.done`.
3. The hint card's place (60% down) doesn't hide the crosshair, the objective line, or the skill pick's cards.
4. The beats: `HELL_BEAT vesper.f1.lantern on LanternTaken`; floor 2's figure stands on the ground ahead and fades in
   smoke; floor 3's shaft gets the letterbox and the slowed fall, and control comes back after 3.5 s (also check that
   a death or the pause menu during the hold doesn't leave the time scale slowed or the input frozen).
5. A pad: the hint switches to "[RB / R1]" when the pad is touched, and back with the mouse.
6. Restart the game: the prompts don't come back; `-hell-tutorials replay` brings them back.
7. A voice file dropped in `Resources/SkylineAudio/hell-voice/vesper/` plays (and loads despite the dots in its name).
8. The clean-picture switch (after the capture merge) hides the hint card, letterbox and subtitles.
