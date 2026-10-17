using JamKit.Skyline;
using UnityEngine;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The Hell descent's game feel (docs/HELL-FX.md), on the effect rig, listening only:
    /// <list type="bullet">
    /// <item><b>Camera shake</b> through the player's own <see cref="ScreenShake"/>, scaled by the Look page's "Screen shake"
    /// slider (<see cref="SkylineOptions.screenShake"/>): hard landings, kills, Combustion blasts and falling platforms
    /// (<see cref="ShakeAt"/>, by distance), the Rewind's snap and the Shadow Step's arrival (<see cref="AddTrauma"/>).</item>
    /// <item><b>Hits:</b> a spit of embers off every hit the player lands (the bots' own white flash and the guns' hit stop are
    /// already the kit's); <b>kills:</b> a ring of fire, a fountain of embers, a warm screen pop and a little shake (the gun's
    /// kill hit stop is the kit's, 0.08-0.1 s).</item>
    /// <item><b>Time Dilation:</b> bolts in flight leave a dotted blue-violet afterimage trail, and speed streaks rim the
    /// screen when you move fast (the grade itself is <see cref="HellFx.SetDilation"/>'s).</item>
    /// <item><b>The skill pick's reveal:</b> the screen opens with a flash, the title drops in, the three cards rise one after
    /// another with an overshoot (unscaled: the game is stopped).</item>
    /// </list>
    /// </summary>
    public sealed class HellFeelFx : MonoBehaviour
    {
        [Header("Shake")]
        public float landingFrom = 9f, landingFull = 22f, landingMax = 0.38f;
        public float killTrauma = 0.14f;
        [Header("Time Dilation")]
        public float afterimageEvery = 0.035f, afterimageLife = 0.32f;
        public float speedLinesFrom = 6f, speedLinesFull = 14f;
        [Header("Skill pick")]
        public float revealSeconds = 0.75f, cardDelay = 0.09f, cardSeconds = 0.4f;

        static HellFeelFx _instance;
        SkylinePlayer _player;
        float _baseOffset = -1f, _baseRoll;
        bool _wasGrounded = true;
        float _lastVy;
        float _afterimageLeft;

        void Awake() => _instance = this;

        void OnEnable()
        {
            SkylineBot.Killed += OnKilled;
            HellRewind.Rewound += OnRewound;
        }

        void OnDisable()
        {
            SkylineBot.Killed -= OnKilled;
            HellRewind.Rewound -= OnRewound;
            Bind(null);
            if (_instance == this) _instance = null;
        }

        void Bind(SkylinePlayer p)
        {
            if (p == _player) return;
            if (_player != null) _player.HitLanded -= OnHit;
            _player = p;
            _baseOffset = -1f;
            if (_player != null) _player.HitLanded += OnHit;
        }

        void OnRewound()
        {
            _wasGrounded = true;   // the body jumped back: no landing from the jump
            _lastVy = 0f;
            _revealT = -1f;
        }

        // ------------------------------------------------------------------ shake

        /// <summary>Adds camera trauma to the player's shake (0..1; the slider scales the result, not the trauma).</summary>
        public static void AddTrauma(float amount)
        {
            var me = _instance;
            if (me == null || me._player == null || amount <= 0f) return;
            me._player.Shake.AddTrauma(amount);
        }

        /// <summary>Trauma from something at <paramref name="at"/>: full within <paramref name="inner"/> m of the player, none
        /// beyond <paramref name="outer"/>.</summary>
        public static void ShakeAt(Vector3 at, float trauma, float inner, float outer)
        {
            var me = _instance;
            if (me == null || me._player == null) return;
            float d = Vector3.Distance(me._player.transform.position, at);
            AddTrauma(trauma * HellFxCurves.Falloff(d, inner, outer));
        }

        void ApplyShakeSetting()
        {
            var s = _player.Shake;
            if (_baseOffset < 0f) { _baseOffset = s.maxOffset; _baseRoll = s.maxRoll; }
            float k = SkylineSettings.Options.screenShake;
            s.maxOffset = _baseOffset * k;
            s.maxRoll = _baseRoll * k;
        }

        // ------------------------------------------------------------------ hits and kills

        void OnHit(SkylineHitInfo info, SkylineHitResult r)
        {
            if (!r.hit || r.killed) return;
            Vector3 n = info.normal.sqrMagnitude > 1e-6f ? info.normal : -info.direction;
            HellMotes.Spray(MoteShape.Streak, HellFx.Glow(HellFxPalette.Ember, 1f), info.point, n, 0.6f, 4, 4f, 9f, 0.18f, 0.035f, 6f, 1f);
        }

        void OnKilled(SkylineBot bot, GunDef gun)
        {
            if (bot == null) return;
            Vector3 c = bot.Center;
            var fire = HellFx.Glow(HellFxPalette.Fire, 2.4f);
            HellMotes.Ring(MoteShape.Blob, fire, c - Vector3.up * 0.3f, 12, 7f, 0.35f, 0.3f, 0.4f, 5f);
            HellMotes.Spray(MoteShape.Streak, HellFx.Glow(HellFxPalette.Ember, 1f), c, Vector3.up, 0.7f, 14, 4f, 10f, 0.8f, 0.06f, 9f, 0.5f);
            if (gun != null) HellFx.KillPulse(0.8f);
            ShakeAt(c, killTrauma, 6f, 30f);
        }

        // ------------------------------------------------------------------ the frame

        void Update()
        {
            var a = HellAbilities.Instance;
            Bind(a != null ? a.Player : null);
            TickReveal();
            if (_player == null) return;
            ApplyShakeSetting();
            TickLanding();
            TickDilation();
        }

        void TickLanding()
        {
            var mc = _player.Movement;
            var sim = mc != null ? mc.Sim : null;
            if (sim == null) return;
            bool grounded = sim.Grounded;
            if (grounded && !_wasGrounded)
            {
                float t = HellFxCurves.LandingTrauma(-_lastVy, landingFrom, landingFull, landingMax);
                if (t > 0f)
                {
                    AddTrauma(t);
                    var dust = HellFx.Lit(HellFxPalette.Dust);
                    HellMotes.Ring(MoteShape.Blob, dust, _player.transform.position + Vector3.up * 0.1f, 10, 4f, 0.45f, 0.3f, 0.3f, 5f);
                }
            }
            _wasGrounded = grounded;
            _lastVy = sim.Velocity.Y;
        }

        void TickDilation()
        {
            float s = HellFx.DilationNow;
            if (s <= 0.02f) { HellFx.SetSpeedLines(0f); return; }
            var mc = _player.Movement;
            float speed = mc != null ? mc.Velocity.magnitude : 0f;
            HellFx.SetSpeedLines(s * HellFxCurves.Clamp01((speed - speedLinesFrom) / Mathf.Max(0.1f, speedLinesFull - speedLinesFrom)));
            _afterimageLeft -= Time.unscaledDeltaTime;
            if (_afterimageLeft > 0f) return;
            _afterimageLeft = afterimageEvery;
            var mat = HellFx.Glow(HellFxPalette.Dilation, HellFxPalette.DilationGlow);
            var pool = SkylineBolt.Pool;
            for (int i = 0; i < pool.Count; i++)
            {
                var b = pool[i];
                if (b == null || !b.gameObject.activeSelf) continue;
                HellMotes.Emit(new MoteSpec
                {
                    shape = MoteShape.Blob, material = mat, position = b.transform.position, life = afterimageLife,
                    size = 0.14f * (0.6f + 0.4f * s), swell = 0.05f, unscaled = true,
                });
            }
        }

        // ------------------------------------------------------------------ the skill pick's reveal

        float _revealT = -1f;
        bool _pickWas;
        VisualElement _pickLayer;

        void TickReveal()
        {
            bool open = HellSkillPick.Open;
            if (open && !_pickWas)
            {
                var hud = SkylineHud.Instance;
                _pickLayer = hud != null && hud.Root != null ? hud.Root.Q<VisualElement>("hell-skill-pick") : null;
                _revealT = _pickLayer != null ? 0f : -1f;
                if (_pickLayer != null) HellFx.Flash(Color.Lerp(HellFxPalette.Fire, Color.white, 0.35f), 0.55f, 0.35f);
            }
            _pickWas = open;
            if (!open) { _pickLayer = null; _revealT = -1f; return; }
            if (_revealT < 0f || _pickLayer == null) return;
            _revealT += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float t = _revealT;
            _pickLayer.style.opacity = HellFxCurves.EaseOut(t / 0.15f);
            int n = _pickLayer.childCount;
            if (n > 0)
            {
                var title = _pickLayer[0];
                float u = HellFxCurves.Stagger(t, 0, 0f, 0.25f);
                title.style.opacity = u;
                title.style.translate = new Translate(0f, -50f * (1f - HellFxCurves.EaseOut(u, 3f)));
                title.style.letterSpacing = 18f * (1f - HellFxCurves.EaseOut(u, 3f));
            }
            if (n > 1)
            {
                var row = _pickLayer[1];
                for (int i = 0; i < row.childCount; i++)
                {
                    var card = row[i];
                    float u = HellFxCurves.Stagger(t, i, cardDelay, cardSeconds);
                    float lift = 1f - HellFxCurves.BackOut(u);
                    card.style.opacity = HellFxCurves.EaseOut(u * 2f);
                    card.style.translate = new Translate(0f, 120f * lift);
                }
            }
            if (t >= revealSeconds) _revealT = -1f;   // settled: leave the cards to the pick screen
        }
    }
}
