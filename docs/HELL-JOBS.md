# Hell: the God of Death's jobs (branch `hell-jobs`, sprint lane 2)

Built 2026-10-17/18 for the night sprint (`HELL-SPRINT.md`, lane 2). The creator's brief: not "kill a marked imp" but
the errands a God of Death gives her minion because he happens to be in the area: not worth sending someone for, but
important enough for her attention. Each offers a reward; doing most of them is what makes her spare you if you spare
Vesper at the end, so the count is kept.

**Nothing here has run in Unity yet.** The pure tests, the pocket lint and the Unity-side compile check are green; the
play checks are at the end. **All dialogue is PLACEHOLDER** for the creator to rewrite (it lives in `HellJobCatalog.All`).

## The nine jobs

Three per floor, each in a different room kind. A run deals 4 of a floor's 6 rooms, so every run offers at least one job
per floor (a test checks 300 seeds) and on average 2 (6 per run).

| Floor | Room | Job | Verb | Her briefing (placeholder) | Reward |
|---|---|---|---|---|---|
| 1 Ember Gate | Six Sigils | **The Viscount of Cinders** | assassinate before he escapes | "There's a viscount on a balcony down there who keeps sending me fruit baskets. Make it stop. Permanently." | Gift: a Shadow skill |
| 1 | Break the Chains | **The Overdue Soul** | reap, through the bars | "Albrecht Huss is three hundred years overdue. He's hiding in a cage. Cute. Step in and collect him." | Quick Shadow |
| 1 | Ooze Wells | **The Lost Hourglass** | retrieve | "One of my reapers dropped my good hourglass down there. Fetch it. Don't shake it." | Short Memory |
| 2 Molten Works | Soul Lantern | **The Ledger of Ash** | destroy before it's filed | "Some clerk is stamping my dead 'CANCELLED'. Burn his book. Not him. Well. Him too, if he fusses." | Long Breath |
| 2 | Hold the Brand | **The Poacher** | kill unseen, from the dark | "A scythe demon is poaching my souls and selling them by the jar. Kill him from the dark. He spooks." | Gift: a Time skill |
| 2 | The Collapse | **The Whelp** | free | "Someone chained up my dog. My SMALL dog. Break the chains. The smith keeps re-forging them, so... deal with that." | Quick Shadow |
| 3 Black Glass | Six Sigils | **The Seconds Thief** | kill in one burst | "A demon has been skimming seconds off my hourglass. He undoes any wound that doesn't finish him. So finish him." | Short Memory |
| 3 | Ooze Wells | **The Lava Bather** | reap, in the lava | "A soul's been soaking in a lava spring for six hundred years and calls it a spa. Wade in. Reap him." | Gift: a Fire skill |
| 3 | Soul Lantern | **The Notice on the Door** | hold | "A demon is renting out my old cottage down there. Stand at his door while my notice burns in. He'll send friends." | Mended |

What each one asks, and its one new behaviour:

1. **The Viscount of Cinders.** An imp in a crown and robe on a balcony over a lava inlet. When he sees you (16 m, in
   sight) or is shot, his palanquin of chained souls carries him across the lava to his door in the far corner (7 s, about
   23 m) and two guard imps come. Kill him (90 hp) before he's through. A walkway runs along one side to his door, and its
   far end is dark: a Shadow Step from the landing cuts him off. *New: the flee path.*
2. **The Overdue Soul.** A roofed iron cage, dark inside, its bars 0.55 m apart: too close to squeeze through, but a
   Shadow Step aimed through a gap lands inside. Stand by Huss 1.5 s (progress fades 1/s away from him). Reaping him
   opens the cage's front. *New: none (the Shadow Step's own rules make the cage).*
