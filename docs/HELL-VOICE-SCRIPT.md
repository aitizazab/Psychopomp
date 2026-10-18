# PSYCHOPOMP: the voice script (Baja and Vesper)

Every line Baja and Vesper speak in the game, in play order: **68 for Baja, 21 for Vesper**. The words come from
the code as of 2026-10-18 05:30 (`HellFlowText.Opening`, `HellLines`, `HellJobCatalog`, `HellHourLines`, `VesperLines`,
`HellEndingLines`); if the code changes, the code wins. The machine-readable copies the splitter uses are
`FableAnimationTests/audio/baja-takes/LINES.md` and `FableAnimationTests/audio/vesper-takes/LINES.md` (same order).

## Recording (phone)

1. **Quiet room**, no fan, soft things around you (a closet or a duvet is ideal). **Airplane mode** on.
2. Phone **15-20 cm** from your mouth, **slightly off to the side** (aim at the corner of your mouth), not straight on.
3. **One continuous recording per character**: all of Baja in one file, all of Vesper in another. Read top to bottom.
   Don't say the numbers or keys, only the lines.
4. **2-3 seconds of silence between lines.** Inside a line, **no pause over 1 second** (the "..." beats stay short).
5. **To redo a line: clap once** near the phone, then say that same line again. (The clap throws away the take before it.)
6. **Deeper and slower than your natural voice for both**, in your chest, never falsetto, never shouting: go quieter
   instead. The computer does the woman's voice; you do the acting.
7. **Send the file in any format** (m4a, mp3 or wav): `baja.m4a` and `vesper.m4a`.

**Baja:** bored, indifferent, faintly amused, regal and unhurried. **Vesper:** withdrawn, lonely, proud but hollow,
centuries-tired.

---

# BAJA (68 lines)

## The opening (the comic, before the game)

### 1. Mortals usually wait their turn. You're early. I could fix that.

<sub>`open-03` · her hall: first sight of you · *bored, unhurried; a quiet threat on "fix that"*</sub>

### 2. ...Wait. I've had the funniest idea.

<sub>`open-04` · she leans in · *flat "Wait", a real beat, then the smile on "funniest"*</sub>

### 3. Lucifer took a bride down there alive. Vesper. Kill her. Teach him whose souls these are. And do me a few favours on the way.

<sub>`open-05` · the deal · *regal; "Kill her." flat and final; the favours tossed off*</sub>

### 4. Shadow to hide in. Time to bend. A door into Hell, which I don't have. Your sister is down there too. Oh, and don't take too long.

<sub>`open-06` · the gate opens under you · *gifts listed lazily; amused on "which I don't have"; light on "Oh"*</sub>

## Teaching (the first time each thing happens)

### 5. Two ways through, always. Blue studs: safe, slow, boring. Ember stripes: fast. Guess which one I'm watching.

<sub>`tut.paths.1` · first room, floor 1 · *a bored tour guide; amused on the last sentence*</sub>

### 6. Stand in the brand until it's set. Move fast inside it and it sets faster. I didn't design it.

<sub>`tut.obj.HoldBrand.1` · Hold the Brand room · *indifferent; "I didn't design it" a dry shrug*</sub>

### 7. The shadows are yours now. Don't make it weird. Step into that dark patch.

<sub>`tut.shadowstep.1` · first dark ground in reach · *intimate, low; "Don't make it weird" dry*</sub>

### 8. Look at you. A little shadow. Adorable.

<sub>`tut.shadowstep.done` · after your first Shadow Step · *faintly amused, patronising*</sub>

### 9. You're quick. Aim while you're quick and the world waits for you. Rude of it, really.

<sub>`tut.dilation.1` · first time fast in the air near an enemy · *unhurried; an amused aside at the end*</sub>

### 10. Faster you go, slower it gets. Don't think about it too hard.

<sub>`tut.dilation.done` · after your first slow-time · *bored*</sub>

### 11. Ooh, that looked fatal. Take four seconds back. I have plenty.

<sub>`tut.rewind.1` · first near-fatal hit · *mock concern on "Ooh", then unbothered*</sub>

### 12. See? Time is cheap when it isn't yours.

<sub>`tut.rewind.done` · after your first Rewind · *smug, slow*</sub>

### 13. Oh, while you're down there. A small favour. Off the main road. You don't have to. You should.

<sub>`tut.job.1` · first job pocket in view · *casual; "You should." a soft threat*</sub>

### 14. Carry the lantern to the altar. No stepping while you hold it; it gets motion sick.

