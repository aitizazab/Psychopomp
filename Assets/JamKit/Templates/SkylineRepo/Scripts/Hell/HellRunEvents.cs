using System;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The run's events (the contract with the abilities branch). At the bottom of a floor's shaft the run raises
    /// <see cref="FloorCleared"/> with the floor's index (0-based: 0 the Ember Gate, 1 the Molten Works; the last floor ends
    /// the run instead) and waits until <see cref="ContinueAfterPick"/> is called (the skill pick closed). With nobody
    /// subscribed it continues at once.
    /// </summary>
    public static class HellRunEvents
    {
        public static event Action<int> FloorCleared;
        /// <summary>A room was entered: (floor, room in the floor 0..3, its kind's name).</summary>
        public static event Action<int, int, string> RoomEntered;
        /// <summary>The player died and the room restarted from its entry (a Rewind should drop its history here).</summary>
        public static event Action RoomRestarted;
        public static event Action RunStarted;
        /// <summary>The run is won (the last floor's shaft).</summary>
        public static event Action RunFinished;

        /// <summary>True between <see cref="FloorCleared"/> and <see cref="ContinueAfterPick"/>.</summary>
        public static bool WaitingForPick { get; private set; }

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

        public static void RaiseRoomEntered(int floor, int room, string kind) => RoomEntered?.Invoke(floor, room, kind);
        public static void RaiseRoomRestarted() => RoomRestarted?.Invoke();
        public static void RaiseRunStarted() => RunStarted?.Invoke();
        public static void RaiseRunFinished() => RunFinished?.Invoke();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            FloorCleared = null; RoomEntered = null; RoomRestarted = null; RunStarted = null; RunFinished = null;
            WaitingForPick = false;
        }
    }
}
