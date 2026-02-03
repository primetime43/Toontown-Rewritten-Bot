using System.Collections.Generic;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Models
{
    public class FishingActionKeys
    {
        // Property to hold the mapping from action names to VirtualKeyCode
        public Dictionary<string, VirtualKeyCode> ActionKeyMap { get; private set; }

        // Property to hold string names for saving to JSON
        public Dictionary<string, string> ActionKeyStringMap { get; private set; }

        // Reverse mapping from string names to VirtualKeyCode for loading from JSON
        public Dictionary<string, VirtualKeyCode> StringToKeyCodeMap { get; private set; }

        // Constructor to initialize the mappings
        public FishingActionKeys()
        {
            ActionKeyMap = new Dictionary<string, VirtualKeyCode>
            {
                {"WALK FORWARDS", VirtualKeyCode.UP},
                {"WALK BACKWARDS", VirtualKeyCode.DOWN},
                {"TURN LEFT", VirtualKeyCode.LEFT},
                {"TURN RIGHT", VirtualKeyCode.RIGHT},
            };

            ActionKeyStringMap = new Dictionary<string, string>
            {
                {"WALK FORWARDS", "UP"},
                {"WALK BACKWARDS", "DOWN"},
                {"TURN LEFT", "LEFT"},
                {"TURN RIGHT", "RIGHT"},
                {"SELL FISH", "SELL"},
            };

            StringToKeyCodeMap = new Dictionary<string, VirtualKeyCode>
            {
                {"UP", VirtualKeyCode.UP},
                {"DOWN", VirtualKeyCode.DOWN},
                {"LEFT", VirtualKeyCode.LEFT},
                {"RIGHT", VirtualKeyCode.RIGHT},
            };
        }

        // Method to get VirtualKeyCode by action name
        public VirtualKeyCode? GetKeyCode(string action)
        {
            if (ActionKeyMap.TryGetValue(action, out var keyCode))
            {
                return keyCode;
            }
            return null;
        }

        // Method to get VirtualKeyCode by string command (e.g., "DOWN", "UP")
        public VirtualKeyCode? GetKeyCodeFromString(string command)
        {
            if (StringToKeyCodeMap.TryGetValue(command, out var keyCode))
            {
                return keyCode;
            }
            return null;
        }

        // Method to get the string representation for saving to JSON
        public string GetKeyCodeString(string action)
        {
            if (ActionKeyStringMap.TryGetValue(action, out var keyCodeString))
            {
                return keyCodeString;
            }
            return null;
        }
    }

    public class FishingActionCommand
    {
        public string Action { get; set; }
        public string Command { get; set; }
    }

}