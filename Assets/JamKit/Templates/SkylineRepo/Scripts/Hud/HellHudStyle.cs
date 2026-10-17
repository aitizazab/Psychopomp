using JamKit.Skyline;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Hell's visual language for the HUD (docs/HELL-UI.md, "The language"): dark obsidian plates with ember edges, ink
    /// keylines, and one hue per meaning: <b>ember</b> = you (health, fire, the gun), <b>violet</b> = Shadow and objectives,
    /// <b>sand</b> (pale hourglass gold) = Time (Rewind, Time Dilation, Baja's hour), <b>bone</b> = the Ward (the outer
    /// health layer). <b>Soulfire cyan is the enemies' only</b> (their outlines, the boss bar): no HUD element of yours uses it.
    /// Faces by role in <see cref="HellFaces"/>. <see cref="Active"/> is on in Hell scenes; Skyline scenes keep their look.
    /// </summary>
    public static class HellHudStyle
    {
        public static readonly Color Ink = new Color32(0x0E, 0x08, 0x0C, 255);
        public static readonly Color Plate = new Color(0.075f, 0.04f, 0.05f, 0.88f);       // obsidian, see-through
        public static readonly Color PlateLight = new Color(0.16f, 0.09f, 0.09f, 0.92f);
        public static readonly Color EmberEdge = new Color32(0xC8, 0x4A, 0x1A, 255);       // a plate's rim
        public static readonly Color Ember = new Color32(0xFF, 0x6A, 0x1F, 255);           // health, fire
        public static readonly Color EmberHot = new Color32(0xFF, 0xD2, 0x7A, 255);        // the ember's core, the low throb's peak
        public static readonly Color Crack = new Color32(0x1A, 0x06, 0x05, 255);
        public static readonly Color Bone = new Color32(0xEC, 0xDF, 0xC6, 255);            // the Ward
        public static readonly Color Shadow = new Color32(0x9B, 0x6B, 0xFF, 255);          // Shadow Step, Nightfall, objectives
        public static readonly Color Sand = new Color32(0xF2, 0xC8, 0x6B, 255);            // Time: Rewind, Dilation, the hour
        public static readonly Color Fire = new Color32(0xFF, 0x7A, 0x1F, 255);
        public static readonly Color Soulfire = new Color32(0x5F, 0xE3, 0xF0, 255);        // ENEMIES ONLY
        public static readonly Color Ash = new Color32(0xF4, 0xEC, 0xDE, 255);             // text
        public static readonly Color AshDim = new Color(0.96f, 0.93f, 0.87f, 0.55f);
        public static readonly Color Spent = new Color(0.38f, 0.33f, 0.36f, 1f);            // blocked / not ready

        /// <summary>On in play in Hell scenes (any scene not named Skyline*): the HUD pieces draw the Hell look. Off in edit mode
        /// (the kit's EditMode HUD tests check the Skyline look), unless <see cref="Override"/> says otherwise.</summary>
        public static bool Active
        {
            get
            {
                if (Override.HasValue) return Override.Value;
                if (!Application.isPlaying) return false;
                if (!_known) Refresh(SceneManager.GetActiveScene());
                return _active;
            }
        }
        static bool _active, _known;

        /// <summary>Forces the look on or off (tests, a still); null follows the scene.</summary>
        public static bool? Override;

        static void Refresh(Scene s)
        {
            var n = s.name;
            _active = string.IsNullOrEmpty(n) || !n.StartsWith("Skyline", System.StringComparison.Ordinal);
            _known = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _known = false;
            SceneManager.activeSceneChanged -= OnSceneChanged;
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        static void OnSceneChanged(Scene from, Scene to) => Refresh(to);

        /// <summary>The skill flavour's HUD hue (Time is sand here, not the card's cyan: cyan belongs to the enemies).</summary>
        public static Color Flavour(HellFlavour f) => f == HellFlavour.Shadow ? Shadow : f == HellFlavour.Time ? Sand : Fire;

        public static Color Dim(Color c, float k = 0.45f) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        /// <summary>A keylined label in a role's face (see <see cref="HellFaces"/>).</summary>
        public static KeylinedLabel Label(float px, string face, Color colour, float keyline = 2.5f, float letterSpacing = 0f)
        {
            var l = new KeylinedLabel(px, keyline, face);
            l.SetColor(colour);
            if (letterSpacing != 0f) foreach (var c in l.Children()) c.style.letterSpacing = letterSpacing;
            return l;
        }

        /// <summary>Places <paramref name="e"/> at <paramref name="box"/> (absolute, inside the safe area).</summary>
        public static void Place(VisualElement e, HellHudBox box)
        {
            var s = e.style;
            s.position = Position.Absolute;
            if (box.wShare > 0f) s.width = Length.Percent(box.wShare * 100f);
            else s.width = box.w;
            s.height = box.hgt;
            switch (box.h)
            {
                case HellHAlign.Left: s.left = box.x; break;
                case HellHAlign.Right: s.right = box.x; break;
                default:
                    s.left = Length.Percent(50);
                    if (box.wShare > 0f) s.translate = new Translate(Length.Percent(-50), 0);
                    else s.marginLeft = -box.w * 0.5f + box.x;
                    break;
            }
            switch (box.v)
            {
                case HellVAlign.Top:
                    if (box.yShare > 0f) { s.top = Length.Percent(box.yShare * 100f); s.marginTop = box.y; }
                    else s.top = box.y;
                    break;
                case HellVAlign.Bottom: s.bottom = box.y; break;
                default:
                    s.top = Length.Percent(50);
                    s.marginTop = -box.hgt * 0.5f + box.y;
                    break;
            }
        }

        // ------------------------------------------------------------------ painting

        /// <summary>A slanted obsidian plate (the "/" lean of the Skyline bars, kept) with an ember rim and an ink keyline.</summary>
        public static void PlateShape(Painter2D p, float x, float y, float w, float h, float skew, Color fill, Color rim, float rimPx = 2f)
        {
            SkylineHudStyle.Parallelogram(p, x, y, w, h, skew);
            p.fillColor = fill;
            p.Fill();
            p.strokeColor = Ink;
            p.lineWidth = rimPx + 2.5f;
            p.lineJoin = LineJoin.Miter;
            p.Stroke();
            SkylineHudStyle.Parallelogram(p, x, y, w, h, skew);
            p.strokeColor = rim;
            p.lineWidth = rimPx;
            p.Stroke();
        }

        /// <summary>A diamond (a charge pip) round <paramref name="c"/>, filled from the bottom by <paramref name="fill01"/>.</summary>
        public static void Diamond(Painter2D p, Vector2 c, float r, float fill01, Color col, Color track)
        {
            DiamondPath(p, c, r);
            p.fillColor = track;
            p.Fill();
            fill01 = Mathf.Clamp01(fill01);
            if (fill01 > 0.001f)
            {
                // the filled part: the diamond cut at a height (a polygon of the diamond below the line)
                float yCut = c.y + r - 2f * r * fill01;
                p.BeginPath();
                if (fill01 >= 0.999f) { DiamondPath(p, c, r); }
                else if (yCut >= c.y)
                {
                    float hw = (c.y + r - yCut);
                    p.MoveTo(new Vector2(c.x, c.y + r));
                    p.LineTo(new Vector2(c.x - hw, yCut));
                    p.LineTo(new Vector2(c.x + hw, yCut));
                    p.ClosePath();
                }
                else
                {
                    float hw = r - (c.y - yCut);
                    p.MoveTo(new Vector2(c.x, c.y + r));
                    p.LineTo(new Vector2(c.x - r, c.y));
                    p.LineTo(new Vector2(c.x - hw, yCut));
                    p.LineTo(new Vector2(c.x + hw, yCut));
                    p.LineTo(new Vector2(c.x + r, c.y));
                    p.ClosePath();
                }
                p.fillColor = fill01 >= 0.999f ? col : Dim(col, 0.6f);
                p.Fill();
            }
            DiamondPath(p, c, r);
            p.strokeColor = Ink;
            p.lineWidth = 2.5f;
            p.lineJoin = LineJoin.Miter;
            p.Stroke();
        }

        static void DiamondPath(Painter2D p, Vector2 c, float r)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(c.x, c.y - r));
            p.LineTo(new Vector2(c.x + r, c.y));
            p.LineTo(new Vector2(c.x, c.y + r));
            p.LineTo(new Vector2(c.x - r, c.y));
            p.ClosePath();
        }

        /// <summary>A dial: a dark disc, a ring track, and an arc filled clockwise from twelve o'clock.</summary>
        public static void Dial(Painter2D p, Vector2 c, float r, float fill01, Color col, float line = 6f)
        {
            p.BeginPath();
            p.Arc(c, r + line * 0.5f + 1.5f, new Angle(0f), new Angle(360f));
            p.fillColor = Plate;
            p.Fill();
            p.BeginPath();
            p.Arc(c, r, new Angle(0f), new Angle(360f));
            p.strokeColor = new Color(Ink.r, Ink.g, Ink.b, 0.9f);
            p.lineWidth = line + 3f;
            p.Stroke();
            fill01 = Mathf.Clamp01(fill01);
            if (fill01 > 0.002f)
            {
                p.BeginPath();
                p.Arc(c, r, new Angle(-90f), new Angle(-90f + 360f * fill01));
                p.strokeColor = fill01 >= 0.999f ? col : Dim(col, 0.62f);
                p.lineWidth = line;
                p.lineCap = LineCap.Butt;
                p.Stroke();
            }
        }

        /// <summary>The Brand's sigil (a ringed, crossed brand mark), drawn for the objective ring and markers in place of
        /// Skyline's asset glyphs (<see cref="SkylineHudObjective.DrawGlyph"/> swaps to it in Hell).</summary>
        public static void BrandSigil(Painter2D p, Rect r, Color fill, Color ink, float line)
        {
            var c = r.center;
            float rad = Mathf.Min(r.width, r.height) * 0.42f;
            p.BeginPath();
            p.Arc(c, rad, new Angle(0f), new Angle(360f));
            p.strokeColor = ink;
            p.lineWidth = line + 3f;
            p.Stroke();
            p.BeginPath();
            p.Arc(c, rad, new Angle(0f), new Angle(360f));
            p.strokeColor = fill;
            p.lineWidth = line;
            p.Stroke();
            // the brand: a three-pronged mark
            p.lineCap = LineCap.Butt;
            for (int pass = 0; pass < 2; pass++)
            {
                p.strokeColor = pass == 0 ? ink : fill;
                p.lineWidth = pass == 0 ? line + 3f : line;
                p.BeginPath();
                p.MoveTo(c + new Vector2(0f, -rad * 0.75f));
                p.LineTo(c + new Vector2(0f, rad * 0.75f));
                p.MoveTo(c + new Vector2(-rad * 0.55f, -rad * 0.35f));
                p.LineTo(c + new Vector2(0f, rad * 0.15f));
                p.LineTo(c + new Vector2(rad * 0.55f, -rad * 0.35f));
                p.Stroke();
            }
        }
    }
}
