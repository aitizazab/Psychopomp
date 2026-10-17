#if JAMKIT_RECORDER
using System;
using System.IO;
using System.Text;
using JamKit.Skyline;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JamKit.SkylineRepo.EditorTools.Recording
{
    /// <summary>
    /// The Hell trailer capture (docs/HELL-CAPTURE.md), ported from the Skyline trailer capture (VibeJamKit-capture,
    /// SkylineTrailerCapture) with only what Hell needs: a graphics editor session (not batch mode: the Recorder needs the
    /// Game view) that records each take named on the command line, each a stretch of a replay through a camera shot, to its
    /// own file at a fixed rate (120 fps 1440p by default), then exits.
    /// <c>Unity.exe -projectPath . -executeMethod JamKit.SkylineRepo.EditorTools.Recording.HellTrailerCapture.RunFromCommandLine
    /// -hell-take run1@12-20:enemy-reveal,run1@12-20 -hell-capture-out out/hell/trailer/&lt;stamp&gt; [-hell-capture-fps 120]
    /// [-hell-capture-res 1440p] [-hell-capture-codec h264|prores|prores4444] [-hell-capture-mbps N] [-hell-capture-hud full]</c>
    /// <para>The Skyline audit's broken items, fixed here: the shot is real (a camera over the replay, not just a file name);
    /// nothing plays before the recording starts on its frame (the replayer pauses the editor exactly at the take's start
    /// and the Recorder starts there, the jump to it drawn and heard by nobody); a shot's keys run on take time (slow
    /// motion never stretches them); the effects' random numbers are seeded from the replay. Every take is the clean
    /// picture unless <c>-hell-capture-hud full</c>.</para>
    /// <para>Game time is the Recorder's constant rate (1/fps a frame), which must be the replay's own step: a replay recorded
    /// at 120 is captured at 120.</para>
    /// </summary>
    [InitializeOnLoad]
    public static class HellTrailerCapture
    {
        const string KeyActive = "HellTrailer.Active", KeyIndex = "HellTrailer.Index", KeyPhase = "HellTrailer.Phase",
                     KeyOut = "HellTrailer.Out", KeyResults = "HellTrailer.Results", KeyStarted = "HellTrailer.Started",
                     KeyDeadline = "HellTrailer.Deadline", KeyFrames = "HellTrailer.Frames", KeyRunInBackground = "HellTrailer.RunInBackground";
        const int PhaseStarting = 0, PhaseRecording = 1, PhaseExiting = 2, PhaseFinalizing = 3;
        const float FileWait = 20f;

        static RecorderController _controller;
        static double _finalizeStart = -1;
        static int _recorded;

        static HellTrailerCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static HellCaptureArgs Args() => HellCapture.Parse(Environment.GetCommandLineArgs());

        public static void RunFromCommandLine()
        {
            var args = Args();
            string outDir = Path.GetFullPath(args.outDir.Length > 0 ? args.outDir : Path.Combine("out", "hell", "trailer", DateTime.Now.ToString("yyyyMMdd-HHmm")));
            SessionState.SetString(KeyOut, outDir);
            SessionState.SetString(KeyResults, "");
            SessionState.SetInt(KeyIndex, 0);
            SessionState.SetInt(KeyPhase, PhaseStarting);
            SessionState.SetFloat(KeyStarted, (float)EditorApplication.timeSinceStartup);
            SessionState.SetBool(KeyRunInBackground, PlayerSettings.runInBackground);
            SessionState.SetBool(KeyActive, true);
            try
            {
                Directory.CreateDirectory(outDir);
                string old = Path.Combine(outDir, "trailer.json");
                if (File.Exists(old)) File.Delete(old);
                if (Application.isBatchMode) { Finish(false, "batch mode cannot record: launch the editor without -batchmode"); return; }
                if (args.problems.Count > 0) { Finish(false, string.Join("; ", args.problems)); return; }
                if (args.takes.Count == 0) { Finish(false, "no takes: pass " + HellCapture.TakeSwitch + " <replay>[@from-to][:shot[:voices]][,...]"); return; }
                float deadline = 180f;
                foreach (var job in args.takes)
                {
                    var rep = HellReplay.Load(HellCapture.ReplayPath(job.replay));
                    if (rep.fps != args.fps) { Finish(false, job.replay + " was recorded at " + rep.fps + " fps: capture it with " + HellCapture.FpsSwitch + " " + rep.fps); return; }
                    float to = job.to > 0f ? Math.Min(job.to, rep.Seconds) : rep.Seconds;
                    if (to <= job.from) { Finish(false, job.replay + " is " + rep.Seconds.ToString("0.0") + " s long: nothing after " + job.from + " s"); return; }
                    if (job.shot.Length > 0)
                    {
                        var problems = new System.Collections.Generic.List<string>();
                        HellShots.Load(job.shot, Directory.GetCurrentDirectory(), problems);
                        foreach (var p in problems) Debug.LogWarning("HELL_TRAILER shot " + p);
                    }
                    foreach (string ext in new[] { ".mp4", ".mov" })
                    {
                        string f = Path.Combine(outDir, job.FileStem + ext);
                        if (File.Exists(f)) File.Delete(f);
                    }
                    // the jump to the start runs undrawn (fast); the take itself may draw far below its rate at 1440p
                    deadline += job.from * 0.5f + (to - job.from) * 10f + 60f;
                }
                SessionState.SetFloat(KeyDeadline, deadline);
                if (!File.Exists(SkylineArenaBuilder.HellScenePath)) SkylineArenaBuilder.BuildHell(SkylineArenaBuilder.HellScenePath);
                OpenAndPlay(0);
            }
            catch (Exception e)
            {
                Finish(false, "setup: " + e.Message);
            }
        }

        static void OpenAndPlay(int index)
        {
            var args = Args();
            var job = args.takes[index];
            var take = new HellCurrentTake
            {
                replay = job.replay, from = job.from, to = job.to, shot = job.shot,
                voices = job.voices == HellVoiceMode.NoVoices ? "no-voices" : job.voices == HellVoiceMode.VoicesOnly ? "voices-only" : "all",
                hud = args.takesKeepHud ? "full" : "clean",
                capture = true,
            };
            Directory.CreateDirectory(Path.GetDirectoryName(HellCaptureBoot.TakePath));
            File.WriteAllText(HellCaptureBoot.TakePath, JsonUtility.ToJson(take));
            SessionState.SetInt(KeyPhase, PhaseStarting);
            EditorSceneManager.OpenScene(SkylineArenaBuilder.HellScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(KeyActive, false)) return;
            try
            {
                double now = EditorApplication.timeSinceStartup;
                if (now - SessionState.GetFloat(KeyStarted, (float)now) > SessionState.GetFloat(KeyDeadline, 600f)) { Finish(false, "deadline"); return; }
                switch (SessionState.GetInt(KeyPhase, PhaseStarting))
                {
                    case PhaseStarting: TickStarting(); break;
                    case PhaseRecording: TickRecording(); break;
                    case PhaseExiting: TickExiting(); break;
                    case PhaseFinalizing: TickFinalizing(now); break;
                }
            }
            catch (Exception e)
            {
                Finish(false, "tick: " + e.Message);
            }
        }

        static void TickStarting()
        {
            if (!EditorApplication.isPlaying) return;
            Application.runInBackground = true;
            // the replayer pauses the editor on the take's first frame (Debug.Break) and says so
            if (!HellReplayer.ReadyForRecording || !EditorApplication.isPaused) return;
            StartRecording();
            EditorApplication.isPaused = false;
        }

        /// <summary>The Recorder settings for one take: a constant frame rate at the capture fps, the size and the codec.</summary>
        public static RecorderControllerSettings Settings(HellCaptureArgs args, string outFileNoExt)
        {
            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "hell-trailer";
            movie.Enabled = true;
            if (args.codec == HellCaptureCodec.H264)
                movie.EncoderSettings = new CoreEncoderSettings
                {
                    EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.Custom,
                    TargetBitRate = HellCapture.EffectiveBitrate(args),
                    GopSize = (uint)args.fps,
                    NumConsecutiveBFrames = 2,
                    Codec = CoreEncoderSettings.OutputCodec.MP4,
                };
            else
                movie.EncoderSettings = new ProResEncoderSettings
                {
                    Format = args.codec == HellCaptureCodec.ProRes4444 ? ProResEncoderSettings.OutputFormat.ProRes4444 : ProResEncoderSettings.OutputFormat.ProRes422HQ,
                };
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = args.width, OutputHeight = args.height };
            movie.AudioInputSettings.PreserveAudio = true;
            movie.OutputFile = outFileNoExt;
            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            settings.FrameRate = args.fps;
            settings.FrameRatePlayback = FrameRatePlayback.Constant;
            settings.CapFrameRate = true;
            return settings;
        }

        static void StartRecording()
        {
            var args = Args();
            int index = SessionState.GetInt(KeyIndex, 0);
            var job = args.takes[index];
            string dir = SessionState.GetString(KeyOut, "");
            try
            {
                var controller = new RecorderController(Settings(args, Path.Combine(dir, job.FileStem)));
                controller.PrepareRecording();
                if (!controller.StartRecording()) throw new InvalidOperationException("StartRecording returned false");
                _controller = controller;
            }
            catch (Exception e)
            {
                _controller = null;
                Finish(false, "the Recorder did not start for " + job.FileStem + ": " + e.Message);
                return;
            }
            var rep = HellReplay.Load(HellCapture.ReplayPath(job.replay));
            // the same end step the replayer stops at; counted from the step it paused on (a take from 0 starts at step 1)
            int toFrame = job.to > job.from ? rep.FrameAt(job.to) : rep.FrameCount;
            _recorded = HellReplayer.Index;
            int frames = Math.Max(1, toFrame - _recorded);
            SessionState.SetInt(KeyFrames, frames);
            SessionState.SetInt(KeyPhase, PhaseRecording);
            Debug.Log("HELL_TRAILER recording " + job.FileStem + " " + args.width + "x" + args.height + "@" + args.fps + " " + args.codec +
                      (args.codec == HellCaptureCodec.H264 ? " " + HellCapture.EffectiveBitrate(args) + " Mbit/s" : "") + ", " + frames + " frames from step " + _recorded);
        }

        static void TickRecording()
        {
            if (_controller == null) { Finish(false, "the recorder was lost during recording (domain reload)"); return; }
            if (!EditorApplication.isPlaying) { Finish(false, "left Play mode during recording"); return; }
            if (EditorApplication.isPaused) return;
            var args = Args();
            int index = SessionState.GetInt(KeyIndex, 0);
            var job = args.takes[index];
            // counted in replay steps, which are the recorded frames (one step a frame at the constant rate)
            if (HellReplayer.Index - _recorded < SessionState.GetInt(KeyFrames, 0)) return;
            var controller = _controller;
            _controller = null;
            try { controller.StopRecording(); }
            catch (Exception e) { Finish(false, "stopping the Recorder for " + job.FileStem + ": " + e.Message); return; }
            Debug.Log("HELL_TRAILER recorded " + job.FileStem + " steps=" + (HellReplayer.Index - _recorded));
            SessionState.SetString(KeyResults, SessionState.GetString(KeyResults, "") + job.FileStem + ";");
            SessionState.SetInt(KeyIndex, index + 1);
            SessionState.SetInt(KeyPhase, PhaseExiting);
            EditorApplication.isPlaying = false;
        }

        static void TickExiting()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            int index = SessionState.GetInt(KeyIndex, 0);
            if (index >= Args().takes.Count)
            {
                SessionState.SetInt(KeyPhase, PhaseFinalizing);
                _finalizeStart = -1;
                return;
            }
            OpenAndPlay(index);
        }

        static void TickFinalizing(double now)
        {
            if (_finalizeStart < 0) _finalizeStart = now;
            string dir = SessionState.GetString(KeyOut, "");
            var missing = new StringBuilder();
            foreach (var job in Args().takes)
            {
                string mp4 = Path.Combine(dir, job.FileStem + ".mp4"), mov = Path.Combine(dir, job.FileStem + ".mov");
                bool ok = (File.Exists(mp4) && new FileInfo(mp4).Length > 0) || (File.Exists(mov) && new FileInfo(mov).Length > 0);
                if (!ok) missing.Append(job.FileStem).Append(' ');
            }
            if (missing.Length == 0) { Finish(true, null); return; }
            if (now - _finalizeStart > FileWait) Finish(false, "missing or empty output: " + missing.ToString().TrimEnd());
        }

        static string Quote(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        static void Finish(bool ok, string error)
        {
            if (_controller != null)
            {
                try { _controller.StopRecording(); } catch (Exception) { }
                _controller = null;
            }
            SessionState.SetBool(KeyActive, false);
            try { if (File.Exists(HellCaptureBoot.TakePath)) File.Delete(HellCaptureBoot.TakePath); } catch (Exception) { }
            try
            {
                bool original = SessionState.GetBool(KeyRunInBackground, PlayerSettings.runInBackground);
                if (PlayerSettings.runInBackground != original) PlayerSettings.runInBackground = original;
            }
            catch (Exception) { }
            string dir = SessionState.GetString(KeyOut, "");
            try
            {
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                    var a = Args();
                    File.WriteAllText(Path.Combine(dir, "trailer.json"),
                        "{\"ok\":" + (ok ? "true" : "false") + ",\"takes\":" + Quote(SessionState.GetString(KeyResults, "")) +
                        ",\"fps\":" + a.fps + ",\"width\":" + a.width + ",\"height\":" + a.height + ",\"codec\":" + Quote(a.codec.ToString()) +
                        ",\"error\":" + (error == null ? "null" : Quote(error)) + "}");
                }
            }
            catch (Exception e) { Debug.LogError("HELL_TRAILER could not write trailer.json: " + e.Message); }
            Debug.Log("HELL_TRAILER " + (ok ? "OK" : "FAILED " + error));
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
#endif
