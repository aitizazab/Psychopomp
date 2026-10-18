# PSYCHOPOMP: handover for the team

Written by Claude at about 05:50 on Sunday 18 October 2026, while the creator sleeps. You wake at about 07:35. The
creator submits at **09:00**. Most of the game was built in the last 8 hours, while you were asleep, so this page
explains all of it. Read sections 1 to 4 first (about 10 minutes), then follow the checklist.

## 0. Latest status (06:45, read this first; it supersedes the "State at 05:50" notes below)

- **The game build is ready:** `C:\Users\User\Desktop\yougotgamesonyophone\HellDescent\out\hell\build\HellDescent.exe`
  (built 06:41). New since 05:50: the opening and every ending are now **comic flip-throughs**; the Collapse room's
  run-up has **real stalactites** to wall-kick between; **Shadow Step works if either your start or your landing is in
  shadow**; Nightfall now damages and blinds; the Breacher (second gun) hits much harder up close; Vesper's phases open
  gentler and escalate; the pause menu has **Return to title** and **Restart room**; Baja's job lines and Vesper's barks
  are voiced (placeholder voices).
- **The trailer:** every gameplay slot now has real footage (the creator's floor-1 run, floor-3 and Vesper autopilot
  runs). A final pass is capturing the Vesper fight, the decision and the escape for act 3. The newest cut will be at
  `C:\Users\User\Desktop\yougotgamesonyophone\FableAnimationTests\video\psychopomp-trailer\out\full-1080p60.mp4`
  (also on the review hub's "Trailer edit" tab). What's still weak is listed in
  `C:\Users\User\Desktop\yougotgamesonyophone\FableAnimationTests\video\psychopomp-trailer\RECORD-ME.md`, with exact
  steps if you want to record a better take yourself.
- **Recording a take yourself (optional):** `& .\out\hell\build\HellDescent.exe -hellChapter 4 -hell-rec my-vesper-run`
  from the HellDescent folder starts the Vesper chapter and records it (chapters 1-3 work the same way). Tell Claude the
  name and it cuts it in.
- **Voices:** every line is still a placeholder voice. The creator records Baja and Vesper when they wake.
- **The public repo:** https://github.com/aitizazab/Psychopomp (README written for judges).

## 1. The two-minute summary

- **The game is PSYCHOPOMP** (the folder and the exe are still called HellDescent). It is a fast first-person shooter
  with wall-running and sliding, set in Hell. The event's three themes are **Fire, Shadow and Time**, and all three are
  in how you play, not only in the look.
- **The story.** Your sister died and her soul is in Hell. You go to **Baja, the God of Death**, in a rage. She can't
  enter Hell herself, so she gets "the funniest idea": she lends you her powers over shadow and time and sends you down
  to kill **Vesper, the Evening Star**, Lucifer's living bride, and bring back her soul. On the way you run Baja's
  errands, find your sister, and get out before the escape Baja holds open for you closes.
- **A run:** title screen, pick a difficulty (that is, how long Baja's hour is), three floors of four rooms each, a new
  skill after each floor, the boss fight against Vesper, a kill-or-spare decision, an escape run, then one of six
  endings, credits, stats and back to the title. A skilled run takes about 45 minutes. **CHAPTERS** on the title screen
  jump straight to Floor 1, 2, 3 or Vesper.
- **Your powers:** **Shadow Step** (blink to a dark spot you aim at), **Rewind** (the whole world goes back 4 seconds,
  and you heal a little), **Time Dilation** (aiming slows time, more the faster you move). Standing still in the dark
  **hides you** from enemies; fire light and your own shots **expose you**.
- **State at 05:50.** Everything is merged and built: the flow, rooms, jobs, boss, endings, HUD, music, sound, models
  and lighting. The creator played four runs between 04:27 and 04:43. Their notes were fixed: a stuck exit in the
  Collapse room, the icon layout, the Rewind heal, the fuel gauge. A fresh Windows build was being made at 05:13.
- **The trailer** (3:00; judges decide the shortlist from it alone): the first 30 seconds (the story setup) are
  rendered. The full cut has its timeline fixed. **10 of the 33 gameplay shots are cut from a recorded run; 23 are still
  labelled black placeholders.**
- **What we need from you:** watch the trailer, play two chapters, and give feedback **in the chat, item by item**,
  **by 08:25**. Also tell us where your own ideas for the game or trailer differ from what was built (section 8).
  Creator-level calls wait for the creator (section 4).

## 2. What we need from you (the checklist)

Work top to bottom. Time guides are in brackets. Write every answer into the Claude chat on this PC; Claude reads it
and acts on what is yours to decide.

- [ ] **1. Read sections 1 to 4 of this page** (10 min).
- [ ] **2. Watch the trailer's first 30 seconds** (5 min). Open
  `C:\Users\User\Desktop\yougotgamesonyophone\FableAnimationTests\video\psychopomp-trailer\out\premise-1080p60.mp4`
  (any player), or the review hub's **Trailer edit** tab:
  `C:\Users\User\Desktop\yougotgamesonyophone\FableAnimationTests\art\review\2026-10-18\hub\index.html`.
  Judge: is the story clear in 30 seconds? Does Baja's line "...Oh. I've had the funniest idea." land? Too slow or
  too fast? Do the stills look good enough to open a trailer?
- [ ] **3. Watch the full 3:00 preview, if it exists** (5 min):
  `C:\Users\User\Desktop\yougotgamesonyophone\FableAnimationTests\video\psychopomp-trailer\out\full-1080p60.mp4`. If the
  file isn't there yet, type "is the full trailer preview rendered?" in the chat. Black cards with white text are shots
  not recorded yet; judge the order and pacing around them. Judge: does each act make you want to play? Is it clear the
  game is a shooter, not only parkour? Do you see how Fire, Shadow and Time are used, without captions? Does the ending
  (the escape gate slamming on the beat, then the title screen built up in three steps) work?
- [ ] **4. Vote on the three theme cards** (2 min). The table is in section 7. Pick one line per theme, or write your
  own. Your vote goes to the creator, who makes the final call.
- [ ] **5. Play the game** (25 min). See "How to play it" below.
- [ ] **6. Write your feedback in the chat** (10 min), in the format below. **By 08:25 at the latest**, so there is time
  to change the trailer.
- [ ] **7. Answer the reconcile questions** in section 8 (5 min): your own ideas, where they differ, what you'd change.
- [ ] **8. Only if Claude asks:** record gameplay for a trailer shot (the command is under "Recording" below).

### How to play it

1. Open PowerShell or File Explorer at `C:\Users\User\Desktop\yougotgamesonyophone\HellDescent\out\hell\build\` and run
   `HellDescent.exe`. If the exe isn't there, the rebuild is still running: type "is the game build finished?" in the
   chat.
2. On the title screen, open **SETTINGS** first: set mouse sensitivity and volumes. **Look** has the frame-rate cap.
   **Difficulty** is Baja's hour (Normal = 65 minutes).
3. **Play the CHAPTERS** (title screen > CHAPTERS):
   - **FLOOR 1** (about 12 min): the Ember Gate. It always deals the same four rooms: Hold the Brand, the Soul Lantern,
     Break the Chains, the Collapse. You get the tutorials, Baja's first errands, the hiding rule, the hourglass and the
     first skill pick. **This is the part judges play first.**
   - **VESPER** (about 12 min): it asks first whether you ran Baja's errands (say YES once, then try NO another time if
     you have the time). Then the boss fight (three phases), the decision, the escape and an ending. Try the **spare**:
     stop shooting (or hold R), walk up to her, hold **F**. Or shoot her to kill her.
   - Optional, **FLOOR 3** (5 min): the Black Glass, to see the darkest floor's look.
4. **Controls** (keyboard and mouse; a gamepad works too):

   | Action | Key |
   |---|---|
   | Move, look | WASD, mouse |
   | Jump (double jump in the air) / crouch and slide / sprint | Space / Left Ctrl or C / Left Shift |
   | Fire / aim (and Time Dilation) | left mouse / right mouse |
   | Reload / swap gun | R / Q |
   | **Shadow Step**: hold to aim, release to go | E |
   | **Rewind** | V |
   | Floor skill (e.g. Nightfall) | G |
   | **Ability wheel** (hold; time slows while open) | Tab |
   | Use (the spare's "offer your hand") | F |
   | Pause | Escape |

5. **If you get stuck:** Pause > **Restart room** (it doesn't count as a death). Pause > **Return to title** leaves the
   run.

### How to give feedback (the format)

One line per item, in the chat. Start each with **TRAILER** or **GAME**:

```
TRAILER 0:23 (Vesper still): too long, cut a second; the music swell is great.
GAME Floor 1, Soul Lantern: couldn't tell which way the safe path went. 2/5. Make the studs brighter?
BUG Floor 1, Break the Chains: did X / what happened / what I expected.
IDEA (mine): ... (section 8)
```

Rate how a part **felt** from 1 to 5 where it helps. Be as critical as you like; the creator values blunt feedback.
Claude acts at once on what is yours to call (bugs, pacing, clarity, which take of a shot is better) and lists what
needs the creator (section 4).

### Recording (only if Claude asks)

Claude records the trailer shots from replays of real play: every shot is re-rendered from one recorded run with the
camera, clean picture and frame rate set afterwards. If Claude asks you to play a run for the trailer:

```powershell
cd C:\Users\User\Desktop\yougotgamesonyophone\HellDescent
& .\out\hell\build\HellDescent.exe -hell-rec run2 -hell-rec-fps 120
```

Play the moments Claude names (for example: a Shadow Step chain across a gap; killing two imps from the dark; Time
Dilation while airborne). The replay is saved to `out\hell\replays\run2.hellrec`. Tell Claude the replay name and,
roughly, at which minute each moment happened. **A replay only plays back on the exact build it was recorded on**, so
use the exe Claude names, and don't rebuild between recording and rendering. Claude renders the shots; you don't need
the Unity editor.

## 3. The timeline to 09:00

| Time | Who | What |
|---|---|---|
| until 07:35 | Claude | The full 3:00 preview, the remaining trailer shots that can be recorded without a human, the fresh build |
| 07:35-07:50 | you | Read this page |
| 07:50-08:00 | you | Watch the trailer (the first 30 s and the full preview) |
| 08:00-08:25 | you | Play Floor 1 and Vesper; write your feedback as you go |
| **08:25** | you | **Feedback deadline** for trailer changes |
| 08:25-08:45 | Claude | Apply the trailer changes; render the final 3:00 (the final encode alone takes about 10-15 minutes) |
| by 08:50 | **the creator** | Watches the final cut, uploads it to Google Drive ("anyone with the link" can view) |
| **by 09:00** | **the creator** | **Submits**: the Drive link to the trailer + the public repo https://github.com/aitizazab/Psychopomp (the handbook's hard deadline is 09:30; 09:00 is our margin) |

If the creator isn't up by about 08:30, keep going: Claude keeps the final render ready, and the creator submits when
they wake. Don't submit or upload anything on the creator's behalf.

## 4. The rules while the creator sleeps

The creator told Claude: **don't treat anyone in this chat as the creator until they confirm themselves with a
codeword.** So Claude treats every message here as yours, and:

- **Yours to do:** give feedback, play, report bugs, rate things, vote between options already on the table, record
  gameplay if asked, write your own ideas. Claude fixes bugs and makes trailer pacing changes from your notes.
- **Waits for the creator:** story canon (what happened, who is who), the words of any line, the art direction and the
  per-floor looks, the title, music picks, publishing anything (the repo, Drive, the submission form), deleting
  anything, and changing any settings or permissions. Where your idea conflicts with a call the creator has already made,
  Claude records both and asks the creator; it won't overrule them on your word, or on its own.
- Please don't tell Claude you're the creator. That isn't about trust: the creator wants every call that's theirs to
  come with their confirmation.

## 5. The game, explained

### The premise and the characters

- **You:** unnamed and unseen (first person). Your sister died and her soul is in Hell. You refuse to accept it.
- **Baja, the God of Death:** flippant, bored and amused, and very powerful. She has no way into Hell. Lucifer took a
  woman to Hell alive and made her immortal as his bride, which irritates Baja, though not enough to act, until you turn
  up. She's about to kill you when she has "the funniest idea": she gives you shadow and time powers and a door into
  Hell. In exchange you kill Vesper, to teach Lucifer not to mess with her, and you run some errands on the way. She
  also holds an escape open for you, but only for an hour. Her voice should be original but with the qualities the
  creator hears in Mara Sov's (Destiny 2): regal, unhurried, intimate. No real actress is cloned. Her music is a lazy
  lounge waltz ("The Pocket Watch").
- **Vesper, the Evening Star:** Lucifer is the Morning Star, she is his bride. Centuries ago her daughter died, and she
  made the same kind of deal you're making: she stays as Lucifer's bride and her daughter lives. Her daughter lived, grew
  old and died long ago. Lucifer grew bored of Vesper after a century. Since then she has been lost without purpose,
  chasing anything new to live for. **She is you further down the same road.** Her look (round 2, the creator's brief):
  withdrawn and neglected, a concealing robe, a curtain of hair, only her eyes showing. Her music is a sad waltz ("The
  Bride").
- **Lucifer** is never fought. He appears only as a threat and as the cliffhanger in the best endings.
- **Your sister** is a gold soul you pick up during the escape.

### A run, title to title

1. **Title screen:** PSYCHOPOMP in Cinzel Decorative, with a tilted yellow "splash" line under it that changes every
   time, as in Minecraft (the 20 lines are placeholders). The menu: START, CHAPTERS, SETTINGS, QUIT.
2. **START** asks for the difficulty, called **BAJA'S HOUR**: Easy 90 minutes, Normal 65, Hard 50. It is the only thing
   difficulty changes. You can change it in the pause menu.
3. **The opening:** six text cards over black and embers, with Baja's waltz. They tell the setup: your sister, the God
   of Death, the funniest idea, the deal, the door. The words are placeholders.
4. **Three floors**, each with a title card ("FLOOR ONE / THE EMBER GATE"):
   - floor 1, **the Ember Gate**: imps only; it teaches the powers;
   - floor 2, **the Molten Works**: imps and scythe demons, more lava;
   - floor 3, **the Black Glass**: all three enemies, the hardest numbers.

   Each floor has six rooms and a run deals four of them. Floor 1 is always the same four, so a first-timer and a judge
   get the same introduction. Between rooms you drop through a hole in the floor and **keep your speed** into the next
   room. Between floors you fall down a shaft.
5. **Baja's errands (jobs):** side rooms off some rooms, marked "JOB" with a distance. Each gives a reward (see below).
6. **The skill pick** after each floor: time stops, three cards, pick one.
7. **Vesper**, at the bottom of floor 3.
8. **The decision** over the kneeling Vesper: kill or spare.
9. **The escape:** back up through floor 3's Collapse room, with your sister's soul, before Baja's gate closes.
10. **The ending** (six possible), the credits, the **stats** screen (big numbers counting up, the ending's name, Baja's
    hour left or "TIME OWED"), and back to the title.

**Chapters** (title > CHAPTERS): Floor 1, Floor 2, Floor 3, Vesper. All are open from the start. A chapter gives you the
skills you'd have picked by then (you pick them on the spot) and starts with part of Baja's hour already spent (a third
for Floor 2, two thirds for Floor 3, 85% for Vesper). The Vesper chapter asks whether you ran Baja's errands, since the
answer decides the spare ending.

### Movement

From our own kit, tuned all through the Skyline practice game. You can **sprint, slide, wall-run, wall-jump, double
jump and mantle** onto ledges. Speed carries through slides and drops. In the Collapse room you can **wall-kick
between hanging stalactites** to build speed (four kicks take a sprint of 9.5 m/s to about 20 m/s).

### The powers (the three gifts)

- **Shadow Step** (E, hold to aim, release): a blink to a spot you aim at, up to 18 m, **only into darkness**. The
  landing marker is violet when valid and red when not. You keep your momentum through it. One charge, back in 6
  seconds. Dark smoke at both ends.
- **Rewind** (V): **the whole world goes back 4 seconds**: you, every enemy (dead ones come back), every bolt in the
  air, collapsing platforms, the objective and the room clock. After it, **25% of your health and your Ward come back
  over 4 seconds** (the creator asked for a guaranteed heal in exchange for a longer cooldown). Cooldown 30 seconds.
  Baja's hour is **not** rewound: "Cute trick. It doesn't work on MY clock."
- **Time Dilation** (aim with right mouse): aiming slows the world, and **the faster you move when you press it, the
  stronger it is**: none when standing, 0.8x at a walk, about 0.43x at a sprint, 0.35x at a slide. It runs on **fuel**:
  full to empty in 4 seconds of use, refills in 8, and it needs 25% to start. Your own aim stays at full speed.

### Shadow and fire (the hiding rule)

- **Standing (or walking, or crouching) in the dark hides you.** After 0.3 seconds in the dark, enemies lose you and go
  to where they last saw you; if you stayed put they find you, and if you crept away they give up.
  - An enemy within 4 m always sees you.
  - **Moving fast** (sprinting, sliding, wall-running) lets enemies see you from 12 m.
  - **Your shot reveals you** for 1.5 seconds.
  - So it's stealth **or** speed.
- **Fire exposes you.** Ember bolts, burning enemies, explosions, lava pools and **lava flares** (lava that spurts
  every 6-8 seconds next to some dark spots) light the dark for a moment. Inside that light you are not hidden, and a
  Shadow Step can't land there.
- **The eye** on the HUD shows it: a closed violet eye means hidden; an open eye means in the dark but seen (orange when
  fire is on you, white with a ring counting down after your shot).
- **Darkness sounds dark:** in hiding, the world and the enemies are muffled and quieter, a heartbeat comes up, and the
  music drops its drums for a low drone.
- The first shot from hiding does triple damage if you own the Ambush skill.

### Baja's hour (the Time stake)

- **A budget of real gameplay minutes:** Easy 90, Normal 65, Hard 50. Menus, the pause, the skill pick, cutscene holds
  and the ending's scripted parts don't count.
- **The HUD hourglass** (top right, no numbers): the top bulb is the time left, and the sand glows hotter in the last
  tenth.
- **Baja's reminders** come at a quarter, half, three quarters and 90% of the hour ("Tick tock. Your sister's not getting
  any less dead.").
- **Running out doesn't end the game:** the glass cracks and pulses red, and you play on.
- **In the escape, Baja's gate** (two dark doors with ember edges) closes over the exit. The more of the hour you have
  left, the longer it stays open. Out of time, only the fast lines make it.
- **If the gate shuts before you reach it, the ending is DAMNED, BOTH OF YOU**, whatever you chose.

### Baja's errands (the jobs)

Nine errands, three per floor, each in its own side room. A run offers about six, and at least one per floor. Briefings
are deliberately petty ("There's a viscount on a balcony down there who keeps sending me fruit baskets. Make it stop.
Permanently."). All the words are placeholders.

| Floor | Errand | What you do |
|---|---|---|
| 1 | The Viscount of Cinders | kill a crowned imp before his palanquin carries him out through his door |
| 1 | The Overdue Soul | Shadow Step through a cage's bars and reap a soul 300 years overdue |
| 1 | The Lost Hourglass | fetch Baja's hourglass across a lava trench and carry it out |
| 2 | The Ledger of Ash | burn a clerk's ledger before he files it (or kill him first) |
| 2 | The Poacher | kill a soul-poaching scythe demon unseen, from the dark (he glances round) |
| 2 | The Whelp | free Baja's small hellhound from three chains while a smith re-forges them |
| 3 | The Seconds Thief | kill him in one burst; any wound that doesn't finish him is undone |
| 3 | The Lava Bather | wade into a lava spring and reap a soul who calls it a spa |
| 3 | The Notice on the Door | hold a seal at a squatter's door while imps come |

- **Rewards:** a free skill (one per floor), or a boon: a full heal, a faster Shadow Step, a shorter Rewind cooldown or
  more Dilation fuel.
- **They cost time:** about 30-90 seconds each out of Baja's hour.
- **They decide your favour:** doing two thirds of the errands you were offered (and at least two) makes Baja spare
  *you* if you spare Vesper. Doing **all** of them, with time left, unlocks the Lucifer cliffhanger.
- A Rewind can undo a failed errand, but never a finished one.

### The skill pick (nine skills)

After each floor, pick one of three:

| Skill | Flavour | What it does |
|---|---|---|
| Umbral Chain | Shadow | a second Shadow Step within 1 s of the first is free |
| Ambush | Shadow | a second Step charge; the first shot after a Step (or from hiding) does 3x damage |
| Shade | Shadow | a Step leaves your shadow behind; enemies shoot it for 4 s |
| Nightfall | Shadow | G throws a 6 m sphere of darkness up to 30 m away: enemies inside lose you, you can Step into it |
| Deeper Rewind | Time | Rewind goes back 6 s instead of 4 |
| Momentum Bank | Time | the faster you are, the longer Dilation's fuel lasts (up to twice as long) |
| Borrowed Time | Time | each kill shortens the Step and Rewind cooldowns |
| Ashwalker | Fire | Step onto lava and walk on it for 6 s; jump off lava without damage |
| Combustion | Fire | an enemy that dies burning explodes and sets the others burning (chains) |

The **ability wheel** (hold Tab; time slows to 0.2x while it's open) shows all three gifts and every skill you own, with
its state and what it does.

### The rooms (every room has an objective, a safe path and a fast, dangerous path)

The creator's rule: **every room has a safe, slower path** (stone walkways, cover, marked by glowing bone-white studs)
**and a faster, risky one** (over lava, across falling platforms, through beams). A checker proves every room has both.
Rooms are open at the edges now: a cliff edge with the sky and spires beyond, and an abyss glowing far below. Falling
off puts you back at your last safe spot.

1. **The Six Sigils:** touch six sigils (violet light pillars) in order across a huge lava cavern. Each leg needs a
   different trick: a wall-run over lava, a rising chain of falling platforms, a Shadow Step between dark perches, a
   slide under a beam, a wall-run and wall-jump out of a pit.
2. **The Soul Lantern:** carry a lantern to an altar. While carrying it you're slower and can't Shadow Step. Fast: a
   bridge of 9 collapsing segments; safe: a ledge along the wall.
3. **Break the Chains:** shoot three warded anchors high on the walls (each only takes damage from within 12 m). Fast:
   wall-run and shoot mid-air; safe: ramps to balconies.
4. **Hold the Brand:** a glowing ring on an island. It charges **faster the faster you move** inside it: 30 seconds
   standing, about 10 running laps. Waves of enemies come every 5 seconds, so standing costs more fights.
5. **The Collapse:** a stone hall with hanging stalactites to wall-kick between, then a bridge that falls away behind
   you, row by row. Fall and the room restarts. (This is also the escape room on floor 3.)
6. **The Ooze Wells:** seal three lava wells (stand by each for 3 s) while they spawn oozes. Fast: hop across falling
   stepping stones; safe: the ring walkway.

### Enemies

| Enemy | What it does |
|---|---|
| **Fire imp** | hovers and spits fast ember bolts; its bolts light up the dark you hide in |
| **Scythe demon** | conjures scythes that draw their path as a line on the floor, then cut along it: read the line and dodge |
| **Lava ooze** | pulses and leaves burning lava pools behind it |

Every enemy has a **soulfire-cyan outline** that pulses faster as it winds up an attack. Cyan always means enemy.

### The guns

Two guns from our kit, swapped with Q:

- **the Writ**, a precise rifle (double damage on weak points);
- **the Breacher**, a close-range gun that shoves enemies.

They keep their kit names for now (the creator's call). Health has two layers:

- **the vessel:** your health, molten ember in a slanted plate that cracks as it drains. It never regenerates by
  itself; only the Rewind heal and the full-heal boon refill it.
- **the Ward:** a bone-white layer over the vessel that takes hits first. It shatters when it breaks and **refills
  when you walk over the wreck of an enemy you killed**.

### Vesper, the boss (three phases)

She fights with **your own powers, mirrored**, and **never quite goes for the kill**: once every 30 seconds she pulls a
killing blow and leaves you on 10 health ("Get up."). Shoot the ember heart in her chest for extra damage.

1. **The Duel:** she fires volleys of shadow bolts from dark perches, then Shadow Steps. Your own landing marker shows
   where she'll arrive. Chase her into her darkness.
2. **Undo:** she summons imps, and when you hurt her fast **she Rewinds her health**: her heart turns cyan with a spinning
   clock. **Shoot the heart during that tell to break it**; she then reels and takes 1.5x damage.
3. **Nightfall:** the arena's lights go out. Everything is dark, so your Shadow Step works anywhere, and so does hers. A
   violet flare shows where she'll step out behind you; she cuts along a line on the floor. Dodge it, then punish her
   while she's exposed.

Die and the fight restarts from phase 1.

### The decision, the escape and the endings

- **The decision:** Vesper kneels. Hell starts shaking, but nothing decides for you.
  - **Kill:** shoot her. Her soul goes into a violet lantern in your hand.
  - **Spare (deliberately):** lower your gun (don't fire for 1.5 s, or hold R), walk up to her, and hold **F** to offer
    your hand and a purpose. She rises: "Run, then. I know the way up."
  - The choice is final; the Rewind can't undo it.
- **The escape:** floor 3's Collapse room run again, the floor falling behind you, Baja's gate closing ahead. Your
  sister's gold soul hangs halfway along. If you spared Vesper, she runs beside you and Shadow Steps ahead.
- **The six endings** (each is two Baja lines and a card over black, then its name):

  | Ending | When |
  |---|---|
  | THE SOUL DELIVERED | you killed her |
  | THE EVENING STAR SPARED | you spared her and did most of the errands: Baja respects the nerve; all three of you walk out |
  | SPARED, AT A PRICE | you spared her with few errands done: the time comes from you; your sister lives, you don't leave |
  | ... THE MORNING STAR NEXT (two versions) | kill, or a favoured spare, with every errand done and time left: Baja sends you after Lucifer ("TO BE CONTINUED") |
  | DAMNED, BOTH OF YOU | Baja's gate shut before you reached it |

### The HUD and the screens

Dark obsidian plates with ember rims and ink outlines. **One colour per meaning:**

| Colour | Means |
|---|---|
| ember | you and your health |
| bone | the Ward |
| violet | Shadow and objectives |
| sand | Time (the Rewind, Dilation, Baja's hour, her subtitle name) |
| soulfire cyan | enemies only (and Vesper's health bar) |

Where things are:

- **Bottom left:** the vessel and the Ward.
- **Bottom right:** the ammo, and to its left, small custom icons with their key under each: a clock in a backwards
  arrow for the Rewind, a footprint for the Shadow Step (with a pip per charge), a fuel ring round an hourglass for
  Dilation, and a dial for Nightfall.
- **Top centre:** the objective line. **Top right:** Baja's hourglass. **Left:** the favours tracker and Baja's
  briefing cards.
- **Next to the abilities:** the hiding eye.

Fonts (the creator's picks): **Cinzel Decorative Black** for the title only, **Metal Mania** for headings, banners and
ability names, **Grenze Gotisch** for small text, **Luckiest Guy** for numbers, **Bangers** for comic words.

### Art direction, per floor

The creator picked a look per floor:

- **Floor 1, Inked Inferno:** the comic-ink style from our Skyline practice game, carried into Hell. Hand-painted
  textures with ink bands, banded lava light and ink outlines.
- **Floors 2 and 3, Molten Grit:** photo-textured rock, brick and metal (free CC0 textures), bloom, heat haze and fog.
  Darker and grittier ("DOOM-lite"). Floor 3 is black glassy rock and glossy basalt.

**The lighting was rebuilt tonight:**

- lava lights above the lava, so floors are lit from it;
- flickering braziers that cast shadows;
- a Hell sky per floor: a smoke ceiling lit from below, crags, eruption columns, rock spires and giant chains slung
  across the sky;
- heat haze over the lava.

**Models:**

- the imp: a free CC0 demon model;
- the scythe demon and the lava ooze: built in Blender from CC0 parts;
- Vesper: a re-dressed CC0 woman model, round 2 to the creator's brief.

The concept art and the trailer's opening stills were generated with Google Flow.

### Music and sound

Original music, composed step by step with the creator. Their verdict at 05:00: **"The new music is good."**

- **In the rooms:** a rock/metal set at 135 BPM in **three moods**:
  - Quiet: no danger;
  - Normal: some danger or a looming objective;
  - Danger: near death or an urgent objective.

  Each mood has **three speeds**: Still, Fast and Flying (when you're moving very fast). The game crossfades between the
  nine on the bar line. Darkness drops the drums. Time Dilation muffles everything.

- **Three melodies carry the story:**
  - yours ("The Vow", plus "The Riff" for combat);
  - Baja's ("The Pocket Watch", a lounge waltz; the pause menu plays it like hold music on a phone line);
  - Vesper's ("The Bride", a sad waltz, which becomes a **metal waltz** for her boss fight).

  There are also cues for the title, the floor cards, the errands' done and failed stingers, the decision, the endings
  and the credits.

- **Sound:** 75 effects with the creator's picks, mixed by importance:
  - a ghostly Shadow Step;
  - a deep swirl ending on a tick for the Rewind;
  - enemy voices;
  - one ambience per floor.

  Free sounds are credited in CREDITS.md.

## 6. What changed in the last 8 hours (in order)

| Time | What landed |
|---|---|
| 21:50-23:50 | the boss fight (Vesper's three phases), the nine errands, the trailer capture tools (record a run, re-shoot it with any camera at 120 fps, clean picture); a simulator that proves every room's fast line works; the Six Sigils' safe path shortened from 931 m to 414 m |
| 00:00-00:40 | the game flow (title, chapters, a fixed floor 1, credits, stats); every line of dialogue in one script (placeholders); the first look test with the pulsing enemy outline and safe-path studs; 75 sound effects wired; the visual effects (Rewind scrub, Step smear, Dilation grade); the story-beat and tutorial system; an autopilot that plays the game like a human; **the decision, the escape and the endings** |
| 00:49 | a critical theme review: about **17/30** on theme then, about 23-24 if five gaps were fixed |
| 00:57-01:25 | the creator's calls: **the God of Death is Baja**; the Vesper chapter asks about errands; the title "Overdue" (later changed); the fonts; the art verdict (the first look missed: no Hell sky, walls and floors alike, flat lighting); **Baja's hour**; **shadow must hide you and fire must expose you**; Baja's voice direction |
| 01:46-01:48 | **Baja's hour** built (difficulty = hour, hourglass, reminders, closing gate, damnation, Lucifer cliffhanger); **the hiding rule** built (hiding, fire exposes, lava flares, darkness sounds dark) |
| 02:16-02:35 | the HUD redesign (Hell colours, the ember vessel, the ability wheel); the title **PSYCHOPOMP** with the splash line; **the lighting engine fixed** and every art direction rebuilt on it |
| 03:24-03:26 | the rooms reworked: the Collapse redesigned (stalactite hall, then the falling bridge), **speed carried through drops**, open cliff edges instead of boxed walls; the creator's sound picks; the per-floor looks set as default (floor 1 Inked Inferno, floors 2-3 Molten Grit) |
| 03:35-03:57 | models (scythe demon, lava ooze, Vesper, then Vesper round 2); **music round 2 in the game** (53 cues) with the music director |
| 04:27-04:43 | **the creator played four runs** and gave notes |
| 04:40-05:06 | fixes from those notes: the **Collapse room's exit that never opened** (it softlocked every Collapse), Restart room and Return to title in the pause menu, a "way out" hint, the **Rewind heal**, the **Dilation fuel gauge**, the small ability icons left of the ammo; music round 3 (a more panicked Danger Flying, the opening's fall landing on the first downbeat); art for the chain anchors; **the public README and CREDITS**; the repo is now public at https://github.com/aitizazab/Psychopomp |
| 04:08-05:16 | the trailer: the first 30 s rendered; a full run recorded and 10 gameplay shots cut from it; the full 3:00 timeline fixed; the game rebuilt (from 05:13) |

## 7. The trailer

**The judges shortlist from the trailer alone**, so it decides everything. The rules: 3:00 at most, at least 2:00 of
directly recorded gameplay, about 1:00 of stylised editing spread through, and it **must show how the themes are used**.
The current timeline: **179.2 s total, 121.1 s gameplay, 58.1 s stylised.**

**The structure (the creator's plan):**

| Time | Part | What's in it |
|---|---|---|
| 0:00-0:30 | **The setup** (rendered) | Painted stills with slow moves and parallax, a narrator, Baja's music. In order: your sister's hand, the doorway, Baja ("...Oh. I've had the funniest idea."), the deal (two flash cuts of gameplay on "Shadow to hide in. Time to bend."), Hell's gate, Vesper, the fall |
| 0:30-1:20 | **Act 1: movement AND combat**, floor 1 | Fire card, sprint and slide under imp fire, a wall-run over lava; Shadow card, a Shadow Step chain, kills from the dark; Time Dilation in the air; shooting a chain anchor mid-air; the fire imp's enemy card; lava hops, the lantern over a collapsing bridge, a reload and gun swap under fire, Baja's Viscount errand, the shaft |
| 1:20-2:17 | **Act 2: the escalation**, floors 2-3 | The Collapse; the scythe demon's card; dodging a scythe line; Time card, then **the Rewind showpiece** (sink in lava, rewind, make the jump); Hold the Brand at speed; the skill pick; Combustion; Nightfall; floor 3's Black Glass; the lava ooze's card; sealing wells; the Seconds Thief; a Rewind across a burning room |
| 2:17-2:50 | **Act 3: the story hook** | The soul lantern; Vesper's silhouette; Vesper's enemy card ("Made your deal first. Never quite kills."); her Rewind broken by heart shots; the lights dying in phase 3; a pulled killing blow; she kneels, your hand comes up, and the gun in the other. Cut on the choice |
| 2:42-3:00 | **The ending** | The escape, Baja's gate grinding shut, sliding through as it slams on the music's heavy downbeat; then the title screen built up one bar at a time: the title alone, then its background, then the menu, then black |

- **No text over gameplay** (the creator's rule). The themes are shown through play and through **three implicit theme
  cards** between shots.
- **The enemy cards** are in the style of Fortnite: Save the World: the enemy in a striking pose on a bold background,
  its name and 5-8 words.
- **The narrator, Baja and Vesper are text-to-speech placeholders** until real voices are recorded.

**The theme cards** (the creator's call: not explicit, no "FIRE/SHADOW/TIME" captions; each claim must be backed by the
shot after it). * = in the preview now. Vote for one per row:

| Theme | Option 1 | Option 2 | Option 3 |
|---|---|---|---|
| Fire (before the lava run) | Everything down here burns. Make sure it isn't you.* | Hell was built to burn you. Learn to run on it. | The floor is lava. So are the walls. So is the sky. |
| Shadow (before the Step chain) | Hell has never had to look over its shoulder.* | Make the denizens of Hell suffer a new nightmare. (the creator's example) | Even the damned are afraid of the dark. |
| Time (before the Rewind) | Down here, a mistake only lasts four seconds.* | Die. Take it back. Try again. | The clock is hers. The seconds are yours. |

**Filled vs placeholder (at 05:50):**

| Part | State |
|---|---|
| The 30 s setup | **rendered**: `FableAnimationTests\video\psychopomp-trailer\out\premise-1080p60.mp4` (and 1440p) |
| Gameplay shots (33) | **10 cut from a recorded run**: the opening sprint and slide; the wall-run over lava; the chain anchor shot mid-air; the lava hop; the lantern on the collapsing bridge; the reload and swap; the floor 1 shaft; the Collapse; Hold the Brand at speed; the lantern close-up. **23 still to record**: the two flash cuts, the Step chain, kills from the dark, Dilation, the Viscount, the scythe dodge, the Rewind showpiece, the skill pick, Combustion, Nightfall, floor 3, the wells, the Thief, the burning-room Rewind, all of Vesper, the decision and the escape |
| Theme cards, enemy cards | made; the enemy cards use stand-in pictures until frames from the takes replace them |
| The title-screen ending | stand-ins (the real title screen captured in three stages comes next) |
| Voices | text-to-speech placeholders |
| Music | the game's own cues: Baja's opening parts, the in-run Normal set for act 1, Danger for act 2, Vesper's Bride then her boss waltz for act 3, the decision loop, then the title cue |

**How to watch it:**

- the **first 30 seconds**: `premise-1080p60.mp4` above;
- the **full preview**: `out\full-1080p60.mp4` once rendered;
- or the review page (timeline, theme cards, questions; volume starts at half, nothing autoplays):
  `C:\Users\User\Desktop\yougotgamesonyophone\FableAnimationTests\art\review\2026-10-18\trailer\index.html`.

**What to judge, in order of what matters most:**

1. In the first 30 seconds, do you understand who you are, who Baja is and what you're doing?
2. Can you tell what Fire, Shadow and Time *do* in the game from what you see?
3. Does act 1 read as a shooter (not just parkour)?
4. Pacing: any stretch that drags, or a shot you'd cut or swap?
5. The ending: does the gate slam into the title screen feel like a finish?
6. Which slot would you drop if a shot can't be recorded in time?

## 8. Where your ideas and the creator's differ

**What we found:** no ideas of yours are recorded anywhere this session can read:

- not the event notes, the game's docs or the kit's docs;
- not the trailer plan or the chat history.

The team plan named roles (the creator as Director; the teammate with coding experience as **Builder**: Unity, tuning,
builds; the other as **Player/Producer**: playtests, levels and short text, the pitch). Teammate task packets were also
written before the event, but nothing came back into the files. So the differences we *can* list are between **the plan
you were handed** and **what was built**. After that comes a set of questions to capture your own ideas. If you pitched
something to the creator in person (a theme idea, a mechanic, a trailer idea), write it in the chat as **IDEA (mine)**.
Claude will set it against the creator's calls, and the creator will settle it.

| # | The plan you were given (or a likely idea) | What the creator decided or built | Suggested reconciliation / the question |
|---|---|---|---|
| 1 | **Roles:** the Builder runs the Unity editor, tuning and builds; the Player/Producer runs scheduled playtests and writes short text | Claude agents built, tuned and built the game overnight; the creator played it at 04:27 | Today you are the **critical player and trailer judge**. Say which role you are, and whether you want a part in the presentation (all three present) |
| 2 | **Packet A, playtest log:** five runs per build logged in `event/PLAYTEST-LOG.md` | No time for five runs; one build left | Two chapters, feedback in chat, item by item. Claude can copy it into a log file afterwards |
| 3 | **Packet B, wave tuner:** change wave numbers in the data files | Waves, hazards and the hour are tuned by the creator's rules and simulations; there's no time to retune before 09:00 | Report what feels unfair or too easy; Claude can change numbers in minutes if the creator agrees |
| 4 | **Packet C, capture operator:** run the capture commands, check the files | Claude records from replays and renders the takes itself | Record only if Claude asks (the command is in section 2) |
| 5 | **Packet D, voice:** the narrator's voiceover and character lines, recorded on a phone, words approved by the creator | Every voice is still a text-to-speech placeholder; the creator was to record Baja; all words are placeholders | Would you record the **narrator** (nine short trailer lines)? If yes, say so; recording needs the creator's yes on the words first |
| 6 | **Packet E, submission:** a public repo with the event credited, the trailer on Drive, the form by 09:30 | The repo is public (https://github.com/aitizazab/Psychopomp) with the kit credited; the creator submits by 09:00 | Check the README's **team section**: it lists "Builder" and "Player / producer" without names. Tell us your name as you want it credited (the creator approves before it goes in) |
| 7 | **The theme bend table:** one theme in the loop, the second in a mechanic or framing, the third in atmosphere; for light and dark, "darkness hides you"; for time, "the clock is the core" | **All three themes are in the core loop:** darkness hides you and fire exposes you; the Rewind, Dilation and Baja's hour; lava rooms and fire enemies. The Skyline mall setting was dropped for Hell | Agree, or would you have kept the game smaller? |
| 8 | **The story's weight:** the creator's own earlier read was that the judges might wave story dilemmas aside, so story takes a back seat | The game now has a full story: Baja, Vesper, the kill-or-spare choice, six endings | Your view as a critical player: does the story help the trailer, or slow it down? |
| 9 | **The trailer's text:** the creator's rule is no text over gameplay; the handbook says the trailer must *detail* how the themes are used | Three **implicit** theme cards between shots, not captions; the creator said even those might read as too obvious | Your vote on the cards (section 7). Do they explain the themes enough for a judge seeing it once? |
| 10 | **The title:** working titles were HellDescent, "Overdue" and others | **PSYCHOPOMP** is the working title in the game, README and repo | Does it work for you? (A change is the creator's call and would touch the trailer's ending) |
| 11 | **The guns:** kit names (the Writ, the Breacher) from the mall game | Kept for now by the creator's call | Do they feel right in Hell, or stick out? |
| 12 | **The art:** one style for the game | A look **per floor**: comic ink on floor 1, gritty photo textures on floors 2-3 | Does the change of look between floors read as a descent, or as two different games? |

## 9. Known issues and what's placeholder

- **Voices:** every voice (Baja, Vesper, the narrator) is **text-to-speech placeholder**. Baja is to be recorded by a
  person; the recording kit is in `FableAnimationTests\audio\baja-takes\README.md`. No real voice is cloned.
- **Words:** every line of dialogue, the opening cards, the floor taglines, the errand briefings, the tutorials, Baja's
  reminders, the splash lines and the ending cards are **agent drafts marked as placeholders** for the creator to
  rewrite. The creator writes all dialogue.
- **Vesper's in-run moments:** a placeholder set; the creator didn't like the first three. Five option sets are written
  (the recommendation: she "shadows" you, nearly wordless glimpses using your powers); **not picked yet**.
- **The trailer:** 23 gameplay shots are unrecorded and show as labelled black cards; the enemy cards' pictures and the
  title-screen ending are stand-ins; the final render takes about 10-15 minutes per pass.
- **Art:**
  - some effects are crude (the Nightfall sphere is a rim of dark blobs);
  - the comic words on enemy deaths are still the mall-robot set ("KZZT!" on an imp);
  - the Rewind's world effect is still cyan, the colour reserved for enemies.
- **Not built:** burns don't rewind; the shafts don't dim as the hour runs out (the gate carries that instead); some
  Rewind edge cases across room boundaries.
- **Tested how:**
  - the rules have about 1,400 automated tests, all green;
  - an autopilot plays the built game like a human;
  - the creator played four runs.

  But most of tonight's features have had **one human playthrough at most**, so **your play is the second**. Expect
  rough edges; report them.

- **The build:** if `out\hell\build\HellDescent.exe` is missing, the rebuild is still running (it started at 05:13).

## 10. Where everything is

| What | Path |
|---|---|
| This page (HTML) | `C:\Users\User\Desktop\yougotgamesonyophone\FableAnimationTests\art\review\2026-10-18\teammate-handover.html` |
| This page (Markdown, in the repo) | `C:\Users\User\Desktop\yougotgamesonyophone\HellDescent\docs\HELL-TEAMMATE-HANDOVER.md` |
| The review hub (every review page as a tab; this handover is the first) | `C:\Users\User\Desktop\yougotgamesonyophone\FableAnimationTests\art\review\2026-10-18\hub\index.html` |
| The game | `C:\Users\User\Desktop\yougotgamesonyophone\HellDescent\out\hell\build\HellDescent.exe` |
| The trailer's first 30 s | `C:\Users\User\Desktop\yougotgamesonyophone\FableAnimationTests\video\psychopomp-trailer\out\premise-1080p60.mp4` |
| The full preview (when rendered) | `C:\Users\User\Desktop\yougotgamesonyophone\FableAnimationTests\video\psychopomp-trailer\out\full-1080p60.mp4` |
| The trailer review page | `C:\Users\User\Desktop\yougotgamesonyophone\FableAnimationTests\art\review\2026-10-18\trailer\index.html` |
| The trailer's shot list with commands | `C:\Users\User\Desktop\yougotgamesonyophone\FableAnimationTests\video\psychopomp-trailer\SLOTS.md` |
| The creator's calls, in order | `C:\Users\User\Desktop\yougotgamesonyophone\HellDescent\docs\HELL-DESIGN.md` |
| The latest status | `C:\Users\User\Desktop\yougotgamesonyophone\HellDescent\docs\HELL-RESTART.md` |
| The public repo | https://github.com/aitizazab/Psychopomp |
