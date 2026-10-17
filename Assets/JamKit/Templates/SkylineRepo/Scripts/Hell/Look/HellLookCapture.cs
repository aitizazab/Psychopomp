using System.Collections;
using System.Collections.Generic;
using System.IO;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using NVec3 = System.Numerics.Vector3;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The look test's stills (docs/HELL-LOOK-TEST.md), in a built player: <c>HellDescent.exe -hellLook A -hellLookCapture &lt;dir&gt;</c>
    /// renders fixed camera shots at 2560x1440 into <c>&lt;dir&gt;/&lt;A|B|C&gt;/</c>, then quits:
    /// <c>floor1-wide.png</c>, <c>floor2-wide.png</c>, <c>floor3-wide.png</c> (each floor's Six Sigils cavern from above its entry),
    /// <c>enemies-at-range.png</c> (three fire imps 12-26 m out over the floor 1 cavern's lava) and <c>dark-zone.png</c> (a floor 1 dark
    /// zone). Under look A it also renders the enemy shot once per outline into <c>&lt;dir&gt;/outlines/</c>: <c>soulfire-pulse</c> (the
    /// creator's pick), <c>lime</c>, <c>soulfire</c>, <c>whitehot</c>, <c>pulse</c>, and <c>studs-blue|bone|stone</c> beside the pick. Logs <c>HELL_LOOK_CAPTURE ...</c>.
    /// The shots render through a camera of their own into a render texture (the run and its HUD carry on underneath, unseen);
    /// time is stopped once the imps are placed, so every direction sees the same moment.
    /// </summary>
    public sealed class HellLookCapture : MonoBehaviour
    {
        public string outDir;
        public int width = 2560, height = 1440;
        public float fov = 70f;

        Camera _cam;
        RenderTexture _rt;
        readonly List<SkylineBot> _imps = new List<SkylineBot>();

        IEnumerator Start()
        {
            var sw = GetComponent<HellLookSwitch>();
            string letter = sw == null || sw.look == HellLookDirection.None ? "none" : sw.Mixed ? "floors-" + sw.DirectionOn(0) + sw.DirectionOn(1) + sw.DirectionOn(2) : sw.look.ToString();
            Debug.Log("HELL_LOOK_CAPTURE start look " + letter + " -> " + outDir);
            // let the run settle (the drop, shader warm-up)
            for (int i = 0; i < 10; i++) yield return null;
            yield return new WaitForSecondsRealtime(2.5f);

            var go = new GameObject("Hell Look Capture Camera");
            _cam = go.AddComponent<Camera>();
            _cam.enabled = false;
            _cam.fieldOfView = fov;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 600f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = RenderSettings.fogColor;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderType = CameraRenderType.Base;
            data.renderPostProcessing = true;
            data.requiresDepthTexture = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            HellLookSwitch.PrepareCamera(_cam);
            _rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "HellLookCapture" };
            _rt.Create();

            string dir = Path.Combine(outDir, letter);
            Directory.CreateDirectory(dir);
            int done = 0;
            HellBeyond.SkyCamera(_cam);

            // the wide shots, one per floor (each floor's Six Sigils cavern from above its entry)
            for (int f = 0; f < HellRoomVariants.FloorCount; f++)
            {
                var room = HellWorld.Room(f, HellRoomKind.SixSigils);
                ShowRoom(f, HellRoomKind.SixSigils);
                Frame(WideShot(room), f, sw);
                yield return Shot(Path.Combine(dir, "floor" + (f + 1) + "-wide.png"));
                done++;
            }
            // eye level, one room per floor (what the player sees), and the sky beyond each floor's walls
            var eyeRooms = new[] { HellRoomKind.SoulLantern, HellRoomKind.BreakChains, HellRoomKind.Collapse };
            for (int f = 0; f < HellRoomVariants.FloorCount; f++)
            {
                var room = HellWorld.Room(f, eyeRooms[f]);
                ShowRoom(f, eyeRooms[f]);
                Frame(EyeShot(room), f, sw);
                yield return Shot(Path.Combine(dir, "floor" + (f + 1) + "-eye.png"));
                done++;
                Frame(SkyShot(room), f, sw);
                yield return Shot(Path.Combine(dir, "floor" + (f + 1) + "-sky.png"));
                done++;
            }

            // the imps against the floor 1 cavern's lava
            var enemyRoom = HellWorld.Room(0, HellRoomKind.SixSigils);
            ShowRoom(0, HellRoomKind.SixSigils);
            var enemyShot = EnemyShot(enemyRoom);
            HideOtherBots();
            SpawnImps(enemyShot);
            for (int i = 0; i < 4; i++) yield return null;   // a few frames of hover so they settle
            if (sw != null) sw.DressNewBots();
            Time.timeScale = 0f;
            Frame(enemyShot, 0, sw);
            yield return Shot(Path.Combine(dir, "enemies-at-range.png"));
            done++;

            // a wall meeting the floor, close (textures, trims, the light on them)
            Frame(WallShot(enemyRoom), 0, sw);
            yield return Shot(Path.Combine(dir, "wall-floor-close.png"));
            done++;

            // a dark zone
            var dark = DarkShot(enemyRoom);
            if (dark.HasValue)
            {
                Frame(dark.Value, 0, sw);
                yield return Shot(Path.Combine(dir, "dark-zone.png"));
                done++;
            }
            else Debug.LogWarning("HELL_LOOK_CAPTURE no dark zone in the floor 1 Six Sigils");

            // the three enemies close: an imp, a scythe demon, an ooze
            foreach (var b in _imps) if (b != null) b.gameObject.SetActive(false);
            Time.timeScale = 1f;
            var close = SpawnLineup(enemyShot);
            for (int i = 0; i < 4; i++) yield return null;
            if (sw != null) sw.DressNewBots();
            Time.timeScale = 0f;
            Frame(enemyShot, 0, sw, close);
            yield return Shot(Path.Combine(dir, "enemies-close.png"));
            done++;

            Debug.Log("HELL_LOOK_CAPTURE done " + done + " stills -> " + outDir);
            Time.timeScale = 1f;
            _rt.Release();
            Application.Quit(0);
        }

        struct ShotDef { public Vector3 position, lookAt; }

        /// <summary>The luminance spread (0..255) over a 64 x 36 grid of the still: a few means nothing but fog in the frame.</summary>
        static float LuminanceSpread(Texture2D tex, out float mean)
        {
            double sum = 0, sum2 = 0;
            int n = 0;
            for (int y = 0; y < 36; y++)
                for (int x = 0; x < 64; x++)
                {
                    var c = tex.GetPixel((int)((x + 0.5f) * tex.width / 64f), (int)((y + 0.5f) * tex.height / 36f));
                    double l = 255.0 * (0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b);
                    sum += l; sum2 += l * l; n++;
                }
            mean = (float)(sum / n);
            return (float)System.Math.Sqrt(System.Math.Max(0.0, sum2 / n - (sum / n) * (sum / n)));
        }

        static Vector3 U(NVec3 v) => new Vector3(v.X, v.Y, v.Z);

        static Vector3 Forward(HellRoomDef room) => Quaternion.Euler(0f, room.entryYaw, 0f) * Vector3.forward;

        static Vector3 Centre(HellRoomDef room) => (U(room.bounds.min) + U(room.bounds.max)) * 0.5f;

        /// <summary>From above the entry, a little into the room, looking down the cavern.</summary>
        static ShotDef WideShot(HellRoomDef room)
        {
            var fwd = Forward(room);
            var entry = U(room.entry);
            var c = Centre(room);
            c.y = 0f;
            return new ShotDef { position = entry + fwd * 6f + Vector3.up * 13f, lookAt = Vector3.Lerp(entry + fwd * 30f, c, 0.5f) + Vector3.down * 2f };
        }

        /// <summary>Eye height a little into the room, looking along it (the imps are placed down this line).</summary>
        static ShotDef EnemyShot(HellRoomDef room)
        {
            var fwd = Forward(room);
            var entry = U(room.entry);
            var p = entry + fwd * 8f + Vector3.up * 2.6f;
            return new ShotDef { position = p, lookAt = p + fwd * 30f + Vector3.down * 1.5f };
        }

        /// <summary>Eye height (1.7 m over the entry floor) a few metres in, looking down the room.</summary>
        static ShotDef EyeShot(HellRoomDef room)
        {
            var fwd = Forward(room);
            var p = U(room.entry) + fwd * 5f + Vector3.up * 1.7f;
            return new ShotDef { position = p, lookAt = p + fwd * 30f + Vector3.down * 3f };
        }

        /// <summary>Up past the walls at the world beyond: from the entry, looking down the room and up.</summary>
        static ShotDef SkyShot(HellRoomDef room)
        {
            var fwd = Forward(room);
            var p = U(room.entry) + fwd * 4f + Vector3.up * 2.5f;
            var right = new Vector3(fwd.z, 0f, -fwd.x);
            return new ShotDef { position = p, lookAt = p + (fwd + right * 0.35f).normalized * 30f + Vector3.up * 12f };
        }

        /// <summary>A wall where it meets the floor, close: at eye height, turned 62 degrees off the room's axis, looking down a little.</summary>
        static ShotDef WallShot(HellRoomDef room)
        {
            var fwd = Forward(room);
            var p = U(room.entry) + fwd * 6f + Vector3.up * 1.7f;
            var dir = Quaternion.Euler(0f, 62f, 0f) * fwd;
            if (Physics.Raycast(p, dir, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore) && hit.distance > 9f) p += dir * (hit.distance - 7f);
            return new ShotDef { position = p, lookAt = p + dir * 10f + Vector3.down * 2.2f };
        }

        static ShotDef? DarkShot(HellRoomDef room)
        {
            if (room.darkZones == null || room.darkZones.Count == 0) return null;
            var z = room.darkZones[0];
            var c = (U(z.min) + U(z.max)) * 0.5f;
            var toCentre = Centre(room) - c;
            toCentre.y = 0f;
            if (toCentre.sqrMagnitude < 1f) toCentre = -Forward(room);
            var p = c + toCentre.normalized * 12f + Vector3.up * 4f;
            return new ShotDef { position = p, lookAt = c };
        }

        void Frame(ShotDef s, int floor, HellLookSwitch sw, List<SkylineBot> keep = null)
        {
            HideOtherBots(keep);   // the run's own spawns stay out of the stills
            _cam.transform.position = s.position;
            _cam.transform.rotation = Quaternion.LookRotation(s.lookAt - s.position, Vector3.up);
            if (sw != null)
            {
                sw.floorOverride = floor;
                sw.AtmosphereFor(floor);
            }
            _cam.backgroundColor = RenderSettings.fogColor;
            HellBeyond.SkyCamera(_cam);
            HellLightRig.Focus = _cam;
            var rig = Object.FindAnyObjectByType<HellLightRig>();
            if (rig != null) { rig.ShadowBudget(); rig.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); }
        }

        /// <summary>The run director keeps every room but the current one inactive (HellRunDirector.Start): a shot's room is
        /// switched on here, as scenery only (no Begin, so it spawns and ticks nothing).</summary>
        static void ShowRoom(int floor, HellRoomKind kind)
        {
            int n = 0;
            foreach (var r in Object.FindObjectsByType<HellRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r.floor == floor && r.kind == kind && !r.gameObject.activeSelf) { r.gameObject.SetActive(true); n++; }
            Debug.Log("HELL_LOOK_CAPTURE room floor " + (floor + 1) + " " + kind + (n > 0 ? " switched on" : " already on"));
        }

        void HideOtherBots(List<SkylineBot> keep = null)
        {
            foreach (var b in SkylineBot.Live.ToArray())
                if (b != null && !_imps.Contains(b) && (keep == null || !keep.Contains(b))) b.gameObject.SetActive(false);
        }

        /// <summary>An imp 5 m out at eye height, a scythe demon 9 m out to the left and an ooze 7 m out to the right, on the ground.</summary>
        List<SkylineBot> SpawnLineup(ShotDef shot)
        {
            var player = Object.FindAnyObjectByType<SkylinePlayer>();
            var fwd = shot.lookAt - shot.position;
            fwd.y = 0f;
            fwd.Normalize();
            var right = new Vector3(fwd.z, 0f, -fwd.x);
            var list = new List<SkylineBot>();
            var spots = new[] { (SkylineEnemies.DroneId, 5.5f, 0.6f, false), (SkylineEnemies.SpotlightId, 9f, -2.8f, true), (SkylineEnemies.FloorCareId, 7f, 3f, true) };
            ulong seed = 4242;
            foreach (var (id, d, side, ground) in spots)
            {
                var at = shot.position + fwd * d + right * side;
                if (ground && Physics.Raycast(at + Vector3.up * 3f, Vector3.down, out var hit, 12f, ~0, QueryTriggerInteraction.Ignore)) at = hit.point;
                else if (!ground) at += Vector3.down * 0.6f;
                float yaw = Quaternion.LookRotation(-fwd).eulerAngles.y;
                var bot = SkylineBot.Spawn(id, at, yaw, seed++, player, player != null ? player.Writ : null, null);
                if (bot != null) list.Add(bot);
            }
            Debug.Log("HELL_LOOK_CAPTURE lineup " + list.Count);
            return list;
        }

        void SpawnImps(ShotDef shot)
        {
            var player = Object.FindAnyObjectByType<SkylinePlayer>();
            var fwd = shot.lookAt - shot.position;
            fwd.y = 0f;
            fwd.Normalize();
            var right = new Vector3(fwd.z, 0f, -fwd.x);
            var spots = new[] { (12f, -3f, 1.8f), (19f, 4.5f, 3.6f), (26f, -1f, 5f) };
            ulong seed = 9001;
            foreach (var (d, side, up) in spots)
            {
                var at = shot.position + fwd * d + right * side + Vector3.up * (up - 2.6f);
                float yaw = Quaternion.LookRotation(-fwd).eulerAngles.y;
                var bot = SkylineBot.Spawn(SkylineEnemies.DroneId, at, yaw, seed++, player, player != null ? player.Writ : null, null);
                if (bot != null) _imps.Add(bot);
            }
            Debug.Log("HELL_LOOK_CAPTURE imps " + _imps.Count);
        }

        IEnumerator Shot(string path)
        {
            yield return new WaitForEndOfFrame();
            bool submitted = false;
            var request = new RenderPipeline.StandardRequest { destination = _rt };
            if (RenderPipeline.SupportsRenderRequest(_cam, request))
            {
                RenderPipeline.SubmitRenderRequest(_cam, request);
                submitted = true;
            }
            if (!submitted)
            {
                _cam.targetTexture = _rt;
                _cam.Render();
                _cam.targetTexture = null;
            }
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply(false);
            RenderTexture.active = prev;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            float sd = LuminanceSpread(tex, out float mean);
            if (sd < 4f) Debug.LogWarning("HELL_LOOK_CAPTURE WARNING nearly uniform still (luminance mean " + mean.ToString("0.0") + ", std dev " + sd.ToString("0.00") + " of 255): no scenery in shot? " + path);
            else Debug.Log("HELL_LOOK_CAPTURE still luminance mean " + mean.ToString("0.0") + ", std dev " + sd.ToString("0.0"));
            Destroy(tex);
            Debug.Log("HELL_LOOK_CAPTURE wrote " + path + (submitted ? "" : " (camera.Render fallback)"));
        }
    }
}
