using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The lava ooze's pool in the world (docs/HELL-ENEMIES.md; the pure timeline is <see cref="LavaPool"/>). It splashes out to its
    /// radius, glows and bubbles while it burns (damage ticks while the target's feet are in it and on its surface: a jump clears it),
    /// then darkens to a crust, shrinks a little and sinks away. Scaled time throughout.
    /// <para>Pooled; rewindable (registers while active; <see cref="HellRewind"/> switches off one that a Rewind goes back
    /// before, through <see cref="IHellRewindSpawnAware"/>).</para>
    /// </summary>
    public sealed class HellLavaPool : MonoBehaviour, IHellRewindable, IHellRewindSpawnAware
    {
        const int PrewarmCount = 8, Bubbles = 6;
        static readonly List<HellLavaPool> _pool = new List<HellLavaPool>();
        static MaterialPropertyBlock _block;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"), ColorId = Shader.PropertyToID("_Color");

        LavaPool _lava;
        ISkylineTarget _target;
        string _attackerId = "";
        Transform _disc, _inner;
        MeshRenderer _discR, _innerR;
        readonly Transform[] _bubbles = new Transform[Bubbles];
        readonly float[] _bubblePhase = new float[Bubbles];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _pool.Clear();

        public LavaPool Lava => _lava;

        /// <summary>A pool at <paramref name="ground"/> (a floor point) with the ooze's tuning.</summary>
        public static HellLavaPool Spawn(Vector3 ground, EnemyDef d, ISkylineTarget target, string attackerId)
        {
            var p = Take();
            p._lava = new LavaPool(N(ground), d);
            p._target = target;
            p._attackerId = attackerId ?? "";
            p.transform.position = ground;
            for (int i = 0; i < Bubbles; i++) p._bubblePhase[i] = Random.value;
            p.gameObject.SetActive(true);
            p.Draw();
            return p;
        }

        public static void Prewarm()
        {
            Purge();
            int idle = 0;
            for (int i = 0; i < _pool.Count; i++) if (!_pool[i].gameObject.activeSelf) idle++;
            for (; idle < PrewarmCount; idle++) Make();
        }

        public static void ClearAll()
        {
            Purge();
            for (int i = 0; i < _pool.Count; i++) _pool[i].gameObject.SetActive(false);
        }

        static void Purge()
        {
            for (int i = _pool.Count - 1; i >= 0; i--) if (_pool[i] == null) _pool.RemoveAt(i);
        }

        static HellLavaPool Take()
        {
            Purge();
            for (int i = 0; i < _pool.Count; i++) if (!_pool[i].gameObject.activeSelf) return _pool[i];
            return Make();
        }

        static HellLavaPool Make()
        {
            var go = new GameObject("Hell lava pool");
            go.SetActive(false);
            var p = go.AddComponent<HellLavaPool>();
            // a flat disc of glowing lava with a hotter middle, in the hostile ink role so its edge is outlined like an enemy's
            var disc = SkylineBotLook.Visual(PrimitiveType.Cylinder, "Lava", go.transform, HellLook.HostileGlow(HellLook.Lava), false);
            p._disc = disc.transform;
            p._discR = disc.GetComponent<MeshRenderer>();
            var inner = SkylineBotLook.Visual(PrimitiveType.Cylinder, "Hot", go.transform, HellLook.HostileGlow(HellLook.LavaHot), false);
            p._inner = inner.transform;
            p._innerR = inner.GetComponent<MeshRenderer>();
            for (int i = 0; i < Bubbles; i++)
            {
                var b = SkylineBotLook.Visual(PrimitiveType.Sphere, "Bubble", go.transform, HellLook.Glow(HellLook.LavaHot), false);
                p._bubbles[i] = b.transform;
            }
            _pool.Add(p);
            return p;
        }

        void OnEnable() => HellRewindRegistry.Register(this);
        void OnDisable() => HellRewindRegistry.Unregister(this);

        void Update()
        {
            if (_lava == null) { gameObject.SetActive(false); return; }
            float dt = Time.deltaTime;
            if (!(dt > 0f)) return;
            bool inside = false;
            if (SkylineBot.TargetLive(_target) && _target.Root != null && _target.Root.gameObject.activeInHierarchy)
                inside = _lava.Contains(N(_target.Root.position));
            float dmg = _lava.Tick(dt, inside);
            if (dmg > 0f) _target.TakeAttack(dmg, _attackerId, transform.position + Vector3.up * 0.2f);
            if (_lava.Gone) { gameObject.SetActive(false); return; }
            for (int i = 0; i < Bubbles; i++) _bubblePhase[i] += dt * (0.9f + 0.25f * i);
            Draw();
        }

        void Draw()
        {
            float r = _lava.CurrentRadius, cooled = _lava.Cooled;
            // the last 30% of the cooling sinks it into the floor
            float sink = Mathf.Clamp01((cooled - 0.7f) / 0.3f);
            _disc.localPosition = new Vector3(0f, 0.015f - 0.03f * sink, 0f);
            _disc.localScale = new Vector3(r * 2f, 0.012f, r * 2f);
            _inner.localPosition = new Vector3(0f, 0.022f - 0.03f * sink, 0f);
            _inner.localScale = new Vector3(r * 1.2f, 0.012f, r * 1.2f) * (1f - cooled);
            if (_block == null) _block = new MaterialPropertyBlock();
            Color c = Color.Lerp(HellLook.Lava, HellLook.LavaCold, cooled);
            _block.SetColor(BaseColorId, c);
            _block.SetColor(ColorId, c);
            _discR.SetPropertyBlock(cooled > 0f ? _block : null);
            _innerR.enabled = cooled < 0.95f;
            // bubbles rise, swell and pop while it burns
            for (int i = 0; i < Bubbles; i++)
            {
                var b = _bubbles[i];
                bool on = _lava.Burning;
                b.gameObject.SetActive(on);
                if (!on) continue;
                float ph = _bubblePhase[i] % 1f;
                float a = i * 2.39996f + Mathf.Floor(_bubblePhase[i]) * 1.7f;
                float d = r * (0.2f + 0.6f * ((i * 0.37f + Mathf.Floor(_bubblePhase[i]) * 0.53f) % 1f));
                b.localPosition = new Vector3(Mathf.Sin(a) * d, 0.03f + ph * 0.05f, Mathf.Cos(a) * d);
                b.localScale = Vector3.one * (0.06f + 0.14f * ph) * (ph > 0.9f ? 0.3f : 1f);
            }
        }

        // ------------------------------------------------------------ rewind

        sealed class Snap
        {
            public bool active;
            public LavaPool lava;
            public object lavaState;
            public ISkylineTarget target;
            public string attackerId;
            public Vector3 position;
        }

        readonly HellSnapPool<Snap> _snaps = new HellSnapPool<Snap>();

        public object Snapshot()
        {
            var s = _snaps.Next();   // pooled: no new snapshot objects per fixed step
            s.lavaState = StateCopy.Capture(_lava, s.lavaState);
            s.active = gameObject.activeSelf; s.lava = _lava; s.target = _target; s.attackerId = _attackerId; s.position = transform.position;
            return s;
        }

        public void Restore(object snapshot)
        {
            if (!(snapshot is Snap s)) return;
            _lava = s.lava;
            StateCopy.Apply(_lava, s.lavaState);
            _target = s.target;
            _attackerId = s.attackerId;
            transform.position = s.position;
            if (!s.active || _lava == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            Draw();
        }

        /// <summary>The Rewind went back to before this appeared: switch off quietly.</summary>
        public void RewoundBeforeSpawn() => gameObject.SetActive(false);

        static NVec3 N(Vector3 v) => new NVec3(v.x, v.y, v.z);
    }
}
