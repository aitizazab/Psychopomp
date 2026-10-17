# HellDescent: story options (Vesper's moments in the run, the cutscenes)

Written 2026-10-17/18 for the creator to review. **These are options, not final lines.** The creator writes all
dialogue: every quoted line below is a draft to show length, tone and purpose, to pick from and rewrite. Nothing here
is built and nothing is committed.

Read from: `HELL-DESIGN.md` (premise, the creator's answers, Vesper), `HELL-JOBS.md`, `HELL-BOSS.md`, `HELL-FLOW.md`,
`HellFlowText.cs` (the six opening cards) and `HELL-SCRIPT.md` (the script lane's every-line page; it exists only on the
`hell-script` branch, in `..\HellDescent-script\docs`, not on main yet).

**The settled story this works from.** Your sister died. Furious, you go to the God of Death (unnamed; flippant, bored,
powerful, no access to Hell). She's about to kill you when she gets "the funniest idea": shadow and time powers and a
door into Hell, in exchange for favours and mainly for killing Vesper, Lucifer's living bride, to teach Lucifer not to
mess with her. "Don't take too long." Your sister's soul is in Hell. Time is the currency: Vesper's endless time, or
yours, for your sister's. Vesper, the Evening Star (Lucifer is the Morning Star): centuries ago her daughter died; she
bargained with Lucifer; he grew bored of her after a century; she has been lost without purpose since, chasing anything
new. She is you further down the same road. After you beat her: kill her and deliver her soul, or spare her (lower the
gun, walk to her, offer your hand and a purpose: help get your sister out). Most jobs done: the God of Death spares
you. Few jobs: your own time pays.

**What the run gives a story to hang on (important for every option below).** Each floor deals 4 of its 6 rooms at
random, so a moment tied to one room (like the Soul Lantern) may never play. The things every run has: the floor card,
the first and the last room of each floor, the shafts between floors, the skill pick, the jobs' pockets (side rooms
beyond a doorway, at least one per floor), the first Rewind, the first near-death. Every moment below hangs on one of
those, or says what pinning it would take.

---

## 1. Vesper's moments in the run: five option sets

The creator didn't like the current three (her voice in the Soul Lantern, her story told at floor 2's shaft, she sees
herself in you on floor 3). Their shared weakness: all three are Vesper talking at you about herself. Each set below
builds her a different way. The history itself (the daughter, the bargain, the century, the boredom) can move into a
comic flip-through before the boss (part 2), which frees the run to *show* her instead of having her explain.

### Set A: She shadows you (glimpses, with your own powers)

**The approach.** Vesper is bored, and you are the first new thing in Hell in a century. She follows you down. You
never get a conversation in the run, only sightings, and every sighting uses one of *your* powers, so by the boss the
player already knows: she has what you have, she's had it for centuries, and it didn't fill anything.

