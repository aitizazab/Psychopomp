using UnityEngine;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The ending's HUD (docs/HELL-ENDING.md): the decision's prompt under the crosshair with the hold ring round the crosshair
    /// (it fills while you hold Use), the escape's objective line, and a banner. Built under the HUD's safe area, so the clean
    /// picture (docs/HELL-CAPTURE.md) hides it with the HUD. Plain class owned by <see cref="HellEndingDirector"/>.
    /// </summary>
    public sealed class HellEndingHud
    {
        public const float RingRadiusPx = 46f, RingLinePx = 7f, PromptTopPercent = 58f;
        static readonly Color Ring = new Color(1f, 0.82f, 0.55f);
        static readonly Color RingBack = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color PromptColour = new Color(1f, 0.86f, 0.62f);
        static readonly Color BannerColour = new Color(1f, 0.55f, 0.25f);

        VisualElement _promptBox, _ring, _lineBox, _bannerBox;
        KeylinedLabel _prompt, _line, _banner;
        float _progress, _bannerUntil;
        bool _ringOn;

        public bool Built => _prompt != null;

        public bool Build()
        {
            if (_prompt != null) return true;
            var hud = SkylineHud.Instance;
            if (hud == null || hud.Safe == null) return false;
            var safe = hud.Safe;
            string face = SkylineHudStyle.AmmoFontDefault;

            _promptBox = SkylineHudStyle.Layer(safe, "hell-ending-prompt");
            _promptBox.style.left = 0;
            _promptBox.style.right = 0;
            _promptBox.style.top = Length.Percent(PromptTopPercent);
            _promptBox.style.alignItems = Align.Center;
            _prompt = new KeylinedLabel(30f, 2.5f, face);
            _prompt.SetColor(PromptColour);
            _promptBox.Add(_prompt);

            // the hold ring: centred on the crosshair
            _ring = SkylineHudStyle.Layer(safe, "hell-ending-ring");
            float size = (RingRadiusPx + RingLinePx) * 2f;
            _ring.style.width = size;
            _ring.style.height = size;
            _ring.style.left = Length.Percent(50);
            _ring.style.top = Length.Percent(50);
            _ring.style.translate = new Translate(-size * 0.5f, -size * 0.5f);
            _ring.generateVisualContent += DrawRing;

            _lineBox = SkylineHudStyle.Layer(safe, "hell-ending-line");
            _lineBox.style.left = 0;
            _lineBox.style.right = 0;
            _lineBox.style.top = 96f;
            _lineBox.style.alignItems = Align.Center;
            _line = new KeylinedLabel(34f, 2.5f, face);
            _line.SetColor(BannerColour);
            _lineBox.Add(_line);

            _bannerBox = SkylineHudStyle.Layer(safe, "hell-ending-banner");
            _bannerBox.style.left = 0;
            _bannerBox.style.right = 0;
            _bannerBox.style.top = Length.Percent(30);
            _bannerBox.style.alignItems = Align.Center;
            _banner = new KeylinedLabel(72f, 4f, face);
            _banner.SetColor(BannerColour);
            _bannerBox.Add(_banner);
            Clear();
            return true;
        }

        void DrawRing(MeshGenerationContext ctx)
        {
            if (!_ringOn) return;
            var p = ctx.painter2D;
            var c = new Vector2(RingRadiusPx + RingLinePx, RingRadiusPx + RingLinePx);
            p.lineWidth = RingLinePx;
            p.lineCap = LineCap.Butt;
            p.strokeColor = RingBack;
            p.BeginPath();
            p.Arc(c, RingRadiusPx, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Stroke();
            if (_progress <= 0.001f) return;
            p.strokeColor = Ring;
            p.BeginPath();
            p.Arc(c, RingRadiusPx, Angle.Degrees(-90f), Angle.Degrees(-90f + 360f * Mathf.Clamp01(_progress)));
            p.Stroke();
        }

        /// <summary>The decision's prompt (empty hides it) and the hold ring (shown when <paramref name="ring"/>).</summary>
        public void SetPrompt(string text, bool ring, float progress)
        {
            if (!Build()) return;
            text = text ?? "";
            if (_prompt.Text != text) _prompt.SetText(text);
            _promptBox.style.display = text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (ring != _ringOn || Mathf.Abs(progress - _progress) > 0.002f)
            {
                _ringOn = ring;
                _progress = progress;
                _ring.MarkDirtyRepaint();
            }
        }

        /// <summary>The escape's objective line (empty hides it).</summary>
        public void SetLine(string text)
        {
            if (!Build()) return;
            text = text ?? "";
            if (_line.Text != text) _line.SetText(text);
            _lineBox.style.display = text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Banner(string text, float seconds)
        {
            if (!Build()) return;
            _banner.SetText(text);
            _bannerBox.style.display = DisplayStyle.Flex;
            _bannerUntil = Time.unscaledTime + seconds;
        }

        public void Tick()
        {
            if (_bannerBox != null && _bannerBox.style.display == DisplayStyle.Flex && Time.unscaledTime > _bannerUntil)
                _bannerBox.style.display = DisplayStyle.None;
        }

        public void Clear()
        {
            if (_prompt == null) return;
            SetPrompt("", false, 0f);
            SetLine("");
            _bannerBox.style.display = DisplayStyle.None;
        }
    }
}
