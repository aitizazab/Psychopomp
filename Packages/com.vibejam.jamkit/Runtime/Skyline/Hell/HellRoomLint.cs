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
    /// <item>entry, exit and the stops are not inside solids; spawns are inside the room; no fire light reaches a dark zone;
    /// every room has at least one dark zone.</item>
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
            if (res.fastLength > MaxFastShare * res.safeLength)
                p.Add(r.name + ": the fast path (" + F(res.fastLength) + " m) is not clearly shorter than the safe path (" + F(res.safeLength) + " m)");
            if (res.fastLength > MaxFastDetour * res.straightLength)
                p.Add(r.name + ": the fast path (" + F(res.fastLength) + " m) is not the straight line (" + F(res.straightLength) + " m through the stops)");

            for (int i = 1; i < r.fastPath.Count; i++)
                foreach (var h in r.hazards)
                    if (!h.Timeline && SegmentCrossesXZ(r.fastPath[i - 1], r.fastPath[i], h.box)) res.fastHazardCrossings++;
            if (res.fastHazardCrossings == 0) p.Add(r.name + ": the fast path crosses no hazard");

            CheckScale(r, p);
            CheckSafeGround(r, p);
            CheckSafeNav(r, p);

            foreach (var s in r.stops)
                if (InsideBlocking(r, s.position + new Vector3(0, 0.9f, 0)) != null) p.Add(r.name + ": stop " + s.id + " is inside a solid");
            if (InsideBlocking(r, r.entry + new Vector3(0, 0.9f, 0)) != null) p.Add(r.name + ": the entry is inside a solid");
            foreach (var s in r.spawns)
                if (!Contains(r.bounds, s.position)) p.Add(r.name + ": a " + s.role + " spawn is outside the room");
            if (r.darkZones.Count == 0) p.Add(r.name + ": no dark zone (the Shadow Step needs darkness)");
            foreach (var l in r.lights)
                foreach (var d in r.darkZones)
                    if (DistanceToBox(l.position, d) < l.radius) p.Add(r.name + ": a fire light at " + V(l.position) + " reaches a dark zone");
            return res;
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

        static string F(float v) => v.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        static string V(Vector3 v) => "(" + F(v.X) + ", " + F(v.Y) + ", " + F(v.Z) + ")";
    }
}
