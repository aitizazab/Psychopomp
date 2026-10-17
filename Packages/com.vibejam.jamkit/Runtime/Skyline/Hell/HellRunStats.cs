using System;
using System.Collections.Generic;
using System.Numerics;

namespace JamKit.Skyline
{
    /// <summary>
    /// The run's counters for the stats screen and the endings (pure; docs/HELL-FLOW.md). <see cref="Current"/> is the run in
    /// progress (made by <see cref="HellFlow.StartGame"/>; null before the first run). Anyone records into it with the Record*
    /// methods. <b>Rewind-safe:</b> what the world can take back (kills, jobs offered and done) is saved and restored with the
    /// Rewind (<see cref="SaveWorld"/>, <see cref="RestoreWorld"/>: an imp killed, rewound alive and killed again counts once);
    /// what the player did (deaths, Rewinds, Shadow Steps, fast paths, the times) is never rewound.
    /// </summary>
    public sealed class HellRunStats
    {
        public const int Floors = 3;

        static HellRunStats _current;

        /// <summary>The run in progress (null before the first Start).</summary>
        public static HellRunStats Current
        {
            get => _current;
            set => _current = value;
        }

        /// <summary>The counters a Rewind takes back. Reuse one per snapshot (no garbage).</summary>
        public sealed class World
        {
            public int kills, jobsOffered, jobsDone;
        }

        readonly float[] _floorSeconds = new float[Floors];
        readonly World _w = new World();
        float _boss, _ending;
        int _deaths, _rewinds, _steps, _fastTaken, _fastOffered;
        string _endingId;

        /// <summary>Real seconds played on each floor (0-based), the boss fight, and the ending scene.</summary>
        public IReadOnlyList<float> FloorSeconds => _floorSeconds;
        public float BossSeconds => _boss;
        public float EndingSeconds => _ending;
        public float TotalSeconds
        {
            get
            {
                float t = _boss + _ending;
                for (int i = 0; i < Floors; i++) t += _floorSeconds[i];
                return t;
            }
        }

        public int Deaths => _deaths;
        public int Rewinds => _rewinds;
        public int ShadowSteps => _steps;
        public int FastPathsTaken => _fastTaken;
        public int FastPathsOffered => _fastOffered;
        public int EnemiesKilled => _w.kills;
        public int JobsOffered => _w.jobsOffered;
        public int JobsDone => _w.jobsDone;
        /// <summary>The ending reached (<see cref="HellEndings"/>), or null.</summary>
        public string EndingId => _endingId;

        public void TickFloor(int floor, float seconds)
        {
            if (floor < 0 || floor >= Floors || !(seconds > 0f)) return;
            _floorSeconds[floor] += seconds;
        }

        public void TickBoss(float seconds) { if (seconds > 0f) _boss += seconds; }
        public void TickEnding(float seconds) { if (seconds > 0f) _ending += seconds; }

        public void RecordDeath() => _deaths++;
        public void RecordRewind() => _rewinds++;
        public void RecordShadowStep() => _steps++;
        /// <summary>A room's exit: it offered a fast path; <paramref name="taken"/> if the player took it.</summary>
        public void RecordFastPath(bool taken)
        {
            _fastOffered++;
            if (taken) _fastTaken++;
        }
        public void RecordKill() => _w.kills++;
        /// <summary>A job (one of the God of Death's favours) was offered to the player.</summary>
        public void RecordJobOffered() => _w.jobsOffered++;
        public void RecordJobDone() => _w.jobsDone++;
        public void RecordEnding(string endingId) => _endingId = endingId;

        /// <summary>The share of offered jobs done (0 when none were offered).</summary>
        public float JobsShare => _w.jobsOffered <= 0 ? 0f : _w.jobsDone / (float)_w.jobsOffered;

        /// <summary>Fills <paramref name="reuse"/> (or a new one) with the rewindable counters.</summary>
        public World SaveWorld(World reuse = null)
        {
            var s = reuse ?? new World();
            s.kills = _w.kills; s.jobsOffered = _w.jobsOffered; s.jobsDone = _w.jobsDone;
            return s;
        }

        public void RestoreWorld(World s)
        {
            if (s == null) return;
            _w.kills = s.kills; _w.jobsOffered = s.jobsOffered; _w.jobsDone = s.jobsDone;
        }

        /// <summary>m:ss.t (h:mm:ss past an hour).</summary>
        public static string Clock(float seconds)
        {
            if (!(seconds > 0f)) seconds = 0f;
            var t = TimeSpan.FromSeconds(seconds);
            if (t.TotalHours >= 1.0) return ((int)t.TotalHours) + ":" + t.Minutes.ToString("00") + ":" + t.Seconds.ToString("00");
            return ((int)t.TotalMinutes) + ":" + t.Seconds.ToString("00") + "." + (t.Milliseconds / 100);
        }
    }

    /// <summary>
    /// Did the player take a room's fast path? (pure; docs/HELL-FLOW.md). The fast path is sampled every
    /// <see cref="SampleStep"/> m; the samples farther than <see cref="Apart"/> m from every point of the safe path are its own
    /// (the shared start and end don't count). A sample is visited when the feet come within <see cref="Reach"/> m of it. The
    /// fast path counts as taken when at least <see cref="TakenShare"/> of its own samples were visited.
    /// </summary>
    public sealed class HellFastPathTracker
    {
        public const float SampleStep = 2f, Apart = 5f, Reach = 3.5f, TakenShare = 0.5f;

        readonly List<Vector3> _own = new List<Vector3>();
        readonly bool[] _seen;
        int _seenCount;

        public HellFastPathTracker(IReadOnlyList<Vector3> safePath, IReadOnlyList<Vector3> fastPath)
        {
            var safe = Sample(safePath, 0.5f);
            foreach (var p in Sample(fastPath, SampleStep))
            {
                bool shared = false;
                foreach (var q in safe)
                    if (Vector3.DistanceSquared(p, q) < Apart * Apart) { shared = true; break; }
                if (!shared) _own.Add(p);
            }
            _seen = new bool[_own.Count];
        }

        /// <summary>The fast path's own samples (none: the tracker can never say taken).</summary>
        public int OwnSamples => _own.Count;
        public int Visited => _seenCount;
        public float Share => _own.Count == 0 ? 0f : _seenCount / (float)_own.Count;
        public bool Taken => _own.Count > 0 && Share >= TakenShare;

        public void Observe(Vector3 feet)
        {
            for (int i = 0; i < _own.Count; i++)
            {
                if (_seen[i]) continue;
                if (Vector3.DistanceSquared(feet, _own[i]) <= Reach * Reach) { _seen[i] = true; _seenCount++; }
            }
        }

        public void Reset()
        {
            for (int i = 0; i < _seen.Length; i++) _seen[i] = false;
            _seenCount = 0;
        }

        /// <summary>Points along the polyline every <paramref name="step"/> m, both ends included.</summary>
        public static List<Vector3> Sample(IReadOnlyList<Vector3> path, float step)
        {
            var o = new List<Vector3>();
            if (path == null || path.Count == 0) return o;
            o.Add(path[0]);
            for (int i = 1; i < path.Count; i++)
            {
                var a = path[i - 1];
                var b = path[i];
                float len = Vector3.Distance(a, b);
                int n = Math.Max(1, (int)Math.Ceiling(len / step));
                for (int k = 1; k <= n; k++) o.Add(Vector3.Lerp(a, b, k / (float)n));
            }
            return o;
        }
    }
}
