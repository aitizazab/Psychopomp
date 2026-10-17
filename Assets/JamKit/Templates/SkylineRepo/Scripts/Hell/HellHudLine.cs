using UnityEngine;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>The run's objective line on the HUD (under the objective strip, top centre) and a big banner for floor and
    /// run titles. Plain class owned by the run director; built once the HUD's safe area exists. Draft wording.</summary>
    public sealed class HellHudLine
    {
        public const float LinePx = 34f, BannerPx = 72f, TopPx = 104f;

        KeylinedLabel _line, _banner;
        VisualElement _bannerBox;
        float _bannerUntil;

        public bool Built => _line != null;

        public void Build(VisualElement safe)
        {
            if (safe == null || _line != null) return;
            string face = SkylineHudStyle.AmmoFontDefault;
            var box = SkylineHudStyle.Layer(safe, "hell-objective-line");
            box.style.left = 0;
            box.style.right = 0;
            box.style.top = TopPx;
            box.style.alignItems = Align.Center;
            _line = new KeylinedLabel(LinePx, 2.5f, face);
            _line.SetColor(new Color(1f, 0.82f, 0.55f));
            box.Add(_line);

            _bannerBox = SkylineHudStyle.Layer(safe, "hell-banner");
            _bannerBox.style.left = 0;
            _bannerBox.style.right = 0;
            _bannerBox.style.top = Length.Percent(30);
            _bannerBox.style.alignItems = Align.Center;
            _banner = new KeylinedLabel(BannerPx, 4f, face);
            _banner.SetColor(new Color(1f, 0.45f, 0.2f));
            _bannerBox.Add(_banner);
            _bannerBox.style.display = DisplayStyle.None;
        }

        public void SetLine(string text)
        {
            if (_line != null && _line.Text != text) _line.SetText(text);
        }

        /// <summary>Shows a banner for <paramref name="seconds"/> (unscaled; negative: until replaced).</summary>
        public void Banner(string text, float seconds)
        {
            if (_banner == null) return;
            _banner.SetText(text);
            _bannerBox.style.display = DisplayStyle.Flex;
            _bannerUntil = seconds < 0f ? float.MaxValue : Time.unscaledTime + seconds;
        }

        public void Tick()
        {
            if (_bannerBox != null && Time.unscaledTime > _bannerUntil) _bannerBox.style.display = DisplayStyle.None;
        }
    }
}
