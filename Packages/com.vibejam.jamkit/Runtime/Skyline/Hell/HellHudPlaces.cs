using System;
using System.Collections.Generic;

namespace JamKit.Skyline
{
    /// <summary>
    /// The Hell UI's faces by role (docs/HELL-UI.md, the creator's picks 2026-10-18). The names are keys into the HUD's face
    /// table (<c>SkylineHudStyle.Faces</c>); the files live under <c>ThirdParty/Fonts/Resources/SkylineFonts/</c>.
    /// </summary>
    public static class HellFaces
    {
        /// <summary>The game's title only (the title screen, the credits' first line).</summary>
        public const string Title = "CinzelDecorative";
        /// <summary>Important subtext: room and floor banners, objective lines, job card titles, ability names, the decision
        /// prompt, menu items, screen headings.</summary>
        public const string Heading = "MetalMania";
        /// <summary>Non-game text: legal and credits small print, settings and their descriptions, control hints on screens.</summary>
        public const string Fine = "GrenzeGotisch";
        /// <summary>Numbers (ammo, clocks, stats) and the tutorial hint cards: kept from Skyline.</summary>
        public const string Numbers = "LuckiestGuy";
        /// <summary>The comic lettering's second face (hit words and callouts), kept from Skyline with Luckiest Guy.</summary>
        public const string Comic = "Bangers";
    }

    /// <summary>A rectangle in panel units (y down), for the layout rules.</summary>
    public struct HellRect
    {
        public float x, y, w, h;
        public HellRect(float x, float y, float w, float h) { this.x = x; this.y = y; this.w = w; this.h = h; }
        public float Right => x + w;
        public float Bottom => y + h;
        public bool Overlaps(HellRect o) => x < o.Right && o.x < Right && y < o.Bottom && o.y < Bottom;
        public bool Inside(float panelW, float panelH) => x >= 0f && y >= 0f && Right <= panelW + 0.01f && Bottom <= panelH + 0.01f;
        public override string ToString() => "(" + x.ToString("0") + "," + y.ToString("0") + " " + w.ToString("0") + "x" + h.ToString("0") + ")";
    }

    public enum HellHAlign { Left, Centre, Right }
    public enum HellVAlign { Top, Middle, Bottom }

    /// <summary>One HUD element's place: anchored to an edge (or the middle) of the safe area, with offsets in panel units and
    /// optional shares of the panel's size (<see cref="yShare"/> of the height from the top, <see cref="wShare"/> of the
    /// width added to <see cref="w"/>).</summary>
    public struct HellHudBox
    {
        public string name;
        public HellHAlign h;
        public HellVAlign v;
        public float x, y, w, hgt, yShare, wShare;

        public HellHudBox(string name, HellHAlign h, HellVAlign v, float x, float y, float w, float hgt, float yShare = 0f, float wShare = 0f)
        {
            this.name = name; this.h = h; this.v = v; this.x = x; this.y = y; this.w = w; this.hgt = hgt; this.yShare = yShare; this.wShare = wShare;
        }

        public float Width(float panelW) => w + wShare * panelW;

        public HellRect Resolve(float panelW, float panelH)
        {
            float ww = Width(panelW);
            float left = h == HellHAlign.Left ? x : h == HellHAlign.Right ? panelW - x - ww : panelW * 0.5f - ww * 0.5f + x;
            float top = v == HellVAlign.Top ? y + yShare * panelH : v == HellVAlign.Bottom ? panelH - y - hgt : panelH * 0.5f - hgt * 0.5f + y;
            return new HellRect(left, top, ww, hgt);
        }
    }

    /// <summary>What is on screen.</summary>
    public enum HellHudState { Play, Job, BeatHold, Boss, Decision, Escape, Wheel }

    /// <summary>
    /// Where every Hell HUD element sits (docs/HELL-UI.md), in one place: the HUD code reads these numbers, and the tests check
    /// that nothing that can show at the same time overlaps, at 16:9 (1080p and 1440p are the same panel: the HUD scales
    /// with the screen from a 1920 x 1080 reference), 16:10 and 21:9. Panel units, y down, inside the safe area.
    /// </summary>
    public static class HellHudPlaces
    {
        public const float RefW = 1920f, RefH = 1080f, Margin = 40f;

