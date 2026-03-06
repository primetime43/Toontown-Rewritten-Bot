using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Services.FishingLocationsWalking;
using ToonTown_Rewritten_Bot.Utilities;
using ToonTown_Rewritten_Bot.Views;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm
    {
        private void createCustomFishingActionsBtn_Click(object sender, EventArgs e)
        {
            using (var form = new CustomFishingActions())
            {
                form.ShowDialog(); // This will block until the form is closed
            }
            LoadCustomActions("Fishing", customFishingFilesComboBox); // load fishing actions after the form is closed
        }

        /// <summary>
        /// Opens the guided wizard for creating custom fishing action files.
        /// </summary>
        private void wizardCustomFishingBtn_Click(object sender, EventArgs e)
        {
            using (var wizard = new CustomFishingWizardForm())
            {
                var result = wizard.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrEmpty(wizard.SavedFileName))
                {
                    // Reload the combo box and select the new file
                    LoadCustomActions("Fishing", customFishingFilesComboBox);

                    // Try to select the newly created file
                    int index = customFishingFilesComboBox.FindStringExact(wizard.SavedFileName);
                    if (index >= 0)
                    {
                        customFishingFilesComboBox.SelectedIndex = index;
                    }
                }
            }
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
            FishingStrategyBase.MaxFishWaitSeconds = 20; // Default value for custom fishing
            FishingStrategyBase.QuickCasting = quickCastingCheckBox.Checked;

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

                string exePath = AppPaths.ExeDirectory;
                string filePath = Path.Combine(exePath, "Custom Fishing Actions", selectedFileName);

                // Show overlay if the checkbox is checked
                if (customShowOverlayCheckBox.Checked)
                    SetFishingOverlay(true, "Custom fishing...", OnCustomFishingEndedCallback);

                await _fishingService.StartFishing("CUSTOM FISHING ACTION", numberOfCasts, numberOfSells,
                    randomFishingCheckBox.Checked, token, filePath + ".json", customAutoDetectFishCheckBox.Checked);

                // These run on the UI thread (await resumes on UI context)
                SetFishingOverlay(false, null, null);
                CoreFunctionality.BringBotWindowToFront();
                int casts = _fishingService.SessionCastCount;
                MessageBox.Show(
                    $"Done Fishing with custom action '{selectedFileName}'.\n\nTotal Casts: {casts}",
                    "Fishing Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (TaskCanceledException)
            {
                Logger.Info("Fishing", "Session end: reason=\"User cancelled\", casts completed=" + _fishingService.SessionCastCount);
                SetFishingOverlay(false, null, null);
                MessageBox.Show("Custom fishing was cancelled.");
            }
            catch (Exception ex)
            {
                Logger.Error("Fishing", $"Session end: reason=\"Error: {ex.Message}\"");
                SetFishingOverlay(false, null, null);
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

            Logger.Info("Fishing", "User pressed Stop button (custom fishing)");
            _cancellationTokenSource.Cancel();
            MessageBox.Show("Custom fishing stopped!", "Stopped", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
