using System;
using System.Drawing;
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
    public partial class MainForm
    {
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
            FishingStrategyBase.QuickCasting = quickCastingCheckBox.Checked;

            try
            {
                string selectedLocation = (string)fishingLocationscomboBox.SelectedItem; // Retrieve the location selected by the user
                int numberOfCasts = Convert.ToInt32(numericUpDownCasts.Value); // Number of times to cast the line
                int numberOfSells = Convert.ToInt32(numericUpDownSells.Value); // Number of times to sell the caught fish

                FishingLocationMessages.TellFishingLocation(selectedLocation); // Provide location-specific messages
                var result = MessageBox.Show("Make sure you're in the fishing dock before pressing OK!",
                    "Ready to Fish?", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                if (result != DialogResult.OK)
                    return;

                fishingStatusLabel.Text = "Status: Fishing...";
                fishingStatusLabel.ForeColor = System.Drawing.Color.DarkGreen;

                // Show overlay if the checkbox is checked
                if (showOverlayCheckBox.Checked)
                    SetFishingOverlay(true, "Fishing...", OnFishingEndedCallback);

                await _fishingService.StartFishing(selectedLocation, numberOfCasts, numberOfSells, randomFishingCheckBox.Checked, token, "", autoDetectFishCheckBox.Checked);

                // These run on the UI thread (await resumes on UI context)
                fishingStatusLabel.Text = "Status: Idle";
                fishingStatusLabel.ForeColor = System.Drawing.Color.Gray;
                SetFishingOverlay(false, null, null);
                CoreFunctionality.BringBotWindowToFront();
                int fish = _fishingService.SessionFishCaught;
                int casts = _fishingService.SessionCastCount;
                int pct = casts > 0 ? (int)Math.Round(100.0 * fish / casts) : 0;
                MessageBox.Show(
                    $"Done Fishing in '{selectedLocation}'.\n\n" +
                    $"Fish Caught: {fish}/{casts} ({pct}% catch rate)",
                    "Fishing Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (TaskCanceledException)
            {
                fishingStatusLabel.Text = "Status: Idle";
                fishingStatusLabel.ForeColor = System.Drawing.Color.Gray;
                SetFishingOverlay(false, null, null);
                MessageBox.Show("Fishing was cancelled."); // Handle cancellation of the task
            }
            catch (Exception ex) // Catch any other unforeseen errors
            {
                fishingStatusLabel.Text = "Status: Idle";
                fishingStatusLabel.ForeColor = System.Drawing.Color.Gray;
                SetFishingOverlay(false, null, null);
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

        private void stopFishingBtn_Click(object sender, EventArgs e)//button to stop fishing
        {
            // Check if the operation is already canceled or not started
            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                MessageBox.Show("Fishing is not currently in progress.");
                return;
            }

            // Signal the cancellation
            _cancellationTokenSource.Cancel();
            fishingStatusLabel.Text = "Status: Idle";
            fishingStatusLabel.ForeColor = System.Drawing.Color.Gray;
            MessageBox.Show("Fishing stopped!");
        }

        private void ShowOverlayCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // Checkbox is a passive setting — overlay is shown/hidden when fishing starts/stops
        }

        private void QuickCastingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (quickCastingCheckBox.Checked)
            {
                waitForFishCheckBox.Checked = false;
                waitForFishCheckBox.Enabled = false;
            }
            else
            {
                waitForFishCheckBox.Enabled = true;
            }
        }

        private void customShowOverlayCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // Checkbox is a passive setting — overlay is shown/hidden when custom fishing starts/stops
        }

        private void SetFishingOverlay(bool enabled, string statusMessage, Action onEndedCallback)
        {
            if (enabled)
            {
                if (_fishingOverlay == null || _fishingOverlay.IsDisposed)
                {
                    _fishingOverlay = new FishingOverlayForm();
                }
                _fishingOverlay.Show();
                _fishingOverlay.SetStatus(statusMessage);

                Services.FishingLocationsWalking.FishingStrategyBase.Overlay = _fishingOverlay;
                Services.FishingLocationsWalking.FishingStrategyBase.OnFishingEnded = onEndedCallback;
            }
            else
            {
                Services.FishingLocationsWalking.FishingStrategyBase.OnFishingEnded = null;
                Services.FishingLocationsWalking.FishingStrategyBase.Overlay = null;

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

            SetFishingOverlay(false, null, null);
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
        /// Called when fishing ends to close the overlay.
        /// </summary>
        private void OnFishingEndedCallback()
        {
            // Must invoke on UI thread since this is called from fishing task
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnFishingEndedCallback()));
                return;
            }

            SetFishingOverlay(false, null, null);
        }

        private async void startRacing_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Press OK when ready to begin!");
            Thread.Sleep(5000);
            Point test = CoreFunctionality.getCursorLocation();
            CoreFunctionality.GetColorAt(test.X, test.Y);
            System.Diagnostics.Debug.WriteLine("HEX: " + CoreFunctionality.HexConverter(CoreFunctionality.GetColorAt(test.X, test.Y)) + " RGB: " + CoreFunctionality.GetColorAt(test.X, test.Y));
            MessageBox.Show("Done");

            CoreFunctionality.FocusTTRWindow();

            Image screenshot = ImageRecognition.GetWindowScreenshot();
            string redFishingButton = "#FD0000";
            await ImageRecognition.locateColorInImage(screenshot, redFishingButton, 10);
        }

        private void fishingLocationscomboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Ensure there's a selected item to avoid NullReferenceException
            if (fishingLocationscomboBox.SelectedItem != null)
            {
                string selectedLocation = fishingLocationscomboBox.SelectedItem.ToString();
                fishingLocationDescLabel.Text = FishingLocationMessages.GetLocationMessage(selectedLocation);

                // Hide "Number of Sells" controls when Fish Anywhere is selected
                // (no sell cycle - no fisherman at that location)
                bool showSellsControls = selectedLocation != FishingLocationNames.FishAnywhere;
                labelSells.Visible = showSellsControls;
                numericUpDownSells.Visible = showSellsControls;
            }
            else
                fishingLocationDescLabel.Text = "Select a location to see its description.";
        }
    }
}
