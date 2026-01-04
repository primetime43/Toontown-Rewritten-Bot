using System;
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
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private FishingService _fishingService = new FishingService();
        private FishingOverlayForm _fishingOverlay;

        /// <summary>
        /// Gets the fishing overlay form if it's active.
        /// </summary>
        public FishingOverlayForm FishingOverlay => _fishingOverlay;
        public MainForm()
        {
            InitializeComponent();

            // Enable keyboard shortcuts
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            // Check if a new version of the program is available
            GithubReleaseChecker.CheckForNewVersion().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    MessageBox.Show("Error checking for updates: " + t.Exception.Flatten().InnerException.Message);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext()); // Ensures the continuation runs on the UI thread

            CoreFunctionality.EnsureAllEmbeddedJsonFilesExist();

            // Move this eventually
            LoadCustomActions("Golf", customGolfFilesComboBox);

            CoordinatesManager.ReadCoordinates();
            BotFunctions.CreateItemsDataFileMap();
            LoadCoordinatesIntoResetBox();
            doodleTrickComboBox.SelectedIndex = 0; // clean this up/move this eventually
            LoadTemplateItemsComboBox();
        }

        /// <summary>
        /// Global keyboard shortcut handler.
        /// Press Escape or F12 to stop fishing/other active tasks.
        /// </summary>
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Escape or F12 stops fishing and other active tasks
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.F12)
            {
                StopAllActiveTasks();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Stops all active tasks (fishing, training, etc.) by cancelling the token.
        /// </summary>
        private void StopAllActiveTasks()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                System.Diagnostics.Debug.WriteLine("[MainForm] Tasks stopped via keyboard shortcut");
            }
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
                // Ensure cancellation token source exists
                if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = new CancellationTokenSource();
                }

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
                // Ensure cancellation token source exists
                if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = new CancellationTokenSource();
                }

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
            // Reset the CancellationTokenSource if it's null or was previously cancelled
            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
            }

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
                        await _fishingService.StartFishing(selectedLocation, numberOfCasts, numberOfSells, randomFishingCheckBox.Checked, token, filePath + ".json", autoDetectFishCheckBox.Checked); // Perform custom fishing actions
                    }
                }
                else
                {
                    FishingLocationMessages.TellFishingLocation(selectedLocation); // Provide location-specific messages
                    MessageBox.Show("Make sure you're in the fishing dock before pressing OK!");
                    await _fishingService.StartFishing(selectedLocation, numberOfCasts, numberOfSells, randomFishingCheckBox.Checked, token, "", autoDetectFishCheckBox.Checked); // Start standard fishing
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

        private async void CalibrateFishBtn_Click(object sender, EventArgs e)
        {
            string selectedLocation = fishingLocationscomboBox.SelectedItem?.ToString() ?? "Fish Anywhere";
            calibrateFishBtn.Enabled = false;
            fishCalibrationLabel.Text = "Scanning...";
            fishCalibrationLabel.ForeColor = Color.Orange;

            try
            {
                // Take a screenshot
                var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot();
                if (screenshot == null)
                {
                    MessageBox.Show("Could not capture screenshot. Make sure Toontown is running.", "Calibration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Run fish detection
                var detector = new FishBubbleDetector(selectedLocation);
                var result = await Task.Run(() => detector.DetectFromScreenshot(screenshot));

                if (result.BestShadowPosition.HasValue)
                {
                    var shadowPos = result.BestShadowPosition.Value;
                    var shadowColor = result.BestShadowColor;

                    var response = MessageBox.Show(
                        $"Found a shadow at ({shadowPos.X}, {shadowPos.Y})\n" +
                        $"Color: RGB({shadowColor.R}, {shadowColor.G}, {shadowColor.B})\n\n" +
                        "Is this a FISH shadow?\n\n" +
                        "Click YES to use this color for detection.\n" +
                        "Click NO if it's not a fish (dock, seaweed, etc.)",
                        "Confirm Fish Shadow",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (response == DialogResult.Yes)
                    {
                        detector.ConfirmFishShadow(screenshot, shadowPos);
                        var learned = detector.GetLearnedShadowColor();
                        if (learned.HasValue)
                        {
                            fishCalibrationLabel.Text = $"RGB({learned.Value.R},{learned.Value.G},{learned.Value.B})";
                            fishCalibrationLabel.ForeColor = Color.LimeGreen;
                            MessageBox.Show("Calibrated! Fish detection will now use this color.", "Calibration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        fishCalibrationLabel.Text = "Not calibrated";
                        fishCalibrationLabel.ForeColor = Color.Gray;
                    }
                }
                else
                {
                    MessageBox.Show(
                        "No fish shadow detected.\n\n" +
                        "Make sure:\n" +
                        "- You're at the fishing dock\n" +
                        "- There's a fish visible in the water\n" +
                        "- Try again when fish is stationary",
                        "No Fish Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    fishCalibrationLabel.Text = "Not calibrated";
                    fishCalibrationLabel.ForeColor = Color.Gray;
                }

                screenshot.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Calibration error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                fishCalibrationLabel.Text = "Error";
                fishCalibrationLabel.ForeColor = Color.Red;
            }
            finally
            {
                calibrateFishBtn.Enabled = true;
            }
        }

        private void ShowOverlayCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (showOverlayCheckBox.Checked)
            {
                // Create and show the overlay
                if (_fishingOverlay == null || _fishingOverlay.IsDisposed)
                {
                    _fishingOverlay = new FishingOverlayForm();
                }
                _fishingOverlay.Show();
                _fishingOverlay.SetStatus("Overlay active - waiting for fishing...");

                // Connect overlay to fishing strategy
                Services.FishingLocationsWalking.FishingStrategyBase.Overlay = _fishingOverlay;

                // Set callback to auto-uncheck when fishing ends
                Services.FishingLocationsWalking.FishingStrategyBase.OnFishingEnded = OnFishingEndedCallback;
            }
            else
            {
                // Clear the callback
                Services.FishingLocationsWalking.FishingStrategyBase.OnFishingEnded = null;

                // Disconnect from fishing strategy
                Services.FishingLocationsWalking.FishingStrategyBase.Overlay = null;

                // Hide and dispose the overlay
                if (_fishingOverlay != null && !_fishingOverlay.IsDisposed)
                {
                    _fishingOverlay.Close();
                    _fishingOverlay.Dispose();
                    _fishingOverlay = null;
                }
            }
        }

        /// <summary>
        /// Called when fishing ends to auto-uncheck the overlay checkbox.
        /// </summary>
        private void OnFishingEndedCallback()
        {
            // Must invoke on UI thread since this is called from fishing task
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnFishingEndedCallback()));
                return;
            }

            // Uncheck the overlay checkbox (this will trigger the CheckedChanged event to close the overlay)
            if (showOverlayCheckBox.Checked)
            {
                showOverlayCheckBox.Checked = false;
            }
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

            CoreFunctionality.FocusTTRWindow();

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

        //Settings page, button to reset all images (legacy - kept for compatibility)
        private void resetImagesBtn_Click(object sender, EventArgs e)
        {
            foreach (SettingsProperty currentProperty in Properties.Settings.Default.Properties)
            {
                Properties.Settings.Default[currentProperty.Name] = "";
            }
            Properties.Settings.Default.Save();
        }

        // Open Image Recognition Debug Window
        private void openImageRecDebugBtn_Click(object sender, EventArgs e)
        {
            var debugForm = new ImageRecognitionDebugForm();
            debugForm.Show();
        }

        // Download OCR data automatically
        private async void downloadOcrDataBtn_Click(object sender, EventArgs e)
        {
            // Check if already exists
            if (TessdataDownloader.LanguageDataExists())
            {
                MessageBox.Show(
                    "OCR data is already downloaded and ready to use!\n\n" +
                    "Click 'Open Debug Window' to test the OCR functionality.",
                    "OCR Data Ready",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Download
            var button = sender as Button;
            if (button != null)
            {
                button.Enabled = false;
                button.Text = "Downloading...";
            }

            try
            {
                bool success = await TessdataDownloader.EnsureLanguageDataExistsAsync();

                if (success)
                {
                    MessageBox.Show(
                        "OCR data downloaded successfully!\n\n" +
                        "Click 'Open Debug Window' to test the OCR functionality.",
                        "Download Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Failed to download OCR data.\n\n" +
                        "Please check your internet connection and try again.",
                        "Download Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Download error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (button != null)
                {
                    button.Enabled = true;
                    button.Text = "Download OCR Data";
                }
            }
        }

        // Template management methods
        private void LoadTemplateItemsComboBox()
        {
            comboBoxTemplateItems.Items.Clear();

            // Load from file-based TemplateDefinitionManager
            var definitions = TemplateDefinitionManager.Instance.GetAllDefinitions();
            foreach (var def in definitions)
            {
                comboBoxTemplateItems.Items.Add($"[{def.Category}] {def.Name}");
            }

            if (comboBoxTemplateItems.Items.Count > 0)
            {
                comboBoxTemplateItems.SelectedIndex = 0;
            }
        }

        private string GetSelectedTemplateName()
        {
            if (comboBoxTemplateItems.SelectedItem == null)
                return null;

            string selected = comboBoxTemplateItems.SelectedItem.ToString();
            // Extract name from "[Category] Name" format
            int bracketEnd = selected.IndexOf("] ");
            if (bracketEnd >= 0)
                return selected.Substring(bracketEnd + 2);
            return selected;
        }

        private void comboBoxTemplateItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedItem = GetSelectedTemplateName();
            if (string.IsNullOrEmpty(selectedItem))
                return;

            // Check if template exists
            bool hasTemplate = UIElementManager.Instance.HasTemplate(selectedItem);

            if (hasTemplate)
            {
                labelTemplateStatus.Text = $"Template exists";
                labelTemplateStatus.ForeColor = Color.Green;
                btnViewTemplate.Enabled = true;
            }
            else
            {
                labelTemplateStatus.Text = $"No template - click 'Capture' to create";
                labelTemplateStatus.ForeColor = Color.Orange;
                btnViewTemplate.Enabled = false;
            }
        }

        private void btnCaptureTemplate_Click(object sender, EventArgs e)
        {
            string selectedItem = GetSelectedTemplateName();
            if (string.IsNullOrEmpty(selectedItem))
            {
                MessageBox.Show("Please select an item first.", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Use the existing TemplateCaptureForm
            bool captured = TemplateCaptureForm.CaptureTemplate(selectedItem);

            if (captured)
            {
                MessageBox.Show($"Template captured successfully for: {selectedItem}", "Template Captured", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Refresh the status
                comboBoxTemplateItems_SelectedIndexChanged(sender, e);
            }
        }

        private void btnViewTemplate_Click(object sender, EventArgs e)
        {
            string selectedItem = GetSelectedTemplateName();
            if (string.IsNullOrEmpty(selectedItem))
            {
                MessageBox.Show("Please select an item first.", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string templatePath = UIElementManager.Instance.GetTemplatePath(selectedItem);

            if (string.IsNullOrEmpty(templatePath) || !System.IO.File.Exists(templatePath))
            {
                MessageBox.Show($"No template found for: {selectedItem}\n\nClick 'Capture Template' to create one.", "Template Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Open the template image in a simple viewer
            try
            {
                using (var viewerForm = new Form())
                {
                    viewerForm.Text = $"Template: {selectedItem}";
                    viewerForm.StartPosition = FormStartPosition.CenterParent;

                    var pictureBox = new PictureBox
                    {
                        Dock = DockStyle.Fill,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Image = Image.FromFile(templatePath)
                    };
                    viewerForm.Controls.Add(pictureBox);

                    // Size the form based on image size
                    viewerForm.ClientSize = new Size(
                        Math.Max(200, Math.Min(pictureBox.Image.Width + 20, 600)),
                        Math.Max(150, Math.Min(pictureBox.Image.Height + 20, 400))
                    );

                    var openFolderBtn = new Button
                    {
                        Text = "Open Folder",
                        Dock = DockStyle.Bottom,
                        Height = 30
                    };
                    openFolderBtn.Click += (s, args) =>
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{templatePath}\"");
                    };
                    viewerForm.Controls.Add(openFolderBtn);

                    viewerForm.ShowDialog(this);

                    // Dispose the image properly
                    pictureBox.Image?.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error viewing template: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddTemplateItem_Click(object sender, EventArgs e)
        {
            using (var inputForm = new Form())
            {
                inputForm.Text = "Add New Template Item";
                inputForm.ClientSize = new Size(380, 180);
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                var lblName = new Label { Text = "Item Name:", Location = new Point(15, 15), AutoSize = true };
                var txtName = new TextBox { Location = new Point(15, 35), Size = new Size(350, 25) };

                var lblCategory = new Label { Text = "Category (select existing or type new):", Location = new Point(15, 70), AutoSize = true };
                var cmbCategory = new ComboBox
                {
                    Location = new Point(15, 90),
                    Size = new Size(350, 25),
                    DropDownStyle = ComboBoxStyle.DropDown
                };

                // Add existing categories as suggestions
                var categories = TemplateDefinitionManager.Instance.GetCategories();
                cmbCategory.Items.AddRange(categories.ToArray());
                cmbCategory.Text = categories.Count > 0 ? categories[0] : "Custom";

                var btnOk = new Button { Text = "Add", Location = new Point(205, 135), Size = new Size(75, 30), DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Cancel", Location = new Point(290, 135), Size = new Size(75, 30), DialogResult = DialogResult.Cancel };

                inputForm.Controls.AddRange(new Control[] { lblName, txtName, lblCategory, cmbCategory, btnOk, btnCancel });
                inputForm.AcceptButton = btnOk;
                inputForm.CancelButton = btnCancel;

                if (inputForm.ShowDialog(this) == DialogResult.OK)
                {
                    string name = txtName.Text.Trim();
                    string category = string.IsNullOrWhiteSpace(cmbCategory.Text) ? "Custom" : cmbCategory.Text.Trim();

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        MessageBox.Show("Please enter a name for the template item.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (TemplateDefinitionManager.Instance.AddDefinition(name, category))
                    {
                        MessageBox.Show($"Added new template item: {name}\n\nYou can now capture a template for it.", "Item Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadTemplateItemsComboBox();

                        // Select the newly added item
                        for (int i = 0; i < comboBoxTemplateItems.Items.Count; i++)
                        {
                            if (comboBoxTemplateItems.Items[i].ToString().Contains(name))
                            {
                                comboBoxTemplateItems.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show($"An item with that name already exists.", "Duplicate Item", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void btnOpenTemplateDefinitions_Click(object sender, EventArgs e)
        {
            string filePath = TemplateDefinitionManager.Instance.GetDefinitionsFilePath();

            if (!System.IO.File.Exists(filePath))
            {
                MessageBox.Show("Definitions file not found. It will be created when you add the first item.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start("notepad.exe", filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnEditTemplate_Click(object sender, EventArgs e)
        {
            string currentName = GetSelectedTemplateName();
            if (string.IsNullOrEmpty(currentName))
            {
                MessageBox.Show("Please select a template to edit.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var definition = TemplateDefinitionManager.Instance.GetDefinition(currentName);
            if (definition == null)
            {
                MessageBox.Show("Template definition not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var dialog = new Form())
            {
                dialog.Text = "Edit Template";
                dialog.ClientSize = new Size(380, 180);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;

                var nameLabel = new Label { Text = "Name:", Location = new Point(15, 20), AutoSize = true };
                var nameTextBox = new TextBox { Text = definition.Name, Location = new Point(80, 17), Size = new Size(280, 23) };

                var categoryLabel = new Label { Text = "Category:", Location = new Point(15, 55), AutoSize = true };
                var categoryComboBox = new ComboBox { Text = definition.Category, Location = new Point(80, 52), Size = new Size(280, 23), DropDownStyle = ComboBoxStyle.DropDown };

                // Add existing categories
                foreach (var cat in TemplateDefinitionManager.Instance.GetCategories())
                    categoryComboBox.Items.Add(cat);

                var saveBtn = new Button { Text = "Save", DialogResult = DialogResult.OK, Location = new Point(185, 130), Size = new Size(80, 30) };
                var cancelBtn = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(275, 130), Size = new Size(80, 30) };

                dialog.Controls.AddRange(new Control[] { nameLabel, nameTextBox, categoryLabel, categoryComboBox, saveBtn, cancelBtn });
                dialog.AcceptButton = saveBtn;
                dialog.CancelButton = cancelBtn;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    string newName = nameTextBox.Text.Trim();
                    string newCategory = categoryComboBox.Text.Trim();

                    if (string.IsNullOrEmpty(newName))
                    {
                        MessageBox.Show("Name cannot be empty.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // If name changed, rename the template file too
                    if (!currentName.Equals(newName, StringComparison.OrdinalIgnoreCase))
                    {
                        string oldPath = UIElementManager.Instance.GetTemplatePath(currentName);
                        string newPath = UIElementManager.Instance.GetTemplatePath(newName);

                        if (System.IO.File.Exists(oldPath) && !System.IO.File.Exists(newPath))
                        {
                            try
                            {
                                System.IO.File.Move(oldPath, newPath);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Failed to rename template file: {ex.Message}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }

                    if (TemplateDefinitionManager.Instance.UpdateDefinition(currentName, newName, newCategory))
                    {
                        MessageBox.Show($"Updated template: {newName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadTemplateItemsComboBox();

                        // Re-select the renamed item (format is "[Category] Name")
                        for (int i = 0; i < comboBoxTemplateItems.Items.Count; i++)
                        {
                            if (comboBoxTemplateItems.Items[i].ToString().EndsWith("] " + newName))
                            {
                                comboBoxTemplateItems.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Failed to update template. Name may already exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnDeleteTemplate_Click(object sender, EventArgs e)
        {
            string templateName = GetSelectedTemplateName();
            if (string.IsNullOrEmpty(templateName))
            {
                MessageBox.Show("Please select a template to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the template definition '{templateName}'?\n\nThis will NOT delete the template image file.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                if (TemplateDefinitionManager.Instance.RemoveDefinition(templateName))
                {
                    MessageBox.Show($"Deleted template definition: {templateName}", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadTemplateItemsComboBox();
                }
                else
                {
                    MessageBox.Show("Failed to delete template definition.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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

                // Update calibration status for this location
                UpdateFishCalibrationStatus(selectedLocation);
            }
            else
                label12.Visible = false;
        }

        private void UpdateFishCalibrationStatus(string locationName)
        {
            var detector = new FishBubbleDetector(locationName);
            var learnedColor = detector.GetLearnedShadowColor();

            if (learnedColor.HasValue)
            {
                var lc = learnedColor.Value;
                fishCalibrationLabel.Text = $"RGB({lc.R},{lc.G},{lc.B})";
                fishCalibrationLabel.ForeColor = Color.LimeGreen;
            }
            else
            {
                fishCalibrationLabel.Text = "Not calibrated";
                fishCalibrationLabel.ForeColor = Color.Gray;
            }
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

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = CoordinatesManager.GetCoordinatesFilePath(),
                    UseShellExecute = true, 
                    Verb = "open" 
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open the folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
