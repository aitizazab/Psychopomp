using System;

namespace JamKit.Skyline
{
    /// <summary>Where the game is, title to title (docs/HELL-FLOW.md).</summary>
    public enum HellFlowStage
    {
        /// <summary>The title screen: Start, Settings, Quit.</summary>
        Title,
        /// <summary>The God of Death's premise, as text cards (skippable).</summary>
        Opening,
        /// <summary>A floor's intro card (the game holds still under it).</summary>
        FloorIntro,
        /// <summary>The rooms (and the shafts and the skill picks between floors).</summary>
        Playing,
        /// <summary>The last floor's shaft is behind: the boss lane runs Vesper's fight.</summary>
        Boss,
        /// <summary>The boss is down: the ending lane runs the decision and the escape.</summary>
        Ending,
        Credits,
        /// <summary>The stats screen; it goes back to the title.</summary>
        Stats,
        /// <summary>Quit from the title.</summary>
        Quit,
    }

    /// <summary>The endings' ids (<see cref="HellFlow.ReachEnding"/>) and their names on the stats screen. Draft names: the
    /// creator names them.</summary>
    public static class HellEndings
    {
        /// <summary>Vesper killed, her soul delivered.</summary>
        public const string Kill = "kill";
        /// <summary>Vesper spared, with most of the God of Death's jobs done: she lets it go.</summary>
        public const string SpareKept = "spare-kept";
        /// <summary>Vesper spared with few jobs done: you pay for it.</summary>
        public const string SpareCost = "spare-cost";
        /// <summary>The boss fell but no ending scene ran (the ending lane isn't in yet).</summary>
        public const string BossOnly = "boss-defeated";
        /// <summary>The last shaft with no boss in the build.</summary>
        public const string Bottom = "bottom";

        public static string Title(string id)
        {
            switch (id)
            {
                case Kill: return "THE SOUL DELIVERED";
                case SpareKept: return "THE EVENING STAR SPARED";
                case SpareCost: return "SPARED, AT A PRICE";
                case BossOnly: return "VESPER FELL";
                case Bottom: return "THE BOTTOM OF HELL";
                case null: case "": return "NO ENDING";
                default: return id.ToUpperInvariant();
            }
        }
    }

    /// <summary>
    /// The game's flow, title to title (pure; docs/HELL-FLOW.md): Title, then <see cref="StartGame"/> the Opening, then
    /// <see cref="OpeningDone"/> starts the <see cref="HellRun"/>; every floor's first room shows a FloorIntro until
    /// <see cref="FloorIntroDone"/>, then Playing. The run's end (the last floor's shaft) asks <see cref="BossHook"/>: true =
    /// a boss fight began (Boss, until <see cref="BossDefeated"/>), false = no boss in the build. After the boss (or without
    /// one) it asks <see cref="EndingHook"/>: true = the ending scene runs (Ending, until <see cref="ReachEnding"/>), false =
    /// straight to the credits with a fallback ending. Then Credits, <see cref="CreditsDone"/> the Stats screen,
    /// <see cref="StatsDone"/> the Title again. Every room entry, shaft, death and stage change past the title raises
    /// <see cref="RewindBoundary"/>: a Rewind never reaches back across one (its history clears). Out-of-order calls change
    /// nothing and return false.
    /// </summary>
    public sealed class HellFlow
    {
        readonly Func<HellRun> _makeRun;

        public HellFlowStage Stage { get; private set; } = HellFlowStage.Title;
        public HellRun Run { get; private set; }
        public HellRunStats Stats { get; private set; }
        /// <summary>How this run began: <see cref="FullGame"/> (Start: the opening, then floor 1), 0-2 a floor's chapter, or
        /// <see cref="VesperChapter"/>.</summary>
        public int Chapter { get; private set; } = FullGame;
        public const int FullGame = -1, VesperChapter = 3, Chapters = 4;

        /// <summary>The floor whose intro card is showing (or last showed).</summary>
        public int IntroFloor { get; private set; } = -1;

        /// <summary>Asked when the last floor's shaft is reached (the stage is already Boss): true when a boss fight began.</summary>
        public Func<bool> BossHook;
        /// <summary>Asked after the boss (the stage is already Ending): true when an ending scene runs and will call
        /// <see cref="ReachEnding"/>.</summary>
        public Func<bool> EndingHook;

        /// <summary>(from, to).</summary>
        public event Action<HellFlowStage, HellFlowStage> StageChanged;
        /// <summary>A Rewind's history must not reach back past now.</summary>
        public event Action RewindBoundary;

        public HellFlow(Func<HellRun> makeRun) { _makeRun = makeRun ?? throw new ArgumentNullException(nameof(makeRun)); }

        /// <summary>A screen covers the game (it holds still).</summary>
        public bool ShowsScreen => Stage == HellFlowStage.Title || Stage == HellFlowStage.Opening || Stage == HellFlowStage.FloorIntro
            || Stage == HellFlowStage.Credits || Stage == HellFlowStage.Stats;

