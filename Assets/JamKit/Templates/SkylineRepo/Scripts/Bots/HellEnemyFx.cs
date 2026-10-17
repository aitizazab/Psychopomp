using System.Collections.Generic;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>The Hell enemies' effects (docs/HELL-ENEMIES.md; named apart from the abilities' <see cref="HellFx"/>): a death becomes a burst of fire and smoke with embers, a fire bolt
    /// strikes with a small flare. Cosmetic (not rewound). Pooled, filled up front by <see cref="Prewarm"/> with the scythes and lava
    /// pools, so nothing is created mid-fight.</summary>
    public static class HellEnemyFx
    {
        public static void Prewarm()
        {
            HellFlame.Prewarm();
            HellScythe.Prewarm();
            HellLavaPool.Prewarm();
            HellLook.Glow(HellLook.CoreHot);
            HellLook.Glow(HellLook.EmberHot);
        }

        /// <summary>A death: a ball of fire swelling and rising, licks of flame thrown out, thick smoke and a spray of embers.
        /// <paramref name="size"/> is the body's rough radius (m).</summary>
        public static void DeathBurst(Vector3 at, float size)
        {
            size = Mathf.Max(0.3f, size);
            HellFlame.Emit(at, Vector3.up * 0.6f, size * 1.6f, 0.55f, HellLook.FlameYellow);
            for (int i = 0; i < 10; i++)
            {
                Vector3 dir = (Random.insideUnitSphere + Vector3.up * 0.6f).normalized;
                HellFlame.Emit(at + dir * size * 0.3f, dir * Random.Range(1.5f, 3.5f) * size + Vector3.up * 0.8f, size * Random.Range(0.5f, 0.9f),
                    Random.Range(0.45f, 0.8f), i % 3 == 0 ? HellLook.FlameDeep : HellLook.Flame);
            }
            for (int i = 0; i < 7; i++)
                SkylineSmokePuff.Emit(at + Random.insideUnitSphere * size * 0.6f + Vector3.up * size * 0.3f, size * Random.Range(0.35f, 0.6f));
            SkylineSpark.Burst(at, Vector3.up, 18, 6f);
        }

        /// <summary>A fire bolt striking: a small flare and a few embers.</summary>
        public static void EmberHit(Vector3 at, Vector3 normal)
        {
            Vector3 n = normal.sqrMagnitude > 1e-6f ? normal.normalized : Vector3.up;
            HellFlame.Emit(at, n * 0.8f, 0.35f, 0.25f, HellLook.FlameYellow);
            HellFlame.Emit(at + n * 0.1f, n * 1.5f + Vector3.up, 0.22f, 0.35f, HellLook.Flame);
            SkylineSpark.Burst(at, n, 6, 4f);
            SkylineSounds.PlayAt(JamKit.Skyline.HellSoundCues.BoltHit, at);   // the sound pass (docs/HELL-SOUND-PASS.md)
            HellShadowFire.Impact(at);   // a bolt at your hiding spot floods it (docs/HELL-SHADOW.md)
        }
    }

    /// <summary>One lick of flame: a glowing ball that swells, drifts and shrinks away. Pooled, oldest taken over when full.</summary>
    public sealed class HellFlame : MonoBehaviour
    {
        const int PoolSize = 64;
        static readonly List<HellFlame> _pool = new List<HellFlame>();
        static int _next;

        float _age, _life, _size;
        Vector3 _vel;
        MeshRenderer _r;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _pool.Clear();
            _next = 0;
        }

        public static void Prewarm()
        {
            Purge();
            while (_pool.Count < 24) Make().gameObject.SetActive(false);
        }

        static void Purge()
        {
            for (int i = _pool.Count - 1; i >= 0; i--) if (_pool[i] == null) _pool.RemoveAt(i);
        }

        static HellFlame Make()
        {
            var go = SkylineBotLook.Visual(PrimitiveType.Sphere, "Flame", null, HellLook.Glow(HellLook.Flame), false);
            var f = go.AddComponent<HellFlame>();
            f._r = go.GetComponent<MeshRenderer>();
            _pool.Add(f);
            return f;
        }

        public static void Emit(Vector3 at, Vector3 velocity, float size, float life, Color hdr)
        {
            Purge();
            HellFlame f = null;
            for (int i = 0; i < _pool.Count; i++) if (!_pool[i].gameObject.activeSelf) { f = _pool[i]; break; }
            if (f == null)
            {
                if (_pool.Count < PoolSize) f = Make();
                else
                {
                    _next = (_next + 1) % _pool.Count;
                    f = _pool[_next];
                }
            }
            f._r.sharedMaterial = HellLook.Glow(hdr);
            f._age = 0f;
            f._life = Mathf.Max(0.05f, life);
            f._size = Mathf.Max(0.02f, size);
            f._vel = velocity;
            f.transform.position = at;
            f.transform.localScale = Vector3.one * f._size * 0.3f;
            f.gameObject.SetActive(true);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            float k = _age / _life;
            if (k >= 1f) { gameObject.SetActive(false); return; }
            _vel *= Mathf.Exp(-2.5f * dt);
            _vel += Vector3.up * 1.5f * dt;   // heat rises
            transform.position += _vel * dt;
            // swells fast, then shrinks away
            float s = k < 0.25f ? Mathf.Lerp(0.3f, 1f, k / 0.25f) : Mathf.Lerp(1f, 0f, (k - 0.25f) / 0.75f);
            transform.localScale = new Vector3(s, s * 1.3f, s) * _size;
        }
    }
}
