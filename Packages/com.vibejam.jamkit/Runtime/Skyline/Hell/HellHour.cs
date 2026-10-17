using System;

namespace JamKit.Skyline
{
    // =====================================================================================================================
    // BAJA'S HOUR (docs/HELL-HOUR.md): the run's time budget. Baja set up your way out of Hell; spend too long down there and
    // it closes, and you and your sister are damned. Only gameplay time counts. It is shown in the world (an ember hourglass,
    // Baja's reminders, the exit gate of the final escape), never as a number until the stats. Pure (no engine); the Unity
    // side is Scripts/Hell/HellHourDirector.cs (the clock, the lines, the gate) and Scripts/Hud/HellHourglass.cs (the HUD).
    // =====================================================================================================================

    /// <summary>The game's difficulty (the title's Start, the pause menu; saved in <see cref="SkylineOptions.difficulty"/>).
    /// For now it sets only the length of Baja's hour.</summary>
    public enum HellDifficulty { Easy = 0, Normal = 1, Hard = 2 }

    /// <summary>One frame of the game, as the hour sees it: does this frame's time count?</summary>
    public struct HellHourMoment
    {
        public HellFlowStage stage;
        /// <summary>The player's input is paused: the title's screens, the skill pick, the ending's cards (they all set
        /// <c>SkylineDeviceInput.Paused</c>).</summary>
        public bool paused;
        /// <summary>The pause menu is open.</summary>
        public bool menu;
        /// <summary>A flow screen (title, opening card, floor card, credits, stats) covers the game.</summary>
        public bool screen;
        /// <summary>Between floors, waiting on the skill pick.</summary>
        public bool pick;
        /// <summary>A story beat holds the world (its letterbox, input paused, the world slowed).</summary>
        public bool beatHold;
        /// <summary>The ending's scripted moments: the lines after the choice, the fades, the cards (not the decision itself and
        /// not the escape: those are play).</summary>
        public bool cutscene;
        /// <summary>The game's time scale (Time Dilation, a hit stop). Ignored: the hour runs on real seconds.</summary>
        public float timeScale;

        /// <summary>A plain moment of play.</summary>
        public static HellHourMoment Play(HellFlowStage stage = HellFlowStage.Playing) => new HellHourMoment { stage = stage, timeScale = 1f };
    }

    /// <summary>The hour's rules: the budget per difficulty, what counts, the chapters' head start, the reminders.</summary>
    public static class HellHourRules
    {
        /// <summary>The budget in minutes: Easy 90, Normal 65, Hard 50 (the creator, 2026-10-18; a skilled run takes about 45
        /// at most).</summary>
        public static float BudgetMinutes(HellDifficulty d)
        {
            switch (d)
            {
                case HellDifficulty.Easy: return 90f;
                case HellDifficulty.Hard: return 50f;
                default: return 65f;
            }
        }

        public static float BudgetSeconds(HellDifficulty d) => BudgetMinutes(d) * 60f;

        /// <summary>A stored difficulty number back to the enum (anything unknown is Normal).</summary>
        public static HellDifficulty FromInt(int v) => v >= 0 && v <= 2 ? (HellDifficulty)v : HellDifficulty.Normal;

        /// <summary>The menus' words.</summary>
        public static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard" };

        /// <summary>
        /// Does this frame count? Only play: the rooms, the shafts, the boss, the decision and the escape. Not the title or any
        /// flow screen, not the pause menu, not the skill pick, not a story beat's held moment, not the ending's scripted lines,
        /// fades and cards. The time scale doesn't matter (Time Dilation slows the world, not Baja's clock: the caller passes
        /// real, unscaled seconds).
        /// </summary>
        public static bool Counts(in HellHourMoment m)
        {
            if (m.stage != HellFlowStage.Playing && m.stage != HellFlowStage.Boss && m.stage != HellFlowStage.Ending) return false;
            return !m.paused && !m.menu && !m.screen && !m.pick && !m.beatHold && !m.cutscene;
        }

        /// <summary>A chapter starts with this share of the hour already spent: the full game and Floor 1 none, Floor 2 a
        /// third, Floor 3 two thirds, Vesper most of it (0.85).</summary>
        public static float ChapterSpentShare(int chapter)
        {
            switch (chapter)
            {
                case 1: return 1f / 3f;
                case 2: return 2f / 3f;
                case HellFlow.VesperChapter: return 0.85f;
                default: return 0f;
            }
        }

