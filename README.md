# PSYCHOPOMP

*A first-person movement shooter about a descent into Hell. Made in Unity 6 (URP) for the **Vibe Coding GameDev**
event, October 2026. Themes: **Fire, Shadow, Time**.*

Your sister is dead, and she is in Hell. You go to Baja, the God of Death, in a rage, ready to do anything. Baja has
no way into Hell herself, but she has an idea she finds very funny: she lends you her powers over shadow and time and
sends you down to kill Vesper, Lucifer's living bride, and bring back her soul. Do Baja's errands on the way, find
your sister, and get out before the escape she has set up for you closes. Three floors of fire, one hour.

## The three themes

- **Fire:** Hell is on fire. Lava rooms, rivers and pools, collapsing platforms over them, fire imps and lava oozes.
  Fire is also light: it **lights the dark and gives you away** to every enemy nearby.
- **Shadow:** **Shadow Step** blinks you to a spot you aim at, but only into darkness, and keeps your momentum.
  Standing still in the dark **hides you**: enemies lose you and go hunting where they last saw you. Move fast or
  shoot and they see you again: stealth or speed.
- **Time:** **Rewind** puts the whole game (you, enemies, bolts, hazards, the objective) back 4 seconds.
  **Time Dilation** slows the world while you aim, more the faster you're moving. Over everything runs **Baja's
  hour**: real minutes of play before your way out closes, and Rewind does not give them back.

## How to play

**Get the game:** build it from source (below). If a Windows build is posted on this repo's Releases page, unzip it
and run `HellDescent.exe`.

**Controls** (all rebindable in Settings > Controls):

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move / look | WASD / mouse | left stick / right stick |
| Jump | Space | A / Cross |
| Crouch, slide | Left Ctrl or C | R3 |
| Sprint | Left Shift | L3 (toggle) |
| Fire | left mouse | RT / R2 |
| Aim (and **Time Dilation**) | right mouse | LT / L2 |
| Reload / swap gun | R / Q | X / Square, Y / Triangle |
| **Shadow Step** (hold to aim, release to go) | E | RB / R1 |
| **Rewind** | V | LB / L1 |
| Floor skill (e.g. Nightfall) | G | D-pad down |
| **Ability wheel** (hold; time slows while open) | Tab | D-pad left |
| Pause | Escape | Start / Options |

**Movement:** wall-run, slide and chain jumps; speed carries through drops. Every room has a safe, slow path and a
faster, dangerous one (over lava, through the beams).

**Difficulty is Baja's hour.** START asks for it: Easy 90 minutes, Normal 65, Hard 50 (only gameplay counts: no
menus, pauses or cutscenes). A skilled run takes about 45. You can change it in the pause menu.

**For a quick look: CHAPTERS** on the title screen. Floor 1, Floor 2, Floor 3 and the Vesper fight are all open from
the start; a chapter gives you the skills you'd have earned and starts with part of the hour already spent.

**A run:** three floors (the Ember Gate, the Molten Works, the Black Glass), each drawing 4 of its 6 rooms, with an
objective in every room (touch the six sigils, carry the soul lantern, break the chains, hold the brand, outrun the
collapse, seal the ooze wells). After each floor you pick one of three skills (shadow, time or fire). Then Vesper, a
choice, the escape, and an ending that depends on what you did.

## Build from source

Needs **Unity 6000.3.25f1** (URP) on Windows, and the .NET SDK for the tests. From the repo folder, with
`$U = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe"`, run these one at a time:

```powershell
# 1. Build the Hell scene from its data (expect "HELL_LINT 0 problems" and "HELL_BUILD built" in the log)
& $U -batchmode -nographics -projectPath . -executeMethod JamKit.SkylineRepo.EditorTools.HellScene.BuildFromCommandLine -logFile out/hell/scene.log

# 2. Build the Windows player (expect "HELL_PLAYER_BUILD OK"; makes out\hell\build\HellDescent.exe)
& $U -batchmode -projectPath . -executeMethod JamKit.SkylineRepo.EditorTools.SkylineBuild.BuildHellFromCommandLine -logFile out/hell/build.log

# Tests, no Unity needed: the game-rule and movement tests, then a compile check of the Unity side
powershell -File tools/skyline/test.ps1
```

