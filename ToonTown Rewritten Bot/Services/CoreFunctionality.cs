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
using static ToonTown_Rewritten_Bot.Utilities.ImageRecognition;

namespace ToonTown_Rewritten_Bot.Services
{
    public class CoreFunctionality
    {
        //eventually clean up repetative code

        public static bool isAutoDetectFishingBtnActive = true;
        //public static Dictionary<string, string> _dataFileMap = BotFunctions.GetDataFileMap();

        public (int x, int y) GetCoordsFromMap(string item)
        {
            int[] coordinates = CoreFunctionality.readCoordinatesFromFile(item);
            return (coordinates[0], coordinates[1]);
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

            //get the coords of the red button and move cursor from there, downward
            int[] coordinates = readCoordinatesFromFile("15");
            int x = coordinates[0];
            int y = coordinates[1];
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

        private static string[] lines;
        public static void readCoordinatesFile()
        {
            if (File.Exists("Coordinates Data File.txt"))
            {
                try
                {
                    lines = File.ReadAllLines(Path.GetFullPath("Coordinates Data File.txt"));
                }
                catch (Exception e)
                {
                    MessageBox.Show("The file could not be read:");
                    Console.WriteLine(e.Message);
                }
            }
            else
                createFreshCoordinatesFile();
        }

        private static void updateTextFile()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(Path.GetFullPath("Coordinates Data File.txt")))
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        writer.WriteLine(lines[i]);
                    }
                    writer.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be written to:");
                Console.WriteLine(e.Message);
            }
        }

        public static void updateTextFile(string[] lines)//manually updating coords
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(Path.GetFullPath("Coordinates Data File.txt")))
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        writer.WriteLine(lines[i]);
                    }
                    writer.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be written to:");
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

            Point coords = getCursorLocation();
            string x = Convert.ToString(coords.X);
            string y = Convert.ToString(coords.Y);
            lines = File.ReadAllLines(Path.GetFullPath("Coordinates Data File.txt"));
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("."))
                {
                    if (locationToUpdate.Equals(lines[i].Substring(0, lines[i].IndexOf('.'))))//look for the number it cooresponds to
                    {
                        lines[i] = locationToUpdate + "." + "(" + x + "," + y + ")";
                        updateTextFile();//changes the coordinate values in the data file
                    }
                }
            }
            maximizeAndFocus();
        }

        public static void ManuallyUpdateCoordinatesNoUI(string locationToUpdate, Point coodinates)
        {
            string[] lines = File.ReadAllLines(Path.GetFullPath("Coordinates Data File.txt"));
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("."))
                {
                    if (locationToUpdate.Equals(lines[i].Substring(0, lines[i].IndexOf('.'))))//look for the number it cooresponds to
                    {
                        lines[i] = locationToUpdate + "." + "(" + coodinates.X + "," + coodinates.Y + ")";
                        updateTextFile(lines);//changes the coordinate values in the data file
                    }
                }
            }
        }

        private static int[] readCoordinatesFromFile(string coordsToRetrieve)
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
        }

        public static bool CheckCoordinates(string checkCoords)
        {
            string filePath = "Coordinates Data File.txt";
            if (!File.Exists(filePath))
            {
                // Create the file and write the default coordinates
                createFreshCoordinatesFile();
            }

            lines = File.ReadAllLines(Path.GetFullPath("Coordinates Data File.txt"));
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("."))
                {
                    if (checkCoords.Equals(lines[i].Substring(0, lines[i].IndexOf('.'))))//look for the number it corresponds to
                    {
                        string check = lines[i];
                        char[] removeChars = { '(', ')' };
                        string coords = check.Substring(check.IndexOf('(') + 1);
                        coords = coords.Trim(removeChars);
                        if ("0,0".Equals(coords))
                        {
                            return false;//returns false if they equals 0,0
                        }
                    }
                }
            }
            return true;//return true if they're not 0,0
        }

        /// <summary>
        /// Create "Custom Fishing Actions" folder if it doesn't exist
        /// </summary>
        public static string CreateCustomFishingActionsFolder()
        {
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string customActionsFolderPath = Path.Combine(exePath, "Custom Fishing Actions");
            Directory.CreateDirectory(customActionsFolderPath);
            return customActionsFolderPath;
        }

        public static void createFreshCoordinatesFile()
        {
            string filePath = "Coordinates Data File.txt";
            // Delete the file if it exists
            if (File.Exists(filePath))
                File.Delete(filePath);

            // Create the file and write the default coordinates
            using (StreamWriter sw = File.CreateText(filePath))
            {
                var allDescriptions = CoordinateActions.GetAllDescriptions();

                foreach (var key in allDescriptions.Keys)
                {
                    sw.WriteLine($"{key}.(0,0)"); // Write each key with default coordinates
                }
            }
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
