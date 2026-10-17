using System;
using System.Collections.Generic;
using System.Reflection;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Plays the story beats (docs/HELL-BEATS.md). The beats are data (<see cref="HellBeatSets"/>, the set named by
    /// <c>-hell-beats &lt;name&gt;</c> or <see cref="HellBeatSets.ActiveName"/>); the rules (<see cref="HellBeatRules"/>)
    /// pick one when something happens. This class only reports what happens and presents the beat: its lines (subtitles,
    /// voice when a clip exists), its staging (a figure seen at a distance) and, for a held beat, a letterbox, the player's
    /// input paused and the world slowed for at most <see cref="HellBeatRules.MaxHoldSeconds"/>.
    /// <para>It listens to <see cref="HellRunEvents"/> (RunStarted, RoomEntered, RoomRestarted, RunFinished; and
    /// FinalBossRequested and the job count by reflection while lane flow's and the jobs lane's files are not merged) and
    /// watches the run director (the shaft, the room's clock, its objective, the lantern, the darkness). It edits none of
    /// them. Attaches itself to any scene with a <see cref="HellRunDirector"/>.</para>
    /// </summary>
    public sealed class HellBeats : MonoBehaviour
    {
        public const string HoldTimeKey = "beat";
        const float PollEvery = 0.2f;

        /// <summary>A beat is speaking now (the tutorials wait).</summary>
        public static bool Playing { get; private set; }
        public static HellBeats Instance { get; private set; }

        public HellBeatRules Rules { get; private set; }

        HellRunDirector _director;
        readonly Queue<HellBeatDef> _waiting = new Queue<HellBeatDef>();
        HellBeatDef _current;
        int _line;
        float _lineEnds, _holdEnds, _figureEnds, _poll;
        bool _holding;
        Func<SkylineInputFrame> _heldInput;
        SkylinePlayer _heldPlayer;
        GameObject _figure;

        // watched state
        HellRunPhase _phase;
        bool _carrying, _objectiveDone, _inDark;
        float _progress;
        int _jobsDone;
        HellRoom _room;
        PropertyInfo _jobsDoneProp;
        bool _jobsLooked;
        Delegate _bossHandler;
        EventInfo _bossEvent;

        // ------------------------------------------------------------------ setup

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AttachOnLoad()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Attach();
        }

        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => Attach();

        /// <summary>Adds the beats to a scene with a run director (once).</summary>
        public static void Attach()
        {
            if (Instance != null) return;
            var d = FindFirstObjectByType<HellRunDirector>();
            if (d == null) return;
            d.gameObject.AddComponent<HellBeats>();
        }

        void Awake()
        {
            Instance = this;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-hell-beats") HellBeatSets.ActiveName = args[i + 1];
            Rules = new HellBeatRules(HellBeatSets.Active);
            _director = GetComponent<HellRunDirector>();
            Debug.Log("HELL_BEATS set " + HellBeatSets.ActiveName + " (" + Rules.Defs.Count + " beats)");
        }

        void OnEnable()
        {
            HellRunEvents.RunStarted += OnRunStarted;
            HellRunEvents.RoomEntered += OnRoomEntered;
            HellRunEvents.RoomRestarted += OnRoomRestarted;
            HellRunEvents.RunFinished += OnRunFinished;
            SubscribeBoss(true);
        }

        void OnDisable()
        {
            HellRunEvents.RunStarted -= OnRunStarted;
            HellRunEvents.RoomEntered -= OnRoomEntered;
            HellRunEvents.RoomRestarted -= OnRoomRestarted;
            HellRunEvents.RunFinished -= OnRunFinished;
            SubscribeBoss(false);
            EndHold();
            ClearFigure();
            Playing = false;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>TODO(lane flow): <c>HellRunEvents.FinalBossRequested</c> (an Action) is on hell-flow, not main yet. Bound
        /// by reflection so this compiles either way; once merged this can be a plain <c>+=</c>.</summary>
        void SubscribeBoss(bool on)
        {
            try
            {
                if (on)
                {
                    _bossEvent = typeof(HellRunEvents).GetEvent("FinalBossRequested", BindingFlags.Public | BindingFlags.Static);
                    if (_bossEvent == null || _bossEvent.EventHandlerType != typeof(Action)) { _bossEvent = null; return; }
                    _bossHandler = (Action)OnBossRequested;
                    _bossEvent.AddEventHandler(null, _bossHandler);
                }
                else if (_bossEvent != null && _bossHandler != null) _bossEvent.RemoveEventHandler(null, _bossHandler);
            }
            catch (Exception e) { Debug.LogWarning("HELL_BEATS FinalBossRequested not bound: " + e.Message); _bossEvent = null; }
        }

        // ------------------------------------------------------------------ what happens

        HellRun Run => _director != null ? _director.Run : null;
        int Floor => Run != null ? Run.Floor : -1;
        int RoomIndex => Run != null ? Run.Room : -1;
        string Kind => Run != null && Run.Phase == HellRunPhase.InRoom ? Run.CurrentKind.ToString() : null;

        void Report(HellBeatTrigger t, float value = 0f, int count = 0)
        {
            if (Rules == null) return;
            var b = Rules.Report(HellBeatEvent.Of(t, Floor, RoomIndex, Kind, value, count));
            if (b == null) return;
            Debug.Log("HELL_BEAT " + b.id + " on " + t + " (floor " + (Floor + 1) + ")");
            _waiting.Enqueue(b);
        }

        void OnRunStarted()
        {
            Rules.RunStarted();
            _waiting.Clear();
            _phase = HellRunPhase.NotStarted;
        }

        void OnRoomEntered(int floor, int room, string kind)
        {
            _room = _director != null ? _director.Current : null;
            ResetRoomWatch();
            if (Rules != null)
            {
                var b = Rules.Report(HellBeatEvent.Of(HellBeatTrigger.RoomEntered, floor, room, kind));
                if (b != null) { Debug.Log("HELL_BEAT " + b.id + " on RoomEntered"); _waiting.Enqueue(b); }
            }
        }

        void OnRoomRestarted() => ResetRoomWatch();

        void ResetRoomWatch()
        {
            _carrying = false;
            _objectiveDone = false;
            _progress = 0f;
            _inDark = false;
        }

        void OnRunFinished() => Report(HellBeatTrigger.RunFinished);
        void OnBossRequested() => Report(HellBeatTrigger.BeforeBoss);

        void Poll()
        {
            var run = Run;
            if (run != null && run.Phase != _phase)
            {
                if (run.Phase == HellRunPhase.Shaft) Report(HellBeatTrigger.ShaftEntered);
                _phase = run.Phase;
            }
            var room = _director != null ? _director.Current : null;
            if (run != null && run.Phase == HellRunPhase.InRoom && room != null && room.Running)
            {
                Report(HellBeatTrigger.RoomTimer, room.Clock);
                var obj = room.Objective;
                if (obj != null)
                {
                    if (obj.Progress > _progress + 1e-4f) { _progress = obj.Progress; Report(HellBeatTrigger.ObjectiveProgress, _progress); }
                    if (obj.Done && !_objectiveDone) { _objectiveDone = true; Report(HellBeatTrigger.ObjectiveDone); }
                }
                bool carrying = HellRoomFlags.CarryingLantern;
                if (carrying && !_carrying) Report(HellBeatTrigger.LanternTaken);
                _carrying = carrying;
                var p = room.Player;
                if (p != null)
                {
                    bool dark = HellDarkness.IsDark(p.transform.position + Vector3.up * 0.5f);
                    if (dark && !_inDark) Report(HellBeatTrigger.DarkZoneEntered);
                    _inDark = dark;
                }
            }
            int jobs = JobsDone();
            if (jobs > _jobsDone) Report(HellBeatTrigger.JobDone, 0f, jobs);
            _jobsDone = jobs;
        }

        /// <summary>TODO(jobs lane): <c>HellJobs.DoneCount</c> is on hell-jobs, not main yet; read by reflection (0 without it).
        /// Once merged, subscribe to <c>HellJobs.Done</c> instead.</summary>
        int JobsDone()
        {
            if (!_jobsLooked)
            {
                _jobsLooked = true;
                var t = typeof(HellRunEvents).Assembly.GetType("JamKit.SkylineRepo.HellJobs");
                _jobsDoneProp = t != null ? t.GetProperty("DoneCount", BindingFlags.Public | BindingFlags.Static) : null;
            }
            if (_jobsDoneProp == null) return 0;
            try { return (int)_jobsDoneProp.GetValue(null); }
            catch (Exception) { _jobsDoneProp = null; return 0; }
        }

        // ------------------------------------------------------------------ presenting

        void Update()
        {
            _poll -= Time.unscaledDeltaTime;
            if (_poll <= 0f)
            {
                _poll = PollEvery;
                Poll();
            }
            float now = Time.unscaledTime;
            if (_current == null && _waiting.Count > 0) Begin(_waiting.Dequeue(), now);
            if (_current == null) return;
            if (_holding && now >= _holdEnds) EndHold();
            if (_figure != null && now >= _figureEnds) ClearFigure();
            if (now >= _lineEnds)
            {
                _line++;
                if (_line < _current.lines.Length) _lineEnds = now + HellLinePlayer.Instance.Say(_current.lines[_line]) + 0.25f;
                else Finish();
            }
        }

        void Begin(HellBeatDef b, float now)
        {
            _current = b;
            Playing = true;
            _line = 0;
            var lp = HellLinePlayer.Instance;
            _lineEnds = now + (b.lines.Length > 0 ? lp.Say(b.lines[0]) + 0.25f : 0f);
            if (b.letterbox) lp.Letterbox(true);
            float hold = HellBeatRules.HoldFor(b);
            if (hold > 0f) StartHold(b, now + hold);
            if (b.staging == HellBeatStaging.FigureAhead) ShowFigure(b, now);
        }

        void Finish()
        {
            if (_current != null && _current.letterbox) HellLinePlayer.Instance.Letterbox(false);
            EndHold();
            ClearFigure();
            _current = null;
            Playing = false;
        }

        void StartHold(HellBeatDef b, float until)
        {
            var p = _director != null ? _director.player : null;
            _holdEnds = until;
            _holding = true;
            HellTime.Request(HoldTimeKey, Mathf.Clamp(b.holdScale, 0.05f, 1f));
            if (!b.letterbox) HellLinePlayer.Instance.Letterbox(true);
            // pause the player's control: an empty input frame (only when nothing else drives the player, e.g. a replay)
            if (p != null && p.ScriptedInput == null)
            {
                _heldPlayer = p;
                _heldInput = Empty;
                p.ScriptedInput = _heldInput;
            }
        }

        static SkylineInputFrame Empty() => default(SkylineInputFrame);

        void EndHold()
        {
            if (!_holding) return;
            _holding = false;
            HellTime.Clear(HoldTimeKey);
            if (_current != null && !_current.letterbox) HellLinePlayer.Instance.Letterbox(false);
            if (_heldPlayer != null && _heldPlayer.ScriptedInput == _heldInput) _heldPlayer.ScriptedInput = null;
            _heldPlayer = null;
            _heldInput = null;
        }

        /// <summary>A dark figure with ember eyes, standing on the ground about <see cref="HellBeatDef.figureDistance"/> m
        /// ahead of the player, facing them (a stand-in until Vesper's body exists).</summary>
        void ShowFigure(HellBeatDef b, float now)
        {
            ClearFigure();
            var p = _director != null ? _director.player : null;
            if (p == null) return;
            var fwd = p.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 eye = p.transform.position + Vector3.up * 1.6f;
            float dist = Mathf.Max(4f, b.figureDistance);
            if (Physics.Raycast(eye, fwd, out var wall, dist, ~0, QueryTriggerInteraction.Ignore)) dist = Mathf.Max(3f, wall.distance - 1f);
            Vector3 at = eye + fwd * dist;
            if (Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out var ground, 40f, ~0, QueryTriggerInteraction.Ignore)) at = ground.point;
            else at.y = p.transform.position.y;

            _figure = new GameObject("HellBeatFigure");
            _figure.transform.position = at;
            _figure.transform.rotation = Quaternion.LookRotation(-fwd, Vector3.up);
            var body = SkylineBotLook.Visual(PrimitiveType.Capsule, "Body", _figure.transform, SkylineBotLook.Lit(HellFx.Smoke), false);
            body.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            var eyes = SkylineBotLook.Glow(HellFx.FireHue * 3f);
            for (int s = -1; s <= 1; s += 2)
            {
                var e = SkylineBotLook.Visual(PrimitiveType.Sphere, "Eye", _figure.transform, eyes, false);
                e.transform.localScale = Vector3.one * 0.07f;
                e.transform.localPosition = new Vector3(0.1f * s, 1.65f, 0.3f);
            }
            foreach (var c in _figure.GetComponentsInChildren<Collider>(true)) Destroy(c);
            _figureEnds = now + Mathf.Max(1f, b.figureSeconds);
        }

        void ClearFigure()
        {
            if (_figure == null) return;
            HellFx.SmokeBurst(_figure.transform.position + Vector3.up);
            Destroy(_figure);
            _figure = null;
        }
    }
}
