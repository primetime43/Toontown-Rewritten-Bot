using System.Collections.Generic;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Models
{
    /// <summary>
    /// Maps gardening walk/movement actions to their corresponding virtual key codes.
    /// </summary>
    public class GardeningActionKeys
    {
        /// <summary>
        /// Dictionary mapping action names to their virtual key codes.
        /// </summary>
        public Dictionary<string, VirtualKeyCode> ActionKeyMap { get; } = new Dictionary<string, VirtualKeyCode>
        {
            { "WALK FORWARD", VirtualKeyCode.UP },
            { "WALK BACKWARD", VirtualKeyCode.DOWN },
            { "WALK LEFT", VirtualKeyCode.LEFT },
            { "WALK RIGHT", VirtualKeyCode.RIGHT },
            { "TURN LEFT", VirtualKeyCode.VK_A },
            { "TURN RIGHT", VirtualKeyCode.VK_D }
        };

        /// <summary>
        /// Gets the display name for an action.
        /// </summary>
        public static string GetDisplayName(string action)
        {
            return action switch
            {
                "WALK FORWARD" => "Walk Forward",
                "WALK BACKWARD" => "Walk Backward",
                "WALK LEFT" => "Strafe Left",
                "WALK RIGHT" => "Strafe Right",
                "TURN LEFT" => "Turn Left",
                "TURN RIGHT" => "Turn Right",
                "PLANT FLOWER" => "Plant Flower",
                "WATER PLANT" => "Water Plant",
                "REMOVE PLANT" => "Remove Plant",
                "DELAY" => "Wait/Delay",
                _ => action
            };
        }
    }
}
