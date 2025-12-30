using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using static ToonTown_Rewritten_Bot.Models.Coordinates;
using static ToonTown_Rewritten_Bot.Utilities.ImageRecognition;

namespace ToonTown_Rewritten_Bot.Services
{
    public class CoreFunctionality
    {
        public static void DoMouseClick()
        {
            DoMouseClick(getCursorLocation());
        }

        public static void DoFishingClick()
        {
            // Get current cursor position (set by CastLine to the red fishing button location)
            Point startPos = getCursorLocation();

            // Click down at the current cursor position (on the red fishing button)
            DoMouseClickDown(startPos);
            Thread.Sleep(500);

            // Drag DOWN from where we clicked to cast the line forward
            // The drag direction is opposite of cast direction - dragging down casts forward into the water
            MoveCursor(startPos.X, startPos.Y + 150);
            Thread.Sleep(500);
            DoMouseClickUp(getCursorLocation());
        }

        /// <summary>
        /// Performs a fishing click with a custom drag destination for auto-detect fishing.
        /// Uses the same proven mechanism as DoFishingClick.
        /// </summary>
        /// <param name="destinationX">Screen X coordinate to drag to</param>
        /// <param name="destinationY">Screen Y coordinate to drag to</param>
        public static void DoFishingClickWithDestination(int destinationX, int destinationY)
        {
            // Get current cursor position (should be on the red fishing button)
            Point startPos = getCursorLocation();

            System.Diagnostics.Debug.WriteLine($"[DoFishingClickWithDestination] Start: ({startPos.X}, {startPos.Y}) -> Dest: ({destinationX}, {destinationY})");

            // Click down at the current cursor position (on the red fishing button)
            DoMouseClickDown(startPos);
            Thread.Sleep(500);

            // Drag to the calculated destination
            MoveCursor(destinationX, destinationY);
            Thread.Sleep(500);

            // Release to cast
            DoMouseClickUp(getCursorLocation());
        }
        private static void DoMouseClick(Point location)//simulate left button mouse click
        {
            //Call the imported function with the cursor's current position
            uint X = Convert.ToUInt32(location.X);
            uint Y = Convert.ToUInt32(location.Y);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        public static void DoMouseClickDown(Point location)
        {
            uint X = Convert.ToUInt32(location.X);
            uint Y = Convert.ToUInt32(location.Y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, X, Y, 0, 0);
        }

        public static void DoMouseClickUp(Point location)
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

        // Maximizes and Focuses TTR
        public static void MaximizeAndFocusTTRWindow()
        {
            nint hwnd = FindToontownWindow();
            if (hwnd == IntPtr.Zero)
                return;

            // Restore first if minimized, then maximize - DON'T minimize first as that causes screen shake
            ShowWindow(hwnd, SW_RESTORE);
            Thread.Sleep(50);
            ShowWindow(hwnd, SW_MAXIMIZE);
            Thread.Sleep(50);
            SetForegroundWindow(hwnd);
        }

        /// <summary>
        /// Focuses the TTR window without changing its size/position.
        /// </summary>
        public static void FocusTTRWindow()
        {
            nint hwnd = FindToontownWindow();
            if (hwnd == IntPtr.Zero)
                return;

            SetForegroundWindow(hwnd);
        }

        /// <summary>
        /// Forces the Toontown Rewritten window to fullscreen position (0,0) covering the primary screen.
        /// Call this before starting any bot operations to ensure consistent window positioning.
        /// </summary>
        /// <returns>True if window was found and positioned, false otherwise</returns>
        public static bool ForceGameWindowFullscreen()
        {
            nint hwnd = FindToontownWindow();
            if (hwnd == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("[CoreFunctionality] Toontown window not found");
                return false;
            }

            // Restore window first if minimized
            ShowWindow(hwnd, SW_RESTORE);
            System.Threading.Thread.Sleep(100);

            // Maximize the window (same as clicking the maximize button)
            ShowWindow(hwnd, SW_MAXIMIZE);
            System.Threading.Thread.Sleep(100);

            // Bring to foreground
            SetForegroundWindow(hwnd);

            // Small delay to let window finish resizing
            System.Threading.Thread.Sleep(300);

            // Verify the window position
            RECT rect;
            if (GetWindowRect(hwnd, out rect))
            {
                System.Diagnostics.Debug.WriteLine($"[CoreFunctionality] Game window maximized at ({rect.Left}, {rect.Top}) size {rect.Right - rect.Left}x{rect.Bottom - rect.Top}");
            }

            return true;
        }

        /// <summary>
        /// Gets the offset that needs to be added to window-relative coordinates to get screen coordinates.
        /// </summary>
        public static Point GetGameWindowOffset()
        {
            nint hwnd = FindToontownWindow();
            if (hwnd == IntPtr.Zero)
                return Point.Empty;

            RECT rect;
            if (GetWindowRect(hwnd, out rect))
            {
                return new Point(rect.Left, rect.Top);
            }
            return Point.Empty;
        }

        /// <summary>
        /// Checks if the Toontown window is running and visible.
        /// </summary>
        public static bool IsGameWindowReady()
        {
            nint hwnd = FindToontownWindow();
            if (hwnd == IntPtr.Zero)
                return false;

            return IsWindowVisible(hwnd);
        }

        /// <summary>
        /// Gets the current position and size of the Toontown window.
        /// </summary>
        public static Rectangle GetGameWindowRect()
        {
            nint hwnd = FindToontownWindow();
            if (hwnd == IntPtr.Zero)
                return Rectangle.Empty;

            RECT rect;
            if (GetWindowRect(hwnd, out rect))
            {
                return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
            return Rectangle.Empty;
        }

        // Window show commands
        private const int SW_RESTORE = 9;
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;
        private const int SW_SHOWNORMAL = 1;

        // SetWindowPos flags
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

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

        /// <summary>
        /// Either creates and returns the path to a specific custom actions folder or returns the paths of all JSON files within that folder.
        /// </summary>
        /// <param name="actionType">The type of actions folder to manage ('Fishing' or 'Golf').</param>
        /// <param name="returnFiles">If true, returns paths of all .json files in the folder; otherwise, returns the folder path.</param>
        /// <returns>If returnFiles is false, returns the path to the folder. If returnFiles is true, returns an array of file paths for .json files in the folder.</returns>
        public static object ManageCustomActionsFolder(string actionType, bool returnFiles = false)
        {
            // Define the folder name based on the action type
            string folderName = actionType == "Fishing" ? "Custom Fishing Actions" : "Custom Golf Actions";

            // Get the directory where the executable is running
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Combine the executable path with the specific folder name
            string customActionsFolderPath = Path.Combine(exePath, folderName);

            // Ensure the directory exists. This method creates the directory if it does not exist
            // and does nothing if it already exists.
            Directory.CreateDirectory(customActionsFolderPath);

            // If only the path is required, return it
            if (!returnFiles)
            {
                return customActionsFolderPath;
            }

            // If files are requested, read and return only .json files in the folder
            return Directory.GetFiles(customActionsFolderPath, "*.json");
        }

        /// <summary>
        /// Extracts an embedded resource from the assembly and writes it to a specified file path.
        /// </summary>
        /// <param name="resourceName">The fully qualified name of the embedded resource.</param>
        /// <param name="outputFile">The path where the resource file should be saved. This method overwrites any existing file.</param>
        /// <exception cref="FileNotFoundException">Thrown if the specified resource is not found in the assembly.</exception>
        public static void ExtractResourceToFile(string resourceName, string outputFile)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new FileNotFoundException($"Resource '{resourceName}' not found in assembly.");
                }

                using (var fileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }
        }

