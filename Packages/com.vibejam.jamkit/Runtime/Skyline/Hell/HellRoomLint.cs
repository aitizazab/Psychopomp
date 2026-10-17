using System;
using System.Collections.Generic;
using System.Numerics;

namespace JamKit.Skyline
{
    /// <summary>What the room lint measured.</summary>
    public sealed class HellLintResult
    {
        public List<string> problems = new List<string>();
        public float safeLength, fastLength, straightLength;
        public int fastHazardCrossings;
        /// <summary>Hold the Brand: seconds each way charges (safe: the slow rate behind a stone; fast: laps at full speed), the
        /// racing line's lap, the laps a full-speed charge takes, and each path's cost in metres at a sprint (length plus the
        /// charge time run at sprint speed).</summary>
        public float holdSafeSeconds, holdFastSeconds, holdLapLength, holdLaps, safeCost, fastCost;
        public bool Ok => problems.Count == 0;
    }

    /// <summary>
    /// The creator's room rule as a check (event/HELL-DESIGN.md: "every room has an objective AND always offers a safe path
    /// plus a more dangerous, faster one"), run on every room's data:
    /// <list type="number">
    /// <item>both paths start at the entry and end at the exit, and reach every objective stop (in order where the room says so);</item>
    /// <item>the fast path is at most <see cref="MaxFastShare"/> of the safe path's length, and at most
    /// <see cref="MaxFastDetour"/> times the straight line through the stops (it IS the straight line);</item>
    /// <item>the fast path crosses at least one hazard (lava, a collapsing platform, a beam lane);</item>
    /// <item>the safe path is solid ground: every sample over a walkable top within half a metre (a ramp followed, a short
    /// climb of up to <see cref="ArenaNav.MaxClimb"/> m allowed), never over a hazard, never inside a solid or a beam, and
    /// each leg either walkable on <see cref="ArenaNav"/> or a short climb;</item>
    /// <item>entry, exit and the stops are not inside solids; spawns are inside the room; no steady fire light reaches a dark
    /// zone (a lava flare may, for a moment, off the fast line: CheckDarkness); every room has at least one dark zone.</item>
    /// </list>
    /// Timeline tiles (The Collapse's floor) count as ground; gates count as open.
    /// </summary>
    public static class HellRoomLint
    {
        public const float MaxFastShare = 0.8f, MaxFastDetour = 1.3f, EndTolerance = 1.5f, Sample = 0.5f, GroundTolerance = 0.6f;
        public const float ClimbHorizontal = 4f;
        /// <summary>The scale rule (the creator, 2026-10-17: "the six points are all so close together"): a room's footprint is at
        /// least <see cref="MinRoomWidth"/> by <see cref="MinRoomLength"/> m; objective legs at least <see cref="HellRoomDef.minLeg"/>.</summary>
        public const float MinRoomWidth = 40f, MinRoomLength = 60f;
        /// <summary>An obstacle a leg's straight line may cross instead of a hazard: a solid this much taller than the higher end (no hop).</summary>
        public const float ObstacleRise = 2.5f;
        /// <summary>Hold rooms: the ring is at least this wide, its racing line at least this long a lap, and the safe charge
        /// spot within this of a stone (cover at least <see cref="CoverMinHeight"/> tall).</summary>
        public const float MinHoldDiameter = 18f, MinHoldLap = 40f, CoverReach = 1.6f, CoverMinHeight = 1f;
        /// <summary>Sprint speed (the movement engine's): turns a hold's seconds into metres for the fast-versus-safe rule.</summary>
        public static readonly float SprintSpeed = new JamKit.Movement.MoveDef().sprintSpeed;

