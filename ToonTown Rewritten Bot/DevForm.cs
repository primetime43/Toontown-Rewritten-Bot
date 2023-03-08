//using Emgu.CV;
//using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
//using Tesseract;
using System.Diagnostics;
using System.IO;

namespace ToonTown_Rewritten_Bot
{
    public partial class DevForm : Form
    {
        string scriptPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @".\Search_Images"));
        public DevForm()
        {
            InitializeComponent();

            //string resourcesPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @".\Search Images"));
            //Console.WriteLine("The current directory is {0}", scriptPath);
            //Console.WriteLine("Num of files: " + Directory.GetFiles(newPath,"*.png", SearchOption.AllDirectories).Length);
            int numOfFiles = Directory.GetFiles(scriptPath, "*.png", SearchOption.AllDirectories).Length;
            string[] fileEntries = Directory.GetFiles(scriptPath);

            for (int i = 0; i < numOfFiles; i++)
            {
                if(!fileEntries[i].Substring(fileEntries[i].LastIndexOf(@"\", fileEntries[i].Length) + 1).Contains(" "))//dont add files that contain a space
                    comboBox1.Items.Add(fileEntries[i].Substring(fileEntries[i].LastIndexOf(@"\", fileEntries[i].Length)+1));
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //var test = Properties.Resources.imageRecognitionTesting;
            cmdexe();
        }

        static float defaultConfidence = 0.5f;

        //Arguments = "/K cd Resources&python ./imageRecognitionTesting.py"
        public void cmdexe()//dont multi-thread so that the py script doesn't keep running if the debug view window isnt closed
        {
            Process p = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            //startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/r echo %cd%";
            if(checkBox1.Checked)
                startInfo.Arguments = "/r cd Search_Images&python ./imageRecognitionTesting.py " + comboBox1.SelectedItem.ToString() + " " + (trackBar1.Value / 10.0f) + " " + debugView;
            else
                startInfo.Arguments = "/r cd Search_Images&python ./imageRecognitionTesting.py " + comboBox1.SelectedItem.ToString() + " " + defaultConfidence + " " + debugView;
            p.StartInfo = startInfo;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.Start();
            while (!p.StandardOutput.EndOfStream)
            {
                string line = p.StandardOutput.ReadLine();
                Console.WriteLine(line);
            }
            p.WaitForExit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            IntPtr hWnd = FindWindow(null, "Toontown Rewritten");
            if (hWnd != (IntPtr)0x00)
            {
                Thread t = new Thread(() => CaptureWindow(hWnd));
                t.Start();
            }
            else
                Console.WriteLine("Toontown Rewritten not running");
        }

        //prob wont use in release version
        //Capturing the screen without boarders using GetClientRect
        public void CaptureWindow(IntPtr handle)
        {
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

                //result.Save("C:/Users/Mike/Documents/Github/Toontown Rewritten Bot/[Source] Toontown Rewritten Bot/ToonTown Rewritten Bot/Resources/screenCapture.bmp");
                //result.ToImage<Bgr, byte>();

                var image = result;
                //readText(image);
                //var previousImage = pictureBox1.Image;
                //pictureBox1.Image = image;
                //previousImage?.Dispose();

                //testing
                //testImageCapture(result);
            }
        }

        //prob just use to show box where the coords are for debugging purposes in release version
        /*Image<Bgr, byte> template = new Image<Bgr, byte>(Properties.Resources.stickerBook.ToImage<Bgr, byte>().Data); // Image A
        private void testImageCapture(Bitmap inputImage)
        {
            //find image a in b
            Image<Bgr, byte> source = new Image<Bgr, byte>(inputImage.ToImage<Bgr, byte>().Data);
            //Image<Bgr, byte> source = new Image<Bgr, byte>("C:/Users/Mike/Documents/Github/Toontown Rewritten Bot/[Source] Toontown Rewritten Bot/ToonTown Rewritten Bot/Resources/screenCapture.bmp"); // Image B
            //Image<Bgr, byte> template = new Image<Bgr, byte>("C:/Users/Mike/Documents/Github/Toontown Rewritten Bot/[Source] Toontown Rewritten Bot/ToonTown Rewritten Bot/Resources/toonLaughBottomLeft.png"); // Image A
            Image<Bgr, byte> imageToShow = source.Copy();


            //CcoeffNormed is more accurate but needs to solve the rescale issue
            //Ccoeff less accurate but doesnt have scale issues, can find the image max window or other sized
            using (Image<Gray, float> result = source.MatchTemplate(template, Emgu.CV.CvEnum.TemplateMatchingType.Ccoeff))
            {
                double[] minValues, maxValues;
                Point[] minLocations, maxLocations;
                result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                if (maxValues[0] > 0.6)
                {
                    // This is a match. Do something with it, for example draw a rectangle around it.
                    Rectangle match = new Rectangle(maxLocations[0], template.Size);
                    imageToShow.Draw(match, new Bgr(Color.Red), 3);
                }
            }

            imageBox1.Image = imageToShow;
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
        //private static TesseractEngine engine;

        /*public static void readText(Image inputImage)
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
        }*/

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //Console.WriteLine("Trackbar val: " + (trackBar1.Value/10.0f));
            label1.Text = (trackBar1.Value / 10.0f).ToString();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                groupBox2.Enabled = true;
                label1.Visible = true;
            }
            else
            {
                groupBox2.Enabled = false;
                label1.Visible = false;
                checkBox2.Checked = false;
            }
        }

        private static bool debugView = false;
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
                debugView = true;
            else
                debugView = false;
        }
    }
}
