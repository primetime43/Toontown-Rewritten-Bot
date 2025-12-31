using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Utilities;
using static ToonTown_Rewritten_Bot.Models.Coordinates;

namespace ToonTown_Rewritten_Bot.Models
{
    /// <summary>
    /// Represents actions associated with coordinates in the UI, such as button locations.
    /// Now uses file-based definitions via TemplateDefinitionManager.
    /// </summary>
    public class CoordinateActions : ICoordinateData
    {
        public string Key { get; set; }
        public string Description { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        /// <summary>
        /// Retrieves the description for a given key.
        /// </summary>
        /// <param name="key">The key whose description is to be retrieved.</param>
        /// <returns>The description if found; otherwise, null.</returns>
        public static string GetDescription(string key)
        {
            return TemplateDefinitionManager.Instance.GetDescriptionByKey(key);
        }

        /// <summary>
        /// Provides access to all action descriptions mapped by their keys.
        /// </summary>
        /// <returns>A dictionary of all descriptions mapped by their keys.</returns>
        public static Dictionary<string, string> GetAllDescriptions()
        {
            return TemplateDefinitionManager.Instance.GetAllDescriptions();
        }

        /// <summary>
        /// Finds the key for a given description.
        /// </summary>
        /// <param name="description">The description to find the key for.</param>
        /// <returns>The key if found; otherwise, null.</returns>
        public static string GetKeyFromDescription(string description)
        {
            return TemplateDefinitionManager.Instance.GetKeyByDescription(description);
        }
    }

    public class Coordinates
    {
        public enum GardeningCoordinatesEnum
        {
            PlantFlowerRemoveButton = 1,
            RedJellybeanButton,
            GreenJellybeanButton,
            OrangeJellybeanButton,
            PurpleJellybeanButton,
            BlueJellybeanButton,
            PinkJellybeanButton,
            YellowJellybeanButton,
            CyanJellybeanButton,
            SilverJellybeanButton,
            BluePlantButton,
            BlueOkButton,
            WateringCanButton,
            BlueYesButton
        }

        public enum FishingCoordinatesEnum
        {
            RedFishingButton = 15,
            ExitFishingButton,
            BlueSellAllButton
        }

        public enum DoodleTrainingCoordinatesEnum
        {
            FeedDoodleButton = 18,
            ScratchDoodleButton,
            GreenSpeedChatButton,
            PetsTabInSpeedChat,
            TricksTabInSpeedChat,
            JumpTrickOptionInSpeedChat,
            BegTrickOptionInSpeedChat,
            PlayDeadTrickOptionInSpeedChat,
            RolloverTrickOptionInSpeedChat,
            BackflipTrickOptionInSpeedChat,
            DanceTrickOptionInSpeedChat,
            SpeakTrickOptionInSpeedChat
        }
    }

    public static class FishingLocationMessages
    {
        private static readonly Dictionary<string, string> _locationMessageMap = new Dictionary<string, string>
        {
            ["TOONTOWN CENTRAL PUNCHLINE PLACE"] = "Fishes in the first dock when you walk in",
            ["DONALD DREAM LAND LULLABY LANE"] = "Fishes in the dock to the left of the small box",
            ["BRRRGH POLAR PLACE"] = "Fishes in the top right dock",
            ["BRRRGH WALRUS WAY"] = "Fishes in the top left dock",
            ["BRRRGH SLEET STREET"] = "Fishes in the first dock when you walk in",
            ["MINNIE'S MELODYLAND TENOR TERRACE"] = "Fishes in the top left dock",
            ["DONALD DOCK LIGHTHOUSE LANE"] = "Fishes in the middle right dock (middle right)",
            ["DAISY'S GARDEN ELM STREET"] = "Fishes in the bottom left dock when you walk in",
            ["FISH ANYWHERE"] = "Fishes for you anywhere, but will only fish, will not sell fish!",
            ["CUSTOM FISHING ACTION"] = "Select your custom fishing actions in the dropdown"
        };

        public static void TellFishingLocation(string location)
        {
            if (_locationMessageMap.TryGetValue(location, out var message))
            {
                MessageBox.Show(message);
            }
            else
            {
                MessageBox.Show("Location not found.");
            }
        }

        public static string GetLocationMessage(string location)
        {
            if (_locationMessageMap.TryGetValue(location, out var message))
            {
                return message;
            }
            else
            {
                return "Location not found."; // You could also return null or throw an exception depending on your requirements
            }
        }
    }
}