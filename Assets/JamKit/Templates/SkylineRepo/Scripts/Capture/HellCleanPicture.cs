using System;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The clean-picture switch (docs/HELL-CAPTURE.md): the HUD layout with no elements (no crosshair, bars, cooldowns,
    /// objective line, banners, lettering, hurt vignette or subtitles; the skill pick still shows while it is open, so a run
    /// can go on) and the voice lines muted, or everything but them (a voices stem). Set by <c>-hell-clean</c>,
    /// <c>-hell-hud clean|full</c> and <c>-hell-voices on|off|only</c>, by the pause menu (Look: HUD; Audio: Voice lines),
    /// or by a capture take. Not saved between sessions: a normal launch is always the full HUD with voices.
    /// <para>Every Skyline/Hell sound takes its player gain from <see cref="SkylineMix.UserGain"/>, which multiplies in
    /// <see cref="Gain"/>; the HUD's root follows <see cref="HideHud"/>. A new sound or voice line must go through
    /// <see cref="SkylineSounds"/> or take <see cref="SkylineMix.UserGain"/> (category Voices for spoken lines) to obey it.</para>
    /// </summary>
    public static class HellCleanPicture
    {
        static HellHudLayout _hud = HellHudLayout.Full;
        static HellVoiceMode _voices = HellVoiceMode.All;

        public static HellHudLayout Hud
        {
            get => _hud;
            set { if (_hud != value) { _hud = value; Debug.Log("HELL_CAPTURE hud " + value); Changed?.Invoke(); } }
        }

        public static HellVoiceMode Voices
        {
            get => _voices;
            set { if (_voices != value) { _voices = value; Debug.Log("HELL_CAPTURE voices " + value); Changed?.Invoke(); } }
        }

        public static event Action Changed;

        /// <summary>The HUD is hidden now (clean, and no skill pick waiting on the player).</summary>
        public static bool HideHud => _hud == HellHudLayout.Clean && !HellSkillPick.Open;

        /// <summary>The voice-mode gain of a category: 0 for the voices with voices off, 0 for everything else with voices only.</summary>
        public static float Gain(SkylineSoundCategory c)
        {
            switch (_voices)
            {
                case HellVoiceMode.NoVoices: return c == SkylineSoundCategory.Voices ? 0f : 1f;
                case HellVoiceMode.VoicesOnly: return c == SkylineSoundCategory.Voices ? 1f : 0f;
                default: return 1f;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void FromCommandLine()
        {
            Changed = null;
            _hud = HellHudLayout.Full;
            _voices = HellVoiceMode.All;
            var a = HellCapture.Parse(Environment.GetCommandLineArgs());
            foreach (var p in a.problems) Debug.LogWarning("HELL_CAPTURE " + p);
            if (a.hudGiven) _hud = a.hud;
            if (a.voicesGiven) _voices = a.voices;
            if (a.hudGiven || a.voicesGiven) Debug.Log("HELL_CAPTURE clean picture: hud " + _hud + ", voices " + _voices);
        }
    }
}