        // ---- the corners: health bottom left, ammo and abilities bottom right, the hour top right, favours top left
        public static readonly HellHudBox Vessel = new HellHudBox("vessel", HellHAlign.Left, HellVAlign.Bottom, Margin - 12f, Margin - 6f, 420f, 92f);
        public static readonly HellHudBox Ammo = new HellHudBox("ammo", HellHAlign.Right, HellVAlign.Bottom, Margin, Margin, 340f, 150f);
        /// <summary>The ability tiles (Nightfall, Time Dilation, Shadow Step, Rewind; 60 x 88 each), small, LEFT of the ammo
        /// counter and gun icon, bottom-aligned with it (the creator, 2026-10-18: "smaller, to the left of the gun ammo counter
        /// and icon, instead of above it"). Under the subtitles' bottom edge, so the two never meet.</summary>
        public const float AbilityTileW = 60f, AbilityGap = 14f;
        public static readonly HellHudBox Abilities = new HellHudBox("abilities", HellHAlign.Right, HellVAlign.Bottom, Margin + 340f + AbilityGap, Margin, 4f * AbilityTileW, 88f);
        /// <summary>A chip per owned floor skill, in a row over the ammo counter (where the tiles used to be).</summary>
        public static readonly HellHudBox SkillChips = new HellHudBox("skill-chips", HellHAlign.Right, HellVAlign.Bottom, Margin, Margin + 150f + 8f, 330f, 26f);
        public static readonly HellHudBox Hourglass = new HellHudBox("hourglass", HellHAlign.Right, HellVAlign.Top, Margin, 30f, 130f, 170f);
        public static readonly HellHudBox JobTracker = new HellHudBox("job-tracker", HellHAlign.Left, HellVAlign.Top, Margin, 34f, 480f, 60f);
        public static readonly HellHudBox JobCard = new HellHudBox("job-card", HellHAlign.Left, HellVAlign.Top, Margin, 108f, 480f, 200f);

        // ---- top centre: the room's objective, its clock under it, the boss bar under that
        public static readonly HellHudBox Objective = new HellHudBox("objective", HellHAlign.Centre, HellVAlign.Top, 0f, 32f, 760f, 48f);
        public static readonly HellHudBox RoomClock = new HellHudBox("room-clock", HellHAlign.Centre, HellVAlign.Top, 0f, 82f, 200f, 40f);
        /// <summary>The active job's one-line instruction ("FAVOUR: ..."), under the objective line and its clock, for as long
        /// as the job is live (two lines at most). The boss floor has no jobs, so it never meets Vesper's bar.</summary>
        public static readonly HellHudBox JobLine = new HellHudBox("job-line", HellHAlign.Centre, HellVAlign.Top, 0f, 124f, 760f, 62f);
        public static readonly HellHudBox VesperBar = new HellHudBox("vesper-bar", HellHAlign.Centre, HellVAlign.Top, 0f, 124f, 660f, 96f);

        // ---- the middle band: banners above, the crosshair and its rings, the decision prompt and the hint card below
        public static readonly HellHudBox Banner = new HellHudBox("banner", HellHAlign.Centre, HellVAlign.Top, 0f, 0f, 1100f, 96f, yShare: 0.30f);
        public static readonly HellHudBox Crosshair = new HellHudBox("crosshair-rings", HellHAlign.Centre, HellVAlign.Middle, 0f, 0f, 180f, 180f);
        public static readonly HellHudBox Prompt = new HellHudBox("decision-prompt", HellHAlign.Centre, HellVAlign.Top, 0f, 0f, 1100f, 44f, yShare: 0.60f);
        public static readonly HellHudBox Hint = new HellHudBox("hint-card", HellHAlign.Centre, HellVAlign.Top, 0f, 0f, 1100f, 44f, yShare: 0.655f);
        public static readonly HellHudBox Wheel = new HellHudBox("ability-wheel", HellHAlign.Centre, HellVAlign.Middle, 0f, 0f, 440f, 440f);