The first import takes a few minutes. You can also open the project in the Unity editor and press Play.

## What's where

| Path | What |
|---|---|
| `Assets/JamKit/Templates/SkylineRepo/Scripts/Hell/` (and `Player/`, `Bots/`, `Hud/`, `Fx/`) | The game's Unity code: rooms, run flow, abilities, enemies, HUD |
| `Packages/com.vibejam.jamkit/Runtime/Skyline/Hell/` | The game's rules in pure C# (testable without Unity) |
| `Assets/JamKit/Templates/SkylineRepo/Look/HellLook/` | Models, materials and the look |
| `core-tests/` | The test projects (`Skyline`, `Movement`, the `SkylineUnity` compile check) |
| `docs/HELL-*.md` | The design and every system: start with [HELL-DESIGN.md](docs/HELL-DESIGN.md), then [HELL-ABILITIES.md](docs/HELL-ABILITIES.md), [HELL-SHADOW.md](docs/HELL-SHADOW.md), [HELL-HOUR.md](docs/HELL-HOUR.md), [HELL-FLOW.md](docs/HELL-FLOW.md), [HELL-UI.md](docs/HELL-UI.md) |
| `tools/` | Build, test, capture, model (`tools/hell-models/`) and audio (`tools/audio/`) scripts |

**Built on our own kit.** The first commit of this repo is [VibeCodePrepKit](https://github.com/aitizazab/VibeCodePrepKit)
(commit d8062385), the framework our team made and published openly before the event (the movement engine, guns,
bots, HUD and game shell). The organisers confirmed a team's own pre-published kit is allowed. Every later commit is
work done at the event. The kit's older docs and practice games (Visiting Hours, Skyline Repo) are still in `docs/`
and `Assets/`; they are not part of PSYCHOPOMP.

## Credits and licences

- **Code:** MIT. **Our own assets** (art, models, sound, music, writing): CC BY 4.0, credit "Muhammad Aitizaz Abro".
  See [LICENSE](LICENSE).
- **Third-party files** keep their own licences: models from Quaternius and KayKit (CC0), textures from ambientCG
  (CC0), sound effects from Freesound (CC0 and CC BY 4.0), fonts (SIL Open Font License, each with its `OFL.txt`).
  Every one is listed with its author, licence and source in [CREDITS.md](CREDITS.md).

## How it was made (AI use)

We used AI heavily, and we want to be clear about where:

- **Code:** written by Claude Code agents (Anthropic's Claude), working in parallel lanes, under the creator's
  direction, with tests on the rules.
- **Art:** concept art and comic-style art generated with Google Flow; the enemy and character models are built
  from primitives and CC0 models by scripts.
- **Voices:** the characters' lines currently use **placeholder text-to-speech voices**; Baja's voice is to be
  recorded by a person.
- **Music:** composed step by step with the creator using AI tools; still being revised.
- **Placeholders:** some story beats, tutorial lines and menu words are agent drafts marked as placeholders for the
  creator to rewrite.
- **The people:** the premise, the story (Baja, Vesper, the ending choice), the design calls (safe and fast paths,
  Baja's hour, shadow hides you and fire exposes you, the abilities' tuning), the art and sound picks and the final
  say on everything came from the creator and the team.

## The team

A team of three:

- **Muhammad Aitizaz Abro** (director): concept, story, design calls, art direction, working with Claude, the trailer.
- **Builder:** Unity, tuning, builds, bug triage.
- **Player / producer:** playtests, levels and text, asset curation, the pitch.
