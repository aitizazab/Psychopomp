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

    /// <summary>Snapshots without garbage: a rewindable takes its snapshot object from here each fixed step and fills it in.
    /// The pool turns over more slowly than the Rewind's ring, so an object is reused only after its frame has left the ring.</summary>
    public sealed class HellSnapPool<T> where T : class, new()
    {
        T[] _items;
        int _i = -1;

        public T Next()
        {
            if (_items == null) _items = new T[HellRewind.PoolSize];
            _i = (_i + 1) % _items.Length;
            var t = _items[_i];
            if (t == null) _items[_i] = t = new T();
            return t;
        }
    }

    /// <summary>A pooled box for a value-type snapshot (<c>HellSnapPool&lt;HellSnapBox&lt;S&gt;&gt;</c>): no boxing per step.</summary>
    public sealed class HellSnapBox<T> where T : struct
    {
        public T value;

        /// <summary>The value in a snapshot that is a box, or the boxed value itself (older snapshots).</summary>
        public static T From(object snapshot) => snapshot is HellSnapBox<T> b ? b.value : (T)snapshot;
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
        public float cooldown = 30f;   // 20 -> 30 s with the mend (the creator, 2026-10-18)
        /// <summary>The ring always holds this much (the longest rewind any skill gives).</summary>
        public const float MaxSeconds = 6f;
        /// <summary>The snapshot pools' size: more than the ring holds at the fixed step.</summary>
        public static int PoolSize => SnapshotRing<int>.CapacityFor(MaxSeconds + 0.5f, Mathf.Max(0.005f, Time.fixedDeltaTime)) + 2;

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
            // sized for the longest rewind a skill can ask for (Deeper Rewind: 6 s)
            _ring = new SnapshotRing<Frame>(SnapshotRing<Frame>.CapacityFor(Mathf.Max(seconds, MaxSeconds) + 0.25f, Time.fixedDeltaTime));
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable() => HellRunEvents.RewindBoundary += ClearHistory;
        void OnDisable() => HellRunEvents.RewindBoundary -= ClearHistory;

        /// <summary>Forgets every recorded frame (<see cref="HellRunEvents.RewindBoundary"/>: a room entered or restarted, a shaft, a
        /// screen; a rewind never reaches back past one, so it clamps to the room's start).</summary>
        public void ClearHistory()
        {
            if (_ring != null) _ring.Clear();
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
