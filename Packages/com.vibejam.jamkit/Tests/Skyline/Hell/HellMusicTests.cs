using System;
using System.IO;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The music director's pure rules (docs/HELL-MUSIC-DIRECTOR.md): the manifest, the cell with hysteresis, the
    /// bar-line maths, the fades, the modifiers and the stage-to-cue mapping.</summary>
    public class HellMusicTests
    {
        const string Sample = @"{
  ""sample_rate"": 48000,
  ""cues"": {
    ""inrun"": { ""kind"": ""layered_loop"", ""status"": ""ready"", ""bpm"": 135, ""beats_per_bar"": 4, ""bars"": 16, ""loop_samples"": 1365333,
      ""stems"": [
        { ""id"": ""quiet_still_bed"", ""mood"": ""quiet"", ""speed"": ""still"", ""role"": ""bed"", ""resource"": ""a/qsb"" },
        { ""id"": ""normal_fast_drums"", ""mood"": ""normal"", ""speed"": ""fast"", ""role"": ""drums"", ""resource"": ""a/nfd"" },
        { ""id"": ""dark_drone"", ""mood"": ""any"", ""speed"": ""any"", ""role"": ""drone"", ""resource"": ""a/dd"" } ] },
    ""baja_job_done"": { ""kind"": ""stinger"", ""status"": ""pending"", ""resource"": ""b/jd"", ""duck_db"": -6 },
    ""vesper_boss"": { ""kind"": ""parts"", ""status"": ""pending"", ""bpm"": 150, ""beats_per_bar"": 3, ""bars"": 24, ""loop_samples"": null,
      ""parts"": { ""p1_bed"": { ""kind"": ""loop"", ""phase"": 1, ""role"": ""bed"", ""resource"": ""v/p1"" },
                   ""kneel"": { ""kind"": ""stinger"", ""resource"": ""v/k"" } } },
    ""title"": { ""kind"": ""loop"", ""status"": ""ready"", ""resource"": ""t/t"", ""note"": ""a \""quoted\"" é word"" }
  }
}";

        [Test]
        public void Manifest_ParsesCuesStemsAndParts()
        {
            var m = HellMusicManifest.Parse(Sample);
            var inrun = m.Ready("inrun");
            Assert.NotNull(inrun);
            Assert.AreEqual("layered_loop", inrun.kind);
            Assert.AreEqual(3, inrun.stems.Count);
            Assert.AreEqual(1, inrun.stems[1].mood);
            Assert.AreEqual(1, inrun.stems[1].speed);
            Assert.AreEqual(HellMusicRole.Drums, inrun.stems[1].role);
            Assert.AreEqual(HellMusicRole.Drone, inrun.stems[2].role);
            Assert.AreEqual(-1, inrun.stems[2].mood);
            Assert.AreEqual(1.777778, inrun.BarSeconds, 1e-5);
            Assert.AreEqual(28.444438, inrun.LoopSeconds, 1e-5);
            Assert.IsNull(m.Ready("baja_job_done"), "a pending cue is skipped");
            Assert.AreEqual(-6, m.Get("baja_job_done").duckDb, 1e-9);
            var boss = m.Get("vesper_boss");
            Assert.AreEqual(1.2, boss.BarSeconds, 1e-9);
            Assert.AreEqual(28.8, boss.LoopSeconds, 1e-9, "no loop_samples: bars x bar");
            Assert.AreEqual(1, boss.Part("p1_bed").phase);
            Assert.AreEqual("v/k", boss.Part("kneel").resource);
            Assert.IsNull(boss.Part("nope"));
        }

        [Test]
        public void Manifest_BadJsonGivesAnEmptyManifest()
        {
            Assert.AreEqual(0, HellMusicManifest.Parse("{ \"cues\": { ").cues.Count);
            Assert.AreEqual(0, HellMusicManifest.Parse("").cues.Count);
        }

        [Test]
        public void Manifest_TheComposersFileParsesWhenPresent()
        {
            string dir = TestContext.CurrentContext.TestDirectory;
            string path = null;
            for (var d = new DirectoryInfo(dir); d != null; d = d.Parent)
            {
                string p = Path.Combine(d.FullName, "Assets", "JamKit", "Templates", "SkylineRepo", "Resources", "SkylineAudio", "hell-music", "hell-music-manifest.json");
                if (File.Exists(p)) { path = p; break; }
            }
            if (path == null) Assert.Ignore("no manifest in this checkout yet");
            var m = HellMusicManifest.Parse(File.ReadAllText(path));
            var inrun = m.Get("inrun");
            Assert.NotNull(inrun);
            Assert.AreEqual(19, inrun.stems.Count);
            Assert.AreEqual(1365333, inrun.loopSamples);
            Assert.AreEqual(1, inrun.stems.FindAll(s => s.role == HellMusicRole.Drone).Count);
            Assert.AreEqual(9, inrun.stems.FindAll(s => s.role == HellMusicRole.Drums).Count);
            Assert.NotNull(m.Get("vesper_boss").Part("kneel"));
            var fall = m.Get("baja_opening").Part("fall");
            Assert.NotNull(fall, "the opening's hand-over");
            Assert.AreEqual("stinger_into_cue", fall.kind);
            Assert.AreEqual("inrun", fall.intoCue);
            Assert.Greater(fall.downbeatSample, 0);
            if (m.Ready("baja_opening") != null) Assert.AreSame(fall, HellMusicStageRules.OpeningFall(m));
        }

        [Test]
        public void Opening_FallHandsOverOnTheDownbeat()
        {
            const string json = @"{ ""sample_rate"": 48000, ""cues"": {
  ""baja_opening"": { ""kind"": ""parts"", ""status"": ""ready"", ""parts"": {
    ""sister"": { ""kind"": ""loop"", ""resource"": ""o/s"" },
    ""fall"": { ""kind"": ""stinger_into_cue"", ""resource"": ""o/f"", ""into_cue"": ""inrun"", ""downbeat_sample"": 170667 } } } } }";
            var m = HellMusicManifest.Parse(json);
            var fall = HellMusicStageRules.OpeningFall(m);
            Assert.NotNull(fall);
            Assert.AreEqual(170667, fall.downbeatSample);
            Assert.AreEqual(100 + 170667 / 48000.0, HellMusicClock.IntoCueStart(100, fall.downbeatSample, 48000), 1e-9);
            Assert.AreEqual(3.555563, HellMusicClock.IntoCueStart(0, 170667, 48000), 1e-6);
            Assert.IsNull(HellMusicStageRules.OpeningFall(HellMusicManifest.Parse(Sample)), "no opening: no hand-over");
            m.Get("baja_opening").status = "pending";
            Assert.IsNull(HellMusicStageRules.OpeningFall(m), "a pending opening isn't played");
        }

        [Test]
        public void Clock_NextBarLine()
        {
            Assert.AreEqual(12.0, HellMusicClock.NextLine(10, 2, 11.2), 1e-9);
            Assert.AreEqual(14.0, HellMusicClock.NextLine(10, 2, 11.97), 1e-9, "too close to the line: the next one");
            Assert.AreEqual(12.0, HellMusicClock.NextLine(10, 2, 10.0, 2.0), 1e-9, "exactly on a line counts");
            Assert.AreEqual(10.0, HellMusicClock.NextLine(10, 2, 3), 1e-9, "before the clock starts: its start");
            double t = HellMusicClock.NextLine(100, 1.777778, 123.4);
            double bars = (t - 100) / 1.777778;
            Assert.AreEqual(Math.Round(bars), bars, 1e-6, "on a bar line");
            Assert.Greater(t, 123.4 + HellMusicClock.MinLead - 1e-9);
        }

        [Test]
        public void Clock_JoinSampleWrapsTheLoop()
        {
            Assert.AreEqual(0.5, HellMusicClock.LoopPosition(10, 2, 12.5), 1e-9);
            Assert.AreEqual(24000, HellMusicClock.JoinSample(10, 2, 12.5, 48000));
            Assert.AreEqual(0, HellMusicClock.JoinSample(10, 2, 9, 48000));
        }

        [Test]
        public void Fade_EqualPowerAndRetarget()
        {
            Assert.AreEqual(0f, HellMusicFade.Gain(0, 1, 10, 1, 9), 1e-6);
            Assert.AreEqual(0.7071f, HellMusicFade.Gain(0, 1, 10, 1, 10.5), 1e-3);
            Assert.AreEqual(0.7071f, HellMusicFade.Gain(1, 0, 10, 1, 10.5), 1e-3);
            Assert.AreEqual(1f, HellMusicFade.Gain(0, 1, 10, 1, 12), 1e-6);
            var f = HellMusicFade.Hold(0f);
            f.Retarget(1f, 10, 2);
            float mid = f.At(11);
            f.Retarget(0f, 11, 1);
            Assert.AreEqual(mid, f.At(11), 1e-6, "a retarget starts from where it was: no jump");
            Assert.AreEqual(0f, f.At(12.5), 1e-6);
            var g = f; g.Retarget(0f, 50, 1);
            Assert.AreEqual(11.0, g.start, 1e-9, "same target: the fade isn't restarted");
        }

        static HellMusicInputs Calm => new HellMusicInputs { health01 = 1f, hourLeft = -1f };

        [Test]
        public void Cell_MoodRules()
        {
            var r = new HellMusicCellRules();
            var i = Calm;
            Assert.AreEqual(HellMusicMood.Quiet, r.Wanted(i));
            i.enemiesAwake = 1; Assert.AreEqual(HellMusicMood.Normal, r.Wanted(i));
            i = Calm; i.objectiveRunning = true; Assert.AreEqual(HellMusicMood.Normal, r.Wanted(i));
            i.objectiveImminent = true; Assert.AreEqual(HellMusicMood.Danger, r.Wanted(i));
            i = Calm; i.health01 = 0.3f; Assert.AreEqual(HellMusicMood.Danger, r.Wanted(i));
            i = Calm; i.hourLeft = 30f; Assert.AreEqual(HellMusicMood.Danger, r.Wanted(i));
            i = Calm; i.hourLeft = 300f; Assert.AreEqual(HellMusicMood.Quiet, r.Wanted(i));
            i = Calm; i.forceDanger = true; Assert.AreEqual(HellMusicMood.Danger, r.Wanted(i));
        }

        [Test]
        public void Cell_DangerAtOnceAndLeavesAfterTwoCalmBars()
        {
            var r = new HellMusicCellRules();
            var i = Calm; i.health01 = 0.2f;
            r.Step(i, 0.016f, 1.0);
            Assert.AreEqual(HellMusicMood.Danger, r.Mood);
            i = Calm; i.enemiesAwake = 2;
            for (int n = 0; n < 10; n++) r.Step(i, 0.1f, 1.0);    // 1 s calm
            Assert.AreEqual(HellMusicMood.Danger, r.Mood, "one calm bar is not enough");
            i.health01 = 0.2f; r.Step(i, 0.1f, 1.0);               // a flicker of danger resets the calm
            i.health01 = 1f;
            for (int n = 0; n < 15; n++) r.Step(i, 0.1f, 1.0);
            Assert.AreEqual(HellMusicMood.Danger, r.Mood);
            for (int n = 0; n < 6; n++) r.Step(i, 0.1f, 1.0);
            Assert.AreEqual(HellMusicMood.Normal, r.Mood, "two calm bars: back to Normal");
        }

        [Test]
        public void Cell_SpeedHysteresisAndFalling()
        {
            var r = new HellMusicCellRules();
            Assert.AreEqual(HellMusicSpeed.Still, r.SpeedFor(10f, HellMusicSpeed.Still));
            Assert.AreEqual(HellMusicSpeed.Fast, r.SpeedFor(11f, HellMusicSpeed.Still));
            Assert.AreEqual(HellMusicSpeed.Fast, r.SpeedFor(9.5f, HellMusicSpeed.Fast), "in the band: stays");
            Assert.AreEqual(HellMusicSpeed.Still, r.SpeedFor(8.9f, HellMusicSpeed.Fast));
            Assert.AreEqual(HellMusicSpeed.Flying, r.SpeedFor(17f, HellMusicSpeed.Flying));
            Assert.AreEqual(HellMusicSpeed.Fast, r.SpeedFor(15.9f, HellMusicSpeed.Flying));

            var i = Calm; i.speed = 20f;
            r.Step(i, 0.05f, 1.0);
            Assert.AreEqual(HellMusicSpeed.Still, r.Speed, "one frame of a jump doesn't flick it (smoothed)");
            for (int n = 0; n < 60; n++) r.Step(i, 0.05f, 1.0);
            Assert.AreEqual(HellMusicSpeed.Flying, r.Speed);
            i.speed = 0f; i.falling = true;
            for (int n = 0; n < 60; n++) r.Step(i, 0.05f, 1.0);
            Assert.AreEqual(HellMusicSpeed.Flying, r.Speed, "falling counts as flying");
        }

        [Test]
        public void Cell_StemTargetsAreAdditive()
        {
            var m = HellMusicManifest.Parse(Sample).Get("inrun");
            var qsb = m.stems[0]; var nfd = m.stems[1]; var dd = m.stems[2];
            Assert.AreEqual(1f, HellMusicCellRules.StemTarget(qsb, HellMusicMood.Quiet, HellMusicSpeed.Flying));
            Assert.AreEqual(0f, HellMusicCellRules.StemTarget(qsb, HellMusicMood.Normal, HellMusicSpeed.Still));
            Assert.AreEqual(0f, HellMusicCellRules.StemTarget(nfd, HellMusicMood.Normal, HellMusicSpeed.Still));
            Assert.AreEqual(1f, HellMusicCellRules.StemTarget(nfd, HellMusicMood.Normal, HellMusicSpeed.Fast));
            Assert.AreEqual(1f, HellMusicCellRules.StemTarget(nfd, HellMusicMood.Normal, HellMusicSpeed.Flying));
            Assert.AreEqual(0f, HellMusicCellRules.StemTarget(dd, HellMusicMood.Danger, HellMusicSpeed.Flying), "the drone is the Dark modifier's");
            Assert.AreEqual(1.0, HellMusicCellRules.MoodFadeSeconds(HellMusicMood.Quiet, HellMusicMood.Danger));
            Assert.AreEqual(3.0, HellMusicCellRules.MoodFadeSeconds(HellMusicMood.Danger, HellMusicMood.Normal));
            Assert.AreEqual(2.0, HellMusicCellRules.MoodFadeSeconds(HellMusicMood.Normal, HellMusicMood.Quiet));
        }

        [Test]
        public void Mods_DilationDarkAndPause()
        {
            Assert.AreEqual(1f, HellMusicMods.DilationPitch(1f), 1e-6);
            Assert.AreEqual(1f - 0.65f * 0.15f, HellMusicMods.DilationPitch(0.35f), 1e-5);
            Assert.AreEqual(22000f, HellMusicMods.Cutoff(0f, 1f, false), 1f);
            Assert.AreEqual(700f, HellMusicMods.Cutoff(0f, 0.35f, false), 1f);
            Assert.AreEqual(900f, HellMusicMods.Cutoff(1f, 1f, false), 1f, "the Dark modifier's cutoff");
            Assert.AreEqual(700f, HellMusicMods.Cutoff(1f, 0.35f, false), 1f, "the lower cutoff wins");
            Assert.AreEqual(600f, HellMusicMods.Cutoff(0f, 1f, true), 1f, "paused: muffled under Baja's hold loop");
            Assert.AreEqual(0.2f, HellMusicMods.Db(-14f), 0.001f);
            Assert.AreEqual(0f, DarkHush.MusicDrums(1f), 1e-6);
            Assert.AreEqual(0.8f, DarkHush.MusicDrone(1f), 1e-6);
        }

        [Test]
        public void Stage_SceneAndCueMapping()
        {
            Func<HellFlowStage, string, HellMusicScene> s = (st, ph) => HellMusicStageRules.SceneFor(new HellMusicState { stage = st, endingPhase = ph });
            Assert.AreEqual(HellMusicScene.Title, s(HellFlowStage.Title, null));
            Assert.AreEqual(HellMusicScene.InRun, s(HellFlowStage.FloorIntro, null));
            Assert.AreEqual(HellMusicScene.InRun, s(HellFlowStage.Playing, null));
            Assert.AreEqual(HellMusicScene.Boss, s(HellFlowStage.Boss, null));
            Assert.AreEqual(HellMusicScene.Decision, s(HellFlowStage.Ending, "Decision"));
            Assert.AreEqual(HellMusicScene.Escape, s(HellFlowStage.Ending, "Escape"));
            Assert.AreEqual(HellMusicScene.EndingCards, s(HellFlowStage.Ending, "Cards"));
            Assert.AreEqual(HellMusicScene.Credits, s(HellFlowStage.Credits, null));
            Assert.AreEqual(HellMusicScene.Stats, s(HellFlowStage.Stats, null));
            Assert.AreEqual(HellMusicScene.Silence, s(HellFlowStage.Quit, null));

            Assert.AreEqual("inrun", HellMusicStageRules.CueFor(HellMusicScene.Escape, null));
            Assert.AreEqual("ending_kill", HellMusicStageRules.CueFor(HellMusicScene.EndingCards, HellEndings.KillLucifer));
            Assert.AreEqual("ending_spare_kept", HellMusicStageRules.EndingCue(HellEndings.SpareKept));
            Assert.AreEqual("ending_spare_cost", HellMusicStageRules.EndingCue(HellEndings.Damned));
            Assert.AreEqual("floor_card_2", HellMusicStageRules.FloorCard(1));
            Assert.IsNull(HellMusicStageRules.FloorCard(3));
            Assert.AreEqual("p2_drums", HellMusicStageRules.BossDrums(2));
            Assert.AreEqual("p3_bed", HellMusicStageRules.BossBed(3));
        }

        [Test]
        public void Stage_FallbacksSkipPendingCues()
        {
            var m = HellMusicManifest.Parse(Sample);
            Assert.AreEqual("inrun", HellMusicStageRules.Resolve(m, "vesper_boss"), "the boss falls back to the in-run set");
            Assert.AreEqual("title", HellMusicStageRules.Resolve(m, "ending_kill"), "ending -> stats -> title");
            Assert.AreEqual("title", HellMusicStageRules.Resolve(m, "credits"));
            Assert.IsNull(HellMusicStageRules.Resolve(m, "vesper_decision"));
            Assert.IsNull(HellMusicStageRules.Resolve(null, "title"));
        }
    }
}
