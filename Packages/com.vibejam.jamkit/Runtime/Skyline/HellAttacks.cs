using System;
using System.Collections.Generic;
using System.Numerics;

namespace JamKit.Skyline
{
    /// <summary>The fire imp's aim, in numbers (docs/HELL-ENEMIES.md): the drone's lagged aim with a bolt three times as fast. A
    /// target moving across the line of fire is missed by what it covers in the lag plus the bolt's flight.</summary>
    public static class HellImpAim
    {
        /// <summary>The bolt's flight time to <paramref name="range"/> (s).</summary>
        public static float FlightSeconds(EnemyDef d, float range) => d.projectileSpeed > 0f ? range / d.projectileSpeed : float.PositiveInfinity;

        /// <summary>How far (m) a target moving across the line at <paramref name="speedAcross"/> m/s is from where the bolt arrives:
        /// it aims at where the chest was <see cref="EnemyDef.aimLagSeconds"/> ago and the bolt takes its flight time to get there.</summary>
        public static float MissAcross(EnemyDef d, float speedAcross, float range) =>
            Math.Abs(speedAcross) * (d.aimLagSeconds + FlightSeconds(d, range));

        /// <summary>The width a bolt must pass within to hit (m): the player's capsule radius plus the bolt's radius.</summary>
        public static float HitWidth(EnemyDef d, float playerRadius = 0.38f) => playerRadius + d.projectileRadius;
    }

    /// <summary>Distances between points and segments (the scythe's beam against the player's capsule).</summary>
    public static class HellGeometry
    {
        /// <summary>The closest point to <paramref name="p"/> on segment a-b, as a share of the way from a (0 to 1).</summary>
        public static float ClosestT(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float l2 = ab.LengthSquared();
            if (!(l2 > 1e-12f)) return 0f;
            float t = Vector3.Dot(p - a, ab) / l2;
            return t < 0f ? 0f : t > 1f ? 1f : t;
        }

        public static float PointSegment(Vector3 p, Vector3 a, Vector3 b) => Vector3.Distance(p, a + (b - a) * ClosestT(a, b, p));

        /// <summary>The shortest distance between segments p1-q1 and p2-q2 (Ericson, Real-Time Collision Detection 5.1.9).</summary>
        public static float SegmentSegment(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
        {
            Vector3 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2;
            float a = d1.LengthSquared(), e = d2.LengthSquared(), f = Vector3.Dot(d2, r);
            float s, t;
            const float Eps = 1e-10f;
            if (a <= Eps && e <= Eps) return Vector3.Distance(p1, p2);
            if (a <= Eps)
            {
                s = 0f;
                t = Clamp01(f / e);
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= Eps)
                {
                    t = 0f;
                    s = Clamp01(-c / a);
                }
                else
                {
                    float b = Vector3.Dot(d1, d2), denom = a * e - b * b;
                    s = denom > Eps ? Clamp01((b * f - c * e) / denom) : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f) { t = 0f; s = Clamp01(-c / a); }
                    else if (t > 1f) { t = 1f; s = Clamp01((b - c) / a); }
                }
            }
            return Vector3.Distance(p1 + d1 * s, p2 + d2 * t);
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }

    public enum ScythePhase { Telegraph, Sweep, Hold, Fade, Done }

    /// <summary>One scythe's path: the blade sweeps from <see cref="from"/> to <see cref="to"/>.</summary>
    public struct ScythePath
    {
        public Vector3 from, to;
        public float Length => Vector3.Distance(from, to);
    }

    /// <summary>
    /// One demon scythe's timeline (docs/HELL-ENEMIES.md): it appears and PROJECTS its path as a line for <see cref="telegraph"/>
    /// seconds (harmless), then BEAMS along it: the blade sweeps from the start to the end at <see cref="speed"/> m/s and the beam
    /// fills the path behind it; the full beam holds <see cref="hold"/> seconds, then fades (<see cref="FadeSeconds"/>, harmless).
    /// Touching the beam (within <see cref="hitRadius"/> of the lit part) while it sweeps or holds hits once. Pure and frame-rate
    /// independent; ticked on scaled time, so Time Dilation and Stasis slow it.
    /// </summary>
    public sealed class ScytheBeam
    {
        public const float FadeSeconds = 0.3f;

        public ScythePath path;
        public float telegraph, speed, hold, hitRadius, damage;
        float _t;
        bool _hit;

