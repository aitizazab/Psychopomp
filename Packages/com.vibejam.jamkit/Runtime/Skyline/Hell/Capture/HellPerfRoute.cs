using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace JamKit.Skyline
{
    /// <summary>
    /// The frame-time check's fly-through of one room (docs/HELL-CAPTURE.md): the eye follows the room's safe path (the
    /// lint's route from the entry through every objective stop to the exit) at eye height, at a steady speed, looking a few
    /// metres ahead along the path. Pure, from the room's data, so it never depends on the geometry lanes' meshes.
    /// </summary>
    public sealed class HellPerfRoute
    {
        public const float EyeHeight = 1.6f, LookAhead = 4f;

        public readonly List<Vector3> points = new List<Vector3>();
        readonly List<float> _along = new List<float>();
        public float Length { get; private set; }

        public static HellPerfRoute For(HellRoomDef room)
        {
            var r = new HellPerfRoute();
            r.Add(room.entry);
            if (room.safePath != null) foreach (var p in room.safePath) r.Add(p);
            r.Add(room.exit);
            r.Finish();
            return r;
        }

        public static HellPerfRoute Through(IEnumerable<Vector3> feet)
        {
            var r = new HellPerfRoute();
            foreach (var p in feet) r.Add(p);
            r.Finish();
            return r;
        }

        void Add(Vector3 feet)
        {
            var eye = feet + new Vector3(0f, EyeHeight, 0f);
            if (points.Count > 0 && Vector3.DistanceSquared(points[points.Count - 1], eye) < 1e-4f) return;
            points.Add(eye);
        }

        void Finish()
        {
            _along.Clear();
            float d = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0) d += Vector3.Distance(points[i - 1], points[i]);
                _along.Add(d);
            }
            Length = d;
        }

        /// <summary>The eye <paramref name="distance"/> metres along the route (clamped).</summary>
        public Vector3 At(float distance)
        {
            if (points.Count == 0) return Vector3.Zero;
            if (distance <= 0f || points.Count == 1) return points[0];
            if (distance >= Length) return points[points.Count - 1];
            int i = 1;
            while (i < _along.Count - 1 && _along[i] < distance) i++;
            float seg = _along[i] - _along[i - 1];
            float u = seg > 1e-5f ? (distance - _along[i - 1]) / seg : 1f;
            return Vector3.Lerp(points[i - 1], points[i], u);
        }

        /// <summary>The eye and its view (yaw clockwise from +Z, pitch positive down) at <paramref name="distance"/>.</summary>
        public void Pose(float distance, out Vector3 eye, out float yaw, out float pitch)
        {
            eye = At(distance);
            Vector3 ahead = At(distance + LookAhead);
            Vector3 dir = ahead - eye;
            if (dir.LengthSquared() < 1e-4f && points.Count > 1) dir = points[points.Count - 1] - points[points.Count - 2];
            HellShots.LookAngles(dir, out yaw, out pitch);
            pitch = Math.Max(-20f, Math.Min(20f, pitch));
        }

        /// <summary>Seconds to fly the route at <paramref name="speed"/> m/s.</summary>
        public float Seconds(float speed) => speed > 0f ? Length / speed : 0f;

        /// <summary>"2.SixSigils" and the like (1-based floor) for a room; <see cref="Matches"/> reads the same.</summary>
        public static string Id(int floor, HellRoomKind kind) => (floor + 1).ToString(CultureInfo.InvariantCulture) + "." + kind;

        /// <summary>Is the room in the list ("all" or empty: yes; "2" alone: all of floor 2; "SixSigils": on every floor).</summary>
        public static bool Matches(IList<string> wanted, int floor, HellRoomKind kind)
        {
            if (wanted == null || wanted.Count == 0) return true;
            string id = Id(floor, kind), f = (floor + 1).ToString(CultureInfo.InvariantCulture), k = kind.ToString();
            foreach (var w in wanted)
                if (string.Equals(w, id, StringComparison.OrdinalIgnoreCase) || w == f || string.Equals(w, k, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(w, "all", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }

    /// <summary>One room's frame times in the check.</summary>
    public sealed class HellPerfRoom
    {
        public string id = "";
        public float seconds;
        public FrameSummary frames;
        public int over;   // frames slower than the target
        /// <summary>The slowest frame of the room's first moments (it switching on and its enemies spawning), kept apart
        /// from the measured frames: in play that happens behind the drop into the room.</summary>
        public float enterWorstMs;
    }

    /// <summary>The frame-time check's result, as perf.json: per room and over all of them, the average, the 1% low (the mean of
    /// the slowest 1% of frames) and the worst frame in ms, and whether each meets the target (its 1% low within the target's
    /// frame time: 8.33 ms at 120 fps).</summary>
    public sealed class HellPerfReport
    {
        public int targetFps = 120;
        public string quality = "", resolution = "", note = "";
        public readonly List<HellPerfRoom> rooms = new List<HellPerfRoom>();
        public FrameSummary all;

        public float TargetMs => targetFps > 0 ? 1000f / targetFps : 0f;

        public static bool Meets(FrameSummary s, float targetMs) => s.frames > 0 && s.onePercentLowMs <= targetMs;

        public static int CountOver(IReadOnlyList<float> frameSeconds, float targetMs)
        {
            int n = 0;
            for (int i = 0; i < frameSeconds.Count; i++) if (frameSeconds[i] * 1000f > targetMs) n++;
            return n;
        }

        /// <summary>The room with the worst 1% low (null with none).</summary>
        public HellPerfRoom Worst()
        {
            HellPerfRoom w = null;
            foreach (var r in rooms) if (w == null || r.frames.onePercentLowMs > w.frames.onePercentLowMs) w = r;
            return w;
        }

        static string F(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);

        static string Q(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                else sb.Append(c);
            }
            return sb.Append('"').ToString();
        }

        static void Summary(StringBuilder sb, FrameSummary s, float targetMs)
        {
            sb.Append("\"frames\":").Append(s.frames)
              .Append(",\"avgMs\":").Append(F(s.avgMs))
              .Append(",\"avgFps\":").Append(F(s.avgMs > 0f ? 1000f / s.avgMs : 0f))
              .Append(",\"onePercentLowMs\":").Append(F(s.onePercentLowMs))
              .Append(",\"onePercentLowFps\":").Append(F(s.onePercentLowMs > 0f ? 1000f / s.onePercentLowMs : 0f))
              .Append(",\"worstMs\":").Append(F(s.worstMs))
              .Append(",\"meetsTarget\":").Append(Meets(s, targetMs) ? "true" : "false");
        }

        public string ToJson()
        {
            float t = TargetMs;
            var sb = new StringBuilder("{");
            sb.Append("\"targetFps\":").Append(targetFps).Append(",\"targetMs\":").Append(F(t));
            sb.Append(",\"quality\":").Append(Q(quality)).Append(",\"resolution\":").Append(Q(resolution)).Append(",\"note\":").Append(Q(note));
            sb.Append(",\"all\":{");
            Summary(sb, all, t);
            sb.Append("}");
            var w = Worst();
            sb.Append(",\"worstRoom\":").Append(w != null ? Q(w.id) : "null");
            bool ok = rooms.Count > 0;
            foreach (var r in rooms) ok &= Meets(r.frames, t);
            sb.Append(",\"allRoomsMeetTarget\":").Append(ok ? "true" : "false");
            sb.Append(",\"rooms\":[");
            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":").Append(Q(r.id)).Append(",\"seconds\":").Append(F(r.seconds)).Append(",\"framesOverTarget\":").Append(r.over).Append(",\"enterWorstMs\":").Append(F(r.enterWorstMs)).Append(',');
                Summary(sb, r.frames, t);
                sb.Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }
    }
}
