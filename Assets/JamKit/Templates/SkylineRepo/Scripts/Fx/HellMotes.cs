using System.Collections.Generic;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>How a mote changes over its life.</summary>
    public enum MoteShape
    {
        /// <summary>A sphere that swells, then shrinks away (ink smoke, embers, glow bubbles).</summary>
        Blob,
        /// <summary>A cube stretched along its velocity that thins away (sparks, speed streaks, ink shards, tendrils).</summary>
        Streak,
        /// <summary>A flat cube lying on the ground that swells and thins (ink splats, cracks, dust skirts).</summary>
        Splat,
    }

    /// <summary>One mote to emit (a value: filling one never allocates).</summary>
    public struct MoteSpec
    {
        public MoteShape shape;
        public Material material;
        public Vector3 position, velocity;
        public float life, size;
        /// <summary>Downward acceleration (m/s²; negative rises).</summary>
        public float gravity;
        /// <summary>Velocity lost per second (0..~6).</summary>
        public float drag;
        /// <summary>The share of the life spent swelling (0.25 by default when 0).</summary>
        public float swell;
        /// <summary>Streak: length per m/s of speed (0.04 by default when 0); Splat: the thickness ratio (0.06 when 0).</summary>
        public float stretch;
        /// <summary>Splat: its yaw (degrees).</summary>
        public float yaw;
        /// <summary>Runs on unscaled time (screen-speed effects that must not slow with Time Dilation).</summary>
        public bool unscaled;
    }

    /// <summary>
    /// The pooled motes every Hell effect draws with (docs/HELL-FX.md): opaque primitives (the comic-ink way: no particle
    /// shaders, nothing a build can strip), a fixed pool per shape filled once (<see cref="Prewarm"/>), the oldest taken over
    /// when full. Nothing is created or destroyed per frame. <see cref="ClearAll"/> runs on every Rewind, so no effect from the
    /// abandoned future survives it.
    /// </summary>
    public sealed class HellMotes : MonoBehaviour
    {
        public const int SphereCount = 320, CubeCount = 320;

        sealed class Mote
        {
            public Transform t;
            public MeshRenderer r;
            public MoteShape shape;
            public Vector3 vel;
            public float age, life, size, gravity, drag, swell, stretch, yaw;
            public bool unscaled, live;
        }

        static HellMotes _instance;
        readonly List<Mote> _spheres = new List<Mote>(SphereCount);
        readonly List<Mote> _cubes = new List<Mote>(CubeCount);
        int _nextSphere, _nextCube;

        public static int LiveCount { get { var i = _instance; if (i == null) return 0; int n = 0; foreach (var m in i._spheres) if (m.live) n++; foreach (var m in i._cubes) if (m.live) n++; return n; } }

        static HellMotes Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("HellMotes");
                    _instance = go.AddComponent<HellMotes>();
                    _instance.Fill();
                }
                return _instance;
            }
        }

        /// <summary>Fills the pools now (at scene start), so nothing is created mid-fight.</summary>
        public static void Prewarm() { var _ = Instance; }

        void OnEnable() => HellRewind.Rewound += ClearAll;
        void OnDisable() => HellRewind.Rewound -= ClearAll;

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        void Fill()
        {
            var placeholder = SkylineBotLook.Lit(HellFxPalette.Ink);
            for (int i = 0; i < SphereCount; i++) _spheres.Add(Make(PrimitiveType.Sphere, placeholder));
            for (int i = 0; i < CubeCount; i++) _cubes.Add(Make(PrimitiveType.Cube, placeholder));
        }

        Mote Make(PrimitiveType type, Material mat)
        {
            var go = SkylineBotLook.Visual(type, "HellMote", transform, mat, false);
            go.SetActive(false);
            return new Mote { t = go.transform, r = go.GetComponent<MeshRenderer>() };
        }

        /// <summary>Emits one mote (taking over the oldest of its shape's pool when all are live).</summary>
        public static void Emit(in MoteSpec s)
        {
            if (s.material == null || !(s.life > 0f) || !(s.size > 0f)) return;
            var me = Instance;
            Mote m;
            if (s.shape == MoteShape.Blob) { m = me._spheres[me._nextSphere]; me._nextSphere = (me._nextSphere + 1) % me._spheres.Count; }
            else { m = me._cubes[me._nextCube]; me._nextCube = (me._nextCube + 1) % me._cubes.Count; }
            m.shape = s.shape;
            m.vel = s.velocity;
            m.age = 0f;
            m.life = s.life;
            m.size = s.size;
            m.gravity = s.gravity;
            m.drag = s.drag;
            m.swell = s.swell > 0f ? s.swell : 0.25f;
            m.stretch = s.stretch > 0f ? s.stretch : (s.shape == MoteShape.Splat ? 0.06f : 0.04f);
            m.yaw = s.yaw;
            m.unscaled = s.unscaled;
            m.live = true;
            if (m.r.sharedMaterial != s.material) m.r.sharedMaterial = s.material;
            m.t.position = s.position;
            m.t.localScale = Vector3.one * 0.001f;
            if (!m.t.gameObject.activeSelf) m.t.gameObject.SetActive(true);
            Pose(m, 0f);
        }

        /// <summary>Switches every mote off (a Rewind, a scene change).</summary>
        public static void ClearAll()
        {
            var me = _instance;
            if (me == null) return;
            foreach (var m in me._spheres) Kill(m);
            foreach (var m in me._cubes) Kill(m);
        }

        static void Kill(Mote m)
        {
            if (!m.live) return;
            m.live = false;
            m.t.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime, udt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            Tick(_spheres, dt, udt);
            Tick(_cubes, dt, udt);
        }

        static void Tick(List<Mote> list, float dt, float udt)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (!m.live) continue;
                float d = m.unscaled ? udt : dt;
                if (d <= 0f) continue;
                m.age += d;
                if (m.age >= m.life) { Kill(m); continue; }
                m.vel.y -= m.gravity * d;
                if (m.drag > 0f) m.vel *= Mathf.Max(0f, 1f - m.drag * d);
                m.t.position += m.vel * d;
                Pose(m, m.age / m.life);
            }
        }

        static void Pose(Mote m, float u)
        {
            float k = u < m.swell ? Mathf.Lerp(0.15f, 1f, u / m.swell) : 1f - Skyline.HellFxCurves.EaseIn((u - m.swell) / (1f - m.swell), 1.6f);
            float s = m.size * Mathf.Max(0.001f, k);
            switch (m.shape)
            {
                case MoteShape.Blob:
                    m.t.localScale = new Vector3(s, s, s);
                    break;
                case MoteShape.Streak:
                {
                    float speed = m.vel.magnitude;
                    if (speed > 1e-3f) m.t.rotation = Quaternion.LookRotation(m.vel / speed);
                    float len = Mathf.Max(s, speed * m.stretch * (0.4f + 0.6f * k));
                    m.t.localScale = new Vector3(s * 0.35f, s * 0.35f, len);
                    break;
                }
                case MoteShape.Splat:
                    m.t.rotation = Quaternion.Euler(0f, m.yaw, 0f);
                    m.t.localScale = new Vector3(m.size * (0.4f + 0.6f * Skyline.HellFxCurves.EaseOut(u * 3f)), m.size * m.stretch * k, m.size * (0.4f + 0.6f * Skyline.HellFxCurves.EaseOut(u * 3f)) * 0.55f);
                    break;
            }
        }

        // ------------------------------------------------------------------ the common recipes

        /// <summary>A spray of <paramref name="count"/> motes from <paramref name="at"/> along <paramref name="dir"/> (a cone of
        /// <paramref name="spread"/>, 0 straight, 1 hemisphere-ish).</summary>
        public static void Spray(MoteShape shape, Material mat, Vector3 at, Vector3 dir, float spread, int count, float speedLo, float speedHi,
            float life, float size, float gravity = 0f, float drag = 1.5f, bool unscaled = false)
        {
            count = Mathf.Max(0, Mathf.RoundToInt(count * HellFxPalette.Density));
            Vector3 n = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector3.up;
            for (int i = 0; i < count; i++)
            {
                Vector3 d = (n + Random.insideUnitSphere * spread).normalized;
                Emit(new MoteSpec
                {
                    shape = shape, material = mat, position = at, velocity = d * Random.Range(speedLo, speedHi),
                    life = life * Random.Range(0.7f, 1.15f), size = size * Random.Range(0.7f, 1.2f), gravity = gravity, drag = drag,
                    yaw = Random.Range(0f, 360f), unscaled = unscaled,
                });
            }
        }

        /// <summary>A ring of <paramref name="count"/> motes thrown outward flat from <paramref name="at"/> (a shockwave that
        /// reads opaque: no transparent shader).</summary>
        public static void Ring(MoteShape shape, Material mat, Vector3 at, int count, float speed, float life, float size, float lift = 0f,
            float drag = 4f, bool unscaled = false)
        {
            count = Mathf.Max(3, Mathf.RoundToInt(count * HellFxPalette.Density));
            float a0 = Random.Range(0f, Mathf.PI * 2f);
            for (int i = 0; i < count; i++)
            {
                float a = a0 + i * Mathf.PI * 2f / count;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Emit(new MoteSpec
                {
                    shape = shape, material = mat, position = at + d * 0.2f, velocity = d * speed * Random.Range(0.85f, 1.1f) + Vector3.up * lift,
                    life = life, size = size, drag = drag, swell = 0.15f, yaw = -a * Mathf.Rad2Deg, unscaled = unscaled,
                });
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _instance = null;
    }
}