        public ScytheBeam(ScythePath path, EnemyDef d) : this(path, d.scytheTelegraph, d.scytheBeamSpeed, d.scytheHold, d.scytheHitRadius, d.scytheDamage) { }

        public ScytheBeam(ScythePath path, float telegraph, float speed, float hold, float hitRadius, float damage)
        {
            this.path = path;
            this.telegraph = Math.Max(0f, telegraph);
            this.speed = speed > 0.01f ? speed : 0.01f;
            this.hold = Math.Max(0f, hold);
            this.hitRadius = Math.Max(0f, hitRadius);
            this.damage = damage;
        }

        public float T => _t;
        public bool HasHit => _hit;
        public float SweepSeconds => path.Length / speed;
        public float TotalSeconds => telegraph + SweepSeconds + hold + FadeSeconds;

        public ScythePhase Phase =>
            _t < telegraph ? ScythePhase.Telegraph :
            _t < telegraph + SweepSeconds ? ScythePhase.Sweep :
            _t < telegraph + SweepSeconds + hold ? ScythePhase.Hold :
            _t < TotalSeconds ? ScythePhase.Fade : ScythePhase.Done;

        /// <summary>How far through its telegraph it is (0 to 1; 1 once it beams).</summary>
        public float TelegraphProgress => telegraph > 0f ? Math.Min(1f, _t / telegraph) : 1f;

        /// <summary>The blade's share of the way along the path (0 before the sweep, 1 after).</summary>
        public float Blade
        {
            get
            {
                float s = SweepSeconds;
                if (_t <= telegraph) return 0f;
                return s > 0f ? Math.Min(1f, (_t - telegraph) / s) : 1f;
            }
        }

        public Vector3 BladePosition => Vector3.Lerp(path.from, path.to, Blade);

        /// <summary>The beam hurts now (sweeping or holding).</summary>
        public bool Lethal { get { var p = Phase; return p == ScythePhase.Sweep || p == ScythePhase.Hold; } }

        /// <summary>The part of the path that is lit and hurts (from the start to the blade); false while it does not hurt.</summary>
        public bool LitSegment(out Vector3 a, out Vector3 b)
        {
            a = path.from;
            b = BladePosition;
            return Lethal;
        }

        public void Tick(float dt)
        {
            if (!(dt > 0f) || float.IsInfinity(dt)) return;
            _t += dt;
        }

        /// <summary>Whether the lit beam touches a capsule from <paramref name="lo"/> to <paramref name="hi"/> of radius
        /// <paramref name="radius"/> (the player: feet to head). Does not mark the hit.</summary>
        public bool Touches(Vector3 lo, Vector3 hi, float radius)
        {
            if (!LitSegment(out var a, out var b)) return false;
            return HellGeometry.SegmentSegment(a, b, lo, hi) <= hitRadius + radius;
        }

        /// <summary>One step's hit test: true the first time the lit beam touches the capsule (the damage lands then); after that
        /// this scythe never hits again.</summary>
        public bool TryHit(Vector3 lo, Vector3 hi, float radius)
        {
            if (_hit || !Touches(lo, hi, radius)) return false;
            _hit = true;
            return true;
        }
    }

    /// <summary>
    /// Where a scythe demon's scythes go (docs/HELL-ENEMIES.md). The first starts beside the demon and runs at the player (a lane
    /// out of the demon); the rest are parallel lanes across the player's position at a seeded angle, the middle one through where
    /// the player stands (standing still is punished) and the others <see cref="EnemyDef.scytheSpacing"/> apart either side, so the
    /// safe spots are the gaps between lanes. Each lane is <see cref="EnemyDef.scytheLength"/> long, centred on its anchor, at
    /// <see cref="EnemyDef.scytheHeight"/> above its ground; sweep directions alternate.
    /// </summary>
    public static class ScytheVolley
    {
        /// <summary>The paths for one volley. <paramref name="demonFeet"/> and <paramref name="playerFeet"/> are ground points.</summary>
        public static void Plan(Vector3 demonFeet, Vector3 playerFeet, EnemyDef d, Rng rng, List<ScythePath> into)
        {
            into.Clear();
            int n = Math.Max(0, d.scytheCount);
            if (n == 0) return;
            Vector3 up = new Vector3(0f, d.scytheHeight, 0f);
            Vector3 flat = new Vector3(playerFeet.X - demonFeet.X, 0f, playerFeet.Z - demonFeet.Z);
            float fl = flat.Length();
            Vector3 fwd = fl > 1e-3f ? flat / fl : new Vector3(0f, 0f, 1f);
            float len = Math.Max(1f, d.scytheLength);

            // 1: out of the demon at the player
            Vector3 start = demonFeet + fwd * 1.2f + up;
            into.Add(new ScythePath { from = start, to = start + fwd * len });

            // the rest: parallel lanes across the player at an angle of 35-90 degrees to the line from the demon
            int rest = n - 1;
            if (rest <= 0) return;
            float side = rng.NextFloat() < 0.5f ? -1f : 1f;
            float ang = side * (35f + 55f * rng.NextFloat()) * (MathF.PI / 180f);
            Vector3 dir = Rotate(fwd, ang);
            Vector3 across = new Vector3(dir.Z, 0f, -dir.X);
            float jitter = (rng.NextFloat() - 0.5f) * 0.6f;   // the middle lane within 0.3 m of the player
            for (int i = 0; i < rest; i++)
            {
                // offsets 0, +1, -1, +2, -2 lanes
                int k = (i + 1) / 2 * (i % 2 == 1 ? 1 : -1);
                Vector3 centre = playerFeet + across * (k * d.scytheSpacing + jitter) + up;
                Vector3 half = dir * (len * 0.5f);
                bool flip = i % 2 == 1;
                into.Add(flip ? new ScythePath { from = centre + half, to = centre - half } : new ScythePath { from = centre - half, to = centre + half });
            }
        }

