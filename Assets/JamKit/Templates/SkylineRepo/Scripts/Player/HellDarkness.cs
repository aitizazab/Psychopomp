using JamKit.Skyline;
using UnityEngine;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Where it is dark (docs/HELL-ABILITIES.md): Shadow Step lands only in darkness. Dark = inside an authored dark zone
    /// (the rooms branch: <see cref="RegisterZone"/> / <see cref="UnregisterZone"/>) or a dynamic dark sphere (Nightfall:
    /// <see cref="AddSphere"/>), and not lit by a fire light (lava, braziers: <see cref="RegisterLight"/>). Lit beats dark.
    /// The handles returned are ints; unregistering by bounds removes the zone registered with exactly those bounds.
    /// </summary>
    public static class HellDarkness
    {
        static readonly DarknessField _field = new DarknessField();
        static readonly System.Collections.Generic.Dictionary<Bounds, int> _byBounds = new System.Collections.Generic.Dictionary<Bounds, int>();
        static readonly System.Collections.Generic.Dictionary<Vector4, int> _lightsByKey = new System.Collections.Generic.Dictionary<Vector4, int>();

        /// <summary>Raised when a dark sphere is made (the visual).</summary>
        public static event System.Action<Vector3, float, float> SphereAdded;

        static NVec3 N(Vector3 v) => new NVec3(v.x, v.y, v.z);
        static double Now => Time.timeAsDouble;

        /// <summary>A scene with no authored dark zone at all (the kit's old arena, before the rooms branch registers any)
        /// counts as dark everywhere that is not lit, so Shadow Step can be tried there. Off: the rule is strict.</summary>
        public static bool FallbackAllDarkWithoutZones = true;

        public static bool IsDark(Vector3 p)
        {
            if (FallbackAllDarkWithoutZones && _field.ZoneCount == 0) return !_field.IsLit(N(p), Now);
            return _field.IsDark(N(p), Now);
        }
        /// <summary>A steady fire light or passing fire (a bolt, a burning enemy, a blast, a lava flare) covers this point now.</summary>
        public static bool IsLit(Vector3 p) => _field.IsLit(N(p), Now);
        /// <summary>Inside an authored dark zone or a live dark sphere, lit or not (the stealth rule: docs/HELL-SHADOW.md). No
        /// zones at all (the old arena) means no hiding: the all-dark fallback is for the Shadow Step only.</summary>
        public static bool InDarkVolume(Vector3 p) => _field.InDarkVolume(N(p), Now);
        /// <summary>Within <paramref name="margin"/> m of a dark zone or a live dark sphere.</summary>
        public static bool NearDarkVolume(Vector3 p, float margin) => _field.NearDarkVolume(N(p), margin, Now);

        /// <summary>Raised for each discrete passing fire light (an ember bolt's strike, a burning enemy's tick, a Combustion
        /// blast, an ooze pool, a lava flare): where, how far, for how long. Not raised per bolt-path stamp. For the look (a light
        /// pulse in the dark zone): the lighting lane may listen.</summary>
        public static event System.Action<Vector3, float, float> Flashed;

        /// <summary>Fire passing through (docs/HELL-SHADOW.md): lights the dark within <paramref name="radius"/> of
        /// <paramref name="at"/> for <paramref name="seconds"/> of game time. <paramref name="announce"/>: raise <see cref="Flashed"/>.</summary>
        public static void Flash(Vector3 at, float radius, float seconds, bool announce = true)
        {
            _field.AddFlash(N(at), radius, Now + seconds);
            if (announce) Flashed?.Invoke(at, radius, seconds);
        }

        /// <summary>Passing fire lights alive now.</summary>
        public static int FlashCount => _field.FlashCount(Now);
        /// <summary>Inside a dynamic dark sphere (Nightfall): enemies there lose you.</summary>
        public static bool InDarkSphere(Vector3 p) => _field.InSphere(N(p), Now);
        /// <summary>The line a..b crosses a dynamic dark sphere: an enemy looking through it can't see you.</summary>
        public static bool SightThroughDark(Vector3 a, Vector3 b) => _field.SegmentThroughSphere(N(a), N(b), Now);

        public static int RegisterZone(Bounds b)
        {
            if (_byBounds.TryGetValue(b, out int old)) return old;
            int id = _field.AddZone(N(b.min), N(b.max));
            _byBounds[b] = id;
            return id;
        }

        public static void UnregisterZone(Bounds b)
        {
            if (_byBounds.TryGetValue(b, out int id)) { _field.RemoveZone(id); _byBounds.Remove(b); }
        }

        public static void UnregisterZone(int id) => _field.RemoveZone(id);

        public static int RegisterLight(Vector3 pos, float radius)
        {
            var key = new Vector4(pos.x, pos.y, pos.z, radius);
            if (_lightsByKey.TryGetValue(key, out int old)) return old;
            int id = _field.AddLight(N(pos), radius);
            _lightsByKey[key] = id;
            return id;
        }

        public static void UnregisterLight(Vector3 pos, float radius)
        {
            var key = new Vector4(pos.x, pos.y, pos.z, radius);
            if (_lightsByKey.TryGetValue(key, out int id)) { _field.RemoveLight(id); _lightsByKey.Remove(key); }
        }

        public static void UnregisterLight(int id) => _field.RemoveLight(id);

        /// <summary>A dark sphere for <paramref name="seconds"/> of game time (Nightfall).</summary>
        public static int AddSphere(Vector3 c, float radius, float seconds)
        {
            _field.Expire(Now);
            int id = _field.AddSphere(N(c), radius, Now + seconds);
            SphereAdded?.Invoke(c, radius, seconds);
            return id;
        }

        public static void RemoveSphere(int id) => _field.RemoveSphere(id);

        /// <summary>Everything off (a new run or scene).</summary>
        public static void Clear()
        {
            _field.Clear();
            _byBounds.Clear();
            _lightsByKey.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Clear();
            SphereAdded = null;
            Flashed = null;
        }
    }
}
