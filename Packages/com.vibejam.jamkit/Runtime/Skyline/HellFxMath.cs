using System;
using System.Numerics;

namespace JamKit.Skyline
{
    // The Hell descent's signature effects, pure parts (docs/HELL-FX.md): the ghost paths the Rewind plays back, the
    // envelopes and curves every effect is timed by, the screen grade's channel mix, the shake falloffs and the emission
    // accumulator. No engine: the Unity side (JamKit.SkylineRepo, Fx/Hell*Fx.cs) draws them.

    /// <summary>A fixed-size ring of time-stamped positions (one per tracked thing: the player, a bot, a bolt), oldest first.
    /// Never allocates after construction. The Rewind's ghost trails are drawn from it.</summary>
    public sealed class GhostTrack
    {
        readonly Vector3[] _p;
        readonly double[] _t;
        int _start, _count;

        public GhostTrack(int capacity)
        {
            if (capacity < 2) capacity = 2;
            _p = new Vector3[capacity];
            _t = new double[capacity];
        }

        public int Count => _count;
        public int Capacity => _p.Length;
        public double OldestTime => _count > 0 ? _t[_start] : 0.0;
        public double NewestTime => _count > 0 ? _t[(_start + _count - 1) % _p.Length] : 0.0;
        public Vector3 Newest => _count > 0 ? _p[(_start + _count - 1) % _p.Length] : Vector3.Zero;

        /// <summary>The i-th sample, 0 the oldest.</summary>
        public Vector3 this[int i] => _p[(_start + i) % _p.Length];
        public double TimeAt(int i) => _t[(_start + i) % _p.Length];

        public void Clear() { _start = 0; _count = 0; }

        /// <summary>Adds a sample (times must not go backwards; an earlier time clears the ring first). Full: the oldest goes.</summary>
        public void Push(double time, Vector3 p)
        {
            if (_count > 0 && time < NewestTime) Clear();
            int i;
            if (_count < _p.Length) { i = (_start + _count) % _p.Length; _count++; }
            else { i = _start; _start = (_start + 1) % _p.Length; }
            _p[i] = p;
            _t[i] = time;
        }

        /// <summary>The index of the first sample at or after <paramref name="time"/> (Count when none).</summary>
        public int FirstAtOrAfter(double time)
        {
            int lo = 0, hi = _count;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (TimeAt(mid) < time) lo = mid + 1; else hi = mid;
            }
            return lo;
        }

        /// <summary>Where the path was at <paramref name="time"/>: linear between the samples around it, clamped to the ends.</summary>
        public Vector3 At(double time)
        {
            if (_count == 0) return Vector3.Zero;
            if (time <= OldestTime) return this[0];
            if (time >= NewestTime) return this[_count - 1];
            int j = FirstAtOrAfter(time);
            int i = j - 1;
            double t0 = TimeAt(i), t1 = TimeAt(j);
            float u = t1 > t0 ? (float)((time - t0) / (t1 - t0)) : 1f;
            return Vector3.Lerp(this[i], this[j], u);
        }

