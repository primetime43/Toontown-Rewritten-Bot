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
using ToonTown_Rewritten_Bot.Utilities;
using static ToonTown_Rewritten_Bot.Utilities.ImageRecognition;

namespace ToonTown_Rewritten_Bot.Services
{
    public class CoreFunctionality
    {
        // ── Background input mode ──────────────────────────────────────
        // Mouse/keyboard events are sent via PostMessage directly to the game
        // window instead of using SendInput. This avoids moving the real cursor,
        // letting the user browse while the bot runs.
        public static volatile bool UseBackgroundInput = false;

        // Tracks the intended cursor position in background mode (screen coords).
        private static Point _backgroundTargetPos;

        // In background mode the unfocused game samples mouse-button state at a reduced rate,
        // so an instant press+release can fall entirely between frames and never register.
        // Holding the button briefly guarantees the down and up straddle a sampling frame —
        // this is why fishing (whose cast already holds the button) works unfocused.
        private const int BackgroundClickHoldMs = 300;

        /// <summary>
        /// Performs a mouse click at the current cursor position using SendInput (modern API).
        /// </summary>
        public static void DoMouseClick()
        {
            if (UseBackgroundInput)
            {
                Logger.Debug("Input", $"DoMouseClick at ({_backgroundTargetPos.X}, {_backgroundTargetPos.Y})");
                PostBackgroundMouseMessage(WM_LBUTTONDOWN, _backgroundTargetPos.X, _backgroundTargetPos.Y);
                Thread.Sleep(BackgroundClickHoldMs);
                PostBackgroundMouseMessage(WM_LBUTTONUP, _backgroundTargetPos.X, _backgroundTargetPos.Y);
                return;
            }
            Logger.Debug("Input", $"DoMouseClick at {getCursorLocation()}");
            SendInputMouseClick();
        }

        public static void DoFishingClick()
        {
            if (UseBackgroundInput)
            {
                var startPos = _backgroundTargetPos;
                Logger.Debug("Input", $"DoFishingClick from ({startPos.X}, {startPos.Y}) drag to ({startPos.X}, {startPos.Y + 150})");
                PostBackgroundMouseMessage(WM_LBUTTONDOWN, startPos.X, startPos.Y);
                Thread.Sleep(500);
                PostBackgroundMouseMessage(WM_LBUTTONDOWN, startPos.X, startPos.Y + 150);
                Thread.Sleep(500);
                PostBackgroundMouseMessage(WM_LBUTTONUP, startPos.X, startPos.Y + 150);
                _backgroundTargetPos = new Point(startPos.X, startPos.Y + 150);
                return;
            }
            Point pos = getCursorLocation();
            Logger.Debug("Input", $"DoFishingClick from ({pos.X}, {pos.Y}) drag to ({pos.X}, {pos.Y + 150})");
            SendInputMouseDown();
            Thread.Sleep(500);
            SimulateDragMove(pos.X, pos.Y + 150);
            Thread.Sleep(500);
            SendInputMouseUp();
        }

        public static void DoFishingClickWithDestination(int destinationX, int destinationY)
        {
            if (UseBackgroundInput)
            {
                Logger.Debug("Input", $"DoFishingClickWithDestination: ({_backgroundTargetPos.X}, {_backgroundTargetPos.Y}) -> ({destinationX}, {destinationY})");
                PostBackgroundMouseMessage(WM_LBUTTONDOWN, _backgroundTargetPos.X, _backgroundTargetPos.Y);
                Thread.Sleep(500);
                PostBackgroundMouseMessage(WM_LBUTTONDOWN, destinationX, destinationY);
                Thread.Sleep(500);
                PostBackgroundMouseMessage(WM_LBUTTONUP, destinationX, destinationY);
                _backgroundTargetPos = new Point(destinationX, destinationY);
                return;
            }
            Point startPos = getCursorLocation();
            Logger.Debug("Input", $"DoFishingClickWithDestination: ({startPos.X}, {startPos.Y}) -> ({destinationX}, {destinationY})");
            SendInputMouseDown();
            Thread.Sleep(500);
            SimulateDragMove(destinationX, destinationY);
            Thread.Sleep(500);
            SendInputMouseUp();
        }

