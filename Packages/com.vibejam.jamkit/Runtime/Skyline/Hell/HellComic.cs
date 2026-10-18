using System;
using System.Collections.Generic;

namespace JamKit.Skyline
{
    // =====================================================================================================================
    // THE COMIC FLIP-THROUGH (docs/HELL-COMIC.md): the opening and every ending as comic pages instead of text cards. Pure
    // data and layout maths (no engine); the Unity side is Scripts/Hell/HellComicPlayer.cs.
    // =====================================================================================================================

    /// <summary>How a line sits in a panel: a caption box (narration, the epilogue) or a speech balloon.</summary>
    public enum HellComicLineKind { Caption, Balloon }

    /// <summary>Where a line sits in its panel. <see cref="Auto"/>: captions top left; balloons top right, then bottom left
    /// (bottom right first when the panel has a caption).</summary>
    public enum HellComicSpot { Auto, TopLeft, TopRight, BottomLeft, BottomRight }

    /// <summary>One caption or balloon: who says it (the voice folder: god, vesper, narrator; "you", "card" or null are text
    /// only), the name over it (null: none), the words, and the voice key
    /// (Resources/SkylineAudio/hell-voice/SPEAKER/KEY; null: no voice).</summary>
    public sealed class HellComicLine
    {
        public HellComicLineKind kind;
        public string speaker, name, text, voice;
        public HellComicSpot spot;

        public static HellComicLine Caption(string text, string speaker = null, string voice = null, HellComicSpot spot = HellComicSpot.Auto) =>
            new HellComicLine { kind = HellComicLineKind.Caption, text = text, speaker = speaker, voice = voice, spot = spot };

        public static HellComicLine Balloon(string speaker, string name, string text, string voice, HellComicSpot spot = HellComicSpot.Auto) =>
            new HellComicLine { kind = HellComicLineKind.Balloon, speaker = speaker, name = name, text = text, voice = voice, spot = spot };

        /// <summary>A voice folder exists for the speaker and the line names a key.</summary>
        public bool Voiced => !string.IsNullOrEmpty(voice) && (speaker == "god" || speaker == "vesper" || speaker == "narrator");
    }

    /// <summary>One panel: its picture (Resources/HellComic/IMAGE.jpg, e.g. "opening/1-1"), what it shows (drawn on a dark
    /// ink placeholder when the picture is missing), its lines in order, and how long it holds after its last line. A
    /// <see cref="title"/> makes it a splash: the words big over the picture (the ending's name).</summary>
    public sealed class HellComicPanel
    {
        public string image, description;
        /// <summary>A picture to use when <see cref="image"/> is missing (a splash falls back to its ending's epilogue panel).</summary>
        public string fallbackImage;
        public List<HellComicLine> lines = new List<HellComicLine>();
        /// <summary>Seconds it stays after its last line (or alone, when it has none).</summary>
        public float hold = 0.8f;
        /// <summary>The crop's centre in the picture (0..1): what stays in frame when the panel is narrower or wider.</summary>
        public float focusX = 0.5f, focusY = 0.5f;
        /// <summary>A splash's words (the ending's name), drawn big and fitted to the screen.</summary>
        public string title;

        public bool Splash => !string.IsNullOrEmpty(title);

        public HellComicPanel(string image, string description, float hold = 0.8f)
        {
            this.image = image; this.description = description; this.hold = hold;
        }

        public HellComicPanel Say(HellComicLine line) { lines.Add(line); return this; }
        public HellComicPanel Focus(float x, float y) { focusX = x; focusY = y; return this; }
    }

    /// <summary>One page: 1 to 4 panels (<see cref="HellComicLayout"/> places them). <see cref="music"/>: the opening's music
    /// part for this page (the manifest's baja_opening parts), or null.</summary>
    public sealed class HellComicPage
    {
        public readonly List<HellComicPanel> panels = new List<HellComicPanel>();
        public string music;

        public HellComicPage(params HellComicPanel[] p) { panels.AddRange(p); }
    }

    /// <summary>A sequence: its id (the events carry it) and its pages, read in order.</summary>
    public sealed class HellComicSequence
    {
        public string id;
        public readonly List<HellComicPage> pages = new List<HellComicPage>();

