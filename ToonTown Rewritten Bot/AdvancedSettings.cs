using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    public partial class AdvancedSettings : Form
    {
        public AdvancedSettings()
        {
            InitializeComponent();
        }

        static float defaultConfidence = 0.5f;
        private static bool debugView = false;
        public string x, y, message = "";
        public void callImageRecScript(string settingNameForImage)//dont multi-thread so that the py script doesn't keep running if the debug view window isnt closed
        {
            string imageName = Path.GetFileName(Properties.Settings.Default[settingNameForImage].ToString());
            Process p = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/r echo %cd%";
            if (checkBox1.Checked)//advanced settings enabled
                startInfo.Arguments = "/r cd Search_Images&python ./imageRecognitionTesting.py " + imageFilesComboBox.SelectedItem.ToString() + " " + (trackBar1.Value / 10.0f) + " " + debugView;
            else
                startInfo.Arguments = "/r cd Search_Images&python ./imageRecognitionTesting.py " + imageName + " " + defaultConfidence + " " + debugView;
            p.StartInfo = startInfo;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.Start();
            while (!p.StandardOutput.EndOfStream)
            {
                string line = p.StandardOutput.ReadLine();
                Debug.WriteLine(line);
                if (line[0] == 'x')
                    x = line.Substring(line.IndexOf(' ') + 1);
                else if (line[0] == 'y')
                    y = line.Substring(line.IndexOf(' ') + 1);
                else//unable to find the image
                    message = line;
            }
            p.WaitForExit();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)//use advanced settings checkbox
        {
            if (checkBox1.Checked)
            {
                groupBox1.Enabled = true;
                label1.Visible = true;
            }
            else
            {
                groupBox1.Enabled = false;
                label1.Visible = false;
                debugViewChkBox.Checked = false;
            }
        }

        private void debugViewChkBox_CheckedChanged(object sender, EventArgs e)//enable debug view checkbox
        {
            if (debugViewChkBox.Checked)
                debugView = true;
            else
                debugView = false;
        }

        string scriptPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @".\Search_Images"));
        private void loadImages()//read the images from the directory. These are the images it is searching for on the screen
        {
            int numOfFiles = Directory.GetFiles(scriptPath, "*.png", SearchOption.AllDirectories).Length;
            string[] fileEntries = Directory.GetFiles(scriptPath);

            for (int i = 0; i < numOfFiles; i++)
            {
                if (!fileEntries[i].Substring(fileEntries[i].LastIndexOf(@"\", fileEntries[i].Length) + 1).Contains(" "))//dont add files that contain a space
                    imageFilesComboBox.Items.Add(fileEntries[i].Substring(fileEntries[i].LastIndexOf(@"\", fileEntries[i].Length) + 1));
            }
        }
    }
}