        static Vector3 Rotate(Vector3 v, float rad)
        {
            float c = MathF.Cos(rad), s = MathF.Sin(rad);
            return new Vector3(v.X * c + v.Z * s, 0f, -v.X * s + v.Z * c);
        }
    }

    /// <summary>A room's "beam lane" (a scythe on a fixed dangerous path, no demon): it fires every <see cref="period"/> seconds,
    /// starting <see cref="offset"/> seconds in. Pure clock; the Unity lane spawns a scythe on each fire.</summary>
    public sealed class BeamLane
    {
        public float period, offset;
        float _t;
        int _fired;

        public BeamLane(float period, float offset = 0f)
        {
            this.period = period > 0.1f ? period : 0.1f;
            this.offset = Math.Max(0f, offset);
        }

        public float T => _t;

        /// <summary>Advances the clock; returns how many scythes fire in this step (0 or 1 at sane frame rates).</summary>
        public int Tick(float dt)
        {
            if (!(dt > 0f) || float.IsInfinity(dt)) return 0;
            _t += dt;
            int due = _t < offset ? 0 : 1 + (int)((_t - offset) / period);
            int fire = due - _fired;
            _fired = due;
            return fire > 0 ? fire : 0;
        }
    }

    public enum LavaPoolPhase { Spreading, Burning, Cooling, Gone }

    /// <summary>
    /// The lava ooze's pool (docs/HELL-ENEMIES.md; the floor-care unit's acid idea): it spreads to its radius over
    /// <see cref="EnemyDef.poolGrowSeconds"/>, burns for <see cref="EnemyDef.poolBurnSeconds"/> (damage while your feet are in it),
    /// then cools and fades for <see cref="EnemyDef.poolCoolSeconds"/> (harmless; it darkens and shrinks a little) and is gone.
    /// Damage comes in ticks of <see cref="EnemyDef.poolTick"/> seconds (<c>poolDps * poolTick</c> each); the first touch ticks at once,
    /// then at most one tick per interval. Spreading counts as burning (stepping into it as it spreads hurts).
    /// </summary>
    public sealed class LavaPool
    {
        public Vector3 center;
        public float radius, grow, burn, cool, dps, tick;
        /// <summary>Feet this far above the surface are clear of it (a jump).</summary>
        public float clearHeight = 0.35f;
        float _t, _sinceTick, _coolStart;

        public LavaPool(Vector3 center, EnemyDef d)
            : this(center, d.poolRadius, d.poolGrowSeconds, d.poolBurnSeconds, d.poolCoolSeconds, d.poolDps, d.poolTick) { }

        public LavaPool(Vector3 center, float radius, float grow, float burn, float cool, float dps, float tick)
        {
            this.center = center;
            this.radius = Math.Max(0.1f, radius);
            this.grow = Math.Max(0f, grow);
            this.burn = Math.Max(0f, burn);
            this.cool = Math.Max(0.01f, cool);
            this.dps = dps;
            this.tick = tick > 0.02f ? tick : 0.02f;
            _sinceTick = this.tick;
            _coolStart = this.grow + this.burn;
        }

