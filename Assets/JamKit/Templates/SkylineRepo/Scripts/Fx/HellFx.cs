using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>The Hell abilities' sound hooks (docs/HELL-ABILITIES.md). Nothing is mixed here: the audio pass listens.
    /// <see cref="Cue"/> ids: <c>shadowstep_out</c> / <c>shadowstep_in</c> (at the two ends), <c>shadowstep_denied</c>,
    /// <c>rewind</c>, <c>dilation_in</c> / <c>dilation_out</c>, <c>nightfall</c>, <c>combustion</c>, <c>shade</c>,
    /// <c>lava_hop</c>, <c>skill_pick</c>. <see cref="Pitch"/>: the music and effects pitch while time is dilated (1 normal,
    /// down to the dilation's scale). By default each cue also plays <c>hell/&lt;id&gt;</c> from SkylineSounds when that
    /// sound exists.</summary>
    public static class HellAbilityAudio
    {
        public static event Action<string, Vector3> Cue;
        public static event Action<float> PitchChanged;
        public static float Pitch { get; private set; } = 1f;

        public static void Play(string id, Vector3 at)
        {
            Cue?.Invoke(id, at);
            if (SkylineSounds.Get("hell/" + id) != null) SkylineSounds.PlayAt("hell/" + id, at);
        }

        public static void SetPitch(float pitch)
        {
            if (Mathf.Abs(pitch - Pitch) < 1e-3f) return;
            Pitch = pitch;
            PitchChanged?.Invoke(pitch);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Cue = null; PitchChanged = null; Pitch = 1f; }
    }

    /// <summary>The Hell abilities' visuals: dark smoke puffs (Shadow Step's two ends, the Shade), the landing marker, the
    /// Nightfall sphere, Combustion's blast, and the screen layer (Time Dilation's violet edge, the Rewind's desaturated
    /// flash: a URP colour-adjustment volume when post-processing is on, plus a UI overlay that always shows). The comic-ink
    /// way: opaque dark blobs, no particle shaders.</summary>
    public sealed class HellFx : MonoBehaviour
    {
        public static readonly Color Smoke = new Color(0.06f, 0.035f, 0.09f);
        public static readonly Color ShadowHue = new Color32(0x9B, 0x6B, 0xFF, 255);
        public static readonly Color FireHue = new Color32(0xFF, 0x7A, 0x1F, 255);

        static HellFx _instance;
        static HellFx Instance
        {
            get
            {
                if (_instance == null) _instance = new GameObject("HellFx").AddComponent<HellFx>();
                return _instance;
            }
        }

        sealed class Puff { public Transform t; public Vector3 vel; public float age, life, size; }
        readonly List<Puff> _puffs = new List<Puff>();
        Material _smokeMat;

        // ------------------------------------------------------------------ smoke

        /// <summary>A burst of dark smoke blobs at <paramref name="at"/> (feet): they swell, rise a little and shrink away.</summary>
        public static void SmokeBurst(Vector3 at, int count = 14, float spread = 0.7f) => Instance.Burst(at, count, spread);

        void Burst(Vector3 at, int count, float spread)
        {
            if (_smokeMat == null) _smokeMat = SkylineBotLook.Lit(Smoke);
            for (int i = 0; i < count; i++)
            {
                Puff p = null;
                foreach (var q in _puffs) if (!q.t.gameObject.activeSelf) { p = q; break; }
                if (p == null)
                {
                    var go = SkylineBotLook.Visual(PrimitiveType.Sphere, "HellSmoke", transform, _smokeMat, false);
                    p = new Puff { t = go.transform };
                    _puffs.Add(p);
                }
                var r = UnityEngine.Random.insideUnitSphere;
                p.t.position = at + new Vector3(r.x * spread, 0.2f + Mathf.Abs(r.y) * 1.6f, r.z * spread);
                p.vel = new Vector3(r.x, 0.6f + Mathf.Abs(r.y), r.z) * 1.2f;
                p.age = 0f;
                p.life = UnityEngine.Random.Range(0.45f, 0.8f);
                p.size = UnityEngine.Random.Range(0.35f, 0.7f);
                p.t.localScale = Vector3.one * 0.05f;
                p.t.gameObject.SetActive(true);
            }
        }

        // ------------------------------------------------------------------ the update (unscaled: effects keep their pace in slow motion)

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            foreach (var p in _puffs)
            {
                if (!p.t.gameObject.activeSelf) continue;
                p.age += dt;
                float u = p.age / p.life;
                if (u >= 1f) { p.t.gameObject.SetActive(false); continue; }
                p.t.position += p.vel * dt;
                p.vel *= 1f - 2.5f * dt;
                float s = u < 0.25f ? Mathf.Lerp(0.05f, p.size, u / 0.25f) : Mathf.Lerp(p.size, 0f, (u - 0.25f) / 0.75f);
                p.t.localScale = Vector3.one * s;
            }
            TickBlasts(dt);
            TickSpheres(dt);
            TickScreen(dt);
        }

        // ------------------------------------------------------------------ the landing marker

        /// <summary>The Shadow Step marker: a flat ring on the landing and a thin pillar; violet when the landing is valid,
        /// dull red when it isn't. Owned by the caller.</summary>
        public static GameObject MakeMarker(Transform parent)
        {
            var root = new GameObject("HellStepMarker");
            root.transform.SetParent(parent, false);
            var disc = SkylineBotLook.Visual(PrimitiveType.Cylinder, "Disc", root.transform, SkylineBotLook.Glow(ShadowHue * 2.2f), false);
            disc.transform.localScale = new Vector3(0.9f, 0.015f, 0.9f);
            var core = SkylineBotLook.Visual(PrimitiveType.Cylinder, "Core", root.transform, SkylineBotLook.Lit(Smoke), false);
            core.transform.localScale = new Vector3(0.7f, 0.02f, 0.7f);
            var pillar = SkylineBotLook.Visual(PrimitiveType.Cylinder, "Pillar", root.transform, SkylineBotLook.Glow(ShadowHue * 2.2f), false);
            pillar.transform.localScale = new Vector3(0.05f, 0.9f, 0.05f);
            pillar.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            foreach (var c in root.GetComponentsInChildren<Collider>(true)) Destroy(c);
            root.SetActive(false);
            return root;
        }

        public static void TintMarker(GameObject marker, bool valid)
        {
            if (marker == null) return;
            var col = valid ? ShadowHue * 2.2f : new Color(0.7f, 0.08f, 0.08f);
            foreach (var r in marker.GetComponentsInChildren<Renderer>(true))
                if (r.gameObject.name != "Core") r.sharedMaterial = SkylineBotLook.Glow(col);
        }

        // ------------------------------------------------------------------ Combustion's blast

        sealed class Blast { public Transform t; public float age, radius; }
        readonly List<Blast> _blasts = new List<Blast>();

        public static void FireBlast(Vector3 at, float radius) => Instance.MakeBlast(at, radius);

        void MakeBlast(Vector3 at, float radius)
        {
            Blast b = null;
            foreach (var q in _blasts) if (!q.t.gameObject.activeSelf) { b = q; break; }
            if (b == null)
            {
                var go = SkylineBotLook.Visual(PrimitiveType.Sphere, "HellBlast", transform, SkylineBotLook.Glow(FireHue * 3f), false);
                b = new Blast { t = go.transform };
                _blasts.Add(b);
            }
            b.t.position = at;
            b.age = 0f;
            b.radius = radius;
            b.t.gameObject.SetActive(true);
            for (int i = 0; i < 4; i++) SkylineSpark.Burst(at, UnityEngine.Random.onUnitSphere, 6, 7f);
            Burst(at - Vector3.up * 0.5f, 10, radius * 0.4f);
        }

        void TickBlasts(float dt)
        {
            foreach (var b in _blasts)
            {
                if (!b.t.gameObject.activeSelf) continue;
                b.age += dt;
                float u = b.age / 0.3f;
                if (u >= 1f) { b.t.gameObject.SetActive(false); continue; }
                float s = b.radius * 2f * (u < 0.4f ? u / 0.4f : 1f - (u - 0.4f) / 0.6f * 0.9f);
                b.t.localScale = Vector3.one * s;
            }
        }

        // ------------------------------------------------------------------ Nightfall's sphere

        sealed class DarkSphere { public Transform shell; public readonly List<Transform> ring = new List<Transform>(); public Vector3 c; public float r, left, total, spin; }
        readonly List<DarkSphere> _spheres = new List<DarkSphere>();

        public static void NightfallSphere(Vector3 c, float radius, float seconds) => Instance.MakeSphere(c, radius, seconds);

        void MakeSphere(Vector3 c, float r, float seconds)
        {
            var d = new DarkSphere { c = c, r = r, left = seconds, total = seconds };
            if (_smokeMat == null) _smokeMat = SkylineBotLook.Lit(Smoke);
            // the ink rim: dark blobs swirling round the edge (no transparent shader: a build strips variants no material asset uses, see SkylineBotLook.Glow)
            for (int i = 0; i < 44; i++)
            {
                var go = SkylineBotLook.Visual(PrimitiveType.Sphere, "NightfallRim", transform, _smokeMat, false);
                foreach (var col in go.GetComponentsInChildren<Collider>(true)) Destroy(col);
                d.ring.Add(go.transform);
            }
            _spheres.Add(d);
            Burst(c, 20, r * 0.5f);
        }

        void TickSpheres(float dt)
        {
            float gdt = Time.deltaTime;
            for (int i = _spheres.Count - 1; i >= 0; i--)
            {
                var d = _spheres[i];
                d.left -= gdt;
                d.spin += dt * 0.6f;
                float grow = Mathf.Clamp01((d.total - d.left) / 0.35f), fade = Mathf.Clamp01(d.left / 0.5f);
                float k = grow * fade;
                if (d.shell != null) d.shell.localScale = Vector3.one * (d.r * 2f * k);
                for (int j = 0; j < d.ring.Count; j++)
                {
                    float a = j * 2.399963f + d.spin * (j % 2 == 0 ? 1f : -1f);   // golden-angle spread over the sphere
                    float y = 1f - 2f * (j + 0.5f) / d.ring.Count;
                    float rad = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
                    var dir = new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad);
                    d.ring[j].position = d.c + dir * d.r * k;
                    d.ring[j].localScale = Vector3.one * (0.6f + 0.3f * Mathf.Sin(d.spin * 3f + j)) * k;
                }
                if (d.left <= 0f)
                {
                    if (d.shell != null) d.shell.gameObject.SetActive(false);
                    foreach (var t in d.ring) Destroy(t.gameObject);
                    _spheres.RemoveAt(i);
                }
            }
        }

        // ------------------------------------------------------------------ the screen

        VisualElement _tint, _rewindVeil;
        Texture2D _edgeTex;
        Volume _volume;
        ColorAdjustments _grade;
        float _dilation, _rewindLeft;
        const float RewindSeconds = 0.3f;

        /// <summary>Time Dilation's strength now (0..1): a violet edge and a slightly drained picture.</summary>
        public static void SetDilation(float strength) => Instance._dilation = Mathf.Clamp01(strength);

        /// <summary>The Rewind's 0.3 s desaturated reverse flash.</summary>
        public static void RewindFlash() => Instance._rewindLeft = RewindSeconds;

        void EnsureScreen()
        {
            if (_tint == null)
            {
                var hud = SkylineHud.Instance;
                if (hud != null && hud.Root != null)
                {
                    _edgeTex = EdgeTexture();
                    _tint = Layer(hud.Root);
                    _tint.style.backgroundImage = new StyleBackground(_edgeTex);
                    _tint.style.unityBackgroundImageTintColor = new Color(ShadowHue.r, ShadowHue.g, ShadowHue.b, 1f);
                    _rewindVeil = Layer(hud.Root);
                    _rewindVeil.style.backgroundColor = new Color(0.55f, 0.55f, 0.58f, 1f);
                }
            }
            if (_volume == null)
            {
                var go = new GameObject("HellGrade");
                go.transform.SetParent(transform, false);
                _volume = go.AddComponent<Volume>();
                _volume.isGlobal = true;
                _volume.priority = 50f;
                _volume.weight = 0f;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _grade = profile.Add<ColorAdjustments>(true);
                _grade.saturation.Override(-100f);
                _grade.contrast.Override(18f);
                _volume.sharedProfile = profile;
            }
        }

        static VisualElement Layer(VisualElement parent)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute;
            e.style.left = 0; e.style.top = 0; e.style.right = 0; e.style.bottom = 0;
            e.style.opacity = 0f;
            parent.Add(e);
            return e;
        }

        static Texture2D EdgeTexture()
        {
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((d - 0.55f) / 0.55f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            t.Apply();
            return t;
        }

        void TickScreen(float dt)
        {
            if (_dilation <= 0f && _rewindLeft <= 0f && _tint == null) return;
            EnsureScreen();
            if (_rewindLeft > 0f) _rewindLeft = Mathf.Max(0f, _rewindLeft - dt);
            float rw = _rewindLeft > 0f ? Mathf.Sin(_rewindLeft / RewindSeconds * Mathf.PI) : 0f;
            if (_tint != null) _tint.style.opacity = _dilation * 0.55f;
            if (_rewindVeil != null) _rewindVeil.style.opacity = rw * 0.42f;
            if (_volume != null) _volume.weight = Mathf.Max(rw, _dilation * 0.35f);
        }

        void OnDestroy()
        {
            if (_edgeTex != null) Destroy(_edgeTex);
            if (_instance == this) _instance = null;
        }
    }
}
