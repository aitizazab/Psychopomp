using System;
using System.Collections.Generic;
using System.Numerics;

namespace JamKit.Skyline
{
    // The Hell descent's player abilities, pure parts (docs/HELL-ABILITIES.md): cooldowns, the Time Dilation curve and its
    // envelope, the time-scale requests (one owner, the minimum wins), the Rewind's snapshot ring, the Shadow Step's target
    // planner, and the floor skills' data and offer. No engine: the Unity side (JamKit.SkylineRepo, Hell*.cs) plays them.

    /// <summary>An ability's cooldown in seconds. <see cref="TryUse"/> starts it when ready.</summary>
    public sealed class AbilityCooldown
    {
        public float Duration;
        public float Remaining { get; private set; }
        public bool Ready => Remaining <= 0f;
        /// <summary>0 just used, 1 ready.</summary>
        public float Fraction => Duration > 0f ? 1f - Math.Min(1f, Math.Max(0f, Remaining / Duration)) : 1f;

        public AbilityCooldown(float duration) { Duration = duration; }

        public void Tick(float dt)
        {
            if (dt > 0f && Remaining > 0f) Remaining = Math.Max(0f, Remaining - dt);
        }

        public bool TryUse()
        {
            if (!Ready) return false;
            Remaining = Duration;
            return true;
        }

        /// <summary>Sets the time left (a rewind may restore it; a skill may refund it).</summary>
        public void Set(float remaining) => Remaining = Math.Max(0f, Math.Min(Duration, remaining));
        public void Reset() => Remaining = 0f;
    }

    /// <summary>How strong Time Dilation is for the player's speed at the aim press: below <see cref="noneBelow"/> m/s no
    /// slow-down; at <see cref="fullAt"/> m/s and above <see cref="minScale"/>; between, a power curve
    /// (<see cref="exponent"/> 1 = straight). Speeds are horizontal (walk 5, sprint 9.5, a slide boost up to 13).</summary>
    public sealed class DilationCurve
    {
        public float noneBelow = 2.5f;
        public float fullAt = 10.5f;
        public float minScale = 0.35f;
        public float exponent = 1f;

        public float Scale(float speed)
        {
            if (!(speed > noneBelow)) return 1f;
            float span = Math.Max(0.01f, fullAt - noneBelow);
            float t = Math.Min(1f, (speed - noneBelow) / span);
            if (exponent > 0f && Math.Abs(exponent - 1f) > 1e-4f) t = (float)Math.Pow(t, exponent);
            return 1f + (minScale - 1f) * t;
        }
    }

    /// <summary>
    /// Time Dilation's envelope, on real (unscaled) time: <see cref="Press"/> with the target scale; the scale eases from 1
    /// to the target over <see cref="easeIn"/>, holds until <see cref="holdSeconds"/> after the press, then eases back to 1
    /// over <see cref="easeOut"/>. A new press restarts from wherever the scale is now (no jump). Smoothstep eases.
    /// </summary>
    public sealed class TimeDilationState
    {
        public float holdSeconds = 3f;
        public float easeIn = 0.15f;
        public float easeOut = 0.15f;

        float _since = float.PositiveInfinity, _target = 1f, _from = 1f;

        public bool Active => _since < holdSeconds + easeOut;
        public float Target => _target;
        public float Scale { get; private set; } = 1f;
        /// <summary>0 normal time, 1 the strongest slow-down (0.35x at the default curve): for the screen effect and the pitch.</summary>
        public float Strength(float minScale) => minScale < 1f ? Math.Max(0f, Math.Min(1f, (1f - Scale) / (1f - minScale))) : 0f;

        public void Press(float targetScale)
        {
            _from = Scale;
            _target = Math.Max(0.01f, Math.Min(1f, targetScale));
            _since = 0f;
        }

        public void Cancel()
        {
            _since = float.PositiveInfinity;
            Scale = 1f;
        }

        public float Tick(float unscaledDt)
        {
            if (!(unscaledDt > 0f)) unscaledDt = 0f;
            if (float.IsPositiveInfinity(_since)) { Scale = 1f; return Scale; }
            _since += unscaledDt;
            if (_since < easeIn) Scale = Lerp(_from, _target, Smooth(_since / Math.Max(1e-4f, easeIn)));
            else if (_since < holdSeconds) Scale = _target;
            else if (_since < holdSeconds + easeOut) Scale = Lerp(_target, 1f, Smooth((_since - holdSeconds) / Math.Max(1e-4f, easeOut)));
            else { Scale = 1f; _since = float.PositiveInfinity; }
            return Scale;
        }

        static float Smooth(float t) { t = Math.Max(0f, Math.Min(1f, t)); return t * t * (3f - 2f * t); }
        static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }

    /// <summary>Everything that wants the game slower asks here by key (Time Dilation, the hit stop, a skill); the game's
    /// time scale is the smallest request (1 when there are none). A request of 1 or more removes the key.</summary>
    public sealed class TimeScaleRequests
    {
        readonly Dictionary<string, float> _requests = new Dictionary<string, float>();

        public float Scale { get; private set; } = 1f;
        public int Count => _requests.Count;

