# HellDescent: the trailer (3 minutes)

Written 2026-10-18 at about 01:00 by lane A (branch `hell-script`). The plan for the event trailer, mapped to what exists
tonight. The lines it uses are in `HELL-SCRIPT.md` (keys `tr-01` to `tr-09`, plus game lines); they are placeholders like
every line there.

## 0. The rules it follows

- **The handbook:** 3 minutes; **2:00 of directly recorded gameplay**, 1:00 of stylised editing; it **must show how the
  themes are used**. The trailer is judged first and only the top 5 get played, so it decides everything.
- **The creator's plan** (2026-10-16): a 30 s cinematic premise with a narrator; act 1 movement **and** combat together
  (the game is mostly shooting, not parkour alone); act 2 the escalation; act 3 the story hook, ending on an open
  question at the climax; **the title screen is the end card**; enemy name cards like Fortnite Save the World's (the
  enemy in a striking pose on a simple stylised background, its name and 5-8 words on what it does); **no text over
  gameplay**; the other 30 s of non-gameplay cut in among the gameplay, tuned to its emotion.
- **The themes are shown, never captioned:** Fire is the world and the enemies; Shadow is how you move and hide; Time is
  how you survive. The narration names them once (`tr-04`: "Shadow to hide in. Time to bend."), and every act has at
  least one shot where two themes work together (the synergy score).
- **Capture:** 1440p at 120 fps (90 at worst), HUD off, voices off (voices are laid in the edit from
  `Resources/SkylineAudio/hell-voice/`, or a voices-only stem), from replays of real play so a shot can be re-shot with
  another camera.

## 1. The time budget

| Part | Time | Gameplay | Stylised |
|---|---|---|---|
| Premise | 0:00-0:30 | 0 | 30 s (four stills with parallax, the narrator) |
| Act 1: movement and combat (floor 1) | 0:30-1:15 | 42 s | 3 s (the Fire Imp card) |
| Act 2: the escalation (floors 2 and 3) | 1:15-2:15 | 50 s | 10 s (two enemy cards, the skill pick) |
| Act 3: the story hook (Vesper) | 2:15-2:50 | 28 s | 7 s (her card, two black beats) |
| Title card | 2:50-3:00 | 0 | 10 s |
| **Total** | **3:00** | **120 s** | **60 s** |

The music drives the edit, and no music exists yet (HELL-GAPS gap 9: composed with the creator on day 2). Until then the
cut is timed to the narration and these act lengths; the shots are cut to the beat once the track exists.

## 2. The capture tools (the capture lane, branch `hell-capture`, as of commit 61801a3b)

`docs/HELL-CAPTURE.md` has not landed yet; this is what the branch already has.

| Tool | What it does | Switch |
|---|---|---|
| Play recorder | records the creator's (or the autopilot's) play on a fixed step, seeded, skill picks kept | `-hell-rec`, `-hell-rec-fps`, `-hellSeed <n>` |
| Replayer | deterministic replay with drift correction and jump-to-time | `-hell-replay <file>`, `-hell-replay-from`, `-hell-replay-to` |
| Shot cameras | a camera over a replay: follow, lens, roll, shake, depth of field. Four shots so far: `chase-high`, `dutch-low`, `enemy-reveal`, `locked-wide` (`Assets/JamKit/Templates/SkylineRepo/HellCapture/Shots/*.json`) | `-hell-shot <name>` |
| Trailer capture | renders takes: `run1@12-20:enemy-reveal` (replay `run1`, seconds 12-20, that shot) to files | `-hell-take ...`, `-hell-capture-fps 120`, `-hell-capture-res`, `-hell-capture-codec`, `-hell-capture-mbps`, `-hell-capture-out` |
| Clean picture | no HUD (Hell line and banners included); voices off or voices only | `-hell-clean`, `-hell-hud clean|full`, `-hell-voices on|off|only` |
| Frame-time check | a fly-through of every room against 120 fps | `-hell-perf`, `-hell-perf-target` |

**First-person shots** (most of the 2:00) are the replay through the player's own camera, at 120 fps. **Third-person shots**
use the shot cameras over the same replay. New shot files this list needs: `boss-orbit` (a slow orbit of the arena),
`lava-low` (a low lens skimming the lava toward the player), `card-freeze` (a locked camera on an enemy for its card).

