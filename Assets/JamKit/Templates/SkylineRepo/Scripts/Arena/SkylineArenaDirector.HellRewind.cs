using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>The Hell descent's Rewind on the M1 arena (docs/HELL-ABILITIES.md): each encounter's phase and combat flag,
    /// the floor markers and the lift's shutter go back; the bots themselves rewind on their own (a revived bot replaces its
    /// old self in the lists, a bot switched off by a rewind leaves them). Registered by HellRewind.</summary>
    public sealed partial class SkylineArenaDirector : IHellRewindable
    {
        sealed class HellSnap
        {
            public Phase[] phases;
            public bool[] inCombat;
            public bool liftOpen, exitReached;
        }

        object IHellRewindable.Snapshot()
        {
            var s = new HellSnap { phases = new Phase[_encounters.Count], inCombat = new bool[_encounters.Count], liftOpen = LiftOpen, exitReached = ExitReached };
            for (int i = 0; i < _encounters.Count; i++) { s.phases[i] = _encounters[i].phase; s.inCombat[i] = _encounters[i].inCombat; }
            return s;
        }

        void IHellRewindable.Restore(object snapshot)
        {
            if (!(snapshot is HellSnap s) || s.phases.Length != _encounters.Count) return;
            int marker = -1;
            for (int i = 0; i < _encounters.Count; i++)
            {
                var e = _encounters[i];
                if (e.inCombat && !s.inCombat[i]) player.Ledger.EndCombat();
                else if (!e.inCombat && s.inCombat[i]) player.Ledger.BeginCombat();
                e.phase = s.phases[i];
                e.inCombat = s.inCombat[i];
                if (e.phase == Phase.Armed && i > 0) marker = i;
            }
            ShowMarker(marker);
            if (LiftOpen && !s.liftOpen) RollShutter(false);
            ExitReached = s.exitReached;
        }

        void HellSwap(SkylineBot old, SkylineBot fresh)
        {
            if (!_botEncounter.TryGetValue(old, out int i)) return;
            _botEncounter.Remove(old);
            var list = _encounters[i].bots;
            int k = list.IndexOf(old);
            if (k >= 0) list[k] = fresh; else list.Add(fresh);
            _botEncounter[fresh] = i;
        }

        void HellDrop(SkylineBot bot)
        {
            if (!_botEncounter.TryGetValue(bot, out int i)) return;
            _botEncounter.Remove(bot);
            _encounters[i].bots.Remove(bot);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void HookHellRewind()
        {
            SkylineBot.HellRevived += (o, n) => { foreach (var d in FindObjectsByType<SkylineArenaDirector>(FindObjectsSortMode.None)) d.HellSwap(o, n); };
            SkylineBot.HellRetired += b => { foreach (var d in FindObjectsByType<SkylineArenaDirector>(FindObjectsSortMode.None)) d.HellDrop(b); };
        }
    }
}
