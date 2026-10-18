using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using JamKit.Movement;
using JamKit.Skyline;
using JamKit.Tests.Movement;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The Hell rooms as data (docs/HELL-ROOMS.md): every layout and every per-floor variant passes the room lint
    /// (safe path and fast path, scale, obstacles, darkness), and the transforms keep the geometry.</summary>
    public class HellRoomTests
    {
        static IEnumerable<HellRoomKind> Kinds => HellRoomLayouts.Kinds;

        [TestCaseSource(nameof(Kinds))]
        public void BaseLayoutPassesTheLint(HellRoomKind kind)
        {
            var r = HellRoomLayouts.Build(kind);
            var res = HellRoomLint.Check(r);
            TestContext.WriteLine(kind + ": safe " + res.safeLength + " m, fast " + res.fastLength + " m, straight " + res.straightLength + " m, fast crosses " + res.fastHazardCrossings + " hazards");
            Assert.That(res.problems, Is.Empty, string.Join("\n", res.problems));
        }

        [Test]
        public void AllEighteenVariantsPassTheLint()
        {
            var all = HellRoomVariants.All();
            Assert.That(all.Count, Is.EqualTo(18));
            var problems = new List<string>();
            foreach (var r in all) problems.AddRange(HellRoomLint.Check(r).problems.Select(p => "floor " + r.floor + ": " + p));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void TheSixSigilsAreFarApartAndAtDifferentHeights()
        {
            var r = HellRoomLayouts.Build(HellRoomKind.SixSigils);
            Assert.That(r.stops.Count, Is.EqualTo(6));
            for (int i = 1; i < 6; i++)
                Assert.That(Vector3.Distance(r.stops[i - 1].position, r.stops[i].position), Is.GreaterThanOrEqualTo(30f), "leg " + i);
            Assert.That(r.stops.Select(s => s.position.Y).Distinct().Count(), Is.GreaterThanOrEqualTo(4), "vertical spread");
            float w = r.bounds.max.X - r.bounds.min.X, l = r.bounds.max.Z - r.bounds.min.Z;
            Assert.That(w * l, Is.GreaterThan(10000f), "a big space");
        }

        [Test]
        public void TheLintCatchesAPlainWalk()
        {
            // a room whose fast path is the safe path (no hazard crossed) and whose sigils are close together fails
            var r = HellRoomLayouts.Build(HellRoomKind.SixSigils);
            r.fastPath = new List<Vector3>(r.safePath);
            r.stops[1].position = r.stops[0].position + new Vector3(5f, 0, 0);
            var res = HellRoomLint.Check(r);
            Assert.That(res.problems.Any(p => p.Contains("not clearly shorter")), Is.True);
            Assert.That(res.problems.Any(p => p.Contains("under 30")), Is.True);
        }

        [Test]
        public void TheLintCatchesASafePathOverLava()
        {
            var r = HellRoomLayouts.Build(HellRoomKind.HoldBrand);
            r.safePath = new List<Vector3>(r.fastPath);
            var res = HellRoomLint.Check(r);
            Assert.That(res.problems.Any(p => p.Contains("over hazard")), Is.True, string.Join("\n", res.problems));
        }

        [TestCaseSource(nameof(Kinds))]
        public void EveryRoomHasDarknessAndNoFireLightInIt(HellRoomKind kind)
        {
            var r = HellRoomLayouts.Build(kind);
            Assert.That(r.darkZones.Count, Is.GreaterThan(0));
            Assert.That(r.lights.Count, Is.GreaterThan(0), "the lava glows");
            foreach (var l in r.lights)
                foreach (var d in r.darkZones)
                    Assert.That(HellRoomLint.DistanceToBox(l.position, d), Is.GreaterThanOrEqualTo(l.radius));
        }

        [Test]
        public void TransformTurnsPointsAndYawsClockwise()
        {
            var t = new HellRoomTransform { quarterTurns = 1 };
            var p = t.Point(new Vector3(0, 0, 1));
            Assert.That(p.X, Is.EqualTo(1f).Within(1e-5));
            Assert.That(p.Z, Is.EqualTo(0f).Within(1e-5));
            Assert.That(t.Yaw(0f), Is.EqualTo(90f));
            var m = new HellRoomTransform { mirror = true };
            Assert.That(m.Point(new Vector3(3, 1, 2)), Is.EqualTo(new Vector3(-3, 1, 2)));
            Assert.That(m.Yaw(90f), Is.EqualTo(270f));
        }

        [TestCase(false, 0)]
        [TestCase(true, 0)]
        [TestCase(false, 1)]
        [TestCase(true, 2)]
        [TestCase(true, 3)]
        public void RampsKeepTheirSlopeUnderTheTransform(bool mirror, int turns)
        {
            var t = new HellRoomTransform { mirror = mirror, quarterTurns = turns, offset = new Vector3(100, -50, 7) };
            foreach (var kind in Kinds)
                foreach (var s in HellRoomLayouts.Build(kind).solids.Where(x => x.ramp))
                {
                    var o = t.Solid(s);
                    for (float a = 0.1f; a < 1f; a += 0.2f)
                    {
                        var q = new Vector3(s.box.min.X + (s.box.max.X - s.box.min.X) * a, 0, s.box.min.Z + (s.box.max.Z - s.box.min.Z) * (1f - a));
                        var w = t.Point(q);
                        Assert.That(o.TopAt(w.X, w.Z) - t.offset.Y, Is.EqualTo(s.TopAt(q.X, q.Z)).Within(1e-3), kind + " " + s.id);
                    }
                }
        }

        [Test]
        public void FloorsChangeTheEnemiesAndTheHazards()
        {
            foreach (var kind in Kinds)
            {
                var f1 = HellRoomVariants.Build(kind, 0);
                Assert.That(f1.spawns.All(s => s.role == HellEnemyRole.Imp), Is.True, "the Ember Gate fields imps only: " + kind);
            }
            Assert.That(HellRoomVariants.All().Where(r => r.floor == 1).SelectMany(r => r.spawns).Any(s => s.role == HellEnemyRole.Scythe), Is.True);
            Assert.That(HellRoomVariants.All().Where(r => r.floor == 1).SelectMany(r => r.spawns).Any(s => s.role == HellEnemyRole.Ooze), Is.False);
            Assert.That(HellRoomVariants.All().Where(r => r.floor == 2).SelectMany(r => r.spawns).Any(s => s.role == HellEnemyRole.Ooze), Is.True);
            var a = HellRoomVariants.Build(HellRoomKind.SixSigils, 0);
            var c = HellRoomVariants.Build(HellRoomKind.SixSigils, 2);
            Assert.That(c.lavaDps, Is.GreaterThan(a.lavaDps));
            Assert.That(c.spawns.Count, Is.GreaterThan(a.spawns.Count));
            Assert.That(HellRoomVariants.Build(HellRoomKind.SixSigils, 1).mirrored, Is.True);
        }

        [Test]
        public void TheCollapseFallsFromTheBridgeStartAndSlowerOnTheSafeLane()
        {
            Assert.That(HellRoomLayouts.CollapseTileSeconds(90f, false), Is.GreaterThan(HellRoomLayouts.CollapseTileSeconds(70f, false)));
            Assert.That(HellRoomLayouts.CollapseTileSeconds(60f, true), Is.GreaterThan(HellRoomLayouts.CollapseTileSeconds(60f, false)));
            Assert.That(HellRoomLayouts.CollapseTileSeconds(HellRoomLayouts.CollapseStartZ, false), Is.EqualTo(HellRoomLayouts.CollapseFastDelay), "the bridge starts falling at its start, after the delay");
            var r = HellRoomLayouts.Build(HellRoomKind.Collapse);
            Assert.That(r.hazards.Count(h => h.Timeline), Is.GreaterThan(25));
            Assert.That(r.exitGateId, Is.EqualTo("exit-gate"), "the Collapse's gate opens when you reach the exit (the 2026-10-18 softlock)");
            Assert.That(HellRoomLayouts.CollapseFastLaneSpeed, Is.GreaterThan(new MoveDef().sprintSpeed), "the fast lane's front outruns a sprint");
            Assert.That(HellRoomLayouts.CollapseSafeLaneSpeed, Is.LessThan(new MoveDef().sprintSpeed));
            Assert.That(HellRoomLayouts.CollapseFrontZ(HellRoomLayouts.CollapseTileSeconds(80f, false), false), Is.EqualTo(80f).Within(1e-3));
        }

        /// <summary>The 2026-10-18 softlock (the creator stuck in The Collapse with the objective done): in every room, base
        /// layout and floor variant, the exit is passable once the objective is done. The exit gate is named (HellRoom opens
        /// exactly <c>exitGateId</c> on Done), every gate solid is one something opens, and from just inside the exit doorway
        /// to the drop hole and down into its zone nothing solid stands in a body's way except that gate.</summary>
        [Test]
        public void EveryRoomsExitIsPassableOnceTheObjectiveIsDone()
        {
            var rooms = Kinds.Select(k => HellRoomLayouts.Build(k)).Concat(HellRoomVariants.All()).ToList();
            foreach (var r in rooms)
            {
                string who = r.name + " (floor " + r.floor + ")";
                Assert.That(r.exitGateId, Is.Not.Empty, who + ": the exit gate must be named, or nothing opens it");
                var gate = r.Solid(r.exitGateId);
                Assert.That(gate, Is.Not.Null, who + ": the exit gate id names no solid");
                Assert.That(gate.role, Is.EqualTo("gate"), who);
                foreach (var s in r.solids.Where(s => s.role == "gate"))
                    Assert.That(s.id == r.exitGateId || s.id == r.innerGateId, Is.True, who + ": gate " + s.id + " is never opened");
                // a body's way: 1 m inside the exit doorway at chest height, over the corridor to above the hole, then down
                var drop = (r.dropZone.min + r.dropZone.max) * 0.5f;
                Assert.That(HellRoomLint.Contains(r.dropZone, drop), Is.True, who);
                var b = new Vector3(drop.X, r.exit.Y + 1f, drop.Z);
                var way = new Vector3(b.X - r.exit.X, 0f, b.Z - r.exit.Z);
                Assert.That(way.Length(), Is.GreaterThan(0.5f), who + ": the drop hole is beyond the doorway");
                var a = r.exit - Vector3.Normalize(way) + new Vector3(0f, 1f, 0f);
                Assert.That(SegmentHits(gate.box, a, b, 0f), Is.True, who + ": the exit gate should stand across the way out (else this test checks the wrong way)");
                foreach (var s in r.solids)
                {
                    if (s.id == r.exitGateId) continue;
                    Assert.That(SegmentHits(s.box, a, b, 0.3f), Is.False, who + ": " + s.id + " (" + s.role + ") blocks the exit corridor");
                    Assert.That(SegmentHits(s.box, b, drop, 0.3f), Is.False, who + ": " + s.id + " (" + s.role + ") blocks the drop hole");
                }
            }
        }

        /// <summary>Does the segment a-b pass through the box grown by <paramref name="pad"/> on X and Z (slab test)?</summary>
        static bool SegmentHits(CoverBox box, Vector3 a, Vector3 b, float pad)
        {
            Vector3 lo = box.min - new Vector3(pad, 0f, pad), hi = box.max + new Vector3(pad, 0f, pad), d = b - a;
            float t0 = 0f, t1 = 1f;
            for (int i = 0; i < 3; i++)
            {
                float o = i == 0 ? a.X : i == 1 ? a.Y : a.Z, v = i == 0 ? d.X : i == 1 ? d.Y : d.Z;
                float l = i == 0 ? lo.X : i == 1 ? lo.Y : lo.Z, h = i == 0 ? hi.X : i == 1 ? hi.Y : hi.Z;
                if (System.Math.Abs(v) < 1e-6f) { if (o <= l || o >= h) return false; continue; }
                float u0 = (l - o) / v, u1 = (h - o) / v;
                if (u0 > u1) { var t = u0; u0 = u1; u1 = t; }
                t0 = System.Math.Max(t0, u0); t1 = System.Math.Min(t1, u1);
                if (t0 >= t1) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ The Collapse, run in the movement engine

        /// <summary>The floors' Collapse numbers (speed scale, shake time); -1 is the base layout's.</summary>
        static IEnumerable<int> CollapseFloors => new[] { -1, 0, 1, 2 };

        static float Scale(int floor) => floor < 0 ? 1f : HellRoomVariants.Floors[floor].collapseSpeedScale;
        static float Shake(int floor) => floor < 0 ? new HellRoomDef().collapseShakeSeconds : HellRoomVariants.Floors[floor].shakeSeconds;

        /// <summary>The room second the tile under <paramref name="feet"/> falls (its time on the floor's timeline plus the shake),
        /// or +infinity where there is no tile (a lava gap, the end floor).</summary>
        static float FallsAt(HellRoomDef baseRoom, Vector3 feet, int floor, float fastDelay = float.NaN)
        {
            foreach (var h in baseRoom.hazards)
            {
                if (!h.Timeline) continue;
                if (feet.X < h.box.min.X || feet.X > h.box.max.X || feet.Z < h.box.min.Z || feet.Z >= h.box.max.Z) continue;
                bool safe = h.id.StartsWith("tile-safe", System.StringComparison.Ordinal);
                if (!safe && !float.IsNaN(fastDelay))
                    return HellRoomLayouts.CollapseTileSeconds(h.box.min.Z, fastDelay, HellRoomLayouts.CollapseFastLaneSpeed * Scale(floor)) + Shake(floor);
                return HellRoomLayouts.CollapseTileSeconds(h.box.min.Z, safe, Scale(floor)) + Shake(floor);
            }
            return float.PositiveInfinity;
        }

        /// <summary>The run starts as the room does: dropped in 4 m above the entry (the room clock starts at the drop).</summary>
        static MoveSim Dropped(BoxWorld w, HellRoomDef r) => MoveTestKit.Standing(w, r.entry + new Vector3(0, HellRunDropHeight, 0));

        /// <summary>The run director drops you in from this high above the vestibule (HellRunDirector).</summary>
        const float HellRunDropHeight = 4f;

        /// <summary>One run of The Collapse on the room clock in the traversal simulator (the room's real solids; the bridge's
        /// tiles fall on their timeline, and the floorless lava under them ends the run), from the drop at the entry.</summary>
        sealed class CollapseRace
        {
            public HellLegResult result;
            /// <summary>The smallest lead (s) the runner had on the ground over a bridge tile (before it fell); the speed when
            /// reaching the bridge (z 56) and the most speed seen.</summary>
            public float minLead = float.PositiveInfinity, bridgeSpeed = -1f, maxSpeed;
            /// <summary>Each wall caught and left on the way (where, when, how fast): the kicks, for the log.</summary>
            public readonly List<string> walls = new List<string>();
            public bool onWall;
        }

        /// <summary>Where the speed out of the run-up hall is measured: past the stalactites, before the bridge.</summary>
        const float HallEndZ = 48f;

        static CollapseRace Race(HellLeg leg, int floor)
        {
            var race = new CollapseRace();
            var b = HellRoomLayouts.Build(HellRoomKind.Collapse);
            leg.OnTheClock(p =>
            {
                var v = p.Sim.HorizontalSpeed;
                if (v > race.maxSpeed) race.maxSpeed = v;
                if (race.bridgeSpeed < 0f && p.Pos.Z >= HallEndZ) race.bridgeSpeed = v;
                if (p.Wallrunning != race.onWall)
                {
                    race.onWall = p.Wallrunning;
                    race.walls.Add((p.Wallrunning ? "catch" : "leave") + " at (" + p.Pos.X.ToString("0.0") + ", " + p.Pos.Y.ToString("0.0") + ", "
                        + p.Pos.Z.ToString("0.0") + ") t " + p.Time.ToString("0.00") + " s, " + v.ToString("0.0") + " m/s");
                }
                if (!p.Grounded) return;
                float falls = FallsAt(b, p.Pos, floor);
                if (!float.IsPositiveInfinity(falls)) race.minLead = System.Math.Min(race.minLead, falls - p.Time);
            });
            race.result = HellTraversal.Run(leg, floor, 1f, null);
            return race;
        }

        static string Describe(CollapseRace r) => (r.result.ok ? "made it" : "lost: " + r.result.why) + " in " + r.result.seconds.ToString("0.00")
            + " s, at the bridge " + r.bridgeSpeed.ToString("0.0") + " m/s, top " + r.maxSpeed.ToString("0.0") + " m/s, smallest lead "
            + r.minLead.ToString("0.00") + " s";

        static IEnumerable<int> VariantFloors => new[] { 0, 1, 2 };

        [TestCaseSource(nameof(VariantFloors))]
        public void TheSpeedRouteKicksUpToSpeedAndOutrunsTheFastLane(int floor)
        {
            var r = Race(HellFastLines.CollapseSpeedRoute(), floor);
            TestContext.WriteLine("floor " + floor + ": the kick route " + Describe(r) + "\n  " + string.Join("\n  ", r.walls));
            Assert.That(r.result.ok, Is.True, Describe(r) + "\n" + string.Join("\n", r.result.log));
            Assert.That(r.bridgeSpeed, Is.GreaterThan(new MoveDef().sprintSpeed + 3f), "the kicks build speed well past a sprint");
            Assert.That(r.minLead, Is.GreaterThan(0.2f), "a lead on the fast front all the way");
        }

        [TestCaseSource(nameof(VariantFloors))]
        public void TheNormalRouteCannotMakeTheFastLane(int floor)
        {
            var r = Race(HellFastLines.CollapseNormalRouteFastLane(), floor);
            TestContext.WriteLine("floor " + floor + ": a sprint below the stalactites, then the fast lane: " + Describe(r));
            Assert.That(r.result.ok, Is.False, "a sprint (no kicks) makes the fast lane: " + Describe(r));
            Assert.That(r.bridgeSpeed, Is.LessThanOrEqualTo(new MoveDef().sprintSpeed + 0.1f), "running below the stalactites gains no speed");
        }

        [TestCaseSource(nameof(VariantFloors))]
        public void ASlideAndHopBelowTheStalactitesCannotMakeTheFastLaneEither(int floor)
        {
            var r = Race(HellFastLines.CollapseSlideHopFastLane(), floor);
            TestContext.WriteLine("floor " + floor + ": a slide and hops below, then the fast lane: " + Describe(r));
            Assert.That(r.result.ok, Is.False, "a slide-hop (no kicks) makes the fast lane: " + Describe(r));
        }

        [Test]
        public void BothRoutesThroughTheHallAreTheSameLength()
        {
            // the kick line and the run below it cover the same ground: the normal route is not slower by distance, only by speed
            var a = new Vector3(0.6f, 0, 8f);
            var c = new Vector3(0.6f, 0, HellRoomLayouts.CollapseStartZ);
            var r = HellRoomLayouts.Build(HellRoomKind.Collapse);
            var rocks = r.solids.Where(s => HellRock.IsRock(s.id)).ToList();
            float H = HellRoomLayouts.CollapseStalactiteHalfGap, stand = new MoveDef().standHeight;
            foreach (var s in rocks)
            {
                Assert.That(s.box.min.Z > a.Z && s.box.max.Z < c.Z, Is.True, s.id + " is in the run-up hall");
                // the runner's lane between the rows is clear up to well over head height (kick height for the high ones)
                bool inLane = s.box.min.X < H - 1e-3f && s.box.max.X > -H + 1e-3f;
                if (inLane) Assert.That(s.box.min.Y, Is.GreaterThan(8f), s.id + " hangs over the lane high above any runner or kick");
                Assert.That(s.box.max.Y - s.box.min.Y, Is.GreaterThan(0.5f), s.id);
            }
            var kicks = rocks.Where(s => s.runnable).OrderBy(s => s.box.min.Z).ToList();
            Assert.That(kicks.Count, Is.EqualTo(HellRoomLayouts.CollapseKickColumns.Length), "the kick columns are the runnable rocks");
            Assert.That(rocks.Count(s => s.id.StartsWith("stalactite") && s.runnable), Is.GreaterThan(0), "hanging kick columns");
            Assert.That(rocks.Count(s => s.id.StartsWith("stalagmite") && s.runnable), Is.GreaterThan(0), "standing kick columns");
            for (int i = 0; i < kicks.Count; i++)
            {
                var s = kicks[i];
                bool east = s.box.min.X > 0f;
                Assert.That(east ? s.box.min.X : -s.box.max.X, Is.EqualTo(H).Within(1e-3), s.id + "'s kick face is on its row");
                Assert.That(s.box.min.Y, Is.LessThan(2.3f), s.id + " is kickable from a first jump's height");
                Assert.That(s.box.max.Y, Is.GreaterThan(6f), s.id + " is kickable up to the last kick's height");
                if (i > 0)
                {
                    var p = kicks[i - 1];
                    Assert.That(p.box.min.X > 0f, Is.Not.EqualTo(east), s.id + " zig-zags: the next column is on the other row");
                    Assert.That(s.box.min.Z - p.box.max.Z, Is.GreaterThan(1f), s.id + " is a separate rock, with a gap along the hall");
                }
            }
            Assert.That(r.hazards.Where(h => h.Timeline).All(h => h.box.min.Z >= HellRoomLayouts.CollapseStartZ - 1e-3f), Is.True, "the hall never falls");
        }

        [Test]
        public void TheHallsRocksArePointedConesOverTheirColliders()
        {
            foreach (int floor in new[] { 0, 1, 2 })
            {
                var r = HellRoomVariants.Build(HellRoomKind.Collapse, floor);
                foreach (var s in r.solids.Where(x => HellRock.IsRock(x.id)))
                {
                    var v = new List<Vector3>();
                    var t = new List<int>();
                    bool hangs = HellRock.Hangs(s.id);
                    HellRock.Mesh(s.box.min, s.box.max, hangs, s.id, v, t);
                    Assert.That(t.Count, Is.EqualTo(HellRock.Sides * HellRock.Rings * 6), s.id);
                    float sx = s.box.max.X - s.box.min.X, sz = s.box.max.Z - s.box.min.Z;
                    var c = (s.box.min + s.box.max) * 0.5f;
                    // the point reaches past the collider's end (the thin tip has none), the root sits in the roof / floor
                    float tip = hangs ? v.Min(p => p.Y) : v.Max(p => p.Y);
                    Assert.That(hangs ? tip < s.box.min.Y : tip > s.box.max.Y, Is.True, s.id + " ends in a point past its collider");
                    // and the rock stays over its collider (never more than a root radius and a bit outside it)
                    foreach (var p in v)
                    {
                        Assert.That(System.Math.Abs(p.X - c.X), Is.LessThan(sx * 0.5f / HellRock.ColliderWidth * 1.3f), s.id);
                        Assert.That(System.Math.Abs(p.Z - c.Z), Is.LessThan(sz * 0.5f / HellRock.ColliderWidth * 1.3f), s.id);
                    }
                }
            }
        }

        /// <summary>Runs the safe lane in the movement engine through the room's real solids (the floors, the fins, the tiles as
        /// floor), steering along the safe path at a sprint, after <paramref name="stall"/> seconds standing at the entry (a
        /// mistake). Returns whether it stayed ahead of the front, and the smallest lead it had (seconds before the tile under
        /// it fell).</summary>
        static bool RunSafeLane(int floor, float stall, out float minLead, out float seconds)
        {
            var r = HellRoomLayouts.Build(HellRoomKind.Collapse);
            var w = new BoxWorld();
            foreach (var s in HellRoomLint.NavSolids(r))
                if (!s.ramp) w.Add((s.box.min + s.box.max) * 0.5f, s.box.max - s.box.min, s.runnable);
            var sim = Dropped(w, r);
            int next = 1;
            float t = 0f;
            minLead = float.PositiveInfinity;
            seconds = 0f;
            while (t < 40f)
            {
                var p = sim.Position;
                var target = r.safePath[next];
                float dx = target.X - p.X, dz = target.Z - p.Z;
                if ((dx * dx + dz * dz < 1.2f * 1.2f || p.Z > target.Z + 0.2f) && next < r.safePath.Count - 1) { next++; continue; }
                float yaw = (float)(System.Math.Atan2(dx, dz) * 180.0 / System.Math.PI);
                var input = t < stall ? new MoveInput { yaw = yaw } : MoveTestKit.Forward(yaw);
                sim.Step(input, MoveTestKit.H);
                t += MoveTestKit.H;
                p = sim.Position;
                seconds = t;
                if (p.Z >= HellRoomLayouts.CollapseEndZ) return true;
                if (p.Y < -1f) return false;
                float falls = FallsAt(r, p, floor);
                if (!float.IsPositiveInfinity(falls)) minLead = System.Math.Min(minLead, falls - t);
                if (sim.Grounded && t >= falls) return false;
            }
            return false;
        }

        [TestCaseSource(nameof(CollapseFloors))]
        public void TheSafeLaneIsSurvivableAtASprint(int floor)
        {
            bool ok = RunSafeLane(floor, 0f, out float lead, out float t);
            TestContext.WriteLine("floor " + floor + ": safe lane in " + t + " s, smallest lead " + lead + " s");
            Assert.That(ok, Is.True, "a sprint down the safe lane falls (lead " + lead + " s)");
            Assert.That(lead, Is.LessThan(2.5f), "no room for mistakes: the front stays close");
        }

        [Test]
        public void TheSafeLaneHasNoRoomForMistakes()
        {
            // standing still for 2 s at the start (a fumble) loses the safe lane on every floor
            foreach (int floor in CollapseFloors)
                Assert.That(RunSafeLane(floor, 2f, out _, out _), Is.False, "floor " + floor + ": a 2 s fumble survives");
        }

        /// <summary>The creator's bug (2026-10-18): on the safe lane the tiles fell ("the textures fell down") and the player didn't
        /// drop into the lava. The lava basins had a collider at their surface (lava you wade out of, 1.2 m down), so a fallen
        /// tile left you standing on the glowing lava for its 1 s sink time, and every hop reset that clock. The Collapse's
        /// bridge lava is now floorless: you fall into it and sink at once (HellRoom: LavaContact.SunkIn).</summary>
        [TestCase(0)]
        [TestCase(2)]
        public void AFallenSafeTileDropsYouIntoTheLava(int floor)
        {
            var room = HellRoomVariants.Build(HellRoomKind.Collapse, floor);
            Assert.That(room.lavaFloor, Is.False, "the bridge's lava has no floor");
            var tune = HellRoomVariants.Floors[floor];
            var w = new HellTraversalWorld(room, tune.mirror, tune.quarterTurns, timeline: true);
            // standing still on a safe-lane tile, mid-bridge
            var spot = w.ToWorld(new Vector3(HellRoomLayouts.CollapseSafeLineEast, 0f, HellRoomLayouts.CollapseStartZ + 7.5f));
            var tile = room.hazards.First(h => h.Timeline && h.id.StartsWith("tile-safe") && HellRoomLint.Contains(new CoverBox { min = h.box.min - new Vector3(0, 1, 0), max = h.box.max + new Vector3(0, 1, 0) }, spot));
            var sim = MoveTestKit.Standing(w.World, spot);
            float fell = tile.timelineSeconds + room.collapseShakeSeconds, sunkAt = -1f;
            for (float t = 0f; t < fell + 2f; t += MoveTestKit.H)
            {
                sim.Step(new MoveInput(), MoveTestKit.H);
                w.TickPlatforms(sim, MoveTestKit.H, null, t);
                if (t < fell - 0.05f) Assert.That(sim.Grounded, Is.True, "standing on the tile before it falls");
                if (LavaContact.SunkIn(room, sim.Position)) { sunkAt = t; break; }
            }
            Assert.That(sunkAt, Is.GreaterThan(0f), "the player never sank: still at " + sim.Position);
            Assert.That(sunkAt - fell, Is.LessThan(0.6f), "sunk within a moment of the tile falling (" + (sunkAt - fell) + " s)");
            // and every other room keeps lava you can wade out of (a floor at its surface, never SunkIn)
            foreach (var r in HellRoomVariants.All())
                if (r.kind != HellRoomKind.Collapse)
                {
                    Assert.That(r.lavaFloor, Is.True, r.name);
                    Assert.That(LavaContact.SunkIn(r, r.entry - new Vector3(0, 5, 0)), Is.False, r.name);
                }
        }

        [Test]
        public void SpeedIsCarriedThroughADrop()
        {
            var m = new HellMomentum();
            // a sprint into the drop hole, then a bump on its far wall and the fall
            for (int i = 0; i < 120; i++) m.Tick(1f / 120f, 13f);
            for (int i = 0; i < 108; i++) m.Tick(1f / 120f, 0.5f);
            var v = m.Arrive(90f);   // the next room faces +x
            Assert.That(v.X, Is.EqualTo(13f - 0.9f * HellMomentum.FadePerSecond).Within(0.05f), "rotated into the entry direction, faded a little over the fall");
            Assert.That(System.Math.Abs(v.Z), Is.LessThan(1e-3f));
            Assert.That(m.Arrive(0f), Is.EqualTo(Vector3.Zero), "used up on arrival");
            for (int i = 0; i < 60; i++) m.Tick(1f / 120f, 30f);
            Assert.That(m.Arrive(0f).Z, Is.EqualTo(HellMomentum.MaxSpeed), "capped so the landing stays on the entry floor");
            m.Tick(0.1f, 9f);
            m.Reset();
            Assert.That(m.Arrive(0f), Is.EqualTo(Vector3.Zero), "a death carries nothing");
            // the capped arrival from the drop height lands on every room's entry floor (z 0..8 at least), not in its lava
            float g = new MoveDef().gravity, fall = (float)System.Math.Sqrt(2.0 * HellWorld.DropInHeight / g);
            Assert.That(-4f + HellMomentum.MaxSpeed * fall, Is.LessThan(8f));
        }

        [Test]
        public void TheFinsStandOnTheLavaBedNotOnTheirTiles()
        {
            var r = HellRoomLayouts.Build(HellRoomKind.Collapse);
            var fins = r.solids.Where(s => s.id.StartsWith("baffle")).ToList();
            Assert.That(fins.Count, Is.EqualTo(HellRoomLayouts.CollapseBaffleCount));
            foreach (var f in fins) Assert.That(f.box.min.Y, Is.LessThanOrEqualTo(HellRoomLayouts.LavaSurface - HellRoomLayouts.LavaDepth + 1e-4f), f.id);
            // the lint catches a fin standing on its tile (the old bug)
            fins[0].box = new CoverBox { min = new Vector3(fins[0].box.min.X, 0f, fins[0].box.min.Z), max = fins[0].box.max };
            var res = HellRoomLint.Check(r);
            Assert.That(res.problems.Any(p => p.Contains("would float")), Is.True, string.Join("\n", res.problems));
        }

        // ------------------------------------------------------------------ The Collapse's debris

        [Test]
        public void TheDebrisIsSeededAndLandsAheadOfTheFrontOnTheRunningLine()
        {
            for (int floor = 0; floor < 3; floor++)
            {
                var b = HellRoomLayouts.Build(HellRoomKind.Collapse);
                var a = HellRoomLayouts.CollapseDebris(b, 7UL, Scale(floor));
                var again = HellRoomLayouts.CollapseDebris(b, 7UL, Scale(floor));
                var other = HellRoomLayouts.CollapseDebris(b, 8UL, Scale(floor));
                Assert.That(a.Select(d => d.time), Is.EqualTo(again.Select(d => d.time)), "seeded");
                Assert.That(a.Select(d => d.position), Is.Not.EqualTo(other.Select(d => d.position)), "another seed, other drops");
                Assert.That(a.Count(d => d.position.X > -3f), Is.GreaterThanOrEqualTo(3), "the fast lane gets debris");
                Assert.That(a.Count(d => d.position.X < -8f), Is.GreaterThanOrEqualTo(4), "the safe lane gets debris");
                for (int i = 1; i < a.Count; i++) Assert.That(a[i].time, Is.GreaterThanOrEqualTo(a[i - 1].time), "in landing order");
                foreach (var d in a)
                {
                    float fall = FallsAt(b, d.position, floor);
                    Assert.That(fall, Is.Not.EqualTo(float.PositiveInfinity), "debris on a tile, not a gap: " + d.position);
                    Assert.That(fall, Is.GreaterThan(d.time + 0.3f), "debris lands ahead of the front: " + d.position + " at " + d.time);
                    bool safe = d.position.X < -6f;
                    float line = safe ? HellRoomLayouts.LineXAt(b.safePath, d.position.Z) : 0f;
                    Assert.That(System.Math.Abs(d.position.X - line), Is.LessThanOrEqualTo(HellDebris.SideJitter + 1e-3f), "on the running line");
                    Assert.That(d.time, Is.GreaterThanOrEqualTo(HellDebris.TelegraphSeconds), "its shadow shows from the room's start");
                    foreach (var f in b.solids.Where(s => s.id.StartsWith("baffle")))
                        Assert.That(HellRoomLint.DistanceToBox(new Vector3(d.position.X, f.box.min.Y + 0.1f, d.position.Z), f.box), Is.GreaterThan(0.5f), "not on a fin");
                }
            }
            Assert.That(HellRoomVariants.Build(HellRoomKind.Collapse, 0).debris.Count, Is.GreaterThan(4), "the variants carry their debris");
        }

        [Test]
        public void TheDebrisIsTelegraphedThenHitsAndASidestepClearsIt()
        {
            var d = new HellDebrisDrop { time = 5f, position = new Vector3(0, 0, 50), radius = HellDebris.Radius };
            Assert.That(HellDebris.Telegraph(d, 3.9f), Is.EqualTo(-1f), "no shadow before");
            Assert.That(HellDebris.Telegraph(d, 4f), Is.EqualTo(0f).Within(1e-4), "the shadow 1 s before");
            Assert.That(HellDebris.Telegraph(d, 4.5f), Is.EqualTo(0.5f).Within(1e-4));
            Assert.That(HellDebris.RockHeight(d, 4.5f), Is.EqualTo(-1f), "the rock is seen only at the end");
            Assert.That(HellDebris.RockHeight(d, 4.8f), Is.InRange(0f, HellDebris.DropHeight));
            Assert.That(HellDebris.Lands(d, 4.99f, 5.01f), Is.True);
            Assert.That(HellDebris.Lands(d, 5.01f, 5.03f), Is.False, "it lands once");
            Assert.That(HellDebris.Rubble(d, 5.2f), Is.True);
            Assert.That(HellDebris.Hits(d, new Vector3(0.7f, 0, 50.5f)), Is.True, "on the line: hit");
            Assert.That(HellDebris.Hits(d, new Vector3(2.4f, 0, 50f)), Is.False, "a sidestep of 2.4 m (the lane is 6 m wide) clears it");
        }

        // ------------------------------------------------------------------ Hold the Brand's ring

        [Test]
        public void TheBrandRingIsARacetrackRoundALavaPool()
        {
            var r = HellRoomLayouts.Build(HellRoomKind.HoldBrand);
            var res = HellRoomLint.Check(r);
            TestContext.WriteLine("lap " + res.holdLapLength + " m, laps " + res.holdLaps + ", safe cost " + res.safeCost + " m, fast cost " + res.fastCost + " m");
            Assert.That(res.problems, Is.Empty, string.Join("\n", res.problems));
            Assert.That(r.hold.radius * 2f, Is.EqualTo(20f));
            Assert.That(res.holdLaps, Is.GreaterThanOrEqualTo(1f));
            Assert.That(res.fastCost, Is.LessThanOrEqualTo(HellRoomLint.MaxFastShare * res.safeCost));
            Assert.That(r.solids.Count(s => s.role == "cover" && HellRoomLint.DistanceToBox(r.hold.center, s.box) < r.hold.radius), Is.GreaterThanOrEqualTo(3), "a few stones inside");
            Assert.That(r.solids.Count(s => s.runnable && s.role == "wall" && HellRoomLint.DistanceToBox(r.hold.center, s.box) < r.hold.radius), Is.GreaterThanOrEqualTo(2), "wall-run walls inside");
        }

        [Test]
        public void TheLintCatchesABadRing()
        {
            var r = HellRoomLayouts.Build(HellRoomKind.HoldBrand);
            var c = r.hold.center;
            // a racing line through the pool, a safe path with no stone
            r.hold.loop = new List<Vector3> { c + new Vector3(3, 0, -3), c + new Vector3(3, 0, 3), c + new Vector3(-3, 0, 3), c + new Vector3(-3, 0, -3), c + new Vector3(3, 0, -3) };
            r.safePath = r.safePath.Where(p => p.Z != 33f).ToList();
            var res = HellRoomLint.Check(r);
            Assert.That(res.problems.Any(p => p.Contains("racing line at") && p.Contains("over hazard")), Is.True, string.Join("\n", res.problems));
            Assert.That(res.problems.Any(p => p.Contains("under 40")), Is.True, "a lap too short");
            Assert.That(res.problems.Any(p => p.Contains("no stone to charge behind")), Is.True);
            r = HellRoomLayouts.Build(HellRoomKind.HoldBrand);
            r.hold.radius = 6f;
            res = HellRoomLint.Check(r);
            Assert.That(res.problems.Any(p => p.Contains("under 18")), Is.True);
            Assert.That(res.problems.Any(p => p.Contains("outside the ring")), Is.True);
        }

        [Test]
        public void ASprintLappingTheRingChargesTheBrandInAboutTenSeconds()
        {
            // the movement engine runs the racing line in the room's real solids: the corners keep a sprint at the full rate
            var r = HellRoomLayouts.Build(HellRoomKind.HoldBrand);
            var w = new BoxWorld();
            foreach (var s in HellRoomLint.NavSolids(r))
                if (!s.ramp) w.Add((s.box.min + s.box.max) * 0.5f, s.box.max - s.box.min, s.runnable);
            var loop = r.hold.loop;
            var sim = MoveTestKit.Standing(w, loop[0]);
            var o = (BrandHold)HellObjective.For(r);
            int next = 1;
            float t = 0f;
            while (!o.Done && t < 40f)
            {
                var p = sim.Position;
                var target = loop[next];
                float dx = target.X - p.X, dz = target.Z - p.Z;
                if (dx * dx + dz * dz < 2.5f * 2.5f) { next = next % (loop.Count - 1) + 1; continue; }
                float yaw = (float)(System.Math.Atan2(dx, dz) * 180.0 / System.Math.PI);
                sim.Step(MoveTestKit.Forward(yaw), MoveTestKit.H);
                t += MoveTestKit.H;
                Assert.That(o.InCircle(sim.Position), Is.True, "the line stays in the ring at " + sim.Position);
                Assert.That(sim.Position.Y, Is.GreaterThan(-0.2f), "on the track, not in the pool");
                o.Tick(MoveTestKit.H, sim.Position, sim.HorizontalSpeed);
            }
            TestContext.WriteLine("a sprint lapping the ring charged it in " + t + " s");
            Assert.That(o.Done, Is.True);
            Assert.That(t, Is.LessThan(12f), "laps at a sprint: about 10 s (with the start from standing)");
        }
    }
}
