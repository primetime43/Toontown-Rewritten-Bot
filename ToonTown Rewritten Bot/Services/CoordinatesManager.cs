using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Services
{
    public class CoordinatesManager
    {
        private const string CoordinatesFileName = "UIElementCoordinates.json";
        // Static readonly field that computes the file path only once when the class is loaded
        private static readonly string CoordinatesFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), CoordinatesFileName);
        public CoordinatesManager()
        {
            if (!File.Exists(CoordinatesFilePath))
            {
                CreateFreshCoordinatesFile();
            }
        }

        /// <summary>
        /// Provides the fully qualified path to the coordinates file.
        /// This path is based on the assembly's current execution location,
        /// ensuring consistent access to the coordinates file throughout the application.
        /// </summary>
        /// <returns>A string containing the path to the coordinates file.</returns>
        public static string GetCoordinatesFilePath()
        {
            return CoordinatesFilePath;
        }

        /// <summary>
        /// Reads the coordinate data from the JSON file and returns a list of CoordinateActions.
        /// </summary>
        /// <returns>A list of CoordinateActions representing the coordinates. If the file does not exist, returns an empty list.</returns>
        public static List<CoordinateActions> ReadCoordinates()
        {
            return ReadCoordinatesFromJsonFile();
        }

        /// <summary>
        /// Checks if the coordinates associated with a given key are set and valid.
        /// </summary>
        /// <param name="coordinateKey">The enum value representing the coordinate key to check.</param>
        /// <returns>True if the coordinates are valid and set; otherwise, false if they are default (0,0) or not found.</returns>
        public static bool CheckCoordinates(Enum coordinateKey)
        {
            // Check if the coordinates file exists. If not, create a fresh one with default values.
            if (!File.Exists(CoordinatesFilePath))
            {
                CreateFreshCoordinatesFile();
            }

            // Read the JSON file containing the coordinates data.
            string json = File.ReadAllText(CoordinatesFilePath);
            // Deserialize the JSON data into a list of CoordinateActions objects.
            List<CoordinateActions> coordinateActions = JsonConvert.DeserializeObject<List<CoordinateActions>>(json);

            // Convert the enum key to its corresponding integer value, then convert that to a string.
            string keyAsString = Convert.ToInt32(coordinateKey).ToString();

            // Attempt to find a CoordinateAction that matches the provided key.
            var coordinate = coordinateActions.FirstOrDefault(ca => ca.Key == keyAsString);

            // Check if the coordinate exists and if its X and Y values are not the default (0,0), indicating they have been set.
            if (coordinate != null && (coordinate.X == 0 && coordinate.Y == 0))
            {
                return false; // The coordinates are default (0,0), indicating they have not been properly set.
            }

            // If the coordinate exists and is not default, or if no coordinate with the provided key exists, assume the coordinates are valid.
            return true; // The coordinates are valid and have been set.
        }

        /// <summary>
        /// Retrieves the coordinates from a JSON file based on the given enum key.
        /// </summary>
        /// <param name="key">An enum value representing the coordinate key to retrieve. The enum should be convertible to an integer that matches keys stored in the JSON.</param>
        /// <returns>A tuple containing the X and Y coordinates associated with the provided enum key.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the UIElementCoordinates cannot be found at the expected location.</exception>
        /// <exception cref="Exception">Thrown if no coordinates are found for the given key, indicating a possible issue with data consistency or key validity.</exception>
        /// <remarks>
        /// This method reads from a JSON file located relative to the executable's directory, deserializing it into a list of <see cref="CoordinateActions"/> objects.
        /// It then attempts to find a <see cref="CoordinateActions"/> object where the key matches the provided enum's numeric value converted to string.
        /// If found, it returns the X and Y coordinates; otherwise, it throws an exception indicating the key was not found.
        /// </remarks>
        public static (int x, int y) GetCoordsFromMap(Enum key)
        {
            // Convert the Enum to its integer value, then to string
            string keyAsString = Convert.ToInt32(key).ToString();

            if (File.Exists(CoordinatesFilePath))
            {
                string json = File.ReadAllText(CoordinatesFilePath);
                //Gets all of the coordinate models
                var coordinateActions = JsonConvert.DeserializeObject<List<CoordinateActions>>(json);

                var action = coordinateActions.FirstOrDefault(a => a.Key == keyAsString);
                if (action != null)
                {
                    return (action.X, action.Y);
                }
                else
                {
                    throw new Exception($"No coordinates found for the key: {keyAsString}");
                }
            }
            else
            {
                throw new FileNotFoundException("UIElementCoordinates not found.");
            }
        }

        /// <summary>
        /// Gets coordinates using image recognition as primary method, with manual coordinates as fallback.
        /// Will prompt user to capture template if none exists.
        /// Coordinates are automatically converted to screen coordinates by adding the game window offset.
        /// </summary>
        /// <param name="key">The enum key for the coordinate</param>
        /// <returns>The screen coordinates (x, y) of the element</returns>
        public static async Task<(int x, int y)> GetCoordsWithImageRecAsync(Enum key)
        {
            string keyAsString = Convert.ToInt32(key).ToString();
            string description = CoordinateActions.GetDescription(keyAsString);
            string elementName = description ?? $"Element_{keyAsString}";

            // First, load manual coordinates as fallback for UIElementManager
            var manualCoords = GetManualCoordsOrDefault(key);
            if (manualCoords.x != 0 || manualCoords.y != 0)
            {
                UIElementManager.Instance.SetManualCoordinates(elementName, new Point(manualCoords.x, manualCoords.y));
            }

            // Try to find using image recognition (will prompt for template capture if needed)
            var location = await UIElementManager.Instance.GetElementLocationAsync(elementName, description);

            if (location.HasValue)
            {
                // Convert window-relative coordinates to screen coordinates
                var windowOffset = CoreFunctionality.GetGameWindowOffset();
                int screenX = location.Value.X + windowOffset.X;
                int screenY = location.Value.Y + windowOffset.Y;

                System.Diagnostics.Debug.WriteLine($"[CoordinatesManager] {elementName}: window coords ({location.Value.X}, {location.Value.Y}) + offset ({windowOffset.X}, {windowOffset.Y}) = screen ({screenX}, {screenY})");

                return (screenX, screenY);
            }

            // If still not found, fall back to manual coordinates from the old system
            // Manual coordinates are already screen coordinates
            if (manualCoords.x != 0 || manualCoords.y != 0)
            {
                return manualCoords;
            }

            throw new Exception($"Could not find element '{elementName}' via image recognition or manual coordinates.");
        }

        /// <summary>
        /// Gets manual coordinates from the JSON file, returning (0,0) if not found or not set.
        /// </summary>
        private static (int x, int y) GetManualCoordsOrDefault(Enum key)
        {
            try
            {
                string keyAsString = Convert.ToInt32(key).ToString();

                if (File.Exists(CoordinatesFilePath))
                {
                    string json = File.ReadAllText(CoordinatesFilePath);
                    var coordinateActions = JsonConvert.DeserializeObject<List<CoordinateActions>>(json);
                    var action = coordinateActions.FirstOrDefault(a => a.Key == keyAsString);
                    if (action != null)
                    {
                        return (action.X, action.Y);
                    }
                }
            }
            catch { }
            return (0, 0);
        }

        /// <summary>
        /// Updates the coordinates for a specified location programmatically without user interaction.
        /// This function reads the existing coordinates from the JSON file, updates them with new values,
        /// and then writes the updated coordinates back to the file.
        /// </summary>
        /// <param name="locationToUpdateEnum">The enum value representing the location to update. 
        /// This should be a value from an established Enum type that corresponds to specific coordinate keys.</param>
        /// <param name="coordinates">The new coordinates to set for the location, encapsulated in a Point structure.</param>
        public static void UpdateCoordinatesAutomatically(Enum locationToUpdateEnum, Point coordinates)
        {
            // Convert the Enum to its integer value, then to string
            string keyAsString = Convert.ToInt32(locationToUpdateEnum).ToString();

            // Read the existing JSON file
            if (File.Exists(CoordinatesFilePath))
            {
                string json = File.ReadAllText(CoordinatesFilePath);
                var coordinateActions = JsonConvert.DeserializeObject<List<CoordinateActions>>(json);

                // Find and update the coordinates for the specified location
                var actionToUpdate = coordinateActions.Find(action => action.Key == keyAsString);
                if (actionToUpdate != null)
                {
                    actionToUpdate.X = coordinates.X;
                    actionToUpdate.Y = coordinates.Y;

                    // Serialize the updated list back to JSON and write it to the file
                    string updatedJson = JsonConvert.SerializeObject(coordinateActions, Formatting.Indented);
                    File.WriteAllText(CoordinatesFilePath, updatedJson);
                }
                else
                {
                    throw new InvalidOperationException("No matching coordinate action found for the given key.");
                }
            }
            else
            {
                throw new FileNotFoundException("UIElementCoordinates not found.");
            }
        }

        public static async Task ManualUpdateCoordinates(Enum locationToUpdateEnum)
        {
            CoreFunctionality.BringBotWindowToFront();

            UpdateCoordsHelper updateCoordsWindow = new UpdateCoordsHelper();
            // Convert the Enum to its integer value, then to string
            string keyAsString = Convert.ToInt32(locationToUpdateEnum).ToString();
            try
            {
                // Use CoordinateActions.GetDescription to retrieve the description by key
                string description = CoordinateActions.GetDescription(keyAsString);
                if (description == null)
                {
                    throw new Exception("Description not found for the given key.");
                }
                updateCoordsWindow.startCountDown(description);
                // Set the window to be topmost to ensure it appears above other applications.
                updateCoordsWindow.TopMost = true;
                updateCoordsWindow.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Unable to perform this action", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }

            // Get the updated cursor location
            Point coords = CoreFunctionality.getCursorLocation();
            string x = Convert.ToString(coords.X);
            string y = Convert.ToString(coords.Y);

            // Read the JSON file and deserialize it into a list of CoordinateAction objects
            List<CoordinateActions> coordinateActions;
            if (File.Exists(CoordinatesFilePath))
            {
                string json = File.ReadAllText(CoordinatesFilePath);
                coordinateActions = JsonConvert.DeserializeObject<List<CoordinateActions>>(json);
            }
            else
            {
                // Handle case where file does not exist
                coordinateActions = new List<CoordinateActions>();
            }

            // Find the coordinate by key and update its X and Y values
            var coordinateToUpdate = coordinateActions.FirstOrDefault(ca => ca.Key == keyAsString);
            if (coordinateToUpdate != null)
            {
                coordinateToUpdate.X = int.Parse(x);
                coordinateToUpdate.Y = int.Parse(y);
            }

            // Serialize the list back to JSON and write it to the file
            string updatedJson = JsonConvert.SerializeObject(coordinateActions, Formatting.Indented);
            File.WriteAllText(CoordinatesFilePath, updatedJson);

            CoreFunctionality.FocusTTRWindow();
        }

        public async Task ManualUpdateCoordinates(string locationToUpdate)
        {
            CoreFunctionality.BringBotWindowToFront();

            UpdateCoordsHelper updateCoordsWindow = new UpdateCoordsHelper();
            try
            {
                // Use CoordinateActions.GetDescription to retrieve the description by key
                string description = CoordinateActions.GetDescription(locationToUpdate);
                if (description == null)
                {
                    throw new Exception("Description not found for the given key.");
                }
                updateCoordsWindow.startCountDown(description);
                // Set the window to be topmost to ensure it appears above other applications.
                updateCoordsWindow.TopMost = true;
                updateCoordsWindow.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Unable to perform this action", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }

            // Get the updated cursor location
            Point coords = CoreFunctionality.getCursorLocation();
            string x = Convert.ToString(coords.X);
            string y = Convert.ToString(coords.Y);

            // Read the JSON file and deserialize it into a list of CoordinateAction objects
            List<CoordinateActions> coordinateActions;
            if (File.Exists(CoordinatesFilePath))
            {
                string json = File.ReadAllText(CoordinatesFilePath);
                coordinateActions = JsonConvert.DeserializeObject<List<CoordinateActions>>(json);
            }
            else
            {
                // Handle case where file does not exist
                coordinateActions = new List<CoordinateActions>();
            }

            // Find the coordinate by key and update its X and Y values
            var coordinateToUpdate = coordinateActions.FirstOrDefault(ca => ca.Key == locationToUpdate);
            if (coordinateToUpdate != null)
            {
                coordinateToUpdate.X = int.Parse(x);
                coordinateToUpdate.Y = int.Parse(y);
            }

            // Serialize the list back to JSON and write it to the file
            string updatedJson = JsonConvert.SerializeObject(coordinateActions, Formatting.Indented);
            File.WriteAllText(CoordinatesFilePath, updatedJson);
        }

        /// <summary>
        /// Reads the coordinate data from the JSON file and returns a list of CoordinateActions.
        /// </summary>
        /// <returns>A list of CoordinateActions representing the coordinates. If the file does not exist, returns an empty list.</returns>
        private static List<CoordinateActions> ReadCoordinatesFromJsonFile()
        {
            // Check if the file exists at the specified path.
            if (File.Exists(CoordinatesFilePath))
            {
                // Read the JSON content from the file.
                string json = File.ReadAllText(CoordinatesFilePath);

                // Deserialize the JSON content into a list of CoordinateActions objects.
                return JsonConvert.DeserializeObject<List<CoordinateActions>>(json);
            }

            // If the file does not exist, return an empty list to prevent null reference issues.
            return new List<CoordinateActions>();
        }

        public static void CreateFreshCoordinatesFile()
        {
            // Delete the file if it exists
            if (File.Exists(CoordinatesFilePath))
                File.Delete(CoordinatesFilePath);

            // Retrieve all descriptions to populate the JSON file
            var allDescriptions = CoordinateActions.GetAllDescriptions();

            // Create a list to hold coordinate data
            List<CoordinateActions> coordinateList = new List<CoordinateActions>();

            // Populate the list with default values
            foreach (var entry in allDescriptions)
            {
                coordinateList.Add(new CoordinateActions
                {
                    Key = entry.Key,
                    Description = entry.Value,
                    X = 0, // Default X coordinate
                    Y = 0  // Default Y coordinate
                });
            }

            // Serialize the list to JSON
            string json = JsonConvert.SerializeObject(coordinateList, Formatting.Indented);

            // Write the JSON to the file
            File.WriteAllText(CoordinatesFilePath, json);
        }
    }
}