3. **The Lost Hourglass.** On a shelf across a lava trench: two collapsing stones straight across, or the walkway round
   the side. Carrying it you're slower (0.85x) and can't Shadow Step (the lantern's rule); a sink sends it back to its
   shelf. It's done when you carry it out through the room's drop. *New: none (the lantern's carry).*
4. **The Ledger of Ash.** A clerk imp in a tall hat at his desk. When you walk into his office (or shoot) he starts
   filing: 9 s to burn the ledger (60 hp, warded: shots bite only within 9 m), and two guard imps come. Kill the clerk
   (60 hp) and the filing stops, so you can burn it at leisure. *New: the keeper's timer.*
5. **The Poacher.** A scythe demon at his rack of soul jars, back to the door; every 4 s he glances round for 1.2 s. The
   entry and a side gallery are dark. He only notices you while glancing, within 18 m, in sight, and not in darkness.
   Shots while he hasn't noticed do triple damage (150 hp: about three Writ shots unseen). Once he knows, he tears a rift
   and is gone in 3 s. *New: the glance.*
6. **The Whelp.** Her small hellhound chained to three anchors (low on one side wall, higher on the other, high on the
   back wall), 40 hp each, warded at 9 m. A smith imp re-forges one broken chain every 5 s while he lives (60 hp). Kill
   the smith first, or break all three inside 5 s. *New: the re-forging.*
7. **The Seconds Thief.** An imp with a stolen dial hopping between three dark perches over lava every 2.5 s. Any wound
   that doesn't kill him within 1.2 s of the first hit is undone (he heals to 80 hp). He never hops with a wound open.
   Burst him: Time Dilation, Ambush, the Breacher up close (the perches are Shadow Step landings). *New: the undo window.*
8. **The Lava Bather.** A soul drifting in a slow circle in a lava spring. Reap him: 2.5 s within 2.5 m (progress fades
   only 0.3/s, so dips add up). Four collapsing stones get you close; wading works too (lava burns and sinks you after
   1 s, Ashwalker makes it easy). *New: none.*
9. **The Notice on the Door.** Stand on the seal before the squatter's door until the notice burns in (6 s, it pauses
   when you step off). An imp comes the moment you step on, then every 3 s (at most 3). *New: none (a hold, like the
   Brand).*

**Failing:** the target escapes (the Viscount, the Poacher), the ledger is filed, or you leave the room with the job
unfinished. **A death** restarts the job with the room. **A Rewind can undo a failure** (the Viscount through his door,
the ledger filed) but **never a success** (a done job stays done; its reward is paid at once). A failure is written to the
ledger when you leave the room, so a Rewind still has the chance.

## Rewards, and how they fit the skill pick

- **Gifts** (one per floor: the Viscount, the Poacher, the Bather): a floor skill of the job's flavour (Shadow, Time,
  Fire), chosen at random from those you don't own and granted at once through `HellAbilities.Grant`. It comes from the
  same nine as the floor pick, so the pick never offers it again. With every skill of that flavour owned, the gift becomes
  that flavour's boon.
- **Boons** (stacking, with floors): **Mended** (plating and chassis full), **Quick Shadow** (Shadow Step recharges 1 s
  sooner, not under 3 s), **Short Memory** (Rewind cools down 3 s sooner, not under 11 s), **Long Breath** (Time Dilation
  fuel lasts 0.75 s longer).

## The ending's count

`HellJobs.Favoured` (pure: `HellJobLedger.Favoured`): you did at least 2/3 of the jobs **offered** this run (rounded up),
and at least 2 (or all, if fewer were offered). 6 offered needs 4, 3 needs 2. Lane 4 reads it for "spare with most jobs".

## Where things are