        // ---- the bottom: subtitles over the letterbox
        public static readonly HellHudBox Subtitles = new HellHudBox("subtitles", HellHAlign.Centre, HellVAlign.Bottom, 0f, 132f, 0f, 110f, wShare: 0.50f);
        /// <summary>The beat hold's bars, as a share of the panel's height (<c>HellLinePlayer.LetterboxPercent</c> / 100).</summary>
        public const float LetterboxShare = 0.11f;

        /// <summary>The share of the screen's height the hint card's top sits at (for code that places it in percent).</summary>
        public static float HintTopPercent => Hint.yShare * 100f;
        public static float PromptTopPercent => Prompt.yShare * 100f;
        public static float BannerTopPercent => Banner.yShare * 100f;

        static HellHudBox LetterTop(float panelH) => new HellHudBox("letterbox-top", HellHAlign.Left, HellVAlign.Top, 0f, 0f, 0f, LetterboxShare * panelH, wShare: 1f);
        static HellHudBox LetterBottom(float panelH) => new HellHudBox("letterbox-bottom", HellHAlign.Left, HellVAlign.Bottom, 0f, 0f, 0f, LetterboxShare * panelH, wShare: 1f);

        /// <summary>The panel's size for a screen (scale-with-screen-size from the 1920 x 1080 reference, width/height match
        /// 0.5, as the HUD's panel settings).</summary>
        public static void Panel(float screenW, float screenH, out float panelW, out float panelH, float match = 0.5f)
        {
            double lw = Math.Log(screenW / RefW, 2), lh = Math.Log(screenH / RefH, 2);
            float scale = (float)Math.Pow(2, lw + (lh - lw) * match);
            panelW = screenW / scale;
            panelH = screenH / scale;
        }

        /// <summary>The elements that can show together in a state.</summary>
        public static List<HellHudBox> In(HellHudState s, float panelH)
        {
            var l = new List<HellHudBox>();
            switch (s)
            {
                case HellHudState.Play:
                case HellHudState.Job:
                case HellHudState.Escape:
                    l.AddRange(new[] { Vessel, Ammo, Abilities, SkillChips, Hourglass, Objective, RoomClock, Crosshair, Banner, Hint, Subtitles });
                    if (s != HellHudState.Escape) l.Add(JobTracker);
                    if (s == HellHudState.Job) { l.Add(JobCard); l.Add(JobLine); }
                    break;
                case HellHudState.BeatHold:
                    // the bars cover the corners; the subtitles, the banner and the crosshair must stay clear of them
                    l.AddRange(new[] { LetterTop(panelH), LetterBottom(panelH), Subtitles, Banner, Crosshair });
                    break;
                case HellHudState.Boss:
                    l.AddRange(new[] { Vessel, Ammo, Abilities, SkillChips, Hourglass, Objective, RoomClock, VesperBar, Crosshair, Banner, Hint, Subtitles });
                    break;
                case HellHudState.Decision:
                    l.AddRange(new[] { Vessel, Ammo, Abilities, SkillChips, Hourglass, Objective, Crosshair, Prompt, Hint, Subtitles });
                    break;
                case HellHudState.Wheel:
                    // banners and cards in the middle hide while the wheel is open (HellAbilityWheel.IsOpen)
                    l.AddRange(new[] { Vessel, Ammo, Abilities, SkillChips, Hourglass, Objective, RoomClock, JobTracker, JobCard, VesperBar, Subtitles, Wheel });
                    break;
            }
            return l;
        }

