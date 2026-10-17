# Hell: Baja's hour (branch `hell-hour`)

Built 2026-10-18 (~01:40) from the creator's design of the same night (`HELL-DESIGN.md`, "The creator's calls, 2026-10-18
~01:30": Time as Baja's closing escape). Baja sets up your way out of Hell. Spend too long down there and it closes, and you
and your sister are damned forever. **Nothing here has run in Unity yet:** the pure tests and the Unity-side compile check
pass. The play checks are at the end.

## The rules as built

1. **The budget, by difficulty:** Easy 90 min, Normal 65, Hard 50 (`HellHourRules.BudgetMinutes`). A skilled run takes
   about 45 at most.
   - **The difficulty** is new: `SkylineOptions.difficulty` (saved with the other options; Normal by default).
   - The title's **START** now asks first ("BAJA'S HOUR", three choices, placeholder words, no numbers). Escape / B goes
     back.
   - The **pause menu** has a Difficulty row on its main page.
   - **Chapters** use the saved setting; they don't ask.
   - Nothing else in the kit read a difficulty before this (the Skyline `DifficultyDef` is only used by the old arena's dodge
     checks), so the difficulty sets **only the hour** for now.
   - Changing it mid-run changes the budget live. The time already spent stays.
2. **Only gameplay time counts** (`HellHourRules.Counts`). Counted:
   - the rooms and the shafts;
   - the boss fight;
   - the decision over the kneeling Vesper;
   - the escape.

   Not counted:
   - the title and every flow screen (the opening's cards, the floor cards, the credits, the stats);
   - the pause menu;
   - the skill pick (and its wait between floors);
   - a story beat's held moment (`HellTime.Has(HellBeats.HoldTimeKey)`);
   - anything that pauses the player's input (`SkylineDeviceInput.Paused`), such as Vesper's fight's own pauses;
   - the ending's scripted stretches: the lines after the choice, the fades, the cards.
3. **Errands cost time** only by taking gameplay time; no artificial cost was added.
   - The numbers: a job is a pocket off the room (18 m) with a 3-9 s mechanic, about 30-90 s with the walk in and out.
   - A full run offers about 6, so doing them all costs about 3-9 minutes: 5-14% of Normal's hour, up to 18% of Hard's.
   - That is a real choice against Hard's 5-minute margin, so jobs are not too cheap. Revisit after the first playtest.
4. **The Rewind does not give Baja's time back.** The hour is not an `IHellRewindable`: it has no snapshot at all (a test
   checks this). The first Rewind of a run gets Baja's remark (`hour.rewind`).
   **Time Dilation:** the hour runs on real, unscaled seconds, so slowing the world doesn't slow her clock. Decided: the
   powers are hers, and so is the clock. A slow-motion trick that also stretched the hour would make dilation a way to bank
   time, which the design doesn't want.
5. **The window closing shows in the escape** (`HellHourGate`). The Collapse's exit doorway gets **Baja's gate**: the
   room's own red gate is hidden (its collider stays) and two dark leaves with ember edges slide in from the sides.
   - **Each escape attempt gets a window:** 13 s, plus 3 s for every minute left on the hour (none when overtime).
   - **How it looks:** the gate is drawn fully open while 40 s or more of its window remain, then closes linearly. It
     grinds (`well_grind`, more often as it closes) and slams (`platform_fall`) when it shuts.
     - plenty left (9 min or more): wide open the whole run;
     - 3 minutes: about half open and closing;
     - out of time: a third open and grinding shut.
   - **Against the floor-3 Collapse** (fast lane about 11-12 s, safe lane about 16-17 s):
     - out of time (13 s): only the fast lines make it;
     - 1 minute left (16 s): very tight on the safe lane;
     - 2 minutes or more (19 s and up): the safe lane makes it.
   - **If it shuts before you reach the exit:** **damnation**. Whatever you chose, the run ends there (a 2.4 s fade, Baja's
     `hour.gate-shut`, then the damnation cards).
   - **A death or a sink restarts the attempt** with a new window from the hour then. It kept running, so each failed try
     costs time, but never less than 13 s.
   - **Out of time before the escape:** the run does not end. Baja says `hour.out`, the hourglass cracks and pulses red,
     and play goes on (the boss, the decision). The escape then starts with the gate nearly shut: the fast lines can still
     make it. Decided so it is felt, not a sudden game over, as the creator asked.
