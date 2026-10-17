using System;
using System.Collections.Generic;

namespace JamKit.Skyline
{
    /// <summary>
    /// The ability wheel's maths (docs/HELL-UI.md, "The ability wheel"), pure. Slots go clockwise from twelve o'clock; slot 0's
    /// middle is straight up. Input is a direction with x right and y UP (a stick, or the wheel's virtual mouse cursor).
    /// </summary>
    public static class HellWheel
    {
        /// <summary>The stick has to lean this far (0..1) before it points at a slot.</summary>
        public const float StickDeadZone = 0.4f;
        /// <summary>The virtual mouse cursor: its reach (panel units of mouse travel) and the dead zone in its middle.</summary>
        public const float CursorReach = 140f, CursorDeadZone = 36f;

        /// <summary>The slot <paramref name="x"/>, <paramref name="y"/> points at, or -1 inside the dead zone (or no slots).</summary>
        public static int Pick(float x, float y, int count, float deadZone)
        {
            if (count <= 0 || float.IsNaN(x) || float.IsNaN(y)) return -1;
            if (x * x + y * y < deadZone * deadZone) return -1;
            double deg = Math.Atan2(x, y) * 180.0 / Math.PI;      // 0 up, 90 right (clockwise)
            if (deg < 0) deg += 360.0;
            double step = 360.0 / count;
            int i = (int)Math.Floor((deg + step * 0.5) / step);
            return ((i % count) + count) % count;
        }

        /// <summary>Slot <paramref name="i"/>'s middle, in degrees clockwise from twelve o'clock.</summary>
        public static float SlotAngle(int i, int count) => count <= 0 ? 0f : 360f * i / count;

        /// <summary>Slot <paramref name="i"/>'s middle on a circle of <paramref name="radius"/> round the wheel's centre, in
        /// screen space (x right, y DOWN).</summary>
        public static void SlotPosition(int i, int count, float radius, out float x, out float y)
        {
            double a = SlotAngle(i, count) * Math.PI / 180.0;
            x = (float)(Math.Sin(a) * radius);
            y = (float)(-Math.Cos(a) * radius);
        }
    }

    /// <summary>What the wheel points at while it is open: the mouse moves a virtual cursor (clamped to
    /// <see cref="HellWheel.CursorReach"/>), the stick points directly. Moving back to the middle keeps the last slot, so
    /// letting go of the stick (it springs back) still picks what it pointed at.</summary>
    public sealed class HellWheelSelector
    {
        public int Count { get; private set; }
        public int Selected { get; private set; } = -1;
        public float CursorX { get; private set; }
        public float CursorY { get; private set; }

        public void Open(int count, int start = -1)
        {
            Count = Math.Max(0, count);
            Selected = start >= 0 && start < Count ? start : -1;
            CursorX = CursorY = 0f;
        }

        /// <summary>Mouse travel this frame (x right, y up, panel units).</summary>
        public int Mouse(float dx, float dy)
        {
            if (float.IsNaN(dx) || float.IsNaN(dy)) return Selected;
            float x = CursorX + dx, y = CursorY + dy;
            float m = (float)Math.Sqrt(x * x + y * y);
            if (m > HellWheel.CursorReach) { x *= HellWheel.CursorReach / m; y *= HellWheel.CursorReach / m; }
            CursorX = x;
            CursorY = y;
            int p = HellWheel.Pick(x, y, Count, HellWheel.CursorDeadZone);
            if (p >= 0) Selected = p;
            return Selected;
        }

        /// <summary>The stick (x right, y up, -1..1).</summary>
        public int Stick(float x, float y)
        {
            int p = HellWheel.Pick(x, y, Count, HellWheel.StickDeadZone);
            if (p >= 0) Selected = p;
            return Selected;
        }

        /// <summary>One step round (keyboard or D-pad nudges), wrapping.</summary>
        public int Step(int dir)
        {
            if (Count <= 0) return Selected;
            Selected = Selected < 0 ? (dir >= 0 ? 0 : Count - 1) : ((Selected + Math.Sign(dir)) % Count + Count) % Count;
            return Selected;
        }
    }

