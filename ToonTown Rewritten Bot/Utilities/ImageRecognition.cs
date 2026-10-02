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
    internal sealed class WindowCaptureException : InvalidOperationException
    {
        public WindowCaptureException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }

    class ImageRecognition
    {
        /// <summary>
        /// Captures a screenshot of the game window.
        /// </summary>
        /// <param name="captureBackground">If true, captures the window even when obscured by other windows</param>
        /// <returns>Screenshot of the game window</returns>
        public static Image GetWindowScreenshot(bool captureBackground = true)
        {
            nint windowHandle = GameProfile.FindWindow();
            if (windowHandle == nint.Zero)
            {
                throw new WindowCaptureException(GameProfile.WindowNotFoundMessage);
            }

            return CaptureGameClient(windowHandle, captureBackground);
        }

        internal static Bitmap CaptureGameClient(nint windowHandle, bool captureBackground)
        {
            if (NativeMethods.IsIconic(windowHandle))
                throw new WindowCaptureException("Toontown is minimized. Restore the game window before running the bot.");
            if (!GameWindowGeometry.TryRead(windowHandle, out var geometry))
            {
                throw new WindowCaptureException("Could not read the Toontown window bounds for screen capture.");
            }

            // All capture paths use the same client-area coordinate space.
            Bitmap frame = captureBackground
                ? GameGraphicsCapture.Capture(windowHandle, geometry)
                : CaptureVisibleWindow(windowHandle, geometry.ClientBounds);
            if (!GameWindowGeometry.TryRead(windowHandle, out var current) || current != geometry)
            {
                frame.Dispose();
                throw new WindowCaptureException("The game window moved or resized during capture. Keep it stationary while the bot runs.");
            }
            return frame;
        }

        private static Bitmap CaptureVisibleWindow(nint windowHandle, Rectangle windowRect)
        {
            if (windowRect.Width <= 0 || windowRect.Height <= 0)
            {
                throw new WindowCaptureException("The Toontown window has invalid capture dimensions.");
            }

            if (NativeMethods.IsIconic(windowHandle))
            {
                throw new WindowCaptureException(
                    "Toontown is minimized. " +
                    "Restore the game window and try again.");
            }

            Bitmap screenshot = new Bitmap(windowRect.Width, windowRect.Height, PixelFormat.Format32bppArgb);
            try
            {
                using (Graphics graphics = Graphics.FromImage(screenshot))
                {
                    graphics.CopyFromScreen(windowRect.Left, windowRect.Top, 0, 0, screenshot.Size);
                }

                if (IsBitmapEffectivelyBlack(screenshot))
                {
                    throw new WindowCaptureException(
                        "Visible Toontown capture returned a black frame. Keep the game " +
                        "visible and unobscured, then try disabling Hardware-accelerated GPU scheduling or " +
                        "updating/rolling back the graphics driver.");
                }

                return screenshot;
            }
            catch
            {
                screenshot.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Detects effectively empty frames returned by visible screen capture on affected
        /// configurations. A grid is used instead of a single pixel so dark UI borders do not
        /// hide an empty frame, while a frame with only a few non-black artifacts still counts as empty.
        /// </summary>
        private static bool IsBitmapEffectivelyBlack(Bitmap bitmap)
        {
            if (bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0)
            {
                return true;
            }

            const int columns = 9;
            const int rows = 7;
            const int blackChannelThreshold = 12;
            const double requiredBlackRatio = 0.95;

            int marginX = bitmap.Width / 10;
            int marginY = bitmap.Height / 10;
            int sampleWidth = Math.Max(1, bitmap.Width - (marginX * 2));
            int sampleHeight = Math.Max(1, bitmap.Height - (marginY * 2));
            int blackSamples = 0;
            int totalSamples = 0;

            for (int row = 0; row < rows; row++)
            {
                int y = marginY + (sampleHeight - 1) * row / (rows - 1);
                y = Math.Min(y, bitmap.Height - 1);

                for (int column = 0; column < columns; column++)
                {
                    int x = marginX + (sampleWidth - 1) * column / (columns - 1);
                    x = Math.Min(x, bitmap.Width - 1);

                    Color pixel = bitmap.GetPixel(x, y);
                    if (pixel.R <= blackChannelThreshold &&
                        pixel.G <= blackChannelThreshold &&
                        pixel.B <= blackChannelThreshold)
                    {
                        blackSamples++;
                    }
                    totalSamples++;
                }
            }

            return totalSamples == 0 || (double)blackSamples / totalSamples >= requiredBlackRatio;
        }

        /// <summary>
        /// Gets the game window handle.
        /// </summary>
        /// <returns>Window handle or IntPtr.Zero if not found</returns>
        public static IntPtr GetGameWindowHandle()
        {
            return GameProfile.FindWindow();
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

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsIconic(IntPtr hWnd);

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