6. **The Lucifer cliffhanger** (`HellHourEndings.Pick`) is a new top ending. It needs:
   - every job **offered** done (at least one offered), or the Vesper chapter's YES;
   - the exit reached with **any time left** on the hour (`LuciferMinLeftSeconds` = 0: not overtime).

   It applies to **both** the kill and the spare, because it is about Baja's favour, not about Vesper. The spare must be the
   favoured one, which all jobs done always is.

   After the normal ending's three cards come two more Baja lines (`end-lucifer-01`, `-02`: she sends you after Lucifer)
   and a cliffhanger card (`end-lucifer-card`, "TO BE CONTINUED"). The ending ends there, before the fight.
   - Ids: `kill-lucifer`, `spare-lucifer`.
   - Draft titles: "THE SOUL DELIVERED. THE MORNING STAR NEXT" and "THE EVENING STAR SPARED. THE MORNING STAR NEXT".

   **Damnation** (`damned`, "DAMNED, BOTH OF YOU") outranks everything. Its own cards are `end-damned-01`, `-02` and
   `-card`.
7. **Chapters start with the hour part spent:** Floor 2 a third, Floor 3 two thirds, Vesper 0.85. These are shares, so they
   scale with the difficulty. A chapter doesn't replay the reminders it skipped.
8. **Stats:** the stats screen's top row gets **BAJA'S HOUR LEFT** (m:ss) or, in overtime, **TIME OWED**. The ending's name
   was already shown. The log has `HELL_HOUR stats: <difficulty>, <m:ss> left|owed, ending <id>`.

**Baja's reminders** (`HellHour.TakeReminder`) come once each, at a share of the hour spent:

| Spent | Key | Placeholder line |
|---|---|---|
| 25% | `hour.quarter` | "Tick tock. Your sister's not getting any less dead." |
| 50% | `hour.half` | "Half my hour, gone. I do hope the sightseeing was worth it." |
| 75% | `hour.three-quarters` | "The door I'm holding open is getting heavy, darling." |
| 90% | `hour.last` | "Hear that grinding? That's your way out, closing. Hurry." |
| 100% | `hour.out` | "That's my hour. The door's nearly shut. Run anyway. I love a long shot." |
| first Rewind | `hour.rewind` | "Cute trick. It doesn't work on MY clock." |
| the gate shuts | `hour.gate-shut` | "Too late. I did say." |

- **All are PLACEHOLDERS** for the creator to rewrite (`HellHourLines.All`). They are voiced when
  `Resources/SkylineAudio/hell-voice/god/<key>.wav` exists; none do yet.
- When several thresholds pass at once, only the latest is said.
- A reminder waits while a story beat speaks, during the ending, and while the hour isn't running.

**The hourglass** (`HellHourglass`) is a small ember hourglass on the HUD, right side above the abilities' bars, with no
number:
- the top bulb is the hour left, the bottom the hour spent;
- a stream falls while the hour runs (it stops under menus and held moments);
- the sand glows hotter in the last tenth;
- in overtime the glass is cracked and pulses red.

It is hidden outside play and once the hour stops (at the exit). It is one element named `hell-hourglass` under
`SkylineHud.Safe`, drawn from `Set(leftShare, running, overtime)`. Its data API is separate (`HellHour.Current`), so the HUD
lane can move (`Place`), restyle (its static colours) or replace it.

