using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The game's flow title to title, the run's stats, the fast-path tracker and the Rewind's room boundary
    /// (docs/HELL-FLOW.md).</summary>
    public class HellFlowTests
    {
        static HellFlow NewFlow(List<string> stages = null, List<int> boundaries = null)
        {
            var flow = new HellFlow(() => new HellRun(7));
            if (stages != null) flow.StageChanged += (a, b) => stages.Add(b.ToString());
            if (boundaries != null) flow.RewindBoundary += () => boundaries[0]++;
            return flow;
        }

        /// <summary>Plays floor <paramref name="f"/>'s four rooms to its shaft bottom.</summary>
        static void ClearFloor(HellFlow flow)
        {
            var run = flow.Run;
            for (int r = 0; r < 4; r++)
            {
                Assert.That(run.ReachedDrop(), Is.True);
                if (r < 3) Assert.That(run.Landed(), Is.True);
            }
            flow.EnteredShaft();
            Assert.That(run.ShaftBottom(), Is.True);
        }

        static void PlayToTheLastShaft(HellFlow flow)
        {
            Assert.That(flow.StartGame(), Is.True);
            Assert.That(flow.OpeningDone(), Is.True);
            for (int f = 0; f < 3; f++)
            {
                Assert.That(flow.Stage, Is.EqualTo(HellFlowStage.FloorIntro));
                Assert.That(flow.IntroFloor, Is.EqualTo(f));
                Assert.That(flow.FloorIntroDone(), Is.True);
                Assert.That(flow.Stage, Is.EqualTo(HellFlowStage.Playing));
                ClearFloor(flow);
                if (f < 2)
                {
                    Assert.That(flow.Stage, Is.EqualTo(HellFlowStage.Playing), "the skill pick happens in play");
                    Assert.That(flow.Run.Continue(), Is.True);
                }
            }
        }

        [Test]
        public void TheFlowGoesTitleOpeningFloorsBossEndingCreditsStatsTitle()
        {
            var stages = new List<string>();
            var flow = NewFlow(stages);
            bool bossAsked = false, endingAsked = false;
            flow.BossHook = () => { bossAsked = true; return true; };
            flow.EndingHook = () => { endingAsked = true; return true; };
            Assert.That(flow.Stage, Is.EqualTo(HellFlowStage.Title));
            Assert.That(flow.ShowsScreen, Is.True);
            Assert.That(flow.OpeningDone(), Is.False, "out of order");
            PlayToTheLastShaft(flow);
            Assert.That(bossAsked, Is.True);
            Assert.That(flow.Stage, Is.EqualTo(HellFlowStage.Boss));
            Assert.That(flow.ShowsScreen, Is.False);
            Assert.That(endingAsked, Is.False, "not before the boss falls");
            Assert.That(flow.BossDefeated(), Is.True);
            Assert.That(endingAsked, Is.True);
            Assert.That(flow.Stage, Is.EqualTo(HellFlowStage.Ending));

            // the full path again, every stage in order
            var stages2 = new List<string>();
            var flow2 = NewFlow(stages2);
            flow2.BossHook = () => true;
            flow2.EndingHook = () => true;
            PlayToTheLastShaft(flow2);
            Assert.That(flow2.BossDefeated(), Is.True);
            Assert.That(flow2.Stage, Is.EqualTo(HellFlowStage.Ending));
            Assert.That(flow2.BossDefeated(), Is.False);
            Assert.That(flow2.ReachEnding(HellEndings.SpareKept), Is.True);
            Assert.That(flow2.Stage, Is.EqualTo(HellFlowStage.Credits));
            Assert.That(flow2.Stats.EndingId, Is.EqualTo(HellEndings.SpareKept));
            Assert.That(flow2.StatsDone(), Is.False, "out of order");
            Assert.That(flow2.CreditsDone(), Is.True);
            Assert.That(flow2.Stage, Is.EqualTo(HellFlowStage.Stats));
            Assert.That(flow2.StatsDone(), Is.True);
            Assert.That(flow2.Stage, Is.EqualTo(HellFlowStage.Title));
            Assert.That(flow2.Run, Is.Null);
            Assert.That(stages2, Is.EqualTo(new[]
            {
                "Opening", "Playing", "FloorIntro", "Playing", "FloorIntro", "Playing", "FloorIntro", "Playing",
                "Boss", "Ending", "Credits", "Stats", "Title",
            }));
        }

        [Test]
        public void WithNoBossAndNoEndingTheCreditsRollAtOnce()
        {
            var stages = new List<string>();
            var flow = NewFlow(stages);
            PlayToTheLastShaft(flow);
            Assert.That(flow.Stage, Is.EqualTo(HellFlowStage.Credits));
            Assert.That(flow.Stats.EndingId, Is.EqualTo(HellEndings.Bottom));
            Assert.That(stages.Skip(stages.Count - 3), Is.EqualTo(new[] { "Boss", "Ending", "Credits" }));
        }

        [Test]
        public void ABossWithoutAnEndingSceneGoesToTheCreditsWhenBeaten()
        {
            var flow = NewFlow();
            flow.BossHook = () => true;
            PlayToTheLastShaft(flow);
            Assert.That(flow.Stage, Is.EqualTo(HellFlowStage.Boss));
            Assert.That(flow.BossDefeated(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(HellFlowStage.Credits));
            Assert.That(flow.Stats.EndingId, Is.EqualTo(HellEndings.BossOnly));
        }

        [Test]
        public void HooksMayFinishAtOnceFromInsideTheirCall()
        {
            HellFlow flow = null;
            flow = NewFlow();
            flow.BossHook = () => { flow.BossDefeated(); return true; };
            flow.EndingHook = () => { flow.ReachEnding(HellEndings.Kill); return true; };
            PlayToTheLastShaft(flow);
            Assert.That(flow.Stage, Is.EqualTo(HellFlowStage.Credits));
            Assert.That(flow.Stats.EndingId, Is.EqualTo(HellEndings.Kill));
        }

        [Test]
        public void QuitOnlyFromTheTitleAndStartOnlyOnce()
        {
            var flow = NewFlow();
            Assert.That(flow.StartGame(), Is.True);
            Assert.That(flow.StartGame(), Is.False);
            Assert.That(flow.Quit(), Is.False);
            Assert.That(HellRunStats.Current, Is.SameAs(flow.Stats));
            var flow2 = NewFlow();
            Assert.That(flow2.Quit(), Is.True);
            Assert.That(flow2.Stage, Is.EqualTo(HellFlowStage.Quit));
        }

        [Test]
        public void DeathsCountOnlyInPlayAndTimeGoesToTheFloorBeingPlayed()
        {
            var flow = NewFlow();
            flow.StartGame();
            flow.Tick(5f);
            Assert.That(flow.Died(), Is.False, "the opening");
            flow.OpeningDone();
            flow.Tick(5f);
            Assert.That(flow.Died(), Is.False, "under the floor card");
            flow.FloorIntroDone();
            flow.Tick(10f);
            Assert.That(flow.Died(), Is.True);
            Assert.That(flow.Died(), Is.True);
            ClearFloor(flow);
            flow.Run.Continue();
            flow.FloorIntroDone();
            flow.Tick(4f);
            Assert.That(flow.Stats.Deaths, Is.EqualTo(2));
            Assert.That(flow.Run.Deaths, Is.EqualTo(2));
            Assert.That(flow.Stats.FloorSeconds[0], Is.EqualTo(10f));
            Assert.That(flow.Stats.FloorSeconds[1], Is.EqualTo(4f));
            Assert.That(flow.Stats.TotalSeconds, Is.EqualTo(14f));
        }

        // ------------------------------------------------------------------ the Rewind's room boundary

        [Test]
        public void EveryRoomEntryShaftDeathAndStageIsARewindBoundary()
        {
            var b = new List<int> { 0 };
            var flow = NewFlow(boundaries: b);
            flow.StartGame();                     // Opening
            Assert.That(b[0], Is.EqualTo(1));
            flow.OpeningDone();                   // Playing, room 0:0 entered, FloorIntro
            int afterStart = b[0];
            Assert.That(afterStart, Is.GreaterThanOrEqualTo(3));
            flow.FloorIntroDone();
            int n = b[0];
            flow.Died();
            Assert.That(b[0], Is.EqualTo(n + 1), "a death");
            flow.Run.ReachedDrop();
            flow.Run.Landed();
            Assert.That(b[0], Is.EqualTo(n + 2), "the next room");
            flow.Run.ReachedDrop(); flow.Run.Landed();
            flow.Run.ReachedDrop(); flow.Run.Landed();
            flow.Run.ReachedDrop();
            int beforeShaft = b[0];
            flow.EnteredShaft();
            Assert.That(b[0], Is.EqualTo(beforeShaft + 1), "the shaft");
        }

        /// <summary>The decision (docs/HELL-FLOW.md): the history clears at a boundary, so a Rewind asked for 4 s back just
        /// after a room's start clamps to the room's first recorded frame, never the room before.</summary>
        [Test]
        public void ARewindAfterARoomBoundaryClampsToTheRoomsStart()
        {
            var ring = new SnapshotRing<string>(SnapshotRing<string>.CapacityFor(6.25f, 0.02f));
            var flow = NewFlow();
            flow.RewindBoundary += ring.Clear;
            double t = 0;
            flow.StartGame();
            flow.OpeningDone();
            flow.FloorIntroDone();
            for (int i = 0; i < 300; i++) { ring.Push(t, "room0 @" + i); t += 0.02; }     // 6 s in room 0
            flow.Run.ReachedDrop();
            flow.Run.Landed();                    // room 1: the boundary clears the history
            Assert.That(ring.Count, Is.EqualTo(0));
            for (int i = 0; i < 50; i++) { ring.Push(t, "room1 @" + i); t += 0.02; }      // 1 s in room 1
            var frame = ring[ring.FindAtOrBefore(ring.NewestTime - 4.0)];
            Assert.That(frame, Is.EqualTo("room1 @0"), "clamped to the room's first frame");
        }

        // ------------------------------------------------------------------ the stats

        [Test]
        public void TheStatsCountAndAWorldRewindTakesBackOnlyWhatTheWorldCan()
        {
            var s = new HellRunStats();
            s.RecordKill(); s.RecordKill();
            s.RecordJobOffered(); s.RecordJobOffered();
            s.RecordJobDone();
            var saved = s.SaveWorld();
            s.RecordKill();
            s.RecordJobDone();
            s.RecordRewind();
            s.RecordShadowStep(); s.RecordShadowStep();
            s.RecordDeath();
            s.RecordFastPath(true); s.RecordFastPath(false);
            Assert.That(s.EnemiesKilled, Is.EqualTo(3));
            Assert.That(s.JobsDone, Is.EqualTo(2));
            s.RestoreWorld(saved);
            Assert.That(s.EnemiesKilled, Is.EqualTo(2), "the imp is alive again: its kill is undone");
            Assert.That(s.JobsDone, Is.EqualTo(2), "jobs are settled once by the jobs ledger: never rewound");
            Assert.That(s.JobsOffered, Is.EqualTo(2));
            Assert.That(s.JobsShare, Is.EqualTo(1f));
            s.RecordJobFailed();
            Assert.That(s.JobsFailed, Is.EqualTo(1));
            Assert.That(s.Rewinds, Is.EqualTo(1), "what the player did stays");
            Assert.That(s.ShadowSteps, Is.EqualTo(2));
            Assert.That(s.Deaths, Is.EqualTo(1));
            Assert.That(s.FastPathsTaken, Is.EqualTo(1));
            Assert.That(s.FastPathsOffered, Is.EqualTo(2));
            var reuse = new HellRunStats.World();
            Assert.That(s.SaveWorld(reuse), Is.SameAs(reuse), "snapshots reuse their object");
        }

        [Test]
        public void TheClockReadsMinutesSecondsAndTenths()
        {
            Assert.That(HellRunStats.Clock(0f), Is.EqualTo("0:00.0"));
            Assert.That(HellRunStats.Clock(75.36f), Is.EqualTo("1:15.3"));
            Assert.That(HellRunStats.Clock(3725f), Is.EqualTo("1:02:05"));
            Assert.That(HellRunStats.Clock(float.NaN), Is.EqualTo("0:00.0"));
        }

        // ------------------------------------------------------------------ fast paths

        static IEnumerable<object[]> AllRooms()
        {
            for (int f = 0; f < HellRoomVariants.FloorCount; f++)
                foreach (var k in HellRoomLayouts.Kinds) yield return new object[] { f, k };
        }

        [TestCaseSource(nameof(AllRooms))]
        public void WalkingTheFastPathCountsAsTakenAndTheSafePathDoesNot(int floor, HellRoomKind kind)
        {
            var d = HellWorld.Room(floor, kind);
            var fast = new HellFastPathTracker(d.safePath, d.fastPath);
            Assert.That(fast.OwnSamples, Is.GreaterThan(3), "the fast path has a stretch of its own");
            foreach (var p in HellFastPathTracker.Sample(d.fastPath, 0.5f)) fast.Observe(p);
            Assert.That(fast.Taken, Is.True);
            var safe = new HellFastPathTracker(d.safePath, d.fastPath);
            foreach (var p in HellFastPathTracker.Sample(d.safePath, 0.5f)) safe.Observe(p);
            Assert.That(safe.Taken, Is.False);
            Assert.That(safe.Visited, Is.EqualTo(0));
            fast.Reset();
            Assert.That(fast.Visited, Is.EqualTo(0));
        }

        // ------------------------------------------------------------------ chapters and the opening deck

        [Test]
        public void ChaptersStartAtTheirFloorOrAtTheBoss()
        {
            for (int ch = 0; ch < 3; ch++)
            {
                var stages = new List<string>();
                var flow = NewFlow(stages);
                Assert.That(flow.StartChapter(ch), Is.True);
                Assert.That(flow.Chapter, Is.EqualTo(ch));
                Assert.That(flow.Stage, Is.EqualTo(HellFlowStage.FloorIntro), "no opening");
                Assert.That(flow.IntroFloor, Is.EqualTo(ch));
                Assert.That(flow.Run.Floor, Is.EqualTo(ch));
                Assert.That(stages.Contains("Opening"), Is.False);
            }
            var boss = NewFlow();
            bool asked = false;
            boss.BossHook = () => { asked = true; return true; };
            Assert.That(boss.StartChapter(HellFlow.VesperChapter), Is.True);
            Assert.That(asked, Is.True);
            Assert.That(boss.Stage, Is.EqualTo(HellFlowStage.Boss));
            Assert.That(NewFlow().StartChapter(4), Is.False);
            var started = NewFlow();
            started.StartGame();
            Assert.That(started.StartChapter(1), Is.False, "only from the title");
            Assert.That(started.Chapter, Is.EqualTo(HellFlow.FullGame));
        }

        [Test]
        public void FloorOneIsTheCuratedDeckWithTheLanternAndARandomDeckPinsTheLantern()
        {
            for (ulong seed = 1; seed < 60; seed++)
            {
                var curated = HellDeck.ForRun(seed, true);
                Assert.That(curated[0], Is.EqualTo(HellDeck.Opening));
                Assert.That(curated[1], Is.EqualTo(HellDeck.Deal(seed)[1]), "floors 2 and 3 still come from the seed");
                var random = HellDeck.ForRun(seed, false);
                Assert.That(random[0], Does.Contain(HellRoomKind.SoulLantern));
                Assert.That(random[0].Distinct().Count(), Is.EqualTo(4));
            }
            Assert.That(HellDeck.Opening.Distinct().Count(), Is.EqualTo(4));
            Assert.That(HellDeck.Opening, Does.Contain(HellRoomKind.SoulLantern));
        }

        [Test]
        public void TheFrameCapAndVsyncAreSavedAndUncappedByDefault()
        {
            var o = new SkylineOptions();
            Assert.That(o.TargetFrameRate, Is.EqualTo(-1));
            Assert.That(o.vsync, Is.False);
            o.frameCap = 4;
            Assert.That(o.TargetFrameRate, Is.EqualTo(120));
            var back = SkylineOptions.Deserialize(o.Serialize());
            Assert.That(back.frameCap, Is.EqualTo(4));
            back.vsync = true;
            Assert.That(SkylineOptions.Deserialize(back.Serialize()).vsync, Is.True);
            Assert.That(back.TargetFrameRate, Is.EqualTo(-1), "vsync paces the frames");
            back.frameCap = 99;
            back.Clamp();
            Assert.That(back.frameCap, Is.EqualTo(0));
        }

        // ------------------------------------------------------------------ the words

        [Test]
        public void TheOpeningIsFourToSixMarkedPlaceholderCardsAndEveryFloorHasACard()
        {
            Assert.That(HellFlowText.Opening.Length, Is.InRange(4, 6));
            Assert.That(HellFlowText.Opening.All(c => c.placeholder && c.text.Length > 0 && c.seconds > 1f), Is.True);
            Assert.That(HellFlowText.Floors.Select(f => f.name), Is.EqualTo(HellRoomVariants.Floors.Select(f => f.name.ToUpperInvariant())));
            Assert.That(HellFlowText.Floor(-1), Is.SameAs(HellFlowText.Floors[0]));
            Assert.That(HellFlowText.Floor(9), Is.SameAs(HellFlowText.Floors[2]));
            Assert.That(HellFlowText.Credits.Any(c => c.text.Contains("CC BY")), Is.True);
            foreach (var id in new[] { HellEndings.Kill, HellEndings.SpareKept, HellEndings.SpareCost, HellEndings.BossOnly, HellEndings.Bottom })
                Assert.That(HellEndings.Title(id), Is.Not.Empty);
        }
    }
}