| # | Where and when | What the player sees and does | What it builds |
|---|---|---|---|
| 1 | **Floor 1, entering the second room** (every run has one). | Across the room, on the highest ledge, a slim veiled figure stands watching, lit violet from below. If you look at her for half a second, or get within 25 m, she dissolves in the exact smoke of your Shadow Step, your violet landing marker flicks up on a far ledge, and she's there for a beat, then gone again. You can shoot at her; the shots pass through the smoke. | She's curious, not hostile. She has your power. |
| 2 | **Floor 2, your first Rewind on the floor** (if you never Rewind there: the floor's last room). | As time snaps back, everything rewinds except one thing: a figure on a ledge who *doesn't* move backwards. She stayed put while the world went back four seconds. One line, quiet, close in your ear. | She has time, too, much more of it. Rewinding is something she's done until it meant nothing. |
| 3 | **Floor 2's shaft, falling.** | You drop down the shaft and pass her sitting on a ledge of the shaft wall, legs dangling, chin in her hand, watching you fall like someone watching the only show on. No line, or one. | Lost. Idle. A woman with nothing to do. |
| 4 | **Floor 3, the first blow that would kill you** (not lava; through the same hook the boss's mercy uses). Fallback if you never come close: the floor's last room entry, a bolt aimed at your back. | The world freezes (the Stasis look: bolts hanging in the air). She's beside you for the first time, close. She plucks the killing bolt out of the air, looks at it, looks at you, and lets time go. Gone. You're left on 10 health. | She saves the person sent to kill her. Sets up the boss, where she never quite goes for the kill, and the ending: why she would take your hand. |

**Draft lines** (all placeholders):
- Glimpse 1: none from her. The God of Death, a second later: "Ah. That's her. Don't wave." / "That's your target. She's staring. It's rude."
- Rewind (2): "Four seconds. I used to take back whole years. They come back the same, you know." / "Again? It doesn't change. Trust me."
- Shaft (3): none, or "Still going down. Good. Nobody comes down anymore."
- The save (4): "Not yet. I want to see how this ends." / "No. You're the most interesting thing that's happened in a hundred years." Then the God of Death: "...Did the target just save you? I'm docking that from something."

**How it builds toward the choice.** She's never a villain in the run; she's an audience. By the boss the player has
seen her wait, watch, idle and save them. Her mercy in the fight then reads as the same person, and the spare (giving
her something to chase that isn't you) answers what the glimpses asked.

**Cost to build.**
- Already in the game: her stand-in body (veil, crown, ember heart; `HellVesperBody`), her Shadow Step (smoke both
  ends, your landing marker; the boss's blink), the mercy hook on the player's death (`VesperFight.TryMercy` through
  `WreckIntercept`), the global Rewind (everything snapshots, so "she doesn't rewind" is just leaving her out of it),
  the Stasis skill's frozen-bolt look, the subtitles and the placeholder voices.
- New: a small "glimpse" script (place her on a ledge from a list per room, look or distance trigger, blink away);
  the save outside the boss (reuse the mercy intercept with a flag); the shaft ledge sitting pose. About 2-3 h with the
  planned beat player, less without letterbox. Her real model (gap 8) matters more here than in any other set: she is
  seen four times.

**In the trailer.** Strong. A veiled figure on a far ledge vanishing in the player's own smoke is a 2-second shot that
says "she has your powers" with no words. The frozen bolt in her fingers is an act 3 hook shot.

### Set B: Her centuries (traces she left behind)

**The approach.** You never see her until the boss. Instead you walk through what she did with forever: rooms she
made and left, hobbies she took up and dropped, a tally of days that stops. Hell is full of her unfinished things.
Mostly wordless; the God of Death comments, flippant, and once goes quiet.

| # | Where and when | What the player sees and does | What it builds |
|---|---|---|---|
| 1 | **Floor 1, a sealed side room off the first room** (a pocket like the jobs', a doorway in the wall). | A child's room carved from black glass. A cradle full of ash. A small wooden horse, the only wood in Hell. A wall of tally marks, thousands, in groups of seven, that just stops partway along a row. You can pick up the horse (it does nothing; it sits on your HUD until floor 3). | A mother. Someone counting days, then giving up counting. |
| 2 | **Floor 2, a gallery off the floor's second room.** | Her abandoned hobbies, each with a little scratched date: a harp with no strings ("year 212"), a half-carved statue of a man chiselled down to almost nothing, a garden of glass flowers, all broken, a chessboard mid-game with one chair, a sword rack ("year 340: swords"; underneath, "year 341: not swords"). | Lost without purpose. Chasing anything, dropping everything. Darkly funny, which the God of Death will enjoy. |
| 3 | **Floor 3, an observatory at the top of the floor's last room.** | A telescope aimed up a narrow shaft at a pinprick of evening sky: the only window out of Hell. Charts of the sky, and on the newest chart, a tiny figure scratched in falling down through the floors: you. A note: "Something new?" | She's been watching you the whole way. She's you down the road, and she knows it before you do. |

**Draft lines:**
- Nursery, the God of Death: "Hm. Someone's been sentimental." Then, at the tally marks, a beat later: "She stopped counting. They all do."
- Gallery: "Year 341, not swords. I'd have given swords at least a decade." / "Oh, she tried the harp. Everyone tries the harp."
- Observatory: none from the God of Death (she goes quiet for the first time). Vesper, once, far off: "You're the first new thing in a long time. Don't be disappointing."

**How it builds toward the choice.** The player learns her purposelessness from objects, so when the spare offers her a
purpose it lands as an answer to something the player saw. The wooden horse can be the ending's prop: offer it back to
her when you offer your hand.

**Cost to build.** The pocket system exists (an 18 m room beyond a doorway, built from data, with a lint test), so three
"trace pockets" are mostly dressing. New: the props (cradle, horse, harp, statue, glass flowers, chessboard, telescope),
a pick-up that just rides on the HUD, three trigger lines. Pockets need a side wall free of a job pocket in the chosen
room, and the room may not be dealt: pin the rooms (one line in the deck deal), or attach the traces to the floor's
first and last rooms, which every run has. About 3-4 h, mostly props. Risk: players on the fast path walk past optional
side rooms; make the doorway glow, or put the trace on the main route.

**In the trailer.** Good stills (the ash cradle, the observatory's pinprick sky), but static; a quiet act 3 beat, not a
hook on its own.

### Set C: The crossed line (she cuts into the God of Death's voice)

**The approach.** The God of Death talks to you through the whole run: briefings, deaths, tutorial prompts. Vesper can
hear it. Once per floor she cuts in on the line, and the two women bicker over your head. They have history: **the God
of Death came for Vesper's daughter, and Lucifer's bargain cheated her of that soul** (a proposal; see part 2). That's
the old insult behind "teach Lucifer not to mess with me".

| # | Where and when | What the player sees and does | What it builds |
|---|---|---|---|
| 1 | **Floor 1, the first job briefing.** | The briefing card starts as usual ("THE GOD OF DEATH"), then flickers violet and the speaker name changes to "VESPER". She talks over the briefing. The God of Death snaps back. The job carries on as normal. | They know each other. Vesper is bored enough to eavesdrop. |
| 2 | **Floor 2, the first job done.** | Vesper comments on your job, then asks the God of Death something nobody asks her, and the God of Death answers honestly by accident: she came for the girl once. | The daughter, the bargain, from the other side. The God of Death has a grudge, not just a whim. |
| 3 | **Floor 3, entering the floor.** | The God of Death's voice breaks up ("...too deep... can't..."); for the first time you're alone with Vesper's voice. She asks what you'll do after you get your sister back. | The mirror, said plainly once, after two floors of build-up. |

**Draft lines:**
- 1. Vesper: "She sends living errand boys now? She must be very bored." God: "Off my line." Vesper: "Your line runs through my house, darling."
- 2. Vesper: "Did she tell you why she wants me dead? Ask her about a little girl with a fever." God (a pause): "...I came for her once. Fevers were my best work. Your husband cheated." Vesper: "And she lived. Sixty more years." God: "And then I got her anyway. I always do."
- 3. Vesper: "She can't hear us this deep. So tell me. You get her back. Then what are you for?"

**How it builds toward the choice.** Both women become people with a past between them, so the ending is a choice
between two of them, not between a god and a target. It gives the God of Death's spare-you ending more weight: you
didn't just finish her chores; you closed her oldest grudge without killing anyone.

**Cost to build.** The cheapest set. The briefing card, the tracker, subtitles and placeholder voices all exist
(`HellJobDirector`'s card; the job events `Done` and `RunStarted`). New: a violet "interrupted" card variant and three
triggered exchanges. About 1-1.5 h. Floor 1's moment hangs on the first job briefing, and every run offers at least one
job per floor, so it always plays.

**In the trailer.** Weak as picture (it's a HUD card, and the trailer plan bans text over gameplay), but the two voices
over gameplay can carry act 2: "Ask her about a little girl with a fever." is a strong line under fast footage.

### Set D: Hell talks about her (the court and the statues)

**The approach.** Vesper isn't seen; Lucifer's Hell tells her story without meaning to. His monuments to the bride are
everywhere and all neglected; the imps gossip; on floor 2 her statues are literally being melted into the chains you
shoot. Lucifer stays unseen: his indifference is the evidence.

| # | Where and when | What the player sees and does | What it builds |
|---|---|---|---|
| 1 | **Floor 1, every room's dressing; the first one framed.** | A huge statue of a crowned bride over the first room's gate, her face lovingly carved, the rest crumbling and buried in ash. Smaller ones in every room, all chipped, one used as a coat rack by an imp. | Lucifer once adored her; nobody has looked after this in centuries. |
| 2 | **Floor 2, the Molten Works.** | Her statues on a conveyor into the furnace; the chains you shoot in Break the Chains and the Whelp's chains are poured from them. Two imps bicker as you enter a room. | He's not just bored; he's melting her down into hardware. |
| 3 | **Floor 3, a portrait hall on the way to the arena.** | A long hall of portraits of Lucifer's newer fancies, all lit. Hers at the far end, turned to face the wall. Turning it round (Use) shows the only bright portrait in Hell: her, a century into the marriage, smiling. | What she was, what she's become, and that nobody cares she's still down here. |

**Draft lines** (imp barks, played when imps spot you, with subtitles):
- "The bride? Still about. Haunts the bottom. Master hasn't asked after her in three hundred years."
- "Forty-seven statues to go. Then the hands. The hands are the worst, they look so sad."
- "Living one? Down here? That's two of them now."
- The God of Death, at the portrait: "He always did like his things shiny and new."

**How it builds toward the choice.** It shows Lucifer's side (seen only as neglect), so killing her finishes what he
started, and sparing her is the only thing anyone has done for her in centuries.

**Cost to build.** Medium. New: one bride statue model reused at several sizes (gap 8's Vesper model can be its
source), a melting-statue conveyor (props only), the portrait hall (a corridor before the arena, or the arena's entry
dressed), and enemy barks (a new small system: lines on the imps' first sight, rotating). The barks are the new code;
the rest is the art lane's. About 3 h plus art.

**In the trailer.** The statues on the conveyor into the furnace is a great act 2 image (Fire, and the story, at once).

### Set E: One meeting (she catches you in the fall)

**The approach.** Instead of three small beats, one real scene in the middle of the run. Floor 1 has only one hint;
floor 2's shaft is the meeting: you fall, and she stops time and catches you in mid-air, and you hang there in a frozen
shaft and talk. Floor 3 has nothing from her until the arena, so the meeting has room to breathe.

| # | Where and when | What the player sees and does | What it builds |
|---|---|---|---|
| 1 | **Floor 1's shaft.** | As you drop, a violet star mark glows on the shaft wall, freshly scratched. Nothing else. | Someone is down here. |
| 2 | **Floor 2's shaft: the meeting** (in-engine, 40-60 s, you can look around but not move). | Mid-fall, everything stops: falling embers hang, your body hangs. She steps out of the dark onto nothing and walks around you. She asks why you came, guesses the answer, and tells you a little of hers (the daughter only, not the whole story). Then she lets time go. | Curious, lonely, a mirror spelled out in one honest scene. |
| 3 | **Floor 3, the arena door.** | The door is open. She's sitting on the throne, waiting, not fighting yet. One line before the fight starts. | She knew you'd come; she's been looking forward to it. |

**Draft lines:**
- The meeting: "Hold still. You're falling, it's hard to talk to falling people." / "Who did you lose? ...Don't tell me. It's always someone." / "I had a daughter. I made a deal for her. You're making one now. Do you want to know how it goes?" / (she lets go) "Come find me at the bottom. I'll tell you the end."
- The arena door: "There you are. I've been looking forward to this. That's new, too."

**How it builds toward the choice.** The player gets to know her as a person in one go, then fights her; her mercy in
the fight lands because they've met. It's the most "story game" of the five.

**Cost to build.** One in-engine scene: a frozen shaft (time scale stops the embers; the player's camera free-look),
her model walking a short path with a few poses, about six voiced lines. About 2-3 h with the beat player. Risk: a long
pause in a speed game; it must be skippable, and it needs her real model and some animation to hold a close shot.

**In the trailer.** The frozen fall is a striking shot (embers hanging, a woman walking on air around you).

### Recommendation: Set A, she shadows you

Pick **Set A**. It is the only set where the player learns Vesper by *playing*: she uses their Shadow Step, ignores
their Rewind and catches their killing blow, so the boss's mirrored powers and her pulled blows pay off something the
player has already felt, and the spare reads as the answer to a woman who has been following them for want of anything
better to do. It reuses the most of what's built (her body, her blink, the mercy hook, the Rewind's snapshots), keeps
her words to a handful so the creator has little to write in the run, and it gives the trailer its best silent shots.

Two cheap borrows if there's time: Set C's floor 2 exchange (the God of Death came for the daughter; one HUD card and
three lines) gives the cast their link, and Set D's melting statues are pure dressing for the art lane.

---

## 2. The cutscenes: what must go in, what would be nice

**The format: an inked comic flip-through.** Each cutscene is a few comic pages. A page appears, its panels reveal
one at a time, the camera drifts slowly across each panel (a gentle push or pan), a voice reads the caption or line, a
page turn with a paper sound takes you to the next. Games that do this: *Max Payne* tells its whole story between
levels as graphic-novel pages, the hero's voice narrating over still panels; *inFamous* plays its story scenes as
animated comic panels that slide and fold into each other. The flip-through is cheap (stills, not animation), skippable
and fits the event's time.

**Drawing it in the comic-ink look** (the art direction being chosen tonight, likely Skyline's: thick hued outlines,
hard light, flat shadow shapes):
- Each panel is a generated 16:9 still (Flow images), then inked: the outline and posterise pass so it matches the game.
  Or an in-engine shot through the game's own outline look (good for anything set in Hell's rooms).
- **One palette rule per world:** the living world is cold (blue-grey, rain, hospital white), Hell is ember and black,
  the God of Death's place is bone-white and grey, Lucifer is only ever gold-white light, Vesper always carries violet.
- **The protagonist is a silhouette in every panel** (Shadow theme): a black cut-out, never a face. The player projects
  onto them, and it saves keeping a face consistent across generations.
- Character sheets first (the God of Death, Vesper young and Vesper now, the sister, the daughter), so generations stay
  consistent. Per the art standing rule, 16:9, stated in every prompt.
- Lettering: captions in boxes (narration), speech in balloons, a hand-lettered comic font; the narrator's captions can
  stay on screen as text inside the panel, which is fine even in the trailer since it's not over gameplay.

### The list

| # | Cutscene | Where it sits | Length | MUST / NICE |
|---|---|---|---|---|
| 1 | The God of Death's deal (the opening) | after Start, before floor 1 | 45-55 s | **MUST** |
| 2 | The sister (who she was, how she died) | the opening's first page, or a page at floor 2's shaft | 15-20 s inside 1, or 20 s alone | **MUST as part of 1**, NICE as its own page |
| 3 | Vesper's history | floor 3's shaft, falling into the arena (before the boss) | 60-75 s | **MUST** |
| 4 | Ending: you kill her | after the escape | 25-30 s | **MUST** |
| 5 | Ending: you spare her, most jobs done | after the escape | 30-35 s | **MUST** |
| 6 | Ending: you spare her, few jobs done | after the escape | 30-35 s | **MUST** |
| 7 | Floor covers (one comic cover per floor) | replacing the floor cards | 3 s each | NICE |
| 8 | The God of Death and Lucifer (the old grudge) | inside 3, one page; or the floor 2 shaft | 15 s | NICE |
| 9 | After the credits: the sting | after the credits, before the title | 8-10 s | NICE |

The decision itself (walk to her, lower the gun, offer your hand, or shoot) stays **in the player's hands**, in
engine, not a cutscene: the creator's rule that sparing must be deliberate only works if the player does it.

### 1. The God of Death's deal (MUST, the opening, 45-55 s)

**Purpose.** Set the premise, the tone (grief played straight, the God of Death played funny), the powers, the target
and the time pressure, faster and better than the six text cards. Doubles as the trailer's first 30 s if cut down.

**Pages and panels** (one line per image):
- **Page 1, the sister** (see 2 for which version):
  1. Rain on a window; inside, a hospital bed, or a burnt doorway: her empty hands.
  2. Close: the silhouette (you) holding her hand; the heart monitor flat, or the ash.
  3. Wide: you alone at a grave, everyone else gone home, it's dark.
- **Page 2, going to Death:**
  4. Your silhouette walking down into a cellar / a subway / a well, carrying a lit candle and something sharp: furious.
  5. A huge grey hall, endless shelves of hourglasses, one running. Very quiet.
  6. The God of Death, reveal: lounging sideways on a throne of stacked hourglasses, eating a pear from a fruit basket
     (the Viscount's baskets: the joke pays off on floor 1), bored stiff. Bone-white, a scythe used as a footrest.
- **Page 3, she's about to kill you:**
  7. You shouting at her (the balloon is just a jagged scribble: the player's words aren't written).
  8. Her, not even sitting up, one finger raised; your silhouette's hourglass on the shelf behind you lifts and tips.
  9. Close on her face: she stops. An eyebrow. A slow grin.
- **Page 4, the funniest idea:**
  10. Her leaning over you, the pear in her hand pointing at you.
  11. A smaller inset: a portrait of Vesper, veiled, violet, a gold-white blaze beside her that's Lucifer (no face).
  12. Her hand on your chest: shadow pours into you from her fingers; a clock face cracks in your eye.
- **Page 5, the door:**
  13. She draws a door in the air with her scythe tip; it opens on ember light (she can't go through; she leans on the
      frame, not touching it).
  14. You step through. Behind you she waves, bored, already reaching for another pear.
  15. Last panel, full page: you falling into Hell, the title card burns in.

**Narration and lines** (drafts; narrator in captions, the God of Death in balloons):
- (Page 1, narrator) "My sister died on a Tuesday. Nobody could tell me why her." / "Everyone said give it time. I didn't want time. I wanted her back."
- (4, narrator) "So I went to the one who took her."
- (6, God) "Oh. A visitor. Nobody visits." / "You're alive. That's a scheduling error."
- (8, God) "You came down here to shout at Death. Brave. Stupid. Mostly stupid. Let me fix it."
- (9, God) "...Wait." (beat) "I've had the funniest idea."
- (10-11, God) "Lucifer keeps a bride down there. Alive. In MY business, with no paperwork. Her name's Vesper." / "Kill her. Bring me her soul. Let him learn what it's like to lose someone."
- (12, God) "Shadow, to hide in. Time, to bend. And a few favours on the way. I'm generous, not free."
- (13, God) "Your sister's down there too, by the way. Hell's long. Her time isn't."
- (14, God) "Off you go. Don't take too long, I get bored."

**Notes.** Skippable after the first viewing (and on Escape). The six existing text cards stay as the fallback until the
pages exist. Who narrates the opening is an open question (the trailer's narrator, or the player character's own voice,
or no narrator and only captions).

### 2. The sister (MUST inside the opening; the open question of who she was)

**Purpose.** Make the player care about getting her out in 10-20 seconds, and explain why a dead woman's soul is in
Hell (the God of Death owns death; why isn't her soul with her?). Three options; each changes the story a little.

| Option | Who she was, how she died | Why her soul is in Hell | What it adds | Risk |
|---|---|---|---|---|
| **1. She made a deal for you** *(my pick)* | Your older sister. You were the one dying (a fever, an accident), and you got better overnight. Weeks later she died in her sleep, smiling. | She did what Vesper did: she bargained with Hell for your life, and paid with her soul. You're not rescuing a victim; you're undoing a gift. | The mirror becomes three people deep: Vesper gave herself for a daughter, your sister gave herself for you, you're about to trade someone for her. It makes the ending's "whose time pays" bite hardest. It explains Hell without the God of Death having lost her soul. | The opening must hide the twist or tell it: hide it (you learn it in Vesper's history: "Who do you think writes those deals? He does. He did yours.") or tell it in panel 3. |
| **2. Stolen on the way** | Your younger sister; died in a fire (a house fire, a crash that burned: the Fire theme), getting you out first. | A soul-poacher took her on the way to the God of Death and sold her down to Hell (the Poacher job is the same trade). She was never meant to be there. | Ties the jobs to the story (the God of Death's errands are about souls going missing); gives the God of Death a small, buried reason to care. The player is purely a rescuer. | Simpler, less of a mirror with Vesper. |
| **3. She earned it** | Your sister did something unforgivable to protect you (she killed the man who hurt you). She died in prison, or of what followed. | She's in Hell justly. | The darkest one: the whole trip is to take a soul out of Hell that the rules say belongs there. Makes killing Vesper (who did nothing wrong) and keeping the deal hard to argue for. | Heavy for an event game's 45-second opening; needs more screen time to land. |

Whichever is chosen: she needs a face and one detail the player remembers (a coat she lent you, a song, a scar), drawn
once in the opening and seen again in the endings. Her name is the creator's.

### 3. Vesper's history (MUST, before the boss, 60-75 s)

**Purpose.** Make her a person before the fight, so the fight and the choice carry weight. Everything the run's
moments (Set A) only hinted at, said once, in order. It replaces the floor 2 "her story" beat.

**Where.** Floor 3's shaft: you fall the long way into her arena, and the pages play as the fall (skippable). The arena
gate is shut behind you when it ends. Alternative: split it into three pages, one per floor's shaft (20 s each), a
serial in the run instead of one block (works well with Set B or D, less with Set A).

**Who tells it.** Three choices:
- **Vesper herself**, to you, as you fall (intimate; the fight's first line follows straight on). *My pick.*
- **The God of Death**, as a dossier on her target, flippant at first, then going quiet at the daughter's page (gives the
  God of Death depth, and it's funny).
- **Both**: the God of Death starts the file, Vesper cuts in to tell it properly (pairs with Set C).

**Pages and panels:**
- **Page 1, the daughter** (an old world, warm candlelight, centuries ago):
  1. A small stone house, night, one lit window.
  2. Young Vesper (unveiled, ordinary, a working woman's hands) holding a feverish little girl.
  3. A grey figure at the foot of the bed with an hourglass running low: the God of Death, come for the child.
     (Optional: the cast link; see 8.)
- **Page 2, the bargain:**
  4. Vesper at a crossroads at dawn, kneeling; a blinding gold-white shape before her: Lucifer, all light, no face.
  5. Two hands meeting: hers, and a hand made of light. Where they touch, her shadow peels off and goes into him.
  6. Back at the house: the girl sitting up, laughing. Vesper outside the window, already veiled, not allowed in.
- **Page 3, the bride:**
  7. A wedding in Hell: the crown, the veil, a thousand demons, Lucifer a sun on the throne beside her.
  8. A montage strip of small panels: him showing her off, giving her gifts, statues of her going up.
  9. A window of the world in a pool: her daughter grown, married, old, a grave. Vesper watching, hand on the surface.
- **Page 4, the boredom:**
  10. The throne room; his light turned away from her toward something new. Her seat on the dais, empty space around it.
  11. Years passing in one panel: the same pose, dust, then ash, piling on her shoulders.
- **Page 5, the centuries since:**
  12. A grid of tiny panels: her trying things: a harp, a sword, a garden of glass, a chessboard alone, a telescope.
      Each panel she's dropped it.
  13. Her at the edge of a high ledge, looking down a shaft. Something falling toward her: a silhouette. You.
  14. Last panel, full page: her standing up from the throne in the dark, the first time she's smiled in a century.
      Straight into the fight.

**Narration** (drafts; told by Vesper):
- (1-3) "I had a daughter. She caught a fever the winter she turned six. I saw the grey woman come to the foot of her bed."
- (4-5) "So I went to the crossroads and found the only one who'd bargain. He was very bright. He wanted a bride."
- (6) "She lived. I wasn't allowed to see it."
- (7-8) "For a century he looked at me like I was the sun. He built me statues. He built me rooms."
- (9) "She grew old. She was happy, I think. I watched through a puddle in Hell."
- (10-11) "Then he found something newer. He always does. Do you know what forever is, with no one looking? A very long afternoon."
- (12) "I tried everything. Music. Swords. Gardening. Chess, alone, which is worse."
- (13-14) "And then something fell down my stairs with a gun and a dead sister. The first new thing in a hundred years. So come on. Show me what she bought with you."

### 4. Ending: you kill her (MUST, 25-30 s)

**Purpose.** The deal kept, the cost felt. Not a punishment ending; a sad, clean one.

**Panels:**
1. Vesper's soul rising from her body as a small violet star; it settles in your Soul Lantern.
2. The God of Death's hall: she takes the star from the lantern like a sweet, looks at it, and pockets it.
3. Far below, a gold-white blaze turns: Lucifer notices, for the first time in a century, a little too late. (Or: he
   doesn't notice at all; see Lucifer below.)
4. Your sister, walking out of the door the God of Death drew, into rain, real, alive, blinking at the sky.
5. The two of you on a roof at dusk. The sister points up at the evening star. You don't look.

**Lines:**
- God: "One Evening Star. Signed, sealed. Lucifer is sulking already. Delightful." / "A deal's a deal: one sister, slightly singed."
- Card: "You brought her home. You never told her the price. Some nights she points at the evening star, and you can't look at it."

### 5. Ending: you spare her, most jobs done (MUST, 30-35 s)

**Purpose.** The best ending: the God of Death, amused, lets it stand, because you did her chores and because Lucifer
losing his bride *to someone else's purpose* is the better joke.

**Panels:**
1. The escape's end: three figures coming out of Hell's gate: you, Vesper carrying your sister's lantern, the sister's
   soul glowing in it.
2. The God of Death leaning on her door frame, arms folded, tapping her foot. The fruit basket by her feet.
3. Close: her face, deciding. Then she laughs.
4. Down below, the gold-white blaze looking at an empty throne.
5. Dawn (Lucifer's star fading) on a hill: you and your sister; Vesper a little apart, her veil off, looking at
   the sun for the first time in centuries.
6. Small last panel: the God of Death writes something in a ledger: Vesper's name, and a date a long way off.

**Lines:**
- God: "You spared her. You disobeyed me, to my face. ...And you did every single one of my chores. Ha!"
- God: "Fine. Keep her. A bride with nothing to do is Lucifer's problem; a woman with something to do is mine. I'll be watching."
- Vesper (optional, panel 5): "So. What do people do up here?"
- Card: "Three walked out of Hell. One of them had somewhere to be, for the first time in a hundred years."

### 6. Ending: you spare her, few jobs done (MUST, 30-35 s)

**Purpose.** You spared her but didn't earn the God of Death's patience: Time pays. Your own years for your sister's.
Bittersweet, not cruel.

**Panels:**
1. The same gate, the same three figures; the God of Death waiting, not amused. No fruit.
2. She holds up your hourglass. Most of the sand is still in the top.
3. She turns it, and pours most of it into a second, smaller one: your sister's.
4. Your silhouette, greying from the edges, the shadow thinning.
5. Your sister, grown up, years later, at a grave (yours) with an old woman beside her: Vesper, who aged, and stayed.
   (Or: you stay below working off the hours, and Vesper keeps you company.)

**Lines:**
- God: "You spared her, and you did nothing I asked. Someone pays for that, darling. Time pays."
- God: "Your sister keeps her years. I'll take mine out of yours."
- Card: "Your sister got her life. You got a little less of yours. Vesper stayed to the end of it. She had the time."

### 7. Floor covers (NICE, 3 s each)

Each floor opens on a comic-book cover instead of the text card: the floor's name in the title font, an issue number
("No. 1"), a striking image of the floor (the Ember Gate's ash bride statue, the Molten Works' furnace, the Black
Glass's mirror floor) and a cover line ("Her first favours!"). From in-engine shots inked. Cheap if the in-engine
outline look is in.

### 8. The God of Death and Lucifer: the old grudge (NICE, one page, 15 s)

**Purpose.** Ties the cast together and gives "teach Lucifer not to mess with me" a reason: Vesper's daughter was the
God of Death's to collect, and Lucifer's bargain took her out of the God of Death's hands for sixty years and put a
living woman in Hell beyond her reach. The insult is centuries old; you are the first tool she's had for it.

**Where.** As panel 3 of Vesper's history (cheapest), or as a page at floor 2's shaft told by the God of Death.

**Panels:** the grey figure at the foot of a child's bed, hourglass nearly out; the sand stopping mid-fall; a gold-white
hand on the hourglass's top; the God of Death's face, still, for once not joking.

**Line:** God: "I came for that girl once. He took her off my shelf. Nobody takes things off my shelf."

### Lucifer: seen, or only felt?

| Option | How | For | Against |
|---|---|---|---|
| **Only felt** | Never on screen; only his light (gold-white glare), his monuments, his empty throne, other people talking about him. | His absence *is* the story (he doesn't care Vesper's gone). No character to design. | Some judges may want the big bad. |
| **Seen only in the past** *(my pick)* | In Vesper's history he's a blinding gold-white shape with no face, a hand of light. In the present he's absent. | He's the Morning Star: drawing him as pure light is on theme, striking in the ink look, and cheap (no face to keep consistent). The present-day absence still says he's bored. | None much. |
| **Seen once at the end** | A sting after the credits: his light turns toward the empty throne, or he sends the God of Death a fruit basket. | A sequel hook, a laugh. | Slight risk of making him the point. |

### 9. After the credits (NICE, 8-10 s)

One panel over black: the God of Death, a fresh fruit basket at her feet with a gold-white card tied to it. She reads
it, sighs: "...I'm going to need someone again." Works after any ending.

---

## 3. Open questions for the creator

1. **Which in-run set?** (A recommended; or another, or a mix.)
2. **The sister:** which option (a deal she made for you, stolen on the way, or she earned it)? Her name, her one
   detail, and your age difference?
3. **Who tells Vesper's history:** Vesper, the God of Death, or both?
4. **Does the player character speak** (voiced narration in the opening and endings), or captions only, or silent?
5. **Lucifer:** felt only, seen only in the past, or a sting at the end?
6. **The cast link:** do you want the God of Death to have come for Vesper's daughter (the old grudge)?
7. **The few-jobs ending:** you age and Vesper stays with you, or you stay below working off the hours?
8. **Format:** comic flip-through for all cutscenes, or the opening in engine and only the history as pages?
