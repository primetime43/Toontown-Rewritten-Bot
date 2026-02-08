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
        /// <summary>
        /// Performs a mouse click at the current cursor position using SendInput (modern API).
        /// </summary>
        public static void DoMouseClick()
        {
            SendInputMouseClick();
        }

        /// <summary>
        /// Performs a fishing click - drag down from current position.
        /// Uses SendInput for reliable mouse simulation.
        /// </summary>
        public static void DoFishingClick()
        {
            Point startPos = getCursorLocation();

            SendInputMouseDown();
            Thread.Sleep(500);

            SimulateDragMove(startPos.X, startPos.Y + 150);
            Thread.Sleep(500);

            SendInputMouseUp();
        }

        /// <summary>
        /// Performs a fishing click with a custom drag destination for auto-detect fishing.
        /// Uses SendInput for reliable mouse simulation.
        /// </summary>
        public static void DoFishingClickWithDestination(int destinationX, int destinationY)
        {
            Point startPos = getCursorLocation();

            System.Diagnostics.Debug.WriteLine($"[DoFishingClickWithDestination] Start: ({startPos.X}, {startPos.Y}) -> Dest: ({destinationX}, {destinationY})");

            SendInputMouseDown();
            Thread.Sleep(500);

            SimulateDragMove(destinationX, destinationY);
            Thread.Sleep(500);

            SendInputMouseUp();
        }

        /// <summary>
        /// Performs a mouse click at the specified location using SendInput (modern API).
        /// </summary>
        private static void DoMouseClick(Point location)
        {
            SimulateDragMove(location.X, location.Y);
            SendInputMouseClick();
        }

        /// <summary>
        /// Presses the left mouse button down using SendInput (modern API).
        /// </summary>
        public static void DoMouseClickDown(Point location)
        {
            SimulateDragMove(location.X, location.Y);
            SendInputMouseDown();
        }

        /// <summary>
        /// Releases the left mouse button using SendInput (modern API).
        /// </summary>
        public static void DoMouseClickUp(Point location)
        {
            SimulateDragMove(location.X, location.Y);
            SendInputMouseUp();
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
        /// Focuses and maximizes the TTR window without the screen shake.
        /// Uses restore→maximize (no minimize step which causes shake).
        /// </summary>
        public static void FocusTTRWindow()
        {
            nint hwnd = FindToontownWindow();
            if (hwnd == IntPtr.Zero)
                return;

            // Restore first if minimized, then maximize - no minimize step to avoid shake
            ShowWindow(hwnd, SW_RESTORE);
            Thread.Sleep(50);
            ShowWindow(hwnd, SW_MAXIMIZE);
            Thread.Sleep(50);
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

        private const string GameWindowNotFoundMessage = "Toontown Rewritten window not found. Please make sure the game is running.";

        /// <summary>
        /// Ensures the game window is ready, throwing an exception if not.
        /// Use this in services that should throw on failure.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when game window is not found.</exception>
        public static void EnsureGameWindowReady()
        {
            if (!IsGameWindowReady())
            {
                throw new InvalidOperationException(GameWindowNotFoundMessage);
            }
        }

        /// <summary>
        /// Checks if game window is ready, showing a message box and returning false if not.
        /// Use this in UI event handlers that should show user-friendly errors.
        /// </summary>
        /// <returns>True if game window is ready, false otherwise.</returns>
        public static bool EnsureGameWindowReadyWithMessage()
        {
            if (!IsGameWindowReady())
            {
                MessageBox.Show(GameWindowNotFoundMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
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
        /// <param name="actionType">The type of actions folder to manage ('Fishing', 'Golf', or 'Gardening').</param>
        /// <param name="returnFiles">If true, returns paths of all .json files in the folder; otherwise, returns the folder path.</param>
        /// <returns>If returnFiles is false, returns the path to the folder. If returnFiles is true, returns an array of file paths for .json files in the folder.</returns>
        public static object ManageCustomActionsFolder(string actionType, bool returnFiles = false)
        {
            // Define the folder name based on the action type
            string folderName = actionType switch
            {
                "Fishing" => "Custom Fishing Actions",
                "Golf" => "Custom Golf Actions",
                "Gardening" => "Custom Gardening Actions",
                _ => $"Custom {actionType} Actions"
            };

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
        /// Retrieves a dictionary of embedded resource file names for the given resource prefix.
        /// Maps embedded resource names to readable JSON filenames for extraction to disk.
        /// </summary>
        private static Dictionary<string, string> GetResourceDictionary(string prefix)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            Dictionary<string, string> resourceMap = new Dictionary<string, string>();

            foreach (string resourceName in resourceNames)
            {
                if (resourceName.StartsWith(prefix))
                {
                    string fileName = Path.GetFileNameWithoutExtension(resourceName.Substring(prefix.Length + 1));
                    resourceMap.Add(resourceName, fileName + ".json");
                }
            }
            return resourceMap;
        }

        public static Dictionary<string, string> GetFishingResourceDictionary()
            => GetResourceDictionary("ToonTown_Rewritten_Bot.Services.CustomFishingActions");

        public static Dictionary<string, string> GetGolfResourceDictionary()
            => GetResourceDictionary("ToonTown_Rewritten_Bot.Services.CustomGolfActions");

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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint GetDesktopWindow();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint GetWindowDC(nint window);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern uint GetPixel(nint dc, int x, int y);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int ReleaseDC(nint window, nint dc);

        // Modern SendInput API for mouse simulation
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        // Mouse event flags for SendInput
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        private const int INPUT_MOUSE = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        /// <summary>
        /// Converts screen coordinates to normalized mouse coordinates (0-65535 range).
        /// Uses the virtual screen (all monitors combined) for proper multi-monitor support.
        /// </summary>
        private static (int mouseX, int mouseY) GetNormalizedMouseCoordinates(int screenX, int screenY)
        {
            // Use virtual screen bounds to support multiple monitors
            // VirtualScreen represents the bounding rectangle of all monitors combined
            int virtualScreenLeft = SystemInformation.VirtualScreen.Left;
            int virtualScreenTop = SystemInformation.VirtualScreen.Top;
            int virtualScreenWidth = SystemInformation.VirtualScreen.Width;
            int virtualScreenHeight = SystemInformation.VirtualScreen.Height;

            // Normalize to 0-65535 range, accounting for virtual screen offset
            // This correctly handles monitors to the left of or above the primary monitor
            int mouseX = (int)(((screenX - virtualScreenLeft) * 65536L) / virtualScreenWidth);
            int mouseY = (int)(((screenY - virtualScreenTop) * 65536L) / virtualScreenHeight);

            return (mouseX, mouseY);
        }

        /// <summary>
        /// Moves the mouse during a drag operation using SendInput (modern API).
        /// This is more reliable than mouse_event for drag operations.
        /// </summary>
        public static void SimulateDragMove(int x, int y)
        {
            var (mouseX, mouseY) = GetNormalizedMouseCoordinates(x, y);

            var input = new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dx = mouseX,
                    dy = mouseY,
                    mouseData = 0,
                    dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Presses the left mouse button down using SendInput.
        /// </summary>
        public static void SendInputMouseDown()
        {
            var currentPos = getCursorLocation();
            var (mouseX, mouseY) = GetNormalizedMouseCoordinates(currentPos.X, currentPos.Y);

            var input = new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dx = mouseX,
                    dy = mouseY,
                    mouseData = 0,
                    dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Releases the left mouse button using SendInput.
        /// </summary>
        public static void SendInputMouseUp()
        {
            var currentPos = getCursorLocation();
            var (mouseX, mouseY) = GetNormalizedMouseCoordinates(currentPos.X, currentPos.Y);

            var input = new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dx = mouseX,
                    dy = mouseY,
                    mouseData = 0,
                    dwFlags = MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Performs a complete mouse click (down + up) at the current cursor position using SendInput.
        /// </summary>
        public static void SendInputMouseClick()
        {
            var currentPos = getCursorLocation();
            var (mouseX, mouseY) = GetNormalizedMouseCoordinates(currentPos.X, currentPos.Y);

            // Send both down and up events
            var inputs = new INPUT[]
            {
                new INPUT
                {
                    type = INPUT_MOUSE,
                    mi = new MOUSEINPUT
                    {
                        dx = mouseX,
                        dy = mouseY,
                        mouseData = 0,
                        dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                },
                new INPUT
                {
                    type = INPUT_MOUSE,
                    mi = new MOUSEINPUT
                    {
                        dx = mouseX,
                        dy = mouseY,
                        mouseData = 0,
                        dwFlags = MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
