# The event game: the descent into Hell (working name HellDescent), design for day 1

Written 2026-10-17 (day 1) from the creator's calls. Themes: **Fire, Shadow, Time**. Built on the kit's Skyline Repo
code (movement engine, guns, bots, waves, objective circles, floor builder from data, HUD). Goal for today's evening:
**all mechanics in**, then art and the trailer.

## Premise (story details open; decide later)
- Setting = **Fire** (Hell). Premise = **Time**. The protagonist / backstory = **Shadow**.
- Your sibling died. You refuse to accept it and make a deal with a powerful entity: powers over time and shadow in
  exchange for going into Hell, bringing your sibling back, and doing some jobs for the entity on the way. (Who the
  entity is, why it can send you and grant powers, what it wants, why it can't go itself: OPEN, decide later.)
- **The creator's story pitch (2026-10-17 evening; overrides the entity notes above where they differ):**
  - The entity is **a God of Death** (maybe **Baja**, Skyline's planned secret boss; Skyline's docs call Baja "she";
    the creator isn't sure yet who Baja or the God of Death is).
  - She wants to reap the soul of **<name>**, whom **Lucifer took to Hell still alive**, effectively immortalising her,
    as his bride. That irritates the God of Death, but she doesn't care enough to do anything about it, until you come
    along in a furious rage, willing to do anything to fix things.
  - She's about to kill you when she gets "the funniest idea": send you to the depths of Hell to kill <name> and bring
    her soul back to Baja. Baja flippantly warns you about taking too long; you go into Hell to find the person you're
    trying to save.
  - **Final boss:** likely <name> or Lucifer, then an **escape sequence**.
  - **A possible ending choice:** spare <name> (drawing Baja's ire, or Baja's respect for daring to disrespect such a
    powerful being), or accept the dichotomy of taking another's partner to save your own flesh and blood, and deliver
    <name>'s soul to Baja. <name> then needs development as a character.
  - Then the credits, an end screen with cool stats, and back to the title screen.
  - Open questions after the pitch: `HELL-RESTART.md`, "Story".
- **The creator's answers (2026-10-17, later that evening):**
  1. The God of Death is **not** Skyline's Baja, but the two could be linked later (Baja possessing Skyline's AI for
     some purpose).
  2. <name> is **Vesper, the Evening Star** (picked 2026-10-17): Lucifer is the Morning Star; she is his bride.
     Centuries ago she made the same deal you are making: her daughter died, she bargained with Lucifer (she stays
     as his bride, her daughter lives). He kept her alive and immortal as a trophy; the daughter lived, grew old and
     died long ago. She is you further down the same road. She reaches you once per floor (floor 1: her voice in the
     Soul Lantern, hostile; floor 2: her deal and her daughter revealed, curious; floor 3: she sees herself in you).
     **The creator's note:** sparing must be more deliberate than turning away, and show why she comes with you
     instead of accepting defeat (pitch in `HELL-RESTART.md`, "Story").
  3. **The deal:** she gives you the powers and access to Hell (she has no access to Hell herself) in exchange for
     favours, and mainly for killing <name> to teach Lucifer not to mess with her. Your **sister** is in Hell.
  4. "Don't take too long" is flavour. The Time theme lands in the exchange: <name>'s time (or your own?) for your
     sister's.
  5. Time and shadow powers: she deals with mortality and crossing over. **The jobs stay:** each gives a chance at an
     upgrade or power, and doing most or all of them is what makes her spare you if you spare <name> after beating her
     and just grab your sister and escape.
  6. The final boss is probably <name>. Either a post-battle choice (kill or spare), or a full-length one (you don't
     fight her, and eventually she stops fighting back). The creator asked for a pitch of which is better.
- Three floors, descending. After each floor: pick one new skill (shadow, time or fire flavoured) from three offered.

## The player's abilities
1. **Shadow Step** (active, cooldown): blink to a spot you aim at within range (Omen's teleport / Dishonored's Blink):
   a short aim preview (a marker where you'd land, valid only on reachable ground or ledges within ~18 m and line of
   sight), then you arrive instantly keeping your momentum direction. Cooldown ~6 s (tunable).
2. **Rewind** (active, cooldown): the WHOLE game state goes back 4 seconds: player position, velocity, look, health,
   ammo, enemies (positions, health, alive/dead, AI state), projectiles, hazards (collapsing platforms, lava timers),
   the objective's progress and the room clock. A short visual (desaturate, reverse whoosh). Cooldown ~20 s.
3. **Time Dilation** (passive): pressing aim-down-sights slows time for 3 s after the input; how much depends on your
   speed at the press (standing: little or none; full wall-run/slide speed: strong, e.g. down to 0.35x). The player's
   own aim/look stays at full rate.
4. **Floor rewards** (after each floor, pick 1 of 3): e.g. Shade Decoy (leave your shadow to draw fire), Dark Cover
   (a sphere of darkness, enemies inside can't see), Echo (your last 4 s replay as a ghost that shoots), Stasis (freeze
   an area: bolts hang in the air), Second Chance (once per floor, death rewinds automatically), Ember Rounds (burn),
   Hellfire Trail (the Shadow Step leaves fire), Cinder Skin (walk on lava for a moment). Build 3-5 of them today.

## Enemies (same functions as the kit's bots, new look)
| New | Replaces | Does |
|---|---|---|
| **Fire imp** (floating) | the mall-cop drone | hovers, fires FAST fire bolts (much faster than the drone's) |
| **Scythe demon** (ground) | the spotlight bot | spawns demon scythes that appear, PROJECT their path (a visible line), then BEAM along it |
| **Lava ooze** (ground, pulsating) | the floor-cleaning bot | pulsates; its area attack (the molly acid idea) becomes lava |

## Hazards
- **Lava:** damage over time while touching it, instant heavy damage and a respawn-to-last-safe-spot if you sink.
- **Collapsing platforms:** shake for ~0.8 s after you land on them (or on a timer), then fall; they come back after a
  while (or never, per room).

## The rooms (the creator's rule: every room has an objective AND always offers a safe path plus a more dangerous, faster one)
**Deck:** each floor has **6 rooms; a run draws 4** per floor (random, seeded). Six room types, each built once as a
layout and varied per floor (hazard intensity, enemy mix, mirrored or rotated). 3 floors x 6 = 18 rooms from 6 layouts.

The **fast path** is the straight line: over lava, across collapsing platforms, through scythe beams; it asks for
movement tech (wall-run, slide, jump chains, Shadow Step) and speed. The **safe path** goes round: solid ground, cover,
longer. Which path is which must be readable at a glance (lava glow and broken edges = fast and risky; stone walkways
and walls = safe and slow).

1. **The Six Sigils** (the creator's six-point room): touch six sigils in order, A to F, across one big room. Each leg
   between sigils needs a different movement trick and speed (A-B a wall-run over lava, B-C a slide under a beam, C-D
   a Shadow Step across a gap, D-E a jump chain over collapsing platforms, E-F a long fall with an air-aimed shot to
   open a gate); every leg has the long safe way round.
2. **The Soul Lantern** (carry): carry a lantern from the far end to an altar. While carrying you can't Shadow Step and
   move a little slower. Fast: a collapsing bridge over lava; safe: switchback ledges along the walls.
3. **Break the Chains** (destroy): three chain anchors hold the exit gate, spread at different heights. Fast: shoot
   them from mid-air off wall-runs (Time Dilation helps); safe: climb ramps to each one.
4. **Hold the Brand** (hold a circle): stand in the brand circle until it's charged (the kit's tow-circle mechanic),
   waves arrive on the clock. Fast: the circle on an island reached over lava; safe: a long bridge exposed to the
   imps' fire.
5. **The Collapse** (escape): reach the exit before the floor falls away behind you. Fast: a straight run that needs a
   slide, a jump and a Shadow Step; safe: a longer route where the collapse comes more slowly.
6. **The Ooze Wells** (seal): seal three lava wells (stand next to each for 3 s) that keep spawning oozes. Fast: hop
   across the lava between wells; safe: the ring walkway round the edge.

Between rooms: a short corridor or drop. End of floor: a shaft down to the next floor, then the skill pick.

## Floors (dressing and difficulty)
1. **The Ember Gate:** ash and cinders, little lava, mostly imps; teaches the abilities.
2. **The Molten Works:** lava rivers, chains, collapsing bridges; imps + scythe demons.
3. **The Black Glass:** obsidian and shadow, beams everywhere; all three enemies.

## Build order today
Rooms framework + 6 layouts (agent 1), abilities (agent 2), enemies + hazards (agent 3), in parallel; then the main
session merges, runs Unity, and the creator plays. Art and the trailer in the evening.
