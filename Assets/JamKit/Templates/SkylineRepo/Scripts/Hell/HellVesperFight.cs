using System;
using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The fight with Vesper, the Evening Star, at the bottom of floor 3 (docs/HELL-BOSS.md). Standalone:
    /// <see cref="Begin"/> builds her arena (<see cref="HellVesperArena"/>, at run time) if it isn't built, puts the player at
    /// its entry, and runs the pure <see cref="VesperFight"/>: bolts, blinks, imps, her Rewind, Nightfall, the hunt, her
    /// mercy. A death restarts the fight (not the floor). On her defeat she stays on her knees in the arena, the exit gate
    /// opens, <see cref="Defeated"/> fires and the <c>onDefeated</c> given to <see cref="Begin"/> is called: the ending lane
    /// builds the decision scene and the escape from there.
    /// <para>The run's hook: <see cref="RequestFinalBoss"/>. If <c>HellRunEvents.FinalBossRequested</c> exists (lane 1, branch
    /// hell-flow) as an <c>Action</c> or an <c>Action&lt;Action&gt;</c>, it is subscribed to at scene load (by reflection, so
    /// this file compiles with or without it).</para>
    /// <para>Rewindable (the global Rewind): her whole state, her own health history, the lava clock; the imps rewind
    /// themselves (they are bots). Everything shown (pose, heart, markers, Nightfall) is derived from the state each frame.</para>
    /// </summary>
    [AddComponentMenu("JamKit/Hell/Vesper Fight")]
    public sealed class HellVesperFight : MonoBehaviour, IHellRewindable
    {
        public const string AttackerId = "vesper";

        public SkylinePlayer player;
        public VesperTuning tuning = new VesperTuning();
        /// <summary>A death restarts the current phase instead of the whole fight (off: the brief's "death restarts the fight").</summary>
        public bool keepPhaseOnDeath;
        public ulong seed = 0x5EB7E4UL;

        public static HellVesperFight Instance { get; private set; }
        /// <summary>True while a fight runs (the run director should leave deaths to the fight while it is).</summary>
        public static bool Active { get; private set; }
        /// <summary>She is beaten (after the kneel). The ending lane can listen here instead of passing a callback.</summary>
        public static event Action Defeated;

        public VesperFight Fight { get; private set; }
        public HellVesperBody Body { get; private set; }
        public HellRoomDef Def { get; private set; }
        /// <summary>Where she kneels after the fight (the ending starts here).</summary>
        public Vector3 VesperPosition => Body != null ? Body.transform.position : transform.position;

        sealed class Snap
        {
            public readonly VesperFight.Snapshot fight = new VesperFight.Snapshot();
            public LavaContact.State lava;
        }

        readonly HellSnapPool<Snap> _pool = new HellSnapPool<Snap>();
        readonly List<VesperEvent> _ev = new List<VesperEvent>();
        readonly List<SkylineBot> _imps = new List<SkylineBot>();
        readonly List<int> _zoneIds = new List<int>();
        readonly List<int> _lightIds = new List<int>();
        readonly HellVesperHud _hud = new HellVesperHud();
        HellVesperArenaView.Built _arena;
        Action _onDefeated;
        LavaContact _lava;
        SafeSpot _safe;
        GameObject _blinkMarker, _huntMarker, _cutLine;
        bool _nightfall, _subscribed, _hintRewind, _hintHunt;
        int _nightZone = -1, _impSeq;
        Color _ambient, _fog;
        UnityEngine.Rendering.SphericalHarmonicsL2 _probe;
        bool _ambientSaved;
        string _lastAttacker;
        float _lastAttackTime;
        Func<bool> _prevIntercept;
        Func<Vector3, bool> _prevLavaQuery;

        // ------------------------------------------------------------------ the hook

        /// <summary>The run's hook (lane 1 calls this after floor 3's shaft, or raises <c>HellRunEvents.FinalBossRequested</c>):
        /// finds or makes the fight in the scene and begins it.</summary>
        public static HellVesperFight RequestFinalBoss(Action onDefeated)
        {
            var f = Instance != null ? Instance : FindAnyObjectByType<HellVesperFight>();
            if (f == null) f = new GameObject("Vesper Fight").AddComponent<HellVesperFight>();
            f.Begin(onDefeated);
            return f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void HookRunEvents()
        {
            HellRunEvents.FinalBossRequested -= OnFinalBossRequested;
            HellRunEvents.FinalBossRequested += OnFinalBossRequested;
            Debug.Log("HELL_VESPER listening to HellRunEvents.FinalBossRequested");
        }

        // the run's boss stage: her defeat hands back to the run director (then the ending)
        static void OnFinalBossRequested(Action onDefeated) => RequestFinalBoss(onDefeated ?? HellRunEvents.BossDefeated);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            Active = false;
            Defeated = null;
        }

        bool _cheatScaled;

        void Awake() { Instance = this; }

        void OnDestroy()
        {
            if (Active) End();
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ begin and end

        /// <summary>Starts the fight (from the top). <paramref name="onDefeated"/> is called once, when she is beaten.</summary>
        public void Begin(Action onDefeated)
        {
            if (player == null) player = FindAnyObjectByType<SkylinePlayer>();
            if (player == null) { Debug.LogError("HELL_VESPER no SkylinePlayer in the scene"); return; }
            if (Active) End();
            _onDefeated = onDefeated;
            Def = HellVesperArena.World();
            if (_arena == null) _arena = HellVesperArenaView.Build(transform, Def);
            if (_arena.exitGate != null) _arena.exitGate.SetActive(true);
            var cheats = HellCaptureCheats.Current;   // CAPTURE-ONLY -hellBossHealth (docs/HELL-CAPTURE.md), scaled once per component
            if (cheats.bossHealth < 1f && !_cheatScaled)
            {
                _cheatScaled = true;
                HellCaptureCheats.ScaleBoss(tuning, cheats.bossHealth);
                Debug.Log("HELL_CAPTURE cheats: Vesper's health x" + cheats.bossHealth.ToString("0.###") + " (" + tuning.maxHealth.ToString("0") + ")");
            }
            _cheatPhase = -1;
            if (cheats.bossPhaseSeconds > 0f) Debug.Log("HELL_CAPTURE cheats: Vesper's phases forced over after " + cheats.bossPhaseSeconds.ToString("0.#") + " s each");
            Fight = new VesperFight(tuning, seed);
            var perches = HellVesperArena.Perches(Def);
            Fight.SetArena(perches, Def.bounds);
            if (Body == null) Body = HellVesperBody.Create(transform, this);
            Body.gameObject.SetActive(true);
            if (_blinkMarker == null) _blinkMarker = HellFx.MakeMarker(transform);
            if (_huntMarker == null) { _huntMarker = HellFx.MakeMarker(transform); _huntMarker.transform.localScale = Vector3.one * 2.2f; }
            if (_cutLine == null)
            {
                _cutLine = SkylineBotLook.Visual(PrimitiveType.Cube, "Vesper Cut Line", transform, HellLook.HostileGlow(HellVesperBody.HeartViolet), false);
                _cutLine.SetActive(false);
            }

            SkylineBot.HellRoster = true;
            HellEnemyFx.Prewarm();
            SkylineBolt.Prewarm();
            var bmin = Def.bounds.min;
            var bmax = Def.bounds.max;
            SkylineBot.SearchArea = Rect.MinMaxRect(bmin.X, bmin.Z, bmax.X, bmax.Z);
            RegisterDarkness();
            _lava = new LavaContact(Def.lavaDps, Def.lavaSinkDamage, Def.lavaSinkSeconds);
            _safe = new SafeSpot(Def.entry, Def.entryYaw);
            _prevLavaQuery = HellAbilities.LavaQuery;
            HellAbilities.LavaQuery = p => IsLava(p) || (_prevLavaQuery != null && _prevLavaQuery(p));
            ClearImps();
            Subscribe(true);

            player.Restart(U(Def.entry), Def.entryYaw);
            _ev.Clear();
            Fight.Begin(_ev);
            Active = true;
            HellRewindRegistry.Register(this);
            if (HellRewind.Instance != null) HellRewind.Instance.ClearHistory();
            Body.transform.position = U(Fight.S.position);
            Process();
            _hud.Show(true);
            _hud.Banner("VESPER", 2.5f);
            HellAbilityAudio.Play("vesper/intro", Body.transform.position);
            Debug.Log("HELL_VESPER begin at " + U(Def.entry) + " (" + perches.Count + " perches)");
        }

        /// <summary>Stops the fight without a winner (the scene ends, a new Begin). Leaves the arena standing.</summary>
        public void End()
        {
            Active = false;
            HellRewindRegistry.Unregister(this);
            Subscribe(false);
            SetNightfall(false);
            UnregisterDarkness();
            HellAbilities.LavaQuery = _prevLavaQuery;
            _prevLavaQuery = null;
            _hud.Show(false);
            if (_blinkMarker != null) _blinkMarker.SetActive(false);
            if (_huntMarker != null) _huntMarker.SetActive(false);
            if (_cutLine != null) _cutLine.SetActive(false);
        }

        void Subscribe(bool on)
        {
            if (player == null || on == _subscribed) return;
            _subscribed = on;
            if (on)
            {
                player.Wrecked += OnWrecked;
                player.AttackTaken += OnAttackTaken;
                _prevIntercept = player.WreckIntercept;
                player.WreckIntercept = Intercept;
            }
            else
            {
                player.Wrecked -= OnWrecked;
                player.AttackTaken -= OnAttackTaken;
                if (player.WreckIntercept == (Func<bool>)Intercept) player.WreckIntercept = _prevIntercept;
                _prevIntercept = null;
            }
        }

        void RegisterDarkness()
        {
            UnregisterDarkness();
            foreach (var z in Def.darkZones)
            {
                var bd = new Bounds();
                bd.SetMinMax(U(z.min), U(z.max));
                _zoneIds.Add(HellDarkness.RegisterZone(bd));
            }
            foreach (var l in Def.lights) _lightIds.Add(HellDarkness.RegisterLight(U(l.position), l.radius));
        }

        void UnregisterDarkness()
        {
            foreach (int id in _zoneIds) HellDarkness.UnregisterZone(id);
            _zoneIds.Clear();
            foreach (int id in _lightIds) HellDarkness.UnregisterLight(id);
            _lightIds.Clear();
            if (_nightZone >= 0) { HellDarkness.UnregisterZone(_nightZone); _nightZone = -1; }
        }

        // ------------------------------------------------------------------ the frame

        void Update()
        {
            if (!_hud.Built && SkylineHud.Instance != null && SkylineHud.Instance.Safe != null)
            {
                _hud.Build(SkylineHud.Instance.Safe, tuning.phase2At / tuning.maxHealth, tuning.phase3At / tuning.maxHealth);
                _hud.Show(Active);
            }
            _hud.Tick();
            if (!Active || Fight == null || player == null) return;
            if (SkylineDeviceInput.Paused) return;
            var input = new VesperFight.Input
            {
                dt = Time.deltaTime,
                player = N(player.transform.position),
                playerForward = N(player.AimDirection),
                playerHealth = player.Armor != null ? player.Armor.Plating + player.Armor.Chassis : 200f,
                impsAlive = ImpsAlive(),
            };
            Fight.Tick(input, _ev);
            Process();
            if (!Active) return;
            ForcePhaseCheat();
            if (!Active) return;
            Show();
        }

        int _cheatPhase = -1;
        float _cheatPhaseClock;

        /// <summary>CAPTURE-ONLY -hellBossPhaseSeconds (docs/HELL-CAPTURE.md): a phase that lasts longer than that is forced over
        /// (the damage to her phase's floor in one hit, so she shifts, or kneels in the last), so a capture always reaches the end.</summary>
        void ForcePhaseCheat()
        {
            float limit = HellCaptureCheats.Current.bossPhaseSeconds;
            if (!(limit > 0f) || Fight.Defeated) return;
            if (Fight.S.phase != _cheatPhase) { _cheatPhase = Fight.S.phase; _cheatPhaseClock = 0f; }
            _cheatPhaseClock += Time.deltaTime;
            if (_cheatPhaseClock < limit || Fight.S.action == VesperAction.Shift || !Fight.Vulnerable) return;
            int phase = Fight.S.phase;
            float done = Fight.Hit(1e7f, false, 1f, _ev);
            if (done <= 0f) return;
            _cheatPhaseClock = 0f;
            Debug.Log("HELL_CAPTURE cheats: Vesper's phase " + phase + " forced over after " + limit.ToString("0.#") + " s (-" + done.ToString("0") + ")");
            Process();
        }

        /// <summary>Everything she shows comes from her state (so a Rewind shows right at once).</summary>
        void Show()
        {
            var s = Fight.S;
            var a = s.action;
            Body.SetVisible(Fight.Visible);
            Body.transform.position = Vector3.Lerp(Body.transform.position, U(s.position), a == VesperAction.Exposed ? 18f * Time.deltaTime : 1f);
            if (a == VesperAction.StrikeWindup) Body.Face(U(s.position + s.aim));
            else if (a != VesperAction.Kneel && a != VesperAction.Done) Body.Face(player.transform.position);
            Body.SetPose(a == VesperAction.VolleyTell || a == VesperAction.Volley || a == VesperAction.SummonTell ? HellVesperBody.Pose.Cast
                : a == VesperAction.RewindTell ? HellVesperBody.Pose.Rewind
                : a == VesperAction.Stagger ? HellVesperBody.Pose.Reel
                : a == VesperAction.StrikeWindup ? HellVesperBody.Pose.Strike
                : a == VesperAction.Kneel || a == VesperAction.Done ? HellVesperBody.Pose.Kneel
                : HellVesperBody.Pose.Stand);
            Body.SetHeart(a == VesperAction.RewindTell ? HellVesperBody.HeartLook.Cold
                : a == VesperAction.Stagger ? HellVesperBody.HeartLook.Cracked
                : a == VesperAction.Exposed ? HellVesperBody.HeartLook.Blaze
                : a == VesperAction.Kneel || a == VesperAction.Done || a == VesperAction.Mercy ? HellVesperBody.HeartLook.Dim
                : HellVesperBody.HeartLook.Ember);
            Marker(_blinkMarker, a == VesperAction.BlinkTell, s.marked);
            Marker(_huntMarker, a == VesperAction.HuntTell, s.marked);
            if (_cutLine != null)
            {
                bool on = a == VesperAction.StrikeWindup;
                if (_cutLine.activeSelf != on) _cutLine.SetActive(on);
                if (on)
                {
                    var from = U(s.position) + Vector3.up * 0.05f;
                    var dir = U(s.aim);
                    float len = tuning.strikeReach;
                    _cutLine.transform.SetPositionAndRotation(from + dir * (len * 0.5f), Quaternion.LookRotation(dir));
                    _cutLine.transform.localScale = new Vector3(tuning.strikeHalfWidth * 2f * Mathf.Lerp(0.2f, 1f, 1f - s.timer / Mathf.Max(0.01f, s.actionSeconds)), 0.03f, len);
                }
            }
            SetNightfall(s.phase >= 3 && !Fight.Defeated);
            _hud.SetHealth(s.health / tuning.maxHealth, a == VesperAction.RewindTell);
        }

        static void Marker(GameObject m, bool on, NVec3 at)
        {
            if (m == null) return;
            if (m.activeSelf != on) m.SetActive(on);
            if (on) m.transform.position = U(at) + Vector3.up * 0.05f;
        }

        void FixedUpdate()
        {
            if (!Active || player == null || _lava == null) return;
            var feet = player.transform.position;
            bool touching = IsLava(feet, 0.15f);
            var sim = player.Movement != null ? player.Movement.Sim : null;
            bool grounded = sim != null && sim.Grounded;
            _safe.Tick(Time.fixedDeltaTime, N(feet), player.Yaw, grounded && !touching);
            if (touching && HellAbilities.TryLavaBounce()) _lava.S = default(LavaContact.State);
            else
            {
                float dmg = _lava.Tick(Time.fixedDeltaTime, touching, HellAbilities.LavaImmune, out bool sank);
                if (dmg > 0f) player.TakeAttack(dmg, "lava", feet + Vector3.down);
                if (sank && player.Armor != null && !player.Armor.Wrecked) BackToSafety();
            }
            if (feet.y < Def.bounds.min.Y - 6f) BackToSafety();
        }

        void BackToSafety()
        {
            player.Movement.Teleport(U(_safe.Spot) + Vector3.up * 0.1f, _safe.Yaw, Vector3.zero);
            _lava.S = default(LavaContact.State);
            HellAbilityAudio.Play("vesper/lava_sink", player.transform.position);
        }

        bool IsLava(Vector3 p) => IsLava(p, 0.3f);

        bool IsLava(Vector3 p, float above)
        {
            if (Def == null) return false;
            var n = N(p);
            foreach (var h in Def.hazards)
                if (h.kind == HellHazardKind.Lava && LavaContact.Touching(h.box, n, above)) return true;
            return false;
        }

        // ------------------------------------------------------------------ her events

        void Process()
        {
            for (int i = 0; i < _ev.Count; i++) Handle(_ev[i]);
            _ev.Clear();
        }

        void Handle(in VesperEvent e)
        {
            var at = U(e.position);
            switch (e.kind)
            {
                case VesperEventKind.Line:
                    string text = VesperLines.Text(e.line);
                    Debug.Log("HELL_VESPER line " + e.line + ": " + text);
                    // her voice (hell-voice/vesper/boss-<id>, docs/HELL-VOICE-SCRIPT.md) when the file exists, else text only;
                    // the fight's own cooldowns pace the lines and a new one stops her last
                    float voiced = HellLinePlayer.Instance.PlayVoice(HellSpeakers.Vesper, "boss-" + e.line);
                    if (SkylineHud.Instance != null) SkylineHud.Instance.Subtitles.Say("VESPER", text, false, Mathf.Max(3f, text.Length / 14f, voiced + 0.3f));
                    break;
                case VesperEventKind.PhaseBegan:
                    Debug.Log("HELL_VESPER phase " + e.index);
                    if (e.index == 2) _hud.Banner("UNDO", 2f);
                    HellAbilityAudio.Play("vesper/phase" + e.index, at);
                    break;
                case VesperEventKind.VolleyTell: HellAbilityAudio.Play("vesper/volley_tell", at); break;
                case VesperEventKind.Bolt: FireBolt(e.amount); break;
                case VesperEventKind.BlinkTell:
                    HellFx.SmokeBurst(Body.transform.position, 10, 0.5f);
                    Body.PlayBlink();   // the model's lunge as she dissolves (no-op on the stand-in)
                    HellAbilityAudio.Play("vesper/blink_tell", at);
                    break;
                case VesperEventKind.Blinked:
                    HellFx.SmokeBurst(Body.transform.position, 14, 0.7f);
                    Body.transform.position = at;
                    HellFx.SmokeBurst(at, 14, 0.7f);
                    HellAbilityAudio.Play("vesper/blink", at);
                    break;
                case VesperEventKind.SummonTell: HellAbilityAudio.Play("vesper/summon_tell", at); break;
                case VesperEventKind.Summoned: SpawnImps(e.index); break;
                case VesperEventKind.RewindTell:
                    HellAbilityAudio.Play("vesper/rewind_tell", at);
                    if (!_hintRewind) { _hintRewind = true; _hud.Hint("Her heart is winding time back: SHOOT THE HEART", 4f); }
                    break;
                case VesperEventKind.Rewound:
                    HellFx.RewindFlash();
                    HellAbilityAudio.Play("vesper/rewound", at);
                    Debug.Log("HELL_VESPER rewound +" + e.amount.ToString("0"));
                    break;
                case VesperEventKind.RewindBroken:
                    HellFx.SmokeBurst(Body.Center, 18, 0.5f);
                    HellAbilityAudio.Play("vesper/rewind_break", at);
                    _hud.Hint("Broken! She's open", 2f);
                    Debug.Log("HELL_VESPER rewind broken");
                    break;
                case VesperEventKind.NightfallBegan:
                    _hud.Banner("NIGHTFALL", 2.5f);
                    if (!_hintHunt) { _hintHunt = true; _hud.Hint("She steps out where the violet flare burns: move off her line", 5f); }
                    break;
                case VesperEventKind.HuntTell: HellAbilityAudio.Play("vesper/hunt_tell", at); break;
                case VesperEventKind.Appeared:
                    Body.transform.position = at;
                    HellFx.SmokeBurst(at, 16, 0.8f);
                    break;
                case VesperEventKind.StrikeWindup: HellAbilityAudio.Play("vesper/strike_tell", at); break;
                case VesperEventKind.StrikeHit:
                    HellAbilityAudio.Play("vesper/strike", at);
                    if (player != null) player.TakeAttack(e.amount, AttackerId, at);
                    break;
                case VesperEventKind.StrikeMissed: HellAbilityAudio.Play("vesper/strike", at); break;
                case VesperEventKind.Exposed: HellAbilityAudio.Play("vesper/exposed", at); break;
                case VesperEventKind.Mercy:
                    HellAbilityAudio.Play("vesper/mercy", at);
                    Debug.Log("HELL_VESPER pulled a killing blow short (" + e.index + ")");
                    break;
                case VesperEventKind.Shift:
                    HellAbilityAudio.Play("vesper/shift", at);
                    HellFx.SmokeBurst(Body.transform.position, 20, 1f);
                    break;
                case VesperEventKind.Kneel:
                    HellAbilityAudio.Play("vesper/kneel", at);
                    ClearImps();
                    _hud.Banner("SHE KNEELS", 3f);
                    break;
                case VesperEventKind.Defeated: OnDefeated(); break;
            }
        }

        void FireBolt(float yawOffset)
        {
            if (player == null) return;
            var from = Body.Hand.position + Vector3.up * 0.1f;
            if (!Body.gameObject.activeInHierarchy) from = U(Fight.S.position) + Vector3.up * 1.6f;
            var aim = player.Chest + player.Velocity * 0.08f;
            var dir = (aim - from).normalized;
            dir = Quaternion.AngleAxis(yawOffset, Vector3.up) * dir;
            var b = SkylineBolt.Fire(from, dir * tuning.boltSpeed, 0.22f, tuning.boltDamage, AttackerId, player, Body.transform);
            if (b != null) b.MakeEmber();
            HellAbilityAudio.Play("vesper/bolt", from);
        }

        void SpawnImps(int n)
        {
            var spots = Def.spawns;
            for (int i = 0; i < n && spots.Count > 0; i++)
            {
                var s = spots[(_impSeq + i) % spots.Count];
                var bot = SkylineBot.Spawn(SkylineEnemies.DroneId, U(s.position), s.yawDeg, seed + (ulong)(100 + _impSeq + i), player, null, transform, 7700 + _impSeq + i, Vector3.zero);
                if (bot == null) continue;
                SkylineBot.Wake(bot.group, bot);
                _imps.Add(bot);
                HellFx.SmokeBurst(U(s.position), 12, 0.6f);
            }
            _impSeq += n;
            HellAbilityAudio.Play("vesper/summon", Body.transform.position);
            Debug.Log("HELL_VESPER summoned " + n + " imps");
        }

        int ImpsAlive()
        {
            int n = 0;
            foreach (var b in _imps) if (b != null && b.Alive) n++;
            return n;
        }

        void ClearImps()
        {
            foreach (var b in _imps) if (b != null) b.gameObject.SetActive(false);
            _imps.Clear();
        }

        void OnDefeated()
        {
            Debug.Log("HELL_VESPER defeated");
            End();
            if (_arena != null && _arena.exitGate != null) _arena.exitGate.SetActive(false);
            Body.SetVisible(true);
            Body.SetPose(HellVesperBody.Pose.Kneel);
            Body.SetHeart(HellVesperBody.HeartLook.Dim);
            var cb = _onDefeated;
            _onDefeated = null;
            Defeated?.Invoke();
            cb?.Invoke();
        }

        // ------------------------------------------------------------------ the player

        /// <summary>A shot on her (from <see cref="HellVesperBody"/>): the damage done.</summary>
        public float TakeShot(float damage, bool heart, float heartMultiplier, Vector3 point)
        {
            if (!Active || Fight == null) return 0f;
            float done = Fight.Hit(damage, heart, heartMultiplier, _ev);
            Process();
            return done;
        }

        void OnAttackTaken(float damage, string attackerId, Vector3 from)
        {
            _lastAttacker = attackerId;
            _lastAttackTime = Time.time;
        }

        /// <summary>The player's wreck intercept: a killing blow of hers is pulled short (once per cooldown).</summary>
        bool Intercept()
        {
            if (_prevIntercept != null && _prevIntercept()) return true;
            if (!Active || Fight == null || _lastAttacker != AttackerId || Time.time - _lastAttackTime > 0.1f) return false;
            if (!Fight.TryMercy(_ev)) return false;
            player.Armor.Restore(0f, tuning.mercySliver);
            Process();
            return true;
        }

        void OnWrecked()
        {
            if (!Active || Fight == null) return;
            Debug.Log("HELL_VESPER the player died: the fight restarts" + (keepPhaseOnDeath ? " (this phase)" : ""));
            ClearImps();
            SkylineBolt.ClearAll();
            _lava.S = default(LavaContact.State);
            _safe.Reset(Def.entry, Def.entryYaw);
            Fight.Restart(keepPhaseOnDeath, _ev);
            player.Restart(U(Def.entry), Def.entryYaw);
            Body.transform.position = U(Fight.S.position);
            Process();
            _hud.Banner("AGAIN", 1.5f);
            HellRunEvents.RaiseRoomRestarted();   // the Rewind drops its history: it never reaches back past a death
        }

        // ------------------------------------------------------------------ darkness

        /// <summary>Nightfall (phase 3): the fire lights go out, the whole arena counts as dark (her hunt and your Shadow Step),
        /// the ambient light and fog drop. The lava keeps glowing.</summary>
        void SetNightfall(bool on)
        {
            if (on == _nightfall) return;
            _nightfall = on;
            // the braziers (the fire lights) go out; the lava's own lights keep burning, so the arena stays navigable by the lava
            if (_arena != null) foreach (var l in _arena.fireLights) if (l != null) l.enabled = !on;
            if (on)
            {
                foreach (int id in _lightIds) HellDarkness.UnregisterLight(id);
                _lightIds.Clear();
                var bd = new Bounds();
                bd.SetMinMax(U(Def.bounds.min), U(Def.bounds.max));
                _nightZone = HellDarkness.RegisterZone(bd);
                // darker, not black: the whole ambient (the look's gradient probe too) at half, the fog a deep violet
                _ambient = RenderSettings.ambientLight;
                _fog = RenderSettings.fogColor;
                _probe = RenderSettings.ambientProbe;
                _ambientSaved = true;
                var dim = _probe;
                dim *= 0.5f;
                RenderSettings.ambientProbe = dim;
                RenderSettings.fogColor = Color.Lerp(_fog, new Color(0.05f, 0.015f, 0.08f), 0.6f);
                HellAbilityAudio.Play("vesper/nightfall", Body != null ? Body.transform.position : transform.position);
            }
            else
            {
                if (_nightZone >= 0) { HellDarkness.UnregisterZone(_nightZone); _nightZone = -1; }
                if (Def != null && Active) foreach (var l in Def.lights) _lightIds.Add(HellDarkness.RegisterLight(U(l.position), l.radius));
                if (_ambientSaved)
                {
                    _ambientSaved = false;
                    RenderSettings.ambientLight = _ambient;
                    RenderSettings.fogColor = _fog;
                    RenderSettings.ambientProbe = _probe;
                }
            }
        }

        // ------------------------------------------------------------------ the global Rewind

        public object Snapshot()
        {
            var s = _pool.Next();
            Fight.SaveTo(s.fight);
            s.lava = _lava != null ? _lava.S : default(LavaContact.State);
            return s;
        }

        public void Restore(object snapshot)
        {
            if (!(snapshot is Snap s) || Fight == null) return;
            Fight.LoadFrom(s.fight);
            if (_lava != null) _lava.S = s.lava;
            if (Body != null) Body.transform.position = U(Fight.S.position);
            Debug.Log("HELL_VESPER rewound with the player: phase " + Fight.S.phase + ", " + Fight.S.action + ", health " + Fight.S.health.ToString("0"));
        }

        static Vector3 U(NVec3 v) => new Vector3(v.X, v.Y, v.Z);
        static NVec3 N(Vector3 v) => new NVec3(v.x, v.y, v.z);
    }
}
