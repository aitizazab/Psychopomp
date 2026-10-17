# HellDescent music: the plan (composed WITH the creator, step by step; round 1 sketched 2026-10-18)

## The structure (the creator, 2026-10-18 00:05)
Done the same way as Skyline's floor 1 tracks, but **one set for the whole game**:
- **Three moods on one skeleton**, so they're reminiscent of each other:
  - **Quiet:** no danger.
  - **Normal:** some danger, or a looming objective.
  - **Danger:** near death, or an imminent objective.
- **Each mood in three speed variations:** **Still**, **Fast**, **Flying** (moving very fast).
- So 3 x 3 = 9 layers over one skeleton; the game crossfades between them (danger from health/objective state,
  speed from the player's speed), like Skyline's music director plan.

## Genre (discussed 2026-10-18; the creator leaves the genre to us where it makes sense)
- The creator leans rock or metal for this game; the God of Death, the spare cutscene(s) and other story themes
  should not share one genre.
- Proposal (not decided): the in-run set (the 9 layers) rises into rock/metal: Quiet sparse, Normal driving, Danger
  and Flying at full force. Character themes are melodies that change costume rather than genres: the God of Death a
  lazy swinging waltz or lounge feel (bored, flippant), Vesper a sorrowful waltz (a bride; the same dance as the God of
  Death's, played two ways), the spare: her waltz warming as your theme joins it; the kill: her theme stopping mid-phrase.
- Reducing genre further: identity lives in three short tunes (yours, Vesper's, the God of Death's) that appear across
  the game; Time Dilation thins the music to a muffled pulse, dark zones drop the drums.
- References from the sound research (`HELL-SOUND-PASS.md`, music section): DOOM Eternal, Metal: Hellsinger, Hades.

## Every track (first pass, 2026-10-18 ~01:00; for the creator to steer)

**The three melodies carry identity across genres** (sketches: `FableAnimationTests/art/review/2026-10-18/hell-music.html`):
- **Yours** (4/4, D minor): the cell "long, short repeat, up a minor third, down a step" (take A "The Vow").
- **Vesper's** (3/4 waltz, D minor): take A opens with your cell slowed down (she is you further down the road), so your
  tune can join hers in the spare ending.
- **The God of Death's** (3/4 swung waltz, C minor): a slouching tune circling one note (take A "The Shrug").
Each has two alternative takes on the page; the creator picks before anything grows further.

**Genre map:** the in-run set and the boss are rock/metal; the God of Death is a swung lounge waltz (muted trumpet,
upright bass, brushes, piano, vibraphone); Vesper is a chamber waltz (harp, cello, violin, music box); Lucifer is never a
melody, only a blinding major chord (organ + choir); the endings take their colour from whoever they're about.

| # | Track | Purpose | Length | Loop? | Melody | Genre / instruments | Priority |
|---|---|---|---|---|---|---|---|
| 1 | **In-run set**: Quiet / Normal / Danger x Still / Fast / Flying | Plays in the rooms; crossfaded on bar lines by danger (health, objective) and speed | 16 bars, 28.4 s at 135 BPM | Loop, one clock | Yours (lead in Normal, fragments in Quiet, low horns in Danger) | Rock/metal: drums, picked bass, doubled drop-D rhythm guitars, lead guitar, organ. Quiet: clean guitar + pad, no drums. Danger: half time + choir + tremolo strings. Fast = double kick + gallop guitar. Flying = 16th kicks, crashes, fast strings, twin-guitar harmony, choir | **MUST (03:00)**: the three Still moods + Normal Fast/Flying (sketched); Quiet's and Danger's own Fast/Flying after the creator's notes (Skyline lesson: Quiet's speed layers must stay soft) |
| 2 | **Title** | First impression; also the trailer's end card | 30-40 s | Loop | Yours, alone first | Your melody on a clean guitar over an organ drone and the lava bed, then one heavy statement on the in-run band, back to quiet | **MUST (03:00)** (simple version) |
| 3 | **The God of Death's opening** (cutscene 1, 45-55 s) | Grief played straight, then the God of Death played funny | Fitted to the pages | No | Yours on page 1 (the sister: solo piano), then hers (her waltz enters at her reveal, stops dead on "...Wait", restarts sly at "the funniest idea") | Piano, then the lounge waltz; one heavy hit of your riff as you fall and the title burns in | **Trailer** (its first 30 s); for the 03:00 build her waltz under the text cards is enough |
| 4 | **Floor cards** (2.8 s each) | Each floor's arrival | 2.5-3 s x 3 | No (stingers) | Your cell, one bar | Floor 1 guitars; floor 2 + anvil/metal hits; floor 3 + choir and glassy bells: darker each floor | **MUST (03:00)** (cheap) |
| 5 | **Skill pick** (time stopped) | A breath; her gift | Under the screen | Loop | The God of Death's (slow, on a music box) | Music box over a held choir chord; cheapest: the in-run set muffled (low-pass, -10 dB) | 03:00: the muffled in-run set (code only); the music box loop **later** |
| 6 | **Job done / job failed** stingers | Feedback on her favours | 1.5-2 s each | No | The God of Death's first notes | Done: muted trumpet + vibes, cheeky; failed: the same notes sagging down a half-step | **MUST (03:00)** (cheap) |
| 7 | **Death "AGAIN"** sting | You died; the room restarts | 1.5-2.5 s | No | Your cell's first two notes, reversed | The chosen low-impact SFX plus a reversed low guitar hit | SFX exists; music sting **later** |
| 8 | **Shafts** between floors | The fall | - | - | - | The in-run set keeps playing (Flying while falling) | Code only |
| 9 | **Vesper's history** (cutscene 3, 60-75 s) | Make her a person before the fight | Fitted to the pages | No | Vesper's | Her waltz: music box + harp (the daughter); Lucifer's blinding major chord (the bargain); the waltz grand with strings (the bride); slowing to one instrument (the boredom); the instruments she tried each picking up her tune and dropping it (the centuries); her tune turning into the boss riff as she stands | **MUST (build)**, **trailer** (act 3 hook) |
| 10 | **Boss fight** (3 phases) | The duel | 16 bars per phase | Loop per phase, switched at phase changes | Vesper's (yours answers) | Proposal: her own track, a **metal waltz in 3/4** on her melody (her dance, now fighting) on the in-run band. Phase 1 Duel: full. Phase 2 Undo: + a ticking clock, and the music rewinds during her Rewind tell (1.6 s). Phase 3 Nightfall: the lights go out, so the drums drop: bass, choir, her tune whispered, a heartbeat. Her kneel: the band cuts to her tune on a lone cello | 03:00: the in-run **Danger** set as a stand-in; the real track for the **trailer** (boss footage) |
| 11 | **The decision** (in engine) | Kill or spare, in the player's hands | Open-ended | Loop + 2 stings | Vesper's, unresolved | A low string drone and her first three notes on cello, repeating; shoot: silence; offer a hand: your cell answers on clean guitar | **MUST (build)** (short, cheap) |
| 12 | **The escape** | Out of Hell | Open-ended | Loop | Yours (+ hers if spared) | The in-run Danger + Flying at full; if she's with you, her tune as a counter-line layer | 03:00: the in-run set; her counter-line **later** |
| 13 | **Ending: kill** (25-30 s) | The deal kept, the cost felt | Fitted | No | Hers stopping mid-phrase, then yours | The music box runs down mid-phrase; your melody on solo piano, resolved but empty; one high held note (the evening star) | **MUST (build)** |
| 14 | **Ending: spare, most jobs** (30-35 s) | The best ending | Fitted | No | Hers + yours together; the God of Death laughs | Vesper's waltz turned major (D major) with your melody joining as a counter-line; the God of Death's trumpet laughs; dawn = full strings | **MUST (build)** |
| 15 | **Ending: spare, few jobs** (30-35 s) | Time pays | Fitted | No | The God of Death's as a ticking clock; yours aging | Her tune slowed into a minor clock tick (the hourglass); your melody slower and lower; Vesper's tune low at the end (she stayed) | **MUST (build)** |
| 16 | **Credits** (60-90 s) | Send-off; doubles as the trailer's music bed | 60-90 s | No | All three | A medley: your melody as a full rock piece, Vesper's waltz in the middle, out on the God of Death's lounge waltz | **Trailer** (the trailer's music comes from this and the in-run set) |
| 17 | **Stats screen** | Numbers count up | Under the screen | Loop | Yours | The title loop's quiet half (reuse) | **MUST (03:00)** via reuse |
| 18 | **After the credits** (NICE, 8-10 s) | The fruit basket sting | 8 s | No | The God of Death's | Her trumpet's bored slide down (the tag of her theme) | Later |

