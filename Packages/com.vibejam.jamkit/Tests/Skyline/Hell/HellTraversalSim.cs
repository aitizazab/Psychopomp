using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using JamKit.Movement;
using JamKit.Skyline;
using JamKit.Tests.Movement;

namespace JamKit.Tests.Skyline
{
    /// <summary>
    /// The Hell rooms as the movement engine sees them (docs/HELL-ROOMS.md, "The traversal simulator"): a <see cref="BoxWorld"/>
    /// built from a <see cref="HellRoomDef"/> (every solid one box or ramp, gates open; every lava basin a solid box at the lava
    /// surface, as the Unity builder makes it; every collapsing platform a box with its own <see cref="CollapseCycle"/> at the
    /// room's shake time), the darkness (the room's dark zones and fire lights, <see cref="DarknessField"/>) and the Shadow Step's
    /// world (<see cref="IBlinkWorld"/> rays against the same boxes). Beams are not solid: a body crossing one fails the leg (a
    /// beam is treated as always on). The Collapse's timeline tiles stay solid (they fall on the room clock, which
    /// HellRoomTests races separately).
    /// </summary>
    public sealed class HellTraversalWorld : IBlinkWorld
    {
        public readonly HellRoomDef Room;
        public readonly BoxWorld World = new BoxWorld();
        public readonly DarknessField Dark = new DarknessField();
        public readonly List<(HellHazard hazard, BoxWorld.Box box, CollapseCycle cycle)> Platforms = new List<(HellHazard, BoxWorld.Box, CollapseCycle)>();
        public readonly List<HellHazard> Lava = new List<HellHazard>();
        public readonly List<HellHazard> Beams = new List<HellHazard>();
        readonly bool _mirror;
        readonly int _turns;

        public HellTraversalWorld(HellRoomDef room, bool mirror, int quarterTurns)
        {
            Room = room;
            _mirror = mirror;
            _turns = ((quarterTurns % 4) + 4) % 4;
            foreach (var s in room.solids)
            {
                if (s.role == "gate") continue;   // the gates are open (the objective is done when you get there)
                var b = s.box;
                if (s.ramp)
                {
                    if (s.rampAlongX) World.RampX(b.min.X, b.max.X, b.min.Z, b.max.Z, s.rampTopStart, s.rampTopEnd, b.min.Y);
                    else World.Ramp(b.min.X, b.max.X, b.min.Z, b.max.Z, s.rampTopStart, s.rampTopEnd, b.min.Y);
                }
                else World.Add((b.min + b.max) * 0.5f, b.max - b.min, s.runnable);
            }
            foreach (var h in room.hazards)
            {
                var b = h.box;
                switch (h.kind)
                {
                    case HellHazardKind.Lava:
                        World.Add((b.min + b.max) * 0.5f, b.max - b.min, false);
                        Lava.Add(h);
                        break;
                    case HellHazardKind.Collapse:
                        var box = World.Add((b.min + b.max) * 0.5f, b.max - b.min, false);
                        // The Collapse's timeline tiles fall on the room clock, not under you: they stay solid here (the race
                        // against the clock is HellRoomTests' own simulation)
                        if (!h.Timeline) Platforms.Add((h, box, new CollapseCycle(room.collapseShakeSeconds, room.collapseReturnSeconds)));
                        break;
                    case HellHazardKind.Beam:
                        Beams.Add(h);
                        break;
                }
            }
            foreach (var d in room.darkZones) Dark.AddZone(d.min, d.max);
            foreach (var l in room.lights) Dark.AddLight(l.position, l.radius);
        }

        /// <summary>A base-layout point in this variant's frame (mirror, then quarter turns), as <see cref="HellRoomTransform"/>.</summary>
        public Vector3 ToWorld(Vector3 local) => new HellRoomTransform { mirror = _mirror, quarterTurns = _turns }.Point(local);

        /// <summary>A point of this variant back in the base layout's frame (the scripts' conditions are written there).</summary>
        public Vector3 ToLocal(Vector3 p)
        {
            p = new HellRoomTransform { quarterTurns = 4 - _turns }.Point(p);
            if (_mirror) p.X = -p.X;
            return p;
        }

