using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>The Hell descent's Rewind on the Relay (docs/HELL-ABILITIES.md): the wave clock, the wave count and the run's
    /// clock go back (the steps already done stay done: their props don't rewind). Revived bots replace their old selves in
    /// the wave list; bots a rewind switched off leave it. Registered by HellRewind.</summary>
    public sealed partial class SkylineRelayDirector : IHellRewindable
    {
        sealed class HellSnap { public object clock; public float elapsed; public int waveNumber; }

        object IHellRewindable.Snapshot()
        {
            if (_waves == null || _run == null) return null;
            return new HellSnap { clock = _waves.Clock.SaveState(), elapsed = _run.Elapsed, waveNumber = _waveNumber };
        }

        void IHellRewindable.Restore(object snapshot)
        {
            if (!(snapshot is HellSnap s) || _waves == null || _run == null) return;
            _waves.Clock.RestoreState(s.clock);
            _run.RestoreElapsed(s.elapsed);
            _waveNumber = s.waveNumber;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void HookHellRewind()
        {
            SkylineBot.HellRevived += (o, n) =>
            {
                foreach (var d in FindObjectsByType<SkylineRelayDirector>(FindObjectsSortMode.None))
                {
                    int k = d._bots.IndexOf(o);
                    if (k >= 0) d._bots[k] = n;
                }
            };
            SkylineBot.HellRetired += b => { foreach (var d in FindObjectsByType<SkylineRelayDirector>(FindObjectsSortMode.None)) d._bots.Remove(b); };
        }
    }
}
