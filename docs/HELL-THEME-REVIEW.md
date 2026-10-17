# HellDescent: theme-adherence review (Fire, Shadow, Time)

Written 2026-10-18 at about 01:30 (day 2) by a critical reviewer. It is a review only: nothing in the game or the other docs
was changed. It covers main as of `89a0c32b` (the ending merged). Sources: the handbook (`GameDevBooklet.pdf`), every
HELL-*.md listed in the brief, the code where the docs were unclear (`HellTargeting`, `HellDarkness`, `HellJobSite`), and
the look-test stills in `FableAnimationTests/art/review/2026-10-17/hell-look/` (A, B, C).

## 0. What the handbook actually says (and doesn't)

The handbook says very little about themes, so its few lines carry a lot of weight:

- "**30%** of your evaluation will be done based on theme adherence: Core Integration (15), Multi-Theme Synergy (10),
  Atmospheric Framing (5)." "You are not required to use all 3 themes, but at least 1."
- "Your evaluation will be done based on a three-minute trailer of your game." "Your initial evaluation will be done
  **solely** on your trailer, so it is encouraged to put effort in it."
- "Two minutes of those three must be **actual gameplay** directly screen-recorded." "You are encouraged to use
  stylistic editing and graphics like in teaser trailers for one minute, which can be structured throughout the trailer."
- "**Your trailer must detail how exactly the themes were incorporated in your game.**" (spelled "themese" in the
  handbook.)
- It gives no definitions of the three buckets. Read them plainly: *core integration* = the theme changes how you play;
  *synergy* = the themes work through each other rather than beside each other; *atmospheric framing* = the theme is
  felt in the look, sound and story.

Two consequences run through this review:
1. **What isn't in the trailer scores nothing at the first cut.** A system a judge would only find in minute 20 of play
   is worth nothing for the shortlist.
2. **"Must detail how exactly"** is a stronger demand than the trailer plan's rule "the themes are shown, never
   captioned". Showing alone risks a judge failing to see *why* a shot is Shadow. See fault 1.

---

## 1. Per theme

### Fire

**Where it lives now**
- *Level design:* every room is built around lava. Lava basins are the hazard on every fast path: damage over time, a
  sink after 1 s, back to the last safe spot. The design's readability rule is "lava glow and broken edges = fast and
  risky". Floors scale the lava's damage (10, 15 and 22 a second).
- *Enemies:* all three are fire. The fire imp spits ember bolts. The scythe demon is charred black-red with bone horns.
  The lava ooze slams down lava pools that burn, then cool to crust. Damaged enemies show glowing fire cracks.
- *Player mechanics:* only two of the nine floor skills are Fire. **Ashwalker** lets you Shadow Step onto lava and wade
  it for 6 s, with a lava hop. **Combustion** makes a burning enemy explode when it dies, chaining. Burn (`HellBurn`)
  exists, and lava burns enemies too. **Hellfire Trail** and **Ember Rounds** from the design are not built.
- *Jobs:* the Viscount of Cinders, the Ledger of Ash (burn the book) and the Lava Bather.
- *Art:* the lava is the strongest-looking thing in the game. In look A it is a banded cartoon magma that floods the room
  with orange. Embers rise and hot blobs heave on the surface (`HellWorldFx`). Kills throw a fire ring and embers.
- *Sound:* 75 cues are wired: lava bubbles, braziers, ember bolts, the per-floor beds (ash wind, then forge and chains,
  then a cold drone, each with lava under it).
- *Story:* Hell is the setting, and "Every floor burns hotter" is in the narration.

**How core is it?** Fire is core to the **level design**: take the lava away and the safe/fast-path structure, the
collapsing bridges and the Ooze Wells all fall apart. For the **player's verbs** it is shallow. You never *use* fire
except through two optional skills a judge may never pick. Fire is mostly "the floor is hot and the enemies are red",
which is what every game set in Hell has. A judge will credit it as present and well-dressed, not as an idea.

**What a judge sees:** in 3 minutes of trailer, lava in nearly every shot, orange enemies, fire bursts on kills. They
read "Hell, fire". In a few minutes of play: dodge lava, hop out of it, fight imps. That is solid but generic.

**Faults**
- Fire is the background, not a tool. Nothing in the base kit (Shadow Step, Rewind, Time Dilation) touches fire.
- Combustion is the one dramatic fire mechanic, and it sits behind a random pick of one card in three.
- **Fire and shadow are kept apart on purpose.** The room lint requires that "no fire light reaches a dark zone", and the
  fire lights are clamped clear of the dark zones. The two themes never meet in play (see Synergy).
