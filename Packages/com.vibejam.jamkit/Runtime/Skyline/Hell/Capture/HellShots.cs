using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace JamKit.Skyline
{
    public enum HellShotEase { Linear, Smooth, In, Out, Hold }

    /// <summary>Where a shot's points are: in the world, or around the followed target (its feet; an offset turned by the
    /// target's facing in <see cref="Target"/>, not turned in <see cref="TargetWorld"/>).</summary>
    public enum HellShotSpace { World, Target, TargetWorld }

    /// <summary>What a shot follows: nothing, the player, or the nearest living enemy to the player (picked when the shot
    /// starts and kept).</summary>
    public enum HellShotFollow { None, Player, NearestEnemy }

    /// <summary>One key of a camera path. Times are take seconds (seconds of recorded video, untouched by slow motion).</summary>
    [Serializable]
    public sealed class HellShotKey
    {
        public float t;
        /// <summary>[x, y, z] in the shot's <see cref="HellShot.space"/>.</summary>
        public float[] position;
        /// <summary>[x, y, z] to look at (same space); or leave it out and give <see cref="yaw"/> / <see cref="pitch"/>.</summary>
        public float[] lookAt;
        /// <summary>Degrees: yaw clockwise from +Z (the game's), pitch positive looking down. Used when there is no look-at.</summary>
        public float yaw, pitch;
        /// <summary>Vertical field of view in degrees (0: from <see cref="lens"/>, or the previous key's).</summary>
        public float fov;
        /// <summary>Focal length in mm on a full-frame 24 mm-high sensor (0: use <see cref="fov"/>). 24 wide, 50 normal, 85 tight.</summary>
        public float lens;
        /// <summary>Dutch angle, degrees (positive tilts the horizon clockwise).</summary>
        public float roll;
        /// <summary>The easing of the stretch from this key to the next.</summary>
        public HellShotEase ease = HellShotEase.Smooth;
    }

    [Serializable]
    public sealed class HellShotShake
    {
        /// <summary>Degrees of handheld wobble (0: none), its rough frequency (Hz) and metres of drift.</summary>
        public float amplitude, frequency = 1.1f, position;
        public int seed = 1;
    }

    [Serializable]
    public sealed class HellShotDof
    {
        public bool on;
        /// <summary>Focus on the followed target (true) or at <see cref="distance"/> metres.</summary>
        public bool focusTarget = true;
        public float distance = 6f;
        /// <summary>f-stop (lower: blurrier background).</summary>
        public float aperture = 2.8f;
        /// <summary>Lens focal length for the blur maths (mm); 0: from the shot's field of view.</summary>
        public float focalLength;
    }

    /// <summary>A camera shot (docs/HELL-CAPTURE.md, <c>HellCapture/Shots/&lt;name&gt;.json</c>): a path through keys with
    /// a look-at (or yaw and pitch), lens, roll and easing; optionally following the player or an enemy; handheld shake;
    /// depth of field. Played over a replay.</summary>
    [Serializable]
    public sealed class HellShot
    {
        public string name = "";
        public HellShotSpace space = HellShotSpace.World;
        public HellShotFollow follow = HellShotFollow.None;
        /// <summary>Look at the followed target (plus <see cref="lookOffset"/>) instead of the keys' look-at.</summary>
        public bool lookAtTarget;
        public float[] lookOffset = { 0f, 1.4f, 0f };
        /// <summary>The followed target is smoothed over this long (s; 0: locked on).</summary>
        public float followLag = 0.25f;
        /// <summary>Take the followed target's place once, when the shot starts, and keep it (a locked-off camera set where the
        /// action is).</summary>
        public bool anchorAtStart;
        /// <summary>Catmull-Rom through the key positions (true) or straight lines (false).</summary>
        public bool spline = true;
        public List<HellShotKey> keys = new List<HellShotKey>();
        public HellShotShake shake = new HellShotShake();
        public HellShotDof dof = new HellShotDof();
    }

    /// <summary>A sampled camera: world position, yaw / pitch / roll (degrees, the game's convention), vertical FOV, focus.</summary>
    public struct HellShotPose
    {
        public Vector3 position;
        public float yaw, pitch, roll, fov;
        public bool dof;
        public float focusDistance, aperture, focalLength;
    }

    /// <summary>The shots: easing, lens maths, sampling a shot at a take time, the handheld noise and the loader.</summary>
    public static class HellShots
    {
        public const float DefaultFov = 60f, SensorHeightMm = 24f;
        public const string RelativeRoot = "Assets/JamKit/Templates/SkylineRepo/HellCapture/Shots";

        public static float Ease(HellShotEase e, float u)
        {
            u = u < 0f ? 0f : u > 1f ? 1f : u;
            switch (e)
            {
                case HellShotEase.Linear: return u;
                case HellShotEase.In: return u * u;
                case HellShotEase.Out: return 1f - (1f - u) * (1f - u);
                case HellShotEase.Hold: return 0f;
                default: return u * u * (3f - 2f * u);
            }
        }

        /// <summary>Vertical FOV (degrees) of a focal length on the full-frame sensor.</summary>
        public static float FovOfLens(float mm) => mm > 0f ? (float)(2.0 * Math.Atan(SensorHeightMm * 0.5 / mm) * 180.0 / Math.PI) : DefaultFov;

        public static float LensOfFov(float fov) => (float)(SensorHeightMm * 0.5 / Math.Tan(fov * 0.5 * Math.PI / 180.0));

        static Vector3 V(float[] a, Vector3 fallback) => a != null && a.Length == 3 ? new Vector3(a[0], a[1], a[2]) : fallback;

        /// <summary>Yaw (clockwise from +Z) and pitch (positive down) looking along <paramref name="dir"/>.</summary>
        public static void LookAngles(Vector3 dir, out float yaw, out float pitch)
        {
            float flat = (float)Math.Sqrt(dir.X * dir.X + dir.Z * dir.Z);
            yaw = (float)(Math.Atan2(dir.X, dir.Z) * 180.0 / Math.PI);
            pitch = (float)(-Math.Atan2(dir.Y, flat) * 180.0 / Math.PI);
        }

        /// <summary>Turns <paramref name="v"/> by <paramref name="yawDeg"/> about +Y (the game's yaw: +Z towards +X).</summary>
        public static Vector3 Turn(Vector3 v, float yawDeg)
        {
            double r = yawDeg * Math.PI / 180.0;
            float c = (float)Math.Cos(r), s = (float)Math.Sin(r);
            return new Vector3(v.X * c + v.Z * s, v.Y, -v.X * s + v.Z * c);
        }

        static float LerpAngle(float a, float b, float u)
        {
            float d = (b - a) % 360f;
            if (d > 180f) d -= 360f;
            if (d < -180f) d += 360f;
            return a + d * u;
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float u)
        {
            float u2 = u * u, u3 = u2 * u;
            return 0.5f * (2f * p1 + (p2 - p0) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 + (3f * p1 - p0 - 3f * p2 + p3) * u3);
        }

        /// <summary>The key's field of view: its own, its lens's, else <paramref name="previous"/>.</summary>
        static float KeyFov(HellShotKey k, float previous) => k.fov > 0f ? k.fov : k.lens > 0f ? FovOfLens(k.lens) : previous;

        /// <summary>
        /// The camera at take time <paramref name="t"/>. <paramref name="target"/> / <paramref name="targetYaw"/> are the
        /// followed target's (already smoothed) feet and facing; <paramref name="hasTarget"/> false places target-space points
        /// around the origin. Shake is added by <see cref="Shake"/>, not here.
        /// </summary>
        public static HellShotPose Sample(HellShot shot, float t, Vector3 target, float targetYaw, bool hasTarget)
        {
            var pose = new HellShotPose { fov = DefaultFov };
            if (shot == null || shot.keys == null || shot.keys.Count == 0) return pose;
            var keys = shot.keys;
            int n = keys.Count;
            int i = 0;
            while (i < n - 1 && keys[i + 1].t <= t) i++;
            int j = Math.Min(i + 1, n - 1);
            var a = keys[i];
            var b = keys[j];
            float span = b.t - a.t;
            float u;
            if (i == j || t <= a.t) u = 0f;          // before the first key, or at / past the last
            else if (span <= 1e-5f) u = 1f;          // two keys at one time: a cut
            else u = Ease(a.ease, (t - a.t) / span);

            Vector3 Pos(int k) => V(keys[Math.Max(0, Math.Min(n - 1, k))].position, Vector3.Zero);
            Vector3 local = shot.spline && n > 2 ? CatmullRom(Pos(i - 1), Pos(i), Pos(j), Pos(j + 1), u) : Vector3.Lerp(Pos(i), Pos(j), u);
            Vector3 world = ToWorld(shot.space, local, target, targetYaw, hasTarget);
            pose.position = world;

            // the field of view runs through the keys (a key without one keeps the last)
            float fov = DefaultFov;
            for (int k = 0; k <= i; k++) fov = KeyFov(keys[k], fov);
            float fovB = KeyFov(b, fov);
            pose.fov = fov + (fovB - fov) * u;
            pose.roll = a.roll + (b.roll - a.roll) * u;

            if (shot.lookAtTarget && hasTarget)
            {
                Vector3 look = target + V(shot.lookOffset, new Vector3(0f, 1.4f, 0f));
                LookAngles(look - world, out pose.yaw, out pose.pitch);
            }
            else if (a.lookAt != null || b.lookAt != null)
            {
                Vector3 la = ToWorld(shot.space, V(a.lookAt ?? b.lookAt, Vector3.Zero), target, targetYaw, hasTarget);
                Vector3 lb = ToWorld(shot.space, V(b.lookAt ?? a.lookAt, Vector3.Zero), target, targetYaw, hasTarget);
                LookAngles(Vector3.Lerp(la, lb, u) - world, out pose.yaw, out pose.pitch);
            }
            else
            {
                float yawA = a.yaw + (shot.space == HellShotSpace.Target && hasTarget ? targetYaw : 0f);
                float yawB = b.yaw + (shot.space == HellShotSpace.Target && hasTarget ? targetYaw : 0f);
                pose.yaw = LerpAngle(yawA, yawB, u);
                pose.pitch = a.pitch + (b.pitch - a.pitch) * u;
            }

            var d = shot.dof;
            if (d != null && d.on)
            {
                pose.dof = true;
                pose.aperture = d.aperture > 0f ? d.aperture : 2.8f;
                pose.focalLength = d.focalLength > 0f ? d.focalLength : LensOfFov(pose.fov);
                pose.focusDistance = d.focusTarget && hasTarget
                    ? Math.Max(0.1f, (target + V(shot.lookOffset, new Vector3(0f, 1.4f, 0f)) - world).Length())
                    : Math.Max(0.1f, d.distance);
            }
            return pose;
        }

        static Vector3 ToWorld(HellShotSpace space, Vector3 p, Vector3 target, float targetYaw, bool hasTarget)
        {
            if (!hasTarget || space == HellShotSpace.World) return p;
            return target + (space == HellShotSpace.Target ? Turn(p, targetYaw) : p);
        }

        /// <summary>The shot's length: its last key's time.</summary>
        public static float Length(HellShot shot) => shot == null || shot.keys == null || shot.keys.Count == 0 ? 0f : shot.keys[shot.keys.Count - 1].t;

        // ------------------------------------------------------------------ handheld shake

        static readonly float[] NoiseWeights = { 0.55f, 0.3f, 0.15f };

        /// <summary>Smooth, seeded, repeatable noise in -1..1 (three detuned sines per axis): the same take time always shakes the same.</summary>
        public static float Noise(int seed, int axis, float t)
        {
            uint h = (uint)(seed * 73856093) ^ (uint)(axis * 19349663);
            float sum = 0f;
            var w = NoiseWeights;
            for (int o = 0; o < 3; o++)
            {
                h = h * 1664525u + 1013904223u;
                float phase = (h >> 8) / 16777216f * 6.2831853f;
                h = h * 1664525u + 1013904223u;
                float detune = 0.8f + 0.4f * ((h >> 8) / 16777216f);
                float f = (o == 0 ? 1f : o == 1 ? 2.3f : 5.1f) * detune;
                sum += w[o] * (float)Math.Sin(t * f * 6.2831853f + phase);
            }
            return sum;
        }

        /// <summary>Adds the shot's handheld shake at take time <paramref name="t"/> to <paramref name="pose"/>.</summary>
        public static HellShotPose Shake(HellShot shot, HellShotPose pose, float t)
        {
            var s = shot != null ? shot.shake : null;
            if (s == null || (s.amplitude <= 0f && s.position <= 0f)) return pose;
            float ft = t * Math.Max(0.01f, s.frequency);
            pose.yaw += s.amplitude * Noise(s.seed, 0, ft);
            pose.pitch += s.amplitude * 0.7f * Noise(s.seed, 1, ft);
            pose.roll += s.amplitude * 0.35f * Noise(s.seed, 2, ft);
            pose.position += new Vector3(Noise(s.seed, 3, ft), Noise(s.seed, 4, ft), Noise(s.seed, 5, ft)) * s.position;
            return pose;
        }

        /// <summary>Critically damped follow (the smoothed target): moves <paramref name="current"/> towards
        /// <paramref name="goal"/> over about <paramref name="lag"/> seconds; frame-rate independent.</summary>
        public static Vector3 Follow(Vector3 current, Vector3 goal, float lag, float dt)
        {
            if (lag <= 1e-4f || dt <= 0f) return lag <= 1e-4f ? goal : current;
            float k = 1f - (float)Math.Exp(-dt * 4f / lag);
            return current + (goal - current) * k;
        }

        // ------------------------------------------------------------------ loading

        /// <summary>Checks a shot: keys in time order, positions present, a target for target space or look-at-target.</summary>
        public static void Lint(HellShot shot, List<string> problems)
        {
            string n = shot.name;
            if (shot.keys == null || shot.keys.Count == 0) { problems.Add(n + ": no keys"); return; }
            for (int i = 0; i < shot.keys.Count; i++)
            {
                var k = shot.keys[i];
                if (k.position == null || k.position.Length != 3) problems.Add(n + ": key " + i + " needs a position [x, y, z]");
                if (k.lookAt != null && k.lookAt.Length != 3) problems.Add(n + ": key " + i + " lookAt needs [x, y, z]");
                if (i > 0 && k.t < shot.keys[i - 1].t) problems.Add(n + ": key " + i + " is earlier than the key before it");
                if (k.fov < 0f || k.fov > 170f) problems.Add(n + ": key " + i + " fov " + k.fov);
            }
            if (shot.follow == HellShotFollow.None && (shot.space != HellShotSpace.World || shot.lookAtTarget))
                problems.Add(n + ": target space or lookAtTarget needs a follow (player or nearest-enemy)");
        }

        public static HellShot Parse(string name, string json, List<string> problems)
        {
            var shot = HellCaptureJson.FromJson<HellShot>(json, problems);
            if (string.IsNullOrEmpty(shot.name)) shot.name = name;
            else if (shot.name != name) problems?.Add(name + ": its name is '" + shot.name + "' (it must match the file name)");
            if (problems != null) Lint(shot, problems);
            return shot;
        }

        /// <summary>Finds the shots folder at or above <paramref name="dir"/> (the project).</summary>
        public static string FindRoot(string dir)
        {
            for (var d = new DirectoryInfo(dir); d != null; d = d.Parent)
            {
                string p = Path.Combine(d.FullName, RelativeRoot);
                if (Directory.Exists(p)) return p;
            }
            return null;
        }

        /// <summary>A shot by name (from the shots folder under <paramref name="projectDir"/>) or by path to a .json file.</summary>
        public static HellShot Load(string nameOrPath, string projectDir, List<string> problems)
        {
            string path = nameOrPath;
            if (!nameOrPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                string root = FindRoot(projectDir);
                if (root == null) throw new FileNotFoundException("no " + RelativeRoot + " folder above " + projectDir);
                path = Path.Combine(root, nameOrPath + ".json");
            }
            if (!File.Exists(path)) throw new FileNotFoundException("no shot '" + nameOrPath + "' (" + path + ")");
            return Parse(Path.GetFileNameWithoutExtension(path), File.ReadAllText(path), problems);
        }
    }
}
