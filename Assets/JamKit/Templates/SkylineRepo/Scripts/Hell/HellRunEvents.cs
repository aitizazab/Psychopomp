using System;
using JamKit.Skyline;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The run's events: the contract between the run (<see cref="HellRunDirector"/>) and the other lanes (docs/HELL-FLOW.md,
    /// "Contracts").
    /// <list type="bullet">
    /// <item><b>Skill pick:</b> at the bottom of a floor's shaft the run raises <see cref="FloorCleared"/> (0-based floor; not
    /// for the last floor) and waits for <see cref="ContinueAfterPick"/>. With nobody subscribed it continues at once.</item>
    /// <item><b>Boss:</b> after the last floor's shaft the run raises <see cref="FinalBossRequested"/>. A listener starts the
    /// fight and calls <see cref="BossDefeated"/> when Vesper is beaten. No listener: straight on to the ending.</item>
    /// <item><b>Ending:</b> after the boss the run raises <see cref="EndingRequested"/>. A listener runs the decision scene and
    /// the escape, then calls <see cref="RaiseEnding"/> with a <see cref="HellEndings"/> id: the credits, the stats, the
    /// title. No listener: the credits roll at once (ending <see cref="HellEndings.BossOnly"/>).</item>
    /// <item><b>Stats:</b> <see cref="Stats"/> (= <see cref="HellRunStats.Current"/>): Record* for jobs, kills and the rest;
    /// <c>Stats.JobsDone</c> / <c>JobsOffered</c> for the endings.</item>
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

        /// <summary>The boss lane: start Vesper's fight (move the player into the arena), then call <see cref="BossDefeated"/>.</summary>
        public static event Action FinalBossRequested;
        /// <summary>Raised when <see cref="BossDefeated"/> is called (the run director listens).</summary>
        public static event Action FinalBossDefeated;
        /// <summary>The ending lane: run the decision and the escape, then call <see cref="RaiseEnding"/>.</summary>
        public static event Action EndingRequested;
        /// <summary>Raised by <see cref="RaiseEnding"/> with the ending's id (the run director listens: credits, stats, title).</summary>
        public static event Action<string> EndingReached;

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
            h();
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
            WaitingForPick = false;
            Stage = HellFlowStage.Title;
            HellRunStats.Current = null;
        }
    }
}