        /// <summary>Baja's reminders: the share of the hour spent at which each is said, and its line's key (lines in
        /// <see cref="HellHourLines"/>). The last one is the hour running out.</summary>
        public static readonly float[] ReminderAt = { 0.25f, 0.5f, 0.75f, 0.9f, 1f };
        public static readonly string[] ReminderKey = { "hour.quarter", "hour.half", "hour.three-quarters", "hour.last", "hour.out" };
    }

    /// <summary>
    /// The hour itself: how much of it is spent. <see cref="Tick"/> adds real seconds when the moment counts. It is NOT a
    /// rewindable: the Rewind undoes the world, not Baja's clock (so nothing here snapshots or restores). Overtime is
    /// allowed: <see cref="Left"/> goes negative and the run goes on (the escape then starts with the gate nearly shut).
    /// <see cref="Stop"/> freezes it (the escape is over).
    /// </summary>
    public sealed class HellHour
    {
        static HellHour _current;

        /// <summary>The run in progress (null before the first run starts).</summary>
        public static HellHour Current
        {
            get => _current;
            set => _current = value;
        }

        HellDifficulty _difficulty;
        float _spent;
        bool _stopped;
        int _reminders;     // how many of HellHourRules.ReminderAt have been passed

        public HellHour(HellDifficulty difficulty, int chapter = HellFlow.FullGame)
        {
            _difficulty = difficulty;
            Chapter = chapter;
            _spent = HellHourRules.ChapterSpentShare(chapter) * HellHourRules.BudgetSeconds(difficulty);
            SkipPassedReminders();
        }

        public int Chapter { get; private set; }
        public HellDifficulty Difficulty => _difficulty;
        public float BudgetSeconds => HellHourRules.BudgetSeconds(_difficulty);
        /// <summary>Seconds of the hour spent (a chapter's head start included).</summary>
        public float Spent => _spent;
        /// <summary>Seconds left (negative: overtime, the time owed).</summary>
        public float Left => BudgetSeconds - _spent;
        public bool Overtime => Left < 0f;
        /// <summary>The share left, 0..1 (the hourglass's top bulb).</summary>
        public float LeftShare
        {
            get
            {
                float b = BudgetSeconds;
                if (!(b > 0f)) return 0f;
                float s = Left / b;
                return s < 0f ? 0f : s > 1f ? 1f : s;
            }
        }
        public bool Stopped => _stopped;

        /// <summary>The difficulty changed mid-run (the pause menu): the budget follows it; the time spent stays.</summary>
        public void SetDifficulty(HellDifficulty d)
        {
            if (d == _difficulty) return;
            _difficulty = d;
            // reminders follow the new budget: anything now behind us is passed silently, anything ahead is said again
            _reminders = 0;
            SkipPassedReminders();
        }

        /// <summary>Adds <paramref name="realSeconds"/> (unscaled) when the moment counts. True when it counted.</summary>
        public bool Tick(float realSeconds, in HellHourMoment m)
        {
            if (_stopped || !(realSeconds > 0f) || !HellHourRules.Counts(m)) return false;
            _spent += realSeconds;
            return true;
        }

        /// <summary>The hour stops (you reached the exit, or the gate closed): nothing counts after.</summary>
        public void Stop() => _stopped = true;

        /// <summary>The newest reminder crossed since the last call (its line's key), or null. Skipped ones (crossed in the same
        /// call) are passed silently: Baja says only the latest.</summary>
        public string TakeReminder()
        {
            string key = null;
            float share = BudgetSeconds > 0f ? _spent / BudgetSeconds : 1f;
            while (_reminders < HellHourRules.ReminderAt.Length && share >= HellHourRules.ReminderAt[_reminders] - 1e-6f)
            {
                key = HellHourRules.ReminderKey[_reminders];
                _reminders++;
            }
            return key;
        }

        void SkipPassedReminders()
        {
            float share = BudgetSeconds > 0f ? _spent / BudgetSeconds : 1f;
            while (_reminders < HellHourRules.ReminderAt.Length && share >= HellHourRules.ReminderAt[_reminders] - 1e-6f) _reminders++;
        }