        private static void DoMouseClick(Point location)
        {
            if (UseBackgroundInput)
            {
                _backgroundTargetPos = location;
                PostBackgroundMouseMessage(WM_LBUTTONDOWN, location.X, location.Y);
                Thread.Sleep(BackgroundClickHoldMs);
                PostBackgroundMouseMessage(WM_LBUTTONUP, location.X, location.Y);
                return;
            }
            SimulateDragMove(location.X, location.Y);
            SendInputMouseClick();
        }

        public static void DoMouseClickDown(Point location)
        {
            if (UseBackgroundInput)
            {
                _backgroundTargetPos = location;
                PostBackgroundMouseMessage(WM_LBUTTONDOWN, location.X, location.Y);
                return;
            }
            SimulateDragMove(location.X, location.Y);
            SendInputMouseDown();
        }

        public static void DoMouseClickUp(Point location)
        {
            if (UseBackgroundInput)
            {
                _backgroundTargetPos = location;
                PostBackgroundMouseMessage(WM_LBUTTONUP, location.X, location.Y);
                return;
            }
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

        /// <summary>
        /// Gets pixel color from a pre-captured game window screenshot.
        /// Converts screen coordinates to window-relative coordinates automatically.
        /// </summary>
        /// <param name="screenshot">A bitmap captured via ImageRecognition.GetWindowScreenshot()</param>
        /// <param name="screenX">Screen X coordinate</param>
        /// <param name="screenY">Screen Y coordinate</param>
        /// <param name="windowOffset">The game window's top-left corner in screen coords</param>
        /// <returns>The color at the specified position, or Color.Empty if out of bounds</returns>
        public static Color GetColorFromScreenshot(Bitmap screenshot, int screenX, int screenY, Point windowOffset)
        {
            int clientX = screenX - windowOffset.X;
            int clientY = screenY - windowOffset.Y;

            if (clientX < 0 || clientY < 0 || clientX >= screenshot.Width || clientY >= screenshot.Height)
                return Color.Empty;

            return screenshot.GetPixel(clientX, clientY);
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
            Logger.Debug("Input", $"MoveCursor to ({x}, {y})");
            if (UseBackgroundInput)
            {
                _backgroundTargetPos = new Point(x, y);
                PostBackgroundMouseMove(x, y);
                return;
            }
            Cursor.Position = new Point(x, y);
        }

        /// <summary>
        /// The bot's effective cursor position: the real cursor in foreground mode, or the last
        /// background target point in background mode (where the real cursor is never moved).
        /// </summary>
        public static Point GetEffectiveCursorLocation()
        {
            return UseBackgroundInput ? _backgroundTargetPos : getCursorLocation();
        }

        public static void MaximizeAndFocusTTRWindow()
        {
            if (UseBackgroundInput) return;
            nint hwnd = FindToontownWindow();
            if (hwnd == IntPtr.Zero) return;
            ShowWindow(hwnd, SW_RESTORE);
            Thread.Sleep(50);
            ShowWindow(hwnd, SW_MAXIMIZE);
            Thread.Sleep(50);
            SetForegroundWindow(hwnd);
        }

        public static void FocusTTRWindow()
        {
            if (UseBackgroundInput) return;
            nint hwnd = FindToontownWindow();
            if (hwnd == IntPtr.Zero) return;
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
            if (UseBackgroundInput) return IsGameWindowReady();
            nint hwnd = FindToontownWindow();
            if (hwnd == IntPtr.Zero)
            {
                Logger.Warning("Input", "Toontown window not found");
                return false;
            }
            ShowWindow(hwnd, SW_RESTORE);
            Thread.Sleep(100);
            ShowWindow(hwnd, SW_MAXIMIZE);
            Thread.Sleep(100);
            SetForegroundWindow(hwnd);
            Thread.Sleep(300);
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
            string exePath = AppPaths.ExeDirectory;

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

        // ── Background input: PostMessage-based mouse/keyboard ─────────
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessageW(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        // Window message constants
        private const uint WM_MOUSEMOVE = 0x0200;
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const int MK_LBUTTON = 0x0001;

        /// <summary>
        /// Posts a mouse message (WM_LBUTTONDOWN/UP) to the game window using client-relative coordinates.
        /// Screen coordinates are converted to client coordinates automatically.
        /// </summary>
        private static void PostBackgroundMouseMessage(uint msg, int screenX, int screenY)
        {
            IntPtr hwnd = FindToontownWindow();
            if (hwnd == IntPtr.Zero)
            {
                Logger.Warning("Input", "PostBackgroundMouseMessage: Toontown window not found — message dropped");
                return;
            }

            Point client = new Point(screenX, screenY);
            ScreenToClient(hwnd, ref client);

            IntPtr wParam = msg == WM_LBUTTONDOWN ? (IntPtr)MK_LBUTTON : IntPtr.Zero;
            IntPtr lParam = (IntPtr)((client.Y << 16) | (client.X & 0xFFFF));

            bool posted = PostMessageW(hwnd, msg, wParam, lParam);
            string msgName = msg == WM_LBUTTONDOWN ? "LBUTTONDOWN" : msg == WM_LBUTTONUP ? "LBUTTONUP" : msg.ToString("X");
            Logger.Debug("Input", $"Post {msgName} hwnd=0x{hwnd.ToInt64():X} screen=({screenX},{screenY}) client=({client.X},{client.Y}) posted={posted}");
        }

        /// <summary>
        /// Posts a mouse-move (WM_MOUSEMOVE) to the game window in client-relative coordinates.
        /// Lets background mode drive hover-activated UI (e.g. SpeedChat submenus) that would
        /// otherwise need the real cursor to physically move over the menu.
        /// </summary>
        private static void PostBackgroundMouseMove(int screenX, int screenY)
        {
            IntPtr hwnd = FindToontownWindow();
            if (hwnd == IntPtr.Zero)
            {
                Logger.Warning("Input", "PostBackgroundMouseMove: Toontown window not found — move dropped");
                return;
            }

            Point client = new Point(screenX, screenY);
            ScreenToClient(hwnd, ref client);

            IntPtr lParam = (IntPtr)((client.Y << 16) | (client.X & 0xFFFF));
            bool posted = PostMessageW(hwnd, WM_MOUSEMOVE, IntPtr.Zero, lParam);
            Logger.Debug("Input", $"Post MOUSEMOVE hwnd=0x{hwnd.ToInt64():X} screen=({screenX},{screenY}) client=({client.X},{client.Y}) posted={posted}");
        }

        /// <summary>
        /// Sends a key down event to the game window via PostMessage (background mode).
        /// Handles extended key flag for arrow keys and navigation keys.
        /// </summary>
        public static void PostBackgroundKeyDown(int virtualKeyCode)
        {
            IntPtr hwnd = FindToontownWindow();
            if (hwnd == IntPtr.Zero) return;

            int lParam = 1; // repeat count = 1
            // Extended key flag (bit 24) for arrow keys, Home, End
            if (virtualKeyCode is 0x25 or 0x26 or 0x27 or 0x28 or 0x23 or 0x24)
                lParam |= 1 << 24;

            PostMessageW(hwnd, WM_KEYDOWN, (IntPtr)virtualKeyCode, (IntPtr)lParam);
        }

        /// <summary>
        /// Sends a key up event to the game window via PostMessage (background mode).
        /// Sets previous-key-state and transition-state bits as Windows expects.
        /// </summary>
        public static void PostBackgroundKeyUp(int virtualKeyCode)
        {
            IntPtr hwnd = FindToontownWindow();
            if (hwnd == IntPtr.Zero) return;

            int lParam = 1; // repeat count = 1
            lParam |= 3 << 30; // previous key state (bit 30) + transition state (bit 31)
            // Extended key flag (bit 24) for arrow keys, Home, End
            if (virtualKeyCode is 0x25 or 0x26 or 0x27 or 0x28 or 0x23 or 0x24)
                lParam |= 1 << 24;

            PostMessageW(hwnd, WM_KEYUP, (IntPtr)virtualKeyCode, (IntPtr)lParam);
        }

        /// <summary>
        /// Sends a key press to the game window. Uses PostMessage in background mode,
        /// InputSimulator in foreground mode.
        /// </summary>
        public static void SendKeyDown(WindowsInput.VirtualKeyCode keyCode)
        {
            if (UseBackgroundInput)
            {
                PostBackgroundKeyDown((int)keyCode);
                return;
            }
            WindowsInput.InputSimulator.SimulateKeyDown(keyCode);
        }

        public static void SendKeyUp(WindowsInput.VirtualKeyCode keyCode)
        {
            if (UseBackgroundInput)
            {
                PostBackgroundKeyUp((int)keyCode);
                return;
            }
            WindowsInput.InputSimulator.SimulateKeyUp(keyCode);
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
            Logger.Debug("Input", $"SimulateDragMove to ({x}, {y})");
            if (UseBackgroundInput)
            {
                _backgroundTargetPos = new Point(x, y);
                PostBackgroundMouseMessage(WM_LBUTTONDOWN, x, y);
                return;
            }
            var (mouseX, mouseY) = GetNormalizedMouseCoordinates(x, y);
            var input = new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dx = mouseX, dy = mouseY, mouseData = 0,
                    dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK,
                    time = 0, dwExtraInfo = IntPtr.Zero
                }
            };
            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Presses the left mouse button down using SendInput.
        /// </summary>
        public static void SendInputMouseDown()
        {
            if (UseBackgroundInput)
            {
                Logger.Debug("Input", $"SendInputMouseDown at ({_backgroundTargetPos.X}, {_backgroundTargetPos.Y})");
                PostBackgroundMouseMessage(WM_LBUTTONDOWN, _backgroundTargetPos.X, _backgroundTargetPos.Y);
                return;
            }
            var currentPos = getCursorLocation();
            Logger.Debug("Input", $"SendInputMouseDown at ({currentPos.X}, {currentPos.Y})");
            var (mouseX, mouseY) = GetNormalizedMouseCoordinates(currentPos.X, currentPos.Y);
            var input = new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dx = mouseX, dy = mouseY, mouseData = 0,
                    dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK,
                    time = 0, dwExtraInfo = IntPtr.Zero
                }
            };
            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void SendInputMouseUp()
        {
            if (UseBackgroundInput)
            {
                Logger.Debug("Input", $"SendInputMouseUp at ({_backgroundTargetPos.X}, {_backgroundTargetPos.Y})");
                PostBackgroundMouseMessage(WM_LBUTTONUP, _backgroundTargetPos.X, _backgroundTargetPos.Y);
                return;
            }
            var currentPos = getCursorLocation();
            Logger.Debug("Input", $"SendInputMouseUp at ({currentPos.X}, {currentPos.Y})");
            var (mouseX, mouseY) = GetNormalizedMouseCoordinates(currentPos.X, currentPos.Y);
            var input = new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dx = mouseX, dy = mouseY, mouseData = 0,
                    dwFlags = MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK,
                    time = 0, dwExtraInfo = IntPtr.Zero
                }
            };
            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void SendInputMouseClick()
        {
            if (UseBackgroundInput)
            {
                PostBackgroundMouseMessage(WM_LBUTTONDOWN, _backgroundTargetPos.X, _backgroundTargetPos.Y);
                Thread.Sleep(BackgroundClickHoldMs);
                PostBackgroundMouseMessage(WM_LBUTTONUP, _backgroundTargetPos.X, _backgroundTargetPos.Y);
                return;
            }
            var currentPos = getCursorLocation();
            var (mouseX, mouseY) = GetNormalizedMouseCoordinates(currentPos.X, currentPos.Y);
            var inputs = new INPUT[]
            {
                new INPUT { type = INPUT_MOUSE, mi = new MOUSEINPUT { dx = mouseX, dy = mouseY, mouseData = 0,
                    dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK, time = 0, dwExtraInfo = IntPtr.Zero } },
                new INPUT { type = INPUT_MOUSE, mi = new MOUSEINPUT { dx = mouseX, dy = mouseY, mouseData = 0,
                    dwFlags = MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK, time = 0, dwExtraInfo = IntPtr.Zero } }
            };
            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
