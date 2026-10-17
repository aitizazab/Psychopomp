using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using JamKit.Skyline;
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
            // a sprint (9.5 m/s) down the safe lane's path stays ahead of its front
            float s = 0f;
            for (int i = 1; i < r.safePath.Count; i++)
            {
                s += Vector3.Distance(r.safePath[i - 1], r.safePath[i]);
                float z = r.safePath[i].Z;
                if (z <= HellRoomLayouts.CollapseStartZ || z >= 112f) continue;
                Assert.That(s / 9.5f, Is.LessThan(HellRoomLayouts.CollapseTileSeconds(z - 4f, true) + 0.8f), "at z " + z);
            }
        }
    }
}
