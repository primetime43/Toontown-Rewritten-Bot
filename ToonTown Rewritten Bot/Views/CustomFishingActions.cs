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
                    // Add the time in milliseconds to the ListBox
                    actionItemsListBox.Items.Add($"{selectedItem} ({timeInMilliseconds} milliseconds)");
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
        }

        private void removeItemBtn_Click(object sender, EventArgs e)
        {
            actionItemsListBox.Items.Remove(actionItemsListBox.SelectedItem);
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
                    // Extract just the digits from the time value (handles various formats)
                    command = new string(actionText.Where(char.IsDigit).ToArray());
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

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(actionsList, Newtonsoft.Json.Formatting.Indented);
            SaveToJsonFile(json);
        }

        private void SaveToJsonFile(string jsonContent)
        {
            string folderPath = (string)CoreFunctionality.ManageCustomActionsFolder("Fishing", false);  // Getting the folder path only

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON File|*.json",
                Title = "Save an Actions JSON File",
                InitialDirectory = folderPath
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, jsonContent);
                MessageBox.Show($"Actions saved to {saveFileDialog.FileName}", "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                string json = File.ReadAllText(openFileDialog.FileName);
                var actionsList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<FishingActionCommand>>(json);

                actionItemsListBox.Items.Clear();
                foreach (var actionCommand in actionsList)
                {
                    string displayText;
                    if (actionCommand.Action == "TIME")
                    {
                        // Extract just the number from the command (handles "500", "500 milliseconds", "500)", etc.)
                        string timeValue = new string(actionCommand.Command.Where(char.IsDigit).ToArray());
                        displayText = $"TIME ({timeValue} milliseconds)";
                    }
                    else
                    {
                        displayText = actionCommand.Action;
                    }
                    actionItemsListBox.Items.Add(displayText);
                }
            }
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

                    // Extract the numeric value (time in seconds)
                    string timeValue = new String(selectedItem.Where(char.IsDigit).ToArray());

                    // Set the extracted time into textBox1
                    actionTimeTxtBox.Text = timeValue;
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
                    // Update the item in the ListBox with the new time in milliseconds
                    actionItemsListBox.Items[selectedIndex] = $"{selectedItem} ({timeInMilliseconds} milliseconds)";
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
        }

        #region Walk Path Recorder

        private void btnStartRecording_Click(object sender, EventArgs e)
        {
            // Clear previous recording
            _recordedKeys.Clear();
            _currentKeyHeld = null;
            _keyDownTime = 0;

            // Update UI
            btnStartRecording.Enabled = false;
            btnStopRecording.Enabled = true;
            btnAddSellFish.Enabled = true;
            lblRecordingStatus.Text = "Status: Recording... Press arrow keys in TTR";
            lblRecordingStatus.ForeColor = Color.Green;

            // Start recording
            _isRecording = true;
            _recordingStopwatch.Restart();
            _keyboardHook.Start();

            // Minimize this window so user can switch to TTR
            this.WindowState = FormWindowState.Minimized;
        }

        private void btnStopRecording_Click(object sender, EventArgs e)
        {
            // Finalize any key still being held
            FinalizeCurrentKey();

            StopRecordingCleanup();

            // Convert recorded keys to action items
            ConvertRecordedKeysToActionItems();

            // Update UI
            lblRecordingStatus.Text = $"Status: Stopped - {_recordedKeys.Count} actions recorded";
            lblRecordingStatus.ForeColor = Color.Blue;

            // Restore window
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
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

            // Provide feedback
            lblRecordingStatus.Text = "Status: Recording... SELL FISH added!";
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

                // Update status
                this.BeginInvoke(new Action(() =>
                {
                    lblRecordingStatus.Text = $"Status: Recording... {_recordedKeys.Count} actions";
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
            return key switch
            {
                Keys.Up => "WALK FORWARDS",
                Keys.Down => "WALK BACKWARDS",
                Keys.Left => "TURN LEFT",
                Keys.Right => "TURN RIGHT",
                _ => null
            };
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
                    // Add the duration as a TIME action
                    actionItemsListBox.Items.Add($"TIME ({recorded.DurationMs} milliseconds)");
                }
            }
        }

        #endregion
    }
}
