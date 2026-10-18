using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using JamKit.Skyline;
using NUnit.Framework;

namespace JamKit.Tests.Skyline
{
    /// <summary>The Hell capture tools' pure parts (docs/HELL-CAPTURE.md): the command line, the replay file, the camera
    /// shots, the frame-time route and report.</summary>
    public class HellCaptureTests
    {
        // ------------------------------------------------------------------ command line

        [Test]
        public void Args_Defaults_AreAPlainGame()
        {
            var a = HellCapture.Parse(new string[0]);
            Assert.That(a.hud, Is.EqualTo(HellHudLayout.Full));
            Assert.That(a.voices, Is.EqualTo(HellVoiceMode.All));
            Assert.That(a.hudGiven || a.voicesGiven || a.Recording || a.Replaying || a.Perf, Is.False);
            Assert.That(a.fps, Is.EqualTo(120));
            Assert.That(a.width, Is.EqualTo(2560));
            Assert.That(a.height, Is.EqualTo(1440));
            Assert.That(a.problems, Is.Empty);
        }

        [Test]
        public void Args_Clean_HidesHudAndMutesVoices_UnlessGivenApart()
        {
            var a = HellCapture.Parse(new[] { "-hell-clean" });
            Assert.That(a.hud, Is.EqualTo(HellHudLayout.Clean));
            Assert.That(a.voices, Is.EqualTo(HellVoiceMode.NoVoices));
            var b = HellCapture.Parse(new[] { "-hell-clean", "-hell-voices", "only" });
            Assert.That(b.hud, Is.EqualTo(HellHudLayout.Clean));
            Assert.That(b.voices, Is.EqualTo(HellVoiceMode.VoicesOnly));
            var c = HellCapture.Parse(new[] { "-hell-hud", "clean" });
            Assert.That(c.hud, Is.EqualTo(HellHudLayout.Clean));
            Assert.That(c.voicesGiven, Is.False);
        }

        [Test]
        public void Args_BadValues_AreProblems()
        {
            var a = HellCapture.Parse(new[] { "-hell-hud", "half", "-hell-voices", "loud", "-hell-rec-fps", "75", "-hell-capture-res", "123x7",
                "-hell-capture-codec", "gif", "-hell-perf-speed", "0" });
            Assert.That(a.problems.Count, Is.EqualTo(6));
        }

        [Test]
        public void Args_RecordAndReplay_BareNamesGoToTheReplayFolder()
        {
            var a = HellCapture.Parse(new[] { "-hell-rec", "run1", "-hell-rec-fps", "90", "-hellSeed", "42" });
            Assert.That(a.record, Is.EqualTo("out/hell/replays/run1.hellrec"));
            Assert.That(a.recordFps, Is.EqualTo(90));
            Assert.That(a.seed, Is.EqualTo(42));
            var b = HellCapture.Parse(new[] { "-hell-replay", "D:/takes/a.hellrec", "-hell-replay-from", "12.5", "-hell-replay-to", "20", "-hell-shot", "orbit" });
            Assert.That(b.replay, Is.EqualTo("D:/takes/a.hellrec"));
            Assert.That(b.replayFrom, Is.EqualTo(12.5f));
            Assert.That(b.replayTo, Is.EqualTo(20f));
            Assert.That(b.shot, Is.EqualTo("orbit"));
            var c = HellCapture.Parse(new[] { "-hell-rec", "a", "-hell-replay", "b" });
            Assert.That(c.problems.Count, Is.EqualTo(1));
            var d = HellCapture.Parse(new[] { "-hell-replay", "b", "-hell-replay-from", "10", "-hell-replay-to", "5" });
            Assert.That(d.problems.Count, Is.EqualTo(1));
        }

        [Test]
        public void Take_FullForm_Parses()
        {
            var pr = new List<string>();
            var t = HellCapture.ParseTake("run1@12.5-20:orbit-imp:no-voices", "", pr);
            Assert.That(pr, Is.Empty);
            Assert.That(t.replay, Is.EqualTo("run1"));
            Assert.That(t.from, Is.EqualTo(12.5f));
            Assert.That(t.to, Is.EqualTo(20f));
            Assert.That(t.shot, Is.EqualTo("orbit-imp"));
            Assert.That(t.voices, Is.EqualTo(HellVoiceMode.NoVoices));
            Assert.That(t.FileStem, Is.EqualTo("take-run1-125ds-orbit-imp-novoices"));
        }

