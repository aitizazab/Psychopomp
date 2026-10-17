using System;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Records the creator's play (docs/HELL-CAPTURE.md, <c>-hell-rec &lt;name&gt;</c>): from the frame the run starts, every
    /// fixed step's input frame (stick and buttons, the Hell abilities' buttons too), the view after the frame's look and the
    /// feet after the frame, plus each skill picked on the pick screen; the run's seed is chosen up front and stored. Frames
    /// while the pause menu or the pick screen hold the game are left out (the replay never pauses). Saved every 30 s and on
    /// exit to <c>out/hell/replays/&lt;name&gt;.hellrec</c> (or the path given).
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public sealed class HellPlayRecorder : MonoBehaviour
    {
        const float AutosaveSeconds = 30f;

        SkylinePlayer _player;
        HellAbilities _abilities;
        HellReplay _rec;
        string _path;
        bool _started, _skip, _dirty;
        float _nextSave;

        public void Configure(SkylinePlayer player, bool waitForRun, string path, int fps, int seed)
        {
            _player = player;
            _path = path;
            _rec = new HellReplay { fps = fps, seed = seed, scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, note = DateTime.Now.ToString("yyyy-MM-dd HH:mm") };
            if (waitForRun) HellRunEvents.RunStarted += OnRunStarted;
            else Begin();
            Debug.Log("HELL_REC armed: " + path + " at " + fps + " fps, seed " + seed + (waitForRun ? " (starts with the run)" : ""));
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
            UnityEngine.Random.InitState(_rec.seed);   // the effects' random numbers, the same in the replay
            _nextSave = Time.realtimeSinceStartup + AutosaveSeconds;
            Debug.Log("HELL_REC start frame " + Time.frameCount);
        }

        void OnDestroy()
        {
            HellRunEvents.RunStarted -= OnRunStarted;
            if (_abilities != null) _abilities.Granted -= OnGranted;
            Save("end");
        }

        void OnApplicationQuit() => Save("quit");

        void OnGranted(string id)
        {
            if (!_started) return;
            _rec.AddEvent(_rec.FrameCount, "grant", id);   // replayed before the next recorded step
            Debug.Log("HELL_REC grant " + id + " before step " + _rec.FrameCount);
        }

        void Update()
        {
            // the state the frame starts in decides whether it counts (a frame the pause or the pick holds is not played)
            _skip = SkylineDeviceInput.Paused || HellSkillPick.Open || SkylinePauseMenu.IsOpen;
            if (_abilities == null && HellAbilities.Instance != null)
            {
                _abilities = HellAbilities.Instance;
                _abilities.Granted += OnGranted;
            }
        }

        void LateUpdate()
        {
            if (!_started || _player == null || _skip) return;
            var f = _player.Frame;
            bool mouseOk = Application.isBatchMode || Cursor.lockState == CursorLockMode.Locked;
            const BindDevice KM = BindDevice.KeyboardMouse, Pad = BindDevice.Pad;
            var b = HellReplayButtons.None;
            if (f.jumpHeld) b |= HellReplayButtons.JumpHeld;
            if (f.jumpPressed) b |= HellReplayButtons.JumpPressed;
            if (f.crouchHeld) b |= HellReplayButtons.CrouchHeld;
            if (f.crouchPressed) b |= HellReplayButtons.CrouchPressed;
            if (f.sprintHeld) b |= HellReplayButtons.SprintHeld;
            if (f.sprintTogglePressed) b |= HellReplayButtons.SprintTogglePressed;
            if (f.fireHeld) b |= HellReplayButtons.FireHeld;
            if (f.firePressed) b |= HellReplayButtons.FirePressed;
            if (f.aimHeld) b |= HellReplayButtons.AimHeld;
            if (f.reloadPressed) b |= HellReplayButtons.ReloadPressed;
            if (f.swapPressed) b |= HellReplayButtons.SwapPressed;
            if (f.inspectPressed) b |= HellReplayButtons.InspectPressed;
            if (SkylineBindings.Held(KM, BindAction.ShadowStep, mouseOk) || SkylineBindings.Held(Pad, BindAction.ShadowStep, mouseOk)) b |= HellReplayButtons.StepHeld;
            if (SkylineBindings.Pressed(KM, BindAction.Rewind, mouseOk) || SkylineBindings.Pressed(Pad, BindAction.Rewind, mouseOk)) b |= HellReplayButtons.RewindPressed;
            if (SkylineBindings.Pressed(KM, BindAction.Skill, mouseOk) || SkylineBindings.Pressed(Pad, BindAction.Skill, mouseOk)) b |= HellReplayButtons.SkillPressed;
            if (HellAbilityWheel.IsOpen) b |= HellReplayButtons.WheelHeld;
            var p = _player.transform.position;
            _rec.frames.Add(new HellReplayFrame
            {
                moveX = f.move.x, moveY = f.move.y, buttons = b,
                yaw = _player.Yaw, pitch = _player.Pitch,
                x = p.x, y = p.y, z = p.z,
            });
            _dirty = true;
            if (Time.realtimeSinceStartup >= _nextSave)
            {
                _nextSave = Time.realtimeSinceStartup + AutosaveSeconds;
                Save("autosave");
            }
        }

        void Save(string why)
        {
            if (_rec == null || !_dirty || string.IsNullOrEmpty(_path)) return;
            try
            {
                _rec.Save(_path);
                _dirty = false;
                Debug.Log("HELL_REC saved (" + why + ") " + System.IO.Path.GetFullPath(_path) + ": " + _rec.Describe());
            }
            catch (Exception e)
            {
                Debug.LogError("HELL_REC could not save " + _path + ": " + e.Message);
            }
        }
    }
}
