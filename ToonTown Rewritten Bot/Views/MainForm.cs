using System;
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

            // Set version and author from global settings
            mainVersionLabel.Text = $"v{GlobalSettings.ApplicationInfo.Version}";
            mainAuthorLabel.Text = $"by {GlobalSettings.ApplicationInfo.Author}";

            // Hide Racing tab
            tabControl1.TabPages.Remove(Racing);

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

            // Load custom actions for Golf, Custom Fishing, and Gardening tabs
            LoadCustomActions("Golf", customGolfFilesComboBox);
            LoadCustomActions("Fishing", customFishingFilesComboBox);
            LoadCustomActions("Gardening", customGardeningFilesComboBox);

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
            numericUpDownCasts.Value = Math.Max(numericUpDownCasts.Minimum, Math.Min(numericUpDownCasts.Maximum, prefs.NumberOfCasts));
            numericUpDownSells.Value = Math.Max(numericUpDownSells.Minimum, Math.Min(numericUpDownSells.Maximum, prefs.NumberOfSells));
            numericUpDownBiteTimeout.Value = Math.Max(numericUpDownBiteTimeout.Minimum, Math.Min(numericUpDownBiteTimeout.Maximum, prefs.BiteTimeoutSeconds));
            randomFishingCheckBox.Checked = prefs.RandomVariance;
            autoDetectFishCheckBox.Checked = prefs.AutoDetectFish;
            waitForFishCheckBox.Checked = prefs.WaitForFishBeforeCasting;
            numericUpDownWaitAttempts.Value = Math.Max(numericUpDownWaitAttempts.Minimum, Math.Min(numericUpDownWaitAttempts.Maximum, prefs.MaxFishWaitAttempts));
            showOverlayCheckBox.Checked = prefs.ShowFishingOverlay;

            // Custom Fishing preferences
            if (!string.IsNullOrEmpty(prefs.CustomFishingFile))
            {
                int customFishingIndex = customFishingFilesComboBox.FindStringExact(prefs.CustomFishingFile);
                if (customFishingIndex >= 0) customFishingFilesComboBox.SelectedIndex = customFishingIndex;
            }
            numericUpDownCustomCasts.Value = Math.Max(numericUpDownCustomCasts.Minimum, Math.Min(numericUpDownCustomCasts.Maximum, prefs.CustomFishingCasts));
            numericUpDownCustomSells.Value = Math.Max(numericUpDownCustomSells.Minimum, Math.Min(numericUpDownCustomSells.Maximum, prefs.CustomFishingSells));
            customAutoDetectFishCheckBox.Checked = prefs.CustomAutoDetectFish;
            customWaitForFishCheckBox.Checked = prefs.CustomWaitForFish;
            customShowOverlayCheckBox.Checked = prefs.CustomShowOverlay;
            customNumericUpDownBiteTimeout.Value = Math.Max(customNumericUpDownBiteTimeout.Minimum, Math.Min(customNumericUpDownBiteTimeout.Maximum, prefs.CustomBiteTimeoutSeconds));

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
                // Convert old "X Bean Plant" format to new "X Bean(s)" format for the dropdown
                string beanAmount = prefs.FlowerBeanAmount;
                int beanIndex = -1;
                for (int i = 0; i < beanCountComboBox.Items.Count; i++)
                {
                    string item = beanCountComboBox.Items[i].ToString();
                    if (beanAmount.StartsWith(item.Split(' ')[0]))
                    {
                        beanIndex = i;
                        break;
                    }
                }
                if (beanIndex >= 0) beanCountComboBox.SelectedIndex = beanIndex;
            }
            if (!string.IsNullOrEmpty(prefs.SelectedFlower))
            {
                int flowerIndex = flowerComboBox.FindStringExact(prefs.SelectedFlower);
                if (flowerIndex >= 0) flowerComboBox.SelectedIndex = flowerIndex;
            }
            if (!string.IsNullOrEmpty(prefs.CustomGardeningFile))
            {
                int gardenIndex = customGardeningFilesComboBox.FindStringExact(prefs.CustomGardeningFile);
                if (gardenIndex >= 0) customGardeningFilesComboBox.SelectedIndex = gardenIndex;
            }

            // Misc preferences
            keepOnTopCheckBox.Checked = prefs.KeepProgramOnTop;
            numericUpDownAwakeMinutes.Value = Math.Max(numericUpDownAwakeMinutes.Minimum, Math.Min(numericUpDownAwakeMinutes.Maximum, prefs.KeepToonAwakeMinutes));
        }

        /// <summary>
        /// Saves current UI values to user preferences.
        /// </summary>
        private void SaveUserPreferences()
        {
            var prefs = UserPreferences.Instance;

            // Fishing preferences
            prefs.FishingLocation = fishingLocationscomboBox.SelectedItem?.ToString() ?? "";
            prefs.NumberOfCasts = (int)numericUpDownCasts.Value;
            prefs.NumberOfSells = (int)numericUpDownSells.Value;
            prefs.BiteTimeoutSeconds = (int)numericUpDownBiteTimeout.Value;
            prefs.RandomVariance = randomFishingCheckBox.Checked;
            prefs.AutoDetectFish = autoDetectFishCheckBox.Checked;
            prefs.WaitForFishBeforeCasting = waitForFishCheckBox.Checked;
            prefs.MaxFishWaitAttempts = (int)numericUpDownWaitAttempts.Value;
            prefs.ShowFishingOverlay = showOverlayCheckBox.Checked;

            // Custom Fishing preferences
            prefs.CustomFishingFile = customFishingFilesComboBox.SelectedItem?.ToString() ?? "";
            prefs.CustomFishingCasts = (int)numericUpDownCustomCasts.Value;
            prefs.CustomFishingSells = (int)numericUpDownCustomSells.Value;
            prefs.CustomAutoDetectFish = customAutoDetectFishCheckBox.Checked;
            prefs.CustomWaitForFish = customWaitForFishCheckBox.Checked;
            prefs.CustomShowOverlay = customShowOverlayCheckBox.Checked;
            prefs.CustomBiteTimeoutSeconds = (int)customNumericUpDownBiteTimeout.Value;

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
            prefs.FlowerBeanAmount = beanCountComboBox.SelectedItem?.ToString() ?? "";
            prefs.SelectedFlower = flowerComboBox.SelectedItem?.ToString() ?? "";
            prefs.CustomGardeningFile = customGardeningFilesComboBox.SelectedItem?.ToString() ?? "";

            // Misc preferences
            prefs.KeepProgramOnTop = keepOnTopCheckBox.Checked;
            prefs.KeepToonAwakeMinutes = (int)numericUpDownAwakeMinutes.Value;

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
    }
}
