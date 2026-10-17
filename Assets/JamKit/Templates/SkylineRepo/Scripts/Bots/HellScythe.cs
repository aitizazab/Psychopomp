using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// One demon scythe in the world (docs/HELL-ENEMIES.md; the pure timeline is <see cref="ScytheBeam"/>). It appears spinning at
    /// the start of its path and PROJECTS the path: a thin red line along it with a bright mote running start to end again and again
    /// (so you read which way the sweep will come), brightening through the telegraph. Then it BEAMS: the blade flies along the line
    /// at the beam speed, a thick white-hot beam filling the path behind it, held a moment, then fading. Touching the lit beam hits
    /// once (<see cref="ISkylineTarget.TakeAttack"/>). Scaled time throughout (Time Dilation and Stasis slow it).
    /// <para>Pooled; rewindable (registers while active; <see cref="HellRewind"/> switches off one that a Rewind goes back
    /// before, through <see cref="IHellRewindSpawnAware"/>).</para>
    /// </summary>
    public sealed class HellScythe : MonoBehaviour, IHellRewindable, IHellRewindSpawnAware
    {
        const int PrewarmCount = 12;
        /// <summary>The player's capsule for the hit test (m): its radius, and the feet-to-head span from the target's root and chest.</summary>
        const float PlayerRadius = 0.38f, HeadAboveChest = 0.45f;
        const float MoteSeconds = 0.32f;
        static readonly List<HellScythe> _pool = new List<HellScythe>();

        ScytheBeam _beam;
        ISkylineTarget _target;
        string _attackerId = "";
        Transform _blade, _mote;
        LineRenderer _line, _beamLine;
        float _spin;

        /// <summary>The scythes active now.</summary>
        public static int ActiveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _pool.Count; i++) if (_pool[i] != null && _pool[i].gameObject.activeSelf) n++;
                return n;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _pool.Clear();

        public ScytheBeam Beam => _beam;

        /// <summary>A scythe on <paramref name="path"/> with a demon's tuning (<see cref="EnemyDef"/> scythe fields).</summary>
        public static HellScythe Spawn(ScythePath path, EnemyDef d, ISkylineTarget target, string attackerId) =>
            Spawn(new ScytheBeam(path, d), target, attackerId);

        /// <summary>A scythe from <paramref name="from"/> to <paramref name="to"/> with its own tuning (a room's beam lane).</summary>
        public static HellScythe Spawn(Vector3 from, Vector3 to, float telegraph, float speed, float hold, float hitRadius, float damage,
            ISkylineTarget target, string attackerId) =>
            Spawn(new ScytheBeam(new ScythePath { from = N(from), to = N(to) }, telegraph, speed, hold, hitRadius, damage), target, attackerId);

        static HellScythe Spawn(ScytheBeam beam, ISkylineTarget target, string attackerId)
        {
            var s = Take();
            s._beam = beam;
            s._target = target;
            s._attackerId = attackerId ?? "";
            s._spin = 0f;
            s.transform.position = U(beam.path.from);
            s.gameObject.SetActive(true);
            s.Draw();
            return s;
        }

        public static void Prewarm()
        {
            Purge();
            int idle = 0;
            for (int i = 0; i < _pool.Count; i++) if (!_pool[i].gameObject.activeSelf) idle++;
            for (; idle < PrewarmCount; idle++) Make();
        }

        /// <summary>Switches off every scythe (a room's restart).</summary>
        public static void ClearAll()
        {
            Purge();
            for (int i = 0; i < _pool.Count; i++) _pool[i].gameObject.SetActive(false);
        }

        static void Purge()
        {
            for (int i = _pool.Count - 1; i >= 0; i--) if (_pool[i] == null) _pool.RemoveAt(i);
        }

        static HellScythe Take()
        {
            Purge();
            for (int i = 0; i < _pool.Count; i++) if (!_pool[i].gameObject.activeSelf) return _pool[i];
            return Make();
        }

        static HellScythe Make()
        {
            var go = new GameObject("Hell scythe");
            go.SetActive(false);
            var s = go.AddComponent<HellScythe>();
            // the spinning blade: two blades back to back, so it reads as a whirling scythe from any side
            s._blade = SkylineBotLook.Empty("Spinner", go.transform, Vector3.zero);
            var bone = HellLook.HostileLit(HellLook.Bone);
            var edge = HellLook.HostileGlow(HellLook.BeamHot);
            // two blades on opposite arms of the wheel (the spin is about the Spinner's local Z, across the lane)
            for (int k = 0; k < 2; k++)
            {
                var arm = SkylineBotLook.Empty("Arm" + k, s._blade, Vector3.zero);
                arm.localRotation = Quaternion.Euler(0f, 0f, k * 180f);
                HellBodies.BuildBlade(arm, new Vector3(0f, 0.3f, 0f), 0.8f, bone, edge);
            }
            SkylineBotLook.Visual(PrimitiveType.Sphere, "Hub", s._blade, HellLook.HostileGlow(HellLook.FlameYellow), false).transform.localScale = Vector3.one * 0.2f;
            s._mote = SkylineBotLook.Visual(PrimitiveType.Sphere, "Mote", null, HellLook.HostileGlow(HellLook.BeamHot), false).transform;
            s._mote.SetParent(go.transform, false);
            s._mote.localScale = Vector3.one * 0.18f;
            s._line = Line(go.transform, "Telegraph line", HellLook.HostileGlow(HellLook.TelegraphLine));
            s._beamLine = Line(go.transform, "Beam", HellLook.HostileGlow(HellLook.BeamHot));
            _pool.Add(s);
            return s;
        }

        static LineRenderer Line(Transform parent, string name, Material m)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<LineRenderer>();
            l.sharedMaterial = m;
            l.useWorldSpace = true;
            l.positionCount = 2;
            l.numCapVertices = 3;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.widthMultiplier = 0f;
            return l;
        }

        void OnEnable() => HellRewindRegistry.Register(this);
        void OnDisable() => HellRewindRegistry.Unregister(this);

        void Update()
        {
            if (_beam == null) { gameObject.SetActive(false); return; }
            float dt = Time.deltaTime;
            if (!(dt > 0f)) return;
            _beam.Tick(dt);
            if (_beam.Phase == ScythePhase.Done) { gameObject.SetActive(false); return; }
            if (SkylineBot.TargetLive(_target) && _target.Root != null && _target.Root.gameObject.activeInHierarchy)
            {
                Vector3 feet = _target.Root.position, head = _target.Chest + Vector3.up * HeadAboveChest;
                Vector3 lo = feet + Vector3.up * PlayerRadius, hi = head - Vector3.up * PlayerRadius;
                if (hi.y < lo.y) hi = lo;
                if (_beam.TryHit(N(lo), N(hi), PlayerRadius)) _target.TakeAttack(_beam.damage, _attackerId, U(_beam.BladePosition));
            }
            _spin += dt * (_beam.Phase == ScythePhase.Telegraph ? 540f : 1440f);
            Draw();
        }

        void Draw()
        {
            Vector3 a = U(_beam.path.from), b = U(_beam.path.to);
            var phase = _beam.Phase;
            Vector3 dir = b - a;
            float len = dir.magnitude;
            Vector3 fwd = len > 1e-4f ? dir / len : Vector3.forward;
            // the blade: grows in over the first 0.2 s, spins about the path's direction (a wheel rolling down the lane), and
            // rides the blade position; it shrinks away in the fade
            float appear = Mathf.Clamp01(_beam.T / 0.2f);
            float fade = phase == ScythePhase.Fade ? 1f - Mathf.Clamp01((_beam.T - (_beam.telegraph + _beam.SweepSeconds + _beam.hold)) / ScytheBeam.FadeSeconds) : 1f;
            transform.position = U(_beam.BladePosition);
            _blade.rotation = Quaternion.LookRotation(fwd) * Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(0f, 0f, _spin);
            _blade.localScale = Vector3.one * (appear * fade);
            if (phase == ScythePhase.Telegraph)
            {
                // the projected path: thin, brightening (widening) through the telegraph, with a mote running down it
                float k = _beam.TelegraphProgress;
                _line.SetPosition(0, a);
                _line.SetPosition(1, b);
                _line.widthMultiplier = Mathf.Lerp(0.04f, 0.12f, k) * (1f + 0.25f * Mathf.Sin(_beam.T * 40f));
                float m = (_beam.T % MoteSeconds) / MoteSeconds;
                _mote.gameObject.SetActive(true);
                _mote.position = Vector3.Lerp(a, b, m);
                _beamLine.widthMultiplier = 0f;
                return;
            }
            _mote.gameObject.SetActive(false);
            _line.widthMultiplier = 0f;
            _beamLine.SetPosition(0, a);
            _beamLine.SetPosition(1, U(_beam.BladePosition));
            float w = phase == ScythePhase.Fade ? 0.4f * fade : 0.4f;
            _beamLine.widthMultiplier = w * (1f + 0.15f * Mathf.Sin(_beam.T * 70f));
        }

        // ------------------------------------------------------------ rewind

        sealed class Snap
        {
            public bool active;
            public ScytheBeam beam;
            public object beamState;
            public ISkylineTarget target;
            public string attackerId;
            public float spin;
        }

        public object Snapshot() => new Snap
        {
            active = gameObject.activeSelf, beam = _beam, beamState = StateCopy.Capture(_beam), target = _target, attackerId = _attackerId, spin = _spin,
        };

        public void Restore(object snapshot)
        {
            if (!(snapshot is Snap s)) return;
            _beam = s.beam;
            StateCopy.Apply(_beam, s.beamState);
            _target = s.target;
            _attackerId = s.attackerId;
            _spin = s.spin;
            if (!s.active || _beam == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            Draw();
        }

        /// <summary>The Rewind went back to before this appeared: switch off quietly.</summary>
        public void RewoundBeforeSpawn() => gameObject.SetActive(false);

        static NVec3 N(Vector3 v) => new NVec3(v.x, v.y, v.z);
        static Vector3 U(NVec3 v) => new Vector3(v.X, v.Y, v.Z);
    }
}