        public float Request(string key, float scale)
        {
            if (string.IsNullOrEmpty(key)) return Scale;
            if (!(scale < 1f)) _requests.Remove(key);
            else _requests[key] = Math.Max(0f, scale);
            return Recompute();
        }

        public float Clear(string key) => Request(key, 1f);

        public void ClearAll()
        {
            _requests.Clear();
            Scale = 1f;
        }

        public bool Has(string key) => _requests.ContainsKey(key);

        float Recompute()
        {
            float s = 1f;
            foreach (var v in _requests.Values) if (v < s) s = v;
            Scale = s;
            return s;
        }
    }

    /// <summary>A fixed-size ring of timestamped snapshots (the Rewind's buffer): <see cref="Push"/> adds the newest, dropping the
    /// oldest when full. <see cref="FindAtOrBefore"/> finds the newest one taken at or before a time.</summary>
    public sealed class SnapshotRing<T>
    {
        readonly T[] _items;
        readonly double[] _times;
        int _start, _count;

        public SnapshotRing(int capacity)
        {
            if (capacity < 1) capacity = 1;
            _items = new T[capacity];
            _times = new double[capacity];
        }

        public int Capacity => _items.Length;
        public int Count => _count;

        /// <summary>Capacity for <paramref name="seconds"/> of history at one push every <paramref name="step"/> seconds, plus a margin.</summary>
        public static int CapacityFor(float seconds, float step) => (int)Math.Ceiling(seconds / Math.Max(1e-4f, step)) + 4;

        public void Push(double time, T item)
        {
            int at;
            if (_count < _items.Length) { at = (_start + _count) % _items.Length; _count++; }
            else { at = _start; _start = (_start + 1) % _items.Length; }
            _items[at] = item;
            _times[at] = time;
        }

        /// <summary>The i-th oldest (0 = oldest).</summary>
        public T this[int i] => _items[(_start + i) % _items.Length];
        public double TimeAt(int i) => _times[(_start + i) % _items.Length];
        public double OldestTime => _count > 0 ? TimeAt(0) : double.NaN;
        public double NewestTime => _count > 0 ? TimeAt(_count - 1) : double.NaN;

        /// <summary>Index of the newest snapshot taken at or before <paramref name="time"/>; the oldest one when all are later;
        /// -1 when empty.</summary>
        public int FindAtOrBefore(double time)
        {
            if (_count == 0) return -1;
            for (int i = _count - 1; i >= 0; i--) if (TimeAt(i) <= time + 1e-9) return i;
            return 0;
        }

        public void Clear()
        {
            for (int i = 0; i < _items.Length; i++) _items[i] = default(T);
            _start = 0;
            _count = 0;
        }
    }

    // ------------------------------------------------------------------ Shadow Step

    /// <summary>The world as the blink planner sees it (the Unity side: physics casts against level geometry only).</summary>
    public interface IBlinkWorld
    {
        /// <summary>First solid hit along the ray within <paramref name="maxDistance"/>.</summary>
        bool Raycast(Vector3 from, Vector3 dir, float maxDistance, out Vector3 point, out Vector3 normal);
        /// <summary>The standing body (a capsule of the given radius and height, feet at <paramref name="feet"/>) fits there.</summary>
        bool BodyFits(Vector3 feet, float radius, float height);
    }

    public sealed class BlinkDef
    {
        /// <summary>The longest blink, eye to landing (the creator's ~18 m).</summary>
        public float range = 18f;
        /// <summary>A surface counts as ground when its normal's up part is at least this (0.7 = about 45 degrees).</summary>
        public float minGroundUp = 0.7f;
        /// <summary>Aimed at a wall: how high above the hit a ledge top may be (a mantle-sized reach).</summary>
        public float ledgeReach = 2.2f;
        /// <summary>Aimed at the sky or past range: the ground below the end point may be this far down.</summary>
        public float dropReach = 6f;
        public float bodyRadius = 0.4f;
        public float bodyHeight = 1.8f;
        /// <summary>Line of sight is checked to this height above the landing (chest).</summary>
        public float sightHeight = 1.2f;
        /// <summary>The landing is lifted this much off the surface (the engine then finds the ground).</summary>
        public float lift = 0.05f;
        /// <summary>The cooldown (s).</summary>
        public float cooldown = 6f;
        /// <summary>Skills scale the range by this (1 = 18 m).</summary>
        public float rangeScale = 1f;
        public float Range => range * Math.Max(0.1f, rangeScale);
    }

    public enum BlinkKind { None, Ground, Ledge, BelowAim, AtWallFoot }

    public struct BlinkPlan
    {
        public bool valid;
        public BlinkKind kind;
        /// <summary>Where the feet land.</summary>
        public Vector3 feet;
        /// <summary>Why not (empty when valid).</summary>
        public string reason;
    }