**Also planned (system, not tracks):** Time Dilation thins the music to a muffled pulse (low-pass + slowed playback on
the music bus); dark zones drop the drums; the Rewind could reverse the music for its 0.5 s.

**The Dark modifier hook (built 2026-10-18, `docs/HELL-SHADOW.md` section 3):** `HellShadowAudio.Depth` (0..1) is how
hidden the player is; on the current tune apply `DarkHush.MusicDrums(depth)` to the drums stem (1 to 0),
`DarkHush.MusicCutoff(depth)` to the music bus low-pass (22 kHz to 900 Hz) and `DarkHush.MusicDrone(depth)` to a held
"Dark" drone layer (0 to 0.8). The world and enemies are already hushed under it (-7 dB, low-pass to 1.2 kHz).

**Round 1 sketches (2026-10-18, page above):** the 9 melody statements (piano), the in-run skeleton as layered builds
(Normal Still, Quiet Still, Danger Still, Normal Fast, Normal Flying) plus a switch demo through all of them, the God of
Death's lounge waltz (51 s) and Vesper's waltz (55 s) as layered builds, and four rhythm-guitar sounds to pick from.
Files and scripts: `FableAnimationTests/audio/hell-music/` (`scripts/melodies.py`, `inrun.py`, `themes.py`,
`guitar_tones.py`, `page.py`; shared `hell_lib.py`). Every render measured with the ears tool and levelled to -18 LUFS.
Waiting on the creator before anything else is made.
