# Hell: Vesper, the Evening Star (the boss, branch `hell-boss`)

Built 2026-10-17/18 (night sprint, lane 3). Vesper is Lucifer's living bride: immortal, bored of by him after a century,
lost without purpose since. She is you further down the same road. She fights you at the bottom of floor 3 with **your own
powers mirrored** (she Shadow Steps through darkness, she has her own Rewind) and **never quite goes for the kill**.
Nothing here has run in Unity yet: the pure tests and the Unity-side compile check pass; the play checks are at the end.

## Files

| What | Where |
|---|---|
| The fight as a pure state machine: phases, actions, her Rewind, damage rules, mercy, snapshots, lines | `Packages/com.vibejam.jamkit/Runtime/Skyline/Hell/VesperFight.cs` (`VesperFight`, `VesperTuning`, `VesperLines`, `VesperStrike`, `VesperDamage`) |
| The arena as room data (a `HellRoomDef`, its world slot, her perches) | `Runtime/Skyline/Hell/HellVesperArena.cs` |
| Tests (26) | `Tests/Skyline/Hell/VesperFightTests.cs` |
| The fight in Unity: `Begin(Action onDefeated)`, the hook, events into bolts/blinks/imps/effects, lava, Nightfall, death restart, mercy, the global Rewind | `Assets/JamKit/Templates/SkylineRepo/Scripts/Hell/HellVesperFight.cs` (Add Component > JamKit/Hell/Vesper Fight) |
| Her stand-in body, hit zones, aim-assist target | `Scripts/Hell/HellVesperBody.cs` |
| The arena built at run time from the data | `Scripts/Hell/HellVesperArenaView.cs` |
| Her health bar, hints, banner | `Scripts/Hell/HellVesperHud.cs` |
| The test bed | `Scripts/Hell/HellVesperTestBed.cs` (Add Component > **JamKit/Hell/Vesper Test Bed**) |

No file of another lane was edited (not `HellRoomLayouts.cs`, `HellWorld.cs`, the scene builder or the run director).

## The arena: the Evening Star's Hall

52 x 64 m, walls 14 m high, floor 3's obsidian look. World slot: floor 3's row, the column after the floor's shaft
(`HellVesperArena.Offset` = x 2240, z 840; a test checks it is clear of all 18 rooms and the shafts). Built at run time
by `Begin` (about 70 primitives), so it works in the Hell scene as it is and in any test scene.

- **Lava:** a 28 x 32 m pit fills the middle (floor 3's numbers: 22/s, sink 45 after 1 s; sinking puts you back at your
  last safe spot). Ashwalker and the lava hop work (`HellAbilities.LavaImmune`, `TryLavaBounce`, `LavaQuery` chained).
- **Her darkness** (7 dark zones, obsidian floors with a violet haze rim; the fire lights over the lava are clamped clear
  of them as in the rooms): her **dais** in the lava (2 m, perch 0, where she starts), **four pillars** in the lava, an
  **alcove on the east gallery**, and **the throne** on the balcony. These are her perches; you can Shadow Step into all of them.
- **Verticality:** the east gallery (5 m, ramp from its north end), the throne balcony over the exit door (4 m, ramp from
  the east), a 6 m wall-run stone on the west walkway, tall standing stones and low cover on the lava edges.
- **The room lint** (the creator's rule, adapted for a fight): the stops are the high ground (the gallery, then the
  balcony with the exit door). Safe path 156 m round the walkways and ramps; fast path 92 m, the Shadow Step line over
  the lava (entry, pillar, gallery alcove, pillar, throne). Passes in its own frame and in its world slot.
- **The exit:** a door in the north wall over the balcony, closed by a red gate until she is beaten; a short corridor
  behind it with no drop hole (the escape lane starts from there).

## The fight (three phases, 1600 health)

Her heart (an ember core in a violet cage at her chest) is the weak point: the gun's weak-point multiplier (Writ 2x,
Breacher 1.25x). Health stops at each phase's floor (one big hit can't skip a phase); between phases she shifts for 3 s
(cannot be hit, speaks).

