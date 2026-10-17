using System;
using System.Numerics;

namespace JamKit.Skyline
{
    /// <summary>
    /// Lava contact (pure): damage per second while the feet touch the lava; staying in it for <see cref="SinkSeconds"/> sinks
    /// you: <see cref="Tick"/> reports the sink once (the caller deals the heavy hit and respawns at the last safe spot), and the
    /// contact clock starts over. Immune (a skill) takes no damage and never sinks. The state is a plain value for the Rewind.
    /// </summary>
    public sealed class LavaContact
    {
        public struct State
        {
            public float contact;
            public bool touching;
        }

        public float Dps, SinkDamage, SinkSeconds;
        public State S;

        public LavaContact(float dps, float sinkDamage, float sinkSeconds)
        {
            Dps = dps; SinkDamage = sinkDamage; SinkSeconds = sinkSeconds;
        }

        /// <summary>The damage this step (0 outside the lava or immune); <paramref name="sank"/> true on the step contact passes the sink time.</summary>
        public float Tick(float dt, bool touching, bool immune, out bool sank)
        {
            sank = false;
            S.touching = touching;
            if (!touching || immune || !(dt > 0f)) { if (!touching || immune) S.contact = 0f; return 0f; }
            S.contact += dt;
            if (S.contact >= SinkSeconds)
            {
                sank = true;
                S.contact = 0f;
                return SinkDamage;
            }
            return Dps * dt;
        }

        /// <summary>Feet touching a lava box: inside its footprint and no higher than a hand above its surface.</summary>
        public static bool Touching(CoverBox lava, Vector3 feet, float above = 0.15f) =>
            feet.X > lava.min.X && feet.X < lava.max.X && feet.Z > lava.min.Z && feet.Z < lava.max.Z &&
            feet.Y <= lava.max.Y + above && feet.Y >= lava.min.Y - 1f;
    }

    public enum CollapseState { Solid, Shaking, Fallen }

    /// <summary>
    /// A collapsing platform (pure): Solid until the player stands on it (or, for a timeline tile, until the room clock
    /// passes its time), then Shaking for <see cref="ShakeSeconds"/>, then Fallen (no collider) until
    /// <see cref="ReturnSeconds"/> have passed (negative: never), when it comes back Solid, unless the player's body is in
    /// its place (it waits). Leaving a shaking platform does not save it.
    /// </summary>
    public sealed class CollapseCycle
    {
        public struct State
        {
            public CollapseState state;
            public float t;
        }

        public float ShakeSeconds, ReturnSeconds;
        /// <summary>How long the fall's animation takes (the visual: it drops and fades; the collider is gone at once).</summary>
        public const float FallSeconds = 0.7f;
        public State S;

        public CollapseCycle(float shakeSeconds, float returnSeconds)
        {
            ShakeSeconds = shakeSeconds; ReturnSeconds = returnSeconds;
        }

        public bool Solid => S.state != CollapseState.Fallen;

        /// <summary>0..1 through the shake (0 when not shaking).</summary>
        public float Shake => S.state == CollapseState.Shaking && ShakeSeconds > 0f ? Math.Min(1f, S.t / ShakeSeconds) : 0f;
        /// <summary>0..1 through the fall's animation (1 once down).</summary>
        public float Fall => S.state == CollapseState.Fallen ? Math.Min(1f, S.t / FallSeconds) : 0f;

        /// <summary>Starts the shake now (the landing, or a timeline tile's time).</summary>
        public void Trigger()
        {
            if (S.state != CollapseState.Solid) return;
            S.state = CollapseState.Shaking;
            S.t = 0f;
        }

        /// <summary>Returns true on the step it falls.</summary>
        public bool Tick(float dt, bool standing, bool bodyInPlace)
        {
            if (!(dt > 0f)) return false;
            switch (S.state)
            {
                case CollapseState.Solid:
                    if (standing) Trigger();
                    return false;
                case CollapseState.Shaking:
                    S.t += dt;
                    if (S.t >= ShakeSeconds) { S.state = CollapseState.Fallen; S.t = 0f; return true; }
                    return false;
                default:
                    S.t += dt;
                    if (ReturnSeconds >= 0f && S.t >= ReturnSeconds && !bodyInPlace) { S.state = CollapseState.Solid; S.t = 0f; }
                    return false;
            }
        }

        public void Reset() { S = default(State); }

        /// <summary>Standing on a platform: grounded, the feet inside its footprint (with the body's radius of slack) and at its top.</summary>
        public static bool StandingOn(CoverBox top, Vector3 feet, bool grounded, float radius = 0.3f) =>
            grounded && feet.X > top.min.X - radius && feet.X < top.max.X + radius && feet.Z > top.min.Z - radius &&
            feet.Z < top.max.Z + radius && Math.Abs(feet.Y - top.max.Y) < 0.2f;

        /// <summary>The player's body (feet to <paramref name="height"/>, a <paramref name="radius"/> round) overlaps the box.</summary>
        public static bool BodyIn(CoverBox b, Vector3 feet, float height, float radius = 0.4f) =>
            feet.X + radius > b.min.X && feet.X - radius < b.max.X && feet.Z + radius > b.min.Z && feet.Z - radius < b.max.Z &&
            feet.Y + height > b.min.Y && feet.Y < b.max.Y;
    }

