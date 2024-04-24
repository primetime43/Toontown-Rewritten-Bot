using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    public partial class UpdateImages : Form
    {
        private static ToolTip tip = new ToolTip();
        private static string currentSettingName = "", currentSettingValue = "";
        private static Dictionary<string, TextBox> dataMap = new Dictionary<string, TextBox>();
        public UpdateImages()
        {
            InitializeComponent();
            createDataMap();
            updateTextBoxes();
        }

        //settingName,txtboxObj
        private void createDataMap()
        {
            //fishing
            dataMap["fishingCastBtn"] = fishingCastImg;//do this instead of add method so it will just overwrite
            dataMap["exitFishingBtn"] = fishingExitImg;
            dataMap["sellFishBtn"] = fishingSellImg;
            //
        }

        private void updateTextBoxes()
        {
            foreach (SettingsProperty currentProperty in Properties.Settings.Default.Properties)
            {
                if (Properties.Settings.Default[currentProperty.Name].ToString() != "")//the value of the setting name doesnt equal null
                {
                    dataMap[currentProperty.Name].Text = Properties.Settings.Default[currentProperty.Name].ToString();
                }
                else
                    Console.WriteLine("Value is null");
            }
        }

        private void fishingCastImg_Click(object sender, EventArgs e)
        {
            currentSettingName = "fishingCastBtn";
            currentSettingValue = Properties.Settings.Default[currentSettingName].ToString();
            if (currentSettingValue == "")//has no path set, so set path
            {
                updateImage();
                fishingCastImg.Text = currentSettingValue;
            }
            else
                pictureBox1.Image = Image.FromFile(currentSettingValue);
        }

        private void fishingExitImg_Click(object sender, EventArgs e)
        {
            currentSettingName = "exitFishingBtn";
            currentSettingValue = Properties.Settings.Default[currentSettingName].ToString();
            if (currentSettingValue == "")//has no path set, so set path
            {
                updateImage();
                fishingExitImg.Text = currentSettingValue;
            }
            else
                pictureBox1.Image = Image.FromFile(currentSettingValue);
        }

        private void fishingSellImg_Click(object sender, EventArgs e)
        {
            currentSettingName = "sellFishBtn";
            currentSettingValue = Properties.Settings.Default[currentSettingName].ToString();
            if (currentSettingValue == "")//has no path set, so set path
            {
                updateImage();
                fishingSellImg.Text = currentSettingValue;
            }
            else
                pictureBox1.Image = Image.FromFile(currentSettingValue);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            updateImage();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                new AdvancedSettings().ShowDialog();
            }
            catch
            {
                MessageBox.Show("Unable to perform this action", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private void updateImage()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                string filePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @".\Search_Images"));
                openFileDialog.InitialDirectory = filePath;
                openFileDialog.Filter = "PNG files (*.png)|*.png";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Properties.Settings.Default[currentSettingName] = openFileDialog.FileName;
                    Properties.Settings.Default.Save();
                    pictureBox1.Image = Image.FromFile(Properties.Settings.Default[currentSettingName].ToString());
                    currentSettingValue = Properties.Settings.Default[currentSettingName].ToString();
                    updateTextBoxes();
                }
            }
        }
    }
}