<sub>`tut.obj.SoulLantern.1` · Soul Lantern room · *dry*</sub>

### 15. That's lava. It's hot. I'm told that's the main thing about it.

<sub>`tut.lava.1` · first lava contact · *deadpan*</sub>

### 16. Three anchors hold that gate. Shoot them. Breaking things, you'll be a natural.

<sub>`tut.obj.BreakChains.1` · Break the Chains room · *a faint smile on "natural"*</sub>

### 17. Run. The floor is falling in behind you. That's the whole instruction.

<sub>`tut.obj.Collapse.1` · Collapse room starts · *unhurried, even though it's urgent*</sub>

### 18. That floor is leaving. I'd leave too.

<sub>`tut.collapse.1` · first cracked stone shakes · *bored*</sub>

### 19. A little present for getting this far. Pick one. Quickly; I age. Technically.

<sub>`tut.skillpick.1` · first skill pick · *regal generosity; amused on "Technically"*</sub>

### 20. Six sigils, in order, A to F. Hell loves paperwork.

<sub>`tut.obj.SixSigils.1` · first Six Sigils room · *weary contempt*</sub>

### 21. Those wells keep spitting ooze. Stand by each one until it seals. Three of them.

<sub>`tut.obj.OozeWells.1` · first Ooze Wells room · *mildly disgusted*</sub>

## Her errands (the jobs: briefing, done, failed)

### 22. There's a viscount on a balcony down there who keeps sending me fruit baskets. Make it stop. Permanently.

<sub>`job-viscount-brief` · floor 1 job briefing · *lazy delegating; a pleased, light "Permanently."*</sub>

### 23. No more pears. Bless you.

<sub>`job-viscount-done` · job done · *pleased, small*</sub>

### 24. He got away. Expect a basket. Addressed to YOU.

<sub>`job-viscount-fail` · job failed · *weight on YOU, not volume*</sub>

### 25. Albrecht Huss is three hundred years overdue. He's hiding in a cage. Cute. Step in and collect him.

<sub>`job-overdue-brief` · floor 1 job briefing · *"Cute." amused*</sub>

### 26. Filed. Three centuries late, but filed.

<sub>`job-overdue-done` · job done · *dry satisfaction*</sub>

### 27. You walked past him. He waved. I saw.

<sub>`job-overdue-fail` · job failed · *cold, slow*</sub>

### 28. One of my reapers dropped my good hourglass down there. Fetch it. Don't shake it.

<sub>`job-hourglass-brief` · floor 1 job briefing · *bored; careful on "Don't shake it"*</sub>

### 29. Oh, it's even still running. Good boy.

<sub>`job-hourglass-done` · job done · *warm, to a pet*</sub>

### 30. You left it. On the floor. In HELL.

<sub>`job-hourglass-fail` · job failed · *each bit flatter than the last*</sub>

### 31. Some clerk is stamping my dead 'CANCELLED'. Burn his book. Not him. Well. Him too, if he fusses.

<sub>`job-ledger-brief` · floor 2 job briefing · *irritated, then amused at herself*</sub>

### 32. Ashes. Lovely. Paperwork is the real Hell.

<sub>`job-ledger-done` · job done · *a contented sigh*</sub>

### 33. Filed. In triplicate. I hate him so much.

<sub>`job-ledger-fail` · job failed · *quiet loathing, almost funny*</sub>

### 34. A scythe demon is poaching my souls and selling them by the jar. Kill him from the dark. He spooks.

<sub>`job-poacher-brief` · floor 2 job briefing · *matter-of-fact*</sub>

### 35. Quietly done. I didn't even hear it, and I hear everything.

<sub>`job-poacher-done` · job done · *impressed, softly*</sub>

### 36. He spooked. Through a rift, with my jars.

<sub>`job-poacher-fail` · job failed · *disappointed*</sub>

### 37. Someone chained up my dog. My SMALL dog. Break the chains. The smith keeps re-forging them, so... deal with that.

<sub>`job-whelp-brief` · floor 2 job briefing · *weight on SMALL, not volume*</sub>

### 38. Who's a good boy. Not you. HIM.

<sub>`job-whelp-done` · job done · *baby-talk to the dog, cold to you*</sub>

### 39. You left my dog in chains. Noted.

<sub>`job-whelp-fail` · job failed · *"Noted." a quiet threat*</sub>

### 40. A demon has been skimming seconds off my hourglass. He undoes any wound that doesn't finish him. So finish him.

<sub>`job-thief-brief` · floor 3 job briefing · *unhurried; "So finish him" final*</sub>

### 41. My seconds. Every last one.

