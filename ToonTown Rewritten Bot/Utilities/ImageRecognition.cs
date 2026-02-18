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
        /// Falls back to CopyFromScreen if PrintWindow fails or returns a black frame.
        /// </summary>
        private static Bitmap CaptureWindowWithPrintWindow(nint windowHandle, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            bool printWindowSucceeded = false;

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    // PW_RENDERFULLCONTENT (0x2) works better with DWM/hardware-accelerated windows
                    printWindowSucceeded = NativeMethods.PrintWindow(windowHandle, hdc, NativeMethods.PW_RENDERFULLCONTENT);

                    if (!printWindowSucceeded)
                    {
                        // Fallback: try without the flag
                        printWindowSucceeded = NativeMethods.PrintWindow(windowHandle, hdc, 0);
                    }
                }
                finally
                {
                    graphics.ReleaseHdc(hdc);
                }

                if (!printWindowSucceeded)
                {
                    // PrintWindow failed completely, fall back to screen capture
                    Debug.WriteLine("PrintWindow failed, falling back to CopyFromScreen");
                    NativeMethods.Rect windowRect = new NativeMethods.Rect();
                    NativeMethods.GetWindowRect(windowHandle, ref windowRect);
                    graphics.CopyFromScreen(windowRect.Left, windowRect.Top, 0, 0, bitmap.Size);
                    return bitmap;
                }
            }

            // PrintWindow reported success — verify the capture isn't a black frame
            // (common with DirectX/OpenGL games on certain GPU/driver configurations)
            if (IsBitmapBlack(bitmap))
            {
                Debug.WriteLine("PrintWindow returned a black frame, falling back to CopyFromScreen");
                NativeMethods.Rect windowRect = new NativeMethods.Rect();
                NativeMethods.GetWindowRect(windowHandle, ref windowRect);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(windowRect.Left, windowRect.Top, 0, 0, bitmap.Size);
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Checks if a bitmap is effectively all black by sampling pixels across the inner region.
        /// Avoids window chrome/borders by sampling within the center 60% of the image.
        /// </summary>
        private static bool IsBitmapBlack(Bitmap bitmap)
        {
            if (bitmap.Width == 0 || bitmap.Height == 0)
                return true;

            // 20% margin on each side to avoid window frame/title bar
            int marginX = bitmap.Width / 5;
            int marginY = bitmap.Height / 5;
            int innerWidth = bitmap.Width - 2 * marginX;
            int innerHeight = bitmap.Height - 2 * marginY;

            if (innerWidth <= 0 || innerHeight <= 0)
                return true;

            const int cols = 5;
            const int rows = 4;
            const int brightnessThreshold = 10;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int x = marginX + (innerWidth * col) / (cols - 1);
                    int y = marginY + (innerHeight * row) / (rows - 1);

                    x = Math.Min(x, bitmap.Width - 1);
                    y = Math.Min(y, bitmap.Height - 1);

                    Color pixel = bitmap.GetPixel(x, y);
                    if (pixel.R > brightnessThreshold || pixel.G > brightnessThreshold || pixel.B > brightnessThreshold)
                        return false;
                }
            }

            return true;
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
            public const uint PW_RENDERFULLCONTENT = 0x00000002; // Works better with DWM/hardware acceleration
        }
        #endregion

        /// <summary>
        /// Converts a rectangle from PictureBox preview coordinates to actual image coordinates,
        /// accounting for Zoom mode letterboxing/pillarboxing.
        /// </summary>
        /// <param name="previewRect">The rectangle in PictureBox coordinates.</param>
        /// <param name="imageSize">The actual image dimensions.</param>
        /// <param name="pictureBoxSize">The PictureBox control dimensions.</param>
        /// <returns>The rectangle in actual image coordinates, clamped to image bounds.</returns>
        public static Rectangle ConvertToImageCoordinates(Rectangle previewRect, Size imageSize, Size pictureBoxSize)
        {
            if (imageSize.Width == 0 || imageSize.Height == 0)
                return previewRect;

            float imageAspect = (float)imageSize.Width / imageSize.Height;
            float boxAspect = (float)pictureBoxSize.Width / pictureBoxSize.Height;

            float scale;
            int offsetX = 0, offsetY = 0;

            if (imageAspect > boxAspect)
            {
                scale = (float)pictureBoxSize.Width / imageSize.Width;
                offsetY = (int)((pictureBoxSize.Height - imageSize.Height * scale) / 2);
            }
            else
            {
                scale = (float)pictureBoxSize.Height / imageSize.Height;
                offsetX = (int)((pictureBoxSize.Width - imageSize.Width * scale) / 2);
            }

            int x = (int)((previewRect.X - offsetX) / scale);
            int y = (int)((previewRect.Y - offsetY) / scale);
            int width = (int)(previewRect.Width / scale);
            int height = (int)(previewRect.Height / scale);

            x = Math.Max(0, Math.Min(x, imageSize.Width));
            y = Math.Max(0, Math.Min(y, imageSize.Height));
            width = Math.Min(width, imageSize.Width - x);
            height = Math.Min(height, imageSize.Height - y);

            return new Rectangle(x, y, width, height);
        }
    }
}
