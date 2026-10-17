# HellDescent: the sound pass (day 1 evening, 2026-10-17)

Not committed. Written by the sound-pass agent on the evening of day 1, after the abilities, enemies and rooms branches
were merged. It lists every sound the game needs. For each one it gives:

- what the sound physically is (its identity);
- where it hooks into the code;
- the premier game reference, where one helps;
- 2-4 free candidates, with the best one picked.

It also lists the sounds that have no good free match and how we'd make them, and the music direction options.
The sounds are now installed with defaults for the open questions (see "Installed" below); the creator's answers swap them in one place.

**How the candidates were found and judged:**

- **Search.** The creator's order: free preset content first. Freesound was searched with 136 queries (115 terms; 21 of them also for CC-BY), filtered to CC0 and
  then CC-BY and sorted by downloads. 159 picks were downloaded. Each one's licence was re-read from its own page: 135 are
  CC0 1.0, 24 are CC BY (20 at 4.0, 4 at 3.0), and none are NC. OpenGameArt was searched for music (CC0 and CC-BY, licence read from
  each page).
- **Cutting and loudness.** Each candidate was cut to its useful piece and loudness-matched to -18 LUFS, with the peak
  kept under -1 dBFS (the page flags the few that hit the ceiling first).
- **Judging.** Each piece was measured with the `ears` tool (length, attack, tail) and scored by the CLAP listener
  against a plain description of the sound, and against the premier reference where there is one.
- **The picks.** "Pick" is the best CLAP match, with a CC0 file preferred over a CC-BY one when they are close. A few were
  overridden for timing (noted). The creator's ears decide.
- **Premier references.** These are short cuts of YouTube clips, fetched with yt-dlp for listening only. They are stored in
  `FableAnimationTests/art/hell-pass/sound/refs/` and are never shipped.
  - Every reference was checked for what it contains. The Valorant Omen clip turned out to be his smoke ability plus a
    voice line, so it was dropped. The Tracer clip is cut before her voice line.
  - The Titanfall 2 clip is gameplay mixed with footsteps and ambience. It is labelled as such.
  - YouTube began demanding a sign-in part way through, so there are no more fetches (no Ultrakill or Hades sound
    references beyond the music).

