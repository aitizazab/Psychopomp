namespace JamKit.SkylineRepo
{
    /// <summary>The Hell descent's Rewind on the repossession objective (docs/HELL-ABILITIES.md): the floor clock and every
    /// unfinished tow meter go back (a finished tow stays finished: the asset has gone). Registered by HellRewind.</summary>
    public sealed partial class SkylineRepossess : IHellRewindable
    {
        sealed class HellSnap { public float floorSeconds; public double[] meters; }

        object IHellRewindable.Snapshot()
        {
            if (_objective == null) return null;
            var sheet = _objective.Sheet;
            var s = new HellSnap { floorSeconds = _floorSeconds, meters = new double[sheet.Count] };
            for (int i = 0; i < sheet.Count; i++) s.meters[i] = sheet[i].meter.SecondsIn;
            return s;
        }

        void IHellRewindable.Restore(object snapshot)
        {
            if (!(snapshot is HellSnap s) || _objective == null) return;
            _floorSeconds = s.floorSeconds;
            var sheet = _objective.Sheet;
            for (int i = 0; i < sheet.Count && i < s.meters.Length; i++) sheet[i].meter.RestoreSeconds(s.meters[i]);
        }
    }
}
