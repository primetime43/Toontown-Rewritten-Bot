using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm
    {
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

        private bool isTrainingActive = false;
        private Views.DoodleOverlayForm _doodleOverlay;

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
                _cancellationTokenSource.Dispose();
            }
            _cancellationTokenSource = new CancellationTokenSource();
            isTrainingActive = true;
            doodleStatusLabel.Text = "Status: Training...";
            doodleStatusLabel.ForeColor = System.Drawing.Color.DarkGreen;

            // Show overlay
            if (_doodleOverlay == null || _doodleOverlay.IsDisposed)
            {
                _doodleOverlay = new Views.DoodleOverlayForm();
            }
            DoodleTraining.Overlay = _doodleOverlay;
            _doodleOverlay.Show();
            _doodleOverlay.UpdateProgress(numberOfFeeds, numberOfScratches, unlimitedCheckBox, selectedTrick);

            try
            {
                // Run the training task
                await Task.Run(() => new DoodleTraining().StartDoodleTraining(
                    numberOfFeeds, numberOfScratches, unlimitedCheckBox,
                    selectedTrick, justFeed, justScratch, _cancellationTokenSource.Token),
                    _cancellationTokenSource.Token);

                // Training completed successfully
                isTrainingActive = false;
                doodleStatusLabel.Text = "Status: Complete";
                doodleStatusLabel.ForeColor = System.Drawing.Color.DarkBlue;
                CoreFunctionality.BringBotWindowToFront();
                MessageBox.Show("Doodle training completed successfully!", "Training Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                isTrainingActive = false;
                doodleStatusLabel.Text = "Status: Stopped";
                doodleStatusLabel.ForeColor = System.Drawing.Color.DarkRed;
                CoreFunctionality.BringBotWindowToFront();
                MessageBox.Show("Doodle Training stopped!", "Training Stopped", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                isTrainingActive = false;
                doodleStatusLabel.Text = "Status: Error";
                doodleStatusLabel.ForeColor = System.Drawing.Color.DarkRed;
                CoreFunctionality.BringBotWindowToFront();
                MessageBox.Show($"Error occurred during doodle training: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isTrainingActive = false;
                doodleStatusLabel.Text = "Status: Idle";
                doodleStatusLabel.ForeColor = System.Drawing.Color.Gray;

                // Close overlay
                DoodleTraining.Overlay = null;
                if (_doodleOverlay != null && !_doodleOverlay.IsDisposed)
                {
                    _doodleOverlay.Close();
                    _doodleOverlay.Dispose();
                    _doodleOverlay = null;
                }
            }
        }

        private void stopDoodleTrainingBtn_Click(object sender, EventArgs e)
        {
            // Check if the cancellation token source is created and the training is active
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested && isTrainingActive)
            {
                _cancellationTokenSource.Cancel();  // Request cancellation; disposal handled by start handler's finally block
                isTrainingActive = false;  // Clear the flag

                doodleStatusLabel.Text = "Status: Stopped";
                doodleStatusLabel.ForeColor = System.Drawing.Color.DarkRed;
                MessageBox.Show("Doodle Training stopped!", "Training Stopped", MessageBoxButtons.OK, MessageBoxIcon.Information);
                doodleStatusLabel.Text = "Status: Idle";
                doodleStatusLabel.ForeColor = System.Drawing.Color.Gray;
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
    }
}
