using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Tesseract;

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
            //need to fix it so when the window is moved/resized, it gets the updated points (pnt)
            while (true)
            {
                //Console.WriteLine(pnt);
                GetClientRect(handle, out var rect);
                var size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
                var result = new Bitmap(size.Width, size.Height);
                using (var graphics = Graphics.FromImage(result))
                {
                    graphics.CopyFromScreen(pnt, Point.Empty, size);
                }

                result.Save("C:/Users/Mike/Documents/Github/Toontown Rewritten Bot/[Source] Toontown Rewritten Bot/ToonTown Rewritten Bot/Resources/screenCapture.png");

                var image = result;
                //readText(image);
                //var previousImage = pictureBox1.Image;
                //pictureBox1.Image = image;
                //previousImage?.Dispose();

                //testing
                testImageCapture();
            }
        }

        private void testImageCapture()
        {
            //find image a in b
            Image<Bgr, byte> source = new Image<Bgr, byte>("C:/Users/Mike/Documents/Github/Toontown Rewritten Bot/[Source] Toontown Rewritten Bot/ToonTown Rewritten Bot/Resources/screenCapture.png"); // Image B
            Image<Bgr, byte> template = new Image<Bgr, byte>("C:/Users/Mike/Documents/Github/Toontown Rewritten Bot/[Source] Toontown Rewritten Bot/ToonTown Rewritten Bot/Resources/friendsButton.png"); // Image A
            Image<Bgr, byte> imageToShow = source.Copy();

            using (Image<Gray, float> result = source.MatchTemplate(template, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
            {
                double[] minValues, maxValues;
                Point[] minLocations, maxLocations;
                result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.
                if (maxValues[0] > 0.6)
                {
                    // This is a match. Do something with it, for example draw a rectangle around it.
                    Rectangle match = new Rectangle(maxLocations[0], template.Size);
                    imageToShow.Draw(match, new Bgr(Color.Red), 3);
                }
            }

            // Show imageToShow in an ImageBox (here assumed to be called imageBox1)
            imageBox1.Image = imageToShow;
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

        //testing Tesseract stuff here

        public const string TESS_PATH = "tessdata/";
        public const string TESS_LANGUAGE = "eng";
        private static TesseractEngine engine;

        public static void readText(Image inputImage)
        {
            using (engine = new TesseractEngine(TESS_PATH, TESS_LANGUAGE))
            {
                // have to load Pix via a bitmap since Pix doesn't support loading a stream.
                using (var image = new Bitmap(inputImage))
                {
                    using (var pix = PixConverter.ToPix(image))
                    {
                        using (var page = engine.Process(pix))
                        {
                            Console.WriteLine(page.GetMeanConfidence() + " : " + page.GetText());
                        }
                    }
                }
            }
        }
    }
}
