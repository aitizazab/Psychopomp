using UnityEngine;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Baja's hour on the HUD (docs/HELL-HOUR.md): a small ember hourglass, no number. The top bulb holds the hour left, the
    /// bottom what is spent; a thin stream falls while the hour runs (it stops under menus, cards and held moments); the sand
    /// glows hotter in the last tenth, and in overtime the glass is cracked and pulses red over an empty top.
    /// <para>Self-contained and data-free on purpose: it draws whatever <see cref="Set"/> gives it (the director feeds it from
    /// the pure <c>HellHour</c>), on one element under <c>SkylineHud.Safe</c> named <c>hell-hourglass</c>, so the HUD redesign can
    /// move, restyle or replace it (<see cref="Element"/>, <see cref="Place"/>, the colours below) without touching the rules.</para>
    /// </summary>
    public sealed class HellHourglass
    {
        public const float Width = 34f, Height = 58f, Line = 2.5f;
        public static Color Sand = new Color(1f, 0.55f, 0.16f, 1f);
        public static Color SandHot = new Color(1f, 0.86f, 0.45f, 1f);
        public static Color Owed = new Color(1f, 0.18f, 0.12f, 1f);

        /// <summary>The live hourglass (null until the HUD exists).</summary>
        public static HellHourglass Instance { get; private set; }

        VisualElement _el;
        float _left = 1f, _pulse;
        bool _running, _overtime, _visible = true;

        public VisualElement Element => _el;
        public bool Built => _el != null;

        /// <summary>Builds under <paramref name="parent"/> (the HUD's safe area), right side above the abilities' bars.</summary>
        public void Build(VisualElement parent)
        {
            if (_el != null || parent == null) return;
            _el = SkylineHudStyle.Layer(parent, "hell-hourglass");
            Place(48f, 282f);
            _el.style.width = Width + Line * 2f;
            _el.style.height = Height + Line * 2f;
            _el.generateVisualContent += Draw;
            Instance = this;
        }

        /// <summary>Puts it <paramref name="right"/> px from the safe area's right and <paramref name="bottom"/> px from its bottom.</summary>
        public void Place(float right, float bottom)
        {
            if (_el == null) return;
            _el.style.right = right;
            _el.style.bottom = bottom;
        }

        /// <summary>The state to draw: the share of the hour left (0..1), whether it is running this frame, overtime.</summary>
        public void Set(float leftShare, bool running, bool overtime)
        {
            _left = Mathf.Clamp01(leftShare);
            _running = running;
            _overtime = overtime;
            _pulse += Time.unscaledDeltaTime;
            if (_el != null) _el.MarkDirtyRepaint();
        }

        public void SetVisible(bool on)
        {
            if (_el == null || on == _visible) return;
            _visible = on;
            _el.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Destroy()
        {
            if (_el != null) _el.RemoveFromHierarchy();
            _el = null;
            if (Instance == this) Instance = null;
        }

        void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            float x0 = Line, y0 = Line, w = Width, h = Height, half = h * 0.5f, cx = x0 + w * 0.5f, neck = y0 + half;
            bool low = !_overtime && _left < 0.1f;
            float beat = 0.5f + 0.5f * Mathf.Sin(_pulse * (_overtime ? 5f : 7f));
            var sand = _overtime ? Color.Lerp(Owed * 0.6f, Owed, beat) : low ? Color.Lerp(Sand, SandHot, beat) : Sand;
            sand.a = 1f;

            // the glass: two bulbs meeting at the neck
            Glass(p, x0, y0, w, half, cx, neck, h);
            SkylineHudStyle.FillStroke(p, SkylineHudStyle.Track, _overtime ? Color.Lerp(SkylineHudStyle.Ink, Owed, 0.5f * beat) : SkylineHudStyle.Ink, Line);

            // the top bulb's sand: a triangle down to the neck, its height the root of the share (area grows with height squared)
            float t = half * Mathf.Sqrt(_left);
            if (t > 0.5f)
            {
                float hw = w * 0.5f * (t / half);
                p.BeginPath();
                p.MoveTo(new Vector2(cx - hw, neck - t));
                p.LineTo(new Vector2(cx + hw, neck - t));
                p.LineTo(new Vector2(cx, neck));
                p.ClosePath();
                p.fillColor = sand;
                p.Fill();
            }
            // the bottom bulb's sand: the bulb minus an empty triangle at its apex as tall as the top's sand
            float spent = 1f - _left;
            if (spent > 0.002f)
            {
                float ew = w * 0.5f * (t / half);
                p.BeginPath();
                p.MoveTo(new Vector2(cx - ew, neck + t));
                p.LineTo(new Vector2(cx + ew, neck + t));
                p.LineTo(new Vector2(x0 + w, y0 + h));
                p.LineTo(new Vector2(x0, y0 + h));
                p.ClosePath();
                p.fillColor = sand * 0.85f + Color.black * 0.15f;
                p.Fill();
            }
            // the stream while the hour runs
            if (_running && _left > 0f)
            {
                p.lineWidth = 1.5f;
                p.strokeColor = sand;
                p.BeginPath();
                p.MoveTo(new Vector2(cx, neck));
                p.LineTo(new Vector2(cx, neck + Mathf.Max(t, 2f) + 1f));
                p.Stroke();
            }
            // overtime: a crack across the top bulb
            if (_overtime)
            {
                p.lineWidth = 1.5f;
                p.strokeColor = Owed;
                p.BeginPath();
                p.MoveTo(new Vector2(x0 + w * 0.2f, y0 + 3f));
                p.LineTo(new Vector2(x0 + w * 0.45f, y0 + half * 0.4f));
                p.LineTo(new Vector2(x0 + w * 0.35f, y0 + half * 0.6f));
                p.LineTo(new Vector2(x0 + w * 0.6f, y0 + half * 0.85f));
                p.Stroke();
            }
            // the frame's caps
            Cap(p, x0 - 1f, y0 - 1f, w + 2f);
            Cap(p, x0 - 1f, y0 + h - 2f, w + 2f);
        }

        static void Glass(Painter2D p, float x0, float y0, float w, float half, float cx, float neck, float h)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(x0, y0));
            p.LineTo(new Vector2(x0 + w, y0));
            p.LineTo(new Vector2(cx + 2f, neck));
            p.LineTo(new Vector2(x0 + w, y0 + h));
            p.LineTo(new Vector2(x0, y0 + h));
            p.LineTo(new Vector2(cx - 2f, neck));
            p.ClosePath();
        }

        static void Cap(Painter2D p, float x, float y, float w)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(x, y));
            p.LineTo(new Vector2(x + w, y));
            p.LineTo(new Vector2(x + w, y + 3f));
            p.LineTo(new Vector2(x, y + 3f));
            p.ClosePath();
            SkylineHudStyle.FillStroke(p, SkylineHudStyle.Ink, SkylineHudStyle.Ink, 1f);
        }
    }
}
