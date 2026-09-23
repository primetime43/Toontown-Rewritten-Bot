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
            if (_fishingSessionActive) return;
            string selected = customFishingFilesComboBox.SelectedItem?.ToString();
            using var form = new CustomFishingActions(string.IsNullOrEmpty(selected) ? null :
                Path.Combine(CustomFishingActionFileManager.GetCustomActionsFolder(), selected + ".json"));
            form.ShowDialog(this);
            RefreshFishingRouteSelection(form.SavedFileName ?? selected);
        }

        /// <summary>
        /// Opens the guided wizard for creating custom fishing action files.
        /// </summary>
        private void wizardCustomFishingBtn_Click(object sender, EventArgs e)
        {
            if (_fishingSessionActive) return;
            using var wizard = new CustomFishingWizardForm();
            wizard.ShowDialog(this);
            RefreshFishingRouteSelection(wizard.SavedFileName ?? customFishingFilesComboBox.SelectedItem?.ToString());
        }

        private void RefreshFishingRouteSelection(string name)
        {
            LoadCustomActions("Fishing", customFishingFilesComboBox);
            if (name == null) return;
            int index = customFishingFilesComboBox.FindStringExact(name);
            if (index >= 0) customFishingFilesComboBox.SelectedIndex = index;
        }

        /// <summary>
        /// Starts custom fishing with the selected action file.
        /// </summary>
        private async void startCustomFishingBtn_Click(object sender, EventArgs e)
        {
            if (_fishingSessionActive) return;
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
                SetFishingSessionActive(true);
                string selectedFileName = customFishingFilesComboBox.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedFileName))
                {
                    MessageBox.Show("Please select a custom fishing action file.", "No File Selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int numberOfCasts = Convert.ToInt32(numericUpDownCustomCasts.Value);
                int numberOfSells = Convert.ToInt32(numericUpDownCustomSells.Value);

                // Prompt user to calibrate scan area and pond colors if not set
                if (customAutoDetectFishCheckBox.Checked)
                {
                    if (!PromptCalibrationIfNeeded("CUSTOM FISHING ACTION"))
                        return;
                }

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
            catch (OperationCanceledException) when (token.IsCancellationRequested)
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
            finally
            {
                SetFishingSessionActive(false);
            }
        }

        /// <summary>
        /// Stops the current custom fishing operation.
        /// </summary>
        private void stopCustomFishingBtn_Click(object sender, EventArgs e)
        {
            if (!_fishingSessionActive || _cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                MessageBox.Show("Custom fishing is not currently in progress.", "Not Running",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Logger.Info("Fishing", "User pressed Stop button (custom fishing)");
            _cancellationTokenSource.Cancel();
        }
    }
}
