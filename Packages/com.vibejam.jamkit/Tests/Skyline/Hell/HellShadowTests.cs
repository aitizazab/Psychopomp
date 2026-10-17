using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The Shadow mechanics' pure rules (docs/HELL-SHADOW.md): hidden/revealed, the ranges, the reveal timers, fire
    /// lighting the dark, the hush, and the room lint's relaxed fire rule.</summary>
    public class HellShadowTests
    {
        const float Dt = 1f / 60f;

        static PlayerStealth Settled(bool fast = false)
        {
            var s = new PlayerStealth();
            for (int i = 0; i < 30; i++) s.Tick(Dt, true, false, fast);
            return s;
        }

        // ------------------------------------------------------------------ hidden / revealed

        [Test]
        public void DarknessHidesYouOnlyAfterSettlingAndBeyondFourMetres()
        {
            var s = new PlayerStealth();
            s.Tick(0.1f, true, false, false);
            Assert.That(s.Concealed, Is.False, "a dash through a dark patch doesn't hide you");
            s.Tick(0.25f, true, false, false);
            Assert.That(s.Concealed, Is.True);
            Assert.That(s.HiddenFrom(4.5f), Is.True);
            Assert.That(s.HiddenFrom(3.9f), Is.False, "an enemy within 4 m sees you in any darkness");
        }

        [Test]
        public void MovingFastShrinksTheCoverToTwelveMetres()
        {
            var s = Settled(fast: true);
            Assert.That(s.Concealed, Is.True);
            Assert.That(s.HiddenFrom(8f), Is.False, "sprinting in the dark: seen at 8 m");
            Assert.That(s.HiddenFrom(12.5f), Is.True);
            s.Tick(Dt, true, false, false);
            Assert.That(s.HiddenFrom(8f), Is.True, "slow down and the dark takes you back at once");
        }

        [Test]
        public void FastMeansOverSevenMetresASecondOrASlideOrWallrun()
        {
            var t = new ShadowTuning();
            Assert.That(t.IsFast(5f, false), Is.False, "walking");
            Assert.That(t.IsFast(9.5f, false), Is.True, "sprinting");
            Assert.That(t.IsFast(3f, true), Is.True, "a slow slide still counts");
        }

        [Test]
        public void YourShotRevealsYouForOneAndAHalfSecondsAndTheFirstCountsAsFromHiding()
        {
            var s = Settled();
            s.Fired();
            Assert.That(s.TakeShotFromShadow(), Is.True, "the first shot came from hiding");
            Assert.That(s.TakeShotFromShadow(), Is.False, "one Ambush per shot");
            Assert.That(s.Concealed, Is.False);
            Assert.That(s.HiddenFrom(30f), Is.False);
            s.Fired();
            Assert.That(s.TakeShotFromShadow(), Is.False, "the second shot is from a revealed spot");
            for (int i = 0; i < 89; i++) s.Tick(Dt, true, false, false);
            Assert.That(s.Concealed, Is.False, "still revealed just under 1.5 s on");
            for (int i = 0; i < 3; i++) s.Tick(Dt, true, false, false);
            Assert.That(s.Concealed, Is.True, "hidden again after 1.5 s");
        }

        [Test]
        public void FireLightOnYouRevealsYouAndForOneAndAHalfSecondsAfterItPasses()
        {
            var s = Settled();
            s.Tick(Dt, true, true, false);
            Assert.That(s.Lit, Is.True);
            Assert.That(s.Concealed, Is.False);
            s.Tick(0.5f, true, false, false);
            Assert.That(s.Concealed, Is.False, "the light passed 0.5 s ago");
            s.Tick(1.05f, true, false, false);
            Assert.That(s.Concealed, Is.True);
        }

        [Test]
        public void LeavingTheDarkEndsItAndComingBackSettlesAgain()
        {
            var s = Settled();
            s.Tick(Dt, false, false, false);
            Assert.That(s.Concealed, Is.False);
            Assert.That(s.DarkTime, Is.EqualTo(0f));
            s.Tick(Dt, true, false, false);
            Assert.That(s.Concealed, Is.False);
        }

        [Test]
        public void LightOutsideTheDarkDoesNotCountAsBeingLit()
        {
            var s = new PlayerStealth();
            s.Tick(Dt, false, true, false);
            Assert.That(s.Lit, Is.False);
            Assert.That(s.Revealed, Is.False);
        }

        [Test]
        public void TheStateSavesAndRestoresForTheRewind()
        {
            var s = Settled();
            var snap = s.Save();
            s.Fired();
            Assert.That(s.Concealed, Is.False);
            s.Restore(snap);
            Assert.That(s.Concealed, Is.True);
            Assert.That(s.TakeShotFromShadow(), Is.False);
        }

        // ------------------------------------------------------------------ fire lights the dark

        static DarknessField Zone()
        {
            var f = new DarknessField();
            f.AddZone(new Vector3(0, 0, 0), new Vector3(4, 3, 4));
            return f;
        }

        [Test]
        public void APassingFlashLightsTheDarkForItsTimeOnly()
        {
            var f = Zone();
            var p = new Vector3(2, 1, 2);
            Assert.That(f.IsDark(p, 10.0), Is.True);
            f.AddFlash(new Vector3(2, 1, 3), FireExposure.ImpactRadius, 10.0 + FireExposure.ImpactSeconds);
            Assert.That(f.IsDark(p, 10.0), Is.False, "a bolt fired at your hiding spot floods it");
            Assert.That(f.IsLit(p, 10.0), Is.True);
            Assert.That(f.InDarkVolume(p, 10.0), Is.True, "still a dark zone, only lit");
            Assert.That(f.IsDark(p, 11.6), Is.True, "dark again once the flash is over");
            Assert.That(f.IsDark(p), Is.True, "with no time given (the traversal sims) passing fire is ignored");
        }

        [Test]
        public void FlashesLiveInAFixedRingTheOldestGoesFirst()
        {
            var f = Zone();
            f.AddFlash(new Vector3(2, 1, 2), 1f, 100.0);
            for (int i = 0; i < DarknessField.FlashCapacity - 1; i++) f.AddFlash(new Vector3(50, 0, 50), 1f, 100.0);
            Assert.That(f.IsDark(new Vector3(2, 1, 2), 1.0), Is.False);
            f.AddFlash(new Vector3(50, 0, 50), 1f, 100.0);
            Assert.That(f.IsDark(new Vector3(2, 1, 2), 1.0), Is.True, "the oldest was overwritten");
            Assert.That(f.FlashCount(1.0), Is.EqualTo(DarknessField.FlashCapacity));
            f.ClearFlashes();
            Assert.That(f.FlashCount(1.0), Is.EqualTo(0));
        }

        [Test]
        public void ABoltLightsOnlyNearTheDark()
        {
            var f = Zone();
            Assert.That(f.NearDarkVolume(new Vector3(6, 1, 2), FireExposure.BoltNearDark), Is.True, "2 m off the zone");
            Assert.That(f.NearDarkVolume(new Vector3(9, 1, 2), FireExposure.BoltNearDark), Is.False, "5 m off");
            f.AddSphere(new Vector3(30, 1, 30), 6f, 20.0);
            Assert.That(f.NearDarkVolume(new Vector3(30, 1, 38), FireExposure.BoltNearDark, 1.0), Is.True, "beside a Nightfall sphere");
            Assert.That(f.NearDarkVolume(new Vector3(30, 1, 38), FireExposure.BoltNearDark, 21.0), Is.False, "the sphere is gone");
            Assert.That(FireExposure.StampsFor(0.2f), Is.EqualTo(1));
            Assert.That(FireExposure.StampsFor(3.1f), Is.EqualTo(3), "a 90 m/s bolt at 30 fps leaves no gaps");
        }

        [Test]
        public void AFlashMakesAShadowStepLandingInvalid()
        {
            var f = Zone();
            var landing = new Vector3(2, 0.5f, 2);
            var plan = new BlinkPlan { valid = true };
            Assert.That(BlinkRules.Apply(plan, f.IsDark(landing, 5.0), false, false).valid, Is.True);
            f.AddFlash(landing, FireExposure.BoltRadius, 5.5);
            Assert.That(BlinkRules.Apply(plan, f.IsDark(landing, 5.0), false, false).valid, Is.False);
        }

        // ------------------------------------------------------------------ the hush

        [Test]
        public void TheHushDeepensWhenHiddenAndLetsGoWhenExposed()
        {
            Assert.That(DarkHush.TargetFor(true, false, true), Is.EqualTo(1f));
            Assert.That(DarkHush.TargetFor(true, true, true), Is.EqualTo(0.7f));
            Assert.That(DarkHush.TargetFor(false, false, true), Is.EqualTo(0.35f));
            Assert.That(DarkHush.TargetFor(false, false, false), Is.EqualTo(0f));
            var h = new DarkHush();
            for (int i = 0; i < 120; i++) h.Step(1f, Dt);
            Assert.That(h.Depth, Is.GreaterThan(0.99f));
            Assert.That(h.WorldGainDb, Is.EqualTo(-7f).Within(0.1f));
            Assert.That(h.WorldCutoff, Is.EqualTo(1200f).Within(20f));
            Assert.That(h.Heartbeat, Is.EqualTo(0.85f).Within(0.01f));
            h.Step(0f, 0.6f);
            Assert.That(h.Depth, Is.EqualTo((float)(1.0 / System.Math.E)).Within(0.01f), "one release time constant");
        }

        [Test]
        public void TheMusicDarkModifierDropsTheDrumsClosesTheFilterAndRaisesTheDrone()
        {
            Assert.That(DarkHush.MusicDrums(0f), Is.EqualTo(1f));
            Assert.That(DarkHush.MusicDrums(1f), Is.EqualTo(0f));
            Assert.That(DarkHush.MusicCutoff(0f), Is.EqualTo(22000f).Within(1f));
            Assert.That(DarkHush.MusicCutoff(1f), Is.EqualTo(900f).Within(1f));
            Assert.That(DarkHush.MusicCutoff(0.5f), Is.EqualTo(4450f).Within(50f), "halfway to the ear (log)");
            Assert.That(DarkHush.MusicDrone(1f), Is.EqualTo(0.8f));
        }

        // ------------------------------------------------------------------ the lint: steady fire stays out, passing fire may come in

        [Test]
        public void ALavaFlareIsUpForAMomentEachCycle()
        {
            var l = new HellLight { position = Vector3.Zero, radius = 4f, flareRadius = 9f, flarePeriod = 6f, flareSeconds = 1.2f, flarePhase = 0f };
            Assert.That(l.FlareUp(0.5), Is.True);
            Assert.That(l.FlareUp(1.3), Is.False);
            Assert.That(l.FlareUp(6.1), Is.True);
            Assert.That(new HellLight { radius = 4f }.FlareUp(0.5), Is.False, "a steady light never flares");
        }

        [Test]
        public void SomeRoomsGetLavaFlaresIntoTheirDarkAndNoneOnTheFastLine()
        {
            int flares = 0, rooms = 0;
            foreach (var r in HellRoomVariants.All())
            {
                int n = r.lights.Count(l => l.Flares);
                flares += n;
                if (n > 0) rooms++;
                foreach (var l in r.lights.Where(l => l.Flares))
                    foreach (var d in r.darkZones)
                        if (HellRoomLint.DistanceToBox(l.position, d) < l.flareRadius)
                            Assert.That(HellRoomLint.OnFastLine(r, d), Is.False, r.name + ": a flare into a Shadow Step landing");
            }
            TestContext.WriteLine(flares + " lava flares in " + rooms + " of 18 rooms: " + string.Join(", ", HellRoomVariants.All().Where(r => r.lights.Any(l => l.Flares)).Select(r => r.name)));
            Assert.That(rooms, Is.GreaterThanOrEqualTo(3), "fire reaches the dark somewhere in the descent");
        }

        static (HellRoomDef r, CoverBox fast, CoverBox other) RoomWithBothKinds()
        {
            foreach (var kind in HellRoomLayouts.Kinds)
            {
                var r = HellRoomLayouts.Build(kind);
                var fast = r.darkZones.Where(d => HellRoomLint.OnFastLine(r, d)).ToList();
                var other = r.darkZones.Where(d => !HellRoomLint.OnFastLine(r, d)).ToList();
                if (fast.Count == 0) continue;
                // an off-line zone whose side spot (where Beside puts a light) is clear of every fast-line zone and every other zone
                foreach (var o in other)
                {
                    var at = Beside(o, 0f, 0f).position;
                    if (r.darkZones.All(d => d.Equals(o) || HellRoomLint.DistanceToBox(at, d) > 6f)) return (r, fast[0], o);
                }
            }
            Assert.Fail("no room has a dark zone on the fast line and one off it");
            return (null, default(CoverBox), default(CoverBox));
        }

        static HellLight Beside(CoverBox d, float steady, float flare)
        {
            var c = (d.min + d.max) * 0.5f;
            return new HellLight { position = new Vector3(d.max.X + 5f, c.Y, c.Z), radius = steady, flareRadius = flare, flarePeriod = 6f, flareSeconds = 1.2f };
        }

        static List<string> Problems(HellRoomDef r) => HellRoomLint.Check(r).problems;

        [Test]
        public void TheLintStillForbidsASteadyLightInTheDark()
        {
            var (r, _, other) = RoomWithBothKinds();
            Assert.That(Problems(r), Is.Empty);
            r.lights.Add(Beside(other, 6f, 0f));
            Assert.That(Problems(r).Any(p => p.Contains("reaches a dark zone")), Is.True);
        }

        [Test]
        public void TheLintAllowsAShortFlareOffTheFastLineAndRefusesOneOnIt()
        {
            var (r, fast, other) = RoomWithBothKinds();
            r.lights.Add(Beside(other, 1f, 5.5f));
            Assert.That(Problems(r), Is.Empty, "fire passing into the dark is the point");
            var (r2, fast2, _) = RoomWithBothKinds();
            r2.lights.Add(Beside(fast2, 1f, 5.5f));
            Assert.That(Problems(r2).Any(p => p.Contains("fast line")), Is.True);
        }

        [Test]
        public void TheLintRefusesALongOrFrequentFlareAndADarkWithNoSafeCorner()
        {
            var (r, _, other) = RoomWithBothKinds();
            var l = Beside(other, 1f, 5.5f);
            l.flareSeconds = 2.5f;
            r.lights.Add(l);
            Assert.That(Problems(r).Any(p => p.Contains("stays up")), Is.True);

            var (r2, _, other2) = RoomWithBothKinds();
            var busy = Beside(other2, 1f, 5.5f);
            busy.flarePeriod = 3f;
            r2.lights.Add(busy);
            Assert.That(Problems(r2).Any(p => p.Contains("too much of its cycle")), Is.True);

            var (r3, _, _) = RoomWithBothKinds();
            foreach (var d in r3.darkZones.ToList())
            {
                var c = (d.min + d.max) * 0.5f;
                r3.lights.Add(new HellLight { position = c, radius = 0f, flareRadius = 0.5f, flarePeriod = 6f, flareSeconds = 1f });
            }
            Assert.That(Problems(r3).Any(p => p.Contains("every dark zone")), Is.True);
        }
    }
}
