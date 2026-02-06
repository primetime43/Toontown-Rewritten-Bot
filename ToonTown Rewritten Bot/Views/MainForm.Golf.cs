using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Views;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm
    {
        private void createCustomGolfActionsBtn_Click(object sender, EventArgs e)
        {
            using (var form = new CustomGolfActions())
            {
                form.ShowDialog(); // This will block until the form is closed
            }

            LoadCustomActions("Golf", customGolfFilesComboBox); // load golf actions after the form is closed
        }

        private void wizardCustomGolfBtn_Click(object sender, EventArgs e)
        {
            using (var form = new CustomGolfWizardForm())
            {
                form.ShowDialog();
            }

            LoadCustomActions("Golf", customGolfFilesComboBox); // Reload after wizard closes
        }

        private async void startGolfBtn_Click(object sender, EventArgs e)
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

        private void devOpenConfigBtn_Click(object sender, EventArgs e)
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
