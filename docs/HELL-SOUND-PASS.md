# HellDescent: the sound pass (day 1 evening, 2026-10-17)

Not committed. Written by the sound-pass agent on the evening of day 1, after the abilities, enemies and rooms branches
were merged. It lists every sound the game needs. For each one it gives:

- what the sound physically is (its identity);
- where it hooks into the code;
- the premier game reference, where one helps;
- 2-4 free candidates, with the best one picked.

It also lists the sounds that have no good free match and how we'd make them, and the music direction options.
Nothing is in the game yet. The creator picks on the listening page first.

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

## How the sounds go in (for whoever wires them)

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
