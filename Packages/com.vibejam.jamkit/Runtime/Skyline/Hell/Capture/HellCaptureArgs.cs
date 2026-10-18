using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace JamKit.Skyline
{
    /// <summary>What the HUD shows: everything, or nothing at all (the clean picture for the trailer: no crosshair, bars,
    /// objective line, banners or subtitles).</summary>
    public enum HellHudLayout { Full, Clean }

    /// <summary>Which sounds play: all of them; all but the spoken lines and barks (the trailer's effects bed under the
    /// narrator); or only the spoken lines (a voices stem, from a second pass of the same replay).</summary>
    public enum HellVoiceMode { All, NoVoices, VoicesOnly }

    public enum HellCaptureCodec { H264, ProRes422HQ, ProRes4444 }

    /// <summary>One take to record in the editor: a replay, the stretch of it (replay seconds), the camera shot (empty: the
    /// player's own camera) and which sounds play.</summary>
    public sealed class HellTakeJob
    {
        public string replay = "";
        /// <summary>Replay seconds; <see cref="to"/> &lt;= 0 means the replay's end.</summary>
        public float from, to;
        public string shot = "";
        public HellVoiceMode voices = HellVoiceMode.All;

        /// <summary>take-&lt;replay&gt;[-&lt;from&gt;s][-&lt;shot&gt;][-voices|-novoices], so two takes of one replay never share a file.</summary>
        public string FileStem
        {
            get
            {
                var sb = new StringBuilder("take-").Append(HellCapture.Safe(Path.GetFileNameWithoutExtension(replay)));
                if (from > 0f) sb.Append('-').Append(((int)Math.Round(from * 10f)).ToString(CultureInfo.InvariantCulture)).Append("ds");
                if (shot.Length > 0) sb.Append('-').Append(HellCapture.Safe(Path.GetFileNameWithoutExtension(shot)));
                if (voices == HellVoiceMode.NoVoices) sb.Append("-novoices");
                if (voices == HellVoiceMode.VoicesOnly) sb.Append("-voices");
                return sb.ToString();
            }
        }
    }

    /// <summary>Every capture switch on one command line, parsed (docs/HELL-CAPTURE.md).</summary>
    public sealed class HellCaptureArgs
    {
        // the picture and the sound (any run)
        public HellHudLayout hud = HellHudLayout.Full;
        public HellVoiceMode voices = HellVoiceMode.All;
        /// <summary>A switch set the HUD or voices (so the pause menu's own choice is not overridden otherwise).</summary>
        public bool hudGiven, voicesGiven;

        // recording the creator's play
        public string record = "";
        public int recordFps = HellCapture.DefaultFps;
        /// <summary>The run's seed (0: one is picked and saved in the recording).</summary>
        public int seed;

        // replaying it
        public string replay = "";
        public float replayFrom, replayTo;
        public string shot = "";

        // the frame-time check
        public string perfOut = "";
        public float perfSpeed = 8f;
        public int perfTargetFps = 120;
        public bool perfEnemies = true;
        /// <summary>Rooms to fly through as "floor.kind" (1-based floor, kind name); empty: all 18.</summary>
        public readonly List<string> perfRooms = new List<string>();

        // the editor's Recorder capture
        public readonly List<HellTakeJob> takes = new List<HellTakeJob>();
        public int fps = HellCapture.DefaultFps;
        public int width = 2560, height = 1440;
        public HellCaptureCodec codec = HellCaptureCodec.H264;
        public float bitrate;
        public string outDir = "";
        /// <summary>The takes keep the HUD (by default a take is the clean picture).</summary>
        public bool takesKeepHud;

        public readonly List<string> problems = new List<string>();

        public bool Recording => record.Length > 0;
        public bool Replaying => replay.Length > 0;
        public bool Perf => perfOut.Length > 0;
    }

    /// <summary>
    /// A chapter started from the command line (docs/HELL-CAPTURE.md, docs/HELL-AUTOPILOT.md), for capture and testing:
    /// <c>-hellChapter 1|2|3|4</c> (as the title's CHAPTERS menu counts them; 4 is the Vesper fight) skips the title and starts
    /// that chapter. <c>-hellErrands yes|no</c> answers the Vesper chapter's errands question (default yes);
    /// <c>-hellDifficulty easy|normal|hard</c> (default normal). The skipped floors' picks are taken by a seeded choice.
    /// </summary>
    public sealed class HellChapterStart
    {
        public const string ChapterSwitch = "-hellChapter", ErrandsSwitch = "-hellErrands", DifficultySwitch = "-hellDifficulty";

        /// <summary>The flow's chapter (0-based: 0-2 a floor, <see cref="HellFlow.VesperChapter"/>), or
        /// <see cref="HellFlow.FullGame"/> when none was asked for (or it was not understood).</summary>
        public int chapter = HellFlow.FullGame;
        public bool errands = true;
        public HellDifficulty difficulty = HellDifficulty.Normal;
        public readonly List<string> problems = new List<string>();

        public bool Given => chapter >= 0;

        public static HellChapterStart Parse(string[] args)
        {
            var r = new HellChapterStart();
            if (args == null) return r;
            for (int i = 0; i + 1 < args.Length; i++)
            {
                string v = (args[i + 1] ?? "").Trim().ToLowerInvariant();
                if (args[i] == ChapterSwitch)
                {
                    if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int c) && c >= 1 && c <= HellFlow.Chapters) r.chapter = c - 1;
                    else r.problems.Add(ChapterSwitch + " " + args[i + 1] + ": 1 to " + HellFlow.Chapters);
                }
                else if (args[i] == ErrandsSwitch)
                {
                    if (v == "yes" || v == "y" || v == "true" || v == "1") r.errands = true;
                    else if (v == "no" || v == "n" || v == "false" || v == "0") r.errands = false;
                    else r.problems.Add(ErrandsSwitch + " " + args[i + 1] + ": yes or no");
                }
                else if (args[i] == DifficultySwitch)
                {
                    if (v == "easy") r.difficulty = HellDifficulty.Easy;
                    else if (v == "normal") r.difficulty = HellDifficulty.Normal;
                    else if (v == "hard") r.difficulty = HellDifficulty.Hard;
                    else r.problems.Add(DifficultySwitch + " " + args[i + 1] + ": easy, normal or hard");
                }
            }
            return r;
        }

        /// <summary>The picks the skipped floors give before <paramref name="chapter"/> (one per floor before it).</summary>
        public static int PicksBefore(int chapter, int floorCount) => chapter <= 0 ? 0 : Math.Min(chapter, floorCount - 1);
    }

    /// <summary>
    /// CAPTURE-ONLY cheats (docs/HELL-CAPTURE.md, "Capture cheats"), so the autopilot can reach the end of the Vesper fight, the
    /// decision and the escape for trailer footage. They exist only on the command line (no menu, no save):
    /// <c>-hellInvulnerable</c> (no death: a hit that would wreck you leaves you whole; a sink or a fall goes back to the last
    /// safe spot, The Collapse included, without a restart; the hit effects still play) and <c>-hellBossHealth 0..1</c>
    /// (Vesper's health and phase thresholds scaled, e.g. 0.35).
    /// </summary>
    public sealed class HellCaptureCheats
    {
        public const string InvulnerableSwitch = "-hellInvulnerable", BossHealthSwitch = "-hellBossHealth";
        /// <summary>The smallest boss scale (a phase still has some health to take off).</summary>
        public const float MinBossHealth = 0.05f;

        public bool invulnerable;
        /// <summary>Vesper's health scale (1: as tuned).</summary>
        public float bossHealth = 1f;
        public readonly List<string> problems = new List<string>();

        public bool Any => invulnerable || bossHealth < 1f;

        static HellCaptureCheats _current;
        /// <summary>This launch's cheats (read once from the process's command line).</summary>
        public static HellCaptureCheats Current => _current ?? (_current = Parse(Environment.GetCommandLineArgs()));
        /// <summary>Tests and the editor: set the cheats (null reads the command line again).</summary>
        public static void SetCurrent(HellCaptureCheats c) { _current = c; }

        public static HellCaptureCheats Parse(string[] args)
        {
            var r = new HellCaptureCheats();
            if (args == null) return r;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == InvulnerableSwitch) r.invulnerable = true;
                else if (args[i] == BossHealthSwitch)
                {
                    string v = i + 1 < args.Length ? (args[i + 1] ?? "").Trim() : "";
                    if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float k) && k > 0f && k <= 1f)
                        r.bossHealth = Math.Max(MinBossHealth, k);
                    else r.problems.Add(BossHealthSwitch + " " + v + ": a number above 0, up to 1");
                }
            }
            return r;
        }

        /// <summary>Scales Vesper's health and her phase thresholds together (the phases keep their proportions).</summary>
        public static void ScaleBoss(VesperTuning t, float k)
        {
            if (t == null || !(k > 0f) || k >= 1f) return;
            t.maxHealth *= k;
            t.phase2At *= k;
            t.phase3At *= k;
        }

        /// <summary>The start-up log line's body: "invulnerable, boss health 0.35" or "none".</summary>
        public string Describe()
        {
            var parts = new List<string>();
            if (invulnerable) parts.Add("invulnerable");
            if (bossHealth < 1f) parts.Add("boss health " + bossHealth.ToString("0.###", CultureInfo.InvariantCulture));
            foreach (var p in problems) parts.Add("ignored " + p);
            return parts.Count == 0 ? "none" : string.Join(", ", parts);
        }
    }

    /// <summary>
    /// The Hell capture tools' command line and numbers (docs/HELL-CAPTURE.md), ported from the Skyline trailer capture
    /// (<c>VibeJamKit-capture</c>, docs/SKYLINE-TRAILER-CAPTURE.md) with only what Hell needs: the fixed capture rate, size,
    /// codec and bitrate; the clean picture; recording and replaying play; camera shots; the frame-time check.
    /// </summary>
    public static class HellCapture
    {
        public const int DefaultFps = 120;
        public static readonly int[] Rates = { 60, 90, 120 };
        public const string ReplayExtension = ".hellrec";
        /// <summary>Where a bare replay name lives (relative to the working directory: the project in the editor).</summary>
        public const string ReplayFolder = "out/hell/replays";

        public const string HudSwitch = "-hell-hud", VoicesSwitch = "-hell-voices", CleanSwitch = "-hell-clean";
        public const string RecordSwitch = "-hell-rec", RecordFpsSwitch = "-hell-rec-fps", SeedSwitch = "-hellSeed";
        public const string ReplaySwitch = "-hell-replay", FromSwitch = "-hell-replay-from", ToSwitch = "-hell-replay-to", ShotSwitch = "-hell-shot";
        public const string PerfSwitch = "-hell-perf", PerfSpeedSwitch = "-hell-perf-speed", PerfTargetSwitch = "-hell-perf-target",
                            PerfRoomsSwitch = "-hell-perf-rooms", PerfEnemiesSwitch = "-hell-perf-enemies";
        public const string TakeSwitch = "-hell-take", FpsSwitch = "-hell-capture-fps", ResSwitch = "-hell-capture-res",
                            CodecSwitch = "-hell-capture-codec", BitrateSwitch = "-hell-capture-mbps", OutSwitch = "-hell-capture-out",
                            KeepHudSwitch = "-hell-capture-hud";

        public static bool ValidFps(int fps) => Array.IndexOf(Rates, fps) >= 0;

        /// <summary>The game-time step of one frame (s).</summary>
        public static float Step(int fps) => fps > 0 ? 1f / fps : 0f;

        /// <summary>Frames for <paramref name="seconds"/> at <paramref name="fps"/>.</summary>
        public static int Frames(float seconds, int fps) => seconds > 0f && fps > 0 ? (int)Math.Round(seconds * fps) : 0;

        /// <summary>"1440p", "1080p", "2160p"/"4k" (16:9) or "WxH" (even sizes, 640..7680 wide).</summary>
        public static bool TryResolution(string s, out int width, out int height)
        {
            width = height = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim().ToLowerInvariant();
            if (s == "4k") s = "2160p";
            if (s.EndsWith("p") && int.TryParse(s.Substring(0, s.Length - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int h))
            {
                height = h;
                width = (int)Math.Round(h * 16.0 / 9.0);
                if ((width & 1) == 1) width++;
            }
            else
            {
                int x = s.IndexOf('x');
                if (x <= 0 || !int.TryParse(s.Substring(0, x), NumberStyles.Integer, CultureInfo.InvariantCulture, out width) ||
                    !int.TryParse(s.Substring(x + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out height)) { width = height = 0; return false; }
            }
            if (width < 640 || width > 7680 || height < 360 || height > 4320 || (width & 1) == 1 || (height & 1) == 1) { width = height = 0; return false; }
            return true;
        }

        /// <summary>A very high H.264 bitrate: 0.35 bits per pixel per frame, clamped to 40..200 Mbit/s (1440p120: about 155).</summary>
        public static float BitrateMbps(int width, int height, int fps)
        {
            double mbps = (double)width * height * fps * 0.35 / 1e6;
            return (float)Math.Max(40.0, Math.Min(200.0, Math.Round(mbps)));
        }

        public static float EffectiveBitrate(HellCaptureArgs a) => a.bitrate > 0f ? a.bitrate : BitrateMbps(a.width, a.height, a.fps);

        /// <summary>Lower-case letters, digits and '-' only (file names).</summary>
        public static string Safe(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in (s ?? "").ToLowerInvariant())
                sb.Append((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' ? c : '-');
            return sb.ToString();
        }

        /// <summary>A replay argument as a path: a bare name ("run1") is <see cref="ReplayFolder"/>/run1.hellrec; a name
        /// without the extension gets it; anything with a folder in it is kept as given.</summary>
        public static string ReplayPath(string nameOrPath)
        {
            if (string.IsNullOrWhiteSpace(nameOrPath)) return "";
            string s = nameOrPath.Trim();
            if (!s.EndsWith(ReplayExtension, StringComparison.OrdinalIgnoreCase)) s += ReplayExtension;
            if (s.IndexOf('/') < 0 && s.IndexOf('\\') < 0) s = ReplayFolder + "/" + s;
            return s;
        }

        public static bool TryVoices(string s, out HellVoiceMode mode)
        {
            mode = HellVoiceMode.All;
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "on": case "all": case "": mode = HellVoiceMode.All; return true;
                case "off": case "no-voices": case "novoices": case "mute": mode = HellVoiceMode.NoVoices; return true;
                case "only": case "voices-only": case "voices": mode = HellVoiceMode.VoicesOnly; return true;
                default: return false;
            }
        }

        static string Value(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        static bool Has(string[] args, string name) => Array.IndexOf(args, name) >= 0;

        static bool TryFloat(string s, out float f) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f);

        /// <summary>
        /// One take: <c>replay[@from[-to]][:shot[:voices]]</c>, e.g. <c>run1@12.5-20:orbit-imp:no-voices</c>. Times are replay
        /// seconds; voices are <c>all</c>, <c>no-voices</c> or <c>voices-only</c>. Returns null (and a problem) when malformed.
        /// </summary>
        public static HellTakeJob ParseTake(string raw, string defaultShot, List<string> problems)
        {
            string[] p = (raw ?? "").Trim().Split(':');
            if (p[0].Trim().Length == 0) { problems?.Add("take '" + raw + "': no replay"); return null; }
            var job = new HellTakeJob { shot = defaultShot ?? "" };
            string head = p[0].Trim();
            int at = head.IndexOf('@');
            if (at >= 0)
            {
                string span = head.Substring(at + 1);
                head = head.Substring(0, at);
                int dash = span.IndexOf('-', 1);
                string a = dash > 0 ? span.Substring(0, dash) : span, b = dash > 0 ? span.Substring(dash + 1) : "";
                if (!TryFloat(a, out job.from) || job.from < 0f) { problems?.Add("take '" + raw + "': bad start time '" + a + "'"); return null; }
                if (b.Length > 0 && (!TryFloat(b, out job.to) || job.to <= job.from)) { problems?.Add("take '" + raw + "': bad end time '" + b + "'"); return null; }
            }
            if (head.Trim().Length == 0) { problems?.Add("take '" + raw + "': no replay"); return null; }
            job.replay = head.Trim();
            if (p.Length > 1 && p[1].Trim().Length > 0) job.shot = p[1].Trim();
            if (p.Length > 2 && !TryVoices(p[2], out job.voices)) { problems?.Add("take '" + raw + "': voices '" + p[2] + "': use all, no-voices or voices-only"); return null; }
            if (p.Length > 3) { problems?.Add("take '" + raw + "': too many ':' parts"); return null; }
            return job;
        }

        public static HellCaptureArgs Parse(string[] args)
        {
            var a = new HellCaptureArgs();
            args = args ?? new string[0];
            var pr = a.problems;

            string hud = Value(args, HudSwitch);
            if (hud != null)
            {
                a.hudGiven = true;
                switch (hud.Trim().ToLowerInvariant())
                {
                    case "full": case "on": a.hud = HellHudLayout.Full; break;
                    case "clean": case "none": case "off": a.hud = HellHudLayout.Clean; break;
                    default: pr.Add(HudSwitch + " " + hud + ": use full or clean"); a.hudGiven = false; break;
                }
            }
            string voices = Value(args, VoicesSwitch);
            if (voices != null)
            {
                if (TryVoices(voices, out a.voices)) a.voicesGiven = true;
                else pr.Add(VoicesSwitch + " " + voices + ": use on, off or only");
            }
            if (Has(args, CleanSwitch))
            {
                if (!a.hudGiven) { a.hud = HellHudLayout.Clean; a.hudGiven = true; }
                if (!a.voicesGiven) { a.voices = HellVoiceMode.NoVoices; a.voicesGiven = true; }
            }

            string rec = Value(args, RecordSwitch);
            if (rec != null) a.record = ReplayPath(rec);
            string rfps = Value(args, RecordFpsSwitch);
            if (rfps != null)
            {
                if (int.TryParse(rfps, NumberStyles.Integer, CultureInfo.InvariantCulture, out int f) && ValidFps(f)) a.recordFps = f;
                else pr.Add(RecordFpsSwitch + " " + rfps + ": use 60, 90 or 120");
            }
            string seed = Value(args, SeedSwitch);
            if (seed != null && !int.TryParse(seed, NumberStyles.Integer, CultureInfo.InvariantCulture, out a.seed)) pr.Add(SeedSwitch + " " + seed + ": a whole number");

            string rep = Value(args, ReplaySwitch);
            if (rep != null) a.replay = ReplayPath(rep);
            string from = Value(args, FromSwitch), to = Value(args, ToSwitch);
            if (from != null && (!TryFloat(from, out a.replayFrom) || a.replayFrom < 0f)) { pr.Add(FromSwitch + " " + from + ": seconds"); a.replayFrom = 0f; }
            if (to != null && (!TryFloat(to, out a.replayTo) || a.replayTo <= a.replayFrom)) { pr.Add(ToSwitch + " " + to + ": seconds after the start"); a.replayTo = 0f; }
            a.shot = (Value(args, ShotSwitch) ?? "").Trim();
            if (a.Recording && a.Replaying) pr.Add(RecordSwitch + " and " + ReplaySwitch + " together: pick one");

            string perf = Value(args, PerfSwitch);
            if (perf != null) a.perfOut = perf;
            string speed = Value(args, PerfSpeedSwitch);
            if (speed != null && (!TryFloat(speed, out a.perfSpeed) || a.perfSpeed < 1f || a.perfSpeed > 60f)) { pr.Add(PerfSpeedSwitch + " " + speed + ": 1..60 m/s"); a.perfSpeed = 8f; }
            string target = Value(args, PerfTargetSwitch);
            if (target != null && (!int.TryParse(target, NumberStyles.Integer, CultureInfo.InvariantCulture, out a.perfTargetFps) || a.perfTargetFps < 30 || a.perfTargetFps > 500))
            { pr.Add(PerfTargetSwitch + " " + target + ": 30..500 fps"); a.perfTargetFps = 120; }
            string rooms = Value(args, PerfRoomsSwitch);
            if (rooms != null && rooms.Trim().ToLowerInvariant() != "all")
                foreach (string r in rooms.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) a.perfRooms.Add(r.Trim());
            string enemies = Value(args, PerfEnemiesSwitch);
            if (enemies != null)
            {
                string e = enemies.Trim().ToLowerInvariant();
                if (e == "on" || e == "off") a.perfEnemies = e == "on";
                else pr.Add(PerfEnemiesSwitch + " " + enemies + ": on or off");
            }

            string shotDefault = a.shot;
            string takes = Value(args, TakeSwitch);
            if (!string.IsNullOrWhiteSpace(takes))
                foreach (string raw in takes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var job = ParseTake(raw, shotDefault, pr);
                    if (job != null) a.takes.Add(job);
                }
            string fps = Value(args, FpsSwitch);
            if (fps != null)
            {
                if (int.TryParse(fps, NumberStyles.Integer, CultureInfo.InvariantCulture, out int f) && ValidFps(f)) a.fps = f;
                else pr.Add(FpsSwitch + " " + fps + ": use 60, 90 or 120");
            }
            string res = Value(args, ResSwitch);
            if (res != null)
            {
                if (TryResolution(res, out int w, out int h)) { a.width = w; a.height = h; }
                else pr.Add(ResSwitch + " " + res + ": use 1080p, 1440p, 2160p or WxH");
            }
            string codec = Value(args, CodecSwitch);
            if (codec != null)
            {
                switch (codec.Trim().ToLowerInvariant())
                {
                    case "h264": case "mp4": a.codec = HellCaptureCodec.H264; break;
                    case "prores": case "prores422hq": a.codec = HellCaptureCodec.ProRes422HQ; break;
                    case "prores4444": a.codec = HellCaptureCodec.ProRes4444; break;
                    default: pr.Add(CodecSwitch + " " + codec + ": use h264, prores or prores4444"); break;
                }
            }
            string mbps = Value(args, BitrateSwitch);
            if (mbps != null)
            {
                if (TryFloat(mbps, out float m) && m >= 5f && m <= 400f) a.bitrate = m;
                else pr.Add(BitrateSwitch + " " + mbps + ": 5..400");
            }
            a.outDir = Value(args, OutSwitch) ?? "";
            string keep = Value(args, KeepHudSwitch);
            if (keep != null)
            {
                string k = keep.Trim().ToLowerInvariant();
                if (k == "full" || k == "clean") a.takesKeepHud = k == "full";
                else pr.Add(KeepHudSwitch + " " + keep + ": full or clean");
            }
            return a;
        }
    }

    /// <summary>
    /// The take's own clock: seconds of recorded video, advanced by the capture step each frame (by the real frame time when
    /// no capture step is set). It ignores <c>Time.timeScale</c>, so slow motion (Time Dilation, a hit stop) never stretches
    /// a shot's keys: camera shots run on this clock, the game on <c>Time.time</c>. (The Skyline audit's "slow motion
    /// stretches the scripted waits" fix: nothing scripted in a Hell take runs on game time.)
    /// </summary>
    public sealed class HellTakeClock
    {
        public float Seconds { get; private set; }
        public int Frames { get; private set; }

        public void Advance(float captureDeltaTime, float unscaledDeltaTime)
        {
            Seconds += captureDeltaTime > 0f ? captureDeltaTime : Math.Max(0f, unscaledDeltaTime);
            Frames++;
        }

        public void Reset() { Seconds = 0f; Frames = 0; }
    }
}
