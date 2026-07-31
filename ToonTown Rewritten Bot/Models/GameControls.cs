using System;
using WindowsInput;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Models
{
    /// <summary>
    /// Holds the player's in-game Toontown Rewritten movement key bindings.
    ///
    /// The bot's services are written against TTR's default controls (arrow keys for
    /// movement, Control for jump). When a user has rebound those controls in TTR's
    /// Options &amp; Codes → Controls screen, the bot would otherwise send the wrong keys.
    /// Every movement/jump key the bot sends is funneled through <see cref="Remap"/> at
    /// the input layer, which translates the hardcoded default key into the key the user
    /// actually has bound. With default bindings <see cref="Remap"/> is the identity, so
    /// existing behavior is unchanged unless the user customizes their controls.
    ///
    /// Fields are <c>volatile</c> because they are written from the UI thread (when the
    /// user saves new bindings) and read from background bot threads while input is sent.
    /// </summary>
    public static class GameControls
    {
        public static volatile VirtualKeyCode Forward = VirtualKeyCode.UP;
        public static volatile VirtualKeyCode Reverse = VirtualKeyCode.DOWN;
        public static volatile VirtualKeyCode Left = VirtualKeyCode.LEFT;
        public static volatile VirtualKeyCode Right = VirtualKeyCode.RIGHT;
        public static volatile VirtualKeyCode Jump = VirtualKeyCode.CONTROL;

        /// <summary>
        /// Translates one of the bot's default control keys into the key the user has
        /// bound in TTR. Any key that isn't a movement/jump control passes through
        /// unchanged, so callers can safely wrap every key they send.
        /// </summary>
        public static VirtualKeyCode Remap(VirtualKeyCode key)
        {
            return key switch
            {
                VirtualKeyCode.UP => Forward,
                VirtualKeyCode.DOWN => Reverse,
                VirtualKeyCode.LEFT => Left,
                VirtualKeyCode.RIGHT => Right,
                VirtualKeyCode.CONTROL => Jump,
                _ => key
            };
        }

        /// <summary>
        /// Maps a raw Windows virtual-key code from the global recorder to a movement action.
        /// This keeps custom-action recording aligned with the bindings configured in the app.
        /// </summary>
        public static string GetMovementAction(int virtualKeyCode)
        {
            if (virtualKeyCode == (int)Forward) return "WALK FORWARDS";
            if (virtualKeyCode == (int)Reverse) return "WALK BACKWARDS";
            if (virtualKeyCode == (int)Left) return "TURN LEFT";
            if (virtualKeyCode == (int)Right) return "TURN RIGHT";
            return null;
        }

        public static string GetMovementBindingSummary()
        {
            return $"{GetDisplayName(Forward)}, {GetDisplayName(Reverse)}, " +
                   $"{GetDisplayName(Left)}, {GetDisplayName(Right)}";
        }

        /// <summary>
        /// Applies the saved bindings from <see cref="UserPreferences"/> to the runtime fields.
        /// Unparseable or empty values fall back to the TTR default for that control.
        /// </summary>
        public static void LoadFrom(UserPreferences prefs)
        {
            Forward = Parse(prefs.ControlForward, VirtualKeyCode.UP);
            Reverse = Parse(prefs.ControlReverse, VirtualKeyCode.DOWN);
            Left = Parse(prefs.ControlLeft, VirtualKeyCode.LEFT);
            Right = Parse(prefs.ControlRight, VirtualKeyCode.RIGHT);
            Jump = Parse(prefs.ControlJump, VirtualKeyCode.CONTROL);
        }

        /// <summary>
        /// Writes the current runtime bindings back into <see cref="UserPreferences"/>
        /// (as enum-name strings) so they persist across sessions.
        /// </summary>
        public static void SaveTo(UserPreferences prefs)
        {
            prefs.ControlForward = Forward.ToString();
            prefs.ControlReverse = Reverse.ToString();
            prefs.ControlLeft = Left.ToString();
            prefs.ControlRight = Right.ToString();
            prefs.ControlJump = Jump.ToString();
        }

        /// <summary>
        /// Resets the runtime bindings to TTR's defaults.
        /// </summary>
        public static void ResetToDefaults()
        {
            Forward = VirtualKeyCode.UP;
            Reverse = VirtualKeyCode.DOWN;
            Left = VirtualKeyCode.LEFT;
            Right = VirtualKeyCode.RIGHT;
            Jump = VirtualKeyCode.CONTROL;
        }

        private static VirtualKeyCode Parse(string value, VirtualKeyCode fallback)
        {
            if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, out VirtualKeyCode parsed))
            {
                return parsed;
            }
            return fallback;
        }

        /// <summary>
        /// Returns a friendly, human-readable name for a key (e.g. "Up Arrow", "Control",
        /// "W") for display in the controls UI.
        /// </summary>
        public static string GetDisplayName(VirtualKeyCode key)
        {
            switch (key)
            {
                case VirtualKeyCode.UP: return "Up Arrow";
                case VirtualKeyCode.DOWN: return "Down Arrow";
                case VirtualKeyCode.LEFT: return "Left Arrow";
                case VirtualKeyCode.RIGHT: return "Right Arrow";
                case VirtualKeyCode.CONTROL:
                case VirtualKeyCode.LCONTROL:
                case VirtualKeyCode.RCONTROL: return "Control";
                case VirtualKeyCode.SHIFT:
                case VirtualKeyCode.LSHIFT:
                case VirtualKeyCode.RSHIFT: return "Shift";
                case VirtualKeyCode.SPACE: return "Space";
                case VirtualKeyCode.RETURN: return "Enter";
                case VirtualKeyCode.TAB: return "Tab";
            }

            string name = key.ToString();

            // Letter keys are VK_A..VK_Z; digit keys are VK_0..VK_9.
            if (name.StartsWith("VK_"))
            {
                return name.Substring(3);
            }

            return name;
        }
    }
}
