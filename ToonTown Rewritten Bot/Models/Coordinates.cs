using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ToonTown_Rewritten_Bot.Models.Coordinates;

namespace ToonTown_Rewritten_Bot.Models
{
    public class CoordinateActions : ICoordinateData
    {
        public string Key { get; set; }
        public string Description { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        private static readonly Dictionary<string, string> _actionDescriptionMap = new Dictionary<string, string>();

        // Static constructor to fill the dictionary
        static CoordinateActions()
        {
            CreateActionDescriptionMap();
        }

        private static void CreateActionDescriptionMap()
        {
            // Gardening Actions
            _actionDescriptionMap.Add("1", "Plant Flower/Remove Button");
            _actionDescriptionMap.Add("2", "Red Jellybean Button");
            _actionDescriptionMap.Add("3", "Green Jellybean Button");
            _actionDescriptionMap.Add("4", "Orange Jellybean Button");
            _actionDescriptionMap.Add("5", "Purple Jellybean Button");
            _actionDescriptionMap.Add("6", "Blue Jellybean Button");
            _actionDescriptionMap.Add("7", "Pink Jellybean Button");
            _actionDescriptionMap.Add("8", "Yellow Jellybean Button");
            _actionDescriptionMap.Add("9", "Cyan Jellybean Button");
            _actionDescriptionMap.Add("10", "Silver Jellybean Button");
            _actionDescriptionMap.Add("11", "Blue Plant Button");
            _actionDescriptionMap.Add("12", "Blue Ok Button");
            _actionDescriptionMap.Add("13", "Watering Can Button");
            _actionDescriptionMap.Add("14", "Blue Yes Button");

            // Fishing Actions
            _actionDescriptionMap.Add("15", "Red Fishing Button");
            _actionDescriptionMap.Add("16", "Exit Fishing Button");
            _actionDescriptionMap.Add("17", "Blue Sell All Button");

            // Doodle Training Actions
            _actionDescriptionMap.Add("18", "Feed Doodle Button");
            _actionDescriptionMap.Add("19", "Scratch Doodle Button");
            _actionDescriptionMap.Add("20", "Green SpeedChat Button");
            _actionDescriptionMap.Add("21", "Pets Tab in SpeedChat");
            _actionDescriptionMap.Add("22", "Tricks Tab in SpeedChat");
            _actionDescriptionMap.Add("23", "Jump Trick Option in SpeedChat");
            _actionDescriptionMap.Add("24", "Beg Trick Option in SpeedChat");
            _actionDescriptionMap.Add("25", "Play Dead Trick Option in SpeedChat");
            _actionDescriptionMap.Add("26", "Rollover Trick Option in SpeedChat");
            _actionDescriptionMap.Add("27", "Backflip Trick Option in SpeedChat");
            _actionDescriptionMap.Add("28", "Dance Trick Option in SpeedChat");
            _actionDescriptionMap.Add("29", "Speak Trick Option in SpeedChat");
        }

        // Public method to get the description by key
        public static string GetDescription(string key)
        {
            if (_actionDescriptionMap.TryGetValue(key, out var description))
            {
                return description;
            }

            return null; // Or throw an exception, depending on your needs
        }

        // Method to get the full dictionary if needed elsewhere
        public static Dictionary<string, string> GetAllDescriptions() => _actionDescriptionMap;

        public static string GetKeyFromDescription(string description)
        {
            // Iterate over the key-value pairs in the map
            foreach (var pair in _actionDescriptionMap)
            {
                // Check if the value matches the provided description
                if (pair.Value == description)
                {
                    return pair.Key;  // Return the key that matches the description
                }
            }

            return null; // Return null if no match is found
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