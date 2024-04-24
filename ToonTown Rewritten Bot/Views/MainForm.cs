//using Emgu.CV.XPhoto;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private FishingService _fishingService = new FishingService();
        public MainForm()
        {
            InitializeComponent();

            CoreFunctionality.EnsureAllEmbeddedJsonFilesExist();

            // Move this eventually
            LoadCustomActions("Golf", customGolfFilesComboBox);

            CoordinatesManager.ReadCoordinates();
            BotFunctions.CreateItemsDataFileMap();
            LoadCoordinatesIntoResetBox();
            doodleTrickComboBox.SelectedIndex = 0; // clean this up/move this eventually
        }

        //important functions for bot
        private void startSpamButton_Click(object sender, EventArgs e)//spam message on screen
        {//if the user presses ALT key, it will break the loop
            bool loopBroken = BotFunctions.SendMessage(messageToType.Text, Convert.ToInt32(numericUpDown2.Value), checkBox1.Checked, numericUpDown2);
        }

        private int timeLeft;
        private bool isToonAwakeActive = false;  // Flag to track if the function is active
        private void startKeepToonAwakeButton_Click(object sender, EventArgs e)
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Dispose();  // Dispose any existing token source
            }
            _cancellationTokenSource = new CancellationTokenSource();
            isToonAwakeActive = true;  // Flag to indicate the task is active

            int timeInSeconds = Convert.ToInt32(numericUpDown1.Value) * 60;  // Convert minutes to seconds
            timeLeft = timeInSeconds;  // Set timeLeft for countdown
            MessageBox.Show("Press OK when ready to begin!");

            timer1.Start();  // Start the countdown timer

            Task.Run(() =>
            {
                return BotFunctions.KeepToonAwake(timeInSeconds, _cancellationTokenSource.Token);
            }, _cancellationTokenSource.Token)
            .ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    CoreFunctionality.BringBotWindowToFront();
                    MessageBox.Show("Keep Toon Awake completed successfully!", "Keep Awake Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (task.IsFaulted)
                {
                    timer1.Stop();  // Ensure timer is stopped on error
                    MessageBox.Show($"Error: {task.Exception.InnerException.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());  // Ensure UI updates are on the main thread
        }

        private void selectFlowerBeanAmountBtn_Click(object sender, EventArgs e)//open the flower manager
        {
            Plants plantsForm = new Plants();
            try
            {
                string selected = (string)flowerBeanAmountDropdown.SelectedItem;
                plantsForm.PopulateFlowerOptionsBasedOnBeanCount(selected);
                this.Hide();
                plantsForm.ShowDialog();// Shows the form that allows the user to select one of the flowers from PopulateFlowerOptionsBasedOnBeanCount
                this.Show();
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

        private async void waterPlantBtn_Click(object sender, EventArgs e)
        {
            try
            {
                await Services.Gardening.WaterPlantAsync((int)waterPlantNumericUpDown.Value, _cancellationTokenSource.Token);
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

        private async void removePlantBtn_Click(object sender, EventArgs e)
        {
            try
            {
                // Assuming RemovePlantAsync is an instance method requiring a CancellationToken.
                await Services.Gardening.RemovePlantAsync(_cancellationTokenSource.Token);
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
                CoreFunctionality.BringBotWindowToFront();
                MessageBox.Show("Coordinates updated for " + selectedDescription);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to perform this action: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

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
            var token = _cancellationTokenSource.Token; // Token to handle task cancellation

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
                        await _fishingService.StartCustomFishingDebugging(filePath + ".json"); // Debugging custom fishing actions
                    }
                    else
                    {
                        await _fishingService.StartFishing(selectedLocation, numberOfCasts, numberOfSells, randomFishingCheckBox.Checked, token, filePath + ".json"); // Perform custom fishing actions
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
            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                MessageBox.Show("Fishing is not currently in progress.");
                return;
            }

            // Signal the cancellation
            _cancellationTokenSource.Cancel();
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

            CoreFunctionality.MaximizeAndFocusTTRWindow();

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

        private void unlimitedTrainingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (unlimitedTrainingCheckBox.Checked)
            {
                numberOfDoodleScratchesNumericUpDown.Enabled = false;
                numberOfDoodleFeedsNumericUpDown.Enabled = false;
                justFeedDoodleCheckBox.Checked = false;
                justScratchDoodleCheckBox.Checked = false;
                justFeedDoodleCheckBox.Enabled = false;
                justScratchDoodleCheckBox.Enabled = false;
            }
            else
            {
                numberOfDoodleScratchesNumericUpDown.Enabled = true;
                numberOfDoodleFeedsNumericUpDown.Enabled = true;
                justFeedDoodleCheckBox.Enabled = true;
                justScratchDoodleCheckBox.Enabled = true;
            }
        }

        private bool isTrainingActive = false;  // Flag to track training status

        private async void startDoodleTrainingBtn_Click(object sender, EventArgs e)
        {
            string selectedTrick = (string)doodleTrickComboBox.SelectedItem;
            int numberOfFeeds = Convert.ToInt32(numberOfDoodleFeedsNumericUpDown.Value);
            int numberOfScratches = Convert.ToInt32(numberOfDoodleScratchesNumericUpDown.Value);
            bool unlimitedCheckBox = unlimitedTrainingCheckBox.Checked;
            bool justFeed = justFeedDoodleCheckBox.Checked;
            bool justScratch = justScratchDoodleCheckBox.Checked;

            // Ensure we have a fresh CancellationTokenSource
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Dispose(); // Dispose the old one if it exists
            }
            _cancellationTokenSource = new CancellationTokenSource();
            isTrainingActive = true;  // Set the flag to indicate that training has started

            try
            {
                // Run the training task and handle completion
                await Task.Run(() => new DoodleTraining().StartDoodleTraining(
                    numberOfFeeds, numberOfScratches, unlimitedCheckBox,
                    selectedTrick, justFeed, justScratch, _cancellationTokenSource.Token),
                    _cancellationTokenSource.Token)
                .ContinueWith(task =>
                {
                    isTrainingActive = false;  // Clear the flag when training completes or is canceled
                    if (task.IsCompletedSuccessfully)
                    {
                        CoreFunctionality.BringBotWindowToFront();
                        MessageBox.Show("Doodle training completed successfully!", "Training Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (task.IsFaulted)
                    {
                        MessageBox.Show($"Error occurred during doodle training: {task.Exception?.GetBaseException().Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext()); // Ensure UI updates are done on the main thread.
            }
            catch (OperationCanceledException)
            {
                isTrainingActive = false;  // Ensure flag is cleared if training is canceled
            }
        }

        private void stopDoodleTrainingBtn_Click(object sender, EventArgs e)
        {
            // Check if the cancellation token source is created and the training is active
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested && isTrainingActive)
            {
                _cancellationTokenSource.Cancel();  // Request cancellation
                _cancellationTokenSource.Dispose();  // Dispose the token source
                _cancellationTokenSource = null;     // Reset the source to be sure it's fresh when restarted
                isTrainingActive = false;  // Clear the flag

                MessageBox.Show("Doodle Training stopped!", "Training Stopped", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // If the training was not active, show a different message
                MessageBox.Show("No active training to stop.", "Stop Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void justFeedDoodleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (justFeedDoodleCheckBox.Checked)
            {
                numberOfDoodleScratchesNumericUpDown.Enabled = false;
                justScratchDoodleCheckBox.Checked = false;
            }
            else
            {
                numberOfDoodleScratchesNumericUpDown.Enabled = true;
                if (unlimitedTrainingCheckBox.Checked)
                {
                    numberOfDoodleFeedsNumericUpDown.Enabled = false;
                    numberOfDoodleScratchesNumericUpDown.Enabled = false;
                }
            }
        }

        private void justScratchDoodleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (justScratchDoodleCheckBox.Checked)
            {
                numberOfDoodleFeedsNumericUpDown.Enabled = false;
                justFeedDoodleCheckBox.Checked = false;
            }
            else
            {
                numberOfDoodleFeedsNumericUpDown.Enabled = true;
                if (unlimitedTrainingCheckBox.Checked)
                {
                    numberOfDoodleFeedsNumericUpDown.Enabled = false;
                    numberOfDoodleScratchesNumericUpDown.Enabled = false;
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
                    LoadCustomActions("Fishing", customFishingFilesComboBox);
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

        private void createCustomFishingActionsBtn_Click(object sender, EventArgs e)
        {
            using (var form = new CustomFishingActions())
            {
                form.ShowDialog(); // This will block until the form is closed
            }
            LoadCustomActions("Fishing", customFishingFilesComboBox); // load fishing actions after the form is closed
        }

        public void LoadCustomActions(string actionType, ComboBox comboBox)
        {
            string[] files = (string[])CoreFunctionality.ManageCustomActionsFolder(actionType, true);

            // Clear the items from the ComboBox passed as a parameter.
            comboBox.Items.Clear();

            // Iterate through the files, adding them to the ComboBox if they are JSON files.
            foreach (string file in files)
            {
                // Check if the file extension is .json
                if (Path.GetExtension(file).Equals(".json", StringComparison.OrdinalIgnoreCase))
                {
                    // Add the file name without the .json extension to the ComboBox
                    comboBox.Items.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
        }

        private void stopKeepToonAwakeButton_Click(object sender, EventArgs e)
        {
            // Check if the cancellation token source is created and not yet cancelled and the function is active
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested && isToonAwakeActive)
            {
                _cancellationTokenSource.Cancel();  // Request cancellation
                _cancellationTokenSource.Dispose();  // Dispose the token source
                _cancellationTokenSource = null;     // Reset the source to ensure it's fresh when restarted
                isToonAwakeActive = false;  // Clear the flag
                timer1.Stop();
                timeLeft = 0;
                label1.Visible = false;

                MessageBox.Show("Keep Toon Awake stopped!", "Keep Toon Awake Stopped", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // If the function was not active, show a different message
                MessageBox.Show("No active 'Keep Toon Awake' function to stop.", "Stop Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void createCustomGolfActionsBtn_Click(object sender, EventArgs e)
        {
            using (var form = new CustomGolfActions())
            {
                form.ShowDialog(); // This will block until the form is closed
            }

            LoadCustomActions("Golf", customGolfFilesComboBox); // load golf actions after the form is closed
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string selectedFileName = customGolfFilesComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedFileName))
            {
                MessageBox.Show("Please select a custom golf action file.");
                return;
            }

            // Get the full path to the selected golf action file.
            string filePath = GolfService.GetCustomGolfActionFilePath(selectedFileName);

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await GolfService.StartCustomGolfAction(filePath, _cancellationTokenSource.Token);
                CoreFunctionality.BringBotWindowToFront();
                MessageBox.Show("Golf actions completed successfully.", "Golf Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Golf actions were cancelled.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void customGolfFilesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (customGolfFilesComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a custom golf action file.");
                return;
            }

            golfActionsListBox.Items.Clear();
            string selectedFileName = customGolfFilesComboBox.SelectedItem.ToString();
            string filePath = GolfService.GetCustomGolfActionFilePath(selectedFileName);
            var actions = GolfService.GetCustomGolfActions(filePath);

            foreach (var action in actions)
            {
                //Debug.WriteLine($"Action: {action.Action}, Command: {action.Command}, Duration: {action.Duration}");
                golfActionsListBox.Items.Add($"{action.Action} - {action.Duration} ms");
            }
        }
    }
}
