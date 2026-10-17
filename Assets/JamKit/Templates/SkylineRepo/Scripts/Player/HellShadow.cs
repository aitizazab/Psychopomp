using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Fire kills shadow, live (docs/HELL-SHADOW.md): every fire source that passes lights the dark round it for a moment
    /// (<see cref="FireExposure"/>): there you are not hidden and a Shadow Step can't land. Each call lights only near darkness,
    /// into <see cref="HellDarkness"/>'s fixed ring of passing lights (no garbage). Callers are one line each: the ember bolt in
    /// flight and its strike, a burning enemy, Combustion's blast, an ooze's pool, and the room's lava flares (ticked here).
    /// </summary>
    public static class HellShadowFire
    {
        /// <summary>An ember bolt moved <paramref name="from"/>..<paramref name="to"/> this frame: stamps light along it near darkness.</summary>
        public static void BoltPassing(Vector3 from, Vector3 to)
        {
            if (!HellDarkness.NearDarkVolume(to, FireExposure.BoltNearDark) && !HellDarkness.NearDarkVolume(from, FireExposure.BoltNearDark)) return;
            int n = FireExposure.StampsFor(Vector3.Distance(from, to));
            for (int k = 1; k <= n; k++) HellDarkness.Flash(Vector3.Lerp(from, to, k / (float)n), FireExposure.BoltRadius, FireExposure.BoltSeconds, false);
        }

        /// <summary>An ember bolt struck at <paramref name="at"/>: a bolt fired at your hiding spot floods it.</summary>
        public static void Impact(Vector3 at) => Near(at, FireExposure.ImpactRadius, FireExposure.ImpactSeconds);

        /// <summary>A burning enemy's ember tick at its centre.</summary>
        public static void Burning(Vector3 center) => Near(center, FireExposure.BurnRadius, FireExposure.BurnSeconds);

        /// <summary>Combustion's blast (or any fire blast) of <paramref name="radius"/>.</summary>
        public static void Blast(Vector3 at, float radius) => Near(at, radius + FireExposure.BlastExtra, FireExposure.BlastSeconds);

        /// <summary>An ooze's burning pool of <paramref name="radius"/> (call every few frames while it burns).</summary>
        public static void Pool(Vector3 at, float radius) => Near(at, radius + FireExposure.PoolExtra, FireExposure.PoolSeconds);

        static void Near(Vector3 at, float radius, float seconds)
        {
            if (HellDarkness.NearDarkVolume(at, radius)) HellDarkness.Flash(at, radius, seconds);
        }

        // ------------------------------------------------------------------ the room's lava flares

        static List<HellLight> _flares;
        static bool[] _up = new bool[0];

        /// <summary>The room now played (HellRoom.Begin): its lava lights, some of which flare (<see cref="HellLight.Flares"/>).
        /// Null: none (HellRoom.End).</summary>
        public static void SetRoomFlares(List<HellLight> lights)
        {
            _flares = lights;
            int n = lights != null ? lights.Count : 0;
            if (_up.Length < n) _up = new bool[n];
            System.Array.Clear(_up, 0, _up.Length);
        }

        /// <summary>Each frame (HellTargeting): a flare coming up spurts (embers, a flame) and lights out to its flare radius for
        /// its time up.</summary>
        public static void TickRoomFlares()
        {
            var list = _flares;
            if (list == null) return;
            double t = Time.timeAsDouble;
            for (int i = 0; i < list.Count && i < _up.Length; i++)
            {
                var l = list[i];
                if (!l.Flares) continue;
                bool up = l.FlareUp(t);
                if (up && !_up[i])
                {
                    var at = new Vector3(l.position.X, l.position.Y, l.position.Z);
                    HellDarkness.Flash(at, l.flareRadius, l.flareSeconds);
                    SkylineSpark.Burst(at, Vector3.up, 10, 7f);
                    HellFlame.Emit(at, Vector3.up * 3f, 0.9f, 0.6f, HellLook.Flame);
                    HellFlame.Emit(at + Vector3.up * 0.4f, Vector3.up * 4.5f, 0.6f, 0.5f, HellLook.FlameYellow);
                }
                _up[i] = up;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _flares = null; _up = new bool[0]; }
    }

    /// <summary>
    /// Darkness sounds dark (docs/HELL-SHADOW.md): while you are hidden the world hushes (the bots, the world and the beds are
    /// dipped <see cref="DarkHush.worldDb"/> and low-passed toward <see cref="DarkHush.worldCutoffHz"/> through
    /// <see cref="SkylineMix.HushDb"/> / <see cref="SkylineMix.HushCutoffHz"/>), your heartbeat comes up front (the sound
    /// binder's heartbeat loop takes the larger of Time Dilation's and <see cref="HeartbeatLevel"/>), and <see cref="Depth"/> is the
    /// music's Dark modifier hook (<see cref="DarkHush.MusicDrums"/>, <see cref="DarkHush.MusicCutoff"/>, <see cref="DarkHush.MusicDrone"/>).
    /// Runs on unscaled time (the pause and the pick screen hold it).
    /// </summary>
    public sealed class HellShadowAudio : MonoBehaviour
    {
        /// <summary>The hush now: 0 open, 1 hidden. The music director's Dark modifier reads this.</summary>
        public static float Depth { get; private set; }
        /// <summary>The heartbeat's level for the sound binder (0..1).</summary>
        public static float HeartbeatLevel { get; private set; }
        /// <summary>Raised when the hush crosses half depth (true: in the dark, hidden).</summary>
        public static event System.Action<bool> DarkChanged;

        public readonly DarkHush hush = new DarkHush();
        HellTargeting _t;
        bool _wasDark;

        void Awake() => _t = GetComponent<HellTargeting>();

        void Update()
        {
            if (_t == null) return;
            var s = _t.Stealth;
            bool on = _t.StealthOn;
            float target = on ? DarkHush.TargetFor(s.Concealed, s.Fast, s.InDarkVolume) : 0f;
            float d = Time.timeScale <= 0f ? hush.Depth : hush.Step(target, Time.unscaledDeltaTime);
            Depth = d;
            HeartbeatLevel = hush.Heartbeat;
            SkylineMix.HushDb = hush.WorldGainDb;
            SkylineMix.HushCutoffHz = hush.WorldCutoff;
            bool dark = d > 0.5f;
            if (dark != _wasDark) { _wasDark = dark; DarkChanged?.Invoke(dark); }
        }

        void OnDisable() => Open();

        static void Open()
        {
            Depth = 0f;
            HeartbeatLevel = 0f;
            SkylineMix.HushDb = 0f;
            SkylineMix.HushCutoffHz = DarkHush.OpenHz;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Open(); DarkChanged = null; }
    }

    /// <summary>
    /// The "you are hidden" cue (docs/HELL-SHADOW.md): a small eye on the HUD's safe area, left of the ability bars. Hidden: a
    /// closed violet eye (lashes down), dimmer while you move fast (seen nearer). In darkness but exposed: an open eye, orange
    /// when fire is on you, white while your shot gives you away (the reveal shrinking as a ring). Out of the dark: nothing.
    /// One layer named <c>hell-hidden</c> under <c>SkylineHud.Safe</c>; position and colours are the constants below.
    /// </summary>
    public sealed class HellHiddenCue : MonoBehaviour
    {
        public static float Right = 290f, Bottom = 190f, Size = 46f;
        static readonly Color Hidden = new Color32(0x9B, 0x6B, 0xFF, 255);
        static readonly Color Fire = new Color32(0xFF, 0x8A, 0x2B, 255);
        static readonly Color Ink = new Color32(0x10, 0x0C, 0x14, 255);

        HellTargeting _t;
        VisualElement _el;
        float _alpha;
        int _state;   // 0 none, 1 hidden, 2 exposed (shot), 3 lit
        float _reveal, _fastness;

        void Awake() => _t = GetComponent<HellTargeting>();

        void Update()
        {
            if (_t == null) return;
            if (_el == null)
            {
                var hud = SkylineHud.Instance;
                if (hud == null || hud.Safe == null) return;
                _el = SkylineHudStyle.Layer(hud.Safe, "hell-hidden");
                _el.style.right = Right;
                _el.style.bottom = Bottom;
                _el.style.width = Size;
                _el.style.height = Size;
                _el.pickingMode = PickingMode.Ignore;
                _el.generateVisualContent += Draw;
            }
            var s = _t.Stealth;
            int state = !_t.StealthOn || !s.InDarkVolume ? 0 : s.Concealed ? 1 : s.Lit ? 3 : 2;
            float want = state == 0 ? 0f : 1f;
            _alpha = Mathf.MoveTowards(_alpha, want, Time.unscaledDeltaTime * 6f);
            if (state != 0) _state = state;
            _reveal = s.tuning.revealSeconds > 0f ? Mathf.Clamp01(s.RevealLeft / s.tuning.revealSeconds) : 0f;
            _fastness = s.Fast ? 1f : 0f;
            _el.style.opacity = _alpha;
            _el.MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext ctx)
        {
            if (_alpha <= 0f) return;
            var p = ctx.painter2D;
            float w = Size, cx = w * 0.5f, cy = w * 0.5f, rx = w * 0.42f, ry = w * 0.22f;
            Color col = _state == 1 ? Color.Lerp(Hidden, Hidden * 0.6f + Color.black * 0.4f, _fastness * 0.6f) : _state == 3 ? Fire : Color.white;
            p.lineWidth = 3f;
            p.strokeColor = Ink;
            p.fillColor = Ink;
            if (_state == 1)
            {
                // closed: the lower lid as an arc, lashes hanging
                Lid(p, cx, cy, rx, ry, true);
                p.lineWidth = 5f; p.strokeColor = Ink; p.Stroke();
                Lid(p, cx, cy, rx, ry, true);
                p.lineWidth = 2.5f; p.strokeColor = col; p.Stroke();
                for (int i = -2; i <= 2; i++)
                {
                    float x = cx + i * rx * 0.38f, y = cy + ry * (1f - 0.15f * i * i) * 0.9f;
                    p.BeginPath(); p.MoveTo(new Vector2(x, y)); p.LineTo(new Vector2(x + i * 1.5f, y + 5f));
                    p.lineWidth = 2f; p.strokeColor = col; p.Stroke();
                }
                return;
            }
            // open: an almond with a pupil
            p.BeginPath();
            Almond(p, cx, cy, rx, ry);
            p.fillColor = Ink; p.Fill();
            p.lineWidth = 2.5f; p.strokeColor = col; p.Stroke();
            p.BeginPath();
            p.Arc(new Vector2(cx, cy), ry * 0.7f, 0f, 360f);
            p.fillColor = col; p.Fill();
            if (_state == 2 && _reveal > 0f)
            {
                // the reveal left, a ring running down
                p.BeginPath();
                p.Arc(new Vector2(cx, cy), w * 0.48f, -90f, -90f + 360f * _reveal);
                p.lineWidth = 2f; p.strokeColor = col; p.Stroke();
            }
        }

        static void Lid(Painter2D p, float cx, float cy, float rx, float ry, bool lower)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(cx - rx, cy));
            p.QuadraticCurveTo(new Vector2(cx, cy + (lower ? ry * 2f : -ry * 2f)), new Vector2(cx + rx, cy));
        }

        static void Almond(Painter2D p, float cx, float cy, float rx, float ry)
        {
            p.MoveTo(new Vector2(cx - rx, cy));
            p.QuadraticCurveTo(new Vector2(cx, cy - ry * 2f), new Vector2(cx + rx, cy));
            p.QuadraticCurveTo(new Vector2(cx, cy + ry * 2f), new Vector2(cx - rx, cy));
            p.ClosePath();
        }
    }
}
