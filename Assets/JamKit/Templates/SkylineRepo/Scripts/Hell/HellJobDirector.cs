using JamKit.Skyline;
using UnityEngine;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The jobs in the run (docs/HELL-JOBS.md). Adds itself next to the <see cref="HellRunDirector"/> after a scene load and
    /// needs no other hook: on <see cref="HellRunEvents.RoomEntered"/> it settles the last room's job (unfinished = failed;
    /// the hourglass carried out = done), and if the new room has a job on this floor makes its <see cref="HellJobSite"/> in
    /// the room's pocket and shows the God of Death's briefing card. On a death (<see cref="HellRunEvents.RoomRestarted"/>) the
    /// job starts over with the room; when the room ends (the shaft, the run's end) the job is settled. The HUD: the card
    /// (her line, the job's name, what to do), then a tracker line (the job and the favours done so far), a result card (her
    /// reaction and the reward), and a marker that points at the pocket's door with its distance.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public sealed class HellJobDirector : MonoBehaviour
    {
        public const float CardSeconds = 7f, ResultSeconds = 5f;
        /// <summary>Coming this close to the pocket's door shows the job's instruction again (a card and a pulse of the line);
        /// it re-arms once you are <see cref="NearRearmMetres"/> further away.</summary>
        public const float NearMetres = 14f, NearRearmMetres = 10f, RemindSeconds = 4f, PulseSeconds = 1.6f;
        static readonly Color LineColour = new Color(0.85f, 0.7f, 1f);

        HellRunDirector _run;
        HellJobSite _site;
        HellRoom _siteRoom;
        bool _hudBuilt;
        VisualElement _cardBox, _markerBox, _trackBox, _trackBar, _lineBox;
        KeylinedLabel _jobLine;
        // the job's lingering instruction under the objective line (docs/HELL-JOBS.md, "The HUD"): made once per offer
        string _lineText = "", _lineShown, _shotLine;
        bool _nearShown;
        float _pulseUntil, _pulseShown = -1f;
        KeylinedLabel _cardHead, _cardLine, _cardTask, _tracker, _trackCount, _marker;
        float _cardUntil, _resultUntil, _barShown = -1f;
        // the HUD's texts are cached by value, so a frame never builds a string (docs/HELL-UI.md)
        string _trackName;
        int _trackDone = -1, _trackOffered = -1, _markerMetres = -1;
        static readonly string[,] CountText = new string[16, 16];
        static readonly string[] MarkerText = new string[1000];
        const string FavoursWord = "FAVOURS";

        static string Count(int done, int offered)
        {
            done = Mathf.Clamp(done, 0, 15);
            offered = Mathf.Clamp(offered, 0, 15);
            return CountText[done, offered] ?? (CountText[done, offered] = done + " / " + offered);
        }

        static string Marker(int metres)
        {
            metres = Mathf.Clamp(metres, 0, 999);
            return MarkerText[metres] ?? (MarkerText[metres] = "JOB  " + metres + " m");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoAttach()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Attach();
        }

        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => Attach();

        static void Attach()
        {
            var run = FindFirstObjectByType<HellRunDirector>();
            if (run != null && run.GetComponent<HellJobDirector>() == null) run.gameObject.AddComponent<HellJobDirector>();
        }

        void Awake()
        {
            _run = GetComponent<HellRunDirector>();
            HellJobs.ResetRun();
        }

        void OnEnable()
        {
            HellRunEvents.RoomEntered += OnRoomEntered;
            HellRunEvents.RoomRestarted += OnRoomRestarted;
            HellRunEvents.RunStarted += OnRunStarted;
            HellRunEvents.RunFinished += OnRunFinished;
        }

        void OnDisable()
        {
            HellRunEvents.RoomEntered -= OnRoomEntered;
            HellRunEvents.RoomRestarted -= OnRoomRestarted;
            HellRunEvents.RunStarted -= OnRunStarted;
            HellRunEvents.RunFinished -= OnRunFinished;
        }

        // ------------------------------------------------------------------ the run's events

        void OnRunStarted()
        {
            // the first room is entered just before RunStarted is raised: keep its offer
            HellJobs.ResetRun();
            if (_site != null) HellJobs.RaiseOffered(_site.Job.def);
        }

        void OnRoomEntered(int floor, int index, string kindName)
        {
            Settle(true);
            var room = _run != null ? _run.Current : null;
            if (room == null) return;
            var def = HellJobCatalog.For(room.kind, room.floor);
            if (def == null || HellJobs.Ledger.IsSettled(def.id)) return;
            var pocket = HellJobPockets.World(room.floor, room.kind);
            if (pocket == null) return;
            var go = new GameObject("Job " + def.id);
            go.transform.SetParent(room.transform, false);
            _site = go.AddComponent<HellJobSite>();
            _siteRoom = room;
            _site.Bind(room, def, pocket, room.Player != null ? room.Player : _run.player);
            _site.Raised += OnJobEvent;
            HellJobs.RaiseOffered(def);
            _lineText = HellJobLineText.Of(def.objective);
            _nearShown = false;
            ShowCard("BAJA", def.briefing, def.name.ToUpperInvariant() + ":  " + def.objective, CardSeconds);
            Voice(def, "brief");
            Debug.Log("HELL_JOB offered " + def.id + " in " + room.Def.name);
        }

        void OnRoomRestarted()
        {
            if (_site != null) _site.Restart();
        }

        void OnRunFinished() => Settle(false);

        /// <summary>The job's room is being left: settle it and take the site down.</summary>
        void Settle(bool throughDrop)
        {
            if (_site == null) return;
            var site = _site;
            _site = null;
            _siteRoom = null;
            if (!site.Job.Settled) site.Leave(throughDrop);
            Report(site.Job);
            site.Raised -= OnJobEvent;
            site.gameObject.SetActive(false);
            Destroy(site.gameObject);
        }

        void OnJobEvent(HellJobEvent ev)
        {
            if (_site == null) return;
            var def = _site.Job.def;
            if ((ev & HellJobEvent.Done) != 0)
            {
                string paid = HellJobs.RaiseDone(def);
                if (paid != null)
                {
                    ShowResult("DONE:  " + def.name.ToUpperInvariant(), def.doneLine, paid);
                    Voice(def, "done");
                }
            }
            else if ((ev & HellJobEvent.Failed) != 0)
            {
                ShowResult("FAILED:  " + def.name.ToUpperInvariant(), def.failLine, Fail(_site.Job.State.failReason));
                Voice(def, "fail");
            }
            else if ((ev & HellJobEvent.Alerted) != 0) Flash(def.verb == HellJobVerb.Destroy ? "HE'S FILING IT!" : "HE'S SEEN YOU!");
            else if ((ev & HellJobEvent.Undone) != 0) Flash("HE TOOK THE SECONDS BACK");
            else if ((ev & HellJobEvent.Reforged) != 0) Flash("THE SMITH RE-FORGED A CHAIN");
            else if ((ev & HellJobEvent.Deflected) != 0) Flash("WARDED: GET CLOSER");
        }

        /// <summary>A settled job's failure goes on the ledger when the room is left (a Rewind can still undo it until then).</summary>
        void Report(HellJob job)
        {
            if (job.Status == HellJobStatus.Failed) HellJobs.RaiseFailed(job.def, job.State.failReason);
            else if (job.Status == HellJobStatus.Done) HellJobs.RaiseDone(job.def);
        }

        /// <summary>Baja speaks the card's line: <c>hell-voice/god/job-&lt;id&gt;-&lt;part&gt;</c> (docs/HELL-VOICE-SCRIPT.md),
        /// when the file exists (else the card stays text only). A new line stops her previous one.</summary>
        static void Voice(HellJobDef def, string part) =>
            HellLinePlayer.Instance.PlayVoice(HellSpeakers.Death, "job-" + VoiceId(def.id) + "-" + part);

        /// <summary>The job's id as the voice files name it (the script's short ids).</summary>
        static string VoiceId(string id)
        {
            switch (id)
            {
                case HellJobCatalog.OverdueSoul: return "overdue";
                case HellJobCatalog.SecondsThief: return "thief";
                default: return id.Replace('_', '-');
            }
        }

        static string Fail(string why)
        {
            switch (why)
            {
                case "escaped": return "He got away.";
                case "filed": return "The ledger was filed.";
                case "left": return "You left the room.";
                default: return why;
            }
        }

        // ------------------------------------------------------------------ the HUD

        void Update()
        {
            if (!_hudBuilt && SkylineHud.Instance != null && SkylineHud.Instance.Safe != null) BuildHud(SkylineHud.Instance.Safe);
            if (!_hudBuilt) return;
            float now = Time.unscaledTime;
            var cardDisp = now < _cardUntil || now < _resultUntil ? DisplayStyle.Flex : DisplayStyle.None;
            if (_cardBox.style.display != cardDisp) _cardBox.style.display = cardDisp;
            if (_site != null && _siteRoom != null && !_siteRoom.Running) Settle(true);   // down the shaft or the run ended

            // the tracker: the job's name (or FAVOURS between jobs), a drawn progress bar, the favours done
            var job = _site != null ? _site.Job : null;
            bool live = job != null && !job.Settled;
            string name = live ? job.def.name : HellJobs.OfferedCount > 0 ? FavoursWord : "";
            if (!ReferenceEquals(name, _trackName))
            {
                _trackName = name;
                _tracker.SetText(name);
            }
            if (HellJobs.DoneCount != _trackDone || HellJobs.OfferedCount != _trackOffered)
            {
                _trackDone = HellJobs.DoneCount;
                _trackOffered = HellJobs.OfferedCount;
                _trackCount.SetText(_trackOffered > 0 ? Count(_trackDone, _trackOffered) : "");
            }
            float bar = live ? Mathf.Clamp01(job.Progress01) : -1f;
            if (bar != _barShown)
            {
                _barShown = bar;
                _trackBar.style.display = bar >= 0f ? DisplayStyle.Flex : DisplayStyle.None;
                _trackBar.MarkDirtyRepaint();
            }
            UpdateMarker(job);
            UpdateJobLine(live, now);
        }

        /// <summary>The job's instruction stays under the objective line while the job is live, and goes when it is done or
        /// failed (or the room is left). It pulses when you come near the pocket.</summary>
        void UpdateJobLine(bool live, float now)
        {
            string text = _shotLine ?? (live ? _lineText : "");
            if (!ReferenceEquals(text, _lineShown))
            {
                _lineShown = text;
                _jobLine.SetText(text);
                var disp = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
                if (_lineBox.style.display != disp) _lineBox.style.display = disp;
            }
            float pulse = now < _pulseUntil ? 0.5f + 0.5f * Mathf.Cos((_pulseUntil - now) * 9f) : 0f;
            if (pulse != _pulseShown)
            {
                _pulseShown = pulse;
                _jobLine.SetColor(Color.Lerp(LineColour, HellHudStyle.EmberHot, pulse));
            }
        }

        /// <summary>Near the pocket: the job's name and instruction on the card again, and the line pulses.</summary>
        void Remind(HellJobDef def)
        {
            if (!_hudBuilt || def == null) return;
            _cardHead.SetText(def.name.ToUpperInvariant());
            _cardLine.SetText("");
            _cardTask.SetText(def.objective);
            _resultUntil = Mathf.Max(_resultUntil, Time.unscaledTime + RemindSeconds);
            _pulseUntil = Time.unscaledTime + PulseSeconds;
        }

        void DrawTrackBar(MeshGenerationContext ctx)
        {
            if (_barShown < 0f) return;
            var p = ctx.painter2D;
            float w = _trackBar.layout.width, h = 10f;
            if (float.IsNaN(w) || w <= 0f) w = 300f;
            SkylineHudStyle.Parallelogram(p, 1f, 1f, w - 8f, h, 5f);
            p.fillColor = HellHudStyle.Plate;
            p.Fill();
            if (_barShown > 0f)
            {
                SkylineHudStyle.Parallelogram(p, 1f, 1f, (w - 8f) * _barShown, h, 5f);
                p.fillColor = HellHudStyle.Shadow;
                p.Fill();
            }
            SkylineHudStyle.Parallelogram(p, 1f, 1f, w - 8f, h, 5f);
            p.strokeColor = HellHudStyle.Ink;
            p.lineWidth = 2f;
            p.Stroke();
        }

        void UpdateMarker(HellJob job)
        {
            var player = _run != null ? _run.player : null;
            var cam = player != null ? player.Camera : null;
            bool show = job != null && !job.Settled && cam != null && _markerBox.panel != null;
            if (show)
            {
                var door = HellRoom.U(_site.Pocket.door) + Vector3.up * 2f;
                var feet = player.transform.position;
                if (HellRoomLint.Contains(_site.Pocket.area, HellRoom.N(feet + Vector3.up * 0.5f))) show = false;
                else
                {
                    var safe = _markerBox.parent != null ? _markerBox.parent.layout : new Rect(0, 0, Screen.width, Screen.height);
                    var v = cam.WorldToViewportPoint(door);
                    bool behind = v.z < 0f;
                    if (behind) { v.x = 1f - v.x; v.y = 0.02f; }
                    float x = Mathf.Clamp(v.x, 0.04f, 0.96f) * safe.width;
                    float y = Mathf.Clamp(1f - v.y, 0.08f, 0.92f) * safe.height;
                    _markerBox.style.left = x - 60f;
                    _markerBox.style.top = y - 20f;
                    int metres = Mathf.RoundToInt(Vector3.Distance(feet, door));
                    if (!_nearShown && metres <= NearMetres) { _nearShown = true; Remind(job.def); }
                    else if (_nearShown && metres > NearMetres + NearRearmMetres) _nearShown = false;
                    if (metres != _markerMetres)
                    {
                        _markerMetres = metres;
                        _marker.SetText(Marker(metres));
                    }
                }
            }
            var disp = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (_markerBox.style.display != disp) _markerBox.style.display = disp;
        }

        /// <summary>The job HUD (docs/HELL-UI.md): the tracker top left (<see cref="HellHudPlaces.JobTracker"/>), the card under
        /// it (<see cref="HellHudPlaces.JobCard"/>), the door marker in the world. Card titles in Metal Mania, her line in the
        /// project face (read at a glance), counts and the marker in Luckiest Guy. Violet: objectives and favours.</summary>
        void BuildHud(VisualElement safe)
        {
            _hudBuilt = true;
            _cardBox = SkylineHudStyle.Layer(safe, "hell-job-card");
            HellHudStyle.Place(_cardBox, HellHudPlaces.JobCard);
            _cardBox.style.height = StyleKeyword.Auto;   // the card grows with her line; the layout box is its usual size
            _cardBox.style.paddingLeft = _cardBox.style.paddingRight = 18f;
            _cardBox.style.paddingTop = _cardBox.style.paddingBottom = 12f;
            _cardBox.style.backgroundColor = HellHudStyle.Plate;
            _cardBox.style.borderLeftWidth = 6f;
            _cardBox.style.borderLeftColor = HellHudStyle.Shadow;
            _cardBox.style.borderBottomWidth = 2f;
            _cardBox.style.borderBottomColor = HellHudStyle.EmberEdge;
            _cardHead = HellHudStyle.Label(24f, HellFaces.Heading, new Color(0.78f, 0.6f, 1f), 2f, 1f);
            _cardLine = new KeylinedLabel(25f, 2f, null);
            _cardLine.SetWrap(true, TextAnchor.UpperLeft);
            _cardLine.SetItalic(true);
            _cardLine.style.marginTop = 4f;
            _cardTask = HellHudStyle.Label(24f, HellFaces.Heading, HellHudStyle.EmberHot, 2f);
            _cardTask.SetWrap(true, TextAnchor.UpperLeft);
            _cardTask.style.marginTop = 8f;
            _cardBox.Add(_cardHead);
            _cardBox.Add(_cardLine);
            _cardBox.Add(_cardTask);
            _cardBox.style.display = DisplayStyle.None;

            _trackBox = SkylineHudStyle.Layer(safe, "hell-job-tracker");
            HellHudStyle.Place(_trackBox, HellHudPlaces.JobTracker);
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.FlexEnd;
            _trackBox.Add(row);
            _tracker = HellHudStyle.Label(28f, HellFaces.Heading, new Color(0.85f, 0.7f, 1f), 2.5f, 1f);
            row.Add(_tracker);
            _trackCount = HellHudStyle.Label(24f, HellFaces.Numbers, HellHudStyle.Ash, 2f);
            _trackCount.style.marginLeft = 14f;
            row.Add(_trackCount);
            _trackBar = new VisualElement { pickingMode = PickingMode.Ignore };
            _trackBar.style.width = 300f;
            _trackBar.style.height = 14f;
            _trackBar.style.marginTop = 6f;
            _trackBar.generateVisualContent += DrawTrackBar;
            _trackBar.style.display = DisplayStyle.None;
            _trackBox.Add(_trackBar);

            _lineBox = SkylineHudStyle.Layer(safe, "hell-job-line");
            HellHudStyle.Place(_lineBox, HellHudPlaces.JobLine);
            _lineBox.style.alignItems = Align.Center;
            _jobLine = HellHudStyle.Label(24f, HellFaces.Heading, LineColour, 2f, 1f);
            _jobLine.SetWrap(true, TextAnchor.UpperCenter);
            _jobLine.style.maxWidth = HellHudPlaces.JobLine.w;
            _lineBox.Add(_jobLine);
            _lineBox.style.display = DisplayStyle.None;

            _markerBox = SkylineHudStyle.Layer(safe, "hell-job-marker");
            _markerBox.style.width = 120f;
            _markerBox.style.alignItems = Align.Center;
            _marker = HellHudStyle.Label(24f, HellFaces.Numbers, new Color(0.85f, 0.6f, 1f), 2.5f);
            _markerBox.Add(_marker);
            _markerBox.style.display = DisplayStyle.None;
        }

        void ShowCard(string head, string line, string task, float seconds)
        {
            if (!_hudBuilt && SkylineHud.Instance != null && SkylineHud.Instance.Safe != null) BuildHud(SkylineHud.Instance.Safe);
            if (!_hudBuilt) return;
            _cardHead.SetText(head);
            _cardLine.SetText("“" + line + "”", "“" + line + "”");
            _cardTask.SetText(task);
            _cardUntil = Time.unscaledTime + seconds;
        }

        /// <summary>For the UI stills (<see cref="HellUiShots"/>): catalogue job <paramref name="index"/>'s briefing card, as
        /// when it is offered. Touches no job state.</summary>
        public void ShowBriefingForShot(int index)
        {
            var all = HellJobCatalog.All;
            if (all == null || all.Length == 0) return;
            var def = all[Mathf.Clamp(index, 0, all.Length - 1)];
            ShowCard("BAJA", def.briefing, def.name.ToUpperInvariant() + ":  " + def.objective, CardSeconds);
        }

        /// <summary>For the UI stills: catalogue job <paramref name="index"/>'s instruction under the objective line (and, with
        /// <paramref name="remind"/>, the near-the-pocket card and pulse); a negative index clears it. Touches no job state.</summary>
        public void ShowJobLineForShot(int index, bool remind = false)
        {
            var all = HellJobCatalog.All;
            if (index < 0 || all == null || all.Length == 0) { _shotLine = null; return; }
            var def = all[Mathf.Clamp(index, 0, all.Length - 1)];
            _shotLine = HellJobLineText.Of(def.objective);
            if (remind) Remind(def);
        }

        void ShowResult(string head, string line, string reward)
        {
            ShowCard(head, line, reward, 0f);
            _resultUntil = Time.unscaledTime + ResultSeconds;
        }

        /// <summary>A short line in the card's task slot, keeping the card up a moment.</summary>
        void Flash(string text)
        {
            if (!_hudBuilt) return;
            _cardHead.SetText(_site != null ? _site.Job.def.name.ToUpperInvariant() : "");
            _cardLine.SetText("");
            _cardTask.SetText(text);
            _resultUntil = Mathf.Max(_resultUntil, Time.unscaledTime + 2f);
        }
    }
}