        public float T => _t;
        public LavaPoolPhase Phase =>
            _t < Math.Min(grow, _coolStart) ? LavaPoolPhase.Spreading :
            _t < _coolStart ? LavaPoolPhase.Burning :
            _t < _coolStart + cool ? LavaPoolPhase.Cooling : LavaPoolPhase.Gone;

        public bool Burning { get { var p = Phase; return p == LavaPoolPhase.Spreading || p == LavaPoolPhase.Burning; } }
        public bool Gone => Phase == LavaPoolPhase.Gone;

        /// <summary>0 hot to 1 cold, through the cooling.</summary>
        public float Cooled => _t <= _coolStart ? 0f : Math.Min(1f, (_t - _coolStart) / cool);

        /// <summary>The radius now: spreading out, full while it burns, shrinking to 85% as it cools.</summary>
        public float CurrentRadius
        {
            get
            {
                if (_t < grow && grow > 0f)
                {
                    float k = _t / grow;
                    return radius * (1f - (1f - k) * (1f - k));   // fast out, easing in
                }
                return radius * (1f - 0.15f * Cooled);
            }
        }

        /// <summary>Starts it cooling now (the ooze laid one pool too many).</summary>
        public void CoolNow()
        {
            if (_t < _coolStart) _coolStart = _t;
        }

        /// <summary>Whether feet at <paramref name="feet"/> stand in it (within its radius on the floor plan, on its surface).</summary>
        public bool Contains(Vector3 feet)
        {
            float dy = feet.Y - center.Y;
            if (dy > clearHeight || dy < -0.6f) return false;
            float dx = feet.X - center.X, dz = feet.Z - center.Z;
            float r = CurrentRadius;
            return dx * dx + dz * dz <= r * r;
        }

        /// <summary>One step; returns the damage to deal this step (0 most steps).</summary>
        public float Tick(float dt, bool feetInside)
        {
            if (!(dt > 0f) || float.IsInfinity(dt)) return 0f;
            _t += dt;
            _sinceTick += dt;
            if (!feetInside || !Burning) return 0f;
            if (_sinceTick < tick) return 0f;
            _sinceTick = 0f;
            return dps * tick;
        }
    }

    /// <summary>
    /// The lava ooze's throb (docs/HELL-ENEMIES.md): its scale and glow beat at <see cref="EnemyDef.pulseIdleHz"/> asleep and
    /// <see cref="EnemyDef.pulseAwakeHz"/> awake, and through its wind-up the beat speeds up to <see cref="EnemyDef.pulseTelegraphHz"/>,
    /// so the attack reads from the rhythm. The phase only ever moves forward (no jump when the rate changes).
    /// </summary>
    public sealed class OozePulse
    {
        readonly EnemyDef _def;
        float _phase;

        public OozePulse(EnemyDef def, float phase = 0f)
        {
            _def = def ?? throw new ArgumentNullException(nameof(def));
            _phase = phase - MathF.Floor(phase);
        }

        /// <summary>0 to 1, one beat per unit.</summary>
        public float Phase => _phase;

        /// <summary>Beats per second for a bot <paramref name="awake"/> or not, <paramref name="windUp"/> of the way through its wind-up
        /// (0 when not winding up).</summary>
        public float Rate(bool awake, float windUp)
        {
            float b = awake ? _def.pulseAwakeHz : _def.pulseIdleHz;
            float k = windUp < 0f ? 0f : windUp > 1f ? 1f : windUp;
            return b + (_def.pulseTelegraphHz - b) * k * k;
        }

        public void Tick(float dt, bool awake, float windUp)
        {
            if (!(dt > 0f) || float.IsInfinity(dt)) return;
            _phase += dt * Rate(awake, windUp);
            _phase -= MathF.Floor(_phase);
        }

        /// <summary>The beat now: 0 at rest, 1 at the peak, a quick swell and a slower fall (a heartbeat).</summary>
        public float Throb
        {
            get
            {
                float s = MathF.Sin(_phase * 2f * MathF.PI);
                return s > 0f ? s * s * s : 0f;
            }
        }

        /// <summary>The blob's scale now: it swells sideways and squats on each beat.</summary>
        public Vector3 Scale
        {
            get
            {
                float a = _def.pulseAmount * Throb;
                return new Vector3(1f + a, 1f - a * 0.6f, 1f + a);
            }
        }

        /// <summary>The glow's multiplier now (1 at rest, up to 2.5 at a beat).</summary>
        public float Glow => 1f + 1.5f * Throb;
    }
}
