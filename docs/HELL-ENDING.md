# Hell: the ending (branch `hell-ending`)

Built 2026-10-18 night (sprint lane 4). After Vesper kneels: **the decision in play**, **the escape** up through the
Collapse, and **the three endings**, then the flow's credits, stats and title. Nothing here has run in Unity yet: the pure
tests and the Unity-side compile check pass; the play checks are at the end.

## Files

| What | Where |
|---|---|
| The pure rules: every line (`HellEndingLines`), the ending from the choice and the jobs (`HellEndingRules`), the decision (`HellDecision`), the escape route and your sister's soul (`HellEscapeRoute`, `HellSoulPickup`), Vesper beside you (`HellFollower`), the sound cue names (`HellEndingCues`) | `Packages/com.vibejam.jamkit/Runtime/Skyline/Hell/HellEndingRules.cs` |
| Tests (22) | `Packages/com.vibejam.jamkit/Tests/Skyline/Hell/HellEndingTests.cs` |
| The ending in Unity: the hook, the decision, the escape, the cards | `Assets/JamKit/Templates/SkylineRepo/Scripts/Hell/HellEndingDirector.cs` (Add Component > JamKit/Hell/Ending Director; it makes itself when needed) |
| The prompt, the hold ring, the escape's line, the banner (under the HUD's safe area) | `Scripts/Hell/HellEndingHud.cs` |
| The cards over black (a screen like the opening's) | `Scripts/Hell/HellEndingCards.cs` |
| The test bed | `Scripts/Hell/HellEndingTestBed.cs` (Add Component > JamKit/Hell/Ending Test Bed) |
| Sound rows | `HellSoundTable.Rows`, section "the ending" (9 rows, reused files) |

**Edits in other lanes' files:** only the 9 rows in `Packages/com.vibejam.jamkit/Runtime/Skyline/HellSound/HellSoundTable.cs`
(the section `// ---- the ending`, before the beds). Nothing else outside the new files: the director listens to
`HellRunEvents.EndingRequested` (subscribed at scene load, like the boss), reads `HellVesperFight.Instance.Body`, and re-runs
floor 3's Collapse room through its public `Begin` / `Restart` / `End`.

## 1. The decision (in play)

Vesper is beaten and kneels where the fight left her (`HellVesperFight.VesperPosition`). The Rewind's history clears
(`RaiseRewindBoundary`). She says `dec-01` ("Go on. She's waiting for my soul..."). Hell starts to shake: the camera's
trauma holds at a level that builds from 0.2 to full over 45 s, with a rumble that comes more often (every 3.2 s down to
0.9 s). **No timer**: standing there never decides for you; the God of Death presses at 8 s and 16 s (`dec-god-01`,
`dec-god-02`).

- **Kill:** shoot her. Any shot that hits her body or her heart ends it (her hit zones answer to the ending while you
  decide: `HellEndingDirector` swaps their owner and gives them back after). She dissolves into smoke; her soul glows
  violet in a lantern in your hand; her last line `dec-kill-01`.
- **Spare (deliberate):**
  1. **Lower the gun:** don't fire for 1.5 s, or hold Reload (0.35 s). A shot that misses her raises it again and empties
     the ring.
  2. **Walk to her:** within 2.8 m.
  3. **Hold Use** (the Inspect binding: **F** / D-pad up; or **B** on a pad) for 1.6 s: the ring round the crosshair fills.
     Letting go drains it (1.5 of the ring a second); walking away cancels it.
  - Then the offer of a purpose (`dec-you-spare`, the player's words as a subtitle only), her answer (`dec-spare-01`),
    and she rises (`dec-spare-02`: "Run, then. I know the way up.").
- **The prompt** under the crosshair follows the state, with the controls filled in for the device you use:
  - out of reach: `WALK TO HER TO OFFER YOUR HAND   [LEFT MOUSE] END IT`;
  - in reach, gun up: `LOWER YOUR GUN: HOLD [R], OR HOLD YOUR FIRE`;
  - ready: `HOLD [F] OFFER YOUR HAND   [LEFT MOUSE] END IT` (the ring's track shows);
  - holding: `OFFERING YOUR HAND...`.
- **The choice is final:** once made, nothing changes it. The decision isn't a rewindable, a hit after a spare does
  nothing, and the Rewind can't reach back before the kneel or the escape's start.
- **A death while deciding** (unlikely: no enemies, the arena's lava is off once the fight ends) puts you back where you
  stood.

## 2. The escape

**Where:** floor 3's Collapse (The Black Glass), in its own slot, as the scene builder built it. Reused, not rebuilt,
because the Collapse's fronts, timeline tiles, debris, wall imps, sink-restart and Rewind all live in `HellRoom` and
`HellCollapsePlatform`, which read their data from `HellWorld.Room(floor, kind)`. A new slot would have meant a second copy
of that machine. `HellRoomLayouts` is untouched. If the creator wants a dedicated strip later, it is a new room def plus a
builder entry; `HellEscapeRoute.Room()` is the one place to point at it.

**The cut:** after the choice's lines, a fade to black (with the escape rumble). The room starts from its top (`Begin`),
and you stand at its entry with full armour and ammo. A `RewindBoundary`, then the fade in, **RUN**, and the escape bark
(killed: the God of Death `esc-god-01`; spared: Vesper `esc-vesper-01`). The line at the top reads `GET OUT (YOUR SISTER'S
SOUL: ON THE WAY)`.

**The run:** exactly The Collapse at floor 3's numbers:
- the fast lane's front at 11.1 m/s, the safe lane's at 8.5;
- the debris;
- the imps at the walls.

The room's own objective (reach the exit) ends it.

**Killed: her soul as a lantern.** Decided: **no slowdown, and the Shadow Step stays allowed**. The Soul Lantern room's
carry rules (0.8x speed, no Shadow Step) would make this run unwinnable. The Collapse is tuned so a sprint survives the
safe lane with about 1 s to spare, and the fast lane needs a Shadow Step across its 12 m gap. Her soul weighs nothing now;
the lantern is a glow in your hand (violet), not a handicap.

**Spared: she runs beside you.** Her own body (the boss's `HellVesperBody`, standing pose) glides at your left shoulder:
1.8 m to the side, 0.8 m back, at up to 16 m/s (`HellFollower`).
- Every 7 s while you run, she **Shadow Steps 7 m ahead** of you (smoke at both ends, a step sound) and waits up to 1.4 s
  for you to draw level.
- If you get more than 9 m from her place (your own Shadow Step, a fall), she Shadow Steps back to your side.
- She ignores the lava and the falling floor (she is Vesper).
- Shooting her does nothing.

**Your sister's soul (both endings):** a gold soul hovers 1.2 m up, halfway along each lane's running line (the fast
lane's and the safe lane's; take either and both go).
- Passing within 3 m takes it. The check is swept along your movement, so a slide or a Shadow Step through it counts.
- It goes into the lantern: gold, beside Vesper's violet on a kill.
- The line becomes `GET OUT`.
- Spared: Vesper's halfway line `esc-vesper-02`.
- If you somehow pass it by, it follows you out at the exit (logged), so the run never dead-ends.

**Dying** (or sinking: the Collapse restarts itself) **restarts the escape**:
- the room from its top;
- you at the entry;
- the soul back in place;
- the lantern as you left the arena;
- Vesper at your side;
- `RoomRestarted` (the Rewind drops its history).

**The Rewind** works as in the Collapse: the room, its tiles and bots rewind themselves. The ending adds the soul (taken or
not) and Vesper's place and timers.

## 3. The endings

At the exit: the bell, a fade to black, and the ending is picked: **kill**, or **spare** with or without the God of
Death's favour (`HellEndingRules.Pick(killed, HellJobs.OfferedCount, HellJobs.DoneCount)`, the same rule as
`HellJobs.Favoured`: at least two-thirds of the jobs offered, and at least 2). Then, over black, while the game holds
still:

1. two voiced lines of the God of Death as cards (speaker over the words);
2. the epilogue card;
3. the ending's name (`HellEndings.Title`), 3.2 s.

Each card lasts its voice plus a second; Enter, Space, Escape, a click, A or B skips to the next. Then
`HellRunEvents.RaiseEnding(id)`: the credits, the stats (with the ending's name), the title.

| Ending | Id | Lines (keys) | Title (draft) |
|---|---|---|---|
| Kill: her endless time pays for your sister's; you carry what you did | `kill` | `end-kill-01`, `end-kill-02`, `end-kill-card` | THE SOUL DELIVERED |
| Spare, favoured: the God of Death respects the nerve, all of you walk out | `spare-kept` | `end-favour-01`, `end-favour-02`, `end-favour-card` | THE EVENING STAR SPARED |
| Spare, not favoured: the time comes from you; your sister lives, you don't leave | `spare-cost` | `end-few-01`, `end-few-02`, `end-few-card` | SPARED, AT A PRICE |

**Note for the judges' chapter:** the Vesper chapter skips the floors, so no job is offered and a spare always ends
`spare-cost`. That is the rule as written (no favour without the jobs). If the creator wants the chapter to show the good
spare, the switch is one line in `StartCards` (for example, treat 0 offered as favoured).

## Words and voices (ALL PLACEHOLDERS)

Every line is in `HellEndingLines.All` (the keys are `docs/HELL-SCRIPT.md` sections 10, 10.1 and 11, so the placeholder
voices already in `Resources/SkylineAudio/hell-voice/god` and `.../vesper` play). The speakers:
- `god` and `vesper` are voiced: the line's subtitle, and its clip at the Voices volume (so `-hell-voices` and the clean
  picture obey it);
- `you` is a subtitle only;
- `card` is text on black.

The prompts are UI words, also in `HellEndingLines` (`Prompt*`). To rewrite: change the text there and in
`HELL-SCRIPT.md`, then re-run the voice generator.

## Sounds (new rows in `HellSoundTable`, reused files until the ear check)

| Cue | File | When |
|---|---|---|
| `hell/ending/rumble` | `hell/world/platform_shake` | Hell shaking while you decide (more often as it builds) |
| `hell/ending/hold` | `hell/world/sigil` | you start to offer your hand |
| `hell/ending/spare` | `hell/world/brand_done` | she takes your hand |
| `hell/ending/kill` | `hell/world/anchor_break` | the shot that ends her |
| `hell/ending/soul` | `hell/world/lantern_pickup` | a soul into the lantern |
| `hell/ending/step` | `hell/abilities/shadowstep_in` | Vesper Shadow Steps beside you |
| `hell/ending/escape` | `hell/world/shaft_start` | the cut into the escape |
| `hell/ending/out` | `hell/ui/room_cleared` | out of the Collapse |
| `hell/ending/card` | `hell/ui/skill_open` | an epilogue card, the title |

Not wired: a carry hum for the ending's lantern (the binder's `lantern_carry` loop follows the Soul Lantern room only).
The sound binder follows only the run director's current room, so in the escape it plays no room sounds (the bed, braziers,
lava bubbles). The director plays the one that matters itself: `hell/world/platform_fall` when a floor tile within 24 m drops
(at most one every 0.2 s, silent after a restart or a Rewind). Check by ear.

## Scripted runs

A scripted player (`ScriptedInput`), batch mode or `-skyline-demo` chooses by itself after 2 s:
- `-hellEnding spare` spares; anything else kills;
- the escape is skipped;
- the cards run 0.3 s each.

The log shows `HELL_ENDING ...` and the flow's `HELL_STATS ... ending <id>`.

## Check it in Unity (main session)

1. **Compile.** No scene change is needed: the director makes itself on `EndingRequested`, and its HUD and cards are made
   at run time. Unity makes the new `.meta` files.
2. **The full path:**
   - title, then CHAPTERS > VESPER;
   - beat her (the boss test bed's F6 helps: 250 damage);
   - she kneels and the log shows `HELL_ENDING decision`.
3. **The decision:**
   - her line, then the shaking builds;
   - at 8 s and 16 s the God of Death presses;
   - the prompt changes with distance and with the gun lowered (stop firing 1.5 s, or hold R);
   - the ring round the crosshair fills over 1.6 s while F is held next to her, and drains when let go;
   - a shot that misses empties it;
   - on a pad: B to offer, X (Reload) to holster, and the prompt shows the pad's names.
4. **Kill:**
   - shoot her (body or heart): the hit marker, she breaks into smoke, her line;
   - a violet lantern in your hand, lower right;
   - the fade, then the escape.
5. **Spare:**
   - the lines (YOU, then VESPER twice), she stands;
   - the fade; in the escape she runs at your left and steps ahead now and then (smoke);
   - she catches up after your own Shadow Step.
6. **The escape:**
   - you stand at floor 3's Collapse entry; RUN, the bark;
   - the floor falls behind you, debris, imps;
   - the gold soul halfway on each lane: running through it puts it in the lantern, and the line changes;
   - die or sink: the escape restarts with the soul back and Vesper beside you;
   - a Rewind mid-run puts the soul back if it was taken after the rewound moment.
7. **The endings:**
   - at the exit: black, two God of Death cards with her voice, the epilogue card, the title;
   - then the credits, and the stats showing the ending's name;
   - check all three: kill; spare after doing most jobs (a full run); spare with few (the Vesper chapter).
8. **Clean picture:**
   - `-hell-clean` hides the prompt, the ring and the escape line;
   - the cards stay (like the opening's);
   - `-hell-voices off` mutes the lines.
9. **Test bed:**
   - Add Component > JamKit/Hell/Ending Test Bed in the Hell scene;
   - F9 begins the ending where you stand (a stand-in kneels 6 m ahead if no fight ran);
   - F10 kill, F11 spare.
10. **Likely tuning:**
    - the reach (2.8 m) and hold (1.6 s);
    - the shaking's strength (`Quake`: trauma 0.12 to 0.45);
    - Vesper's side offset and blink rhythm (`HellFollowerTuning`);
    - the lantern's place in view (`lanternOffset`);
    - the soul's light.

## What's left (honest list)

- Nothing has been played; no Unity-side test.
- The gun isn't visibly lowered: "lowered" is a state the prompt shows. The viewmodel has no holster pose to drive.
- The escape reuses floor 3's Collapse as built; it isn't re-dressed (no special lighting or crumbling arena). The cut is
  a fade to black.
- Vesper glides (no run animation; her body is the boss's primitive stand-in).
- The lantern has no carry hum; the soul views are a glow sphere, a halo and a light.
- The cards' text is the placeholder script; the ending names are `HellEndings.Title`'s drafts.