- Burns don't rewind (`HELL-ABILITIES`, "What rewinds"). So the trailer's planned shot B11, "a Rewind across a burning
  room", has to avoid fire.

### Shadow

**Where it lives now**
- *Mechanics:*
  - **Shadow Step only lands in darkness** (`HellAbilities.cs:263`): the one real Shadow rule.
  - **Nightfall** (a skill) throws a dark sphere. Enemies inside it, or looking through it, lose you.
  - **Shade** leaves a decoy shadow. **Umbral Chain** and **Ambush** are step skills.
- *Level design:*
  - Every room has at least one dark zone, enforced by the lint. In practice that is a handful of perches and pillars on
    the fast lines: two perches in the Six Sigils, the pillars in Break the Chains and Hold the Brand, one landing in the
    Collapse.
  - The Overdue Soul's cage can only be entered by Shadow Stepping through its bars.
  - The Poacher job is the **only** place where darkness hides you from an enemy (`HellJobSite.cs:117`, `darkHides`).
- *Boss:*
  - Vesper Shadow Steps between her dark perches, and your marker shows where she will land.
  - Phase 3, Nightfall, puts out the arena's lights. She hunts you from behind, and your Step works anywhere.
  - This is the best Shadow content in the game.
- *Art:* violet is the Shadow colour (the marker, the smear, the tendril). In the look tests, dark zones are a
  **translucent grey box**: in `A/dark-zone.png` and `C/dark-zone.png` they read as glass cubes sitting on the lava, not
  as shadow. Floor 3, "the Black Glass", is meant to be "obsidian and shadow", but in `A/floor3-wide.png` it looks the
  same as floor 1 with a purple sky.
- *Sound:* the Shadow Step has its in/out cues. There is no sound for darkness itself: no hush when you stand in shadow.
  The planned "dark zones drop the drums" waits for music that doesn't exist yet.
- *Story:* in the design, Shadow is "the protagonist / backstory". In what is written, it shows up only as the powers'
  name ("Shadow to hide in"). The recommended Vesper set ("She shadows you": she uses your Shadow Step in glimpses) is
  not chosen or built.

**How core is it?** Half core. The darkness rule on the Step is a genuine constraint. But **"Shadow to hide in" is false
in the base game**: the imps, demons and oozes ignore darkness entirely. `HellTargeting` blinds them only inside
Nightfall's sphere or against the Shade. Remove the darkness rule and you have an ordinary teleport. Remove the dark
zones and only a handful of authored landings change. So to a player, Shadow is "a blink that works on special tiles".
The creator's read is right, and slightly generous: Shadow lives in the abilities, and even there it is mostly a colour.

**What a judge sees:** in the trailer, violet smoke and a smear when you blink, plus Nightfall if B7 is captured, plus
Vesper's dark phase. Nothing tells them the Step *needs* shadow, because the dark landings don't look dark. In play the
tutorial points at a dark zone ("step into that shadow") and the judge steps into... a grey glass box beside the lava.
They will not understand why other spots are refused (a red marker, with no reason given).

**Faults**
- Darkness has no meaning for enemies (the worst fault). The narration promises "Shadow to hide in", and the game only
  keeps that promise inside one optional skill and one job.
- Dark zones don't look like darkness, and lava light floods everything.
- Too few dark zones for a theme: they are puzzle pieces, not a second layer of the world.
- Floor 3 doesn't deliver its "obsidian and shadow" identity.
- Vesper's bolts are orange ember bolts, not violet shadow bolts (`HELL-BOSS`, "What's left"). The Shadow boss shoots
  fire.

### Time

**Where it lives now**
- *Mechanics:*
  - **Rewind** puts the whole world back 4 s: the player, enemies, bolts, lava, collapsing platforms, the room clock and
    the objectives. Revived enemies included.
  - **Time Dilation** is bound to your speed, so movement buys slow motion.
  - Time skills: Deeper Rewind, Momentum Bank, Borrowed Time (kills shave cooldowns).
  - Boons: Short Memory, Long Breath.
- *Level design:* clocks drive the objectives.
  - The Collapse chases you on a timed front.
  - Hold the Brand charges faster at speed, with waves every 5 s.
  - Collapsing platforms run on shake timers.
  - The Rewind is legitimately how you undo a missed jump on the collapsing chain.
