using System;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The run's events: the contract between the run (<see cref="HellRunDirector"/>) and the other lanes (docs/HELL-FLOW.md,
    /// "Contracts").
    /// <list type="bullet">
    /// <item><b>Skill pick:</b> at the bottom of a floor's shaft the run raises <see cref="FloorCleared"/> (0-based floor; not
    /// for the last floor) and waits for <see cref="ContinueAfterPick"/>. With nobody subscribed it continues at once.</item>
    /// <item><b>Boss:</b> after the last floor's shaft the run raises <see cref="FinalBossRequested"/> with an onDefeated action
    /// (= <see cref="BossDefeated"/>). A listener starts the fight and calls it when Vesper is beaten. No listener: straight on to the ending.</item>
    /// <item><b>Ending:</b> after the boss the run raises <see cref="EndingRequested"/>. A listener runs the decision scene and
    /// the escape, then calls <see cref="RaiseEnding"/> with a <see cref="HellEndings"/> id: the credits, the stats, the
    /// title. No listener: the credits roll at once (ending <see cref="HellEndings.BossOnly"/>).</item>
    /// <item><b>Stats:</b> <see cref="Stats"/> (= <see cref="HellRunStats.Current"/>): <c>Stats.JobsDone</c> / <c>JobsOffered</c>
    /// for the endings. The jobs lane calls <see cref="RaiseJobOffered"/>, <see cref="RaiseJobDone"/>, <see cref="RaiseJobFailed"/>.</item>
    /// <item><b>To listen to:</b> <see cref="FloorStarted"/>, <see cref="RoomEntered"/>, <see cref="RoomObjectiveDone"/>,
    /// <see cref="PlayerDied"/>, <see cref="RewindUsed"/>, <see cref="ShadowStepUsed"/>, the job events, <see cref="StageChanged"/>,
    /// <see cref="ChapterStarted"/>.</item>
    /// <item><b>Rewind:</b> <see cref="RewindBoundary"/> clears the Rewind's history (room entries, shafts, deaths, screens).
    /// A lane that moves the player somewhere new (the boss arena, the escape) calls <see cref="RaiseRewindBoundary"/>.</item>
    /// </list>
    /// </summary>
    public static class HellRunEvents
    {
        public static event Action<int> FloorCleared;
        /// <summary>A room was entered: (floor, room in the floor 0..3, its kind's name).</summary>
        public static event Action<int, int, string> RoomEntered;
        /// <summary>The player died (or sank in The Collapse) and the room restarted from its entry.</summary>
        public static event Action RoomRestarted;
        /// <summary>The run began (after the opening; the first floor's card is up).</summary>
        public static event Action RunStarted;
        /// <summary>The last floor's shaft was reached (the rooms are done; the boss comes next).</summary>
        public static event Action RunFinished;
        /// <summary>The flow's stage changed: (from, to).</summary>
        public static event Action<HellFlowStage, HellFlowStage> StageChanged;
        /// <summary>The Rewind must not reach back past now (its history clears).</summary>
        public static event Action RewindBoundary;

        /// <summary>The boss lane: start Vesper's fight (move the player into the arena) and call the given action (it is
        /// <see cref="BossDefeated"/>) when she is beaten: <c>HellVesperFight.RequestFinalBoss(onDefeated)</c>.</summary>
        public static event Action<Action> FinalBossRequested;
        /// <summary>Raised when <see cref="BossDefeated"/> is called (the run director listens).</summary>
        public static event Action FinalBossDefeated;
        /// <summary>The ending lane: run the decision and the escape, then call <see cref="RaiseEnding"/>.</summary>
        public static event Action EndingRequested;
        /// <summary>Raised by <see cref="RaiseEnding"/> with the ending's id (the run director listens: credits, stats, title).</summary>
        public static event Action<string> EndingReached;

        /// <summary>A floor began (its card shows): 0-based floor. Also for a chapter start.</summary>
        public static event Action<int> FloorStarted;
        /// <summary>A room's objective was done (its exit opened): (floor, the room's kind, the room's clock in seconds). A Rewind
        /// can undo it, and it then fires again when redone.</summary>
        public static event Action<int, string, float> RoomObjectiveDone;
        /// <summary>The player died: (floor, where: the room's kind, "shaft", "boss" or "ending"). Raised in every stage.</summary>
        public static event Action<int, string> PlayerDied;
        /// <summary>A Rewind was used (after everything was restored).</summary>
        public static event Action RewindUsed;
        /// <summary>A Shadow Step was taken: (from, to), the feet.</summary>
        public static event Action<Vector3, Vector3> ShadowStepUsed;
        /// <summary>The jobs lane's job events (job id). The run's stats count them; the jobs lane calls the Raise* methods.</summary>
        public static event Action<string> JobOffered, JobDone, JobFailed;
        /// <summary>A start from the title: -1 the full game (with the opening), 0-2 a floor's chapter, 3 Vesper
        /// (<see cref="HellFlow.Chapter"/>). A chapter's skill picks are granted before it, through <see cref="FloorCleared"/>.</summary>
        public static event Action<int> ChapterStarted;

        /// <summary>The comic player turned to a page: (sequence id, 0-based page). The opening's id is
        /// <see cref="HellComics.OpeningId"/>; the endings' are "ending-" + the ending id (docs/HELL-COMIC.md).</summary>
        public static event Action<string, int> ComicPage;
        /// <summary>The opening's comic turned to a page (0-based): the music director follows it (sister, hall, wait, idea).</summary>
        public static event Action<int> OpeningPage;
        /// <summary>A comic sequence ended (played through or skipped): its id.</summary>
        public static event Action<string> ComicFinished;

        /// <summary>True between <see cref="FloorCleared"/> and <see cref="ContinueAfterPick"/>.</summary>
        public static bool WaitingForPick { get; private set; }

        /// <summary>The run's counters (null before the first Start). Record* into it from anywhere.</summary>
        public static HellRunStats Stats => HellRunStats.Current;

        /// <summary>The flow's stage now (Title before the director runs).</summary>
        public static HellFlowStage Stage { get; private set; }

        /// <summary>The skill pick closed: the run goes on to the next floor.</summary>
        public static void ContinueAfterPick() { WaitingForPick = false; }

        /// <summary>Raised by the run director; returns false when nobody listens (no pick to wait for).</summary>
        public static bool RaiseFloorCleared(int floor)
        {
            var h = FloorCleared;
            if (h == null) { WaitingForPick = false; return false; }
            WaitingForPick = true;
            h(floor);
            return true;
        }

        /// <summary>Raised by the run director; false when nobody listens (no boss in the build).</summary>
        public static bool RaiseFinalBossRequested()
        {
            var h = FinalBossRequested;
            if (h == null) return false;
            h(BossDefeated);
            return true;
        }

        /// <summary>The boss lane: Vesper is beaten.</summary>
        public static void BossDefeated() => FinalBossDefeated?.Invoke();

        /// <summary>Raised by the run director; false when nobody listens (no ending scene in the build).</summary>
        public static bool RaiseEndingRequested()
        {
            var h = EndingRequested;
            if (h == null) return false;
            h();
            return true;
        }

        /// <summary>The ending lane: the ending reached (<see cref="HellEndings"/> ids): the credits roll, then the stats,
        /// then the title.</summary>
        public static void RaiseEnding(string endingId) => EndingReached?.Invoke(endingId);

        public static void RaiseRoomEntered(int floor, int room, string kind) => RoomEntered?.Invoke(floor, room, kind);

        /// <summary>A room restarted (a death or a Collapse sink): also a Rewind boundary.</summary>
        public static void RaiseRoomRestarted()
        {
            RewindBoundary?.Invoke();
            RoomRestarted?.Invoke();
        }

        public static void RaiseRewindBoundary() => RewindBoundary?.Invoke();
        public static void RaiseFloorStarted(int floor) => FloorStarted?.Invoke(floor);
        public static void RaiseRoomObjectiveDone(int floor, string kind, float seconds) => RoomObjectiveDone?.Invoke(floor, kind, seconds);
        public static void RaisePlayerDied(int floor, string where) => PlayerDied?.Invoke(floor, where);
        public static void RaiseRewindUsed() => RewindUsed?.Invoke();
        public static void RaiseShadowStepUsed(Vector3 from, Vector3 to) => ShadowStepUsed?.Invoke(from, to);
        public static void RaiseChapterStarted(int chapter) => ChapterStarted?.Invoke(chapter);

        /// <summary>The jobs lane: a job was offered (its room entered). Counted in <see cref="Stats"/>.</summary>
        public static void RaiseJobOffered(string jobId)
        {
            HellRunStats.Current?.RecordJobOffered();
            JobOffered?.Invoke(jobId);
        }

        /// <summary>The jobs lane: a job was done (once per job; the jobs ledger dedupes).</summary>
        public static void RaiseJobDone(string jobId)
        {
            HellRunStats.Current?.RecordJobDone();
            JobDone?.Invoke(jobId);
        }

        /// <summary>The jobs lane: a job failed (left unfinished).</summary>
        public static void RaiseJobFailed(string jobId)
        {
            HellRunStats.Current?.RecordJobFailed();
            JobFailed?.Invoke(jobId);
        }
        public static void RaiseComicPage(string sequenceId, int page)
        {
            ComicPage?.Invoke(sequenceId, page);
            if (sequenceId == HellComics.OpeningId) OpeningPage?.Invoke(page);
        }

        public static void RaiseComicFinished(string sequenceId) => ComicFinished?.Invoke(sequenceId);

        public static void RaiseRunStarted() => RunStarted?.Invoke();
        public static void RaiseRunFinished() => RunFinished?.Invoke();

        public static void RaiseStageChanged(HellFlowStage from, HellFlowStage to)
        {
            Stage = to;
            StageChanged?.Invoke(from, to);
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            FloorCleared = null; RoomEntered = null; RoomRestarted = null; RunStarted = null; RunFinished = null;
            StageChanged = null; RewindBoundary = null; FinalBossRequested = null; FinalBossDefeated = null;
            EndingRequested = null; EndingReached = null;
            FloorStarted = null; RoomObjectiveDone = null; PlayerDied = null; RewindUsed = null; ShadowStepUsed = null;
            JobOffered = null; JobDone = null; JobFailed = null; ChapterStarted = null;
            ComicPage = null; OpeningPage = null; ComicFinished = null;
            WaitingForPick = false;
            Stage = HellFlowStage.Title;
            HellRunStats.Current = null;
        }
    }
}