| Piece | File |
|---|---|
| The jobs, the boons, the ledger, the reward resolver | `Packages/com.vibejam.jamkit/Runtime/Skyline/Hell/HellJobs.cs` |
| The state machine (one class, one verb each; rewindable by copy) | `Runtime/Skyline/Hell/HellJobMachine.cs` |
| The pockets and the pocket lint | `Runtime/Skyline/Hell/HellJobPockets.cs` |
| The one hook | `HellRoomVariants.Build`: `HellJobPockets.Append(b, floor);` (one line, before the floor's mirror and turn) |
| Tests (22) | `Tests/Skyline/Hell/HellJobTests.cs` |
| The run's job state and events, the rewards paid | `Assets/.../Scripts/Hell/HellJobs.cs` |
| The director (self-attaching, HUD) | `Scripts/Hell/HellJobDirector.cs` |
| One job in its pocket (figures, guards, carry, cage, rewind) | `Scripts/Hell/HellJobSite.cs` |
| A shootable job part | `Scripts/Hell/HellJobTarget.cs` |

**No layout function was edited, and the editor builder needs no change:** the pocket's solids, lava, platforms and dark
zones are in the room's data, so `BuildHellRoom` builds them like any other. The figures and props are made at run time
(primitives; the imps and the demon use `HellBodies`' own builders) when the room is entered.

### The pockets

A pocket is an 18 m wide room beyond one of the room's side walls, through a 4 m doorway (4 m tall) cut into that wall
(`HellJobPockets.Site`). The wall solid is split round the doorway (a lintel above, a sill below the floor); the pocket
adds its floor, walls, lava, platforms and dark zones; the room's fire lights are kept clear of its dark zones; the room's
bounds grow to cover it. Doors (base layout; mirrored on floor 2, turned on floor 3 with the room):

| Room | Door | Next to |
|---|---|---|
| Six Sigils | west wall, z 120 | the west ring walkway |
| Soul Lantern | east wall, z 52 | the north floor by the altar |
| Break the Chains | east wall, z 22 | the east walkway |
| Hold the Brand | west wall, z 46 | the west walkway past the long bridge |
| The Collapse | east wall, z 116 | the north floor (the collapse never reaches it) |
| Ooze Wells | east wall, z 40 | the east walkway |

**The pocket lint** (`HellJobPocketLint.Check`, run on all nine in the tests, along with the room lint on every room
with a pocket): no sample of the safe or fast path is inside the pocket; the door is walkable from the safe path (ArenaNav,
1 m); the job's spot is walkable from the door, except the cage, which must NOT be walkable but must be reachable by a
Shadow Step from a walkable spot (in range, in sight through a gap in the bars, landing in darkness); none of the room's
own solids runs into the pocket; every chain can be shot from inside its ward. A negative test proves it bites (an open
cage, a path through a pocket).

### The HUD

- **The briefing card** (left, under the tracker, 7 s): "THE GOD OF DEATH", her line in quotes, then the job's name and
  what to do.
- **The tracker** (left, always): "FAVOUR: <job> ||||....  2/3 DONE" (the bar is the flee, the reaping, the filing or the
  hold), or "FAVOURS 2/3" between jobs.
- **The result card** (5 s): "DONE: <JOB>" with her reaction and the reward, or "FAILED: <JOB>" with hers and why.
- **Short flashes:** "HE'S SEEN YOU!", "HE'S FILING IT!", "HE TOOK THE SECONDS BACK", "THE SMITH RE-FORGED A CHAIN",
  "WARDED: GET CLOSER".
- **The marker:** "JOB 23 m" at the pocket's door on screen (clamped to the edge when it's off screen), hidden inside the
  pocket; plus a tall violet beacon at the door in the world.
- **Sound hooks:** `HellAbilityAudio.Play("job_done" | "job_failed" | "job_alerted" | "job_undone" | "job_reforged" |
  "job_pickup" | "job_hit")`, playing `hell/job_*` when the sound lane adds them.

## Contracts with the other lanes

- **Lane 1 (hell-flow, `HellRunStats`, `HellRunDirector`, `HellRunEvents`):** this branch edits none of their files. It
  uses `HellRunEvents.RoomEntered(floor, index, kind)`, `RoomRestarted`, `RunStarted`, `RunFinished`, and
  `HellRunDirector.Current` / `.player`. If those change shape, `HellJobDirector` follows. For the stats screen: read
  `HellJobs.DoneCount` / `OfferedCount` / `Ledger.Done`, or subscribe to `HellJobs.Done` / `Failed`; two TODO call sites
  in `Scripts/Hell/HellJobs.cs` name `HellRunStats.RecordJobDone(id)` / `RecordJobFailed(id)` for when it lands. A run
  restarted without a scene load: `HellJobs.ResetRun()` happens on `RunStarted`.
- **Lane 4 (the ending):** `HellJobs.Favoured` (and `DoneCount` / `OfferedCount` for her lines).
- **Lane 3 (Vesper's arena):** nothing needed; no job sits on the boss floor's arena.
- **Rooms lanes (rooms2, fast lines):** the pockets depend on each room's side wall at the door (`HellJobPockets.Site`)
  and on a clear 18 x 22 m outside it. A layout change that moves a wall or a walkway there shows up in the pocket lint
  test, and the fix is a new door z in `Site`, not a layout edit.
- **Sound lane:** the `hell/job_*` cue ids above.
- **Art lane:** every figure and prop is a primitive placeholder built in `HellJobSite.BuildProps`.

## Tuning

Every number is on the job in `HellJobCatalog.All` (health, escape seconds, alert radius, glance, reach, seconds, decay,
ward, timer, reforge, burst window, hop, spawn every / cap, keeper health). Pocket geometry: `HellJobPockets` (one
function per job). The favour share: `HellJobLedger.FavourShare` (2/3) and `FavourMinimum` (2). Boon sizes:
`HellJobBoons`. The card and result times: `HellJobDirector.CardSeconds` / `ResultSeconds`.

## What needs checking in Unity

1. **Rebuild the Hell scene** (JamKit > Hell > Build the Hell scene): `HELL_LINT 0 problems`. Each room with a job has a
   doorway in its side wall and a pocket beyond it.
2. **Play with a seed that deals a job room first** (the Six Sigils is room kind 0; `-hellSeed N`): `HELL_JOB offered`
   in the log, the card shows, the tracker and the marker appear, the beacon stands at the door.
3. **Per job:** the Viscount flies his path when he sees you and dies to shots (do the hit zones register on job figures:
   `SkylineHitZone` on primitives built at run time); the cage's bars stop you and a Shadow Step aimed through a gap lands
   inside (the bar gaps, 0.55 m, against the real capsule and the blink's casts); the hourglass rides in view, blocks the
   Shadow Step, and counts when you drop out of the room; the ledger deflects far shots and the clerk's filing bar fills;
   the Poacher turns round on his glances and doesn't see you in the dark; the smith's re-forge shows the chain again; the
   thief hops and heals; the Bather drifts and fades as he's reaped; the seal glows while held and imps come.
4. **The HUD layout:** the card (left, 170 px down) and tracker (130 px) against the objective line and the abilities' HUD.
5. **Rewind:** rewind the Viscount back from his door; rewind after a done job (it stays done).
6. **Guards** are ordinary bots under the site: check they don't break the room's spawn counts.

## What's left

- Nothing has been played. Figures are primitives; no art, no sounds.
- The Viscount and the thief float on scripted paths (no physics); the imp bodies are visual only (their bob, no wing flap).
- The Poacher's sight ray ignores the job's own props only by hierarchy; a rack collider could block it oddly.
- The marker is a text label, not an arrow.