    /// <summary>
    /// Where a Shadow Step aimed from <c>eye</c> along <c>aim</c> lands: ground hit directly (within range); a wall hit is tried
    /// as a ledge (the top within <see cref="BlinkDef.ledgeReach"/> above the hit, just past the face), else the foot of the
    /// wall on your side; nothing hit within range: the ground under the end of the aim within <see cref="BlinkDef.dropReach"/>.
    /// Every landing must fit the body, be in line of sight from the eye (to <see cref="BlinkDef.sightHeight"/>) and within
    /// range (+ the ledge reach).
    /// </summary>
    public static class BlinkPlanner
    {
        static readonly Vector3 Up = Vector3.UnitY;

        public static BlinkPlan Plan(Vector3 eye, Vector3 aim, BlinkDef d, IBlinkWorld world)
        {
            if (aim.LengthSquared() < 1e-8f) return Fail("no aim");
            aim = Vector3.Normalize(aim);
            float range = d.Range;
            if (world.Raycast(eye, aim, range, out var p, out var n))
            {
                if (n.Y >= d.minGroundUp) return Check(eye, p, BlinkKind.Ground, d, world);
                if (Math.Abs(n.Y) < d.minGroundUp)
                {
                    // a wall: a ledge on top of it?
                    var flatIn = new Vector3(-n.X, 0f, -n.Z);
                    if (flatIn.LengthSquared() > 1e-6f) flatIn = Vector3.Normalize(flatIn);
                    var probe = p + flatIn * (d.bodyRadius + 0.1f) + Up * d.ledgeReach;
                    if (world.Raycast(probe, -Up, d.ledgeReach + 0.3f, out var top, out var tn) && tn.Y >= d.minGroundUp && top.Y > p.Y - 0.3f)
                    {
                        var plan = Check(eye, top, BlinkKind.Ledge, d, world);
                        if (plan.valid) return plan;
                    }
                    // else the foot of the wall on our side
                    var flatOut = new Vector3(n.X, 0f, n.Z);
                    if (flatOut.LengthSquared() > 1e-6f) flatOut = Vector3.Normalize(flatOut);
                    var back = p + flatOut * (d.bodyRadius + 0.15f);
                    if (world.Raycast(back + Up * 0.3f, -Up, d.dropReach + 0.3f, out var foot, out var fn) && fn.Y >= d.minGroundUp)
                        return Check(eye, foot, BlinkKind.AtWallFoot, d, world);
                    return Fail("wall, no footing");
                }
                return Fail("ceiling");
            }
            var end = eye + aim * range;
            if (world.Raycast(end, -Up, d.dropReach, out var g, out var gn) && gn.Y >= d.minGroundUp)
                return Check(eye, g, BlinkKind.BelowAim, d, world);
            return Fail("no ground in range");
        }

        static BlinkPlan Check(Vector3 eye, Vector3 surface, BlinkKind kind, BlinkDef d, IBlinkWorld world)
        {
            var feet = surface + Up * d.lift;
            float reach = d.Range + (kind == BlinkKind.Ledge || kind == BlinkKind.BelowAim ? Math.Max(d.ledgeReach, d.dropReach) : 0.5f);
            if (Vector3.Distance(eye, feet) > reach) return Fail("out of range");
            if (!world.BodyFits(feet, d.bodyRadius, d.bodyHeight)) return Fail("no room");
            var chest = feet + Up * d.sightHeight;
            var to = chest - eye;
            float len = to.Length();
            if (len > 0.05f && world.Raycast(eye, to / len, len - 0.05f, out _, out _)) return Fail("not in sight");
            return new BlinkPlan { valid = true, kind = kind, feet = feet, reason = "" };
        }

        static BlinkPlan Fail(string why) => new BlinkPlan { valid = false, kind = BlinkKind.None, reason = why };
    }

    // ------------------------------------------------------------------ floor skills

    public enum HellFlavour { Shadow, Time, Fire }

    /// <summary>One floor skill's data: <see cref="id"/> also names the component that plays it (the Unity side's registry).</summary>
    public sealed class HellSkillDef
    {
        public string id = "";
        public string name = "";
        public HellFlavour flavour;
        public string description = "";
        /// <summary>A short glyph for the card (no label text on the HUD; the card shows this big).</summary>
        public string icon = "";
    }

    /// <summary>The floor skills on offer and the 1-of-3 pick.</summary>
    public static class HellSkillPool
    {
        /// <summary>Up to <paramref name="count"/> distinct skills from <paramref name="pool"/> not in <paramref name="owned"/>,
        /// in a shuffled order from <paramref name="nextInt"/> (returns 0..max-1).</summary>
        public static List<HellSkillDef> Offer(IReadOnlyList<HellSkillDef> pool, ICollection<string> owned, int count, Func<int, int> nextInt)
        {
            var open = new List<HellSkillDef>();
            if (pool != null)
                foreach (var s in pool)
                    if (s != null && (owned == null || !owned.Contains(s.id)) && !open.Exists(o => o.id == s.id)) open.Add(s);
            // Fisher-Yates on the open list, then take the first count
            for (int i = open.Count - 1; i > 0; i--)
            {
                int j = nextInt != null ? nextInt(i + 1) : i;
                if (j < 0 || j > i) j = i;
                var t = open[i]; open[i] = open[j]; open[j] = t;
            }
            if (open.Count > count) open.RemoveRange(count, open.Count - count);
            return open;
        }
    }
}