        public HellComicSequence(string id) { this.id = id; }
        public HellComicSequence Page(HellComicPage p) { pages.Add(p); return this; }

        public int PanelCount
        {
            get { int n = 0; foreach (var p in pages) n += p.panels.Count; return n; }
        }

        /// <summary>Every picture the sequence asks for (Resources/HellComic keys), in reading order.</summary>
        public IEnumerable<string> Images()
        {
            foreach (var p in pages) foreach (var pn in p.panels) if (!string.IsNullOrEmpty(pn.image)) yield return pn.image;
        }
    }

    /// <summary>A rectangle in 0..1 of the page's area (y down).</summary>
    public struct HellComicRect
    {
        public float x, y, w, h;
        public HellComicRect(float x, float y, float w, float h) { this.x = x; this.y = y; this.w = w; this.h = h; }
        public float Right => x + w;
        public float Bottom => y + h;
        public bool Overlaps(HellComicRect o) => x < o.Right - 1e-4f && o.x < Right - 1e-4f && y < o.Bottom - 1e-4f && o.y < Bottom - 1e-4f;
    }

    /// <summary>Where a page's panels go (fractions of the page; gutters between them).</summary>
    public static class HellComicLayout
    {
        public const int MaxPanels = 4;
        /// <summary>The staggered two-panel page: each panel's share of the page (they overlap by 2 x this - 1).</summary>
        public const float StaggerSize = 0.6f;

        public static HellComicRect[] Rects(int count, float gutterX = 0.016f, float gutterY = 0.028f)
        {
            float gx = gutterX * 0.5f, gy = gutterY * 0.5f;
            switch (count <= 0 ? 1 : count > MaxPanels ? MaxPanels : count)
            {
                case 1: return new[] { new HellComicRect(0f, 0f, 1f, 1f) };
                case 2:   // staggered: the second laps over the first's lower right corner (both keep the art's 16:9)
                    return new[] { new HellComicRect(0f, 0f, StaggerSize, StaggerSize), new HellComicRect(1f - StaggerSize, 1f - StaggerSize, StaggerSize, StaggerSize) };
                case 3:   // a tall panel at the left, two stacked at the right
                    return new[]
                    {
                        new HellComicRect(0f, 0f, 0.52f - gx, 1f),
                        new HellComicRect(0.52f + gx, 0f, 0.48f - gx, 0.5f - gy),
                        new HellComicRect(0.52f + gx, 0.5f + gy, 0.48f - gx, 0.5f - gy),
                    };
                default:  // a two-by-two grid
                    return new[]
                    {
                        new HellComicRect(0f, 0f, 0.5f - gx, 0.5f - gy), new HellComicRect(0.5f + gx, 0f, 0.5f - gx, 0.5f - gy),
                        new HellComicRect(0f, 0.5f + gy, 0.5f - gx, 0.5f - gy), new HellComicRect(0.5f + gx, 0.5f + gy, 0.5f - gx, 0.5f - gy),
                    };
            }
        }

        /// <summary>The spot a line takes in its panel (its index among the panel's balloons; whether the panel has a caption;
        /// whether its lower right corner is under the next panel: then it never goes there).</summary>
        public static HellComicSpot Spot(HellComicLine line, int balloonIndex, bool panelHasCaption, bool coveredCorner = false)
        {
            var s = AutoSpot(line, balloonIndex, panelHasCaption);
            return coveredCorner && s == HellComicSpot.BottomRight ? HellComicSpot.BottomLeft : s;
        }

        /// <summary>The first panel of a staggered two-panel page has its lower right corner under the second.</summary>
        public static bool CornerCovered(int panelIndex, int panelCount) => panelCount == 2 && panelIndex == 0;

        static HellComicSpot AutoSpot(HellComicLine line, int balloonIndex, bool panelHasCaption)
        {
            if (line.spot != HellComicSpot.Auto) return line.spot;
            if (line.kind == HellComicLineKind.Caption) return HellComicSpot.TopLeft;
            if (panelHasCaption) return balloonIndex % 2 == 0 ? HellComicSpot.BottomRight : HellComicSpot.TopRight;
            return balloonIndex % 2 == 0 ? HellComicSpot.TopRight : HellComicSpot.BottomLeft;
        }

