using System;
using System.Collections.Generic;

namespace JamKit.Skyline
{
    /// <summary>One spoken line or on-screen hint: its stable key (lane A's docs/HELL-SCRIPT.md id), who says it (a
    /// <see cref="HellSpeakers"/> id; <see cref="HellSpeakers.Hint"/> = on-screen only, never voiced) and the words.
    /// Hints may hold input tokens (<see cref="HellPromptText"/>).</summary>
    public sealed class HellLine
    {
        public string key = "", speaker = "", text = "";

        public HellLine() { }
        public HellLine(string key, string speaker, string text) { this.key = key; this.speaker = speaker; this.text = text; }
    }

    /// <summary>The speakers. The id names the voice folder (<c>Resources/SkylineAudio/hell-voice/&lt;id&gt;/&lt;key&gt;.wav</c>);
    /// the display name is the subtitle's prefix.</summary>
    public static class HellSpeakers
    {
        public const string Death = "god", Vesper = "vesper", Hint = "hint";

        /// <summary>The subtitle prefix. The God of Death has no name yet (docs/HELL-GAPS.md #28): change it here.</summary>
        public static string Display(string id)
        {
            switch (id)
            {
                case Death: return "BAJA";
                case Vesper: return "VESPER";
                default: return (id ?? "").ToUpperInvariant();
            }
        }
    }

