using System;
using System.Collections.Generic;
using JamKit.Movement;
using JamKit.Skyline;
using UnityEngine;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The player's Hell abilities (docs/HELL-ABILITIES.md), on the player next to <see cref="SkylinePlayer"/>:
    /// <list type="bullet">
    /// <item><b>Shadow Step</b> (E / RB, rebindable): hold to aim (a marker on the landing: violet valid, red not), release to
    /// arrive there at once keeping your velocity. The landing must be reachable ground or a ledge within 18 m in sight
    /// (<see cref="BlinkPlanner"/>) and dark (<see cref="HellDarkness"/>; lava too with Ashwalker). Charges (1; 2 with
    /// Ambush), 6 s each. Dark smoke at both ends. Blocked while <see cref="ShadowStepBlocked"/>.</item>
    /// <item><b>Rewind</b> (V / LB): the whole game goes back 4 s (6 with Deeper Rewind), cooldown 30 s
    /// (<see cref="HellRewind"/>), then mends 25% of the max health and of the max shields over 4 s (<see cref="Mend"/>; a
    /// new Rewind restarts the mend, never stacks it). This component is also the player's <see cref="IHellRewindable"/>.</item>
    /// <item><b>Time Dilation</b> (passive, a fuel gauge, the creator 2026-10-18): pressing aim down sights slows time by the
    /// speed at the press (<see cref="DilationCurve"/>: none standing, 0.35x at slide speed) while you keep aiming and the
    /// fuel lasts (<see cref="Fuel"/>: full to empty in 4 s of use, refills in 8 s out of use, needs 25% to start), easing
    /// 0.15 s in and out; aiming when it can't start, or after it ran dry, is plain aiming. The look stays real-time (the
    /// player reads look on unscaled time). Through <see cref="HellTime"/>. The HUD reads <see cref="DilationFuel01"/>.</item>
    /// <item><b>Floor skills</b>: <see cref="Grant"/> adds a skill's component (<see cref="HellSkills"/>); <see cref="Has"/>
    /// asks. The skills hook <see cref="Stepped"/>, <see cref="HellRewind.Rewound"/>, the player's gun and damage events,
    /// <see cref="SkylineBot.Killed"/> and <see cref="HellTime"/>.</item>
    /// </list>
    /// Added to every <see cref="SkylinePlayer"/> after a scene loads (<see cref="AutoAttach"/>), with <see cref="HellRewind"/>,
    /// <see cref="HellTargeting"/>, <see cref="HellSkillPick"/> and <see cref="HellHud"/>. Runs after the player has read its
    /// input frame and before the body moves (-45: SkylinePlayer -50, MovementController -40).
    /// </summary>
    [DefaultExecutionOrder(-45)]
    public sealed class HellAbilities : MonoBehaviour, IHellRewindable
    {
        // ------------------------------------------------------------------ statics (the contract with the other branches)

        /// <summary>Set by rooms or hazards to forbid Shadow Step (the preview shows red).</summary>
        public static bool ShadowStepBlocked;
        /// <summary>Ashwalker: true for 6 s after a Shadow Step onto lava (the rooms branch's lava does no harm then).</summary>
        public static bool LavaImmune => Instance != null && Time.time < Instance._lavaImmuneUntil;
        /// <summary>Ashwalker (passive, owned): a jump-timed touch of lava bounces you with no damage (see <see cref="TryLavaBounce"/>).</summary>
        public static bool LavaHop => Instance != null && Instance.Has(HellSkills.Ashwalker);
        /// <summary>The rooms branch sets this: is the surface at this point lava? (Shadow Step onto lava with Ashwalker.)</summary>
        public static Func<Vector3, bool> LavaQuery;
        /// <summary>Off: nothing is added to the player (scripted demos that must stay as they were).</summary>
        public static bool AutoAttach = true;
        public static HellAbilities Instance { get; private set; }
        /// <summary>Time Dilation's fuel, 0 empty to 1 full (1 when there is no player): for the HUD's gauge.</summary>
        public static float DilationFuel01 => Instance != null ? Instance.Fuel.Fuel01 : 1f;
        /// <summary>Time Dilation is slowing time now (the fuel draining).</summary>
        public static bool DilationInUse => Instance != null && Instance.Fuel.InUse;
        /// <summary>Time Dilation could start on the next aim press (at least 25% fuel).</summary>
        public static bool DilationCanStart => Instance == null || Instance.Fuel.CanStart;

        // ------------------------------------------------------------------ tuning

        [Header("Shadow Step")]
        public BlinkDef blink = new BlinkDef();
        [Tooltip("Hold-to-aim shorter than this blinks on release anyway (a tap aims and goes).")]
        public float minAimSeconds = 0f;
        [Header("Time Dilation")]
        public DilationCurve dilationCurve = new DilationCurve();
        [Tooltip("Seconds of slow-down a full gauge holds (the Long Breath boon adds to it); it refills at half that rate.")]
        public float dilationFuelSeconds = 4f;
        public float dilationEase = 0.15f;
        [Header("Rewind's mend")]
        public float rewindMendShare = 0.25f, rewindMendSeconds = 4f;
        public MomentumBank momentumBank = new MomentumBank();
        [Header("Skills")]
        public float umbralChainWindow = 1f;
        public float ashwalkerSeconds = 6f;
        public float lavaHopWindow = 0.15f, lavaHopHeight = 1.4f;

        // ------------------------------------------------------------------ state

        public SkylinePlayer Player { get; private set; }
        public HellRewind Rewind { get; private set; }
        public HellTargeting Targeting { get; private set; }
        public AbilityCharges StepCharges { get; private set; }
        public TimeDilationState Dilation { get; } = new TimeDilationState();
        /// <summary>Time Dilation's fuel gauge (real seconds; not rewound).</summary>
        public DilationFuel Fuel { get; } = new DilationFuel();
        /// <summary>The Rewind's mend over time (game time; not rewound: it starts after the restore).</summary>
        public RewindMend Mend { get; } = new RewindMend();
        /// <summary>The Shadow Step preview this frame (valid or why not), while aiming.</summary>
        public BlinkPlan Preview => _plan;
        public bool Aiming => _aiming;
        /// <summary>A Shadow Step happened: from feet, to feet.</summary>
        public event Action<Vector3, Vector3> Stepped;
        /// <summary>A skill was granted (its id).</summary>
        public event Action<string> Granted;
        public IReadOnlyCollection<string> Owned => _owned;

        readonly HashSet<string> _owned = new HashSet<string>();
        readonly UnityBlinkWorld _world = new UnityBlinkWorld();
        BlinkPlan _plan;
        bool _aiming, _aimWas;
        float _aimSince, _chainUntil = -1f, _lavaImmuneUntil = -1f, _lastJumpAt = -10f, _drainScale = 1f;
        GameObject _marker;

        // ------------------------------------------------------------------ setup

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AttachOnLoad()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            AttachAll();
        }

        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => AttachAll();

        /// <summary>Puts the abilities on every player in the scene that has none.</summary>
        public static void AttachAll()
        {
            if (!AutoAttach) return;
            // the scripted runs (capture, demo, perf probe) stay as they were unless asked for: their recordings are timed
            if (SkylineRunArgs.Demo && !SkylineRunArgs.Has("-hell-abilities")) return;
            foreach (var p in FindObjectsByType<SkylinePlayer>(FindObjectsSortMode.None))
                if (p.GetComponent<HellAbilities>() == null) p.gameObject.AddComponent<HellAbilities>();
        }

        void Awake()
        {
            Instance = this;
            Player = GetComponent<SkylinePlayer>();
            StepCharges = new AbilityCharges(1, blink.cooldown);
            Rewind = GetComponent<HellRewind>();
            if (Rewind == null) Rewind = gameObject.AddComponent<HellRewind>();
            Targeting = GetComponent<HellTargeting>();
            if (Targeting == null) Targeting = gameObject.AddComponent<HellTargeting>();
            Targeting.player = Player;
            if (GetComponent<HellSkillPick>() == null) gameObject.AddComponent<HellSkillPick>();
            if (GetComponent<HellHud>() == null) gameObject.AddComponent<HellHud>();
            _marker = HellFx.MakeMarker(null);
        }

        void OnEnable()
        {
            HellRewindRegistry.Register(this);
        }

        void OnDisable()
        {
            HellRewindRegistry.Unregister(this);
            HellTime.Clear(HellTime.DilationKey);
            HellAbilityAudio.SetPitch(1f);
            if (_marker != null) _marker.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_marker != null) Destroy(_marker);
        }

        // ------------------------------------------------------------------ skills

        public bool Has(string id) => _owned.Contains(id);

        /// <summary>Gives the player a floor skill: its component is added and told (<see cref="HellSkill.OnGranted"/>).</summary>
        public void Grant(string id)
        {
            if (string.IsNullOrEmpty(id) || !_owned.Add(id)) return;
            HellSkills.AddComponent(this, id);
            Debug.Log("HELL_SKILL granted " + id);
            Granted?.Invoke(id);
        }

        // ------------------------------------------------------------------ the frame

        void Update()
        {
            if (Player == null) return;
            if (SkylineDeviceInput.Paused || HellSkillPick.Open) { CancelAim(); return; }
            HellTime.Apply();   // after the pause menu let go, the scale comes back
            float dt = Time.deltaTime, udt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            StepCharges.Recharge = blink.cooldown;
            StepCharges.Tick(dt);
            var f = Player.Frame;
            if (f.jumpPressed) _lastJumpAt = Time.time;
            bool wrecked = Player.Armor != null && Player.Armor.Wrecked;

            ReadButtons(out bool stepHeld, out bool rewindPressed);
            if (!wrecked) TickShadowStep(stepHeld);
            else CancelAim();
            if (rewindPressed && !wrecked)
            {
                if (Rewind.TryRewind()) OnRewound();
                else HellAbilityAudio.Play("rewind_denied", transform.position);
            }
            TickMend(dt, wrecked);
            TickDilation(udt);
        }

        /// <summary>A replay's buttons (docs/HELL-CAPTURE.md): when set, Shadow Step, Rewind and the skill button read from it
        /// instead of the devices (step held, rewind pressed, skill pressed).</summary>
        public static Func<HellScriptedButtons> ScriptedButtons;

        static void ReadButtons(out bool stepHeld, out bool rewindPressed)
        {
            if (ScriptedButtons != null)
            {
                var b = ScriptedButtons();
                stepHeld = b.stepHeld;
                rewindPressed = b.rewindPressed;
                return;
            }
            bool mouseOk = Application.isBatchMode || Cursor.lockState == CursorLockMode.Locked;
            const BindDevice KM = BindDevice.KeyboardMouse, Pad = BindDevice.Pad;
            stepHeld = SkylineBindings.Held(KM, BindAction.ShadowStep, mouseOk) || SkylineBindings.Held(Pad, BindAction.ShadowStep, mouseOk);
            rewindPressed = SkylineBindings.Pressed(KM, BindAction.Rewind, mouseOk) || SkylineBindings.Pressed(Pad, BindAction.Rewind, mouseOk);
            if (HellAbilityWheel.IsOpen) stepHeld = rewindPressed = false;   // the wheel has the look; no stepping blind
        }

        /// <summary>The floor skill button (G / D-pad down) this frame, for active skills (Nightfall).</summary>
        public static bool SkillPressed()
        {
            if (SkylineDeviceInput.Paused || HellSkillPick.Open) return false;
            if (ScriptedButtons != null) return ScriptedButtons().skillPressed;
            bool mouseOk = Application.isBatchMode || Cursor.lockState == CursorLockMode.Locked;
            return SkylineBindings.Pressed(BindDevice.KeyboardMouse, BindAction.Skill, mouseOk) || SkylineBindings.Pressed(BindDevice.Pad, BindAction.Skill, mouseOk);
        }

        // ------------------------------------------------------------------ Shadow Step

        bool FreeStepReady => Has(HellSkills.UmbralChain) && Time.time < _chainUntil;
        /// <summary>A step could be taken now (a charge or Umbral Chain's free one), not blocked.</summary>
        public bool StepReady => !ShadowStepBlocked && (StepCharges.Ready || FreeStepReady);

        void TickShadowStep(bool held)
        {
            if (held && !_aiming) { _aiming = true; _aimSince = Time.unscaledTime; }
            if (_aiming)
            {
                _plan = PlanNow();
                if (_marker != null)
                {
                    bool show = _plan.feet != Vector3Zero || _plan.valid;
                    _marker.SetActive(show);
                    if (show)
                    {
                        _marker.transform.position = U(_plan.feet);
                        HellFx.TintMarker(_marker, _plan.valid && StepReady);
                    }
                }
            }
            if (_aiming && !held)
            {
                _aiming = false;
                if (_marker != null) _marker.SetActive(false);
                if (Time.unscaledTime - _aimSince < minAimSeconds) return;
                if (_plan.valid && StepReady) Blink(U(_plan.feet));
                else HellAbilityAudio.Play("shadowstep_denied", transform.position);
            }
        }

        static readonly NVec3 Vector3Zero = NVec3.Zero;

        void CancelAim()
        {
            _aiming = false;
            if (_marker != null && _marker.activeSelf) _marker.SetActive(false);
        }

        /// <summary>Where a step aimed now would land (geometry, then the darkness rule). An invalid plan still carries the
        /// spot it was refused at when there is one, so the marker can show it red.</summary>
        BlinkPlan PlanNow()
        {
            _world.mask = WorldMask();
            Vector3 eye = Player.EyePosition, aim = Player.AimDirection;
            var plan = BlinkPlanner.Plan(N(eye), N(aim), blink, _world);
            if (!plan.valid)
            {
                // show where the aim points, red, when it hits something
                if (Physics.Raycast(eye, aim, out var hit, blink.Range, _world.mask, QueryTriggerInteraction.Ignore)) plan.feet = N(hit.point);
                return plan;
            }
            Vector3 feet = U(plan.feet);
            bool lava = LavaQuery != null && LavaQuery(feet);
            bool dark = HellDarkness.IsDark(feet + Vector3.up * 0.5f);
            plan = BlinkRules.Apply(plan, dark, lava, Has(HellSkills.Ashwalker));
            if (ShadowStepBlocked) { plan.valid = false; plan.reason = "blocked"; }
            return plan;
        }

        int WorldMask()
        {
            var mc = Player.Movement;
            int m = mc != null ? mc.worldMask.value : ~0;
            return m & ~(1 << 2);
        }

        /// <summary>Moves the player to <paramref name="feet"/> at once, keeping the velocity, and runs the step's effects. No
        /// checks (use <see cref="Preview"/>); skills may call it.</summary>
        public void Blink(Vector3 feet)
        {
            bool free = FreeStepReady;
            if (free) _chainUntil = -1f;                                     // Umbral Chain's free step is spent (it chains once)
            else
            {
                if (!StepCharges.TrySpend()) return;
                if (Has(HellSkills.UmbralChain)) _chainUntil = Time.time + umbralChainWindow;
            }
            var mc = Player.Movement;
            Vector3 from = transform.position;
            Vector3 vel = mc != null ? mc.Velocity : Vector3.zero;
            if (mc != null) mc.Teleport(feet, Player.Yaw, vel, Player.Pitch);
            Player.SetAim(Player.Yaw, Player.Pitch);
            HellFx.SmokeBurst(from);
            HellFx.SmokeBurst(feet);
            HellAbilityAudio.Play("shadowstep_out", from);
            HellAbilityAudio.Play("shadowstep_in", feet);
            if (Has(HellSkills.Ashwalker) && LavaQuery != null && LavaQuery(feet)) _lavaImmuneUntil = Time.time + ashwalkerSeconds;
            Debug.Log("HELL_STEP " + from.ToString("F1") + " -> " + feet.ToString("F1") + (free ? " (chain)" : "") + " charges=" + StepCharges.Charges);
            Stepped?.Invoke(from, feet);
        }

        // ------------------------------------------------------------------ Rewind

        void OnRewound()
        {
            HellFx.RewindFlash();
            HellAbilityAudio.Play("rewind", transform.position);
            Dilation.Cancel();
            Fuel.Stop();
            CancelAim();
            StartMend();
        }

        /// <summary>The Rewind's mend: 25% of the max health and shields over 4 s, from the restored values. Called after every
        /// Rewind the player presses; a running mend restarts rather than stacks.</summary>
        public void StartMend()
        {
            Mend.share = rewindMendShare;
            Mend.seconds = rewindMendSeconds;
            Mend.Start();
            Debug.Log("HELL_REWIND mend " + (rewindMendShare * 100f).ToString("0") + "% over " + rewindMendSeconds.ToString("0.#") + " s");
        }

        void TickMend(float dt, bool wrecked)
        {
            var ar = Player.Armor;
            if (wrecked || ar == null) { Mend.Cancel(); return; }
            if (!Mend.Active) return;
            float k = Mend.Tick(dt);
            if (k > 0f) ar.Restore(ar.Plating + k * ar.PlatingMax, ar.Chassis + k * ar.ChassisMax);
        }

        readonly HellSnapPool<SkylinePlayer.RewindState> _snaps = new HellSnapPool<SkylinePlayer.RewindState>();

        object IHellRewindable.Snapshot() => Player != null ? Player.SaveForRewind(_snaps.Next()) : null;

        void IHellRewindable.Restore(object snapshot)
        {
            if (Player != null && snapshot is SkylinePlayer.RewindState s) Player.RestoreFromRewind(s);
        }

        // ------------------------------------------------------------------ Time Dilation

        /// <summary>The fuel gauge: an aim press while moving fast starts it (if at least 25% is left), at a strength set by the
        /// speed at the press (the old rule, kept); it lasts while the aim is held and the fuel lasts. Letting go of the aim
        /// or running dry eases time back; holding the aim on after that, or pressing with too little fuel, is plain aiming.</summary>
        void TickDilation(float udt)
        {
            bool aim = Player.AimWanted;
            Fuel.drainSeconds = dilationFuelSeconds;
            if (aim && !_aimWas)
            {
                var mc = Player.Movement;
                float speed = mc != null ? mc.Speed : 0f;
                float target = dilationCurve.Scale(speed);
                if (target < 0.995f && Fuel.TryStart())
                {
                    // Momentum Bank: the faster the press, the slower the fuel drains (up to twice as long at full speed)
                    _drainScale = Has(HellSkills.MomentumBank) ? momentumBank.baseHold / Mathf.Max(0.01f, momentumBank.Hold(speed)) : 1f;
                    Dilation.holdSeconds = Fuel.SecondsLeft(_drainScale);
                    Dilation.easeIn = Dilation.easeOut = dilationEase;
                    if (!Dilation.Active) HellAbilityAudio.Play("dilation_in", transform.position);
                    Dilation.Press(target);
                    Debug.Log("HELL_DILATION speed=" + speed.ToString("0.0") + " scale=" + target.ToString("0.00") + " fuel=" + Fuel.Fuel01.ToString("0.00") + " lasts=" + Dilation.holdSeconds.ToString("0.0"));
                }
                else if (target < 0.995f) Debug.Log("HELL_DILATION refused: fuel " + Fuel.Fuel01.ToString("0.00") + " (needs " + Fuel.minToStart.ToString("0.00") + ")");
            }
            if (Fuel.InUse && !aim) { Fuel.Stop(); Dilation.Release(); }
            if (Fuel.Tick(udt, _drainScale)) { Dilation.Release(); Debug.Log("HELL_DILATION fuel ran dry"); }
            _aimWas = aim;
            bool was = Dilation.Active;
            float scale = Dilation.Tick(udt);
            if (was && !Dilation.Active) HellAbilityAudio.Play("dilation_out", transform.position);
            HellTime.Request(HellTime.DilationKey, scale);
            HellAbilityAudio.SetPitch(scale);
            HellFx.SetDilation(Dilation.Strength(dilationCurve.minScale));
        }

        // ------------------------------------------------------------------ Ashwalker's lava hop

        /// <summary>For the rooms branch's lava, every frame the player's feet touch it: with Ashwalker owned (<see cref="LavaHop"/>)
        /// and a jump pressed within the hop window, the player bounces up and true comes back: no lava damage this touch.</summary>
        public static bool TryLavaBounce()
        {
            var a = Instance;
            if (a == null || !LavaHop || a.Player == null) return false;
            if (Time.time - a._lastJumpAt > a.lavaHopWindow) return false;
            var sim = a.Player.Movement != null ? a.Player.Movement.Sim : null;
            if (sim == null) return false;
            float up = Mathf.Sqrt(2f * Mathf.Abs(sim.def.gravity) * a.lavaHopHeight);
            float need = up - sim.Velocity.Y;
            if (need <= 0f) return true;
            if (!sim.AddPush(new NVec3(0f, need, 0f))) return false;
            a._lastJumpAt = -10f;
            HellAbilityAudio.Play("lava_hop", a.transform.position);
            HellFx.SmokeBurst(a.transform.position, 6, 0.4f);
            return true;
        }

        // ------------------------------------------------------------------ helpers

        static NVec3 N(Vector3 v) => new NVec3(v.x, v.y, v.z);
        static Vector3 U(NVec3 v) => new Vector3(v.X, v.Y, v.Z);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            ShadowStepBlocked = false;
            LavaQuery = null;
            Instance = null;
        }

        /// <summary>The blink planner's view of the level: solid colliders only (no triggers, no bots' hit zones or shed parts,
        /// not the player).</summary>
        sealed class UnityBlinkWorld : IBlinkWorld
        {
            public int mask = ~0;
            static readonly RaycastHit[] _hits = new RaycastHit[32];
            static readonly Collider[] _over = new Collider[16];

            public bool Raycast(NVec3 from, NVec3 dir, float maxDistance, out NVec3 point, out NVec3 normal)
            {
                point = normal = NVec3.Zero;
                int n = Physics.RaycastNonAlloc(U(from), U(dir), _hits, maxDistance, mask, QueryTriggerInteraction.Ignore);
                float best = float.PositiveInfinity;
                for (int i = 0; i < n; i++)
                {
                    var c = _hits[i].collider;
                    if (Skip(c) || _hits[i].distance >= best) continue;
                    best = _hits[i].distance;
                    point = N(_hits[i].point);
                    normal = N(_hits[i].normal);
                }
                return best < float.PositiveInfinity;
            }

            public bool BodyFits(NVec3 feet, float radius, float height)
            {
                Vector3 f = U(feet);
                int n = Physics.OverlapCapsuleNonAlloc(f + Vector3.up * (radius + 0.05f), f + Vector3.up * (height - radius), radius, _over, mask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++) if (!Skip(_over[i])) return false;
                return true;
            }

            static bool Skip(Collider c) =>
                c == null || c.TryGetComponent<SkylineHitZone>(out _) || c.TryGetComponent<SkylineDebris>(out _) || c.GetComponentInParent<SkylinePlayer>() != null
                || c.GetComponentInParent<HellShade>() != null;
        }
    }
}
