using System;
using System.IO;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>The Hell abilities' buttons for one frame, from a replay (<see cref="HellAbilities.ScriptedButtons"/>).</summary>
    public struct HellScriptedButtons
    {
        public bool stepHeld, rewindPressed, skillPressed, wheelHeld;
    }

    /// <summary>What the editor hands play mode (domain reloads wipe statics): a capture take, a preview of a replay, or a
    /// recording of play started from the editor window. Written to <see cref="HellCaptureBoot.TakePath"/>.</summary>
    [Serializable]
    public sealed class HellCurrentTake
    {
        public string replay = "";
        public float from, to;
        public string shot = "";
        public string voices = "all";
        public string hud = "clean";
        /// <summary>The Recorder is recording this take (the replayer pauses the editor at <see cref="from"/> so it can start).</summary>
        public bool capture;
        /// <summary>From the editor window: used once, then the file is deleted.</summary>
        public bool preview;
        /// <summary>From the editor window: record play to this file instead of replaying.</summary>
        public string record = "";
        public int recordFps = HellCapture.DefaultFps;
    }

    /// <summary>
    /// Starts the capture tools (docs/HELL-CAPTURE.md) from the command line or the editor's hand-over file:
    /// <list type="bullet">
    /// <item><c>-hell-rec &lt;name|path&gt;</c>: record the creator's play (<see cref="HellPlayRecorder"/>), on a fixed step.</item>
    /// <item><c>-hell-replay &lt;name|path&gt;</c>: play a recording again (<see cref="HellReplayer"/>), optionally from a time and
    /// through a camera shot (<see cref="HellShotCamera"/>).</item>
    /// <item><c>-hell-perf &lt;dir&gt;</c>: the frame-time fly-through of every room (<see cref="HellPerfFlight"/>).</item>
    /// </list>
    /// Recording and replaying lock game time to a fixed step (<c>Time.captureDeltaTime</c>), so the same input replays the
    /// same run. A plain launch does nothing here.
    /// </summary>
    public static class HellCaptureBoot
    {
        public const string TakePath = "Temp/HellCapture/current.json";

        enum Mode { None, Record, Replay, Perf }

        static Mode _mode;
        static HellCaptureArgs _args;
        static HellCurrentTake _take;
        static HellReplay _replay;
        static string _recordPath;
        static int _recordFps;

        /// <summary>The mode this play session runs in (for the editor's capture and the logs).</summary>
        public static bool Replaying => _mode == Mode.Replay;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void BeforeScene()
        {
            _mode = Mode.None;
            _take = null;
            _replay = null;
            _args = HellCapture.Parse(Environment.GetCommandLineArgs());
            HellCaptureCheats.SetCurrent(null);   // read this launch's command line afresh (domain reload off in the editor)
            var cheats = HellCaptureCheats.Current;
            if (cheats.Any || cheats.problems.Count > 0) Debug.Log("HELL_CAPTURE cheats: " + cheats.Describe());
            try { ReadTake(); }
            catch (Exception e) { Debug.LogError("HELL_CAPTURE could not read the editor's take: " + e.Message); }

            if (_take != null)
            {
                if (HellCapture.TryVoices(_take.voices, out var v)) HellCleanPicture.Voices = v;
                HellCleanPicture.Hud = string.Equals(_take.hud, "full", StringComparison.OrdinalIgnoreCase) ? HellHudLayout.Full : HellHudLayout.Clean;
            }

            if (_take != null && _take.record.Length > 0) { _recordPath = HellCapture.ReplayPath(_take.record); _recordFps = _take.recordFps; _mode = Mode.Record; }
            else if (_take != null && _take.replay.Length > 0) _mode = Mode.Replay;
            else if (_args.Recording) { _recordPath = _args.record; _recordFps = _args.recordFps; _mode = Mode.Record; }
            else if (_args.Replaying) _mode = Mode.Replay;
            else if (_args.Perf) _mode = Mode.Perf;

            if (_mode == Mode.Replay)
            {
                string path = HellCapture.ReplayPath(_take != null ? _take.replay : _args.replay);
                try
                {
                    _replay = HellReplay.Load(path);
                    Debug.Log("HELL_REPLAY loaded " + path + ": " + _replay.Describe());
                }
                catch (Exception e)
                {
                    Debug.LogError("HELL_REPLAY could not load " + path + ": " + e.Message);
                    _mode = Mode.None;
                    return;
                }
                LockStep(_replay.fps, _take == null || !_take.capture);
            }
            else if (_mode == Mode.Record)
            {
                if (!HellCapture.ValidFps(_recordFps)) _recordFps = HellCapture.DefaultFps;
                LockStep(_recordFps, true);
            }
        }

        /// <summary>Game time advances exactly one step a frame. <paramref name="realTime"/> also caps the frame rate there, so
        /// a machine that draws faster than the step plays at real speed (one that draws slower plays slightly slowed).</summary>
        static void LockStep(int fps, bool realTime)
        {
            Time.captureDeltaTime = HellCapture.Step(fps);
            if (realTime)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = fps;
            }
            Debug.Log("HELL_CAPTURE fixed step 1/" + fps + " s" + (realTime ? ", capped at " + fps + " fps" : ""));
        }

        static void ReadTake()
        {
            if (!Application.isEditor || !File.Exists(TakePath)) return;
            var take = HellCaptureJson.FromJson<HellCurrentTake>(File.ReadAllText(TakePath));
            if (take.preview)
            {
                File.Delete(TakePath);   // a preview is used once
                _take = take;
                return;
            }
            // a capture's take only counts in the editor the capture launched (a file left by a crash never takes over a session)
            if (take.capture && Array.IndexOf(Environment.GetCommandLineArgs(), HellCapture.TakeSwitch) >= 0) _take = take;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterScene()
        {
            if (_mode == Mode.None) return;
            var player = UnityEngine.Object.FindAnyObjectByType<SkylinePlayer>();
            var director = UnityEngine.Object.FindAnyObjectByType<HellRunDirector>();
            var go = new GameObject("HellCapture");
            switch (_mode)
            {
                case Mode.Record:
                {
                    int seed = _args.seed != 0 ? _args.seed : (Environment.TickCount & 0x3fffffff) | 1;
                    if (director != null) director.seed = seed;
                    var rec = go.AddComponent<HellPlayRecorder>();
                    rec.Configure(player, director != null, _recordPath, _recordFps, seed);
                    break;
                }
                case Mode.Replay:
                {
                    if (director != null)
                    {
                        director.seed = _replay.seed;
                        // how the recorded run began: a chapter from the title, its difficulty and the Vesper errands answer
                        // (in memory only: the saved options are not touched)
                        if (_replay.difficulty >= 0) SkylineSettings.Options.difficulty = _replay.difficulty;
                        if (_replay.errands >= 0) HellJobs.AssumeErrands(_replay.errands == 1);
                        else HellJobs.ClearAssumedErrands();
                        if (HellReplay.StartsOnChapter(_replay.chapter)) director.replayChapter = _replay.chapter;
                        if (_args.seed != 0 && _args.seed != _replay.seed)
                            Debug.LogWarning("HELL_REPLAY -hellSeed " + _args.seed + " on the command line overrides the recording's seed " + _replay.seed + ": the run will differ");
                    }
                    var rep = go.AddComponent<HellReplayer>();
                    float from = _take != null ? _take.from : _args.replayFrom;
                    float to = _take != null ? _take.to : _args.replayTo;
                    string shot = _take != null ? _take.shot : _args.shot;
                    rep.Configure(player, director != null, _replay, from, to, shot, _take != null && _take.capture);
                    break;
                }
                case Mode.Perf:
                {
                    var perf = go.AddComponent<HellPerfFlight>();
                    perf.Configure(player, director, _args);
                    break;
                }
            }
        }
    }
}
