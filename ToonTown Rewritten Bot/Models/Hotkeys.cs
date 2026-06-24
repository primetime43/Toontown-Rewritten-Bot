using System;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Models
{
    /// <summary>
    /// Holds the global hotkeys that control the bot (stop and pause). These are matched
    /// against key presses seen by the global keyboard hook, so they work even while TTR
    /// (or any other window) has focus.
    ///
    /// The keys are user-configurable via <see cref="Views.HotkeysForm"/> and persisted to
    /// <see cref="UserPreferences"/>. Esc is offered as an optional stop key on its own toggle:
    /// it is convenient in-game but, because the bot can run in the background, leaving it on
    /// means pressing Esc in any other application also stops the bot — so users who hit that
    /// can turn it off and rely on the dedicated stop key instead.
    ///
    /// Fields are <c>volatile</c> because they are written from the UI thread (when the user
    /// saves new hotkeys) and read from the keyboard-hook callback thread.
    /// </summary>
    public static class Hotkeys
    {
        public static volatile Keys Stop = Keys.F12;
        public static volatile Keys Pause = Keys.F11;
        public static volatile bool AllowEscToStop = true;

        /// <summary>Returns true if the given key should stop active tasks.</summary>
        public static bool IsStop(Keys key)
        {
            return key == Stop || (AllowEscToStop && key == Keys.Escape);
        }

        /// <summary>Returns true if the given key should toggle pause/resume.</summary>
        public static bool IsPause(Keys key)
        {
            return key == Pause;
        }

        /// <summary>
        /// Applies the saved hotkeys from <see cref="UserPreferences"/> to the runtime fields.
        /// Unparseable or empty values fall back to the defaults (F12 stop, F11 pause).
        /// </summary>
        public static void LoadFrom(UserPreferences prefs)
        {
            Stop = Parse(prefs.HotkeyStop, Keys.F12);
            Pause = Parse(prefs.HotkeyPause, Keys.F11);
            AllowEscToStop = prefs.HotkeyAllowEscToStop;
        }

        /// <summary>
        /// Writes the current runtime hotkeys back into <see cref="UserPreferences"/>
        /// (as enum-name strings) so they persist across sessions.
        /// </summary>
        public static void SaveTo(UserPreferences prefs)
        {
            prefs.HotkeyStop = Stop.ToString();
            prefs.HotkeyPause = Pause.ToString();
            prefs.HotkeyAllowEscToStop = AllowEscToStop;
        }

        /// <summary>Resets the runtime hotkeys to their defaults.</summary>
        public static void ResetToDefaults()
        {
            Stop = Keys.F12;
            Pause = Keys.F11;
            AllowEscToStop = true;
        }

        private static Keys Parse(string value, Keys fallback)
        {
            if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, out Keys parsed))
            {
                return parsed;
            }
            return fallback;
        }

        /// <summary>
        /// Returns a friendly, human-readable name for a key (e.g. "Esc", "1", "A") for
        /// display in the hotkeys UI. Falls back to the enum name for uncommon keys.
        /// </summary>
        public static string GetDisplayName(Keys key)
        {
            switch (key)
            {
                case Keys.Escape: return "Esc";
                case Keys.Space: return "Space";
                case Keys.Return: return "Enter";
                case Keys.Back: return "Backspace";
                case Keys.ControlKey: return "Control";
                case Keys.ShiftKey: return "Shift";
                case Keys.Menu: return "Alt";
            }

            // Digit keys are D0..D9; numpad digits are NumPad0..NumPad9.
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                return ((char)('0' + (key - Keys.D0))).ToString();
            }

            return key.ToString();
        }
    }
}
