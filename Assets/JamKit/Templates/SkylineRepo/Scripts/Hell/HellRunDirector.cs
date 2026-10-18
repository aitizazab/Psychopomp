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
        public static HellRunDirector Instance { get; private set; }
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
        /// <summary>Set by the capture boot before Start: a replay of a chapter run starts that chapter instead of the full
        /// game (its picks are granted by the replayer, so none are offered here).</summary>
        [System.NonSerialized] public int replayChapter = HellFlow.FullGame;
        /// <summary><c>-hellChapter</c> starts its chapter once per launch (back at the title after the run, the title shows).</summary>
        static bool _cliChapterUsed;
        int _cliPicks;
        System.Random _cliPickRng;
        int _chapter = -1, _chapterPicks, _chapterPicked;
        float _pickWait;
        float _lineAt = -1f;   // the objective line is re-read at most every LineEvery s (the objectives build their text on read)
        const float LineEvery = 0.2f;
        float _doneFor;   // seconds the current room's objective has been done (the exit hint, HellExitHint)

        /// <summary>The pause menu's "Restart room" can act now: a room is being played (not the boss, the ending, a shaft).</summary>
        public static bool CanRestartRoom =>
            Instance != null && Instance.Flow != null && Instance.Flow.Stage == HellFlowStage.Playing && Instance.Run != null
            && Instance.Run.Phase == HellRunPhase.InRoom && Instance.Current != null && Instance.player != null;

        /// <summary>A run is on (past the title): the pause menu offers "Return to title".</summary>
        public static bool InGame => Instance != null && Instance.Flow != null && Instance.Flow.Stage != HellFlowStage.Title
            && Instance.Flow.Stage != HellFlowStage.Quit;

        /// <summary>The pause menu's "Return to title" (the creator, 2026-10-18): the same fresh scene as the end of a run
        /// (the Title stage reloads it), with what outlives a scene load cleared first: the time scale and its requests, the
        /// Rewind's history, the jobs' ledger and the Vesper chapter's assumed answer, and the music (its director survives
        /// scene loads; a fresh one boots with the scene and follows the title).</summary>
        public static void ReturnToTitle()
        {
            Debug.Log("HELL_FLOW return to title (pause menu)");
            Time.timeScale = 1f;
            AudioListener.pause = false;
            HellTime.ClearAll();
            if (HellRewind.Instance != null) HellRewind.Instance.ClearHistory();
            HellJobs.ResetRun();
            HellJobs.ClearAssumedErrands();
            var music = FindAnyObjectByType<HellMusicDirector>();
            if (music != null) Destroy(music.gameObject);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>The pause menu's "Restart room" (the softlock safety net, 2026-10-18): the current room starts over as after a
        /// death, back at its entry, but no death is counted. False when no room is being played.</summary>
        public static bool RestartRoom()
        {
            if (!CanRestartRoom) return false;
            var d = Instance;
            d.Current.Restart();
            d._momentum.Reset();
            if (d._fast != null) d._fast.Reset();
            d._doneFor = 0f;
            d.player.Restart(HellRoom.U(d.Current.Def.entry), d.Current.Def.entryYaw);
            d._hud.Banner("RESTART", 1.2f);
            Debug.Log("HELL_RUN restarted " + d.Current.kind + " from the pause menu (no death counted)");
            HellRunEvents.RaiseRoomRestarted();
            return true;
        }

        static long Key(int floor, HellRoomKind kind) => floor * 100L + (int)kind;

        void Awake() { Instance = this; }

        void OnEnable()
        {
            Instance = this;
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
                if (!StartCliChapter())
                {
                    Screens.ShowTitle();
                    Debug.Log("HELL_FLOW title");
                }
            }
            else
            {
                Debug.Log("HELL_FLOW screens skipped (scripted run or -hellSkipTitle)");
                if (replayChapter >= 0)
                {
                    _chapter = replayChapter;
                    _chapterPicks = _chapterPicked = 0;
                    Debug.Log("HELL_FLOW chapter " + replayChapter + " (a replay of a chapter run)");
                    TickChapter();
                }
                else if (StartCliChapter()) { }
                else
                {
                    Flow.StartGame();
                    Flow.OpeningDone();
                }
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
                    // the opening is a comic flip-through now (docs/HELL-COMIC.md); the screens step aside for it
                    if (Screens != null)
                    {
                        Screens.Hide();
                        HellComicPlayer.Play(HellComics.Opening(), () => Flow.OpeningDone(), true);
                    }
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

        /// <summary><c>-hellChapter</c> (docs/HELL-CAPTURE.md): the title is skipped and the chapter starts on the next frame
        /// (Update), after the recorder and the autopilot are listening; its picks are taken by a seeded choice there.</summary>
        bool StartCliChapter()
        {
            if (_cliChapterUsed) return false;
            var cli = HellChapterStart.Parse(System.Environment.GetCommandLineArgs());
            foreach (var p in cli.problems) Debug.LogWarning("HELL_FLOW " + p);
            if (!cli.Given) return false;
            _cliChapterUsed = true;
            SkylineSettings.Options.difficulty = (int)cli.difficulty;   // in memory: the saved options are untouched
            if (cli.chapter == HellFlow.VesperChapter) HellJobs.AssumeErrands(cli.errands);
            else HellJobs.ClearAssumedErrands();
            int s = seed;
            var v = SkylineRunArgs.Value("-hellSeed");
            if (v != null && int.TryParse(v, out var parsed)) s = parsed;
            if (s == 0) s = System.Environment.TickCount;
            _cliPickRng = new System.Random(s * 31 + 7);
            _cliPicks = HellChapterStart.PicksBefore(cli.chapter, HellRoomVariants.FloorCount);
            _chapter = cli.chapter;
            _chapterPicks = _chapterPicked = 0;
            if (Screens != null) Screens.Hide();
            Debug.Log("HELL_FLOW chapter " + cli.chapter + " from the command line (" + cli.difficulty + ", " + _cliPicks + " seeded picks"
                      + (cli.chapter == HellFlow.VesperChapter ? ", errands " + (cli.errands ? "yes" : "no") : "") + ")");
            return true;
        }

        void GrantSeededPick()
        {
            var ab = HellAbilities.Instance != null ? HellAbilities.Instance : (player != null ? player.GetComponent<HellAbilities>() : null);
            if (ab == null) { Debug.LogWarning("HELL_FLOW no abilities for the chapter's pick"); return; }
            var offer = HellSkillPool.Offer(HellSkills.All, new List<string>(ab.Owned), 3, _cliPickRng.Next);
            if (offer.Count == 0) return;
            ab.Grant(offer[_cliPickRng.Next(offer.Count)].id);
        }

        void TickChapter()
        {
            if (_chapter < 0 || Flow.Stage != HellFlowStage.Title || HellRunEvents.WaitingForPick) return;
            while (_cliPicks > 0) { _cliPicks--; GrantSeededPick(); }
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
                        if (!held) _doneFor = HellExitHint.Tick(_doneFor, Current.Objective.Done, Time.unscaledDeltaTime);
                        if (Time.unscaledTime >= _lineAt)
                        {
                            _lineAt = Time.unscaledTime + LineEvery;
                            // the safety net: done for 45 s and still here, the line points at the way out
                            _hud.SetLine(HellExitHint.Due(_doneFor) ? HellExitHint.Line : Current.Objective.Line);
                        }
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