        public static HellLintResult Check(HellRoomDef r)
        {
            var res = new HellLintResult();
            var p = res.problems;
            if (r.safePath.Count < 2 || r.fastPath.Count < 2) { p.Add(r.name + ": a path is missing"); return res; }

            CheckEnds(r, r.safePath, "safe", p);
            CheckEnds(r, r.fastPath, "fast", p);
            CheckStops(r, r.safePath, "safe", p);
            CheckStops(r, r.fastPath, "fast", p);

            res.safeLength = Length(r.safePath);
            res.fastLength = Length(r.fastPath);
            var straight = new List<Vector3> { r.entry };
            foreach (var s in r.stops) straight.Add(s.position);
            straight.Add(r.exit);
            res.straightLength = Length(straight);
            if (r.hold != null) CheckHold(r, res, p);
            if (res.fastLength > MaxFastShare * res.safeLength)
                p.Add(r.name + ": the fast path (" + F(res.fastLength) + " m) is not clearly shorter than the safe path (" + F(res.safeLength) + " m)");
            if (res.fastLength > MaxFastDetour * res.straightLength)
                p.Add(r.name + ": the fast path (" + F(res.fastLength) + " m) is not the straight line (" + F(res.straightLength) + " m through the stops)");

            for (int i = 1; i < r.fastPath.Count; i++)
                foreach (var h in r.hazards)
                    if (!h.Timeline && SegmentCrossesXZ(r.fastPath[i - 1], r.fastPath[i], h.box)) res.fastHazardCrossings++;
            if (res.fastHazardCrossings == 0) p.Add(r.name + ": the fast path crosses no hazard");

            CheckScale(r, p);
            CheckFloating(r, p);
            CheckSafeGround(r, p);
            CheckSafeNav(r, p);

            foreach (var s in r.stops)
                if (InsideBlocking(r, s.position + new Vector3(0, 0.9f, 0)) != null) p.Add(r.name + ": stop " + s.id + " is inside a solid");
            if (InsideBlocking(r, r.entry + new Vector3(0, 0.9f, 0)) != null) p.Add(r.name + ": the entry is inside a solid");
            foreach (var s in r.spawns)
                if (!Contains(r.bounds, s.position)) p.Add(r.name + ": a " + s.role + " spawn is outside the room");
            CheckDarkness(r, p);
            CheckOpenEdges(r, p);
            return res;
        }

        /// <summary>
        /// Hold the Brand's ring (charge from speed): the ring is wide (<see cref="MinHoldDiameter"/>) and holds a lava pool
        /// (its middle is over lava) and a runnable wall; its racing line (<see cref="HellHoldRing.loop"/>) is closed, at least
        /// <see cref="MinHoldLap"/> m a lap, and every sample of it is inside the ring on solid ground, never over a hazard nor
        /// inside a solid. The fast path enters the ring; the safe path stops inside it within <see cref="CoverReach"/> of a
        /// stone (the slow charge behind cover). Then the fast-versus-safe rule is measured as time: each path's cost is its
        /// length plus its charge time run at sprint speed (safe: <see cref="HellBrandTuning.SlowSeconds"/>, fast:
        /// <see cref="HellBrandTuning.FastSeconds"/>, which a sprint covers as laps of the racing line), and the fast cost must be
        /// at most <see cref="MaxFastShare"/> of the safe cost. The plain length rule still applies to the paths in and out.
        /// </summary>
        static void CheckHold(HellRoomDef r, HellLintResult res, List<string> p)
        {
            var h = r.hold;
            if (2f * h.radius < MinHoldDiameter - 0.01f) p.Add(r.name + ": the ring is " + F(2f * h.radius) + " m across, under " + F(MinHoldDiameter));
            bool pool = false;
            foreach (var z in r.hazards)
                if (z.kind == HellHazardKind.Lava && InXZ(z.box, h.center)) pool = true;
            if (!pool) p.Add(r.name + ": the ring has no lava pool in its middle");
            bool wall = false;
            foreach (var o in r.solids)
                if (o.runnable && o.role == "wall" && FlatDistance((o.box.min + o.box.max) * 0.5f, h.center) < h.radius) wall = true;
            if (!wall) p.Add(r.name + ": the ring has no wall to run");

            var loop = h.loop;
            if (loop.Count < 4) { p.Add(r.name + ": the ring has no racing line"); return; }
            if (Vector3.Distance(loop[0], loop[loop.Count - 1]) > EndTolerance) p.Add(r.name + ": the racing line is not closed");
            res.holdLapLength = Length(loop);
            if (res.holdLapLength < MinHoldLap) p.Add(r.name + ": the racing line is " + F(res.holdLapLength) + " m a lap, under " + F(MinHoldLap));
            int bad = 0;
            foreach (var s in Samples(loop))
            {
                if (bad >= 5) break;
                string why = null;
                if (FlatDistance(s, h.center) > h.radius || Math.Abs(s.Y - h.center.Y) > h.halfHeight) why = "outside the ring";
                else
                {
                    foreach (var z in r.hazards)
                        if (z.kind != HellHazardKind.Beam && !z.Timeline && InXZ(z.box, s) && Ground(r, s, false) <= z.box.max.Y + 0.01f) why = "over hazard " + z.id;
                    if (why == null)
                    {
                        float g = Ground(r, s, true);
                        string solid = InsideBlocking(r, s + new Vector3(0, 0.9f, 0));
                        if (float.IsNegativeInfinity(g) || Math.Abs(g - s.Y) > GroundTolerance) why = "not on ground";
                        else if (solid != null) why = "inside " + solid;
                    }
                }
                if (why != null) { p.Add(r.name + ": the racing line at " + V(s) + " is " + why); bad++; }
            }

            bool fastIn = false;
            foreach (var s in Samples(r.fastPath)) if (FlatDistance(s, h.center) <= h.radius) fastIn = true;
            if (!fastIn) p.Add(r.name + ": the fast path never enters the ring");
            bool covered = false;
            foreach (var s in r.safePath)
            {
                if (FlatDistance(s, h.center) > h.radius) continue;
                foreach (var o in r.solids)
                    if (o.role == "cover" && o.box.max.Y - s.Y >= CoverMinHeight && FlatDistanceToBox(s, o.box) <= CoverReach) covered = true;
            }
            if (!covered) p.Add(r.name + ": the safe path has no stone to charge behind inside the ring");

            res.holdSafeSeconds = HellBrandTuning.SlowSeconds;
            res.holdFastSeconds = HellBrandTuning.FastSeconds;
            res.holdLaps = res.holdLapLength > 0f ? res.holdFastSeconds * SprintSpeed / res.holdLapLength : 0f;
            if (res.holdLaps < 1f) p.Add(r.name + ": a full-speed charge is under one lap of the racing line (" + F(res.holdLaps) + ")");
            res.safeCost = res.safeLength + res.holdSafeSeconds * SprintSpeed;
            res.fastCost = res.fastLength + res.holdFastSeconds * SprintSpeed;
            if (res.fastCost > MaxFastShare * res.safeCost)
                p.Add(r.name + ": the fast way (" + F(res.fastCost) + " m at a sprint, charge included) is not clearly quicker than the safe way (" + F(res.safeCost) + ")");
        }

