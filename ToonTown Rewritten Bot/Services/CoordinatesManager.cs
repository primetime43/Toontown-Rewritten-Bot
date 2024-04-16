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

namespace ToonTown_Rewritten_Bot.Services
{
    public class CoordinatesManager
    {
        private const string CoordinatesFileName = "Coordinates Data File.json";

        public CoordinatesManager()
        {

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
            // Define the path to the JSON file that stores the coordinates.
            string filePath = CoordinatesFileName;

            // Check if the coordinates file exists. If not, create a fresh one with default values.
            if (!File.Exists(filePath))
            {
                CreateFreshCoordinatesFile();
            }

            // Read the JSON file containing the coordinates data.
            string json = File.ReadAllText(Path.GetFullPath(filePath));
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
        /// <exception cref="FileNotFoundException">Thrown if the coordinates data file cannot be found at the expected location.</exception>
        /// <exception cref="Exception">Thrown if no coordinates are found for the given key, indicating a possible issue with data consistency or key validity.</exception>
        /// <remarks>
        /// This method reads from a JSON file located relative to the executable's directory, deserializing it into a list of <see cref="CoordinateActions"/> objects.
        /// It then attempts to find a <see cref="CoordinateActions"/> object where the key matches the provided enum's numeric value converted to string.
        /// If found, it returns the X and Y coordinates; otherwise, it throws an exception indicating the key was not found.
        /// </remarks>
        public static (int x, int y) GetCoordsFromMap(Enum key)
        {
            string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), CoordinatesFileName);

            // Convert the Enum to its integer value, then to string
            string keyAsString = Convert.ToInt32(key).ToString();

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
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
                throw new FileNotFoundException("Coordinates data file not found.");
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
            // Convert the Enum to its integer value, then to string
            string keyAsString = Convert.ToInt32(locationToUpdateEnum).ToString();
            string filePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), CoordinatesFileName);

            // Read the existing JSON file
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var coordinateActions = JsonConvert.DeserializeObject<List<CoordinateActions>>(json);

                // Find and update the coordinates for the specified location
                var actionToUpdate = coordinateActions.Find(action => action.Key == keyAsString);
                if (actionToUpdate != null)
                {
                    actionToUpdate.X = coordinates.X;
                    actionToUpdate.Y = coordinates.Y;

                    // Serialize the updated list back to JSON and write it to the file
                    string updatedJson = JsonConvert.SerializeObject(coordinateActions, Formatting.Indented);
                    File.WriteAllText(filePath, updatedJson);
                }
                else
                {
                    throw new InvalidOperationException("No matching coordinate action found for the given key.");
                }
            }
            else
            {
                throw new FileNotFoundException("Coordinates data file not found.");
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
            string filePath = CoordinatesFileName;
            List<CoordinateActions> coordinateActions;
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
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
            File.WriteAllText(filePath, updatedJson);

            CoreFunctionality.maximizeAndFocus();
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
            string filePath = CoordinatesFileName;
            List<CoordinateActions> coordinateActions;
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
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
            File.WriteAllText(filePath, updatedJson);

            CoreFunctionality.maximizeAndFocus();
        }

        /// <summary>
        /// Reads the coordinate data from the JSON file and returns a list of CoordinateActions.
        /// </summary>
        /// <returns>A list of CoordinateActions representing the coordinates. If the file does not exist, returns an empty list.</returns>
        private static List<CoordinateActions> ReadCoordinatesFromJsonFile()
        {
            // Define the path to the JSON file containing the coordinates.
            string filePath = CoordinatesFileName;

            // Check if the file exists at the specified path.
            if (File.Exists(filePath))
            {
                // Read the JSON content from the file.
                string json = File.ReadAllText(filePath);

                // Deserialize the JSON content into a list of CoordinateActions objects.
                return JsonConvert.DeserializeObject<List<CoordinateActions>>(json);
            }

            // If the file does not exist, return an empty list to prevent null reference issues.
            return new List<CoordinateActions>();
        }

        public static void CreateFreshCoordinatesFile()
        {
            // Path to the new JSON file
            string filePath = CoordinatesFileName;

            // Delete the file if it exists
            if (File.Exists(filePath))
                File.Delete(filePath);

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
            File.WriteAllText(filePath, json);
        }
    }
}
