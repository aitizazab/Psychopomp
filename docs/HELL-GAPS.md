# HellDescent: the plan's gaps (review of HELL-SPRINT.md)

Written 2026-10-17 at about 23:50 (day 1 night) by the plan reviewer. Not committed. It reads `HELL-SPRINT.md`,
`HELL-RESTART.md`, `HELL-DESIGN.md`, `HELL-ROOMS.md`, `HELL-ABILITIES.md`, `HELL-ENEMIES.md`, `HELL-INTEGRATION.md`,
`HELL-ART-PASS.md`, the handbook (`GameDevBooklet.pdf`), the trailer plan and the creator's standards, and the running
worktrees' git status (read only).

**What the handbook scores:** the trailer decides everything first (3 min, 2 min of directly recorded gameplay, 1 min of
stylised editing, and it must show how the themes are used). Only the top 5 trailers get played. Themes are 30% (core
integration 15, multi-theme synergy 10, atmospheric framing 5). The rest: gameplay and feel, visual polish, audio,
innovation, technical stability, scope and completeness, trailer quality. A public repo with all assets is required;
plagiarism disqualifies; a deployed URL is optional.

**Sizes:** past estimates ran far too long. Most features take under a day, and small UI or data changes take minutes.
The sizes below are agent wall time and already trimmed.

## 0. Findings from the worktrees (read only)

- **Nobody has played the game yet.** The first Unity build passed (scene lint 0, player exe OK), but the exe has not been
  run. Seven lanes are building on top of it. The first real play will find things, and the plan has no slot for it before
  02:30.
- **There is no `HellDescent-look` worktree.** The worktrees are boss, capture, fastlines, flow, jobs and rooms2. The only
  look worktree is the old `VibeJamKit-look`. Check that the look lane is really running, and in which tree.
- **Merge hot spots** (the same file edited in two lanes):

  | File | Lanes |
  |---|---|
  | `Scripts/Hell/HellRoom.cs` | rooms2 (committed), flow (modified) |
  | `Runtime/Skyline/Hell/HellRoomLayouts.cs` | rooms2 (committed), fastlines (modified) |
  | `Runtime/Skyline/Hell/HellRoomVariants.cs` | rooms2 (committed), jobs (modified) |
  | `Scripts/Hell/HellRunDirector.cs`, `HellRunEvents.cs` | flow (modified); boss, jobs and the ending all need them (the boss doc already asks flow to route deaths through `HellVesperFight.Active`) |
  | `docs/HELL-ROOMS.md` | rooms2, flow |
  | `Editor/SkylineArenaBuilder.Hell.cs` | rooms2 (committed). The jobs' pockets, the boss arena (built at run time for now, so not in the scene) and art-in will all want it. |

  **`HellRunEvents` will be the worst one.** Flow (stats: jobs done, fast paths), jobs (job completed) and the ending
  (choice made) will each add events to it.
- **Placeholder lines sit in at least four places:**
  - `HellFlowText.cs` (flow): marked `placeholder`;
  - `VesperLines` in `VesperFight.cs` (boss): marked `[PLACEHOLDER]`;
  - the job texts in `HellJobs.cs` (jobs);
  - `HellHudLine` (rooms): a draft.

  No single script document collects them for the creator.
- **There is no music system.** `SkylineMix` has a Music bus, but its comment says "none yet". Nothing plays music, and
  nothing is scheduled to.
- **The pause menu** (`SkylinePauseMenu`) already has audio, look (mouse sensitivity, FOV), bindings and quit. It has **no
  frame cap or vsync option**, which the creator's frame-rate standard asks for.
