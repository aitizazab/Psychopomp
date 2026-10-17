using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JamKit.Skyline
{
    /// <summary>The buttons of one recorded frame (the player's input frame plus the Hell abilities' own buttons).</summary>
    [Flags]
    public enum HellReplayButtons : uint
    {
        None = 0,
        JumpHeld = 1 << 0, JumpPressed = 1 << 1,
        CrouchHeld = 1 << 2, CrouchPressed = 1 << 3,
        SprintHeld = 1 << 4, SprintTogglePressed = 1 << 5,
        FireHeld = 1 << 6, FirePressed = 1 << 7,
        AimHeld = 1 << 8,
        ReloadPressed = 1 << 9, SwapPressed = 1 << 10, InspectPressed = 1 << 11,
        StepHeld = 1 << 12, RewindPressed = 1 << 13, SkillPressed = 1 << 14,
    }

    /// <summary>One fixed step of the creator's play: the stick, the buttons, where the view pointed after the frame's look,
    /// and where the feet were after the frame (the replay's drift check and correction).</summary>
    public struct HellReplayFrame
    {
        public float moveX, moveY;
        public HellReplayButtons buttons;
        public float yaw, pitch;
        public float x, y, z;

        public bool Has(HellReplayButtons b) => (buttons & b) == b;
    }

    /// <summary>Something that happened between steps that the replay must do again: a floor skill picked on the pick screen
    /// ("grant", the skill id), applied before step <see cref="frame"/> runs.</summary>
    public struct HellReplayEvent
    {
        public int frame;
        public string kind;
        public string value;
    }

    /// <summary>
    /// A recording of the creator's play (docs/HELL-CAPTURE.md): every fixed step's input, view and feet, the run's seed and
    /// the skill picks. The game runs on a fixed step while recording (<see cref="fps"/>), so replaying the same input on the
    /// same step from the same seed plays the same run again; the stored view and feet keep the player exact even if
    /// something drifts. Frame 0 is the frame the run started (HellRunEvents.RunStarted). Pure; a small binary file.
    /// </summary>
    public sealed class HellReplay
    {
        public const uint Magic = 0x50455248;   // "HREP"
        public const int Version = 1;

        public int fps = HellCapture.DefaultFps;
        public int seed;
        public string scene = "";
        public string note = "";
        public readonly List<HellReplayFrame> frames = new List<HellReplayFrame>();
        public readonly List<HellReplayEvent> events = new List<HellReplayEvent>();

        public int FrameCount => frames.Count;
        public float Step => HellCapture.Step(fps);
        public float Seconds => fps > 0 ? frames.Count / (float)fps : 0f;

        /// <summary>The step that starts at <paramref name="seconds"/> (clamped to 0..FrameCount).</summary>
        public int FrameAt(float seconds)
        {
            if (fps <= 0 || seconds <= 0f) return 0;
            int f = (int)Math.Round(seconds * fps);
            return Math.Min(f, frames.Count);
        }

        public float TimeOf(int frame) => fps > 0 ? frame / (float)fps : 0f;

        /// <summary>The events due before step <paramref name="frame"/>, added to <paramref name="into"/> in order.</summary>
        public void EventsAt(int frame, List<HellReplayEvent> into)
        {
            for (int i = 0; i < events.Count; i++) if (events[i].frame == frame) into.Add(events[i]);
        }

        public void AddEvent(int frame, string kind, string value) => events.Add(new HellReplayEvent { frame = frame, kind = kind ?? "", value = value ?? "" });

        // ------------------------------------------------------------------ the file

        public void Write(Stream s)
        {
            using (var w = new BinaryWriter(s, Encoding.UTF8, true))
            {
                w.Write(Magic);
                w.Write(Version);
                w.Write(fps);
                w.Write(seed);
                w.Write(scene ?? "");
                w.Write(note ?? "");
                w.Write(frames.Count);
                for (int i = 0; i < frames.Count; i++)
                {
                    var f = frames[i];
                    w.Write(f.moveX); w.Write(f.moveY);
                    w.Write((uint)f.buttons);
                    w.Write(f.yaw); w.Write(f.pitch);
                    w.Write(f.x); w.Write(f.y); w.Write(f.z);
                }
                w.Write(events.Count);
                for (int i = 0; i < events.Count; i++)
                {
                    w.Write(events[i].frame);
                    w.Write(events[i].kind ?? "");
                    w.Write(events[i].value ?? "");
                }
            }
        }

        public static HellReplay Read(Stream s)
        {
            using (var r = new BinaryReader(s, Encoding.UTF8, true))
            {
                if (r.ReadUInt32() != Magic) throw new InvalidDataException("not a Hell replay");
                int version = r.ReadInt32();
                if (version != Version) throw new InvalidDataException("replay version " + version + " (this build reads " + Version + ")");
                var rep = new HellReplay { fps = r.ReadInt32(), seed = r.ReadInt32(), scene = r.ReadString(), note = r.ReadString() };
                if (!HellCapture.ValidFps(rep.fps)) throw new InvalidDataException("replay fps " + rep.fps);
                int n = r.ReadInt32();
                if (n < 0 || n > 50000000) throw new InvalidDataException("replay frame count " + n);
                rep.frames.Capacity = n;
                for (int i = 0; i < n; i++)
                    rep.frames.Add(new HellReplayFrame
                    {
                        moveX = r.ReadSingle(), moveY = r.ReadSingle(),
                        buttons = (HellReplayButtons)r.ReadUInt32(),
                        yaw = r.ReadSingle(), pitch = r.ReadSingle(),
                        x = r.ReadSingle(), y = r.ReadSingle(), z = r.ReadSingle(),
                    });
                int e = r.ReadInt32();
                if (e < 0 || e > 100000) throw new InvalidDataException("replay event count " + e);
                for (int i = 0; i < e; i++) rep.events.Add(new HellReplayEvent { frame = r.ReadInt32(), kind = r.ReadString(), value = r.ReadString() });
                return rep;
            }
        }

        public void Save(string path)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string tmp = path + ".tmp";
            using (var f = File.Create(tmp)) Write(f);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public static HellReplay Load(string path)
        {
            using (var f = File.OpenRead(path)) return Read(f);
        }

        /// <summary>One line for the log: length, steps, seed, picks.</summary>
        public string Describe() =>
            Seconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s (" + frames.Count + " steps at " + fps +
            " fps), seed " + seed + ", scene " + (scene.Length > 0 ? scene : "?") + ", " + events.Count + " event(s)";
    }

    /// <summary>
    /// Keeping a replayed player on the recording: <see cref="Check"/> says whether the feet have drifted far enough to put
    /// back (a fixed-step run of the same input normally never drifts; a recording made in a build and replayed in the editor
    /// can, by float differences), and keeps the worst drift for the log.
    /// </summary>
    public sealed class HellReplayDrift
    {
        /// <summary>Metres of drift tolerated before the feet are put back on the recording.</summary>
        public float tolerance = 0.25f;
        public float Worst { get; private set; }
        public int Corrections { get; private set; }

        public bool Check(float dx, float dy, float dz)
        {
            float d = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (d > Worst) Worst = d;
            if (d <= tolerance) return false;
            Corrections++;
            return true;
        }
    }
}