    public enum BeamState { Off, Warning, On }

    /// <summary>A beam lane's cycle (pure): on for <c>on</c> seconds, off for <c>off</c>, the last <see cref="WarningSeconds"/>
    /// of the off time a warning (the lane flickers). It hits a body that crosses its band; sliding (a 1 m body) passes under a
    /// beam at 1.3 m. A hit is dealt at most every <see cref="HitEvery"/> seconds.</summary>
    public static class BeamCycle
    {
        public const float WarningSeconds = 0.4f, Damage = 25f, HitEvery = 0.5f;

        public static BeamState At(float time, float on, float off, float phase)
        {
            float period = on + off;
            if (!(period > 0f)) return BeamState.On;
            float t = (time + phase) % period;
            if (t < 0f) t += period;
            if (t < on) return BeamState.On;
            return t >= period - WarningSeconds ? BeamState.Warning : BeamState.Off;
        }

        public static bool Hits(CoverBox beam, Vector3 feet, float height, float radius = 0.38f) =>
            feet.X + radius > beam.min.X && feet.X - radius < beam.max.X && feet.Z + radius > beam.min.Z && feet.Z - radius < beam.max.Z &&
            feet.Y < beam.max.Y && feet.Y + height > beam.min.Y;
    }

    /// <summary>
    /// The Collapse's falling debris (pure; the drops are <see cref="HellRoomDef.debris"/>, made by
    /// <see cref="HellRoomLayouts.CollapseDebris"/>). Everything follows from the room clock: a drop's shadow glows on the floor
    /// from <see cref="TelegraphSeconds"/> before it lands (<see cref="Telegraph"/>), the rock falls in the last
    /// <see cref="FallSeconds"/> of that, it hits a body within its radius on the step it lands (<see cref="Lands"/>), and its
    /// rubble shows for <see cref="RubbleSeconds"/>. A Rewind of the room clock rewinds it all.
    /// </summary>
    public static class HellDebris
    {
        /// <summary>The shadow's warning before the hit, the rock's visible fall, the rubble's life.</summary>
        public const float TelegraphSeconds = 1f, FallSeconds = 0.45f, RubbleSeconds = 0.6f;
        /// <summary>The hit, its radius, and the height the rock falls from.</summary>
        public const float Damage = 30f, Radius = 1.5f, DropHeight = 12f;
        /// <summary>Seconds between drops in a lane (seeded between these).</summary>
        public const float MinGap = 1.3f, MaxGap = 2.1f;
        /// <summary>How far ahead of the lane's front a drop lands (seeded between these): where a runner on pace is.</summary>
        public const float MinLead = 7f, MaxLead = 24f;
        /// <summary>The most a drop lands to either side of the lane's running line.</summary>
        public const float SideJitter = 0.7f;

        /// <summary>0..1 through the shadow's warning (0 before it, 1 at the landing); -1 when not telegraphing.</summary>
        public static float Telegraph(HellDebrisDrop d, float clock)
        {
            float t = clock - (d.time - TelegraphSeconds);
            if (t < 0f || clock >= d.time) return -1f;
            return t / TelegraphSeconds;
        }

        /// <summary>The drop landed in (<paramref name="before"/>, <paramref name="now"/>] (the room's step).</summary>
        public static bool Lands(HellDebrisDrop d, float before, float now) => d.time > before && d.time <= now;

        /// <summary>A body with its feet here is hit by the landing (within the radius across, and on the drop's floor).</summary>
        public static bool Hits(HellDebrisDrop d, Vector3 feet)
        {
            float dx = feet.X - d.position.X, dz = feet.Z - d.position.Z;
            return dx * dx + dz * dz <= d.radius * d.radius && feet.Y > d.position.Y - 1f && feet.Y < d.position.Y + 2f;
        }

        /// <summary>The rock's height above the floor at <paramref name="clock"/> (falling in the last <see cref="FallSeconds"/>),
        /// or -1 when it is not in the air.</summary>
        public static float RockHeight(HellDebrisDrop d, float clock)
        {
            float left = d.time - clock;
            if (left < 0f || left > FallSeconds) return -1f;
            float k = left / FallSeconds;
            return DropHeight * k * k;
        }

        /// <summary>The rubble shows for a moment after the landing.</summary>
        public static bool Rubble(HellDebrisDrop d, float clock) => clock >= d.time && clock < d.time + RubbleSeconds;
    }

    /// <summary>The last safe spot (pure): where the feet last stood on solid, non-hazard ground for <see cref="Settle"/> seconds.
    /// A sink in lava respawns you here.</summary>
    public sealed class SafeSpot
    {
        public const float Settle = 0.3f;
        public Vector3 Spot;
        public float Yaw;
        float _t;

        public SafeSpot(Vector3 start, float yaw) { Spot = start; Yaw = yaw; }

        public void Tick(float dt, Vector3 feet, float yaw, bool groundedOnSafe)
        {
            if (!groundedOnSafe) { _t = 0f; return; }
            _t += dt;
            if (_t >= Settle) { Spot = feet; Yaw = yaw; }
        }

        public void Reset(Vector3 start, float yaw) { Spot = start; Yaw = yaw; _t = 0f; }
    }
}
