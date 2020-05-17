using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    public partial class DevForm : Form
    {
        public DevForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            IntPtr hWnd = FindWindow(null, "Toontown Rewritten");
            Thread t = new Thread(() => CaptureWindow(hWnd));
            t.Start();
        }

        //Capturing the screen without boarders using GetClientRect
        public void CaptureWindow(IntPtr handle)
        {
            //GetClientRect(handle, out var rect);
            //var size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
            //var result = new Bitmap(size.Width, size.Height);
            ClientToScreen(handle, out var pnt);//must stay out of the loop

            while (true)
            {
                GetClientRect(handle, out var rect);
                var size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
                var result = new Bitmap(size.Width, size.Height);
                using (var graphics = Graphics.FromImage(result))
                {
                    graphics.CopyFromScreen(pnt, Point.Empty, size);
                }

                var image = result;
                var previousImage = pictureBox1.Image;
                pictureBox1.Image = image;
                previousImage?.Dispose();
            }
        }

        //Capturing the screen with boarders using GetWindowRect
        //ref: https://stackoverflow.com/questions/1163761/capture-screenshot-of-active-window/24879511#24879511
        /*public void CaptureWindow(IntPtr handle)
        {
            while (true)
            {
                var rect = new Rect();
                GetWindowRect(handle, ref rect);
                var bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                var result = new Bitmap(bounds.Width, bounds.Height);

                using (var graphics = Graphics.FromImage(result))
                {
                    graphics.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
                }

                //return result;
                var image = result;
                var previousImage = pictureBox1.Image;
                pictureBox1.Image = image;
                previousImage?.Dispose();
            }
        }*/

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public static implicit operator Point(POINT p)
            {
                return new Point(p.X, p.Y);
            }

            public static implicit operator POINT(Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, out Point lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    }
}
