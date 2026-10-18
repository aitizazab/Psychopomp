using System.Collections.Generic;
using JamKit.Movement;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Who the bots go after (docs/HELL-ABILITIES.md), decided every frame before the bots think: a live Shade (the shadow a
    /// Shadow Step leaves with the Shade skill) draws every bot; a bot inside Nightfall's darkness, or looking through it at
    /// its target, loses track (it gets a target that is never live, so it neither sees nor attacks). Only bots aimed at the
    /// player, the Shade or the blind stand-in are touched: anything else another system set is left alone.
    /// <para>Shadow hides you (docs/HELL-SHADOW.md): it also ticks the player's <see cref="Stealth"/> (darkness and fire light
    /// at the chest, speed, the gun's shots) and marks each bot the player is hidden from (<see cref="SkylineBot.targetHidden"/>):
    /// it stops firing and hunts the last-seen spot. Off during Vesper's fight (she keeps her own rules of darkness).</para>
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public sealed class HellTargeting : MonoBehaviour, IHellRewindable
    {
        public SkylinePlayer player;

        /// <summary>The player's hidden/revealed state (pure rules: <see cref="PlayerStealth"/>, tuning in <see cref="ShadowTuning"/>).</summary>
        public readonly PlayerStealth Stealth = new PlayerStealth();
        public static HellTargeting Instance { get; private set; }
        /// <summary>The stealth rule is live (not during Vesper's fight).</summary>
        public bool StealthOn => !HellVesperFight.Active;
        /// <summary>How many bots saw the player last frame.</summary>
        public int SeenBy { get; private set; }
        /// <summary>A bot that was seeing you lost you into the dark (the "vanish" moment).</summary>
        public static event System.Action<SkylineBot> LostInShadow;

        SkylinePlayer _cuesFrom;
        readonly HellSnapPool<HellSnapBox<StealthState>> _snaps = new HellSnapPool<HellSnapBox<StealthState>>();

        /// <summary>The Poacher's rule (HellJobSite) and anyone else's: is the player hidden from an eye at <paramref name="eye"/>?
        /// Falls back to plain darkness at <paramref name="playerChest"/> when there is no player kit.</summary>
        public static bool PlayerHiddenFrom(Vector3 eye, Vector3 playerChest)
        {
            var t = Instance;
            if (t == null || t.player == null) return HellDarkness.IsDark(playerChest);
            return t.StealthOn && t.Stealth.HiddenFrom(Vector3.Distance(eye, t.player.Chest));
        }

        HellShade _shade;
        BlindTarget _blind;
        readonly List<SkylineBot> _scratch = new List<SkylineBot>();

        /// <summary>A target that is never live (its root is switched off): a bot holding it sees nothing.</summary>
        sealed class BlindTarget : MonoBehaviour, ISkylineTarget
        {
            public SkylinePlayer player;
            public Vector3 Chest => player != null ? player.Chest : transform.position;
            public Vector3 Velocity => Vector3.zero;
            public Transform Root => transform;
            public void TakeAttack(float damage, string attackerId, Vector3 attackerPosition) { }
        }

        void Awake()
        {
            var go = new GameObject("HellBlindTarget");
            go.transform.SetParent(transform, false);
            _blind = go.AddComponent<BlindTarget>();
            _blind.player = player;
            go.SetActive(false);
            Instance = this;
            if (GetComponent<HellShadowAudio>() == null) gameObject.AddComponent<HellShadowAudio>();
            if (GetComponent<HellHiddenCue>() == null) gameObject.AddComponent<HellHiddenCue>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; LostInShadow = null; }

        void OnEnable() => HellRewindRegistry.Register(this);
        void OnDisable() => HellRewindRegistry.Unregister(this);

        void OnDestroy()
        {
            if (_cuesFrom != null) _cuesFrom.CueRaised -= OnCue;
            if (Instance == this) Instance = null;
        }

        void OnCue(GunCue cue, int slot)
        {
            if (cue.kind == GunCueKind.Shot) Stealth.Fired();
        }

        object IHellRewindable.Snapshot()
        {
            var b = _snaps.Next();
            b.value = Stealth.Save();
            return b;
        }

        void IHellRewindable.Restore(object snapshot) => Stealth.Restore(HellSnapBox<StealthState>.From(snapshot));

        /// <summary>Darkness, fire light and speed at the player's chest, this frame.</summary>
        void TickStealth()
        {
            if (!ReferenceEquals(_cuesFrom, player))
            {
                if (_cuesFrom != null) _cuesFrom.CueRaised -= OnCue;
                _cuesFrom = player;
                player.CueRaised += OnCue;
            }
            HellShadowFire.TickRoomFlares();
            var chest = player.Chest;
            bool inDark = HellDarkness.InDarkVolume(chest);
            bool lit = inDark && HellDarkness.IsLit(chest);
            var sim = player.Movement != null ? player.Movement.Sim : null;
            bool modeFast = sim != null && (sim.Mode == MoveMode.Sprint || sim.Mode == MoveMode.Slide || sim.Mode == MoveMode.Wallrun);
            var v = player.Velocity;
            float speed = Mathf.Sqrt(v.x * v.x + v.z * v.z);
            Stealth.Tick(Time.deltaTime, inDark, lit, Stealth.tuning.IsFast(speed, modeFast));
        }

        /// <summary>Leaves the Shade at <paramref name="feet"/> for <paramref name="seconds"/>.</summary>
        public void LeaveShade(Vector3 feet, float yaw, float seconds)
        {
            if (_shade == null) _shade = HellShade.Create();
            _shade.Show(feet, yaw, seconds);
        }

        void Update()
        {
            if (player == null) return;
            if (_blind.player == null) _blind.player = player;
            TickStealth();
            bool stealth = StealthOn;
            ISkylineTarget shade = _shade != null && _shade.Live ? _shade : null;
            ISkylineTarget want = shade ?? player;
            Vector3 chest = player.Chest;
            int seenBy = 0;
            bool vanished = false;
            _scratch.Clear();
            _scratch.AddRange(SkylineBot.Live);
            foreach (var bot in _scratch)
            {
                if (bot == null || !bot.Alive) continue;
                var t = bot.target;
                bool ours = ReferenceEquals(t, player) || ReferenceEquals(t, _blind) || (_shade != null && ReferenceEquals(t, _shade));
                if (!ours) { if (bot.targetHidden) bot.targetHidden = false; continue; }
                var c = bot.Center;
                bool blind = HellDarkness.InDarkSphere(c) || HellDarkness.SightThroughDark(c, want.Chest) || HellSkillNightfall.Blinded(bot);   // + 1.5 s after Nightfall let go
                var next = blind ? (ISkylineTarget)_blind : want;
                if (!ReferenceEquals(t, next)) bot.target = next;
                bool sawYou = ReferenceEquals(t, player) && bot.SeesTarget;
                if (sawYou) seenBy++;
                bool hidden = stealth && ReferenceEquals(next, player) && Stealth.HiddenFrom(Vector3.Distance(c, chest));
                if (hidden && sawYou && !bot.targetHidden) { vanished = true; LostInShadow?.Invoke(bot); }
                bot.targetHidden = hidden;
            }
            SeenBy = seenBy;
            // the vanish: a wisp of shadow where you stood, the moment a bot that saw you loses you
            if (vanished) HellFx.SmokeBurst(player.transform.position + Vector3.up * 0.2f, 5, 0.35f);
        }
    }

    /// <summary>The Shade: the player's shadow left where a Shadow Step began (Shade skill). Bots target it for a few seconds;
    /// their bolts strike it (it has a body collider) and it shrugs them off. An ink-dark figure in smoke; it sinks away at
    /// the end.</summary>
    public sealed class HellShade : MonoBehaviour, ISkylineTarget
    {
        float _until, _sink;
        Transform _body;

        public bool Live => gameObject.activeSelf && Time.time < _until;

        public Vector3 Chest => transform.position + Vector3.up * 1.25f;
        public Vector3 Velocity => Vector3.zero;
        public Transform Root => transform;
        public void TakeAttack(float damage, string attackerId, Vector3 attackerPosition) => HellFx.SmokeBurst(transform.position, 3, 0.3f);

        public static HellShade Create()
        {
            var go = new GameObject("HellShade");
            var s = go.AddComponent<HellShade>();
            var mat = SkylineBotLook.Lit(HellFx.Smoke);
            var body = SkylineBotLook.Visual(PrimitiveType.Capsule, "Body", go.transform, mat, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            var head = SkylineBotLook.Visual(PrimitiveType.Sphere, "Head", body.transform, mat, false);
            head.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            head.transform.localScale = Vector3.one * 0.55f;
            var eyes = SkylineBotLook.Visual(PrimitiveType.Cube, "Eyes", head.transform, SkylineBotLook.Glow(HellFx.ShadowHue * 2.5f), false);
            eyes.transform.localPosition = new Vector3(0f, 0.05f, 0.42f);
            eyes.transform.localScale = new Vector3(0.6f, 0.08f, 0.1f);
            go.layer = 2;   // Ignore Raycast, like the player: the player's shots pass it; the bolts (all layers) strike it
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.height = 1.8f;
            col.radius = 0.35f;
            s._body = body.transform;
            go.SetActive(false);
            return s;
        }

        public void Show(Vector3 feet, float yaw, float seconds)
        {
            transform.SetPositionAndRotation(feet, Quaternion.Euler(0f, yaw, 0f));
            _until = Time.time + seconds;
            _sink = 0f;
            _body.localPosition = new Vector3(0f, 0.9f, 0f);
            gameObject.SetActive(true);
            HellFx.SmokeBurst(feet, 10, 0.5f);
            HellAbilityAudio.Play("shade", feet);
        }

        void Update()
        {
            if (Time.time < _until) return;
            _sink += Time.deltaTime;
            _body.localPosition = new Vector3(0f, 0.9f - _sink * 4f, 0f);
            if (_sink > 0.45f)
            {
                HellFx.SmokeBurst(transform.position, 6, 0.4f);
                gameObject.SetActive(false);
            }
        }
    }
}
