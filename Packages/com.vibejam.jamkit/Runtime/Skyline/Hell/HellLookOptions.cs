using System;

namespace JamKit.Skyline
{
    /// <summary>The three art directions of the Hell look test (docs/HELL-ART-PASS.md section 4): A Inked Inferno, B Molten Grit,
    /// C Black and Ember. <see cref="None"/> leaves the grey-box scene as the builder made it.</summary>
    public enum HellLookDirection { None, A, B, C }

    /// <summary>The hostile outline in Hell: the kit's red-magenta, acid lime, soulfire (cold pale cyan-white), white-hot, or the
    /// pulse (lime whose brightness pulses, faster as the enemy winds up an attack).</summary>
    public enum HellOutlineVariant { Kit, Lime, Soulfire, WhiteHot, Pulse }

    /// <summary>The safe path's guide studs: the builder's cold blue, bone-white, or dim stone (if soulfire's cyan clashes).</summary>
    public enum HellStudColour { Blue, Bone, Stone }

    /// <summary>
    /// The look test's switches, parsed from the command line (pure, so it is tested without Unity):
    /// <c>-hellLook A|B|C</c>, <c>-hellOutline lime|soulfire|whitehot|pulse|kit</c>, <c>-hellStuds blue|bone|stone</c>,
    /// <c>-hellLookCapture &lt;dir&gt;</c> (the stills, then quit). A look with no outline named gets lime (the art pass's pick).
    /// </summary>
    public struct HellLookOptions
    {
        public const string LookSwitch = "-hellLook", OutlineSwitch = "-hellOutline", StudsSwitch = "-hellStuds", CaptureSwitch = "-hellLookCapture";

        public HellLookDirection look;
        public HellOutlineVariant outline;
        public HellStudColour studs;
        /// <summary>True when -hellOutline was given (otherwise the outline follows the look's default).</summary>
        public bool outlineGiven, studsGiven;
        public string captureDir;

        public bool Any => look != HellLookDirection.None || outlineGiven || studsGiven || captureDir != null;

        public static HellLookOptions Parse(string[] args)
        {
            var o = new HellLookOptions();
            if (args == null) return o;
            string look = Value(args, LookSwitch), outline = Value(args, OutlineSwitch), studs = Value(args, StudsSwitch);
            o.look = ParseLook(look);
            if (outline != null) { o.outline = ParseOutline(outline); o.outlineGiven = true; }
            else o.outline = DefaultOutline(o.look);
            if (studs != null) { o.studs = ParseStuds(studs); o.studsGiven = true; }
            o.captureDir = Value(args, CaptureSwitch);
            return o;
        }

        public static HellOutlineVariant DefaultOutline(HellLookDirection look) => look == HellLookDirection.None ? HellOutlineVariant.Kit : HellOutlineVariant.Lime;

        public static HellLookDirection ParseLook(string s)
        {
            switch ((s ?? "").Trim().ToUpperInvariant())
            {
                case "A": case "INK": case "INKED": return HellLookDirection.A;
                case "B": case "GRIT": case "MOLTEN": return HellLookDirection.B;
                case "C": case "BLACK": case "EMBER": return HellLookDirection.C;
                default: return HellLookDirection.None;
            }
        }

        public static HellOutlineVariant ParseOutline(string s)
        {
            switch ((s ?? "").Trim().ToLowerInvariant().Replace("-", "").Replace("_", ""))
            {
                case "lime": return HellOutlineVariant.Lime;
                case "soulfire": case "cyan": return HellOutlineVariant.Soulfire;
                case "whitehot": case "white": return HellOutlineVariant.WhiteHot;
                case "pulse": return HellOutlineVariant.Pulse;
                default: return HellOutlineVariant.Kit;
            }
        }

        public static HellStudColour ParseStuds(string s)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "bone": case "bonewhite": return HellStudColour.Bone;
                case "stone": case "dim": return HellStudColour.Stone;
                default: return HellStudColour.Blue;
            }
        }

        /// <summary>The outline's hue as sRGB 0..1 (r, g, b), and the composite's mode (0 off = the kit's hue, 1 steady, 2 pulse).</summary>
        public static (float r, float g, float b, int mode) Hue(HellOutlineVariant v)
        {
            switch (v)
            {
                case HellOutlineVariant.Lime: return (0xA6 / 255f, 1f, 0f, 1);              // #A6FF00
                case HellOutlineVariant.Soulfire: return (0xC8 / 255f, 0xF4 / 255f, 1f, 1);  // #C8F4FF cold pale cyan-white
                case HellOutlineVariant.WhiteHot: return (1f, 0xF6 / 255f, 0xE8 / 255f, 1);  // #FFF6E8 near-white, a touch warm
                case HellOutlineVariant.Pulse: return (0xA6 / 255f, 1f, 0f, 2);             // lime, pulsing
                default: return (1f, 0x2E / 255f, 0x63 / 255f, 0);                          // #FF2E63, the kit's
            }
        }

        /// <summary>The pulse's rate in Hz for a wind-up of 0 (idle) .. 1 (about to attack): 1 Hz up to 6 Hz (the composite's
        /// formula, kept here so a test pins it).</summary>
        public static float PulseHz(float windup) => 1f + 5f * Math.Max(0f, Math.Min(1f, windup));

        /// <summary>Which floor (0..2) a world position belongs to: the floors are rows <see cref="HellWorld.FloorSpacing"/> apart in z.</summary>
        public static int FloorAt(float worldZ)
        {
            int f = (int)Math.Round(worldZ / HellWorld.FloorSpacing);
            return Math.Max(0, Math.Min(HellRoomVariants.FloorCount - 1, f));
        }

        static string Value(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }
    }
}
