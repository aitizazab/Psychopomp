using System;

namespace JamKit.Skyline
{
    /// <summary>The Hell sound pass's small rules, pure so they are tested (HellSoundBinder applies them).</summary>
    public static class HellSoundRules
    {
        /// <summary>Time Dilation at its slowest (the dilation curve's floor): the world is pitched to this share and low-passed to
        /// <see cref="MuffleMinHz"/>.</summary>
        public const float MuffleMinHz = 900f, MuffleOpenHz = 22000f, PitchFloor = 0.6f;

        /// <summary>How far time is dilated (0 = normal speed at pitch 1, 1 = at <paramref name="slowest"/>).</summary>
        public static float DilationDepth(float pitch, float slowest)
        {
            if (!(pitch < 0.999f)) return 0f;
            float span = 1f - Math.Max(0.01f, Math.Min(0.99f, slowest));
            return Clamp01((1f - pitch) / span);
        }

        /// <summary>The world's low-pass cutoff (Hz) at a dilation depth: open at 0, <see cref="MuffleMinHz"/> at 1, exponential between.</summary>
        public static float MuffleCutoff(float depth)
        {
            float d = Clamp01(depth);
            return (float)(MuffleOpenHz * Math.Pow(MuffleMinHz / MuffleOpenHz, Math.Pow(d, 0.6)));
        }

        /// <summary>The world's pitch while dilated: the time scale itself, but never below <see cref="PitchFloor"/> (deeper than that
        /// turns every sound to mud).</summary>
        public static float WorldPitch(float timeScale) => timeScale >= 1f ? 1f : Math.Max(PitchFloor, timeScale);

        /// <summary>True when a 0..1 beat phase wrapped from <paramref name="before"/> to <paramref name="now"/> (one throb started).</summary>
        public static bool BeatWrapped(float before, float now) => before >= 0f && now >= 0f && now < before - 0.5f;

        /// <summary>The leg plants between two stride phases (two a cycle, at each half): how many happened.</summary>
        public static int StepsBetween(float before, float now)
        {
            if (!(now > before)) return 0;
            return (int)(Math.Floor(now * 2.0) - Math.Floor(before * 2.0));
        }

        /// <summary>The brand loop's pitch from the meter: it climbs an octave as the brand charges.</summary>
        public static float ChargePitch(float progress) => 0.8f + 0.8f * Clamp01(progress);

        /// <summary>The shaft wind's level and pitch over a fall (seconds in): it rises for <paramref name="rise"/> seconds.</summary>
        public static float WindLevel(float seconds, float rise = 2.5f) => 0.35f + 0.65f * Clamp01(seconds / Math.Max(0.01f, rise));

        /// <summary>Where a timed warning starts so it ENDS on the beat: the seconds into the clip to start from now (0 = from the top),
        /// or a negative wait (start later) when the beat is further off than the clip is long.</summary>
        public static float WarnOffset(float clipSeconds, float secondsToBeat) => clipSeconds - secondsToBeat;

        /// <summary>
        /// A watched count (sigils lit, anchors broken, wells sealed): a cue plays only when it goes UP in play. A Rewind or a room
        /// restart re-bases it without playing (the restore can lower or raise it); going up again afterwards plays, because the player
        /// really did it again.
        /// </summary>
        public struct Watch
        {
            int _last;
            bool _based;

            /// <summary>Feeds the count now; true when it rose since the last feed (and the watch was based).</summary>
            public bool Rose(int now)
            {
                bool rose = _based && now > _last;
                _last = now;
                _based = true;
                return rose;
            }

            /// <summary>Takes <paramref name="now"/> as the base without playing (after a Rewind, a restart, a new room).</summary>
            public void Rebase(int now) { _last = now; _based = true; }

            public void Clear() { _based = false; }
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
