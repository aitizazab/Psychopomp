using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The deck, the run's flow, the six objectives and the hazards' state machines (docs/HELL-ROOMS.md).</summary>
    public class HellRunTests
    {
        // ------------------------------------------------------------------ the deck and the run

        [Test]
        public void TheDeckDealsFourDistinctRoomsPerFloorFromTheSeed()
        {
            var a = HellDeck.Deal(42);
            Assert.That(a.Length, Is.EqualTo(3));
            foreach (var f in a)
            {
                Assert.That(f.Length, Is.EqualTo(4));
                Assert.That(f.Distinct().Count(), Is.EqualTo(4));
            }
            Assert.That(HellDeck.Deal(42).SelectMany(f => f), Is.EqualTo(a.SelectMany(f => f)), "same seed, same deal");
            var seen = new HashSet<HellRoomKind>();
            int differ = 0;
            for (ulong s = 1; s < 50; s++)
            {
                var d = HellDeck.Deal(s);
                foreach (var f in d) foreach (var k in f) seen.Add(k);
                if (!d[0].SequenceEqual(a[0])) differ++;
            }
            Assert.That(seen.Count, Is.EqualTo(6));
            Assert.That(differ, Is.GreaterThan(40));
        }

        [Test]
        public void TheRunGoesRoomByRoomFloorByFloorWithAPickBetween()
        {
            var run = new HellRun(7);
            var entered = new List<string>();
            var cleared = new List<int>();
            bool finished = false;
            run.RoomEntered += (f, r) => entered.Add(f + ":" + r);
            run.FloorCleared += f => cleared.Add(f);
            run.Finished += () => finished = true;
            Assert.That(run.Landed(), Is.False, "not started");
            Assert.That(run.Start(), Is.True);
            for (int f = 0; f < 3; f++)
            {
                for (int r = 0; r < 4; r++)
                {
                    Assert.That(run.Phase, Is.EqualTo(HellRunPhase.InRoom));
                    Assert.That(run.Died(), Is.True);
                    Assert.That(run.ReachedDrop(), Is.True);
                    if (r < 3)
                    {
                        Assert.That(run.Phase, Is.EqualTo(HellRunPhase.Dropping));
                        Assert.That(run.ShaftBottom(), Is.False);
                        Assert.That(run.Landed(), Is.True);
                    }
                }
                Assert.That(run.Phase, Is.EqualTo(HellRunPhase.Shaft));
                Assert.That(run.ShaftBottom(), Is.True);
                if (f < 2)
                {
                    Assert.That(run.Phase, Is.EqualTo(HellRunPhase.AwaitingPick));
                    Assert.That(run.Landed(), Is.False);
                    Assert.That(run.Continue(), Is.True);
                }
            }
            Assert.That(finished, Is.True);
            Assert.That(run.Phase, Is.EqualTo(HellRunPhase.Finished));
            Assert.That(cleared, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(entered.Count, Is.EqualTo(12));
            Assert.That(entered[4], Is.EqualTo("1:0"));
            Assert.That(run.Deaths, Is.EqualTo(12));
            Assert.That(run.RoomsCleared, Is.EqualTo(12));
        }

        // ------------------------------------------------------------------ the objectives

        static HellObjective For(HellRoomKind k) => HellObjective.For(HellRoomLayouts.Build(k));

        [Test]
        public void SigilsCountOnlyInOrder()
        {
            var r = HellRoomLayouts.Build(HellRoomKind.SixSigils);
            var o = (SigilChain)HellObjective.For(r);
            Assert.That(o.Tick(r.stops[1].position), Is.EqualTo(-1), "B before A");
            Assert.That(o.Tick(r.stops[0].position + new Vector3(0, 6, 0)), Is.EqualTo(-1), "from far above");
            Assert.That(o.Tick(r.stops[0].position + new Vector3(1.5f, 0, 0)), Is.EqualTo(0));
            var snap = o.Snapshot();
            for (int i = 1; i < 6; i++) Assert.That(o.Tick(r.stops[i].position), Is.EqualTo(i));
            Assert.That(o.Done, Is.True);
            o.Restore(snap);
            Assert.That(o.Next, Is.EqualTo(1));
            Assert.That(o.Line, Does.Contain("SIGIL B"));
        }

        [Test]
        public void TheLanternIsTakenCarriedDroppedAndPlaced()
        {
            var r = HellRoomLayouts.Build(HellRoomKind.SoulLantern);
            var o = (LanternCarry)HellObjective.For(r);
            Assert.That(o.Tick(r.stops[1].position), Is.Null, "the altar without the lantern");
            Assert.That(o.Tick(r.stops[0].position), Is.EqualTo(LanternState.Carried));
            Assert.That(o.Carrying, Is.True);
            Assert.That(o.Drop(), Is.True);
            Assert.That(o.State, Is.EqualTo(LanternState.OnPedestal));
            o.Tick(r.stops[0].position);
            Assert.That(o.Tick(r.stops[1].position), Is.EqualTo(LanternState.Placed));
            Assert.That(o.Done && !o.Carrying, Is.True);
        }

        [Test]
        public void AnchorsAreWarded()
        {
            var r = HellRoomLayouts.Build(HellRoomKind.BreakChains);
            var o = (ChainAnchors)HellObjective.For(r);
            var a0 = r.stops[0].position;
            Assert.That(o.Hit(0, 500f, a0 + new Vector3(30, 0, 0)), Is.EqualTo(ChainAnchors.HitResult.Deflected));
            Assert.That(o.Hit(0, 50f, a0 + new Vector3(5, 0, 0)), Is.EqualTo(ChainAnchors.HitResult.Damaged));
            var snap = o.Snapshot();
            Assert.That(o.Hit(0, 500f, a0 + new Vector3(5, 0, 0)), Is.EqualTo(ChainAnchors.HitResult.Broke));
            Assert.That(o.BrokenCount, Is.EqualTo(1));
            o.Restore(snap);
            Assert.That(o.BrokenCount, Is.EqualTo(0));
            for (int i = 0; i < 3; i++) o.Hit(i, 1000f, r.stops[i].position);
            Assert.That(o.Done, Is.True);
        }

        [Test]
        public void TheBrandChargesPausesAndCallsWaves()
        {
            var r = HellRoomLayouts.Build(HellRoomKind.HoldBrand);
            var o = (BrandHold)HellObjective.For(r);
            var c = r.stops[0].position;
            var inside = c + new Vector3(-6.6f, 0, -5f);                 // behind the south-west stone
            var outside = c + new Vector3(10.5f, 0, 0);
            Assert.That(o.Radius, Is.EqualTo(HellBrandTuning.RingRadius));
            Assert.That(o.Tick(0.1f, outside, 0f), Is.EqualTo(0), "no waves before it is touched");
            Assert.That(o.Circle.ActiveCircle, Is.EqualTo(-1), "the HUD ring hides outside");
            Assert.That(o.Tick(0.1f, inside, 0f), Is.EqualTo(1), "the first wave at once");
            Assert.That(o.Circle.ActiveCircle, Is.EqualTo(0), "the HUD ring shows inside");
            Assert.That(o.Progress, Is.EqualTo(0.1f / HellBrandTuning.SlowSeconds).Within(1e-5), "standing: the slow rate");
            Assert.That(o.Circle.Sheet[0].meter.Progress, Is.EqualTo(o.Progress).Within(1e-4), "the HUD ring shows the charge");
            int waves = 1;
            for (int i = 0; i < 40; i++) waves += o.Tick(0.1f, outside, 9.5f);     // 4 s outside: paused, waves keep coming
            Assert.That(o.Progress, Is.EqualTo(0.1f / HellBrandTuning.SlowSeconds).Within(1e-5), "outside, even at a sprint, it pauses");
            Assert.That(waves, Is.EqualTo(1));
            var snap = o.Snapshot();
            for (int i = 0; i < 400 && !o.Done; i++) waves += o.Tick(0.1f, inside, 0f);
            Assert.That(o.Done, Is.True);
            Assert.That(o.Circle.Sheet[0].Towed, Is.True, "the HUD's meter completes with it");
            Assert.That(waves, Is.EqualTo(o.WavesReleased));
            Assert.That(waves, Is.GreaterThanOrEqualTo(6), "30 s of standing: a wave every 5 s");
            o.Restore(snap);
            Assert.That(o.Done, Is.False);
            Assert.That(o.Progress, Is.EqualTo(0.1f / HellBrandTuning.SlowSeconds).Within(1e-4));
            Assert.That(o.WavesReleased, Is.EqualTo(1));
            Assert.That(o.Circle.Sheet[0].Towed, Is.False);
            Assert.That(o.Circle.Sheet.Count, Is.EqualTo(1), "one circle for the HUD");
        }

        [TestCase(0f, 30f)]          // standing, crouching behind a stone
        [TestCase(3f, 30f)]          // crouch-walking
        [TestCase(9.5f, 10f)]        // a sprint
        [TestCase(8.64f, 10f)]       // a wall-run
        [TestCase(13f, 10f)]         // a boosted slide
        public void TheBrandChargesFromSpeed(float speed, float seconds)
        {
            var r = HellRoomLayouts.Build(HellRoomKind.HoldBrand);
            var o = (BrandHold)HellObjective.For(r);
            var inside = r.hold.loop[0];
            float t = 0f;
            int waves = 0;
            while (!o.Done && t < 60f) { waves += o.Tick(0.02f, inside, speed); t += 0.02f; }
            Assert.That(t, Is.EqualTo(seconds).Within(0.05f));
            Assert.That(waves, Is.EqualTo(1 + (int)((seconds - 0.02f) / HellBrandTuning.WaveEvery)), "the waves come on the clock, not the charge");
        }

        [Test]
        public void TheBrandsRateGrowsWithSpeedAndTheGlowFollows()
        {
            Assert.That(HellBrandTuning.Multiplier(0f), Is.EqualTo(1f));
            Assert.That(HellBrandTuning.Multiplier(5f), Is.InRange(1.5f, 2f), "a walk is in between");
            Assert.That(HellBrandTuning.Multiplier(HellBrandTuning.FullSpeed), Is.EqualTo(HellBrandTuning.FastRate));
            Assert.That(HellBrandTuning.FastSeconds, Is.EqualTo(10f).Within(1e-4));
            for (float v = 0f; v < 15f; v += 0.5f)
                Assert.That(HellBrandTuning.Rate(v + 0.5f), Is.GreaterThanOrEqualTo(HellBrandTuning.Rate(v)));
            var r = HellRoomLayouts.Build(HellRoomKind.HoldBrand);
            var o = (BrandHold)HellObjective.For(r);
            o.Tick(0.1f, r.hold.loop[0], 9.5f);
            Assert.That(o.RateShare, Is.EqualTo(1f));
            Assert.That(o.RateMultiplier, Is.EqualTo(3f));
            Assert.That(o.Line, Does.Contain("x3.0"));
            o.Tick(0.1f, r.hold.loop[0], 0f);
            Assert.That(o.RateShare, Is.EqualTo(0f));
            o.Tick(0.1f, r.hold.center + new Vector3(0, 0, 10.5f), 9.5f);
            Assert.That(o.RateShare, Is.EqualTo(0f), "outside the ring: no glow");
            Assert.That(o.InCircle(r.hold.center + new Vector3(9f, 3.5f, 0)), Is.True, "a wall-run's height counts");
        }

        [Test]
        public void TheCollapseCallsImpsAtTheWallsOnTheClock()
        {
            var r = HellRoomLayouts.Build(HellRoomKind.Collapse);
            var o = (CollapseEscape)HellObjective.For(r);
            Assert.That(o.WaveCount, Is.EqualTo(3));
            Assert.That(o.WaveEvery, Is.EqualTo(HellRoomLayouts.CollapseWaveEvery));
            int waves = 0;
            for (int i = 0; i < 50 * 20; i++) waves += o.TickWaves(0.02f);
            Assert.That(waves, Is.EqualTo(3), "each wave once");
            var snap = o.Snapshot();
            o.Reset();
            Assert.That(o.TickWaves(2.9f), Is.EqualTo(0));
            Assert.That(o.TickWaves(0.2f), Is.EqualTo(1), "the first at 3 s");
            o.Restore(snap);
            Assert.That(o.WavesReleased, Is.EqualTo(3));
            foreach (var s in r.spawns.Where(s => s.wave > 0))
            {
                Assert.That(s.role, Is.EqualTo(HellEnemyRole.Imp));
                Assert.That(System.Math.Abs(s.position.X), Is.GreaterThan(r.bounds.max.X - 3f), "at the walls");
            }
        }

        [Test]
        public void WellsSealAndSpawnUnderTheirCap()
        {
            var r = HellRoomLayouts.Build(HellRoomKind.OozeWells);
            var o = (OozeWells)HellObjective.For(r);
            var spawns = new List<int>();
            var far = r.entry;
            var living = new[] { 0, 2, 0 };
            int total1 = 0, total2 = 0;
            for (int i = 0; i < 300; i++)
            {
                o.Tick(0.1f, far, living, spawns);
                total1 += spawns.Count(s => s == 0);
                total2 += spawns.Count(s => s == 1);
            }
            Assert.That(total1, Is.GreaterThan(2));
            Assert.That(total2, Is.EqualTo(0), "a well at its cap spawns nothing");
            int sealedAt = -1;
            for (int i = 0; i < 40 && sealedAt < 0; i++) sealedAt = o.Tick(0.1f, r.stops[2].position + new Vector3(2, 0, 0), living, spawns);
            Assert.That(sealedAt, Is.EqualTo(2));
            Assert.That(o.SealedCount, Is.EqualTo(1));
        }

        [Test]
        public void TheCollapseEndsAtTheExit()
        {
            var r = HellRoomLayouts.Build(HellRoomKind.Collapse);
            var o = (CollapseEscape)HellObjective.For(r);
            Assert.That(o.Tick(r.entry), Is.False);
            Assert.That(o.Tick(r.stops[0].position), Is.True);
            Assert.That(o.Done, Is.True);
        }

        // ------------------------------------------------------------------ the hazards

        [Test]
        public void LavaBurnsThenSinks()
        {
            var lava = new LavaContact(10f, 35f, 1f);
            float dmg = 0f;
            bool sank = false;
            for (int i = 0; i < 9; i++) { dmg += lava.Tick(0.1f, true, false, out sank); Assert.That(sank, Is.False); }
            Assert.That(dmg, Is.EqualTo(9f).Within(0.01f));
            float hit = lava.Tick(0.15f, true, false, out sank);
            Assert.That(sank, Is.True);
            Assert.That(hit, Is.EqualTo(35f));
            Assert.That(lava.Tick(0.1f, true, true, out sank), Is.EqualTo(0f), "immune");
            Assert.That(lava.Tick(0.1f, false, false, out sank), Is.EqualTo(0f));
            Assert.That(lava.S.contact, Is.EqualTo(0f), "leaving resets the sink clock");
            var box = new CoverBox { min = new Vector3(0, -2.5f, 0), max = new Vector3(10, -1.2f, 10) };
            Assert.That(LavaContact.Touching(box, new Vector3(5, -1.2f, 5)), Is.True);
            Assert.That(LavaContact.Touching(box, new Vector3(5, 0f, 5)), Is.False);
        }

        [Test]
        public void PlatformsShakeFallAndReturn()
        {
            var p = new CollapseCycle(0.8f, 4f);
            Assert.That(p.Tick(0.1f, false, false), Is.False);
            Assert.That(p.S.state, Is.EqualTo(CollapseState.Solid));
            p.Tick(0.1f, true, false);
            Assert.That(p.S.state, Is.EqualTo(CollapseState.Shaking));
            bool fell = false;
            float t = 0f;
            while (!fell && t < 2f) { fell = p.Tick(0.05f, false, false); t += 0.05f; }
            Assert.That(t, Is.EqualTo(0.8f).Within(0.06f), "leaving does not save it");
            Assert.That(p.Solid, Is.False);
            for (int i = 0; i < 100; i++) p.Tick(0.05f, false, true);
            Assert.That(p.Solid, Is.False, "waits while the body is in its place");
            p.Tick(0.05f, false, false);
            Assert.That(p.Solid, Is.True);
            var never = new CollapseCycle(0.8f, -1f);
            never.Trigger();
            for (int i = 0; i < 1000; i++) never.Tick(0.1f, false, false);
            Assert.That(never.Solid, Is.False);
            var snap = p.S;
            p.Trigger();
            p.S = snap;
            Assert.That(p.S.state, Is.EqualTo(CollapseState.Solid));
        }

        [Test]
        public void BeamsCycleAndOnlyASlidePassesUnder()
        {
            Assert.That(BeamCycle.At(0.5f, 2f, 1f, 0f), Is.EqualTo(BeamState.On));
            Assert.That(BeamCycle.At(2.2f, 2f, 1f, 0f), Is.EqualTo(BeamState.Off));
            Assert.That(BeamCycle.At(2.8f, 2f, 1f, 0f), Is.EqualTo(BeamState.Warning));
            var beam = new CoverBox { min = new Vector3(-3, 1.2f, 49.8f), max = new Vector3(3, 1.4f, 50.2f) };
            Assert.That(BeamCycle.Hits(beam, new Vector3(0, 0, 50), 1.7f), Is.True, "standing");
            Assert.That(BeamCycle.Hits(beam, new Vector3(0, 0, 50), 1.0f), Is.False, "sliding");
            Assert.That(BeamCycle.Hits(beam, new Vector3(0, 0, 52), 1.7f), Is.False);
        }

        [Test]
        public void TheSafeSpotFollowsSettledFeet()
        {
            var s = new SafeSpot(Vector3.Zero, 0f);
            s.Tick(0.2f, new Vector3(5, 0, 0), 90f, true);
            Assert.That(s.Spot, Is.EqualTo(Vector3.Zero));
            s.Tick(0.2f, new Vector3(5, 0, 0), 90f, true);
            Assert.That(s.Spot, Is.EqualTo(new Vector3(5, 0, 0)));
            s.Tick(1f, new Vector3(9, 0, 0), 0f, false);
            Assert.That(s.Spot, Is.EqualTo(new Vector3(5, 0, 0)));
        }
    }
}