### Phase 1, the Duel (1600 to 1100)
- She stands on a perch, her hand flares violet (**tell, 0.55 s**), then a volley of **3 shadow bolts** (0.3 s apart,
  36 m/s, 14 damage, aimed a touch ahead of you). Two volleys per perch.
- Then she **Shadow Steps**: she dissolves into smoke and **your Shadow Step's marker appears where she will land (0.9 s)**,
  then she is there. She picks a different perch, at least 10 m from you when one is.
- **Pressure:** after 160 damage on one perch she leaves early (no more volleys there). Chasing her into her darkness
  with your own Shadow Step is the counter.

### Phase 2, Undo (1100 to 600)
- Opens with a **summon** (tell 1.2 s): 2 fire imps out of the dark (at most 3 alive; again every 16 s while fewer than 2).
- Volleys of 4 bolts fanned 6 degrees.
- **Her Rewind:** when you have done at least 70 damage in the last 4 s (and it is off its 10 s cooldown), she raises her
  arm, her **heart turns cold cyan, a clock ring spins round it, the health bar turns cyan** (tell 1.6 s, rooted, she
  can be shot). At the end she takes back her health from 4 s before the tell (never above the phase's start).
- **The counter:** **80 damage on her heart during the tell breaks it** (two Writ heart shots; one with Ambush's 3x).
  Broken: she **reels for 3 s** (no attacks, **1.5x damage**), her heart cracked red, and the line. Body shots don't break it,
  and they are undone too. The first tell shows a hint ("SHOOT THE HEART").

### Phase 3, Nightfall (600 to 0)
- **The arena's lights go out:** the fire lights switch off, the ambient light and fog drop, and the whole arena counts as
  dark: **your Shadow Step works anywhere**, and so does hers. The lava keeps glowing.
- **The hunt:** she vanishes (1.5 s, cannot be hit), then **a large violet flare burns on the floor where she will step
  out**: 6 m behind you (against where you look), turned up to 45 degrees either way (1.0 s, cannot be hit; whisper hook).
  She steps out, **locks a line at you (a violet strip widening on the floor, 0.6 s)** and **cuts along it**: 8 m long,
  1.4 m either side, 32 damage. **Sidestep the line, Shadow Step away or get above her.** Time Dilation stretches the windup.
- After the cut she ends 4.5 m along the line, **exposed for 1.8 s** (heart blazing white, **1.5x damage**): turn and punish.
- Every third hunt she Shadow Steps to a perch instead and fires a 5-bolt fan.

### Her defeat
At 0 she **drops to her knees** (3 s, the line), the imps go, then the fight ends: the exit gate opens, she stays kneeling
in the arena (`HellVesperFight.VesperPosition`), `HellVesperFight.Defeated` fires and `onDefeated` is called once.

## She never quite goes for the kill
- **Pulled blows:** a hit of hers (bolt or cut) that would kill you is pulled short **once per 30 s**: you're left on 10
  health, she lowers her arm and holds back for 2.5 s ("Get up."). Imps and lava still kill; her second killing blow within
  30 s kills. (`VesperFight.TryMercy`, through the player's `WreckIntercept`, chained after any skill's own intercept.)
- **She lets you recover:** while your health is under 50 her pauses are 1.6x longer.
- Her lines lean on it (see below). Attentive players notice; nothing on screen says so.

## Death, restarts and the Rewind
- **A death restarts the fight** from phase 1 (inspector `keepPhaseOnDeath` restarts the current phase instead): bolts
  cleared, imps gone, you back at the entry with fresh armour and ammo, the "AGAIN" banner, and
  `HellRunEvents.RaiseRoomRestarted()` so the Rewind drops its history.
- **The global Rewind** restores her whole state (phase, action, timers, health, position, her own health history, her
  random stream, so she makes the same choices again) and the lava clock. Imps rewind themselves (they're bots). Her pose,
  heart, markers and Nightfall are derived from the state every frame, so a Rewind shows correctly at once (rewinding out
  of phase 3 brings the lights back).
- Her bolts are the kit's pooled ember bolts (rewound by the pool).

## Tuning (`VesperTuning`, inspector on HellVesperFight)

| What | Value |
|---|---|
| Health, phase floors | 1600, 1100, 600 |
| Heart (no gun multiplier), stagger, exposed | 2x, 1.5x, 1.5x |
| Shift, kneel | 3 s, 3 s |
| Bolts | 14 damage, 36 m/s; P1 3 a volley, P2 4 (6 deg fan), P3 5 (9 deg) |
| Blink tell, pressure blink, perch distance | 0.9 s, 160 damage, 10 m |
| Summon | tell 1.2 s, 2 imps, cap 3, every 16 s |
| Her Rewind | 4 s back, tell 1.6 s, needs 70 undone, cooldown 10 s, breaks at 80 heart damage, stagger 3 s |
| Hunt | hidden 1.5 s, flare 1.0 s, 6 m behind, +-45 deg; windup 0.6 s, reach 8 m, half width 1.4 m, 32 damage, lunge 4.5 m, exposed 1.8 s; a volley every 3rd hunt |
| Mercy | once per 30 s, leaves 10 health, holds 2.5 s; pauses x1.6 under 50 health |

**Length:** a scripted Writ shooter in the tests (`AFirstTryTakesTwoToFourMinutesAndASkilledRunAboutOne`): a first try
(a shot every 0.5 s, 35% landing, 1 in 5 on the heart, never breaking her Rewind) takes **137 s** of her side alone; a
skilled run (0.28 s, 65%, breaks every Rewind) **54 s**. Add dodging, reloads under fire and deaths: about 2-4 minutes on
a first try. To lengthen: raise `maxHealth` and the floors together.

## Sound hooks (for the sound lane)
All through `HellAbilityAudio.Play(id, position)` (plays `hell/<id>` from SkylineSounds when it exists):
`vesper/intro`, `vesper/phase1..3`, `vesper/volley_tell` (her hand flares), `vesper/bolt`, `vesper/blink_tell` (she
dissolves; at the destination), `vesper/blink`, `vesper/summon_tell`, `vesper/summon`, `vesper/rewind_tell` (a clock
winding backwards: the most important tell), `vesper/rewound`, `vesper/rewind_break` (glass/clock shattering),
`vesper/nightfall`, `vesper/hunt_tell` (a whisper at the flare), `vesper/strike_tell`, `vesper/strike`, `vesper/exposed`,
`vesper/mercy`, `vesper/shift`, `vesper/kneel`, `vesper/lava_sink`.

## Lines (ALL PLACEHOLDERS: the creator rewrites them; `VesperLines.Text`)

| Id | When | Draft |
|---|---|---|
| intro | the fight starts | So she sent you. I made a deal once too. Let me see what yours bought you. |
| phase2 | shift into Undo | Time? I have had nothing BUT time. Watch. |
| phase3 | shift into Nightfall | He grew bored of the light. So did I. Come and find me. |
| rewound | her Rewind lands | Again. I have all of it to spare. |
| rewind-broken | you break it | ...You broke it. No one has done that in a long time. |
| mercy / mercy2 | she pulls a killing blow | Get up. / Not like this. Get up. |
| defeat | she kneels | ...There. Something new, at last. |
| restart | after your death | Again, then. |

Shown as subtitles (speaker VESPER) and logged as `HELL_VESPER line <id>`.

## The API

```csharp
// start the fight (builds the arena if needed, puts the player at its entry)
HellVesperFight.RequestFinalBoss(Action onDefeated);      // finds or makes the component, then Begin
fight.Begin(Action onDefeated);                            // the component's own entry
HellVesperFight.Defeated += ...;                           // static event, once per win
HellVesperFight.Active;                                    // a fight runs (the director should leave deaths to it)
HellVesperFight.Instance.VesperPosition;                   // where she kneels (the ending starts here)
fight.End();                                               // stop without a winner
```

**The run's hook:** `HellRunEvents.FinalBossRequested` is not on main yet. At scene load `HellVesperFight` looks for it by
reflection and subscribes if it is an `Action` (onDefeated = null; listen to `Defeated`) or an `Action<Action>`
(the callback passed on). Otherwise lane 1 calls `HellVesperFight.RequestFinalBoss(onDefeated)` after floor 3's shaft.

**For the merge with lane 1 (hell-flow):** `HellRunDirector.OnWrecked` also handles deaths. While `HellVesperFight.Active`
it should return early (today, after floor 3 the run is `Finished`, so the director teleports the player to the shaft
first and the fight's own handler, subscribed later, then puts them at the arena's entry; it works but is two teleports).
Also its "THE BOTTOM OF HELL / Enter: descend again" finish screen would sit over the fight: the flow lane should start the
boss instead of finishing the run.

## See it in Unity (for the main session)
1. Open any scene with a `SkylinePlayer` (the Hell scene works; the director then also runs).
2. Add an empty GameObject with **Add Component > JamKit/Hell/Vesper Test Bed** and press Play.
3. The arena builds at x 2240, z 840 and you stand at its entry; she speaks and starts volleying. Keys: **F6** 250 damage to
   her (walk the phases), **F7** the real Rewind for free, **F8** begin again. The console logs `HELL_VESPER ...`.
4. Check:
   - **The arena:** lava glows in the pit, the dark zones read (obsidian, violet rim), ramps walkable, the gallery and the
     throne reachable, the wall-run stone runs, the exit gate closed.
   - **Her body:** the stand-in reads as a tall slim figure with a veil and a crown; the heart is the hottest thing on her,
     outlined as a weak point; heart hits give weak-point markers and double damage; the bar drops.
   - **Phase 1:** hand flare then 3 bolts; the violet marker shows where she will blink; smoke at both ends.
   - **Phase 2:** 2 imps spawn; after a burst of damage her heart turns cyan with a spinning ring and the bar turns cyan;
     two heart shots break it (she reels, cracked heart); otherwise her health jumps back.
   - **Phase 3:** the lights go out; a big violet flare behind you, she steps out, a violet strip on the floor, the cut;
     sidestepping avoids it; she is open after (blazing heart). Your Shadow Step lands anywhere now.
   - **Mercy:** let her bolts take you to almost nothing: the killing one leaves you on 10 and she holds ("Get up.").
   - **Death:** die to the lava or imps: AGAIN, back at the entry, phase 1, full health for both.
   - **The global Rewind (V):** mid-fight, she goes back with you (health, place, phase; out of Nightfall too).
   - **Defeat:** she kneels, the gate opens, `HELL_VESPER defeated` and the test bed's onDefeated line.
5. Likely tuning points after a first play: bolt speed and damage, the blink tell length, the rewind break threshold, the
   cut's width, the arena's light levels in Nightfall.

## What's left (honest list)
- Nothing has been played; no Unity-side (EditMode/PlayMode) test.
- **Her body is a model (2026-10-18)**, `Resources/HellModels/vesper` (built by `HellModelsBuild` from
  `Look/HellLook/Models/vesper.fbx`, made by `tools/hell-models/vesper.py`): Quaternius' "Woman in Dress" (CC0, rigged)
  re-dressed as the Evening Star: obsidian skin with lava cracks, long fire hair, a floor-length black-violet gown with an
  ember hem, a gold crown with a violet eight-point star on her brow, earrings, a choker with a violet pendant, a high
  standing collar, gold shoulder plates, cuffs and belt. Her Animator plays Idle (a breath loop), Cast (also Rewind and
  Strike), Reel, Kneel by the int `Pose` (= `HellVesperBody.Pose`), and the Blink clip (a lunge, arms swept back) on the
  trigger `Blink` at each blink tell. The `Heart` rides her chest bone; capsules on her chest and hips and a sphere on her
  head are the body zones; decorations have disabled boxes. Without the prefab the primitive stand-in returns.
- **Her bolts are the imps' ember bolts** (orange), not violet shadow bolts: a tint wouldn't survive the bolt pool's rewind.
- The arena is built at run time, not by the scene builder (the builder belongs to the rooms lane); it isn't in the
  scene file, so it isn't visible in the editor before Play and costs one build of ~70 primitives at `Begin`.
- The run director's death handling and finish screen (above) need the flow lane's one-line guard.
- The imps are cleared on her defeat by deactivating them (not a death burst).
- Sounds are hooks only; the lines are placeholders.