        [Test]
        public void Take_ShortForms_AndDefaults()
        {
            var pr = new List<string>();
            var t = HellCapture.ParseTake("run1", "follow", pr);
            Assert.That(t.from, Is.EqualTo(0f));
            Assert.That(t.to, Is.EqualTo(0f));
            Assert.That(t.shot, Is.EqualTo("follow"));
            Assert.That(t.FileStem, Is.EqualTo("take-run1-follow"));
            var u = HellCapture.ParseTake("run1@30", "", pr);
            Assert.That(u.from, Is.EqualTo(30f));
            Assert.That(u.to, Is.EqualTo(0f));
            var v = HellCapture.ParseTake("run1::voices-only", "", pr);
            Assert.That(v.voices, Is.EqualTo(HellVoiceMode.VoicesOnly));
            Assert.That(v.FileStem, Is.EqualTo("take-run1-voices"));
            Assert.That(pr, Is.Empty);
        }

        [Test]
        public void Take_Malformed_IsAProblem()
        {
            var pr = new List<string>();
            Assert.That(HellCapture.ParseTake("@3", "", pr), Is.Null);
            Assert.That(HellCapture.ParseTake("a@x", "", pr), Is.Null);
            Assert.That(HellCapture.ParseTake("a@5-2", "", pr), Is.Null);
            Assert.That(HellCapture.ParseTake("a:b:loud", "", pr), Is.Null);
            Assert.That(HellCapture.ParseTake("a:b:all:x", "", pr), Is.Null);
            Assert.That(pr.Count, Is.EqualTo(5));
        }

        [Test]
        public void Takes_CommaList_AndCaptureSwitches()
        {
            var a = HellCapture.Parse(new[] { "-hell-take", "a@1-3,b:orbit", "-hell-shot", "wide", "-hell-capture-fps", "90", "-hell-capture-res", "1080p",
                "-hell-capture-codec", "prores", "-hell-capture-mbps", "120", "-hell-capture-out", "out/x", "-hell-capture-hud", "full" });
            Assert.That(a.problems, Is.Empty);
            Assert.That(a.takes.Count, Is.EqualTo(2));
            Assert.That(a.takes[0].shot, Is.EqualTo("wide"));
            Assert.That(a.takes[1].shot, Is.EqualTo("orbit"));
            Assert.That(a.fps, Is.EqualTo(90));
            Assert.That(a.width, Is.EqualTo(1920));
            Assert.That(a.codec, Is.EqualTo(HellCaptureCodec.ProRes422HQ));
            Assert.That(HellCapture.EffectiveBitrate(a), Is.EqualTo(120f));
            Assert.That(a.outDir, Is.EqualTo("out/x"));
            Assert.That(a.takesKeepHud, Is.True);
        }

        [Test]
        public void Rate_Resolution_Bitrate()
        {
            Assert.That(HellCapture.TryResolution("1440p", out int w, out int h), Is.True);
            Assert.That(w, Is.EqualTo(2560));
            Assert.That(h, Is.EqualTo(1440));
            Assert.That(HellCapture.TryResolution("4k", out w, out h) && w == 3840 && h == 2160, Is.True);
            Assert.That(HellCapture.TryResolution("1001x600", out w, out h), Is.False);
            Assert.That(HellCapture.BitrateMbps(2560, 1440, 120), Is.EqualTo(155f));
            Assert.That(HellCapture.BitrateMbps(640, 360, 60), Is.EqualTo(40f));
            Assert.That(HellCapture.Frames(2.5f, 120), Is.EqualTo(300));
            Assert.That(HellCapture.Step(120), Is.EqualTo(1f / 120f));
        }

