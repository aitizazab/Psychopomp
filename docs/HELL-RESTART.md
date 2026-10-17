# HellDescent: start here after a restart

Written 2026-10-17 (event day 1, afternoon), when the creator restarted the session. A new session reads this page
first, gives the creator the **report** below (everything since the pivot) and then the **next steps**, and keeps this
page current after every build.

Repo: `C:\Users\User\Desktop\yougotgamesonyophone\HellDescent` (branch `main`, latest commit at the end of this page).
Not on GitHub yet. Commit only with the repo-local identity:
`git -c user.name="Muhammad Aitizaz Abro" -c user.email="aitizaz.ab24@gmail.com" commit ...` (never the global
config: it has the uni ID).

## The report: everything since the pivot

### The pivot (morning of day 1)
- Themes revealed: **Fire, Shadow, Time**. The Skyline game became **a descent into Hell**: 3 floors, each a deck of
  6 rooms of which a run draws 4; every room has an objective, a **safe path** and a **faster, dangerous path**.
- Design written: `docs/HELL-DESIGN.md` (premise, abilities, enemies, hazards, the six room types, floors).
- **Story so far:** setting = Fire (Hell), premise = Time, protagonist = Shadow. Your sibling dies; you refuse to
  accept it and seek a powerful entity; you make a deal: powers over time and shadow in exchange for going into Hell,
  bringing your sibling back, and doing jobs for the entity on the way. **Open, to decide later:** who the entity is,
  why it can send you and grant those powers, what it wants in Hell, why it can't go itself. Titles come only after the
  story's major themes are settled ("The Burning Hour" was rejected).
- The event repo was created: `HellDescent`, first commit imports the published kit (VibeCodePrepKit d8062385).
  Unity's `Library` was moved in from VibeJamKit, so the first import should be quicker.

### What was built (three agents in parallel, then merged by an integration pass)
All of it is code with tests. **None of it has run in Unity yet** (see "Where it stopped").

**Enemies** (`docs/HELL-ENEMIES.md`): same functions as the old bots, new bodies (primitive stand-ins until art):
- **Fire imp** (replaces the drone): hovers and fires fast ember bolts.
- **Scythe demon** (replaces the spotlight bot): spawns demon scythes that show their path as a line, then beam along
  it (a wheel of two blades crossing the lane at waist height).
- **Lava ooze** (replaces the floor-care bot): pulsates; leaves lava pools instead of acid.
- Deaths in fire and smoke. A test bed: Add Component > JamKit/Hell/Enemy Test Bed.

**Abilities** (`docs/HELL-ABILITIES.md`):
- **Shadow Step:** an aimed blink with a landing preview, only INTO darkness, keeps your momentum.
- **Rewind:** the whole game state goes back 4 s (you, enemies alive or dead, bolts, hazards, the objective, the clock).
- **Time Dilation** (passive): aiming down sights slows time for 3 s, more the faster you're moving.
- **The skill pick after each floor, with your nine skills:** Umbral Chain (a free second step within 1 s), Ambush
  (triple damage on the first shot after a step and an extra step charge), Shade (a decoy), Nightfall (darkness you can
  step into), Deeper Rewind (6 s), Momentum Bank, Borrowed Time, Ashwalker (step onto lava, walk on it for 6 s, and
  bunny-hop off lava), Combustion.

**Rooms and hazards** (`docs/HELL-ROOMS.md`): all six room layouts built new for Hell (no Skyline rooms reused), each
varied per floor (18 rooms in all), the deck draw, the run flow (room to room, the shaft down, the skill pick), lava
and collapsing platforms, and a checker that proves every room has a safe path and a faster risky one.
- **The Six Sigils** is the new Relay, made from scratch: a 72 x 170 m lava cavern, six sigils A to F at six heights,
  legs of 30-47 m, each leg needing a different trick (wall-run over lava, slide under a beam, Shadow Step across a
  gap, jump chain over collapsing platforms, a long fall with a shot), each with a long safe way round. It replaces the
  old Skyline Relay you rejected (points too close, nothing in the way).