    /// <summary>
    /// EVERY story-beat and tutorial line in one table (docs/HELL-BEATS.md lists where each plays). ALL LINES ARE
    /// PLACEHOLDERS drafted by an agent for the creator to rewrite (the creator writes all dialogue). Edit the words here;
    /// keep the keys stable (the voice files and docs/HELL-SCRIPT.md use them). The God of Death is flippant, bored and
    /// funny (she gave you the powers, so she teaches them); Vesper is hostile on floor 1, curious on floor 2, and sees
    /// herself in you on floor 3 (docs/HELL-DESIGN.md).
    /// </summary>
    public static class HellLines
    {
        public static readonly HellLine[] All =
        {
            // ---------------------------------------------------------------- Vesper's beats (PLACEHOLDER)
            // Floor 1, the Soul Lantern: her voice in it, hostile.
            new HellLine("beat.vesper.f1.lantern.1", HellSpeakers.Vesper, "Put that down. The lantern is older than your whole family, and it doesn't like your hands."),
            new HellLine("beat.vesper.f1.lantern.2", HellSpeakers.Vesper, "Whoever sent you, go back up and tell her the Evening Star is not for collecting."),
            // Floor 2: her deal and her daughter, Lucifer's boredom; curious.
            new HellLine("beat.vesper.f2.deal.1", HellSpeakers.Vesper, "Still coming? Hm. I made a deal like yours once. My daughter was dying, and he was so charming about it."),
            new HellLine("beat.vesper.f2.deal.2", HellSpeakers.Vesper, "She lived. Grew old. Died, long ago. He got bored of me in a century. So tell me: what are you down here for?"),
            // Floor 3, before the boss: she sees herself in you (the letterboxed beat, a short hold).
            new HellLine("beat.vesper.f3.mirror.1", HellSpeakers.Vesper, "Look at you. Someone you love, a deal you didn't read, and powers that aren't yours."),
            new HellLine("beat.vesper.f3.mirror.2", HellSpeakers.Vesper, "I know that face. I wore it for three hundred years. Come down, then. Show me how it ends."),

            // ---------------------------------------------------------------- the God of Death's tutorials (PLACEHOLDER)
            new HellLine("tut.shadowstep.1", HellSpeakers.Death, "The shadows are yours now. Don't make it weird. Step into that dark patch."),
            new HellLine("tut.shadowstep.hint", HellSpeakers.Hint, "HOLD {ShadowStep} TO AIM   RELEASE TO STEP   DARK GROUND ONLY"),
            new HellLine("tut.shadowstep.done", HellSpeakers.Death, "Look at you. A little shadow. Adorable."),

            new HellLine("tut.rewind.1", HellSpeakers.Death, "Ooh, that looked fatal. Take four seconds back. I have plenty."),
            new HellLine("tut.rewind.hint", HellSpeakers.Hint, "{Rewind}   REWIND 4 SECONDS   THEN HEAL A QUARTER"),
            new HellLine("tut.rewind.done", HellSpeakers.Death, "See? Time is cheap when it isn't yours."),

            new HellLine("tut.dilation.1", HellSpeakers.Death, "You're quick. Aim while you're quick and the world waits for you. Rude of it, really."),
            new HellLine("tut.dilation.hint", HellSpeakers.Hint, "AIM {Aim} WHILE MOVING FAST   TIME SLOWS WHILE THE GAUGE LASTS"),
            new HellLine("tut.dilation.done", HellSpeakers.Death, "Faster you go, slower it gets. Don't think about it too hard."),

            new HellLine("tut.lava.1", HellSpeakers.Death, "That's lava. It's hot. I'm told that's the main thing about it."),
            new HellLine("tut.lava.hint", HellSpeakers.Hint, "LAVA BURNS   SINK IN IT AND IT DRAGS YOU BACK"),

            new HellLine("tut.collapse.1", HellSpeakers.Death, "That floor is leaving. I'd leave too."),
            new HellLine("tut.collapse.hint", HellSpeakers.Hint, "CRACKED STONE SHAKES, THEN FALLS   KEEP MOVING"),

            new HellLine("tut.paths.1", HellSpeakers.Death, "Two ways through, always. Blue studs: safe, slow, boring. Ember stripes: fast. Guess which one I'm watching."),
            new HellLine("tut.paths.hint", HellSpeakers.Hint, "BLUE STUDS: THE SAFE WAY   EMBER STRIPES: THE FAST WAY"),

            new HellLine("tut.obj.SixSigils.1", HellSpeakers.Death, "Six sigils, in order, A to F. Hell loves paperwork."),
            new HellLine("tut.obj.SixSigils.hint", HellSpeakers.Hint, "TOUCH THE SIGILS IN ORDER, A TO F"),
            new HellLine("tut.obj.SoulLantern.1", HellSpeakers.Death, "Carry the lantern to the altar. No stepping while you hold it; it gets motion sick."),
            new HellLine("tut.obj.SoulLantern.hint", HellSpeakers.Hint, "CARRY THE LANTERN TO THE ALTAR   NO SHADOW STEP WHILE CARRYING"),
            new HellLine("tut.obj.BreakChains.1", HellSpeakers.Death, "Three anchors hold that gate. Shoot them. Breaking things, you'll be a natural."),
            new HellLine("tut.obj.BreakChains.hint", HellSpeakers.Hint, "SHOOT THE THREE CHAIN ANCHORS"),
            new HellLine("tut.obj.HoldBrand.1", HellSpeakers.Death, "Stand in the brand until it's set. Move fast inside it and it sets faster. I didn't design it."),
            new HellLine("tut.obj.HoldBrand.hint", HellSpeakers.Hint, "STAY IN THE BRAND   MOVING FAST CHARGES IT FASTER"),
            new HellLine("tut.obj.Collapse.1", HellSpeakers.Death, "Run. The floor is falling in behind you. That's the whole instruction."),
            new HellLine("tut.obj.Collapse.hint", HellSpeakers.Hint, "REACH THE EXIT BEFORE THE FLOOR FALLS"),
            new HellLine("tut.obj.OozeWells.1", HellSpeakers.Death, "Those wells keep spitting ooze. Stand by each one until it seals. Three of them."),
            new HellLine("tut.obj.OozeWells.hint", HellSpeakers.Hint, "STAND BY EACH WELL TO SEAL IT"),

            new HellLine("tut.skillpick.1", HellSpeakers.Death, "A little present for getting this far. Pick one. Quickly; I age. Technically."),
            new HellLine("tut.skillpick.hint", HellSpeakers.Hint, "PICK ONE   {kbm:CLICK OR 1 2 3|pad:D-PAD AND A}"),

            new HellLine("tut.job.1", HellSpeakers.Death, "Oh, while you're down there. A small favour. Off the main road. You don't have to. You should."),
            new HellLine("tut.job.hint", HellSpeakers.Hint, "A JOB FOR DEATH   OPTIONAL   OFF THE MAIN PATH"),
        };

        static Dictionary<string, HellLine> _byKey;

        /// <summary>The line for <paramref name="key"/>, or null.</summary>
        public static HellLine Get(string key)
        {
            if (key == null) return null;
            if (_byKey == null)
            {
                var d = new Dictionary<string, HellLine>(StringComparer.Ordinal);
                foreach (var l in All) d[l.key] = l;
                _byKey = d;
            }
            return _byKey.TryGetValue(key, out var found) ? found : null;
        }

        public static bool Has(string key) => Get(key) != null;

        /// <summary>How long a subtitle stays when there is no voice clip: a floor, then reading time (about 15 characters a
        /// second), capped. With a clip: its length plus a short tail, never shorter than the reading floor.</summary>
        public static float Seconds(string text, float clipSeconds = 0f)
        {
            int n = text != null ? text.Length : 0;
            float read = Math.Min(MaxSeconds, Math.Max(MinSeconds, 1.2f + n / 15f));
            return clipSeconds > 0f ? Math.Max(MinSeconds, clipSeconds + 0.35f) : read;
        }

        public const float MinSeconds = 2.2f, MaxSeconds = 7f;
    }
}
