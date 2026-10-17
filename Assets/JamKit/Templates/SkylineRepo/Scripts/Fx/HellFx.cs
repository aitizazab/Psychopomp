using System;
using System.Collections.Generic;
using JamKit.Skyline;
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

    /// <summary>
    /// The Hell abilities' visuals (docs/HELL-FX.md): ink smoke (through <see cref="HellMotes"/>), the Shadow Step marker
    /// (<see cref="HellStepMarkerFx"/>), the Nightfall sphere, Combustion's blast, and <b>the screen layer</b>: one URP volume
    /// (colour adjustments, chromatic aberration, lens distortion, vignette: all inside URP's one uber post pass, so no extra
    /// full-screen pass) mixed from channels (Time Dilation, the Rewind, its snap, the Shadow Step, a kill) by
    /// <see cref="ScreenGradeMix"/>, plus a few UI Toolkit overlays that always show (edge tints, speed streaks, the Rewind's
    /// tape bars, a flash veil) and the camera's field-of-view kick. Colours come from <see cref="HellFxPalette"/>.
    /// Also boots the effect rig (<see cref="HellRewindFx"/>, <see cref="HellStepFx"/>, <see cref="HellFeelFx"/>,
    /// <see cref="HellWorldFx"/>) on every scene with a player.
    /// </summary>
    [DefaultExecutionOrder(32000)]   // after the movement camera and the viewmodel have set the field of view
    public sealed class HellFx : MonoBehaviour
    {
        public static Color Smoke => HellFxPalette.Ink;
        public static Color ShadowHue => HellFxPalette.Shadow;
        public static Color FireHue => HellFxPalette.Fire;

        static HellFx _instance;
        static HellFx Instance
        {
            get
            {
                if (_instance == null) _instance = new GameObject("HellFx").AddComponent<HellFx>();
                return _instance;
            }
        }

        // ------------------------------------------------------------------ the rig

        /// <summary>Off: no effect rig is added on scene load (tests, tools).</summary>
        public static bool AutoRig = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void BootOnLoad()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Boot();
        }

        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => Boot();

        /// <summary>Adds the effect rig when the scene has a player that gets the Hell abilities (same rule as
        /// <see cref="HellAbilities.AttachAll"/>).</summary>
        public static void Boot()
        {
            if (!AutoRig || !HellAbilities.AutoAttach) return;
            if (SkylineRunArgs.Demo && !SkylineRunArgs.Has("-hell-abilities")) return;
            if (FindAnyObjectByType<SkylinePlayer>() == null) return;
            if (FindAnyObjectByType<HellRewindFx>() != null) return;
            var rig = new GameObject("HellFxRig");
            rig.AddComponent<HellRewindFx>();
            rig.AddComponent<HellStepFx>();
            rig.AddComponent<HellFeelFx>();
            rig.AddComponent<HellWorldFx>();
            HellMotes.Prewarm();
            var _ = Instance;
        }

        void OnEnable() => HellRewind.Rewound += OnRewound;
        void OnDisable() => HellRewind.Rewound -= OnRewound;

        void OnRewound()
        {
            // no effect from the abandoned future survives (the motes clear themselves)
            foreach (var b in _blasts) b.t.gameObject.SetActive(false);
            _stepAt = _killAt = -10f;
            _fovKickAt = -10f;
        }

        // ------------------------------------------------------------------ materials

        /// <summary>A shared unlit glow of the palette colour at its strength.</summary>
        public static Material Glow(Color c, float glow) => SkylineBotLook.Glow(HellFxPalette.Hdr(c, glow));
        /// <summary>A shared lit colour (ink, dust).</summary>
        public static Material Lit(Color c) => SkylineBotLook.Lit(c);

        // ------------------------------------------------------------------ smoke

        /// <summary>A burst of dark ink smoke blobs at <paramref name="at"/> (feet): they swell, rise a little and shrink away.</summary>
        public static void SmokeBurst(Vector3 at, int count = 14, float spread = 0.7f)
        {
            var mat = Lit(HellFxPalette.Ink);
            count = Mathf.Max(1, Mathf.RoundToInt(count * HellFxPalette.Density));
            for (int i = 0; i < count; i++)
            {
                var r = UnityEngine.Random.insideUnitSphere;
                HellMotes.Emit(new MoteSpec
                {
                    shape = MoteShape.Blob, material = mat,
                    position = at + new Vector3(r.x * spread, 0.2f + Mathf.Abs(r.y) * 1.6f, r.z * spread),
                    velocity = new Vector3(r.x, 0.6f + Mathf.Abs(r.y), r.z) * 1.2f,
                    life = UnityEngine.Random.Range(0.45f, 0.8f), size = UnityEngine.Random.Range(0.35f, 0.7f), drag = 2.5f, unscaled = true,
                });
            }
        }

        // ------------------------------------------------------------------ the landing marker

        /// <summary>The Shadow Step marker (owned by the caller): an ink disc ringed by violet shards that turn, a thin pillar,
        /// and the dark tendril from the player's hand to it (<see cref="HellStepMarkerFx"/>); violet when the landing is
        /// valid, dull red when it isn't.</summary>
        public static GameObject MakeMarker(Transform parent)
        {
            var root = new GameObject("HellStepMarker");
            root.transform.SetParent(parent, false);
            root.AddComponent<HellStepMarkerFx>().Build();
            root.SetActive(false);
            return root;
        }

        public static void TintMarker(GameObject marker, bool valid)
        {
            if (marker != null && marker.TryGetComponent<HellStepMarkerFx>(out var fx)) fx.SetValid(valid);
        }

        // ------------------------------------------------------------------ Combustion's blast

        sealed class Blast { public Transform t; public float age, radius; }
        readonly List<Blast> _blasts = new List<Blast>();

        public static void FireBlast(Vector3 at, float radius) { HellShadowFire.Blast(at, radius); Instance.MakeBlast(at, radius); }

        void MakeBlast(Vector3 at, float radius)
        {
            Blast b = null;
            foreach (var q in _blasts) if (!q.t.gameObject.activeSelf) { b = q; break; }
            if (b == null)
            {
                var go = SkylineBotLook.Visual(PrimitiveType.Sphere, "HellBlast", transform, Glow(HellFxPalette.Fire, 3f), false);
                b = new Blast { t = go.transform };
                _blasts.Add(b);
            }
            b.t.position = at;
            b.age = 0f;
            b.radius = radius;
            b.t.gameObject.SetActive(true);
            for (int i = 0; i < 4; i++) SkylineSpark.Burst(at, UnityEngine.Random.onUnitSphere, 6, 7f);
            var fire = Glow(HellFxPalette.Fire, 2.6f);
            HellMotes.Ring(MoteShape.Blob, fire, at - Vector3.up * 0.4f, 16, radius * 2.6f, 0.4f, 0.55f, 0.6f);
            HellMotes.Spray(MoteShape.Streak, Glow(HellFxPalette.Ember, 1f), at, Vector3.up, 0.9f, 18, 5f, 11f, 0.7f, 0.07f, 9f, 0.6f);
            SmokeBurst(at - Vector3.up * 0.5f, 10, radius * 0.4f);
            HellFeelFx.ShakeAt(at, 0.35f, 4f, 22f);
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

        sealed class DarkSphere { public readonly List<Transform> ring = new List<Transform>(); public Vector3 c; public float r, left, total, spin; }
        readonly List<DarkSphere> _spheres = new List<DarkSphere>();

        public static void NightfallSphere(Vector3 c, float radius, float seconds) => Instance.MakeSphere(c, radius, seconds);

        void MakeSphere(Vector3 c, float r, float seconds)
        {
            var d = new DarkSphere { c = c, r = r, left = seconds, total = seconds };
            var ink = Lit(HellFxPalette.Ink);
            var glow = Glow(HellFxPalette.Shadow, HellFxPalette.ShadowGlow);
            // the ink rim: dark blobs swirling round the edge, every fifth a violet ember so the edge reads against black stone
            for (int i = 0; i < 44; i++)
            {
                var go = SkylineBotLook.Visual(PrimitiveType.Sphere, "NightfallRim", transform, i % 5 == 0 ? glow : ink, false);
                d.ring.Add(go.transform);
            }
            _spheres.Add(d);
            SmokeBurst(c, 20, r * 0.5f);
            HellMotes.Ring(MoteShape.Blob, ink, c, 24, r * 3f, 0.5f, 0.8f, 0f, 5f);
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
                float k = HellFxCurves.BackOut(grow) * fade;
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
                    foreach (var t in d.ring) Destroy(t.gameObject);
                    _spheres.RemoveAt(i);
                }
            }
        }

        // ------------------------------------------------------------------ the frame

        void LateUpdate()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            TickBlasts(dt);
            TickSpheres(dt);
            TickScreen(dt);
            TickFov();
        }

        // ------------------------------------------------------------------ the screen: channels

        float _dilation, _rewind, _speedLines;
        float _stepAt = -10f, _killAt = -10f, _killK, _snapAt = -10f, _flashAt = -10f, _flashSeconds = 0.2f, _flashK;
        Color _flashColour = Color.white;

        /// <summary>Time Dilation's strength now (0..1): a deep blue-violet grade, a violet edge, a vignette.</summary>
        public static void SetDilation(float strength) => Instance._dilation = Mathf.Clamp01(strength);

        /// <summary>Time Dilation's strength as last set (0..1).</summary>
        public static float DilationNow => _instance != null ? _instance._dilation : 0f;

        /// <summary>The Rewind's scrub strength now (0..1, set every frame by <see cref="HellRewindFx"/>).</summary>
        public static void SetRewind(float strength) => Instance._rewind = Mathf.Clamp01(strength);

        /// <summary>Speed streaks at the screen edge (0..1, set every frame by <see cref="HellFeelFx"/> while dilated).</summary>
        public static void SetSpeedLines(float strength) => Instance._speedLines = Mathf.Clamp01(strength);

        /// <summary>The Rewind's snap: a hard bright pop as the restored moment catches.</summary>
        public static void Snap() => Instance._snapAt = Time.unscaledTime;

        /// <summary>The Shadow Step's arrival: an ink edge closing and opening, a chroma pop.</summary>
        public static void StepPulse() => Instance._stepAt = Time.unscaledTime;

        /// <summary>A kill: a small warm contrast pop (<paramref name="k"/> 0..1).</summary>
        public static void KillPulse(float k)
        {
            var me = Instance;
            me._killAt = Time.unscaledTime;
            me._killK = Mathf.Clamp01(k);
        }

        /// <summary>A full-screen colour flash (UI veil) of <paramref name="strength"/> fading over <paramref name="seconds"/>.</summary>
        public static void Flash(Color c, float strength, float seconds)
        {
            var me = Instance;
            me._flashColour = c;
            me._flashK = Mathf.Clamp01(strength);
            me._flashSeconds = Mathf.Max(0.02f, seconds);
            me._flashAt = Time.unscaledTime;
        }

        /// <summary>Kept for the abilities' call: the Rewind's look is <see cref="HellRewindFx"/>'s (it listens to
        /// <see cref="HellRewind.Rewound"/>); with no rig in the scene this falls back to a plain snap.</summary>
        public static void RewindFlash()
        {
            if (HellRewindFx.Instance == null) Snap();
        }

        // seconds the pulses last
        const float SnapSeconds = 0.28f, StepSeconds = 0.38f, KillSeconds = 0.22f;

        // the channel targets: saturation, hue shift, contrast, chroma, lens, vignette, filter rgb, vignette rgb
        readonly float[] _tDilation = new float[ScreenGradeMix.ParamCount];
        readonly float[] _tRewind = new float[ScreenGradeMix.ParamCount];
        readonly float[] _tSnap = new float[ScreenGradeMix.ParamCount];
        readonly float[] _tStep = new float[ScreenGradeMix.ParamCount];
        readonly float[] _tKill = new float[ScreenGradeMix.ParamCount];
        readonly ScreenGradeMix _mix = new ScreenGradeMix();

        static void Targets(float[] t, float sat, float hue, float contrast, float chroma, float lens, float vignette, Color filter, Color vig)
        {
            t[ScreenGradeMix.Saturation] = sat; t[ScreenGradeMix.HueShift] = hue; t[ScreenGradeMix.Contrast] = contrast;
            t[ScreenGradeMix.Chroma] = chroma; t[ScreenGradeMix.Lens] = lens; t[ScreenGradeMix.Vignette] = vignette;
            t[ScreenGradeMix.FilterR] = filter.r; t[ScreenGradeMix.FilterG] = filter.g; t[ScreenGradeMix.FilterB] = filter.b;
            t[ScreenGradeMix.VignetteR] = vig.r; t[ScreenGradeMix.VignetteG] = vig.g; t[ScreenGradeMix.VignetteB] = vig.b;
        }

        /// <summary>The channels' looks from the palette (cheap: run every frame so a palette change shows at once).</summary>
        void FillTargets()
        {
            var p = HellFxPalette.Dilation;
            Targets(_tDilation, -45f, 0f, 12f, 0.25f, 0f, 0.4f, Color.Lerp(Color.white, p, 0.3f), p * 0.35f);
            var r = HellFxPalette.Rewind;
            Targets(_tRewind, -72f, -12f, 28f, 1f, -0.45f, 0.48f, Color.Lerp(Color.white, r, 0.35f), r * 0.3f);
            Targets(_tSnap, 15f, 0f, 30f, 0.7f, 0.3f, 0.15f, new Color(1.25f, 1.25f, 1.25f), Color.black);
            var s = HellFxPalette.Shadow;
            Targets(_tStep, -35f, 0f, 22f, 0.75f, 0.22f, 0.55f, Color.Lerp(Color.white, s, 0.25f), HellFxPalette.Ink);
            var f = HellFxPalette.Fire;
            Targets(_tKill, 12f, 0f, 14f, 0.3f, 0f, 0.25f, Color.Lerp(Color.white, f, 0.12f), f * 0.2f);
        }

        // ------------------------------------------------------------------ the screen: the volume and the overlays

        VisualElement _tint, _inkEdge, _streaks, _flash;
        readonly VisualElement[] _bars = new VisualElement[3];
        Texture2D _edgeTex, _streakTex;
        Volume _volume;
        ColorAdjustments _grade;
        ChromaticAberration _chroma;
        LensDistortion _lens;
        Vignette _vignette;
        VolumeProfile _profile;
        float _streakFlip;

        void EnsureScreen()
        {
            if (_tint == null)
            {
                var hud = SkylineHud.Instance;
                if (hud != null && hud.Root != null)
                {
                    _edgeTex = EdgeTexture();
                    _streakTex = StreakTexture();
                    _streaks = Layer(hud.Root);
                    _streaks.style.left = new Length(-50f, LengthUnit.Percent);
                    _streaks.style.top = new Length(-50f, LengthUnit.Percent);
                    _streaks.style.right = new Length(-50f, LengthUnit.Percent);
                    _streaks.style.bottom = new Length(-50f, LengthUnit.Percent);
                    _streaks.style.backgroundImage = new StyleBackground(_streakTex);
                    for (int i = 0; i < _bars.Length; i++)
                    {
                        var b = Layer(hud.Root);
                        b.style.bottom = StyleKeyword.Auto;
                        b.style.height = new Length(3f + 3f * i, LengthUnit.Percent);
                        _bars[i] = b;
                    }
                    _tint = Layer(hud.Root);
                    _tint.style.backgroundImage = new StyleBackground(_edgeTex);
                    _inkEdge = Layer(hud.Root);
                    _inkEdge.style.backgroundImage = new StyleBackground(_edgeTex);
                    _flash = Layer(hud.Root);
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
                // the Hell effects' volume profile (made here, so nothing in the project's profiles changes; every effect it
                // uses is in the project's DefaultVolumeProfile too, so the build keeps their shader variants)
                _profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _profile.name = "HellFxProfile";
                _grade = _profile.Add<ColorAdjustments>(true);
                _chroma = _profile.Add<ChromaticAberration>(true);
                _lens = _profile.Add<LensDistortion>(true);
                _lens.scale.Override(1f);
                _vignette = _profile.Add<Vignette>(true);
                _vignette.smoothness.Override(0.45f);
                _volume.sharedProfile = _profile;
                _volume.enabled = false;
            }
        }

        static VisualElement Layer(VisualElement parent)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute;
            e.style.left = 0; e.style.top = 0; e.style.right = 0; e.style.bottom = 0;
            e.style.opacity = 0f;
            e.style.display = DisplayStyle.None;
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

        /// <summary>Radial speed streaks: thin wedges of random length starting part-way out, a clear middle. Made once.</summary>
        static Texture2D StreakTexture()
        {
            const int n = 256, spokes = 140;
            var rng = new System.Random(7);
            var on = new float[spokes];
            var start = new float[spokes];
            for (int i = 0; i < spokes; i++)
            {
                on[i] = rng.NextDouble() < 0.45 ? (float)(0.5 + 0.5 * rng.NextDouble()) : 0f;
                start[i] = (float)(0.18 + 0.2 * rng.NextDouble());
            }
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a01 = (Mathf.Atan2(dy, dx) / (2f * Mathf.PI) + 1f) % 1f;
                    float f = a01 * spokes;
                    int s = (int)f % spokes;
                    float across = Mathf.Abs(f - Mathf.Floor(f) - 0.5f) * 2f;           // 0 on the spoke's centre line
                    float width = 0.25f + 0.35f * Mathf.Clamp01((r - start[s]) / 0.5f);   // wedges widen outward
                    float line = Mathf.Clamp01((width - across) / 0.15f);
                    float a = on[s] * line * Mathf.Clamp01((r - start[s]) / 0.12f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        static void Show(VisualElement e, float opacity)
        {
            if (e == null) return;
            bool on = opacity > 0.003f;
            var want = on ? DisplayStyle.Flex : DisplayStyle.None;
            if (e.resolvedStyle.display != want) e.style.display = want;
            if (on) e.style.opacity = Mathf.Clamp01(opacity);
        }

        void TickScreen(float dt)
        {
            float now = Time.unscaledTime;
            float snap = HellFxCurves.Decay(now - _snapAt, SnapSeconds);
            float step = HellFxCurves.Decay(now - _stepAt, StepSeconds);
            float kill = HellFxCurves.Decay(now - _killAt, KillSeconds) * _killK;
            float flash = HellFxCurves.Decay(now - _flashAt, _flashSeconds) * _flashK;
            bool any = _dilation > 0f || _rewind > 0f || snap > 0f || step > 0f || kill > 0f || flash > 0f || _speedLines > 0f;
            if (!any && _tint == null) return;
            EnsureScreen();
            float k = HellFxPalette.ScreenStrength;

            // the volume
            FillTargets();
            _mix.Begin();
            _mix.Add(_dilation * k, _tDilation);
            _mix.Add(_rewind * k, _tRewind);
            _mix.Add(snap * k, _tSnap);
            _mix.Add(step * k, _tStep);
            _mix.Add(kill * k, _tKill);
            float w = Mathf.Clamp01(_mix.Weight);
            if (_volume != null)
            {
                bool on = w > 0.001f;
                if (_volume.enabled != on) _volume.enabled = on;
                if (on)
                {
                    _volume.weight = w;
                    _grade.saturation.Override(_mix.Get(ScreenGradeMix.Saturation));
                    _grade.hueShift.Override(_mix.Get(ScreenGradeMix.HueShift));
                    _grade.contrast.Override(_mix.Get(ScreenGradeMix.Contrast));
                    _grade.colorFilter.Override(new Color(_mix.Get(ScreenGradeMix.FilterR), _mix.Get(ScreenGradeMix.FilterG), _mix.Get(ScreenGradeMix.FilterB)));
                    _chroma.intensity.Override(Mathf.Clamp01(_mix.Get(ScreenGradeMix.Chroma)));
                    _lens.intensity.Override(Mathf.Clamp(_mix.Get(ScreenGradeMix.Lens), -1f, 1f));
                    _vignette.intensity.Override(Mathf.Clamp01(_mix.Get(ScreenGradeMix.Vignette)));
                    _vignette.color.Override(new Color(_mix.Get(ScreenGradeMix.VignetteR), _mix.Get(ScreenGradeMix.VignetteG), _mix.Get(ScreenGradeMix.VignetteB)));
                }
            }

            // the overlays (they show with or without post-processing)
            if (_tint == null) return;
            var dil = HellFxPalette.Dilation;
            _tint.style.unityBackgroundImageTintColor = new Color(dil.r, dil.g, dil.b, 1f);
            Show(_tint, _dilation * 0.5f * k);
            var ink = HellFxPalette.Ink;
            _inkEdge.style.unityBackgroundImageTintColor = new Color(ink.r, ink.g, ink.b, 1f);
            Show(_inkEdge, step * 0.95f * k);

            float streaks = Mathf.Max(_rewind * 0.55f, _speedLines * 0.3f) * k;
            Show(_streaks, streaks);
            if (streaks > 0.003f)
            {
                _streakFlip -= dt;
                if (_streakFlip <= 0f)
                {
                    // re-dealt every 1/30 s: the classic flickering speed lines
                    _streakFlip = 1f / 30f;
                    _streaks.style.rotate = new Rotate(new Angle(UnityEngine.Random.Range(0f, 360f), AngleUnit.Degree));
                    _streaks.style.scale = new Scale(Vector3.one * UnityEngine.Random.Range(0.92f, 1.12f));
                }
                Color sc = _rewind * 0.55f >= _speedLines * 0.3f ? HellFxPalette.Rewind : HellFxPalette.Dilation;
                _streaks.style.unityBackgroundImageTintColor = new Color(sc.r, sc.g, sc.b, 1f);
            }

            // the Rewind's tape bars: pale bands rolling up the screen, as on a tape scrubbed backwards
            for (int i = 0; i < _bars.Length; i++)
            {
                var b = _bars[i];
                float o = _rewind * (0.16f + 0.06f * i) * k;
                Show(b, o);
                if (o <= 0.003f) continue;
                float phase = (now * (1.6f + 0.7f * i) + i * 0.37f) % 1f;
                b.style.top = new Length(110f - phase * 130f, LengthUnit.Percent);
                b.style.backgroundColor = Color.Lerp(HellFxPalette.Rewind, Color.white, 0.5f);
                b.style.translate = new Translate(new Length(UnityEngine.Random.Range(-1.5f, 1.5f), LengthUnit.Percent), 0f);
            }

            float veil = Mathf.Max(flash, snap * 0.42f) * k;
            Show(_flash, veil);
            if (veil > 0.003f)
                _flash.style.backgroundColor = flash >= snap * 0.42f ? _flashColour : Color.Lerp(HellFxPalette.Rewind, Color.white, 0.6f);
        }

        // ------------------------------------------------------------------ the field-of-view kick

        float _fovKickAt = -10f, _fovAmount, _fovPunch = 0.05f, _fovBack = 0.3f, _fovWritten = -1f, _fovFactor = 1f;

        /// <summary>Widens the main camera's view by <paramref name="amount"/> (0.1 = 10%) over <paramref name="punch"/> s and
        /// eases back over <paramref name="back"/> s, on top of whatever sets it.</summary>
        public static void FovKick(float amount, float punch = 0.05f, float back = 0.3f)
        {
            var me = Instance;
            me._fovKickAt = Time.unscaledTime;
            me._fovAmount = amount;
            me._fovPunch = punch;
            me._fovBack = back;
        }

        void TickFov()
        {
            var cam = Camera.main;
            if (cam == null) return;
            float factor = HellFxCurves.FovKick(Time.unscaledTime - _fovKickAt, _fovAmount, _fovPunch, _fovBack);
            float cur = cam.fieldOfView;
            // nobody reset the view since our last write: take our own factor back out first, so it never compounds
            float baseFov = _fovWritten > 0f && Mathf.Abs(cur - _fovWritten) < 1e-3f ? cur / _fovFactor : cur;
            if (Mathf.Approximately(factor, 1f) && Mathf.Approximately(_fovFactor, 1f)) { _fovWritten = -1f; return; }
            cam.fieldOfView = Mathf.Clamp(baseFov * factor, 1f, 170f);
            _fovWritten = cam.fieldOfView;
            _fovFactor = factor;
        }

        void OnDestroy()
        {
            if (_edgeTex != null) Destroy(_edgeTex);
            if (_streakTex != null) Destroy(_streakTex);
            if (_profile != null) Destroy(_profile);
            if (_instance == this) _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _instance = null;
            AutoRig = true;
        }
    }
}