- *Jobs:*
  - The **Seconds Thief** undoes any wound that doesn't kill him within 1.2 s.
  - The **Lost Hourglass** is a fetch job.
  - A Rewind can undo a job failure, never a success.
- *Boss:* Vesper has her own Rewind (phase 2). Her heart turns cyan and a clock ring spins; shoot the heart to break it.
- *Story:* the premise is an exchange of time. Vesper has had "nothing BUT time". The "spare, at a price" ending takes the
  time from you.
- *Art and FX:* the strongest signature effect in the game (`HellRewindFx`). The world holds, then cyan ghosts race
  backwards along every path: yours, every enemy's, every bolt back into its muzzle. Then tape bars and a snap. Dilation
  gets a blue-violet grade with dotted bolt trails.
- *Sound:* the rewind sweep plus `rewind_end` (a reverse cymbal and a clock tick), and the dilation in/out cues. The
  dilation **muffle and heartbeat are off by default** (`DilationMuffle = false`, `DilationHeartbeat = false`).

**How core is it?** Fully core. Take the Rewind and the Dilation away and the game's movement-combat loop and the boss's
phase 2 lose their identity. This is the theme the game is really *about*, and the Rewind's ghost scrub is the single
best "show, don't tell" theme shot available.

**What a judge sees:** the Rewind shot (B4: sink in lava, rewind, the platform rises, make the jump) and a Dilation kill
(A5) read as Time immediately, with no caption needed.

**Faults**
- **The story's Time stake isn't felt in play.** "Don't take too long" is flavour only. There is no clock on your
  sister. Deaths cost nothing: the "time owed" stat and the God of Death's death lines are written in HELL-SCRIPT
  section 5 but **not built** (no code matches).