        /// <summary>A picture's size inside a panel so it covers it with room to drift (<paramref name="overscan"/>), and its
        /// offset so the focus point stays in frame: (w, h, left, top) in the panel's units.</summary>
        public static (float w, float h, float left, float top) Cover(float panelW, float panelH, float imageAspect, float focusX, float focusY, float overscan = 1.08f)
        {
            if (imageAspect <= 0f) imageAspect = 16f / 9f;
            float w, h;
            if (panelW / Math.Max(1f, panelH) > imageAspect) { w = panelW * overscan; h = w / imageAspect; }
            else { h = panelH * overscan; w = h * imageAspect; }
            float left = (panelW - w) * Clamp01(focusX);
            float top = (panelH - h) * Clamp01(focusY);
            return (w, h, left, top);
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }

    /// <summary>Text fitting (a conservative estimate for the display faces; the player also shrinks a box that measures
    /// taller than its room). No caption, balloon or title may run off the screen.</summary>
    public static class HellComicFit
    {
        /// <summary>A glyph's advance as a fraction of the font size (Metal Mania; capitals and digits are wider).</summary>
        public const float CharWidth = 0.56f, CapsWidth = 0.74f, LineHeight = 1.3f;

        /// <summary>How many lines the text wraps to in the width at the size; -1 when a single word is wider than the box
        /// (it would overflow, not wrap).</summary>
        public static int Lines(string text, float width, float size, float letterSpacing = 0f)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int lines = 0;
            foreach (var para in text.Split('\n'))
            {
                lines++;
                float x = 0f;
                foreach (var word in para.Split(' '))
                {
                    if (word.Length == 0) continue;
                    float ww = WordWidth(word, size, letterSpacing);
                    if (ww > width) return -1;
                    float space = size * CharWidth * 0.6f + letterSpacing;
                    if (x > 0f && x + space + ww > width) { lines++; x = ww; }
                    else x += (x > 0f ? space : 0f) + ww;
                }
            }
            return lines;
        }

        public static float WordWidth(string word, float size, float letterSpacing = 0f)
        {
            float w = 0f;
            foreach (char c in word) w += size * (char.IsUpper(c) || char.IsDigit(c) ? CapsWidth : CharWidth) + letterSpacing;
            return w;
        }

        /// <summary>The largest size from max down to min at which the text fits the box (word-wrapped; letter spacing as a
        /// fraction of the size). Below min it returns min (the player then shrinks on measure).</summary>
        public static float Size(string text, float width, float height, float max, float min, float letterSpacingEm = 0f)
        {
            for (float s = max; s >= min; s -= Math.Max(0.5f, s * 0.04f))
            {
                int n = Lines(text, width, s, letterSpacingEm * s);
                if (n >= 0 && n * s * LineHeight <= height) return s;
            }
            return min;
        }
    }

    /// <summary>
    /// The game's comic sequences: the opening (the premise art the creator picked) and every ending (art arriving as
    /// hell-end-*.jpg; until then each panel is a dark ink placeholder with its description). The words are the placeholder
    /// script's (<see cref="HellFlowText.Opening"/>, <see cref="HellEndingLines"/>); the voices are the existing hell-voice
    /// files.
    /// </summary>
    public static class HellComics
    {
        public const string OpeningId = "opening";
        public const string Root = "HellComic/";

        /// <summary>The opening's music parts by page (docs/HELL-MUSIC-DIRECTOR.md: sister, hall, wait then idea).</summary>
        public const string MusicSister = "sister", MusicHall = "hall", MusicWait = "wait", MusicIdea = "idea";

        static string God => HellEndingLines.God;
        static string Baja => HellEndingLines.Display(HellEndingLines.God);

