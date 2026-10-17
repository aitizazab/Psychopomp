# The night sprint (2026-10-17 23:00 to about 03:00): playable by 03:00

Drafted for the creator at 23:00. Goal: a playable game end to end by 03:00 (title to credits), so the time after it
goes to the trailer. Usage: over 50% of the week free, to be spent before 09:00. Everything here runs without the
creator's input; anything that needs a pick defaults to the agents' recommendation and stays swappable.

## Running already
- **Rooms** (branch `hell-rooms2`): Hold the Brand charged by speed; the Collapse made hard (fronts 10.5 / 8 m/s,
  debris, imps), the floating baffle walls fixed.
- **Art research** and **sound research**: free content first, then premier examples; review pages in
  `FableAnimationTests/art/review/2026-10-17/`.

## Lanes (one agent each, own worktree and files; the main session merges and runs Unity, one job at a time)
1. **Run flow and screens:** a title screen (start, settings, quit), the God of Death's opening (text cards and
   placeholder lines marked for the creator to rewrite), floor intro cards, the credits, the stats screen (time per
   floor, deaths, Rewinds, Shadow Steps, jobs done, fast paths taken), back to the title. Fix a Rewind across a room
   boundary and rewind the room's spawn counter.
2. **The jobs (the God of Death's favours):** the creator (23:30): more than "kill a marked imp": jobs like
   "assassinate this annoying imp aristocrat, off the beaten path", the errands a God of Death gives her minion
   because he happens to be in the area: not worth sending someone for, but important enough for her attention.
   1-2 per floor, off the main line, each offering an upgrade; the count is tracked for the ending.
3. **Vesper, the boss:** her arena at the bottom of floor 3; she fights with your powers mirrored (Shadow Step, a
   Rewind of her own) in 2-3 phases, never quite going for the kill; her lines as placeholders.
4. **The ending:** the decision scene after her defeat, the escape (the Collapse with the lantern or with Vesper running
   beside you) and the three endings (kill; spare with most jobs; spare with few), into the credits.
5. **Fast lines proven:** simulate the fast lines with the pure movement engine (the Sigils' A-B and E-F above all),
   fix the geometry that fails, shorten the Sigils' 931 m safe path with a crossing.
6. **Enemy readability** (the creator: AFTER the art and sound pass, using both audio and visuals): a spawn warning
   seen and heard, clear silhouettes and outlines (acid lime proposed) against the new art, a sound signature per enemy
   that tells you what and where it is.
7. **Sound in:** wire the sound research's best free picks into every hook (abilities, enemies, hazards, objectives,
   menus), through the kit's mixer groups; swappable per sound.
8. **Art in:** the art research's recommended direction applied to surfaces, lava, lighting, fog and fire VFX, unless
   the creator picks another before bed.
9. **Trailer tooling:** port the clean-picture switch (an empty HUD layout, voices muteable) and the 120 fps
   recorder from `VibeJamKit-capture`; a frame-time check of the Hell build against 120 fps.

## The main session
Merges each lane, builds the scene and the player (capped-run), plays scripted captures, has adversarial judge agents
review them, fixes what they find, and refreshes `HELL-RESTART.md` after every build. A stay-awake runs all night.

## Go (the creator, 23:30): full permission to run anything that doesn't endanger the PC or their privacy and
security. The art direction is confirmed before their 3-hour nap.

## Order
Now: lanes 1, 2, 3, 5, 9 (no input needed) alongside rooms. When the research lands (about an hour): 6, 7, 8. When
1-3 merge: 4. Target: merged and built by 02:30, judged and fixed by 03:00.

## Queued (the creator, 2026-10-18 ~00:40)
- **Theme-adherence review** once most layers are merged (effects, tutorials and beats, ending): an agent that does NOT
  implement anything; it critically reviews the build against the event's advice (themes = 30%: core integration 15,
  multi-theme synergy 10, atmospheric framing 5; Fire, Shadow, Time), finds the faults and ranks what to shore up
  first. The creator's read: Time has plenty of seeds (Vesper's story, ours, the abilities), Fire is everywhere in the
  setting, Shadow is weaker (mostly the abilities).
- **The ending lane** (decision, escape, three endings) started 00:40, branch `hell-ending`.
