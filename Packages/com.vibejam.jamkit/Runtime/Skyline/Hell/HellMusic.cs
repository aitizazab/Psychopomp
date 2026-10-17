using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace JamKit.Skyline
{
    // HellDescent's music, the pure rules (docs/HELL-MUSIC-DIRECTOR.md). The Unity side is
    // Assets/JamKit/Templates/SkylineRepo/Scripts/Audio/HellMusicDirector.cs; everything here runs in the unit tests.

    public enum HellMusicMood { Quiet = 0, Normal = 1, Danger = 2 }
    public enum HellMusicSpeed { Still = 0, Fast = 1, Flying = 2 }
    public enum HellMusicRole { Bed, Drums, Drone }

    // ------------------------------------------------------------------ the manifest

    /// <summary>One stem of a layered loop, or one part of a "parts" cue.</summary>
    public sealed class HellMusicStem
    {
        public string id, resource, kind;
        /// <summary>-1 = any (the drone).</summary>
        public int mood = -1, speed = -1, phase;
        public HellMusicRole role;
        /// <summary>A "stinger_into_cue" part: the cue it hands over to, and the stinger's sample where that cue's first downbeat
        /// lands (0 = none).</summary>
        public string intoCue;
        public int downbeatSample;
    }

    /// <summary>A cue of the composer's manifest (Resources/SkylineAudio/hell-music/hell-music-manifest.json).</summary>
    public sealed class HellMusicCue
    {
        public string id, kind, status, resource;
        public double bpm, duckDb;
        public int beatsPerBar = 4, bars, loopSamples, sampleRate = 48000;
        public readonly List<HellMusicStem> stems = new List<HellMusicStem>();
        public readonly Dictionary<string, HellMusicStem> parts = new Dictionary<string, HellMusicStem>();

        public bool Ready => status == "ready";
        public bool IsLoop => kind == "loop" || kind == "layered_loop";
        /// <summary>Seconds per bar (0 when the cue has no tempo).</summary>
        public double BarSeconds => bpm > 0 ? 60.0 / bpm * Math.Max(1, beatsPerBar) : 0;
        public double BeatSeconds => bpm > 0 ? 60.0 / bpm : 0;
        /// <summary>The loop's length in seconds: loop_samples when given, else bars x bar, else 0 (the clip's own length).</summary>
        public double LoopSeconds => loopSamples > 0 ? (double)loopSamples / sampleRate : bars > 0 ? bars * BarSeconds : 0;
        public HellMusicStem Part(string name) => name != null && parts.TryGetValue(name, out var p) ? p : null;
    }

    public sealed class HellMusicManifest
    {
        public const string ResourcePath = "SkylineAudio/hell-music/hell-music-manifest";
        public const string InRun = "inrun", BajaHold = "baja_hold", BajaGift = "baja_gift", Opening = "baja_opening",
            JobBrief = "baja_job_brief", JobDone = "baja_job_done", JobFailed = "baja_job_failed", Stats = "baja_stats",
            Cliffhanger = "baja_cliffhanger", Boss = "vesper_boss", History = "vesper_history", Decision = "vesper_decision",
            EndingKill = "ending_kill", EndingSpareKept = "ending_spare_kept", EndingSpareCost = "ending_spare_cost",
            Title = "title", Credits = "credits", FloorCard = "floor_card_";

        public int sampleRate = 48000;
        public readonly Dictionary<string, HellMusicCue> cues = new Dictionary<string, HellMusicCue>();

        public HellMusicCue Get(string id) => id != null && cues.TryGetValue(id, out var c) ? c : null;
        /// <summary>The cue when it exists and is ready (its files landed), else null.</summary>
        public HellMusicCue Ready(string id) { var c = Get(id); return c != null && c.Ready ? c : null; }

        public static HellMusicManifest Parse(string json)
        {
            var m = new HellMusicManifest();
            if (!(HellMiniJson.Parse(json) is Dictionary<string, object> root)) return m;
            m.sampleRate = Int(root, "sample_rate", 48000);
            if (!(Val(root, "cues") is Dictionary<string, object> cues)) return m;
            foreach (var kv in cues)
            {
                if (!(kv.Value is Dictionary<string, object> o)) continue;
                var c = new HellMusicCue
                {
                    id = kv.Key, kind = Str(o, "kind"), status = Str(o, "status") ?? "pending", resource = Str(o, "resource"),
                    bpm = Num(o, "bpm"), duckDb = Num(o, "duck_db"), beatsPerBar = Int(o, "beats_per_bar", 4), bars = Int(o, "bars", 0),
                    loopSamples = Int(o, "loop_samples", 0), sampleRate = m.sampleRate,
                };
                if (Val(o, "stems") is List<object> stems)
                    foreach (var s in stems) if (s is Dictionary<string, object> so) c.stems.Add(Stem(so, Str(so, "id")));
                if (Val(o, "parts") is Dictionary<string, object> parts)
                    foreach (var p in parts) if (p.Value is Dictionary<string, object> po) c.parts[p.Key] = Stem(po, p.Key);
                m.cues[c.id] = c;
            }
            return m;
        }

        static HellMusicStem Stem(Dictionary<string, object> o, string id) => new HellMusicStem
        {
            id = id, resource = Str(o, "resource"), kind = Str(o, "kind"), mood = MoodOf(Str(o, "mood")), speed = SpeedOf(Str(o, "speed")),
            role = RoleOf(Str(o, "role")), phase = Int(o, "phase", 0),
            intoCue = Str(o, "into_cue"), downbeatSample = Int(o, "downbeat_sample", 0),
        };

        public static int MoodOf(string s) => s == "quiet" ? 0 : s == "normal" ? 1 : s == "danger" ? 2 : -1;
        public static int SpeedOf(string s) => s == "still" ? 0 : s == "fast" ? 1 : s == "flying" ? 2 : -1;
        public static HellMusicRole RoleOf(string s) => s == "drums" ? HellMusicRole.Drums : s == "drone" ? HellMusicRole.Drone : HellMusicRole.Bed;

        static object Val(Dictionary<string, object> o, string k) => o.TryGetValue(k, out var v) ? v : null;
        static string Str(Dictionary<string, object> o, string k) => Val(o, k) as string;
        static double Num(Dictionary<string, object> o, string k) => Val(o, k) is double d ? d : 0;
        static int Int(Dictionary<string, object> o, string k, int def) => Val(o, k) is double d ? (int)Math.Round(d) : def;
    }

    /// <summary>A small JSON reader (objects to Dictionary, arrays to List, numbers to double); load time only.</summary>
    public static class HellMiniJson
    {
        public static object Parse(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            int i = 0;
            try { return Value(s, ref i); } catch (FormatException) { return null; } catch (IndexOutOfRangeException) { return null; }
        }

        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>(); i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Ws(s, ref i); string k = Str(s, ref i); Ws(s, ref i);
                    if (s[i++] != ':') throw new FormatException();
                    d[k] = Value(s, ref i); Ws(s, ref i);
                    char e = s[i++];
                    if (e == '}') return d;
                    if (e != ',') throw new FormatException();
                }
            }
            if (c == '[')
            {
                var l = new List<object>(); i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Value(s, ref i)); Ws(s, ref i);
                    char e = s[i++];
                    if (e == ']') return l;
                    if (e != ',') throw new FormatException();
                }
            }
            if (c == '"') return Str(s, ref i);
            if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int st = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (i == st) throw new FormatException();
            return double.Parse(s.Substring(st, i - st), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        static string Str(string s, ref int i)
        {
            if (s[i++] != '"') throw new FormatException();
            var b = new StringBuilder();
            while (true)
            {
                char c = s[i++];
                if (c == '"') return b.ToString();
                if (c != '\\') { b.Append(c); continue; }
                char e = s[i++];
                switch (e)
                {
                    case 'n': b.Append('\n'); break;
                    case 't': b.Append('\t'); break;
                    case 'r': b.Append('\r'); break;
                    case 'b': b.Append('\b'); break;
                    case 'f': b.Append('\f'); break;
                    case 'u': b.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber)); i += 4; break;
                    default: b.Append(e); break;
                }
            }
        }
    }

    // ------------------------------------------------------------------ the clock

    /// <summary>Bar-line maths on the audio clock (dspTime seconds).</summary>
    public static class HellMusicClock
    {
        /// <summary>The scheduler needs this much lead: a boundary closer than this is skipped for the next one.</summary>
        public const double MinLead = 0.05;

        /// <summary>The first grid line (start + n x unit, n &gt;= 0) at least <paramref name="lead"/> after <paramref name="now"/>.</summary>
        public static double NextLine(double start, double unit, double now, double lead = MinLead)
        {
            if (!(unit > 0)) return now + lead;
            double t = now + lead;
            if (t <= start) return start;
            double n = Math.Ceiling((t - start) / unit - 1e-9);
            return start + n * unit;
        }

        /// <summary>Where in the loop (seconds) the clock that started at <paramref name="start"/> is at <paramref name="now"/>.</summary>
        public static double LoopPosition(double start, double loop, double now)
        {
            if (!(loop > 0) || now <= start) return 0;
            double p = (now - start) % loop;
            return p < 0 ? p + loop : p;
        }

        /// <summary>A "stinger_into_cue" scheduled at <paramref name="stingerAt"/>: when the cue it hands over to starts (its clock),
        /// so the stinger's hit lands on that cue's first downbeat.</summary>
        public static double IntoCueStart(double stingerAt, int downbeatSample, int sampleRate) =>
            stingerAt + (sampleRate > 0 ? (double)Math.Max(0, downbeatSample) / sampleRate : 0);

        /// <summary>The sample a source must start from to join the clock at <paramref name="at"/> (for PlayScheduled + timeSamples).</summary>
        public static int JoinSample(double start, double loop, double at, int sampleRate) =>
            (int)Math.Round(LoopPosition(start, loop, at) * sampleRate);
    }

    // ------------------------------------------------------------------ fades

    /// <summary>One gain moving from <see cref="from"/> to <see cref="to"/> over [start, start + seconds] (dsp time), equal power.</summary>
    public struct HellMusicFade
    {
        public float from, to;
        public double start, seconds;

        public float Target => to;
        public static HellMusicFade Hold(float g) => new HellMusicFade { from = g, to = g };

        public float At(double now) => Gain(from, to, start, seconds, now);

        /// <summary>Restarts toward <paramref name="target"/> from wherever it is at <paramref name="at"/>. No-op when already aiming there.</summary>
        public void Retarget(float target, double at, double secs)
        {
            if (Math.Abs(target - to) < 1e-5f) return;
            from = At(at); to = target; start = at; seconds = secs;
        }

        public static float Gain(float from, float to, double start, double seconds, double now)
        {
            if (now <= start) return from;
            if (!(seconds > 0) || now >= start + seconds) return to;
            double x = (now - start) / seconds;
            if (to >= from) return from + (to - from) * (float)Math.Sin(x * Math.PI * 0.5);
            return to + (from - to) * (float)Math.Cos(x * Math.PI * 0.5);
        }
    }

    // ------------------------------------------------------------------ the in-run cell

    /// <summary>What the in-run music reads each frame.</summary>
    public struct HellMusicInputs
    {
        /// <summary>Enemies awake (alerted or attacking).</summary>
        public int enemiesAwake;
        /// <summary>A room objective is running (not done).</summary>
        public bool objectiveRunning;
        /// <summary>The objective is about to finish or fail (the Brand nearly charged, the Collapse's last stretch).</summary>
        public bool objectiveImminent;
        /// <summary>The player's health (plating + chassis) as a share, 0..1.</summary>
        public float health01;
        /// <summary>Seconds left on the hour (Baja's clock); &lt;0 = no clock.</summary>
        public float hourLeft;
        /// <summary>The player's horizontal speed (m/s).</summary>
        public float speed;
        /// <summary>Falling (a drop hole, a shaft): speed counts as Flying.</summary>
        public bool falling;
        /// <summary>The escape: Danger at least.</summary>
        public bool forceDanger;
    }

    /// <summary>
    /// Chooses the in-run cell (mood x speed) with hysteresis, on the rules of docs/HELL-MUSIC.md and the manifest: Danger at once
    /// (at the next bar), out of Danger only after the threat stayed low for <see cref="CalmBarsToLeaveDanger"/> bars; Normal to
    /// Quiet after the same calm; the speed smoothed over ~0.5 s with a band under each threshold.
    /// </summary>
    public sealed class HellMusicCellRules
    {
        public float fastMps = 11f, flyingMps = 18f, speedBand = 2f, speedSmoothing = 0.5f;
        public float dangerHealth = 0.35f, hourDangerSeconds = 60f;
        public const int CalmBarsToLeaveDanger = 2, CalmBarsToQuiet = 2;

        public HellMusicMood Mood { get; private set; } = HellMusicMood.Quiet;
        public HellMusicSpeed Speed { get; private set; } = HellMusicSpeed.Still;
        public float SmoothSpeed { get; private set; }
        double _calmFor;

        /// <summary>The raw mood the inputs ask for (no hysteresis).</summary>
        public HellMusicMood Wanted(in HellMusicInputs i)
        {
            if (i.forceDanger || i.objectiveImminent || i.health01 < dangerHealth || (i.hourLeft >= 0f && i.hourLeft < hourDangerSeconds))
                return HellMusicMood.Danger;
            if (i.enemiesAwake > 0 || i.objectiveRunning) return HellMusicMood.Normal;
            return HellMusicMood.Quiet;
        }

        public void Reset(HellMusicMood mood = HellMusicMood.Quiet, HellMusicSpeed speed = HellMusicSpeed.Still)
        {
            Mood = mood; Speed = speed; _calmFor = 0; SmoothSpeed = speed == HellMusicSpeed.Still ? 0f : speed == HellMusicSpeed.Fast ? fastMps : flyingMps;
        }

        /// <summary>Steps the rules by <paramref name="dt"/> (real seconds) with a bar of <paramref name="barSeconds"/>.</summary>
        public void Step(in HellMusicInputs i, float dt, double barSeconds)
        {
            var want = Wanted(i);
            if (want > Mood) { Mood = want; _calmFor = 0; }
            else if (want < Mood)
            {
                _calmFor += Math.Max(0f, dt);
                int bars = Mood == HellMusicMood.Danger ? CalmBarsToLeaveDanger : CalmBarsToQuiet;
                if (_calmFor >= bars * Math.Max(0.01, barSeconds)) { Mood = want; _calmFor = 0; }
            }
            else _calmFor = 0;

            float k = dt > 0f ? 1f - (float)Math.Exp(-dt / Math.Max(1e-3f, speedSmoothing)) : 0f;
            SmoothSpeed += (Math.Max(0f, i.speed) - SmoothSpeed) * k;
            Speed = i.falling ? HellMusicSpeed.Flying : SpeedFor(SmoothSpeed, Speed);
        }

        /// <summary>The speed tier with hysteresis: up at the threshold, down only <see cref="speedBand"/> under it.</summary>
        public HellMusicSpeed SpeedFor(float v, HellMusicSpeed current)
        {
            if (v >= flyingMps) return HellMusicSpeed.Flying;
            if (current == HellMusicSpeed.Flying && v >= flyingMps - speedBand) return HellMusicSpeed.Flying;
            if (v >= fastMps) return HellMusicSpeed.Fast;
            if (current >= HellMusicSpeed.Fast && v >= fastMps - speedBand) return HellMusicSpeed.Fast;
            return HellMusicSpeed.Still;
        }

        /// <summary>A stem's target gain for a cell: one mood on, speed additive (fast = still + fast), the drone 0 here (the
        /// Dark modifier lifts it).</summary>
        public static float StemTarget(HellMusicStem s, HellMusicMood mood, HellMusicSpeed speed)
        {
            if (s.role == HellMusicRole.Drone) return 0f;
            if (s.mood != (int)mood) return 0f;
            return s.speed >= 0 && s.speed <= (int)speed ? 1f : 0f;
        }

        /// <summary>Fade seconds for a mood change: into Danger 1 s, out of Danger 3 s, Quiet/Normal 2 s.</summary>
        public static double MoodFadeSeconds(HellMusicMood from, HellMusicMood to) =>
            to == HellMusicMood.Danger ? 1.0 : from == HellMusicMood.Danger ? 3.0 : 2.0;

        /// <summary>A speed stem fades in over 0.5 s (from the next beat) and out over 1.5 s.</summary>
        public const double SpeedInSeconds = 0.5, SpeedOutSeconds = 1.5;
    }

    // ------------------------------------------------------------------ modifiers

    public static class HellMusicMods
    {
        public const float OpenHz = 22000f;
        /// <summary>The pause menu: the game music under Baja's hold loop.</summary>
        public const float PauseCutoffHz = 600f, PauseDb = -14f;
        /// <summary>Time Dilation: the music is muffled toward this and pitched down by this share of the world's slow-down.</summary>
        public const float DilationCutoffHz = 700f, DilationPitchShare = 0.15f, DilationFullAt = 0.35f;

        /// <summary>How far in Time Dilation is (0..1) from the world pitch (1 = none, <see cref="DilationFullAt"/> = full).</summary>
        public static float DilationDepth(float worldPitch) => Clamp01((1f - worldPitch) / (1f - DilationFullAt));
        public static float DilationPitch(float worldPitch) => 1f - (1f - Clamp01(worldPitch)) * DilationPitchShare;
        public static float DilationCutoff(float worldPitch) => DarkHush.LogLerp(OpenHz, DilationCutoffHz, DilationDepth(worldPitch));

        /// <summary>The music bus low-pass: the lowest of the Dark modifier, Time Dilation and the pause muffle.</summary>
        public static float Cutoff(float darkDepth, float worldPitch, bool paused)
        {
            float c = Math.Min(DarkHush.MusicCutoff(darkDepth), DilationCutoff(worldPitch));
            return paused ? Math.Min(c, PauseCutoffHz) : c;
        }

        public static float Db(float db) => (float)Math.Pow(10.0, db / 20.0);
        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }

    // ------------------------------------------------------------------ stage to cue

    /// <summary>What the music is doing now, in one value (the director plays it).</summary>
    public enum HellMusicScene { Silence, Title, Opening, InRun, Boss, Decision, Escape, EndingCards, Credits, Stats }

    public struct HellMusicState
    {
        public HellFlowStage stage;
        public bool bossActive;
        /// <summary>The ending director's phase name (HellEndingDirector.Phase): Decision / AfterChoice / ToEscape / Escape / Out / Cards.</summary>
        public string endingPhase;
    }

    public static class HellMusicStageRules
    {
        public static HellMusicScene SceneFor(in HellMusicState s)
        {
            switch (s.stage)
            {
                case HellFlowStage.Title: return HellMusicScene.Title;
                case HellFlowStage.Opening: return HellMusicScene.Opening;
                case HellFlowStage.FloorIntro:
                case HellFlowStage.Playing: return HellMusicScene.InRun;
                case HellFlowStage.Boss: return HellMusicScene.Boss;
                case HellFlowStage.Ending:
                    switch (s.endingPhase)
                    {
                        case "Decision": case "AfterChoice": return HellMusicScene.Decision;
                        case "ToEscape": case "Escape": return HellMusicScene.Escape;
                        case "Out": case "Cards": case "Done": return HellMusicScene.EndingCards;
                        default: return s.bossActive ? HellMusicScene.Boss : HellMusicScene.Decision;
                    }
                case HellFlowStage.Credits: return HellMusicScene.Credits;
                case HellFlowStage.Stats: return HellMusicScene.Stats;
                default: return HellMusicScene.Silence;
            }
        }

        /// <summary>The cue a scene plays (before the fallback); InRun and Escape are the layered set.</summary>
        public static string CueFor(HellMusicScene scene, string endingId)
        {
            switch (scene)
            {
                case HellMusicScene.Title: return HellMusicManifest.Title;
                case HellMusicScene.Opening: return HellMusicManifest.Opening;
                case HellMusicScene.InRun: case HellMusicScene.Escape: return HellMusicManifest.InRun;
                case HellMusicScene.Boss: return HellMusicManifest.Boss;
                case HellMusicScene.Decision: return HellMusicManifest.Decision;
                case HellMusicScene.EndingCards: return EndingCue(endingId);
                case HellMusicScene.Credits: return HellMusicManifest.Credits;
                case HellMusicScene.Stats: return HellMusicManifest.Stats;
                default: return null;
            }
        }

        public static string EndingCue(string endingId)
        {
            switch (endingId)
            {
                case HellEndings.Kill: case HellEndings.KillLucifer: return HellMusicManifest.EndingKill;
                case HellEndings.SpareKept: case HellEndings.SpareLucifer: return HellMusicManifest.EndingSpareKept;
                case HellEndings.SpareCost: case HellEndings.Damned: return HellMusicManifest.EndingSpareCost;
                default: return HellMusicManifest.Stats;
            }
        }

        /// <summary>Where a cue falls back to while it is pending: the boss to the in-run set (Danger), an ending to the stats
        /// loop, the credits to the title loop, the stats to the title loop; null = silence.</summary>
        public static string Fallback(string cue)
        {
            switch (cue)
            {
                case HellMusicManifest.Boss: return HellMusicManifest.InRun;
                case HellMusicManifest.EndingKill: case HellMusicManifest.EndingSpareKept: case HellMusicManifest.EndingSpareCost:
                    return HellMusicManifest.Stats;
                case HellMusicManifest.Credits: case HellMusicManifest.Stats: return HellMusicManifest.Title;
                case HellMusicManifest.Decision: return null;
                default: return null;
            }
        }

        /// <summary>The first ready cue along <paramref name="cue"/>'s fallback chain (null: silence).</summary>
        public static string Resolve(HellMusicManifest m, string cue)
        {
            for (int n = 0; cue != null && n < 6; n++)
            {
                if (m != null && m.Ready(cue) != null) return cue;
                cue = Fallback(cue);
            }
            return null;
        }

        /// <summary>The opening's hand-over part: a "stinger_into_cue" part named "fall" (null when the opening has none).</summary>
        public static HellMusicStem OpeningFall(HellMusicManifest m)
        {
            var p = m != null ? m.Ready(HellMusicManifest.Opening)?.Part("fall") : null;
            return p != null && p.kind == "stinger_into_cue" && p.resource != null ? p : null;
        }

        /// <summary>The floor card stinger of a 0-based floor.</summary>
        public static string FloorCard(int floor) => floor == 0 ? "floor_card_1" : floor == 1 ? "floor_card_2" : floor == 2 ? "floor_card_3" : null;

        /// <summary>The boss part names of a Vesper phase (1..3): the bed and the drums ("p3" has no drums by design).</summary>
        public static string BossBed(int phase) => phase <= 1 ? "p1_bed" : phase == 2 ? "p2_bed" : "p3_bed";
        public static string BossDrums(int phase) => phase <= 1 ? "p1_drums" : phase == 2 ? "p2_drums" : "p3_drums";
    }
}
