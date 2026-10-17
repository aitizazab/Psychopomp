using System;
using System.Collections.Generic;
using System.Numerics;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The event game's enemies (docs/HELL-ENEMIES.md): the defs keep the Skyline ids; the fire imp's fast bolt stays
    /// dodgeable through the lagged aim; the scythe's telegraph is harmless and its beam sweeps and hits once; a volley leaves gaps
    /// between its lanes; the lava pool burns, cools and fades; the ooze throbs faster through its wind-up.</summary>
    public class HellEnemiesTests
    {
        const float H = 1f / 120f;

        // ------------------------------------------------------------ defs

        [Test]
        public void TheHellDefsKeepTheSkylineIdsAndSpawnByThem()
        {
            Assert.That(HellEnemies.Find(SkylineEnemies.DroneId).hell, Is.EqualTo(HellKind.FireImp));
            Assert.That(HellEnemies.Find(SkylineEnemies.SpotlightId).hell, Is.EqualTo(HellKind.ScytheDemon));
            Assert.That(HellEnemies.Find(SkylineEnemies.FloorCareId).hell, Is.EqualTo(HellKind.LavaOoze));
            Assert.That(HellEnemies.Find(SkylineEnemies.GondolaId).id, Is.EqualTo(SkylineEnemies.GondolaId), "no Hell stand-in: the Skyline gondola");
            Assert.That(HellEnemies.Find("nope"), Is.Null);
            foreach (var d in HellEnemies.All())
            {
                Assert.That(HellEnemies.Find(d.id).displayName, Is.EqualTo(d.displayName));
                Assert.That(d.weakPointRadius, Is.GreaterThan(0f), d.id);
                var b = new EnemyBody(d);
                Assert.That(b.Wrecked, Is.False);
                Assert.That(b.Damage(1e5f, -1).wrecked, Is.True, d.id + " can be killed");
            }
            // the Skyline defs are untouched (the sims balance them)
            Assert.That(SkylineEnemies.MallCopDrone().hell, Is.EqualTo(HellKind.None));
            Assert.That(SkylineEnemies.MallCopDrone().projectileSpeed, Is.EqualTo(30f));
        }

        // ------------------------------------------------------------ the fire imp's bolt

        [Test]
        public void TheImpBoltIsThreeTimesTheDronesAndCrossesThirtyMetresInAThird()
        {
            var imp = HellEnemies.FireImp();
            Assert.That(imp.projectileSpeed, Is.EqualTo(SkylineEnemies.MallCopDrone().projectileSpeed * 3f).Within(1e-3f));
            Assert.That(HellImpAim.FlightSeconds(imp, 30f), Is.LessThan(0.35f));
            Assert.That(imp.aimLagSeconds, Is.EqualTo(0.2f), "the lag keeps it dodgeable");
        }

        [Test]
        public void AWalkerAcrossTheLineIsMissedAtEveryRangeAndAStandingTargetIsNot()
        {
            var imp = HellEnemies.FireImp();
            float width = HellImpAim.HitWidth(imp);
            foreach (float r in new[] { 8f, 12f, 16f, 20f, 25f, 30f })
            {
                Assert.That(HellImpAim.MissAcross(imp, 0f, r), Is.EqualTo(0f), "standing at " + r);
                Assert.That(HellImpAim.MissAcross(imp, 4f, r), Is.GreaterThan(width * 1.4f), "walking at " + r);
            }
        }

        /// <summary>The same rule flown: a bolt at 90 m/s at where the chest was 0.2 s ago, from an imp 3 m above the chest, against a
        /// target strafing across (missed) and one standing (hit), 8 to 30 m.</summary>
        [Test]
        public void FlownBoltsMissAStrafeAndHitAStandingTarget()
        {
            var imp = HellEnemies.FireImp();
            float width = HellImpAim.HitWidth(imp);
            foreach (float r in new[] { 8f, 16f, 30f })
            {
                Assert.That(ClosestPass(imp, r, 5f), Is.GreaterThan(width), "a 5 m/s strafe at " + r + " m");
                Assert.That(ClosestPass(imp, r, 0f), Is.LessThan(width * 0.5f), "standing at " + r + " m");
            }
        }

        static float ClosestPass(EnemyDef d, float range, float speedAcross)
        {
            var trail = new PositionTrail();
            var imp = new Vector3(0f, 1.25f + 3f, 0f);
            Func<float, Vector3> chest = t => new Vector3(speedAcross * t, 1.25f, range);
            float fireAt = 1f;
            for (float t = 0f; t <= fireAt + 1e-4f; t += H) trail.Record(t, chest(t));
            Vector3 aim = Aim.LaggedPoint(trail, fireAt, d.aimLagSeconds, chest(fireAt));
            Vector3 vel = Vector3.Normalize(aim - imp) * d.projectileSpeed;
            float best = float.PositiveInfinity;
            for (float s = 0f; s < 1f; s += H / 4f)
                best = Math.Min(best, Vector3.Distance(imp + vel * s, chest(fireAt + s)));
            return best;
        }

        // ------------------------------------------------------------ the scythe

        static ScytheBeam Straight(float len = 14f) =>
            new ScytheBeam(new ScythePath { from = new Vector3(0f, 1f, 0f), to = new Vector3(len, 1f, 0f) }, HellEnemies.ScytheDemon());

        [Test]
        public void TheScytheProjectsItsPathHarmlesslyThenBeamsAlongIt()
        {
            var s = Straight();
            var d = HellEnemies.ScytheDemon();
            Vector3 lo = new Vector3(1f, 0f, 0f), hi = new Vector3(1f, 1.8f, 0f);   // standing on the line near its start
            Assert.That(s.Phase, Is.EqualTo(ScythePhase.Telegraph));
            for (float t = 0f; t < d.scytheTelegraph - 0.02f; t += H)
            {
                s.Tick(H);
                Assert.That(s.TryHit(lo, hi, 0.38f), Is.False, "the telegraph never hurts (t " + s.T + ")");
            }
            Assert.That(s.TelegraphProgress, Is.GreaterThan(0.9f));
            bool hit = false;
            int hits = 0;
            while (s.Phase != ScythePhase.Done)
            {
                s.Tick(H);
                if (s.TryHit(lo, hi, 0.38f)) { hit = true; hits++; }
            }
            Assert.That(hit, Is.True, "the beam hits whoever stands on the path");
            Assert.That(hits, Is.EqualTo(1), "one scythe hits once");
        }

        [Test]
        public void TheBladeReachesTheFarEndAtItsBeamSpeed()
        {
            var d = HellEnemies.ScytheDemon();
            var s = Straight(14f);
            Assert.That(s.SweepSeconds, Is.EqualTo(14f / d.scytheBeamSpeed).Within(1e-4f));
            Vector3 lo = new Vector3(13.5f, 0f, 0f), hi = new Vector3(13.5f, 1.8f, 0f);   // at the far end
            float firstHit = -1f;
            while (s.Phase != ScythePhase.Done)
            {
                s.Tick(H);
                if (firstHit < 0f && s.TryHit(lo, hi, 0.38f)) firstHit = s.T;
            }
            // the blade gets within reach (13.5 - 0.93 m) at telegraph + that / speed
            float expect = d.scytheTelegraph + (13.5f - d.scytheHitRadius - 0.38f) / d.scytheBeamSpeed;
            Assert.That(firstHit, Is.EqualTo(expect).Within(2f * H));
        }

        [Test]
        public void ClearOfTheBeamOrAboveItIsSafe()
        {
            var s = Straight();
            Vector3 lo = new Vector3(5f, 0f, 1.2f), hi = new Vector3(5f, 1.8f, 1.2f);   // 1.2 m to the side
            while (s.Phase != ScythePhase.Done)
            {
                s.Tick(H);
                Assert.That(s.TryHit(lo, hi, 0.38f), Is.False);
            }
            Assert.That(s.TotalSeconds, Is.EqualTo(s.telegraph + s.SweepSeconds + s.hold + ScytheBeam.FadeSeconds).Within(1e-5f));
        }

        [Test]
        public void AVolleyCutsLanesWithGapsAndPunishesStandingStill()
        {
            var d = HellEnemies.ScytheDemon();
            var paths = new List<ScythePath>();
            var demon = new Vector3(0f, 0f, 0f);
            var player = new Vector3(3f, 0f, 14f);
            for (ulong seed = 1; seed <= 40; seed++)
            {
                ScytheVolley.Plan(demon, player, d, new Rng(seed), paths);
                Assert.That(paths.Count, Is.EqualTo(d.scytheCount));
                foreach (var p in paths)
                {
                    Assert.That(p.Length, Is.EqualTo(d.scytheLength).Within(1e-3f));
                    Assert.That(p.from.Y, Is.EqualTo(d.scytheHeight).Within(1e-4f));
                }
                // standing still where it read you: a lane through you
                Assert.That(MinDistance(paths, player), Is.LessThan(d.scytheHitRadius + 0.38f), "seed " + seed + ": standing still is hit");
                // a gap within 2.5 m that no lane touches
                bool gap = false;
                for (int a = 0; a < 32 && !gap; a++)
                    for (float r = 1f; r <= 2.5f && !gap; r += 0.25f)
                    {
                        var q = player + new Vector3(MathF.Sin(a * MathF.PI / 16f) * r, 0f, MathF.Cos(a * MathF.PI / 16f) * r);
                        if (MinDistance(paths, q) > d.scytheHitRadius + 0.38f + 0.1f) gap = true;
                    }
                Assert.That(gap, Is.True, "seed " + seed + ": a safe gap to step into");
            }
        }

        static float MinDistance(List<ScythePath> paths, Vector3 feet)
        {
            float best = float.PositiveInfinity;
            foreach (var p in paths) best = Math.Min(best, HellGeometry.SegmentSegment(p.from, p.to, feet, feet + new Vector3(0f, 1.8f, 0f)));
            return best;
        }

        [Test]
        public void ABeamLaneFiresOnItsClock()
        {
            var lane = new BeamLane(2.5f, 1f);
            var at = new List<float>();
            for (float t = 0f; t < 10f; t += H) if (lane.Tick(H) > 0) at.Add(lane.T);
            Assert.That(at.Count, Is.EqualTo(4), string.Join(", ", at));   // 1, 3.5, 6, 8.5
            Assert.That(at[0], Is.EqualTo(1f).Within(2f * H));
            Assert.That(at[1] - at[0], Is.EqualTo(2.5f).Within(2f * H));
        }

        // ------------------------------------------------------------ the lava pool

        [Test]
        public void ThePoolBurnsWhileYouStandInItThenCoolsAndFades()
        {
            var d = HellEnemies.LavaOoze();
            var pool = new LavaPool(Vector3.Zero, d);
            var feet = new Vector3(1f, 0f, 0f);
            float total = 0f, first = -1f, cooling = 0f;
            while (!pool.Gone)
            {
                bool inside = pool.Contains(feet);
                float dmg = pool.Tick(H, inside);
                if (dmg > 0f && first < 0f) first = pool.T;
                if (pool.Phase == LavaPoolPhase.Cooling) cooling += dmg;
                total += dmg;
            }
            Assert.That(first, Is.LessThan(0.2f), "stepping in burns at once (as it spreads past 1 m)");
            Assert.That(cooling, Is.EqualTo(0f), "a cooling pool is harmless");
            float burnTime = d.poolGrowSeconds + d.poolBurnSeconds - first;
            Assert.That(total, Is.EqualTo(d.poolDps * burnTime).Within(d.poolDps * d.poolTick * 1.5f));
            Assert.That(pool.T, Is.EqualTo(d.poolGrowSeconds + d.poolBurnSeconds + d.poolCoolSeconds).Within(2f * H));
        }

        [Test]
        public void JumpingOverThePoolOrStandingOutsideIsSafe()
        {
            var d = HellEnemies.LavaOoze();
            var pool = new LavaPool(Vector3.Zero, d);
            for (int i = 0; i < 120; i++) pool.Tick(H, false);
            Assert.That(pool.Contains(new Vector3(1f, 0.5f, 0f)), Is.False, "feet 0.5 m up clear it");
            Assert.That(pool.Contains(new Vector3(d.poolRadius + 0.2f, 0f, 0f)), Is.False);
            Assert.That(pool.Contains(new Vector3(d.poolRadius - 0.2f, 0.1f, 0f)), Is.True);
            Assert.That(pool.Tick(H, false), Is.EqualTo(0f));
        }

        [Test]
        public void HoppingInAndOutTicksNoFasterThanTheInterval()
        {
            var d = HellEnemies.LavaOoze();
            var pool = new LavaPool(Vector3.Zero, d);
            float total = 0f;
            for (int i = 0; i < 240; i++) total += pool.Tick(H, (i / 6) % 2 == 0);   // 0.05 s in, 0.05 s out, for 2 s
            int maxTicks = (int)(2f / d.poolTick) + 1;
            Assert.That(total, Is.LessThanOrEqualTo(maxTicks * d.poolDps * d.poolTick + 1e-3f));
            Assert.That(total, Is.GreaterThan(0f));
        }

        [Test]
        public void CoolingNowEndsTheBurn()
        {
            var d = HellEnemies.LavaOoze();
            var pool = new LavaPool(Vector3.Zero, d);
            for (int i = 0; i < 60; i++) pool.Tick(H, false);
            pool.CoolNow();
            Assert.That(pool.Phase, Is.EqualTo(LavaPoolPhase.Cooling));
            Assert.That(pool.Tick(H, true), Is.EqualTo(0f));
            for (int i = 0; i < (int)(d.poolCoolSeconds / H) + 2; i++) pool.Tick(H, false);
            Assert.That(pool.Gone, Is.True);
        }

        // ------------------------------------------------------------ the ooze's throb

        [Test]
        public void TheOozeThrobsFasterThroughItsWindUp()
        {
            var d = HellEnemies.LavaOoze();
            var p = new OozePulse(d);
            Assert.That(p.Rate(false, 0f), Is.EqualTo(d.pulseIdleHz));
            Assert.That(p.Rate(true, 0f), Is.EqualTo(d.pulseAwakeHz));
            Assert.That(p.Rate(true, 1f), Is.EqualTo(d.pulseTelegraphHz).Within(1e-4f));
            float prev = 0f;
            for (float k = 0f; k <= 1f; k += 0.1f)
            {
                Assert.That(p.Rate(true, k), Is.GreaterThanOrEqualTo(prev));
                prev = p.Rate(true, k);
            }
            int beats = 0;
            float last = p.Phase;
            for (float t = 0f; t < 10f; t += H)
            {
                p.Tick(H, false, 0f);
                if (p.Phase < last) beats++;
                last = p.Phase;
                Assert.That(p.Throb, Is.InRange(0f, 1f));
                Assert.That(p.Scale.X, Is.InRange(1f, 1f + d.pulseAmount + 1e-4f));
            }
            Assert.That(beats, Is.EqualTo((int)(10f * d.pulseIdleHz)).Within(1));
        }
    }
}