        [Test]
        public void TakeClock_IgnoresTimeScale_UsesTheCaptureStep()
        {
            var c = new HellTakeClock();
            for (int i = 0; i < 120; i++) c.Advance(1f / 120f, 0.5f);
            Assert.That(c.Seconds, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(c.Frames, Is.EqualTo(120));
            c.Reset();
            c.Advance(0f, 0.02f);
            Assert.That(c.Seconds, Is.EqualTo(0.02f).Within(1e-6f));
        }

        // ------------------------------------------------------------------ replay file

        static HellReplay Sample(int n)
        {
            var r = new HellReplay { fps = 120, seed = 77, scene = "HellDescent", note = "test" };
            for (int i = 0; i < n; i++)
                r.frames.Add(new HellReplayFrame
                {
                    moveX = i * 0.01f, moveY = -i * 0.02f,
                    buttons = (i % 3 == 0 ? HellReplayButtons.FireHeld : 0) | (i == 5 ? HellReplayButtons.RewindPressed | HellReplayButtons.StepHeld : 0),
                    yaw = i, pitch = -i * 0.5f, x = i, y = 2f, z = -i,
                });
            r.AddEvent(60, "grant", "ambush");
            return r;
        }

        [Test]
        public void Replay_RoundTripsExactly()
        {
            var r = Sample(500);
            var ms = new MemoryStream();
            r.Write(ms);
            ms.Position = 0;
            var back = HellReplay.Read(ms);
            Assert.That(back.fps, Is.EqualTo(120));
            Assert.That(back.seed, Is.EqualTo(77));
            Assert.That(back.scene, Is.EqualTo("HellDescent"));
            Assert.That(back.note, Is.EqualTo("test"));
            Assert.That(back.FrameCount, Is.EqualTo(500));
            for (int i = 0; i < 500; i++)
            {
                Assert.That(back.frames[i].buttons, Is.EqualTo(r.frames[i].buttons));
                Assert.That(back.frames[i].moveX, Is.EqualTo(r.frames[i].moveX));
                Assert.That(back.frames[i].yaw, Is.EqualTo(r.frames[i].yaw));
                Assert.That(back.frames[i].z, Is.EqualTo(r.frames[i].z));
            }
            Assert.That(back.events.Count, Is.EqualTo(1));
            Assert.That(back.events[0].value, Is.EqualTo("ambush"));
            Assert.That(back.frames[5].Has(HellReplayButtons.RewindPressed), Is.True);
            Assert.That(back.frames[4].Has(HellReplayButtons.RewindPressed), Is.False);
        }

        [Test]
        public void Replay_ChapterRun_RoundTripsHowItBegan()
        {
            var r = Sample(10);
            r.chapter = HellFlow.VesperChapter;
            r.difficulty = 2;
            r.errands = 0;
            r.startSkills.Add("ambush");
            r.startSkills.Add("ember");
            var ms = new MemoryStream();
            r.Write(ms);
            ms.Position = 0;
            var back = HellReplay.Read(ms);
            Assert.That(back.chapter, Is.EqualTo(HellFlow.VesperChapter));
            Assert.That(back.difficulty, Is.EqualTo(2));
            Assert.That(back.errands, Is.EqualTo(0));
            Assert.That(back.startSkills, Is.EqualTo(new[] { "ambush", "ember" }));
            Assert.That(back.FrameCount, Is.EqualTo(10));
            Assert.That(back.Describe(), Does.Contain("chapter 3"));
        }

        [Test]
        public void Replay_Version1File_ReadsAsAStartRun()
        {
            var ms = new MemoryStream();
            using (var w = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
            {
                w.Write(HellReplay.Magic); w.Write(1); w.Write(120); w.Write(5); w.Write("HellDescent"); w.Write("old");
                w.Write(0);   // frames
                w.Write(0);   // events
            }
            ms.Position = 0;
            var back = HellReplay.Read(ms);
            Assert.That(back.seed, Is.EqualTo(5));
            Assert.That(back.chapter, Is.EqualTo(HellFlow.FullGame));
            Assert.That(back.difficulty, Is.EqualTo(HellReplay.Unknown));
            Assert.That(back.errands, Is.EqualTo(HellReplay.Unknown));
            Assert.That(back.startSkills, Is.Empty);
        }

        [Test]
        public void Replay_ChaptersStartOnChapterStarted_StartOnRunStarted()
        {
            // START raises ChapterStarted(FullGame) before its opening; its recording starts with RunStarted after it
            Assert.That(HellReplay.StartsOnChapter(HellFlow.FullGame), Is.False);
            for (int c = 0; c < HellFlow.Chapters; c++) Assert.That(HellReplay.StartsOnChapter(c), Is.True);
        }

        [Test]
        public void ChapterStart_ParsesTheCommandLine()
        {
            var none = HellChapterStart.Parse(new[] { "-hell-rec", "a" });
            Assert.That(none.Given, Is.False);
            Assert.That(none.problems, Is.Empty);

            var vesper = HellChapterStart.Parse(new[] { "-hellChapter", "4", "-hellErrands", "no", "-hellDifficulty", "Hard" });
            Assert.That(vesper.chapter, Is.EqualTo(HellFlow.VesperChapter));
            Assert.That(vesper.errands, Is.False);
            Assert.That(vesper.difficulty, Is.EqualTo(HellDifficulty.Hard));

            var floor2 = HellChapterStart.Parse(new[] { "-hellChapter", "2" });
            Assert.That(floor2.chapter, Is.EqualTo(1));
            Assert.That(floor2.errands, Is.True);
            Assert.That(floor2.difficulty, Is.EqualTo(HellDifficulty.Normal));

            var bad = HellChapterStart.Parse(new[] { "-hellChapter", "5", "-hellErrands", "maybe", "-hellDifficulty", "nightmare" });
            Assert.That(bad.Given, Is.False);
            Assert.That(bad.problems.Count, Is.EqualTo(3));
        }

        [Test]
        public void ChapterStart_PicksOnePerSkippedFloor()
        {
            Assert.That(HellChapterStart.PicksBefore(0, 3), Is.EqualTo(0));
            Assert.That(HellChapterStart.PicksBefore(1, 3), Is.EqualTo(1));
            Assert.That(HellChapterStart.PicksBefore(2, 3), Is.EqualTo(2));
            Assert.That(HellChapterStart.PicksBefore(HellFlow.VesperChapter, 3), Is.EqualTo(2));
            Assert.That(HellChapterStart.PicksBefore(HellFlow.FullGame, 3), Is.EqualTo(0));
        }

        [Test]
        public void Replay_RejectsOtherFiles()
        {
            Assert.Throws<InvalidDataException>(() => HellReplay.Read(new MemoryStream(new byte[] { 1, 2, 3, 4, 0, 0, 0, 0 })));
            var ms = new MemoryStream();
            var w = new BinaryWriter(ms);
            w.Write(HellReplay.Magic);
            w.Write(99);
            ms.Position = 0;
            Assert.Throws<InvalidDataException>(() => HellReplay.Read(ms));
        }

        [Test]
        public void Replay_SaveAndLoad_OnDisk()
        {
            string dir = Path.Combine(Path.GetTempPath(), "hellrec-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(dir, "a.hellrec");
            try
            {
                Sample(10).Save(path);
                Sample(20).Save(path);   // overwrites
                Assert.That(HellReplay.Load(path).FrameCount, Is.EqualTo(20));
                Assert.That(File.Exists(path + ".tmp"), Is.False);
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        [Test]
        public void Replay_TimeAndEvents()
        {
            var r = Sample(240);
            Assert.That(r.Seconds, Is.EqualTo(2f));
            Assert.That(r.FrameAt(0.5f), Is.EqualTo(60));
            Assert.That(r.FrameAt(-1f), Is.EqualTo(0));
            Assert.That(r.FrameAt(99f), Is.EqualTo(240));
            Assert.That(r.TimeOf(120), Is.EqualTo(1f));
            var into = new List<HellReplayEvent>();
            r.EventsAt(59, into);
            Assert.That(into, Is.Empty);
            r.EventsAt(60, into);
            Assert.That(into.Count, Is.EqualTo(1));
            Assert.That(into[0].kind, Is.EqualTo("grant"));
        }

        [Test]
        public void Drift_CorrectsOnlyPastTheTolerance()
        {
            var d = new HellReplayDrift { tolerance = 0.25f };
            Assert.That(d.Check(0.1f, 0f, 0.1f), Is.False);
            Assert.That(d.Check(0.3f, 0f, 0f), Is.True);
            Assert.That(d.Worst, Is.EqualTo(0.3f).Within(1e-5f));
            Assert.That(d.Corrections, Is.EqualTo(1));
        }

        // ------------------------------------------------------------------ shots

        static HellShot TwoKeys(HellShotEase ease) => new HellShot
        {
            name = "t",
            spline = false,
            keys = new List<HellShotKey>
            {
                new HellShotKey { t = 0f, position = new[] { 0f, 2f, 0f }, lookAt = new[] { 0f, 2f, 10f }, fov = 40f, ease = ease },
                new HellShotKey { t = 2f, position = new[] { 10f, 2f, 0f }, lookAt = new[] { 10f, 2f, 10f }, fov = 80f, roll = 10f },
            },
        };

        [Test]
        public void Shot_LinearPath_LookAt_Fov_Roll()
        {
            var s = TwoKeys(HellShotEase.Linear);
            var p = HellShots.Sample(s, 1f, Vector3.Zero, 0f, false);
            Assert.That(p.position.X, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(p.yaw, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(p.pitch, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(p.fov, Is.EqualTo(60f).Within(1e-3f));
            Assert.That(p.roll, Is.EqualTo(5f).Within(1e-3f));
            Assert.That(HellShots.Sample(s, -1f, Vector3.Zero, 0f, false).position.X, Is.EqualTo(0f));
            Assert.That(HellShots.Sample(s, 9f, Vector3.Zero, 0f, false).position.X, Is.EqualTo(10f));
            Assert.That(HellShots.Length(s), Is.EqualTo(2f));
        }

        [Test]
        public void Shot_Easing()
        {
            Assert.That(HellShots.Ease(HellShotEase.Smooth, 0.5f), Is.EqualTo(0.5f));
            Assert.That(HellShots.Ease(HellShotEase.In, 0.5f), Is.EqualTo(0.25f));
            Assert.That(HellShots.Ease(HellShotEase.Out, 0.5f), Is.EqualTo(0.75f));
            Assert.That(HellShots.Ease(HellShotEase.Hold, 0.9f), Is.EqualTo(0f));
            var hold = TwoKeys(HellShotEase.Hold);
            Assert.That(HellShots.Sample(hold, 1.9f, Vector3.Zero, 0f, false).position.X, Is.EqualTo(0f));
            Assert.That(HellShots.Sample(hold, 2f, Vector3.Zero, 0f, false).position.X, Is.EqualTo(10f));
        }

        [Test]
        public void Shot_SplinePassesThroughKeys()
        {
            var s = new HellShot
            {
                name = "s",
                keys = new List<HellShotKey>
                {
                    new HellShotKey { t = 0f, position = new[] { 0f, 0f, 0f }, ease = HellShotEase.Linear },
                    new HellShotKey { t = 1f, position = new[] { 5f, 0f, 5f }, ease = HellShotEase.Linear },
                    new HellShotKey { t = 2f, position = new[] { 10f, 0f, 0f }, ease = HellShotEase.Linear },
                },
            };
            var p = HellShots.Sample(s, 1f, Vector3.Zero, 0f, false).position;
            Assert.That(Vector3.Distance(p, new Vector3(5f, 0f, 5f)), Is.LessThan(1e-4f));
            var mid = HellShots.Sample(s, 0.5f, Vector3.Zero, 0f, false).position;
            Assert.That(mid.Z, Is.GreaterThan(2.5f));   // curves outward, not a straight chord
        }

        [Test]
        public void Shot_LensAndFov()
        {
            Assert.That(HellShots.FovOfLens(50f), Is.EqualTo(26.99f).Within(0.01f));
            Assert.That(HellShots.LensOfFov(HellShots.FovOfLens(35f)), Is.EqualTo(35f).Within(1e-3f));
            var s = TwoKeys(HellShotEase.Linear);
            s.keys[0].fov = 0f;
            s.keys[0].lens = 50f;
            Assert.That(HellShots.Sample(s, 0f, Vector3.Zero, 0f, false).fov, Is.EqualTo(26.99f).Within(0.01f));
        }

        [Test]
        public void Shot_TargetSpace_TurnsWithTheTarget_AndLooksAtIt()
        {
            var s = new HellShot
            {
                name = "f",
                space = HellShotSpace.Target,
                follow = HellShotFollow.Player,
                lookAtTarget = true,
                keys = new List<HellShotKey> { new HellShotKey { t = 0f, position = new[] { 0f, 1.4f, -4f } } },
                dof = new HellShotDof { on = true, focusTarget = true },
            };
            var p = HellShots.Sample(s, 0f, new Vector3(10f, 0f, 10f), 90f, true);
            // behind a target facing +X is at -X
            Assert.That(p.position.X, Is.EqualTo(6f).Within(1e-4f));
            Assert.That(p.position.Z, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(p.yaw, Is.EqualTo(90f).Within(1e-3f));
            Assert.That(p.dof, Is.True);
            Assert.That(p.focusDistance, Is.EqualTo(4f).Within(1e-3f));
        }

        [Test]
        public void Shot_YawPitchKeys_TakeTheShortWayRound()
        {
            var s = new HellShot
            {
                name = "y",
                spline = false,
                keys = new List<HellShotKey>
                {
                    new HellShotKey { t = 0f, position = new[] { 0f, 0f, 0f }, yaw = 350f, ease = HellShotEase.Linear },
                    new HellShotKey { t = 1f, position = new[] { 0f, 0f, 0f }, yaw = 10f },
                },
            };
            float y = HellShots.Sample(s, 0.5f, Vector3.Zero, 0f, false).yaw;
            Assert.That(((y % 360f) + 360f) % 360f, Is.EqualTo(0f).Within(1e-3f).Or.EqualTo(360f).Within(1e-3f));
        }

        [Test]
        public void Shake_IsRepeatable_Bounded_AndOffWhenZero()
        {
            var s = TwoKeys(HellShotEase.Linear);
            var p = HellShots.Sample(s, 1f, Vector3.Zero, 0f, false);
            Assert.That(HellShots.Shake(s, p, 1f).yaw, Is.EqualTo(p.yaw));
            s.shake = new HellShotShake { amplitude = 2f, frequency = 1f, seed = 5 };
            var a = HellShots.Shake(s, p, 1.3f);
            var b = HellShots.Shake(s, p, 1.3f);
            Assert.That(a.yaw, Is.EqualTo(b.yaw));
            Assert.That(a.yaw, Is.Not.EqualTo(p.yaw));
            for (float t = 0f; t < 20f; t += 0.37f) Assert.That(Math.Abs(HellShots.Noise(5, 0, t)), Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void Follow_ConvergesWithoutOvershoot()
        {
            var c = Vector3.Zero;
            for (int i = 0; i < 240; i++) c = HellShots.Follow(c, new Vector3(10f, 0f, 0f), 0.25f, 1f / 120f);
            Assert.That(c.X, Is.EqualTo(10f).Within(0.01f));
            Assert.That(HellShots.Follow(Vector3.Zero, Vector3.One, 0f, 0.01f), Is.EqualTo(Vector3.One));
        }

        [Test]
        public void ShotJson_ParsesAndLints()
        {
            var pr = new List<string>();
            var s = HellShots.Parse("orbit", "{\"name\":\"orbit\",\"follow\":\"player\",\"space\":\"target\",\"lookAtTarget\":true," +
                "\"keys\":[{\"t\":0,\"position\":[0,2,-5],\"lens\":35},{\"t\":3,\"position\":[5,2,0],\"ease\":\"out\"}]," +
                "\"shake\":{\"amplitude\":0.5},\"dof\":{\"on\":true}}", pr);
            Assert.That(pr, Is.Empty);
            Assert.That(s.follow, Is.EqualTo(HellShotFollow.Player));
            Assert.That(s.keys[1].ease, Is.EqualTo(HellShotEase.Out));
            var bad = new List<string>();
            HellShots.Parse("x", "{\"name\":\"y\",\"space\":\"target\",\"keys\":[{\"t\":2,\"position\":[0,0,0]},{\"t\":1},{\"t\":3,\"position\":[0,0,0],\"oops\":1}]}", bad);
            // the name mismatch, an unknown key, a missing position, a key out of order, target space with nothing followed
            Assert.That(bad.Count, Is.EqualTo(5));
        }

        [Test]
        public void ShippedShots_LoadWithoutProblems()
        {
            string root = HellShots.FindRoot(TestContext.CurrentContext.TestDirectory);
            if (root == null) Assert.Ignore("no shots folder above the test directory");
            var files = Directory.GetFiles(root, "*.json");
            Assert.That(files.Length, Is.GreaterThan(0));
            foreach (var f in files)
            {
                var pr = new List<string>();
                HellShots.Load(f, root, pr);
                Assert.That(pr, Is.Empty, Path.GetFileName(f) + ": " + string.Join("; ", pr));
            }
        }

        // ------------------------------------------------------------------ frame-time route and report

        [Test]
        public void PerfRoute_FollowsThePathAtEyeHeight()
        {
            var r = HellPerfRoute.Through(new[] { new Vector3(0, 0, 0), new Vector3(0, 0, 10), new Vector3(10, 0, 10) });
            Assert.That(r.Length, Is.EqualTo(20f).Within(1e-4f));
            Assert.That(r.At(5f), Is.EqualTo(new Vector3(0, 1.6f, 5)));
            Assert.That(r.At(15f).X, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(r.At(99f), Is.EqualTo(new Vector3(10, 1.6f, 10)));
            r.Pose(0f, out _, out float yaw, out float pitch);
            Assert.That(yaw, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(pitch, Is.EqualTo(0f).Within(1e-3f));
            r.Pose(19.9f, out _, out yaw, out _);
            Assert.That(yaw, Is.EqualTo(90f).Within(1e-2f));
            Assert.That(r.Seconds(10f), Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void PerfRoute_EveryRoomHasARoute()
        {
            for (int f = 0; f < HellRoomVariants.FloorCount; f++)
                foreach (var k in HellRoomLayouts.Kinds)
                {
                    var route = HellPerfRoute.For(HellWorld.Room(f, k));
                    Assert.That(route.Length, Is.GreaterThan(10f), HellPerfRoute.Id(f, k));
                }
        }

        [Test]
        public void PerfRoute_RoomFilter()
        {
            var w = new List<string> { "2", "SixSigils", "3.Collapse" };
            Assert.That(HellPerfRoute.Matches(w, 1, HellRoomKind.OozeWells), Is.True);
            Assert.That(HellPerfRoute.Matches(w, 0, HellRoomKind.SixSigils), Is.True);
            Assert.That(HellPerfRoute.Matches(w, 2, HellRoomKind.Collapse), Is.True);
            Assert.That(HellPerfRoute.Matches(w, 0, HellRoomKind.Collapse), Is.False);
            Assert.That(HellPerfRoute.Matches(new List<string>(), 0, HellRoomKind.Collapse), Is.True);
            Assert.That(HellPerfRoute.Id(0, HellRoomKind.HoldBrand), Is.EqualTo("1.HoldBrand"));
        }

        [Test]
        public void PerfReport_JudgesThe1PercentLowAgainstTheTarget()
        {
            var fast = new List<float>();
            for (int i = 0; i < 1000; i++) fast.Add(1f / 200f);
            var slow = new List<float>(fast);
            for (int i = 0; i < 20; i++) slow[i] = 0.02f;
            var rep = new HellPerfReport { targetFps = 120 };
            rep.rooms.Add(new HellPerfRoom { id = "1.SixSigils", frames = FrameStats.Summarize(fast), over = HellPerfReport.CountOver(fast, rep.TargetMs) });
            rep.rooms.Add(new HellPerfRoom { id = "1.Collapse", frames = FrameStats.Summarize(slow), over = HellPerfReport.CountOver(slow, rep.TargetMs) });
            rep.all = FrameStats.Summarize(slow);
            Assert.That(HellPerfReport.Meets(rep.rooms[0].frames, rep.TargetMs), Is.True);
            Assert.That(HellPerfReport.Meets(rep.rooms[1].frames, rep.TargetMs), Is.False);
            Assert.That(rep.rooms[1].over, Is.EqualTo(20));
            Assert.That(rep.Worst().id, Is.EqualTo("1.Collapse"));
            string json = rep.ToJson();
            var tree = (Dictionary<string, object>)HellCaptureJson.Parse(json);
            Assert.That(tree["allRoomsMeetTarget"], Is.EqualTo(false));
            Assert.That(tree["worstRoom"], Is.EqualTo("1.Collapse"));
            Assert.That(((List<object>)tree["rooms"]).Count, Is.EqualTo(2));
        }
    }
}