        /// <summary>The path's length between two times (m), summed over the samples.</summary>
        public float LengthBetween(double from, double to)
        {
            if (_count < 2 || to <= from) return 0f;
            float len = 0f;
            Vector3 prev = At(from);
            int i = FirstAtOrAfter(from);
            for (; i < _count && TimeAt(i) < to; i++)
            {
                len += Vector3.Distance(prev, this[i]);
                prev = this[i];
            }
            return len + Vector3.Distance(prev, At(to));
        }
    }

    /// <summary>The curves the Hell effects are timed by. Every input is clamped; every output is 0..1 unless said.</summary>
    public static class HellFxCurves
    {
        public static float Clamp01(float x) => x < 0f ? 0f : x > 1f ? 1f : x;

        public static float Smooth(float u) { u = Clamp01(u); return u * u * (3f - 2f * u); }

        /// <summary>Ease out (fast start, gentle arrival): 1 - (1-u)^power.</summary>
        public static float EaseOut(float u, float power = 2f) => 1f - (float)Math.Pow(1f - Clamp01(u), power);

        /// <summary>Ease in (slow start, fast end): u^power.</summary>
        public static float EaseIn(float u, float power = 2f) => (float)Math.Pow(Clamp01(u), power);

        /// <summary>A back-out ease (overshoots past 1 by about 10% then settles at 1): a card or a ring popping in.</summary>
        public static float BackOut(float u, float overshoot = 1.70158f)
        {
            u = Clamp01(u) - 1f;
            return 1f + u * u * ((overshoot + 1f) * u + overshoot);
        }

        /// <summary>The Rewind's scrub: how far back along the recorded path the ghosts are at <paramref name="u"/> (0..1 of the
        /// scrub): accelerates out of the present and brakes into the restored moment (a tape spinning up and catching).</summary>
        public static float Scrub(float u) => Smooth(u) * 0.75f + EaseIn(u, 3f) * 0.25f;

        /// <summary>An attack-hold-release envelope over <paramref name="total"/> seconds: rises over <paramref name="rise"/>,
        /// holds at 1, falls over the last <paramref name="fall"/>. 0 outside [0, total].</summary>
        public static float Envelope(float t, float total, float rise, float fall)
        {
            if (t < 0f || t > total || total <= 0f) return 0f;
            float a = rise > 0f ? Clamp01(t / rise) : 1f;
            float r = fall > 0f ? Clamp01((total - t) / fall) : 1f;
            return Math.Min(a, r);
        }

        /// <summary>A decay from 1 at <paramref name="since"/> = 0 to 0 at <paramref name="seconds"/> (quadratic: a hard snap that
        /// lets go fast).</summary>
        public static float Decay(float since, float seconds)
        {
            if (since < 0f || seconds <= 0f || since >= seconds) return 0f;
            float k = 1f - since / seconds;
            return k * k;
        }

        /// <summary>A field-of-view kick as a multiplier: out to 1 + <paramref name="amount"/> over <paramref name="punch"/>
        /// seconds, back to 1 over <paramref name="back"/>.</summary>
        public static float FovKick(float t, float amount, float punch, float back)
        {
            if (t < 0f) return 1f;
            if (t < punch) return 1f + amount * EaseOut(punch > 0f ? t / punch : 1f, 2f);
            float u = back > 0f ? (t - punch) / back : 1f;
            if (u >= 1f) return 1f;
            return 1f + amount * (1f - Smooth(u));
        }

        /// <summary>The camera trauma a landing adds: none below <paramref name="from"/> m/s of fall, rising to
        /// <paramref name="max"/> at <paramref name="full"/> m/s.</summary>
        public static float LandingTrauma(float fallSpeed, float from, float full, float max)
        {
            if (fallSpeed <= from || full <= from) return 0f;
            return max * Clamp01((fallSpeed - from) / (full - from));
        }

        /// <summary>Distance falloff: 1 within <paramref name="inner"/>, 0 beyond <paramref name="outer"/>, smooth between.</summary>
        public static float Falloff(float distance, float inner, float outer)
        {
            if (distance <= inner) return 1f;
            if (distance >= outer || outer <= inner) return 0f;
            return 1f - Smooth((distance - inner) / (outer - inner));
        }

        /// <summary>A staggered reveal: item <paramref name="index"/> starts at index * <paramref name="delay"/> and takes
        /// <paramref name="duration"/>; returns its raw 0..1 progress.</summary>
        public static float Stagger(float t, int index, float delay, float duration)
        {
            float local = t - index * delay;
            if (local <= 0f) return 0f;
            return duration > 0f ? Clamp01(local / duration) : 1f;
        }

        /// <summary>A pulse that breathes between <paramref name="lo"/> and 1 at <paramref name="hz"/> (phase in seconds).</summary>
        public static float Breathe(float time, float hz, float lo)
        {
            float s = 0.5f + 0.5f * (float)Math.Sin(time * hz * 2.0 * Math.PI);
            return lo + (1f - lo) * s;
        }
    }

    /// <summary>Spreads an emission rate (per second) over frames without losing the fractions; capped per frame so a long
    /// frame never bursts.</summary>
    public struct EmitAccumulator
    {
        float _carry;

        public int Take(float rate, float dt, int cap)
        {
            if (!(rate > 0f) || !(dt > 0f)) return 0;
            _carry += rate * dt;
            int n = (int)_carry;
            _carry -= n;
            if (n > cap) { n = cap; _carry = 0f; }
            return n;
        }

        public void Reset() => _carry = 0f;
    }

    /// <summary>The screen grade's channels (Time Dilation, the Rewind, its snap, the Shadow Step, a kill) mixed into one
    /// volume: the volume's weight is the strongest channel, each parameter the strength-weighted average of the channels'
    /// targets (so one channel alone reaches exactly its own look, and two blend).</summary>
    public sealed class ScreenGradeMix
    {
        public const int Saturation = 0, HueShift = 1, Contrast = 2, Chroma = 3, Lens = 4, Vignette = 5,
            FilterR = 6, FilterG = 7, FilterB = 8, VignetteR = 9, VignetteG = 10, VignetteB = 11, ParamCount = 12;

        readonly float[] _sum = new float[ParamCount];
        float _weightSum, _max;

        public void Begin()
        {
            Array.Clear(_sum, 0, ParamCount);
            _weightSum = 0f;
            _max = 0f;
        }

        /// <summary>A channel at <paramref name="strength"/> (0..1) wanting <paramref name="targets"/> (ParamCount values).</summary>
        public void Add(float strength, float[] targets)
        {
            strength = HellFxCurves.Clamp01(strength);
            if (strength <= 0f || targets == null) return;
            for (int i = 0; i < ParamCount && i < targets.Length; i++) _sum[i] += targets[i] * strength;
            _weightSum += strength;
            if (strength > _max) _max = strength;
        }

        public float Weight => _max;
        public float Get(int param) => _weightSum > 0f ? _sum[param] / _weightSum : 0f;
    }
}