        /// <summary>Every pair that overlaps, and every element outside the panel, in a state (empty = the layout holds).</summary>
        public static List<string> Problems(HellHudState s, float panelW, float panelH)
        {
            var boxes = In(s, panelH);
            var rects = new HellRect[boxes.Count];
            var bad = new List<string>();
            for (int i = 0; i < boxes.Count; i++)
            {
                rects[i] = boxes[i].Resolve(panelW, panelH);
                if (!rects[i].Inside(panelW, panelH)) bad.Add(s + ": " + boxes[i].name + " " + rects[i] + " is off the " + panelW.ToString("0") + "x" + panelH.ToString("0") + " panel");
            }
            for (int i = 0; i < boxes.Count; i++)
                for (int j = i + 1; j < boxes.Count; j++)
                {
                    bool bars = boxes[i].name.StartsWith("letterbox", StringComparison.Ordinal) && boxes[j].name.StartsWith("letterbox", StringComparison.Ordinal);
                    if (!bars && rects[i].Overlaps(rects[j])) bad.Add(s + ": " + boxes[i].name + " " + rects[i] + " overlaps " + boxes[j].name + " " + rects[j]);
                }
            return bad;
        }
    }

    /// <summary>Clock text without garbage: "m:ss" strings made once per second value and kept (a frame never allocates once
    /// a value has been seen).</summary>
    public static class HellClockText
    {
        const int Max = 100 * 60;
        static readonly string[] Cache = new string[Max];

        public static string Of(float seconds) => Of(seconds > 0f ? (int)seconds : 0);

        public static string Of(int seconds)
        {
            if (seconds < 0) seconds = 0;
            if (seconds >= Max) seconds = Max - 1;
            return Cache[seconds] ?? (Cache[seconds] = (seconds / 60) + ":" + (seconds % 60).ToString("00"));
        }
    }

    /// <summary>The health vessel's look as numbers (docs/HELL-UI.md): cracks open across the obsidian as health is lost, and
    /// the ember inside throbs when it runs low.</summary>
    public static class HellVesselModel
    {
        public const int MaxCracks = 5;
        /// <summary>Under this share of health the ember throbs and the screen's edges tint (the Skyline low-chassis rule).</summary>
        public const float LowShare = 0.35f;

        /// <summary>How many cracks show: one more for every sixth of the health lost, up to <see cref="MaxCracks"/>.</summary>
        public static int Cracks(float healthShare)
        {
            if (float.IsNaN(healthShare)) return 0;
            float lost = 1f - Math.Max(0f, Math.Min(1f, healthShare));
            return Math.Min(MaxCracks, (int)Math.Floor(lost * 6f + 1e-4f));
        }
    }
}

namespace JamKit.Skyline
{
    /// <summary>The active job's lingering instruction under the objective line (docs/HELL-JOBS.md, "The HUD"; the creator,
    /// 2026-10-18: "I missed the message, busy shooting"). Made once per offer, never per frame.</summary>
    public static class HellJobLineText
    {
        public const string Prefix = "FAVOUR:  ";
        public static string Of(string objective) => string.IsNullOrEmpty(objective) ? "" : Prefix + objective;
    }

    /// <summary>The small ability icons' numbers (docs/HELL-UI.md, "The abilities"): the Time Dilation fuel gauge's start
    /// mark and the Shadow Step charge pips.</summary>
    public static class HellAbilityIcons
    {
        /// <summary>Time Dilation needs this share of its fuel to start (the fuel-gauge lane, 2026-10-18); the gauge marks it.</summary>
        public const float DilationStartShare = 0.25f;

        public static bool CanStartDilation(float fuel01) => fuel01 >= DilationStartShare;

        /// <summary>Pip <paramref name="i"/>'s fill: full for each charge held, the next one filling by
        /// <paramref name="fraction"/>, the rest empty.</summary>
        public static float Pip(int i, int charges, float fraction) =>
            i < charges ? 1f : i == charges ? Math.Max(0f, Math.Min(1f, fraction)) : 0f;

        /// <summary>Every pip's fill (for tests; the HUD calls <see cref="Pip"/>).</summary>
        public static float[] Pips(int charges, int max, float fraction)
        {
            var a = new float[Math.Max(0, max)];
            for (int i = 0; i < a.Length; i++) a[i] = Pip(i, charges, fraction);
            return a;
        }
    }
}
