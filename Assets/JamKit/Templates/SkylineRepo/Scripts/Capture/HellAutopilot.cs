using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JamKit.Movement;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The Hell autopilot (docs/HELL-AUTOPILOT.md): plays the built game like a person, only under <c>-hellAutopilot safe|fast</c>.
    /// <list type="bullet">
    /// <item><b>The plan</b> per room comes from the room's data (<see cref="HellAutopilotPlan"/>): its safe or fast path, split
    /// into runs, climbs, jumps, slides, Shadow Steps and wall-runs, with the objective's tasks on it (touch the sigils in
    /// order, take and carry the lantern, shoot each anchor from inside its ward, hold or lap the brand, seal the wells, outrun
    /// the collapse) and the drop hole at the end.</item>
    /// <item><b>The hands</b> are the real player's: movement, jump, crouch (slide), fire and reload through
    /// <see cref="SkylinePlayer.ScriptedInput"/>; the view turned by <see cref="HumanAim"/> (a reaction beat on a new target, a
    /// capped turn rate, an overshoot that settles; it never snaps); the Shadow Step (hold to aim, release), the Rewind and the
    /// skill pick by key presses on a virtual keyboard, so they go through the game's own bindings and screens. If that
    /// keyboard is not read (it logs <c>HELL_AUTO keys fallback</c>), the same code paths are called directly.</item>
    /// <item><b>The fight:</b> the nearest bot in a clear line, with spread on the aim point, fired at only while the
    /// crosshair's own ray lands on it, at a human cadence. Never in The Collapse, never mid-jump.</item>
    /// <item><b>The watch:</b> per room, entered, objective done, time, deaths, sinks, where it got stuck (no progress for
    /// <c>-hellAutopilotStuck</c> s: position, room, last action), errors and exceptions from the log, frame times. A stuck fast
    /// route falls back to the safe one; a room stuck for <c>-hellAutopilotSkipAfter</c> s is skipped by a teleport to its drop
    /// hole (logged; <c>-hellAutopilotNoSkip</c> turns that off).</item>
    /// <item><b>The Rewind edge cases</b> (<c>-hellAutopilotRewind</c>): a Rewind at a room's first second, across a death,
    /// during The Collapse, during the lantern carry, mid-Shadow-Step and during the skill pick, each checked by
    /// <see cref="HellRewindChecks"/>.</item>
    /// </list>
    /// It writes <c>-hellAutopilotLog</c> (JSON) and ends with <c>HELL_AUTOPILOT done ...</c>, then quits the player.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public sealed class HellAutopilot : MonoBehaviour
    {
        // ------------------------------------------------------------------ the switches

        public const string Flag = "-hellAutopilot";
        public static bool Enabled => SkylineRunArgs.Has(Flag);

        static float ArgF(string name, float fallback)
        {
            var v = SkylineRunArgs.Value(name);
            return v != null && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : fallback;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Enabled) return;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Attach();
        }

        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => Attach();

        static void Attach()
        {
            if (FindFirstObjectByType<HellAutopilot>() != null) return;
            var dir = FindFirstObjectByType<HellRunDirector>();
            if (dir == null) return;
            var go = new GameObject("Hell Autopilot");
            DontDestroyOnLoad(go);
            go.AddComponent<HellAutopilot>().director = dir;
        }

        // ------------------------------------------------------------------ tuning

        const float ArriveRadius = 1.0f, TaskRadius = 0.7f, PassLateral = 1.6f;
        const float CalmRate = 280f, FightRate = 650f, FightRange = 38f, StopWait = 2.5f, NudgeWait = 4f;
        const float StrafeMin = 0.5f, StrafeMax = 1.2f, FireGapMin = 0.11f, FireGapMax = 0.24f;
        const float StepAimHold = 0.35f, StepSettle = 1.5f, StepGiveUp = 3.5f;
        const float LookAhead = 4f;

        public HellRunDirector director;

        HellAutoRoute _mode = HellAutoRoute.Safe;
        float _stuckSeconds, _skipAfter, _timeLimit;
        bool _noSkip, _rewindCases, _toCredits;
        string _logPath;
        System.Random _rng;

        SkylinePlayer _player;
        HellAbilities _abilities;
        readonly HumanAim _hand = new HumanAim();
        bool _handReady;
        readonly HellAutoReport _report = new HellAutoReport();
        readonly HellFrameStats _frames = new HellFrameStats();
        readonly HellProgressWatch _watch = new HellProgressWatch();

        // the room and its plan
        HellRoom _room;
        HellAutoPlan _plan;
        HellAutoRoute _route;
        int _idx, _lapIdx, _fastFailures, _stuckInRoom;
        float _roomStuckSeconds, _taskSince = -1f, _nextStrafe;
        bool _lapping, _finished;
        string _action = "start";
        float _strafeSign = 1f;

        // this frame's hands
        Vector2 _move;
        bool _sprint, _jump, _crouch, _fire, _reload, _swap, _aimHeld;
        bool _wantAimPoint;
        Vector3 _aimPoint;
        float _lastFire, _nextFireGap = 0.15f;
        bool _djArmed, _djTook, _jumpedThisLeg;
        float _calmYaw, _calmPitch;
        // the fight
        SkylineBot _target;
        Collider _anchorTarget;
        Vector3 _spread;
        float _spreadUntil;

        // the Shadow Step
        enum StepPhase { None, Aim, Released }
        StepPhase _step;
        float _stepSince;
        bool _stepped;
        int _stepFails;
        float _stepWaitFrom = -1f;

        // ------------------------------------------------------------------ setup

        void Awake()
        {
            var m = SkylineRunArgs.Value(Flag);
            _mode = m != null && m.Trim().ToLowerInvariant() == "fast" ? HellAutoRoute.Fast : HellAutoRoute.Safe;
            _stuckSeconds = ArgF("-hellAutopilotStuck", 10f);
            _skipAfter = ArgF("-hellAutopilotSkipAfter", 120f);
            _timeLimit = ArgF("-hellAutopilotMinutes", 75f) * 60f;
            _noSkip = SkylineRunArgs.Has("-hellAutopilotNoSkip");
            _rewindCases = SkylineRunArgs.Has("-hellAutopilotRewind");
            // -hellChapter 4 starts at the Vesper fight, past the rooms: there is nothing to play unless it plays on to the credits
            _toCredits = SkylineRunArgs.Has("-hellAutopilotToCredits")
                || HellChapterStart.Parse(Environment.GetCommandLineArgs()).chapter == HellFlow.VesperChapter;
            // playing on to the credits: the ending's escape is played (a scripted player otherwise skips it)
            HellEndingDirector.ScriptedPlaysEscape = _toCredits;
            _logPath = SkylineRunArgs.Value("-hellAutopilotLog") ?? "out/hell/autopilot.json";
            int seed = (int)ArgF("-hellSeed", 1);
            _rng = new System.Random(seed * 7919 + 17);
            _watch.seconds = _stuckSeconds;
            _report.mode = (_mode == HellAutoRoute.Fast ? "fast" : "safe") + (_rewindCases ? "+rewind" : "");
            _report.seed = SkylineRunArgs.Value("-hellSeed") ?? "random";
            Application.runInBackground = true;
            try { InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; }
            catch (Exception e) { Debug.LogWarning("HELL_AUTO could not set the input background behaviour: " + e.Message); }
            Keys.Setup();
            if (_rewindCases) _cases = new List<string> { "room-first-second", "lantern-carry", "during-collapse", "mid-shadow-step", "across-death", "during-pick" };
            // take the player's hands now, before the run director's Start: a scripted player tells the flow to skip its screens
            Bind();
            Debug.Log("HELL_AUTO start mode " + _report.mode + " seed " + _report.seed + " stuck " + _stuckSeconds + " s, skip after " + (_noSkip ? "never" : _skipAfter + " s"));
        }

        void OnEnable()
        {
            HellRunEvents.RoomEntered += OnRoomEntered;
            HellRunEvents.RoomRestarted += OnRoomRestarted;
            HellRunEvents.RunFinished += OnRunFinished;
            HellRewind.Rewound += OnRewound;
            Application.logMessageReceivedThreaded += OnLogThreaded;
        }

        void OnDisable()
        {
            HellRunEvents.RoomEntered -= OnRoomEntered;
            HellRunEvents.RoomRestarted -= OnRoomRestarted;
            HellRunEvents.RunFinished -= OnRunFinished;
            HellRewind.Rewound -= OnRewound;
            Application.logMessageReceivedThreaded -= OnLogThreaded;
            if (_player != null)
            {
                _player.Wrecked -= OnWrecked;
                _player.AttackTaken -= OnAttackTaken;
                if (_player.ScriptedInput == (Func<SkylineInputFrame>)NextFrame) _player.ScriptedInput = null;
            }
            if (_abilities != null) _abilities.Stepped -= OnStepped;
            Keys.ReleaseAll();
        }

        bool Bind()
        {
            if (director == null) director = FindFirstObjectByType<HellRunDirector>();
            if (director == null) return false;
            if (_player == null)
            {
                _player = director.player != null ? director.player : FindFirstObjectByType<SkylinePlayer>();
                if (_player == null) return false;
                _player.ScriptedInput = NextFrame;
                _player.Wrecked += OnWrecked;
                _player.AttackTaken += OnAttackTaken;
                _calmYaw = _player.Yaw;
            }
            if (_abilities == null)
            {
                _abilities = _player.GetComponent<HellAbilities>();
                if (_abilities != null) _abilities.Stepped += OnStepped;
            }
            return true;
        }

        // ------------------------------------------------------------------ the log

        readonly object _logLock = new object();
        readonly List<string> _pendingErrors = new List<string>();
        readonly List<string> _pendingLines = new List<string>();

        void OnLogThreaded(string msg, string stack, LogType type)
        {
            if (msg == null || msg.StartsWith("HELL_AUTO", StringComparison.Ordinal)) return;
            bool err = type == LogType.Exception || type == LogType.Error || type == LogType.Assert;
            bool mine = msg.StartsWith("HELL_ROOM sank", StringComparison.Ordinal) || msg.StartsWith("HELL_ROOM objective done", StringComparison.Ordinal)
                || msg.StartsWith("HELL_STATS", StringComparison.Ordinal) || msg.StartsWith("HELL_RUN finished", StringComparison.Ordinal);
            if (!err && !mine) return;
            lock (_logLock)
            {
                if (err) _pendingErrors.Add(type + ": " + Trim(msg, 300) + (type == LogType.Exception && stack != null ? " @ " + Trim(FirstLine(stack), 200) : ""));
                else _pendingLines.Add(msg);
            }
        }

        static string FirstLine(string s) { int i = s.IndexOf('\n'); return i < 0 ? s : s.Substring(0, i); }
        static string Trim(string s, int n) => s.Length <= n ? s : s.Substring(0, n) + "...";

        void DrainLog()
        {
            lock (_logLock)
            {
                foreach (var e in _pendingErrors)
                {
                    _report.errorCount++;
                    if (_report.errors.Count < 200) _report.errors.Add(RoomTag() + " " + e);
                    var r = _report.Current;
                    if (r != null && r.errors.Count < 30) r.errors.Add(e);
                }
                _pendingErrors.Clear();
                foreach (var l in _pendingLines)
                {
                    if (l.StartsWith("HELL_ROOM sank", StringComparison.Ordinal)) OnSank();
                    else if (l.StartsWith("HELL_ROOM objective done", StringComparison.Ordinal)) { var r = _report.Current; if (r != null && r.objectiveAt < 0f) { r.objectiveAt = Time.time - r.entered; r.objectiveDone = true; } }
                    else if (_toCredits && l.StartsWith("HELL_STATS", StringComparison.Ordinal)) Finish("finished");
                }
                _pendingLines.Clear();
            }
        }

        string RoomTag()
        {
            var r = _report.Current;
            return r == null ? "[no room]" : "[" + (r.floor + 1) + "." + (r.index + 1) + " " + r.kind + "]";
        }

        static void Log(string s) => Debug.Log("HELL_AUTO " + s);

        // ------------------------------------------------------------------ the run's events

        void OnRoomEntered(int floor, int index, string kind)
        {
            CloseRoom(false);
            var rec = new HellAutoReport.Room { floor = floor, index = index, kind = kind, route = _mode == HellAutoRoute.Fast ? "fast" : "safe", entered = Time.time };
            _report.rooms.Add(rec);
            _frames.Clear();
            _room = null;   // bound on the next frame, once the director has the room up
            _roomStuckSeconds = 0f;
            _stuckInRoom = 0;
            _fastFailures = 0;
            Log("room " + (floor + 1) + "." + (index + 1) + " " + kind + " entered");
        }

        void CloseRoom(bool finishing)
        {
            var r = _report.Current;
            if (r == null || r.left >= 0f) return;
            if (finishing && !r.objectiveDone && _room != null && _room.Objective != null && _room.Objective.Done) r.objectiveDone = true;
            if (finishing && !r.objectiveDone) return;   // a room the run ended in, unfinished: left open
            r.left = Time.time;
            r.meanFps = _frames.MeanFps;
            r.lowFps = _frames.OnePercentLowFps;
            r.maxFrameMs = _frames.MaxFrameMs;
            Log("room " + (r.floor + 1) + "." + (r.index + 1) + " " + r.kind + " " + (r.skipped ? "skipped" : "ok") + " " + r.Seconds.ToString("0.0") +
                " s deaths " + r.deaths + " sinks " + r.sinks + " route " + r.route + " fps " + r.meanFps.ToString("0") + " (1% low " + r.lowFps.ToString("0") + ")");
        }

        int _diedFrame = -1, _restartFrame = -1;

        // a death restarts the room (the director raises RoomRestarted from the same Wrecked event, before or after us); a
        // restart with no death is The Collapse's sink
        void OnWrecked()
        {
            var r = _report.Current;
            if (r != null)
            {
                r.deaths++;
                if (_restartFrame == Time.frameCount && r.sinks > 0) r.sinks--;
            }
            _diedFrame = Time.frameCount;
            _action = "died";
            Log("died " + RoomTag());
        }

        void OnRoomRestarted()
        {
            var r = _report.Current;
            if (_diedFrame != Time.frameCount && r != null) r.sinks++;
            _restartFrame = Time.frameCount;
            _deathAt = Time.unscaledTime;
            ResetRoomPlay(fromStart: true);
        }

        void OnSank()
        {
            var r = _report.Current;
            if (r != null && (_room == null || _room.kind != HellRoomKind.Collapse)) r.sinks++;   // The Collapse's sink is counted at its restart
            _action = "sank";
            if (_route == HellAutoRoute.Fast && ++_fastFailures >= 2) SwitchToSafe("two sinks on the fast route");
            else Resume();
        }

        void OnRunFinished()
        {
            CloseRoom(false);
            if (!_toCredits) StartCoroutine(FinishSoon("finished"));
        }

        IEnumerator FinishSoon(string why)
        {
            float until = Time.unscaledTime + 2f;
            while (Time.unscaledTime < until) yield return null;
            Finish(why);
        }

        void OnStepped(Vector3 from, Vector3 to)
        {
            _stepped = true;
            var r = _report.Current;
            if (r != null) r.steps++;
        }

        bool _rewoundThisFrame;
        float _hurt, _hurtAt;

        /// <summary>A player's reflex: a lot of damage in a short time (a big hit, a burst) and the Rewind ready: use it. Not in
        /// the edge-case script (it keeps the cooldown for its cases).</summary>
        void OnAttackTaken(float damage, string attacker, Vector3 from)
        {
            _hurt = _hurt * Mathf.Exp(-(Time.time - _hurtAt) / 1.5f) + damage;
            _hurtAt = Time.time;
            if (_rewindCases || _hurt < 45f || attacker == "autopilot-rewind-case") return;
            var rw = _abilities != null ? _abilities.Rewind : null;
            if (rw == null || !rw.Cooldown.Ready || HellSkillPick.Open) return;
            _hurt = 0f;
            Log("rewind reflex " + RoomTag() + " after " + damage.ToString("0") + " damage from " + attacker);
            if (Keys.Broken) Keys.FallbackRewind(_abilities);
            else Keys.Tap(KeyAction.Rewind);
        }

        void OnRewound()
        {
            _rewoundThisFrame = true;
            var r = _report.Current;
            if (r != null) r.rewinds++;
            _step = StepPhase.None;
            Resume();
        }

        // ------------------------------------------------------------------ the frame

        void Update()
        {
            if (_finished) return;
            if (!Bind()) return;
            DrainLog();
            float now = Time.unscaledTime;
            if (Time.unscaledDeltaTime > 0f && !HellSkillPick.Open) _frames.Add(Time.unscaledDeltaTime * 1000f);
            if (now > _timeLimit) { Finish("timeout"); return; }
            ClearHands();
            if (_rewindCases) TickCases();

            // the skill pick: read the cards a moment, then press a number
            if (HellSkillPick.Open) { TickPick(); return; }
            _pickSeenAt = -1f;

            // the ending (docs/HELL-ENDING.md): hold fire through the decision (the director makes the -hellEnding choice by
            // itself after 2 s), run the escape's Collapse like a room, and let the ending's comic turn its own pages
            if (_toCredits && HellEndingDirector.Active)
            {
                var end = HellEndingDirector.Instance;
                var esc = end.EscapeRoom;
                if (esc != null)
                {
                    if (esc != _room && director.Run != null) BindRoom(esc);
                    if (_room == null || _plan == null) return;
                    try { TickRoom(); }
                    catch (Exception e)
                    {
                        string m = e.GetType().Name + ": " + e.Message;
                        if (_internal.Add(m)) Debug.LogWarning("HELL_AUTO internal (escape) " + m + " @ " + FirstLine(e.StackTrace ?? ""));
                    }
                    return;
                }
                _calmPitch = 10f;
                if (end.Stage == HellEndingDirector.Phase.Cards && HellComicPlayer.Playing) return;
                if (Time.timeScale < 0.01f) TickScreens();
                return;
            }

            var run = director.Run;
            if (run == null) { TickScreens(); return; }
            if (run.Phase != HellRunPhase.InRoom || director.Current == null)
            {
                // falling down a drop or the shaft, waiting: hands off, eyes ahead
                _calmPitch = 10f;
                if (run.Phase == HellRunPhase.NotStarted) TickScreens();
                if (run.Phase == HellRunPhase.Finished && _toCredits)
                {
                    // after the rooms (the boss, the ending, the credits): fight what is there, press on through the screens
                    if (Time.timeScale < 0.01f) TickScreens();
                    else TickFreeFight();
                }
                return;
            }
            if (director.Current != _room) BindRoom(director.Current);
            if (_room == null || _plan == null) return;
            try { TickRoom(); }
            catch (Exception e)
            {
                // the autopilot's own fault: say so once per message, keep playing
                string m = e.GetType().Name + ": " + e.Message;
                if (_internal.Add(m))
                {
                    Debug.LogWarning("HELL_AUTO internal " + m + " @ " + FirstLine(e.StackTrace ?? ""));
                    var r = _report.Current;
                    if (r != null) r.notes.Add("autopilot internal: " + m);
                }
            }
        }

        readonly HashSet<string> _internal = new HashSet<string>();

        void LateUpdate()
        {
            Keys.Tick();
            if (_rewoundThisFrame)
            {
                _rewoundThisFrame = false;
                _aimingAfterRewind = _abilities != null && _abilities.Aiming;
                _rewoundFrame = Time.frameCount;
            }
        }

        void ClearHands()
        {
            _move = Vector2.zero;
            _sprint = _jump = _crouch = _fire = _reload = _swap = _aimHeld = false;
            _wantAimPoint = false;
        }

        // ------------------------------------------------------------------ a room

        void BindRoom(HellRoom room)
        {
            if (_report.Current == null || _report.Current.left >= 0f) OnRoomEntered(director.Run.Floor, director.Run.Room, room.kind.ToString());
            _room = room;
            _route = _mode;
            BuildPlan(0);
            var r = _report.Current;
            if (r != null) { r.route = _route == HellAutoRoute.Fast ? "fast" : "safe"; foreach (var n in _plan.notes) r.notes.Add("plan: " + n); }
            _watch.Reset(Time.time, N(_player.transform.position), 0);
            _taskSince = -1f;
            _target = null;
            _anchorTarget = null;
        }

        void BuildPlan(int idx)
        {
            _plan = HellAutopilotPlan.Build(_room.Def, _route);
            _idx = Mathf.Clamp(idx, 0, _plan.points.Count - 1);
            _lapping = false;
            _step = StepPhase.None;
            _jumpedThisLeg = false;
        }

        void ResetRoomPlay(bool fromStart)
        {
            if (_room == null || _plan == null) return;
            _route = _fastFailures >= 2 ? HellAutoRoute.Safe : _mode;
            BuildPlan(0);
            if (!fromStart) Resume();
            _watch.Reset(Time.time, N(_player.transform.position), Progress());
        }

        void SwitchToSafe(string why)
        {
            if (_route == HellAutoRoute.Safe || _room == null) { Resume(); return; }
            _route = HellAutoRoute.Safe;
            var r = _report.Current;
            if (r != null) { r.routeSwitches++; r.route = "fast>safe"; r.notes.Add("switched to the safe route: " + why); }
            Log("route safe " + RoomTag() + ": " + why);
            BuildPlan(0);
            Resume();
        }

        void Resume()
        {
            if (_plan == null || _room == null || _player == null) return;
            _idx = HellAutopilotPlan.ResumeIndex(_plan, N(_player.transform.position), TaskDone);
            _lapping = false;
            _step = StepPhase.None;
            _jumpedThisLeg = false;
            _taskSince = -1f;
        }

        float Progress() => _idx * 1000f + (_room != null && _room.Objective != null ? _room.Objective.Progress * 100f : 0f) + (_lapping ? _lapIdx : 0);

        bool TaskDone(HellAutoPoint p)
        {
            var o = _room != null ? _room.Objective : null;
            if (o == null) return true;
            switch (p.task)
            {
                case HellAutoTask.Stop:
                    if (o is SigilChain s) return s.Next > p.stop;
                    if (o is LanternCarry l) return p.stop == 0 ? l.State != LanternState.OnPedestal : l.State == LanternState.Placed;
                    return o.Done;
                case HellAutoTask.Anchor: return o is ChainAnchors c ? c.Broken(p.stop) : o.Done;
                case HellAutoTask.Seal: return o is OozeWells w ? w.Sealed(p.stop) : o.Done;
                case HellAutoTask.Hold:
                case HellAutoTask.Lap: return o.Done;
                default: return true;
            }
        }

        void TickRoom()
        {
            var feet = _player.transform.position;
            var sim = Sim;

            // stuck: no move and no progress for a while
            if (_watch.Tick(Time.time, N(feet), Progress())) OnStuck(feet);

            if (_idx >= _plan.points.Count) { _idx = _plan.points.Count - 1; }
            var p = _plan.points[_idx];
            var pp = U(p.point);

            // the objective is not done but the plan is at its exit: the fast route missed a task in the air
            if (p.task == HellAutoTask.Exit && _room.Objective != null && !_room.Objective.Done && _room.kind != HellRoomKind.Collapse)
            {
                if (_route == HellAutoRoute.Fast) { SwitchToSafe("the fast route ended with the objective undone"); return; }
            }

            bool urgent = _plan.urgent;
            bool airborne = sim != null && !sim.Grounded;

            // the fight: not in The Collapse, not on a gap's leg or near its takeoff (a jump needs the sprint and the view ahead);
            // the fast route fights only at its task points (a speedrunner shoots where it has to stop anyway)
            bool legBusy = p.move == HellAutoMove.Jump || p.move == HellAutoMove.ShadowStep || p.move == HellAutoMove.WallRun || p.move == HellAutoMove.Climb || _step != StepPhase.None;
            bool gapNext = _idx + 1 < _plan.points.Count && IsGapMove(_plan.points[_idx + 1].move) && Near(feet, pp, 8f);
            bool atTask = p.task != HellAutoTask.None && p.task != HellAutoTask.Exit && Near(feet, pp, 2f);
            PickTarget(urgent || legBusy || gapNext || airborne || (_route == HellAutoRoute.Fast && !atTask));

            if (_lapping) { TickLap(); return; }
            if (_step != StepPhase.None) { TickStep(p); return; }

            // at a task point (on the ground): do the task
            if (p.task != HellAutoTask.None && p.task != HellAutoTask.Exit && !airborne && Near(feet, pp, TaskRadius + (p.task == HellAutoTask.Seal ? 1f : 0f)))
            {
                if (TickTask(p)) Advance();
                return;
            }
            // tasks done on the way (the objective counted it early: a sigil touched from the bridge's end)
            if (p.task != HellAutoTask.None && p.task != HellAutoTask.Exit && p.task != HellAutoTask.Lap && p.task != HellAutoTask.Hold && TaskDone(p)
                && Near(feet, p.point, 3f))
            {
                Advance();
                return;
            }
            // an anchor in the air (a wall-run's shot): shoot it while passing
            if (p.task == HellAutoTask.Anchor && airborne) AimAtAnchor(p.stop);

            // the next point is a Shadow Step: at its takeoff, start the step
            if (p.move == HellAutoMove.ShadowStep && _idx > 0 && Near(feet, U(_plan.points[_idx - 1].point), 1.2f) && !airborne)
            {
                _step = StepPhase.Aim;
                _stepSince = Time.time;
                _stepped = false;
                _action = "shadow-step aim";
                return;
            }

            // arrived (or passed it)?
            if (Arrived(feet, p))
            {
                // a task point passed in the air (an anchor shot from a wall-run) is not waited for: the safe route picks up what it missed
                if (p.task == HellAutoTask.None || p.task == HellAutoTask.Exit || airborne) Advance();
                return;
            }
            Steer(feet, p, urgent);
        }

        static bool IsGapMove(HellAutoMove m) => m == HellAutoMove.Jump || m == HellAutoMove.ShadowStep || m == HellAutoMove.WallRun;

        bool Arrived(Vector3 feet, HellAutoPoint p)
        {
            float r = p.task != HellAutoTask.None ? TaskRadius : ArriveRadius;
            bool air = Sim != null && !Sim.Grounded;
            if (p.move == HellAutoMove.Jump || p.move == HellAutoMove.WallRun) r = 1.6f;
            if (Near(feet, U(p.point), r)) return true;
            if (p.task != HellAutoTask.None && !air) return false;
            // passed it: beyond the point along the next leg, and close to the line
            if (_idx + 1 < _plan.points.Count)
            {
                var a = U(p.point);
                var b = U(_plan.points[_idx + 1].point);
                var ab = Flat(b - a);
                var af = Flat(feet - a);
                if (ab.sqrMagnitude > 0.01f && Vector3.Dot(af, ab.normalized) > 0f && Mathf.Abs(Cross(ab.normalized, af)) < PassLateral && Mathf.Abs(feet.y - a.y) < 2f) return true;
            }
            return false;
        }

        void Advance()
        {
            _idx = Mathf.Min(_idx + 1, _plan.points.Count - 1);
            _taskSince = -1f;
            _jumpedThisLeg = false;
            _djArmed = false;
        }

        // ------------------------------------------------------------------ moving

        void Steer(Vector3 feet, HellAutoPoint p, bool urgent)
        {
            var sim = Sim;
            bool grounded = sim == null || sim.Grounded;
            var target = U(p.point);
            // look ahead along the plan for the view, aim the feet at the point
            var to = Flat(target - feet);
            float dist = to.magnitude;
            Vector3 dir = dist > 0.01f ? to / dist : Vector3.zero;
            _action = p.move.ToString().ToLowerInvariant() + " to " + (_idx + 1) + "/" + _plan.points.Count;

            // the view: the fight's target, or the way ahead
            Vector3 ahead = LookPoint(feet, target);
            if (!_wantAimPoint)
            {
                AimAssist.Angles(N(_player.EyePosition), N(ahead), out float yaw, out float pitch);
                _calmYaw = yaw;
                _calmPitch = Mathf.Clamp(pitch, -30f, 25f);
            }

            // the stick: the world direction in the view's frame (a turned view strafes, as a player does)
            _move = Local(dir);
            bool fighting = _wantAimPoint && !urgent;
            bool mostlyForward = _move.y > 0.75f;
            _sprint = mostlyForward && (!fighting || urgent);
            if (fighting && p.move == HellAutoMove.Run) _move *= 0.85f;   // walking and shooting

            switch (p.move)
            {
                case HellAutoMove.Climb:
                    if (grounded && dist < 2.6f && target.y - feet.y > 0.6f && !_jumpedThisLeg) PressJump(doubleAtApex: target.y - feet.y > 1.1f);
                    _move = new Vector2(_move.x, Mathf.Max(_move.y, 0.6f)).normalized;
                    break;
                case HellAutoMove.Jump:
                    _sprint = mostlyForward;
                    if (grounded && !_jumpedThisLeg && (GapAhead(feet, dir) || dist < 0.6f + p.gap * 0.5f && p.gap > 0f && GapAhead(feet, dir, 0.8f))) PressJump(doubleAtApex: p.gap > 3f || target.y > feet.y + 0.3f);
                    else if (!grounded && sim != null && sim.Mode == MoveMode.Wallrun && !_jumpedThisLeg) PressJump(true);   // kick off the wall
                    else if (!grounded && sim != null && sim.Velocity.Y < 0f && sim.DoubleJumpAvailable && Flat(target - feet).magnitude > 2f && target.y > feet.y - 1.5f) _jump = true;
                    break;
                case HellAutoMove.WallRun:
                    _sprint = true;
                    if (grounded && !_jumpedThisLeg && (dist < 6f || GapAhead(feet, dir))) PressJump(false);
                    break;
                case HellAutoMove.Slide:
                    if (grounded && BeamNear(feet, 3.2f) && (sim == null || sim.HorizontalSpeed > 6f)) { _crouch = true; _sprint = true; _action = "slide"; }
                    break;
                case HellAutoMove.Drop:
                    _sprint = mostlyForward && !fighting;
                    break;
            }
            // a gap the plan didn't call (a fallen platform): jump it
            if (p.move == HellAutoMove.Run && grounded && _route == HellAutoRoute.Fast && GapAhead(feet, dir, 1.0f) && !_jumpedThisLeg && LavaBelow(feet + dir * 1.5f))
                PressJump(true);
            if (grounded && _djArmed && _djTook) _djArmed = false;
            TickDoubleJump();
        }

        Vector3 LookPoint(Vector3 feet, Vector3 target)
        {
            // the point about LookAhead metres along the plan, at eye height
            float left = LookAhead;
            Vector3 from = feet;
            for (int i = _idx; i < _plan.points.Count; i++)
            {
                var q = U(_plan.points[i].point);
                float d = Flat(q - from).magnitude;
                if (d >= left) return from + (q - from) * (left / Mathf.Max(d, 1e-3f)) + Vector3.up * 1.4f;
                left -= d;
                from = q;
            }
            return target + Vector3.up * 1.4f;
        }

        void PressJump(bool doubleAtApex)
        {
            _jump = true;
            _jumpedThisLeg = true;
            _djArmed = doubleAtApex;
            _djTook = false;
        }

        void TickDoubleJump()
        {
            var sim = Sim;
            if (!_djArmed || sim == null) return;
            if (!sim.Grounded) _djTook = true;
            if (_djTook && sim.Velocity.Y <= 0.3f && !sim.Grounded)
            {
                _jump = true;
                _djArmed = false;
            }
        }

        /// <summary>No floor in front of the feet (a probe at <paramref name="ahead"/> m, down 2.5 m), or lava there.</summary>
        bool GapAhead(Vector3 feet, Vector3 dir, float ahead = 1.3f)
        {
            if (dir.sqrMagnitude < 0.01f) return false;
            var probe = feet + dir * ahead + Vector3.up * 0.8f;
            if (!Physics.Raycast(probe, Vector3.down, out var hit, 3.3f, WorldMask, QueryTriggerInteraction.Ignore)) return true;
            if (hit.point.y < feet.y - 1.2f) return true;
            return LavaAt(hit.point);
        }

        bool LavaBelow(Vector3 p) => LavaAt(p) || !Physics.Raycast(p + Vector3.up, Vector3.down, 3f, WorldMask, QueryTriggerInteraction.Ignore);

        static bool LavaAt(Vector3 p) => HellAbilities.LavaQuery != null && HellAbilities.LavaQuery(p);

        bool BeamNear(Vector3 feet, float within)
        {
            foreach (var h in _room.Def.hazards)
            {
                if (h.kind != HellHazardKind.Beam) continue;
                var c = HellRoomLint.DistanceToBox(new NVec3(feet.x, (h.box.min.Y + h.box.max.Y) * 0.5f, feet.z), h.box);
                if (c <= within) return true;
            }
            return false;
        }

        int WorldMask
        {
            get
            {
                var mc = _player.Movement;
                return (mc != null ? mc.worldMask.value : ~0) & ~(1 << 2);
            }
        }

        // ------------------------------------------------------------------ tasks

        /// <summary>One frame at a task point; true when it is done.</summary>
        bool TickTask(HellAutoPoint p)
        {
            if (_taskSince < 0f) _taskSince = Time.time;
            float held = Time.time - _taskSince;
            var feet = _player.transform.position;
            if (TaskDone(p))
            {
                Log("task " + p.task + (p.stop >= 0 ? " " + p.stop : "") + " done " + RoomTag() + " after " + held.ToString("0.0") + " s");
                return true;
            }
            _action = "task " + p.task.ToString().ToLowerInvariant() + (p.stop >= 0 ? " " + p.stop : "");
            switch (p.task)
            {
                case HellAutoTask.Stop:
                {
                    // the objective counts the touch on the next step; a stop not counted yet: walk onto its very spot
                    var st = _room.Def.stops[p.stop];
                    if (held > StopWait) MoveTo(feet, HellRoom.U(st.position), walk: true);
                    if (held > StopWait + NudgeWait) { NoteRoom("stop " + st.id + " not counted after " + held.ToString("0") + " s"); return true; }
                    LookAround();
                    break;
                }
                case HellAutoTask.Anchor:
                    if (_target == null || Vector3.Distance(_target.Center, feet) > 10f) AimAtAnchor(p.stop);
                    StrafeAround(U(p.point), 0.6f);
                    if (held > 25f) { NoteRoom("anchor " + p.stop + " not broken after 25 s"); return true; }
                    break;
                case HellAutoTask.Seal:
                    StrafeAround(U(p.point), 0.9f);
                    if (!_wantAimPoint) LookAround();
                    if (held > 20f) { NoteRoom("well " + p.stop + " not sealed after 20 s"); return true; }
                    break;
                case HellAutoTask.Hold:
                    StrafeAround(U(p.point), 0.5f);
                    if (!_wantAimPoint) LookAround();
                    if (held > 70f) { NoteRoom("the brand not set after 70 s"); return true; }
                    break;
                case HellAutoTask.Lap:
                    _lapping = true;
                    _lapIdx = NearestLap(feet);
                    _action = "lap";
                    break;
            }
            return false;
        }

        void NoteRoom(string s)
        {
            var r = _report.Current;
            if (r != null) r.notes.Add(s);
            Log("note " + RoomTag() + " " + s);
        }

        void MoveTo(Vector3 feet, Vector3 point, bool walk)
        {
            var to = Flat(point - feet);
            if (to.magnitude < 0.15f) return;
            _move = Local(to.normalized) * (walk ? 0.6f : 1f);
            _sprint = !walk && _move.y > 0.75f;
        }

        /// <summary>Holding a spot like a player: side steps of half a second to a second, back toward the spot when drifting.</summary>
        void StrafeAround(Vector3 spot, float radius)
        {
            var feet = _player.transform.position;
            if (Time.time >= _nextStrafe)
            {
                _strafeSign = _rng.NextDouble() < 0.5 ? -1f : 1f;
                _nextStrafe = Time.time + Range(StrafeMin, StrafeMax);
                if (_rng.NextDouble() < 0.12 && Sim != null && Sim.Grounded) _jump = true;   // a hop now and then
            }
            var back = Flat(spot - feet);
            if (back.magnitude > radius) { _move = Local(back.normalized) * 0.7f; return; }
            _move = new Vector2(_strafeSign * 0.55f, 0f);
        }

        float _lookAroundAt;

        void LookAround()
        {
            if (Time.time < _lookAroundAt) return;
            _lookAroundAt = Time.time + Range(1.2f, 2.6f);
            _calmYaw = _player.Yaw + Range(-50f, 50f);
            _calmPitch = Range(-8f, 6f);
        }

        void TickLap()
        {
            var feet = _player.transform.position;
            if (_room.Objective != null && _room.Objective.Done)
            {
                _lapping = false;
                Log("lap done " + RoomTag());
                Advance();
                return;
            }
            var lap = _plan.lap;
            if (lap.Count == 0) { _lapping = false; Advance(); return; }
            var q = HellRoom.U(lap[_lapIdx % lap.Count]);
            if (Near(feet, q, 2.2f)) _lapIdx = (_lapIdx + 1) % lap.Count;
            q = HellRoom.U(lap[_lapIdx % lap.Count]);
            var dir = Flat(q - feet).normalized;
            if (!_wantAimPoint || Vector3.Angle(dir, Flat(_aimPoint - feet)) > 50f)
            {
                _wantAimPoint = false;
                var la = HellRoom.U(lap[(_lapIdx + 1) % lap.Count]);
                AimAssist.Angles(N(_player.EyePosition), N(Vector3.Lerp(q, la, 0.5f) + Vector3.up * 1.4f), out float yaw, out float pitch);
                _calmYaw = yaw;
                _calmPitch = Mathf.Clamp(pitch, -20f, 15f);
            }
            _move = Local(dir);
            _sprint = _move.y > 0.6f;
            if (_rng.NextDouble() < 0.004 && Sim != null && Sim.Grounded && Sim.HorizontalSpeed > 7f) _crouch = true;   // a slide now and then
            _action = "lap " + _lapIdx;
        }

        int NearestLap(Vector3 feet)
        {
            int best = 0;
            float bd = float.MaxValue;
            for (int i = 0; i < _plan.lap.Count; i++)
            {
                float d = Vector3.Distance(HellRoom.U(_plan.lap[i]), feet);
                if (d < bd) { bd = d; best = i; }
            }
            return (best + 1) % Math.Max(1, _plan.lap.Count);
        }

        // ------------------------------------------------------------------ the Shadow Step

        void TickStep(HellAutoPoint p)
        {
            var landing = U(p.point);
            float since = Time.time - _stepSince;
            var feet = _player.transform.position;
            _wantAimPoint = true;
            _aimPoint = landing + (since > StepSettle ? Flat(landing - feet).normalized * 0.8f : Vector3.zero);
            _target = null;
            if (_step == StepPhase.Aim)
            {
                bool blocked = HellAbilities.ShadowStepBlocked;
                bool ready = _abilities != null && _abilities.StepReady;
                if (blocked || _abilities == null) { _step = StepPhase.Released; _stepSince = Time.time - 1f; return; }
                if (!ready)
                {
                    // a charge is coming back: wait for it at the edge (up to the recharge), then aim from the start
                    if (_stepWaitFrom < 0f) _stepWaitFrom = Time.time;
                    if (Time.time - _stepWaitFrom < 7f) { _action = "shadow-step wait charge"; _stepSince = Time.time; return; }
                }
                _stepWaitFrom = -1f;
                Keys.Hold(KeyAction.ShadowStep, true);
                _action = "shadow-step aim";
                bool valid = Keys.Broken ? since > StepAimHold : _abilities.Aiming && _abilities.Preview.valid;
                bool settled = _hand.ErrorTo(_wantYaw, _wantPitch) < 1.5f && _hand.Rate < 40f;
                if (since > 0.25f && !_abilities.Aiming && !Keys.Broken && Keys.Check("step")) Keys.MarkBroken("the Shadow Step key was not read");
                if ((since > StepAimHold && settled && valid) || since > StepGiveUp)
                {
                    Keys.Hold(KeyAction.ShadowStep, false);
                    if (Keys.Broken) Keys.FallbackStep(_abilities);
                    _step = StepPhase.Released;
                    _stepSince = Time.time;
                }
                return;
            }
            // released: did it go?
            if (_stepped || Near(feet, landing, 2f))
            {
                _step = StepPhase.None;
                _action = "shadow-step landed";
                Advance();
                return;
            }
            if (since > 0.5f)
            {
                _step = StepPhase.None;
                _stepFails++;
                NoteRoom("Shadow Step to " + landing.ToString("F1") + " refused (" + (_abilities != null ? _abilities.Preview.reason : "no abilities") + ")");
                if (++_fastFailures >= 2) SwitchToSafe("the Shadow Step failed twice");
            }
        }

        // ------------------------------------------------------------------ the fight

        void PickTarget(bool calm)
        {
            if (calm) { _target = null; return; }
            var eye = _player.EyePosition;
            SkylineBot best = null;
            float bd = FightRange;
            foreach (var b in SkylineBot.Live)
            {
                if (b == null || !b.Alive || !b.isActiveAndEnabled) continue;
                float d = Vector3.Distance(eye, b.Center);
                if (d >= bd) continue;
                if (!ClearShot(eye, b)) continue;
                bd = d;
                best = b;
            }
            if (best != _target)
            {
                if (best != null) _hand.React(Range(0.16f, 0.3f));   // a beat before the wrist moves to a new target
                _target = best;
                _spreadUntil = 0f;
            }
            if (_target == null) return;
            if (Time.time >= _spreadUntil)
            {
                _spread = UnityEngine.Random.insideUnitSphere * (_target.Radius * 0.45f);
                _spreadUntil = Time.time + Range(0.3f, 0.6f);
            }
            _wantAimPoint = true;
            _aimPoint = _target.Center + _spread;
            _action = "fight " + (_target.Def != null ? _target.Def.id : "bot");
            TriggerOn(c => c.TryGetComponent<SkylineHitZone>(out var z) && z.owner == _target);
            TickAmmo(true);
        }

        void AimAtAnchor(int i)
        {
            var col = AnchorCollider(i);
            if (col == null) return;
            _wantAimPoint = true;
            _aimPoint = col.bounds.center + _spread * 0.2f;
            if (Time.time >= _spreadUntil) { _spread = UnityEngine.Random.insideUnitSphere * 0.3f; _spreadUntil = Time.time + Range(0.3f, 0.6f); }
            TriggerOn(c => c == col);
            TickAmmo(true);
        }

        Collider AnchorCollider(int i)
        {
            if (_anchorTarget != null && _anchorTarget.gameObject.activeInHierarchy)
            {
                var a0 = _anchorTarget.GetComponentInParent<HellAnchor>();
                if (a0 != null && a0.index == i) return _anchorTarget;
            }
            _anchorTarget = null;
            foreach (var a in _room.GetComponentsInChildren<HellAnchor>(false))
                if (a.index == i) { _anchorTarget = a.GetComponentInChildren<Collider>(); break; }
            return _anchorTarget;
        }

        void TriggerOn(Func<Collider, bool> isTarget)
        {
            if (!Physics.Raycast(_player.EyePosition, _player.AimDirection, out var hit, FightRange + 5f, SkylineShooting.ShotMask, QueryTriggerInteraction.Ignore)) return;
            if (!isTarget(hit.collider)) return;
            if (Time.time - _lastFire < _nextFireGap) { _fireHeldOnly = true; return; }
            _fire = true;
            _lastFire = Time.time;
            _nextFireGap = Range(FireGapMin, FireGapMax);
        }

        bool _fireHeldOnly;

        bool ClearShot(Vector3 eye, SkylineBot b)
        {
            var to = b.Center - eye;
            if (!Physics.Raycast(eye, to.normalized, out var hit, to.magnitude + 1f, SkylineShooting.ShotMask, QueryTriggerInteraction.Ignore)) return false;
            return hit.collider.TryGetComponent<SkylineHitZone>(out var z) && z.owner == b;
        }

        void TickAmmo(bool engaged)
        {
            var lo = _player.Loadout;
            if (lo == null) return;
            var g = lo.Active;
            if (g.State.IsReloading) return;
            int mag = g.Def.weapon.magazineSize;
            if (g.Ammo == 0)
            {
                if (g.State.Reserve > 0) _reload = true;
                else if (lo.Count > 1) _swap = true;
            }
            else if (!engaged && g.Ammo < mag * 0.4f && g.State.Reserve > 0) _reload = true;
        }

        // ------------------------------------------------------------------ stuck

        void OnStuck(Vector3 feet)
        {
            _stuckInRoom++;
            _roomStuckSeconds += _stuckSeconds;
            string where = feet.ToString("F1");
            string line = "room " + RoomTag() + " stuck " + _action + " " + _watch.StillFor(Time.time).ToString("0") + " s at " + where + " point " + (_idx + 1) + "/" + _plan.points.Count + " route " + _route;
            Log(line);
            var r = _report.Current;
            if (r != null) r.stuck.Add(_watch.StillFor(Time.time).ToString("0") + " s at " + where + " (" + _action + ", point " + (_idx + 1) + "/" + _plan.points.Count + ", route " + _route + ")");
            if (_route == HellAutoRoute.Fast) { SwitchToSafe("stuck"); return; }
            if (!_noSkip && _roomStuckSeconds >= _skipAfter) { Skip(); return; }
            // try again: a hop and a resume from the nearest point
            _jump = true;
            Resume();
        }

        void Skip()
        {
            var r = _report.Current;
            if (r != null) r.skipped = true;
            var d = _room.Def;
            var c = (d.dropZone.min + d.dropZone.max) * 0.5f;
            Log("skip " + RoomTag() + ": teleport to the drop hole after " + _roomStuckSeconds.ToString("0") + " s stuck");
            _player.Movement.Teleport(new Vector3(c.X, c.Y, c.Z), _player.Yaw, Vector3.zero);
        }

        // ------------------------------------------------------------------ the pick and the screens

        float _pickSeenAt = -1f, _nextScreenPress;

        void TickPick()
        {
            if (_pickSeenAt < 0f) _pickSeenAt = Time.unscaledTime;
            float t = Time.unscaledTime - _pickSeenAt;
            if (_caseWantsPick) return;   // the edge-case script tries a Rewind first
            if (t > 1.4f && !_pickPressed)
            {
                int card = _rng.Next(3);
                Keys.Tap(card == 0 ? Key.Digit1 : card == 1 ? Key.Digit2 : Key.Digit3);
                _pickPressed = true;
                _pickCard = card;
                Log("pick card " + (card + 1));
            }
            if (t > 3.5f && _pickPressed) { Keys.FallbackPick(_player, _pickCard); _pickPressed = false; _pickSeenAt = Time.unscaledTime - 1.5f; }
        }

        bool _pickPressed;
        int _pickCard;

        void TickScreens()
        {
            // a title or a card (the flow lane's screens) or the run's end: press Enter now and then
            _pickPressed = false;
            if (Time.unscaledTime < _nextScreenPress) return;
            _nextScreenPress = Time.unscaledTime + 1.5f;
            if (director != null && director.Run != null && director.Run.Phase == HellRunPhase.Finished && !_toCredits) return;   // Enter would reload the scene
            Keys.Tap(Key.Enter);
        }

        /// <summary>Past the rooms (a boss fight the plan knows nothing of): stand, strafe and shoot the nearest bot in sight,
        /// look round when there is none. It never walks off: the arena's ground is not in the room data.</summary>
        void TickFreeFight()
        {
            PickTarget(false);
            if (_player == null) return;
            StrafeAround(_player.transform.position, 0.1f);
            if (_target == null) LookAround();
            else TickAmmo(true);
            _action = "free fight";
        }

        // ------------------------------------------------------------------ the hands' frame

        float _wantYaw, _wantPitch;

        SkylineInputFrame NextFrame()
        {
            var f = new SkylineInputFrame();
            if (_finished || HellSkillPick.Open || SkylineDeviceInput.Paused) return f;
            f.move = Vector2.ClampMagnitude(_move, 1f);
            f.sprintHeld = _sprint;
            f.jumpPressed = _jump;
            f.jumpHeld = _jump;
            f.crouchPressed = _crouch;
            f.crouchHeld = _crouch;
            f.reloadPressed = _reload;
            f.swapPressed = _swap;
            f.aimHeld = _aimHeld;
            if (!_handReady) { _hand.Snap(_player.Yaw, _player.Pitch); _handReady = true; }
            if (_wantAimPoint)
            {
                AimAssist.Angles(N(_player.EyePosition), N(_aimPoint), out float yaw, out float pitch);
                _wantYaw = yaw;
                _wantPitch = pitch;
                _hand.maxRate = FightRate;
            }
            else
            {
                _wantYaw = _calmYaw;
                _wantPitch = _calmPitch;
                _hand.maxRate = CalmRate;
            }
            _wantPitch = Mathf.Clamp(_wantPitch, -SkylinePlayer.MaxPitch + 1f, SkylinePlayer.MaxPitch - 1f);
            // a teleport (a drop, a sink, a Rewind, a step) moved the view: the hand starts from where the view is
            if (Mathf.Abs(Mathf.DeltaAngle(_hand.Yaw, _player.Yaw)) > 0.5f || Mathf.Abs(_hand.Pitch - _player.Pitch) > 0.5f) _hand.Snap(_player.Yaw, _player.Pitch);
            _hand.Step(Time.unscaledDeltaTime, _wantYaw, _wantPitch);
            _player.SetAim(_hand.Yaw, _hand.Pitch);
            f.firePressed = _fire;
            f.fireHeld = _fire || _fireHeldOnly;
            _fireHeldOnly = false;
            _jump = _crouch = _fire = _reload = _swap = false;
            return f;
        }

        // ------------------------------------------------------------------ finishing

        void Finish(string why)
        {
            if (_finished) return;
            DrainLog();
            CloseRoom(true);
            _finished = true;
            _report.result = why;
            Keys.ReleaseAll();
            string json = _report.ToJson();
            try
            {
                var full = System.IO.Path.GetFullPath(_logPath);
                var dir = System.IO.Path.GetDirectoryName(full);
                if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(full, json);
                Log("log written " + full);
            }
            catch (Exception e) { Debug.LogWarning("HELL_AUTO could not write the log " + _logPath + ": " + e.Message); }
            Debug.Log(_report.SummaryLine());
            if (!Application.isEditor) Application.Quit(why == "finished" && _report.Skipped == 0 ? 0 : 2);
        }

        // ------------------------------------------------------------------ the Rewind edge cases

        List<string> _cases;
        bool _caseRunning, _caseWantsPick, _aimingAfterRewind;
        int _rewoundFrame = -1;
        float _deathAt = -100f, _carrySince = -1f, _nextCaseAt;
        readonly List<float> _liveCounts = new List<float>();
        float _liveSampleAt;

        void TickCases()
        {
            if (_cases == null || _player == null) return;
            // the live bots over the last 6 s (for "more bots after the Rewind than before")
            if (Time.time >= _liveSampleAt)
            {
                _liveSampleAt = Time.time + 0.25f;
                _liveCounts.Add(LiveBots());
                if (_liveCounts.Count > 26) _liveCounts.RemoveAt(0);
            }
            var lantern = _room != null ? _room.Objective as LanternCarry : null;
            if (lantern != null && lantern.Carrying) { if (_carrySince < 0f) _carrySince = Time.time; }
            else _carrySince = -1f;
            if (_caseRunning || _cases.Count == 0) return;
            if (HellSkillPick.Open && _cases.Contains("during-pick")) { StartCoroutine(CasePick()); return; }
            if (Time.time < _nextCaseAt) return;
            var rw = _abilities != null ? _abilities.Rewind : null;
            if (rw == null || !rw.Cooldown.Ready || director.Run == null || director.Run.Phase != HellRunPhase.InRoom || _room == null) return;
            float clock = _room.Clock;
            var rec = _report.Current;
            if (_cases.Contains("room-first-second") && rec != null && rec.index >= 1 && clock > 0.6f && clock < 1.2f) { StartCoroutine(CaseRewind("room-first-second", null)); return; }
            if (_cases.Contains("lantern-carry") && _carrySince > 0f && Time.time - _carrySince > 1.5f) { StartCoroutine(CaseRewind("lantern-carry", null)); return; }
            if (_cases.Contains("during-collapse") && _room.kind == HellRoomKind.Collapse && clock > 5f) { StartCoroutine(CaseRewind("during-collapse", null)); return; }
            if (_cases.Contains("mid-shadow-step") && _abilities.StepReady && clock > 6f && _room.kind != HellRoomKind.Collapse && _step == StepPhase.None && Sim != null && Sim.Grounded)
            { StartCoroutine(CaseRewind("mid-shadow-step", "step")); return; }
            if (_cases.Contains("across-death") && rec != null && rec.index >= 1 && clock > 8f && _room.kind != HellRoomKind.Collapse && !_cases.Contains("room-first-second"))
            { StartCoroutine(CaseDeath()); return; }
        }

        int LiveBots()
        {
            int n = 0;
            foreach (var b in SkylineBot.Live) if (b != null && b.Alive && b.isActiveAndEnabled) n++;
            return n;
        }

        int DoubledBots()
        {
            int n = 0;
            var l = SkylineBot.Live;
            for (int i = 0; i < l.Count; i++)
            {
                var a = l[i];
                if (a == null || !a.Alive || !a.isActiveAndEnabled) continue;
                for (int j = i + 1; j < l.Count; j++)
                {
                    var b = l[j];
                    if (b == null || !b.Alive || !b.isActiveAndEnabled || b.Def != a.Def) continue;
                    if (Vector3.Distance(a.transform.position, b.transform.position) < 0.3f) n++;
                }
            }
            return n;
        }

        HellRewindObservation Observe(string id)
        {
            var o = new HellRewindObservation { caseId = id, maxLiveBotsSeen = -1 };
            o.clockBefore = _room != null ? _room.Clock : 0f;
            o.progressBefore = _room != null && _room.Objective != null ? _room.Objective.Progress : 0f;
            o.liveBotsBefore = LiveBots();
            float mx = o.liveBotsBefore;
            foreach (var c in _liveCounts) mx = Mathf.Max(mx, c);
            o.maxLiveBotsSeen = (int)mx;
            var rw = _abilities != null ? _abilities.Rewind : null;
            o.rewindSeconds = rw != null ? Mathf.Min(rw.seconds, (float)rw.HistorySeconds) : 4f;
            return o;
        }

        void ObserveAfter(ref HellRewindObservation o)
        {
            o.clockAfter = _room != null ? _room.Clock : 0f;
            o.progressAfter = _room != null && _room.Objective != null ? _room.Objective.Progress : 0f;
            o.liveBotsAfter = LiveBots();
            o.doubledBots = DoubledBots();
            var lantern = _room != null ? _room.Objective as LanternCarry : null;
            o.objectiveCarrying = lantern != null && lantern.Carrying;
            o.flagCarrying = HellRoomFlags.CarryingLantern;
            o.playerInsideSolid = InsideSolid();
            o.playerOutsideRoom = _room != null && !HellRoomLint.Contains(Grow(_room.Def.bounds, 2f, 6f), N(_player.transform.position));
            o.timeScaleAfter = Time.timeScale;
            o.pickOpenAfter = HellSkillPick.Open;
        }

        static CoverBox Grow(CoverBox b, float side, float up) => new CoverBox
        {
            min = new NVec3(b.min.X - side, b.min.Y - side - 4f, b.min.Z - side),
            max = new NVec3(b.max.X + side, b.max.Y + up, b.max.Z + side),
        };

        bool InsideSolid()
        {
            var sim = Sim;
            float radius = sim != null ? sim.def.radius * 0.85f : 0.3f;
            float height = sim != null ? sim.Height : 1.8f;
            var feet = _player.transform.position;
            var a = feet + Vector3.up * (radius + 0.15f);
            var b = feet + Vector3.up * Mathf.Max(radius + 0.15f, height - radius - 0.1f);
            var hits = Physics.OverlapCapsule(a, b, radius, WorldMask, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
                if (h != null && !h.transform.IsChildOf(_player.transform)) return true;
            return false;
        }

        void CaseDone(string id, HellRewindObservation o, string extra)
        {
            _cases.Remove(id);
            var problems = HellRewindChecks.Problems(o);
            string line = "rewind " + id + " " + RoomTag() + (problems.Count == 0 ? " ok" : " PROBLEMS: " + string.Join("; ", problems)) +
                " (rewound " + o.rewound + ", clock " + o.clockBefore.ToString("0.00") + " -> " + o.clockAfter.ToString("0.00") + ", bots " + o.liveBotsBefore + " -> " + o.liveBotsAfter + (extra != null ? ", " + extra : "") + ")";
            Log(line);
            _report.rewindCases.Add(line);
            foreach (var p in problems) _report.rewindProblems.Add(id + " " + RoomTag() + ": " + p);
            _caseRunning = false;
            _nextCaseAt = Time.time + 1f;
        }

        IEnumerator WaitUnscaled(float s)
        {
            float until = Time.unscaledTime + s;
            while (Time.unscaledTime < until) yield return null;
        }

        IEnumerator CaseRewind(string id, string variant)
        {
            _caseRunning = true;
            string extra = null;
            if (variant == "step")
            {
                // aim a Shadow Step at the floor ahead, and Rewind while still aiming
                Keys.Hold(KeyAction.ShadowStep, true);
                yield return WaitUnscaled(0.45f);
                extra = "aiming before " + (_abilities != null && _abilities.Aiming);
            }
            var o = Observe(id);
            o.expectRewind = true;
            int before = _rewoundFrame;
            yield return PressRewind();
            o.rewound = _rewoundFrame != before;
            o.aimingAfter = o.rewound && _aimingAfterRewind;
            if (variant == "step")
            {
                bool steppedBefore = _stepped;
                _stepped = false;
                yield return WaitUnscaled(0.2f);
                Keys.Hold(KeyAction.ShadowStep, false);
                yield return WaitUnscaled(0.2f);
                extra += ", a step on release after the Rewind: " + _stepped;
                _stepped = steppedBefore;
            }
            yield return WaitUnscaled(0.15f);
            ObserveAfter(ref o);
            CaseDone(id, o, extra);
        }

        IEnumerator PressRewind()
        {
            Keys.Tap(KeyAction.Rewind);
            int until = Time.frameCount + 4;
            int start = _rewoundFrame;
            while (Time.frameCount < until && _rewoundFrame == start) yield return null;
            if (_rewoundFrame == start && Keys.Check("rewind") && _abilities != null && _abilities.Rewind.Cooldown.Ready && !HellSkillPick.Open)
            {
                // the virtual key was not read: the same path, called directly
                Keys.MarkBroken("the Rewind key was not read");
                Keys.FallbackRewind(_abilities);
                yield return null;
            }
        }

        IEnumerator CaseDeath()
        {
            _caseRunning = true;
            float diedAt = _deathAt;
            Log("rewind across-death: the script ends this life " + RoomTag());
            _player.TakeAttack(100000f, "autopilot-rewind-case", _player.transform.position + _player.transform.forward);
            float until = Time.unscaledTime + 3f;
            while (_deathAt == diedAt && Time.unscaledTime < until) yield return null;
            if (_deathAt == diedAt) { _cases.Remove("across-death"); _caseRunning = false; Log("rewind across-death: no restart seen"); yield break; }
            yield return WaitUnscaled(0.6f);
            var o = Observe("across-death");
            o.expectRewind = true;   // 0.6 s of history since the restart: it goes back that far and no farther
            int before = _rewoundFrame;
            yield return PressRewind();
            o.rewound = _rewoundFrame != before;
            yield return WaitUnscaled(0.15f);
            ObserveAfter(ref o);
            CaseDone("across-death", o, "history " + o.rewindSeconds.ToString("0.00") + " s");
        }

        IEnumerator CasePick()
        {
            _caseRunning = true;
            _caseWantsPick = true;
            yield return WaitUnscaled(0.8f);
            var o = Observe("during-pick");
            o.expectRewind = false;
            int before = _rewoundFrame;
            Keys.Tap(KeyAction.Rewind);
            yield return WaitUnscaled(0.3f);
            o.rewound = _rewoundFrame != before;
            ObserveAfter(ref o);
            _caseWantsPick = false;
            // after the pick closes: time runs again, and no Rewind went off late
            float until = Time.unscaledTime + 6f;
            while (HellSkillPick.Open && Time.unscaledTime < until) yield return null;
            yield return WaitUnscaled(0.3f);
            if (_rewoundFrame != before) o.rewound = true;
            o.timeScaleAfter = Time.timeScale;
            o.pickOpenAfter = HellSkillPick.Open;
            CaseDone("during-pick", o, null);
        }

        // ------------------------------------------------------------------ helpers

        MoveSim Sim => _player != null && _player.Movement != null ? _player.Movement.Sim : null;

        float Range(float a, float b) => a + (float)_rng.NextDouble() * (b - a);

        Vector2 Local(Vector3 worldDir)
        {
            float y = _player.Yaw * Mathf.Deg2Rad;
            var fwd = new Vector3(Mathf.Sin(y), 0f, Mathf.Cos(y));
            var right = new Vector3(Mathf.Cos(y), 0f, -Mathf.Sin(y));
            var v = new Vector2(Vector3.Dot(worldDir, right), Vector3.Dot(worldDir, fwd));
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        static bool Near(Vector3 feet, Vector3 point, float r) => Flat(point - feet).magnitude <= r && Mathf.Abs(point.y - feet.y) < 2.2f;
        static bool Near(Vector3 feet, NVec3 point, float r) => Near(feet, HellRoom.U(point), r);
        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
        static float Cross(Vector3 a, Vector3 b) => a.x * b.z - a.z * b.x;
        static NVec3 N(Vector3 v) => new NVec3(v.x, v.y, v.z);
        static Vector3 U(NVec3 v) => new Vector3(v.X, v.Y, v.Z);

        // ------------------------------------------------------------------ the virtual keyboard

        enum KeyAction { ShadowStep, Rewind }

        /// <summary>A keyboard of our own, added to the Input System: the autopilot's Shadow Step, Rewind, card and Enter presses
        /// go through the game's bindings exactly as a player's keys do. If a press is not read (the device was reset, focus
        /// rules), it says so once and the same game code is called directly.</summary>
        static class Keys
        {
            static Keyboard _kb;
            static readonly HashSet<Key> _held = new HashSet<Key>();
            static readonly List<Key> _tapRelease = new List<Key>();
            static bool _dirty;
            public static bool Broken;
            static bool _warned;

            public static void Setup()
            {
                try
                {
                    _kb = InputSystem.AddDevice<Keyboard>("HellAutopilotKeyboard");
                    Debug.Log("HELL_AUTO keys: a virtual keyboard is added (" + _kb.name + ")");
                }
                catch (Exception e)
                {
                    MarkBroken("no virtual keyboard: " + e.Message);
                }
            }

            public static void MarkBroken(string why)
            {
                if (Broken) return;
                Broken = true;
                Debug.LogWarning("HELL_AUTO keys fallback: " + why + "; the abilities are called directly from now on");
            }

            /// <summary>Is the virtual keyboard the one the game reads? (Something else was used: it isn't.)</summary>
            public static bool Check(string what)
            {
                if (Broken || _kb == null) return false;
                if (Keyboard.current != _kb && !_warned)
                {
                    _warned = true;
                    Debug.LogWarning("HELL_AUTO keys: another keyboard is current (" + (Keyboard.current != null ? Keyboard.current.name : "none") + ") at " + what + "; taking it back");
                }
                return true;
            }

            static Key KeyFor(KeyAction a)
            {
                var action = a == KeyAction.ShadowStep ? BindAction.ShadowStep : BindAction.Rewind;
                string binding = SkylineBindings.Table.Get(BindDevice.KeyboardMouse, action);
                foreach (var p in BindingTable.Split(binding))
                {
                    if (!p.StartsWith("<Keyboard>/", StringComparison.Ordinal) || _kb == null) continue;
                    var c = _kb.TryGetChildControl<KeyControl>(p.Substring("<Keyboard>/".Length));
                    if (c != null) return c.keyCode;
                }
                return a == KeyAction.ShadowStep ? Key.E : Key.V;
            }

            public static void Hold(KeyAction a, bool down) => Set(KeyFor(a), down);
            public static void Tap(KeyAction a) => Tap(KeyFor(a));

            public static void Tap(Key k)
            {
                Set(k, true);
                _tapRelease.Add(k);
                _tapFrames.Add(Time.frameCount);
            }

            static void Set(Key k, bool down)
            {
                if (down ? _held.Add(k) : _held.Remove(k)) _dirty = true;
            }

            public static void ReleaseAll()
            {
                _tapRelease.Clear();
                _tapFrames.Clear();
                if (_held.Count == 0) return;
                _held.Clear();
                _dirty = true;
                Flush();
            }

            /// <summary>Once a frame, late (after the autopilot's Update and its coroutines): release the taps of earlier frames, and
            /// send the keys held now. A tap is thus down for exactly one of the game's frames.</summary>
            public static void Tick() => Flush();

            static readonly List<int> _tapFrames = new List<int>();

            static void Flush()
            {
                if (_kb == null || !_kb.added) return;
                for (int i = _tapRelease.Count - 1; i >= 0; i--)
                {
                    if (_tapFrames[i] >= Time.frameCount) continue;
                    Set(_tapRelease[i], false);
                    _tapRelease.RemoveAt(i);
                    _tapFrames.RemoveAt(i);
                }
                if (!_dirty) return;
                _dirty = false;
                if (_held.Count > 0) _kb.MakeCurrent();
                var keys = new Key[_held.Count];
                _held.CopyTo(keys);
                InputSystem.QueueStateEvent(_kb, new KeyboardState(keys));
            }

            // ---- the direct fallbacks (the same code the keys reach)

            static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

            public static void FallbackRewind(HellAbilities a)
            {
                if (a == null || a.Rewind == null) return;
                if (a.Rewind.TryRewind()) typeof(HellAbilities).GetMethod("OnRewound", Private)?.Invoke(a, null);
            }

            public static void FallbackStep(HellAbilities a)
            {
                if (a == null) return;
                var m = typeof(HellAbilities).GetMethod("TickShadowStep", Private);
                if (m == null) return;
                m.Invoke(a, new object[] { true });
                m.Invoke(a, new object[] { false });
            }

            public static void FallbackPick(SkylinePlayer p, int card)
            {
                if (p == null || !HellSkillPick.Open) return;
                MarkBroken("the card key was not read");
                var pick = p.GetComponent<HellSkillPick>();
                typeof(HellSkillPick).GetMethod("Pick", Private)?.Invoke(pick, new object[] { card });
            }
        }
    }
}
