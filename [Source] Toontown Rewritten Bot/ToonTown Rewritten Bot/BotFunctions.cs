using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    class BotFunctions
    {
        public static void DoMouseClick()
        {
            DoMouseClick(getCursorLocation());
        }

        public static void DoFishingClick()
        {
            //click red button
            DoMouseClickDown(getCursorLocation());
            Thread.Sleep(500);//sleep 2 sec

            //get the coords of the red button and move cursor from there, downward
            int[] coordinates = getCoordinates("15");
            int x = coordinates[0];
            int y = coordinates[1];
            MoveCursor(x, (y+150));//pull it back
            Thread.Sleep(500);
            DoMouseClickUp(getCursorLocation());
        }
        private static void DoMouseClick(Point location)//simulate left button mouse click
        {
            //Call the imported function with the cursor's current position
            uint X = Convert.ToUInt32(location.X);
            uint Y = Convert.ToUInt32(location.Y);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        private static void DoMouseClickDown(Point location)
        {
            uint X = Convert.ToUInt32(location.X);
            uint Y = Convert.ToUInt32(location.Y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, X, Y, 0, 0);
        }

        private static void DoMouseClickUp(Point location)
        {
            uint X = Convert.ToUInt32(location.X);
            uint Y = Convert.ToUInt32(location.Y);
            mouse_event(MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        public static Color GetColorAt(int x, int y)
        {
            IntPtr desk = GetDesktopWindow();
            IntPtr dc = GetWindowDC(desk);
            int a = (int)GetPixel(dc, x, y);
            ReleaseDC(desk, dc);
            return Color.FromArgb(255, (a >> 0) & 0xff, (a >> 8) & 0xff, (a >> 16) & 0xff);
        }

        public static Point getCursorLocation()
        {
            Point cursorLocation = new Point();
            GetCursorPos(ref cursorLocation);
            return cursorLocation;
        }

        public static String HexConverter(System.Drawing.Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        public static void MoveCursor(int x, int y)
        {
            Cursor.Position = new Point(x, y);
        }

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // Maximizes and Focuces TTR
        public static void maximizeAndFocus()
        {
            IntPtr hwnd = FindWindowByCaption(IntPtr.Zero, "Toontown Rewritten [BETA]");
            ShowWindow(hwnd, 6);//6 min
            ShowWindow(hwnd, 3);//3 max
        }

        public static void maximizeTTRWindow()
        {
            IntPtr hwnd = FindWindowByCaption(IntPtr.Zero, "Toontown Rewritten [BETA]");
            ShowWindow(hwnd, 3);
        }

        private static string[] lines;
        public static void readTextFile()
        {
            try
            {
                lines = File.ReadAllLines(Path.GetFullPath("Coordinates Data File.txt"));
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
        }

        private static void writeToTextFile(String message)//probably isn't needed, maybe remove later. write to file without overwriting
        {
            lines = File.ReadAllLines(Path.GetFullPath("Coordinates Data File.txt"));
            try
            {
                using (StreamWriter writer = new StreamWriter(Path.GetFullPath("Coordinates Data File.txt")))
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        writer.WriteLine(lines[i]);
                    }
                    writer.WriteAsync(message);
                    writer.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be written to:");
                Console.WriteLine(e.Message);
            }
        }

        private static void writeDefaultCords(String[] line)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(Path.GetFullPath("Coordinates Data File.txt")))
                {
                    for (int i = 0; i < line.Length; i++)
                    {
                        writer.WriteLine(line[i]);
                    }
                    writer.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be written to:");
                Console.WriteLine(e.Message);
            }
        }


        private static void updateTextFile()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(Path.GetFullPath("Coordinates Data File.txt")))
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        writer.WriteLine(lines[i]);
                    }
                    writer.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be written to:");
                Console.WriteLine(e.Message);
            }
        }

        public static void updateCoordinates(String locationToUpdate)
        {
            UpdateCoordsHelper updateCoordsWindow = new UpdateCoordsHelper();
            try
            {
                updateCoordsWindow.startCountDown(Form1.dataFileMap[locationToUpdate]);
                updateCoordsWindow.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Unable to perform this action", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }

            Point coords = getCursorLocation();
            string x = Convert.ToString(coords.X);
            string y = Convert.ToString(coords.Y);
            lines = File.ReadAllLines(Path.GetFullPath("Coordinates Data File.txt"));
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("."))
                {
                    if (locationToUpdate.Equals(lines[i].Substring(0, lines[i].IndexOf('.'))))//look for the number it cooresponds to
                    {
                        lines[i] = locationToUpdate + "." + "(" + x + "," + y + ")";
                        updateTextFile();//changes the coordinate values in the data file
                    }
                }
            }
        }

        public static int[] getCoordinates(String coordsToRetrieve)
        {
            lines = File.ReadAllLines(Path.GetFullPath("Coordinates Data File.txt"));
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("."))
                {
                    if (coordsToRetrieve.Equals(lines[i].Substring(0, lines[i].IndexOf('.'))))//look for the number it cooresponds to
                    {
                        String check = lines[i];
                        char[] removeChars = {'(', ')' };
                        String coords = check.Substring(check.IndexOf('(') + 1);
                        coords = coords.Trim(removeChars);
                        String[] points = coords.Split(',');
                        int x = Convert.ToInt32(points[0]);
                        int y = Convert.ToInt32(points[1]);
                        int[] locations = {x,y};
                        return locations;
                    }
                }
            }
            return null;
        }

        public static bool checkCoordinates(String checkCoords)
        {
            lines = File.ReadAllLines(Path.GetFullPath("Coordinates Data File.txt"));
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("."))
                {
                    if (checkCoords.Equals(lines[i].Substring(0, lines[i].IndexOf('.'))))//look for the number it cooresponds to
                    {
                        String check = lines[i];
                        char[] removeChars = { '(', ')' };
                        String coords = check.Substring(check.IndexOf('(') + 1);
                        coords = coords.Trim(removeChars);
                        if ("0,0".Equals(coords))
                        {
                            return false;//returns false if they equals 0,0
                        }
                    }
                }
            }
            return true;//return true if they're not 0,0
        }

        public static void resetAllCoordinates()
        {
            lines = File.ReadAllLines(Path.GetFullPath("Coordinates Data File.txt"));
            String[] test = new String[lines.Length]; 
            for(int i = 0; i < Form1.dataFileMap.Count; i++)
            {
                test[i] = ((i+1) + ".(0,0)");
            }
            writeDefaultCords(test);
        }

        public static void tellFishingLocation(string location)
        {
            switch (location)
            {
                case "TOONTOWN CENTRAL PUNCHLINE PLACE":
                    MessageBox.Show("Fishes in the first dock when you walk in");
                    break;
                case "DONALD DREAM LAND LULLABY LANE":
                    MessageBox.Show("Fishes in the dock to the left of the small box");
                    break;
                case "BRRRGH POLAR PLACE":
                    MessageBox.Show("Fishes in the top right dock");
                    break;
                case "BRRRGH WALRUS WAY":
                    MessageBox.Show("Fishes in the top left dock");
                    break;
                case "BRRRGH SLEET STREET":
                    MessageBox.Show("Fishes in the first dock when you walk in");
                    break;
                case "MINNIE'S MELODYLAND TENOR TERRACE":
                    MessageBox.Show("Fishes in the top left dock");
                    break;
                case "DONALD DOCK LIGHTHOUSE LANE":
                    MessageBox.Show("Fishes in the 2nd one on the right");
                    break;
                case "DAISY'S GARDEN ELM STREET":
                    MessageBox.Show("Fishes in the bottom left dock when you walk in");
                    break;
                case "FISH ANYWHERE":
                    MessageBox.Show("Fishes for you anywhere, but will only fish, will not sell fish!");
                    break;
            }
        }




        //ignore .dll imports below
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(ref Point lpPoint);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        private static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowDC(IntPtr window);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern uint GetPixel(IntPtr dc, int x, int y);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
    }
}
