# HellDescent: the script (every line, in play order)

Written 2026-10-18 at about 00:30 by lane A (branch `hell-script`). **The creator writes all dialogue.** Every spoken or
story line below is a **PLACEHOLDER** written by an agent to show the length, tone and purpose a line needs. Rewrite
any of them freely; nothing here is settled wording.

**How to use this page**
- Each line has a **key**. The key is the contract between this page, the code and the voice files:
  `Resources/SkylineAudio/hell-voice/<speaker>/<key>.wav` (speakers: `god`, `vesper`, `narrator`), listed in
  `Resources/SkylineAudio/hell-voice/manifest.json`.
- **Where it lives** says which lane's file holds the line today. Lines marked *(planned)* belong to a lane that has not
  written its file yet: the key here is what that lane should use.
- **To rewrite:** change the line here (the "Line" column), then ask for the code tables and the voices to be refreshed.
  The voice generator reads the tables on this page directly:
  `C:/Users/User/tools/seed-vc/.venv/Scripts/python.exe Assets/JamKit/Templates/SkylineRepo/Resources/SkylineAudio/hell-voice/make_hell_voices.py`
  (Kokoro TTS on the CPU, under a minute; it rewrites every clip and the manifest).
- **Speakers:** `GOD` the God of Death (unnamed for now; name options at the end), `VESPER` the Evening Star,
  `NARRATOR` (the opening's narration and the trailer), `YOU` the player (silent so far: see open question 1),
  `CARD` text on screen with no voice, `HUD` a prompt.
- **Length limits** (so a rewrite still fits the timing): a subtitle is at most 2 lines of about 45 characters. Each
  section gives its limit in seconds of speech (about 2.5 words a second at these voices' pace).

Code files, by lane (branches as of 2026-10-18 00:20):

| Lane | Branch | File | Holds |
|---|---|---|---|
| flow | `hell-flow` | `Packages/com.vibejam.jamkit/Runtime/Skyline/Hell/HellFlowText.cs` | `Title`, `Opening[]`, `Floors[]`, `Credits[]`, menu words, `StatsTitle` |
| jobs | `hell-jobs` | `Packages/com.vibejam.jamkit/Runtime/Skyline/Hell/HellJobs.cs` | `HellJobCatalog` (each job's `name`, `briefing`, `objective`, `doneLine`, `failLine`), `HellJobBoons.Describe` |
| jobs | `hell-jobs` | `Assets/JamKit/Templates/SkylineRepo/Scripts/Hell/HellJobDirector.cs` | the fail reasons ("He got away.") |
| boss | `hell-boss` | `Packages/com.vibejam.jamkit/Runtime/Skyline/Hell/VesperFight.cs` | `VesperLines.Text(id)` |
| beats + tutorial (lane C) | `hell-beats` | *(planned)* `Runtime/Skyline/Hell/HellBeatRules.cs`, `HellTutorialRules.cs` | Vesper's floor beats, the God of Death's prompts |
| ending | *(not started)* | *(planned)* `Runtime/Skyline/Hell/HellEndingRules.cs` | the decision, the escape barks, the endings |
| rooms | `hell-rooms2` | `Assets/JamKit/Templates/SkylineRepo/Scripts/Hell/HellHudLine.cs` | the objective line and banners (no story words) |

---

## 1. The title screen

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `title` | CARD | the title screen | HELL DESCENT *(working name; options in section 12)* | flow: `HellFlowText.Title` |
| `menu` | CARD | the title menu | START / SETTINGS / QUIT; hints "ENTER / A select" | flow: `HellFlowText.Start` etc. (UI words, not dialogue) |

## 2. The opening (the God of Death's meeting)

Text cards on black and ember today (flow lane); the cinematic premise replaces them after 03:00 (see `HELL-TRAILER.md`).
**Limit:** 6 cards, each at most 7 s; the whole opening under 30 s. The cards with no speaker are narration, voiced by
the narrator.

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `open-01` | NARRATOR | Start pressed, card 1 | [PLACEHOLDER] Your sister is dead. | flow: `HellFlowText.Opening[0]` |
| `open-02` | NARRATOR | card 2 | [PLACEHOLDER] You didn't grieve. You went, furious, to the one who took her. | flow: `Opening[1]` |
| `open-03` | GOD | card 3 | [PLACEHOLDER] Mortals usually wait their turn. You're early. I could fix that. | flow: `Opening[2]` |
| `open-04` | GOD | card 4 | [PLACEHOLDER] ...Wait. I've had the funniest idea. | flow: `Opening[3]` |
| `open-05` | GOD | card 5 | [PLACEHOLDER] Lucifer took a bride down there alive. Vesper. Kill her. Teach him whose souls these are. And do me a few favours on the way. | flow: `Opening[4]` |
| `open-06` | GOD | card 6 | [PLACEHOLDER] Shadow to hide in. Time to bend. A door into Hell, which I don't have. Your sister is down there too. Oh, and don't take too long. | flow: `Opening[5]` |

## 3. The floor cards (text only)

**Limit:** a tagline of at most 6 words, 2.8 s. The floor names are the design's.

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `floor-1` | CARD | entering floor 1 | FLOOR ONE, THE EMBER GATE: [PLACEHOLDER] Ash and cinders. Her first favours. | flow: `HellFlowText.Floors[0]` |
| `floor-2` | CARD | after the floor 1 shaft and pick | FLOOR TWO, THE MOLTEN WORKS: [PLACEHOLDER] Lava rivers. Chains. Things that cut. | flow: `Floors[1]` |
| `floor-3` | CARD | after the floor 2 shaft and pick | FLOOR THREE, THE BLACK GLASS: [PLACEHOLDER] Obsidian and shadow. She is close. | flow: `Floors[2]` |

## 4. The God of Death's teaching (first-time prompts)

She gave you the powers, so she teaches them (gap 4). Each plays **once per save**, set off by a condition, voiced and
subtitled, with a HUD prompt that reads the real binding. Hidden by the capture lane's clean-picture switch.
**Limit:** at most 5 s of speech (about 12 words); the HUD prompt at most 6 words. Flippant and bored, never a manual.

| Key | Speaker | Trigger | Line | HUD prompt | Lives in |
|---|---|---|---|---|---|
| `tut-paths` | GOD | the first room's entry, floor 1 | [PLACEHOLDER] Two ways through every room. The long safe one, and the one I'd take. | (none) | lane C *(planned)* `HellTutorialRules` |
| `tut-step` | GOD | the first dark landing in Step range | [PLACEHOLDER] See the dark there? Step into it. Shadows are doors, if you're mine. | `[Step]` Shadow Step: dark ground only | lane C *(planned)* |
| `tut-dilate` | GOD | the first time you are fast and airborne with an enemy in view | [PLACEHOLDER] Aim while you're flying. The faster you move, the longer the world waits. | `[Aim]` at speed: slow time | lane C *(planned)* |
| `tut-rewind` | GOD | the first hit that leaves you under 30 health, or the first lethal bolt in flight | [PLACEHOLDER] That looked fatal. Take four seconds back. I'm only lending them. | `[Rewind]` four seconds back | lane C *(planned)* |
| `tut-lava` | GOD | the first lava contact | [PLACEHOLDER] That's lava. It's hot. I really shouldn't have to tell you this. | (none) | lane C *(planned)* |
| `tut-collapse` | GOD | the Collapse room's start | [PLACEHOLDER] The floor's leaving. I'd leave with it. Ahead of it, ideally. | (none) | lane C *(planned)* |
| `tut-carry` | GOD | first pick-up of the Soul Lantern (or the hourglass) | [PLACEHOLDER] Both hands. No stepping through shadows until you put it down. | Carrying: no Shadow Step | lane C *(planned)* |
| `tut-pick` | GOD | the first skill pick opens | [PLACEHOLDER] A gift. Pick one. Don't make me regret my generosity. | (the cards) | lane C *(planned)* |
| `tut-job` | GOD | the first job pocket comes into view | [PLACEHOLDER] Oh, while you're down there. A small favour. You'll see my mark. | (job card) | lane C *(planned)*, then the job's briefing |

## 5. Dying (the in-world reason and its cost)

A death restarts the room. The God of Death pulls you back (she owns death), and **it costs time** that comes off your
sister's: the stats screen shows "time owed" (gap 21). One line per death, rotating, never the same twice in a row.
**Limit:** 3 s (about 8 words).

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `death-01` | GOD | a death (rotation) | [PLACEHOLDER] No, no. You die when I say. Up. | flow *(proposed)*: a `HellFlowText.DeathLines[]` |
| `death-02` | GOD | a death (rotation) | [PLACEHOLDER] That one comes off her clock, you know. | flow *(proposed)* |
| `death-03` | GOD | a death (rotation) | [PLACEHOLDER] Embarrassing. For both of us. Again. | flow *(proposed)* |
| `death-04` | GOD | a death (rotation) | [PLACEHOLDER] I'm counting. She's counting too. | flow *(proposed)* |
| `death-05` | GOD | a death in the boss fight | [PLACEHOLDER] She let you live once. Don't make it a habit. | flow *(proposed)*, or the boss's restart |

## 6. Floor 1: the Ember Gate

Order in a run: the floor card, `tut-paths`, rooms (tutorial prompts as they come), the jobs offered on floor 1, Vesper's
first beat, the shaft, `tut-pick`.

### 6.1 The jobs of floor 1 (the God of Death's errands; text by the jobs lane)

Briefings play when you first see the job's pocket, the result line when it ends. **Limit:** a briefing at most 7 s
(about 18 words), a result at most 3 s. Flippant, bored, funny.

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `job-viscount-brief` | GOD | The Viscount of Cinders (Six Sigils, floor 1): briefing | There's a viscount on a balcony down there who keeps sending me fruit baskets. Make it stop. Permanently. | jobs: `HellJobCatalog` `Viscount.briefing` |
| `job-viscount-done` | GOD | done | No more pears. Bless you. | `Viscount.doneLine` |
| `job-viscount-fail` | GOD | failed | He got away. Expect a basket. Addressed to YOU. | `Viscount.failLine` |
| `job-overdue-brief` | GOD | The Overdue Soul (Break the Chains, floor 1): briefing | Albrecht Huss is three hundred years overdue. He's hiding in a cage. Cute. Step in and collect him. | `OverdueSoul.briefing` |
| `job-overdue-done` | GOD | done | Filed. Three centuries late, but filed. | `OverdueSoul.doneLine` |
| `job-overdue-fail` | GOD | failed | You walked past him. He waved. I saw. | `OverdueSoul.failLine` |
| `job-hourglass-brief` | GOD | The Lost Hourglass (Ooze Wells, floor 1): briefing | One of my reapers dropped my good hourglass down there. Fetch it. Don't shake it. | `Hourglass.briefing` |
| `job-hourglass-done` | GOD | done | Oh, it's even still running. Good boy. | `Hourglass.doneLine` |
| `job-hourglass-fail` | GOD | failed | You left it. On the floor. In HELL. | `Hourglass.failLine` |

(The jobs lane's lines are its own drafts; they are placeholders like every other line here. The objective lines, such
as "Kill the Viscount before his palanquin reaches his door", are HUD text in `objective` and are not voiced.)

### 6.2 Vesper's first beat: her voice in the Soul Lantern (hostile)

Plays when you pick up the Soul Lantern on floor 1. **Fallback** when the deck doesn't deal the Lantern on floor 1: the
second room's entry, her voice from the nearest brazier (gap 3). Letterbox, no camera hold. **Limit:** the beat at most
15 s.

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `v1-01` | VESPER | the Lantern picked up (or the fallback) | [PLACEHOLDER] Who let a living thing down here? ...Oh. You smell of her. The bone collector. | lane C *(planned)* `HellBeatRules` |
| `v1-02` | VESPER | follows | [PLACEHOLDER] Turn around. Whatever she promised you, she's lying, or she's bored. | lane C *(planned)* |
| `v1-03` | VESPER | follows | [PLACEHOLDER] I'll be at the bottom. Everything reaches the bottom eventually. | lane C *(planned)* |
| `v1-god` | GOD | after the beat | [PLACEHOLDER] Don't chat with the target. It's unprofessional. | lane C *(planned)* |

## 7. Floor 2: the Molten Works

### 7.1 The jobs of floor 2

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `job-ledger-brief` | GOD | The Ledger of Ash (Soul Lantern, floor 2): briefing | Some clerk is stamping my dead 'CANCELLED'. Burn his book. Not him. Well. Him too, if he fusses. | `Ledger.briefing` |
| `job-ledger-done` | GOD | done | Ashes. Lovely. Paperwork is the real Hell. | `Ledger.doneLine` |
| `job-ledger-fail` | GOD | failed | Filed. In triplicate. I hate him so much. | `Ledger.failLine` |
| `job-poacher-brief` | GOD | The Poacher (Hold the Brand, floor 2): briefing | A scythe demon is poaching my souls and selling them by the jar. Kill him from the dark. He spooks. | `Poacher.briefing` |
| `job-poacher-done` | GOD | done | Quietly done. I didn't even hear it, and I hear everything. | `Poacher.doneLine` |
| `job-poacher-fail` | GOD | failed | He spooked. Through a rift, with my jars. | `Poacher.failLine` |
| `job-whelp-brief` | GOD | The Whelp (the Collapse, floor 2): briefing | Someone chained up my dog. My SMALL dog. Break the chains. The smith keeps re-forging them, so... deal with that. | `Whelp.briefing` |
| `job-whelp-done` | GOD | done | Who's a good boy. Not you. HIM. | `Whelp.doneLine` |
| `job-whelp-fail` | GOD | failed | You left my dog in chains. Noted. | `Whelp.failLine` |

### 7.2 Vesper's second beat: her deal, her daughter, Lucifer's boredom (curious)

Plays at floor 2's shaft, before the skill pick (gap 3). A short camera hold (at most 4 s) on the shaft's glow, then the
lines over the fall. **Limit:** the beat at most 25 s. The facts are the creator's (HELL-DESIGN.md): her daughter died,
she bargained with Lucifer (she stays as his bride, her daughter lives), the daughter grew old and died long ago, and
Lucifer grew bored of her after a century; she is lost without purpose.

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `v2-01` | VESPER | floor 2's shaft | [PLACEHOLDER] You're still coming. Why? ...Someone, then. Someone you lost. | lane C *(planned)* |
| `v2-02` | VESPER | follows | [PLACEHOLDER] I had a daughter. A fever, a week to live. I gave him forever for her week. | lane C *(planned)* |
| `v2-03` | VESPER | follows | [PLACEHOLDER] She grew old. She was happy, I'm told. He kept me like a trophy. For a century, he even looked. | lane C *(planned)* |
| `v2-04` | VESPER | follows | [PLACEHOLDER] Then he got bored. Do you know what forever is, with no one watching? A very long afternoon. | lane C *(planned)* |

## 8. Floor 3: the Black Glass

### 8.1 The jobs of floor 3

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `job-thief-brief` | GOD | The Seconds Thief (Six Sigils, floor 3): briefing | A demon has been skimming seconds off my hourglass. He undoes any wound that doesn't finish him. So finish him. | `SecondsThief.briefing` |
| `job-thief-done` | GOD | done | My seconds. Every last one. | `SecondsThief.doneLine` |
| `job-thief-fail` | GOD | failed | He's still out there, being ten seconds younger than me. | `SecondsThief.failLine` |
| `job-bather-brief` | GOD | The Lava Bather (Ooze Wells, floor 3): briefing | A soul's been soaking in a lava spring for six hundred years and calls it a spa. Wade in. Reap him. | `Bather.briefing` |
| `job-bather-done` | GOD | done | Out of the bath. Finally. He was SO pruney. | `Bather.doneLine` |
| `job-bather-fail` | GOD | failed | He's still in the tub. Smug. | `Bather.failLine` |
| `job-notice-brief` | GOD | The Notice on the Door (Soul Lantern, floor 3): briefing | A demon is renting out my old cottage down there. Stand at his door while my notice burns in. He'll send friends. | `Notice.briefing` |
| `job-notice-done` | GOD | done | Evicted. I may visit. I won't. | `Notice.doneLine` |
| `job-notice-fail` | GOD | failed | The notice fell off. He's subletting now. | `Notice.failLine` |

The jobs' gifts (HUD text, not voiced; jobs: `HellJobBoons.Describe`): "Mended: plating and chassis full", "Quick
Shadow: Shadow Step recharges 1 s sooner", "Short Memory: Rewind cools down 3 s sooner", "Long Breath: Time Dilation
holds 0.75 s longer", "Her gift: <skill>". The fail reasons (`HellJobDirector`): "He got away.", "The ledger was
filed.", "You left the room."

### 8.2 Vesper's third beat: she sees herself in you

Plays on entering floor 3's last room (gap 3). **Limit:** the beat at most 15 s.

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `v3-01` | VESPER | floor 3's last room entry | [PLACEHOLDER] I know that walk. I walked it, with that same face. | lane C *(planned)* |
| `v3-02` | VESPER | follows | [PLACEHOLDER] You'll get her back. And then what will you be for? | lane C *(planned)* |
| `v3-03` | VESPER | follows | [PLACEHOLDER] Come down. Let's see which of us is still looking for something. | lane C *(planned)* |

## 9. The boss: Vesper, the Evening Star (barks)

From the boss lane (`VesperLines`, docs/HELL-BOSS.md "Lines"). **Limit:** a bark at most 4 s; she never quite goes for
the kill, so her lines read as testing you, not hating you.

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `boss-intro` | VESPER | the fight begins | [PLACEHOLDER] So she sent you. I made a deal once too. Let me see what yours bought you. | boss: `VesperLines.Intro` |
| `boss-phase2` | VESPER | phase 2 (Undo) | [PLACEHOLDER] Time? I have had nothing BUT time. Watch. | `VesperLines.Phase2` |
| `boss-rewound` | VESPER | her Rewind lands | [PLACEHOLDER] Again. I have all of it to spare. | `VesperLines.Rewound` |
| `boss-rewind-broken` | VESPER | you break her Rewind | [PLACEHOLDER] ...You broke it. No one has done that in a long time. | `VesperLines.RewindBroken` |
| `boss-phase3` | VESPER | phase 3 (Nightfall) | [PLACEHOLDER] He grew bored of the light. So did I. Come and find me. | `VesperLines.Phase3` |
| `boss-mercy` | VESPER | she pulls a killing blow | [PLACEHOLDER] Get up. | `VesperLines.Mercy` |
| `boss-mercy2` | VESPER | the second pulled blow | [PLACEHOLDER] Not like this. Get up. | `VesperLines.Mercy2` |
| `boss-restart` | VESPER | the fight restarts after your death | [PLACEHOLDER] Again, then. | `VesperLines.Restart` |
| `boss-defeat` | VESPER | her health reaches 0 | [PLACEHOLDER] ...There. Something new, at last. | `VesperLines.Defeat` |

## 10. The decision

After her defeat: she kneels by her throne; the camera is yours. The spare is **deliberate** (the creator's note): you
must walk to her and hold Use to offer your hand, and the offer is a purpose, not just mercy. Killing her is a shot.
While you stand there, the God of Death presses you every 8 s. **Limit:** each line at most 5 s.

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `dec-01` | VESPER | her defeat, kneeling | [PLACEHOLDER] Go on. She's waiting for my soul. I'd be waiting too. | ending *(planned)* `HellEndingRules` |
| `dec-god-01` | GOD | 8 s with no choice | [PLACEHOLDER] Well? Chop chop. I have a lunch. | ending *(planned)* |
| `dec-god-02` | GOD | 16 s with no choice | [PLACEHOLDER] It's one shot. You've done a thousand today. | ending *(planned)* |
| `dec-prompt` | HUD | in reach of her | Hold [Use]: offer your hand. [Fire]: end it. | ending *(planned)* |
| `dec-you-spare` | YOU | the hand offered (text only unless the player speaks) | [PLACEHOLDER] Help me get my sister out. Then find something to stay for. | ending *(planned)* |
| `dec-spare-01` | VESPER | the hand offered | [PLACEHOLDER] ...Something to do. It's been so long since anyone asked. | ending *(planned)* |
| `dec-spare-02` | VESPER | she takes it | [PLACEHOLDER] All right. Run, then. I know the way up. | ending *(planned)* |
| `dec-kill-01` | VESPER | you fire | [PLACEHOLDER] Thank you. Tell her she took her time. | ending *(planned)* |

### 10.1 The escape

The Collapse room re-dressed (gap 12): with your sister's soul in the Lantern, and Vesper running beside you if you
spared her. **Limit:** a bark at most 3 s.

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `esc-vesper-01` | VESPER | the escape starts (spared) | [PLACEHOLDER] Left! The bridge still remembers me. | ending *(planned)* |
| `esc-vesper-02` | VESPER | halfway (spared) | [PLACEHOLDER] Don't look back. It's the oldest rule down here. | ending *(planned)* |
| `esc-god-01` | GOD | the escape starts (killed) | [PLACEHOLDER] Lovely. Now run. The whole place is sulking. | ending *(planned)* |

## 11. The three endings, the stats and the credits

Which ending: **kill**; or **spare** with the God of Death's favour (`HellJobLedger.Favoured`: jobs done at least
two-thirds of those offered, and at least 2); or **spare** without it. Each ending is 2 voiced lines and one epilogue
card, then the stats. **Limit:** a line at most 7 s; a card at most 25 words.

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `end-kill-01` | GOD | ending: you killed her | [PLACEHOLDER] Lucifer is sulking. Delightful. A deal's a deal: one sister, slightly singed. | ending *(planned)* |
| `end-kill-02` | GOD | follows | [PLACEHOLDER] Do come by again. Not soon. | ending *(planned)* |
| `end-kill-card` | CARD | follows | [PLACEHOLDER] You brought your sister home. Some nights you look for the evening star, and it isn't there. | ending *(planned)* |
| `end-favour-01` | GOD | ending: spared, most jobs done | [PLACEHOLDER] You spared her. You disobeyed me. ...And you did every one of my chores. Ha! | ending *(planned)* |
| `end-favour-02` | GOD | follows | [PLACEHOLDER] Fine. Keep her. Something new for her to live for. You'll owe me one. | ending *(planned)* |
| `end-favour-card` | CARD | follows | [PLACEHOLDER] Three walked out of Hell: you, your sister, and the Evening Star, who finally had somewhere to be. | ending *(planned)* |
| `end-few-01` | GOD | ending: spared, few jobs done | [PLACEHOLDER] You spared her, and you did nothing I asked. Someone pays for that, darling. Time pays. | ending *(planned)* |
| `end-few-02` | GOD | follows | [PLACEHOLDER] Your sister keeps her years. I'll take mine out of yours. | ending *(planned)* |
| `end-few-card` | CARD | follows | [PLACEHOLDER] Your sister went home. You stayed below, paying off the hours. Vesper waits with you. She has the time. | ending *(planned)* |

**The stats screen** (flow: `HellFlowText.StatsTitle` = "THE DESCENT"; row names are UI words): time per floor, deaths,
time owed (the death cost, section 5), Rewinds, Shadow Steps, jobs done of offered, fast paths taken, the ending reached.

**The credits** (flow: `HellFlowText.Credits`): the title, "A descent in three floors" [PLACEHOLDER tagline], the makers,
"STORY AND DIALOGUE: [the creator]", the CC-BY and CC0 credits, fonts, sound, "THANK YOU FOR PLAYING".

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `sting` | GOD | after the credits, over black, before the title | [PLACEHOLDER] Lucifer sent me a fruit basket. ...I'm going to need someone again. | flow *(proposed)*: after `Credits` |

## 12. Trailer narration (see `HELL-TRAILER.md` for the cut)

**Limit:** the premise (`tr-01` to `tr-05`) at most 28 s of speech so it sits inside the 30 s with breaths.

| Key | Speaker | Trigger | Line | Lives in |
|---|---|---|---|---|
| `tr-01` | NARRATOR | trailer 0:02, the sister's death | [PLACEHOLDER] When your sister died, you didn't grieve. | trailer only |
| `tr-02` | NARRATOR | 0:06, the God of Death | [PLACEHOLDER] You went looking for the one who took her. | trailer only |
| `tr-03` | GOD | 0:11, her face | [PLACEHOLDER] ...Oh. I've had the funniest idea. | trailer only (or reuse `open-04`) |
| `tr-04` | NARRATOR | 0:15, the deal | [PLACEHOLDER] A deal. Shadow to hide in. Time to bend. A door into Hell. | trailer only |
| `tr-05` | NARRATOR | 0:22, Hell's gate | [PLACEHOLDER] Your sister is down there. So is the woman you were sent to kill. | trailer only |
| `tr-06` | NARRATOR | act 2 start, about 1:15 | [PLACEHOLDER] Every floor burns hotter. Every second is borrowed. | trailer only |
| `tr-07` | NARRATOR | act 3 start, about 2:15 | [PLACEHOLDER] She made the same deal, centuries ago. | trailer only |
| `tr-08` | VESPER | act 3, her face in the dark | [PLACEHOLDER] You'll get her back. And then what will you be for? | trailer (reuses `v3-02`) |
| `tr-09` | NARRATOR | 2:48, before the title | [PLACEHOLDER] Kill the Evening Star. Or offer her your hand. | trailer only |

## 13. Title candidates (decide after the story themes settle)

The themes: **Fire** (the setting, Hell), **Time** (the premise: the exchange of one person's time for another's),
**Shadow** (the protagonist and the powers). "The Burning Hour" was rejected. A good title should carry the story's
centre (the deal, the exchange of time, the Evening Star) more than the setting, since every hell game says "Hell".
Fonts for the title card: Pirata One (recommended), New Rocker, Metal Mania (HELL-ART-PASS.md).

| # | Title | Why |
|---|---|---|
| 1 | **Overdue** *(my first pick)* | Time and death in one word. Every soul in the story is overdue: your sister taken early, Vesper three centuries past her death, Albrecht Huss in his cage, you on borrowed seconds. The God of Death's bored bookkeeping voice is in it too. Short, memorable, a good end card. Risk: says little about fire. |
| 2 | **Evening Star** | Names the boss and the choice the trailer ends on. Venus is the light that shows at dusk: fire in the coming shadow, a time of day. It pays off when the judge learns who she is. Risk: reads soft for a shooter. |
| 3 | **Borrowed Hours** | The exchange of time is the premise (her time, or yours, for your sister's), and the ending "your own time pays" is its literal end. Rewind and Time Dilation are borrowed seconds too. Risk: no Fire, no Shadow. |
| 4 | **Errand into Hell** | The God of Death's flippant framing: Hell as a chore list. Funny, tells the premise, sets the tone the jobs carry. Risk: undersells the drama of act 3. |
| 5 | **Quietus** | Latin: release from debt, and death itself ("his quietus make with a bare bodkin"). The deal, the debt and the reaping in one word. Risk: hard to say and spell; also a name option below. |
| 6 | **Dusk Debt** | Shadow (dusk) plus the deal (debt); alliterative; Vesper is the star of dusk. Risk: sounds like a mobile game. |
| 7 | **The Long Afternoon** | From Vesper's centuries of boredom: forever with no one watching. Time and loneliness; literary. Risk: no heat, quiet for a trailer. |
| 8 | **Cinder Clock** | Fire and Time as an object: an hourglass of ash. Clear, visual, logo-able (an hourglass of embers). Risk: no story in it. |
| 9 | **Shadowbound** | Shadow and a deal that binds; you're bound below. Risk: generic, many games use it. |
| 10 | **Down Before Dawn** | The descent (down), a deadline (before dawn) and the dark (shadow): your sister must be out before the morning star rises, which is Lucifer. Risk: dawn reads hopeful. |
| 11 | **Hellbound Hours** | All three themes plainly: Hell, bound (shadow, the deal), hours. Risk: plain. |

## 14. Names for the God of Death (not Baja)

She is flippant, bored, amused, a bookkeeper of souls who has no access to Hell. A name should sound like her, not like
a metal album.

| # | Name | Why |
|---|---|---|
| 1 | **Morrow** *(my first pick)* | "Tomorrow", the day that never comes for the dead. Soft, almost friendly, which suits her teasing; Time is in it. |
| 2 | **Tally** | She counts: souls, overdue debts, your deaths ("I'm counting"). Bored-bookkeeper energy, a little comic, easy to say in a trailer. |
| 3 | **Quietus** | The old word for the settling of a debt and for death. Grand when the narrator says it, which plays against how casual she is. (If it isn't the title.) |
| 4 | **Hora** | Latin "hour"; the Horae were the goddesses of the hours. Time first, death implied; short and strange. |
| 5 | **Dusk** | Between day and night; she sits where the Evening Star (Vesper) and the Morning Star (Lucifer) meet. A shadow name that links the cast. |
| 6 | **Sable** | A colour of mourning and a soft fur: shadow, and a little vain. Fits a god who sulks about fruit baskets. |
| 7 | **Ankou** | The Breton folk figure who collects the souls of the dead (public-domain folklore, not anyone's IP). Old and specific. Risk: traditionally male, skeletal. |
| 8 | **Mother Lethe** | Lethe, the river of forgetting, as a matron. Old-world and a bit funny with "Mother". Risk: Greek myth names are common. |
| 9 | **The Reckoner** | A title, not a name: she settles accounts. Good for the narrator ("the Reckoner had the funniest idea"). Risk: stern, less of her humour. |

## 15. Open questions for the creator

1. **Does the player speak?** The deliberate spare needs the offer of a purpose. Today it is text only (`dec-you-spare`),
   or the gesture alone (the hand plus the lantern). Voiced, text, or silent?
2. **Who says the opening's narration** in the game: the trailer's narrator, or the God of Death telling it to you?
3. **The death cost:** "time owed" off your sister's clock as a stat only, or should many deaths change an ending?
4. **The sister's name**, and how she died (it shapes the premise still; the prompts in `HELL-TRAILER.md` keep it
   unshown).
5. The God of Death's name and the title: sections 13 and 14.

## 16. Voice placeholders

Generated 2026-10-18 by `make_hell_voices.py` with Kokoro-82M (local, Apache-2.0; no real person's voice): the God of
Death `bf_isabella` (British, dry), Vesper `af_kore` (lower, slowed 8%, a little room), the narrator `bm_george`
(deep, slowed 8%). Levelled to -18 LUFS. Listening page:
`FableAnimationTests/art/review/2026-10-18/hell-voices.html` (with two alternative voices per speaker). They exist for
timing only: the creator picks or records the final voices.