        /// <summary>Title: Start. A new run and new stats (<see cref="HellRunStats.Current"/>).</summary>
        public bool StartGame()
        {
            if (Stage != HellFlowStage.Title) return false;
            NewRun(FullGame);
            Go(HellFlowStage.Opening);
            return true;
        }

        /// <summary>Title: a chapter start (judges play a few minutes). 0-2: that floor's first room, without the opening;
        /// <see cref="VesperChapter"/>: straight to the boss hook. (The caller grants the floor picks first.)</summary>
        public bool StartChapter(int chapter)
        {
            if (Stage != HellFlowStage.Title || chapter < 0 || chapter >= Chapters) return false;
            NewRun(chapter);
            Go(HellFlowStage.Playing);
            if (chapter == VesperChapter) Run.SkipToEnd();     // raises Finished: the boss hook
            else Run.StartAt(chapter);                          // raises RoomEntered(chapter, 0): the floor card
            return true;
        }

        void NewRun(int chapter)
        {
            Chapter = chapter;
            Run = _makeRun();
            Run.RoomEntered += OnRoomEntered;
            Run.Finished += OnRunFinished;
            Stats = new HellRunStats();
            HellRunStats.Current = Stats;
            IntroFloor = -1;
        }

        public bool Quit()
        {
            if (Stage != HellFlowStage.Title) return false;
            Go(HellFlowStage.Quit);
            return true;
        }

        /// <summary>The opening's last card (or a skip): the run starts, and the first floor's card shows.</summary>
        public bool OpeningDone()
        {
            if (Stage != HellFlowStage.Opening) return false;
            Go(HellFlowStage.Playing);
            Run.Start();                     // raises RoomEntered(0, 0): the floor card
            return true;
        }

        public bool FloorIntroDone()
        {
            if (Stage != HellFlowStage.FloorIntro) return false;
            Go(HellFlowStage.Playing);
            return true;
        }

        /// <summary>A death in a room (the run counts it; the room restarts).</summary>
        public bool Died()
        {
            if (Stage != HellFlowStage.Playing || Run == null || !Run.Died()) return false;
            Stats.RecordDeath();
            Boundary();
            return true;
        }

        /// <summary>The player went down a floor's shaft.</summary>
        public void EnteredShaft() { if (Stage == HellFlowStage.Playing) Boundary(); }

        /// <summary>Real seconds of play (not paused, not under a screen): the floor's or the boss's time.</summary>
        public void Tick(float realSeconds)
        {
            if (Stats == null) return;
            if (Stage == HellFlowStage.Playing && Run != null) Stats.TickFloor(Run.Floor, realSeconds);
            else if (Stage == HellFlowStage.Boss) Stats.TickBoss(realSeconds);
            else if (Stage == HellFlowStage.Ending) Stats.TickEnding(realSeconds);
        }

        /// <summary>The boss lane: Vesper is beaten.</summary>
        public bool BossDefeated()
        {
            if (Stage != HellFlowStage.Boss) return false;
            AfterBoss(HellEndings.BossOnly);
            return true;
        }

        /// <summary>The ending lane (or the fallback): the ending reached; the credits roll.</summary>
        public bool ReachEnding(string endingId)
        {
            if (Stage != HellFlowStage.Boss && Stage != HellFlowStage.Ending) return false;
            Stats.RecordEnding(endingId);
            Go(HellFlowStage.Credits);
            return true;
        }

        public bool CreditsDone()
        {
            if (Stage != HellFlowStage.Credits) return false;
            Go(HellFlowStage.Stats);
            return true;
        }

        /// <summary>The stats screen closed: back to the title (the run is over).</summary>
        public bool StatsDone()
        {
            if (Stage != HellFlowStage.Stats) return false;
            if (Run != null)
            {
                Run.RoomEntered -= OnRoomEntered;
                Run.Finished -= OnRunFinished;
            }
            Run = null;
            Go(HellFlowStage.Title);
            return true;
        }

        // ------------------------------------------------------------------ the run's events

        void OnRoomEntered(int floor, int room)
        {
            Boundary();
            if (room != 0 || Stage != HellFlowStage.Playing) return;
            IntroFloor = floor;
            Go(HellFlowStage.FloorIntro);
        }

        void OnRunFinished()
        {
            if (Stage != HellFlowStage.Playing) return;
            Go(HellFlowStage.Boss);
            bool fight = BossHook != null && BossHook();
            if (!fight && Stage == HellFlowStage.Boss) AfterBoss(HellEndings.Bottom);
        }

        void AfterBoss(string fallback)
        {
            Go(HellFlowStage.Ending);
            bool scene = EndingHook != null && EndingHook();
            if (!scene && Stage == HellFlowStage.Ending) ReachEnding(fallback);
        }

        void Go(HellFlowStage to)
        {
            var from = Stage;
            Stage = to;
            if (to != HellFlowStage.Title && to != HellFlowStage.Quit) Boundary();
            StageChanged?.Invoke(from, to);
        }

        void Boundary() => RewindBoundary?.Invoke();
    }
}
