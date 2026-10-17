using System;
using System.Collections.Generic;
using System.Numerics;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The autopilot's pure planner, its stuck watch, its frame-time stats, the Rewind edge-case checks and the
    /// report (docs/HELL-AUTOPILOT.md).</summary>
    public class HellAutopilotTests
    {
        static IEnumerable<HellRoomDef> AllRooms()
        {
            for (int f = 0; f < HellRoomVariants.FloorCount; f++)
                foreach (var k in HellRoomLayouts.Kinds) yield return HellWorld.Room(f, k);
        }

        static string Name(HellRoomDef r) => r.name + " floor " + (r.floor + 1);

        // ------------------------------------------------------------------ the plan

        [Test]
        public void EveryRoomOnEveryFloorHasAPlanOnBothRoutesThatEndsInTheDropHole()
        {
            foreach (var r in AllRooms())
                foreach (HellAutoRoute route in Enum.GetValues(typeof(HellAutoRoute)))
                {
                    var plan = HellAutopilotPlan.Build(r, route);
                    Assert.That(plan.points.Count, Is.GreaterThan(2), Name(r) + " " + route);
                    var last = plan.points[plan.points.Count - 1];
                    Assert.That(last.task, Is.EqualTo(HellAutoTask.Exit), Name(r) + " " + route);
                    var c = (r.dropZone.min + r.dropZone.max) * 0.5f;
                    Assert.That(HellAutopilotPlan.Horizontal(last.point, c), Is.LessThan(0.01f), Name(r) + " " + route);
                    Assert.That(Vector3.Distance(plan.points[0].point, r.entry), Is.LessThan(0.01f), Name(r) + " " + route + " starts at the entry");
                }
        }

        [Test]
        public void TheSafePlanNeedsNoShadowStepNoWallRunAndNoWideJump()
        {
            foreach (var r in AllRooms())
            {
                var plan = HellAutopilotPlan.Build(r, HellAutoRoute.Safe);
                Assert.That(plan.Count(HellAutoMove.ShadowStep), Is.EqualTo(0), Name(r));
                Assert.That(plan.Count(HellAutoMove.WallRun), Is.EqualTo(0), Name(r));
                foreach (var p in plan.points)
                    if (p.move == HellAutoMove.Jump) Assert.That(p.gap, Is.LessThanOrEqualTo(HellAutopilotPlan.JumpGap), Name(r) + " jump at " + p.point);
                Assert.That(plan.notes, Is.Empty, Name(r));
            }
        }

        [Test]
        public void EveryStopGetsItsTaskOnBothRoutes()
        {
            foreach (var r in AllRooms())
                foreach (HellAutoRoute route in Enum.GetValues(typeof(HellAutoRoute)))
                {
                    var plan = HellAutopilotPlan.Build(r, route);
                    string n = Name(r) + " " + route;
                    switch (r.kind)
                    {
                        case HellRoomKind.SixSigils:
                        case HellRoomKind.SoulLantern:
                            for (int i = 0; i < r.stops.Count; i++) Assert.That(plan.IndexOfTask(HellAutoTask.Stop, i), Is.GreaterThanOrEqualTo(0), n + " stop " + i);
                            for (int i = 1; i < r.stops.Count; i++)
                                Assert.That(plan.IndexOfTask(HellAutoTask.Stop, i), Is.GreaterThan(plan.IndexOfTask(HellAutoTask.Stop, i - 1)), n + " in order");
                            break;
                        case HellRoomKind.BreakChains:
                            for (int i = 0; i < r.stops.Count; i++) Assert.That(plan.IndexOfTask(HellAutoTask.Anchor, i), Is.GreaterThanOrEqualTo(0), n + " anchor " + i);
                            break;
                        case HellRoomKind.OozeWells:
                            for (int i = 0; i < r.stops.Count; i++) Assert.That(plan.IndexOfTask(HellAutoTask.Seal, i), Is.GreaterThanOrEqualTo(0), n + " well " + i);
                            break;
                        case HellRoomKind.HoldBrand:
                            Assert.That(plan.IndexOfTask(route == HellAutoRoute.Fast ? HellAutoTask.Lap : HellAutoTask.Hold, -1), Is.GreaterThanOrEqualTo(0), n);
                            Assert.That(plan.lap.Count, Is.GreaterThan(3), n);
                            break;
                        case HellRoomKind.Collapse:
                            Assert.That(plan.urgent, Is.True, n);
                            break;
                    }
                }
        }

        [Test]
        public void TaskPointsAreWhereTheObjectiveCountsThem()
        {
            foreach (var r in AllRooms())
                foreach (HellAutoRoute route in Enum.GetValues(typeof(HellAutoRoute)))
                {
                    var plan = HellAutopilotPlan.Build(r, route);
                    foreach (var p in plan.points)
                    {
                        string n = Name(r) + " " + route + " " + p.task + " " + p.stop;
                        switch (p.task)
                        {
                            case HellAutoTask.Stop:
                                Assert.That(Vector3.Distance(p.point, r.stops[p.stop].position), Is.LessThanOrEqualTo(r.stops[p.stop].reach + 0.5f), n);
                                break;
                            case HellAutoTask.Anchor:
                                Assert.That(Vector3.Distance(p.point + Vector3.UnitY * HellAutopilotPlan.EyeHeight, r.stops[p.stop].position), Is.LessThanOrEqualTo(r.tuning.wardRadius), n);
                                break;
                            case HellAutoTask.Seal:
                                Assert.That(HellAutopilotPlan.Horizontal(p.point, r.stops[p.stop].position), Is.LessThanOrEqualTo(r.stops[p.stop].reach), n);
                                break;
                            case HellAutoTask.Hold:
                            case HellAutoTask.Lap:
                                Assert.That(HellAutopilotPlan.InRing(r.hold, p.point), Is.True, n);
                                break;
                        }
                    }
                }
        }

        [Test]
        public void ShadowStepsLandInTheDarkWithinReachAndOnlyOnTheFastRoute()
        {
            int steps = 0;
            foreach (var r in AllRooms())
            {
                var plan = HellAutopilotPlan.Build(r, HellAutoRoute.Fast);
                for (int i = 1; i < plan.points.Count; i++)
                {
                    var p = plan.points[i];
                    if (p.move != HellAutoMove.ShadowStep) continue;
                    steps++;
                    Assert.That(HellAutopilotPlan.InDark(r, p.point), Is.True, Name(r) + " step to " + p.point);
                    var from = plan.points[i - 1].point + Vector3.UnitY * HellAutopilotPlan.EyeHeight;
                    Assert.That(Vector3.Distance(from, p.point), Is.LessThanOrEqualTo(HellAutopilotPlan.StepReach), Name(r));
                }
            }
            Assert.That(steps, Is.GreaterThan(0), "some fast route uses the Shadow Step");
        }

        [Test]
        public void TheFastRouteCrossesHazardsTheSafeRouteDoesNot()
        {
            foreach (var r in AllRooms())
            {
                var fast = HellAutopilotPlan.Build(r, HellAutoRoute.Fast);
                int moves = fast.Count(HellAutoMove.Jump) + fast.Count(HellAutoMove.ShadowStep) + fast.Count(HellAutoMove.WallRun) + fast.Count(HellAutoMove.Slide);
                if (r.kind == HellRoomKind.HoldBrand) continue;     // the brand's fast route is the lap (and stepping stones that are ground)
                if (r.kind == HellRoomKind.SoulLantern) continue;   // the lantern's fast route is a bridge of collapsing segments (ground)
                Assert.That(moves, Is.GreaterThan(0), Name(r) + ": the fast plan has a gap, a beam or a wall");
            }
        }

        [Test]
        public void NoRoomHasAGapThePlannerCannotCross()
        {
            foreach (var r in AllRooms())
                foreach (HellAutoRoute route in Enum.GetValues(typeof(HellAutoRoute)))
                    Assert.That(HellAutopilotPlan.Build(r, route).notes, Is.Empty, Name(r) + " " + route);
        }

        [Test]
        public void TheCollapseFastLaneJumpsTheLavaGap()
        {
            var r = HellWorld.Room(0, HellRoomKind.Collapse);
            var plan = HellAutopilotPlan.Build(r, HellAutoRoute.Fast);
            Assert.That(plan.Count(HellAutoMove.Jump), Is.GreaterThanOrEqualTo(1));
            Assert.That(plan.Count(HellAutoMove.ShadowStep), Is.LessThanOrEqualTo(1), "rooms3: the bridge has no 12 m Shadow Step gap");
            var safe = HellAutopilotPlan.Build(r, HellAutoRoute.Safe);
            Assert.That(safe.Count(HellAutoMove.Jump) + safe.Count(HellAutoMove.Slide) + safe.Count(HellAutoMove.ShadowStep), Is.EqualTo(0), "the safe lane is a plain run");
        }

        [Test]
        public void ResumePicksTheNearestPointBetweenTheLastDoneTaskAndTheNextPending()
        {
            var r = HellWorld.Room(0, HellRoomKind.SixSigils);
            var plan = HellAutopilotPlan.Build(r, HellAutoRoute.Safe);
            int a = plan.IndexOfTask(HellAutoTask.Stop, 0), b = plan.IndexOfTask(HellAutoTask.Stop, 1);
            Assert.That(a, Is.GreaterThan(0));
            // nothing done: never past sigil A, even standing on sigil B
            int i = HellAutopilotPlan.ResumeIndex(plan, r.stops[1].position, p => false);
            Assert.That(i, Is.LessThanOrEqualTo(a));
            // A done, standing near B: B's point
            i = HellAutopilotPlan.ResumeIndex(plan, r.stops[1].position, p => p.task == HellAutoTask.Stop && p.stop == 0);
            Assert.That(i, Is.EqualTo(b));
            // A done, standing at the entry: A's point (nearest in [A, B])
            i = HellAutopilotPlan.ResumeIndex(plan, plan.points[a].point, p => p.task == HellAutoTask.Stop && p.stop == 0);
            Assert.That(i, Is.EqualTo(a));
            // all done: somewhere up to the exit
            i = HellAutopilotPlan.ResumeIndex(plan, plan.points[plan.points.Count - 1].point, p => true);
            Assert.That(i, Is.EqualTo(plan.points.Count - 1));
        }

        [Test]
        public void ResumeNeverLandsOnTheFarSideOfAGap()
        {
            foreach (var r in AllRooms())
            {
                var plan = HellAutopilotPlan.Build(r, HellAutoRoute.Fast);
                for (int k = 1; k < plan.points.Count; k++)
                {
                    var p = plan.points[k];
                    if (p.move != HellAutoMove.ShadowStep && p.move != HellAutoMove.WallRun && p.move != HellAutoMove.Jump) continue;
                    int i = HellAutopilotPlan.ResumeIndex(plan, p.point, q => true);
                    Assert.That(plan.points[i].move == HellAutoMove.ShadowStep && i != 0, Is.False, Name(r));
                }
            }
        }

        // ------------------------------------------------------------------ the stuck watch

        [Test]
        public void TheStuckWatchFiresOnceAfterNoMoveAndNoProgress()
        {
            var w = new HellProgressWatch { radius = 1.5f, seconds = 8f };
            var at = new Vector3(1, 0, 1);
            Assert.That(w.Tick(0f, at, 0), Is.False);
            Assert.That(w.Tick(7.9f, at + new Vector3(1f, 0, 0), 0), Is.False, "a small shuffle is not progress, but not yet stuck");
            Assert.That(w.Tick(8.1f, at, 0), Is.True);
            Assert.That(w.Tick(9f, at, 0), Is.False, "once");
            Assert.That(w.Tick(9.5f, at + new Vector3(3f, 0, 0), 0), Is.False, "moved: progress");
            Assert.That(w.Tick(17f, at + new Vector3(3f, 0, 0), 0), Is.False);
            Assert.That(w.Tick(17.6f, at + new Vector3(3f, 0, 0), 0), Is.True, "stuck again 8 s after moving");
            Assert.That(w.Tick(18f, at + new Vector3(3f, 0, 0), 1), Is.False, "progress resets it");
            Assert.That(w.StillFor(20f), Is.EqualTo(2f).Within(1e-4f));
        }

        // ------------------------------------------------------------------ frame times

        [Test]
        public void FrameStatsGiveTheMeanThePercentileAndTheOnePercentLow()
        {
            var s = new HellFrameStats();
            for (int i = 0; i < 990; i++) s.Add(8f);
            for (int i = 0; i < 10; i++) s.Add(20f);
            Assert.That(s.Count, Is.EqualTo(1000));
            Assert.That(s.MeanMs, Is.EqualTo(8.12f).Within(1e-3f));
            Assert.That(s.PercentileMs(0.5f), Is.EqualTo(8.1f).Within(0.11f));
            Assert.That(s.PercentileMs(0.99f), Is.EqualTo(8.1f).Within(0.11f));
            Assert.That(s.PercentileMs(0.995f), Is.EqualTo(20.1f).Within(0.11f));
            Assert.That(s.MaxFrameMs, Is.EqualTo(20f));
            s.Add(9000f);
            Assert.That(s.MaxFrameMs, Is.EqualTo(9000f), "the max keeps the real value");
            Assert.That(s.PercentileMs(1f), Is.EqualTo(HellFrameStats.MaxMs + HellFrameStats.BinMs).Within(0.01f), "the bins clamp");
            s.Clear();
            Assert.That(s.Count, Is.EqualTo(0));
            Assert.That(s.OnePercentLowFps, Is.EqualTo(0f));
        }

        // ------------------------------------------------------------------ the Rewind edge cases

        static HellRewindObservation Good() => new HellRewindObservation
        {
            caseId = "x", rewound = true, expectRewind = true, clockBefore = 10f, clockAfter = 6f, rewindSeconds = 4f,
            progressBefore = 0.5f, progressAfter = 0.25f, maxLiveBotsSeen = 3, liveBotsBefore = 2, liveBotsAfter = 3, timeScaleAfter = 1f,
        };

        [Test]
        public void AConsistentRewindHasNoProblems()
        {
            Assert.That(HellRewindChecks.Problems(Good()), Is.Empty);
            var early = Good();
            early.clockBefore = 1f;
            early.clockAfter = 0f;   // back to the room's start: no older history
            Assert.That(HellRewindChecks.Problems(early), Is.Empty);
        }

        [Test]
        public void TheRewindChecksCatchEachInconsistency()
        {
            var o = Good(); o.progressAfter = 0.75f;
            Assert.That(string.Join("|", HellRewindChecks.Problems(o)), Does.Contain("forward"));
            o = Good(); o.objectiveCarrying = true;
            Assert.That(string.Join("|", HellRewindChecks.Problems(o)), Does.Contain("lantern"));
            o = Good(); o.playerInsideSolid = true;
            Assert.That(string.Join("|", HellRewindChecks.Problems(o)), Does.Contain("inside a solid"));
            o = Good(); o.doubledBots = 1;
            Assert.That(string.Join("|", HellRewindChecks.Problems(o)), Does.Contain("doubled"));
            o = Good(); o.liveBotsAfter = 5;
            Assert.That(string.Join("|", HellRewindChecks.Problems(o)), Does.Contain("more live bots"));
            o = Good(); o.clockAfter = 9.5f;
            Assert.That(string.Join("|", HellRewindChecks.Problems(o)), Does.Contain("only"));
            o = Good(); o.clockAfter = 11f;
            Assert.That(string.Join("|", HellRewindChecks.Problems(o)), Does.Contain("forward"));
            o = Good(); o.aimingAfter = true;
            Assert.That(string.Join("|", HellRewindChecks.Problems(o)), Does.Contain("aiming"));
            o = Good(); o.expectRewind = false;
            Assert.That(string.Join("|", HellRewindChecks.Problems(o)), Does.Contain("should not"));
            o = Good(); o.rewound = false;
            Assert.That(string.Join("|", HellRewindChecks.Problems(o)), Does.Contain("refused"));
            o = Good(); o.timeScaleAfter = 0f;
            Assert.That(string.Join("|", HellRewindChecks.Problems(o)), Does.Contain("time is stopped"));
            o.pickOpenAfter = true;
            Assert.That(string.Join("|", HellRewindChecks.Problems(o)), Does.Not.Contain("time is stopped"));
        }

        // ------------------------------------------------------------------ the report

        [Test]
        public void TheReportCountsRoomsAndWritesTheSummaryLineAndJson()
        {
            var rep = new HellAutoReport { mode = "safe", seed = "7" };
            var a = new HellAutoReport.Room { floor = 0, index = 0, kind = "SixSigils", route = "safe", entered = 1f, left = 90f, objectiveDone = true };
            a.stuck.Add("12.0 s at (1, 0, 2): \"run\"");
            rep.rooms.Add(a);
            rep.rooms.Add(new HellAutoReport.Room { floor = 0, index = 1, kind = "Collapse", route = "safe", entered = 90f, left = 120f, deaths = 2, skipped = true });
            rep.rooms.Add(new HellAutoReport.Room { floor = 0, index = 2, kind = "OozeWells", route = "safe", entered = 120f });
            rep.errorCount = 3;
            rep.result = "timeout";
            Assert.That(rep.RoomsCleared, Is.EqualTo(1));
            Assert.That(rep.StuckCount, Is.EqualTo(1));
            Assert.That(rep.Skipped, Is.EqualTo(1));
            Assert.That(rep.Deaths, Is.EqualTo(2));
            Assert.That(rep.SummaryLine(), Does.StartWith("HELL_AUTOPILOT done rooms=1/3 stuck=1 skipped=1 deaths=2 errors=3"));
            var json = rep.ToJson();
            Assert.That(json, Does.Contain("\"kind\": \"Collapse\""));
            Assert.That(json, Does.Contain("\\\"run\\\""), "quotes escaped");
            Assert.That(json, Does.Contain("\"seconds\": null"), "a room not left has no time");
            Assert.That(json.Split('{').Length, Is.EqualTo(json.Split('}').Length));
        }
    }
}
