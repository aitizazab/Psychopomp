using System.Linq;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>Baja's hour (docs/HELL-HOUR.md): the budget per difficulty, what counts as gameplay time, the chapters' head
    /// start, the Rewind not giving time back, the reminders, the exit gate closing with the time left, and the endings it
    /// adds (damnation, the Lucifer cliffhanger).</summary>
    public class HellHourTests
    {
        static HellHourMoment Play(HellFlowStage s = HellFlowStage.Playing) => HellHourMoment.Play(s);

        // ------------------------------------------------------------------ the budget

        [Test]
        public void TheBudgetFollowsTheDifficulty()
        {
            Assert.That(HellHourRules.BudgetMinutes(HellDifficulty.Easy), Is.EqualTo(90f));
            Assert.That(HellHourRules.BudgetMinutes(HellDifficulty.Normal), Is.EqualTo(65f));
            Assert.That(HellHourRules.BudgetMinutes(HellDifficulty.Hard), Is.EqualTo(50f));
            Assert.That(new HellHour(HellDifficulty.Hard).Left, Is.EqualTo(50f * 60f));
            Assert.That(new SkylineOptions().difficulty, Is.EqualTo((int)HellDifficulty.Normal), "Normal by default");
        }

        [Test]
        public void TheDifficultyIsSavedWithTheOptions()
        {
            var o = new SkylineOptions { difficulty = (int)HellDifficulty.Hard };
            Assert.That(SkylineOptions.Deserialize(o.Serialize()).difficulty, Is.EqualTo((int)HellDifficulty.Hard));
            Assert.That(SkylineOptions.Deserialize("difficulty=7").difficulty, Is.EqualTo((int)HellDifficulty.Normal), "nonsense is Normal");
            Assert.That(SkylineOptions.Deserialize("").difficulty, Is.EqualTo((int)HellDifficulty.Normal));
        }

        [Test]
        public void ChangingTheDifficultyMidRunKeepsTheTimeSpent()
        {
            var h = new HellHour(HellDifficulty.Normal);
            h.Tick(600f, Play());
            h.SetDifficulty(HellDifficulty.Easy);
            Assert.That(h.Spent, Is.EqualTo(600f));
            Assert.That(h.Left, Is.EqualTo(90f * 60f - 600f));
        }

        // ------------------------------------------------------------------ what counts

        [Test]
        public void OnlyGameplayTimeCounts()
        {
            Assert.That(HellHourRules.Counts(Play(HellFlowStage.Playing)), Is.True, "the rooms and shafts");
            Assert.That(HellHourRules.Counts(Play(HellFlowStage.Boss)), Is.True, "the fight");
            Assert.That(HellHourRules.Counts(Play(HellFlowStage.Ending)), Is.True, "the decision and the escape");
            foreach (var s in new[] { HellFlowStage.Title, HellFlowStage.Opening, HellFlowStage.FloorIntro, HellFlowStage.Credits, HellFlowStage.Stats, HellFlowStage.Quit })
                Assert.That(HellHourRules.Counts(Play(s)), Is.False, s.ToString());
            var m = Play();
            m.paused = true; Assert.That(HellHourRules.Counts(m), Is.False, "the skill pick, the ending cards (input paused)");
            m = Play(); m.menu = true; Assert.That(HellHourRules.Counts(m), Is.False, "the pause menu");
            m = Play(); m.screen = true; Assert.That(HellHourRules.Counts(m), Is.False, "a flow screen");
            m = Play(); m.pick = true; Assert.That(HellHourRules.Counts(m), Is.False, "awaiting the pick");
            m = Play(); m.beatHold = true; Assert.That(HellHourRules.Counts(m), Is.False, "a beat's held moment");
            m = Play(HellFlowStage.Ending); m.cutscene = true; Assert.That(HellHourRules.Counts(m), Is.False, "the ending's lines and cards");
        }

        [Test]
        public void TimeDilationDoesNotSlowBajasClock()
        {
            var h = new HellHour(HellDifficulty.Normal);
            var m = Play();
            m.timeScale = 0.35f;
            h.Tick(3f, m);                      // 3 real seconds under dilation (about 1 s of game time)
            Assert.That(h.Spent, Is.EqualTo(3f), "real (unscaled) seconds");
        }

        [Test]
        public void HeldTimeAddsNothing()
        {
            var h = new HellHour(HellDifficulty.Normal);
            var m = Play();
            m.beatHold = true;
            Assert.That(h.Tick(5f, m), Is.False);
            Assert.That(h.Tick(5f, Play(HellFlowStage.Title)), Is.False);
            Assert.That(h.Spent, Is.EqualTo(0f));
            h.Stop();
            Assert.That(h.Tick(5f, Play()), Is.False, "stopped at the exit");
            Assert.That(h.Spent, Is.EqualTo(0f));
        }

        [Test]
        public void TheRewindDoesNotGiveBajasTimeBack()
        {
            // the Rewind restores what registered as rewindable; the hour has no snapshot at all, and the run's stats' world
            // counters (the only rewound stats) don't touch it
            Assert.That(typeof(HellHour).GetMethod("Snapshot"), Is.Null);
            Assert.That(typeof(HellHour).GetMethod("Restore"), Is.Null);
            var h = new HellHour(HellDifficulty.Normal);
            var stats = new HellRunStats();
            h.Tick(10f, Play());
            var snap = stats.SaveWorld();
            h.Tick(4f, Play());
            stats.RestoreWorld(snap);            // a Rewind 4 s back
            Assert.That(h.Spent, Is.EqualTo(14f));
        }

        [Test]
        public void OvertimeGoesOnAndIsOwed()
        {
            var h = new HellHour(HellDifficulty.Hard);
            h.Tick(50f * 60f + 90f, Play());
            Assert.That(h.Overtime, Is.True);
            Assert.That(h.Left, Is.EqualTo(-90f));
            Assert.That(h.LeftShare, Is.EqualTo(0f));
            Assert.That(HellHourLines.StatsLabel(h), Is.EqualTo("TIME OWED"));
            Assert.That(HellHourLines.StatsValue(h), Is.EqualTo("1:30"));
            Assert.That(h.Tick(1f, Play()), Is.True, "the run doesn't end: the hour keeps counting");
        }

        // ------------------------------------------------------------------ chapters

        [Test]
        public void ChaptersStartWithTheHourPartSpent()
        {
            float b = HellHourRules.BudgetSeconds(HellDifficulty.Normal);
            Assert.That(new HellHour(HellDifficulty.Normal, HellFlow.FullGame).Spent, Is.EqualTo(0f));
            Assert.That(new HellHour(HellDifficulty.Normal, 0).Spent, Is.EqualTo(0f));
            Assert.That(new HellHour(HellDifficulty.Normal, 1).Spent, Is.EqualTo(b / 3f).Within(0.01f));
            Assert.That(new HellHour(HellDifficulty.Normal, 2).Spent, Is.EqualTo(b * 2f / 3f).Within(0.01f));
            Assert.That(new HellHour(HellDifficulty.Normal, HellFlow.VesperChapter).Spent, Is.EqualTo(b * 0.85f).Within(0.01f));
            Assert.That(new HellHour(HellDifficulty.Easy, 2).Left, Is.EqualTo(90f * 60f / 3f).Within(0.01f), "scaled by difficulty");
        }

        [Test]
        public void AChapterDoesNotReplayTheRemindersItSkipped()
        {
            var h = new HellHour(HellDifficulty.Normal, 2);           // two thirds spent: the quarter and the half are behind
            Assert.That(h.TakeReminder(), Is.Null);
            h.Tick(h.BudgetSeconds * 0.1f, Play());
            Assert.That(h.TakeReminder(), Is.EqualTo("hour.three-quarters"));
        }

        // ------------------------------------------------------------------ reminders

        [Test]
        public void BajaRemindsYouAtThresholdsOnceEach()
        {
            var h = new HellHour(HellDifficulty.Normal);
            float b = h.BudgetSeconds;
            Assert.That(h.TakeReminder(), Is.Null);
            h.Tick(b * 0.26f, Play());
            Assert.That(h.TakeReminder(), Is.EqualTo("hour.quarter"));
            Assert.That(h.TakeReminder(), Is.Null, "once");
            h.Tick(b * 0.5f, Play());                                  // past the half and the three quarters at once
            Assert.That(h.TakeReminder(), Is.EqualTo("hour.three-quarters"), "only the latest is said");
            h.Tick(b * 0.3f, Play());
            Assert.That(h.TakeReminder(), Is.EqualTo("hour.out"));
            foreach (var k in HellHourRules.ReminderKey) Assert.That(HellHourLines.Get(k), Is.Not.Null, k);
            Assert.That(HellHourLines.All.All(l => l.key.StartsWith("hour.") && l.text.StartsWith("[PLACEHOLDER]")), Is.True);
        }

        // ------------------------------------------------------------------ the gate

        [Test]
        public void TheGateClosesWithTheTimeLeft()
        {
            Assert.That(HellHourGate.Window(0f), Is.EqualTo(HellHourGate.MinWindow), "out of time: nearly shut");
            Assert.That(HellHourGate.Window(-600f), Is.EqualTo(HellHourGate.MinWindow), "overtime: never less");
            Assert.That(HellHourGate.Window(120f), Is.EqualTo(HellHourGate.MinWindow + 6f));
            Assert.That(HellHourGate.Describe(15f * 60f), Is.EqualTo(HellGateLook.WideOpen), "plenty");
            Assert.That(HellHourGate.Describe(3f * 60f), Is.EqualTo(HellGateLook.Closing));
            Assert.That(HellHourGate.Describe(0f), Is.EqualTo(HellGateLook.NearlyShut));
            Assert.That(HellHourGate.Openness(HellHourGate.Window(15f * 60f), 0f), Is.EqualTo(1f));
            Assert.That(HellHourGate.Openness(HellHourGate.Window(0f), 0f), Is.LessThan(0.4f), "drawn nearly shut");
            float w = HellHourGate.Window(60f);
            Assert.That(HellHourGate.Openness(w, w * 0.5f), Is.LessThan(HellHourGate.Openness(w, 0f)), "it grinds shut as you run");
            Assert.That(HellHourGate.Shut(w, w - 0.01f), Is.False);
            Assert.That(HellHourGate.Shut(w, w), Is.True);
            Assert.That(HellHourGate.Shut(HellHourGate.Window(float.PositiveInfinity), 1e6f), Is.False, "no hour: never shuts");
        }

        [Test]
        public void OutOfTimeStillLeavesTheFastLinesAChance()
        {
            // the floor-3 Collapse: fast lane ~11-12 s, safe lane ~16-17 s (docs/HELL-ROOMS.md)
            float w = HellHourGate.Window(0f);
            Assert.That(w, Is.GreaterThan(12f), "the fast lane makes it");
            Assert.That(w, Is.LessThan(16f), "the safe lane doesn't");
            Assert.That(HellHourGate.Window(2f * 60f), Is.GreaterThan(17.5f), "two minutes left: the safe lane makes it");
        }

        // ------------------------------------------------------------------ the endings

        [Test]
        public void TheGateShuttingIsDamnationWhateverYouChose()
        {
            Assert.That(HellHourEndings.Pick(true, 6, 6, null, true, 100f), Is.EqualTo(HellEndings.Damned));
            Assert.That(HellHourEndings.Pick(false, 6, 6, null, true, 100f), Is.EqualTo(HellEndings.Damned));
            Assert.That(HellHourEndings.Pick(false, 0, 0, true, true, 100f), Is.EqualTo(HellEndings.Damned));
            Assert.That(HellHourEndings.Sequence(HellEndings.Damned), Is.EqualTo(new[] { "end-damned-01", "end-damned-02", "end-damned-card" }));
        }

        [Test]
        public void AllJobsAndTimeLeftSendYouAfterLucifer()
        {
            Assert.That(HellHourEndings.Pick(true, 6, 6, null, false, 30f), Is.EqualTo(HellEndings.KillLucifer));
            Assert.That(HellHourEndings.Pick(false, 6, 6, null, false, 30f), Is.EqualTo(HellEndings.SpareLucifer));
            Assert.That(HellHourEndings.Pick(false, 0, 0, true, false, 30f), Is.EqualTo(HellEndings.SpareLucifer), "the Vesper chapter's yes counts as all jobs");
            Assert.That(HellHourEndings.Pick(true, 0, 0, true, false, 30f), Is.EqualTo(HellEndings.KillLucifer));
            // not all jobs
            Assert.That(HellHourEndings.Pick(false, 6, 5, null, false, 30f), Is.EqualTo(HellEndings.SpareKept));
            Assert.That(HellHourEndings.Pick(true, 6, 5, null, false, 30f), Is.EqualTo(HellEndings.Kill));
            Assert.That(HellHourEndings.Pick(false, 0, 0, false, false, 30f), Is.EqualTo(HellEndings.SpareCost));
            Assert.That(HellHourEndings.Pick(true, 0, 0, null, false, 30f), Is.EqualTo(HellEndings.Kill), "no jobs offered is not all jobs");
            // overtime at the exit
            Assert.That(HellHourEndings.Pick(true, 6, 6, null, false, 0f), Is.EqualTo(HellEndings.Kill));
            Assert.That(HellHourEndings.Pick(false, 6, 6, null, false, -40f), Is.EqualTo(HellEndings.SpareKept));
            // no hour running (a test bed)
            Assert.That(HellHourEndings.Pick(true, 3, 3, null, false, float.PositiveInfinity), Is.EqualTo(HellEndings.KillLucifer));
        }

        [Test]
        public void TheLuciferEndingsPlayTheBaseEndingFirstThenTheCliffhanger()
        {
            var k = HellHourEndings.Sequence(HellEndings.KillLucifer);
            Assert.That(k.Take(3), Is.EqualTo(HellEndingRules.Sequence(HellEndings.Kill)));
            Assert.That(k.Skip(3), Is.EqualTo(new[] { "end-lucifer-01", "end-lucifer-02", "end-lucifer-card" }));
            var s = HellHourEndings.Sequence(HellEndings.SpareLucifer);
            Assert.That(s.Take(3), Is.EqualTo(HellEndingRules.Sequence(HellEndings.SpareKept)));
            Assert.That(HellHourEndings.Sequence(HellEndings.SpareCost), Is.EqualTo(HellEndingRules.Sequence(HellEndings.SpareCost)));
            foreach (var id in new[] { HellEndings.Damned, HellEndings.KillLucifer, HellEndings.SpareLucifer })
            {
                Assert.That(HellEndings.Title(id), Is.Not.EqualTo(id.ToUpperInvariant()), id);
                foreach (var key in HellHourEndings.Sequence(id)) Assert.That(HellEndingLines.Get(key), Is.Not.Null, key);
            }
        }
    }
}
