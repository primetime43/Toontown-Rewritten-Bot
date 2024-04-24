using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ToonTown_Rewritten_Bot.Utilities
{
    public class ScreenCapture
    {
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static Bitmap CaptureGameWindow(IntPtr gameWindowHandle)
        {
            // Get the dimensions of the game window
            if (GetWindowRect(gameWindowHandle, out RECT rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                // Create a new bitmap with the size of the window
                Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                // Create a graphics object from the bitmap
                using (Graphics gfxBmp = Graphics.FromImage(bmp))
                {
                    // Use CopyFromScreen to capture the specific window
                    gfxBmp.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                }

                return bmp;
            }
            else
            {
                throw new InvalidOperationException("Unable to capture the game window.");
            }
        }
    }
}