<sub>`job-thief-done` · job done · *savouring it*</sub>

### 42. He's still out there, being ten seconds younger than me.

<sub>`job-thief-fail` · job failed · *petty, amused*</sub>

### 43. A soul's been soaking in a lava spring for six hundred years and calls it a spa. Wade in. Reap him.

<sub>`job-bather-brief` · floor 3 job briefing · *amused disdain on "spa"*</sub>

### 44. Out of the bath. Finally. He was SO pruney.

<sub>`job-bather-done` · job done · *gossipy delight*</sub>

### 45. He's still in the tub. Smug.

<sub>`job-bather-fail` · job failed · *flat*</sub>

### 46. A demon is renting out my old cottage down there. Stand at his door while my notice burns in. He'll send friends.

<sub>`job-notice-brief` · floor 3 job briefing · *landlady, bored*</sub>

### 47. Evicted. I may visit. I won't.

<sub>`job-notice-done` · job done · *a beat before "I won't."*</sub>

### 48. The notice fell off. He's subletting now.

<sub>`job-notice-fail` · job failed · *weary*</sub>

## Her hour (reminders as her time runs out)

### 49. Tick tock. Your sister's not getting any less dead.

<sub>`hour.quarter` · a quarter of the hour gone · *light, cruel*</sub>

### 50. Half my hour, gone. I do hope the sightseeing was worth it.

<sub>`hour.half` · half gone · *polite sarcasm*</sub>

### 51. The door I'm holding open is getting heavy, darling.

<sub>`hour.three-quarters` · three quarters gone · *mock strain, still unhurried*</sub>

### 52. Hear that grinding? That's your way out, closing. Hurry.

<sub>`hour.last` · the last minutes · *"Hurry" low, not loud*</sub>

### 53. That's my hour. The door's nearly shut. Run anyway. I love a long shot.

<sub>`hour.out` · the hour is out · *amused at the end*</sub>

### 54. Cute trick. It doesn't work on MY clock.

<sub>`hour.rewind` · you Rewind; her clock doesn't · *weight on MY*</sub>

### 55. Too late. I did say.

<sub>`hour.gate-shut` · the gate shuts · *cold, final*</sub>

## The decision and the escape

### 56. Well? Chop chop. I have a lunch.

<sub>`dec-god-01` · 8 s over the kneeling Vesper with no choice · *bored; "Well?" goes down*</sub>

### 57. It's one shot. You've done a thousand today.

<sub>`dec-god-02` · 16 s with no choice · *impatient, quiet*</sub>

### 58. Lovely. Now run. The whole place is sulking.

<sub>`esc-god-01` · the escape starts (you killed her) · *pleased, light*</sub>

## The endings

### 59. Lucifer is sulking. Delightful. A deal's a deal: one sister, slightly singed.

<sub>`end-kill-01` · kill ending · *delighted, regal*</sub>

### 60. Do come by again. Not soon.

<sub>`end-kill-02` · kill ending · *a beat before "Not soon."*</sub>

### 61. You spared her. You disobeyed me. ...And you did my chores. Ha!

<sub>`end-favour-01` · spare ending, errands done · *outraged, then the one real laugh*</sub>

### 62. Fine. Keep her. Something new for her to live for. You'll owe me one.

<sub>`end-favour-02` · spare ending, errands done · *grudging, warm underneath*</sub>

### 63. You spared her, and you did nothing I asked. Someone pays for that, darling. Time pays.

<sub>`end-few-01` · spare ending, errands skipped · *cold*</sub>

### 64. Your sister keeps her years. I'll take mine out of yours.

<sub>`end-few-02` · spare ending, errands skipped · *calm, final*</sub>

### 65. Every errand, and on time. I could get used to you.

<sub>`end-lucifer-01` · every errand done, in time · *genuinely pleased*</sub>

### 66. One more. The Morning Star himself. Bring me his crown; keep the change.

<sub>`end-lucifer-02` · the Lucifer cliffhanger · *sly, savouring*</sub>

### 67. I held that door for an hour, darling. You took longer.

<sub>`end-damned-01` · the hour ran out · *almost sorry*</sub>

### 68. Well. You wanted to be with her. Forever is a long time to be right.

<sub>`end-damned-02` · the hour ran out · *quiet, turning away*</sub>

---

# VESPER (21 lines)

## Her voice on the way down (floor beats)

### 1. Put that down. The lantern is older than your whole family, and it doesn't like your hands.

<sub>`beat.vesper.f1.lantern.1` · floor 1: you pick up the Soul Lantern, her voice in it · *cold, proud; tired, not angry*</sub>

