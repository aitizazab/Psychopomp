using JamKit.Skyline;
using UnityEngine;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The Rewind as the Time showpiece (docs/HELL-FX.md). The Rewind itself is instant (<see cref="HellRewind"/> restores the
    /// snapshot from 4 s ago in one frame); this plays it back so the world visibly runs backwards:
    /// <list type="bullet">
    /// <item><b>Recording</b> (always, cheap): 30 times a game second the player's, every live bot's and every bolt in flight's
    /// position go into a <see cref="GhostTrack"/> ring each (fixed slots, no allocation).</item>
    /// <item><b>On <see cref="HellRewind.Rewound"/></b>: each track's last <c>seconds</c> are resampled into a ghost path, the
    /// tracks are cleared (the history is gone, as the Rewind's own ring is), and the world is held nearly still for the scrub
    /// (<see cref="HellTime"/> key <c>rewindfx</c>, <see cref="freezeScale"/>) while cyan ghosts race backwards along the paths
    /// (yours runs back to where you now stand; each enemy's back to where it was; each bolt flies back into the muzzle that
    /// fired it), leaving echoes, under the Rewind's screen treatment (grade, chroma, an inward lens pull, speed streaks, tape
    /// bars).</item>
    /// <item><b>The snap</b> at the end: a bright pop (flash, lens push, shake), and an afterimage ring on each restored enemy.</item>
    /// </list>
    /// Visual only: nothing here changes what the Rewind restored. Listens to events only (no edits to HellRewind).
    /// </summary>
    [DefaultExecutionOrder(1100)]
    public sealed class HellRewindFx : MonoBehaviour
    {
        public const string TimeKey = "rewindfx";
        const int BotSlots = 16, BoltSlots = 24, Slots = 1 + BotSlots + BoltSlots, PathPoints = 48;
        const int KindPlayer = 0, KindBot = 1, KindBolt = 2;

        [Tooltip("Samples per game second.")]
        public float sampleHz = 30f;
        [Tooltip("How long the ghosts take to run back (real seconds).")]
        public float scrubSeconds = 0.5f;
        [Tooltip("Hold the world nearly still during the scrub (the restored moment waits for its ghosts).")]
        public bool freeze = true;
        [Tooltip("The time scale while the scrub plays (1 = no hold).")]
        public float freezeScale = 0.06f;
        [Tooltip("The share of a ghost's path its tail covers.")]
        public float tailShare = 0.45f;
        public float snapTrauma = 0.3f;

        public static HellRewindFx Instance { get; private set; }
        /// <summary>True while the scrub plays.</summary>
        public bool Playing => _playing;

        sealed class Slot
        {
            public readonly GhostTrack track;
            public object owner;
            public int kind;
            public double lastSeen;
            public Slot(int capacity, int kind) { track = new GhostTrack(capacity); this.kind = kind; }
        }

        sealed class Ghost
        {
            public LineRenderer line;
            public Transform head;
            public readonly Vector3[] path = new Vector3[PathPoints];
            public int kind;
            public bool on;
        }

        Slot[] _slots;
        Ghost[] _ghosts;
        double _nextSample;
        bool _playing;
        float _playT, _echoLeft;

        void Awake()
        {
            Instance = this;
            int cap = Mathf.CeilToInt((HellRewind.MaxSeconds + 0.6f) * Mathf.Max(5f, sampleHz)) + 2;
            _slots = new Slot[Slots];
            _ghosts = new Ghost[Slots];
            for (int i = 0; i < Slots; i++)
            {
                int kind = i == 0 ? KindPlayer : i <= BotSlots ? KindBot : KindBolt;
                _slots[i] = new Slot(kind == KindBolt ? 80 : cap, kind);
                _ghosts[i] = MakeGhost(kind);
            }
        }

        Ghost MakeGhost(int kind)
        {
            var g = new Ghost { kind = kind };
            var lineGo = new GameObject("RewindGhostPath");
            lineGo.transform.SetParent(transform, false);
            var l = lineGo.AddComponent<LineRenderer>();
            l.useWorldSpace = true;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.numCapVertices = 2;
            l.positionCount = 0;
            l.widthMultiplier = kind == KindPlayer ? 0.16f : kind == KindBot ? 0.11f : 0.05f;
            l.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.25f, 0.6f), new Keyframe(1f, 0f));
            l.enabled = false;
            g.line = l;
            var head = SkylineBotLook.Visual(PrimitiveType.Sphere, "RewindGhost", transform, HellFx.Glow(HellFxPalette.Rewind, HellFxPalette.RewindGlow), false);
            head.transform.localScale = kind == KindPlayer ? new Vector3(0.45f, 0.95f, 0.45f) : kind == KindBot ? Vector3.one * 0.7f : Vector3.one * 0.16f;
            head.SetActive(false);
            g.head = head.transform;
            return g;
        }

        void OnEnable()
        {
            HellRewind.Rewound += OnRewound;
            HellRunEvents.RoomRestarted += ClearTracks;
        }

        void OnDisable()
        {
            HellRewind.Rewound -= OnRewound;
            HellRunEvents.RoomRestarted -= ClearTracks;
            if (_playing) EndScrub(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void ClearTracks()
        {
            foreach (var s in _slots) { s.track.Clear(); s.owner = null; }
        }

        // ------------------------------------------------------------------ recording

        void LateUpdate()
        {
            if (_playing) TickScrub(Mathf.Min(Time.unscaledDeltaTime, 0.1f));
            else Sample();
        }

        void Sample()
        {
            double now = Time.timeAsDouble;
            if (now < _nextSample) return;
            _nextSample = now + 1.0 / Mathf.Max(5f, sampleHz);
            var a = HellAbilities.Instance;
            if (a == null || a.Player == null) return;

            // the player (chest height)
            var ps = _slots[0];
            ps.track.Push(now, N(a.Player.transform.position + Vector3.up * 0.9f));
            ps.lastSeen = now;

            // the bots
            var live = SkylineBot.Live;
            for (int i = 0; i < live.Count; i++)
            {
                var b = live[i];
                if (b == null || !b.isActiveAndEnabled) continue;
                var s = BotSlot(b, now);
                if (s == null) continue;
                s.track.Push(now, N(b.Center));
                s.lastSeen = now;
            }

            // the bolts in flight (slot by pool index; a reused bolt starts a fresh track)
            var pool = SkylineBolt.Pool;
            for (int i = 0; i < pool.Count && i < BoltSlots; i++)
            {
                var b = pool[i];
                if (b == null || !b.gameObject.activeSelf) continue;
                var s = _slots[1 + BotSlots + i];
                if (!ReferenceEquals(s.owner, b) || (s.track.Count > 0 && now - s.lastSeen > 0.15)) { s.track.Clear(); s.owner = b; }
                s.track.Push(now, N(b.transform.position));
                s.lastSeen = now;
            }
        }

        Slot BotSlot(SkylineBot b, double now)
        {
            Slot free = null;
            for (int i = 1; i <= BotSlots; i++)
            {
                var s = _slots[i];
                if (ReferenceEquals(s.owner, b)) return s;
                if (free == null && (s.owner == null || now - s.lastSeen > HellRewind.MaxSeconds + 0.5)) free = s;
            }
            if (free != null) { free.track.Clear(); free.owner = b; }
            return free;
        }

        // ------------------------------------------------------------------ the scrub

        void OnRewound()
        {
            float seconds = HellRewind.Instance != null ? HellRewind.Instance.seconds : 4f;
            double now = Time.timeAsDouble, target = now - seconds;
            int shown = 0;
            for (int i = 0; i < Slots; i++)
            {
                var s = _slots[i];
                var g = _ghosts[i];
                g.on = false;
                if (s.track.Count < 2 || s.track.NewestTime < target) continue;
                for (int k = 0; k < PathPoints; k++)
                    g.path[k] = U(s.track.At(now - seconds * k / (PathPoints - 1.0)));
                // a ghost that never moved has nothing to show (a bot standing still): its snap ring still plays
                float len = 0f;
                for (int k = 1; k < PathPoints; k++) len += Vector3.Distance(g.path[k - 1], g.path[k]);
                g.on = len > 0.4f || s.kind == KindBot;
                if (g.on) shown++;
            }
            ClearTracks();
            ClearKitEffects();
            _playing = true;
            _playT = 0f;
            _echoLeft = 0f;
            if (freeze) HellTime.Request(TimeKey, Mathf.Clamp(freezeScale, 0.01f, 1f));
            HellFx.Flash(Color.Lerp(HellFxPalette.Rewind, Color.white, 0.5f), 0.35f, 0.14f);
            HellFx.FovKick(-0.08f, 0.08f, scrubSeconds);
            Debug.Log("HELL_FX rewind scrub: " + shown + " ghosts over " + seconds.ToString("0.#") + " s");
        }

        /// <summary>The kit's and the enemies' own short-lived effects (death flames, smoke puffs, sparks) from the abandoned
        /// future: switched off, which is how their pools free them (once per Rewind; never per frame).</summary>
        static void ClearKitEffects()
        {
            foreach (var f in FindObjectsByType<HellFlame>(FindObjectsSortMode.None)) f.gameObject.SetActive(false);
            foreach (var p in FindObjectsByType<SkylineSmokePuff>(FindObjectsSortMode.None)) p.gameObject.SetActive(false);
            foreach (var s in FindObjectsByType<SkylineSpark>(FindObjectsSortMode.None)) s.gameObject.SetActive(false);
        }

        void TickScrub(float udt)
        {
            _playT += udt;
            float total = Mathf.Max(0.1f, scrubSeconds);
            float u = Mathf.Clamp01(_playT / total);
            HellFx.SetRewind(HellFxCurves.Envelope(_playT, total, 0.05f, 0.1f));
            if (freeze) HellTime.Request(TimeKey, Mathf.Clamp(freezeScale, 0.01f, 1f));   // re-asserted (a hit stop clearing never lifts it)
            float h = HellFxCurves.Scrub(u) * (PathPoints - 1);
            int tail = Mathf.Max(2, Mathf.RoundToInt(PathPoints * tailShare));
            _echoLeft -= udt;
            bool echo = _echoLeft <= 0f;
            if (echo) _echoLeft = 0.045f;
            var echoMat = HellFx.Glow(HellFxPalette.Rewind, HellFxPalette.RewindGlow * 0.7f);
            for (int i = 0; i < Slots; i++)
            {
                var g = _ghosts[i];
                if (!g.on) continue;
                int hi = Mathf.Min(PathPoints - 1, Mathf.FloorToInt(h));
                Vector3 head = Vector3.Lerp(g.path[hi], g.path[Mathf.Min(PathPoints - 1, hi + 1)], h - hi);
                if (!g.head.gameObject.activeSelf) g.head.gameObject.SetActive(true);
                g.head.position = head;
                // the line: from the head back toward the present, as long as the tail (head first, so the width tapers away)
                int lo = Mathf.Max(0, hi - tail);
                int count = hi - lo + 2;
                var l = g.line;
                if (!l.enabled) l.enabled = true;
                l.positionCount = count;
                l.SetPosition(0, head);
                for (int k = 1; k < count; k++) l.SetPosition(k, g.path[hi - k + 1]);
                if (echo && g.kind != KindBolt)
                    HellMotes.Emit(new MoteSpec
                    {
                        shape = MoteShape.Blob, material = echoMat, position = head, life = 0.35f,
                        size = g.kind == KindPlayer ? 0.32f : 0.4f, swell = 0.05f, unscaled = true,
                    });
            }
            if (u >= 1f) EndScrub(true);
        }

        void EndScrub(bool snap)
        {
            _playing = false;
            HellTime.Clear(TimeKey);
            HellFx.SetRewind(0f);
            var ring = HellFx.Glow(HellFxPalette.Rewind, HellFxPalette.RewindGlow);
            for (int i = 0; i < Slots; i++)
            {
                var g = _ghosts[i];
                if (g.on && snap && g.kind == KindBot)
                    HellMotes.Ring(MoteShape.Blob, ring, g.path[PathPoints - 1], 10, 3.5f, 0.32f, 0.22f, 0.5f, 5f, true);
                g.on = false;
                g.line.enabled = false;
                g.head.gameObject.SetActive(false);
            }
            if (!snap) return;
            HellFx.Snap();
            HellFx.FovKick(0.07f, 0.04f, 0.25f);
            HellFeelFx.AddTrauma(snapTrauma);
        }

        static NVec3 N(Vector3 v) => new NVec3(v.x, v.y, v.z);
        static Vector3 U(NVec3 v) => new Vector3(v.X, v.Y, v.Z);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;
    }
}
