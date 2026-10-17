using System.Collections;
using System.Collections.Generic;
using System.IO;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The frame-time check of the Hell build (docs/HELL-CAPTURE.md, <c>-hell-perf &lt;dir&gt;</c>): the run director is held
    /// back, and the player's eye flies through each room in turn along the room's safe path (<see cref="HellPerfRoute"/>), at
    /// <c>-hell-perf-speed</c> m/s (8), with the room started as in play (its enemies spawned and fighting; the player is
    /// put back up if wrecked) unless <c>-hell-perf-enemies off</c>. Every frame's real time is kept; the first
    /// <see cref="Settle"/> s of a room (it switching on, its enemies spawning) are reported apart as <c>enterWorstMs</c>.
    /// Writes <c>&lt;dir&gt;/perf.json</c> (per room and overall: average, 1% low, worst; whether each meets the
    /// <c>-hell-perf-target</c> fps, 120 by default) and quits. Uncapped: v-sync off, no frame-rate cap.
    /// </summary>
    public sealed class HellPerfFlight : MonoBehaviour
    {
        const float Warmup = 2f, Settle = 0.5f;

        SkylinePlayer _player;
        HellRunDirector _director;
        HellCaptureArgs _args;
        readonly Dictionary<long, HellRoom> _rooms = new Dictionary<long, HellRoom>();
        readonly List<float> _all = new List<float>(65536);
        readonly List<float> _room = new List<float>(8192);

        public void Configure(SkylinePlayer player, HellRunDirector director, HellCaptureArgs args)
        {
            _player = player;
            _director = director;
            _args = args;
            if (_director != null) _director.enabled = false;   // before its Start: no run, the rooms are ours
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Time.captureDeltaTime = 0f;
        }

        IEnumerator Start()
        {
            if (_player == null) { Fail("no player in the scene"); yield break; }
            foreach (var r in FindObjectsByType<HellRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                _rooms[r.floor * 100L + (int)r.kind] = r;
                r.gameObject.SetActive(false);
            }
            if (_rooms.Count == 0) { Fail("no Hell rooms in the scene (build it: JamKit/Hell/Build the Hell scene)"); yield break; }
            _player.ScriptedInput = () => default(SkylineInputFrame);
            _player.Wrecked += OnWrecked;

            var report = new HellPerfReport
            {
                targetFps = _args.perfTargetFps,
                quality = QualitySettings.names.Length > 0 ? QualitySettings.names[QualitySettings.GetQualityLevel()] : "",
                resolution = Screen.width + "x" + Screen.height + (Screen.fullScreen ? " full screen" : " windowed"),
                note = (Application.isEditor ? "editor" : "build") + ", " + _args.perfSpeed + " m/s, enemies " + (_args.perfEnemies ? "on" : "off") +
                       ", " + SystemInfo.graphicsDeviceName,
            };
            Debug.Log("HELL_PERF start: " + report.note + ", " + report.quality + ", " + report.resolution);

            bool warmed = false;
            for (int f = 0; f < HellRoomVariants.FloorCount; f++)
                foreach (var kind in HellRoomLayouts.Kinds)
                {
                    if (!HellPerfRoute.Matches(_args.perfRooms, f, kind)) continue;
                    if (!_rooms.TryGetValue(f * 100L + (int)kind, out var room)) { Debug.LogWarning("HELL_PERF no room " + HellPerfRoute.Id(f, kind)); continue; }
                    var route = HellPerfRoute.For(HellWorld.Room(f, kind));
                    room.gameObject.SetActive(true);
                    if (_args.perfEnemies) room.Begin(_player);
                    Place(route, 0f);
                    if (!warmed)
                    {
                        warmed = true;
                        float until = Time.realtimeSinceStartup + Warmup;
                        while (Time.realtimeSinceStartup < until) { Place(route, 0f); yield return null; }
                    }
                    _room.Clear();
                    float enterWorst = 0f, t = 0f, dist = 0f;
                    while (dist < route.Length)
                    {
                        yield return null;
                        float dt = Time.unscaledDeltaTime;
                        t += dt;
                        if (t <= Settle) { enterWorst = Mathf.Max(enterWorst, dt * 1000f); Place(route, 0f); continue; }
                        _room.Add(dt);
                        _all.Add(dt);
                        dist += _args.perfSpeed * dt;
                        Place(route, dist);
                    }
                    var res = new HellPerfRoom
                    {
                        id = HellPerfRoute.Id(f, kind),
                        seconds = t,
                        frames = FrameStats.Summarize(_room),
                        over = HellPerfReport.CountOver(_room, report.TargetMs),
                        enterWorstMs = enterWorst,
                    };
                    report.rooms.Add(res);
                    Debug.Log("HELL_PERF room " + res.id + ": avg " + res.frames.avgMs.ToString("0.00") + " ms, 1% low " +
                              res.frames.onePercentLowMs.ToString("0.00") + " ms, worst " + res.frames.worstMs.ToString("0.00") + " ms, enter " +
                              enterWorst.ToString("0.0") + " ms" + (HellPerfReport.Meets(res.frames, report.TargetMs) ? "" : "  BELOW " + report.targetFps + " fps"));
                    if (_args.perfEnemies) room.End();
                    room.gameObject.SetActive(false);
                }

            report.all = FrameStats.Summarize(_all);
            string dir = _args.perfOut;
            try
            {
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "perf.json");
                File.WriteAllText(path, report.ToJson());
                Debug.Log("HELL_PERF wrote " + Path.GetFullPath(path) + ": all avg " + report.all.avgMs.ToString("0.00") + " ms, 1% low " +
                          report.all.onePercentLowMs.ToString("0.00") + " ms, worst room " + (report.Worst() != null ? report.Worst().id : "-"));
            }
            catch (System.Exception e) { Debug.LogError("HELL_PERF could not write perf.json: " + e.Message); }
            Quit(0);
        }

        void Place(HellPerfRoute route, float dist)
        {
            route.Pose(dist, out var eye, out float yaw, out float pitch);
            var feet = new Vector3(eye.X, eye.Y - HellPerfRoute.EyeHeight, eye.Z);
            if (_player.Movement != null) _player.Movement.Teleport(feet, yaw, Vector3.zero, pitch);
            else _player.transform.position = feet;
            _player.SetAim(yaw, pitch);
        }

        void OnWrecked()
        {
            if (_player != null) _player.Restart(_player.transform.position, _player.Yaw);
        }

        void OnDestroy()
        {
            if (_player != null) _player.Wrecked -= OnWrecked;
        }

        void Fail(string why)
        {
            Debug.LogError("HELL_PERF FAILED " + why);
            try
            {
                Directory.CreateDirectory(_args.perfOut);
                File.WriteAllText(Path.Combine(_args.perfOut, "perf.json"), "{\"error\":\"" + why.Replace("\"", "'") + "\"}");
            }
            catch (System.Exception) { }
            Quit(1);
        }

        static void Quit(int code)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(code);
#endif
        }
    }
}
