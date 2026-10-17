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
        public void TheCollapseFallsFromTheEntryAndSlowerOnTheSafeLane()
        {
            Assert.That(HellRoomLayouts.CollapseTileSeconds(40f, false), Is.GreaterThan(HellRoomLayouts.CollapseTileSeconds(20f, false)));
            Assert.That(HellRoomLayouts.CollapseTileSeconds(60f, true), Is.GreaterThan(HellRoomLayouts.CollapseTileSeconds(60f, false)));
            var r = HellRoomLayouts.Build(HellRoomKind.Collapse);
            Assert.That(r.hazards.Count(h => h.Timeline), Is.GreaterThan(40));
            Assert.That(r.exitGateId, Is.Empty);
            Assert.That(HellRoomLayouts.CollapseFastLaneSpeed, Is.GreaterThan(new MoveDef().sprintSpeed), "the fast lane's front outruns a sprint");
            Assert.That(HellRoomLayouts.CollapseSafeLaneSpeed, Is.LessThan(new MoveDef().sprintSpeed));
            Assert.That(HellRoomLayouts.CollapseFrontZ(HellRoomLayouts.CollapseTileSeconds(40f, false), false), Is.EqualTo(40f).Within(1e-3));
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

        /// <summary>Runs the fast lane in the movement engine on a flat strip from the entry (x -3..3), with the given policy and
        /// a Shadow Step across the 12 m gap (instant, momentum kept: credited to every runner), until the feet reach the exit
        /// floor or stand on a fallen tile. Returns the z reached; <paramref name="survived"/> is false when the floor went.</summary>
        static float RunFastLane(int floor, System.Func<MoveSim, MoveInput> policy, out float seconds, out bool survived, float fastDelay = float.NaN)
        {
            var r = HellRoomLayouts.Build(HellRoomKind.Collapse);
            var w = new BoxWorld();
            w.Floor(-3f, 3f, -8f, 120f, 0f);
            var sim = Dropped(w, r);
            LastFastLead = float.PositiveInfinity;
            bool stepped = false;
            float t = 0f;
            survived = true;
            while (t < 30f)
            {
                var input = policy(sim);
                sim.Step(input, MoveTestKit.H);
                t += MoveTestKit.H;
                var p = sim.Position;
                if (!stepped && p.Z >= HellRoomLayouts.CollapseFastGaps[1][0] - 2f)
                {
                    stepped = true;
                    sim.Teleport(new Vector3(p.X, p.Y, HellRoomLayouts.CollapseFastGaps[1][1] + 1f), 0f, sim.Velocity);
                    p = sim.Position;
                }
                if (p.Z >= HellRoomLayouts.CollapseEndZ) break;
                float falls = FallsAt(r, p, floor, fastDelay);
                if (sim.Grounded && !float.IsPositiveInfinity(falls)) LastFastLead = System.Math.Min(LastFastLead, falls - t);
                if (sim.Grounded && t >= falls) { survived = false; break; }
            }
            seconds = t;
            return sim.Position.Z;
        }

        /// <summary>The smallest lead (seconds before the tile under the feet fell) of the last fast-lane run, on the ground.</summary>
        static float LastFastLead;

        static MoveInput SprintOnly(MoveSim sim) => MoveTestKit.Forward();

        /// <summary>A skilled runner: slide whenever the boost is ready, hop out of every slide, bunny-hop while faster than a
        /// sprint (the jump on the landing step), otherwise sprint.</summary>
        static MoveInput SlideHop(MoveSim sim)
        {
            var i = MoveTestKit.Forward();
            float v = sim.HorizontalSpeed;
            if (sim.Mode == MoveMode.Slide)
            {
                if (sim.SlideTime > 0.12f) { i.jumpPressed = true; i.jumpHeld = true; }
                else i.crouchHeld = true;
            }
            else if (sim.Grounded && v >= sim.def.slideMinSpeed && sim.SlideBoostCooldown <= 0f)
            {
                i.crouchPressed = true;
                i.crouchHeld = true;
            }
            else if (sim.Grounded && v > sim.def.sprintSpeed + 0.2f)
            {
                i.jumpPressed = true;
                i.jumpHeld = true;
            }
            return i;
        }

        [TestCaseSource(nameof(CollapseFloors))]
        public void ASprintAloneLosesTheFastLane(int floor)
        {
            float z = RunFastLane(floor, SprintOnly, out float t, out bool survived);
            TestContext.WriteLine("floor " + floor + ": a sprint lost the floor at z " + z + " after " + t + " s");
            Assert.That(survived, Is.False, "a plain sprint stays ahead of the fast lane's front");
        }

        [TestCaseSource(nameof(CollapseFloors))]
        public void SlidesAndHopsWinTheFastLane(int floor)
        {
            float z = RunFastLane(floor, SlideHop, out float t, out bool survived);
            TestContext.WriteLine("floor " + floor + ": slides and hops reached z " + z + " in " + t + " s, smallest lead " + LastFastLead + " s");
            Assert.That(survived, Is.True, "slides and hops lose the fast lane at z " + z + " after " + t + " s");
            Assert.That(LastFastLead, Is.GreaterThan(0.2f), "the hops keep a lead on the front (smallest " + LastFastLead + " s)");
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
                Assert.That(a.Count(d => d.position.X > -3f), Is.GreaterThanOrEqualTo(4), "the fast lane gets debris");
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
            Assert.That(HellRoomVariants.Build(HellRoomKind.Collapse, 0).debris.Count, Is.GreaterThan(8), "the variants carry their debris");
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
