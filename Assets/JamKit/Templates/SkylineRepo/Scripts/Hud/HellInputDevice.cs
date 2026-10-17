using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The device the player used last (a pad stick or button vs a key, mouse button or mouse movement), polled once a frame,
    /// and short control labels for the HUD ("E", "V", "RB", "LT", "D-PAD LEFT") cached per device and action: rebuilt only
    /// when the device or the bindings change, so the HUD makes no garbage. The tutorial and the ending track their own
    /// device the same way (HellTutorial, HellEndingDirector).
    /// </summary>
    public static class HellInputDevice
    {
        static double _lastPad = -1, _lastKbm;
        static int _frame = -1;
        static readonly Dictionary<int, string> Labels = new Dictionary<int, string>();
        static bool _hooked;

        public static BindDevice Current
        {
            get
            {
                Poll();
                return HellPromptText.DeviceFor(_lastPad, _lastKbm);
            }
        }

        static void Poll()
        {
            if (_frame == Time.frameCount) return;
            _frame = Time.frameCount;
            double t = Time.unscaledTimeAsDouble;
            var pad = Gamepad.current;
            if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.16f || pad.rightStick.ReadValue().sqrMagnitude > 0.16f
                || pad.buttonSouth.isPressed || pad.buttonEast.isPressed || pad.buttonWest.isPressed || pad.buttonNorth.isPressed
                || pad.rightTrigger.isPressed || pad.leftTrigger.isPressed || pad.rightShoulder.isPressed || pad.leftShoulder.isPressed
                || pad.dpad.ReadValue().sqrMagnitude > 0.25f || pad.startButton.isPressed)) _lastPad = t;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if ((kb != null && kb.anyKey.isPressed) || (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 4f || mouse.leftButton.isPressed || mouse.rightButton.isPressed)))
                _lastKbm = t;
        }

        /// <summary>The bound control for <paramref name="a"/> on the device in use, short and upper case: "E", "RIGHT MOUSE",
        /// "RB" (a pad name's first half: "RB / R1" gives "RB").</summary>
        public static string Short(BindAction a) => Short(a, Current);

        public static string Short(BindAction a, BindDevice d)
        {
            if (!_hooked)
            {
                _hooked = true;
                SkylineBindings.Changed += Labels.Clear;
            }
            int key = (int)d * 64 + (int)a;
            if (Labels.TryGetValue(key, out var s)) return s;
            string full = BindingNames.Display(SkylineBindings.Table.Get(d, a));
            int slash = d == BindDevice.Pad ? full.IndexOf(" / ", System.StringComparison.Ordinal) : -1;
            s = (slash > 0 ? full.Substring(0, slash) : full).ToUpperInvariant();
            Labels[key] = s;
            return s;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _lastPad = -1;
            _lastKbm = 0;
            _frame = -1;
            Labels.Clear();
            _hooked = false;
        }
    }
}