        /// <summary>Ticks the collapsing platforms (the Unity side's <c>HellCollapsePlatform.FixedUpdate</c>).</summary>
        public void TickPlatforms(MoveSim sim, float dt, List<string> log, float time)
        {
            foreach (var (h, box, cycle) in Platforms)
            {
                bool standing = CollapseCycle.StandingOn(h.box, sim.Position, sim.Grounded);
                bool was = cycle.Solid;
                if (cycle.Tick(dt, standing, CollapseCycle.BodyIn(h.box, sim.Position, sim.Height)) && log != null)
                    log.Add(F(time) + " s: platform " + h.id + " fell");
                box.enabled = cycle.Solid;
                if (!was && cycle.Solid && log != null) log.Add(F(time) + " s: platform " + h.id + " came back");
            }
        }

        public HellHazard LavaTouched(Vector3 feet)
        {
            foreach (var h in Lava) if (LavaContact.Touching(h.box, feet, 0.05f)) return h;
            return null;
        }

        public HellHazard BeamTouched(Vector3 feet, float height, float radius)
        {
            foreach (var h in Beams)
            {
                var b = h.box;
                if (feet.X + radius > b.min.X && feet.X - radius < b.max.X && feet.Z + radius > b.min.Z && feet.Z - radius < b.max.Z &&
                    feet.Y + height > b.min.Y && feet.Y < b.max.Y) return h;
            }
            return null;
        }

        // ---------- IBlinkWorld (the Unity side: physics casts against the level, lava colliders included)

        public bool Raycast(Vector3 from, Vector3 dir, float maxDistance, out Vector3 point, out Vector3 normal)
        {
            point = default;
            normal = default;
            float best = maxDistance;
            bool found = false;
            foreach (var b in World.Boxes)
            {
                if (!b.enabled) continue;
                if (b.ramp ? RayRamp(b, from, dir, out float t, out var n) : RayBox(from, dir, b.min, b.max, out t, out n))
                {
                    if (t < 0f || t > best) continue;
                    best = t;
                    point = from + dir * t;
                    normal = n;
                    found = true;
                }
            }
            return found;
        }

        public bool BodyFits(Vector3 feet, float radius, float height) => World.CapsuleFits(feet, radius, height);

        static bool RayRamp(BoxWorld.Box b, Vector3 o, Vector3 d, out float t, out Vector3 n)
        {
            n = b.RampNormal;
            t = 0f;
            var p0 = new Vector3(b.min.X, b.rampTopStart, b.min.Z);
            float den = Vector3.Dot(n, d);
            if (Math.Abs(den) < 1e-6f) return false;
            t = Vector3.Dot(n, p0 - o) / den;
            if (t < 0f) return false;
            var p = o + d * t;
            return p.X >= b.min.X && p.X <= b.max.X && p.Z >= b.min.Z && p.Z <= b.max.Z;
        }

        static bool RayBox(Vector3 o, Vector3 d, Vector3 mn, Vector3 mx, out float tHit, out Vector3 normal)
        {
            tHit = 0f;
            normal = default;
            float tmin = float.MinValue, tmax = float.MaxValue;
            Vector3 nmin = default;
            for (int axis = 0; axis < 3; axis++)
            {
                float oo = axis == 0 ? o.X : axis == 1 ? o.Y : o.Z;
                float dd = axis == 0 ? d.X : axis == 1 ? d.Y : d.Z;
                float lo = axis == 0 ? mn.X : axis == 1 ? mn.Y : mn.Z;
                float hi = axis == 0 ? mx.X : axis == 1 ? mx.Y : mx.Z;
                if (Math.Abs(dd) < 1e-8f)
                {
                    if (oo < lo || oo > hi) return false;
                    continue;
                }
                float t1 = (lo - oo) / dd, t2 = (hi - oo) / dd;
                var n1 = Axis(axis, -1f);
                var n2 = Axis(axis, 1f);
                if (t1 > t2) { float tt = t1; t1 = t2; t2 = tt; var nn = n1; n1 = n2; n2 = nn; }
                if (t1 > tmin) { tmin = t1; nmin = n1; }
                if (t2 < tmax) tmax = t2;
                if (tmin > tmax) return false;
            }
            if (tmax < 0f || tmin < 0f) return false;   // starting inside a box: no hit (Unity's rays ignore the collider they start in)
            tHit = tmin;
            normal = nmin;
            return true;
        }

