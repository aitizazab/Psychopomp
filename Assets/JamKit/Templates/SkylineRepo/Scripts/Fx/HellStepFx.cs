using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The Shadow Step as the Shadow shot (docs/HELL-FX.md), listening to <see cref="HellAbilities.Stepped"/>:
    /// <list type="bullet">
    /// <item><b>Departure:</b> you come apart into ink: dark shards lift off where you stood, a few violet sparks.</item>
    /// <item><b>The smear:</b> a trail of ink blobs along the jump (fading from the start toward the landing) cut through by a
    /// few long violet streaks, so a side camera sees the line you travelled.</item>
    /// <item><b>Arrival:</b> an ink splash: flat splats thrown out across the ground, droplets flung up, a violet ring; on screen,
    /// an ink edge that snaps shut and opens, a chroma/contrast pop and a field-of-view kick (out, then back).</item>
    /// </list>
    /// The aim preview is <see cref="HellStepMarkerFx"/> (on the marker <see cref="HellFx.MakeMarker"/> builds).
    /// </summary>
    public sealed class HellStepFx : MonoBehaviour
    {
        public float fovKick = 0.12f, fovPunch = 0.05f, fovBack = 0.32f;
        public float arrivalTrauma = 0.12f;

        HellAbilities _bound;

        void Update()
        {
            var a = HellAbilities.Instance;
            if (a == _bound) return;
            if (_bound != null) _bound.Stepped -= OnStepped;
            _bound = a;
            if (_bound != null) _bound.Stepped += OnStepped;
        }

        void OnDestroy()
        {
            if (_bound != null) _bound.Stepped -= OnStepped;
        }

        void OnStepped(Vector3 from, Vector3 to) => Play(from, to);

        /// <summary>The whole step effect from feet <paramref name="from"/> to feet <paramref name="to"/>.</summary>
        public void Play(Vector3 from, Vector3 to)
        {
            var ink = HellFx.Lit(HellFxPalette.Ink);
            var glow = HellFx.Glow(HellFxPalette.Shadow, HellFxPalette.ShadowGlow);
            Vector3 up = Vector3.up;
            Vector3 d = to - from;
            float dist = d.magnitude;
            Vector3 dir = dist > 1e-3f ? d / dist : Vector3.forward;

            // departure: you come apart into ink
            HellMotes.Spray(MoteShape.Streak, ink, from + up * 1f, up, 0.8f, 16, 2f, 5.5f, 0.5f, 0.13f, -3f, 1.5f, true);
            HellMotes.Spray(MoteShape.Blob, glow, from + up * 1f, up, 1f, 6, 1f, 3f, 0.3f, 0.1f, 0f, 2f, true);

            // the smear: ink along the line, the start fading first, cut by long violet streaks
            int n = Mathf.Clamp(Mathf.RoundToInt(dist / 0.5f), 6, 36);
            n = Mathf.Max(3, Mathf.RoundToInt(n * HellFxPalette.Density));
            for (int i = 0; i < n; i++)
            {
                float t = n > 1 ? i / (n - 1f) : 0f;
                Vector3 p = Vector3.Lerp(from, to, t) + up * Random.Range(0.5f, 1.5f) + Random.insideUnitSphere * 0.25f;
                HellMotes.Emit(new MoteSpec
                {
                    shape = MoteShape.Blob, material = ink, position = p, velocity = dir * Random.Range(1f, 4f),
                    life = 0.14f + 0.32f * t, size = Random.Range(0.25f, 0.45f), drag = 4f, swell = 0.1f, unscaled = true,
                });
            }
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, Random.Range(0.1f, 0.7f)) + up * Random.Range(0.6f, 1.4f);
                HellMotes.Emit(new MoteSpec
                {
                    shape = MoteShape.Streak, material = glow, position = p, velocity = dir * Random.Range(24f, 34f),
                    life = 0.22f, size = 0.07f, drag = 9f, swell = 0.05f, unscaled = true,
                });
            }

            // arrival: an ink splash
            HellMotes.Ring(MoteShape.Splat, ink, to + up * 0.03f, 14, 6f, 0.6f, 0.9f, 0f, 6f, true);
            HellMotes.Spray(MoteShape.Streak, ink, to + up * 0.3f, up, 0.85f, 14, 3f, 7f, 0.45f, 0.12f, 9f, 0.8f, true);
            HellMotes.Ring(MoteShape.Blob, glow, to + up * 0.5f, 10, 7f, 0.3f, 0.13f, 0f, 5f, true);

            // the screen
            HellFx.StepPulse();
            HellFx.FovKick(fovKick, fovPunch, fovBack);
            HellFeelFx.AddTrauma(arrivalTrauma);
        }
    }

    /// <summary>
    /// The Shadow Step's aim preview, on the marker (<see cref="HellFx.MakeMarker"/>): an ink disc ringed by violet shards that
    /// turn and breathe, a thin pillar, and a dark tendril from the player's hand to the landing (an ink line with a violet
    /// core, arcing and writhing). Dull red when the landing isn't valid. Nothing allocates per frame (the renderers are
    /// cached; materials change only when validity does).
    /// </summary>
    public sealed class HellStepMarkerFx : MonoBehaviour
    {
        const int Shards = 10, TendrilPoints = 20;
        static readonly Color Refused = new Color(0.7f, 0.08f, 0.08f);

        Transform _ring;
        Renderer[] _glowParts;
        LineRenderer _inkLine, _coreLine;
        bool _valid = true, _built;

        public void Build()
        {
            if (_built) return;
            _built = true;
            var glow = HellFx.Glow(HellFxPalette.Shadow, HellFxPalette.ShadowGlow);
            var ink = HellFx.Lit(HellFxPalette.Ink);
            var core = SkylineBotLook.Visual(PrimitiveType.Cylinder, "Core", transform, ink, false);
            core.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
            _ring = new GameObject("Ring").transform;
            _ring.SetParent(transform, false);
            _glowParts = new Renderer[Shards + 1];
            for (int i = 0; i < Shards; i++)
            {
                float a = i * Mathf.PI * 2f / Shards;
                var s = SkylineBotLook.Visual(PrimitiveType.Cube, "Shard", _ring, glow, false);
                s.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.55f, 0.03f, Mathf.Sin(a) * 0.55f);
                s.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                s.transform.localScale = new Vector3(0.2f, 0.035f, 0.06f);
                _glowParts[i] = s.GetComponent<Renderer>();
            }
            var pillar = SkylineBotLook.Visual(PrimitiveType.Cylinder, "Pillar", transform, glow, false);
            pillar.transform.localScale = new Vector3(0.04f, 0.9f, 0.04f);
            pillar.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            _glowParts[Shards] = pillar.GetComponent<Renderer>();
            foreach (var c in GetComponentsInChildren<Collider>(true)) Destroy(c);
            _inkLine = Line("TendrilInk", HellFx.Glow(HellFxPalette.Ink, 1f), 0.09f);
            _coreLine = Line("TendrilCore", glow, 0.025f);
        }

        LineRenderer Line(string name, Material m, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var l = go.AddComponent<LineRenderer>();
            l.useWorldSpace = true;
            l.sharedMaterial = m;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.numCapVertices = 2;
            l.widthMultiplier = width;
            l.widthCurve = new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.55f));
            l.positionCount = TendrilPoints;
            return l;
        }

        public void SetValid(bool valid)
        {
            if (!_built || valid == _valid) return;
            _valid = valid;
            var m = valid ? HellFx.Glow(HellFxPalette.Shadow, HellFxPalette.ShadowGlow) : SkylineBotLook.Glow(Refused);
            foreach (var r in _glowParts) r.sharedMaterial = m;
            _coreLine.sharedMaterial = m;
        }

        void LateUpdate()
        {
            if (!_built) return;
            float t = Time.unscaledTime;
            _ring.localRotation = Quaternion.Euler(0f, t * (_valid ? 90f : 25f), 0f);
            float pulse = _valid ? HellFxCurves.Breathe(t, 2.2f, 0.85f) : 0.9f;
            _ring.localScale = new Vector3(pulse, 1f, pulse);

            var a = HellAbilities.Instance;
            var cam = Camera.main;
            if (a == null || a.Player == null || cam == null) { _inkLine.enabled = _coreLine.enabled = false; return; }
            _inkLine.enabled = _coreLine.enabled = true;
            var ct = cam.transform;
            Vector3 hand = a.Player.EyePosition + ct.right * 0.22f - ct.up * 0.28f + ct.forward * 0.35f;
            Vector3 end = transform.position + Vector3.up * 0.05f;
            Vector3 d = end - hand;
            float dist = d.magnitude;
            Vector3 ctrl = (hand + end) * 0.5f + Vector3.up * (dist * 0.18f);
            Vector3 side = Vector3.Cross(dist > 1e-3f ? d / dist : Vector3.forward, Vector3.up);
            if (side.sqrMagnitude < 1e-4f) side = ct.right;
            side.Normalize();
            float writhe = _valid ? 0.12f : 0.05f;
            for (int i = 0; i < TendrilPoints; i++)
            {
                float u = i / (TendrilPoints - 1f);
                float iu = 1f - u;
                Vector3 p = iu * iu * hand + 2f * iu * u * ctrl + u * u * end;
                float env = Mathf.Sin(u * Mathf.PI);
                p += side * (Mathf.Sin(u * Mathf.PI * 3f - t * 9f) * writhe * env);
                p += Vector3.up * (Mathf.Sin(u * Mathf.PI * 5f + t * 7f) * writhe * 0.5f * env);
                _inkLine.SetPosition(i, p);
                _coreLine.SetPosition(i, p);
            }
        }
    }
}
