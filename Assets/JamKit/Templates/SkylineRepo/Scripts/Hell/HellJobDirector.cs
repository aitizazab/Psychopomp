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

        HellRunDirector _run;
        HellJobSite _site;
        HellRoom _siteRoom;
        bool _hudBuilt;
        VisualElement _cardBox, _markerBox;
        KeylinedLabel _cardHead, _cardLine, _cardTask, _tracker, _marker;
        float _cardUntil, _resultUntil;

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
            ShowCard("BAJA", def.briefing, def.name.ToUpperInvariant() + ":  " + def.objective, CardSeconds);
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
                if (paid != null) ShowResult("DONE:  " + def.name.ToUpperInvariant(), def.doneLine, paid);
            }
            else if ((ev & HellJobEvent.Failed) != 0)
                ShowResult("FAILED:  " + def.name.ToUpperInvariant(), def.failLine, Fail(_site.Job.State.failReason));
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
            _cardBox.style.display = now < _cardUntil || now < _resultUntil ? DisplayStyle.Flex : DisplayStyle.None;
            if (_site != null && _siteRoom != null && !_siteRoom.Running) Settle(true);   // down the shaft or the run ended

            var job = _site != null ? _site.Job : null;
            if (job != null && !job.Settled)
            {
                string bar = "";
                float p = job.Progress01;
                if (p > 0f) bar = "   " + new string('|', Mathf.RoundToInt(p * 12f)) + new string('.', 12 - Mathf.RoundToInt(p * 12f));
                _tracker.SetText("FAVOUR:  " + job.def.name + bar + "      " + HellJobs.DoneCount + "/" + HellJobs.OfferedCount + " DONE");
            }
            else _tracker.SetText(HellJobs.OfferedCount > 0 ? "FAVOURS  " + HellJobs.DoneCount + "/" + HellJobs.OfferedCount : "");
            UpdateMarker(job);
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
                    _marker.SetText("JOB  " + Mathf.RoundToInt(Vector3.Distance(feet, door)) + " m");
                }
            }
            _markerBox.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void BuildHud(VisualElement safe)
        {
            _hudBuilt = true;
            string face = SkylineHudStyle.AmmoFontDefault;
            _cardBox = SkylineHudStyle.Layer(safe, "hell-job-card");
            _cardBox.style.left = 40f;
            _cardBox.style.top = 170f;
            _cardBox.style.width = 620f;
            _cardBox.style.paddingLeft = _cardBox.style.paddingRight = 18f;
            _cardBox.style.paddingTop = _cardBox.style.paddingBottom = 12f;
            _cardBox.style.backgroundColor = new Color(0.04f, 0.01f, 0.06f, 0.82f);
            _cardBox.style.borderLeftWidth = 6f;
            _cardBox.style.borderLeftColor = new Color(0.7f, 0.3f, 1f);
            _cardHead = new KeylinedLabel(22f, 2f, face);
            _cardHead.SetColor(new Color(0.78f, 0.55f, 1f));
            _cardLine = new KeylinedLabel(26f, 2f, null);
            _cardLine.SetWrap(true, TextAnchor.UpperLeft);
            _cardLine.SetItalic(true);
            _cardTask = new KeylinedLabel(22f, 2f, face);
            _cardTask.SetWrap(true, TextAnchor.UpperLeft);
            _cardTask.SetColor(new Color(1f, 0.82f, 0.55f));
            _cardTask.style.marginTop = 8f;
            _cardBox.Add(_cardHead);
            _cardBox.Add(_cardLine);
            _cardBox.Add(_cardTask);
            _cardBox.style.display = DisplayStyle.None;

            var trackBox = SkylineHudStyle.Layer(safe, "hell-job-tracker");
            trackBox.style.left = 40f;
            trackBox.style.top = 130f;
            _tracker = new KeylinedLabel(24f, 2f, face);
            _tracker.SetColor(new Color(0.85f, 0.65f, 1f));
            trackBox.Add(_tracker);

            _markerBox = SkylineHudStyle.Layer(safe, "hell-job-marker");
            _markerBox.style.width = 120f;
            _markerBox.style.alignItems = Align.Center;
            _marker = new KeylinedLabel(24f, 2.5f, face);
            _marker.SetColor(new Color(0.85f, 0.5f, 1f));
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
