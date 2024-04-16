//using Emgu.CV.XPhoto;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Utilities;
using ToonTown_Rewritten_Bot.Views;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm : Form
    {
        private CoordinatesManager _coordinatesManagerService = new CoordinatesManager();
        public MainForm()
        {
            InitializeComponent();

            CoreFunctionality.CreateCustomFishingActionsFolder();

            CoordinatesManager.ReadCoordinates();
            BotFunctions.CreateItemsDataFileMap();
            LoadCoordinatesIntoResetBox();
        }

        //important functions for bot
        private void startSpamButton_Click(object sender, EventArgs e)//spam message on screen
        {//if the user presses ALT key, it will break the loop
            bool loopBroken = BotFunctions.sendMessage(messageToType.Text, Convert.ToInt32(numericUpDown2.Value), checkBox1.Checked, numericUpDown2);
        }

        private int timeLeft;
        private void keepToonAwakeButton_Click(object sender, EventArgs e)//keep toon 
        {
            timeLeft = Convert.ToInt32(numericUpDown1.Value) * 60;
            MessageBox.Show("Press OK when ready to begin!");
            Thread.Sleep(2000);
            timer1.Start();
            bool loopBroken = BotFunctions.keepToonAwake(Convert.ToInt32(numericUpDown1.Value));
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

        /*private void loadActonItemBtn_Click(object sender, EventArgs e)
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

        private Gardening gardeningService = new Gardening();
        private async void button2_Click(object sender, EventArgs e)
        {
            try
            {
                await gardeningService.WaterPlantAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Watering was canceled.");
            }
            catch (Exception ex)
            {
                // General error handling
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            try
            {
                // Assuming RemovePlantAsync is an instance method requiring a CancellationToken.
                await gardeningService.RemovePlantAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Removing plant was canceled.");
            }
            catch (Exception ex)
            {
                // General error handling
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            CoordinatesManager.CreateFreshCoordinatesFile();
            MessageBox.Show("All coordinates reset!");
        }

        private void LoadCoordinatesIntoResetBox()
        {
            comboBox1.Items.Clear();
            var descriptions = CoordinateActions.GetAllDescriptions();
            comboBox1.Items.AddRange(descriptions.Values.ToArray());
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            string selectedDescription = comboBox1.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedDescription))
            {
                MessageBox.Show("Please select a valid item from the list.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string keyToUpdate = CoordinateActions.GetKeyFromDescription(selectedDescription);
            if (keyToUpdate == null)
            {
                MessageBox.Show("No valid key found for the selected description.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                await _coordinatesManagerService.ManualUpdateCoordinates(keyToUpdate);
                MessageBox.Show("Coordinates updated for " + selectedDescription);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to perform this action: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private CancellationTokenSource cancellationTokenSource;
        private FishingService _fishingService = new FishingService();

        /// <summary>
        /// Handles the start fishing button click event. This method initiates fishing
        /// based on the selected location and settings specified in the user interface.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Event data that provides information about the click event.</param>
        /// <remarks>
        /// This method checks the selected fishing location from a comboBox and determines
        /// whether to initiate standard fishing or a custom fishing action based on JSON configurations.
        /// If "CUSTOM FISHING ACTION" is selected, it allows for either debugging the custom actions or
        /// performing them normally based on a checkbox selection. If any other location is selected,
        /// it proceeds with standard fishing operations. Exceptions are handled to address user cancellation
        /// and other errors, providing appropriate feedback.
        /// </remarks>
        private async void startFishing_Click(object sender, EventArgs e)
        {
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token; // Token to handle task cancellation

            try
            {
                string selectedLocation = (string)fishingLocationscomboBox.SelectedItem; // Retrieve the location selected by the user
                int numberOfCasts = Convert.ToInt32(numericUpDown3.Value); // Number of times to cast the line
                int numberOfSells = Convert.ToInt32(numericUpDown4.Value); // Number of times to sell the caught fish

                // Check if the selected location is to perform custom fishing actions
                if (selectedLocation == "CUSTOM FISHING ACTION")
                {
                    MessageBox.Show("Make sure you're in the fishing dock before pressing OK!");
                    string selectedFileName = customFishingFilesComboBox.SelectedItem?.ToString();
                    if (string.IsNullOrEmpty(selectedFileName))
                    {
                        MessageBox.Show("Please select a custom fishing action.");
                        return;
                    }

                    string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string filePath = Path.Combine(exePath, "Custom Fishing Actions", selectedFileName);

                    // Decide whether to debug custom actions or perform them normally
                    if (debugCustomActionsCheckBox.Checked)
                    {
                        await _fishingService.StartCustomFishingDebugging(filePath); // Debugging custom fishing actions
                    }
                    else
                    {
                        await _fishingService.StartFishing(selectedLocation, numberOfCasts, numberOfSells, randomFishingCheckBox.Checked, token, filePath); // Perform custom fishing actions
                    }
                }
                else
                {
                    FishingLocationMessages.TellFishingLocation(selectedLocation); // Provide location-specific messages
                    MessageBox.Show("Make sure you're in the fishing dock before pressing OK!");
                    await _fishingService.StartFishing(selectedLocation, numberOfCasts, numberOfSells, randomFishingCheckBox.Checked, token); // Start standard fishing
                }
            }
            catch (TaskCanceledException)
            {
                MessageBox.Show("Fishing was cancelled."); // Handle cancellation of the task
            }
            catch (Exception ex) // Catch any other unforeseen errors
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }

        private void randomFishing_CheckedChanged(object sender, EventArgs e)
        {
            if (randomFishingCheckBox.Checked)
            {
                MessageBox.Show("This will add randomness to the line casting!");
            }
        }

        private void button4_Click(object sender, EventArgs e)//button to stop fishing
        {
            // Check if the operation is already canceled or not started
            if (cancellationTokenSource == null || cancellationTokenSource.IsCancellationRequested)
            {
                MessageBox.Show("Fishing is not currently in progress.");
                return;
            }

            // Signal the cancellation
            cancellationTokenSource.Cancel();
            MessageBox.Show("Fishing stopped!");
        }

        private void smartFishing_CheckedChanged(object sender, EventArgs e)
        {
            if (smartFishing.Checked)
                CoreFunctionality.isAutoDetectFishingBtnActive = true;
            else
                CoreFunctionality.isAutoDetectFishingBtnActive = false;
        }

        private async void button5_Click(object sender, EventArgs e)//racing test
        {
            MessageBox.Show("Press OK when ready to begin!");
            Thread.Sleep(5000);
            Point test = CoreFunctionality.getCursorLocation();
            CoreFunctionality.GetColorAt(test.X, test.Y);
            string hexColor = CoreFunctionality.HexConverter(CoreFunctionality.GetColorAt(test.X, test.Y));
            //Debug.WriteLine("HEX: " + BotFunctions.HexConverter(BotFunctions.GetColorAt(test.X, test.Y)) + " RGB: " + BotFunctions.GetColorAt(test.X, test.Y));
            Debug.WriteLine("HEX: " + CoreFunctionality.HexConverter(CoreFunctionality.GetColorAt(test.X, test.Y)) + " RGB: " + CoreFunctionality.GetColorAt(test.X, test.Y));
            MessageBox.Show("Done");

            CoreFunctionality.maximizeAndFocus();

            Image screenshot = ImageRecognition.GetWindowScreenshot();
            /*PictureBox pictureBox = new PictureBox();
            pictureBox.Image = screenshot;
            pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;*/

            string redFishingButton = "#FD0000";
            string fishingExitButton = "#E6A951";

            await ImageRecognition.locateColorInImage(screenshot, redFishingButton, 10);

            // Set the size of the form to the size of the image
            /*Form form = new Form();
            form.ClientSize = screenshot.Size;
            form.Controls.Add(pictureBox);
            form.ShowDialog();*/


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
            Services.Golf.afternoonTee();
        }

        // GOLF - Holey Mackeral
        private void golfHoleyMackeral(object sender, EventArgs e)
        {
            Services.Golf.holeyMackeral();
        }

        // GOLF - Hole on the Range
        private void golfHoleOnTheRange(object sender, EventArgs e)
        {
            Services.Golf.holeOnTheRange();
        }

        // GOLF - Seeing green
        private void golfSeeingGreen(object sender, EventArgs e)
        {
            Services.Golf.seeingGreen();
        }

        // GOLF - Swing Time
        private void button15_Click(object sender, EventArgs e)
        {
            Services.Golf.swingTime();
        }

        // GOLF - Down the Hatch
        private void button14_Click(object sender, EventArgs e)
        {
            Services.Golf.downTheHatch();
        }

        //GOLF - Peanut Putter
        private void button13_Click(object sender, EventArgs e)
        {
            Services.Golf.peanutPutter();
        }

        //GOLF - Hot Links
        private void button16_Click(object sender, EventArgs e)
        {
            Services.Golf.hotLinks();
        }

        //GOLF - Hole In Fun
        private void button17_Click(object sender, EventArgs e)
        {
            Services.Golf.holeInFun();
        }

        //GOLF - Swing-A-Long
        private void button18_Click(object sender, EventArgs e)
        {
            Services.Golf.swingALong();
        }

        //GOLF - One Little Birdie
        private void One_Little_Birdie_Click(object sender, EventArgs e)
        {
            Services.Golf.oneLittleBirdie();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
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
            /*if (!stopTrainingClicked)
            {
                doodleTrainingThreading = new Thread(() => DoodleTraining.startTrainingDoodle(numberOfFeeds, numberOfScratches, checkBox3.Checked, selectedTrick, checkBox4.Checked, checkBox5.Checked));
                doodleTrainingThreading.Start();
            }*/
        }

        private void button19_Click(object sender, EventArgs e)
        {
            DoodleTraining.shouldStopTraining = true;
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
                if (checkBox3.Checked)
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

        //Settings page, button to open update images setting
        private void updateImagesBtn_Click(object sender, EventArgs e)
        {
            UpdateImages updateRecImages = new UpdateImages();
            try
            {
                updateRecImages.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Unable to perform this action", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        //Settings page, button to reset all images
        private void resetImagesBtn_Click(object sender, EventArgs e)
        {
            foreach (SettingsProperty currentProperty in Properties.Settings.Default.Properties)
            {
                Properties.Settings.Default[currentProperty.Name] = "";
            }
            Properties.Settings.Default.Save();
        }

        private void fishingLocationscomboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Ensure there's a selected item to avoid NullReferenceException
            if (fishingLocationscomboBox.SelectedItem != null)
            {
                string selectedLocation = fishingLocationscomboBox.SelectedItem.ToString();
                label12.Text = FishingLocationMessages.GetLocationMessage(selectedLocation);
                label12.Visible = true;

                if (selectedLocation == "CUSTOM FISHING ACTION")
                {
                    debugCustomActionsCheckBox.Visible = true;
                    debugCustomActionsCheckBox.Enabled = true;
                    customFishingFilesComboBox.Visible = true;
                    LoadCustomFishingActions();
                }
                else
                {
                    debugCustomActionsCheckBox.Visible = false;
                    debugCustomActionsCheckBox.Enabled = false;
                    customFishingFilesComboBox.Visible = false;
                }
            }
            else
                label12.Visible = false;
        }

        private void button21_Click(object sender, EventArgs e)
        {
            new CustomFishingActions().Show();
        }

        public void LoadCustomFishingActions()
        {
            string[] files = CoreFunctionality.loadCustomFishingActions();

            customFishingFilesComboBox.Items.Clear();
            foreach (string file in files)
            {
                customFishingFilesComboBox.Items.Add(Path.GetFileName(file)); // Add only file names to the ComboBox
            }
        }
    }
}
