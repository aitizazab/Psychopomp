#if !HELL_ROOMS
namespace JamKit.SkylineRepo
{
    // A STUB so the abilities branch compiles on its own. The rooms branch owns the real HellRunEvents (same names):
    // delete this file at the merge. Only FloorCleared and ContinueAfterPick are part of the contract; nothing else here
    // is used outside this file.
    public static class HellRunEvents
    {
        /// <summary>A floor was cleared (its number): the skill pick screen opens.</summary>
        public static event System.Action<int> FloorCleared;

        /// <summary>The pick is made: the run goes on (the rooms branch loads the next floor).</summary>
        public static void ContinueAfterPick()
        {
            UnityEngine.Debug.Log("HELL_RUN continue after pick (stub)");
        }

        /// <summary>Stub only: raises FloorCleared (the real one is raised by the rooms branch).</summary>
        public static void RaiseFloorClearedForStub(int floor) => FloorCleared?.Invoke(floor);
    }
}
#endif