        /// <summary>The opening: your sister, Baja's hall, the funniest idea, the way down.</summary>
        public static HellComicSequence Opening()
        {
            var o = HellFlowText.Opening;
            string T(int i) => i < o.Length ? o[i].text : "";
            var s = new HellComicSequence(OpeningId);
            s.Page(new HellComicPage(
                new HellComicPanel("opening/1-1", "Your sister's hand, still, on the sheet.").Say(HellComicLine.Caption(T(0), "narrator", "open-01")),
                new HellComicPanel("opening/1-2", "You in the doorway, not grieving: furious.", 1.0f).Say(HellComicLine.Caption(T(1), "narrator", "open-02")))
            { music = MusicSister });
            s.Page(new HellComicPage(
                new HellComicPanel("opening/2-1", "Baja, the God of Death, in her hall.", 0.6f).Focus(0.5f, 0.4f)
                    .Say(HellComicLine.Balloon(God, Baja, T(2), "open-03")))
            { music = MusicHall });
            s.Page(new HellComicPage(
                new HellComicPanel("opening/3-1", "Baja leans in: she's had the funniest idea.", 0.5f)
                    .Say(HellComicLine.Balloon(God, Baja, T(3), "open-04"))
                    .Say(HellComicLine.Balloon(God, Baja, T(4), "open-05")))
            { music = MusicWait });
            s.Page(new HellComicPage(
                new HellComicPanel("opening/4-1", "The gate into Hell opens under you.", 0.4f).Say(HellComicLine.Balloon(God, Baja, T(5), "open-06")),
                new HellComicPanel("opening/4-2", "You fall into the dark, toward the embers.", 1.4f))
            { music = MusicIdea });
            return s;
        }

        /// <summary>The opening's music part for a page (null: keep what plays).</summary>
        public static string OpeningMusic(int page)
        {
            var s = Opening();
            return page >= 0 && page < s.pages.Count ? s.pages[page].music : null;
        }

        /// <summary>The Lucifer cliffhanger's art (its pages follow the base ending's).</summary>
        public const string LuciferArt = "lucifer";

        /// <summary>Every ending art name (the folder-less file prefix under endings/: ART-PAGE-PANEL, ART-title).</summary>
        public static readonly string[] EndingArt = { HellEndings.Kill, HellEndings.SpareKept, HellEndings.SpareCost, HellEndings.Damned, LuciferArt };

        /// <summary>The ending's comic: two staggered pages of the ending's four panels (ART-1-1 .. ART-1-4: the choice, the
        /// escape, then Baja's two lines in balloons and the epilogue as a caption), the Lucifer pages after them for the
        /// cliffhanger forms, then the ending's name as a splash (ART-title, else the epilogue's panel, darkened). Built from
        /// <see cref="HellHourEndings.Sequence"/>'s keys, so it follows the script.</summary>
        public static HellComicSequence Ending(string endingId)
        {
            var s = new HellComicSequence("ending-" + (endingId ?? ""));
            string baseArt = BaseArt(endingId);
            var keys = HellHourEndings.Sequence(endingId);
            if (baseArt != null && keys.Length >= 3) AddEndingPages(s, baseArt, keys[0], keys[1], keys[2]);
            if (keys.Length >= 6) AddEndingPages(s, LuciferArt, keys[3], keys[4], keys[5]);
            string splashArt = keys.Length >= 6 ? LuciferArt : baseArt ?? "none";
            var splash = new HellComicPanel("endings/" + splashArt + "-title", "The ending's name over " + Describe(splashArt, 0), 3.2f)
            {
                title = HellEndings.Title(endingId),
                fallbackImage = "endings/" + splashArt + "-1-" + EpiloguePanel(splashArt),
            };
            s.Page(new HellComicPage(splash));
            return s;
        }

        /// <summary>Which of an ending's four panels carries Baja's two lines, and which the epilogue (the art's order: the
        /// kill's third panel is your sister home and its fourth Baja; the others have Baja third).</summary>
        public static int BajaPanel(string art) => art == HellEndings.Kill ? 4 : 3;
        public static int EpiloguePanel(string art) => art == HellEndings.Kill ? 3 : 4;

        /// <summary>The art of an ending id (kill, spare-kept, spare-cost, damned), or null for one with no scene.</summary>
        public static string BaseArt(string endingId)
        {
            switch (endingId)
            {
                case HellEndings.Kill: case HellEndings.KillLucifer: return HellEndings.Kill;
                case HellEndings.SpareKept: case HellEndings.SpareLucifer: return HellEndings.SpareKept;
                case HellEndings.SpareCost: return HellEndings.SpareCost;
                case HellEndings.Damned: return HellEndings.Damned;
                default: return null;
            }
        }

