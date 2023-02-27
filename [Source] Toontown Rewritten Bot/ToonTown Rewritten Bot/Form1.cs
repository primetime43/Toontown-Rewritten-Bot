using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    public partial class Form1 : Form
    {
        public bool fishVariance = false;

        public Form1()
        {
            //isTTRRunning();
            InitializeComponent();
            BotFunctions.readTextFile();
            createDataFileMap();
            loadCoordsIntoResetBox();
        }

        //important functions for bot
        private void startSpamButton_Click(object sender, EventArgs e)//spam message on screen
        {//if the user presses ALT key, it will break the loop
            bool loopBroken = ToonTown_Rewritten_Bot.Misc.sendMessage(messageToType.Text, Convert.ToInt32(numericUpDown2.Value), checkBox1.Checked, numericUpDown2);
        }

        private int timeLeft;
        private void keepToonAwakeButton_Click(object sender, EventArgs e)//keep toon 
        {
            timeLeft = Convert.ToInt32(numericUpDown1.Value) * 60;
            MessageBox.Show("Press OK when ready to begin!");
            Thread.Sleep(2000);
            timer1.Start();
            bool loopBroken = ToonTown_Rewritten_Bot.Misc.keepToonAwake(Convert.ToInt32(numericUpDown1.Value));
            if (loopBroken)
            {
                timer1.Stop();
                label1.Visible = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)//open the flower manager
        {
            Plants plantsForm = new Plants();
            try
            {
                string selected = (string)comboBox2.SelectedItem;
                plantsForm.loadFlowers(selected);
                plantsForm.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Unable to perform this action", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            plantsForm.comboBox1.Items.Clear();
        }

        //misc functions for bot
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                numericUpDown2.Visible = true;
            else
                numericUpDown2.Visible = false;
        }

        private void checkBox2_CheckedChanged_1(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
                TopMost = true;
            else
                TopMost = false;
        }
        private void isTTRRunning()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("Toontown Rewritten [BETA]");
            DialogResult confirmation;
            while (!(Process.GetProcessesByName("TTREngine").Length > 0))
            {
                confirmation = MessageBox.Show("Press OK once running or Cancel.","ToonTown Rewritten is not running!", MessageBoxButtons.OKCancel);
                if (confirmation.Equals(DialogResult.Cancel))
                    Environment.Exit(0);
            }
            BotFunctions.maximizeTTRWindow();
        }

        public static Dictionary<string, string> dataFileMap = new Dictionary<string, string>();
        private void createDataFileMap()
        {
            //Gardening Coords
            dataFileMap.Add("1", "Plant Flower/Remove Button");
            dataFileMap.Add("2", "Red Jellybean Button");
            dataFileMap.Add("3", "Green Jellybean Button");
            dataFileMap.Add("4", "Orange Jellybean Button");
            dataFileMap.Add("5", "Purple Jellybean Button");
            dataFileMap.Add("6", "Blue Jellybean Button");
            dataFileMap.Add("7", "Pink Jellybean Button");
            dataFileMap.Add("8", "Yellow Jellybean Button");
            dataFileMap.Add("9", "Cyan Jellybean Button");
            dataFileMap.Add("10", "Silver Jellybean Button");
            dataFileMap.Add("11", "Blue Plant Button");
            dataFileMap.Add("12", "Blue Ok Button");
            dataFileMap.Add("13", "Watering Can Button");
            dataFileMap.Add("14", "Blue Yes Button");
            //Fishing Coords
            dataFileMap.Add("15", "Red Fishing Button");
            dataFileMap.Add("16", "Exit Fishing Button");
            dataFileMap.Add("17", "Blue Sell All Button");
            //Racing Coords
            //Doodle Training Coords
            dataFileMap.Add("18", "Feed Doodle Button");
            dataFileMap.Add("19", "Scratch Doodle Button");
            dataFileMap.Add("20", "Green SpeedChat Button");
            dataFileMap.Add("21", "Pets Tab in SpeedChat");
            dataFileMap.Add("22", "Tricks Tab in SpeedChat");
            dataFileMap.Add("23", "Jump Trick Option in SpeedChat");
            dataFileMap.Add("24", "Beg Trick Option in SpeedChat");
            dataFileMap.Add("25", "Play Dead Trick Option in SpeedChat");
            dataFileMap.Add("26", "Rollover Trick Option in SpeedChat");
            dataFileMap.Add("27", "Backflip Trick Option in SpeedChat");
            dataFileMap.Add("28", "Dance Trick Option in SpeedChat");
            dataFileMap.Add("29", "Speak Trick Option in SpeedChat");

        }

        /*private void button4_Click(object sender, EventArgs e)
        {
            //Thread.Sleep(4000);
            //textBox1.Text = BotFunctions.HexConverter(BotFunctions.GetColorAt(BotFunctions.getCursorLocation().X, BotFunctions.getCursorLocation().Y));
        }*/

        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Visible = true;
            if (timeLeft > 0)
            {
                timeLeft = timeLeft - 1;
                label1.Text = timeLeft + " seconds";
            }
            else
            {
                timer1.Stop();
                label1.Visible = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ToonTown_Rewritten_Bot.Gardening.waterPlant();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ToonTown_Rewritten_Bot.Gardening.removePlant();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            BotFunctions.resetAllCoordinates();
            MessageBox.Show("All coordinates reset!");
        }

        private void loadCoordsIntoResetBox()
        {
            comboBox1.Items.Clear();
            words = new String[dataFileMap.Count];
            for (int i = 0; i < dataFileMap.Count; i++)
            {
                words[i] = dataFileMap[Convert.ToString(i + 1)];
            }
            comboBox1.Items.AddRange(words);
        }

        private static String[] words;
        private void button6_Click(object sender, EventArgs e)
        {
            string selected = (string)comboBox1.SelectedItem;
            try
            {
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].Equals(selected))
                        BotFunctions.updateCoordinates(Convert.ToString(i + 1));
                }
            }
            catch
            {
                MessageBox.Show("Unable to perform this action", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            MessageBox.Show("Coordinate's updated for " + selected);
        }

        private void startFishing_Click(object sender, EventArgs e)//button to start fishing
        {
            string selected = (string)comboBox3.SelectedItem;
            int numberOfCasts = Convert.ToInt32(numericUpDown3.Value);
            int numberOfSells = Convert.ToInt32(numericUpDown4.Value);
            BotFunctions.tellFishingLocation(selected);
            MessageBox.Show("Make sure you're in the fishing dock before pressing OK!");
            startFishingThread(selected, numberOfCasts, numberOfSells, false);
        }

        private void randomFishing_CheckedChanged(object sender, EventArgs e)
        {
            if (randomFishing.Checked)
            {
                MessageBox.Show("This will add randomness to the line casting!");
                fishVariance = true;
            }
            else
            {
                fishVariance = false;
            }
        }

        Thread fishingThreading;
        public void startFishingThread(string selected, int numberOfCasts, int numberOfSells, bool stopCheck)
        {
            if (!stopCheck)
            {
                fishingThreading = new Thread(() => ToonTown_Rewritten_Bot.Fishing.startFishing(selected, numberOfCasts, numberOfSells, fishVariance));
                fishingThreading.Start();
            }
            else if (stopCheck)
                fishingThreading.Abort();
        }

        private void button4_Click(object sender, EventArgs e)//button to stop fishing
        {
            startFishingThread(null, 0, 0, true);
            MessageBox.Show("Fishing stopped!");
        }

        private void smartFishing_CheckedChanged(object sender, EventArgs e)
        {
            if (smartFishing.Checked)
            {
                MessageBox.Show("Will be added later!");
                smartFishing.Checked = false;
            }
        }

        private void button5_Click(object sender, EventArgs e)//racing test
        {
            MessageBox.Show("Press OK when ready to begin!");
            Thread.Sleep(5000);
            //Point test = BotFunctions.getCursorLocation();
            //BotFunctions.GetColorAt(test.X, test.Y);
            //Console.WriteLine("HEX: " + BotFunctions.HexConverter(BotFunctions.GetColorAt(test.X, test.Y)) + " RGB: " + BotFunctions.GetColorAt(test.X, test.Y));
            //MessageBox.Show("Done"); 
            /*BotFunctions.DoMouseClick();
            ToonTown_Rewritten_Bot.Racing.startRacing();

            Rectangle bounds = Screen.GetWorkingArea(Point.Empty);
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }
                int x = 950;
                int y = 755;
                while (y <= 780 && x <= 970)
                {
                    richTextBox1.AppendText(BotFunctions.HexConverter(bitmap.GetPixel(x, y)) + "\n");
                    x++;
                    y++;
                }
            }*/
        }

        private void button8_Click(object sender, EventArgs e)
        {
            AboutBox1 aboutBox = new AboutBox1();
            try
            {
                aboutBox.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Unable to perform this action", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Help helpBox = new Help();
            try
            {
                helpBox.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Unable to perform this action", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        // GOLF- Afternoon Tee
        private void golfAfternoonTee(object sender, EventArgs e)
        {
            ToonTown_Rewritten_Bot.Golf.afternoonTee();
        }

        // GOLF - Holey Mackeral
        private void golfHoleyMackeral(object sender, EventArgs e)
        {
            ToonTown_Rewritten_Bot.Golf.holeyMackeral();
        }

        // GOLF - Hole on the Range
        private void golfHoleOnTheRange(object sender, EventArgs e)
        {
            ToonTown_Rewritten_Bot.Golf.holeOnTheRange();
        }

        // GOLF - Seeing green
        private void golfSeeingGreen(object sender, EventArgs e)
        {
            ToonTown_Rewritten_Bot.Golf.seeingGreen();
        }

        // GOLF - Swing Time
        private void button15_Click(object sender, EventArgs e)
        {
            ToonTown_Rewritten_Bot.Golf.swingTime();
        }

        // GOLF - Down the Hatch
        private void button14_Click(object sender, EventArgs e)
        {
            ToonTown_Rewritten_Bot.Golf.downTheHatch();
        }

        //GOLF - Peanut Putter
        private void button13_Click(object sender, EventArgs e)
        {
            ToonTown_Rewritten_Bot.Golf.peanutPutter();
        }

        //GOLF - Hot Links
        private void button16_Click(object sender, EventArgs e)
        {
            ToonTown_Rewritten_Bot.Golf.hotLinks();
        }

        //GOLF - Hole In Fun
        private void button17_Click(object sender, EventArgs e)
        {
            ToonTown_Rewritten_Bot.Golf.holeInFun();
        }

        //GOLF - Swing-A-Long
        private void button18_Click(object sender, EventArgs e)
        {
            ToonTown_Rewritten_Bot.Golf.swingALong();
        }

        //GOLF - One Little Birdie
        private void One_Little_Birdie_Click(object sender, EventArgs e)
        {
            ToonTown_Rewritten_Bot.Golf.oneLittleBirdie();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox3.Checked)
            {
                numericUpDown5.Enabled = false;
                numericUpDown6.Enabled = false;
            }
            else
            {
                numericUpDown5.Enabled = true;
                numericUpDown6.Enabled = true;
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            string selected = (string)comboBox4.SelectedItem;
            startDoodleTrainingThread(Convert.ToInt32(numericUpDown6.Value), Convert.ToInt32(numericUpDown5.Value), checkBox3.Checked, false, selected);
        }

        Thread doodleTrainingThreading;
        public void startDoodleTrainingThread(int numberOfFeeds, int numberOfScratches, bool checkBoxChecked, bool stopTrainingClicked, string selectedTrick)
        {
            if (!stopTrainingClicked)
            {
                doodleTrainingThreading = new Thread(() => DoodleTraining.startTrainingDoodle(numberOfFeeds, numberOfScratches, checkBox3.Checked, selectedTrick, checkBox4.Checked, checkBox5.Checked));
                doodleTrainingThreading.Start();
            }
            else if (stopTrainingClicked)
                doodleTrainingThreading.Abort();
        }

        private void button19_Click(object sender, EventArgs e)
        {
            startDoodleTrainingThread(0, 0, false, true, "");
            MessageBox.Show("Doodle Training stopped!");
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            {
                numericUpDown5.Enabled = false;
                checkBox5.Checked = false;
            }
            else
            {
                numericUpDown5.Enabled = true;
                if(checkBox3.Checked)
                {
                    numericUpDown6.Enabled = false;
                    numericUpDown5.Enabled = false;
                }
            }
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox5.Checked)
            {
                numericUpDown6.Enabled = false;
                checkBox4.Checked = false;
            }
            else
            {
                numericUpDown6.Enabled = true;
                if (checkBox3.Checked)
                {
                    numericUpDown6.Enabled = false;
                    numericUpDown5.Enabled = false;
                }
            }
        }
    }
}
