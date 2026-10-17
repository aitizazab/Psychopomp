using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Plays a recording of the creator's play again (docs/HELL-CAPTURE.md, <c>-hell-replay &lt;name&gt;</c> or a capture take),
    /// on the recording's fixed step from the recording's seed, so the run happens again for re-takes from other cameras:
    /// <list type="bullet">
    /// <item>Each frame the player gets the recorded input frame (<see cref="SkylinePlayer.ScriptedInput"/>) and the Hell
    /// buttons (<see cref="HellAbilities.ScriptedButtons"/>); the view is set to the recorded one before the player's frame
    /// (so shots go where they went), and the feet are put back on the recording if they drift past
    /// <see cref="HellReplayDrift.tolerance"/> (logged; a recording made in the same editor normally never drifts).</item>
    /// <item>Skill picks are replayed on the pick screen (<see cref="HellSkillPick.PickById"/>).</item>
    /// <item><b>Jump to a time:</b> with a start time, the frames before it run with every camera off and the sound silent
    /// (simulation only, many times faster than real time), then the picture comes on.</item>
    /// <item><b>Capture:</b> at the start time the editor is paused (<c>Debug.Break</c>) so the Recorder starts on exactly that
    /// frame (<see cref="ReadyForRecording"/>); take time 0 is that frame.</item>
    /// <item>At the end (or the end time) a preview hands the controls back to the player: play on from the replayed moment.</item>
    /// </list>
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public sealed class HellReplayer : MonoBehaviour
    {
        /// <summary>A capture take reached its start: the editor's capture starts the Recorder now.</summary>
        public static bool ReadyForRecording { get; private set; }
        /// <summary>The replay's step index (frames played) and the take's own clock (s since the start time).</summary>
        public static int Index { get; private set; }
        public static float TakeSeconds { get; private set; }

        SkylinePlayer _player;
        HellReplay _rep;
        int _fromFrame, _toFrame;
        bool _capture, _started, _fastForward, _done, _released;
        HellReplayFrame _cur;
        readonly HellReplayDrift _drift = new HellReplayDrift();
        readonly List<HellReplayEvent> _due = new List<HellReplayEvent>();
        readonly List<HellReplayEvent> _pending = new List<HellReplayEvent>();
        readonly List<Camera> _muted = new List<Camera>();
        float _listenerVolume = 1f;
        string _shotName;
        HellShotCamera _shot;

        public void Configure(SkylinePlayer player, bool waitForRun, HellReplay replay, float from, float to, string shot, bool capture)
        {
            _player = player;
            _rep = replay;
            _capture = capture;
            _shotName = shot ?? "";
            _fromFrame = replay.FrameAt(from);
            _toFrame = to > from ? replay.FrameAt(to) : replay.FrameCount;
            ReadyForRecording = false;
            Index = 0;
            TakeSeconds = 0f;
            if (_player == null) { Debug.LogError("HELL_REPLAY no player in the scene"); enabled = false; return; }
            _player.ScriptedInput = Input;
            HellAbilities.ScriptedButtons = Buttons;
            if (waitForRun) HellRunEvents.RunStarted += OnRunStarted;
            else Begin();
            Debug.Log("HELL_REPLAY armed: steps " + _fromFrame + ".." + _toFrame + " of " + replay.FrameCount +
                      (_shotName.Length > 0 ? ", shot " + _shotName : "") + (capture ? ", capture" : ""));
        }

        void OnRunStarted()
        {
            HellRunEvents.RunStarted -= OnRunStarted;
            Begin();
        }

        void Begin()
        {
            if (_started) return;
            _started = true;
            UnityEngine.Random.InitState(_rep.seed);
            if (_shotName.Length > 0) _shot = HellShotCamera.Create(_player, _shotName);
            if (_fromFrame > 0) FastForward(true);
            Debug.Log("HELL_REPLAY start frame " + Time.frameCount);
        }

        void OnDestroy()
        {
            HellRunEvents.RunStarted -= OnRunStarted;
            Release();
            if (_fastForward) FastForward(false);
            ReadyForRecording = false;
        }

        SkylineInputFrame Input()
        {
            if (!_started || _done) return default(SkylineInputFrame);
            var f = _cur;
            return new SkylineInputFrame
            {
                move = new Vector2(f.moveX, f.moveY),
                look = Vector2.zero,   // the view is set straight to the recorded one (Update)
                lookFromPad = false,
                jumpHeld = f.Has(HellReplayButtons.JumpHeld), jumpPressed = f.Has(HellReplayButtons.JumpPressed),
                crouchHeld = f.Has(HellReplayButtons.CrouchHeld), crouchPressed = f.Has(HellReplayButtons.CrouchPressed),
                sprintHeld = f.Has(HellReplayButtons.SprintHeld), sprintTogglePressed = f.Has(HellReplayButtons.SprintTogglePressed),
                fireHeld = f.Has(HellReplayButtons.FireHeld), firePressed = f.Has(HellReplayButtons.FirePressed),
                aimHeld = f.Has(HellReplayButtons.AimHeld),
                reloadPressed = f.Has(HellReplayButtons.ReloadPressed), swapPressed = f.Has(HellReplayButtons.SwapPressed),
                inspectPressed = f.Has(HellReplayButtons.InspectPressed),
            };
        }

        HellScriptedButtons Buttons()
        {
            if (!_started || _done) return default(HellScriptedButtons);
            return new HellScriptedButtons
            {
                stepHeld = _cur.Has(HellReplayButtons.StepHeld),
                rewindPressed = _cur.Has(HellReplayButtons.RewindPressed),
                skillPressed = _cur.Has(HellReplayButtons.SkillPressed),
            };
        }

        void Update()
        {
            if (!_started || _done) return;
            ApplyPending();
            if (Index >= _toFrame || Index >= _rep.FrameCount) { Finish(); return; }
            if (Index == 0) ApplyEvents(0);

            // the feet where the recording had them before this step, then this step's view
            if (Index > 0)
            {
                var prev = _rep.frames[Index - 1];
                var p = _player.transform.position;
                if (_drift.Check(p.x - prev.x, p.y - prev.y, p.z - prev.z) && _player.Movement != null)
                {
                    _player.Movement.Teleport(new Vector3(prev.x, prev.y, prev.z), prev.yaw, _player.Movement.Velocity, prev.pitch);
                    Debug.LogWarning("HELL_REPLAY drift put back at step " + Index + " (" + Vector3.Distance(p, new Vector3(prev.x, prev.y, prev.z)).ToString("0.00") + " m)");
                }
            }
            _cur = _rep.frames[Index];
            _player.SetAim(_cur.yaw, _cur.pitch);
        }

        void LateUpdate()
        {
            if (!_started || _done) return;
            Index++;
            if (Index > _fromFrame) TakeSeconds += _rep.Step;
            // the skill picks recorded before the next step: the pick screen opened during this frame, so close it now
            ApplyEvents(Index);
            if (_fastForward && Index >= _fromFrame) FastForward(false);
            if (Index == _fromFrame && _fromFrame > 0) Debug.Log("HELL_REPLAY reached " + _rep.TimeOf(Index).ToString("0.00") + " s (step " + Index + ")");
            if (_capture && !ReadyForRecording && Index >= _fromFrame)
            {
                ReadyForRecording = true;
                TakeSeconds = 0f;
                Debug.Log("HELL_REPLAY ready for the Recorder at step " + Index);
#if UNITY_EDITOR
                Debug.Break();   // pause after this frame; the capture starts the Recorder and resumes
#endif
            }
            if (_shot != null) _shot.Tick(TakeSeconds, Index >= _fromFrame);
        }

        void ApplyEvents(int index)
        {
            _due.Clear();
            _rep.EventsAt(index, _due);
            foreach (var e in _due) _pending.Add(e);
            ApplyPending();
        }

        void ApplyPending()
        {
            for (int i = 0; i < _pending.Count; i++)
            {
                var e = _pending[i];
                if (e.kind != "grant") { _pending.RemoveAt(i--); continue; }
                var pick = _player.GetComponent<HellSkillPick>();
                if (pick != null && pick.PickById(e.value)) { _pending.RemoveAt(i--); continue; }
                // no pick screen open: the floor's end came later than in the recording (or there is no screen); wait for it a
                // little, then grant directly so the run has the skill
                if (Index > e.frame + 2 * _rep.fps)
                {
                    var ab = HellAbilities.Instance;
                    if (ab != null) ab.Grant(e.value);
                    if (HellRunEvents.WaitingForPick) HellRunEvents.ContinueAfterPick();
                    Debug.LogWarning("HELL_REPLAY granted " + e.value + " without its pick screen (step " + Index + ", recorded before step " + e.frame + ")");
                    _pending.RemoveAt(i--);
                }
            }
        }

        void FastForward(bool on)
        {
            if (on == _fastForward) return;
            _fastForward = on;
            if (on)
            {
                _muted.Clear();
                foreach (var c in Camera.allCameras) if (c.enabled) { c.enabled = false; _muted.Add(c); }
                _listenerVolume = AudioListener.volume;
                AudioListener.volume = 0f;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                Debug.Log("HELL_REPLAY jumping to step " + _fromFrame + " (" + _rep.TimeOf(_fromFrame).ToString("0.00") + " s) without drawing");
            }
            else
            {
                foreach (var c in _muted) if (c != null) c.enabled = true;
                _muted.Clear();
                AudioListener.volume = _listenerVolume;
                if (_shot != null) _shot.TakeOverCamera();
                if (!_capture) Application.targetFrameRate = _rep.fps;
            }
        }

        void Finish()
        {
            if (_done) return;
            _done = true;
            Debug.Log("HELL_REPLAY done at step " + Index + " (" + _rep.TimeOf(Index).ToString("0.00") + " s); worst drift " +
                      _drift.Worst.ToString("0.000") + " m, " + _drift.Corrections + " correction(s)");
            if (!_capture) Release();
        }

        /// <summary>Hands the player back to the devices (a preview plays on from here) and frees the normal frame rate.</summary>
        void Release()
        {
            if (_released) return;
            _released = true;
            if (_player != null && _player.ScriptedInput == (System.Func<SkylineInputFrame>)Input) _player.ScriptedInput = null;
            HellAbilities.ScriptedButtons = null;
            if (_shot != null && !_capture) _shot.Restore();
            if (!_capture)
            {
                Time.captureDeltaTime = 0f;
                Application.targetFrameRate = -1;
            }
        }
    }
}
