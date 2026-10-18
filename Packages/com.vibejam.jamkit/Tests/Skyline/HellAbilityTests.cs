using System;
using System.Collections.Generic;
using System.Numerics;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    public class HellAbilityTests
    {
        // ---------------------------------------------------------------- a box world for the blink planner

        sealed class Boxes : IBlinkWorld
        {
            public readonly List<(Vector3 min, Vector3 max)> list = new List<(Vector3, Vector3)>();
            public Boxes Add(Vector3 min, Vector3 max) { list.Add((min, max)); return this; }

            public bool Raycast(Vector3 from, Vector3 dir, float maxDistance, out Vector3 point, out Vector3 normal)
            {
                point = normal = Vector3.Zero;
                float best = float.PositiveInfinity;
                foreach (var b in list)
                {
                    if (Inside(from, b)) continue;   // like physics: a ray starting inside a collider ignores it
                    if (!Slab(from, dir, b, out float t, out var n) || t > maxDistance || t >= best) continue;
                    best = t; normal = n;
                }
                if (float.IsPositiveInfinity(best)) return false;
                point = from + dir * best;
                return true;
            }

            public bool BodyFits(Vector3 feet, float radius, float height)
            {
                var min = feet + new Vector3(-radius, 0.01f, -radius);
                var max = feet + new Vector3(radius, height, radius);
                foreach (var b in list)
                    if (min.X < b.max.X && max.X > b.min.X && min.Y < b.max.Y && max.Y > b.min.Y && min.Z < b.max.Z && max.Z > b.min.Z) return false;
                return true;
            }

            static bool Inside(Vector3 p, (Vector3 min, Vector3 max) b) =>
                p.X > b.min.X && p.X < b.max.X && p.Y > b.min.Y && p.Y < b.max.Y && p.Z > b.min.Z && p.Z < b.max.Z;

            static bool Slab(Vector3 o, Vector3 d, (Vector3 min, Vector3 max) b, out float tHit, out Vector3 n)
            {
                float tmin = 0f, tmax = float.PositiveInfinity;
                n = Vector3.Zero;
                tHit = 0f;
                for (int a = 0; a < 3; a++)
                {
                    float oa = a == 0 ? o.X : a == 1 ? o.Y : o.Z, da = a == 0 ? d.X : a == 1 ? d.Y : d.Z;
                    float lo = a == 0 ? b.min.X : a == 1 ? b.min.Y : b.min.Z, hi = a == 0 ? b.max.X : a == 1 ? b.max.Y : b.max.Z;
                    if (Math.Abs(da) < 1e-8f) { if (oa < lo || oa > hi) return false; continue; }
                    float t1 = (lo - oa) / da, t2 = (hi - oa) / da;
                    float sign = -1f;
                    if (t1 > t2) { var t = t1; t1 = t2; t2 = t; sign = 1f; }
                    if (t1 > tmin)
                    {
                        tmin = t1;
                        n = a == 0 ? new Vector3(sign, 0, 0) : a == 1 ? new Vector3(0, sign, 0) : new Vector3(0, 0, sign);
                    }
                    tmax = Math.Min(tmax, t2);
                    if (tmin > tmax) return false;
                }
                tHit = tmin;
                return n != Vector3.Zero;
            }
        }

        static Boxes Floor() => new Boxes().Add(new Vector3(-100, -1, -100), new Vector3(100, 0, 100));
        static readonly Vector3 Eye = new Vector3(0, 1.6f, 0);

        static Vector3 Dir(float yawDeg, float pitchDownDeg)
        {
            double y = yawDeg * Math.PI / 180, p = pitchDownDeg * Math.PI / 180;
            return Vector3.Normalize(new Vector3((float)(Math.Sin(y) * Math.Cos(p)), (float)-Math.Sin(p), (float)(Math.Cos(y) * Math.Cos(p))));
        }

        [Test]
        public void BlinkLandsOnGroundItAimsAt()
        {
            var plan = BlinkPlanner.Plan(Eye, Dir(0, 10), new BlinkDef(), Floor());
            Assert.That(plan.valid, Is.True, plan.reason);
            Assert.That(plan.kind, Is.EqualTo(BlinkKind.Ground));
            Assert.That(plan.feet.Z, Is.EqualTo(1.6f / (float)Math.Tan(10 * Math.PI / 180)).Within(0.05f));
            Assert.That(plan.feet.Y, Is.EqualTo(0.05f).Within(1e-3f));
        }

        [Test]
        public void BlinkPastRangeFindsGroundUnderTheEndOfTheAim()
        {
            var plan = BlinkPlanner.Plan(Eye, Dir(0, 0), new BlinkDef(), Floor());
            Assert.That(plan.valid, Is.True, plan.reason);
            Assert.That(plan.kind, Is.EqualTo(BlinkKind.BelowAim));
            Assert.That(plan.feet.Z, Is.EqualTo(18f).Within(0.01f));
        }

        [Test]
        public void BlinkIntoTheVoidIsRefused()
        {
            var w = new Boxes().Add(new Vector3(-2, -1, -2), new Vector3(2, 0, 2));   // a small platform, nothing beyond
            var plan = BlinkPlanner.Plan(Eye, Dir(0, 0), new BlinkDef(), w);
            Assert.That(plan.valid, Is.False);
        }

        [Test]
        public void BlinkAtAWallClimbsToItsLedge()
        {
            // a 2 m block 8 m ahead, aimed at its face
            var w = Floor().Add(new Vector3(-3, 0, 8), new Vector3(3, 2f, 12));
            var plan = BlinkPlanner.Plan(Eye, Dir(0, 0), new BlinkDef(), w);
            Assert.That(plan.valid, Is.True, plan.reason);
            Assert.That(plan.kind, Is.EqualTo(BlinkKind.Ledge));
            Assert.That(plan.feet.Y, Is.EqualTo(2.05f).Within(1e-3f));
            Assert.That(plan.feet.Z, Is.GreaterThan(8f));
        }

        [Test]
        public void BlinkAtATallWallStopsAtItsFoot()
        {
            var w = Floor().Add(new Vector3(-3, 0, 8), new Vector3(3, 10f, 12));
            var plan = BlinkPlanner.Plan(Eye, Dir(0, 0), new BlinkDef(), w);
            Assert.That(plan.valid, Is.True, plan.reason);
            Assert.That(plan.kind, Is.EqualTo(BlinkKind.AtWallFoot));
            Assert.That(plan.feet.Z, Is.LessThan(8f - 0.4f));
            Assert.That(plan.feet.Y, Is.EqualTo(0.05f).Within(1e-3f));
        }

        [Test]
        public void BlinkUnderALowCeilingHasNoRoom()
        {
            // a slab 1 m over the floor from 4 m on: the ground under it is aimed at but the body doesn't fit
            var w = Floor().Add(new Vector3(-5, 1f, 4), new Vector3(5, 1.2f, 20));
            var plan = BlinkPlanner.Plan(new Vector3(0, 0.6f, 0), Dir(0, 5), new BlinkDef(), w);
            Assert.That(plan.valid, Is.False);
        }

        [Test]
        public void BlinkRangeScalesWithTheSkill()
        {
            var d = new BlinkDef { rangeScale = 1.2f };
            var plan = BlinkPlanner.Plan(Eye, Dir(0, 0), d, Floor());
            Assert.That(plan.valid, Is.True);
            Assert.That(plan.feet.Z, Is.EqualTo(18f * 1.2f).Within(0.01f));
        }

        // ---------------------------------------------------------------- cooldown

        [Test]
        public void CooldownBlocksUntilItRunsOut()
        {
            var c = new AbilityCooldown(6f);
            Assert.That(c.TryUse(), Is.True);
            Assert.That(c.TryUse(), Is.False);
            c.Tick(5.9f);
            Assert.That(c.Ready, Is.False);
            Assert.That(c.Fraction, Is.EqualTo(5.9f / 6f).Within(1e-4f));
            c.Tick(0.2f);
            Assert.That(c.Ready, Is.True);
            Assert.That(c.TryUse(), Is.True);
        }

        // ---------------------------------------------------------------- dilation

        [Test]
        public void DilationCurveIsNoneStandingAndStrongAtSlideSpeed()
        {
            var c = new DilationCurve();
            Assert.That(c.Scale(0f), Is.EqualTo(1f));
            Assert.That(c.Scale(5f), Is.GreaterThan(0.75f));          // walking: a little
            Assert.That(c.Scale(9.5f), Is.LessThan(0.55f));           // sprint / wall-run: strong
            Assert.That(c.Scale(13f), Is.EqualTo(0.35f).Within(1e-5f));
            float last = 1f;
            for (float v = 0; v < 15f; v += 0.5f) { float s = c.Scale(v); Assert.That(s, Is.LessThanOrEqualTo(last + 1e-6f)); last = s; }
        }

        [Test]
        public void DilationEasesInHoldsThreeSecondsAndEasesOut()
        {
            var d = new TimeDilationState();
            d.Press(0.35f);
            d.Tick(0.075f);
            Assert.That(d.Scale, Is.InRange(0.36f, 0.99f));     // halfway through the ease
            d.Tick(0.1f);
            Assert.That(d.Scale, Is.EqualTo(0.35f).Within(1e-5f));
            d.Tick(2.8f);                                        // 2.975 s after the press
            Assert.That(d.Scale, Is.EqualTo(0.35f).Within(1e-5f));
            d.Tick(0.1f);                                        // easing out
            Assert.That(d.Scale, Is.InRange(0.36f, 0.999f));
            d.Tick(0.2f);
            Assert.That(d.Scale, Is.EqualTo(1f));
            Assert.That(d.Active, Is.False);
        }

        [Test]
        public void DilationRepressStartsFromTheCurrentScale()
        {
            var d = new TimeDilationState();
            d.Press(0.35f);
            d.Tick(1f);
            d.Press(0.8f);
            d.Tick(0.001f);
            Assert.That(d.Scale, Is.EqualTo(0.35f).Within(0.01f));   // no jump back to 1
            d.Tick(0.2f);
            Assert.That(d.Scale, Is.EqualTo(0.8f).Within(1e-5f));
        }

        [Test]
        public void TimeScaleIsTheSmallestRequest()
        {
            var r = new TimeScaleRequests();
            Assert.That(r.Scale, Is.EqualTo(1f));
            r.Request("dilation", 0.5f);
            r.Request("hitstop", 0.05f);
            Assert.That(r.Scale, Is.EqualTo(0.05f));
            r.Clear("hitstop");
            Assert.That(r.Scale, Is.EqualTo(0.5f));
            r.Request("dilation", 1f);
            Assert.That(r.Scale, Is.EqualTo(1f));
            Assert.That(r.Count, Is.EqualTo(0));
        }

        // ---------------------------------------------------------------- ring

        [Test]
        public void RingKeepsTheNewestAndFindsFourSecondsAgo()
        {
            const double step = 0.02;
            var ring = new SnapshotRing<int>(SnapshotRing<int>.CapacityFor(4.2f, (float)step));
            for (int i = 0; i < 1000; i++) ring.Push(i * step, i);
            Assert.That(ring.Count, Is.EqualTo(ring.Capacity));
            Assert.That(ring[ring.Count - 1], Is.EqualTo(999));
            double now = 999 * step;
            int at = ring.FindAtOrBefore(now - 4.0);
            Assert.That(ring[at], Is.EqualTo(799));
            Assert.That(ring.OldestTime, Is.LessThanOrEqualTo(now - 4.0));
        }

        [Test]
        public void RingWithShortHistoryGivesTheOldest()
        {
            var ring = new SnapshotRing<string>(10);
            Assert.That(ring.FindAtOrBefore(5), Is.EqualTo(-1));
            ring.Push(3.0, "a");
            ring.Push(3.5, "b");
            Assert.That(ring[ring.FindAtOrBefore(0)], Is.EqualTo("a"));
            ring.Clear();
            Assert.That(ring.Count, Is.EqualTo(0));
        }

        // ---------------------------------------------------------------- the player's rewindable parts

        [Test]
        public void GunArmorAndLedgerRestoreForRewind()
        {
            var cues = new List<GunCue>();
            var gun = new SkylineGun(SkylineGuns.Writ());
            gun.Equip(cues);
            for (int i = 0; i < 200; i++) gun.Tick(1f / 60f, true, i == 0, cues);
            int mag = gun.Ammo;
            Assert.That(mag, Is.LessThan(gun.Def.weapon.magazineSize));
            gun.Reload(cues);
            Assert.That(gun.State.IsReloading, Is.True);
            gun.RestoreForRewind(3, 0);
            Assert.That(gun.State.IsReloading, Is.False);
            Assert.That(gun.Ammo, Is.EqualTo(3));
            gun.Tick(0.5f, false, false, cues);
            Assert.That(gun.Ammo, Is.EqualTo(3));                 // no reload finishes later

            var armor = new PlayerArmor();
            armor.Damage(1000f);
            Assert.That(armor.Wrecked, Is.True);
            armor.Restore(40f, 75f);
            Assert.That(armor.Wrecked, Is.False);
            Assert.That(armor.Plating, Is.EqualTo(40f));
            Assert.That(armor.Chassis, Is.EqualTo(75f));

            var ledger = new SpecialAmmoLedger();
            ledger.Restore(5, 2);
            Assert.That(ledger.Shots, Is.EqualTo(5));
            Assert.That(ledger.KillProgress, Is.EqualTo(2));
        }

        [Test]
        public void WaveClockRestoresItsCount()
        {
            var c = new WaveClock(8, 20f);
            var released = new List<WaveDef>();
            c.Start();
            c.Tick(5f, 0, false, released);
            var save = c.SaveState();
            float toNext = c.SecondsToNext;
            int rel = c.Released;
            c.Tick(30f, 0, false, released);
            Assert.That(c.Released, Is.GreaterThan(rel));
            c.RestoreState(save);
            Assert.That(c.Released, Is.EqualTo(rel));
            Assert.That(c.SecondsToNext, Is.EqualTo(toNext).Within(1e-4f));
        }

        // ---------------------------------------------------------------- charges, darkness, burn, momentum

        [Test]
        public void ChargesRechargeOneAtATimeAndShave()
        {
            var c = new AbilityCharges(2, 6f);
            Assert.That(c.TrySpend() && c.TrySpend(), Is.True);
            Assert.That(c.TrySpend(), Is.False);
            c.Tick(5.9f);
            Assert.That(c.Charges, Is.EqualTo(0));
            c.Tick(0.2f);
            Assert.That(c.Charges, Is.EqualTo(1));
            Assert.That(c.Remaining, Is.EqualTo(5.9f).Within(1e-3f));
            c.Shave(6f);                                   // Borrowed Time: the second comes back at once
            Assert.That(c.Charges, Is.EqualTo(2));
            Assert.That(c.Remaining, Is.EqualTo(0f));
        }

        [Test]
        public void ChargesGainAChargeWithANewMax()
        {
            var c = new AbilityCharges(1, 6f);
            c.TrySpend();
            c.SetMax(2);
            Assert.That(c.Charges, Is.EqualTo(1));
            Assert.That(c.Ready, Is.True);
        }

        [Test]
        public void DarknessIsZonesAndSpheresUnlessLit()
        {
            var f = new DarknessField();
            Assert.That(f.IsDark(Vector3.Zero), Is.False);
            int z = f.AddZone(new Vector3(-5, -1, -5), new Vector3(5, 4, 5));
            Assert.That(f.IsDark(new Vector3(1, 0, 1)), Is.True);
            int l = f.AddLight(new Vector3(4, 0, 4), 2f);
            Assert.That(f.IsDark(new Vector3(4, 0, 3.5f)), Is.False);    // a fire lights it
            Assert.That(f.IsDark(new Vector3(-4, 0, -4)), Is.True);
            f.RemoveLight(l);
            f.RemoveZone(z);
            f.AddSphere(new Vector3(20, 0, 0), 4f, 8.0);
            Assert.That(f.IsDark(new Vector3(22, 0, 0), 5.0), Is.True);
            Assert.That(f.IsDark(new Vector3(22, 0, 0), 9.0), Is.False);  // Nightfall ended
            Assert.That(f.SegmentThroughSphere(new Vector3(10, 0, 0), new Vector3(30, 0, 0), 5.0), Is.True);
            Assert.That(f.SegmentThroughSphere(new Vector3(10, 0, 10), new Vector3(30, 0, 10), 5.0), Is.False);
            f.Expire(9.0);
            Assert.That(f.InSphere(new Vector3(20, 0, 0)), Is.False);
        }

        [Test]
        public void BlinkNeedsDarknessOrLavaWithAshwalker()
        {
            var ok = new BlinkPlan { valid = true, kind = BlinkKind.Ground };
            Assert.That(BlinkRules.Apply(ok, true, false, false).valid, Is.True);
            Assert.That(BlinkRules.Apply(ok, false, false, false).valid, Is.False);
            Assert.That(BlinkRules.Apply(ok, false, true, false).valid, Is.False);
            Assert.That(BlinkRules.Apply(ok, false, true, true).valid, Is.True);
        }

        [Test]
        public void ShadowStepNeedsShadowAtEitherEnd()
        {
            // the creator, 2026-10-18: the destination OR the starting point in shadow (Nightfall counts for either)
            var ok = new BlinkPlan { valid = true, kind = BlinkKind.Ground };
            Assert.That(BlinkRules.Apply(ok, true, false, false, false).valid, Is.True, "into shadow from the light");
            Assert.That(BlinkRules.Apply(ok, false, true, false, false).valid, Is.True, "out of shadow into the light");
            Assert.That(BlinkRules.Apply(ok, true, true, false, false).valid, Is.True, "shadow to shadow");
            var none = BlinkRules.Apply(ok, false, false, false, false);
            Assert.That(none.valid, Is.False, "light to light");
            Assert.That(none.reason, Is.EqualTo("no shadow at either end"));
            Assert.That(BlinkRules.Apply(ok, false, true, true, false).valid, Is.False, "lava without Ashwalker even from shadow");
            Assert.That(BlinkRules.Apply(ok, false, false, true, true).valid, Is.True, "lava with Ashwalker");
            var geo = new BlinkPlan { valid = false, reason = "out of range" };
            Assert.That(BlinkRules.Apply(geo, true, true, false, false).reason, Is.EqualTo("out of range"), "range and sight still rule");

            // a fire-lit start is not shadow; a Nightfall sphere is, at either end
            var f = new DarknessField();
            f.AddZone(new Vector3(-5, -1, -5), new Vector3(5, 4, 5));
            int l = f.AddLight(new Vector3(3, 0, 3), 1.5f);
            var litStart = new Vector3(3, 0.5f, 3);
            var lightLanding = new Vector3(40, 0.5f, 0);
            Assert.That(BlinkRules.Apply(ok, f.IsDark(lightLanding), f.IsDark(litStart), false, false).valid, Is.False, "a fire-lit start doesn't count");
            Assert.That(BlinkRules.Apply(ok, f.IsDark(lightLanding), f.IsDark(new Vector3(-3, 0.5f, -3)), false, false).valid, Is.True, "a dark start does");
            f.AddSphere(new Vector3(60, 0, 0), 6f, 8.0);
            Assert.That(BlinkRules.Apply(ok, f.IsDark(lightLanding, 1.0), f.IsDark(new Vector3(58, 0.5f, 0), 1.0), false, false).valid, Is.True, "stepping out of Nightfall");
            Assert.That(BlinkRules.Apply(ok, f.IsDark(new Vector3(62, 0.5f, 0), 1.0), f.IsDark(lightLanding, 1.0), false, false).valid, Is.True, "stepping into Nightfall");
            f.RemoveLight(l);
        }

        [Test]
        public void NightfallHurtsAndBlindsWhatItCatches()
        {
            // the creator, 2026-10-18: "Nightfall does no damage"
            var clock = NightfallPayoff.Clock();
            float total = NightfallPayoff.CastBurst, first = 0f;
            for (int i = 0; i < 600 && clock.Burning; i++)
            {
                float due = clock.Tick(1f / 60f);
                if (first == 0f && due > 0f) first = i;
                total += due;
            }
            Assert.That(total, Is.EqualTo(NightfallPayoff.DamageFor(8f)).Within(0.01f));
            Assert.That(total, Is.EqualTo(140f).Within(0.01f), "20 on the cast + 15/s for 8 s");
            Assert.That(first, Is.LessThan(16), "the first tick comes within a quarter second");
            Assert.That(NightfallPayoff.DamageFor(2.7f), Is.GreaterThanOrEqualTo(60f), "an imp's 60 hp chassis goes in under 3 s inside");
            Assert.That(NightfallPayoff.Blind(10f, 11.4f), Is.True, "still blind just after leaving");
            Assert.That(NightfallPayoff.Blind(10f, 11.6f), Is.False, "sees again after 1.5 s");
        }

        [Test]
        public void BurnTicksItsDamageAndEnds()
        {
            var b = new BurnStatus();
            b.Apply(2f, 10f);
            float total = 0f;
            for (int i = 0; i < 300; i++) total += b.Tick(1f / 60f);
            Assert.That(total, Is.EqualTo(20f).Within(0.3f));
            Assert.That(b.Burning, Is.False);
        }

        [Test]
        public void MomentumBankHoldsLongerWhenFast()
        {
            var m = new MomentumBank();
            Assert.That(m.Hold(0f), Is.EqualTo(3f));
            Assert.That(m.Hold(13f), Is.EqualTo(6f));
            Assert.That(m.Hold(8f), Is.InRange(3.5f, 5.5f));
        }

        // ---------------------------------------------------------------- skills

        static readonly HellSkillDef[] Pool =
        {
            new HellSkillDef { id = "a" }, new HellSkillDef { id = "b" }, new HellSkillDef { id = "c" },
            new HellSkillDef { id = "d" }, new HellSkillDef { id = "e" },
        };

        [Test]
        public void OfferIsThreeDistinctSkillsNotOwned()
        {
            var rng = new Random(4);
            for (int k = 0; k < 50; k++)
            {
                var owned = new HashSet<string> { "a", "c" };
                var offer = HellSkillPool.Offer(Pool, owned, 3, rng.Next);
                Assert.That(offer.Count, Is.EqualTo(3));
                var ids = new HashSet<string>();
                foreach (var s in offer) { Assert.That(owned.Contains(s.id), Is.False); Assert.That(ids.Add(s.id), Is.True); }
            }
        }

        [Test]
        public void OfferShrinksWhenThePoolRunsOut()
        {
            var offer = HellSkillPool.Offer(Pool, new HashSet<string> { "a", "b", "c", "d" }, 3, new Random(1).Next);
            Assert.That(offer.Count, Is.EqualTo(1));
            Assert.That(offer[0].id, Is.EqualTo("e"));
        }
    }
}
