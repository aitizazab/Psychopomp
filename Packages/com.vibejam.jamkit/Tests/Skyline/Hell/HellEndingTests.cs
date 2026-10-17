using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The ending (docs/HELL-ENDING.md): the decision (kill, spare, the lowered gun, the hold ring and its cancel, the
    /// choice is final), the ending picked from the jobs, the lines, and the escape (its fronts, your sister's soul, Vesper
    /// keeping pace).</summary>
    public class HellEndingTests
    {
        const float Dt = 0.02f;
        readonly List<HellDecisionEvent> _ev = new List<HellDecisionEvent>();

        [SetUp]
        public void Clear() => _ev.Clear();

        static HellDecisionInput In(float distance = 2f, bool use = false, bool holster = false, bool fired = false, bool hit = false) =>
            new HellDecisionInput { dt = Dt, distance = distance, useHeld = use, holsterHeld = holster, fired = fired, hitHer = hit };

        HellDecision Begun()
        {
            var d = new HellDecision();
            d.Begin(_ev);
            return d;
        }

        void Run(HellDecision d, float seconds, HellDecisionInput i)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += Dt) d.Tick(i, _ev);
        }

        bool Saw(HellDecisionEventKind k) => _ev.Any(e => e.kind == k);

        // ------------------------------------------------------------------ the decision

        [Test]
        public void SheSpeaksWhenSheKneels()
        {
            Begun();
            Assert.That(_ev.Any(e => e.kind == HellDecisionEventKind.Line && e.line == "dec-01"), Is.True);
        }

        [Test]
        public void AnyHitOnHerKills()
        {
            var d = Begun();
            d.Tick(In(distance: 20f, hit: true), _ev);
            Assert.That(d.State, Is.EqualTo(HellDecisionState.Killed));
            Assert.That(Saw(HellDecisionEventKind.Killed), Is.True);
        }

        [Test]
        public void AHitWhileOfferingYourHandStillKills()
        {
            var d = Begun();
            Run(d, 2f, In());                              // gun lowered
            Run(d, 1f, In(use: true));                     // the ring part full
            Assert.That(d.Progress, Is.GreaterThan(0.3f));
            d.Tick(In(use: true, hit: true), _ev);
            Assert.That(d.State, Is.EqualTo(HellDecisionState.Killed));
        }

        [Test]
        public void SparingNeedsTheGunLoweredReachAndTheFullHold()
        {
            var d = Begun();
            // straight away the gun is still up: holding Use does nothing
            Run(d, 0.5f, In(use: true));
            Assert.That(d.Lowered, Is.False);
            Assert.That(d.Progress, Is.EqualTo(0f));
            Assert.That(d.Prompt, Is.EqualTo(HellEndingLines.PromptLower));
            // not firing lowers the gun
            Run(d, 1.2f, In());
            Assert.That(d.Lowered, Is.True);
            Assert.That(Saw(HellDecisionEventKind.Lowered), Is.True);
            // out of reach: no progress
            Run(d, 1f, In(distance: 6f, use: true));
            Assert.That(d.Progress, Is.EqualTo(0f));
            Assert.That(d.Prompt, Is.EqualTo(HellEndingLines.PromptApproach));
            // in reach, held: the ring fills over holdSeconds, and only then she is spared
            Assert.That(d.Prompt, Is.Not.EqualTo(HellEndingLines.PromptOffer));
            d.Tick(In(), _ev);
            Assert.That(d.Prompt, Is.EqualTo(HellEndingLines.PromptOffer));
            Run(d, d.T.holdSeconds - 0.1f, In(use: true));
            Assert.That(d.State, Is.EqualTo(HellDecisionState.Waiting));
            Assert.That(Saw(HellDecisionEventKind.HoldStarted), Is.True);
            Run(d, 0.2f, In(use: true));
            Assert.That(d.State, Is.EqualTo(HellDecisionState.Spared));
            Assert.That(Saw(HellDecisionEventKind.Spared), Is.True);
        }

        [Test]
        public void HoldingTheHolsterInputLowersTheGunAtOnce()
        {
            var d = Begun();
            d.Tick(In(fired: true), _ev);
            Run(d, 0.4f, In(holster: true));
            Assert.That(d.Lowered, Is.True);
            Run(d, 0.5f, In(holster: true, use: true));
            Assert.That(d.Progress, Is.GreaterThan(0.2f));
        }

        [Test]
        public void LettingGoDrainsTheRingAndCancels()
        {
            var d = Begun();
            Run(d, 2f, In());
            Run(d, 0.8f, In(use: true));
            float p = d.Progress;
            Assert.That(p, Is.InRange(0.4f, 0.6f));
            _ev.Clear();
            d.Tick(In(), _ev);
            Assert.That(Saw(HellDecisionEventKind.HoldCancelled), Is.True);
            Assert.That(d.Progress, Is.LessThan(p));
            Run(d, 1f, In());
            Assert.That(d.Progress, Is.EqualTo(0f));
            Assert.That(d.State, Is.EqualTo(HellDecisionState.Waiting));
        }

        [Test]
        public void WalkingAwayCancelsTheHold()
        {
            var d = Begun();
            Run(d, 2f, In());
            Run(d, 0.6f, In(use: true));
            _ev.Clear();
            d.Tick(In(distance: 5f, use: true), _ev);
            Assert.That(Saw(HellDecisionEventKind.HoldCancelled), Is.True);
            Assert.That(d.Holding, Is.False);
        }

        [Test]
        public void AMissedShotEmptiesTheRingAndRaisesTheGun()
        {
            var d = Begun();
            Run(d, 2f, In());
            Run(d, 1f, In(use: true));
            _ev.Clear();
            d.Tick(In(use: true, fired: true), _ev);
            Assert.That(d.Progress, Is.EqualTo(0f));
            Assert.That(d.Lowered, Is.False);
            Assert.That(Saw(HellDecisionEventKind.HoldCancelled), Is.True);
            Assert.That(Saw(HellDecisionEventKind.Raised), Is.True);
            Assert.That(d.State, Is.EqualTo(HellDecisionState.Waiting), "a miss is not a choice");
        }

        [Test]
        public void TheChoiceIsFinal()
        {
            var d = Begun();
            Run(d, 2f, In());
            Run(d, 2f, In(use: true));
            Assert.That(d.State, Is.EqualTo(HellDecisionState.Spared));
            Assert.That(d.Kill(_ev), Is.False);
            d.Tick(In(hit: true), _ev);
            Assert.That(d.State, Is.EqualTo(HellDecisionState.Spared));

            var k = Begun();
            k.Tick(In(hit: true), _ev);
            Run(k, 5f, In(use: true));
            Assert.That(k.State, Is.EqualTo(HellDecisionState.Killed));
        }

        [Test]
        public void NoTimerOnlyTheShakingBuildsAndTheGodPresses()
        {
            var d = Begun();
            float q0 = d.Quake;
            Run(d, 120f, In(distance: 10f));
            Assert.That(d.State, Is.EqualTo(HellDecisionState.Waiting), "standing there never decides for you");
            Assert.That(d.Quake, Is.EqualTo(1f));
            Assert.That(q0, Is.LessThan(0.5f));
            var lines = _ev.Where(e => e.kind == HellDecisionEventKind.Line).Select(e => e.line).ToList();
            Assert.That(lines, Is.EqualTo(new[] { "dec-01", "dec-god-01", "dec-god-02" }));
        }

        // ------------------------------------------------------------------ the ending from the jobs

        [Test]
        public void TheEndingFollowsTheChoiceAndTheJobs()
        {
            Assert.That(HellEndingRules.Pick(true, 6, 6), Is.EqualTo(HellEndings.Kill));
            Assert.That(HellEndingRules.Pick(true, 0, 0), Is.EqualTo(HellEndings.Kill));
            Assert.That(HellEndingRules.Pick(false, 6, 4), Is.EqualTo(HellEndings.SpareKept));
            Assert.That(HellEndingRules.Pick(false, 6, 3), Is.EqualTo(HellEndings.SpareCost));
            Assert.That(HellEndingRules.Pick(false, 3, 2), Is.EqualTo(HellEndings.SpareKept));
            Assert.That(HellEndingRules.Pick(false, 3, 1), Is.EqualTo(HellEndings.SpareCost));
            Assert.That(HellEndingRules.Pick(false, 1, 1), Is.EqualTo(HellEndings.SpareKept), "all of one");
            Assert.That(HellEndingRules.Pick(false, 0, 0), Is.EqualTo(HellEndings.SpareCost), "no jobs (the Vesper chapter): no favour");
        }

        [Test]
        public void TheVesperChaptersAnswerStandsInForTheJobs()
        {
            Assert.That(HellEndingRules.Pick(false, 0, 0, true), Is.EqualTo(HellEndings.SpareKept), "yes, I did her favours");
            Assert.That(HellEndingRules.Pick(false, 0, 0, false), Is.EqualTo(HellEndings.SpareCost), "no, straight for my sister");
            Assert.That(HellEndingRules.Pick(true, 0, 0, true), Is.EqualTo(HellEndings.Kill));
            Assert.That(HellEndingRules.Pick(true, 0, 0, false), Is.EqualTo(HellEndings.Kill));
            Assert.That(HellEndingRules.Pick(false, 6, 0, true), Is.EqualTo(HellEndings.SpareKept), "the answer wins over the counts");
            Assert.That(HellEndingRules.Pick(false, 6, 6, false), Is.EqualTo(HellEndings.SpareCost), "the answer wins over the counts");
            for (int offered = 0; offered <= 6; offered++)
                for (int done = 0; done <= offered; done++)
                    Assert.That(HellEndingRules.Pick(false, offered, done, null), Is.EqualTo(HellEndingRules.Pick(false, offered, done)), "no answer: the counts");
            Assert.That(HellFlowText.ErrandsPlaceholder, Is.True, "the question's words are the creator's to rewrite");
        }

        [Test]
        public void TheEndingAgreesWithTheJobLedger()
        {
            for (int offered = 0; offered <= 6; offered++)
                for (int done = 0; done <= offered; done++)
                {
                    var l = new HellJobLedger();
                    for (int i = 0; i < offered; i++)
                    {
                        if (i < done) l.MarkDone("j" + i);
                        else l.MarkFailed("j" + i);
                    }
                    Assert.That(HellEndingRules.Pick(false, offered, done) == HellEndings.SpareKept, Is.EqualTo(l.Favoured), offered + "/" + done);
                }
        }

        [Test]
        public void EveryLineTheEndingSaysExistsAndEveryVoicedLineHasItsKey()
        {
            var keys = new List<string> { "dec-01" };
            keys.AddRange(new HellDecisionTuning().nagLines);
            keys.AddRange(HellEndingRules.AfterChoice(true));
            keys.AddRange(HellEndingRules.AfterChoice(false));
            keys.Add(HellEndingRules.EscapeStart(true));
            keys.Add(HellEndingRules.EscapeStart(false));
            keys.Add(HellEndingRules.EscapeHalfway(false));
            foreach (var id in new[] { HellEndings.Kill, HellEndings.SpareKept, HellEndings.SpareCost })
            {
                var seq = HellEndingRules.Sequence(id);
                Assert.That(seq.Length, Is.EqualTo(3), id);
                Assert.That(HellEndingLines.Get(seq[2]).speaker, Is.EqualTo(HellEndingLines.Card), id + " ends on its card");
                keys.AddRange(seq);
            }
            foreach (var k in keys)
            {
                var line = HellEndingLines.Get(k);
                Assert.That(line, Is.Not.Null, k);
                Assert.That(line.text.Length, Is.GreaterThan(0), k);
            }
            Assert.That(HellEndingLines.All.Select(l => l.key).Distinct().Count(), Is.EqualTo(HellEndingLines.All.Length));
            Assert.That(HellEndingLines.All.Where(l => !l.Voiced).All(l => l.speaker == HellEndingLines.You || l.speaker == HellEndingLines.Card), Is.True);
        }

        [Test]
        public void EveryEndingCueHasARow()
        {
            foreach (var cue in HellEndingCues.All)
            {
                var row = HellSoundTable.Find(cue);
                Assert.That(row, Is.Not.Null, cue);
                Assert.That(row.File, Is.Not.Empty, cue);
            }
        }

        [Test]
        public void EveryEndingHasADraftTitle()
        {
            foreach (var id in new[] { HellEndings.Kill, HellEndings.SpareKept, HellEndings.SpareCost })
                Assert.That(HellEndings.Title(id), Is.Not.EqualTo(id.ToUpperInvariant()));
        }

        // ------------------------------------------------------------------ the escape: the fronts

        static HellRoomDef Escape() => HellEscapeRoute.Room();

        static float Along(HellRoomDef d, Vector3 p) => Vector3.Dot(p - d.entry, HellEscapeRoute.Forward(d));

        [Test]
        public void TheEscapeIsFloorThreesCollapseWithItsFronts()
        {
            var d = Escape();
            Assert.That(d.kind, Is.EqualTo(HellRoomKind.Collapse));
            Assert.That(d.floor, Is.EqualTo(2));
            var tiles = d.hazards.Where(h => h.Timeline).ToList();
            Assert.That(tiles.Count, Is.GreaterThan(25));
            // in each lane the floor falls in order from the entry: a tile farther along never goes before a nearer one
            foreach (var prefix in new[] { "tile-fast", "tile-safe" })
            {
                var lane = tiles.Where(h => h.id.StartsWith(prefix, StringComparison.Ordinal))
                    .OrderBy(h => Math.Min(Along(d, h.box.min), Along(d, h.box.max))).ToList();
                Assert.That(lane.Count, Is.GreaterThan(10), prefix);
                for (int i = 1; i < lane.Count; i++)
                    Assert.That(lane[i].timelineSeconds, Is.GreaterThanOrEqualTo(lane[i - 1].timelineSeconds - 1e-4f), prefix + " " + lane[i].id);
            }
            // the fast lane's front comes sooner than the safe lane's at the same distance (the risk is the speed)
            float fastFirst = tiles.Where(h => h.id.StartsWith("tile-fast", StringComparison.Ordinal)).Min(h => h.timelineSeconds);
            float safeFirst = tiles.Where(h => h.id.StartsWith("tile-safe", StringComparison.Ordinal)).Min(h => h.timelineSeconds);
            Assert.That(fastFirst, Is.LessThan(safeFirst));
            Assert.That(d.debris.Count, Is.GreaterThan(0), "debris falls in the escape too");
        }

        static HellHazard TileUnder(HellRoomDef d, Vector3 p) =>
            d.hazards.FirstOrDefault(h => h.Timeline && p.X >= h.box.min.X && p.X <= h.box.max.X && p.Z >= h.box.min.Z && p.Z <= h.box.max.Z);

        [Test]
        public void YourSistersSoulHoversOnEachLanesRunningLine()
        {
            var d = Escape();
            var souls = HellEscapeRoute.SoulPoints(d);
            Assert.That(souls.Length, Is.EqualTo(2));
            string[] lanes = { "tile-fast", "tile-safe" };
            for (int i = 0; i < 2; i++)
            {
                var tile = TileUnder(d, souls[i]);
                Assert.That(tile, Is.Not.Null, "soul " + i + " is over its lane's floor, not a lava gap");
                Assert.That(tile.id.StartsWith(lanes[i], StringComparison.Ordinal), Is.True, tile.id);
                // a runner keeping the lane's front pace reaches the soul before its tile drops (the shake is the margin)
                float lead = Along(d, souls[i]) - Math.Min(Along(d, tile.box.min), Along(d, tile.box.max));
                float speed = (i == 0 ? HellRoomLayouts.CollapseFastLaneSpeed : HellRoomLayouts.CollapseSafeLaneSpeed) * HellRoomVariants.Floors[2].collapseSpeedScale;
                Assert.That(lead / speed, Is.LessThan(d.collapseShakeSeconds), "soul " + i);
                // in the middle stretch of the run, not at either end
                float share = Along(d, souls[i]) / Along(d, d.exit);
                Assert.That(share, Is.InRange(0.3f, 0.7f));
                // not inside a fin
                foreach (var s in d.solids)
                    if (s.role == "wall")
                        Assert.That(souls[i].X >= s.box.min.X && souls[i].X <= s.box.max.X && souls[i].Z >= s.box.min.Z && souls[i].Z <= s.box.max.Z, Is.False, s.id);
            }
        }

        [Test]
        public void TheSoulIsTakenOnTheWayEvenAtSpeed()
        {
            var d = Escape();
            var pick = new HellSoulPickup(HellEscapeRoute.SoulPoints(d));
            var f = HellEscapeRoute.Forward(d);
            var soul = pick.Points[0];
            // a Shadow Step straight through it (12 m in one step) takes it
            Assert.That(pick.Tick(soul - f * 6f, soul + f * 6f), Is.True);
            Assert.That(pick.Collected, Is.True);
            Assert.That(pick.Tick(soul - f, soul), Is.False, "once");
            pick.Reset();
            // passing 5 m to the side does not
            var side = new Vector3(-f.Z, 0f, f.X) * 5f;
            Assert.That(pick.Tick(soul + side - f * 3f, soul + side + f * 3f), Is.False);
            // either lane's soul counts
            Assert.That(pick.Tick(pick.Points[1] - f, pick.Points[1] + f), Is.True);
        }

        [Test]
        public void WalkingTheFastPathOrTheSafePathTakesTheSoul()
        {
            var d = Escape();
            foreach (var path in new[] { d.fastPath, d.safePath })
            {
                var pick = new HellSoulPickup(HellEscapeRoute.SoulPoints(d));
                float len = HellEscapeRoute.Length(path);
                var last = path[0];
                for (float s = 0.5f; s <= len; s += 0.5f)
                {
                    var p = HellEscapeRoute.PointAlong(path, s / len);
                    pick.Tick(last, p);
                    last = p;
                }
                Assert.That(pick.Collected, Is.True);
            }
        }

        // ------------------------------------------------------------------ the escape: Vesper beside you

        [Test]
        public void VesperKeepsPaceAlongTheFastLaneAndStepsAhead()
        {
            var d = Escape();
            var path = d.fastPath;
            float len = HellEscapeRoute.Length(path);
            const float speed = 11.5f;          // a slide-and-hop run, faster than floor 3's fast front (11.1)
            var f = new HellFollower(path[0] + new Vector3(0, 0, 0), HellEscapeRoute.Forward(d));
            var prev = path[0];
            int blinks = 0, aheadAfterBlink = 0;
            float worst = 0f;
            for (float t = Dt; t * speed <= len; t += Dt)
            {
                var p = HellEscapeRoute.PointAlong(path, t * speed / len);
                var v = (p - prev) / Dt;
                prev = p;
                if (f.Tick(Dt, p, v, out _))
                {
                    blinks++;
                    if (Vector3.Dot(f.S.position - p, Vector3.Normalize(new Vector3(v.X, 0, v.Z))) > 2f) aheadAfterBlink++;
                }
                if (t > 0.5f) worst = Math.Max(worst, Vector3.Distance(f.S.position, f.Place(p, f.S.facing)));
            }
            Assert.That(worst, Is.LessThanOrEqualTo(f.T.catchUp + 0.01f), "she never falls farther behind than the catch-up");
            Assert.That(blinks, Is.GreaterThanOrEqualTo(1), "she Shadow Steps ahead now and then");
            Assert.That(aheadAfterBlink, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void VesperCatchesUpWithAShadowStepAndSettlesWhenYouStop()
        {
            var f = new HellFollower(Vector3.Zero, Vector3.UnitZ);
            var player = new Vector3(0, 0, 2);
            for (int i = 0; i < 100; i++) Assert.That(f.Tick(Dt, player, Vector3.Zero, out _), Is.False, "no blinks while you stand");
            Assert.That(Vector3.Distance(f.S.position, f.Place(player, f.S.facing)), Is.LessThan(0.05f));
            // you Shadow Step 14 m on
            player += new Vector3(0, 0, 14);
            Assert.That(f.Tick(Dt, player, new Vector3(0, 0, 10), out var from), Is.True);
            Assert.That(from.Z, Is.LessThan(1.5f), "she left from her place beside where you stood");
            Assert.That(Vector3.Distance(f.S.position, player), Is.LessThan(3f));
        }

        [Test]
        public void TheFollowersStateCopiesForTheRewind()
        {
            var f = new HellFollower(Vector3.Zero, Vector3.UnitZ);
            for (int i = 0; i < 50; i++) f.Tick(Dt, new Vector3(0, 0, i * 0.2f), new Vector3(0, 0, 10), out _);
            var saved = f.S;
            for (int i = 0; i < 400; i++) f.Tick(Dt, new Vector3(0, 0, 10 + i * 0.2f), new Vector3(0, 0, 10), out _);
            f.S = saved;
            Assert.That(f.S.position, Is.EqualTo(saved.position));
            Assert.That(f.S.sinceBlink, Is.EqualTo(saved.sinceBlink));
        }
    }
}
