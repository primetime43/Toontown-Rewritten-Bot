using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using static ToonTown_Rewritten_Bot.Models.Coordinates;
using static ToonTown_Rewritten_Bot.Utilities.ImageRecognition;

namespace ToonTown_Rewritten_Bot.Services
{
    public class CoreFunctionality
    {
        //eventually clean up repetative code

        public static bool isAutoDetectFishingBtnActive = true;
        //public static Dictionary<string, string> _dataFileMap = BotFunctions.GetDataFileMap();

        /*public static (int x, int y) GetCoordsFromMap(string item)
        {
            string filePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Coordinates Data File.json");

            // Ensure the file exists
            if (File.Exists(filePath))
            {
                // Read the JSON file
                string json = File.ReadAllText(filePath);
                var coordinateActions = JsonConvert.DeserializeObject<List<CoordinateActions>>(json);

                // Find the coordinate action based on the key
                var action = coordinateActions.Find(a => a.Key == item);
                if (action != null)
                {
                    return (action.X, action.Y);
                }
                else
                {
                    throw new Exception($"No coordinates found for the item with key: {item}");
                }
            }
            else
            {
                throw new FileNotFoundException("Coordinates data file not found.");
            }
        }*/

        public static (int x, int y) GetCoordsFromMap(Enum key)
        {
            string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Coordinates Data File.json");

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

        public static void DoMouseClick()
        {
            DoMouseClick(getCursorLocation());
        }

        public static void DoFishingClick()
        {
            //click red button
            DoMouseClickDown(getCursorLocation());
            Thread.Sleep(500);//sleep 2 sec

            // Retrieve coordinates for the red fishing button from the JSON-based coordinates map
            (int x, int y) = GetCoordsFromMap(FishingCoordinatesEnum.RedFishingButton);
            MoveCursor(x, y + 150);//pull it back
            Thread.Sleep(500);
            DoMouseClickUp(getCursorLocation());
        }
        private static void DoMouseClick(Point location)//simulate left button mouse click
        {
            //Call the imported function with the cursor's current position
            uint X = Convert.ToUInt32(location.X);
            uint Y = Convert.ToUInt32(location.Y);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        private static void DoMouseClickDown(Point location)
        {
            uint X = Convert.ToUInt32(location.X);
            uint Y = Convert.ToUInt32(location.Y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, X, Y, 0, 0);
        }

        private static void DoMouseClickUp(Point location)
        {
            uint X = Convert.ToUInt32(location.X);
            uint Y = Convert.ToUInt32(location.Y);
            mouse_event(MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        public static Color GetColorAt(int x, int y)
        {
            nint desk = GetDesktopWindow();
            nint dc = GetWindowDC(desk);
            int a = (int)GetPixel(dc, x, y);
            ReleaseDC(desk, dc);
            return Color.FromArgb(255, a >> 0 & 0xff, a >> 8 & 0xff, a >> 16 & 0xff);
        }

        public static Point getCursorLocation()
        {
            Point cursorLocation = new Point();
            GetCursorPos(ref cursorLocation);
            return cursorLocation;
        }

        public static string HexConverter(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        public static void MoveCursor(int x, int y)
        {
            Cursor.Position = new Point(x, y);
        }

        // Maximizes and Focuces TTR
        public static void maximizeAndFocus()
        {
            nint hwnd = FindWindowByCaption(nint.Zero, "Toontown Rewritten");
            ShowWindow(hwnd, 6);//6 min
            ShowWindow(hwnd, 3);//3 max
        }

        public static void UpdateCoordinatesFile(List<CoordinateActions> coordinateActions)
        {
            try
            {
                // Serialize the list of coordinate actions to JSON
                string json = JsonConvert.SerializeObject(coordinateActions, Formatting.Indented);

                // Write the JSON to the coordinates file
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Coordinates Data File.json"), json);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not write to the file:");
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Brings the Toontown Rewritten Bot window to the foreground.
        /// </summary>
        /// <remarks>
        /// This function searches for the bot window by its title and, if found, brings it to the front of all other windows. 
        /// This is useful for ensuring the bot's window is visible, especially when displaying messages or prompts that require user attention.
        /// </remarks>
        public static void BringBotWindowToFront()
        {
            // Get the current process
            Process currentProcess = Process.GetCurrentProcess();

            // Use the main window title of the current process
            string windowTitle = currentProcess.MainWindowTitle;

            // Attempt to find the window by its title
            IntPtr hWnd = NativeMethods.FindWindow(null, windowTitle);
            // If a handle was found, attempt to bring the window to the front
            if (hWnd != IntPtr.Zero)
            {
                NativeMethods.SetForegroundWindow(hWnd);
            }
        }

        public async Task ManualUpdateCoordinates(Enum locationToUpdateEnum)
        {
            BringBotWindowToFront();

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
            Point coords = getCursorLocation();
            string x = Convert.ToString(coords.X);
            string y = Convert.ToString(coords.Y);

            // Read the JSON file and deserialize it into a list of CoordinateAction objects
            string filePath = "Coordinates Data File.json";
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

            maximizeAndFocus();
        }

        public async Task ManualUpdateCoordinates(string locationToUpdate)
        {
            BringBotWindowToFront();

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
            Point coords = getCursorLocation();
            string x = Convert.ToString(coords.X);
            string y = Convert.ToString(coords.Y);

            // Read the JSON file and deserialize it into a list of CoordinateAction objects
            string filePath = "Coordinates Data File.json";
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

            maximizeAndFocus();
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
            string filePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Coordinates Data File.json");

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

        /// <summary>
        /// Reads the coordinate data from the JSON file and returns a list of CoordinateActions.
        /// </summary>
        /// <returns>A list of CoordinateActions representing the coordinates. If the file does not exist, returns an empty list.</returns>
        public static List<CoordinateActions> ReadCoordinatesFromJsonFile()
        {
            // Define the path to the JSON file containing the coordinates.
            string filePath = "Coordinates Data File.json";

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

        public static void UpdateCoordinatesInJsonFile(string key, int x, int y)
        {
            var coordinates = ReadCoordinatesFromJsonFile();
            var coordinate = coordinates.FirstOrDefault(c => c.Key == key);
            if (coordinate != null)
            {
                coordinate.X = x;
                coordinate.Y = y;
                string json = JsonConvert.SerializeObject(coordinates, Formatting.Indented);
                File.WriteAllText("Coordinates Data File.json", json);
            }
        }


        /*private static int[] readCoordinatesFromFile(string coordsToRetrieve)
        {
            lines = File.ReadAllLines(Path.GetFullPath("Coordinates Data File.txt"));
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("."))
                {
                    if (coordsToRetrieve.Equals(lines[i].Substring(0, lines[i].IndexOf('.'))))//look for the number it corresponds to
                    {
                        string check = lines[i];
                        char[] removeChars = { '(', ')' };
                        string coords = check.Substring(check.IndexOf('(') + 1);
                        coords = coords.Trim(removeChars);
                        string[] points = coords.Split(',');
                        int x = Convert.ToInt32(points[0]);
                        int y = Convert.ToInt32(points[1]);
                        int[] locations = { x, y };
                        return locations;
                    }
                }
            }
            return null;
        }*/

        /// <summary>
        /// Checks if the coordinates associated with a given key are set and valid.
        /// </summary>
        /// <param name="coordinateKey">The enum value representing the coordinate key to check.</param>
        /// <returns>True if the coordinates are valid and set; otherwise, false if they are default (0,0) or not found.</returns>
        public static bool CheckCoordinates(Enum coordinateKey)
        {
            // Define the path to the JSON file that stores the coordinates.
            string filePath = "Coordinates Data File.json";

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
        /// Ensures that a "Custom Fishing Actions" folder exists in the application's directory.
        /// Creates the folder if it does not exist. Always returns the path to this folder.
        /// </summary>
        /// <returns>The path to the "Custom Fishing Actions" folder.</returns>
        public static string CreateCustomFishingActionsFolder()
        {
            // Get the directory where the executable is running
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Combine the executable path with the "Custom Fishing Actions" folder name
            string customActionsFolderPath = Path.Combine(exePath, "Custom Fishing Actions");

            // Ensure the directory exists. This method creates the directory if it does not exist
            // and does nothing if it already exists.
            Directory.CreateDirectory(customActionsFolderPath);

            // Return the full path to the folder
            return customActionsFolderPath;
        }

        public static void CreateFreshCoordinatesFile()
        {
            // Path to the new JSON file
            string filePath = "Coordinates Data File.json";

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

        public static string[] loadCustomFishingActions()
        {
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string customActionsFolderPath = Path.Combine(exePath, "Custom Fishing Actions");
            // Ensure the directory exists
            Directory.CreateDirectory(customActionsFolderPath); // This line ensures the directory is created if it doesn't exist

            // Read files in the folder
            return Directory.GetFiles(customActionsFolderPath);
        }

        //ignore .dll imports below
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(ref Point lpPoint);

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern nint FindWindowByCaption(nint ZeroOnly, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(nint hWnd, int nCmdShow);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        private static extern int BitBlt(nint hDC, int x, int y, int nWidth, int nHeight, nint hSrcDC, int xSrc, int ySrc, int dwRop);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint GetDesktopWindow();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint GetWindowDC(nint window);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern uint GetPixel(nint dc, int x, int y);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int ReleaseDC(nint window, nint dc);
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
    }
}