        static Vector3 Axis(int axis, float s) => axis == 0 ? new Vector3(s, 0, 0) : axis == 1 ? new Vector3(0, s, 0) : new Vector3(0, 0, s);

        internal static string F(float v) => v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>What a leg's conditions can read, in the base layout's frame.</summary>
    public struct HellProbe
    {
        public Vector3 Pos;
        public MoveSim Sim;
        /// <summary>Seconds since the current beat started.</summary>
        public float BeatTime;
        public float Time;
        public bool Grounded => Sim.Grounded && Sim.Mode != MoveMode.Air && Sim.Mode != MoveMode.Wallrun;
        public bool Falling => Sim.Velocity.Y < 0f;
        public bool Wallrunning => Sim.Mode == MoveMode.Wallrun;
        public float HorizontalDistance(Vector3 p) { var d = Pos - p; d.Y = 0f; return d.Length(); }
    }

    public enum HellPress { None, Jump, Crouch, Step }

    /// <summary>
    /// One leg of a fast line as a human would play it: a start (standing, already moving toward a point at a share of sprint
    /// speed), then beats. A beat looks at a point (the view's yaw turns to it; the stick is held forward, sprint held), may press
    /// something as it starts (jump, crouch, a Shadow Step aimed at a point) and may hold crouch, and lasts until its condition.
    /// After the last beat the player keeps running at the goal. The leg succeeds when the feet stand on ground within
    /// <see cref="goalRadius"/> of <see cref="goal"/> (and within half a metre of its height); it fails on lava, a beam, a fall or
    /// the timeout. All points and conditions are in the base layout's frame; the floor variants are run through their transform.
    /// </summary>
    public sealed class HellLeg
    {
        public sealed class Beat
        {
            public Vector3 look;
            public Func<HellProbe, bool> until;
            public HellPress press;
            public Vector3 stepAt;
            public bool crouchHeld;
            /// <summary>The stick (x right, y forward), forward by default.</summary>
            public Vector2 stick = new Vector2(0f, 1f);
            public bool sprint = true;
            /// <summary>The press reacts to a landing (a hop on landing): it is only tried late, since a press before the
            /// landing is a double jump, which no player means there.</summary>
            public bool lateOnly;
        }

        public string name;
        public HellRoomKind room;
        public Vector3 from, face;
        /// <summary>The start's speed as a share of the sprint speed (of the leg's run, so 90% runs start at 0.9 x 0.9).</summary>
        public float startSpeed = 1f;
        public Vector3 goal;
        public float goalRadius = 1.5f, timeout = 8f;
        /// <summary>Carrying the Soul Lantern: the stick scaled as the Unity player scales it (MoveScale 0.8), no Shadow Step.</summary>
        public bool carry;
        public readonly List<Beat> beats = new List<Beat>();

        public HellLeg(HellRoomKind room, string name, Vector3 from, Vector3 face)
        {
            this.room = room;
            this.name = name;
            this.from = from;
            this.face = face;
        }

        public HellLeg Do(Vector3 look, Func<HellProbe, bool> until, HellPress press = HellPress.None, bool crouch = false, Vector3 stepAt = default)
        {
            beats.Add(new Beat { look = look, until = until, press = press, crouchHeld = crouch, stepAt = stepAt });
            return this;
        }

        /// <summary>Runs (sprinting) at <paramref name="look"/> until the condition.</summary>
        public HellLeg Run(Vector3 look, Func<HellProbe, bool> until) => Do(look, until);
        /// <summary>Presses jump now, then looks at <paramref name="look"/> until the condition.</summary>
        public HellLeg Jump(Vector3 look, Func<HellProbe, bool> until) => Do(look, until, HellPress.Jump);
        /// <summary>A jump as soon as the previous beat lands (bunny hop); its timing slop is late only.</summary>
        public HellLeg Hop(Vector3 look, Func<HellProbe, bool> until)
        {
            Do(look, until, HellPress.Jump);
            beats[beats.Count - 1].lateOnly = true;
            return this;
        }
        /// <summary>Presses crouch now (a slide at speed) and holds it until the condition.</summary>
        public HellLeg Slide(Vector3 look, Func<HellProbe, bool> until) => Do(look, until, HellPress.Crouch, crouch: true);
        /// <summary>A Shadow Step aimed at <paramref name="at"/> now, then looks at <paramref name="look"/> until the condition.</summary>
        public HellLeg Step(Vector3 at, Vector3 look, Func<HellProbe, bool> until) => Do(look, until, HellPress.Step, stepAt: at);