- The other five: the Soul Lantern (carry), Break the Chains (shoot three anchors), Hold the Brand (hold a circle),
  the Collapse (escape), the Ooze Wells (seal three wells).

**Integration** (`docs/HELL-INTEGRATION.md`): one Rewind recorder for everything, `HellTime` as the only thing that
changes game speed, the stand-in stubs deleted. A build command for a Hell-only player exe was added.

**Tests (no Unity):** Skyline 1057 passed, 3 skipped; Movement 215/215; compile and assembly checks green.

### Other work today
- I opened the latest Skyline build for your teammate.
- Restart docs for the old Skyline/music line were committed in VibeJamKit (music: flutes option C picked; the
  PopularMMOs pre-drop/drop is **parked**).
- The capture tooling audit is written: `VibeJamKit-capture/docs/SKYLINE-CAPTURE-AUDIT.md` (branch `skyline-capture`).
  Top gaps: camera shots, a clean-picture switch (empty HUD, voices off), preview with jump-to-time, timed player
  actions and replay, directable enemies and enemy cards.

### Where it stopped
- **2026-10-17 evening: the first Unity build passed.** Scene: `HELL_LINT 0 problems over 18 rooms`, `HELL_BUILD built`
  (18 rooms, 852 solids; the scene under `Generated/` is gitignored and rebuilt from data). Player:
  `HELL_PLAYER_BUILD OK`, `out\hell\build\HellDescent.exe`. No compile errors. Both ran through capped-run at
  `--share 0.35` (5.3 GB cap) with about 5 GB free: the scene build (first import) took 3 min, the player 10 min.
  Not yet played. (Earlier the same day the first attempt had been stopped by the memory guard at about 3 GB free.)
- Unity's import also touched render-pipeline assets and made metas for VisitingHours art; left uncommitted (import
  churn from the kit, not Hell work).

## Next steps (in order)
1. **Build and play** (built 2026-10-17 evening, see above; next: play the exe). To rebuild, run one at a time, from the HellDescent folder (`$U` = `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe`):
   1. The scene: `& $U -batchmode -nographics -projectPath . -executeMethod JamKit.SkylineRepo.EditorTools.HellScene.BuildFromCommandLine -logFile out/hell/scene.log`
      (expect `HELL_LINT 0 problems` and `HELL_BUILD built`).
   2. The player: `& $U -batchmode -projectPath . -executeMethod JamKit.SkylineRepo.EditorTools.SkylineBuild.BuildHellFromCommandLine -logFile out/hell/build.log`
      (expect `HELL_PLAYER_BUILD OK`; makes `out\hell\build\HellDescent.exe`).
   3. Optional: `powershell -NoProfile -File tools/skyline/pass.ps1 -NoWindows` (EditMode tests).
   4. Commit the generated scene and Build Settings, then launch the exe for the creator.
   Fix whatever Unity's first real compile and run show (first contact always shows something).
2. **Play-test the mechanics together:** Shadow Step, Rewind, Time Dilation, the nine skills, the three enemies,
   lava and collapse, and especially **the Six Sigils' fast lines** (untested: the E-F wall-run out of the pit and the
   A-B jump onto B; its safe path is very long, 931 m against 277 m fast, and probably needs shortening).
3. **Known gaps to close before the art pass:** enemies spawn without a warning sign; a Rewind across a room boundary
   isn't handled; no title screen; collapse baffle walls float when their tiles fall; room snapshots allocate memory and
   the room's spawn counter isn't rewound; the Nightfall look is crude; the ability and enemy sound hooks are empty.
4. **Art**, then **the trailer** at 90-120 fps (trailer plan: 30 s cinematic premise with a narrator; act 1 movement and
   combat, act 2 escalation, act 3 story hook, title card; enemy cards; no text over gameplay). The capture gaps from the
   audit (clean-picture switch first: an empty HUD layout, minutes of work) come before recording.