    /// <summary>Which active floor skill the Skill button fires (the wheel binds it). Passive skills and the three gifts
    /// (Shadow Step, Rewind, Time Dilation, which have their own buttons) are never bound. The first active skill granted
    /// is bound by itself, so the button works without ever opening the wheel.</summary>
    public sealed class HellSkillBinding
    {
        public string Bound { get; private set; }

        public void Reset() => Bound = null;

        /// <summary>A skill was granted: an active one is bound when nothing is.</summary>
        public void Granted(string id, bool active)
        {
            if (active && Bound == null && !string.IsNullOrEmpty(id)) Bound = id;
        }

        /// <summary>The wheel picked <paramref name="id"/>: binds it if it is active. True when the binding changed.</summary>
        public bool Pick(string id, bool active)
        {
            if (!active || string.IsNullOrEmpty(id) || id == Bound) return false;
            Bound = id;
            return true;
        }

        /// <summary>Does the Skill button fire <paramref name="id"/>? Yes when it is bound, or when nothing is bound.</summary>
        public bool Fires(string id) => Bound == null || Bound == id;
    }

    /// <summary>One slot of the wheel.</summary>
    public struct HellWheelSlot
    {
        public string id;
        /// <summary>The three gifts (Shadow Step, Rewind, Time Dilation): their own buttons, shown for their state.</summary>
        public bool gift;
        /// <summary>An active floor skill (the Skill button fires the bound one).</summary>
        public bool active;
    }

    /// <summary>The wheel's words for the three gifts and its state lines (UI words, not dialogue; the floor skills use their
    /// own names and descriptions from the skill catalogue). State strings are made once and kept, so the open wheel makes
    /// no garbage.</summary>
    public static class HellWheelText
    {
        public const string Passive = "PASSIVE", Ready = "READY", Active = "ACTIVE", Bound = "ON THE SKILL BUTTON", Bind = "RELEASE TO BIND";

        public static string GiftName(string id) =>
            id == HellWheelSlots.ShadowStep ? "Shadow Step" : id == HellWheelSlots.Rewind ? "Rewind" : id == HellWheelSlots.Dilation ? "Time Dilation" : "";

        public static string GiftDescription(string id) =>
            id == HellWheelSlots.ShadowStep ? "Hold to aim, release to arrive. Only into darkness."
            : id == HellWheelSlots.Rewind ? "Pull you, your foes and their shots back a few seconds, then heal a quarter."
            : id == HellWheelSlots.Dilation ? "Aim while moving fast: the faster you go, the slower the world, while the gauge lasts."
            : "";

        static readonly string[] SecondsCache = new string[121];
        static readonly string[,] ChargesCache = new string[5, 5];

        /// <summary>"8 s" (whole seconds, rounded up, 0..120).</summary>
        public static string Seconds(float s)
        {
            int n = (int)Math.Ceiling(Math.Max(0f, Math.Min(120f, float.IsNaN(s) ? 0f : s)));
            return SecondsCache[n] ?? (SecondsCache[n] = n + " s");
        }

        /// <summary>"1 / 2" (charges of max, 0..4).</summary>
        public static string Charges(int have, int max)
        {
            have = Math.Max(0, Math.Min(4, have));
            max = Math.Max(0, Math.Min(4, max));
            return ChargesCache[have, max] ?? (ChargesCache[have, max] = have + " / " + max);
        }
    }

    public static class HellWheelSlots
    {
        public const string ShadowStep = "gift.shadowstep", Rewind = "gift.rewind", Dilation = "gift.dilation";

        /// <summary>The wheel's slots: the three gifts first (Shadow Step at twelve o'clock), then the owned floor skills in
        /// the order of <paramref name="skillOrder"/> (the catalogue's), at most <paramref name="max"/> slots.</summary>
        public static List<HellWheelSlot> Build(IEnumerable<string> skillOrder, Func<string, bool> owned, Func<string, bool> active, int max = 12)
        {
            var l = new List<HellWheelSlot>
            {
                new HellWheelSlot { id = ShadowStep, gift = true },
                new HellWheelSlot { id = Rewind, gift = true },
                new HellWheelSlot { id = Dilation, gift = true },
            };
            if (skillOrder != null)
                foreach (var id in skillOrder)
                {
                    if (l.Count >= max) break;
                    if (owned != null && owned(id)) l.Add(new HellWheelSlot { id = id, active = active != null && active(id) });
                }
            return l;
        }
    }
}
