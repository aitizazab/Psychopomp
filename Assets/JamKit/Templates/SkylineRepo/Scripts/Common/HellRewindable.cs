namespace JamKit.SkylineRepo
{
    // Anything the 4-second Rewind must restore implements this and registers with HellRewind (abilities branch).
    // Snapshot() is called every fixed step; Restore() puts the object back exactly. Never Destroy a rewindable while
    // play continues: deactivate it, so a rewind can bring it back.
    public interface IHellRewindable
    {
        object Snapshot();
        void Restore(object snapshot);
    }
}
