using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The only writer of <c>Time.timeScale</c> in play (docs/HELL-ABILITIES.md): anything that wants the game slower asks by
    /// key (<see cref="HitStopKey"/> from the player's hit stop, <see cref="DilationKey"/> from Time Dilation, a skill's own
    /// key) and the scale is the smallest request. The pause menu still holds 0 while it is open (nothing is written then; the
    /// next request after it closes puts the scale back). Physics keeps its fixed step: the game's logic (movement, bots,
    /// guns) runs in Update on <c>Time.deltaTime</c>, and the Rewind records on the fixed step in game time, so a slowed
    /// second records the same number of snapshots per game second.
    /// </summary>
    public static class HellTime
    {
        public const string HitStopKey = "hitstop", DilationKey = "dilation";

        static readonly TimeScaleRequests _requests = new TimeScaleRequests();

        public static float Scale => _requests.Scale;

        public static void Request(string key, float scale)
        {
            _requests.Request(key, scale);
            Apply();
        }

        public static void Clear(string key) => Request(key, 1f);
        public static bool Has(string key) => _requests.Has(key);     // a beat's hold (Baja's hour skips it)

        public static void ClearAll()
        {
            _requests.ClearAll();
            Apply();
        }

        /// <summary>Writes the scale (not while the pause menu holds the game at 0).</summary>
        public static void Apply()
        {
            if (SkylineDeviceInput.Paused || SkylinePauseMenu.IsOpen) return;
            if (!Mathf.Approximately(Time.timeScale, _requests.Scale)) Time.timeScale = _requests.Scale;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _requests.ClearAll();
    }
}
