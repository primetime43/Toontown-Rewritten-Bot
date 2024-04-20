using System.Collections.Generic;

namespace ToonTown_Rewritten_Bot.Models
{
    public class FishingActionKeys
    {
        // Property to hold the mapping
        public Dictionary<string, string> ActionKeyMap { get; private set; }

        // Constructor to initialize the mapping
        public FishingActionKeys()
        {
            ActionKeyMap = new Dictionary<string, string>
            {
                {"WALK FORWARDS", "UP"},
                {"WALK BACKWARDS", "DOWN"},
                {"TURN LEFT", "LEFT"},
                {"TURN RIGHT", "RIGHT"},
                {"SELL FISH", "SELL"},
            };
        }

        // Method to get the VirtualKeyCode string representation by action name
        public string GetKeyCodeString(string action)
        {
            if (ActionKeyMap.TryGetValue(action, out var keyCodeString))
            {
                return keyCodeString;
            }
            else
            {
                return null; // or throw an exception, depending on your error handling preference
            }
        }
    }

    public class FishingActionCommand
    {
        public string Action { get; set; }
        public string Command { get; set; }
    }

}