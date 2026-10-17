using UnityEngine;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>Vesper's health bar (top centre: her name over a slanted ember bar with notches at the phase shifts), a hint
    /// line under it (the counters, shown once each) and a banner. Plain class owned by <see cref="HellVesperFight"/>; built
    /// once the HUD's safe area exists. Draft wording.</summary>
    public sealed class HellVesperHud
    {
        public const float TopPx = 150f, BarWidthPx = 620f, BarHeightPx = 14f;

        VisualElement _root, _fill, _bannerBox;
        KeylinedLabel _name, _hint, _banner;
        float _hintUntil, _bannerUntil;

        public bool Built => _root != null;

        public void Build(VisualElement safe, float phase2Share, float phase3Share)
        {
            if (safe == null || _root != null) return;
            string face = SkylineHudStyle.AmmoFontDefault;
            _root = SkylineHudStyle.Layer(safe, "vesper-bar");
            _root.style.left = 0;
            _root.style.right = 0;
            _root.style.top = TopPx;
            _root.style.alignItems = Align.Center;
            _name = new KeylinedLabel(30f, 2.5f, face);
            _name.SetColor(new Color(0.85f, 0.7f, 1f));
            _name.SetText("VESPER, THE EVENING STAR");
            _root.Add(_name);
            var bar = new VisualElement();
            bar.style.width = BarWidthPx;
            bar.style.height = BarHeightPx;
            bar.style.marginTop = 4f;
            bar.style.backgroundColor = new Color(0.05f, 0.02f, 0.06f, 0.85f);
            bar.style.borderTopWidth = bar.style.borderBottomWidth = bar.style.borderLeftWidth = bar.style.borderRightWidth = 2f;
            bar.style.borderTopColor = bar.style.borderBottomColor = bar.style.borderLeftColor = bar.style.borderRightColor = Color.black;
            _fill = new VisualElement();
            _fill.style.position = Position.Absolute;
            _fill.style.left = 0;
            _fill.style.top = 0;
            _fill.style.bottom = 0;
            _fill.style.width = Length.Percent(100);
            _fill.style.backgroundColor = new Color(1f, 0.45f, 0.18f);
            bar.Add(_fill);
            foreach (float share in new[] { phase2Share, phase3Share })
            {
                var notch = new VisualElement();
                notch.style.position = Position.Absolute;
                notch.style.top = -3f;
                notch.style.bottom = -3f;
                notch.style.width = 3f;
                notch.style.left = Length.Percent(share * 100f);
                notch.style.backgroundColor = new Color(0.6f, 0.4f, 1f);
                bar.Add(notch);
            }
            _root.Add(bar);
            _hint = new KeylinedLabel(26f, 2f, face);
            _hint.SetColor(new Color(0.55f, 0.95f, 1f));
            _hint.style.marginTop = 8f;
            _root.Add(_hint);

            _bannerBox = SkylineHudStyle.Layer(safe, "vesper-banner");
            _bannerBox.style.left = 0;
            _bannerBox.style.right = 0;
            _bannerBox.style.top = Length.Percent(32);
            _bannerBox.style.alignItems = Align.Center;
            _banner = new KeylinedLabel(64f, 4f, face);
            _banner.SetColor(new Color(0.75f, 0.45f, 1f));
            _bannerBox.Add(_banner);
            _bannerBox.style.display = DisplayStyle.None;
            Show(false);
        }

        public void Show(bool on)
        {
            if (_root != null) _root.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Health as a share of her maximum; the bar turns cold cyan while her Rewind winds up.</summary>
        public void SetHealth(float share, bool rewinding)
        {
            if (_fill == null) return;
            _fill.style.width = Length.Percent(Mathf.Clamp01(share) * 100f);
            _fill.style.backgroundColor = rewinding ? new Color(0.4f, 0.95f, 1f) : new Color(1f, 0.45f, 0.18f);
        }

        public void Hint(string text, float seconds)
        {
            if (_hint == null) return;
            _hint.SetText(text);
            _hintUntil = Time.unscaledTime + seconds;
        }

        public void Banner(string text, float seconds)
        {
            if (_banner == null) return;
            _banner.SetText(text);
            _bannerBox.style.display = DisplayStyle.Flex;
            _bannerUntil = Time.unscaledTime + seconds;
        }

        public void Tick()
        {
            if (_hint != null && Time.unscaledTime > _hintUntil && _hint.Text != "") _hint.SetText("");
            if (_bannerBox != null && Time.unscaledTime > _bannerUntil) _bannerBox.style.display = DisplayStyle.None;
        }
    }
}