        /// <summary>m:ss of a number of seconds (its absolute value), for the stats screen.</summary>
        public static string Clock(float seconds)
        {
            float s = Math.Abs(seconds);
            if (float.IsNaN(s) || float.IsInfinity(s)) s = 0f;
            int whole = (int)Math.Floor(s);
            if (whole >= 3600) return (whole / 3600) + ":" + (whole / 60 % 60).ToString("00") + ":" + (whole % 60).ToString("00");
            return (whole / 60) + ":" + (whole % 60).ToString("00");
        }
    }

    /// <summary>How open the final escape's exit gate is in each state (<see cref="HellHourGate.Describe"/>).</summary>
    public enum HellGateLook { WideOpen, Closing, NearlyShut }

    /// <summary>
    /// The window closing, felt in the escape (docs/HELL-HOUR.md): the time left on Baja's hour when an escape attempt starts
    /// sets how long the exit gate stays open. <see cref="Window"/> = <see cref="MinWindow"/> + <see cref="SecondsPerMinuteLeft"/>
    /// for every minute left (none when overtime). The gate is drawn open by <see cref="Openness"/>: fully open while more
    /// than <see cref="VisualSpan"/> seconds remain, closing linearly to shut at the window's end. If it shuts before you
    /// reach it, the run ends in damnation. A death restarts the attempt with a new window from the hour then (it kept
    /// running), never less than <see cref="MinWindow"/>, so a run that is out of time still gets a real, tight chance: the
    /// floor-3 Collapse's fast lane takes about 11-12 s, its safe lane about 16-17 s.
    /// </summary>
    public static class HellHourGate
    {
        /// <summary>The shortest window (overtime, or nothing left): only the fast lines make it.</summary>
        public const float MinWindow = 13f;
        /// <summary>Seconds of window per minute left on the hour.</summary>
        public const float SecondsPerMinuteLeft = 3f;
        /// <summary>The gate looks fully open while at least this many seconds of its window remain.</summary>
        public const float VisualSpan = 40f;

        /// <summary>Seconds from an attempt's start until the gate shuts.</summary>
        public static float Window(float hourLeftSeconds)
        {
            float left = hourLeftSeconds > 0f ? hourLeftSeconds : 0f;
            if (float.IsInfinity(left)) return float.PositiveInfinity;
            return MinWindow + left / 60f * SecondsPerMinuteLeft;
        }

        /// <summary>0 shut .. 1 wide open, <paramref name="t"/> seconds into an attempt with this window.</summary>
        public static float Openness(float window, float t)
        {
            if (float.IsInfinity(window)) return 1f;
            float o = (window - t) / VisualSpan;
            return o < 0f ? 0f : o > 1f ? 1f : o;
        }

        public static bool Shut(float window, float t) => !float.IsInfinity(window) && t >= window;

        /// <summary>How the gate looks when an attempt starts with this much of the hour left.</summary>
        public static HellGateLook Describe(float hourLeftSeconds)
        {
            float o = Openness(Window(hourLeftSeconds), 0f);
            if (o >= 0.999f) return HellGateLook.WideOpen;
            return Window(hourLeftSeconds) < 20f ? HellGateLook.NearlyShut : HellGateLook.Closing;
        }
    }

    /// <summary>
    /// The endings the hour adds (docs/HELL-HOUR.md, docs/HELL-ENDING.md): <b>damnation</b> (the gate shut before you reached
    /// it: whatever you chose, you and your sister stay) over everything, and the <b>Lucifer cliffhanger</b> on top of the kill
    /// or the favoured spare: every job offered was done (or the Vesper chapter's answer was yes) and you reached the exit
    /// with time left on the hour (more than <see cref="LuciferMinLeftSeconds"/>). Then Baja's line sends you after Lucifer
    /// and a card ends the game there, before the fight.
    /// </summary>
    public static class HellHourEndings
    {
        /// <summary>The hour left at the exit for the Lucifer ending: any (above 0: not overtime).</summary>
        public const float LuciferMinLeftSeconds = 0f;

        /// <summary>Every job offered was done (at least one offered), or the Vesper chapter's answer stands in.</summary>
        public static bool AllJobs(int jobsOffered, int jobsDone, bool? assumedErrands) =>
            assumedErrands ?? (jobsOffered > 0 && jobsDone >= jobsOffered);