- The Time story (her time for your sister's) only lands in the endings, which a judge may never reach.
- Rewinding is a shared genre trope: Braid rewinds time for puzzles, and Prince of Persia: The Sands of Time rewinds
  mistakes in combat and platforming. A judge has seen it. What sets this one apart is the whole-world scrub *including
  the enemies and their shots*, and Vesper rewinding *her* health while you shoot the heart to break it. Those two must
  be in the trailer.

---

## 2. Synergy (10 points)

Where two or three themes **multiply** each other (each changes what the other means):

| Interaction | Themes | Real or side by side? | Visible in a trailer? |
|---|---|---|---|
| Rewind out of a lava sink: the fire hazard is undone by time, the platform rises again | Time x Fire | Real, but it is "rewind a hazard": the fire is only the thing being rewound | Yes (B4), strong |
| Vesper phase 2: she rewinds, you break it on her (fire-cored) heart | Time x Fire(ish) | Real | Yes (C3) |
| Vesper phase 3, Nightfall: the fire lights die, the arena goes dark, her Step and yours work anywhere, she hunts from the shadows | Shadow x Fire | **The best one in the game**: removing fire *creates* shadow, and that changes your movement | Yes (C4), if captured with the lights visibly dying |
| Ashwalker: Shadow Step onto lava | Shadow x Fire | Real but small, an optional pick | Rarely |
| Combustion off a burn | Fire only | Not synergy | -- |
| Time Dilation tied to your *speed* | Time x movement | Not a theme pair | -- |
| Seconds Thief on dark perches: burst him (Dilation, Ambush after a Step) | Time x Shadow | Real, in one optional job | Maybe (B10) |
| Ambush (first shot after a Step does 3x) plus Dilation | Shadow x Time | Real for a player who owns Ambush | Possible |
| The lint: fire lights never reach a dark zone | Fire vs Shadow | **Anti-synergy**: it guarantees the two never touch | -- |
| The ending: Vesper's endless time, or yours, pays for your sister's; she runs beside you stepping through shadow | Time x Shadow (story) | Real in the story, but only at the very end | Only as a hook (C6) |
| The skill pick: one card each of Shadow, Time and Fire | All three | **Side by side**: three flavours on a menu | Yes (the B5 card), and it reads as "three themes, separately" |

**Honest score today: about 5 of 10.** The boss does genuine multi-theme work (phases 2 and 3). The run, where the judge
spends their minutes, mostly puts the themes side by side: fire is the world, shadow is the blink's landing rule, time
is your toolkit. Fire and shadow never interact in the rooms, *by design*. And the one live fire-shadow rule (light
kills darkness) exists in the code (`HellDarkness.RegisterLight`) but is used only for static lava lights placed away from
the darkness.

---

## 3. Atmospheric framing (5 points)

- **Title:** "HELL DESCENT" is a working name in Anton on a gradient with embers. It names the setting only, and Pirata
  One isn't in yet. The best candidates in HELL-SCRIPT section 13 carry one theme each ("Overdue" = Time). "Cinder Clock"
  and "Hellbound Hours" carry two or three, but plainly. No title is decided.
- **Opening:** six placeholder text cards over black and ember. The writing frames all three themes ("Shadow to hide in.
  Time to bend. A door into Hell"), but as text, and that line overpromises Shadow (fault 2). The planned premise stills
  (S1 to S4) don't exist yet.
- **Rooms and lighting:**
  - Under look A, the lava is vivid and the room silhouettes are clean.
  - Everything else is untextured boxes, a brown void ceiling and no props: no braziers, chains, shrines or bones yet,
    because the art-in list is still to come.
  - The game's **default** look is the grey box with outlines; look A is applied only by `-hellLook A`.
  - The three floors don't read as a descent: floor 3 is floor 1 with a violet sky.
  - The darkness has no look.
- **Enemies:** dark bat-like imps with pale soulfire outlines read well at range (`A/enemies-at-range.png`). Vesper is a
  primitive stand-in.
- **Sound:** a credible set of 75 sounds and three floor beds. **There is no music at all yet** (round 1 sketches await
  the creator). Darkness has no sound, and the dilation muffle is off.
- **Story framing:** the God of Death's voice teaches the powers she gave you. That is a good framing device for the
  themes, and it costs nothing more.

**Score today: about 2.5 of 5.** The fire mood is strong; shadow and time are carried only by UI colour and effects; and
the floors don't escalate.

---

## 4. Faults against the event's advice, ranked by score impact

1. **The trailer plan doesn't *detail* the themes** (core 15 and synergy 10 are judged from the trailer alone). The
   handbook says the trailer "must detail how exactly the themes were incorporated". The plan's "shown, never
   captioned" and "no text over gameplay", with one narrated line ("Shadow to hide in. Time to bend."), risk a judge
   missing the Shadow rule and the synergies entirely. The fix is cheap and lives in the stylised minute.
2. **Shadow is a placement rule, not a system.** Enemies ignore darkness everywhere except inside Nightfall and the
   Poacher's pocket. Core integration for Shadow is weak, and the game's own line "Shadow to hide in" is unkept.
3. **Fire and shadow are walled off from each other.** The lint keeps light out of darkness, so the most natural pair in
   the theme set (light destroys shadow) never happens in a room. This costs synergy points.
4. **Darkness is invisible, and the game is captured in the grey box by default.** Dark zones look like glass cubes, and
   floor 3 has no Black Glass identity. Atmospheric framing, and every Shadow shot in the trailer, suffer.
5. **Time's story stake (your sister's clock) isn't felt in play.** Deaths and slowness cost nothing, so the premise's
   time exchange stays text.
6. **Fire is scenery for the player.** Only 2 of 9 skills are Fire, and Hellfire Trail and Ember Rounds are unbuilt.
7. **No music,** and no darkness or dilation audio treatment (atmospheric framing; the trailer is cut to music).
8. **Vesper's Shadow identity is diluted:** orange bolts, a stand-in body, and her Set A "she shadows you" glimpses
   unbuilt.
9. **Burns don't rewind,** which forbids the cleanest Fire x Time shot (B11).

---

## 5. What to shore up first (ranked by theme points per hour)

### In the game

**1. Shadow hides you: darkness blinds the enemies, and fire light exposes you.**
- **What:**
  - In `HellTargeting`, give a bot the never-live stand-in target when the player stands in darkness
    (`HellDarkness.IsDark` at chest height) and is beyond a short reach (about 4 m). Use the rule the Poacher job
    already has.
  - Firing your gun, or standing within reach of a burning enemy or a fire light, reveals you for about 1.5 s.
  - A bot that loses you searches toward your last seen position (the kit's search state).
  - Ambush's 3x first shot makes striking *from* the dark pay.
- **Lifts:** Shadow core integration (a large step), plus Fire x Shadow synergy, because fire is now what finds you.
- **In the trailer:** an imp's outline drops from its alert pulse to idle the moment you step into the violet dark. Then
  a 3x Ambush shot out of the shadow, and the pack turns toward the muzzle flash. It is a two-second, self-explaining
  shot.
- **Size:** 1-2 h with tests (the stand-in target and the dark query already exist).
- **Touches:** `Scripts/Player/HellTargeting.cs`, `HellDarkness.cs`, a reveal hook on the player's shot (`CueRaised` /
  `HitLanded`), and the tutorial line `tut.shadowstep` (one more sentence).
- **Pairs with:** item 3 (more dark), or the dark zones are too few to hide in.

**2. Fire kills shadow, live.**
- **What:**
  - Register **moving lights** with `HellDarkness.RegisterLight`: every ember bolt in flight (radius about 2 m), every
    burning enemy (3 m), Combustion's blast (6 m for 1 s), the lava ooze's fresh pools.
  - A Step aimed into a dark zone that a bolt is crossing is refused, and the marker turns red with a flicker.
  - Under item 1, an imp's volley that lights your hiding place exposes you.
  - Keep the lint as it is: it guards the *static* lava lights only.
- **Lifts:** Fire x Shadow synergy (the headline pair), and Fire as a mechanic for the enemies, not scenery.
- **In the trailer:** you crouch in a dark alcove, an imp's bolt sweeps past and lights it orange for half a second, and
  you Step out as the alcove floods. It is the shot that makes "Fire, Shadow" one sentence.
- **Size:** 1 h. `RegisterLight` exists. It needs per-frame unregistration (a small pooled list) and a dark-zone glow
  that reacts.
- **Touches:** `HellDarkness.cs` / `DarknessField` (dynamic light slots), `SkylineBolt.cs` (the pool is already exposed),
  `HellBurn.cs`, `HellFx.cs` (the zone's glow).

**3. Make darkness look and sound like darkness, and give floor 3 its Black Glass.**
- **What:**
  - Replace the translucent box with an ink-black floor pall: the art pass's `#2A1640` with a soft violet rim and a few
    drifting ink wisps (`HellMotes`). Black fog inside the zone, and no lava light on its floor.
  - Add a low-pass "hush" on the world sound while you stand in the dark.
  - Make dark zones larger and more frequent on the safe paths too (alcoves behind cover and under ledges), so hiding
    (item 1) is a route choice and not only a landing.
  - Floor 3: apply direction C's colour-free stone inside look A, as the art pass already recommends. Only fire, the
    shadow pall and the outlines keep colour.
  - Make look A the Hell scene's default, so every capture uses it.
- **Lifts:** atmospheric framing; Shadow core (readability makes the rule learnable); the trailer's floor progression
  (grey ash, red iron, black glass).
- **In the trailer:** every Shadow shot becomes legible. B8, "the stone loses all colour", becomes a real act 2 beat.
- **Size:**
  - the pall and the hush: about 1 h;
  - the floor 3 palette: about 1 h (`HellLookSwitch` already has C);
  - extra dark alcoves: 1-2 h with the lint and the fast-line sims re-run.
- **Touches:** `HellLookSwitch.cs`, `HellLookOptions.cs`, `SkylineArenaBuilder.Hell.cs` (the dark zone's material and
  fog), `HellSoundBinder.cs` (the hush), `HellRoomLayouts.cs` (alcoves: re-run `HellRoomLint` and `HellFastLineTests`).

**4. The sister's clock: make the Time stake felt.**
- **What:**
  - Build the written death mechanic: the God of Death pulls you back, rotating her death lines (HELL-SCRIPT section 5),
    and each death and each Rewind "comes off her clock".
  - Show it as a small hourglass on the HUD, and as "TIME OWED" on the stats screen.
  - Optionally, let it feed the ending's rule alongside the jobs.
  - It costs nothing in difficulty: it is framing and a stat.
- **Lifts:** Time atmospheric framing, and Time x story synergy (the premise's exchange of time is now in every death).
- **In the trailer:** one stylised insert, an hourglass of embers losing a grain as the AGAIN banner hits. That is Fire
  x Time in one object, which also serves the "Cinder Clock" title idea.
- **Size:** 45 min.
- **Touches:** `HellFlowText.cs` (`DeathLines`), `HellRunDirector.OnWrecked`, `HellRunStats.cs`, `HellScreens.cs`
  (the stats row), a HUD element in `HellHud.cs`.

**5. Burns and fire rewind (Fire x Time).**
- **What:** snapshot `BurnStatus` and the lava pools' burn state in the Rewind, so a Rewind visibly un-burns enemies and
  re-ignites what burned. Add burning enemies to the ghost scrub (an ember ghost).
- **Lifts:** Fire x Time synergy, and it unblocks trailer shot B11.
- **In the trailer:** a Combustion chain, then a Rewind: the explosions suck back into the bodies, the enemies stand up,
  and you do it again better.
- **Size:** 45-60 min.
- **Touches:** `Scripts/Bots/HellBurn.cs` (make it `IHellRewindable`), `HellRewindFx.cs`.

**6. Vesper "shadows you" (story Set A), with violet bolts.**
- **What:**
  - Build the recommended beat set: a veiled figure on a far ledge that dissolves in *your* Shadow Step smoke when seen,
    your violet marker flicking up where she reappears. The beat system's `FigureAhead` staging is most of it.
  - Tint her boss bolts violet: give the bolt pool a per-shot colour field that is snapshotted with the pool.
- **Lifts:** Shadow framing and story synergy (she uses your Shadow and Time powers), and the trailer's act 3 hook.
- **Size:** 1.5-2 h.
- **Touches:** `HellBeatSets.cs`, `HellLines.cs`, `HellBeats.cs`, `SkylineBolt.cs`, `HellVesperFight.cs`.

**7. Music and audio treatment for the themes.**
- **What:**
  - Turn on `DilationMuffle` and `DilationHeartbeat` (minutes).
  - When the music player lands, "dark zones drop the drums", and the Rewind reverses the music for its 0.5 s (already
    in HELL-MUSIC).
- **Lifts:** atmospheric framing.
- **Size:** minutes for the switches. The music itself is the creator's sessions.
- **Touches:** `HellSoundTable` / `HellSoundBinder` flags, and the music player.

### For the trailer (capture-time framing)

**T1. Three theme cards in the stylised minute, so the trailer *details* the themes.**
- **What:** at the start of each act, a 2-3 s stylised card in the enemy-card style, black plus the theme's colour, with
  one sentence of rule each:
  - "SHADOW: step only where the light can't reach. Hide where it can't find you."
  - "TIME: take back four seconds. Everything comes with you."
  - "FIRE: Hell burns. And fire is what finds you."
- It is not text over gameplay, so it keeps the creator's rule, and it meets the handbook's "must detail how exactly".
- End the title card on three marks (a flame, a shadow, an hourglass) under the title.
- **Lifts:** core and synergy as the judges *perceive* them. This is the cheapest points on the board.
- **Size:** 1 h inside the card template the enemy cards already need.
- **Touches:** the trailer edit, the HELL-TRAILER shot list, and the HELL-SCRIPT `tr-*` lines.

**T2. Shoot for synergy, one shot per act.**
- **Act 1:** the stealth-and-ambush shot (items 1 and 2) instead of A9's plain reload.
- **Act 2:** the Combustion-then-Rewind shot (item 5) in place of B11.
- **Act 3:** Vesper's Nightfall *with the lights visibly dying* (C4). Hold the lights going out for a second.
- **Everything:** captured with `-hellLook A` (the default today is the grey box).
- **Size:** capture time only.
- **Touches:** HELL-TRAILER section 3 (B6, B11, A9, C4), and the `-hell-take` commands.

**T3. Make the one premise still that holds all three themes: S3, "the deal".**
- **What:** two hands, black shadow coiling round a clock spinning backwards, lit teal and ember. If only one premise
  image gets made, make this one, and put the narration's "Shadow to hide in. Time to bend." on it.
- **Size:** 30 min via Flow, after the art direction pick.

---

## 6. Verdict

Today I'd put the theme score at about **17 of 30**:
- core integration about 9 of 15 (Time strong, Fire as set dressing, Shadow a landing rule);
- synergy about 5 of 10 (the boss does it; the rooms keep the themes side by side, and the lint walls fire off from
  shadow);
- atmosphere about 2.5 of 5 (no music, darkness without a look, floors that don't descend).

The trailer as planned would probably score a point or two lower, because it shows Shadow without saying what it is and
captures in the grey box unless told otherwise.

The creator's read holds, with one correction: Shadow isn't just "weaker", **its own promise is broken.** "Shadow to hide
in" is true only inside one skill and one job.

After the top five (Shadow hides you; fire kills shadow live; darkness and floor 3 that look the part; the sister's
clock; and the trailer's theme cards, with burns rewinding as the runner-up), I'd expect about **23-24 of 30**:
- core about 12;
- synergy about 7.5 (fire *finds* you in the shadow, and time *un-burns* the fire);
- atmosphere about 3.5-4.

That is roughly 4-6 hours of agent work plus capture.