**Disk:** about 12 GB free (HELL-GAPS gap 23). 1440p at 120 fps in NVENC at 80-100 Mbit/s is about 0.6-0.75 GB a
minute; 2:00 of picks needs about 15-20 minutes of takes. It does not fit without the cleanup the creator has to approve.

## 3. The shot list

**Status words:** EXISTS (built and on main or a lane), BUILDING (a lane is on it tonight), MISSING (no lane yet).
**Seeds:** each shot is recorded in a run whose deck deals the named room on the named floor; the record lists the seed
it used. The chapter start (gap 11) will make this one click; until then use `-hellSeed` and the deck.

### 0:00-0:30 The premise (stylised, the narrator)

| # | Time | Shot | Theme | Source | Voice | Status / missing |
|---|---|---|---|---|---|---|
| P1 | 0:00-0:06 | **The sister's death.** A still, slow push-in; a stopped clock. Black first, one heartbeat, then the picture. | Time | image prompt S2 below | `tr-01` | MISSING: the image (after the art direction pick) |
| P2 | 0:06-0:13 | **The God of Death.** A still, parallax: she lounges, bored, then (a cut on the line) looks at you. | Shadow | image prompt S1 | `tr-02`, `tr-03` | MISSING: the image; her name (HELL-SCRIPT section 14) |
| P3 | 0:13-0:21 | **The deal.** A still: hands nearly touching, shadow and a spinning clock between them. A flash of the three powers as quick in-engine inserts (a Shadow Step arrival, a Rewind, a slowed bolt), 0.5 s each. | Shadow, Time | image prompt S3; inserts from act 1 takes | `tr-04` | MISSING: the image. The inserts EXIST once act 1 is captured. |
| P4 | 0:21-0:30 | **Hell's gate.** A still pushing in, dissolving into the in-engine floor 1 entry (the same framing), the first step through. | Fire | image prompt S4, then an in-engine take: floor 1, first room's vestibule, `locked-wide` into first-person | `tr-05` | MISSING: the image. The in-engine end EXISTS (the floor 1 vestibule). |

The premise can also be fully in-engine later (gap 7) if a God of Death model exists; the stills are the plan for day 2.

### 0:30-1:15 Act 1: movement and combat, floor 1 (the Ember Gate)

Fast cuts (1-4 s), first-person, to the music. Ash-grey stone, orange lava light.