Listening page: `FableAnimationTests/art/review/2026-10-17/hell-sound-pass.html` (audio in `hell-sound-pass/audio/`). Downloads, references and the working files: `FableAnimationTests/art/hell-pass/sound/` (downloads/freesound/meta.json holds every downloaded sound's title, author, URL and licence as read from its page).

## Questions for the creator

1. Shadow Step's character: ghostly and smoky (a backwards whoosh, a cloth snap, a soft puff: Dishonored's Blink) or punchy and magical (a bright warp with a boom on arrival)?
2. Rewind: a deep swirling sub-bass rewind like Tracer's (felt in the chest), or a literal tape-and-clock rewind (mechanical, ticking)? It is the Time theme's loudest sound.
3. Time Dilation: should slow time also muffle and pitch down EVERYTHING you hear (Max Payne's bullet time, with a heartbeat), or only play a whoosh in and out? The muffle is a mixer change on the existing pitch hook, not a sound file.
4. Enemy voices: do the imp and the demon get creature voices (screeches, growls, death cries), or stay voiceless so only fire, metal and lava speak?
5. Spawn warnings: enemies appear with no warning today. Add a one-second fire-rift sound (plus a visual) where each one is about to appear?
6. Room tones: one shared lava bed everywhere, or a different bed per floor (dry ash wind / forge and chains / cold glass drone)?
7. CC-BY is allowed with a credit line in the repo. Keep CC-BY picks, or CC0 only?
8. Music: which direction do we compose toward (see the last section): DOOM-style metal with choir, Metal: Hellsinger's beat-driven metal, Hades' fast catchy rock, or dark ambient between fights with metal only in combat? And do you want a free CC0 track as a stand-in until ours is built?

## Installed (sprint lane 7, 2026-10-18)

The sound is in the game. Nothing in it is final: the creator's ears decide, and every pick swaps in one place.

**Where things are:**

- **The swap table:** `Packages/com.vibejam.jamkit/Runtime/Skyline/HellSound/HellSoundTable.cs` (`HellSoundTable.Rows`). Every cue
  maps to one file. To change a sound, change its row's file. An empty file silences the cue.
- **The files:** `Assets/JamKit/Templates/SkylineRepo/Resources/SkylineAudio/hell/` (75 WAVs, 44.1 kHz 16-bit, mono except the
  beds). They are built by `tools/audio/make-hell-sounds.py`, which also writes the credits, so a file never ships without its credit.
  Re-run it after changing a recipe:
  `uv run --with numpy --with scipy --with pyloudnorm --with soundfile python tools/audio/make-hell-sounds.py`.
- **The wiring:** `Scripts/Audio/HellSoundBinder.cs`. It watches the game's public state and events and plays the cues, so
  `Scripts/Hell/*` and the other lanes' scripts carry no sound code. `Scripts/Bots/SkylineBot.HellSound.cs` is a read-only view of
  the ooze's beat, the demon's stride and the ooze's pools.
- **The credits:** `CREDITS.md` (Audio section). The credits screen can read either
  `Resources/HellAudioCredits.txt` (licence TAB credit line) or `JamKit.Skyline.HellAudioCredits.Lines()` (CC BY first).

**The mix (`SkylineMix`, `SkylineSounds`):**

- **New categories.** `Abilities` sits at the guns' level (-2 dB). The ability cues used to fall into Bots, 10 dB down. `World`
  sits at -6 dB and holds the rooms.
- **The rest of the routing.** Enemies stay in Bots (-12 dB). Beds go to Ambience (-8 dB), and the screen moments (the skill pick,
  the death, room cleared) go to Feedback.
- **How a name finds its bus.** `SkylineMix.CategoryOf` reads any `hell/...` name's bus from the table. `SkylineSounds.Get` plays
  the file the table names.
- **The scythe's beam is never voice-gated.** It is a warning.
- **File levels:**

  | Family | Level |
  |---|---|
  | Abilities | -22 LUFS |
  | Enemy one-shots | -16 to -19 LUFS |
  | World | -18 to -22 LUFS |
  | Beds | -24 LUFS |
  | Quiet loops (aim hum, lantern, heartbeat, pools) | -24 to -34 LUFS |

  Every file peaks under -1 dBFS, through a soft limiter.

**Time Dilation:**

- `HellAbilityAudio.PitchChanged` drives `SkylineSounds.SetWorldTime`.
- **What slows.** The Bots, World and Ambience categories play at the time scale, never lower than 0.6. A low-pass closes from
  22 kHz down to 900 Hz at the slowest dilation.
- **What doesn't.** The player's guns, abilities, feedback and the screen moments are untouched.
- **The heartbeat.** A heartbeat loop fades in with the depth.

**Rewind (decided):**

- **One-shots fire only on a change seen in play.** A Rewind, a room restart or a room change re-bases every watch
  (`HellSoundRules.Watch`) silently, whichever way the restore moved it. Doing the thing again afterwards plays again, because the
  player really did it again.
- **Loops follow the restored state.** If you rewind back into carrying the lantern, its hum comes back.
- **A scythe's warning restarts where it still ENDS on its beam.** Its speed is bent every frame to match the time left, whatever
  the time scale.
- **The rewind's own sound** is `rewind` (played by HellAbilities). The binder adds `rewind_end`, the tick 0.3 s later, when the
  flash ends.

**Bound by the binder (no edits in the owners' scripts):**

- **Abilities:**
  - the aim hum while E is held;
  - Umbral Chain's free step (a step that spent no charge);
  - Ambush (it wraps `ShotDamageScale`, and a tripled shot plays the crack);
  - Borrowed Time (`SkylineBot.Killed`);
  - Ashwalker's lava steps.
- **Enemies:**
  - the imp's hover loop;
  - the ooze's throb, one blorp on each `OozePulse` beat, so it quickens through the wind-up;
  - the demon's footsteps, one per leg plant of its stride;
  - the ooze's pools (a burning loop, then the cooling hiss);
  - the scythes' warning and beam, for the demons' volleys and the rooms' beam lanes.
- **Rooms:**
  - a bed per floor;
  - the 3 nearest fire lights (brazier crackle);
  - random lava bubbles within 18 m;
  - the lava sizzle every 0.5 s while burning;
  - a sink (a teleport with no step, rewind, death or room change behind it);
  - the platforms' shake and fall;
  - the sigils, the lantern (pick up, carry hum, place), the anchors (hit, warded deflect, break), the brand (a charge loop whose
    pitch follows the meter, then the seal), the wells (a grind while sealing, then sealed);
  - when any objective is done, the gate grinding open and the room-cleared bell;
  - the shaft (a start whoosh, wind rising with the fall, a landing thump);
  - the skill pick's open and hover;
  - the death boom.
- **The bolt's hit** is the one cue that needed a line in another lane's file (below).

**One-line edits in other lanes' files:**

- `Scripts/Bots/HellEnemyFx.cs:44`, in `EmberHit`:
  `SkylineSounds.PlayAt(JamKit.Skyline.HellSoundCues.BoltHit, at);`

Nothing in `Scripts/Hell/*` was touched. The other edits are the sound system's own files: `SkylineMix.cs` and `SkylineSounds.cs`.

**Not wired, on purpose:**

- `hell/spawn/warning` (file `spawn/warning`) is ready for the enemy-readability lane. It is one `SkylineSounds.PlayAt` where the
  warning starts.
- **The bolt whiz-by.** No file was made. `SkylineBolt` would need a closest-approach check; the pick is 414422 (CC BY) or 855248
  (CC0).
- **The Nightfall hum and collapse.** The one-shot carries a 3 s tail.
- **Music.** It stays silent. The hook is in `HellSoundBinder.TickBeds`: add `hell/music/<floor>` rows.

### Synthesised or layered (the gaps)

- **Scythe path warning:** synthesised. 0.9 s, a thin tone rising an octave, with a tremolo speeding from 8 to 30 Hz and a hard
  stop. It is bent in code so it ends on the beam.
- **Scythe appear** (the demon's `attack`): the dark-rift whoosh, the unsheath shing pitched down 5 semitones, and a synthesised
  12 Hz whir.
- **Demon footstep** (made as the gap note says; the creator's pick): the heavy body fall's first 0.25 s from its hit (0.355 s
  in; the old cut began 80 ms early, in the scuff, and ran 0.27 s), pitched down 4 semitones, with a small rock-crumble tail
  (262118, -12 dB, 0.12 s after the hit).
- **Imp death:** the goblin death cry cut at 0.4 s, straight into the fireball, with an ember crackle tail.
- **Ooze throb:** one bubble, found automatically as the loudest event in the bubbling-mud recording, and retriggered on each beat
  in code.
- **Nightfall:** a synthesised dark inrush, the dark-spell rise and the sub boom (low-passed).
- **Rewind:**
  - the start: the tape stop into the reverse vortex, a synthesised sub swirl falling from 75 to 38 Hz with a wobble, and the
    reverse cymbal;
  - **rewind_end:** the reverse-cymbal swallow, then the clock tick, with a synthesised thump.
- **Shaft:** synthesised gusting wind (a loop that rises in code), the low whoosh at the start, and a landing thump (the body fall
  plus the sub boom).
- **Platform shake:** the stone drag twice over the earthquake's low-passed rumble, rising, 0.9 s.
- **Lava bubbles:** the three loudest single pops from the lava recording.
- **Anchor hit** (the creator's pick): the "Metal impact" (530486) alone, from its hit, its ring cut to 0.55 s. The chain
  jangle is gone.
- **Skill pick:**
  - **open:** the long ethereal woosh with the dark rise under it;
  - **hover:** a synthesised soft 1.3 kHz tick.
- **Beds** (9 s seamless loops, stereo), each with the lava sea under it:
  - Ember Gate: the tonal wind;
  - Molten Works: the industrial drone plus chains;
  - Black Glass: the terror drone.

### The creator's picks (2026-10-18)

| Sound | Was | Now (in `make-hell-sounds.py`) |
|---|---|---|
| Shade (the decoy) | "Creepy whispering" (234044) | **"Breath of Death"** (60571, CC BY 4.0, credited): 1.6-3.6 s of the recording. Its 1.4 s fade-up is cut (the decoy appears at once), a 0.12 s fade in, 0.8 s fade out. 2.0 s. |
| Scythe demon footsteps | body fall from 0.28 s (80 ms of scuff before the hit), 0.27 s, -4 st, crumble | **Made as described**: the body fall's first 0.25 s from the hit, -4 st, small rock-crumble tail. 0.41 s. |
| Scythe demon death | "Dragon's dying breath" (276577) 0.3-2.8 s + fireball | **The first part of "Dragon death"** (366096): 0.08-1.42 s, the roar from its rise to just before its second breath (onset at 1.44 s), 0.5 s fade; the fireball 8 dB under it. 1.34 s. |
| Chain anchor hit | metal-impact transient + chain jangle | **"Metal impact"** (530486) alone: from the hit, ring cut at 0.55 s. |
| Ooze well sealing / sealed | "Opening the sarcophagus" (457529) loop; push + steam | **"Heavy Stone Push"** (558254) for both. The sealing loop is the push's grinding body (0.10-0.78 s) twice, the second a semitone lower, crossfaded seamless (1.15 s). Sealed: the whole push to its stop (0.04-0.86 s, the lid lands) then the steam hiss 6 dB under. |

Also in this pass: every file is now kept under -1 dBTP true peak as well as -1 dBFS sample peak (the imp's hover loop measured
+1.1 dBTP, the pool loop +0.5, the ooze's death 0.0 before the fix).

### The mix by importance (2026-10-18)

Every cue now has its own gain (`GainDb`, the last argument of its row in `HellSoundTable.cs`), applied on top of its bus in
`SkylineMix.GainOf`. The gains put every cue at its tier's **heard level**: the file's loudest 400 ms (momentary max, LUFS; fair
between a 0.3 s hit and a 3 s swell) plus its bus (Abilities and UI -2 dB, World -6, enemies -12, beds -8) plus its gain. Bright
cues (over 85% of their energy above 2 kHz, the "harsh" column) sit 2 dB under their tier: they cut through at the same LUFS.
A gain never pushes bus x gain over 1 (the voice volume is clamped there); `HellSoundTests.TheMixByImportanceKeepsItsOrder`
checks it. To re-level after changing a file: measure it, then set its row's gain to (tier target - momentary max - bus).

| Tier | Heard target (momentary max + bus + gain) | Cues |
|---|---|---|
| 1 warnings + the player's own | -24 LUFS (measured after: -26.1 to -23.8) | 33 cues |
| 2 hits and deaths | -27 LUFS (measured after: -29.2 to -26.8) | 18 cues |
| 3 objective feedback | -28 LUFS (measured after: -30.0 to -27.8) | 13 cues |
| 4 enemy presence + world one-shots | -31 LUFS (measured after: -33.2 to -30.8) | 11 cues |
| 5 loops | -35 LUFS (measured after: -35.2 to -34.8) | 9 cues |
| 6 beds | -36 LUFS (measured after: -36.0 to -35.8) | 3 cues |

Every cue, measured with the ears tool (`listen.py measure`: integrated LUFS, true peak, attack) and the momentary max, before and after its gain. "Heard" = momentary max + bus (+ gain). Flags: outliers more than 4 dB off their tier before the gain, and bright cues.

| Cue | File | Bus | Tier | LUFS (ears) | True peak dBTP (ears) | Attack ms (ears) | Momentary max | Heard before | Gain dB | Heard now | Flag |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `shadowstep_out` | `abilities/shadowstep_out` | Abilities | 1 | -22.0 | -2.3 | 129 | -20.8 | -22.8 | -1.0 | -23.8 |  |
| `shadowstep_out_free` | `abilities/shadowstep_out_free` | Abilities | 1 | -22.0 | -3.2 | 116 | -21.6 | -23.6 | -0.5 | -24.1 |  |
| `shadowstep_in` | `abilities/shadowstep_in` | Abilities | 1 | -22.0 | -3.9 | 9 | -20.1 | -22.1 | -2.0 | -24.1 |  |
| `shadowstep_denied` | `abilities/denied` | Abilities | 1 | -23.0 | -7.8 | 21 | -23.0 | -25.0 | -1.0 | -26.0 | bright, -2 dB |
| `rewind_denied` | `abilities/denied` | Abilities | 1 | -23.0 | -7.8 | 21 | -23.0 | -25.0 | -1.0 | -26.0 | bright, -2 dB |
| `nightfall_denied` | `abilities/denied` | Abilities | 1 | -23.0 | -7.8 | 21 | -23.0 | -25.0 | -1.0 | -26.0 | bright, -2 dB |
| `rewind` | `abilities/rewind` | Abilities | 1 | -20.0 | -10.7 | 264 | -18.6 | -20.6 | -3.5 | -24.1 |  |
| `rewind_end` | `abilities/rewind_end` | Abilities | 1 | -22.2 | -1.0 | 51 | -22.2 | -24.2 | +0.0 | -24.2 |  |
| `dilation_in` | `abilities/dilation_in` | Abilities | 1 | -22.0 | -8.8 | 350 | -20.4 | -22.4 | -1.5 | -23.9 |  |
| `dilation_out` | `abilities/dilation_out` | Abilities | 1 | -22.0 | -6.0 | 46 | -19.7 | -21.7 | -2.5 | -24.2 |  |
| `ambush` | `abilities/ambush` | Abilities | 1 | -20.0 | -1.9 | 1 | -14.8 | -16.8 | -7.0 | -23.8 | was 7 dB over its tier |
| `shade` | `abilities/shade` | Abilities | 1 | -24.0 | -10.8 | 723 | -22.2 | -24.2 | +0.0 | -24.2 |  |
| `nightfall` | `abilities/nightfall` | Abilities | 1 | -20.0 | -5.1 | 303 | -18.2 | -20.2 | -4.0 | -24.2 |  |
| `borrowed_time` | `abilities/borrowed_time` | Abilities | 1 | (under 0.4 s) | -6.3 | 4 | -23.6 | -25.6 | -0.5 | -26.1 | bright, -2 dB |
| `lava_hop` | `abilities/lava_hop` | Abilities | 1 | -24.0 | -11.4 | 190 | -23.6 | -25.6 | -0.5 | -26.1 | bright, -2 dB |
| `combustion` | `abilities/combustion` | Abilities | 1 | -20.3 | -1.0 | 34 | -17.2 | -19.2 | -5.0 | -24.2 | was 5 dB over its tier |
| `skill_pick` | `ui/skill_pick` | Feedback | 1 | -24.0 | -16.4 | 2 | -21.8 | -23.8 | +0.0 | -23.8 |  |
| `wheel_bind` | `ui/skill_pick` | Feedback | 1 | -24.0 | -16.4 | 2 | -21.8 | -23.8 | +0.0 | -23.8 |  |
| `death` | `ui/death` | Feedback | 1 | -20.0 | -4.9 | 343 | -17.1 | -19.1 | -5.0 | -24.1 | was 5 dB over its tier |
| `fire-imp/telegraph` | `fire-imp/telegraph` | Bots | 1 | -17.9 | -4.4 | 56 | -17.8 | -29.8 | +6.0 | -23.8 | was 6 dB under its tier |
| `fire-imp/attack` | `fire-imp/attack` | Bots | 1 | -17.0 | -2.9 | 56 | -16.2 | -28.2 | +4.0 | -24.2 | was 4 dB under its tier |
| `scythe-demon/telegraph` | `scythe-demon/telegraph` | Bots | 1 | -18.0 | -5.6 | 58 | -16.1 | -28.1 | +2.0 | -26.1 | bright, -2 dB; was 4 dB under its tier |
| `scythe-demon/attack` | `scythe-demon/attack` | Bots | 1 | -18.1 | -1.0 | 41 | -15.8 | -27.8 | +4.0 | -23.8 |  |
| `scythe/warn` | `scythe/warn` | Bots | 1 | -16.0 | -2.9 | 554 | -12.9 | -24.9 | +1.0 | -23.9 |  |
| `scythe/beam` | `scythe/beam` | Bots | 1 | -16.0 | -2.7 | 65 | -14.0 | -26.0 | +2.0 | -24.0 |  |
| `lava-ooze/telegraph` | `lava-ooze/telegraph` | Bots | 1 | -18.1 | -1.0 | 15 | -16.5 | -28.5 | +4.5 | -24.0 | was 5 dB under its tier |
| `lava-ooze/attack` | `lava-ooze/attack` | Bots | 1 | -17.5 | -1.0 | 3 | -14.1 | -26.1 | +2.0 | -24.1 |  |
| `spawn/warning` | `spawn/warning` | Bots | 1 | -17.2 | -1.0 | 145 | -14.6 | -26.6 | +2.5 | -24.1 |  |
| `ending/hold` | `world/sigil` | Abilities | 1 | -19.0 | -5.7 | 231 | -15.8 | -17.8 | -6.0 | -23.8 | was 6 dB over its tier |
| `ending/spare` | `world/brand_done` | Abilities | 1 | -19.4 | -1.0 | 175 | -14.5 | -16.5 | -7.5 | -24.0 | was 7 dB over its tier |
| `ending/kill` | `world/anchor_break` | Abilities | 1 | -17.0 | -1.6 | 89 | -13.2 | -15.2 | -9.0 | -24.2 | was 9 dB over its tier |
| `ending/out` | `ui/room_cleared` | Feedback | 1 | -22.0 | -8.1 | 97 | -18.9 | -20.9 | -3.0 | -23.9 |  |
| `ending/card` | `ui/skill_open` | Feedback | 1 | -26.0 | -9.8 | 435 | -23.1 | -25.1 | +1.0 | -24.1 |  |
| `fire-imp/bolt_hit` | `fire-imp/bolt_hit` | Bots | 2 | -19.7 | -1.0 | 3 | -17.0 | -29.0 | +2.0 | -27.0 |  |
| `fire-imp/hurt` | `fire-imp/hurt` | Bots | 2 | (under 0.4 s) | -1.8 | 30 | -17.4 | -29.4 | +2.5 | -26.9 |  |
| `fire-imp/wreck` | `fire-imp/wreck` | Bots | 2 | -16.2 | -1.0 | 317 | -13.5 | -25.5 | -1.5 | -27.0 |  |
| `fire-imp/plate_shed` | `fire-imp/plate_shed` | Bots | 2 | -19.0 | -2.4 | 171 | -18.9 | -30.9 | +2.0 | -28.9 | bright, -2 dB |
| `scythe-demon/hurt` | `scythe-demon/hurt` | Bots | 2 | -18.3 | -1.0 | 6 | -16.8 | -28.8 | +2.0 | -26.8 |  |
| `scythe-demon/plate_shed` | `scythe-demon/plate_shed` | Bots | 2 | -17.5 | -1.0 | 1 | -17.5 | -29.5 | +2.5 | -27.0 |  |
| `scythe-demon/wreck` | `scythe-demon/wreck` | Bots | 2 | -16.0 | -2.3 | 71 | -14.2 | -26.2 | -1.0 | -27.2 |  |
| `lava-ooze/hurt` | `lava-ooze/hurt` | Bots | 2 | (under 0.4 s) | -1.0 | 3 | -18.1 | -30.1 | +3.0 | -27.1 |  |
| `lava-ooze/wreck` | `lava-ooze/wreck` | Bots | 2 | -18.3 | -1.0 | 7 | -15.8 | -27.8 | -1.0 | -28.8 | bright, -2 dB |
| `lava-ooze/plate_shed` | `lava-ooze/plate_shed` | Bots | 2 | -18.1 | -1.0 | 100 | -18.1 | -30.1 | +3.0 | -27.1 |  |
| `world/lava_touch` | `world/lava_touch` | World | 2 | -19.0 | -2.1 | 66 | -18.3 | -24.3 | -4.5 | -28.8 | bright, -2 dB |
| `world/sink` | `world/sink` | World | 2 | -18.0 | -1.0 | 459 | -15.4 | -21.4 | -5.5 | -26.9 | was 6 dB over its tier |
| `world/platform_shake` | `world/platform_shake` | World | 2 | -19.0 | -3.3 | 487 | -17.6 | -23.6 | -3.5 | -27.1 |  |
| `world/platform_fall` | `world/platform_fall` | World | 2 | -18.1 | -1.0 | 7 | -15.8 | -21.8 | -5.0 | -26.8 | was 5 dB over its tier |
| `world/anchor_hit` | `world/anchor_hit` | World | 2 | -18.1 | -1.0 | 1 | -15.5 | -21.5 | -5.5 | -27.0 | was 6 dB over its tier |
| `world/anchor_deflect` | `world/anchor_deflect` | World | 2 | -20.0 | -4.5 | 52 | -17.2 | -23.2 | -6.0 | -29.2 | bright, -2 dB |
| `world/anchor_break` | `world/anchor_break` | World | 2 | -17.0 | -1.6 | 89 | -13.2 | -19.2 | -8.0 | -27.2 | was 8 dB over its tier |
| `ending/rumble` | `world/platform_shake` | World | 2 | -19.0 | -3.3 | 487 | -17.6 | -23.6 | -3.5 | -27.1 |  |
| `skill_open` | `ui/skill_open` | Feedback | 3 | -26.0 | -9.8 | 435 | -23.1 | -25.1 | -3.0 | -28.1 |  |
| `wheel_open` | `ui/skill_open` | Feedback | 3 | -26.0 | -9.8 | 435 | -23.1 | -25.1 | -3.0 | -28.1 |  |
| `room_cleared` | `ui/room_cleared` | Feedback | 3 | -22.0 | -8.1 | 97 | -18.9 | -20.9 | -7.0 | -27.9 | was 7 dB over its tier |
| `world/shaft_start` | `world/shaft_start` | World | 3 | -20.0 | -7.2 | 1077 | -15.8 | -21.8 | -6.0 | -27.8 | was 6 dB over its tier |
| `world/shaft_land` | `world/shaft_land` | World | 3 | -18.1 | -1.0 | 134 | -16.6 | -22.6 | -5.5 | -28.1 | was 5 dB over its tier |
| `world/sigil` | `world/sigil` | World | 3 | -19.0 | -5.7 | 231 | -15.8 | -21.8 | -6.0 | -27.8 | was 6 dB over its tier |
| `world/lantern_pickup` | `world/lantern_pickup` | World | 3 | -21.2 | -1.0 | 1 | -17.4 | -23.4 | -4.5 | -27.9 | was 5 dB over its tier |
| `world/lantern_place` | `world/lantern_place` | World | 3 | -20.0 | -7.8 | 374 | -17.0 | -23.0 | -7.0 | -30.0 | bright, -2 dB; was 5 dB over its tier |
| `world/brand_done` | `world/brand_done` | World | 3 | -19.4 | -1.0 | 175 | -14.5 | -20.5 | -7.5 | -28.0 | was 7 dB over its tier |
| `world/well_sealed` | `world/well_sealed` | World | 3 | -19.0 | -2.9 | 386 | -16.1 | -22.1 | -6.0 | -28.1 | was 6 dB over its tier |
| `world/gate_open` | `world/gate_open` | World | 3 | -19.0 | -4.7 | 76 | -17.7 | -23.7 | -4.5 | -28.2 | was 4 dB over its tier |
| `ending/soul` | `world/lantern_pickup` | World | 3 | -21.2 | -1.0 | 1 | -17.4 | -23.4 | -4.5 | -27.9 | was 5 dB over its tier |
| `ending/escape` | `world/shaft_start` | World | 3 | -20.0 | -7.2 | 1077 | -15.8 | -21.8 | -6.0 | -27.8 | was 6 dB over its tier |
| `lava_walk` | `abilities/lava_walk` | Abilities | 4 | (under 0.4 s) | -8.4 | 47 | -26.7 | -28.7 | -4.5 | -33.2 | bright, -2 dB |
| `skill_hover` | `ui/skill_hover` | Feedback | 4 | (under 0.4 s) | -9.6 | 1 | -30.4 | -32.4 | +1.5 | -30.9 |  |
| `wheel_close` | `ui/skill_hover` | Feedback | 4 | (under 0.4 s) | -9.6 | 1 | -30.4 | -32.4 | +1.5 | -30.9 |  |
| `fire-imp/alert` | `fire-imp/alert` | Bots | 4 | -18.0 | -2.6 | 57 | -17.4 | -29.4 | -1.5 | -30.9 |  |
| `scythe-demon/alert` | `scythe-demon/alert` | Bots | 4 | -17.1 | -1.0 | 434 | -15.8 | -27.8 | -3.0 | -30.8 |  |
| `scythe-demon/footstep` | `scythe-demon/footstep` | Bots | 4 | -17.9 | -1.0 | 94 | -17.9 | -29.9 | -1.0 | -30.9 |  |
| `lava-ooze/throb` | `lava-ooze/throb` | Bots | 4 | (under 0.4 s) | -2.2 | 11 | -20.0 | -32.0 | +1.0 | -31.0 |  |
| `lava-ooze/alert` | `lava-ooze/alert` | Bots | 4 | -19.1 | -1.0 | 23 | -17.9 | -29.9 | -1.0 | -30.9 |  |
| `lava-ooze/pool_cool` | `lava-ooze/pool_cool` | Bots | 4 | -24.0 | -8.5 | 549 | -21.2 | -33.2 | +0.0 | -33.2 | bright, -2 dB |
| `world/lava_bubble` | `world/lava_bubble_1` | World | 4 | (under 0.4 s) | -1.0 | 15 | -20.1 | -26.1 | -5.0 | -31.1 | was 5 dB over its tier |
| `ending/step` | `abilities/shadowstep_in` | World | 4 | -22.0 | -3.9 | 9 | -20.1 | -26.1 | -5.0 | -31.1 | was 5 dB over its tier |
| `shadowstep_aim` | `abilities/shadowstep_aim` | Abilities | 5 | -34.0 | -21.3 | 179 | -33.3 | -35.3 | +0.5 | -34.8 |  |
| `dilation_heartbeat` | `abilities/dilation_heartbeat` | Abilities | 5 | -28.0 | -10.5 | 2 | -25.2 | -27.2 | -8.0 | -35.2 | was 8 dB over its tier |
| `fire-imp/hover_loop` | `fire-imp/hover_loop` | Bots | 5 | -26.4 | -1.0 | 27 | -22.7 | -34.7 | -0.5 | -35.2 |  |
| `lava-ooze/pool_loop` | `lava-ooze/pool_loop` | Bots | 5 | -25.5 | -1.0 | 291 | -22.5 | -34.5 | -0.5 | -35.0 |  |
| `world/fire_loop` | `world/fire_loop` | World | 5 | -24.0 | -11.4 | 28 | -23.1 | -29.1 | -6.0 | -35.1 | was 6 dB over its tier |
| `world/shaft_wind` | `world/shaft_wind` | World | 5 | -22.0 | -7.4 | 14 | -19.9 | -25.9 | -9.0 | -34.9 | was 9 dB over its tier |
| `world/lantern_carry` | `world/lantern_carry` | World | 5 | -30.0 | -16.6 | 11 | -28.7 | -34.7 | -0.5 | -35.2 |  |
| `world/brand_charge` | `world/brand_charge` | World | 5 | -24.0 | -4.5 | 1149 | -22.6 | -28.6 | -6.5 | -35.1 | was 6 dB over its tier |
| `world/well_grind` | `world/well_grind` | World | 5 | -24.0 | -10.3 | 336 | -23.4 | -29.4 | -5.5 | -34.9 | was 6 dB over its tier |
| `bed/0` | `beds/ember_gate` | Ambience | 6 | -24.0 | -1.0 | 158 | -20.4 | -28.4 | -7.5 | -35.9 | was 8 dB over its tier |
| `bed/1` | `beds/molten_works` | Ambience | 6 | -24.0 | -5.8 | 3665 | -17.0 | -25.0 | -11.0 | -36.0 | was 11 dB over its tier |
| `bed/2` | `beds/black_glass` | Ambience | 6 | -24.0 | -11.7 | 97 | -22.8 | -30.8 | -5.0 | -35.8 | was 5 dB over its tier |
| (reference) `writ/shot` | | Guns | - | -27.8 | -8.5 | 2 | -27.8 | -29.8 | 0 | -29.8 | the player's gun |
| (reference) `breacher/blast` | | Guns | - | -18.6 | -2.9 | 4 | -14.0 | -16.0 | 0 | -16.0 | the player's gun |

### Swap points for the creator's answers

Every swap is in `HellSoundTable.cs`. Change a row's file, or flip a switch under the table:

| Question | Default now | To swap |
|---|---|---|
| 1. Shadow Step | ghostly (backwards whoosh + cloth snap; puff + weight) | Punchy: point `shadowstep_out`/`_in` at new files (recipe: 735062 fast warp in + 512389 bass impact in `make-hell-sounds.py`) |
| 2. Rewind | deep swirl ending on a tick | Tape-and-clock: rebuild `abilities/rewind` from 372876 tape rewind + 547305 tick tock |
| 3. Time Dilation | muffle + slow + heartbeat | `DilationMuffle = false` (whooshes only), `DilationHeartbeat = false` |
| 4. Enemy voices | the imp and the demon have voices | Voiceless: set the `alert` and `hurt` rows' file to `""` |
| 5. Spawn warnings | file + cue ready, not wired | the enemy-readability lane calls `hell/spawn/warning` |
| 6. Room tones | a bed per floor | `OneBedForAll = true` (the Ember Gate's everywhere) |
| 7. CC BY | kept (4 ship: `dilation_out` 123438, `lava-ooze/wreck` 167074, `brand_done` 854670, `shade` 60571 (the creator's pick); credited) | CC0 only: `dilation_out` -> rebuild from 683101; `lava-ooze/wreck` -> 315931 + 155660; `brand_done` -> 18654 gong |
| 8. Music | silent | add `hell/music/<floor>` rows and a `Loop` in `HellSoundBinder.TickBeds` |

### Swap for the originals later

Every Freesound file in the game is cut from the **HQ MP3 preview** (128 kbps; the same recording and licence as the original, but
lossy). Originals need a Freesound login.

- **Swap first** (long or airy sounds, where MP3 smear is audible):
  - the three beds (397092, 393398, 155142, 474852, 108265);
  - the bells and gong (76405, 18654, 61521);
  - the whispers and pads (234044, 427292, 808032);
  - the brand charge (407052);
  - the steam (365071, 234782).
- **Then the rest:** short transients hide it.

To swap, put the original WAVs under `FableAnimationTests/art/hell-pass/sound/downloads/freesound/` and point `SRC` in the script at
pieces cut from them.

### Check in Unity, by ear

1. **The ability level against the gun.** Abilities are now at the gun's level, where they used to be 10 dB under: Rewind, Shadow
   Step and Nightfall should sit just under a Writ shot.
2. **The scythe warning.** It must end exactly as the beam fires, at normal speed and in Time Dilation.
3. **The Time Dilation muffle.** Is the depth right (900 Hz at the slowest; the pitch floor is 0.6)? Does the heartbeat sit under
   the gunfire?
4. **The ooze throb** at idle and through its wind-up: not machine-gunning.
5. **The demon's footsteps** against its stride animation.
6. **The beds' level** under combat, and whether the loops are seamless (9 s).
7. **The sink detection.** It plays on a lava sink and must stay silent on a Shadow Step, a Rewind, a death and a room change.
8. **The imp's hover loop.** Three imps at once: it is gated by distance only (22 m).
9. **The brazier crackle** near the lava (the fire lights are made from the lava).
10. **The mix by importance** (2026-10-18): the enemies' telegraphs and attacks went up 1-6 dB to the abilities' level, the
    world and objective one-shots came down 4-9 dB, the beds 5-11 dB (the Molten Works bed was the loudest outlier). If a tier
    is wrong as a whole, move its target in the table above and shift that tier's gains by the same amount.

### Every cue

| Cue | File | Bus | Made from (Freesound id, licence) | Note |
|---|---|---|---|---|
| `shadowstep_aim` | `abilities/shadowstep_aim` (loop) | Abilities | 427292 "Another Atmospheric Pad" (CC0 1.0) | while E is held (the landing preview) |
| `shadowstep_out` | `abilities/shadowstep_out` | Abilities | 446010 "Backwards Whoosh" (CC0 1.0); 428336 "Canvas Dropcloth Snap 2" (CC0 1.0) | ghostly: backwards whoosh + cloth snap |
| `shadowstep_out_free` | `abilities/shadowstep_out_free` | Abilities | 446010 "Backwards Whoosh" (CC0 1.0); 428336 "Canvas Dropcloth Snap 2" (CC0 1.0); 802472 "Whoosh - Magic disappear" (CC0 1.0) | Umbral Chain's free step (layered on the out) |
| `shadowstep_in` | `abilities/shadowstep_in` | Abilities | 714257 "Puff of Smoke" (CC0 1.0); 504626 "BODY FALL - V HVY - DIRT" (CC0 1.0) | a puff + weight |
| `shadowstep_denied` | `abilities/denied` | Abilities | 133449 "Cigarette Sizzle 02" (CC0 1.0) |  |
| `rewind_denied` | `abilities/denied` | Abilities | 133449 "Cigarette Sizzle 02" (CC0 1.0) |  |
| `nightfall_denied` | `abilities/denied` | Abilities | 133449 "Cigarette Sizzle 02" (CC0 1.0) |  |
| `rewind` | `abilities/rewind` | Abilities | 74385 "To Hell With Vinyl" (CC0 1.0); 280222 "Fast reverse vortex" (CC0 1.0); synthesised; 23127 "reverse cymbal" (CC0 1.0) | deep swirl (Tracer-like): tape stop, vortex, sub |
| `rewind_end` | `abilities/rewind_end` | Abilities | 23127 "reverse cymbal" (CC0 1.0); 188033 "Ticking Clock" (CC0 1.0); synthesised | 0.3 s after the rewind: the tick |
| `dilation_in` | `abilities/dilation_in` | Abilities | 366092 "Quick Zoom / Slow Down" (CC0 1.0) |  |
| `dilation_heartbeat` | `abilities/dilation_heartbeat` (loop) | Abilities | 22416 "Four Heartbeats HQ_BeatSmith" (CC0 1.0) | while time is slowed (DilationHeartbeat) |
| `dilation_out` | `abilities/dilation_out` | Abilities | 123438 "Slowdown_Short_Up" (CC BY 4.0) | CC BY |
| `ambush` | `abilities/ambush` | Abilities | 512389 "Bass Impact - Ki_01" (CC0 1.0); 234044 "Creepy Whispering" (CC0 1.0) | the tripled shot |
| `shade` | `abilities/shade` | Abilities | 60571 "BreathOfDeath" by gabemiller74 (CC BY 4.0) | the creator's pick; CC BY, credited |
| `nightfall` | `abilities/nightfall` | Abilities | synthesised; 657541 "Crystal Magic - Dark Spell Rise" (CC0 1.0); 189779 "Big Boom" (CC0 1.0) | layered: inrush, dark rise, sub boom |
| `borrowed_time` | `abilities/borrowed_time` | Abilities | 211203 "assymetric_tick" (CC0 1.0) | a tick on each kill |
| `lava_hop` | `abilities/lava_hop` | Abilities | 234782 "Steam/hiss" (CC0 1.0) |  |
| `lava_walk` | `abilities/lava_walk` | Abilities | 155660 "cig_extinguish" (CC0 1.0) | Ashwalker's steps on lava |
| `combustion` | `abilities/combustion` | Abilities | 431174 "Fireball Explosion" (CC0 1.0) |  |
| `skill_pick` | `ui/skill_pick` | Feedback | 199940 "07-confirmation" (CC0 1.0) | the confirm |
| `skill_open` | `ui/skill_open` | Feedback | 462879 "Long Ethereal Woosh" (CC0 1.0); 657541 "Crystal Magic - Dark Spell Rise" (CC0 1.0) |  |
| `skill_hover` | `ui/skill_hover` | Feedback | synthesised |  |
| `death` | `ui/death` | Feedback | 181889 "Whosh - Reversed/Reverbed breath" (CC0 1.0); 430978 "Low impact" (CC0 1.0) | the AGAIN banner |
| `room_cleared` | `ui/room_cleared` | Feedback | 76405 "Old Church Bell" (CC0 1.0) | one bell toll |
| `fire-imp/hover_loop` | `fire-imp/hover_loop` (loop) | Bots | 71417 "wingflap_fast" (CC0 1.0); 650574 "fire crackling loop" (CC0 1.0) | wings + crackle |
| `fire-imp/telegraph` | `fire-imp/telegraph` | Bots | 172631 "Power Up Charge [Remix of LegoLunatic's Charged laser 151243]" (CC0 1.0); 534702 "Lighting a Fire" (CC0 1.0) |  |
| `fire-imp/attack` | `fire-imp/attack` | Bots | 267887 "Short-Fireball-Woosh" (CC0 1.0) |  |
| `fire-imp/bolt_hit` | `fire-imp/bolt_hit` | Bots | 539972 "Fire Burst Flash" (CC0 1.0) |  |
| `fire-imp/alert` | `fire-imp/alert` | Bots | 202100 "Goblin Scream" (CC0 1.0) | voice (empty = voiceless) |
| `fire-imp/hurt` | `fire-imp/hurt` | Bots | 442815 "Goblin Snarl" (CC0 1.0) | voice |
| `fire-imp/wreck` | `fire-imp/wreck` | Bots | 249813 "Goblin Death" (CC0 1.0); 431174 "Fireball Explosion" (CC0 1.0); 650574 "fire crackling loop" (CC0 1.0) | shriek cut by a fireball |
| `fire-imp/plate_shed` | `fire-imp/plate_shed` | Bots | 144159 "BreakingNeck" (CC0 1.0) |  |
| `scythe-demon/alert` | `scythe-demon/alert` | Bots | 222523 "Deep Monster Growl" (CC0 1.0) | voice |
| `scythe-demon/hurt` | `scythe-demon/hurt` | Bots | 444445 "Demon Growl for Halloween Spooky Creepy Scary Monster Ghoul Ghost Sounds" (CC0 1.0) | voice |
| `scythe-demon/telegraph` | `scythe-demon/telegraph` | Bots | 581594 "Sword draw unsheathe" (CC0 1.0) |  |
| `scythe-demon/attack` | `scythe-demon/attack` | Bots | 657653 "Black Portal - Dark Dimension Rift" (CC0 1.0); 107589 "unsheath_sword" (CC0 1.0); synthesised | the volley appears: rift + shing + whir |
| `scythe-demon/footstep` | `scythe-demon/footstep` | Bots | 504626 "BODY FALL - V HVY - DIRT" (CC0 1.0); 262118 "Kirkstall Rock Crumble " (CC0 1.0) | one per leg plant |
| `scythe-demon/plate_shed` | `scythe-demon/plate_shed` | Bots | 616492 "clank" (CC0 1.0); 530486 "Metal Impact" (CC0 1.0) |  |
| `scythe-demon/wreck` | `scythe-demon/wreck` | Bots | 366096 "Dragon Death" (CC0 1.0), its first part; 431174 "Fireball Explosion" (CC0 1.0) |  |
| `scythe/warn` | `scythe/warn` | Bots | synthesised | synthesised, 0.9 s, ends exactly on the beam |
| `scythe/beam` | `scythe/beam` | Bots | 369516 "Energy Blast" (CC0 1.0); 447941 "Blast + Flamethrower cooldown" (CC0 1.0) |  |
| `lava-ooze/throb` | `lava-ooze/throb` | Bots | 456806 "Hot Bubbling Mud" (CC0 1.0) | one blorp per OozePulse beat |
| `lava-ooze/alert` | `lava-ooze/alert` | Bots | 456806 "Hot Bubbling Mud" (CC0 1.0) |  |
| `lava-ooze/telegraph` | `lava-ooze/telegraph` | Bots | 352904 "Bubbling Mud with Steam" (CC0 1.0) |  |
| `lava-ooze/attack` | `lava-ooze/attack` | Bots | 414296 "goreSplat" (CC0 1.0); 445109 "Mud Splat" (CC0 1.0) |  |
| `lava-ooze/hurt` | `lava-ooze/hurt` | Bots | 445109 "Mud Splat" (CC0 1.0) |  |
| `lava-ooze/wreck` | `lava-ooze/wreck` | Bots | 167074 "Slime Death (1)" (CC BY 4.0); 155660 "cig_extinguish" (CC0 1.0) | CC BY |
| `lava-ooze/plate_shed` | `lava-ooze/plate_shed` | Bots | 321478 "Concrete Hit 2" (CC0 1.0) |  |
| `lava-ooze/pool_loop` | `lava-ooze/pool_loop` (loop) | Bots | 382313 "Sizzle / Acid Burn" (CC0 1.0) | while a pool burns |
| `lava-ooze/pool_cool` | `lava-ooze/pool_cool` | Bots | 365071 "Steam" (CC0 1.0) |  |
| `spawn/warning` | `spawn/warning` | Bots | 186374 "Bonfire Being Lit" (CC0 1.0); 657653 "Black Portal - Dark Dimension Rift" (CC0 1.0) | NOT WIRED: for the enemy-readability lane |
| `world/lava_bubble` | `world/lava_bubble_1` | World | 145036 "lava" (CC0 1.0) | variants _1.._3 picked at random |
| `world/lava_touch` | `world/lava_touch` | World | 234782 "Steam/hiss" (CC0 1.0); 155660 "cig_extinguish" (CC0 1.0) | repeats while burning |
| `world/sink` | `world/sink` | World | 315931 "viscous liquid fall in" (CC0 1.0); 430978 "Low impact" (CC0 1.0); 234782 "Steam/hiss" (CC0 1.0) |  |
| `world/platform_shake` | `world/platform_shake` | World | 347149 "Stone,Block,Drag,Wood,Grind" (CC0 1.0); 222521 "Cracking Earthquake (cracking soil, cracking stone)" (CC0 1.0) |  |
| `world/platform_fall` | `world/platform_fall` | World | 321478 "Concrete Hit 2" (CC0 1.0); 567249 "Bricks/Stones/Rocks/Gravel Falling" (CC0 1.0) |  |
| `world/fire_loop` | `world/fire_loop` (loop) | World | 186924 "Fire_Loop" (CC0 1.0) | the nearest braziers |
| `world/shaft_start` | `world/shaft_start` | World | 697432 "Heavy Whoosh, Swoosh, Low, sfx, effect" (CC0 1.0) |  |
| `world/shaft_wind` | `world/shaft_wind` (loop) | World | synthesised | rises through the fall |
| `world/shaft_land` | `world/shaft_land` | World | 504626 "BODY FALL - V HVY - DIRT" (CC0 1.0); 189779 "Big Boom" (CC0 1.0) |  |
| `world/sigil` | `world/sigil` | World | 18654 "gong" (CC0 1.0); 186374 "Bonfire Being Lit" (CC0 1.0) |  |
| `world/lantern_pickup` | `world/lantern_pickup` | World | 171939 "Put Down Glass Object" (CC0 1.0); 234044 "Creepy Whispering" (CC0 1.0) |  |
| `world/lantern_carry` | `world/lantern_carry` (loop) | World | 808032 "Ambient F Sharp Minor Ethereal Choir Pad 1" (CC0 1.0) |  |
| `world/lantern_place` | `world/lantern_place` | World | 61521 "Final angelic sound" (CC0 1.0) |  |
| `world/anchor_hit` | `world/anchor_hit` | World | 530486 "Metal Impact" (CC0 1.0) |  |
| `world/anchor_deflect` | `world/anchor_deflect` | World | 570853 "magicShield_block" (CC0 1.0) |  |
| `world/anchor_break` | `world/anchor_break` | World | 703247 "Big falling debris (crash)" (CC0 1.0); 387240 "chain" (CC0 1.0) |  |
| `world/brand_charge` | `world/brand_charge` (loop) | World | 407052 "Power Charge" (CC0 1.0) | pitch follows the meter |
| `world/brand_done` | `world/brand_done` | World | 854670 "Sacred Rune Lock 02 - Arcane Seal Activation" (CC BY 4.0) | CC BY |
| `world/well_grind` | `world/well_grind` (loop) | World | 558254 "Heavy Stone Push" (CC0 1.0) | while a well is being sealed |
| `world/well_sealed` | `world/well_sealed` | World | 558254 "Heavy Stone Push" (CC0 1.0); 365071 "Steam" (CC0 1.0) |  |
| `world/gate_open` | `world/gate_open` | World | 352829 "Tomb Door Open, Stone Scrape" (CC0 1.0) |  |
| `bed/0` | `beds/ember_gate` (loop) | Ambience | 397092 "Soft Tonal Wind_02" (CC0 1.0); 474852 "Lava Loop 4" (CC0 1.0) | The Ember Gate: ash wind over the lava |
| `bed/1` | `beds/molten_works` (loop) | Ambience | 393398 "Industrial ambience" (CC0 1.0); 108265 "ChainsRattling" (CC0 1.0); 474852 "Lava Loop 4" (CC0 1.0) | The Molten Works: forge drone, chains, lava |
| `bed/2` | `beds/black_glass` (loop) | Ambience | 155142 "terror ambience" (CC0 1.0); 474852 "Lava Loop 4" (CC0 1.0) | The Black Glass: a cold drone |

## How the sounds go in (for whoever wires them) (the research notes; see "Installed" above for what was done)

- **Where the files go.** Every Hell sound plays through Skyline's sound system (`Scripts/Audio/SkylineSounds.cs`) from
  `Resources/SkylineAudio/<name>`.
  - **Ability cues:** `HellAbilityAudio.Play(id, pos)` (`Scripts/Fx/HellFx.cs`) already plays `hell/<id>` when the file
    exists. Dropping in `Resources/SkylineAudio/hell/shadowstep_out.wav` is enough.
  - **Enemy cues:** `SkylineBot.HellSound` (`Bots/SkylineBotHell.cs:212`) plays `hell/<fire-imp|scythe-demon|lava-ooze>/<telegraph|attack|plate_shed|wreck|alert|hurt>`.
    The existing files override the borrowed robot sounds.
- **The mixer groups.** These are categories in `SkylineMix`, not Unity mixer assets: Guns, Handling, Impacts, Bots,
  Ambience, Voices, Music, Feedback.
  - Any `hell/...` name falls into **Bots** (-12 dB), because `SkylineMix.CategoryOf` has no Hell branch.
  - Anything ending in `/idle_loop` goes to **Ambience**.
  - The ability cues are the player's own feedback, so they should get their own branch (say `hell/` ability ids ->
    Feedback). Otherwise they will sit 10 dB under the guns. This is a one-line change in `CategoryOf`.
- **Time Dilation's muffle.** `HellAbilityAudio.PitchChanged` fires with the dilation scale but is not wired to anything.
  The bullet-time feel (Max Payne) is a pitch plus low-pass on the effects and music, driven by it.
- **"Needs a new cue".** These sounds have no call in the code yet: everything under rooms, hazards and objectives
  (`Scripts/Hell/*` has no sound calls at all), the bolt fly-by and impact, the scythe's warning and beam, the lava pool,
  the loops, and the spawn warnings. Each is one `SkylineSounds.PlayAt(...)` at the line named in its "Hook".

## Abilities

### Shadow Step: the aim preview (loop while E is held)

**Is:** While you hold E and a marker shows where you'd land, a quiet breathy shadow hum hangs around you, like wind heard through a keyhole; it must stay under the enemies so you can still hear an ember coming.

**Hook:** NEW CUE needed: HellAbilities.cs (aim start / release); today only shadowstep_out/in/denied exist. (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Atmospheric pad | CC0 1.0 | 7.00 s · attack 486 ms · tail to -40 dB 1553 ms · -18.0 LUFS; CLAP 0.41 | https://freesound.org/people/Teacoma/sounds/427292/ |
|  | Soft tonal wind | CC0 1.0 | 8.00 s · attack 149 ms · tail to -40 dB 5728 ms · -18.0 LUFS; CLAP 0.36 | https://freesound.org/people/janbezouska/sounds/397091/ |
|  | Ghost whispers | CC0 1.0 | 8.00 s · attack 3518 ms · tail to -40 dB 4093 ms · -18.0 LUFS; CLAP 0.08 | https://freesound.org/people/Litruv/sounds/175944/ |

### Shadow Step: blink out

**Is:** You are pulled through a crack of darkness: a short dry inward suck of air (a whoosh played backwards) with a cloth-snap at the end, all inside a quarter to half a second.

**Hook:** Cue 'shadowstep_out' (HellAbilities.cs:282) -> Resources/SkylineAudio/hell/shadowstep_out (hook exists)

**Premier reference (listening only):** Dishonored: Blink: Corvo's short-range teleport: a sharp inward suck of air, a thin ghostly smear, and a soft pop as you arrive. Under half a second of body, a short airy tail.

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Backwards whoosh | CC0 1.0 | 0.66 s · attack 271 ms · tail to -40 dB 137 ms · -18.0 LUFS; CLAP 0.61 | https://freesound.org/people/SlavicMagic/sounds/446010/ |
|  | Reversed, reverbed breath | CC0 1.0 | 2.37 s · attack 233 ms · tail to -40 dB 560 ms · -18.0 LUFS; CLAP 0.55 | https://freesound.org/people/PrdiChlp/sounds/181889/ |
|  | Whoosh: magic disappear | CC0 1.0 | 0.24 s · attack 15 ms · tail to -40 dB 95 ms · -23.2 LUFS (peak-limited: quieter than -18); CLAP 0.58 | https://freesound.org/people/Sadiquecat/sounds/802472/ |
|  | Canvas dropcloth snap (layer) | CC0 1.0 | 0.64 s · attack 173 ms · tail to -40 dB 259 ms · -22.6 LUFS (peak-limited: quieter than -18); CLAP 0.41 | https://freesound.org/people/zembacraftworks/sounds/428336/ |

**Verdict:** The backwards whoosh (CC0) matches best (CLAP 0.61) and is the second closest to Dishonored's Blink (0.61; the cloth snap is 0.67 as a layer). It is 0.66 s: cut it to about 0.35 s and end it on the cloth snap.

**If none fits:** Best made as a layer: the backwards whoosh + the cloth snap at its end (cut to ~0.35 s).

### Shadow Step: arrive

**Is:** You burst out of smoke at the landing: a soft puff of displaced air and a low thump of your weight arriving, short, felt more than heard.

**Hook:** Cue 'shadowstep_in' (HellAbilities.cs:283) (hook exists)

**Premier reference (listening only):** Dishonored: Blink: Corvo's short-range teleport: a sharp inward suck of air, a thin ghostly smear, and a soft pop as you arrive. Under half a second of body, a short airy tail.

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Puff of smoke | CC0 1.0 | 0.50 s · attack 9 ms · tail to -40 dB 361 ms · -18.1 LUFS (peak-limited: quieter than -18); CLAP 0.31 | https://freesound.org/people/qubodup/sounds/714257/ |
|  | Poof in cloud | CC0 1.0 | 0.83 s · attack 40 ms · tail to -40 dB 479 ms · -19.6 LUFS (peak-limited: quieter than -18); CLAP 0.30 | https://freesound.org/people/Mateusz_Chenc/sounds/512217/ |
|  | Poof of smoke | CC0 1.0 | 0.92 s · attack 23 ms · tail to -40 dB 615 ms · -20.6 LUFS (peak-limited: quieter than -18); CLAP 0.27 | https://freesound.org/people/Planman/sounds/208111/ |
|  | Body fall, heavy, dirt (layer) | CC0 1.0 | 1.63 s · attack 303 ms · tail to -40 dB 914 ms · -21.1 LUFS (peak-limited: quieter than -18); CLAP -0.04 | https://freesound.org/people/leonelmail/sounds/504626/ |

**Verdict:** All three puffs are close (0.27-0.31). The 0.5 s 'Puff of smoke' is the most like the Blink reference (0.74) and has a 9 ms attack.

**If none fits:** Layer a puff with the first 0.2 s of the body fall for the weight.

### Denied (Shadow Step / Rewind / Nightfall not ready, or no darkness)

**Is:** A dry, muted fizzle like a match that won't light: tells you 'not now' without sounding like a menu error.

**Hook:** Cues 'shadowstep_denied' (HellAbilities.cs:224), 'rewind_denied' (:174), 'nightfall_denied' (HellSkills.cs:130) (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Cigarette sizzle | CC0 1.0 | 0.72 s · attack 21 ms · tail to -40 dB 694 ms · -18.0 LUFS; CLAP 0.47 | https://freesound.org/people/chaosportal/sounds/133449/ |
|  | UI game error | CC0 1.0 | 0.59 s · attack 5 ms · tail to -40 dB 560 ms · -18.0 LUFS; CLAP 0.41 | https://freesound.org/people/SignatureSoundsOrg/sounds/869285/ |
|  | Error buzz, soft (CC-BY) | CC BY 4.0 | 0.50 s · attack 3 ms · tail to -40 dB 269 ms · -18.4 LUFS (peak-limited: quieter than -18); CLAP 0.46 | "Error buzz (Soft UI SFX Free)" by heyheytheree ( https://freesound.org/people/heyheytheree/sounds/871919/ ) licensed under CC BY 4.0 |

### Rewind: start and the reverse sweep

**Is:** The whole world catches and is wound back four seconds: a tape-stop drop, then a reverse swirl rising under ticking, about 0.5 to 1 s, deep like Tracer's recall.

**Hook:** Cue 'rewind' (HellAbilities.cs:294): one cue today. A start/sweep/end split needs 'rewind_end' added where the 0.3 s flash ends. (hook exists)

**Premier reference (listening only):** Overwatch: Tracer's Recall: Tracer jumps back three seconds along her own path: a deep swirling rewind (sub-heavy: 69% of its energy is below 60 Hz in the full clip) with falling chirps, ending in a soft snap. The clip is cut before her voice line.

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Fast reverse vortex | CC0 1.0 | 3.00 s · attack 222 ms · tail to -40 dB 32 ms · -18.0 LUFS; CLAP 0.62 | https://freesound.org/people/earlcash05/sounds/280222/ |
|  | Reverse cymbal | CC0 1.0 | 2.00 s · attack 243 ms · tail to -40 dB 621 ms · -18.0 LUFS; CLAP 0.42 | https://freesound.org/people/John Rayson/sounds/23127/ |
|  | Tape recorder rewind | CC0 1.0 | 2.89 s · attack 7 ms · tail to -40 dB 2477 ms · -18.0 LUFS; CLAP 0.30 | https://freesound.org/people/simplewave/sounds/372876/ |
|  | Slowdown short up (CC-BY) | CC BY 4.0 | 1.86 s · attack 501 ms · tail to -40 dB 201 ms · -18.0 LUFS; CLAP 0.62 | "Slowdown_Short_Up.wav" by anomalous_underdog ( https://freesound.org/people/anomalous_underdog/sounds/123438/ ) licensed under CC BY 4.0 |

**Verdict:** Two strong matches: the CC0 reverse vortex (0.62) and the CC-BY slowdown-up (0.62, the closest to Tracer at 0.64).

**If none fits:** No single free sound does both halves: layer the tape-stop (74385) into the reverse vortex, with the clock tick on top.

### Rewind: end (snap back into place)

**Is:** Time lets go: a single hard clock 'tock' and a short swallowed reverse-cymbal ending, so you know you're back in control.

**Hook:** NEW CUE needed: 'rewind_end' at the end of the Rewind's 0.3 s flash (HellAbilities.cs / HellRewind.cs). (needs a new cue)

**Premier reference (listening only):** Overwatch: Tracer's Recall: Tracer jumps back three seconds along her own path: a deep swirling rewind (sub-heavy: 69% of its energy is below 60 Hz in the full clip) with falling chirps, ending in a soft snap. The clip is cut before her voice line.

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Asymmetric tick | CC0 1.0 | 0.96 s · attack 4 ms · tail to -40 dB 232 ms · -19.2 LUFS (peak-limited: quieter than -18); CLAP 0.29 | https://freesound.org/people/Diboz/sounds/211203/ |
|  | Tape stop ('To Hell With Vinyl') | CC0 1.0 | 0.80 s · attack 147 ms · tail to -40 dB 647 ms · -18.0 LUFS; CLAP 0.00 | https://freesound.org/people/dland/sounds/74385/ |
| **pick** | Ticking clock (one tick) | CC0 1.0 | 0.14 s · attack 23 ms · tail to -40 dB 27 ms · -20.4 LUFS (peak-limited: quieter than -18); CLAP 0.33 | https://freesound.org/people/AntumDeluge/sounds/188033/ |

### Time Dilation: enter slow time

**Is:** Time thickens: the air drops in pitch like a record slowing, the world goes muffled, a heartbeat starts underneath.

**Hook:** Cue 'dilation_in' (HellAbilities.cs:322); HellAbilityAudio.PitchChanged (the mixer pitch hook) is not wired yet. (hook exists)

**Premier reference (listening only):** Max Payne 3: Bullet Time: Slow-motion on: the world drops into a muffled low rumble with slow heartbeat thuds and a slowed breath; it leaves with a reverse riser (heard by the ears tool's listener: heartbeat thuds, low drone, slow breathing, reverse riser; no music or speech).

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Slowdown | CC0 1.0 | 3.28 s · attack 7 ms · tail to -40 dB 2655 ms · -18.0 LUFS; CLAP 0.23 | https://freesound.org/people/DarkoZL/sounds/101118/ |
| **pick** | Quick zoom / slow down | CC0 1.0 | 4.52 s · attack 817 ms · tail to -40 dB 1979 ms · -18.0 LUFS; CLAP 0.41 | https://freesound.org/people/bevibeldesign/sounds/366092/ |
|  | Pitch-shift whistle down | CC0 1.0 | 1.12 s · attack 46 ms · tail to -40 dB 499 ms · -18.0 LUFS; CLAP 0.37 | https://freesound.org/people/CJDeets/sounds/449642/ |
|  | Four heartbeats (layer) | CC0 1.0 | 3.61 s · attack 1 ms · tail to -40 dB 3473 ms · -18.0 LUFS; CLAP 0.15 | https://freesound.org/people/Lunardrive/sounds/22416/ |

**Verdict:** The quick zoom/slow-down (CC0, 0.41) is the best. Add the four heartbeats layer (the closest to Max Payne's bullet time, 0.52) if the creator wants the heartbeat.

**If none fits:** The muffling is a mixer job: a low-pass on the Effects group driven by PitchChanged, not a sample.

### Time Dilation: exit

**Is:** Time snaps back to speed: a short rising rush of air, the muffling opens.

**Hook:** Cue 'dilation_out' (HellAbilities.cs:330) (hook exists)

**Premier reference (listening only):** Max Payne 3: Bullet Time: Slow-motion on: the world drops into a muffled low rumble with slow heartbeat thuds and a slowed breath; it leaves with a reverse riser (heard by the ears tool's listener: heartbeat thuds, low drone, slow breathing, reverse riser; no music or speech).

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Slowdown short UP (CC-BY) | CC BY 4.0 | 1.86 s · attack 501 ms · tail to -40 dB 201 ms · -18.0 LUFS; CLAP 0.49 | "Slowdown_Short_Up.wav" by anomalous_underdog ( https://freesound.org/people/anomalous_underdog/sounds/123438/ ) licensed under CC BY 4.0 |
|  | Quick woosh | CC0 1.0 | 2.05 s · attack 17 ms · tail to -40 dB 82 ms · -19.5 LUFS (peak-limited: quieter than -18); CLAP 0.36 | https://freesound.org/people/florianreichelt/sounds/683101/ |
|  | Long wispy woosh (CC-BY) | CC BY 4.0 | 1.88 s · attack 116 ms · tail to -40 dB 449 ms · -18.0 LUFS; CLAP 0.36 | "long wispy woosh1.wav" by newagesoup ( https://freesound.org/people/newagesoup/sounds/377830/ ) licensed under CC BY 4.0 |

### Umbral Chain: the free second step

**Is:** Same blink-out, but the free one: a higher, brighter version of the step with a little shimmer, so you hear you got it for nothing.

**Hook:** No cue: the step plays shadowstep_out. A variant needs 'shadowstep_out_free' raised where the Umbral window is used (HellAbilities.cs). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Fast warp in | CC0 1.0 | 1.23 s · attack 280 ms · tail to -40 dB 370 ms · -18.0 LUFS; CLAP 0.42 | https://freesound.org/people/GammaGool/sounds/735062/ |
|  | Whoosh: magic disappear | CC0 1.0 | 0.24 s · attack 15 ms · tail to -40 dB 95 ms · -23.2 LUFS (peak-limited: quieter than -18); CLAP 0.30 | https://freesound.org/people/Sadiquecat/sounds/802472/ |

**If none fits:** Simplest: play shadowstep_out pitched up 3-4 semitones (no new file).

### Ambush: the tripled shot after a step

**Is:** The shot from the shadows lands with a deep shadow crack under the gun's own shot: a low boom and a whispery tail.

**Hook:** NEW CUE needed: where ShotDamageScale = 3 is consumed (HellSkills.cs, HellSkillAmbush). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Bass impact | CC0 1.0 | 3.94 s · attack 1 ms · tail to -40 dB 2225 ms · -18.0 LUFS; CLAP 0.59 | https://freesound.org/people/Kievinay/sounds/512389/ |
|  | Whoosh + boom | CC0 1.0 | 4.15 s · attack 40 ms · tail to -40 dB 2563 ms · -18.0 LUFS; CLAP 0.47 | https://freesound.org/people/Capashen/sounds/118934/ |
|  | Cinematic woosh | CC0 1.0 | 1.75 s · attack 130 ms · tail to -40 dB 700 ms · -18.0 LUFS; CLAP 0.48 | https://freesound.org/people/AudioPapkin/sounds/648729/ |

### Shade: the decoy

**Is:** Your shadow peels off and stays behind: a ghostly breath and a whisper that hangs where you stood.

**Hook:** Cue 'shade' (HellTargeting.cs:116) (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Creepy whispering | CC0 1.0 | 4.09 s · attack 297 ms · tail to -40 dB 638 ms · -18.0 LUFS; CLAP 0.53 | https://freesound.org/people/RaspberryTickle/sounds/234044/ |
|  | Whispers and screams | CC0 1.0 | 2.50 s · attack 1171 ms · tail to -40 dB 235 ms · -18.0 LUFS; CLAP 0.43 | https://freesound.org/people/Fyodore/sounds/211872/ |
|  | Breath of death (CC-BY) | CC BY 4.0 | 5.00 s · attack 1404 ms · tail to -40 dB 2029 ms · -18.0 LUFS; CLAP 0.48 | "BreathOfDeath" by gabemiller74 ( https://freesound.org/people/gabemiller74/sounds/60571/ ) licensed under CC BY 4.0 |

### Nightfall: the sphere of darkness

**Is:** A sphere of night blooms where you aim: a deep sub swell and a swirling inrush of dark air, holding for 8 s as a low hum, then a soft collapse.

**Hook:** Cue 'nightfall' (HellSkills.cs:136); a hum loop and a collapse would need cues at the sphere's life and end. (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Demonic anger, dark spell | CC0 1.0 | 5.86 s · attack 277 ms · tail to -40 dB 2544 ms · -17.8 LUFS · 2 separate sounds in the piece; CLAP 0.25 | https://freesound.org/people/LilMati/sounds/527636/ |
|  | Crystal magic: dark spell rise | CC0 1.0 | 6.00 s · attack 923 ms · tail to -40 dB 3152 ms · -18.0 LUFS; CLAP 0.28 | https://freesound.org/people/LilMati/sounds/657541/ |
|  | Shadow evil spell (CC-BY) | CC BY 4.0 | 1.87 s · attack 129 ms · tail to -40 dB 1096 ms · -18.0 LUFS; CLAP 0.30 | "Shadow Evil Spell Dark Magic" by EminYILDIRIM ( https://freesound.org/people/EminYILDIRIM/sounds/559087/ ) licensed under CC BY 4.0 |
|  | Big boom (sub layer) | CC0 1.0 | 5.00 s · attack 4 ms · tail to -40 dB 4840 ms · -18.0 LUFS; CLAP 0.31 | https://freesound.org/people/unfa/sounds/189779/ |

### Borrowed Time: time stolen on a kill

**Is:** Each kill shaves your cooldowns: one crisp clock tick, quiet, under the kill sound.

**Hook:** NEW CUE needed: HellSkillBorrowedTime on SkylineBot.Killed (HellSkills.cs). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Tick tock | CC0 1.0 | 0.85 s · attack 1 ms · tail to -40 dB 63 ms · -18.0 LUFS · 2 separate sounds in the piece; CLAP 0.30 | https://freesound.org/people/StarNinjas37/sounds/547305/ |
| **pick** | Asymmetric tick | CC0 1.0 | 0.96 s · attack 4 ms · tail to -40 dB 232 ms · -19.2 LUFS (peak-limited: quieter than -18); CLAP 0.29 | https://freesound.org/people/Diboz/sounds/211203/ |
|  | Old Scots clock, single ring | CC0 1.0 | 4.00 s · attack 35 ms · tail to -40 dB 3937 ms · -18.0 LUFS; CLAP 0.28 | https://freesound.org/people/fauxpress/sounds/42137/ |

### Ashwalker: lava-walk and the lava hop

**Is:** Your boots touch lava and it doesn't burn: a hiss and a crust crunch on each step; the hop is a sharp steam-spit as you spring off.

**Hook:** Cue 'lava_hop' (HellAbilities.cs:352); the lava-walk steps need a cue while LavaImmune and touching. (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Cigarette extinguish | CC0 1.0 | 1.06 s · attack 47 ms · tail to -40 dB 911 ms · -20.8 LUFS (peak-limited: quieter than -18); CLAP 0.40 | https://freesound.org/people/the_semen_incident/sounds/155660/ |
| **pick** | Steam / hiss | CC0 1.0 | 1.47 s · attack 205 ms · tail to -40 dB 733 ms · -18.0 LUFS; CLAP 0.62 | https://freesound.org/people/wubitog/sounds/234782/ |
|  | Sizzle / acid burn | CC0 1.0 | 5.00 s · attack 2921 ms · tail to -40 dB 2036 ms · -24.2 LUFS (peak-limited: quieter than -18); CLAP 0.36 | https://freesound.org/people/bottles/sounds/382313/ |

### Combustion: a burning enemy explodes

**Is:** A burning body bursts: a fireball 'whump' with crackle flying outward; chains of them go off 0.12 s apart, so it must be short.

**Hook:** Cue 'combustion' (HellSkills.cs:191) (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Fireball explosion | CC0 1.0 | 1.08 s · attack 58 ms · tail to -40 dB 605 ms · -18.0 LUFS; CLAP 0.48 | https://freesound.org/people/Blankened/sounds/431174/ |
|  | Fire explosion | CC0 1.0 | 4.27 s · attack 547 ms · tail to -40 dB 2033 ms · -18.0 LUFS; CLAP 0.53 | https://freesound.org/people/ReadeOnly/sounds/186932/ |
|  | Powerful magic fireball impact (CC-BY) | CC BY 4.0 | 2.63 s · attack 168 ms · tail to -40 dB 606 ms · -18.0 LUFS; CLAP 0.46 | "Powerful Magic Fireball Impact" by mikiko850 ( https://freesound.org/people/mikiko850/sounds/848736/ ) licensed under CC BY 4.0 |

**Verdict:** The 1.08 s CC0 fireball explosion, chosen for timing over the slower 4.3 s one.

## Enemies

### Fire imp: hover loop

**Is:** A one-metre burning imp hanging in the air: quick leathery wingbeats over a small steady flame crackle, so you can find it by ear.

**Hook:** NEW: a per-enemy loop (Hell enemies have the robots' idle hums switched off). SkylineSounds' '*/idle_loop' loop path exists; HellSound only plays one-shots. (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Wing flap, fast | CC0 1.0 | 6.00 s · attack 50 ms · tail to -40 dB 1690 ms · -26.5 LUFS (peak-limited: quieter than -18); CLAP 0.24 | https://freesound.org/people/philberts/sounds/71417/ |
|  | Wing flaps, layered | CC0 1.0 | 6.00 s · attack 4398 ms · tail to -40 dB 1202 ms · -26.1 LUFS (peak-limited: quieter than -18); CLAP 0.24 | https://freesound.org/people/martian/sounds/19295/ |
|  | Fire crackling loop (layer) | CC0 1.0 | 5.03 s · attack 2083 ms · tail to -40 dB 2753 ms · -30.3 LUFS (peak-limited: quieter than -18); CLAP 0.39 | https://freesound.org/people/soundofsong/sounds/650574/ |

**If none fits:** Layer: wings + crackle, loop-cut.

### Fire imp: the ember swells (0.4 s wind-up)

**Is:** An ember swells in its mouth: a quick rising inhale of fire that peaks right as it shoots; your warning.

**Hook:** hell/fire-imp/telegraph (SkylineBot.cs:638 -> HellSound) (hook exists)

**Premier reference (listening only):** DOOM: the imp's fireball: The imp gathers fire in its hand with a crackling rising roar, throws, and the ball flies with a fizzing whoosh; impacts are short fiery bursts. Several throws in a row.

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Gas fire catch (CC-BY) | CC BY 4.0 | 1.40 s · attack 56 ms · tail to -40 dB 457 ms · -18.0 LUFS; CLAP 0.42 | "gas_fire_catch.wav" by themfish ( https://freesound.org/people/themfish/sounds/45809/ ) licensed under CC BY 4.0 |
|  | Power-up charge | CC0 1.0 | 1.00 s · attack 20 ms · tail to -40 dB 705 ms · -18.0 LUFS; CLAP 0.42 | https://freesound.org/people/qubodup/sounds/172631/ |
| **pick** | Lighting a fire | CC0 1.0 | 2.50 s · attack 34 ms · tail to -40 dB 2342 ms · -19.8 LUFS (peak-limited: quieter than -18); CLAP 0.64 | https://freesound.org/people/plasterbrain/sounds/534702/ |

### Fire imp: ember bolt fired

**Is:** A fast fire bolt leaves its mouth: a short snapping 'fwoosh', tight because it flies at 90 m/s.

**Hook:** hell/fire-imp/attack (SkylineBotHell.cs:185) (hook exists)

**Premier reference (listening only):** DOOM: the imp's fireball: The imp gathers fire in its hand with a crackling rising roar, throws, and the ball flies with a fizzing whoosh; impacts are short fiery bursts. Several throws in a row.

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Short fireball woosh | CC0 1.0 | 1.19 s · attack 56 ms · tail to -40 dB 785 ms · -18.0 LUFS; CLAP 0.55 | https://freesound.org/people/wjl/sounds/267887/ |
|  | Fire magic | CC0 1.0 | 0.87 s · attack 1 ms · tail to -40 dB 822 ms · -19.6 LUFS (peak-limited: quieter than -18); CLAP 0.43 | https://freesound.org/people/qubodup/sounds/442872/ |
|  | Fireball cast 3 (CC-BY) | CC BY 4.0 | 1.37 s · attack 225 ms · tail to -40 dB 489 ms · -18.0 LUFS; CLAP 0.62 | "Fireball Cast 3" by LiamG_SFX ( https://freesound.org/people/LiamG_SFX/sounds/334237/ ) licensed under CC BY 4.0 |

**Verdict:** The CC0 short fireball woosh (0.55); the CC-BY fireball cast scores 0.62.

### Fire imp: bolt whiz-by

**Is:** A bolt that just missed: a hot fizzing zip past your ear, left to right; the near-miss you dodged.

**Hook:** NEW: SkylineBolt has no fly-by sound. Needs a closest-approach check in SkylineBolt (ember only). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Fireball, woosh, pass, fast | CC0 1.0 | 3.85 s · attack 740 ms · tail to -40 dB 2342 ms · -18.0 LUFS; CLAP 0.35 | https://freesound.org/people/julianmateo_/sounds/522705/ |
| **pick** | Whoosh (CC-BY) | CC BY 4.0 | 0.58 s · attack 190 ms · tail to -40 dB 293 ms · -18.0 LUFS; CLAP 0.45 | "Whoosh.wav" by _bepis ( https://freesound.org/people/_bepis/sounds/414422/ ) licensed under CC BY 4.0 |
|  | Real bullet flyby | CC0 1.0 | 0.08 s · attack 2 ms · tail to -40 dB 73 ms · -18.0 LUFS; CLAP 0.41 | https://freesound.org/people/qubodup/sounds/855248/ |

### Fire imp: bolt hits

**Is:** The ember strikes: a small fiery 'pff' flare and a crackle of sparks.

**Hook:** NEW: SkylineBolt's impact flare plays no sound (the player's hurt sound plays when you're hit). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Fire burst flash | CC0 1.0 | 0.76 s · attack 3 ms · tail to -40 dB 621 ms · -18.0 LUFS; CLAP 0.41 | https://freesound.org/people/Za-Games/sounds/539972/ |
|  | Fire burst (CC-BY) | CC BY 4.0 | 1.00 s · attack 105 ms · tail to -40 dB 644 ms · -18.0 LUFS; CLAP 0.45 | "Fire Burst.wav" by SilverIllusionist ( https://freesound.org/people/SilverIllusionist/sounds/472688/ ) licensed under CC BY 4.0 |

### Fire imp: alert / hurt

**Is:** A small vicious creature: a raspy hiss-screech when it sees you, a short yelp when shot.

**Hook:** hell/fire-imp/alert and /hurt (asked for; borrowing is off for these) (hook exists)

**Premier reference (listening only):** DOOM Eternal: imp voice set (isolated): The imp's isolated vocal set from the end of a sound compilation: raspy, hissing screeches and snarls, short (0.5 to 1.5 s each) with dry tails. Nine separate sounds in a row.

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Goblin snarl | CC0 1.0 | 1.97 s · attack 36 ms · tail to -40 dB 1903 ms · -18.0 LUFS; CLAP 0.16 | https://freesound.org/people/qubodup/sounds/442815/ |
| **pick** | Goblin scream | CC0 1.0 | 2.45 s · attack 154 ms · tail to -40 dB 1649 ms · -18.0 LUFS; CLAP 0.43 | https://freesound.org/people/spookymodem/sounds/202100/ |
|  | Imp laugh | CC0 1.0 | 0.98 s · attack 16 ms · tail to -40 dB 883 ms · -18.0 LUFS; CLAP 0.16 | https://freesound.org/people/scorpion67890/sounds/205751/ |

**Verdict:** The CC0 goblin scream (0.43). The snarl and the laugh score 0.16. The free files are less raspy than DOOM's imp: Gemini wasn't available to compare them by ear (the server was overloaded), so this one is for the creator's ears.

### Fire imp: death in fire

**Is:** The imp bursts into its own fire: a short shriek cut off by a fiery pop and falling embers.

**Hook:** hell/fire-imp/wreck (SkylineBotHell.cs:240) (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Goblin death | CC0 1.0 | 1.67 s · attack 49 ms · tail to -40 dB 561 ms · -18.0 LUFS; CLAP 0.21 | https://freesound.org/people/spookymodem/sounds/249813/ |
|  | Cave monster die (CC-BY) | CC BY 4.0 | 2.54 s · attack 137 ms · tail to -40 dB 2188 ms · -18.0 LUFS; CLAP 0.04 | "Cave Monster Die Sound.wav" by FiveBrosStopMosYT ( https://freesound.org/people/FiveBrosStopMosYT/sounds/535320/ ) licensed under CC BY 4.0 |
|  | Fireball explosion (layer) | CC0 1.0 | 1.08 s · attack 58 ms · tail to -40 dB 605 ms · -18.0 LUFS; CLAP 0.46 | https://freesound.org/people/Blankened/sounds/431174/ |

**If none fits:** No single file is it. Make it: the goblin death cry (249813) cut short at ~0.4 s, straight into the fireball explosion (431174).

### Fire imp: a horn breaks off

**Is:** A bone horn snaps: a dry crack.

**Hook:** hell/fire-imp/plate_shed (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Hard candy / bone crunch | CC0 1.0 | 1.10 s · attack 2 ms · tail to -40 dB 649 ms · -23.1 LUFS (peak-limited: quieter than -18); CLAP 0.39 | https://freesound.org/people/clif_creates/sounds/392883/ |
| **pick** | Breaking neck | CC0 1.0 | 1.03 s · attack 171 ms · tail to -40 dB 276 ms · -18.0 LUFS; CLAP 0.63 | https://freesound.org/people/cinevid/sounds/144159/ |

### Scythe demon: alert / growl

**Is:** A 2.8 m demon notices you: a deep chest growl, slow and heavy.

**Hook:** hell/scythe-demon/alert (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Deep monster growl | CC0 1.0 | 5.75 s · attack 954 ms · tail to -40 dB 3258 ms · -18.0 LUFS · 2 separate sounds in the piece; CLAP 0.62 | https://freesound.org/people/uagadugu/sounds/222523/ |
|  | Demon growl | CC0 1.0 | 4.54 s · attack 7 ms · tail to -40 dB 4063 ms · -20.4 LUFS (peak-limited: quieter than -18); CLAP 0.57 | https://freesound.org/people/Voices_Of_Marz/sounds/444445/ |
|  | Demon lion growl roar | CC0 1.0 | 4.00 s · attack 1399 ms · tail to -40 dB 2061 ms · -18.0 LUFS; CLAP 0.57 | https://freesound.org/people/Manim8/sounds/469123/ |

**Verdict:** All three growls match well (0.57-0.62). The deep growl contains two growls: pick one.

### Scythe demon: raises the scythe (0.6 s)

**Is:** Its scythe arm swings up over its head: a long metal blade drawn with a ringing scrape.

**Hook:** hell/scythe-demon/telegraph (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Unsheathed blade | CC0 1.0 | 3.00 s · attack 49 ms · tail to -40 dB 1533 ms · -18.0 LUFS; CLAP 0.37 | https://freesound.org/people/deleted_user_2906614/sounds/320521/ |
| **pick** | Sword draw | CC0 1.0 | 3.04 s · attack 58 ms · tail to -40 dB 1051 ms · -18.0 LUFS; CLAP 0.49 | https://freesound.org/people/SamsterBirdies/sounds/581594/ |
|  | Unsheath sword | CC0 1.0 | 1.01 s · attack 139 ms · tail to -40 dB 547 ms · -18.0 LUFS; CLAP 0.32 | https://freesound.org/people/Qat/sounds/107589/ |

### Scythe demon: footsteps

**Is:** Each heavy stride lands with a low stone-shaking thud.

**Hook:** NEW: the demon's stride has no step event (SkylineBotHell.cs leg swing). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Huge drum or fantasy footsteps | CC0 1.0 | 12.00 s · attack 1022 ms · tail to -40 dB 10634 ms · -18.0 LUFS; CLAP 0.30 | https://freesound.org/people/craigsmith/sounds/487076/ |
| **pick** | Body fall, heavy | CC0 1.0 | 1.63 s · attack 303 ms · tail to -40 dB 914 ms · -21.1 LUFS (peak-limited: quieter than -18); CLAP 0.34 | https://freesound.org/people/leonelmail/sounds/504626/ |

**If none fits:** No real footstep candidate. Make it: the heavy body fall (504626) cut to its first 0.25 s and pitched down 3-5 semitones, with a small stone-debris tail; one per leg plant.

### Demon scythe: appears spinning

**Is:** A spectral scythe blade tears into being and spins: a dark rift opening with a metallic whir.

**Hook:** NEW: HellScythe.cs (spawn); hell/scythe-demon/attack plays once per volley. (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Black portal: dark dimension rift | CC0 1.0 | 3.06 s · attack 41 ms · tail to -40 dB 875 ms · -18.0 LUFS; CLAP 0.13 | https://freesound.org/people/LilMati/sounds/657653/ |
| **pick** | Short whoosh (one of 13) | CC0 1.0 | 0.27 s · attack 22 ms · tail to -40 dB 136 ms · -18.0 LUFS; CLAP 0.26 | https://freesound.org/people/Kinoton/sounds/427979/ |

**If none fits:** No good match (CLAP 0.13-0.26). Make it: the dark-rift whoosh (657653) under a short metallic 'shing' (the unsheath, 107589) pitched down, with a spinning whir made by amplitude-modulating a filtered noise at ~12 Hz.

### Demon scythe: the path line (0.9 s warning)

**Is:** The red line projects the beam's path: a thin rising whine that tightens as it gets closer to firing, ending exactly when the beam goes.

**Hook:** NEW: HellScythe.cs telegraph phase (no sound). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Riser, short | CC0 1.0 | 3.05 s · attack 1238 ms · tail to -40 dB 85 ms · -18.0 LUFS; CLAP 0.17 | https://freesound.org/people/syntheffects/sounds/685256/ |
|  | md charge (CC-BY) | CC BY 4.0 | 0.37 s · attack 3 ms · tail to -40 dB 179 ms · -18.0 LUFS; CLAP 0.19 | "mdcharge.wav" by wildweasel ( https://freesound.org/people/wildweasel/sounds/39043/ ) licensed under CC BY 4.0 |
|  | Charging loop 2 (CC-BY) | CC BY 4.0 | 3.49 s · attack 4 ms · tail to -40 dB 3277 ms · -18.0 LUFS; CLAP 0.03 | "charging loop 2" by JavierZumer ( https://freesound.org/people/JavierZumer/sounds/257231/ ) licensed under CC BY 4.0 |

**If none fits:** No good match (CLAP 0.03-0.19). Synthesise: a thin saw/sine rising one octave over exactly 0.9 s with a faster tremolo near the end and a hard stop when the beam fires, so sound and line agree to the frame.

### Demon scythe: the beam

**Is:** The blade sweeps the line at 45 m/s and leaves a white-hot beam: a searing blowtorch roar that cuts in hard and dies over 0.3 s.

**Hook:** NEW: HellScythe.cs beam phase; also the rooms' HellBeamLane. (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Blast + flamethrower cooldown | CC0 1.0 | 1.92 s · attack 175 ms · tail to -40 dB 1565 ms · -18.0 LUFS; CLAP 0.33 | https://freesound.org/people/Breviceps/sounds/447941/ |
| **pick** | Energy blast | CC0 1.0 | 3.00 s · attack 538 ms · tail to -40 dB 1310 ms · -18.0 LUFS; CLAP 0.60 | https://freesound.org/people/Kinoton/sounds/369516/ |
|  | Beam (CC-BY) | CC BY 3.0 | 2.70 s · attack 135 ms · tail to -40 dB 1489 ms · -18.0 LUFS; CLAP 0.20 | "Beam.wav" by Deatlev ( https://freesound.org/people/Deatlev/sounds/254942/ ) licensed under CC BY 3.0 |

**Verdict:** Energy blast (CC0, 0.60), cut to 3 s. For a fire beam, layer the flamethrower blast (0.33) under it.

### Scythe demon: a pauldron breaks

**Is:** Rusted armour and bone tear off: a heavy metal clank.

**Hook:** hell/scythe-demon/plate_shed (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Clank | CC0 1.0 | 0.52 s · attack 1 ms · tail to -40 dB 236 ms · -18.0 LUFS; CLAP 0.26 | https://freesound.org/people/Empiremonkey/sounds/616492/ |
|  | Metal impact | CC0 1.0 | 1.79 s · attack 1 ms · tail to -40 dB 1517 ms · -23.0 LUFS (peak-limited: quieter than -18); CLAP 0.14 | https://freesound.org/people/Rickplayer/sounds/530486/ |

**If none fits:** Weak matches (CLAP 0.14-0.26). Fine as a layer under the gun's own impact; or the bone crack layered with the metal impact.

### Scythe demon: death

**Is:** It goes down in a burst of fire: a dying roar under a fireball and smoke.

**Hook:** hell/scythe-demon/wreck (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Dragon's dying breath | CC0 1.0 | 5.86 s · attack 455 ms · tail to -40 dB 2063 ms · -18.0 LUFS; CLAP 0.54 | https://freesound.org/people/MickBoere/sounds/276577/ |
|  | Dragon death | CC0 1.0 | 5.00 s · attack 277 ms · tail to -40 dB 521 ms · -18.0 LUFS; CLAP 0.48 | https://freesound.org/people/bevibeldesign/sounds/366096/ |
|  | Monsters scream (CC-BY) | CC BY 3.0 | 3.00 s · attack 407 ms · tail to -40 dB 1640 ms · -18.0 LUFS; CLAP 0.68 | "Monsters Scream" by XavTech ( https://freesound.org/people/XavTech/sounds/238548/ ) licensed under CC BY 3.0 |

### Lava ooze: the throb (loop)

**Is:** A glowing blob that throbs: thick lava bubbling with a slow pulse, quickening to 5 beats a second as it winds up.

**Hook:** NEW: a loop that follows OozePulse's beat (HellAttacks.cs); hell/lava-ooze/telegraph plays at the wind-up. (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Hot bubbling mud | CC0 1.0 | 6.00 s · attack 2045 ms · tail to -40 dB 3919 ms · -22.2 LUFS (peak-limited: quieter than -18); CLAP 0.44 | https://freesound.org/people/Breviceps/sounds/456806/ |
|  | Bubbling mud with steam | CC0 1.0 | 6.00 s · attack 1535 ms · tail to -40 dB 995 ms · -21.6 LUFS (peak-limited: quieter than -18); CLAP 0.30 | https://freesound.org/people/ProductionNow/sounds/352904/ |
|  | Slime movement (CC-BY) | CC BY 4.0 | 7.00 s · attack 5657 ms · tail to -40 dB 967 ms · -23.5 LUFS (peak-limited: quieter than -18) · 4 separate sounds in the piece; CLAP 0.34 | "Slime Movement" by StephenSaldanha ( https://freesound.org/people/StephenSaldanha/sounds/158759/ ) licensed under CC BY 4.0 |

**If none fits:** The quickening is best done in code: one short 'blorp' retriggered on each OozePulse beat.

### Lava ooze: the slam (lava spit)

**Is:** It squashes and slaps the floor: a heavy wet splat and a gush of molten rock spreading out.

**Hook:** hell/lava-ooze/attack (SkylineBotHell.cs:203) (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Mud splat | CC0 1.0 | 0.36 s · attack 3 ms · tail to -40 dB 292 ms · -20.6 LUFS (peak-limited: quieter than -18); CLAP 0.40 | https://freesound.org/people/Breviceps/sounds/445109/ |
|  | Splat (CC-BY) | CC BY 3.0 | 2.56 s · attack 358 ms · tail to -40 dB 747 ms · -18.0 LUFS; CLAP 0.52 | "Splat.wav" by Audionautics ( https://freesound.org/people/Audionautics/sounds/133968/ ) licensed under CC BY 3.0 |
| **pick** | Gore splat | CC0 1.0 | 2.86 s · attack 48 ms · tail to -40 dB 1194 ms · -18.0 LUFS · 2 separate sounds in the piece; CLAP 0.51 | https://freesound.org/people/ThefitzyG/sounds/414296/ |

### Lava pool: burning (loop) and cooling

**Is:** The pool it leaves: a hiss and steady sizzle while it burns (3 s), then a fading steam hiss as it crusts over.

**Hook:** NEW: HellLavaPool.cs (spread, burn, cool). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Sizzle / acid burn | CC0 1.0 | 5.00 s · attack 355 ms · tail to -40 dB 3635 ms · -30.4 LUFS (peak-limited: quieter than -18); CLAP 0.52 | https://freesound.org/people/bottles/sounds/382313/ |
|  | Steam | CC0 1.0 | 7.80 s · attack 393 ms · tail to -40 dB 6906 ms · -18.1 LUFS (peak-limited: quieter than -18); CLAP 0.23 | https://freesound.org/people/sniperous/sounds/365071/ |
|  | Acid bubbling | CC0 1.0 | 2.19 s · attack 485 ms · tail to -40 dB 909 ms · -19.2 LUFS (peak-limited: quieter than -18); CLAP 0.17 | https://freesound.org/people/spookymodem/sounds/202094/ |

### Lava ooze: death

**Is:** The blob bursts and slumps: a wet pop and a long cooling hiss.

**Hook:** hell/lava-ooze/wreck (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Slime death (CC-BY) | CC BY 4.0 | 0.61 s · attack 7 ms · tail to -40 dB 447 ms · -22.0 LUFS (peak-limited: quieter than -18); CLAP 0.72 | "Slime Death (1)" by DrMinky ( https://freesound.org/people/DrMinky/sounds/167074/ ) licensed under CC BY 4.0 |
|  | Viscous liquid fall in | CC0 1.0 | 0.60 s · attack 301 ms · tail to -40 dB 179 ms · -18.0 LUFS; CLAP 0.39 | https://freesound.org/people/bevibeldesign/sounds/315931/ |
|  | Cigarette extinguish (cooling layer) | CC0 1.0 | 1.06 s · attack 47 ms · tail to -40 dB 911 ms · -20.8 LUFS (peak-limited: quieter than -18); CLAP 0.32 | https://freesound.org/people/the_semen_incident/sounds/155660/ |

**Verdict:** Slime death (CC-BY, 0.72) is far ahead of the CC0 options (0.32-0.39).

### Lava ooze: crust breaks off

**Is:** A plate of cooled crust cracks off: a stony crunch.

**Hook:** hell/lava-ooze/plate_shed (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Concrete hit | CC0 1.0 | 0.84 s · attack 1 ms · tail to -40 dB 354 ms · -19.1 LUFS (peak-limited: quieter than -18); CLAP 0.42 | https://freesound.org/people/dslrguide/sounds/321478/ |
|  | Rock crumble | CC0 1.0 | 1.80 s · attack 1 ms · tail to -40 dB 1436 ms · -33.6 LUFS (peak-limited: quieter than -18) · 2 separate sounds in the piece; CLAP 0.40 | https://freesound.org/people/rokenjocu/sounds/262118/ |

### Spawn warning (any enemy)

**Is:** Before an enemy appears, the air tears open in fire: a one-second rising rush and roar, positional, so you know where to look.

**Hook:** NEW: enemies appear with no warning. Needs a short telegraph before SkylineBot.Spawn in HellRoom.SpawnWave / the wells. (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Black portal: dark dimension rift | CC0 1.0 | 3.06 s · attack 41 ms · tail to -40 dB 875 ms · -18.0 LUFS; CLAP 0.12 | https://freesound.org/people/LilMati/sounds/657653/ |
|  | Magical portal open (CC-BY) | CC BY 4.0 | 3.00 s · attack 1832 ms · tail to -40 dB 167 ms · -18.0 LUFS; CLAP 0.22 | "Magical portal open" by alanmcki ( https://freesound.org/people/alanmcki/sounds/401324/ ) licensed under CC BY 4.0 |
| **pick** | Bonfire being lit | CC0 1.0 | 3.00 s · attack 19 ms · tail to -40 dB 1571 ms · -18.0 LUFS · 3 separate sounds in the piece; CLAP 0.57 | https://freesound.org/people/samararaine/sounds/186374/ |

## World

### Lava ambience (bed)

**Is:** The lava sea: a deep slow rumble with thick bubbles rising and popping, positional near the basins.

**Hook:** NEW: HellLava.cs has no sound; an idle_loop-style loop per basin, or one bed per room. (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Lava loop 4 | CC0 1.0 | 10.00 s · attack 3648 ms · tail to -40 dB 6315 ms · -28.5 LUFS (peak-limited: quieter than -18); CLAP 0.60 | https://freesound.org/people/Fission9/sounds/474852/ |
|  | Volcano | CC0 1.0 | 10.00 s · attack 39 ms · tail to -40 dB 5633 ms · -18.0 LUFS; CLAP 0.52 | https://freesound.org/people/morganpurkis/sounds/385098/ |
|  | Lava loop (CC-BY) | CC BY 3.0 | 10.00 s · attack 423 ms · tail to -40 dB 4178 ms · -18.0 LUFS; CLAP 0.44 | "Lava loop.wav" by Audionautics ( https://freesound.org/people/Audionautics/sounds/133901/ ) licensed under CC BY 3.0 |

**Verdict:** Lava loop 4 (CC0, 0.60): a slow 3.6 s swell, low and dark. A good bed.

### Lava bubbling (one-shots)

**Is:** Single fat bubbles bursting on the surface, scattered at random near the lava.

**Hook:** NEW (with the bed). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Lava | CC0 1.0 | 5.64 s · attack 1378 ms · tail to -40 dB 4192 ms · -20.6 LUFS (peak-limited: quieter than -18); CLAP 0.33 | https://freesound.org/people/ATG142/sounds/145036/ |
|  | Boiling liquid | CC0 1.0 | 6.00 s · attack 2755 ms · tail to -40 dB 1777 ms · -19.0 LUFS (peak-limited: quieter than -18) · 7 separate sounds in the piece; CLAP 0.08 | https://freesound.org/people/Yoyodaman234/sounds/334671/ |

**If none fits:** Only one usable file (145036); the 'boiling liquid' piece is seven small boils (CLAP 0.08). Cut single pops out of the lava loop instead.

### Lava sizzle when you touch it

**Is:** Your boots hit lava: an instant angry sizzle and a steam spit, repeating while you burn.

**Hook:** NEW: HellLava.cs, on the Touching edge (FixedUpdate :81). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Steam / hiss | CC0 1.0 | 1.47 s · attack 205 ms · tail to -40 dB 733 ms · -18.0 LUFS; CLAP 0.54 | https://freesound.org/people/wubitog/sounds/234782/ |
|  | Cigarette extinguish | CC0 1.0 | 1.06 s · attack 47 ms · tail to -40 dB 911 ms · -20.8 LUFS (peak-limited: quieter than -18); CLAP 0.38 | https://freesound.org/people/the_semen_incident/sounds/155660/ |
|  | Sizzle / acid burn | CC0 1.0 | 3.00 s · attack 2921 ms · tail to -40 dB 61 ms · -22.7 LUFS (peak-limited: quieter than -18); CLAP 0.53 | https://freesound.org/people/bottles/sounds/382313/ |

### Sinking in lava

**Is:** You go under: a thick heavy gloop that swallows you, a hiss, then the respawn.

**Hook:** NEW: HellRoom.Sank (HellRoom.cs:226). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Viscous liquid fall in | CC0 1.0 | 0.60 s · attack 301 ms · tail to -40 dB 179 ms · -18.0 LUFS; CLAP 0.51 | https://freesound.org/people/bevibeldesign/sounds/315931/ |
|  | Walking through thick wet mud | CC0 1.0 | 3.00 s · attack 89 ms · tail to -40 dB 165 ms · -22.8 LUFS (peak-limited: quieter than -18); CLAP 0.06 | https://freesound.org/people/walthamstow_walker/sounds/543775/ |

**If none fits:** Layer the gloop with the death boom pitched down.

### Collapsing platform: shake

**Is:** The slab under you grinds and trembles for 0.65 to 0.9 s: stone grating on stone, rising.

**Hook:** NEW: HellCollapsePlatform.cs (shaking phase). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Stone block drag grind | CC0 1.0 | 1.12 s · attack 54 ms · tail to -40 dB 250 ms · -18.0 LUFS; CLAP 0.28 | https://freesound.org/people/Mediaman57/sounds/347149/ |
| **pick** | Cracking earthquake | CC0 1.0 | 3.00 s · attack 513 ms · tail to -40 dB 991 ms · -18.0 LUFS; CLAP 0.32 | https://freesound.org/people/uagadugu/sounds/222521/ |
|  | Rumble, fade in | CC0 1.0 | 3.00 s · attack 1264 ms · tail to -40 dB 432 ms · -18.0 LUFS; CLAP 0.25 | https://freesound.org/people/unfa/sounds/258341/ |

**If none fits:** Weak matches (CLAP 0.25-0.32). Make it: the stone drag (347149) looped under the earthquake's low rumble, rising over the shake time, then cut by the crack.

### Collapsing platform: crack and fall

**Is:** It gives way: a sharp crack, then rocks tumbling away into the lava below.

**Hook:** NEW: HellCollapsePlatform.cs (falls). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Rock crumble | CC0 1.0 | 1.80 s · attack 1 ms · tail to -40 dB 1436 ms · -33.6 LUFS (peak-limited: quieter than -18) · 2 separate sounds in the piece; CLAP 0.47 | https://freesound.org/people/rokenjocu/sounds/262118/ |
|  | Falling rock | CC0 1.0 | 5.04 s · attack 7 ms · tail to -40 dB 2928 ms · -18.0 LUFS · 2 separate sounds in the piece; CLAP 0.60 | https://freesound.org/people/spookymodem/sounds/202098/ |
| **pick** | Bricks / stones falling | CC0 1.0 | 4.20 s · attack 7 ms · tail to -40 dB 3635 ms · -18.0 LUFS; CLAP 0.76 | https://freesound.org/people/iwanPlays/sounds/567249/ |

**Verdict:** Bricks/stones falling (CC0, 0.76), the strongest match in the pass.

### Fire lights: crackle (loop)

**Is:** Each fire light is a burning brazier: soft wood-and-flame crackle you hear only up close.

**Hook:** NEW: the room's fire lights (HellRoom lights) have no sound. (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Crackling fire | CC0 1.0 | 8.00 s · attack 1032 ms · tail to -40 dB 4096 ms · -27.4 LUFS (peak-limited: quieter than -18); CLAP 0.36 | https://freesound.org/people/samarobryn/sounds/414767/ |
|  | Fire crackling loop | CC0 1.0 | 5.03 s · attack 2083 ms · tail to -40 dB 2753 ms · -30.3 LUFS (peak-limited: quieter than -18); CLAP 0.48 | https://freesound.org/people/soundofsong/sounds/650574/ |
| **pick** | Fire loop | CC0 1.0 | 8.00 s · attack 4 ms · tail to -40 dB 1789 ms · -18.0 LUFS; CLAP 0.52 | https://freesound.org/people/ReadeOnly/sounds/186924/ |

### Room tone: The Ember Gate

**Is:** Floor 1, ash and cinders: a dry hollow wind carrying ash, distant crackle, almost empty.

**Hook:** NEW: per-floor bed (HellRunDirector / HellRoom.Begin), on the Ambience bus. (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Soft tonal wind 02 | CC0 1.0 | 10.00 s · attack 5634 ms · tail to -40 dB 3604 ms · -28.9 LUFS (peak-limited: quieter than -18); CLAP 0.38 | https://freesound.org/people/janbezouska/sounds/397092/ |
|  | Cinematic deep rumble | CC0 1.0 | 10.00 s · attack 397 ms · tail to -40 dB 4232 ms · -18.0 LUFS; CLAP 0.23 | https://freesound.org/people/swiftoid/sounds/184313/ |

### Room tone: The Molten Works

**Is:** Floor 2, lava rivers and chains: a forge-like industrial drone, slow chain rattles, heavy rumble.

**Hook:** NEW: per-floor bed. (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Machine steampunk factory | CC0 1.0 | 10.00 s · attack 4980 ms · tail to -40 dB 1894 ms · -18.0 LUFS; CLAP 0.23 | https://freesound.org/people/szegvari/sounds/580633/ |
| **pick** | Industrial ambience | CC0 1.0 | 10.00 s · attack 3344 ms · tail to -40 dB 6281 ms · -18.0 LUFS; CLAP 0.41 | https://freesound.org/people/Lewente/sounds/393398/ |
|  | Chains rattling (layer) | CC0 1.0 | 8.00 s · attack 2529 ms · tail to -40 dB 4124 ms · -26.8 LUFS (peak-limited: quieter than -18) · 6 separate sounds in the piece; CLAP -0.03 | https://freesound.org/people/klakmart/sounds/108265/ |

### Room tone: The Black Glass

**Is:** Floor 3, obsidian and shadow: a cold glassy ringing drone, very low, still and wrong.

**Hook:** NEW: per-floor bed. (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Void monolith subharmonic drone | CC0 1.0 | 10.00 s · attack 1703 ms · tail to -40 dB 5787 ms · -18.0 LUFS; CLAP 0.20 | https://freesound.org/people/bassimat/sounds/854874/ |
| **pick** | Terror ambience | CC0 1.0 | 10.00 s · attack 34 ms · tail to -40 dB 9904 ms · -18.0 LUFS; CLAP 0.39 | https://freesound.org/people/burning-mir/sounds/155142/ |
|  | Singing glasses | CC0 1.0 | 8.00 s · attack 489 ms · tail to -40 dB 665 ms · -18.1 LUFS · 4 separate sounds in the piece; CLAP -0.04 | https://freesound.org/people/alienistcog/sounds/122758/ |

### The shaft fall

**Is:** Dropping down the shaft to the next floor: a long rising rush of wind past your ears.

**Hook:** NEW: HellRunDirector.EnterShaft (HellRunDirector.cs:160). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Heavy whoosh, low | CC0 1.0 | 6.77 s · attack 1393 ms · tail to -40 dB 4484 ms · -18.0 LUFS; CLAP 0.09 | https://freesound.org/people/FALL-E/sounds/697432/ |
|  | Long ethereal woosh | CC0 1.0 | 6.65 s · attack 435 ms · tail to -40 dB 3166 ms · -18.0 LUFS; CLAP 0.03 | https://freesound.org/people/newagesoup/sounds/462879/ |
|  | Long riser hit | CC0 1.0 | 8.00 s · attack 4582 ms · tail to -40 dB 60 ms · -18.1 LUFS; CLAP 0.03 | https://freesound.org/people/AudioPapkin/sounds/812629/ |

**If none fits:** No good match (CLAP 0.03-0.09). Make it: filtered noise whose cutoff and level rise over the fall (wind rushing faster), plus the low whoosh (697432) at the start and the 'land' thump at the bottom.

## Objectives and UI

### Sigil touched

**Is:** You touch a sigil and it ignites: a deep resonant bowl/gong strike with a breath of fire, answering you.

**Hook:** NEW: HellRoom.FixedUpdate, the Sigils objective's stop reached. (needs a new cue)

**Premier reference (listening only):** Dark Souls: lighting a bonfire: You kneel and the bonfire roars alight: a deep whoosh of ignition that swells over a second, then settles into crackle. The model for 'you touched something sacred and it answered'.

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Singing bell hit | CC0 1.0 | 6.00 s · attack 1 ms · tail to -40 dB 5888 ms · -18.0 LUFS; CLAP 0.56 | https://freesound.org/people/ryancacophony/sounds/202017/ |
|  | Gong | CC0 1.0 | 5.24 s · attack 93 ms · tail to -40 dB 4969 ms · -18.0 LUFS; CLAP 0.56 | https://freesound.org/people/nkuitse/sounds/18654/ |
|  | Gleam-glow chime | CC0 1.0 | 5.38 s · attack 430 ms · tail to -40 dB 1603 ms · -18.0 LUFS; CLAP 0.09 | https://freesound.org/people/newagesoup/sounds/351408/ |

**Verdict:** The singing bell and the gong tie (0.56). Neither is much like the Dark Souls bonfire (0.18-0.24), because that reference is a fire ignition, not a bell. Layer the gong with a fire catch if the creator wants the bonfire feel.

### Soul Lantern: pick up

**Is:** You lift a glass lantern holding a soul: a glass clink and a soft sigh as the soul notices you.

**Hook:** NEW: HellRoom.FixedUpdate, LanternCarry change (HellRoom.cs:179). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Put down glass object | CC0 1.0 | 1.00 s · attack 1 ms · tail to -40 dB 300 ms · -20.3 LUFS (peak-limited: quieter than -18); CLAP 0.33 | https://freesound.org/people/qubodup/sounds/171939/ |
|  | Place glass object | CC0 1.0 | 1.80 s · attack 1 ms · tail to -40 dB 1090 ms · -23.0 LUFS (peak-limited: quieter than -18) · 3 separate sounds in the piece; CLAP 0.32 | https://freesound.org/people/milpower/sounds/353105/ |

### Soul Lantern: carry loop

**Is:** While you carry it: a faint hum of a choir or whispers coming from the lantern, quiet.

**Hook:** NEW: while HellRoomFlags.Carry is true. (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Ethereal choir pad (F# minor) | CC0 1.0 | 10.00 s · attack 4759 ms · tail to -40 dB 508 ms · -18.0 LUFS; CLAP 0.45 | https://freesound.org/people/deadrobotmusic/sounds/808032/ |
|  | Spooky dark pad | CC0 1.0 | 10.00 s · attack 1971 ms · tail to -40 dB 7867 ms · -18.0 LUFS; CLAP 0.15 | https://freesound.org/people/JOHNWALLY/sounds/32805/ |
|  | Whispering | CC0 1.0 | 8.00 s · attack 4136 ms · tail to -40 dB 693 ms · -18.0 LUFS; CLAP 0.30 | https://freesound.org/people/PappaBert/sounds/405606/ |

### Soul Lantern: placed on the altar

**Is:** The soul is delivered: a warm resonant bell shimmer that blooms and fades.

**Hook:** NEW: LanternState.Placed. (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Final angelic sound | CC0 1.0 | 6.79 s · attack 375 ms · tail to -40 dB 3939 ms · -18.0 LUFS; CLAP 0.44 | https://freesound.org/people/hjohnl/sounds/61521/ |
|  | Stereo harmony bells | CC0 1.0 | 8.38 s · attack 966 ms · tail to -40 dB 5540 ms · -18.0 LUFS; CLAP 0.25 | https://freesound.org/people/newagesoup/sounds/400809/ |

### Chain anchor: hit

**Is:** Your shot bites the iron anchor: a heavy metal clank with the chain jangling.

**Hook:** NEW: HellAnchor hit (HellRoom.cs:246, ChainAnchors.HitResult). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Wood/metal chain smash | CC0 1.0 | 3.50 s · attack 32 ms · tail to -40 dB 1022 ms · -17.3 LUFS · 3 separate sounds in the piece; CLAP 0.16 | https://freesound.org/people/RutgerMuller/sounds/104003/ |
| **pick** | Chains | CC0 1.0 | 2.50 s · attack 275 ms · tail to -40 dB 591 ms · -18.0 LUFS; CLAP 0.37 | https://freesound.org/people/cribbler/sounds/370877/ |

**If none fits:** Weak matches. Make it: a metal impact transient (530486's first 0.15 s) plus a short chain jangle (370877).

### Chain anchor: warded deflect (too far)

**Is:** Out of range the ward turns your shot: a dull magical 'tunk' and a ricochet whine; clearly not damage.

**Hook:** NEW: HellAnchor deflect (HitResult.Deflected). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Attack blocked | CC0 1.0 | 2.42 s · attack 4 ms · tail to -40 dB 876 ms · -18.0 LUFS; CLAP 0.32 | https://freesound.org/people/LilMati/sounds/523760/ |
| **pick** | Magic shield block | CC0 1.0 | 2.23 s · attack 52 ms · tail to -40 dB 1665 ms · -18.0 LUFS; CLAP 0.39 | https://freesound.org/people/rafaelzimrp/sounds/570853/ |
|  | Bullet ricochet | CC0 1.0 | 2.36 s · attack 0 ms · tail to -40 dB 1991 ms · -22.7 LUFS (peak-limited: quieter than -18) · 2 separate sounds in the piece; CLAP 0.19 | https://freesound.org/people/aust_paul/sounds/30932/ |

### Chain anchor: breaks

**Is:** The anchor bursts and its chain whips loose: a crack, a heavy chain falling and metal debris.

**Hook:** NEW: ChainAnchors broken (SyncViews, HellRoom.cs:307). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Big falling debris crash | CC0 1.0 | 4.67 s · attack 89 ms · tail to -40 dB 3887 ms · -18.0 LUFS; CLAP 0.36 | https://freesound.org/people/xkeril/sounds/703247/ |
|  | Chain | CC0 1.0 | 3.00 s · attack 377 ms · tail to -40 dB 94 ms · -18.0 LUFS · 3 separate sounds in the piece; CLAP 0.27 | https://freesound.org/people/nettimato/sounds/387240/ |

### Brand: charging (loop)

**Is:** Standing in the brand circle: a rising hum, like a forge heating, that climbs with the meter and stops when you step out.

**Hook:** NEW: the tow circle's progress (Repossession / HellRoom brand). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Power charge | CC0 1.0 | 8.00 s · attack 3302 ms · tail to -40 dB 424 ms · -18.4 LUFS (peak-limited: quieter than -18); CLAP 0.59 | https://freesound.org/people/Sojan/sounds/407052/ |
|  | Charge up | CC0 1.0 | 6.35 s · attack 296 ms · tail to -40 dB 4301 ms · -18.0 LUFS; CLAP 0.39 | https://freesound.org/people/BrickDeveloper171/sounds/610361/ |
|  | Energy charging (CC-BY) | CC BY 4.0 | 8.00 s · attack 495 ms · tail to -40 dB 6048 ms · -18.0 LUFS; CLAP 0.45 | "Energy Charging" by nightcraftsounds ( https://freesound.org/people/nightcraftsounds/sounds/266170/ ) licensed under CC BY 4.0 |

**If none fits:** Best: one loop whose pitch follows the meter in code.

### Brand: charged

**Is:** The brand burns into the floor: a deep arcane seal-lock strike.

**Hook:** NEW: the brand objective done. (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Sacred rune lock: arcane seal (CC-BY) | CC BY 4.0 | 4.00 s · attack 123 ms · tail to -40 dB 2864 ms · -18.4 LUFS (peak-limited: quieter than -18); CLAP 0.60 | "Sacred Rune Lock 02 - Arcane Seal Activation" by TommasoMotteran ( https://freesound.org/people/TommasoMotteran/sounds/854670/ ) licensed under CC BY 4.0 |
|  | Gong | CC0 1.0 | 5.24 s · attack 93 ms · tail to -40 dB 4969 ms · -18.0 LUFS; CLAP 0.30 | https://freesound.org/people/nkuitse/sounds/18654/ |

### Ooze well: sealing / sealed

**Is:** You seal a lava well: a heavy stone lid grinding shut and the lava beneath dying in a steam hiss.

**Hook:** NEW: OozeWells w.Sealed(i) (HellRoom.cs:311); a 3 s sealing loop while you stand there. (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Opening the sarcophagus | CC0 1.0 | 7.03 s · attack 1381 ms · tail to -40 dB 1753 ms · -22.4 LUFS (peak-limited: quieter than -18); CLAP 0.51 | https://freesound.org/people/Breviceps/sounds/457529/ |
|  | Heavy stone push | CC0 1.0 | 1.13 s · attack 392 ms · tail to -40 dB 528 ms · -18.0 LUFS; CLAP 0.20 | https://freesound.org/people/msavioti/sounds/558254/ |
|  | Steam (hiss layer) | CC0 1.0 | 7.80 s · attack 393 ms · tail to -40 dB 6906 ms · -18.1 LUFS (peak-limited: quieter than -18); CLAP 0.05 | https://freesound.org/people/sniperous/sounds/365071/ |

### Exit gate opens

**Is:** The red exit gate grinds open: a heavy stone slab sliding with a deep rumble.

**Hook:** NEW: HellRoom.cs:210-213 (_doneOpened). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Hidden wall opening | CC0 1.0 | 3.54 s · attack 942 ms · tail to -40 dB 1643 ms · -18.0 LUFS; CLAP 0.38 | https://freesound.org/people/ertfelda/sounds/243699/ |
| **pick** | Tomb door open, stone scrape | CC0 1.0 | 7.68 s · attack 76 ms · tail to -40 dB 4616 ms · -18.0 LUFS; CLAP 0.41 | https://freesound.org/people/Kinoton/sounds/352829/ |
|  | Gate heavy open/close | CC0 1.0 | 8.47 s · attack 511 ms · tail to -40 dB 2709 ms · -18.0 LUFS; CLAP 0.14 | https://freesound.org/people/Omnisis/sounds/336888/ |

### Room cleared (objective done)

**Is:** The room is yours: one deep funeral bell toll that rings out over the lava.

**Hook:** NEW: same moment as the gate (Objective.Done). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
| **pick** | Old church bell | CC0 1.0 | 4.44 s · attack 97 ms · tail to -40 dB 4019 ms · -18.0 LUFS; CLAP 0.46 | https://freesound.org/people/dsp9000/sounds/76405/ |
|  | Toll bell | CC0 1.0 | 7.89 s · attack 33 ms · tail to -40 dB 7547 ms · -18.0 LUFS; CLAP 0.42 | https://freesound.org/people/daytripper/sounds/73678/ |
|  | Magical level up (CC-BY) | CC BY 4.0 | 4.00 s · attack 369 ms · tail to -40 dB 1589 ms · -18.0 LUFS; CLAP 0.09 | "Fantasy UI Stinger - Magical Level Up 03" by TommasoMotteran ( https://freesound.org/people/TommasoMotteran/sounds/854025/ ) licensed under CC BY 4.0 |

**Verdict:** Old church bell (CC0, 0.46): one low toll, 4.4 s.

### Death: 'AGAIN'

**Is:** You die and the room restarts: one heavy low boom with a sucked-in reverse breath, then silence for the banner.

**Hook:** NEW: HellRunDirector.OnWrecked (HellRunDirector.cs:93-101, the AGAIN banner). (needs a new cue)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Dramatic hit | CC0 1.0 | 7.04 s · attack 96 ms · tail to -40 dB 4953 ms · -18.0 LUFS; CLAP 0.42 | https://freesound.org/people/qubodup/sounds/222517/ |
| **pick** | Low impact | CC0 1.0 | 5.22 s · attack 343 ms · tail to -40 dB 3945 ms · -18.0 LUFS; CLAP 0.47 | https://freesound.org/people/AudioPapkin/sounds/430978/ |
|  | Deepest boom yet (CC-BY) | CC BY 4.0 | 8.00 s · attack 76 ms · tail to -40 dB 5758 ms · -17.8 LUFS; CLAP 0.49 | "Deepest Boom Yet" by hykenfreak ( https://freesound.org/people/hykenfreak/sounds/248150/ ) licensed under CC BY 4.0 |

**Verdict:** Low impact (CC0, 0.47); the CC-BY deepest boom scores 0.49.

### Skill pick screen (open, hover, choose)

**Is:** Time stops for the pick: a long dark ethereal whoosh as the cards appear, a soft tick when you move between them, a warm confirm when you choose.

**Hook:** Cue 'skill_pick' (HellSkillPick.cs:123) on choosing; open and hover need cues. (hook exists)

| | Candidate | Licence | Measured | Credit / source |
|---|---|---|---|---|
|  | Long ethereal woosh (open) | CC0 1.0 | 6.65 s · attack 435 ms · tail to -40 dB 3166 ms · -18.0 LUFS; CLAP 0.10 | https://freesound.org/people/newagesoup/sounds/462879/ |
|  | UI hover item | CC0 1.0 | 0.25 s · attack 25 ms · tail to -40 dB 155 ms · -18.0 LUFS; CLAP 0.18 | https://freesound.org/people/steaq/sounds/757328/ |
| **pick** | Confirmation | CC0 1.0 | 1.38 s · attack 2 ms · tail to -40 dB 1161 ms · -18.0 LUFS; CLAP 0.41 | https://freesound.org/people/drzhnn/sounds/199940/ |

**If none fits:** The open whoosh matches poorly (CLAP 0.10) and the hover tick is 0.25 s of bright noise; the confirm (0.41) is usable. A second candidate for the open: the Nightfall dark-spell rise.

## Sounds with no good free match, and how we'd make them

These have no free file that is the sound on its own. Each one is made by layering the free candidates, or by
synthesis (numpy, like the kit's earlier procedural sounds), and judged with the ears tool against its identity line:

- **Demon scythe, the 0.9 s path warning:** synthesise it, so it ends exactly when the beam fires: a thin tone rising an
  octave with a tremolo that speeds up. The free risers score CLAP 0.03-0.19.
- **Demon scythe, appearing:** the dark-rift whoosh, plus a pitched-down blade 'shing', plus a whir (filtered noise
  pulsed at about 12 Hz).
- **The shaft fall:** a rising filtered-noise wind over the fall's length, the low whoosh at the start, and a landing
  thump. The free whooshes score under 0.1.
- **The fire imp's death:** a goblin death cry cut at 0.4 s, straight into the CC0 fireball explosion.
- **The scythe demon's footsteps:** the heavy body fall's first 0.25 s, pitched down, one per leg plant.
- **Collapsing platform, the shake:** the stone drag looped over a low rumble, rising through the shake, cut by the crack.
- **Chain anchor hit:** a metal-impact transient and a short chain jangle.
- **Nightfall:** the dark-spell rise, the sub boom underneath, and a swirling inrush. All candidates score 0.25-0.31.
- **Rewind:** the reverse vortex (start and sweep) plus a single tick at the end. The tape stop scores 0.00 against "a
  clock tick", so it is a layer, not the end sound.
- **Loops that follow the game:**
  - The ooze's throb (one 'blorp' retriggered on each `OozePulse` beat, so it speeds up as it winds up).
  - The brand's charge (one loop whose pitch follows the meter).
  - The imp's hover (wings plus crackle).

  These are code-driven, not single files.
- **Time Dilation's world muffle:** a mixer change on `PitchChanged`, not a file.

## Music direction

No music was composed. The creator builds the music with us step by step. The references (listening only) and
what each does:

- **DOOM Eternal** (The Only Thing They Fear Is You; End of Level): the music works like an engine.
  - Down-tuned metal guitars and huge synth bass, with a choir.
  - Quiet between fights; it slams in when the demons come.
  - The end-of-level cue is a slow, heavy, triumphant riff.
- **Metal: Hellsinger** (Stygia): the beat is the game. Shooting on the beat does more damage, and the vocals only come
  in when you play well. Fast double-kick metal with growled and clean vocals.
- **Hades** (Out of Tartarus): fast acoustic, bouzouki-like plucking over rock drums. Hellish but bright and catchy,
  never sludgy.

The directions, as choices for the creator:

1. **Metal with choir** (DOOM): the closest match to a demon-filled Hell. It can be layered: an ambient bed between
   fights, with the guitars and drums entering on a spawn.
2. **Beat-driven metal** (Hellsinger): the strongest tie to the **Time** theme. Time Dilation could slow the music with
   the world, and Rewind could literally rewind it.
3. **Catchy rock** (Hades): the most distinctive, and lighter.
4. **Dark ambient only** (the Black Glass bed and Cries From Hell): cheap and moody, but it won't carry a trailer.

**The free stand-ins** are listed below (30 s excerpts are on the page). The CC0 ones can ship as temp music; the CC-BY
ones need the credit line:

- **Boss Battle #2 (Symphonic Metal):** the closest free match to direction 1.
- **Boss Battle #9 (Metal):** a straight metal loop, CC0.

| Track | Author | Licence | What it is | Source |
|---|---|---|---|---|
| Boss Battle #9 (Metal) - loop | nene | CC0 | A looping metal boss track: chugging guitars and double-kick drums; ships with a separate opening. | https://opengameart.org/content/boss-battle-9-metal |
| Boss Battle #2 (Symphonic Metal) | nene | CC0 | Metal guitars and drums with orchestral strings and choir-like pads: the closest free thing to DOOM's mix of metal and choir. | https://opengameart.org/content/boss-battle-2-symphonic-metal |
| The 9th Circle | joth | CC0 | Named for Dante's last circle of Hell; a dark game track (listen for its style: the page calls it a Hell piece). | https://opengameart.org/content/the-9th-circle |
| Abandoned Steel Mill | spring-spring | CC0 | An industrial ambient piece: a candidate bed for The Molten Works between fights. | https://opengameart.org/content/abandoned-steel-mill |
| Going Deep | section31 | CC0 | A descending, dark ambient-music piece: a candidate for the shaft and the skill pick. | https://opengameart.org/content/going-deep |
| Dragged Through Hellfire | Zander Noriega | CC-BY 3.0 | A heavy metal track built for Hell levels. | https://opengameart.org/content/dragged-through-hellfire |
| Chapel of Hate | foxsynergy | CC-BY 3.0 | Dark metal with organ: gothic rather than industrial. | https://opengameart.org/content/chapel-of-hate |
| Cries From Hell (dark ambiance) | Jesús Lastra | CC-BY 3.0 | Dark ambient: drones and distant cries, no beat; a bed for The Black Glass or the menus. | https://opengameart.org/content/dark-ambiance-cries-from-hell |
| Demonic (epic soundtrack) | Fato Shadow | CC-BY 3.0 / 4.0 | Epic orchestral-choir 'demonic' cinematic music: big, trailer-like. | https://opengameart.org/content/demonic-epic-soundtrack |

Premier music references (listening only): **DOOM Eternal: The Only Thing They Fear Is You:** Combat music as an engine: down-tuned djent guitars and huge synth bass on a driving beat, with a choir. It drops into near silence between fights and slams back in when demons appear. **Metal: Hellsinger: Stygia:** A metal track whose beat IS the game: you shoot on the beat for more damage, and the vocals only come in when you fight well. Fast double-kick drums, a female growl and clean vocals. **Hades: Out of Tartarus:** Darren Korb's 'Mediterranean prog-rock': fast acoustic and bouzouki-like plucking over rock drums and guitars. Hellish but bright and catchy, never sludgy.

## Credits for CC-BY candidates (copy into ThirdParty/LICENSES.md for whichever ship)

- "Error buzz (Soft UI SFX Free)" by heyheytheree ( https://freesound.org/people/heyheytheree/sounds/871919/ ) licensed under CC BY 4.0
- "Slowdown_Short_Up.wav" by anomalous_underdog ( https://freesound.org/people/anomalous_underdog/sounds/123438/ ) licensed under CC BY 4.0
- "long wispy woosh1.wav" by newagesoup ( https://freesound.org/people/newagesoup/sounds/377830/ ) licensed under CC BY 4.0
- "BreathOfDeath" by gabemiller74 ( https://freesound.org/people/gabemiller74/sounds/60571/ ) licensed under CC BY 4.0
- "Shadow Evil Spell Dark Magic" by EminYILDIRIM ( https://freesound.org/people/EminYILDIRIM/sounds/559087/ ) licensed under CC BY 4.0
- "Powerful Magic Fireball Impact" by mikiko850 ( https://freesound.org/people/mikiko850/sounds/848736/ ) licensed under CC BY 4.0
- "gas_fire_catch.wav" by themfish ( https://freesound.org/people/themfish/sounds/45809/ ) licensed under CC BY 4.0
- "Fireball Cast 3" by LiamG_SFX ( https://freesound.org/people/LiamG_SFX/sounds/334237/ ) licensed under CC BY 4.0
- "Whoosh.wav" by _bepis ( https://freesound.org/people/_bepis/sounds/414422/ ) licensed under CC BY 4.0
- "Fire Burst.wav" by SilverIllusionist ( https://freesound.org/people/SilverIllusionist/sounds/472688/ ) licensed under CC BY 4.0
- "Cave Monster Die Sound.wav" by FiveBrosStopMosYT ( https://freesound.org/people/FiveBrosStopMosYT/sounds/535320/ ) licensed under CC BY 4.0
- "mdcharge.wav" by wildweasel ( https://freesound.org/people/wildweasel/sounds/39043/ ) licensed under CC BY 4.0
- "charging loop 2" by JavierZumer ( https://freesound.org/people/JavierZumer/sounds/257231/ ) licensed under CC BY 4.0
- "Beam.wav" by Deatlev ( https://freesound.org/people/Deatlev/sounds/254942/ ) licensed under CC BY 3.0
- "Monsters Scream" by XavTech ( https://freesound.org/people/XavTech/sounds/238548/ ) licensed under CC BY 3.0
- "Slime Movement" by StephenSaldanha ( https://freesound.org/people/StephenSaldanha/sounds/158759/ ) licensed under CC BY 4.0
- "Splat.wav" by Audionautics ( https://freesound.org/people/Audionautics/sounds/133968/ ) licensed under CC BY 3.0
- "Slime Death (1)" by DrMinky ( https://freesound.org/people/DrMinky/sounds/167074/ ) licensed under CC BY 4.0
- "Magical portal open" by alanmcki ( https://freesound.org/people/alanmcki/sounds/401324/ ) licensed under CC BY 4.0
- "Lava loop.wav" by Audionautics ( https://freesound.org/people/Audionautics/sounds/133901/ ) licensed under CC BY 3.0
- "Energy Charging" by nightcraftsounds ( https://freesound.org/people/nightcraftsounds/sounds/266170/ ) licensed under CC BY 4.0
- "Sacred Rune Lock 02 - Arcane Seal Activation" by TommasoMotteran ( https://freesound.org/people/TommasoMotteran/sounds/854670/ ) licensed under CC BY 4.0
- "Fantasy UI Stinger - Magical Level Up 03" by TommasoMotteran ( https://freesound.org/people/TommasoMotteran/sounds/854025/ ) licensed under CC BY 4.0
- "Deepest Boom Yet" by hykenfreak ( https://freesound.org/people/hykenfreak/sounds/248150/ ) licensed under CC BY 4.0
- "Dragged Through Hellfire" by Zander Noriega ( https://opengameart.org/content/dragged-through-hellfire ) licensed under CC-BY 3.0
- "Chapel of Hate" by foxsynergy ( https://opengameart.org/content/chapel-of-hate ) licensed under CC-BY 3.0
- "Cries From Hell (dark ambiance)" by Jesús Lastra ( https://opengameart.org/content/dark-ambiance-cries-from-hell ) licensed under CC-BY 3.0
- "Demonic (epic soundtrack)" by Fato Shadow ( https://opengameart.org/content/demonic-epic-soundtrack ) licensed under CC-BY 3.0 / 4.0
