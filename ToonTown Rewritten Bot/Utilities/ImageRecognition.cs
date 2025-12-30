/*using Emgu.CV.CvEnum;
using Emgu.CV;
using Emgu.CV.Stitching;
using Emgu.CV.Structure;*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing.Imaging;

namespace ToonTown_Rewritten_Bot.Utilities
{
    class ImageRecognition
    {
        /// <summary>
        /// Captures a screenshot of the game window.
        /// </summary>
        /// <param name="captureBackground">If true, captures the window even when obscured by other windows</param>
        /// <returns>Screenshot of the game window</returns>
        public static Image GetWindowScreenshot(bool captureBackground = true)
        {
            string windowName = "Toontown Rewritten";
            // Find the window by name
            nint windowHandle = NativeMethods.FindWindow(null, windowName);
            if (windowHandle == nint.Zero)
            {
                throw new ArgumentException("Window not found.");
            }

            // Get the window's position and size
            NativeMethods.Rect windowRect = new NativeMethods.Rect();
            NativeMethods.GetWindowRect(windowHandle, ref windowRect);

            if (captureBackground)
            {
                // Use PrintWindow to capture even when window is behind other windows
                return CaptureWindowWithPrintWindow(windowHandle, windowRect.Width, windowRect.Height);
            }
            else
            {
                // Traditional screen capture (only works when window is visible)
                Bitmap screenshot = new Bitmap(windowRect.Width, windowRect.Height);
                using (Graphics graphics = Graphics.FromImage(screenshot))
                {
                    graphics.CopyFromScreen(windowRect.Left, windowRect.Top, 0, 0, screenshot.Size);
                }
                return screenshot;
            }
        }

        /// <summary>
        /// Captures a window using PrintWindow API, which works even when the window is obscured.
        /// </summary>
        private static Bitmap CaptureWindowWithPrintWindow(nint windowHandle, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    // PW_RENDERFULLCONTENT (0x2) works better with DWM/hardware-accelerated windows
                    bool success = NativeMethods.PrintWindow(windowHandle, hdc, NativeMethods.PW_RENDERFULLCONTENT);

                    if (!success)
                    {
                        // Fallback: try without the flag
                        success = NativeMethods.PrintWindow(windowHandle, hdc, 0);
                    }

                    if (!success)
                    {
                        // If PrintWindow fails completely, fall back to screen capture
                        graphics.ReleaseHdc(hdc);
                        NativeMethods.Rect windowRect = new NativeMethods.Rect();
                        NativeMethods.GetWindowRect(windowHandle, ref windowRect);
                        graphics.CopyFromScreen(windowRect.Left, windowRect.Top, 0, 0, bitmap.Size);
                        return bitmap;
                    }
                }
                finally
                {
                    graphics.ReleaseHdc(hdc);
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Gets the game window handle.
        /// </summary>
        /// <returns>Window handle or IntPtr.Zero if not found</returns>
        public static IntPtr GetGameWindowHandle()
        {
            return NativeMethods.FindWindow(null, "Toontown Rewritten");
        }

        /// <summary>
        /// Checks if the game window exists and is visible.
        /// </summary>
        public static bool IsGameWindowAvailable()
        {
            IntPtr handle = GetGameWindowHandle();
            return handle != IntPtr.Zero && NativeMethods.IsWindow(handle);
        }

        public static async Task<Point> locateColorInImage(Image screenShot, string hexValue, int tolerance)
        {
            // Convert the HEX value to an RGB value
            Color colorToSearch = ColorTranslator.FromHtml(hexValue);

            // Convert the Image object to a Bitmap object
            Bitmap image = new Bitmap(screenShot);

            // Find the first occurrence of the color in the image
            Point colorLocation = Point.Empty;
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color pixelColor = image.GetPixel(x, y);
                    if (Math.Abs(pixelColor.R - colorToSearch.R) <= tolerance &&
                        Math.Abs(pixelColor.G - colorToSearch.G) <= tolerance &&
                        Math.Abs(pixelColor.B - colorToSearch.B) <= tolerance)
                    {
                        colorLocation = new Point(x, y);
                        break;
                    }
                }
                if (!colorLocation.IsEmpty)
                {
                    break;
                }
            }

            if (!colorLocation.IsEmpty)
            {
                Debug.WriteLine("Color found at location ({0}, {1})", colorLocation.X, colorLocation.Y);
                return colorLocation;
                //BotFunctions.MoveCursor(colorLocation.X, colorLocation.Y);
                //Thread.Sleep(1000);
            }
            else
            {
                Debug.WriteLine("Color not found in image");
                return Point.Empty;
            }
        }

        #region Native Window Methods (Win32 API)
        // NativeMethods class to import Win32 API functions
        public static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct Rect
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
                public int Width { get { return Right - Left; } }
                public int Height { get { return Bottom - Top; } }
            }

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern nint FindWindow(string lpClassName, string lpWindowName);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetWindowRect(nint hWnd, ref Rect lpRect);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWindowVisible(IntPtr hWnd);

            // PrintWindow flags
            public const uint PW_CLIENTONLY = 0x00000001;
            public const uint PW_RENDERFULLCONTENT = 0x00000002; // Works better with DWM/hardware acceleration
        }
        #endregion
    }
}