        /// <summary>A solid standing on a falling tile floats when the tile falls (The Collapse's fins did): any solid over a
        /// timeline tile's footprint must reach down past the tile, to the lava bed.</summary>
        static void CheckFloating(HellRoomDef r, List<string> p)
        {
            foreach (var h in r.hazards)
            {
                if (!h.Timeline) continue;
                foreach (var o in r.solids)
                {
                    var b = o.box;
                    bool overlap = b.min.X < h.box.max.X - 0.01f && b.max.X > h.box.min.X + 0.01f && b.min.Z < h.box.max.Z - 0.01f && b.max.Z > h.box.min.Z + 0.01f;
                    if (overlap && b.min.Y > h.box.min.Y - 0.01f && b.max.Y > h.box.max.Y + 0.01f)
                        p.Add(r.name + ": " + o.id + " stands on the falling tile " + h.id + " (it would float)");
                }
            }
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X, dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        static float FlatDistanceToBox(Vector3 p, CoverBox b)
        {
            float dx = Math.Max(Math.Max(b.min.X - p.X, 0f), p.X - b.max.X);
            float dz = Math.Max(Math.Max(b.min.Z - p.Z, 0f), p.Z - b.max.Z);
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>The room is big enough, its legs long enough, and every leg's straight line (stop to stop; entry to the stop
        /// when there is only one) crosses a hazard or a solid too tall to hop: no leg is a plain walk.</summary>
        static void CheckScale(HellRoomDef r, List<string> p)
        {
            float w = r.bounds.max.X - r.bounds.min.X, l = r.bounds.max.Z - r.bounds.min.Z;
            if (Math.Min(w, l) < MinRoomWidth || Math.Max(w, l) < MinRoomLength)
                p.Add(r.name + ": the room is " + F(w) + " x " + F(l) + " m, under " + F(MinRoomWidth) + " x " + F(MinRoomLength));
            var legs = new List<Vector3>();
            if (r.stops.Count == 1) legs.Add(r.entry);
            foreach (var s in r.stops) legs.Add(s.position);
            for (int i = 1; i < legs.Count; i++)
            {
                var a = legs[i - 1];
                var b = legs[i];
                if (r.stops.Count > 1 && Vector3.Distance(a, b) < r.minLeg - 0.01f)
                    p.Add(r.name + ": the leg " + V(a) + " to " + V(b) + " is " + F(Vector3.Distance(a, b)) + " m, under " + F(r.minLeg));
                if (!LegBlocked(r, a, b)) p.Add(r.name + ": the straight line " + V(a) + " to " + V(b) + " crosses no hazard and no obstacle");
            }
        }

        /// <summary>The straight line a-b crosses a hazard (not a timeline tile) or a solid whose top is more than
        /// <see cref="ObstacleRise"/> above the higher end.</summary>
        public static bool LegBlocked(HellRoomDef r, Vector3 a, Vector3 b)
        {
            foreach (var h in r.hazards)
                if (!h.Timeline && SegmentCrossesXZ(a, b, h.box)) return true;
            float top = Math.Max(a.Y, b.Y) + ObstacleRise;
            foreach (var o in r.solids)
                if (o.role != "gate" && !o.ramp && o.box.max.Y > top && SegmentCrossesXZ(a, b, o.box)) return true;
            return false;
        }

        static void CheckEnds(HellRoomDef r, List<Vector3> path, string name, List<string> p)
        {
            if (Vector3.Distance(path[0], r.entry) > EndTolerance) p.Add(r.name + ": the " + name + " path does not start at the entry");
            if (Vector3.Distance(path[path.Count - 1], r.exit) > EndTolerance) p.Add(r.name + ": the " + name + " path does not end at the exit");
        }

        /// <summary>Each stop has a path sample within its reach (measured from eye height for a reach over 6 m: anchors are shot).</summary>
        static void CheckStops(HellRoomDef r, List<Vector3> path, string name, List<string> p)
        {
            var samples = Samples(path);
            int from = 0;
            foreach (var s in r.stops)
            {
                int hit = -1;
                for (int i = r.ordered ? from : 0; i < samples.Count; i++)
                {
                    var at = samples[i] + (s.reach > 6f ? new Vector3(0, ArenaNav.EyeHeight, 0) : Vector3.Zero);
                    if (Vector3.Distance(at, s.position) <= s.reach + 0.05f) { hit = i; break; }
                }
                if (hit < 0) p.Add(r.name + ": the " + name + " path misses " + s.id + (r.ordered ? " (in order)" : ""));
                else from = hit;
            }
        }

        static void CheckSafeGround(HellRoomDef r, List<string> p)
        {
            var path = r.safePath;
            int bad = 0;
            for (int i = 1; i < path.Count && bad < 5; i++)
            {
                var a = path[i - 1];
                var b = path[i];
                bool climb = Math.Abs(b.Y - a.Y) > GroundTolerance && Horizontal(a, b) <= ClimbHorizontal && Math.Abs(b.Y - a.Y) <= ArenaNav.MaxClimb;
                int n = Math.Max(1, (int)Math.Ceiling(Vector3.Distance(a, b) / Sample));
                for (int k = 0; k <= n && bad < 5; k++)
                {
                    var s = Vector3.Lerp(a, b, k / (float)n);
                    string why = null;
                    foreach (var h in r.hazards)
                    {
                        if (h.Timeline) continue;
                        if (h.kind == HellHazardKind.Beam)
                        {
                            if (InXZ(h.box, s) && h.box.max.Y > s.Y && h.box.min.Y < s.Y + 1.7f) why = "in beam lane " + h.id;
                        }
                        else if (InXZ(h.box, s) && Ground(r, s, false) <= h.box.max.Y + 0.01f) why = "over hazard " + h.id;
                    }
                    if (why == null && !climb)
                    {
                        float g = Ground(r, s, true);
                        if (float.IsNegativeInfinity(g) || Math.Abs(g - s.Y) > GroundTolerance) why = "not on ground (ground " + F(g) + ")";
                        else
                        {
                            var solid = InsideBlocking(r, s + new Vector3(0, 0.9f, 0));
                            if (solid != null) why = "inside " + solid;
                        }
                    }
                    if (why != null) { p.Add(r.name + ": the safe path at " + V(s) + " is " + why); bad++; }
                }
            }
        }

        /// <summary>The walkable graph over the room's ground (hazards out, timeline tiles in, gates open): each safe leg walks, or is a climb.</summary>
        static void CheckSafeNav(HellRoomDef r, List<string> p)
        {
            var nav = ArenaNav.Build(NavSolids(r), 1f);
            var path = r.safePath;
            for (int i = 1; i < path.Count; i++)
            {
                var a = path[i - 1];
                var b = path[i];
                if (Math.Abs(b.Y - a.Y) > GroundTolerance && Horizontal(a, b) <= ClimbHorizontal && Math.Abs(b.Y - a.Y) <= ArenaNav.MaxClimb) continue;
                int na = nav.NodeAt(a), nb = nav.NodeAt(b);
                if (na < 0 || nb < 0 || !nav.Reachable(na, nb))
                    p.Add(r.name + ": no walk on solid ground from " + V(a) + " to " + V(b));
            }
        }

        /// <summary>The solids the nav and the bots walk on: the room's solids without its gates, plus The Collapse's timeline
        /// tiles as floor.</summary>
        public static List<FloorSolid> NavSolids(HellRoomDef r)
        {
            var l = new List<FloorSolid>();
            foreach (var s in r.solids) if (s.role != "gate") l.Add(s);
            foreach (var h in r.hazards)
                if (h.Timeline) l.Add(new FloorSolid { id = h.id, role = "floor", box = h.box });
            return l;
        }

        /// <summary>The highest top at the sample's x, z that is at most <see cref="GroundTolerance"/> above it (solids and, with
        /// <paramref name="solidsOnly"/> false, hazards too; timeline tiles count as solid). -infinity when none.</summary>
        public static float Ground(HellRoomDef r, Vector3 s, bool solidsOnly)
        {
            float best = float.NegativeInfinity;
            foreach (var o in r.solids)
            {
                if (o.role == "gate" || !OnXZ(o.box, s)) continue;
                float top = o.TopAt(s.X, s.Z);
                if (top <= s.Y + GroundTolerance && top > best) best = top;
            }
            foreach (var h in r.hazards)
            {
                if (h.kind == HellHazardKind.Beam || (h.Timeline ? !OnXZ(h.box, s) : !InXZ(h.box, s))) continue;
                if (solidsOnly && !h.Timeline) continue;
                if (h.box.max.Y <= s.Y + GroundTolerance && h.box.max.Y > best) best = h.box.max.Y;
            }
            return best;
        }

        /// <summary>The id of a non-gate solid the point is inside (ramps by their slope), else null.</summary>
        public static string InsideBlocking(HellRoomDef r, Vector3 q)
        {
            foreach (var o in r.solids)
            {
                if (o.role == "gate") continue;
                var b = o.box;
                if (q.X <= b.min.X || q.X >= b.max.X || q.Z <= b.min.Z || q.Z >= b.max.Z || q.Y <= b.min.Y) continue;
                if (q.Y < o.TopAt(q.X, q.Z)) return o.id;
            }
            return null;
        }

        public static float Length(IReadOnlyList<Vector3> path)
        {
            float l = 0f;
            for (int i = 1; i < path.Count; i++) l += Vector3.Distance(path[i - 1], path[i]);
            return l;
        }

        static List<Vector3> Samples(List<Vector3> path)
        {
            var l = new List<Vector3> { path[0] };
            for (int i = 1; i < path.Count; i++)
            {
                int n = Math.Max(1, (int)Math.Ceiling(Vector3.Distance(path[i - 1], path[i]) / Sample));
                for (int k = 1; k <= n; k++) l.Add(Vector3.Lerp(path[i - 1], path[i], k / (float)n));
            }
            return l;
        }

        /// <summary>Does the segment a-b cross the box's footprint (x, z)? Slab test.</summary>
        public static bool SegmentCrossesXZ(Vector3 a, Vector3 b, CoverBox box)
        {
            float t0 = 0f, t1 = 1f;
            if (!Slab(a.X, b.X - a.X, box.min.X, box.max.X, ref t0, ref t1)) return false;
            if (!Slab(a.Z, b.Z - a.Z, box.min.Z, box.max.Z, ref t0, ref t1)) return false;
            return t1 > t0;
        }

        static bool Slab(float a, float d, float lo, float hi, ref float t0, ref float t1)
        {
            if (Math.Abs(d) < 1e-6f) return a > lo && a < hi;
            float u0 = (lo - a) / d, u1 = (hi - a) / d;
            if (u0 > u1) { float t = u0; u0 = u1; u1 = t; }
            t0 = Math.Max(t0, u0);
            t1 = Math.Min(t1, u1);
            return t1 > t0;
        }

        /// <summary>
        /// Darkness and fire (docs/HELL-SHADOW.md; the rule was "no fire light reaches a dark zone", relaxed for fire that passes):
        /// every room has a dark zone; no <b>steady</b> fire light reaches one; a lava <b>flare</b> may, but only for a moment
        /// (at most <see cref="MaxFlareSeconds"/> up, at most <see cref="MaxFlareDuty"/> of its cycle), never into a dark zone on the
        /// fast line (a Shadow Step landing, <see cref="OnFastLine"/>), and some dark zone in the room stays out of every flare (a
        /// hiding place fire never finds by itself). Moving fire (bolts, burning enemies, blasts) is not room data: it may light
        /// any dark.
        /// </summary>
        static void CheckDarkness(HellRoomDef r, List<string> p)
        {
            if (r.darkZones.Count == 0) { p.Add(r.name + ": no dark zone (the Shadow Step needs darkness)"); return; }
            foreach (var l in r.lights)
            {
                foreach (var d in r.darkZones)
                    if (DistanceToBox(l.position, d) < l.radius) p.Add(r.name + ": a fire light at " + V(l.position) + " reaches a dark zone");
                if (!(l.flareRadius > 0f)) continue;
                if (l.flareSeconds > MaxFlareSeconds) p.Add(r.name + ": a lava flare at " + V(l.position) + " stays up " + l.flareSeconds + " s (a moment: at most " + MaxFlareSeconds + ")");
                if (!(l.flarePeriod > 0f) || l.flareSeconds / l.flarePeriod > MaxFlareDuty)
                    p.Add(r.name + ": a lava flare at " + V(l.position) + " is up too much of its cycle (at most " + MaxFlareDuty + ")");
                foreach (var d in r.darkZones)
                    if (DistanceToBox(l.position, d) < l.flareRadius && OnFastLine(r, d))
                        p.Add(r.name + ": a lava flare at " + V(l.position) + " reaches a dark zone on the fast line (a Shadow Step landing)");
            }
            bool someUntouched = false;
            foreach (var d in r.darkZones)
            {
                bool touched = false;
                foreach (var l in r.lights)
                    if (l.flareRadius > 0f && DistanceToBox(l.position, d) < l.flareRadius) { touched = true; break; }
                if (!touched) { someUntouched = true; break; }
            }
            if (!someUntouched) p.Add(r.name + ": every dark zone is reached by a lava flare (keep one hiding place out of the fire)");
        }

        /// <summary>A lava flare's most time up (s) and most share of its cycle.</summary>
        public const float MaxFlareSeconds = 2f, MaxFlareDuty = 0.25f;
        /// <summary>A dark zone this close (m) to the fast path (or a hold ring's racing line) is on the fast line.</summary>
        public const float FastLineDarkReach = 2.5f;

        /// <summary>Is dark zone <paramref name="d"/> on the fast line: within <see cref="FastLineDarkReach"/> of the fast path
        /// or a hold ring's racing line (sampled every <see cref="Sample"/> m)?</summary>
        public static bool OnFastLine(HellRoomDef r, CoverBox d)
        {
            if (NearLine(r.fastPath, d)) return true;
            return r.hold != null && NearLine(r.hold.loop, d);
        }

        static bool NearLine(List<Vector3> line, CoverBox d)
        {
            if (line == null) return false;
            for (int i = 0; i < line.Count; i++)
            {
                if (DistanceToBox(line[i], d) <= FastLineDarkReach) return true;
                if (i == 0) continue;
                var a = line[i - 1];
                var b = line[i];
                int n = Math.Max(1, (int)Math.Ceiling(Vector3.Distance(a, b) / Sample));
                for (int k = 1; k < n; k++)
                    if (DistanceToBox(Vector3.Lerp(a, b, k / (float)n), d) <= FastLineDarkReach) return true;
            }
            return false;
        }

        public static float DistanceToBox(Vector3 p, CoverBox b)
        {
            var c = Vector3.Clamp(p, b.min, b.max);
            return Vector3.Distance(p, c);
        }

        public static bool Contains(CoverBox b, Vector3 p) =>
            p.X >= b.min.X && p.X <= b.max.X && p.Y >= b.min.Y && p.Y <= b.max.Y && p.Z >= b.min.Z && p.Z <= b.max.Z;

        static bool InXZ(CoverBox b, Vector3 s) => s.X > b.min.X && s.X < b.max.X && s.Z > b.min.Z && s.Z < b.max.Z;
        static bool OnXZ(CoverBox b, Vector3 s) => s.X >= b.min.X - 0.01f && s.X <= b.max.X + 0.01f && s.Z >= b.min.Z - 0.01f && s.Z <= b.max.Z + 0.01f;

        static float Horizontal(Vector3 a, Vector3 b)
        {
            float dx = b.X - a.X, dz = b.Z - a.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }


        /// <summary>How near (m) the safe path may come to the open rim without a parapet or wall on it.</summary>
        public const float OpenEdgeClear = 2.5f;

        /// <summary>
        /// Open edges (docs/HELL-ROOMS.md): the rooms' west, east and south sides are an open rim (cliffs into the abyss). Every
        /// safe path sample within <see cref="OpenEdgeClear"/> m of the rim must have a parapet or wall (a solid at least 0.8 m
        /// above the sample) on the rim beside it, so the safe way never runs along a drop. The doors (the
        /// vestibule and the exit doorway: within 8 m of the entry, 4 m of the exit) are the room's own doors.
        /// </summary>
        static void CheckOpenEdges(HellRoomDef r, List<string> p)
        {
            var rim = r.rim;
            if (rim.max.X - rim.min.X < 1f || rim.max.Z - rim.min.Z < 1f) return;
            var samples = Samples(r.safePath);
            int reported = 0;
            foreach (var s in samples)
            {
                if (FlatDistance(s, r.entry) < 8f || FlatDistance(s, r.exit) < 4f) continue;   // the vestibule and the exit doorway
                // the nearest rim point (on whichever side is nearest)
                float dW = s.X - rim.min.X, dE = rim.max.X - s.X, dS = s.Z - rim.min.Z, dN = rim.max.Z - s.Z;
                float d = Math.Min(Math.Min(dW, dE), Math.Min(dS, dN));
                if (d > OpenEdgeClear) continue;
                Vector3 q = d == dW ? new Vector3(rim.min.X - 0.3f, s.Y, s.Z) : d == dE ? new Vector3(rim.max.X + 0.3f, s.Y, s.Z)
                    : d == dS ? new Vector3(s.X, s.Y, rim.min.Z - 0.3f) : new Vector3(s.X, s.Y, rim.max.Z + 0.3f);
                bool guarded = false;
                foreach (var w in r.solids)
                    if (w.role != "floor" && w.role != "ramp" && w.role != "deck" && OnXZ(w.box, q) && w.box.max.Y >= s.Y + 0.8f) { guarded = true; break; }
                if (!guarded && reported++ < 3) p.Add(r.name + ": the safe path at " + V(s) + " runs within " + F(d) + " m of an open edge (no parapet)");
            }
        }

        /// <summary>The flat (xz) distance from <paramref name="p"/> to a polyline.</summary>
        public static float FlatDistanceToPath(List<Vector3> path, Vector3 p)
        {
            float best = float.PositiveInfinity;
            for (int i = 1; i < path.Count; i++)
            {
                var a = path[i - 1];
                var b = path[i];
                float abx = b.X - a.X, abz = b.Z - a.Z, l2 = abx * abx + abz * abz;
                float t = l2 > 1e-8f ? Math.Max(0f, Math.Min(1f, ((p.X - a.X) * abx + (p.Z - a.Z) * abz) / l2)) : 0f;
                float dx = a.X + abx * t - p.X, dz = a.Z + abz * t - p.Z;
                best = Math.Min(best, (float)Math.Sqrt(dx * dx + dz * dz));
            }
            return best;
        }
        static string F(float v) => v.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        static string V(Vector3 v) => "(" + F(v.X) + ", " + F(v.Y) + ", " + F(v.Z) + ")";
    }
}