        public HellLeg Goal(Vector3 at, float radius = 1.5f, float timeout = 8f)
        {
            goal = at;
            goalRadius = radius;
            this.timeout = timeout;
            return this;
        }

        public HellLeg StartSpeed(float share) { startSpeed = share; return this; }
        public HellLeg Carrying() { carry = true; return this; }
        public override string ToString() => room + ": " + name;
    }

    /// <summary>One run of a leg: the result and a log of what happened.</summary>
    public sealed class HellLegResult
    {
        public bool ok;
        public string why = "";
        public float seconds;
        public readonly List<string> log = new List<string>();
        public readonly List<int> pressTicks = new List<int>();

        public override string ToString()
        {
            var sb = new StringBuilder((ok ? "OK" : "FAIL: " + why) + " in " + HellTraversalWorld.F(seconds) + " s");
            foreach (var l in log) sb.Append("\n    ").Append(l);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Drives the real movement engine (<see cref="MoveSim"/>, the code the player runs, at its 120 Hz tick) through a leg. The
    /// margins: <see cref="RunWithMargins"/> runs it at full speed and at 90% (the sprint speed and the start speed scaled), then
    /// replays every press 0.1 s early and 0.1 s late one at a time (a hop on landing late only), and every press 0.1 s late
    /// together. Every run must succeed.
    /// </summary>
    public static class HellTraversal
    {
        public const float H = 1f / 120f;
        public const float Slop = 0.1f, SlowShare = 0.9f;
        /// <summary>The Unity player's carry stick (<c>SkylinePlayer.BuildMoveInput</c>: MoveScale x stickFullAt).</summary>
        public const float CarryStick = 0.8f * 0.7f;

        public static HellRoomDef Variant(HellRoomKind kind, int floor) => HellRoomVariants.Build(kind, floor);

        /// <summary>All the runs of <paramref name="leg"/> on <paramref name="floor"/>; the failures (empty: the leg holds).</summary>
        public static List<string> RunWithMargins(HellLeg leg, int floor)
        {
            var fails = new List<string>();
            foreach (float share in new[] { 1f, SlowShare })
            {
                var nominal = Run(leg, floor, share, null);
                var late = new List<bool>();
                foreach (var b in leg.beats) if (b.press != HellPress.None) late.Add(b.lateOnly);
                if (!nominal.ok)
                {
                    fails.Add("floor " + (floor + 1) + ", speed x" + share + ", on time: " + nominal);
                    continue;
                }
                int n = nominal.pressTicks.Count;
                int d = (int)Math.Round(Slop / H);
                var shifts = new List<(string label, int[] ticks)>();
                foreach (int s in new[] { -d, d })
                {
                    for (int i = 0; i < n; i++)
                    {
                        if (s < 0 && late[i]) continue;
                        var t = new int[n];
                        t[i] = s;
                        shifts.Add(("press " + (i + 1) + " " + (s < 0 ? "0.1 s early" : "0.1 s late"), t));
                    }
                    // all together only late: early presses are timed from the nominal run, which only holds while the presses
                    // before them are on time
                    if (n > 1 && s > 0)
                    {
                        var all = new int[n];
                        for (int i = 0; i < n; i++) all[i] = s;
                        shifts.Add(("every press " + (s < 0 ? "0.1 s early" : "0.1 s late"), all));
                    }
                }
                foreach (var (label, ticks) in shifts)
                {
                    var r = Run(leg, floor, share, ticks, nominal.pressTicks);
                    if (!r.ok) fails.Add("floor " + (floor + 1) + ", speed x" + share + ", " + label + ": " + r);
                }
            }
            return fails;
        }

        /// <summary>One run. <paramref name="shift"/> null: presses fire as their beats start (and are recorded). Otherwise press
        /// i is shifted by shift[i] ticks: late (positive) from the moment its beat starts in this run, as a player reacting late;
        /// early (negative) from its tick in the nominal run (<paramref name="nominalTicks"/>), whatever the beats are doing
        /// then (the beats still steer).</summary>
        public static HellLegResult Run(HellLeg leg, int floor, float speedShare, int[] shift, List<int> nominalTicks = null)
        {
            var room = Variant(leg.room, floor);
            var tune = HellRoomVariants.Floors[floor];
            var w = new HellTraversalWorld(room, tune.mirror, tune.quarterTurns);
            var def = new MoveDef();
            def.sprintSpeed *= speedShare;
            var sim = new MoveSim(w.World, def);
            var res = new HellLegResult();

            var start = w.ToWorld(leg.from);
            var face = w.ToWorld(leg.face);
            float yaw0 = YawTo(start, face);
            var dir = Flat(face - start);
            sim.Teleport(start, yaw0, dir * def.sprintSpeed * leg.startSpeed * (leg.carry ? 0.8f : 1f));

            // the presses in beat order (for the schedule)
            var pressBeats = new List<HellLeg.Beat>();
            foreach (var b in leg.beats) if (b.press != HellPress.None) pressBeats.Add(b);
            int[] schedule = null;
            if (shift != null)
            {
                schedule = new int[pressBeats.Count];
                for (int i = 0; i < schedule.Length; i++)
                    schedule[i] = i < shift.Length && shift[i] < 0 && nominalTicks != null && i < nominalTicks.Count ? Math.Max(0, nominalTicks[i] + shift[i]) : int.MaxValue;
            }
            int beatPress = 0;
            var fired = new bool[pressBeats.Count];

            int beat = 0, tick = 0;
            float beatStart = 0f, lastStep = -100f;
            bool beatEntered = false;
            int maxTicks = (int)(leg.timeout / H);
            var g = w.ToWorld(leg.goal);
            float floorY = Math.Min(leg.from.Y, leg.goal.Y) - 4f;
            MoveMode lastMode = sim.Mode;
            var bodyBeamRadius = def.radius;

            for (; tick < maxTicks; tick++)
            {
                float time = tick * H;
                var probe = new HellProbe { Pos = w.ToLocal(sim.Position), Sim = sim, BeatTime = time - beatStart, Time = time };
                // advance beats whose conditions hold
                // a beat ends on its condition, but never before its own press has fired (a late press holds the beat)
                while (beat < leg.beats.Count && beatEntered && (leg.beats[beat].press == HellPress.None || fired[beatPress - 1]) &&
                       leg.beats[beat].until != null && leg.beats[beat].until(probe))
                {
                    beat++;
                    beatEntered = false;
                    beatStart = time;
                    probe.BeatTime = 0f;
                }
                HellLeg.Beat cur = beat < leg.beats.Count ? leg.beats[beat] : null;
                var input = new MoveInput();
                var look = cur != null ? cur.look : leg.goal;
                input.yaw = YawTo(sim.Position, w.ToWorld(look));
                var stick = cur != null ? cur.stick : new Vector2(0f, 1f);
                if (leg.carry) stick *= CarryStick;
                input.move = stick;
                input.sprintHeld = cur == null || cur.sprint;
                // crouch is held from the beat's own press on (a late press does not crouch-walk before it)
                input.crouchHeld = cur != null && cur.crouchHeld && (cur.press != HellPress.Crouch || (beatPress > 0 && fired[beatPress - 1]));

                // presses
                HellPress press = HellPress.None;
                Vector3 stepAt = default;
                if (cur != null && !beatEntered)
                {
                    beatEntered = true;
                    if (cur.press != HellPress.None)
                    {
                        if (schedule == null)
                        {
                            press = cur.press;
                            stepAt = cur.stepAt;
                            res.pressTicks.Add(tick);
                            fired[beatPress] = true;
                        }
                        else if (schedule[beatPress] == int.MaxValue) schedule[beatPress] = tick + Math.Max(0, shift[beatPress]);
                        beatPress++;
                    }
                }
                if (schedule != null)
                    for (int i = 0; i < schedule.Length; i++)
                        if (schedule[i] == tick)
                        {
                            fired[i] = true;
                            press = pressBeats[i].press;
                            stepAt = pressBeats[i].stepAt;
                        }
                switch (press)
                {
                    case HellPress.Jump:
                        input.jumpPressed = true;
                        input.jumpHeld = true;
                        res.log.Add(T(time) + "jump at " + V(w.ToLocal(sim.Position)) + " (" + sim.Mode + ", " + HellTraversalWorld.F(sim.HorizontalSpeed) + " m/s)");
                        break;
                    case HellPress.Crouch:
                        input.crouchPressed = true;
                        input.crouchHeld = true;
                        res.log.Add(T(time) + "crouch at " + V(w.ToLocal(sim.Position)) + " (" + HellTraversalWorld.F(sim.HorizontalSpeed) + " m/s)");
                        break;
                    case HellPress.Step:
                        if (leg.carry) { res.why = "a Shadow Step while carrying the lantern"; return Done(res, time); }
                        if (time - lastStep < new BlinkDef().cooldown) { res.why = "a second Shadow Step before the 6 s recharge"; return Done(res, time); }
                        var eye = sim.Position + new Vector3(0f, sim.Crouched ? def.slideEyeHeight : def.eyeHeight, 0f);
                        var target = w.ToWorld(stepAt);
                        var plan = BlinkPlanner.Plan(eye, target - eye, new BlinkDef(), w);
                        var geometric = plan;
                        if (plan.valid) plan = BlinkRules.Apply(plan, w.Dark.IsDark(plan.feet + new Vector3(0f, 0.5f, 0f)), w.LavaTouched(plan.feet) != null, false);
                        if (!plan.valid)
                        {
                            res.why = "the Shadow Step at " + V(stepAt) + " from " + V(w.ToLocal(sim.Position)) + " was refused: " + plan.reason + " (" + geometric.kind + " at " + V(w.ToLocal(geometric.feet)) + ", eye " + V(w.ToLocal(eye)) + ")";
                            return Done(res, time);
                        }
                        res.log.Add(T(time) + "Shadow Step " + V(w.ToLocal(sim.Position)) + " -> " + V(w.ToLocal(plan.feet)) + " (" + plan.kind + ")");
                        sim.Teleport(plan.feet, input.yaw, sim.Velocity);
                        lastStep = time;
                        break;
                }

                sim.Step(input, H);
                w.TickPlatforms(sim, H, res.log, time);
                if (sim.Mode != lastMode)
                {
                    res.log.Add(T(time) + sim.Mode + " at " + V(w.ToLocal(sim.Position)) + ", " + HellTraversalWorld.F(sim.HorizontalSpeed) + " m/s");
                    lastMode = sim.Mode;
                }

                var feet = sim.Position;
                var lava = w.LavaTouched(feet);
                if (lava != null) { res.why = "lava (" + lava.id + ") at " + V(w.ToLocal(feet)); return Done(res, time); }
                var beam = w.BeamTouched(feet, sim.Height, bodyBeamRadius);
                if (beam != null) { res.why = "the beam " + beam.id + " at " + V(w.ToLocal(feet)) + " (body " + HellTraversalWorld.F(sim.Height) + " m)"; return Done(res, time); }
                if (w.ToLocal(feet).Y < floorY) { res.why = "fell at " + V(w.ToLocal(feet)); return Done(res, time); }
                var lp = w.ToLocal(feet);
                if (beat >= leg.beats.Count && sim.Grounded && sim.Mode != MoveMode.Air && sim.Mode != MoveMode.Wallrun && sim.Mode != MoveMode.Mantle)
                {
                    var d = lp - leg.goal;
                    d.Y = 0f;
                    if (d.Length() <= leg.goalRadius && Math.Abs(lp.Y - leg.goal.Y) < 0.5f)
                    {
                        res.ok = true;
                        res.log.Add(T(time) + "reached " + V(lp));
                        return Done(res, time);
                    }
                }
            }
            res.why = "timed out at " + V(w.ToLocal(sim.Position)) + " (beat " + (beat + 1) + " of " + leg.beats.Count + ", " + sim.Mode + ")";
            return Done(res, leg.timeout);
        }

        static HellLegResult Done(HellLegResult r, float t) { r.seconds = t; return r; }

        static Vector3 Flat(Vector3 v)
        {
            v.Y = 0f;
            float l = v.Length();
            return l > 1e-6f ? v / l : Vector3.UnitZ;
        }

        static float YawTo(Vector3 from, Vector3 to)
        {
            var d = to - from;
            if (d.X * d.X + d.Z * d.Z < 1e-8f) return 0f;
            return (float)(Math.Atan2(d.X, d.Z) * 180.0 / Math.PI);
        }

        static string T(float t) => HellTraversalWorld.F(t) + " s: ";
        static string V(Vector3 v) => "(" + v.X.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ", " +
            v.Y.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ", " + v.Z.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ")";
    }
}
