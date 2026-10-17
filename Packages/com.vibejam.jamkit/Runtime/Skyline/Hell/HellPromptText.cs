using System;
using System.Text;

namespace JamKit.Skyline
{
    /// <summary>
    /// Fills a hint's input tokens for the device the player is using (pure). <c>{ShadowStep}</c> (any
    /// <see cref="BindAction"/> name) becomes the bound control in brackets, read from the binding table so a rebind shows
    /// ("[E]", "[RB / R1]", "[RIGHT MOUSE]"). <c>{kbm:words|pad:words}</c> picks the words for the device. An unknown token
    /// is left as it is, so a typo shows on screen rather than vanishing.
    /// </summary>
    public static class HellPromptText
    {
        /// <summary>The device to label: the pad when it was used more recently than the keyboard or mouse (times in any
        /// one clock; never used = a negative time).</summary>
        public static BindDevice DeviceFor(double lastPadInput, double lastKeyboardMouseInput) =>
            lastPadInput > lastKeyboardMouseInput ? BindDevice.Pad : BindDevice.KeyboardMouse;

        public static string Fill(string template, BindingTable table, BindDevice device)
        {
            if (string.IsNullOrEmpty(template)) return template ?? "";
            var sb = new StringBuilder(template.Length + 16);
            int i = 0;
            while (i < template.Length)
            {
                char c = template[i];
                int close = c == '{' ? template.IndexOf('}', i + 1) : -1;
                if (close < 0) { sb.Append(c); i++; continue; }
                string token = template.Substring(i + 1, close - i - 1);
                string filled = Token(token, table, device);
                sb.Append(filled ?? template.Substring(i, close - i + 1));
                i = close + 1;
            }
            return sb.ToString();
        }

        /// <summary>The words for one token, or null when it isn't one.</summary>
        static string Token(string token, BindingTable table, BindDevice device)
        {
            if (token.StartsWith("kbm:", StringComparison.Ordinal) || token.StartsWith("pad:", StringComparison.Ordinal))
            {
                string kbm = null, pad = null;
                foreach (var part in token.Split('|'))
                {
                    if (part.StartsWith("kbm:", StringComparison.Ordinal)) kbm = part.Substring(4);
                    else if (part.StartsWith("pad:", StringComparison.Ordinal)) pad = part.Substring(4);
                }
                return device == BindDevice.Pad ? (pad ?? kbm ?? "") : (kbm ?? pad ?? "");
            }
            BindAction action;
            if (!Enum.TryParse(token, false, out action) || !Enum.IsDefined(typeof(BindAction), action)) return null;
            string binding = table != null ? table.Get(device, action) : BindingTable.DefaultPath(device, action);
            return "[" + BindingNames.Display(binding).ToUpperInvariant() + "]";
        }
    }
}
