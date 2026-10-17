using System;
using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>Optional, beside <see cref="IHellRewindable"/>: told when a rewind goes back to before it existed (it was
    /// registered after the restored snapshot was taken), so it can switch itself off (a bot that came through a door 2 s
    /// ago, a bolt fired after the snapshot). Rewindables without it are left as they are.</summary>
    public interface IHellRewindSpawnAware
    {
        void RewoundBeforeSpawn();
    }

    /// <summary>
    /// The Rewind (docs/HELL-ABILITIES.md): every fixed step, a snapshot of every <see cref="IHellRewindable"/> in
    /// <see cref="HellRewindRegistry.All"/> goes into a ring a little over <see cref="seconds"/> long (game time); a rewind
    /// restores the snapshot taken <see cref="seconds"/> ago (the oldest one when the history is shorter) and clears the ring.
    /// Before each record it registers what doesn't register itself: the bolts in flight (one rewindable for the pool) and the
    /// arena / Relay / repossession directors in the scene. Bots and the Hell attacks list themselves while active
    /// (docs/HELL-INTEGRATION.md). Something listed at the moment of a rewind but missing from the restored frame appeared
    /// after it: it is told so through <see cref="IHellRewindSpawnAware"/> and switches itself off. A rewindable whose Unity object was destroyed is dropped. A Restore that throws is logged and
    /// skipped (one bad object never stops the rest). Lives on the player (added by <see cref="HellAbilities"/>).
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class HellRewind : MonoBehaviour
    {
        public float seconds = 4f;
        public float cooldown = 20f;

        sealed class Frame
        {
            public readonly List<IHellRewindable> who = new List<IHellRewindable>();
            public readonly List<object> what = new List<object>();
        }

        public static HellRewind Instance { get; private set; }
        /// <summary>Raised after a rewind restored everything (the visual, the sound, skills).</summary>
        public static event Action Rewound;

        public AbilityCooldown Cooldown { get; private set; }
        public int Recorded => _ring != null ? _ring.Count : 0;
        public double HistorySeconds => _ring != null && _ring.Count > 1 ? _ring.NewestTime - _ring.OldestTime : 0.0;

        SnapshotRing<Frame> _ring;
        readonly HashSet<IHellRewindable> _inFrame = new HashSet<IHellRewindable>();
        readonly List<IHellRewindable> _scratch = new List<IHellRewindable>();
        float _nextSceneScan;

        void Awake()
        {
            Instance = this;
            Cooldown = new AbilityCooldown(cooldown);
            _ring = new SnapshotRing<Frame>(SnapshotRing<Frame>.CapacityFor(seconds + 0.25f, Time.fixedDeltaTime));
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            Cooldown.Duration = cooldown;
            Cooldown.Tick(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (SkylineDeviceInput.Paused) return;
            RegisterKitObjects();
            Record(Time.fixedTimeAsDouble);
        }

        void Record(double now)
        {
            Frame f = _ring.Count == _ring.Capacity ? _ring[0] : new Frame();
            f.who.Clear();
            f.what.Clear();
            var all = HellRewindRegistry.All;
            for (int i = all.Count - 1; i >= 0; i--)
                if (IsGone(all[i])) all.RemoveAt(i);
            for (int i = 0; i < all.Count; i++)
            {
                var r = all[i];
                object s;
                try { s = r.Snapshot(); }
                catch (Exception e) { Debug.LogWarning("HELL_REWIND snapshot failed on " + r + ": " + e.Message); continue; }
                f.who.Add(r);
                f.what.Add(s);
            }
            _ring.Push(now, f);
        }

        static bool IsGone(IHellRewindable r)
        {
            if (r == null) return true;
            var o = r as UnityEngine.Object;
            return (object)o != null && o == null;   // a destroyed component
        }

        /// <summary>Uses the Rewind if its cooldown is ready (or <paramref name="free"/>: a skill's own rewind, no cooldown).
        /// False when on cooldown or nothing is recorded yet.</summary>
        public bool TryRewind(bool free = false)
        {
            if (_ring.Count == 0) return false;
            if (!free && !Cooldown.Ready) return false;
            if (!free) Cooldown.TryUse();
            double target = _ring.NewestTime - seconds;
            var f = _ring[_ring.FindAtOrBefore(target)];
            // what is listed now, taken BEFORE the restores (a restore can list things: a dead enemy reactivated, a bot revived
            // as a fresh one; those must not be taken for things that appeared after the snapshot)
            _scratch.Clear();
            _scratch.AddRange(HellRewindRegistry.All);
            _inFrame.Clear();
            for (int i = 0; i < f.who.Count; i++)
            {
                var r = f.who[i];
                _inFrame.Add(r);
                if (IsGone(r)) continue;
                try { r.Restore(f.what[i]); }
                catch (Exception e) { Debug.LogWarning("HELL_REWIND restore failed on " + r + ": " + e.Message); }
            }
            // what came into being after the snapshot: listed now, not in the frame
            foreach (var r in _scratch)
            {
                if (_inFrame.Contains(r) || IsGone(r)) continue;
                if (r is IHellRewindSpawnAware a)
                {
                    try { a.RewoundBeforeSpawn(); }
                    catch (Exception e) { Debug.LogWarning("HELL_REWIND unspawn failed on " + r + ": " + e.Message); }
                }
            }
            _scratch.Clear();
            _ring.Clear();
            Debug.Log("HELL_REWIND back " + seconds.ToString("0.#") + " s (" + f.who.Count + " objects)");
            Rewound?.Invoke();
            return true;
        }

        // ------------------------------------------------------------------ the kit's objects

        void RegisterKitObjects()
        {
            // bots, Hell attacks (scythes, lava pools, beam lanes) list themselves while active; the bolts are one pool rewindable
            HellRewindRegistry.Register(SkylineBolt.Rewindable);
            if (Time.unscaledTime >= _nextSceneScan)
            {
                _nextSceneScan = Time.unscaledTime + 2f;
                foreach (var d in FindObjectsByType<SkylineArenaDirector>(FindObjectsSortMode.None)) HellRewindRegistry.Register(d);
                foreach (var d in FindObjectsByType<SkylineRelayDirector>(FindObjectsSortMode.None)) HellRewindRegistry.Register(d);
                foreach (var d in FindObjectsByType<SkylineRepossess>(FindObjectsSortMode.None)) HellRewindRegistry.Register(d);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Rewound = null;
            HellRewindRegistry.All.Clear();
        }
    }
}
