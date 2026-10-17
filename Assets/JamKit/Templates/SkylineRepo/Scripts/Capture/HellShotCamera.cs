using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// A trailer camera shot over a replay (docs/HELL-CAPTURE.md; the data is <see cref="HellShot"/>, sampled by
    /// <see cref="HellShots"/> on the take's clock, so slow motion never stretches it). A second camera copies the player
    /// camera's settings and post-processing and takes over the picture (the player camera, and with it the gun overlay, is
    /// switched off: the shot shows the world from outside; the listener stays on the player). It follows the player (their
    /// feet) or the nearest living enemy (its centre, picked when the shot starts), with lag; adds the handheld shake; and
    /// drives a URP depth-of-field override (Bokeh) on a top-priority volume when the shot asks for it.
    /// </summary>
    public sealed class HellShotCamera : MonoBehaviour
    {
        SkylinePlayer _player;
        HellShot _shot;
        Camera _cam, _playerCam;
        Volume _volume;
        DepthOfField _dof;
        ISkylineAimable _enemy;
        Vector3 _target;
        float _targetYaw, _lastT;
        bool _hasTarget, _anchored, _first = true;

        public static HellShotCamera Create(SkylinePlayer player, string nameOrPath)
        {
            HellShot shot;
            var problems = new List<string>();
            try { shot = HellShots.Load(nameOrPath, System.IO.Directory.GetCurrentDirectory(), problems); }
            catch (System.Exception e)
            {
                Debug.LogError("HELL_SHOT could not load '" + nameOrPath + "': " + e.Message + " (the player's own camera is used)");
                return null;
            }
            foreach (var p in problems) Debug.LogWarning("HELL_SHOT " + p);
            var go = new GameObject("HellShotCamera " + shot.name);
            var s = go.AddComponent<HellShotCamera>();
            s.Setup(player, shot);
            return s;
        }

        void Setup(SkylinePlayer player, HellShot shot)
        {
            _player = player;
            _shot = shot;
            _playerCam = player != null ? player.Camera : Camera.main;
            _cam = gameObject.AddComponent<Camera>();
            var data = _cam.GetUniversalAdditionalCameraData();
            int layer = 0;
            if (_playerCam != null)
            {
                _cam.CopyFrom(_playerCam);
                var pd = _playerCam.GetUniversalAdditionalCameraData();
                bool post = pd.renderPostProcessing;
                // the gun overlay carries the post flag while the guns are up (SkylineViewmodel)
                var vm = FindAnyObjectByType<SkylineViewmodel>();
                if (vm != null && vm.OverlayCamera != null) post |= vm.OverlayCamera.GetUniversalAdditionalCameraData().renderPostProcessing;
                data.renderPostProcessing = post;
                data.antialiasing = pd.antialiasing;
                data.antialiasingQuality = pd.antialiasingQuality;
                data.renderShadows = pd.renderShadows;
                data.volumeLayerMask = pd.volumeLayerMask;
                data.volumeTrigger = transform;
                data.SetRenderer(RendererIndex(pd));
                int mask = pd.volumeLayerMask.value;
                for (int i = 0; i < 32; i++) if ((mask & (1 << i)) != 0) { layer = i; break; }
                _cam.depth = _playerCam.depth + 10f;
            }
            _cam.nearClipPlane = 0.05f;
            gameObject.layer = layer;
            if (shot.dof != null && shot.dof.on)
            {
                _volume = gameObject.AddComponent<Volume>();
                _volume.isGlobal = true;
                _volume.priority = 2000f;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _dof = profile.Add<DepthOfField>(true);
                _dof.mode.Override(DepthOfFieldMode.Bokeh);
                _volume.sharedProfile = profile;
            }
            TakeOverCamera();
            Debug.Log("HELL_SHOT " + shot.name + ": follow " + shot.follow + ", " + shot.keys.Count + " keys, " + HellShots.Length(shot) + " s");
        }

        /// <summary>The renderer the player camera uses (URP keeps it private; the default is index -1).</summary>
        static int RendererIndex(UniversalAdditionalCameraData d)
        {
            var f = typeof(UniversalAdditionalCameraData).GetField("m_RendererIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return f != null ? (int)f.GetValue(d) : -1;
        }

        /// <summary>The shot's camera draws the picture (the player camera and its gun overlay are off).</summary>
        public void TakeOverCamera()
        {
            if (_cam != null) _cam.enabled = true;
            if (_playerCam != null && _playerCam != _cam) _playerCam.enabled = false;
        }

        /// <summary>The player's camera again (a preview handing the controls back).</summary>
        public void Restore()
        {
            if (_playerCam != null) _playerCam.enabled = true;
            if (_cam != null) _cam.enabled = false;
            if (_volume != null) _volume.enabled = false;
        }

        void OnDestroy()
        {
            if (_volume != null && _volume.sharedProfile != null) Destroy(_volume.sharedProfile);
        }

        /// <summary>Places the camera for take time <paramref name="t"/> (call after the frame's movement: LateUpdate).</summary>
        public void Tick(float t, bool live)
        {
            if (_shot == null || _cam == null) return;
            float dt = Mathf.Max(0f, t - _lastT);
            _lastT = t;
            UpdateTarget(dt);
            if (!live) return;
            var pose = HellShots.Sample(_shot, t, N(_target), _targetYaw, _hasTarget);
            pose = HellShots.Shake(_shot, pose, t);
            transform.SetPositionAndRotation(U(pose.position), Quaternion.Euler(pose.pitch, pose.yaw, -pose.roll));
            _cam.fieldOfView = Mathf.Clamp(pose.fov, 1f, 170f);
            if (_dof != null)
            {
                _dof.focusDistance.Override(pose.focusDistance);
                _dof.aperture.Override(Mathf.Clamp(pose.aperture, 1f, 32f));
                _dof.focalLength.Override(Mathf.Clamp(pose.focalLength, 1f, 300f));
            }
        }

        void UpdateTarget(float dt)
        {
            if (_anchored) return;
            Vector3 goal;
            float yaw;
            switch (_shot.follow)
            {
                case HellShotFollow.Player:
                    if (_player == null) return;
                    goal = _player.Movement != null ? _player.Movement.RenderPosition : _player.transform.position;
                    yaw = _player.Yaw;
                    break;
                case HellShotFollow.NearestEnemy:
                    if (_enemy == null) _enemy = NearestEnemy();
                    if (_enemy == null) return;
                    var c = _enemy as Component;
                    if ((object)c != null && c == null) { _enemy = null; return; }   // destroyed
                    goal = _enemy.Center;
                    yaw = c != null ? c.transform.eulerAngles.y : 0f;
                    break;
                default:
                    return;
            }
            _target = _first ? goal : U(HellShots.Follow(N(_target), N(goal), _shot.followLag, dt));
            _targetYaw = _first ? yaw : Mathf.LerpAngle(_targetYaw, yaw, _shot.followLag <= 1e-4f ? 1f : 1f - Mathf.Exp(-dt * 4f / _shot.followLag));
            _hasTarget = true;
            _first = false;
            if (_shot.anchorAtStart) _anchored = true;
        }

        ISkylineAimable NearestEnemy()
        {
            Vector3 from = _player != null ? _player.transform.position : Vector3.zero;
            ISkylineAimable best = null;
            float bestD = float.MaxValue;
            foreach (var a in SkylineTargets.Aimables)
            {
                if (a == null || !a.Alive) continue;
                var c = a as Component;
                if ((object)c != null && (c == null || c.GetComponentInParent<SkylinePlayer>() != null)) continue;
                float d = (a.Center - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = a; }
            }
            if (best != null) Debug.Log("HELL_SHOT following " + best.Id);
            return best;
        }

        static NVec3 N(Vector3 v) => new NVec3(v.x, v.y, v.z);
        static Vector3 U(NVec3 v) => new Vector3(v.X, v.Y, v.Z);
    }
}
