# HellDescent: the Shadow mechanics (Shadow hides you; fire kills shadow, live; darkness sounds dark)

Built 2026-10-18 on branch `hell-shadow`, from `HELL-THEME-REVIEW.md` fixes 1-3 and the creator: "Shadow should be hiding
you or otherwise offering an advantage over fast movement ... Fire killing Shadow is the same. And yes, Darkness should look
dark, and sounding dark is an amazing idea." Not yet played in Unity: the pure tests and the Unity-side compile check pass;
the play checks are at the end. How dark zones **look** belongs to the lighting lane; this lane owns the rules, the enemies'
side, the lint and the sound.

## 1. Shadow hides you

**The rule** (`PlayerStealth` + `ShadowTuning` in `Runtime/Skyline/HellShadow.cs`, ticked by `HellTargeting`):

| | Value |
|---|---|
| Where | Your chest inside a dark zone or a dark sphere (Nightfall), and no fire light on it. A scene with no dark zones has no hiding (the all-dark fallback is for the Shadow Step only). |
| Settle | 0.3 s in the dark before it hides you (a run straight through a dark patch doesn't). |
| Range | An enemy within **4 m** sees you in any darkness. |
| Moving fast | Sprint, slide, wall-run, or over **7 m/s**: an enemy within **12 m** sees you. Slow down (walk 5 m/s, stand, crouch) and you are back to 4 m at once. Stealth or speed. |
| Your shot | Reveals you for **1.5 s** (to every enemy). |
| Fire light on you | Reveals you while it lasts and **1.5 s** after. |

**What an enemy does** (`SkylineBot.HellShadow.cs`; `HellTargeting` sets `bot.targetHidden` every frame before the bots
think): it doesn't see you, so it doesn't wake, telegraph or shoot (a telegraph already started finishes into nothing; the
demon's volley is called off). Then it hunts the spot where it last saw you:
- **Imp:** its search flies straight to that spot, keeping its height, and stops **2.5 m** short of it. So a player who
  stayed exactly where they vanished is found (inside 4 m), and one who crept a few metres away is not. After its
  `rememberSeconds` (8 s) it flies home and gives its "lost" bark (the brain's give-up at `loseSightSeconds`, 3 s).
- **Scythe demon:** walks to the last-seen spot (its keep-distance kept).
- **Lava ooze:** drives to the last-seen spot; it never sweeps or slams at a hidden player.

**Readable:**
- The vanish: the moment an enemy that was seeing you loses you, a wisp of shadow puffs at your feet
  (`HellTargeting.LostInShadow` is raised for each such enemy, for any other cue).
- The eye (`HellHiddenCue`): a small eye on the HUD's safe area (`SkylineHud.Safe`, layer `hell-hidden`, left of the
  ability bars; `HellHiddenCue.Right/Bottom/Size` to restyle). Hidden: a **closed violet eye**, dimmer while you move fast.
  In the dark but exposed: an **open eye**, orange while fire is on you, white after your shot with the 1.5 s running down
  as a ring. Out of the dark: nothing.
- The enemies visibly turn and travel to where you were, then give up.

**Ambush** (the skill: first shot after a step does 3x): **the first shot from hiding also does 3x.** Your shot reveals you,
so only the first shot of a burst counts; hide again (1.5 s, out of fire) for the next one. (`PlayerStealth.Fired` notes
"from hiding" before revealing you; `HellSkillAmbush.Scale` takes it.)

**The Poacher** job now asks the same rule (`HellTargeting.PlayerHiddenFrom`): he can't see you in the dark beyond 4 m,
and your shot gives you away.

**Vesper** keeps her own rules: the stealth rule is off while `HellVesperFight.Active` (her imps see you, her phase 3
Nightfall stays hers). Nightfall's sphere and the Shade work as before everywhere.

## 2. Fire kills shadow, live

Fire passing near darkness lights it for a moment (`FireExposure`, `HellShadowFire`): inside that light the dark is not
dark, so you're **not hidden** (and revealed for 1.5 s after) and **that spot doesn't count as shadow for a Shadow Step**
(it uses the same `HellDarkness.IsDark`). Since 2026-10-18 the step needs shadow at **either end** (the landing or the spot
you step from), so a step aimed into a flash is refused (the marker turns red) unless you are standing in unlit shadow.

| Fire | Radius | For |
|---|---|---|
| An ember bolt in flight (stamped along its path, only within 2.5 m of a dark volume) | 2.5 m | 0.6 s |
| An ember bolt striking (a bolt fired at your hiding spot floods it) | 3.5 m | 1.5 s |
| A burning enemy (each ember tick, every 0.12 s) | 4 m | 0.5 s |
| Combustion's blast (any `HellFx.FireBlast`) | blast + 1 m (6 m) | 2 s |
| An ooze's burning pool (every 12 frames) | pool + 2 m | 0.5 s |
| A **lava flare** (room data, below) | the flare radius (up to 9 m) | 1.2 s, every 6-8 s |

Passing lights live in a fixed ring of 128 in `DarknessField` (`AddFlash`, the oldest overwritten): no garbage. They are
keyed on game time. `HellDarkness.Flashed(pos, radius, seconds)` is raised for every discrete flash (not for the bolt-path
stamps): **the lighting lane can listen to pulse a light in the dark zone.**

**Lava flares (lava spurts):** a room's lava light whose steady radius was clamped back by a dark zone now also flares: for
1.2 s every 6-8 s (a seeded phase) it spurts embers and flame and lights out to 9 m, into the dark beside it. A flare never
reaches a dark zone on the fast line (a Shadow Step landing stays dependable), and each room keeps at least one dark zone no
flare reaches. Today: 12 flares in 6 of the 18 rooms, Break the Chains and the Ooze Wells on all three floors (the
generator in `HellRoomLayouts.Done`).

**The lint** (`HellRoomLint.CheckDarkness`; the rule was "no fire light reaches a dark zone"):
- every room has a dark zone;
- no **steady** fire light reaches a dark zone (unchanged);
- a lava **flare** may reach one, but is up at most **2 s** and at most **25%** of its cycle;
- no flare reaches a dark zone **on the fast line** (within 2.5 m of the fast path or a hold ring's racing line);
- some dark zone in the room is reached by no flare (a hiding place fire never finds by itself);
- moving fire (bolts, burning enemies, blasts, pools) isn't room data and may light any dark.

All 18 rooms and every job pocket pass. The traversal sims ignore passing fire (no time given), and flares never touch the
fast line, so the fast-lane results are unchanged.

## 3. Darkness sounds dark

`HellShadowAudio` (on the player, unscaled time) eases a hush depth (`DarkHush`: attack 0.35 s, release 0.6 s):

| State | Depth |
|---|---|
| Hidden | 1 |
| Hidden, moving fast | 0.7 |
| In the dark but exposed (lit, or after a shot) | 0.35 |
| Out of the dark | 0 |

At full depth the **world, the enemies and the beds** (the dilated categories: Bots, World, Ambience) are dipped **-7 dB**
(`SkylineMix.HushDb`, applied in `DuckGain`) and low-passed toward **1.2 kHz** (`SkylineMix.HushCutoffHz`, through
`SkylineSounds.WorldCutoffHz`, so it stacks with Time Dilation's muffle: the lower cutoff wins). Your guns, abilities and
feedback stay clear, and the **heartbeat** loop comes up front (the binder plays the larger of Dilation's depth and
`HellShadowAudio.HeartbeatLevel`, which is 0 below depth 0.3 and 0.85 at full).

### The music's Dark modifier (for the music director)

Music isn't in the game yet. When it lands, read **`HellShadowAudio.Depth`** (0..1, smoothed; `DarkChanged(bool)` fires at
half depth) every frame and apply, on the current tune:

| Parameter | Function | Open (0) | Hidden (1) |
|---|---|---|---|
| Drums stem gain | `DarkHush.MusicDrums(depth)` | 1 | 0 (the drums drop) |
| Music bus low-pass | `DarkHush.MusicCutoff(depth)` | 22 kHz | 900 Hz (log-even) |
| A "Dark" drone layer (a held low note in the tune's key, with the bed) | `DarkHush.MusicDrone(depth)` | 0 | 0.8 |

The game has no AudioMixer asset (the mix is per-voice `AudioSource` + `AudioLowPassFilter`, `SkylineMix` categories), so
these are parameters, not a snapshot. If the music player gets a mixer, these three map onto one "Dark" snapshot's weight.
In the in-run set's terms: the Dark modifier sits on top of whichever mood/speed layer is playing; the drums stem going to 0
is the plan's "dark zones drop the drums".

## 4. Rewind and garbage

- The stealth state (reveal timer, dark time, the last shot's "from hiding") is a value snapshot in `HellTargeting`
  (`IHellRewindable`, pooled `HellSnapBox<StealthState>`). A Rewind puts it back.
- `targetHidden` and the bots' last-seen spot are recomputed every frame (nothing to rewind); a bot restored by the Rewind
  is re-marked the same frame.
- Passing lights are game-time keyed and live at most 2 s; they are not rewound (the bolts the Rewind restores light their
  paths again).
- Per frame: no allocations (the flash ring is a fixed array, `DarknessField` iterates dictionary values with struct
  enumerators, the flare list is the room's own, the state is structs).

## 5. Files

- Pure: `Packages/com.vibejam.jamkit/Runtime/Skyline/HellShadow.cs` (`ShadowTuning`, `PlayerStealth`, `FireExposure`,
  `DarkHush`); `HellAbilities.cs` (`DarknessField`: the flash ring, `InDarkVolume`, `NearDarkVolume`, `IsLit(p, now)`);
  `Hell/HellRoomTypes.cs` (`HellLight` flare fields, `FlareUp`); `Hell/HellRoomLayouts.cs` (flare generation);
  `Hell/HellRoomLint.cs` (`CheckDarkness`, `OnFastLine`).
- Unity: `Scripts/Player/HellShadow.cs` (`HellShadowFire`, `HellShadowAudio`, `HellHiddenCue`); `Scripts/Player/HellTargeting.cs`
  (the stealth tick, per-bot hiding, the vanish); `Scripts/Player/HellDarkness.cs` (`Flash`, `Flashed`, `InDarkVolume`);
  `Scripts/Bots/SkylineBot.HellShadow.cs` (the bot's side).
- Tests: `Tests/Skyline/Hell/HellShadowTests.cs` (19).

## 6. Play checks (Unity, not yet run)

1. A room with imps: stand still in a dark zone 10 m from an awake imp. Within 0.3 s it stops firing, the shadow wisp puffs,
   the eye closes; the imp flies to where you were. Stay put: it finds you at 2.5 m. Creep 3 m away first: it hovers, gives
   up ("lost" bark at 3 s), flies home at 8 s.
2. Sprint inside the same dark: the eye dims; the imp at 10 m sees you, at 15 m it doesn't.
3. Fire from hiding: the eye opens white with the ring; enemies turn on you for 1.5 s. With Ambush, that first shot is 3x
   (the crack plays).
4. Hide by a lava basin in a flare room (Break the Chains or the Ooze Wells): every 6-8 s the lava
   spurts and the eye goes orange for the flare plus 1.5 s; a Step aimed into the dark during the flare is refused.
5. Let an imp shoot at your dark alcove: the strike floods it (the eye opens orange), and a Step aimed there is refused for 1.5 s.
6. The hush: stepping into the dark muffles the lava bed and enemies within half a second, the heartbeat rises; firing
   lifts it.
7. Vesper: nothing changes in her fight (her imps still see you in phase 3).
8. Rewind out of a reveal: the eye and the hush come back as they were.

## Open (for the creator)

- A tutorial sentence would teach the hiding rule ("Stand still in the dark and they lose you"); the lines are the
  creator's (`HellLines`, `tut.shadowstep.*`), so it isn't added.
- The heartbeat reuses Time Dilation's loop; a breath loop of its own would tell the two apart.
- The flare's look is embers and two flames; a light pulse is the lighting lane's (`HellDarkness.Flashed`).