        static void AddEndingPages(HellComicSequence s, string art, string line1, string line2, string card)
        {
            var panels = new HellComicPanel[4];
            for (int i = 0; i < 4; i++) panels[i] = new HellComicPanel("endings/" + art + "-1-" + (i + 1), Describe(art, i + 1), 2.0f);
            var baja = panels[BajaPanel(art) - 1];
            baja.hold = 0.6f;
            baja.Say(Line(HellEndingLines.Get(line1))).Say(Line(HellEndingLines.Get(line2)));
            var epilogue = panels[EpiloguePanel(art) - 1];
            epilogue.hold = 1.2f;
            epilogue.Say(Line(HellEndingLines.Get(card)));
            s.Page(new HellComicPage(panels[0], panels[1]));
            s.Page(new HellComicPage(panels[2], panels[3]));
        }

        static HellComicLine Line(HellEndingLine l)
        {
            if (l == null) return HellComicLine.Caption("");
            if (l.speaker == HellEndingLines.Card) return HellComicLine.Caption(l.text);
            return HellComicLine.Balloon(l.speaker, HellEndingLines.Display(l.speaker), l.text, l.Voiced ? l.key : null);
        }

        /// <summary>What each ending panel shows (the placeholder's words, and the brief for its art); panel 0 is the splash.</summary>
        public static string Describe(string art, int panel)
        {
            switch (art + ":" + panel)
            {
                case "kill:1": return "Vesper's violet soul in a lantern; her body smoke on the floor.";
                case "kill:2": return "You run the crumbling bridge out of Hell, the lantern in your hand.";
                case "kill:3": return "Your sister wakes in her hospital bed, you beside her.";
                case "kill:4": return "Baja on her hourglass throne takes Vesper's soul, delighted.";
                case "kill:0": return "your sister home.";
                case "spare-kept:1": return "You lower the gun and offer Vesper your hand.";
                case "spare-kept:2": return "The three of you burst out through Hell's gate.";
                case "spare-kept:3": return "Baja, on her throne, laughs: you did her chores.";
                case "spare-kept:4": return "On a rooftop with your sister; the evening star over the city.";
                case "spare-kept:0": return "the evening star over the city.";
                case "spare-cost:1": return "You lower the gun and offer Vesper your hand.";
                case "spare-cost:2": return "You and Vesper run the collapsing way out.";
                case "spare-cost:3": return "Baja, cold, an hourglass in her hand: time pays.";
                case "spare-cost:4": return "Your sister walks up into the light; you stay below, Vesper beside you.";
                case "spare-cost:0": return "an hourglass running out in the dark.";
                case "damned:1": return "The escape: the gate grinding shut ahead of you.";
                case "damned:2": return "The gate slams; you're on the wrong side.";
                case "damned:3": return "Baja at the shut gate, the hour gone, almost sorry.";
                case "damned:4": return "You and your sister below, together, for good.";
                case "damned:0": return "the shut gate.";
                case "lucifer:1": return "Baja's ledger: every errand ticked, on time.";
                case "lucifer:2": return "Deep below, a throne of black glass.";
                case "lucifer:3": return "Baja points down: the Morning Star is next.";
                case "lucifer:4": return "Far below, Lucifer looks up.";
                case "lucifer:0": return "Lucifer's eyes in the dark.";
                default: return "An ending panel.";
            }
        }

        /// <summary>The voices a sequence plays (speaker, key), in order.</summary>
        public static IEnumerable<(string speaker, string key)> Voices(HellComicSequence s)
        {
            foreach (var p in s.pages) foreach (var pn in p.panels) foreach (var l in pn.lines)
                if (l.Voiced) yield return (l.speaker, l.voice);
        }

        /// <summary>How long a line stays on its own: its clip plus a tail, else reading time (about 15 characters a second).</summary>
        public static float LineSeconds(HellComicLine line, float clipSeconds)
        {
            int n = line?.text?.Length ?? 0;
            float read = Math.Min(8f, Math.Max(2.2f, 1.2f + n / 15f));
            return clipSeconds > 0f ? Math.Max(1.6f, clipSeconds + 0.6f) : read;
        }
    }
}