        /// <summary>
        /// Ensures that all necessary JSON files from embedded resources related to both Fishing and Golf are available
        /// in the application's directory. It extracts any missing files.
        /// </summary>
        public static void EnsureAllEmbeddedJsonFilesExist()
        {
            // Handle Fishing Actions
            string fishingFolderPath = (string)ManageCustomActionsFolder("Fishing", false);
            var fishingResources = GetFishingResourceDictionary();
            EnsureEmbeddedJsonFilesExist(fishingFolderPath, fishingResources);

            // Handle Golf Actions
            string golfFolderPath = (string)ManageCustomActionsFolder("Golf", false);
            var golfResources = GetGolfResourceDictionary();
            EnsureEmbeddedJsonFilesExist(golfFolderPath, golfResources);
        }

        /// <summary>
        /// Checks and extracts missing files for the specified custom actions based on the given resource dictionary.
        /// </summary>
        /// <param name="folderPath">The folder path where files should be checked and saved.</param>
        /// <param name="resources">A dictionary of embedded resource names and their respective file names.</param>
        private static void EnsureEmbeddedJsonFilesExist(string folderPath, Dictionary<string, string> resources)
        {
            foreach (var resource in resources)
            {
                string fullPath = Path.Combine(folderPath, resource.Value);
                if (!File.Exists(fullPath))
                {
                    ExtractResourceToFile(resource.Key, fullPath);
                    Console.WriteLine($"Extracted: {resource.Value}");
                }
            }
        }

        /// <summary>
        /// Retrieves a dictionary of embedded resource file names related to Custom Fishing Actions.
        /// This dictionary maps the embedded resource names to more readable JSON file names, to be used when extracting these resources to the file system.
        /// The method scans all embedded resources that start with a specific prefix related to Custom Fishing Actions.
        /// </summary>
        /// <returns>A dictionary where keys are the full embedded resource names and values are the corresponding filenames intended for saving to disk.</returns>
        public static Dictionary<string, string> GetFishingResourceDictionary()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            string prefix = "ToonTown_Rewritten_Bot.Services.CustomFishingActions";
            Dictionary<string, string> resourceMap = new Dictionary<string, string>();

            foreach (string resourceName in resourceNames)
            {
                if (resourceName.StartsWith(prefix))
                {
                    // Extracting the filename from the resource path and removing extension for better readability
                    string fileName = Path.GetFileNameWithoutExtension(resourceName.Substring(prefix.Length + 1));
                    resourceMap.Add(resourceName, fileName + ".json");
                }
            }
            return resourceMap;
        }

        /// <summary>
        /// Retrieves a dictionary of embedded resource file names related to Custom Golf Actions.
        /// This dictionary maps the embedded resource names to more readable JSON file names, to be used when extracting these resources to the file system.
        /// The method scans all embedded resources that start with a specific prefix related to Custom Golf Actions.
        /// </summary>
        /// <returns>A dictionary where keys are the full embedded resource names and values are the corresponding filenames intended for saving to disk.</returns>
        public static Dictionary<string, string> GetGolfResourceDictionary()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            string prefix = "ToonTown_Rewritten_Bot.Services.CustomGolfActions";
            Dictionary<string, string> resourceMap = new Dictionary<string, string>();

            foreach (string resourceName in resourceNames)
            {
                if (resourceName.StartsWith(prefix))
                {
                    // Extracting the filename from the resource path and removing extension for better readability
                    string fileName = Path.GetFileNameWithoutExtension(resourceName.Substring(prefix.Length + 1));
                    resourceMap.Add(resourceName, fileName + ".json");
                }
            }
            return resourceMap;
        }

        //ignore .dll imports below
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(ref Point lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public static IntPtr FindToontownWindow()
        {
            // Attempt to find the Toontown window by its title
            return FindWindow(null, "Toontown Rewritten");
        }

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
