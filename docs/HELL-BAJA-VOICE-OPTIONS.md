# Baja's voice: how to get a professional one by the deadline

Written 2026-10-18 02:15 PKT. Submission is **2026-10-19 09:30 PKT**, about 31 hours away. Baja has **58 `GOD`
lines** in `docs/HELL-SCRIPT.md`, roughly 500-800 words. The performance brief is `docs/HELL-BAJA-VOICE.md`:
send that, not a reference clip. Prices are what the web showed today; confirm them on each page before paying.

**Before contacting anyone:** most `GOD` lines are still marked `[PLACEHOLDER]`. A paid actor records exactly what
you send, so lock the wording first. Each revision after delivery costs time and usually money.

## The licence question (it decides who you can hire)
The hackathon wants a public repo with every asset in it. That does not mean the voice files have to be CC BY.
- **Recommended:** keep the voice files out of the CC BY 4.0 grant. Put them in `Assets/Audio/Voice/Baja/` with a
  `LICENSE-VOICE.txt` that says: "© [actor]. Used in HellDescent with permission. Not covered by the repo's CC BY
  licence. No reuse, no AI training or voice cloning." Then ask the actor to agree to:
  (a) use in a free, non-commercial game; (b) the files being publicly visible in a GitHub repo; (c) a credit line.
- **Why it matters:** many voice actors refuse to have raw, clean takes published under an open licence, because
  anyone could use them to clone the voice. AI cloning is the industry's biggest dispute right now. The carve-out
  removes most of that worry.
- **Commit only the processed takes** (with the layers and reverb, below). Keep the raw takes local or in a private
  drive. Processed takes are much harder to clone from.
- **Get the permission in writing.** A Fiverr order message or an email reply counts. Fiverr's default terms assign
  rights in the delivery to the buyer unless the gig page says otherwise. Many voice gigs sell a separate
  "commercial use" add-on, and some sell it as a 1-year licence. Read the gig's licence section. If in doubt, buy
  the perpetual or commercial option: a public repo counts as public distribution.

## Ranked recommendation for this deadline

| # | Option | Cost | Delivery | Licence fit | Quality |
|---|---|---|---|---|---|
| 1 | **Fiverr game-character voice actor, express 24 h**, ordered from 2-3 shortlisted sellers in UK/EU/Asian time zones | about $40-250 (base $5-50 per 100 words, plus 24 h express and commercial/raw-file add-ons) | 24 h after the order. Order by **~10:00 PKT today** | Good if the gig sells commercial or perpetual rights and the seller agrees in writing to the repo carve-out | Varies a lot: pick only from demo reels. The best sellers are genuinely pro |
| 2 | **ElevenLabs Starter ($5-6, one month)**: Voice Design from a text description, then our processing | $5-6 | Same day, done by us | Paid plans include a commercial licence, so CC BY or the carve-out are both fine. Do **not** use the free tier (see below) | Already judged "not good enough for most purposes". Fallback only, or a safety net generated in parallel |
| 3 | **Volunteer/indie casting** (Casting Call Club, r/VoiceActing, r/gameDevClassifieds, Discord VA servers), offer a small fee | $0-100 | Uncertain: auditions alone take most of a day | Negotiable. Volunteers usually accept credit plus the carve-out | Varies. Some jam-hungry actors are excellent, but there's no guarantee within 24 h |
| - | Voices.com / Voice123 / Backstage | Pro rates, often $200-600+ for a game session. Voices.com posting is free plus a 4% fee; Voice123 is by subscription or quote | Casting plus auditions usually takes 1-3 days | Usually clean, negotiated contracts | Highest. **Too slow for this deadline**: use for a post-event version |

### Why this order
- **Fiverr is the only route that reliably pairs a pro-sounding human with a 24-hour turnaround.** Several
  game-character sellers advertise "most orders within 24 h". One per-line seller notes the Fiverr norm of about
  8 words per line, so ~60 lines is about 500 words. Time zones are the real risk. 10:00 PKT is 06:00 in the UK
  and 01:00 on the US East Coast, so a US seller probably starts this evening (PKT) and delivers near the deadline.
  Prefer UK/EU/South Asian sellers, or US sellers who state 24 h express, and ask for delivery by **04:00 PKT on
  the 19th**. That leaves time to process and import.
- **ElevenLabs is cheap, instant and licence-clean on a paid plan**, but it is a step down in quality. Run it in
  parallel today as insurance, so the game never ships without a voice.
