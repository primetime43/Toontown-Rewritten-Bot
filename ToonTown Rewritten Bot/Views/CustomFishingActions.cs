using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    public partial class CustomFishingActions : Form
    {
        // Recording fields
        private GlobalKeyboardHook _keyboardHook;
        private bool _isRecording = false;
        private List<RecordedKeyPress> _recordedKeys = new List<RecordedKeyPress>();
        private Stopwatch _recordingStopwatch = new Stopwatch();
        private Keys? _currentKeyHeld = null;
        private long _keyDownTime = 0;

        // Calibration fields
        private string _currentFilePath = null;
        private CustomFishingActionFile _currentFile = null;

        // Helper class to store recorded key presses with timing
        private class RecordedKeyPress
        {
            public string Action { get; set; }
            public long DurationMs { get; set; }
            public bool IsSellFish { get; set; }
        }

        public CustomFishingActions()
        {
            InitializeComponent();
            _keyboardHook = new GlobalKeyboardHook();
            _keyboardHook.KeyPressed += OnGlobalKeyPressed;
            _keyboardHook.KeyReleased += OnGlobalKeyReleased;
            lblRecorderHelp.Text = $"Start recording, then walk in TTR using {GameControls.GetMovementBindingSummary()}. Add Sell at the bucket, then stop.";
            UpdatePathPreview();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            StopRecordingCleanup();
            if (_keyboardHook != null)
            {
                _keyboardHook.KeyPressed -= OnGlobalKeyPressed;
                _keyboardHook.KeyReleased -= OnGlobalKeyReleased;
                _keyboardHook.Dispose();
            }
        }

        private void addItemBtn_Click(object sender, EventArgs e)
        {
            string selectedItem = comboBox1.SelectedItem?.ToString() ?? "";
            if (selectedItem == "TIME")
            {
                // Now parse the time input as milliseconds
                if (int.TryParse(actionTimeTxtBox.Text, out int timeInMilliseconds))
                {
                    // Add the time in formatted display to the ListBox
                    actionItemsListBox.Items.Add(FormatTimeDisplayItem(timeInMilliseconds));
                    actionTimeTxtBox.Clear(); // Optionally clear the TextBox after adding
                }
                else
                {
                    MessageBox.Show("Please enter a valid time in milliseconds.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (!string.IsNullOrEmpty(selectedItem))
            {
                // For other selections, just add the selected item directly
                actionItemsListBox.Items.Add(selectedItem);
            }
            else
            {
                MessageBox.Show("Please select an item from the ComboBox.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            UpdatePathPreview();
        }

        /// <summary>
        /// Formats a time value for display in the list (e.g., "TIME (0.8s)" instead of "TIME (847 milliseconds)")
        /// </summary>
        private string FormatTimeDisplayItem(long milliseconds)
        {
            return $"TIME ({DurationFormatter.FormatSeconds(milliseconds)})";
        }

        /// <summary>
        /// Extracts milliseconds from a display string like "TIME (0.8s)" or "TIME (800 milliseconds)"
        /// </summary>
        private int ExtractMillisecondsFromDisplay(string displayText)
        {
            if (string.IsNullOrEmpty(displayText) || !displayText.StartsWith("TIME"))
                return 0;

            // Extract the part in parentheses
            int start = displayText.IndexOf('(');
            int end = displayText.IndexOf(')');
            if (start >= 0 && end > start)
            {
                string timeStr = displayText.Substring(start + 1, end - start - 1);
                return DurationFormatter.ParseToMilliseconds(timeStr);
            }

            // Fallback: extract digits only
            string digits = new string(displayText.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out int ms))
                return ms;

            return 0;
        }

        private void removeItemBtn_Click(object sender, EventArgs e)
        {
            actionItemsListBox.Items.Remove(actionItemsListBox.SelectedItem);
            UpdatePathPreview();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem.ToString() == "TIME")
                actionTimeTxtBox.Enabled = true;
            else
                actionTimeTxtBox.Enabled = false;
        }

        private void actionTimeTxtBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private FishingActionKeys _fishingActionKeys = new FishingActionKeys();
        private void saveActionItemBtn_Click(object sender, EventArgs e)
        {
            List<FishingActionCommand> actionsList = new List<FishingActionCommand>();

            foreach (var item in actionItemsListBox.Items)
            {
                string actionText = item.ToString();
                string action, command;

                if (actionText.StartsWith("TIME"))
                {
                    action = "TIME";
                    // Extract milliseconds from the new display format (e.g., "TIME (0.8s)")
                    int ms = ExtractMillisecondsFromDisplay(actionText);
                    command = ms.ToString();
                }
                else
                {
                    action = actionText;
                    command = _fishingActionKeys.GetKeyCodeString(actionText);
                    if (string.IsNullOrEmpty(command))
                    {
                        command = "UNKNOWN"; // Handle the case where no key code is found for the action
                    }
                }

                actionsList.Add(new FishingActionCommand { Action = action, Command = command });
            }

            // Build the v2 file format with embedded calibration
            if (_currentFile == null)
                _currentFile = new CustomFishingActionFile();

            _currentFile.Actions = actionsList;

            SaveActionFile();
        }

        private void SaveActionFile()
        {
            string folderPath = (string)CoreFunctionality.ManageCustomActionsFolder("Fishing", false);

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON File|*.json",
                Title = "Save an Actions JSON File",
                InitialDirectory = folderPath,
                FileName = !string.IsNullOrEmpty(_currentFilePath) ? Path.GetFileName(_currentFilePath) : ""
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Update name from filename
                _currentFile.Name = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                bool success = CustomFishingActionFileManager.Save(_currentFile, saveFileDialog.FileName);
                if (success)
                {
                    _currentFilePath = saveFileDialog.FileName;
                    MessageBox.Show($"Actions saved to {saveFileDialog.FileName}", "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Failed to save file.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void loadActionItemBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON File|*.json",
                Title = "Open an Actions JSON File",
                InitialDirectory = (string)CoreFunctionality.ManageCustomActionsFolder("Fishing", false)
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadActionsFromFile(openFileDialog.FileName);
            }
        }

        /// <summary>
        /// Loads actions from a file path, supporting both v1 and v2 formats.
        /// </summary>
        private void LoadActionsFromFile(string filePath)
        {
            var result = CustomFishingActionFileManager.Load(filePath);
            if (!result.Success)
            {
                MessageBox.Show($"Failed to load file: {result.ErrorMessage}", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Store current file for calibration updates
            _currentFilePath = filePath;
            _currentFile = result.File;

            actionItemsListBox.Items.Clear();
            foreach (var actionCommand in result.File.Actions)
            {
                string displayText;
                if (actionCommand.Action == "TIME")
                {
                    // Convert to new display format (e.g., "TIME (0.8s)")
                    int ms = DurationFormatter.ParseToMilliseconds(actionCommand.Command);
                    displayText = FormatTimeDisplayItem(ms);
                }
                else
                {
                    displayText = actionCommand.Action;
                }
                actionItemsListBox.Items.Add(displayText);
            }
            UpdatePathPreview();
            UpdateCalibrationStatus();
        }

        private void actionItemsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Check if an item is actually selected
            if (actionItemsListBox.SelectedItem != null)
            {
                updateSelectedActionItemBtn.Enabled = true;
                string selectedItem = actionItemsListBox.SelectedItem.ToString();

                // Check if the selected item is a "TIME" entry
                if (selectedItem.StartsWith("TIME"))
                {
                    // Select "TIME" in comboBox1 if available
                    comboBox1.SelectedItem = "TIME";

                    // The editor accepts milliseconds, so convert the formatted display
                    // value back to milliseconds before populating the input.
                    actionTimeTxtBox.Text = ExtractMillisecondsFromDisplay(selectedItem).ToString();
                }
                else
                {
                    // For non-time actions, find and select the action in comboBox1
                    comboBox1.SelectedItem = selectedItem;

                    // Since it's not a time action, disable or clear textBox1
                    actionTimeTxtBox.Clear();
                    actionTimeTxtBox.Enabled = false;
                }
            }
        }

        private void updateSelectedActionItemBtn_Click(object sender, EventArgs e)
        {
            if (actionItemsListBox.SelectedItem == null)
            {
                MessageBox.Show("No item is selected to update.");
                return;
            }

            int selectedIndex = actionItemsListBox.SelectedIndex;
            string selectedItem = comboBox1.SelectedItem?.ToString() ?? "";

            if (selectedItem == "TIME")
            {
                if (int.TryParse(actionTimeTxtBox.Text, out int timeInMilliseconds))
                {
                    // Update the item in the ListBox with the new formatted time
                    actionItemsListBox.Items[selectedIndex] = FormatTimeDisplayItem(timeInMilliseconds);
                }
                else
                {
                    MessageBox.Show("Please enter a valid time in milliseconds.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if (!string.IsNullOrEmpty(selectedItem))
            {
                // Update the item with the new action from the comboBox
                actionItemsListBox.Items[selectedIndex] = selectedItem;
            }
            else
            {
                MessageBox.Show("Please select a valid action.", "No Action Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            UpdatePathPreview();
        }

        #region Walk Path Recorder

        private void btnStartRecording_Click(object sender, EventArgs e)
        {
            // Show countdown overlay to give user time to switch to TTR
            lblRecordingStatus.Text = "Status: Starting countdown...";
            lblRecordingStatus.ForeColor = Color.Orange;

            // Minimize this window first
            this.WindowState = FormWindowState.Minimized;

            // Show countdown overlay
            bool completed = CountdownOverlayForm.ShowCountdown(5);

            if (!completed)
            {
                // User cancelled
                this.WindowState = FormWindowState.Normal;
                lblRecordingStatus.Text = "Status: Recording cancelled";
                lblRecordingStatus.ForeColor = Color.Gray;
                return;
            }

            if (!_keyboardHook.Start())
            {
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
                lblRecordingStatus.Text = "Status: Could not start keyboard recording";
                lblRecordingStatus.ForeColor = Color.Red;
                MessageBox.Show(
                    this,
                    $"The keyboard recorder could not start (Windows error {_keyboardHook.LastErrorCode}).\n\n" +
                    "Restart the bot and make sure the bot and Toontown are running at the same administrator level.",
                    "Recording Unavailable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Clear previous recording
            _recordedKeys.Clear();
            _currentKeyHeld = null;
            _keyDownTime = 0;

            // Update UI
            btnStartRecording.Enabled = false;
            btnStopRecording.Enabled = true;
            btnAddSellFish.Enabled = true;
            lblRecordingStatus.Text = $"Status: Recording... Use {GameControls.GetMovementBindingSummary()}";
            lblRecordingStatus.ForeColor = Color.Green;
            lblLivePreview.Text = "";

            // Start recording
            _isRecording = true;
            _recordingStopwatch.Restart();
        }

        private void btnStopRecording_Click(object sender, EventArgs e)
        {
            // Finalize any key still being held
            FinalizeCurrentKey();

            StopRecordingCleanup();

            // Convert recorded keys to action items
            ConvertRecordedKeysToActionItems();

            // Restore before showing any feedback so warnings cannot appear behind a minimized owner.
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();

            // Update UI
            if (_recordedKeys.Count == 0)
            {
                lblRecordingStatus.Text = "Status: No actions recorded";
                lblRecordingStatus.ForeColor = Color.Red;
                MessageBox.Show(
                    this,
                    $"No movement keys were recorded. Use the configured keys ({GameControls.GetMovementBindingSummary()}) and make sure the bot and Toontown run at the same administrator level.",
                    "Nothing Recorded",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else
            {
                lblRecordingStatus.Text = $"Status: Stopped - {_recordedKeys.Count} actions recorded";
                lblRecordingStatus.ForeColor = Color.Blue;
            }

        }

        private void btnAddSellFish_Click(object sender, EventArgs e)
        {
            if (!_isRecording)
                return;

            // Finalize any key currently being held before adding sell fish
            FinalizeCurrentKey();

            // Add a sell fish marker
            _recordedKeys.Add(new RecordedKeyPress
            {
                Action = "SELL FISH",
                DurationMs = 0,
                IsSellFish = true
            });

            // Provide feedback and update live preview
            lblRecordingStatus.Text = "Status: Recording... SELL FISH added!";
            UpdateLivePreview();
        }

        private void OnGlobalKeyPressed(object sender, Keys key)
        {
            if (!_isRecording)
                return;

            // Only track arrow keys (movement keys in TTR)
            string action = GetActionFromKey(key);
            if (action == null)
                return;

            long currentTime = _recordingStopwatch.ElapsedMilliseconds;

            // If a different key is pressed, finalize the previous key
            if (_currentKeyHeld.HasValue && _currentKeyHeld.Value != key)
            {
                FinalizeCurrentKey();
            }

            // If this is a new key press (not a repeat of the held key)
            if (!_currentKeyHeld.HasValue)
            {
                _currentKeyHeld = key;
                _keyDownTime = currentTime;

                // Update status to show which key is being held
                this.BeginInvoke(new Action(() =>
                {
                    lblRecordingStatus.Text = $"Status: Recording... Holding {action}";
                }));
            }
            // If same key, this is a key repeat - ignore it
        }

        private void OnGlobalKeyReleased(object sender, Keys key)
        {
            if (!_isRecording)
                return;

            // Only process if this is the key we're currently tracking
            if (_currentKeyHeld.HasValue && _currentKeyHeld.Value == key)
            {
                FinalizeCurrentKey();

                // Update status and live preview
                this.BeginInvoke(new Action(() =>
                {
                    lblRecordingStatus.Text = $"Status: Recording... {_recordedKeys.Count} actions";
                    UpdateLivePreview();
                }));
            }
        }

        private void FinalizeCurrentKey()
        {
            if (!_currentKeyHeld.HasValue)
                return;

            long currentTime = _recordingStopwatch.ElapsedMilliseconds;
            long duration = currentTime - _keyDownTime;

            // Only add if there was meaningful duration (at least 50ms to filter accidental presses)
            if (duration >= 50)
            {
                string action = GetActionFromKey(_currentKeyHeld.Value);
                if (action != null)
                {
                    _recordedKeys.Add(new RecordedKeyPress
                    {
                        Action = action,
                        DurationMs = duration,
                        IsSellFish = false
                    });
                }
            }

            _currentKeyHeld = null;
            _keyDownTime = 0;
        }

        private string GetActionFromKey(Keys key)
        {
            return GameControls.GetMovementAction((int)(key & Keys.KeyCode));
        }

        private void StopRecordingCleanup()
        {
            _isRecording = false;
            _keyboardHook.Stop();
            _recordingStopwatch.Stop();

            // Update UI
            btnStartRecording.Enabled = true;
            btnStopRecording.Enabled = false;
            btnAddSellFish.Enabled = false;
        }

        private void ConvertRecordedKeysToActionItems()
        {
            // Ask user if they want to append or replace
            DialogResult result = DialogResult.Yes;
            if (actionItemsListBox.Items.Count > 0)
            {
                result = MessageBox.Show(
                    "Do you want to REPLACE existing actions?\n\nYes = Replace all\nNo = Append to existing\nCancel = Discard recording",
                    "Add Recorded Actions",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
            }

            if (result == DialogResult.Cancel)
                return;

            if (result == DialogResult.Yes)
                actionItemsListBox.Items.Clear();

            // Add each recorded action to the list
            foreach (var recorded in _recordedKeys)
            {
                if (recorded.IsSellFish)
                {
                    actionItemsListBox.Items.Add("SELL FISH");
                }
                else
                {
                    // Add the movement action
                    actionItemsListBox.Items.Add(recorded.Action);
                    // Add the duration as a TIME action with new formatting
                    actionItemsListBox.Items.Add(FormatTimeDisplayItem(recorded.DurationMs));
                }
            }
            UpdatePathPreview();
        }

        /// <summary>
        /// Updates the live preview label during recording to show the path as it's recorded.
        /// </summary>
        private void UpdateLivePreview()
        {
            if (!_isRecording || _recordedKeys.Count == 0)
            {
                lblLivePreview.Text = "";
                return;
            }

            var preview = new StringBuilder("Recording: ");
            int maxItems = 10; // Limit how many items to show
            int startIndex = Math.Max(0, _recordedKeys.Count - maxItems);

            if (startIndex > 0)
            {
                preview.Append("... ");
            }

            for (int i = startIndex; i < _recordedKeys.Count; i++)
            {
                var recorded = _recordedKeys[i];
                if (recorded.IsSellFish)
                {
                    preview.Append("[SELL] ");
                }
                else
                {
                    string arrow = DurationFormatter.GetDirectionArrow(recorded.Action);
                    string time = DurationFormatter.FormatSeconds(recorded.DurationMs);
                    preview.Append($"{arrow} {time} ");
                }
            }

            lblLivePreview.Text = preview.ToString().TrimEnd();
        }

        /// <summary>
        /// Updates the path preview group box with a visual representation of the current actions.
        /// Format: ↓ 0.8s → ← 1.0s → ↑ 0.7s [SELL] ↓ 0.8s
        /// </summary>
        private void UpdatePathPreview()
        {
            if (actionItemsListBox.Items.Count == 0)
            {
                lblPathPreview.Text = "(No actions)";
                return;
            }

            var preview = new StringBuilder();
            string lastAction = null;
            int lastTimeMs = 0;

            for (int i = 0; i < actionItemsListBox.Items.Count; i++)
            {
                string item = actionItemsListBox.Items[i].ToString();

                if (item.StartsWith("TIME"))
                {
                    // This is a duration for the previous action
                    lastTimeMs = ExtractMillisecondsFromDisplay(item);

                    // Add the previous action with its time
                    if (!string.IsNullOrEmpty(lastAction))
                    {
                        string arrow = DurationFormatter.GetDirectionArrow(lastAction);
                        string time = DurationFormatter.FormatSeconds(lastTimeMs);
                        preview.Append($"{arrow} {time} ");
                    }
                    lastAction = null;
                }
                else if (item == "SELL FISH")
                {
                    // First add any pending action
                    if (!string.IsNullOrEmpty(lastAction))
                    {
                        string arrow = DurationFormatter.GetDirectionArrow(lastAction);
                        preview.Append($"{arrow} ");
                    }
                    preview.Append("[SELL] ");
                    lastAction = null;
                }
                else
                {
                    // This is a movement action - save for when we get its TIME
                    // First add any pending action without time
                    if (!string.IsNullOrEmpty(lastAction))
                    {
                        string arrow = DurationFormatter.GetDirectionArrow(lastAction);
                        preview.Append($"{arrow} ");
                    }
                    lastAction = item;
                }
            }

            // Add any trailing action without time
            if (!string.IsNullOrEmpty(lastAction))
            {
                string arrow = DurationFormatter.GetDirectionArrow(lastAction);
                preview.Append($"{arrow} ");
            }

            string result = preview.ToString().TrimEnd();
            lblPathPreview.Text = string.IsNullOrEmpty(result) ? "(No actions)" : result;
        }

        #endregion

        #region Calibration

        /// <summary>
        /// Updates the calibration status label based on the current file's calibration data.
        /// </summary>
        private void UpdateCalibrationStatus()
        {
            if (_currentFile?.Calibration != null)
            {
                bool hasScanArea = _currentFile.Calibration.ScanArea != null;
                bool hasPondColors = _currentFile.Calibration.PondColors != null;

                if (hasScanArea && hasPondColors)
                {
                    lblCalibrationStatus.Text = "✓ Scan area and pond colors calibrated";
                    lblCalibrationStatus.ForeColor = Color.Green;
                }
                else if (hasScanArea)
                {
                    lblCalibrationStatus.Text = "✓ Scan area calibrated (no pond colors)";
                    lblCalibrationStatus.ForeColor = Color.DarkOrange;
                }
                else if (hasPondColors)
                {
                    lblCalibrationStatus.Text = "✓ Pond colors calibrated (no scan area)";
                    lblCalibrationStatus.ForeColor = Color.DarkOrange;
                }
                else
                {
                    lblCalibrationStatus.Text = "No calibration data (will use global settings)";
                    lblCalibrationStatus.ForeColor = Color.Gray;
                }
            }
            else
            {
                lblCalibrationStatus.Text = "No calibration data (will use global settings)";
                lblCalibrationStatus.ForeColor = Color.Gray;
            }
        }

        private void btnCalibrateScanArea_Click(object sender, EventArgs e)
        {
            // Ensure we have a file to save to
            if (_currentFile == null)
            {
                _currentFile = new CustomFishingActionFile();
            }

            // Use the same approach as the wizard - show explanation first
            var result = MessageBox.Show(
                "This will open a fullscreen overlay on the game window where you can adjust the scan area.\n\n" +
                "Make sure Toontown is running and visible before continuing.",
                "Scan Area Calibration",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (result != DialogResult.OK)
                return;

            // Use "CUSTOM FISHING ACTION" as the location for custom fishing calibration
            var detector = new FishBubbleDetector("CUSTOM FISHING ACTION");
            var windowRect = CoreFunctionality.GetGameWindowRect();
            var customScanArea = windowRect.IsEmpty ? null
                : CustomScanAreaManager.GetCustomScanArea("CUSTOM FISHING ACTION", windowRect.Width, windowRect.Height);
            var scanArea = customScanArea ?? detector.GetDefaultScanArea();

            if (scanArea.IsEmpty)
            {
                MessageBox.Show("No scan area defined for custom fishing.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var calibrationForm = new ScanAreaCalibrationForm("CUSTOM FISHING ACTION", scanArea))
            {
                calibrationForm.ShowDialog();

                if (calibrationForm.WasSaved)
                {
                    // Get screen dimensions for percentage calculation (same as wizard)
                    var screenBounds = Screen.PrimaryScreen.Bounds;
                    var rect = calibrationForm.ResultScanArea;

                    // Store calibration in the current file as percentages
                    if (_currentFile.Calibration == null)
                        _currentFile.Calibration = new CalibrationData();

                    _currentFile.Calibration.ScanArea = new ScanAreaCalibration
                    {
                        XPercent = (float)((rect.X / (double)screenBounds.Width) * 100),
                        YPercent = (float)((rect.Y / (double)screenBounds.Height) * 100),
                        WidthPercent = (float)((rect.Width / (double)screenBounds.Width) * 100),
                        HeightPercent = (float)((rect.Height / (double)screenBounds.Height) * 100)
                    };

                    UpdateCalibrationStatus();
                    MessageBox.Show("Scan area calibrated!\n\nRemember to Save the action file to keep this calibration.",
                        "Calibration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void btnCalibratePondColors_Click(object sender, EventArgs e)
        {
            // Ensure we have a file to save to
            if (_currentFile == null)
            {
                _currentFile = new CustomFishingActionFile();
            }

            // Use "CUSTOM FISHING ACTION" as the location for custom fishing calibration
            using (var colorForm = new PondColorCalibrationForm("CUSTOM FISHING ACTION"))
            {
                colorForm.ShowDialog();

                // Same approach as wizard - read back from PondColorManager after save
                if (colorForm.WasSaved)
                {
                    var colors = PondColorManager.GetPondColors("CUSTOM FISHING ACTION");
                    if (colors != null)
                    {
                        if (_currentFile.Calibration == null)
                            _currentFile.Calibration = new CalibrationData();

                        _currentFile.Calibration.PondColors = new PondColorCalibration
                        {
                            ShadowR = colors.ShadowColor.R,
                            ShadowG = colors.ShadowColor.G,
                            ShadowB = colors.ShadowColor.B,
                            WaterR = colors.WaterColor.R,
                            WaterG = colors.WaterColor.G,
                            WaterB = colors.WaterColor.B,
                            ToleranceR = colors.ToleranceR,
                            ToleranceG = colors.ToleranceG,
                            ToleranceB = colors.ToleranceB
                        };

                        UpdateCalibrationStatus();
                        MessageBox.Show("Pond colors calibrated!\n\nRemember to Save the action file to keep this calibration.",
                            "Calibration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        #endregion
    }
}
