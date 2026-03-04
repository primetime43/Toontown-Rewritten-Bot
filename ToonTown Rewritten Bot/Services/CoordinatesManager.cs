using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
        private static readonly string CoordinatesFilePath = Path.Combine(AppPaths.ExeDirectory, CoordinatesFileName);
        private static readonly object _fileLock = new object();
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
            // Ensure file exists
            if (!File.Exists(CoordinatesFilePath))
            {
                CreateFreshCoordinatesFile();
            }

            string keyAsString = Convert.ToInt32(coordinateKey).ToString();
            var coordinate = FindCoordinateByKey(keyAsString);

            // Coordinates at (0,0) are considered unset
            return coordinate != null && !(coordinate.X == 0 && coordinate.Y == 0);
        }

        /// <summary>
        /// Retrieves the coordinates from a JSON file based on the given enum key.
        /// </summary>
        /// <param name="key">An enum value representing the coordinate key to retrieve.</param>
        /// <returns>A tuple containing the X and Y coordinates associated with the provided enum key.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the UIElementCoordinates cannot be found.</exception>
        /// <exception cref="Exception">Thrown if no coordinates are found for the given key.</exception>
        public static (int x, int y) GetCoordsFromMap(Enum key)
        {
            if (!File.Exists(CoordinatesFilePath))
            {
                throw new FileNotFoundException("UIElementCoordinates not found.");
            }

            string keyAsString = Convert.ToInt32(key).ToString();
            var action = FindCoordinateByKey(keyAsString);

            if (action != null)
            {
                return (action.X, action.Y);
            }

            throw new Exception($"No coordinates found for the key: {keyAsString}");
        }

        /// <summary>
        /// Finds a coordinate entry by its key.
        /// </summary>
        private static CoordinateActions FindCoordinateByKey(string key)
        {
            var coordinateActions = ReadCoordinatesFromJsonFile();
            return coordinateActions.FirstOrDefault(ca => ca.Key == key);
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

            Logger.Info("Coordinates", $"Resolving coords for '{elementName}' (key={keyAsString})");

            // First, load manual coordinates as fallback for UIElementManager
            var manualCoords = GetManualCoordsOrDefault(key);
            if (manualCoords.x != 0 || manualCoords.y != 0)
            {
                Logger.Debug("Coordinates", $"Manual coords available for '{elementName}': ({manualCoords.x}, {manualCoords.y})");
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

                Logger.Info("Coordinates", $"'{elementName}': screen ({screenX}, {screenY}) [source=ImageRec]");

                return (screenX, screenY);
            }

            // If still not found, fall back to manual coordinates from the old system
            // Manual coordinates are already screen coordinates
            if (manualCoords.x != 0 || manualCoords.y != 0)
            {
                Logger.Info("Coordinates", $"'{elementName}': screen ({manualCoords.x}, {manualCoords.y}) [source=ManualFallback]");
                return manualCoords;
            }

            // Offer the user a chance to recapture the template before giving up
            // Bring bot window to front so the dialog is visible over TTR
            DialogResult recaptureChoice = DialogResult.No;
            if (Application.OpenForms.Count > 0 && Application.OpenForms[0].InvokeRequired)
            {
                Application.OpenForms[0].Invoke(new Action(() =>
                {
                    CoreFunctionality.BringBotWindowToFront();
                    recaptureChoice = MessageBox.Show(
                        $"Could not find '{elementName}' on screen. Would you like to capture the template for this element?",
                        "Element Not Found",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                }));
            }
            else
            {
                CoreFunctionality.BringBotWindowToFront();
                recaptureChoice = MessageBox.Show(
                    $"Could not find '{elementName}' on screen. Would you like to capture the template for this element?",
                    "Element Not Found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
            }

            if (recaptureChoice == DialogResult.Yes)
            {
                Logger.Info("Coordinates", $"User chose to recapture template for '{elementName}'");
                bool captured = UIElementManager.Instance.PromptForTemplateCapture(elementName, description ?? $"Please select the '{elementName}' on screen");
                if (captured)
                {
                    // Retry with the newly captured template
                    var retryLocation = await UIElementManager.Instance.GetElementLocationAsync(elementName, description, forceSearch: true);
                    if (retryLocation.HasValue)
                    {
                        var windowOffset = CoreFunctionality.GetGameWindowOffset();
                        int screenX = retryLocation.Value.X + windowOffset.X;
                        int screenY = retryLocation.Value.Y + windowOffset.Y;
                        Logger.Info("Coordinates", $"'{elementName}': screen ({screenX}, {screenY}) [source=Recapture]");
                        return (screenX, screenY);
                    }
                }
            }

            Logger.Error("Coordinates", $"Could not find '{elementName}' via image recognition or manual coordinates");
            throw new Exception($"Could not find element '{elementName}' via image recognition or manual coordinates.");
        }

        /// <summary>
        /// Gets manual coordinates from the JSON file, returning (0,0) if not found or not set.
        /// </summary>
        private static (int x, int y) GetManualCoordsOrDefault(Enum key)
        {
            lock (_fileLock)
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
                catch (Exception ex)
                {
                    Logger.Warning("Coordinates", $"Error reading manual coords: {ex.Message}");
                }
                return (0, 0);
            }
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
            lock (_fileLock)
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
        }

        /// <summary>
        /// Manually updates coordinates for a location using cursor position after countdown.
        /// </summary>
        /// <param name="locationToUpdateEnum">The enum value representing the location to update.</param>
        public static async Task ManualUpdateCoordinates(Enum locationToUpdateEnum)
        {
            string keyAsString = Convert.ToInt32(locationToUpdateEnum).ToString();
            await ManualUpdateCoordinatesCore(keyAsString, focusTTRAfter: true);
        }

        /// <summary>
        /// Manually updates coordinates for a location using cursor position after countdown.
        /// </summary>
        /// <param name="locationToUpdate">The string key of the location to update.</param>
        public async Task ManualUpdateCoordinates(string locationToUpdate)
        {
            await ManualUpdateCoordinatesCore(locationToUpdate, focusTTRAfter: false);
        }

        /// <summary>
        /// Core implementation for manual coordinate updates.
        /// </summary>
        private static async Task ManualUpdateCoordinatesCore(string key, bool focusTTRAfter)
        {
            CoreFunctionality.BringBotWindowToFront();

            try
            {
                string description = CoordinateActions.GetDescription(key);
                if (description == null)
                {
                    throw new Exception("Description not found for the given key.");
                }

                UpdateCoordsHelper updateCoordsWindow = new UpdateCoordsHelper();
                updateCoordsWindow.startCountDown(description);
                updateCoordsWindow.TopMost = true;
                updateCoordsWindow.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Unable to perform this action", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            // Get the updated cursor location and save
            Point coords = CoreFunctionality.getCursorLocation();
            UpdateCoordinateByKey(key, coords.X, coords.Y);

            if (focusTTRAfter)
            {
                CoreFunctionality.FocusTTRWindow();
            }
        }

        /// <summary>
        /// Updates a coordinate entry by key and saves to file.
        /// </summary>
        private static void UpdateCoordinateByKey(string key, int x, int y)
        {
            var coordinateActions = ReadCoordinatesFromJsonFile();

            var coordinateToUpdate = coordinateActions.FirstOrDefault(ca => ca.Key == key);
            if (coordinateToUpdate != null)
            {
                coordinateToUpdate.X = x;
                coordinateToUpdate.Y = y;
            }

            SaveCoordinatesToFile(coordinateActions);
        }

        /// <summary>
        /// Saves coordinate actions list to the JSON file.
        /// </summary>
        private static void SaveCoordinatesToFile(List<CoordinateActions> coordinateActions)
        {
            lock (_fileLock)
            {
                string json = JsonConvert.SerializeObject(coordinateActions, Formatting.Indented);
                File.WriteAllText(CoordinatesFilePath, json);
            }
        }

        /// <summary>
        /// Reads the coordinate data from the JSON file and returns a list of CoordinateActions.
        /// </summary>
        /// <returns>A list of CoordinateActions representing the coordinates. If the file does not exist, returns an empty list.</returns>
        private static List<CoordinateActions> ReadCoordinatesFromJsonFile()
        {
            lock (_fileLock)
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
        }

        public static void CreateFreshCoordinatesFile()
        {
            lock (_fileLock)
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
}
