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
using ToonTown_Rewritten_Bot.Services.FishingLocationsWalking;
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
        private GlobalKeyboardHook _globalKeyboardHook;

        /// <summary>
        /// Gets the fishing overlay form if it's active.
        /// </summary>
        public FishingOverlayForm FishingOverlay => _fishingOverlay;
        public MainForm()
        {
            InitializeComponent();

            // Enable keyboard shortcuts (local - when bot has focus)
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            // Enable global keyboard hook (works even when game has focus)
            _globalKeyboardHook = new GlobalKeyboardHook();
            _globalKeyboardHook.KeyPressed += GlobalKeyboardHook_KeyPressed;
            _globalKeyboardHook.Start();

            // Clean up hook when form closes
            this.FormClosing += MainForm_FormClosing;

            // Check if a new version of the program is available
            GithubReleaseChecker.CheckForNewVersion().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    MessageBox.Show("Error checking for updates: " + t.Exception.Flatten().InnerException.Message);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext()); // Ensures the continuation runs on the UI thread

            CoreFunctionality.EnsureAllEmbeddedJsonFilesExist();

            // Load custom actions for Golf and Custom Fishing tabs
            LoadCustomActions("Golf", customGolfFilesComboBox);
            LoadCustomActions("Fishing", customFishingFilesComboBox);

            CoordinatesManager.ReadCoordinates();
            LoadCoordinatesIntoResetBox();
            doodleTrickComboBox.SelectedIndex = 0; // clean this up/move this eventually
            LoadTemplateItemsComboBox();

            // Load saved user preferences
            LoadUserPreferences();

            // Populate the Settings tab preferences display
            RefreshPreferencesDisplay();
        }

        /// <summary>
        /// Loads user preferences from file and applies them to UI controls.
        /// </summary>
        private void LoadUserPreferences()
        {
            var prefs = UserPreferences.Instance;

            // Fishing preferences
            if (!string.IsNullOrEmpty(prefs.FishingLocation))
            {
                int index = fishingLocationscomboBox.FindStringExact(prefs.FishingLocation);
                if (index >= 0) fishingLocationscomboBox.SelectedIndex = index;
            }
            numericUpDown3.Value = Math.Max(numericUpDown3.Minimum, Math.Min(numericUpDown3.Maximum, prefs.NumberOfCasts));
            numericUpDown4.Value = Math.Max(numericUpDown4.Minimum, Math.Min(numericUpDown4.Maximum, prefs.NumberOfSells));
            numericUpDownBiteTimeout.Value = Math.Max(numericUpDownBiteTimeout.Minimum, Math.Min(numericUpDownBiteTimeout.Maximum, prefs.BiteTimeoutSeconds));
            randomFishingCheckBox.Checked = prefs.RandomVariance;
            autoDetectFishCheckBox.Checked = prefs.AutoDetectFish;
            waitForFishCheckBox.Checked = prefs.WaitForFishBeforeCasting;
            numericUpDownWaitAttempts.Value = Math.Max(numericUpDownWaitAttempts.Minimum, Math.Min(numericUpDownWaitAttempts.Maximum, prefs.MaxFishWaitAttempts));

            // Custom Fishing preferences
            if (!string.IsNullOrEmpty(prefs.CustomFishingFile))
            {
                int customFishingIndex = customFishingFilesComboBox.FindStringExact(prefs.CustomFishingFile);
                if (customFishingIndex >= 0) customFishingFilesComboBox.SelectedIndex = customFishingIndex;
            }
            numericUpDownCustomCasts.Value = Math.Max(numericUpDownCustomCasts.Minimum, Math.Min(numericUpDownCustomCasts.Maximum, prefs.CustomFishingCasts));
            numericUpDownCustomSells.Value = Math.Max(numericUpDownCustomSells.Minimum, Math.Min(numericUpDownCustomSells.Maximum, prefs.CustomFishingSells));

            // Golf preferences
            if (!string.IsNullOrEmpty(prefs.GolfCourse))
            {
                int golfIndex = customGolfFilesComboBox.FindStringExact(prefs.GolfCourse);
                if (golfIndex >= 0) customGolfFilesComboBox.SelectedIndex = golfIndex;
            }
            showGolfOverlayCheckBox.Checked = prefs.ShowGolfOverlay;

            // Doodle preferences
            if (!string.IsNullOrEmpty(prefs.DoodleTrick))
            {
                int trickIndex = doodleTrickComboBox.FindStringExact(prefs.DoodleTrick);
                if (trickIndex >= 0) doodleTrickComboBox.SelectedIndex = trickIndex;
            }
            numberOfDoodleFeedsNumericUpDown.Value = Math.Max(numberOfDoodleFeedsNumericUpDown.Minimum, Math.Min(numberOfDoodleFeedsNumericUpDown.Maximum, prefs.NumberOfFeeds));
            numberOfDoodleScratchesNumericUpDown.Value = Math.Max(numberOfDoodleScratchesNumericUpDown.Minimum, Math.Min(numberOfDoodleScratchesNumericUpDown.Maximum, prefs.NumberOfScratches));
            unlimitedTrainingCheckBox.Checked = prefs.UnlimitedTraining;
            justFeedDoodleCheckBox.Checked = prefs.JustFeedDoodle;
            justScratchDoodleCheckBox.Checked = prefs.JustScratchDoodle;

            // Gardening preferences
            waterPlantNumericUpDown.Value = Math.Max(waterPlantNumericUpDown.Minimum, Math.Min(waterPlantNumericUpDown.Maximum, prefs.WaterPlantCount));
            if (!string.IsNullOrEmpty(prefs.FlowerBeanAmount))
            {
                int flowerIndex = flowerBeanAmountDropdown.FindStringExact(prefs.FlowerBeanAmount);
                if (flowerIndex >= 0) flowerBeanAmountDropdown.SelectedIndex = flowerIndex;
            }

            // Misc preferences
            checkBox2.Checked = prefs.KeepProgramOnTop;
            numericUpDown1.Value = Math.Max(numericUpDown1.Minimum, Math.Min(numericUpDown1.Maximum, prefs.KeepToonAwakeMinutes));
        }

        /// <summary>
        /// Saves current UI values to user preferences.
        /// </summary>
        private void SaveUserPreferences()
        {
            var prefs = UserPreferences.Instance;

            // Fishing preferences
            prefs.FishingLocation = fishingLocationscomboBox.SelectedItem?.ToString() ?? "";
            prefs.NumberOfCasts = (int)numericUpDown3.Value;
            prefs.NumberOfSells = (int)numericUpDown4.Value;
            prefs.BiteTimeoutSeconds = (int)numericUpDownBiteTimeout.Value;
            prefs.RandomVariance = randomFishingCheckBox.Checked;
            prefs.AutoDetectFish = autoDetectFishCheckBox.Checked;
            prefs.WaitForFishBeforeCasting = waitForFishCheckBox.Checked;
            prefs.MaxFishWaitAttempts = (int)numericUpDownWaitAttempts.Value;

            // Custom Fishing preferences
            prefs.CustomFishingFile = customFishingFilesComboBox.SelectedItem?.ToString() ?? "";
            prefs.CustomFishingCasts = (int)numericUpDownCustomCasts.Value;
            prefs.CustomFishingSells = (int)numericUpDownCustomSells.Value;

            // Golf preferences
            prefs.GolfCourse = customGolfFilesComboBox.SelectedItem?.ToString() ?? "";
            prefs.ShowGolfOverlay = showGolfOverlayCheckBox.Checked;

            // Doodle preferences
            prefs.DoodleTrick = doodleTrickComboBox.SelectedItem?.ToString() ?? "None";
            prefs.NumberOfFeeds = (int)numberOfDoodleFeedsNumericUpDown.Value;
            prefs.NumberOfScratches = (int)numberOfDoodleScratchesNumericUpDown.Value;
            prefs.UnlimitedTraining = unlimitedTrainingCheckBox.Checked;
            prefs.JustFeedDoodle = justFeedDoodleCheckBox.Checked;
            prefs.JustScratchDoodle = justScratchDoodleCheckBox.Checked;

            // Gardening preferences
            prefs.WaterPlantCount = (int)waterPlantNumericUpDown.Value;
            prefs.FlowerBeanAmount = flowerBeanAmountDropdown.SelectedItem?.ToString() ?? "";

            // Misc preferences
            prefs.KeepProgramOnTop = checkBox2.Checked;
            prefs.KeepToonAwakeMinutes = (int)numericUpDown1.Value;

            prefs.Save();
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

        /// <summary>
        /// Global keyboard hook handler - works even when game has focus.
        /// </summary>
        private void GlobalKeyboardHook_KeyPressed(object sender, Keys key)
        {
            if (key == Keys.Escape || key == Keys.F12)
            {
                // Ignore simulated key presses (e.g., from StraightenToonAsync sending ESC to cancel a cast)
                if (FishingStrategyBase.IsSimulatedKeyPress)
                {
                    System.Diagnostics.Debug.WriteLine("[MainForm] Ignoring simulated ESC key press");
                    return;
                }

                // Stop all active tasks
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(StopAllActiveTasks));
                }
                else
                {
                    StopAllActiveTasks();
                }
            }
            else if (key == Keys.F11)
            {
                // Toggle pause for fishing
                FishingStrategyBase.TogglePause();
            }
        }

        /// <summary>
        /// Clean up global keyboard hook when form closes.
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _globalKeyboardHook?.Dispose();

            // Save user preferences on close
            SaveUserPreferences();
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

            // Set the fishing settings from UI controls
            FishingStrategyBase.BiteTimeoutSeconds = Convert.ToInt32(numericUpDownBiteTimeout.Value);
            FishingStrategyBase.WaitForFishBeforeCasting = waitForFishCheckBox.Checked && autoDetectFishCheckBox.Checked;
            FishingStrategyBase.MaxFishWaitAttempts = Convert.ToInt32(numericUpDownWaitAttempts.Value);

            try
            {
                string selectedLocation = (string)fishingLocationscomboBox.SelectedItem; // Retrieve the location selected by the user
                int numberOfCasts = Convert.ToInt32(numericUpDown3.Value); // Number of times to cast the line
                int numberOfSells = Convert.ToInt32(numericUpDown4.Value); // Number of times to sell the caught fish

                FishingLocationMessages.TellFishingLocation(selectedLocation); // Provide location-specific messages
                var result = MessageBox.Show("Make sure you're in the fishing dock before pressing OK!",
                    "Ready to Fish?", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                if (result != DialogResult.OK)
                    return;

                await _fishingService.StartFishing(selectedLocation, numberOfCasts, numberOfSells, randomFishingCheckBox.Checked, token, "", autoDetectFishCheckBox.Checked);
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

        private void customShowOverlayCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (customShowOverlayCheckBox.Checked)
            {
                // Create and show the overlay
                if (_fishingOverlay == null || _fishingOverlay.IsDisposed)
                {
                    _fishingOverlay = new FishingOverlayForm();
                }
                _fishingOverlay.Show();
                _fishingOverlay.SetStatus("Overlay active - waiting for custom fishing...");

                // Connect overlay to fishing strategy
                Services.FishingLocationsWalking.FishingStrategyBase.Overlay = _fishingOverlay;

                // Set callback to auto-uncheck when fishing ends
                Services.FishingLocationsWalking.FishingStrategyBase.OnFishingEnded = OnCustomFishingEndedCallback;
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

        private void OnCustomFishingEndedCallback()
        {
            // This runs on a background thread, so we need to invoke on the UI thread
            if (InvokeRequired)
            {
                Invoke(new Action(OnCustomFishingEndedCallback));
                return;
            }

            // Uncheck the overlay checkbox which will trigger cleanup
            if (customShowOverlayCheckBox.Checked)
            {
                customShowOverlayCheckBox.Checked = false;
            }
        }

        private void EditScanAreaBtn_Click(object sender, EventArgs e)
        {
            string selectedLocation = fishingLocationscomboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedLocation))
            {
                MessageBox.Show("Please select a fishing location first.", "No Location Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Show explanation before opening
            var result = MessageBox.Show(
                "This will open a fullscreen overlay on the game window where you can adjust the scan area.\n\n" +
                "How to use:\n" +
                "• Drag the corners/edges to resize the green rectangle\n" +
                "• Drag the center to move the entire area\n" +
                "• Press ENTER or click 'Save' to save changes\n" +
                "• Press ESC or click 'Cancel' to exit without saving\n\n" +
                "Make sure Toontown is running and visible before continuing.",
                "Scan Area Calibration",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (result != DialogResult.OK)
                return;

            // Get the default scan area for this location
            var detector = new Utilities.FishBubbleDetector(selectedLocation);
            var defaultScanArea = detector.GetDefaultScanArea();

            if (defaultScanArea.IsEmpty)
            {
                MessageBox.Show($"No scan area defined for location: {selectedLocation}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Open the calibration form
            using (var calibrationForm = new ScanAreaCalibrationForm(selectedLocation, defaultScanArea))
            {
                calibrationForm.ShowDialog();

                if (calibrationForm.WasSaved)
                {
                    MessageBox.Show($"Custom scan area saved for '{selectedLocation}'.\n\n" +
                        $"New dimensions: {calibrationForm.ResultScanArea.Width} x {calibrationForm.ResultScanArea.Height}",
                        "Scan Area Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void CalibrateColorsBtn_Click(object sender, EventArgs e)
        {
            string selectedLocation = fishingLocationscomboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedLocation))
            {
                MessageBox.Show("Please select a fishing location first.", "No Location Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Show explanation before opening
            var result = MessageBox.Show(
                "This will open a calibration window to set the pond water and fish shadow colors.\n\n" +
                "How to use:\n" +
                "• Click on the pond water to sample the water color\n" +
                "• Click on a fish shadow to sample the shadow color\n" +
                "• Use the sliders to adjust color tolerance\n" +
                "• Click 'Save' when done, or 'Cancel' to exit\n\n" +
                "Make sure Toontown is running and you can see the pond.",
                "Pond Color Calibration",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (result != DialogResult.OK)
                return;

            // Open the color calibration form
            using (var colorForm = new PondColorCalibrationForm(selectedLocation))
            {
                colorForm.ShowDialog();
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

        private async void button5_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Press OK when ready to begin!");
            Thread.Sleep(5000);
            Point test = CoreFunctionality.getCursorLocation();
            CoreFunctionality.GetColorAt(test.X, test.Y);
            Debug.WriteLine("HEX: " + CoreFunctionality.HexConverter(CoreFunctionality.GetColorAt(test.X, test.Y)) + " RGB: " + CoreFunctionality.GetColorAt(test.X, test.Y));
            MessageBox.Show("Done");

            CoreFunctionality.FocusTTRWindow();

            Image screenshot = ImageRecognition.GetWindowScreenshot();
            string redFishingButton = "#FD0000";
            await ImageRecognition.locateColorInImage(screenshot, redFishingButton, 10);
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

                // Hide "Number of Sells" controls when Fish Anywhere or Estate is selected
                // (no sell cycle - no fisherman at these locations)
                bool showSellsControls = selectedLocation != FishingLocationNames.FishAnywhere
                    && selectedLocation != FishingLocationNames.EstateLeftDock;
                label4.Visible = showSellsControls;
                numericUpDown4.Visible = showSellsControls;
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

        /// <summary>
        /// Starts custom fishing with the selected action file.
        /// </summary>
        private async void startCustomFishingBtn_Click(object sender, EventArgs e)
        {
            // Reset the CancellationTokenSource if it's null or was previously cancelled
            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
            }

            var token = _cancellationTokenSource.Token;

            // Set the fishing settings from Custom Fishing tab UI controls
            FishingStrategyBase.BiteTimeoutSeconds = Convert.ToInt32(customNumericUpDownBiteTimeout.Value);
            FishingStrategyBase.WaitForFishBeforeCasting = customWaitForFishCheckBox.Checked && customAutoDetectFishCheckBox.Checked;
            FishingStrategyBase.MaxFishWaitAttempts = 10; // Default value for custom fishing

            try
            {
                string selectedFileName = customFishingFilesComboBox.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedFileName))
                {
                    MessageBox.Show("Please select a custom fishing action file.", "No File Selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int numberOfCasts = Convert.ToInt32(numericUpDownCustomCasts.Value);
                int numberOfSells = Convert.ToInt32(numericUpDownCustomSells.Value);

                var result = MessageBox.Show("Make sure you're at the fishing dock before pressing OK!",
                    "Ready to Fish?", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                if (result != DialogResult.OK)
                    return;

                string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string filePath = Path.Combine(exePath, "Custom Fishing Actions", selectedFileName);

                // Decide whether to debug custom actions or perform them normally
                if (debugCustomActionsCheckBox.Checked)
                {
                    await _fishingService.StartCustomFishingDebugging(filePath + ".json", token);
                }
                else
                {
                    await _fishingService.StartFishing("CUSTOM FISHING ACTION", numberOfCasts, numberOfSells,
                        randomFishingCheckBox.Checked, token, filePath + ".json", customAutoDetectFishCheckBox.Checked);
                }
            }
            catch (TaskCanceledException)
            {
                MessageBox.Show("Custom fishing was cancelled.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }

        /// <summary>
        /// Stops the current custom fishing operation.
        /// </summary>
        private void stopCustomFishingBtn_Click(object sender, EventArgs e)
        {
            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                MessageBox.Show("Custom fishing is not currently in progress.", "Not Running",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _cancellationTokenSource.Cancel();
            MessageBox.Show("Custom fishing stopped!", "Stopped", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Opens the scan area calibration form for custom fishing locations.
        /// </summary>
        private void customScanAreaBtn_Click(object sender, EventArgs e)
        {
            // Show explanation before opening
            var result = MessageBox.Show(
                "This will open a fullscreen overlay on the game window where you can adjust the scan area.\n\n" +
                "How to use:\n" +
                "• Drag the corners/edges to resize the green rectangle\n" +
                "• Drag the center to move the entire area\n" +
                "• Press ENTER or click 'Save' to save changes\n" +
                "• Press ESC or click 'Cancel' to exit without saving\n\n" +
                "Make sure Toontown is running and visible before continuing.",
                "Scan Area Calibration",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (result != DialogResult.OK)
                return;

            // Use "CUSTOM FISHING ACTION" as the location for custom fishing calibration
            var detector = new Utilities.FishBubbleDetector("CUSTOM FISHING ACTION");
            var defaultScanArea = detector.GetDefaultScanArea();

            if (defaultScanArea.IsEmpty)
            {
                MessageBox.Show("No scan area defined for custom fishing.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var calibrationForm = new ScanAreaCalibrationForm("CUSTOM FISHING ACTION", defaultScanArea))
            {
                calibrationForm.ShowDialog();

                if (calibrationForm.WasSaved)
                {
                    MessageBox.Show($"Custom scan area saved.\n\n" +
                        $"New dimensions: {calibrationForm.ResultScanArea.Width} x {calibrationForm.ResultScanArea.Height}",
                        "Scan Area Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// Opens the pond color calibration form for custom fishing locations.
        /// </summary>
        private void customPondColorsBtn_Click(object sender, EventArgs e)
        {
            // Show explanation before opening
            var result = MessageBox.Show(
                "This will open a calibration window to set the pond water and fish shadow colors.\n\n" +
                "How to use:\n" +
                "• Click on the pond water to sample the water color\n" +
                "• Click on a fish shadow to sample the shadow color\n" +
                "• Use the sliders to adjust color tolerance\n" +
                "• Click 'Save' when done, or 'Cancel' to exit\n\n" +
                "Make sure Toontown is running and you can see the pond.",
                "Pond Color Calibration",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (result != DialogResult.OK)
                return;

            // Use "CUSTOM FISHING ACTION" as the location for custom fishing calibration
            using (var colorForm = new PondColorCalibrationForm("CUSTOM FISHING ACTION"))
            {
                colorForm.ShowDialog();
            }
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

            // Get shot summary to show helpful instructions
            var summary = GolfService.GetShotSummaryFromFile(filePath);

            // Build instruction message
            string positionInstruction = summary.RequiresPositionChange
                ? $"⚠️ YOU must stand on the {summary.Position.ToUpper()} tee spot BEFORE clicking OK!"
                : "Stand on the CENTER tee spot (default position)";

            var result = MessageBox.Show(
                $"Course: {selectedFileName}\n\n" +
                $"━━━ YOU DO (before clicking OK) ━━━\n" +
                $"1. {positionInstruction}\n" +
                $"2. Make sure your swing key is set to CTRL\n\n" +
                $"━━━ BOT WILL DO ━━━\n" +
                $"• Aim: {summary.Aim}\n" +
                $"• Power: ~{summary.Power}%\n\n" +
                $"After clicking OK, switch to TTR within {summary.DelaySeconds} seconds.\n\n" +
                "Ready to start?",
                "Golf Setup - " + selectedFileName,
                MessageBoxButtons.OKCancel,
                summary.RequiresPositionChange ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

            if (result != DialogResult.OK)
                return;

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                bool showOverlay = showGolfOverlayCheckBox.Checked;
                await GolfService.StartCustomGolfAction(filePath, _cancellationTokenSource.Token, showOverlay, selectedFileName);
                GolfService.HideOverlay();
                CoreFunctionality.BringBotWindowToFront();
                MessageBox.Show("Golf actions completed successfully.", "Golf Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                GolfService.HideOverlay();
                MessageBox.Show("Golf actions were cancelled.");
            }
            catch (Exception ex)
            {
                GolfService.HideOverlay();
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private bool _isAutoGolfRunning = false;

        private async void startAutoGolfBtn_Click(object sender, EventArgs e)
        {
            if (_isAutoGolfRunning)
            {
                // Cancel running auto-golf
                _cancellationTokenSource?.Cancel();
                startAutoGolfBtn.Text = "Auto Golf";
                autoGolfStatusLabel.Text = "Cancelled";
                _isAutoGolfRunning = false;
                return;
            }

            // Remind user about auto-golf setup
            var result = MessageBox.Show(
                "━━━ AUTO GOLF MODE ━━━\n\n" +
                "The bot will automatically:\n" +
                "1. Detect which hole you're playing\n" +
                "2. Wait for your turn\n" +
                "3. Aim and swing with the right power\n" +
                "4. Repeat for all 3 holes\n\n" +
                "━━━ YOU MUST DO ━━━\n" +
                "• Set swing key to CTRL (default)\n" +
                "• Keep TTR visible on screen\n" +
                "• ⚠️ Move to LEFT or RIGHT tee when instructed!\n" +
                "  (Watch the overlay for position instructions)\n\n" +
                "━━━ HOW IT WORKS ━━━\n" +
                "The bot reads the hole name from the screen.\n" +
                "Some holes require LEFT or RIGHT positioning -\n" +
                "YOU must move there before your turn!\n\n" +
                "Ready to start?",
                "Auto Golf Setup",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);

            if (result != DialogResult.OK)
                return;

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _isAutoGolfRunning = true;
            startAutoGolfBtn.Text = "Stop";
            autoGolfStatusLabel.Text = "Starting...";

            // Subscribe to status updates
            GolfService.AutoGolfStatusChanged += OnAutoGolfStatusChanged;

            try
            {
                bool showOverlay = showGolfOverlayCheckBox.Checked;

                // Run on background thread to avoid blocking UI
                // Templates will be auto-prompted if missing
                await Task.Run(() => GolfService.StartContinuousAutoGolfAsync(_cancellationTokenSource.Token, showOverlay));
            }
            catch (OperationCanceledException)
            {
                autoGolfStatusLabel.Text = "Stopped";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Auto-golf error: {ex.Message}");
                autoGolfStatusLabel.Text = "Error";
            }
            finally
            {
                GolfService.AutoGolfStatusChanged -= OnAutoGolfStatusChanged;
                GolfService.HideOverlay();
                startAutoGolfBtn.Text = "Auto Golf";
                _isAutoGolfRunning = false;
            }
        }

        private void OnAutoGolfStatusChanged(object sender, AutoGolfStatusEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnAutoGolfStatusChanged(sender, e)));
                return;
            }

            // Update the status label
            string statusText = e.Status;
            if (!string.IsNullOrEmpty(e.DetectedCourse))
            {
                statusText = $"{e.DetectedCourse}";
            }
            autoGolfStatusLabel.Text = statusText;
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

            // Show shot summary at the top
            var summary = GolfService.GetShotSummary(actions);
            golfActionsListBox.Items.Add("═══════ SHOT SUMMARY ═══════");
            golfActionsListBox.Items.Add($"  Position: {summary.Position}" + (summary.RequiresPositionChange ? " ⚠️" : ""));
            golfActionsListBox.Items.Add($"  Aim: {summary.Aim}");
            golfActionsListBox.Items.Add($"  Power: ~{summary.Power}%");
            golfActionsListBox.Items.Add($"  Delay: {summary.DelaySeconds} seconds");
            golfActionsListBox.Items.Add("═══════════════════════════");
            golfActionsListBox.Items.Add("");

            foreach (var action in actions)
            {
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

        #region Settings Tab Event Handlers

        /// <summary>
        /// Refreshes the preferences list display.
        /// </summary>
        private void btnRefreshPreferences_Click(object sender, EventArgs e)
        {
            RefreshPreferencesDisplay();
        }

        /// <summary>
        /// Saves current preferences immediately.
        /// </summary>
        private void btnSavePreferencesNow_Click(object sender, EventArgs e)
        {
            SaveUserPreferences();
            RefreshPreferencesDisplay();
            MessageBox.Show("Preferences saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Opens the preferences file in the default editor.
        /// </summary>
        private void btnOpenPreferencesFile_Click(object sender, EventArgs e)
        {
            string filePath = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                "user_preferences.json");

            if (!File.Exists(filePath))
            {
                // Create the file first if it doesn't exist
                SaveUserPreferences();
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open preferences file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Resets all preferences to default values.
        /// </summary>
        private void btnResetPreferences_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all preferences to default values?\n\nThis cannot be undone.",
                "Reset Preferences",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                UserPreferences.Instance.ResetToDefaults();
                LoadUserPreferences();
                RefreshPreferencesDisplay();
                MessageBox.Show("Preferences have been reset to defaults.", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Refreshes the preferences list box with current saved values.
        /// </summary>
        private void RefreshPreferencesDisplay()
        {
            preferencesListBox.Items.Clear();

            var prefs = UserPreferences.Instance;

            preferencesListBox.Items.Add("═══════ FISHING ═══════");
            preferencesListBox.Items.Add($"  Location: {(string.IsNullOrEmpty(prefs.FishingLocation) ? "(not set)" : prefs.FishingLocation)}");
            preferencesListBox.Items.Add($"  Number of Casts: {prefs.NumberOfCasts}");
            preferencesListBox.Items.Add($"  Number of Sells: {prefs.NumberOfSells}");
            preferencesListBox.Items.Add($"  Bite Timeout: {prefs.BiteTimeoutSeconds} seconds");
            preferencesListBox.Items.Add($"  Random Variance: {(prefs.RandomVariance ? "Yes" : "No")}");
            preferencesListBox.Items.Add($"  Auto Detect Fish: {(prefs.AutoDetectFish ? "Yes" : "No")}");
            preferencesListBox.Items.Add($"  Wait For Fish: {(prefs.WaitForFishBeforeCasting ? $"Yes ({prefs.MaxFishWaitAttempts} tries)" : "No")}");

            preferencesListBox.Items.Add("");
            preferencesListBox.Items.Add("═══════ GOLF ═══════");
            preferencesListBox.Items.Add($"  Course: {(string.IsNullOrEmpty(prefs.GolfCourse) ? "(not set)" : prefs.GolfCourse)}");
            preferencesListBox.Items.Add($"  Show Overlay: {(prefs.ShowGolfOverlay ? "Yes" : "No")}");

            preferencesListBox.Items.Add("");
            preferencesListBox.Items.Add("═══════ DOODLES ═══════");
            preferencesListBox.Items.Add($"  Trick: {prefs.DoodleTrick}");
            preferencesListBox.Items.Add($"  Feeds: {prefs.NumberOfFeeds}");
            preferencesListBox.Items.Add($"  Scratches: {prefs.NumberOfScratches}");

            preferencesListBox.Items.Add("");
            preferencesListBox.Items.Add("═══════ GARDENING ═══════");
            preferencesListBox.Items.Add($"  Water Count: {prefs.WaterPlantCount}");

            preferencesListBox.Items.Add("");
            preferencesListBox.Items.Add("═══════ MISC ═══════");
            preferencesListBox.Items.Add($"  Keep On Top: {(prefs.KeepProgramOnTop ? "Yes" : "No")}");
            preferencesListBox.Items.Add($"  Keep Awake Minutes: {prefs.KeepToonAwakeMinutes}");
        }

        #endregion
    }
}