- **Volunteers are the best value but the worst gamble on time.** Post a call anyway, at no cost. If someone great
  answers within hours, they can replace option 1 or record the post-event version.

## ElevenLabs details (fallback)
- **Free tier: unusable for the repo.** The free plan "does not include a commercial license", and published
  content must credit "elevenlabs.io" or "11.ai" *in the title*. CC BY 4.0 allows others commercial reuse, which
  conflicts with that. A carve-out licence would avoid the conflict, but the title credit remains.
- **Paid (Starter $5-6/month, about 30k credits, which is plenty for ~60 lines and several takes):** you own the
  outputs and can use them commercially, including in games. The licence stays valid after cancelling, as long as
  the audio was generated while subscribed.
- **Beta caveat:** output from features labelled beta/alpha "cannot be used for any commercial purpose or in any
  production environment". If Eleven v3 (the most expressive model, with audio tags like `[amused]` and
  `[whispers]`) still carries an alpha/beta label in the UI, use the newest non-beta model for shipped lines.
  Alternatively, keep the files in the non-commercial carve-out and check the current terms.
- **Voice Design (described in text), never a clone.** Example prompt: "Mature woman, low-mid register, regal and
  unhurried, intimate close-mic delivery, crisp formal diction, faintly amused, otherworldly calm, no accent
  caricature." Never upload or name Mara Sov or her actress.
- **Privacy:** ElevenLabs keeps a perpetual licence to use your inputs for training and improving its service.
  Don't upload anyone's real voice unless they consent. Terms also bar using outputs to train competing models.
  Note that purely AI audio may not be copyrightable, which is fine for us.
- Others (Hume, Cartesia, MiniMax Voice Design, Gradium) have similar "paid = commercial, free = non-commercial"
  terms, and none shows clear evidence of beating ElevenLabs on acted character delivery. Not worth new sign-ups
  tonight.

## What to send an actor (copy-paste kit)
1. **The script** as a numbered table: line key | line | one-word intent (e.g. `open-04 | ...Wait. I've had the
   funniest idea. | delighted`). Export it from `HELL-SCRIPT.md` with `[PLACEHOLDER]` removed. One file per line,
   named by key.
2. **The direction:** `HELL-BAJA-VOICE.md` from "Who she is" through "Mic and room". It describes qualities, not a
   person: regal, measured, intimate, darkly amused, never shouts, low-mid chest voice, pauses carry the jokes.
3. **References by quality, not imitation:** "Think the calm authority of a queen who has never needed to raise her
   voice. Please don't imitate any existing character or actor; we want *your* voice." Don't send Mara Sov clips:
   it invites an impression, and the repo is public.
4. **Tech spec:** 48 kHz/24-bit WAV, mono, dry (no reverb or effects), peaks around -10 dBFS, 1 s silence head and
   tail, 2 takes per line (one straight, one with more play), files named `open-04-take1.wav`.
5. **Licence and credit text** (the carve-out above), plus the line you'll put in the credits.
6. **Hard deadline** in their time zone, with a request to deliver 10 lines early. That lets you approve the read
   before they finish.

## Making her sound divine: the processing chain
How games and films do it: the performance carries ~90% of the effect, and processing frames it. The usual tools:
- **Unison doubles:** two copies a few cents sharp/flat, delayed 10-30 ms and panned apart, so one voice sounds like
  more than one.
- **A quiet sub-octave layer** (formant-preserved, low-passed) for weight.
- **A whispered take layered underneath**: ask the actor for a whispered pass of the key lines.
- **Reverse-reverb swells** that rise into a word, and a long dark tail.
- **Thirds and fifths in parallel:** the creator's harmony idea is real, but on speech a loud parallel third or fifth
  sounds like a harmonizer pedal or a chipmunk choir. Keep harmony layers very low (-24 to -30 dB), filtered to
  airy highs, or use them only on selected words ("Permanently.", "Ha!"). The octave-down plus the unison doubles do
  most of the work.

Tested 2026-10-18 on this machine (ffmpeg has `rubberband`, SoX is installed). Run per line, with a dry 48 kHz
WAV `in.wav` → `final.wav`:

