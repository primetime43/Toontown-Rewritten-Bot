using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm
    {
        //important functions for bot
        private void startSpamButton_Click(object sender, EventArgs e)//spam message on screen
        {//if the user presses ALT key, it will break the loop
            bool loopBroken = BotFunctions.SendMessage(messageToType.Text, Convert.ToInt32(numericUpDownSpamCount.Value), spamMessageCheckBox.Checked, numericUpDownSpamCount);
        }

        private int timeLeft;
        private bool isToonAwakeActive = false;  // Flag to track if the function is active
        private async void startKeepToonAwakeButton_Click(object sender, EventArgs e)
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Dispose();  // Dispose any existing token source
            }
            _cancellationTokenSource = new CancellationTokenSource();
            isToonAwakeActive = true;  // Flag to indicate the task is active

            int timeInSeconds = Convert.ToInt32(numericUpDownAwakeMinutes.Value) * 60;  // Convert minutes to seconds
            timeLeft = timeInSeconds;  // Set timeLeft for countdown
            MessageBox.Show("Press OK when ready to begin!");

            timer1.Start();  // Start the countdown timer

            try
            {
                await BotFunctions.KeepToonAwake(timeInSeconds, _cancellationTokenSource.Token);

                CoreFunctionality.BringBotWindowToFront();
                MessageBox.Show("Keep Toon Awake completed successfully!", "Keep Awake Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                // Cancelled by user via stop button or Escape/F12
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                timer1.Stop();
                timeLeft = 0;
                isToonAwakeActive = false;
                awakeCountdownLabel.Text = "Status: Idle";
                awakeCountdownLabel.ForeColor = System.Drawing.Color.Gray;
            }
        }

        //misc functions for bot
        private void spamMessageCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDownSpamCount.Visible = spamMessageCheckBox.Checked;
            miscSpamTimesLabel.Visible = spamMessageCheckBox.Checked;
        }

        private void keepOnTopCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TopMost = keepOnTopCheckBox.Checked;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (timeLeft > 0)
            {
                timeLeft = timeLeft - 1;
                awakeCountdownLabel.Text = timeLeft + " seconds";
                awakeCountdownLabel.ForeColor = System.Drawing.Color.DarkGreen;
            }
            else
            {
                timer1.Stop();
                awakeCountdownLabel.Text = "Status: Idle";
                awakeCountdownLabel.ForeColor = System.Drawing.Color.Gray;
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
                awakeCountdownLabel.Text = "Status: Idle";
                awakeCountdownLabel.ForeColor = System.Drawing.Color.Gray;

                MessageBox.Show("Keep Toon Awake stopped!", "Keep Toon Awake Stopped", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // If the function was not active, show a different message
                MessageBox.Show("No active 'Keep Toon Awake' function to stop.", "Stop Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
