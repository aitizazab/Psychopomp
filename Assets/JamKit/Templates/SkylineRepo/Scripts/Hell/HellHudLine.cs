using JamKit.Skyline;
using UnityEngine;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>The run's objective line on the HUD (top centre, <see cref="HellHudPlaces.Objective"/>), the room's clock under
    /// it (<see cref="HellHudPlaces.RoomClock"/>) and a big banner for floor and run titles (<see cref="HellHudPlaces.Banner"/>).
    /// The line and the banner are Metal Mania (important subtext), the clock Luckiest Guy digits. Plain class owned by the run
    /// director; built once the HUD's safe area exists. Set the same text again costs nothing (no garbage a frame). Draft
    /// wording.</summary>
    public sealed class HellHudLine
    {
        public const float LinePx = 36f, BannerPx = 76f, ClockPx = 30f;

        KeylinedLabel _line, _banner, _clock;
        VisualElement _bannerBox, _clockBox;
        float _bannerUntil;
        string _clockText;

        public bool Built => _line != null;

        public void Build(VisualElement safe)
        {
            if (safe == null || _line != null) return;
            var box = SkylineHudStyle.Layer(safe, "hell-objective-line");
            HellHudStyle.Place(box, HellHudPlaces.Objective);
            box.style.alignItems = Align.Center;
            box.style.justifyContent = Justify.Center;
            _line = HellHudStyle.Label(LinePx, HellFaces.Heading, HellHudStyle.EmberHot, 2.5f, 1.5f);
            box.Add(_line);

            _clockBox = SkylineHudStyle.Layer(safe, "hell-room-clock");
            HellHudStyle.Place(_clockBox, HellHudPlaces.RoomClock);
            _clockBox.style.alignItems = Align.Center;
            _clock = HellHudStyle.Label(ClockPx, HellFaces.Numbers, HellHudStyle.Ash, 2.5f);
            _clockBox.Add(_clock);
            _clockBox.style.display = DisplayStyle.None;

            _bannerBox = SkylineHudStyle.Layer(safe, "hell-banner");
            HellHudStyle.Place(_bannerBox, HellHudPlaces.Banner);
            _bannerBox.style.alignItems = Align.Center;
            _bannerBox.style.justifyContent = Justify.Center;
            _banner = HellHudStyle.Label(BannerPx, HellFaces.Heading, HellHudStyle.Ember, 4f, 4f);
            _bannerBox.Add(_banner);
            _bannerBox.style.display = DisplayStyle.None;
        }

        public void SetLine(string text)
        {
            if (_line != null && _line.Text != (text ?? "")) _line.SetText(text);
            if (string.IsNullOrEmpty(text)) SetClock(-1f);
        }

        /// <summary>The room's clock in seconds (negative hides it). The text is made once per second value
        /// (<see cref="HellClockText"/>).</summary>
        public void SetClock(float seconds)
        {
            if (_clockBox == null) return;
            var disp = seconds >= 0f ? DisplayStyle.Flex : DisplayStyle.None;
            if (_clockBox.style.display != disp) _clockBox.style.display = disp;
            if (seconds < 0f) return;
            var t = HellClockText.Of(seconds);
            if (!ReferenceEquals(t, _clockText))
            {
                _clockText = t;
                _clock.SetText(t);
            }
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
            if (_bannerBox == null) return;
            var disp = Time.unscaledTime > _bannerUntil || HellAbilityWheel.IsOpen ? DisplayStyle.None : DisplayStyle.Flex;
            if (_bannerBox.style.display != disp) _bannerBox.style.display = disp;
        }
    }
}
