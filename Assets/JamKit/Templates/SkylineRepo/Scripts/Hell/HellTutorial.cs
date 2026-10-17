using System;
using System.Reflection;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.InputSystem;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The God of Death's first-time prompts (docs/HELL-BEATS.md, "Tutorials"): she gave you the powers, so she teaches
    /// them. Each prompt is one spoken line (subtitled; voiced when a clip exists) plus a short hint card under the crosshair
    /// with the control for the device in use (keyboard and mouse or pad, read from the bindings). Prompts that can be done
    /// ask for it and end when the player does it, with a reaction line. Each shows once, remembered between runs
    /// (PlayerPrefs) until <see cref="ReplayAll"/>; <see cref="SetEnabled"/> turns them off. The queue and its rules are pure
    /// (<see cref="HellTutorialQueue"/>); this class watches the game and presents.
    /// <para>Triggers: Shadow Step and the two paths in the first room (a dark zone near the entry is marked); the room's
    /// objective on the first room of each kind; Rewind on a near-death hit or the first death; Time Dilation the first time
    /// you run fast; lava when it's within a few metres; collapsing stone when you're on or about to step onto it; the skill
    /// pick on the first floor cleared; the first job offered. Nothing starts while a story beat speaks or the game is
    /// paused.</para>
    /// <para>TODO(lane flow): a pause-menu row "Tutorials: On / Off / Replay" calling <see cref="SetEnabled"/> and
    /// <see cref="ReplayAll"/> (flow owns SkylinePauseMenu). Until then: <c>-hell-tutorials off|replay</c>.</para>
    /// </summary>
    public sealed class HellTutorial : MonoBehaviour
    {
        public const string SeenKey = "hell.tutorial.seen", OnKey = "hell.tutorial.on";
        public const float NearDeathShare = 0.45f, FastSpeed = 8f, FastSeconds = 0.6f, LavaWarnMetres = 3.5f;
        const float PollEvery = 0.2f;

        public static HellTutorial Instance { get; private set; }
        public HellTutorialQueue Queue { get; } = new HellTutorialQueue();

        HellRunDirector _director;
        HellAbilities _abilities;
        SkylinePlayer _player;
        GameObject _darkMarker;
        BindDevice _device = BindDevice.KeyboardMouse;
        double _lastPad = -1, _lastKbm = 0;
        float _fastFor, _poll;
        string _hintFor;
        PropertyInfo _jobsOffered;
        bool _jobsLooked;

        // ------------------------------------------------------------------ settings

        public static bool Enabled
        {
            get { try { return PlayerPrefs.GetInt(OnKey, 1) != 0; } catch (Exception) { return true; } }
        }

        /// <summary>The settings toggle: tutorials on or off (saved).</summary>
        public static void SetEnabled(bool on)
        {
            PlayerPrefs.SetInt(OnKey, on ? 1 : 0);
            PlayerPrefs.Save();
            if (Instance != null)
            {
                Instance.Queue.Enabled = on;
                if (!on) Instance.TakeDown();
            }
        }

        /// <summary>The settings button: every prompt shows again.</summary>
        public static void ReplayAll()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            PlayerPrefs.Save();
            if (Instance != null) Instance.Queue.ReplayAll();
            Debug.Log("HELL_TUTORIAL replay all");
        }

        // ------------------------------------------------------------------ setup

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AttachOnLoad()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Attach();
        }

        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => Attach();

        public static void Attach()
        {
            if (Instance != null) return;
            var d = FindFirstObjectByType<HellRunDirector>();
            if (d == null) return;
            d.gameObject.AddComponent<HellTutorial>();
        }

        void Awake()
        {
            Instance = this;
            _director = GetComponent<HellRunDirector>();
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-hell-tutorials")
                {
                    if (args[i + 1] == "off") SetEnabled(false);
                    else if (args[i + 1] == "replay") { SetEnabled(true); ReplayAll(); }
                    else if (args[i + 1] == "on") SetEnabled(true);
                }
            Queue.Enabled = Enabled;
            try { Queue.LoadSeen(PlayerPrefs.GetString(SeenKey, "")); } catch (Exception) { }
        }

        void OnEnable()
        {
            HellRunEvents.RoomEntered += OnRoomEntered;
            HellRunEvents.RoomRestarted += OnRoomRestarted;
            HellRunEvents.FloorCleared += OnFloorCleared;
            HellRewind.Rewound += OnRewound;
        }

        void OnDisable()
        {
            HellRunEvents.RoomEntered -= OnRoomEntered;
            HellRunEvents.RoomRestarted -= OnRoomRestarted;
            HellRunEvents.FloorCleared -= OnFloorCleared;
            HellRewind.Rewound -= OnRewound;
            Bind(null);
            TakeDown();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Bind(HellAbilities a)
        {
            if (_abilities == a) return;
            if (_abilities != null) _abilities.Stepped -= OnStepped;
            if (_player != null) _player.AttackTaken -= OnAttackTaken;
            _abilities = a;
            _player = a != null ? a.Player : null;
            if (_abilities != null) _abilities.Stepped += OnStepped;
            if (_player != null) _player.AttackTaken += OnAttackTaken;
        }

        // ------------------------------------------------------------------ triggers

        static float Now => Time.unscaledTime;

        void OnRoomEntered(int floor, int room, string kind)
        {
            Queue.Request(HellTutorials.ShadowStep, Now, 1.2f);
            Queue.Request(HellTutorials.Objective(kind), Now, 2.5f);
            Queue.Request(HellTutorials.Paths, Now, 4f);
        }

        void OnRoomRestarted() => Queue.Request(HellTutorials.Rewind, Now, 1f);

        void OnFloorCleared(int floor)
        {
            TakeDown();
            Queue.Request(HellTutorials.SkillPick, Now, 0.3f);
        }

        void OnAttackTaken(float damage, string attacker, Vector3 from)
        {
            if (_player == null || _player.Armor == null) return;
            var a = _player.Armor;
            float max = a.PlatingMax + a.ChassisMax;
            if (max > 0f && (a.Plating + a.Chassis) / max < NearDeathShare) Queue.Request(HellTutorials.Rewind, Now);
        }

        void OnStepped(Vector3 from, Vector3 to) => Done(HellTutorials.ShadowStep);
        void OnRewound() => Done(HellTutorials.Rewind);

        void Done(string id)
        {
            bool showing = Queue.Current != null && Queue.Current.id == id;
            if (!showing && Queue.Seen(id)) return;   // nothing to do (no save every frame while dilating)
            bool wasShowing = Queue.Complete(id, Now);
            Save();
            if (!wasShowing) return;
            HideCard();
            var def = HellTutorials.Get(id);
            if (def != null && HellLines.Has(def.DoneKey)) HellLinePlayer.Instance.Say(def.DoneKey);
        }

        void Poll()
        {
            var room = _director != null ? _director.Current : null;
            if (_player == null || room == null || !room.Running) return;
            Vector3 feet = _player.transform.position;
            // lava within a few metres (or underfoot)
            if (!Queue.Seen(HellTutorials.Lava) && NearLava(feet)) Queue.Request(HellTutorials.Lava, Now);
            // on, or about to step onto, collapsing stone
            if (!Queue.Seen(HellTutorials.Collapse) && room.Platforms != null)
            {
                var sim = _player.Movement != null ? _player.Movement.Sim : null;
                bool grounded = sim != null && sim.Grounded;
                var v = _player.Velocity;
                NVec3 now = HellRoom.N(feet), ahead = HellRoom.N(feet + new Vector3(v.x, 0f, v.z) * 0.4f);
                foreach (var p in room.Platforms)
                    if (p != null && (p.Under(now, grounded) || p.Under(ahead, true))) { Queue.Request(HellTutorials.Collapse, Now); break; }
            }
            // the first job offered
            if (!Queue.Seen(HellTutorials.Job) && JobsOffered() > 0) Queue.Request(HellTutorials.Job, Now, 1.5f);
        }

        static readonly Vector3[] Around =
        {
            new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1),
            new Vector3(0.71f, 0, 0.71f), new Vector3(-0.71f, 0, 0.71f), new Vector3(0.71f, 0, -0.71f), new Vector3(-0.71f, 0, -0.71f),
        };

        static bool NearLava(Vector3 feet)
        {
            if (HellLava.IsLava(feet)) return true;
            foreach (var d in Around)
                if (HellLava.IsLava(feet + d * (LavaWarnMetres * 0.5f) + Vector3.down * 0.3f) || HellLava.IsLava(feet + d * LavaWarnMetres + Vector3.down * 0.3f))
                    return true;
            return false;
        }

        /// <summary>TODO(jobs lane): <c>HellJobs.OfferedCount</c> is on hell-jobs, not main; read by reflection (0 without it).
        /// Once merged, subscribe to <c>HellJobs.Offered</c>.</summary>
        int JobsOffered()
        {
            if (!_jobsLooked)
            {
                _jobsLooked = true;
                var t = typeof(HellRunEvents).Assembly.GetType("JamKit.SkylineRepo.HellJobs");
                _jobsOffered = t != null ? t.GetProperty("OfferedCount", BindingFlags.Public | BindingFlags.Static) : null;
            }
            if (_jobsOffered == null) return 0;
            try { return (int)_jobsOffered.GetValue(null); }
            catch (Exception) { _jobsOffered = null; return 0; }
        }

        // ------------------------------------------------------------------ the frame

        void Update()
        {
            if (_abilities != HellAbilities.Instance) Bind(HellAbilities.Instance);
            TrackDevice();

            // fast for a moment: Time Dilation; dilation running: done
            if (_player != null)
            {
                var v = _player.Velocity;
                float speed = new Vector2(v.x, v.z).magnitude;
                _fastFor = speed >= FastSpeed ? _fastFor + Time.unscaledDeltaTime : 0f;
                if (_fastFor >= FastSeconds && !Queue.Seen(HellTutorials.Dilation)) Queue.Request(HellTutorials.Dilation, Now);
                if (_abilities != null && _abilities.Dilation.Scale < 0.95f) Done(HellTutorials.Dilation);
            }

            _poll -= Time.unscaledDeltaTime;
            if (_poll <= 0f) { _poll = PollEvery; Poll(); }

            bool paused = SkylineDeviceInput.Paused || SkylinePauseMenu.IsOpen;
            var was = Queue.Current;
            var started = Queue.Tick(Now, HellBeats.Playing || paused);
            if (was != null && Queue.Current == null && started == null) HideCard();
            if (started != null) Show(started);
            if (Queue.Current != null && _hintFor != null) RefreshHint(false);
        }

        void Show(HellTutorialDef def)
        {
            Save();
            Debug.Log("HELL_TUTORIAL " + def.id);
            HellLinePlayer.Instance.Say(def.LineKey);
            _hintFor = def.HintKey;
            RefreshHint(true);
            if (def.id == HellTutorials.ShadowStep) MarkDarkZone();
        }

        void RefreshHint(bool force)
        {
            var d = HellPromptText.DeviceFor(_lastPad, _lastKbm);
            if (!force && d == _device) return;
            _device = d;
            var line = HellLines.Get(_hintFor);
            if (line != null) HellLinePlayer.Instance.ShowHint(HellPromptText.Fill(line.text, SkylineBindings.Table, _device));
        }

        void HideCard()
        {
            _hintFor = null;
            if (HellLinePlayer.Instance != null) HellLinePlayer.Instance.HideHint();
            if (_darkMarker != null) { Destroy(_darkMarker); _darkMarker = null; }
        }

        void TakeDown()
        {
            Queue.Dismiss(Now);
            HideCard();
        }

        void Save()
        {
            try { PlayerPrefs.SetString(SeenKey, Queue.SaveSeen()); } catch (Exception) { }
        }

        /// <summary>"Step into that shadow": the Shadow Step's own landing marker, on the dark zone nearest the entry.</summary>
        void MarkDarkZone()
        {
            var room = _director != null ? _director.Current : null;
            if (room == null || room.Def == null || room.Def.darkZones.Count == 0) return;
            int i = HellTutorials.PickDarkZone(room.Def.entry, room.Def.darkZones);
            if (i < 0) return;
            var z = room.Def.darkZones[i];
            var c = (z.min + z.max) * 0.5f;
            Vector3 top = new Vector3(c.X, z.max.Y, c.Z), at = new Vector3(c.X, z.min.Y + 0.05f, c.Z);
            if (Physics.Raycast(top + Vector3.up * 0.5f, Vector3.down, out var hit, z.max.Y - z.min.Y + 1f, ~0, QueryTriggerInteraction.Ignore)) at = hit.point + Vector3.up * 0.03f;
            if (_darkMarker != null) Destroy(_darkMarker);
            _darkMarker = HellFx.MakeMarker(null);
            _darkMarker.name = "HellTutorialDarkMarker";
            _darkMarker.transform.position = at;
            _darkMarker.transform.localScale = new Vector3(1.6f, 2.2f, 1.6f);
            HellFx.TintMarker(_darkMarker, true);
            _darkMarker.SetActive(true);
        }

        /// <summary>Which device the player used last (a pad's buttons or a stick off centre; any key, mouse button or mouse
        /// movement).</summary>
        void TrackDevice()
        {
            double t = Time.unscaledTimeAsDouble;
            var pad = Gamepad.current;
            if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.16f || pad.rightStick.ReadValue().sqrMagnitude > 0.16f
                || pad.buttonSouth.isPressed || pad.rightTrigger.isPressed || pad.leftTrigger.isPressed || pad.rightShoulder.isPressed
                || pad.leftShoulder.isPressed || pad.startButton.isPressed)) _lastPad = t;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if ((kb != null && kb.anyKey.isPressed) || (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 4f || mouse.leftButton.isPressed || mouse.rightButton.isPressed)))
                _lastKbm = t;
        }
    }
}
