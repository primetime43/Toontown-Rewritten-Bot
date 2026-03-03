using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm
    {
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
                AppPaths.ExeDirectory,
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
            preferencesListBox.Items.Add("═══════ CUSTOM FISHING ═══════");
            preferencesListBox.Items.Add($"  Action File: {(string.IsNullOrEmpty(prefs.CustomFishingFile) ? "(not set)" : prefs.CustomFishingFile)}");
            preferencesListBox.Items.Add($"  Number of Casts: {prefs.CustomFishingCasts}");
            preferencesListBox.Items.Add($"  Number of Sells: {prefs.CustomFishingSells}");
            preferencesListBox.Items.Add($"  Bite Timeout: {prefs.CustomBiteTimeoutSeconds} seconds");
            preferencesListBox.Items.Add($"  Auto Detect Fish: {(prefs.CustomAutoDetectFish ? "Yes" : "No")}");
            preferencesListBox.Items.Add($"  Wait For Fish: {(prefs.CustomWaitForFish ? "Yes" : "No")}");
            preferencesListBox.Items.Add($"  Show Overlay: {(prefs.CustomShowOverlay ? "Yes" : "No")}");

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
            preferencesListBox.Items.Add($"  Bean Count: {(string.IsNullOrEmpty(prefs.FlowerBeanAmount) ? "(not set)" : prefs.FlowerBeanAmount)}");
            preferencesListBox.Items.Add($"  Selected Flower: {(string.IsNullOrEmpty(prefs.SelectedFlower) ? "(not set)" : prefs.SelectedFlower)}");
            preferencesListBox.Items.Add($"  Water Count: {prefs.WaterPlantCount}");
            preferencesListBox.Items.Add($"  Custom File: {(string.IsNullOrEmpty(prefs.CustomGardeningFile) ? "(not set)" : prefs.CustomGardeningFile)}");

            preferencesListBox.Items.Add("");
            preferencesListBox.Items.Add("═══════ MISC ═══════");
            preferencesListBox.Items.Add($"  Keep On Top: {(prefs.KeepProgramOnTop ? "Yes" : "No")}");
            preferencesListBox.Items.Add($"  Keep Awake Minutes: {prefs.KeepToonAwakeMinutes}");

            preferencesListBox.Items.Add("");
            preferencesListBox.Items.Add("═══════ LOGGING ═══════");
            preferencesListBox.Items.Add($"  Log Level: {prefs.LogLevel}");
            preferencesListBox.Items.Add($"  Log Directory: {Utilities.Logger.Instance.LogDirectory}");
        }

        #endregion
    }
}