### 2. Whoever sent you, go back up and tell her the Evening Star is not for collecting.

<sub>`beat.vesper.f1.lantern.2` · follows · *proud; hollow on "not for collecting"*</sub>

### 3. Still coming? Hm. I made a deal like yours once. My daughter was dying, and he was so charming about it.

<sub>`beat.vesper.f2.deal.1` · floor 2 shaft, over the fall · *curious; bitter fondness on "so charming"*</sub>

### 4. She lived. Grew old. Died, long ago. He got bored of me in a century. So tell me: what are you down here for?

<sub>`beat.vesper.f2.deal.2` · follows · *slow; the loss flat, the question genuinely lonely*</sub>

### 5. Look at you. Someone you love, a deal you didn't read, and powers that aren't yours.

<sub>`beat.vesper.f3.mirror.1` · floor 3, the last room · *quiet recognition, almost tender*</sub>

### 6. I know that face. I wore it for three hundred years. Come down, then. Show me how it ends.

<sub>`beat.vesper.f3.mirror.2` · follows · *centuries-tired; an invitation, not a threat*</sub>

## The fight (barks)

### 7. So she sent you. I made a deal once too. Let me see what yours bought you.

<sub>`boss-intro` · the fight begins · *proud, testing, unhurried*</sub>

### 8. Time? I have had nothing BUT time. Watch.

<sub>`boss-phase2` · phase 2: she starts undoing your damage · *bitter weight on BUT*</sub>

### 9. Again. I have all of it to spare.

<sub>`boss-rewound` · her Rewind lands · *weary, not gloating*</sub>

### 10. ...You broke it. No one has done that in a long time.

<sub>`boss-rewind-broken` · you break her Rewind · *surprised, almost pleased*</sub>

### 11. He grew bored of the light. So did I. Come and find me.

<sub>`boss-phase3` · phase 3: the arena goes dark · *low, lonely*</sub>

### 12. Get up.

<sub>`boss-mercy` · she pulls a killing blow · *short, almost gentle*</sub>

### 13. Not like this. Get up.

<sub>`boss-mercy2` · the second pulled blow · *firmer; she won't win this way*</sub>

### 14. Again, then.

<sub>`boss-restart` · the fight restarts after your death · *tired, patient*</sub>

### 15. ...There. Something new, at last.

<sub>`boss-defeat` · her health reaches zero · *relief, a breath out*</sub>

## The decision and the escape

### 16. Go on. She's waiting for my soul. I'd be waiting too.

<sub>`dec-01` · kneeling, beaten · *hollow, ready to go*</sub>

### 17. ...Something to do. It's been so long since anyone asked.

<sub>`dec-spare-01` · you offer your hand · *disbelief turning to quiet hope*</sub>

### 18. All right. Run, then. I know the way up.

<sub>`dec-spare-02` · she takes it · *a little life back in her voice*</sub>

### 19. Thank you. Tell her she took her time.

<sub>`dec-kill-01` · you fire · *calm, grateful; dry on the end*</sub>

### 20. Left! The bridge still remembers me.

<sub>`esc-vesper-01` · the escape starts (you spared her) · *urgent but low; proud on "remembers me"*</sub>

### 21. Don't look back. It's the oldest rule down here.

<sub>`esc-vesper-02` · halfway up · *steady, a little wry*</sub>

---

## Notes for the integration (not for reading aloud)

- Voice files: `Resources/SkylineAudio/hell-voice/god/<file>.wav` and `.../vesper/<file>.wav`, where `<file>` is the
  key with dots turned into dashes (`tut.shadowstep.1` -> `tut-shadowstep-1.wav`; `HellLinePlayer.LoadVoice` tries that).
- Played with voice today: the opening comic (`open-*`), the tutorials (`tut.*`), Vesper's beats (`beat.vesper.*`),
  the hour (`hour.*`), the decision/escape/endings, and (since 2026-10-18) the job briefings/results
  (`HellJobDirector` plays `god/job-<id>-brief|done|fail.wav` with its cards; the ids `overdue_soul` and
  `seconds_thief` are `overdue` and `thief`) and Vesper's fight barks (`HellVesperFight` plays `vesper/boss-<id>.wav`),
  through `HellLinePlayer.PlayVoice` (Voices volume; a new line stops the same speaker's last one). A missing file
  stays text only.
- Dropped from the old script (not in the code): `death-01..05`, `sting`, `v1-god`, `tr-03`, the old `tut-*`, `v1/v2/v3-*`.
