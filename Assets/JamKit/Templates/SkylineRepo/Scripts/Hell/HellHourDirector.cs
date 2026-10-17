using JamKit.Skyline;
using UnityEngine;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// BAJA'S HOUR in the game (docs/HELL-HOUR.md). It keeps the run's <see cref="HellHour"/> (made when a run starts, with the
    /// difficulty from <see cref="SkylineSettings"/> and the chapter's head start), ticks it on real, unscaled seconds of play
    /// only (<see cref="HellHourRules.Counts"/>: not under the flow's screens, the pause menu, the skill pick, a story beat's
    /// hold, or the ending's scripted lines, fades and cards), feeds the HUD's <see cref="HellHourglass"/>, has Baja remind you
    /// at thresholds (<c>hour.*</c> lines, voiced when <c>hell-voice/god/&lt;key&gt;.wav</c> exists), and runs the final escape's
    /// exit gate: each attempt gets a window from the hour left (<see cref="HellHourGate"/>), the doorway's two leaves grind
    /// shut over it, and if they shut first the ending director ends the run in damnation. The hour is not a rewindable: the
    /// Rewind never gives Baja's time back (and the first Rewind gets a remark). Attaches itself to any scene with a
    /// <see cref="HellRunDirector"/>. Logs <c>HELL_HOUR ...</c>.
    /// </summary>
    [AddComponentMenu("JamKit/Hell/Hour Director")]
    public sealed class HellHourDirector : MonoBehaviour
    {
        public static HellHourDirector Instance { get; private set; }

        /// <summary>Seconds of quiet after a line before the next reminder may speak.</summary>
        const float LineGap = 1.5f;

        HellRunDirector _run;
        HellRunStats _statsSeen;
        readonly HellHourglass _glass = new HellHourglass();
        AudioSource _voice;
        string _pending;
        float _quietUntil;
        bool _rewindSaid, _counted, _statsLogged;

        // the escape's gate
        bool _inEscape;
        float _window = float.PositiveInfinity, _t, _grindAt;
        GameObject _gate;
        Transform _leafA, _leafB;
        Light _gateLight;
        Renderer[] _oldGate;
        Vector3 _gateCentre, _gateAcross;
        float _gateWidth = 4f, _gateHeight = 4f;
        int _attempts;

        // ------------------------------------------------------------------ setup

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AttachOnLoad()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Attach();
        }

        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => Attach();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            HellHour.Current = null;
        }

        /// <summary>Adds the hour to a scene with a run director (once).</summary>
        public static void Attach()
        {
            if (Instance != null) return;
            var d = FindFirstObjectByType<HellRunDirector>();
            if (d == null) return;
            d.gameObject.AddComponent<HellHourDirector>();
        }

        void Awake()
        {
            Instance = this;
            _run = GetComponent<HellRunDirector>();
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.spatialBlend = 0f;
        }

        void OnEnable()
        {
            HellRewind.Rewound += OnRewound;
            HellRunEvents.RoomRestarted += OnRoomRestarted;
        }

        void OnDisable()
        {
            HellRewind.Rewound -= OnRewound;
            HellRunEvents.RoomRestarted -= OnRoomRestarted;
        }

        void OnDestroy()
        {
            _glass.Destroy();
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ the clock

        void Update()
        {
            if (_run == null) _run = FindFirstObjectByType<HellRunDirector>();
            if (!_glass.Built && SkylineHud.Instance != null && SkylineHud.Instance.Safe != null) _glass.Build(SkylineHud.Instance.Safe);
            var stage = HellRunEvents.Stage;
            EnsureHour(stage);
            var hour = HellHour.Current;
            bool inRun = hour != null && _statsSeen != null && _statsSeen == HellRunStats.Current
                && (stage == HellFlowStage.Playing || stage == HellFlowStage.Boss || stage == HellFlowStage.Ending);
            _counted = false;
            if (inRun)
            {
                hour.SetDifficulty(HellHourRules.FromInt(SkylineSettings.Options.difficulty));
                _counted = hour.Tick(Time.unscaledDeltaTime, Moment(stage));
                string key = hour.TakeReminder();
                if (key != null)
                {
                    _pending = key;
                    Debug.Log("HELL_HOUR reminder " + key + " (" + HellHour.Clock(hour.Left) + (hour.Overtime ? " owed)" : " left)"));
                }
                TrySay();
            }
            TickGate(hour);
            if (stage == HellFlowStage.Stats && hour != null && !_statsLogged)
            {
                _statsLogged = true;
                Debug.Log("HELL_HOUR stats: " + hour.Difficulty + ", " + HellHour.Clock(hour.Left) + (hour.Overtime ? " owed" : " left")
                    + ", ending " + (HellRunStats.Current != null ? HellRunStats.Current.EndingId : "?"));
            }
            _glass.SetVisible(inRun && !hour.Stopped);
            if (inRun) _glass.Set(hour.LeftShare, _counted, hour.Overtime);
        }

        void EnsureHour(HellFlowStage stage)
        {
            var stats = HellRunStats.Current;
            if (stats == null || stats == _statsSeen || stage == HellFlowStage.Title || stage == HellFlowStage.Quit) return;
            _statsSeen = stats;
            int chapter = _run != null && _run.Flow != null ? _run.Flow.Chapter : HellFlow.FullGame;
            var d = HellHourRules.FromInt(SkylineSettings.Options.difficulty);
            HellHour.Current = new HellHour(d, chapter);
            _rewindSaid = false;
            _statsLogged = false;
            _pending = null;
            _inEscape = false;
            _window = float.PositiveInfinity;
            Debug.Log("HELL_HOUR start: " + d + " (" + HellHourRules.BudgetMinutes(d) + " min), chapter " + chapter + ", "
                + HellHour.Clock(HellHour.Current.Left) + " left");
        }

        HellHourMoment Moment(HellFlowStage stage)
        {
            var run = _run != null ? _run.Run : null;
            return new HellHourMoment
            {
                stage = stage,
                paused = SkylineDeviceInput.Paused,
                menu = SkylinePauseMenu.IsOpen,
                screen = _run != null && _run.Screens != null && _run.Screens.Visible,
                pick = HellSkillPick.Open || (run != null && run.Phase == HellRunPhase.AwaitingPick),
                beatHold = HellTime.Has(HellBeats.HoldTimeKey),
                cutscene = EndingCutscene(),
                timeScale = Time.timeScale,
            };
        }

        /// <summary>The ending's scripted stretches (after the choice, the fades, the cards): not the decision, not the escape.</summary>
        static bool EndingCutscene()
        {
            var e = HellEndingDirector.Instance;
            if (e == null || !HellEndingDirector.Active) return false;
            return e.Stage != HellEndingDirector.Phase.Decision && e.Stage != HellEndingDirector.Phase.Escape;
        }

        void OnRewound()
        {
            var hour = HellHour.Current;
            // nothing to restore: the hour isn't a rewindable. Baja notices the first time.
            if (hour == null || _rewindSaid || HellRunEvents.Stage != HellFlowStage.Playing) return;
            _rewindSaid = true;
            if (_pending == null) _pending = "hour.rewind";
            Debug.Log("HELL_HOUR a Rewind: the hour keeps " + HellHour.Clock(hour.Left) + " (not given back)");
        }

        // ------------------------------------------------------------------ Baja's lines

        void TrySay()
        {
            if (_pending == null || !_counted || Time.unscaledTime < _quietUntil) return;
            if (HellBeats.Playing || HellEndingDirector.Active) return;
            string key = _pending;
            _pending = null;
            _quietUntil = Time.unscaledTime + Say(key) + LineGap;
        }

        /// <summary>A line of Baja's: the subtitle, and her voice when the file exists. How long it lasts.</summary>
        float Say(string key)
        {
            var line = HellHourLines.Get(key);
            if (line == null) return 0f;
            var clip = HellLinePlayer.LoadVoice(line.speaker, line.key);
            float seconds = HellLines.Seconds(line.text, clip != null ? clip.length : 0f);
            if (clip != null && _voice != null)
            {
                _voice.Stop();
                _voice.clip = clip;
                _voice.volume = SkylineMix.UserGain(SkylineSoundCategory.Voices) * SkylineMix.CategoryGain[(int)SkylineSoundCategory.Voices];
                _voice.Play();
                SkylineSounds.NoteSpeech(_voice, clip.length);
            }
            if (SkylineHud.Instance != null) SkylineHud.Instance.Subtitles.Say(HellSpeakers.Display(line.speaker), line.text, false, seconds);
            Debug.Log("HELL_HOUR line " + key + (clip != null ? " (voiced)" : ""));
            return seconds;
        }

        // ------------------------------------------------------------------ the escape's gate

        void TickGate(HellHour hour)
        {
            var e = HellEndingDirector.Instance;
            bool escaping = e != null && e.Stage == HellEndingDirector.Phase.Escape;
            if (escaping && !_inEscape)
            {
                _inEscape = true;
                _attempts = 0;
                BuildGate();
                StartAttempt();
            }
            else if (!escaping && _inEscape)
            {
                _inEscape = false;
                Debug.Log("HELL_HOUR escape over: " + (hour != null ? HellHour.Clock(hour.Left) + (hour.Overtime ? " owed" : " left") : "no hour"));
            }
            if (!escaping || _gate == null) return;
            if (_counted || hour == null) _t += Time.unscaledDeltaTime;
            float o = HellHourGate.Openness(_window, _t);
            PoseGate(o);
            var p = e.player != null ? e.player.transform.position : _gateCentre;
            if (o > 0f && o < 1f && Time.unscaledTime >= _grindAt)
            {
                _grindAt = Time.unscaledTime + Mathf.Lerp(0.35f, 1.1f, o);
                SkylineSounds.PlayAt(HellSoundCues.WellGrind, _gateCentre + Vector3.up * 2f);
            }
            if (HellHourGate.Shut(_window, _t))
            {
                SkylineSounds.PlayAt(HellSoundCues.PlatformFall, _gateCentre + Vector3.up * 2f);
                if (_voice != null) _voice.Stop();
                Say("hour.gate-shut");
                Debug.Log("HELL_HOUR the gate shut after " + _t.ToString("0.0") + " s of a " + _window.ToString("0.0") + " s window ("
                    + (p - _gateCentre).magnitude.ToString("0") + " m short): damnation");
                if (hour != null) hour.Stop();
                e.Damn();
            }
        }

        /// <summary>A new attempt (the escape began, or restarted after a death or a sink): the window from the hour left now.</summary>
        void StartAttempt()
        {
            var hour = HellHour.Current;
            float left = hour != null ? hour.Left : float.PositiveInfinity;
            _window = HellHourGate.Window(left);
            _t = 0f;
            _attempts++;
            Debug.Log("HELL_HOUR escape attempt " + _attempts + ": " + (hour != null ? HellHour.Clock(left) + (left < 0f ? " owed" : " left") : "no hour")
                + ", the gate " + HellHourGate.Describe(left) + " (shuts in " + (float.IsInfinity(_window) ? "never" : _window.ToString("0.0") + " s") + ")");
        }

        void OnRoomRestarted()
        {
            if (_inEscape) StartAttempt();
        }

        /// <summary>Baja's door in the Collapse's exit doorway: the room's own red gate is hidden (its collider stays) and two
        /// dark leaves with ember edges slide in from the sides as the window runs out.</summary>
        void BuildGate()
        {
            if (_gate != null) return;
            HellRoom room = null;
            foreach (var r in FindObjectsByType<HellRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r.floor == HellEscapeRoute.Floor && r.kind == HellEscapeRoute.Kind) { room = r; break; }
            var def = room != null ? room.Def : HellEscapeRoute.Room();
            var fwd = U(HellEscapeRoute.Forward(def));
            fwd.y = 0f;
            fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
            _gateAcross = Vector3.Cross(Vector3.up, fwd).normalized;
            _gateCentre = U(def.exit);
            var old = room != null ? HellRoom.Find(room.transform, "exit-gate") : null;
            if (old != null)
            {
                Bounds b = default(Bounds);
                bool any = false;
                foreach (var c in old.GetComponentsInChildren<Collider>(true)) { if (!any) b = c.bounds; else b.Encapsulate(c.bounds); any = true; }
                foreach (var rr in old.GetComponentsInChildren<Renderer>(true)) { if (!any) b = rr.bounds; else b.Encapsulate(rr.bounds); any = true; }
                if (any)
                {
                    _gateCentre = new Vector3(b.center.x, b.min.y, b.center.z);
                    _gateWidth = Mathf.Max(2f, Mathf.Abs(Vector3.Dot(b.size, _gateAcross)));
                    _gateHeight = Mathf.Max(2f, b.size.y);
                }
                _oldGate = old.GetComponentsInChildren<Renderer>(true);
                foreach (var rr in _oldGate) rr.enabled = false;
            }
            _gate = new GameObject("Baja's Gate");
            _gate.transform.SetParent(transform, false);
            _gate.transform.position = _gateCentre - fwd * 0.15f;
            _gate.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            var iron = HellLook.Lit(new Color(0.07f, 0.05f, 0.05f));
            var ember = HellLook.Glow(new Color(3.2f, 0.9f, 0.25f));
            _leafA = Leaf("Leaf A", iron, ember, -1f);
            _leafB = Leaf("Leaf B", iron, ember, 1f);
            _gateLight = _gate.AddComponent<Light>();
            _gateLight.type = LightType.Point;
            _gateLight.color = new Color(1f, 0.4f, 0.15f);
            _gateLight.range = 10f;
            _gateLight.shadows = LightShadows.None;
            _gateLight.transform.localPosition = Vector3.up * (_gateHeight * 0.6f);
            PoseGate(1f);
            Debug.Log("HELL_HOUR Baja's gate at " + _gateCentre + " (" + _gateWidth.ToString("0.0") + " m wide)");
        }

        Transform Leaf(string name, Material iron, Material ember, float side)
        {
            var root = new GameObject(name).transform;
            root.SetParent(_gate.transform, false);
            float w = _gateWidth * 0.5f;
            var slab = SkylineBotLook.Visual(PrimitiveType.Cube, "Slab", root, iron, true);
            slab.transform.localScale = new Vector3(w, _gateHeight, 0.25f);
            slab.transform.localPosition = new Vector3(side * w * 0.5f, _gateHeight * 0.5f, 0f);
            var edge = SkylineBotLook.Visual(PrimitiveType.Cube, "Ember Edge", root, ember, false);
            edge.transform.localScale = new Vector3(0.08f, _gateHeight * 0.96f, 0.3f);
            edge.transform.localPosition = new Vector3(0f, _gateHeight * 0.5f, 0f);
            return root;
        }

        /// <summary>Openness 1: the leaves are in the walls; 0: they meet in the middle.</summary>
        void PoseGate(float open)
        {
            if (_gate == null) return;
            float g = _gateWidth * 0.5f * open;
            _leafA.localPosition = new Vector3(-g, 0f, 0f);
            _leafB.localPosition = new Vector3(g, 0f, 0f);
            bool show = open < 0.999f;
            if (_leafA.gameObject.activeSelf != show) { _leafA.gameObject.SetActive(show); _leafB.gameObject.SetActive(show); }
            if (_gateLight != null) _gateLight.intensity = Mathf.Lerp(4f, 0.6f, open);
        }

        static Vector3 U(NVec3 v) => new Vector3(v.X, v.Y, v.Z);
    }
}