**Not built:** the world cue (the shafts' glow dimming with the hour). It was optional and isn't cheap without touching the
shaft builder; the gate carries the "window closing" in the world instead.

## Files

| What | Where |
|---|---|
| The pure rules: `HellDifficulty`, `HellHourMoment`, `HellHourRules` (budget, what counts, chapters, reminders), `HellHour` (the clock), `HellHourGate`, `HellHourEndings` (damnation, Lucifer), `HellHourLines` (every word) | `Packages/com.vibejam.jamkit/Runtime/Skyline/Hell/HellHour.cs` |
| Tests (16) | `Packages/com.vibejam.jamkit/Tests/Skyline/Hell/HellHourTests.cs` |
| The director: the clock's lifecycle and ticking, the reminders, Baja's gate in the escape (self-attaching, like the beats) | `Assets/JamKit/Templates/SkylineRepo/Scripts/Hell/HellHourDirector.cs` |
| The HUD hourglass | `Assets/JamKit/Templates/SkylineRepo/Scripts/Hud/HellHourglass.cs` |

**Edits in other lanes' files** (small):
- `SkylineOptions.cs`: the `difficulty` field, its clamp, save and load.
- `HellFlow.cs` (`HellEndings`): the three ids and their titles.
- `HellEndingRules.cs` (`HellEndingLines.All`): six lines.
- `HellEndingDirector.cs`:
  - `_damned`;
  - `Damn()`;
  - the hour stops at the exit;
  - `StartCards` picks through `HellHourEndings`.
- `HellScreens.cs`: the difficulty question and the stats cell.
- `SkylinePauseMenu.cs`: the Difficulty row.
- `HellTime.cs`: `Has(key)`.

## Check it in Unity (main session)

1. **Compile.** Unity makes the new `.meta` files; no scene change (the director attaches itself to the run director).
2. **Title:**
   - START opens BAJA'S HOUR with three choices, on the saved difficulty;
   - Escape / B goes back;
   - a pick starts the opening, and the log shows `HELL_FLOW difficulty Hard` and then `HELL_HOUR start: Hard (50 min), chapter -1, 50:00 left`.
3. **The pause menu:** a Difficulty row on the main page; it is saved, and the hourglass jumps when the budget changes mid-run.
4. **The hourglass:**
   - right side, above the cooldown bars;
   - the stream falls in play and stops in the pause menu, the skill pick and a beat's letterbox;
   - check it against the HUD lane's layout.
5. **Counting:** on a quick run with `-hellSeed`, watch `HELL_HOUR reminder` lines. A fast check: set Hard and play a
   Floor 3 chapter (two thirds spent, about 16:40 left); the 75% reminder comes after about 4 min of play.
6. **Rewind:** the first Rewind gives `hour.rewind` and `HELL_HOUR a Rewind: the hour keeps ...`; the hourglass doesn't
   refill.
7. **The gate:**
   - **Vesper chapter on Hard** (0.85 spent, 7:30 left): kill her. The escape's doorway should show the gate nearly fully
     open (35.5 s window, `the gate Closing`) and closing slowly; Easy's Vesper chapter (13:30 left) shows `WideOpen`.
   - **Out of time:** to see it, play the Vesper chapter, idle in the boss arena past the hour (or lower the budget in
     `HellHourRules` for a test). The escape opens with the leaves a third open and grinding.
   - **The fast lane** makes it in time; **the safe lane** shuts it: `HELL_HOUR the gate shut ... damnation`, Baja's line,
     a slow fade, then the damnation cards and DAMNED, BOTH OF YOU on the stats.
   - **A death** in the escape restarts the attempt: `escape attempt 2` with a shorter window.
   - Check the leaves' size and place against the doorway on the turned floor 3 (they read the room's red gate's bounds).
8. **Lucifer:**
   - the Vesper chapter answering YES, with time left: after the kill's (or the favoured spare's) cards, Baja's two Lucifer
     lines and the TO BE CONTINUED card;
   - the stats show "... THE MORNING STAR NEXT";
   - answering NO gives no Lucifer.
9. **Stats:** BAJA'S HOUR LEFT (or TIME OWED) in the top row, beside ENEMIES; check that four cells fit.