```sh
ffmpeg -y -i in.wav -filter_complex "\
[0:a]aformat=sample_rates=48000:channel_layouts=mono,highpass=f=70,asplit=6[d][u1][u2][oc][fi][rv];\
[u1]rubberband=pitch=1.00405:formant=preserved,adelay=14:all=1,volume=-12dB[U1];\
[u2]rubberband=pitch=0.99481:formant=preserved,adelay=23:all=1,volume=-12dB[U2];\
[oc]rubberband=pitch=0.5:formant=preserved,lowpass=f=1200,adelay=6:all=1,volume=-20dB[OC];\
[fi]rubberband=pitch=1.4983:formant=preserved,highpass=f=2500,adelay=30:all=1,volume=-28dB[FI];\
[rv]areverse,aecho=0.8:0.6:70|150|260|420:0.5|0.4|0.3|0.2,areverse,lowpass=f=4000,volume=-20dB[RV];\
[d][U1][U2][OC][FI][RV]amix=inputs=6:normalize=0,alimiter=limit=0.9[out]" -map "[out]" layered.wav
sox layered.wav wet.wav channels 2 reverb 40 60 90 80 25 -8
ffmpeg -y -i wet.wav -af loudnorm=I=-18:TP=-1.5:LRA=11 -ar 48000 final.wav
```

What each layer does:
- `U1`/`U2`: doubles at +7 and -9 cents.
- `OC`: octave down.
- `FI`: a faint fifth up, air only. For a major third, swap `pitch=1.2599`.
- `RV`: a reverse-echo swell. It needs the 1 s of leading silence.
- `sox reverb`: the dark hall tail.
- `loudnorm`: -18 LUFS, matching the project's listening level.

Tune by ear: if she sounds robotic, lower `FI` and `OC` first. A whispered take can go in as a 7th input at -18 dB.
For batch runs or finer control, the same chain fits in Python with Spotify's `pedalboard` via `uv run`.

## Step by step for the creator (today)
1. **Now:** lock the 58 `GOD` lines (remove `[PLACEHOLDER]`, final wording). Approve the numbered script export.
2. **Now (5 min):** sign up for ElevenLabs Starter, design 3 voices from the prompt above, and generate 5 test
   lines. This is the safety net. Claude can process and import them.
3. **Before ~10:00 PKT:** on Fiverr, search "female villain voice actor video game" / "regal female character
   voice". Shortlist 3 sellers by **demo reel first**, then reviews, then "24 h", then the licence section. Message
   all three with the kit and the carve-out question. Ask for a free 1-2 line sample. Order from the first one who
   agrees and whose sample is right. Pay for express plus commercial/perpetual rights.
4. **In parallel (free):** post the same kit as a paid-gig call ("$X, 58 short lines, 24 h") on r/gameDevClassifieds
   and r/VoiceActing, and as a Casting Call Club project.
5. **When the first 10 lines land:** check the read. Send one round of notes if needed.
6. **On delivery (target 04:00 PKT on the 19th):** Claude runs the chain, levels the files, imports them and commits
   the processed files with `LICENSE-VOICE.txt` and the credit. Raw takes stay out of the repo.
7. **After the event:** if time allowed only the AI version, recast through Voices.com/Voice123 for a proper human
   version.

## Sources
- ElevenLabs, publishing and licence: https://elevenlabs.io/docs/help-center/legal/can-i-publish-the-content-i-generate-on-the-platform
- ElevenLabs terms summary (ownership, training licence): https://terms.law/ai-output-rights/elevenlabs/
- ElevenLabs 2026 prices (third-party; check the live page): https://magichour.ai/blog/elevenlabs-pricing , https://bigvu.tv/blog/elevenlabs-pricing-2026-plans-credits-commercial-rights-api-costs/
- Fiverr commercial-use licence: https://help.fiverr.com/hc/en-us/articles/37555186939921--For-Commercial-Use-license-details
- Fiverr game-voice gigs (example prices and turnaround): https://fiverr.com/ivettexo/record-a-voice-over-for-your-character , https://fiverr.com/mythicavoices/record-a-videogame-or-animation-character-for-your-project , https://fiverr.com/voiceoverangela/record-animation-narration-voice-over , https://fiverr.com/genescribbles/do-a-villainess-voice-over
- Voices.com client pricing: https://www.voices.com/client/pricing
- Casting Call Club paid/unpaid examples: https://www.castingcall.club/projects/project-f-demo-paid , https://jagogames.itch.io/the-lost-delinquents-of-rollings-high/devlog/163449/voice-acting-casting-call-closed
- Voice actors and AI clauses: https://gamedeveloper.com/business/hi-rez-studios-is-allegedly-cloning-actors-voices-with-ai
- Layered pitch-shift choir texture: https://louiselawlor.blogs.lincoln.ac.uk/2016/05/17/pitch-shifting