5. **Story:** the creator pitched it 2026-10-17 evening (`HELL-DESIGN.md`, "The creator's story pitch"): a God of
   Death (maybe Baja) sends you to kill <name>, Lucifer's living bride, and bring back her soul; final boss <name> or
   Lucifer, an escape, an ending choice, credits, a stats screen, the title screen. Still open:
   - Is the God of Death Baja, and is Baja "she"?
   - <name>: her name, who she was, why Lucifer took her, whether she wants to be there, and where we meet her on the
     way (the third ending needs her developed).
   - The deal: does Baja give your sibling back for <name>'s soul? Is the sibling in Hell? What does "taking too long"
     mean in play (a run clock?) and in story?
   - Why a death god's gift is time and shadow; are the "jobs on the way" from the first premise kept or dropped?
   - The final boss (<name> or Lucifer) and how the ending choice is made in play; which ending the trailer hints at.
   - Then titles.
   **Update 2026-10-17 23:00:** <name> = Vesper (see HELL-DESIGN.md). Pitched for the deliberate spare: **the letter
   (REJECTED 23:30: replaced by the creator's version in HELL-DESIGN.md: Lucifer bored of her, she is lost without
   purpose).** The old pitch, kept for the record: Lucifer told Vesper the deal failed and kept her ignorant; the God of Death, who reaps
   everyone, holds the daughter's last words. On floor 3, before the boss, you find them (or a job earns them). After
   the fight Vesper, beaten, asks you to finish it. To spare her you lower the gun, walk to her and hold Use to give her
   the letter: her daughter lived a full life, had children, died old. Her reason to stay was a lie; she leaves
   Lucifer, and asks to see one sunset before her end. Why the God of Death can accept it: every soul comes to her in
   the end, and Lucifer losing his bride teaches the lesson anyway (with your jobs done she's amused; without, you pay).
   Sprint plan: `HELL-SPRINT.md`.
   Build items it adds: a title screen, a boss, an escape sequence (The Collapse is the base), the ending choice,
   credits, the stats screen.
6. **Publish:** `gh repo create aitizazab/HellDescent --public --source . --push` (the creator runs it; the handbook
   needs a public repo with all assets).

## Settled
- **The organisers allow the pre-published kit** (2026-10-17): our own assets are fine, copying released games
  one-to-one is not; reusing the unreleased Skyline code is fine.

## Waiting on the creator's yes
- **Disk** (about 12 GB free; 120 fps capture needs more). Candidates, nothing deleted yet:
  - `uv cache prune` (the uv cache is 57.6 GB in `C:\Users\User\AppData\Local\uv`; prune keeps what's in use);
  - the old worktrees `VibeJamKit-play` (13.5 GB) and `VibeJamKit-look` (4.8 GB), after checking them for uncommitted work;
  - Windows Temp (7.8 GB);
  - old Skyline passes: only 1.6 GB (`out/skyline/20261015-perf11` 1.1 GB; keep `20261015-0349`, the build shown today).
  - For scale: Desktop 124 GB, AppData 120 GB, HyperV 16 GB, .claude 16 GB, anaconda3 15 GB, Downloads 12 GB.
- **Opening the Unity editor** (it takes focus).

## Parked or later (not today)
- PopularMMOs music line (parked by the creator); flutes option C into the old Skyline game.
- The perf A/B runs (`FableAnimationTests/.work/perf-ab-overnight.ps1`, keep-awake fixed); they need the PC to itself.
- After the event: delete the backup mirror `VibeCodePrepKit-BACKUP-before-name-fix.git`.

## Commit log since the pivot (HellDescent `main`)
- `cb16e90a` design · `4f3b162b` `3576d0a1` `9e10a704` `e2e83279` enemies · `853008d3` `4bdadcc4` `36018c6c` `12998487`
  abilities · `d7c389a6` `51a29bbc` rooms · `7e4ad006` integration · `4eb4f622` the Hell player build · this page.
  (Two of the middle commits don't compile on their own; HEAD does.)
