using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The game, title to title (docs/HELL-FLOW.md, docs/HELL-ROOMS.md). It drives the pure <see cref="HellFlow"/>: the title
    /// screen, the opening's cards, a card per floor, the rooms, the boss hook, the ending hook, the credits, the stats, and
    /// back to the title (the scene reloads). The rooms part: it deals the run's deck (<see cref="HellDeck"/>, from
    /// <see cref="seed"/>; 0 = a new seed each run), drops the player into each dealt room in turn, restarts the room on a
    /// death, moves the player from a room's drop hole into the next room's vestibule (from above), and after a floor's fourth
    /// room down the floor's shaft; at its bottom raises <see cref="HellRunEvents.FloorCleared"/> and waits for
    /// <see cref="HellRunEvents.ContinueAfterPick"/> (the skill pick). The last floor's shaft raises
    /// <see cref="HellRunEvents.FinalBossRequested"/> (then <see cref="HellRunEvents.EndingRequested"/>); with no listeners the
    /// credits roll. The 18 rooms are built in the scene once (<see cref="HellWorld"/>); only the current and the previous room
    /// are active. It keeps <see cref="HellRunStats.Current"/> (kills, Rewinds, Shadow Steps, fast paths, times) and is
    /// rewindable for the counters the world can take back. <c>-hellSkipTitle</c> (or a scripted run) goes straight into the
    /// first room, without the title or any card.
    /// </summary>
    public sealed class HellRunDirector : MonoBehaviour, IHellRewindable
    {
        public SkylinePlayer player;
        /// <summary>The run's seed (0: a fresh one every run). <c>-hellSeed N</c> on the command line overrides it.</summary>
        public int seed;

        public HellFlow Flow { get; private set; }
        public HellRun Run => Flow != null ? Flow.Run : null;
        public HellRoom Current { get; private set; }
        public HellScreens Screens { get; private set; }

        readonly Dictionary<long, HellRoom> _rooms = new Dictionary<long, HellRoom>();
        readonly HellHudLine _hud = new HellHudLine();
        readonly HellSnapPool<HellRunStats.World> _snaps = new HellSnapPool<HellRunStats.World>();
        HellRoom _previous;
        HellFastPathTracker _fast;
        /// <summary>Speed carried through the drops (docs/HELL-ROOMS.md, "Momentum through the drops").</summary>
        readonly HellMomentum _momentum = new HellMomentum();
        HellAbilities _stepSource;
        bool _skipScreens;
        int _chapter = -1, _chapterPicks, _chapterPicked;
        float _pickWait;
        float _lineAt = -1f;   // the objective line is re-read at most every LineEvery s (the objectives build their text on read)
        const float LineEvery = 0.2f;

        static long Key(int floor, HellRoomKind kind) => floor * 100L + (int)kind;

        void OnEnable()
        {
            HellRewindRegistry.Register(this);
            HellRunEvents.FinalBossDefeated += OnBossDefeated;
            HellRunEvents.EndingReached += OnEndingReached;
            SkylineBot.Wrecked += OnBotWrecked;
            HellRewind.Rewound += OnRewound;
        }

        void OnDisable()
        {
            HellRewindRegistry.Unregister(this);
            HellRunEvents.FinalBossDefeated -= OnBossDefeated;
            HellRunEvents.EndingReached -= OnEndingReached;
            SkylineBot.Wrecked -= OnBotWrecked;
            HellRewind.Rewound -= OnRewound;
            if (_stepSource != null) _stepSource.Stepped -= OnStepped;
            _stepSource = null;
        }

        void Start()
        {
            if (player == null) player = FindFirstObjectByType<SkylinePlayer>();
            foreach (var r in FindObjectsByType<HellRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                _rooms[Key(r.floor, r.kind)] = r;
                r.gameObject.SetActive(false);
            }
            if (player != null) player.Wrecked += OnWrecked;

            Flow = new HellFlow(MakeRun)
            {
                BossHook = HellRunEvents.RaiseFinalBossRequested,
                EndingHook = HellRunEvents.RaiseEndingRequested,
            };
            Flow.StageChanged += OnStage;
            if (HellRunEvents.Stage != HellFlowStage.Title) HellRunEvents.RaiseStageChanged(HellRunEvents.Stage, HellFlowStage.Title);
            Flow.RewindBoundary += HellRunEvents.RaiseRewindBoundary;

            _skipScreens = SkylineRunArgs.Demo || SkylineRunArgs.Has("-hellSkipTitle") || SkylineRunArgs.Has("-hell-replay") || SkylineRunArgs.Has("-hell-perf") || player == null || player.ScriptedInput != null;
            if (!_skipScreens)
            {
                PanelSettings ps = null;
                var hud = SkylineHud.Instance != null ? SkylineHud.Instance : FindFirstObjectByType<SkylineHud>();
                if (hud != null)
                {
                    var doc = hud.GetComponent<UIDocument>();
                    if (doc != null) ps = doc.panelSettings;
                }
                Screens = HellScreens.Create(ps);
                Screens.Done += OnScreenDone;
                Screens.ShowTitle();
                Debug.Log("HELL_FLOW title");
            }
            else
            {
                Debug.Log("HELL_FLOW screens skipped (scripted run or -hellSkipTitle)");
                Flow.StartGame();
                Flow.OpeningDone();
            }
        }

        void OnDestroy()
        {
            if (player != null) player.Wrecked -= OnWrecked;
            if (Screens != null) Destroy(Screens.gameObject);
        }

        HellRun MakeRun()
        {
            ulong s = (ulong)(seed != 0 ? seed : 0);
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-hellSeed" && ulong.TryParse(args[i + 1], out var parsed)) s = parsed;
            if (s == 0) s = Rng.TimeSeed();
            // floor 1 is the curated opening deck (a judge's first minutes; the Soul Lantern pinned); -hellRandomDeck deals it
            var run = new HellRun(HellDeck.ForRun(s, !SkylineRunArgs.Has("-hellRandomDeck")));
            // subscribed before the flow's own handlers: the room is live before the floor card shows
            run.RoomEntered += OnRoomEntered;
            run.FloorCleared += f => Debug.Log("HELL_RUN floor " + f + " cleared");
            run.Finished += OnRunFinished;
            Debug.Log("HELL_RUN start seed " + s + " deck " + DeckText(run));
            return run;
        }

        static string DeckText(HellRun run)
        {
            var sb = new System.Text.StringBuilder();
            for (int f = 0; f < run.Deck.Length; f++)
            {
                sb.Append(f == 0 ? "" : " | ");
                for (int i = 0; i < run.Deck[f].Length; i++) sb.Append(i == 0 ? "" : ", ").Append(run.Deck[f][i]);
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ the flow

        void OnStage(HellFlowStage from, HellFlowStage to)
        {
            Debug.Log("HELL_FLOW " + from + " -> " + to);
            HellRunEvents.RaiseStageChanged(from, to);
            if (from == HellFlowStage.Opening && to != HellFlowStage.Title) HellRunEvents.RaiseRunStarted();
            switch (to)
            {
                case HellFlowStage.Opening:
                    if (Screens != null) Screens.ShowOpening(HellFlowText.Opening);
                    else Flow.OpeningDone();
                    break;
                case HellFlowStage.FloorIntro:
                    HellRunEvents.RaiseFloorStarted(Flow.IntroFloor);
                    if (Screens != null) Screens.ShowFloorCard(Flow.IntroFloor);
                    else
                    {
                        _hud.Banner(HellFlowText.Floor(Flow.IntroFloor).name, 3f);
                        Flow.FloorIntroDone();
                    }
                    break;
                case HellFlowStage.Playing:
                case HellFlowStage.Boss:
                case HellFlowStage.Ending:
                    if (Screens != null) Screens.Hide();
                    break;
                case HellFlowStage.Credits:
                    if (Current != null) Current.End();
                    if (Screens != null) Screens.ShowCredits();
                    else
                    {
                        Debug.Log("HELL_FLOW credits (no screens): ending " + Flow.Stats.EndingId);
                        Flow.CreditsDone();       // a scripted run stops on the stats (logged), never reloads
                    }
                    break;
                case HellFlowStage.Stats:
                    if (Screens != null) Screens.ShowStats(Flow.Stats, Flow.Stats.EndingId);
                    LogStats();
                    break;
                case HellFlowStage.Title:
                    // back to the title: a fresh scene (every room, bot and hazard as built)
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                    break;
                case HellFlowStage.Quit:
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        void OnScreenDone(HellScreens.Screen screen, int choice)
        {
            switch (screen)
            {
                case HellScreens.Screen.Title:
                    // the Vesper chapter's answer stands in for the jobs it skips; every other choice clears it
                    if (choice == HellScreens.ChoiceChapter + HellFlow.VesperChapter && Screens != null && Screens.VesperErrands.HasValue)
                        HellJobs.AssumeErrands(Screens.VesperErrands.Value);
                    else HellJobs.ClearAssumedErrands();
                    if (choice == HellScreens.ChoiceStart) { Flow.StartGame(); HellRunEvents.RaiseChapterStarted(HellFlow.FullGame); }
                    else if (choice == HellScreens.ChoiceQuit) Flow.Quit();
                    else if (choice >= HellScreens.ChoiceChapter) BeginChapter(choice - HellScreens.ChoiceChapter);
                    break;
                case HellScreens.Screen.Opening: Flow.OpeningDone(); break;
                case HellScreens.Screen.FloorCard: Flow.FloorIntroDone(); break;
                case HellScreens.Screen.Credits: Flow.CreditsDone(); break;
                case HellScreens.Screen.Stats: Flow.StatsDone(); break;
            }
        }

        /// <summary>A chapter from the title: first the skill picks the skipped floors would have given (one per floor
        /// cleared before it, through the normal pick: <see cref="HellRunEvents.FloorCleared"/>), then the chapter.</summary>
        void BeginChapter(int chapter)
        {
            _chapter = chapter;
            _chapterPicks = Mathf.Min(chapter, HellRoomVariants.FloorCount - 1);
            _chapterPicked = 0;
            Debug.Log("HELL_FLOW chapter " + chapter + " (" + _chapterPicks + " picks first)");
            if (Screens != null) Screens.Hide();
            TickChapter();
        }

        void TickChapter()
        {
            if (_chapter < 0 || Flow.Stage != HellFlowStage.Title || HellRunEvents.WaitingForPick) return;
            if (_chapterPicked < _chapterPicks)
            {
                int f = _chapterPicked++;
                if (HellRunEvents.RaiseFloorCleared(f)) return;     // the pick is open: wait for it to close
                _chapterPicked = _chapterPicks;                     // nobody picks: no skills to grant
            }
            int c = _chapter;
            _chapter = -1;
            Flow.StartChapter(c);
            HellRunEvents.RaiseChapterStarted(c);
        }

        void OnBossDefeated()
        {
            if (Flow != null && Flow.BossDefeated()) Debug.Log("HELL_FLOW boss defeated");
        }

        void OnEndingReached(string id)
        {
            if (Flow != null && Flow.ReachEnding(id)) Debug.Log("HELL_FLOW ending " + id);
        }

        void LogStats()
        {
            var s = Flow.Stats;
            Debug.Log("HELL_STATS time " + HellRunStats.Clock(s.TotalSeconds) + " floors " + HellRunStats.Clock(s.FloorSeconds[0]) + " / "
                + HellRunStats.Clock(s.FloorSeconds[1]) + " / " + HellRunStats.Clock(s.FloorSeconds[2]) + " boss " + HellRunStats.Clock(s.BossSeconds)
                + " deaths " + s.Deaths + " rewinds " + s.Rewinds + " steps " + s.ShadowSteps + " kills " + s.EnemiesKilled
                + " fast " + s.FastPathsTaken + "/" + s.FastPathsOffered + " jobs " + s.JobsDone + "/" + s.JobsOffered + " ending " + s.EndingId);
        }

        // ------------------------------------------------------------------ the rooms

        void OnRoomEntered(int floor, int index)
        {
            var kind = Run.CurrentKind;
            if (!_rooms.TryGetValue(Key(floor, kind), out var room))
            {
                Debug.LogError("HELL_RUN no room built for floor " + floor + " " + kind + ": rebuild the Hell scene");
                return;
            }
            if (_previous != null && _previous != Current) _previous.gameObject.SetActive(false);
            if (Current != null) Current.End();
            _previous = Current;
            Current = room;
            room.gameObject.SetActive(true);
            room.Begin(player);
            var d = room.Def;
            _fast = new HellFastPathTracker(d.safePath, d.fastPath);
            // you keep your speed through the drop: rotated into this room's entry direction, you land running
            if (floor == 0 && index == 0) _momentum.Reset();
            var carried = HellRoom.U(_momentum.Arrive(d.entryYaw));
            player.Movement.Teleport(HellRoom.U(d.entry) + Vector3.up * HellWorld.DropInHeight, d.entryYaw, carried);
            if (carried.sqrMagnitude > 0f) Debug.Log("HELL_RUN carried " + carried.magnitude.ToString("0.0") + " m/s into the room");
            player.SetAim(d.entryYaw, 0f);
            Debug.Log("HELL_RUN room " + (floor + 1) + "." + (index + 1) + " " + kind);
            HellRunEvents.RaiseRoomEntered(floor, index, kind.ToString());
        }

        void OnWrecked()
        {
            if (Run == null || player == null || Flow == null) return;
            string where = Flow.Stage == HellFlowStage.Boss ? "boss" : Flow.Stage == HellFlowStage.Ending ? "ending"
                : Run.Phase == HellRunPhase.InRoom && Current != null ? Current.kind.ToString() : "shaft";
            HellRunEvents.RaisePlayerDied(Run.Floor, where);
            if (Flow.Stage == HellFlowStage.Boss || Flow.Stage == HellFlowStage.Ending)
            {
                // the boss and ending lanes put the player back themselves (player.Wrecked); the run only counts it
                Flow.Stats.RecordDeath();
                return;
            }
            if (Run.Phase == HellRunPhase.InRoom && Current != null)
            {
                Flow.Died();
                Current.Restart();
                _momentum.Reset();
                if (_fast != null) _fast.Reset();
                player.Restart(HellRoom.U(Current.Def.entry), Current.Def.entryYaw);
                _hud.Banner("AGAIN", 1.5f);
                Debug.Log("HELL_RUN died in " + Current.kind + " (deaths " + Run.Deaths + ")");
                HellRunEvents.RaiseRoomRestarted();
            }
            else player.Restart(HellRoom.U(HellWorld.ShaftCentre(Run.Floor)) + Vector3.up * (HellWorld.ShaftBottom + 0.1f), 0f);
        }

        void OnRunFinished()
        {
            if (Current != null) Current.End();
            _hud.SetLine("");
            Debug.Log("HELL_RUN finished, deaths " + Run.Deaths);
            HellRunEvents.RaiseRunFinished();
        }

        void Update()
        {
            if (!_hud.Built && SkylineHud.Instance != null && SkylineHud.Instance.Safe != null) _hud.Build(SkylineHud.Instance.Safe);
            _hud.Tick();
            HookSteps();
            if (_chapter >= 0 && Flow != null) TickChapter();
            if (Run == null || player == null || Flow == null) return;
            bool held = SkylineDeviceInput.Paused || SkylinePauseMenu.IsOpen || (Screens != null && Screens.Visible);
            if (!held && Run.Phase != HellRunPhase.AwaitingPick) Flow.Tick(Time.unscaledDeltaTime);
            if (Flow.Stage != HellFlowStage.Playing) { _hud.SetLine(""); return; }   // the boss and the ending own the objective slot
            var feet = player.transform.position;
            switch (Run.Phase)
            {
                case HellRunPhase.InRoom:
                    if (Current != null)
                    {
                        float c = Current.Clock;
                        if (Time.unscaledTime >= _lineAt) { _lineAt = Time.unscaledTime + LineEvery; _hud.SetLine(Current.Objective.Line); }
                        _hud.SetClock(c);
                        if (_fast != null && !held) _fast.Observe(HellRoom.N(feet));
                        if (!held && player.Movement != null && player.Movement.Sim != null) _momentum.Tick(Time.deltaTime, player.Movement.Sim.HorizontalSpeed);
                        if (Current.InDrop(feet) && Run.ReachedDrop())
                        {
                            bool taken = _fast != null && _fast.Taken;
                            Flow.Stats.RecordFastPath(taken);
                            Debug.Log("HELL_RUN fast path " + (taken ? "taken" : "not taken") + " in " + Current.kind
                                + (_fast != null ? " (" + _fast.Visited + "/" + _fast.OwnSamples + ")" : ""));
                            if (Run.Phase == HellRunPhase.Dropping) Run.Landed();
                            else EnterShaft();
                        }
                    }
                    break;
                case HellRunPhase.Shaft:
                    if (HellRoomLint.Contains(HellWorld.ShaftBottomZone(Run.Floor), HellRoom.N(feet)))
                    {
                        Run.ShaftBottom();            // the last floor: Finished, and the flow moves on to the boss
                        if (Run.Phase == HellRunPhase.AwaitingPick)
                        {
                            _hud.SetLine("");
                            _pickWait = 0f;
                            if (!HellRunEvents.RaiseFloorCleared(Run.Floor)) Run.Continue();
                        }
                    }
                    break;
                case HellRunPhase.AwaitingPick:
                    _pickWait += Time.unscaledDeltaTime;
                    if (!HellRunEvents.WaitingForPick) Run.Continue();
                    break;
            }
        }

        void EnterShaft()
        {
            if (Current != null) Current.End();
            Flow.EnteredShaft();
            player.Movement.Teleport(HellRoom.U(HellWorld.ShaftArrival(Run.Floor)), 0f, Vector3.zero);
            _hud.SetLine("DOWN");
            _hud.SetClock(-1f);
            Debug.Log("HELL_RUN shaft of floor " + Run.Floor);
        }

        // ------------------------------------------------------------------ the stats' sources

        void HookSteps()
        {
            var a = HellAbilities.Instance;
            if (a == _stepSource) return;
            if (_stepSource != null) _stepSource.Stepped -= OnStepped;
            _stepSource = a;
            if (a != null) a.Stepped += OnStepped;
        }

        void OnStepped(Vector3 from, Vector3 to)
        {
            if (InRun) Flow.Stats.RecordShadowStep();
            HellRunEvents.RaiseShadowStepUsed(from, to);
        }

        void OnRewound()
        {
            if (InRun) Flow.Stats.RecordRewind();
            HellRunEvents.RaiseRewindUsed();
        }
        void OnBotWrecked(SkylineBot bot) { if (InRun) Flow.Stats.RecordKill(); }

        bool InRun => Flow != null && Flow.Stats != null
            && (Flow.Stage == HellFlowStage.Playing || Flow.Stage == HellFlowStage.Boss || Flow.Stage == HellFlowStage.Ending);

        // ------------------------------------------------------------------ rewind: the counters the world takes back

        public object Snapshot()
        {
            var st = HellRunStats.Current;
            var w = _snaps.Next();
            return st != null ? st.SaveWorld(w) : w;
        }

        public void Restore(object snapshot)
        {
            var st = HellRunStats.Current;
            if (st != null) st.RestoreWorld(snapshot as HellRunStats.World);
        }
    }
}