| # | Len | Shot | Theme | Room / mechanic | Camera | Status / missing |
|---|---|---|---|---|---|---|
| A1 | 4 s | Drop out of the vestibule, sprint, slide, first imp volley over your head. | Fire | Six Sigils, entry to A | first-person | EXISTS (room, imps). Art: BUILDING (art-in) |
| A2 | 4 s | **Wall-run along the 22 m wall over the lava**, shooting an imp mid-run. | Fire | Six Sigils, A to B | first-person, then `chase-high` for 1 s | EXISTS. Fast line being proven (fastlines lane) |
| A3 | 4 s | **Shadow Step chain** across the 40 m gulf: dark perch, dark perch, land. The aim marker in shot. | Shadow | Six Sigils, C to D | first-person | EXISTS. Step VFX weak: BUILDING (lane D) |
| A4 | 3 s | Same jump from outside: the body vanishes into smoke, appears on the far perch. | Shadow | Six Sigils, C to D | `dutch-low` | EXISTS. Needs a player body in third person (MISSING: check what the capture lane draws) |
| A5 | 4 s | **Time Dilation:** fast and airborne, aim: the world slows, three imps' bolts hang, three shots, three kills, back to speed. | Time | Break the Chains or Six Sigils E to F | first-person | EXISTS (Dilation). Dilation look: BUILDING (lane D) |
| A6 | 3 s | Shoot the anchors from mid-air off a wall-run; the gate's chain snaps. | Fire, Time | Break the Chains | first-person, `enemy-reveal` on the anchor for 1 s | EXISTS |
| -- | 3 s | **Enemy card: FIRE IMP.** | -- | freeze on an imp from A1/A5 | `card-freeze` | MISSING: final imp body (art-in), card template, pose |
| A7 | 4 s | Lava hop, near-sink, out with a jump and a mantle; an imp follows you down. | Fire | Soul Lantern or Ooze Wells | first-person | EXISTS |
| A8 | 4 s | Carry the Soul Lantern across the collapsing bridge while segments fall behind you. | Fire | Soul Lantern, fast line | first-person, then `locked-wide` 1 s | EXISTS. Vesper's voice in the Lantern (beat 1) BUILDING (lane C) |
| A9 | 4 s | Reload under fire, swap guns, a kill. (Combat that isn't movement: the creator's note.) | Fire | any floor 1 room with imps | first-person | EXISTS (the kit's guns) |
| A10 | 4 s | A job: the Viscount's palanquin, the shot that drops him before his door. Her voice over it, no text: `job-viscount-brief` cut down to "Make it stop. Permanently." | Fire | Six Sigils pocket (jobs lane) | first-person | BUILDING (jobs lane); job body art MISSING |
| A11 | 4 s | Floor 1's shaft: the fall into the dark. | Shadow | the floor 1 shaft | first-person | EXISTS |

### 1:15-2:15 Act 2: the escalation, floors 2 and 3

Narration `tr-06` over A11 into B1. Longer, wilder shots; the themes start combining.

| # | Len | Shot | Theme | Room / mechanic | Camera | Status / missing |
|---|---|---|---|---|---|---|
| B1 | 5 s | **The Collapse:** the floor falling behind you at 10.5 m/s, slide under the beam, the 12 m gap, Shadow Step to the dark landing as the front reaches you. | Fire, Shadow | the Collapse, centre lane | first-person | EXISTS (rooms2). Debris shadows EXIST |
| B2 | 4 s | Same run from above: the room falling away in rows behind a small running figure. | Fire | the Collapse | `chase-high` | EXISTS. Third-person body MISSING (as A4) |
| -- | 3 s | **Enemy card: SCYTHE DEMON.** | -- | freeze at the moment its line is projected | `card-freeze` | MISSING: scythe demon body (art-in), card |
| B3 | 4 s | A scythe projects its line (the visible path), you read it and slide under the beam as it fires. | Shadow | Six Sigils D to E or Hold the Brand | first-person | EXISTS (the enemy) |
| B4 | 6 s | **The Rewind showpiece:** you miss a jump and sink in lava (screen red), Rewind: the world runs back 4 s, bolts fly backwards, the platform rises, you make the jump. | Time, Fire | Six Sigils B to C (collapsing chain) | first-person, then a 1 s `dutch-low` of the restored platform | EXISTS (Rewind). Reverse-scrub look BUILDING (lane D), rewind sound BUILDING (sound-in) |
| B5 | 6 s | **Hold the Brand at speed:** wall-run laps round the ring, the ring's glow pulsing faster, imps swarming the island. | Fire | Hold the Brand, floor 2 | first-person, `locked-wide` 1.5 s | EXISTS (rooms2) |
| -- | 4 s | **The skill pick:** three cards (one each of Shadow, Time, Fire), the cursor lands on one. The only UI in the trailer, a stylised insert, not gameplay with text over it. | all three | the pick after floor 2 | screen capture of the pick | EXISTS (text icons). Card art and frame MISSING (gap 20) |
| B6 | 4 s | **Hellfire Trail or Combustion:** a step that leaves fire, or one burning kill chaining explosions through a pack. | Fire, Shadow | any room with a pack | first-person | Combustion EXISTS; Hellfire Trail is not built (the design's list) |
| B7 | 4 s | **Nightfall:** a dark sphere thrown over the imps; inside it they lose you; Step into the dark, kill from it. | Shadow | Ooze Wells or Hold the Brand | first-person | EXISTS (Nightfall) |
| B8 | 4 s | Floor 3, the Black Glass: the stone loses all colour; only lava, fire and the lime outlines left. Ooze wells pulsing. | Fire, Shadow | Ooze Wells, floor 3 | `locked-wide`, then first-person | Floor 3's colour-free look BUILDING (art-in, direction A) |
| -- | 3 s | **Enemy card: LAVA OOZE.** | -- | freeze as it pulses | `card-freeze` | MISSING: ooze body (art-in), card |
| B9 | 5 s | Seal a well while oozes spill lava round your feet; lava hop across the stones to the next. | Fire | Ooze Wells, fast line | first-person | EXISTS |
| B10 | 4 s | The Seconds Thief job: one burst kills him before he undoes it. Her voice: "So finish him." | Time | Six Sigils pocket, floor 3 | first-person | BUILDING (jobs lane) |
| B11 | 4 s | A Rewind across a burning room: fire and enemies snap back with you. | Time, Fire | any floor 3 room | first-person | EXISTS (fire states rewind?) Check: burns don't rewind (HELL-GAPS gap 15). Pick a shot without burns until fixed |

### 2:15-2:50 Act 3: the story hook (Vesper)

Slower. Narration `tr-07`; her line `tr-08`. The question the trailer leaves: kill her, or offer your hand?

| # | Len | Shot | Theme | Room / mechanic | Camera | Status / missing |
|---|---|---|---|---|---|---|
| C1 | 5 s | The Soul Lantern held up close; her voice inside it (beat 1, `v1-02`, cut short). | Fire | Soul Lantern | first-person, slow | Beat BUILDING (lane C) |
| C2 | 3 s | Black, then her silhouette on the throne balcony, violet haze. | Shadow | boss arena (the Evening Star's Hall) | `boss-orbit` (new) | Arena EXISTS (boss lane). Her body is a stand-in: MISSING (gap 8) |
| -- | 3 s | **Boss card: VESPER, THE EVENING STAR.** | -- | freeze on her blink | `card-freeze` | MISSING: her body, card |
| C3 | 5 s | **Her Rewind:** your burst lands, she rewinds her health; you break it with heart shots ("You broke it."). | Time | boss phase 2 | first-person | EXISTS (boss lane) |
| C4 | 6 s | **Nightfall:** the arena's lights die; the violet flare where she'll step; you turn and fire into the dark. | Shadow | boss phase 3 | first-person, a 1 s `dutch-low` | EXISTS |
| C5 | 4 s | **Mercy:** a killing bolt pulled short; "Get up." | Time, Shadow | boss, any phase | first-person | EXISTS (pulled blows). Line voice: placeholder EXISTS |
| C6 | 5 s | She kneels. You walk to her. Your hand comes up... and the gun in the other. Cut to black on the choice. `tr-09`. | all three | the decision | first-person | MISSING: the ending lane (not started; minimal version planned) |
| -- | 4 s | Black beats between C4, C5 and C6 (stylised): heartbeat, a clock tick, an ember. | -- | edit | -- | edit only |

### 2:50-3:00 The title card

The game's own title screen as the end card (the creator's plan): the title (HELL-SCRIPT section 13) in Pirata One over
the Hell plate, then the God of Death's `sting` line over black ("...I'm going to need someone again."), then the event
and the repo link (presentation only, if wanted). MISSING: the title; the title screen's art (art-in).

## 4. Enemy cards (Fortnite Save the World style)

Each card: a freeze-frame of the enemy in a strong pose (from a replay, `card-freeze`), the enemy cut out on a
two-colour stylised background (black and its element colour, a burst of ink lines), its name large in Pirata One, and
5-8 words on what it does. 3 s each: a hit on the beat, a short hold, out. **Card copy is drafted below for the creator
to rewrite.**

| Card | Draft copy (5-8 words) | Background | Needs |
|---|---|---|---|
| FIRE IMP | "Hovers. Spits fire faster than you." | black and orange | the final imp body (art-in) |
| SCYTHE DEMON | "Draws its line. Then cuts along it." | black and acid lime | the robed scythe demon (art-in, Blender) |
| LAVA OOZE | "Every pulse leaves the floor burning." | black and molten red | the ooze body (art-in) |
| VESPER, THE EVENING STAR | "Made your deal first. Never quite kills." | black and violet | her body (gap 8) |

Build the card template once (an editor scene or a compositing template), then each card is a still plus a name.

## 5. The premise stills: image prompts

**DO NOT GENERATE YET.** Generate only after the creator picks the art direction (HELL-ART-PASS.md: A Inked Inferno is the
recommendation; B Molten Grit; C Black and Ember). Each prompt is written for direction A; swap the style clause for B or
C. Generate through `art-tools/flow.mjs --image` (free; not the API key), 16:9, keep every take, copy picks to
`art/review/<date>/`. The creator's character-art values: a sketchbook look, a unique pose, one strong defining colour.

**Style clauses** (paste at the start):
- **A, Inked Inferno:** "Comic-ink illustration, hand-inked thin dark linework, flat painted colour in three hard bands of
  light, like a graphic-novel splash page lit by lava; sketchbook energy, visible ink strokes."
- **B, Molten Grit:** "Dark painterly concept art, realistic textures of basalt, ash and rusted iron, cinematic bloom and
  haze, orange lava light."
- **C, Black and Ember:** "Near-black greyscale illustration where only fire and glowing light carry colour, heavy
  shadow, stark silhouettes, ink-black fog."

**Character anchors** (keep them identical across stills so the cast reads the same):
- **The God of Death:** a tall, thin woman of no clear age, bone-white robes like a burial shroud worn as a ball gown,
  long black hair loose, a calm amused face, **one cold verdigris-teal glow** (her eyes and the sand in her hourglass)
  as her defining colour, the only cool colour in the trailer. Bored and theatrical, never skeletal.
- **The protagonist:** seen from behind or in shadow only, a hooded jacket, no face (the player is anyone).
- **Vesper:** a tall woman in a dark violet gown, a faint silver-violet star glow at her throat; tired, regal.
- **The sister:** never shown clearly (open question 4 in HELL-SCRIPT.md); a hand, a silhouette, an empty bed.

| Still | Prompt (after the style clause) |
|---|---|
| **S1. The God of Death** | "A 16:9 widescreen image. The God of Death, a tall thin woman in bone-white shroud-like robes worn like a ball gown, long loose black hair, lounging sideways across a throne made of leaning grave markers, one leg over the armrest, chin on her hand, utterly bored. She holds an hourglass by its neck like a wineglass; its sand glows a cold verdigris teal, the same teal as her eyes. A dim stone hall, candles guttering. Low camera, wide composition with empty space on the right for the narrator's beat. Unique asymmetric pose, strong silhouette." |
| **S2. The sister's death** | "A 16:9 widescreen image. A quiet hospital room at night seen from the doorway: an empty bed with the sheet pulled straight, a single chair beside it, a wall clock stopped at 3:12, cold blue moonlight through blinds. In the doorway, in the foreground, the dark silhouette of a young person seen from behind, hood up, fists clenched. No faces, nothing graphic. Stillness and grief turning into anger." |
| **S3. The deal** | "A 16:9 widescreen image. Close on two hands about to touch in darkness: on the left a pale long-fingered hand from a bone-white sleeve, on the right a clenched human hand in a dark jacket sleeve. Between them a knot of black smoke-like shadow coils around a small floating clock face whose hands spin backwards, lit by a cold verdigris-teal glow from the left and a faint ember-orange glow from below. Dramatic, intimate, centred composition." |
| **S4. Hell's gate** | "A 16:9 widescreen image. A colossal gate cut into a cliff of black basalt, its doors chained shut with chains as thick as towers, lava light pouring up from a chasm below and banding the rock in hard orange stripes, ash falling like snow. On a narrow stone ledge at the bottom, a tiny hooded figure seen from behind holds up a small lantern. Extreme scale contrast, low horizon, epic and ominous." |
| S5 (optional, act 3) | "A 16:9 widescreen image. Vesper, the Evening Star: a tall woman in a dark violet gown sitting alone on an obsidian throne on a high balcony above a lava hall, one silver-violet star glowing at her throat, looking down with tired curiosity, her face half in shadow. Black glass architecture, violet haze, the only warm light from lava far below. Lonely, regal, centuries of boredom." |

The in-engine plate for P4's dissolve: the floor 1 vestibule, `locked-wide`, matched to S4's framing.

## 6. What is missing, in order of who unblocks it

1. **The creator:** the art direction (unblocks the stills, the cards and the title screen); the title and the God of
   Death's name (the end card, the narration); the voices (placeholders now); the disk cleanup (capture).
2. **Art-in:** the three enemy bodies (the cards, every shot's look), floor 3's colour-free look, the title screen.
3. **Lane D (VFX):** the Rewind's reverse-scrub look, the Step's smear, Dilation's edge: the Time and Shadow shots
   (A3, A5, B4, B11, C3) depend on them.
4. **Lane C (beats):** Vesper's Lantern voice (A8, C1).
5. **The ending lane:** the decision (C6), the trailer's last gameplay shot.
6. **Vesper's body** (gap 8): C2, her card, C6.
7. **The capture lane:** the three new shot files (`boss-orbit`, `lava-low`, `card-freeze`) and a third-person player
   body for A4 and B2.
8. **Music** (day 2, with the creator): the edit is cut to it.

Capture order once the build is final: record the creator's (or the autopilot's) best runs per room with `-hell-rec`,
pick the moments, then render takes with `-hell-take` at 120 fps in the clean picture, first-person first, the
third-person shots second, the card freezes last.
