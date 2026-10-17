using UnityEngine;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The Hell abilities' cooldowns on the minimal HUD (docs/SKYLINE-HUD.md style: slanted bars, ink keylines, no label text),
    /// stacked above the ammo counter: Shadow Step's charges as violet pips (the recharging one fills), the Rewind as one
    /// cyan bar with a back-chevron, Nightfall (when owned) as a smaller violet bar. Ready = full and bright; cooling = the ink
    /// track filling. A pip or bar that comes ready flashes white once.
    /// </summary>
    public sealed class HellHud : MonoBehaviour
    {
        const float Right = 44f, Bottom = 190f, Width = 220f, RowH = 16f, Gap = 10f, Skew = 6f, Line = 2.5f;
        static readonly Color Step = new Color32(0x9B, 0x6B, 0xFF, 255);
        static readonly Color Rewind = new Color32(0x5F, 0xE3, 0xF0, 255);

        HellAbilities _a;
        VisualElement _el;
        float _stepFlash, _rewindFlash;
        bool _stepWasFull, _rewindWasReady = true;

        void Start() => _a = GetComponent<HellAbilities>();

        void Update()
        {
            if (_a == null) return;
            if (_el == null)
            {
                var hud = SkylineHud.Instance;
                if (hud == null || hud.Safe == null) return;
                _el = SkylineHudStyle.Layer(hud.Safe, "hell-cooldowns");
                _el.style.right = Right;
                _el.style.bottom = Bottom;
                _el.style.width = Width + Skew + Line * 2f;
                _el.style.height = RowH * 3f + Gap * 2f + Line * 2f;
                _el.generateVisualContent += Draw;
            }
            var c = _a.StepCharges;
            bool full = c.Charges >= c.Max;
            if (full && !_stepWasFull) _stepFlash = 0.25f;
            _stepWasFull = full;
            bool ready = _a.Rewind != null && _a.Rewind.Cooldown.Ready;
            if (ready && !_rewindWasReady) _rewindFlash = 0.25f;
            _rewindWasReady = ready;
            _stepFlash = Mathf.Max(0f, _stepFlash - Time.unscaledDeltaTime);
            _rewindFlash = Mathf.Max(0f, _rewindFlash - Time.unscaledDeltaTime);
            _el.MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext ctx)
        {
            if (_a == null) return;
            var p = ctx.painter2D;
            float y = Line;
            // Rewind (top): a back-chevron then the bar
            var cd = _a.Rewind != null ? _a.Rewind.Cooldown : null;
            float rf = cd != null ? cd.Fraction : 1f;
            Chevron(p, Line, y, RowH, cd != null && cd.Ready ? Rewind : Dim(Rewind));
            Bar(p, Line + RowH + 6f, y, Width - RowH - 6f, RowH, rf, _rewindFlash > 0f ? Color.white : Rewind);
            y += RowH + Gap;
            // Shadow Step: one pip a charge
            var c = _a.StepCharges;
            int n = Mathf.Max(1, c.Max);
            float pipW = (Width - (n - 1) * 8f) / n;
            for (int i = 0; i < n; i++)
            {
                float fill = i < c.Charges ? 1f : (i == c.Charges ? c.Fraction : 0f);
                bool blocked = HellAbilities.ShadowStepBlocked;
                var col = blocked ? new Color(0.45f, 0.4f, 0.5f) : (_stepFlash > 0f ? Color.white : Step);
                Bar(p, Line + i * (pipW + 8f), y, pipW, RowH, fill, col);
            }
            y += RowH + Gap;
            // Nightfall, when owned
            var night = GetComponent<HellSkillNightfall>();
            if (night != null) Bar(p, Line + Width * 0.45f, y, Width * 0.55f, RowH * 0.7f, night.Cooldown.Fraction, Step * 0.8f + Color.black * 0.2f);
        }

        static Color Dim(Color c) => new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, 1f);

        static void Bar(Painter2D p, float x, float y, float w, float h, float fill, Color col)
        {
            SkylineHudStyle.Parallelogram(p, x, y, w, h, Skew);
            SkylineHudStyle.FillStroke(p, SkylineHudStyle.Track, SkylineHudStyle.Ink, Line);
            fill = Mathf.Clamp01(fill);
            if (fill <= 0.001f) return;
            var c = fill >= 1f ? col : Dim(col);
            SkylineHudStyle.Parallelogram(p, x, y, w * fill, h, Skew);
            SkylineHudStyle.FillStroke(p, c, SkylineHudStyle.Ink, Line);
        }

        static void Chevron(Painter2D p, float x, float y, float h, Color col)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(x + h, y));
            p.LineTo(new Vector2(x, y + h * 0.5f));
            p.LineTo(new Vector2(x + h, y + h));
            p.LineTo(new Vector2(x + h * 0.7f, y + h * 0.5f));
            p.ClosePath();
            SkylineHudStyle.FillStroke(p, col, SkylineHudStyle.Ink, Line);
        }

        void OnDestroy()
        {
            if (_el != null) _el.RemoveFromHierarchy();
        }
    }
}
