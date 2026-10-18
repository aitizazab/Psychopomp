using System;
using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.InputSystem;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// THE ENDING (docs/HELL-ENDING.md), after Vesper kneels: the decision in play, the escape, the three endings.
    /// <list type="bullet">
    /// <item><b>The decision</b> (<see cref="HellDecision"/>): she kneels where the fight left her
    /// (<see cref="HellVesperFight.VesperPosition"/>) and asks you to finish it; Hell shakes harder the longer you stand there,
    /// and the God of Death presses you. <b>Kill:</b> any shot that hits her (her hit zones answer to this component while
    /// you decide). <b>Spare:</b> lower the gun (hold Reload, or simply don't fire), walk to her and hold Use (the Inspect
    /// binding, or B on a pad) until the ring round the crosshair fills: you offer her a purpose, she takes your hand.</item>
    /// <item><b>The escape:</b> floor 3's Collapse (<see cref="HellEscapeRoute"/>), its fronts, debris and wall imps as built,
    /// re-run from its start. Killed: her soul glows violet in your lantern (no slowdown, Shadow Step allowed: the Collapse is
    /// tuned for full speed). Spared: she runs at your shoulder (<see cref="HellFollower"/>, her own body) and Shadow Steps
    /// ahead now and then. Both: your sister's soul hovers halfway along each lane; passing it puts it in your lantern. A
    /// death or a sink restarts the escape. Rewindable like the Collapse (the room, the soul, Vesper beside you).</item>
    /// <item><b>The endings:</b> black, the God of Death's two lines as cards, the epilogue card, the ending's name, then
    /// <see cref="HellRunEvents.RaiseEnding"/>: the credits, the stats, the title.</item>
    /// </list>
    /// The Rewind can't undo the choice: the history clears when she kneels and again when the escape starts, and the
    /// decision is not a rewindable. Scripted runs (a scripted player, batch mode) choose by themselves after 2 s
    /// (<c>-hellEnding kill|spare</c>, default kill) and skip the escape. Logs <c>HELL_ENDING ...</c>.
    /// </summary>
    [AddComponentMenu("JamKit/Hell/Ending Director")]
    public sealed class HellEndingDirector : MonoBehaviour, ISkylineHittable, IHellRewindable
    {
        public enum Phase { Idle, Decision, AfterChoice, ToEscape, Escape, Out, Cards, Done }

        public SkylinePlayer player;
        public HellDecisionTuning decisionTuning = new HellDecisionTuning();
        public HellFollowerTuning followerTuning = new HellFollowerTuning();
        /// <summary>The held lantern, in the camera's frame.</summary>
        public Vector3 lanternOffset = new Vector3(0.42f, -0.36f, 0.85f);

        public static HellEndingDirector Instance { get; private set; }
        /// <summary>True from her kneel to the ending's id.</summary>
        public static bool Active => Instance != null && Instance._phase != Phase.Idle && Instance._phase != Phase.Done;

        public Phase Stage => _phase;
        public HellDecision Decision => _decision;

        static readonly Color VesperSoul = new Color(1.6f, 0.6f, 3.6f);
        static readonly Color SisterSoul = new Color(3.4f, 2.7f, 1.5f);

        Phase _phase;
        HellDecision _decision;
        readonly List<HellDecisionEvent> _ev = new List<HellDecisionEvent>();
        readonly HellEndingHud _hud = new HellEndingHud();
        HellEndingCards _cards;
        HellVesperBody _body;
        readonly List<SkylineHitZone> _zones = new List<SkylineHitZone>();
        readonly List<MonoBehaviour> _zoneOwners = new List<MonoBehaviour>();
        Vector3 _kneelAt, _decisionSpot;
        float _decisionYaw;
        bool _killed, _scripted, _autoSpare, _skipSweep, _damned;
        float _phaseT, _nextLineAt, _rumbleAt, _cardShownAt;
        readonly Queue<string> _lines = new Queue<string>();
        HellRoom _room;
        HellSoulPickup _soul;
        HellFollower _follower;
        Vector3 _lastFeet;
        readonly List<GameObject> _soulViews = new List<GameObject>();
        GameObject _lantern, _lanternVesper, _lanternSister;
        Light _lanternLight;
        string _endingId;
        int _escapeTries;
        AudioSource _voice;
        double _lastPad = -1, _lastKbm;
        bool _subscribed;

        sealed class Snap
        {
            public bool soul;
            public HellFollower.State follower;
            public bool hasFollower;
        }

        readonly HellSnapPool<Snap> _snaps = new HellSnapPool<Snap>();

        // ------------------------------------------------------------------ the hook

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void HookRunEvents()
        {
            HellRunEvents.EndingRequested -= OnEndingRequested;
            HellRunEvents.EndingRequested += OnEndingRequested;
            Debug.Log("HELL_ENDING listening to HellRunEvents.EndingRequested");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        static void OnEndingRequested() => Request();

        /// <summary>Finds or makes the director and begins the ending (she must be kneeling: after the boss).</summary>
        public static HellEndingDirector Request()
        {
            var d = Instance != null ? Instance : FindAnyObjectByType<HellEndingDirector>();
            if (d == null) d = new GameObject("Hell Ending").AddComponent<HellEndingDirector>();
            d.Begin();
            return d;
        }

        void Awake()
        {
            Instance = this;
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.spatialBlend = 0f;
        }

        void OnDestroy()
        {
            Subscribe(false);
            GiveZonesBack();
            HellRewindRegistry.Unregister(this);
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ the decision

        /// <summary>Starts the decision over the kneeling Vesper.</summary>
        public void Begin()
        {
            if (player == null) player = FindAnyObjectByType<SkylinePlayer>();
            if (player == null)
            {
                Debug.LogError("HELL_ENDING no SkylinePlayer: straight to the credits");
                HellRunEvents.RaiseEnding(HellEndings.Kill);
                return;
            }
            Clean();
            var fight = HellVesperFight.Instance;
            _body = fight != null ? fight.Body : null;
            if (_body == null)
            {
                // no fight ran (a test scene): a stand-in kneels 6 m in front of you
                _body = HellVesperBody.Create(transform, null);
                var f = player.AimDirection;
                f.y = 0f;
                f = f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward;
                _body.transform.position = player.transform.position + f * 6f;
            }
            _body.gameObject.SetActive(true);
            _body.SetVisible(true);
            _body.SetPose(HellVesperBody.Pose.Kneel);
            _body.SetHeart(HellVesperBody.HeartLook.Dim);
            _kneelAt = _body.transform.position;
            _decisionSpot = player.transform.position;
            _decisionYaw = player.Yaw;
            TakeZones();

            _scripted = player.ScriptedInput != null || Application.isBatchMode || SkylineRunArgs.Demo;
            _autoSpare = string.Equals(SkylineRunArgs.Value("-hellEnding"), "spare", StringComparison.OrdinalIgnoreCase);
            if (_cards == null) _cards = HellEndingCards.Create();
            _hud.Build();
            Subscribe(true);

            _decision = new HellDecision(decisionTuning);
            _ev.Clear();
            _decision.Begin(_ev);
            _phase = Phase.Decision;
            _phaseT = 0f;
            _rumbleAt = 0f;
            HellRunEvents.RaiseRewindBoundary();     // the Rewind never reaches back into the fight from here
            Debug.Log("HELL_ENDING decision: she kneels at " + _kneelAt + (_scripted ? " (scripted: auto " + (_autoSpare ? "spare" : "kill") + ")" : ""));
            Process();
        }

        void Subscribe(bool on)
        {
            if (on == _subscribed) return;
            _subscribed = on;
            if (on)
            {
                if (player != null) player.Wrecked += OnWrecked;
                HellRunEvents.RoomRestarted += OnRoomRestarted;
                HellRewind.Rewound += OnRewound;
            }
            else
            {
                if (player != null) player.Wrecked -= OnWrecked;
                HellRunEvents.RoomRestarted -= OnRoomRestarted;
                HellRewind.Rewound -= OnRewound;
            }
        }

        /// <summary>While you decide, her hit zones answer to the ending (the fight is over and ignores shots).</summary>
        void TakeZones()
        {
            GiveZonesBack();
            foreach (var z in _body.GetComponentsInChildren<SkylineHitZone>(true))
            {
                _zones.Add(z);
                _zoneOwners.Add(z.owner);
                z.owner = this;
            }
        }

        void GiveZonesBack()
        {
            for (int i = 0; i < _zones.Count; i++) if (_zones[i] != null) _zones[i].owner = _zoneOwners[i];
            _zones.Clear();
            _zoneOwners.Clear();
        }

        /// <summary>A shot on the kneeling Vesper: the choice is made.</summary>
        public SkylineHitResult TakeHit(in SkylineHitInfo hit)
        {
            if (_phase != Phase.Decision || _decision == null || _decision.Decided) return default(SkylineHitResult);
            _decision.Kill(_ev);
            Process();
            return new SkylineHitResult
            {
                hit = true,
                killed = true,
                damage = new BotDamage { toChassis = hit.damage, plateShed = -1, wrecked = true },
            };
        }

        void Update()
        {
            _hud.Tick();
            TrackDevice();
            if (_phase == Phase.Idle || _phase == Phase.Done || player == null) return;
            float dt = Time.deltaTime;
            if (_phase == Phase.Cards) { TickCards(); return; }
            if (SkylineDeviceInput.Paused) return;
            _phaseT += dt;
            switch (_phase)
            {
                case Phase.Decision: TickDecision(dt); break;
                case Phase.AfterChoice:
                    Quake(1f);
                    if (_lines.Count > 0 && Time.unscaledTime >= _nextLineAt) _nextLineAt = Time.unscaledTime + Say(_lines.Dequeue());
                    else if (_lines.Count == 0 && Time.unscaledTime >= _nextLineAt) StartEscape();
                    break;
                case Phase.ToEscape:
                    Quake(1f);
                    if (_cards.Black) BeginEscape();
                    break;
                case Phase.Escape: TickEscape(dt); break;
                case Phase.Out:
                    if (_cards.Black) StartCards();
                    break;
            }
        }

        /// <summary>Makes the choice now (the test bed's keys; scripted runs): kill, or spare as if the hold ring filled.</summary>
        public void ForceChoice(bool kill)
        {
            if (_phase != Phase.Decision || _decision == null || _decision.Decided) return;
            if (kill) _decision.Kill(_ev);
            else
            {
                _decision.Tick(new HellDecisionInput { dt = decisionTuning.lowerAfterSeconds + 0.1f, distance = 0f }, _ev);
                _decision.Tick(new HellDecisionInput { dt = decisionTuning.holdSeconds + 0.1f, distance = 0f, useHeld = true }, _ev);
            }
            Process();
        }

        void TickDecision(float dt)
        {
            if (_scripted)
            {
                if (_phaseT >= 2f) ForceChoice(!_autoSpare);
                return;
            }
            var feet = player.transform.position;
            var flat = new Vector2(feet.x - _kneelAt.x, feet.z - _kneelAt.z);
            var input = new HellDecisionInput
            {
                dt = dt,
                distance = flat.magnitude,
                useHeld = UseHeld(),
                holsterHeld = HolsterHeld(),
                fired = player.Frame.firePressed,
            };
            _decision.Tick(input, _ev);
            Process();
            if (_phase != Phase.Decision) return;
            _body.Face(feet);
            Quake(_decision.Quake);
            _hud.SetPrompt(Fill(_decision.Prompt), _decision.InReach && _decision.Lowered, _decision.Progress);
        }

        /// <summary>Hell shakes: the camera's trauma held at a level that grows with <paramref name="q"/>, and a rumble that
        /// comes more often.</summary>
        void Quake(float q)
        {
            float target = 0.12f + 0.33f * q;
            if (player.Shake.Trauma < target) player.Shake.AddTrauma(target - player.Shake.Trauma);
            if (Time.time >= _rumbleAt)
            {
                _rumbleAt = Time.time + Mathf.Lerp(3.2f, 0.9f, q);
                SkylineSounds.PlayAt(HellEndingCues.Rumble, player.transform.position + UnityEngine.Random.insideUnitSphere * 6f);
            }
        }

        void Process()
        {
            for (int i = 0; i < _ev.Count; i++)
            {
                var e = _ev[i];
                switch (e.kind)
                {
                    case HellDecisionEventKind.Line: Say(e.line); break;
                    case HellDecisionEventKind.HoldStarted: SkylineSounds.PlayAt(HellEndingCues.Hold, _kneelAt); break;
                    case HellDecisionEventKind.Killed: Chose(true); break;
                    case HellDecisionEventKind.Spared: Chose(false); break;
                }
            }
            _ev.Clear();
        }

        void Chose(bool killed)
        {
            _killed = killed;
            GiveZonesBack();
            _hud.SetPrompt("", false, 0f);
            HushVoice();
            var at = _body.Center;
            if (killed)
            {
                SkylineSounds.PlayAt(HellEndingCues.Kill, at);
                HellFx.SmokeBurst(at, 30, 1.2f);
                HellFx.SmokeBurst(_kneelAt + Vector3.up * 0.6f, 20, 1f);
                _body.SetVisible(false);
                ShowLantern(true, false);
                SkylineSounds.PlayAt(HellEndingCues.Soul, player.transform.position);
            }
            else
            {
                SkylineSounds.PlayAt(HellEndingCues.Spare, at);
                _body.SetPose(HellVesperBody.Pose.Stand);
                _body.SetHeart(HellVesperBody.HeartLook.Ember);
                _body.Face(player.transform.position);
            }
            Debug.Log("HELL_ENDING chose " + (killed ? "kill" : "spare") + " after " + _decision.Elapsed.ToString("0.0") + " s");
            _lines.Clear();
            foreach (var k in HellEndingRules.AfterChoice(killed)) _lines.Enqueue(k);
            _nextLineAt = Time.unscaledTime + 0.4f;
            _phase = Phase.AfterChoice;
            _phaseT = 0f;
        }

        // ------------------------------------------------------------------ the escape

        void StartEscape()
        {
            _room = _scripted ? null : FindEscapeRoom();
            if (_room == null)
            {
                if (!_scripted) Debug.LogWarning("HELL_ENDING no escape room (floor " + (HellEscapeRoute.Floor + 1) + " " + HellEscapeRoute.Kind + ") in the scene: straight to the ending");
                _phase = Phase.Out;
                _cards.Fade(1f, 0.4f);
                return;
            }
            SkylineSounds.PlayAt(HellEndingCues.Escape, player.transform.position);
            _cards.ClearText();
            _cards.Fade(1f, 0.35f);
            _phase = Phase.ToEscape;
        }

        static HellRoom FindEscapeRoom()
        {
            foreach (var r in FindObjectsByType<HellRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r.floor == HellEscapeRoute.Floor && r.kind == HellEscapeRoute.Kind) return r;
            return null;
        }

        void BeginEscape()
        {
            _room.gameObject.SetActive(true);
            _room.Begin(player);
            var d = _room.Def;
            player.Restart(U(d.entry), d.entryYaw);
            _soul = new HellSoulPickup(HellEscapeRoute.SoulPoints(d));
            MakeSoulViews();
            ResetEscapeState();
            HellRewindRegistry.Register(this);
            HellRunEvents.RaiseRewindBoundary();
            _escapeTries = 1;
            _phase = Phase.Escape;
            _phaseT = 0f;
            _cards.Fade(0f, 0.5f);
            _hud.Banner("RUN", 1.6f);
            Say(HellEndingRules.EscapeStart(_killed));
            Debug.Log("HELL_ENDING escape begins in " + d.name + (_killed ? " (her soul in your lantern)" : " (Vesper beside you)"));
        }

        /// <summary>The escape from its start: the soul back in place, the lantern as you left the arena, Vesper at your side.</summary>
        void ResetEscapeState()
        {
            _soul.Reset();
            SyncSoulViews();
            ShowLantern(_killed, false);
            var d = _room.Def;
            var fwd = U(HellEscapeRoute.Forward(d));
            if (!_killed)
            {
                var start = U(d.entry);
                _follower = new HellFollower(N(start), N(fwd), followerTuning);
                _follower.S.position = _follower.Place(N(start), N(fwd));
                _body.gameObject.SetActive(true);
                _body.SetVisible(true);
                _body.SetPose(HellVesperBody.Pose.Stand);
                _body.transform.position = U(_follower.S.position);
                _body.Face(_body.transform.position + fwd);
            }
            else _follower = null;
            _hud.SetLine(HellEndingLines.EscapeSoulLine);
            _lastFeet = player.transform.position;
            _skipSweep = true;
            RebaseFloorSounds();
        }

        void TickEscape(float dt)
        {
            var feet = player.transform.position;
            if (!_skipSweep && _soul.Tick(N(_lastFeet), N(feet))) SoulTaken();
            _skipSweep = false;
            _lastFeet = feet;
            if (_follower != null)
            {
                if (_follower.Tick(dt, N(feet), N(player.Velocity), out var from))
                {
                    HellFx.SmokeBurst(U(from) + Vector3.up, 12, 0.6f);
                    HellFx.SmokeBurst(U(_follower.S.position) + Vector3.up, 12, 0.6f);
                    SkylineSounds.PlayAt(HellEndingCues.Step, U(_follower.S.position));
                }
                _body.transform.position = U(_follower.S.position);
                _body.Face(_body.transform.position + U(_follower.S.facing));
            }
            float bob = Mathf.Sin(Time.time * 2.2f) * 0.15f;
            for (int i = 0; i < _soulViews.Count && i < _soul.Points.Length; i++)
                if (_soulViews[i] != null && _soulViews[i].activeSelf)
                {
                    _soulViews[i].transform.position = U(_soul.Points[i]) + Vector3.up * (HellEscapeRoute.SoulHeight + bob);
                    _soulViews[i].transform.Rotate(0f, 90f * dt, 0f);
                }
            if (player.Shake.Trauma < 0.1f) player.Shake.AddTrauma(0.1f - player.Shake.Trauma);
            TickFloorSounds(feet);
            if (_room.Objective != null && _room.Objective.Done) FinishEscape();
        }

        // the sound binder follows the run director's current room only, so the escape plays its own falling floor: a tile
        // that drops within 24 m of you (at most one every 0.2 s); re-based silently after a restart or a Rewind
        readonly List<bool> _solid = new List<bool>();
        float _fallSoundAt;

        void RebaseFloorSounds()
        {
            _solid.Clear();
            if (_room != null) foreach (var p in _room.Platforms) _solid.Add(p != null && p.Solid);
        }

        void TickFloorSounds(Vector3 feet)
        {
            var ps = _room.Platforms;
            if (_solid.Count != ps.Count) { RebaseFloorSounds(); return; }
            for (int i = 0; i < ps.Count; i++)
            {
                bool solid = ps[i] != null && ps[i].Solid;
                if (_solid[i] && !solid && Time.time >= _fallSoundAt && (ps[i].transform.position - feet).sqrMagnitude < 24f * 24f)
                {
                    _fallSoundAt = Time.time + 0.2f;
                    SkylineSounds.PlayAt(HellSoundCues.PlatformFall, ps[i].transform.position);
                }
                _solid[i] = solid;
            }
        }

        void SoulTaken()
        {
            SkylineSounds.PlayAt(HellEndingCues.Soul, player.transform.position);
            SyncSoulViews();
            ShowLantern(_killed, true);
            _hud.SetLine(HellEndingLines.EscapeLine);
            string bark = HellEndingRules.EscapeHalfway(_killed);
            if (bark != null) Say(bark);
            Debug.Log("HELL_ENDING your sister's soul is in the lantern");
        }

        void FinishEscape()
        {
            if (!_soul.Collected)
            {
                // never a dead end: a soul you somehow passed by follows you out
                _soul.Collected = true;
                SyncSoulViews();
                ShowLantern(_killed, true);
                Debug.Log("HELL_ENDING the soul followed you out (missed on the way)");
            }
            HellRewindRegistry.Unregister(this);
            if (HellHour.Current != null) HellHour.Current.Stop();     // Baja's hour stops at the exit (docs/HELL-HOUR.md)
            SkylineSounds.PlayAt(HellEndingCues.Out, player.transform.position);
            _hud.SetLine("");
            _cards.ClearText();
            _cards.Fade(1f, 0.8f);
            _phase = Phase.Out;
            Debug.Log("HELL_ENDING out of the Collapse after " + _escapeTries + " tr" + (_escapeTries == 1 ? "y" : "ies"));
        }

        /// <summary>Baja's hour: the exit gate shut before you reached it (<see cref="HellHourDirector"/>). The escape ends here,
        /// and the ending is damnation whatever you chose (docs/HELL-HOUR.md).</summary>
        public void Damn()
        {
            if (_phase != Phase.Escape) return;
            _damned = true;
            HellRewindRegistry.Unregister(this);
            if (HellHour.Current != null) HellHour.Current.Stop();
            _hud.SetLine("");
            _cards.ClearText();
            _cards.Fade(1f, 2.4f);
            _phase = Phase.Out;
            Debug.Log("HELL_ENDING the gate shut before you reached it (try " + _escapeTries + "): damned");
        }

        void MakeSoulViews()
        {
            foreach (var v in _soulViews) if (v != null) Destroy(v);
            _soulViews.Clear();
            var glow = HellLook.Glow(SisterSoul);
            foreach (var p in _soul.Points)
            {
                var root = new GameObject("Sister's Soul");
                root.transform.SetParent(transform, false);
                root.transform.position = U(p) + Vector3.up * HellEscapeRoute.SoulHeight;
                var core = SkylineBotLook.Visual(PrimitiveType.Sphere, "Core", root.transform, glow, false);
                core.transform.localScale = Vector3.one * 0.45f;
                var halo = SkylineBotLook.Visual(PrimitiveType.Sphere, "Halo", root.transform, HellLook.Glow(SisterSoul * 0.35f), false);
                halo.transform.localScale = new Vector3(0.8f, 1.1f, 0.8f);
                var l = root.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.85f, 0.6f);
                l.range = 9f;
                l.intensity = 2.5f;
                l.shadows = LightShadows.None;
                _soulViews.Add(root);
            }
        }

        void SyncSoulViews()
        {
            bool on = _soul != null && !_soul.Collected && (_phase == Phase.Escape || _phase == Phase.ToEscape);
            foreach (var v in _soulViews) if (v != null && v.activeSelf != on) v.SetActive(on);
        }

        /// <summary>The lantern in your hand: Vesper's soul (violet) and your sister's (gold), as carried.</summary>
        void ShowLantern(bool vesper, bool sister)
        {
            if ((vesper || sister) && _lantern == null) BuildLantern();
            if (_lantern == null) return;
            bool any = vesper || sister;
            if (_lantern.activeSelf != any) _lantern.SetActive(any);
            _lanternVesper.SetActive(vesper);
            _lanternSister.SetActive(sister);
            if (_lanternLight != null) _lanternLight.color = vesper && sister ? new Color(1f, 0.7f, 0.9f) : vesper ? new Color(0.6f, 0.35f, 1f) : new Color(1f, 0.85f, 0.6f);
        }

        void BuildLantern()
        {
            var cam = player.Camera != null ? player.Camera.transform : player.transform;
            _lantern = new GameObject("Ending Lantern");
            _lantern.transform.SetParent(cam, false);
            _lantern.transform.localPosition = lanternOffset;
            var iron = SkylineBotLook.Lit(new Color(0.06f, 0.05f, 0.06f));
            Part(PrimitiveType.Cube, "Base", new Vector3(0f, -0.09f, 0f), new Vector3(0.15f, 0.025f, 0.15f), iron);
            Part(PrimitiveType.Cube, "Top", new Vector3(0f, 0.09f, 0f), new Vector3(0.13f, 0.025f, 0.13f), iron);
            for (int i = 0; i < 4; i++)
                Part(PrimitiveType.Cube, "Post", new Vector3((i % 2 == 0 ? -1f : 1f) * 0.06f, 0f, (i < 2 ? -1f : 1f) * 0.06f), new Vector3(0.012f, 0.18f, 0.012f), iron);
            Part(PrimitiveType.Cylinder, "Ring", new Vector3(0f, 0.13f, 0f), new Vector3(0.05f, 0.01f, 0.05f), iron);
            _lanternVesper = Part(PrimitiveType.Sphere, "Vesper's Soul", new Vector3(-0.025f, 0f, 0f), Vector3.one * 0.07f, HellLook.Glow(VesperSoul));
            _lanternSister = Part(PrimitiveType.Sphere, "Sister's Soul", new Vector3(0.025f, 0.01f, 0f), Vector3.one * 0.075f, HellLook.Glow(SisterSoul));
            _lanternLight = _lantern.AddComponent<Light>();
            _lanternLight.type = LightType.Point;
            _lanternLight.range = 7f;
            _lanternLight.intensity = 1.6f;
            _lanternLight.shadows = LightShadows.None;
        }

        GameObject Part(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material m)
        {
            var go = SkylineBotLook.Visual(type, name, _lantern.transform, m, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            return go;
        }

        // ------------------------------------------------------------------ deaths, restarts, the Rewind

        void OnWrecked()
        {
            switch (_phase)
            {
                case Phase.Decision:
                case Phase.AfterChoice:
                case Phase.ToEscape:
                    player.Restart(_decisionSpot, _decisionYaw);
                    HellRunEvents.RaiseRewindBoundary();
                    Debug.Log("HELL_ENDING died while deciding: back where you stood");
                    break;
                case Phase.Escape:
                    _escapeTries++;
                    _room.Restart();
                    player.Restart(U(_room.Def.entry), _room.Def.entryYaw);
                    _hud.Banner("AGAIN", 1.5f);
                    Debug.Log("HELL_ENDING died in the escape: it starts again (try " + _escapeTries + ")");
                    HellRunEvents.RaiseRoomRestarted();   // the Rewind drops its history; OnRoomRestarted resets the soul and Vesper
                    break;
            }
        }

        /// <summary>The escape restarted (a death, or a sink: the Collapse restarts itself).</summary>
        void OnRoomRestarted()
        {
            if (_phase != Phase.Escape || _room == null) return;
            ResetEscapeState();
        }

        void OnRewound()
        {
            _skipSweep = true;
            if (_phase == Phase.Escape) RebaseFloorSounds();
        }

        public object Snapshot()
        {
            var s = _snaps.Next();
            s.soul = _soul != null && _soul.Collected;
            s.hasFollower = _follower != null;
            if (_follower != null) s.follower = _follower.S;
            return s;
        }

        public void Restore(object snapshot)
        {
            if (!(snapshot is Snap s) || _phase != Phase.Escape || _soul == null) return;
            _soul.Collected = s.soul;
            SyncSoulViews();
            ShowLantern(_killed, s.soul);
            _hud.SetLine(s.soul ? HellEndingLines.EscapeLine : HellEndingLines.EscapeSoulLine);
            if (_follower != null && s.hasFollower)
            {
                _follower.S = s.follower;
                _body.transform.position = U(_follower.S.position);
            }
            _skipSweep = true;
        }

        // ------------------------------------------------------------------ the endings

        void StartCards()
        {
            if (_room != null) _room.End();
            if (_follower != null) _body.SetVisible(false);
            ShowLantern(false, false);
            foreach (var v in _soulViews) if (v != null) v.SetActive(false);
            _hud.Clear();
            HushVoice();
            SkylineDeviceInput.Paused = true;        // the game holds still under the cards
            // Baja's hour: damnation if the gate shut; the Lucifer cliffhanger on top with every job done and time left at the exit
            var hour = HellHour.Current;
            if (hour != null) hour.Stop();
            _endingId = HellHourEndings.Pick(_killed, HellJobs.OfferedCount, HellJobs.DoneCount, HellJobs.AssumedErrands, _damned,
                hour != null ? hour.Left : float.PositiveInfinity);
            _cardShownAt = Time.unscaledTime;
            _phase = Phase.Cards;
            Debug.Log("HELL_ENDING ending " + _endingId + " (" + (_killed ? "killed" : "spared") + ", jobs " + HellJobs.DoneCount + "/" + HellJobs.OfferedCount
                + (HellJobs.AssumedErrands.HasValue ? ", errands assumed " + (HellJobs.AssumedErrands.Value ? "yes" : "no")
                    : HellJobs.Favoured ? ", favoured" : "") + ")");
            // the ending as a comic flip-through (docs/HELL-COMIC.md): Baja's lines in balloons, the epilogue as a caption,
            // the ending's name as a splash that fits the screen; then the credits, the stats, the title (Finish)
            HellComicPlayer.Play(HellComics.Ending(_endingId), Finish, false);
        }

        void TickCards()
        {
            // the comic player runs the ending's pages and calls Finish; this only guards a comic that went missing
            if (!HellComicPlayer.Playing && Time.unscaledTime - _cardShownAt > 1f && _phase == Phase.Cards)
            {
                Debug.LogWarning("HELL_ENDING no comic playing: on to the credits");
                Finish();
            }
        }

        void Finish()
        {
            if (_phase != Phase.Cards) return;
            _phase = Phase.Done;
            HushVoice();
            SkylineDeviceInput.Paused = false;
            Subscribe(false);
            _cards.ClearText();
            Debug.Log("HELL_ENDING done: " + _endingId);
            HellRunEvents.RaiseEnding(_endingId);    // the credits, the stats, the title
            _cards.Fade(0f, 1.5f);
        }

        /// <summary>Back to nothing (a new Begin).</summary>
        void Clean()
        {
            GiveZonesBack();
            HellRewindRegistry.Unregister(this);
            _lines.Clear();
            _hud.Clear();
            ShowLantern(false, false);
            foreach (var v in _soulViews) if (v != null) Destroy(v);
            _soulViews.Clear();
            _follower = null;
            _room = null;
            _soul = null;
            _damned = false;
            _phase = Phase.Idle;
        }

        // ------------------------------------------------------------------ lines and input

        /// <summary>Says a line: the subtitle and its voice (hell-voice/&lt;speaker&gt;/&lt;key&gt;) when there is one. How long it lasts.</summary>
        float Say(string key)
        {
            var line = HellEndingLines.Get(key);
            if (line == null) { Debug.LogWarning("HELL_ENDING no line " + key); return 0f; }
            float clip = PlayVoice(line);
            float seconds = HellEndingLines.Seconds(line, clip);
            if (SkylineHud.Instance != null) SkylineHud.Instance.Subtitles.Say(HellEndingLines.Display(line.speaker), line.text, false, seconds);
            Debug.Log("HELL_ENDING line " + key + (clip > 0f ? " (voiced)" : "") + ": " + line.text);
            return seconds;
        }

        /// <summary>Plays a line's voice (through the Voices volume, so the clean picture's voice switch obeys it); its length, or 0.</summary>
        float PlayVoice(HellEndingLine line)
        {
            if (line == null || !line.Voiced || _voice == null) return 0f;
            var clip = HellLinePlayer.LoadVoice(line.speaker, line.key);
            if (clip == null) return 0f;
            _voice.Stop();
            _voice.clip = clip;
            _voice.volume = SkylineMix.UserGain(SkylineSoundCategory.Voices) * SkylineMix.CategoryGain[(int)SkylineSoundCategory.Voices];
            _voice.Play();
            SkylineSounds.NoteSpeech(_voice, clip.length);
            return clip.length;
        }

        void HushVoice()
        {
            if (_voice != null) _voice.Stop();
        }

        static bool MouseOk => Application.isBatchMode || Cursor.lockState == CursorLockMode.Locked;

        /// <summary>Use: the Inspect binding (F, D-pad up), or B on a pad.</summary>
        static bool UseHeld()
        {
            bool m = MouseOk;
            if (SkylineBindings.Held(BindDevice.KeyboardMouse, BindAction.Inspect, m) || SkylineBindings.Held(BindDevice.Pad, BindAction.Inspect, m)) return true;
            var pad = Gamepad.current;
            return pad != null && pad.buttonEast.isPressed;
        }

        /// <summary>The holster: the Reload binding held.</summary>
        static bool HolsterHeld()
        {
            bool m = MouseOk;
            return SkylineBindings.Held(BindDevice.KeyboardMouse, BindAction.Reload, m) || SkylineBindings.Held(BindDevice.Pad, BindAction.Reload, m);
        }

        static bool SkipPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame)) return true;
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        string Fill(string template)
        {
            var device = HellPromptText.DeviceFor(_lastPad, _lastKbm);
            template = template.Replace("{Use}", device == BindDevice.Pad ? "[B]" : "{Inspect}");
            return HellPromptText.Fill(template, SkylineBindings.Table, device);
        }

        void TrackDevice()
        {
            double t = Time.unscaledTimeAsDouble;
            var pad = Gamepad.current;
            if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.16f || pad.rightStick.ReadValue().sqrMagnitude > 0.16f
                || pad.buttonSouth.isPressed || pad.buttonEast.isPressed || pad.buttonWest.isPressed || pad.rightTrigger.isPressed
                || pad.leftTrigger.isPressed)) _lastPad = t;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if ((kb != null && kb.anyKey.isPressed) || (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 4f || mouse.leftButton.isPressed)))
                _lastKbm = t;
        }

        static Vector3 U(NVec3 v) => new Vector3(v.X, v.Y, v.Z);
        static NVec3 N(Vector3 v) => new NVec3(v.x, v.y, v.z);
    }
}
