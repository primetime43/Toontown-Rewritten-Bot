using Emgu.CV.CvEnum;
using Emgu.CV;
using Emgu.CV.Stitching;
using Emgu.CV.Structure;
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

namespace ToonTown_Rewritten_Bot
{
    class ImageRecognition
    {
        public static Image GetWindowScreenshot()
        {
            string windowName = "Toontown Rewritten";
            // Find the window by name
            IntPtr windowHandle = NativeMethods.FindWindow(null, windowName);
            if (windowHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Window not found.");
            }

            // Get the window's position and size
            NativeMethods.Rect windowRect = new NativeMethods.Rect();
            NativeMethods.GetWindowRect(windowHandle, ref windowRect);

            // Take a screenshot of the window
            Bitmap screenshot = new Bitmap(windowRect.Width, windowRect.Height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(windowRect.Left, windowRect.Top, 0, 0, screenshot.Size);
            }

            return screenshot;
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
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetWindowRect(IntPtr hWnd, ref Rect lpRect);
        }
        #endregion
    }
}