        /// <summary>The ending: <see cref="HellEndings.Damned"/> when the gate shut; else the choice's ending
        /// (<see cref="HellEndingRules.Pick(bool, int, int, bool?)"/>), lifted to its Lucifer form when every job was done and
        /// the hour wasn't out at the exit (<paramref name="hourLeftAtExit"/>; +infinity when no hour runs, e.g. a test bed).</summary>
        public static string Pick(bool killed, int jobsOffered, int jobsDone, bool? assumedErrands, bool damned, float hourLeftAtExit)
        {
            if (damned) return HellEndings.Damned;
            string b = HellEndingRules.Pick(killed, jobsOffered, jobsDone, assumedErrands);
            if (b == HellEndings.SpareCost) return b;
            if (!AllJobs(jobsOffered, jobsDone, assumedErrands) || !(hourLeftAtExit > LuciferMinLeftSeconds)) return b;
            return b == HellEndings.Kill ? HellEndings.KillLucifer : HellEndings.SpareLucifer;
        }

        /// <summary>The ending's card sequence: the base ending's, then for the Lucifer forms Baja's two lines and the cliffhanger
        /// card; damnation has its own.</summary>
        public static string[] Sequence(string endingId)
        {
            switch (endingId)
            {
                case HellEndings.Damned: return new[] { "end-damned-01", "end-damned-02", "end-damned-card" };
                case HellEndings.KillLucifer: return Join(HellEndingRules.Sequence(HellEndings.Kill), Lucifer);
                case HellEndings.SpareLucifer: return Join(HellEndingRules.Sequence(HellEndings.SpareKept), Lucifer);
                default: return HellEndingRules.Sequence(endingId);
            }
        }

        static readonly string[] Lucifer = { "end-lucifer-01", "end-lucifer-02", "end-lucifer-card" };

        static string[] Join(string[] a, string[] b)
        {
            var o = new string[a.Length + b.Length];
            Array.Copy(a, o, a.Length);
            Array.Copy(b, 0, o, a.Length, b.Length);
            return o;
        }
    }

    /// <summary>Baja's reminders (keys <c>hour.*</c>; voice files <c>Resources/SkylineAudio/hell-voice/god/&lt;key&gt;.wav</c>
    /// when present). ALL PLACEHOLDERS: the creator rewrites them.</summary>
    public static class HellHourLines
    {
        public const string Speaker = "god";

        public static readonly HellLine[] All =
        {
            new HellLine("hour.quarter", Speaker, "[PLACEHOLDER] Tick tock. Your sister's not getting any less dead."),
            new HellLine("hour.half", Speaker, "[PLACEHOLDER] Half my hour, gone. I do hope the sightseeing was worth it."),
            new HellLine("hour.three-quarters", Speaker, "[PLACEHOLDER] The door I'm holding open is getting heavy, darling."),
            new HellLine("hour.last", Speaker, "[PLACEHOLDER] Hear that grinding? That's your way out, closing. Hurry."),
            new HellLine("hour.out", Speaker, "[PLACEHOLDER] That's my hour. The door's nearly shut. Run anyway. I love a long shot."),
            new HellLine("hour.rewind", Speaker, "[PLACEHOLDER] Cute trick. It doesn't work on MY clock."),
            new HellLine("hour.gate-shut", Speaker, "[PLACEHOLDER] Too late. I did say."),
        };

        public static HellLine Get(string key)
        {
            if (key == null) return null;
            foreach (var l in All) if (l.key == key) return l;
            return null;
        }

        /// <summary>The title's difficulty question (placeholder words; no numbers: the hour is felt, not read).</summary>
        public const string DifficultyTitle = "BAJA'S HOUR",
            DifficultyBody = "[PLACEHOLDER] Baja holds your way out open for an hour. Hers, not yours. How patient is she today?";
        public static readonly string[] DifficultyChoices =
        {
            "EASY: SHE'S IN A GENEROUS MOOD",
            "NORMAL: SHE HAS A LUNCH",
            "HARD: SHE'S ALREADY BORED",
        };

        /// <summary>The stats screen's cell: the label and the value (time left, or the time owed in overtime).</summary>
        public static string StatsLabel(HellHour h) => h != null && h.Overtime ? "TIME OWED" : "BAJA'S HOUR LEFT";
        public static string StatsValue(HellHour h) => h == null ? "-" : HellHour.Clock(h.Left);
    }
}
