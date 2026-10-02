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
        public WindowCaptureException(string message) : base(message)
        {
        }
    }

    class ImageRecognition
    {
        private static volatile bool _printWindowUnavailable;
        private static int _screenFallbackWarningLogged;

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
            Bitmap frame = captureBackground && GameProfile.IsClash
                ? GameGraphicsCapture.Capture(windowHandle, geometry)
                : captureBackground && !_printWindowUnavailable
                ? CaptureWindowWithPrintWindow(windowHandle, geometry)
                : CaptureVisibleWindow(windowHandle, geometry.ClientBounds);
            if (!GameWindowGeometry.TryRead(windowHandle, out var current) || current != geometry)
            {
                frame.Dispose();
                throw new WindowCaptureException("The game window moved or resized during capture. Keep it stationary while the bot runs.");
            }
            return frame;
        }

        /// <summary>
        /// Captures a window using PrintWindow API, which works even when the window is obscured.
        /// Retries with the legacy flag and falls back to visible screen capture when Windows
        /// reports success but returns a black or uniform blank frame.
        /// </summary>
        private static Bitmap CaptureWindowWithPrintWindow(nint windowHandle, GameWindowGeometry geometry)
        {
            Bitmap bitmap = TryCaptureWithPrintWindow(
                windowHandle,
                geometry,
                NativeMethods.PW_RENDERFULLCONTENT);
            if (bitmap != null)
            {
                return bitmap;
            }

            // Some drivers support PrintWindow but not PW_RENDERFULLCONTENT.
            bitmap = TryCaptureWithPrintWindow(windowHandle, geometry, 0);
            if (bitmap != null)
            {
                return bitmap;
            }

            _printWindowUnavailable = true;
            if (Interlocked.Exchange(ref _screenFallbackWarningLogged, 1) == 0)
            {
                Logger.Warning(
                    "Capture",
                    "PrintWindow returned unusable blank frames. Using visible screen capture for this session; " +
                    "keep the Toontown window visible and unobscured.");
            }

            if (!GameWindowGeometry.TryRead(windowHandle, out geometry))
            {
                throw new WindowCaptureException("Could not read the Toontown window bounds for screen capture.");
            }

            return CaptureVisibleWindow(windowHandle, geometry.ClientBounds);
        }

        private static Bitmap TryCaptureWithPrintWindow(
            nint windowHandle,
            GameWindowGeometry geometry,
            uint flags)
        {
            using Bitmap fullWindow = new Bitmap(geometry.WindowBounds.Width, geometry.WindowBounds.Height, PixelFormat.Format32bppArgb);
            bool success;

            using (Graphics graphics = Graphics.FromImage(fullWindow))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    success = NativeMethods.PrintWindow(windowHandle, hdc, flags);
                }
                finally
                {
                    // Release exactly once. The previous fallback path released here and inside the
                    // failure branch, leaving Graphics in an invalid state on some machines.
                    graphics.ReleaseHdc(hdc);
                }
            }

            if (!success) return null;
            if (!new Rectangle(Point.Empty, fullWindow.Size).Contains(geometry.ClientCrop)) return null;
            // Preserve the existing GPU capture flags, then remove window decorations before
            // validating the pixels. A painted title bar must not hide a blank game frame.
            Bitmap bitmap = fullWindow.Clone(geometry.ClientCrop, PixelFormat.Format32bppArgb);
            string unusableReason = GetPrintWindowFrameFailureReason(bitmap);
            if (success && unusableReason == null)
            {
                return bitmap;
            }

            Debug.WriteLine(success
                ? $"PrintWindow returned an unusable {unusableReason} (flags=0x{flags:X})"
                : $"PrintWindow failed (flags=0x{flags:X})");
            bitmap.Dispose();
            return null;
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
                    "Toontown is minimized and this graphics driver does not support background capture. " +
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
                        "Both background and visible Toontown capture returned a black frame. Keep the game " +
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
        /// Detects the effectively empty frames returned by PrintWindow on affected Intel/Windows
        /// configurations. A grid is used instead of a single pixel so dark UI borders do not
        /// trigger the fallback, while a frame with only a few non-black artifacts still does.
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
        /// Identifies frames where PrintWindow rendered the non-client title bar but filled the
        /// game client area with one flat gray/white color. The interior-only grid deliberately
        /// excludes the title bar and borders that can otherwise make a blank frame look valid.
        /// </summary>
        private static string GetPrintWindowFrameFailureReason(Bitmap bitmap)
        {
            if (IsBitmapEffectivelyBlack(bitmap))
            {
                return "black frame";
            }

            const int columns = 9;
            const int rows = 7;
            const int uniformChannelRange = 8;

            int marginX = bitmap.Width / 10;
            int marginY = bitmap.Height / 10;
            int sampleWidth = Math.Max(1, bitmap.Width - (marginX * 2));
            int sampleHeight = Math.Max(1, bitmap.Height - (marginY * 2));
            int minR = 255;
            int minG = 255;
            int minB = 255;
            int maxR = 0;
            int maxG = 0;
            int maxB = 0;

            for (int row = 0; row < rows; row++)
            {
                int y = marginY + (sampleHeight - 1) * row / (rows - 1);
                y = Math.Min(y, bitmap.Height - 1);

                for (int column = 0; column < columns; column++)
                {
                    int x = marginX + (sampleWidth - 1) * column / (columns - 1);
                    x = Math.Min(x, bitmap.Width - 1);

                    Color pixel = bitmap.GetPixel(x, y);
                    minR = Math.Min(minR, pixel.R);
                    minG = Math.Min(minG, pixel.G);
                    minB = Math.Min(minB, pixel.B);
                    maxR = Math.Max(maxR, pixel.R);
                    maxG = Math.Max(maxG, pixel.G);
                    maxB = Math.Max(maxB, pixel.B);
                }
            }

            bool isUniform = maxR - minR <= uniformChannelRange &&
                             maxG - minG <= uniformChannelRange &&
                             maxB - minB <= uniformChannelRange;
            return isUniform ? "uniform/blank frame" : null;
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

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsIconic(IntPtr hWnd);

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
