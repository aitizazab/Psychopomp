using System.Collections.Generic;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>A rewindable that can appear during play (an enemy spawned, a bolt fired, a scythe called, a lava pool laid). It
    /// registers with <see cref="HellRewindRegistry"/> only while it is active, so a Rewind to a moment before it appeared has no
    /// snapshot of it; <see cref="HellRewindLedger"/> finds it and calls <see cref="VanishForRewind"/>.</summary>
    public interface IHellRewindSpawned : IHellRewindable
    {
        /// <summary>The Rewind went back to before this appeared: switch off quietly (no death effects, no events).</summary>
        void VanishForRewind();
    }

    /// <summary>
    /// Closes the one gap in the register-while-active rule (docs/HELL-ENEMIES.md, "Rewind"): HellRewind restores what was
    /// registered at the moment it rewinds to (and so brings back what was killed since, from its kept snapshot), but something
    /// that appeared after that moment was not registered then and would be left as it is now. The ledger is itself a rewindable,
    /// always registered: its snapshot is the set of spawned things active at that step, and its restore switches off every
    /// spawned thing active now that was not in that set. Order-free with the other restores: the things it switches off were not
    /// in the frame, and everything in the frame is left alone. One per scene, made on first use (call <see cref="Ensure"/> at a
    /// room's start so the earliest frames hold it too).
    /// </summary>
    public sealed class HellRewindLedger : MonoBehaviour, IHellRewindable
    {
        static readonly List<IHellRewindSpawned> _active = new List<IHellRewindSpawned>();
        static readonly List<IHellRewindSpawned> _scratch = new List<IHellRewindSpawned>();
        static readonly HashSet<IHellRewindSpawned> _then = new HashSet<IHellRewindSpawned>();
        static HellRewindLedger _instance;
        static bool _quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _active.Clear();
            _instance = null;
            _quitting = false;
            Application.quitting -= OnQuit;
            Application.quitting += OnQuit;
        }

        static void OnQuit() => _quitting = true;

        /// <summary>The spawned rewindables active now.</summary>
        public static IReadOnlyList<IHellRewindSpawned> Active => _active;

        /// <summary>Makes the scene's ledger if there is none.</summary>
        public static void Ensure()
        {
            if (_instance != null || _quitting || !Application.isPlaying) return;
            _instance = new GameObject("Hell Rewind Ledger").AddComponent<HellRewindLedger>();
        }

        public static void Enabled(IHellRewindSpawned s)
        {
            if (s == null) return;
            if (!_active.Contains(s)) _active.Add(s);
            Ensure();
        }

        public static void Disabled(IHellRewindSpawned s) => _active.Remove(s);

        void OnEnable() => HellRewindRegistry.Register(this);
        void OnDisable() => HellRewindRegistry.Unregister(this);

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public object Snapshot() => _active.ToArray();

        public void Restore(object snapshot)
        {
            if (!(snapshot is IHellRewindSpawned[] then)) return;
            _then.Clear();
            for (int i = 0; i < then.Length; i++) _then.Add(then[i]);
            _scratch.Clear();
            _scratch.AddRange(_active);   // VanishForRewind takes things out of _active
            for (int i = 0; i < _scratch.Count; i++)
            {
                var s = _scratch[i];
                if (s == null || _then.Contains(s)) continue;
                var o = s as Object;
                if ((object)o != null && o == null) { _active.Remove(s); continue; }   // destroyed with a scene
                s.VanishForRewind();
            }
            _scratch.Clear();
            _then.Clear();
        }
    }
}
