using System.Collections.Generic;

namespace ToonTown_Rewritten_Bot.Models
{
    public class ActionKeys
    {
        // Property to hold the mapping
        public Dictionary<string, string> ActionKeyMap { get; private set; }

        // Constructor to initialize the mapping
        public ActionKeys()
        {
            ActionKeyMap = new Dictionary<string, string>
            {
                {"WALK FORWARDS", "UP"},
                {"WALK BACKWARDS", "DOWN"},
                {"TURN LEFT", "LEFT"},
                {"TURN RIGHT", "RIGHT"},
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
}