- **Repo:**
  - no git remote yet;
  - the pack is 1.70 GB (the push limit is 2 GB per push);
  - the largest tracked file is 19.6 MB (under GitHub's 100 MB limit, so no LFS needed);
  - every commit author is "Muhammad Aitizaz Abro" (good);
  - the README is still the kit's README, with a one-paragraph HellDescent header.
- **Disk:** about 12 GB free (`HELL-RESTART.md`). 120 fps capture at 1440p will not fit, and the cleanup is still waiting on
  the creator's yes.

## 1. The gaps

When: **NOW** = in parallel with the running lanes; **AFTER x** = once lane x merges; **AFTER 03:00** = trailer phase;
**CUT** = drop it.

| # | Gap | Why it matters | Size | When | Owner | Files it would touch |
|---|---|---|---|---|---|---|
| 1 | **First real play of the merged build, and a scripted smoke run of every room.** Nothing has run in Unity play mode. A safe-path autopilot (it walks each room's `safePath` polyline and does the objective) would prove there are no softlocks in each of the 18 rooms, the shafts, the pick and the boss. | Technical stability; scope and completeness; no-greybox (the creator never sees an unjudged build). A softlock in front of judges is fatal. | 2-3 h (autopilot 1.5 h, sweep 30 min per build) | **NOW** (new lane B) | new lane **Autopilot and stability** | new: `Scripts/Capture/HellAutopilot.cs`, `Runtime/Skyline/Hell/HellAutopilotPlan.cs`, tests. Read-only use of `HellRoomDef` paths. |
| 2 | **Merge plan and the build cadence.** Six lanes land before 02:30. Each player build takes 10 min, plus 3 min for the scene, one at a time. A build per merge costs over an hour. Name one owner for `HellRunEvents` and the builder, and batch the merges. | Hitting 03:00 at all; technical stability. | 15 min to decide | **NOW** (main session) | main session | `HellRunEvents.cs` (add every lane's events in one commit up front), `SkylineArenaBuilder.Hell.cs` |
| 3 | **Vesper's three once-per-floor beats** (floor 1: her voice in the Soul Lantern; floor 2: her deal and her daughter; floor 3: she sees herself in you) are in the design but **in no lane**. Floor 1's beat also depends on the Soul Lantern, and **the deck draws 4 of 6 rooms, so the Lantern may not be dealt.** It needs a fallback trigger, or the Lantern pinned to floor 1. | The boss and the spare choice mean nothing without them (the creator: "<name> needs development"; the spare must be deliberate). Atmospheric framing; the trailer's act 3 hook. | 2 h for an in-engine beat player (letterbox, held camera, subtitle and voice line) plus the three beats | **NOW** (new lane C) | new lane **Story beats and tutorial** | new: `Scripts/Hell/HellBeats.cs`, `Runtime/Skyline/Hell/HellBeatRules.cs`. It subscribes to `HellRunEvents` and doesn't edit it. Pinning the Lantern means one line in `HellRun.cs` (`HellDeck.Deal`; no lane touches it now). |
| 4 | **A tutorial per mechanic** (Shadow Step, Rewind, Time Dilation, lava, collapse, the skill pick, carrying, the jobs). Floor 1 "teaches the abilities" in the design, but nothing teaches anything. The God of Death makes sense as the teacher: she gave you the powers. First-time prompts come from her voice and subtitles, set off by conditions (the first dark gap in reach, the first lethal bolt, the first ADS at speed). | The creator's standard ("every mechanic gets taught, by someone it makes sense for"). Judges play a few minutes with no manual: if they can't find Shadow Step (dark only) or Rewind, the innovation is invisible. | 1.5 h | **NOW** (lane C) | lane C | new: `Scripts/Hell/HellTutorial.cs`, `Runtime/Skyline/Hell/HellTutorialRules.cs`. Prompt glyphs read the bindings (`BindAction`). |
| 5 | **One script document for the creator.** Every line in the game and trailer goes in one place, with ids, the speaker, where it plays and its placeholder status: the opening cards, floor cards, Vesper's fight lines, her three beats, the job briefs, the tutorial prompts, the ending lines, the stats and credits wording, and the trailer narration. The code keeps reading its own tables, and the doc maps ids to them. | The creator writes all dialogue. Scattered placeholders get missed and would ship as agent-written dialogue. | 45 min (and again after each lane merges) | **NOW** (new lane A) | new lane **Script, voice and trailer plan** | new: `docs/HELL-SCRIPT.md` (docs only) |
| 6 | **Voice and narration: who voices it.** The trailer plan wants "a dramatic narrator". The God of Death, Vesper and the narrator have no voice. Proposal: placeholder voices now, clearly marked, made locally (the same generator family as the Skyline voices, or a free local TTS), then the creator records or picks the final voices. Every voice line also gets a subtitle. | Audio design; trailer quality (a narrated 30 s premise is the trailer's opening). | 1 h for placeholders and a listening page | **NOW** (lane A; files land AFTER the sound-in lane's mixer wiring) | lane A | new folder: `Resources/SkylineAudio/hell-voice/` (new); listening page under `FableAnimationTests/art/review/2026-10-18/` |
| 7 | **The God of Death meeting is text cards only**, but the trailer plan opens on a 30 s in-engine cinematic premise with a narrator. There is no God of Death model, scene or art anywhere. Option: an illustrated sequence (Flow images, 16:9, AI art allowed as final when it looks right), parallax over an in-engine Hell plate, with the narrator. It serves both the game's opening and the trailer's first 30 s. | Trailer quality (the first 30 s decide whether a judge keeps watching); atmospheric framing. | 2-3 h (art 1 h via Flow, sequence 1 h) | **AFTER 03:00** (cards stay as the 03:00 fallback); the art prompts can start NOW in lane A | lane A (prompts and shot list), then a trailer lane | `HellScreens.cs` (flow's file: AFTER flow merges); new art under `Resources/HellStory/` |
| 8 | **Vesper's and the God of Death's bodies.** The boss is the trailer's climax. `HellVesperBody.cs` is a stand-in, and no humanoid art is in the art list (it covers the imp, ooze and scythe demon only). The creator's standard: serious scenes get real art, "nothing less complex than a Roblox model". Candidates: a CC0 humanoid with CC0 animations (not the excluded newer Quaternius QAL packs), or the Tripo + comic-style route the creator found earlier. | Visual polish; story; trailer act 3. | 2-3 h | **AFTER boss** (in art-in), or **AFTER 03:00** | art-in lane (add it to its brief) | `Resources/HellModels/vesper*`, `HellVesperBody.cs` (boss's file) |
| 9 | **Music: nothing is scheduled.** It is composed together with the creator, layer by layer. Before that, the game needs a music player: per-floor loops, the boss track, an intensity layer that follows combat and speed, and a Rewind pitch-bend. Composition sessions with the creator go on day 2. The trailer needs its own cut: music drives the edit. | Audio design; trailer quality (the trailer is edited to the music). | Player 45 min; music: creator sessions, hours | Player **AFTER sound-in** (fold in); composition **AFTER 03:00** (day 2, with the creator) | sound-in lane (player); main + creator (music) | new: `Scripts/Audio/HellMusic.cs`; the Music bus already exists in `SkylineMix` |
| 10 | **The signature Time and Shadow effects are weak.** The Rewind is "a 0.3 s desaturated flash", and the Shadow Step is smoke blobs. The Rewind is the game's showpiece of the Time theme and of multi-theme synergy (it rewinds fire and shadow too). It needs a reverse-scrub look: your past path as a ghost trail, enemies' afterimages, bolts flying backwards, a rewind sound. Time Dilation's edge and the Step's smear need the same pass. | Core integration 15 and synergy 10; feel; trailer (these are the shots that "show how the themes were incorporated" without captions). | 2 h | **NOW** (new lane D; visuals only; the sound-in lane owns the audio) | new lane **Signature VFX and feel** | `Fx/HellFx.cs`, new `Fx/HellRewindFx.cs`, the URP volume profile for Hell. Not `HellAbilityAudio`. |
| 11 | **Judges reach the good parts fast.** A full run is 12 rooms plus the boss: probably 25-40 min (the Sigils' safe path alone is about 100 s). Judges play a few minutes. Needs a **chapter start** on the title (Floor 1 / 2 / 3 / Vesper, unlocked from the start, given skills by floor) and a **curated default deck** (a showcase first room, not a random one). | Scope and completeness *as seen by a judge*; gameplay and feel in the first 3 minutes. | 45 min | **AFTER flow** (it owns `HellScreens` and `HellRunDirector`) | flow lane, second pass | `HellScreens.cs`, `HellRunDirector.cs`, `HellRun.cs` (deck seed) |
| 12 | **The ending lane is queued behind the boss, and both must land by 03:00.** The decision scene, the escape and three endings can't all be built and judged in time. Make a minimal version first: the walk to her plus a hold-Use spare, the escape as the Collapse room re-dressed with the lantern or with Vesper running beside you, and three text-card epilogues into the stats screen. | Scope and completeness; the story's payoff; the trailer's "open question" ending. | Minimal 1.5 h; full AFTER 03:00 | **AFTER boss** | the ending lane (as planned), with a "minimal first" brief | `HellRunDirector.cs` (after flow), new `Scripts/Hell/HellEnding.cs`, new pure `HellEndingRules.cs` |
| 13 | **The boss arena is not in the run.** It is built at run time by `HellVesperArenaView`, and nothing connects floor 3's last shaft to it: today the run ends after floor 3's shaft. The deaths during the fight also need routing in `HellRunDirector.OnWrecked` (the boss doc asks flow for it). | Title-to-credits is impossible without it. | 30 min | **AFTER flow and boss** | main session, at the merge | `HellRunDirector.cs`, `HellWorld.cs` (an arena slot) |
| 14 | **The jobs need a place and a hook.** The job pockets are "off the main line": they need geometry (the builder or run-time props), a brief delivered by the God of Death's voice, and a count fed to the stats and the ending. The jobs lane already edits `HellRoomVariants.cs` (as did rooms2). | The ending depends on the job count; innovation (the creator's own idea). | Part of the jobs lane; the merge 20 min | **AFTER jobs + rooms2** | jobs lane, then the main session | `HellRoomVariants.cs`, `HellRunEvents.cs` (job events, see #2), the builder |
| 15 | **Rewind edge cases** beyond the room boundary (which the flow lane is doing): a Rewind during the skill pick, cards or a beat; across the boss's phase change and her own Rewind; while carrying the lantern (`HellRoomFlags.Carry` is not in a snapshot); into a lava sink or out of a death; with Ashwalker's immunity; across a job completion; during dilation (the `HellTime` requests). Also: burns, hover offsets and beams don't rewind (documented). | Technical stability; the Time mechanic is the headline, so its bugs are the most visible. | 1 h of tests and fixes | **AFTER flow + boss** | lane B (autopilot runs scripted Rewinds at each case) and fixes by the owners | tests only in lane B; fixes in the owners' files |
| 16 | **Performance at 120 fps with 1% lows above 90 is unmeasured on Hell.** Risks: all 18 rooms and 852 solids in one scene, all live; a lava light every 12 m (dozens of realtime point lights, against URP's per-object light limit); the ink pass (30 ms 1% lows when last measured); `StateCopy` allocations for every enemy on every fixed step. Likely fixes: switch rooms on only when current or next; cap the lights per room; the ink A/B gate from the art pass. | Creator's standard (60 fps is "horrendous"); technical stability; the trailer at 120 fps needs headroom. | Measure 15 min (capture lane's frame-time check); fixes 1-2 h | Measure **AFTER capture**; fixes **AFTER flow** (room switching is in `HellRunDirector`) | capture lane measures; main session or a perf lane fixes | `HellRunDirector.cs` (room activation), `SkylineArenaBuilder.Hell.cs` (light count) |
| 17 | **A frame cap / vsync option** in the pause menu (and a sane default: uncapped, vsync off, with the creator's monitor rate). | Creator's frame-rate standard explicitly asks for it. | 15 min | **AFTER flow** (flow is editing `SkylinePauseMenu.cs`) | flow lane, second pass | `SkylinePauseMenu.cs`, `SkylineSettings` |
| 18 | **Game feel on hits and kills against Hell enemies.** The Skyline feedback (hit stop through `HellTime`, damage feedback, shake) exists. Check that it fires for the Hell bodies (they deactivate on death, without a wreck). Add kill confirms (an ember burst plus a sound), lava-hit and sink feedback, and landing and collapse shakes. | Core gameplay and feel. | 1 h | **AFTER sound-in**, together with enemy readability | enemy readability lane (as planned) | `Bots/HellEnemyFx.cs`, `Hud/SkylineDamageFeedback.cs` |
| 19 | **The title is undecided.** It blocks the title screen, the credits, the trailer's end card, the repo name and the Drive file name. Needs a shortlist tied to the settled story (the exchange of time, the Evening Star, the God of Death's errand) and a font pick (Pirata One recommended). | The trailer's end card; recognition. | 20 min shortlist; the creator decides | **NOW** (lane A drafts it), decided at the creator's wake-up | lane A, then the creator | `HellFlowText.Title` (flow's file: one constant, after flow merges) |
| 20 | **The skill pick's presentation.** The cards exist (`HellSkillPick`: colour, icon *text*, name, description). They need icons, a flavour frame per element (shadow, time, fire) and a sound. It is a trailer moment and the build's identity. | Visual polish; synergy (one card per theme). | 45 min | **AFTER art direction** | art-in lane | `Hud/HellSkillPick.cs`, new icon textures |
| 21 | **Death's in-world reason and its cost.** Today a death restarts the room. The creator's standard: retries need an in-world reason that doesn't make the antagonist look incompetent, and a cost. The God of Death pulls you back (she owns death) and it costs time against your sister's: shown on the stats screen, maybe one line from her. | Story standards; theme framing (Time). | 20 min (one line plus a stat) | **NOW** in the script (lane A); in the game **AFTER flow** | lane A writes it; flow shows it | `HellFlowText.cs`, stats |
| 22 | **The trailer's shot list, mapped to what exists.** The trailer plan has 0:00-0:30 premise, act 1 movement and combat, act 2 escalation, act 3 the story hook, the title end card, and enemy cards. Today: the premise has no art (#7); the enemy cards need the final enemy bodies, a pose and a background each; act 3 needs Vesper's body (#8) and her beats (#3); the end card needs the title (#19). The capture lane's camera shots (chase-high, dutch-low, enemy-reveal, locked-wide) are the start. Each shot needs a room, a seed, a replay and a camera. | Trailer quality is scored on its own, and the trailer decides the shortlist. | 45 min to write; capture 3-5 h | Write **NOW** (lane A); capture **AFTER 03:00** | lane A, then the trailer phase | new: `docs/HELL-TRAILER.md` |
| 23 | **Disk space for 120 fps capture.** About 12 GB free. Capture at 1440p and 120 fps runs to several GB a minute even at high-quality NVENC. The cleanup candidates are listed in `HELL-RESTART.md` and still need the creator's yes. | Without it the trailer phase stops. | 5 min once approved | **Ask the creator before the nap** | main session | none (`uv cache prune`, old worktrees, Temp) |
| 24 | **The public repo:** a HellDescent README (how to play, controls, the themes, the build), `ASSETS.md` with every third-party file and its licence (CC0 / CC-BY credits / OFL with `OFL.txt` shipped), the credits screen matching it, the LICENSE updated, `out/` and generated files ignored. Check the author names before the push. Do a **test push early**: 1.7 GB is near the 2 GB per-push limit, and art will grow it. | A required submission item; plagiarism checks (a clear record of what's ours and what's credited). | 1 h | **AFTER 03:00** (day 2 morning); test push day 2 | a repo lane (Sonnet is fine) | `README.md`, new `ASSETS.md`, `LICENSE`, `.gitignore` |
| 25 | **The deliverable: exe, not WebGL.** WebGL builds need capped-run at 0.6 share; il2cpp, URP, ink and 120 fps won't hold in a browser. The deployed URL is optional. Give a zipped Windows build as a GitHub Release (and/or an itch.io download page), with a fallback low-quality preset. | Technical stability; judges must be able to run the top-5 build. | 30 min | **AFTER 03:00** | repo lane | build script only |
| 26 | **Playtest by agent judges with human-like demo players**, before the creator plays (the creator's standard). It needs lane B's autopilot moving like a human (strafing, turning at a human rate, using abilities), and captures per room for the adversarial judges. | No-greybox; game quality bar. | 1 h per round | **AFTER the 02:30 build** (first round); again after art | main session + judge agents, with lane B's player | none new |
| 27 | **Enemy spawn telegraphs** ("enemies spawn without a warning sign"). The plan has them in enemy readability after art and sound. That's fine, but it is also a fairness bug for judges, so it must not slip past day 2. | Feel; stability (unfair deaths). | in the readability lane | **AFTER art + sound** (as planned) | enemy readability lane | as planned |
| 28 | **The God of Death has no name**, and Hell's floors have names but she does not. It affects the script, the cards and the trailer's narration. | Story; trailer narration. | creator decision | **NOW** (lane A lists options) | creator | `docs/HELL-SCRIPT.md` |
| 29 | **Difficulty for judges.** Floor 3's lava does 22 DPS and sinks for 60. The Collapse's fronts are now 10.5 / 8 m/s (faster than the 9.5 sprint). Nobody has played any of it. An assist toggle (slower collapse, less lava damage) or a "story" difficulty keeps a judge from bouncing. | Feel; completeness as experienced. | 30 min | **AFTER first play** (lane B's data first) | rooms lane | `HellRoomVariants.Floors` (a multiplier) |

## 2. Recommended lanes to launch now (4, no file overlap with running lanes)

**A. Script, voice and trailer plan** (docs and new audio folder only)
- **`docs/HELL-SCRIPT.md`:** every line the game and trailer will say, gathered from:
  - `HellFlowText.cs` (flow), `VesperLines` (boss), `HellJobs.cs` (jobs), `HellHudLine`;
  - drafted placeholders for the gaps: Vesper's three floor beats, the God of Death's tutorial lines, the ending's three
    epilogues, the death reason, the 30 s narrated premise.

  Each line gets an id, the speaker, the trigger and where it lives in code, and is marked PLACEHOLDER: the creator writes
  all dialogue.
- **The God of Death's name and the title:** short option lists (and the Pirata One font for the title card), for the
  creator's wake-up.
- **Placeholder voices:** clearly labelled TTS or local-generator voices for the narrator, the God of Death and Vesper,
  into the new folder `Resources/SkylineAudio/hell-voice/`, with a loudness-matched listening page in
  `FableAnimationTests/art/review/2026-10-18/`.
- **`docs/HELL-TRAILER.md`:** the shot list mapped to the trailer plan's acts, each shot with its room, seed, camera
  (capture lane's shot files), what must exist first, and its theme (Fire, Shadow or Time). Also the Flow image prompts for
  the God of Death premise sequence (16:9).

No code files. It must refresh the script after each lane merges.

**B. Autopilot and stability sweep** (new files only)
- **A Hell autopilot:** it drives the real `SkylinePlayer` input along each room's `safePath` (and optionally `fastPath`)
  polylines from `HellRoomDef`, does each objective (holds the brand, carries the lantern, shoots anchors, seals wells),
  takes the shafts and makes the skill pick.
  - It moves like a human: strafes, and turns at a capped rate.
  - It logs `HELL_AUTO room <id> ok/stuck <reason> <seconds>`.
- **A stability sweep:** all 18 rooms by seed, timed scripted Rewinds at the edge cases of gap #15, and a summary per room.
- **Files:** `Scripts/Capture/HellAutopilot.cs`, `Runtime/Skyline/Hell/HellAutopilotPlan.cs`, tests in
  `Tests/Skyline/Hell/HellAutopilotTests.cs`.
- **The boundary with capture:** the capture lane owns the replay format, and the autopilot may write it but doesn't change
  it.
- It hands the main session the command line for a batch run on the 02:30 build. It also becomes the human-like demo
  player for the judge rounds and for trailer B-roll.

**C. Story beats and tutorial** (new files only)
- **A small in-engine beat player:** letterbox, an optional camera hold that never takes control for more than ~4 s, a
  subtitle through `SkylineHudSubtitles`, and a voice-line hook (lane A's ids).
- **Vesper's three floor beats:** floor 1 her voice from the Soul Lantern, with a fallback trigger when the Lantern isn't
  dealt (the second room's entry); floor 2 at the shaft before the pick; floor 3 on the last room's entry.
- **The God of Death's first-time teaching prompts** for Shadow Step, Rewind, Time Dilation, lava, collapse, carrying, the
  skill pick and the first job. They are set off by conditions (a dark landing in reach, a near-death hit, ADS at speed),
  each shown once, and hidden by the capture lane's empty-HUD switch.
- **Files:** `Scripts/Hell/HellBeats.cs`, `Scripts/Hell/HellTutorial.cs`, `Runtime/Skyline/Hell/HellBeatRules.cs`,
  `Runtime/Skyline/Hell/HellTutorialRules.cs`, tests.
- It only subscribes to `HellRunEvents`, `HellAbilities` and `HellRewind` events. If it needs a new event, it lists it for
  the main session; it does not edit flow's files.

**D. Signature VFX and feel** (visual only)
- **The Rewind as the Time showpiece:** a reverse-scrub screen treatment, your last 4 s path drawn as a fading ghost
  ribbon, afterimages on restored enemies, bolts visibly flying back.
- **The Shadow Step's smear and arrival burst; Time Dilation's look** (edge, chromatic shift, slowed embers).
- **Combustion's chain** and Nightfall's sphere, made readable.
- **Files:** `Fx/HellFx.cs`, new `Fx/HellRewindFx.cs`, the Hell URP volume profile.
- **Boundaries:**
  - it doesn't touch `HellAbilityAudio` or the sound folders (sound-in owns them);
  - it doesn't touch `HellRewind.cs` (flow is editing it), and listens to `HellRewind.Rewound` instead;
  - the art-in lane keeps environment VFX (embers over lava, braziers).
- It stays inside 120 fps: no full-screen grab passes beyond one.

## 3. Cut list if 03:00 slips (cut from the top)

1. The full ending → a minimal ending: the spare (walk plus hold Use) or kill, then text-card epilogues and the stats. The
   escape sequence waits until after 03:00.
2. The jobs → one job per floor, not 1-2 (the count still feeds the ending).
3. The Sigils' fast lines fixed by simulation → ship the fastlines lane's shorter safe path only; fix the geometry on day 2.
4. The skill pick → keep it as it is (text icons) until art-in.
5. Rewind across rooms → block the Rewind for the first 4 s of a room (a one-line guard) instead of handling it.
6. Vesper's floor 1 and floor 2 beats → floor 3's beat only, plus her fight lines.
7. Stats → time, deaths and Rewinds only.
8. The opening → the text cards stay; the illustrated or cinematic premise moves wholly to the trailer phase.
9. Never cut: the title → run → boss → choice → credits loop, the chapter start for judges (gap #11), the autopilot
   softlock sweep, and the frame-time check.

**A realism note:** the 03:00 goal is achievable as "title to credits with stand-in art, minimal ending", if the merges are
batched (two builds, about 01:30 and 02:30) and lane B's sweep runs on the second. Art, sound, enemy readability, music and
the cinematic premise belong to day 2 either way. The trailer's 2 minutes of gameplay depend on art and sound landing by day
2 evening, so the capture should start no later than day 2 at about 18:00, leaving the night for the edit.
