using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    public partial class CustomGolfActions : Form
    {
        private GolfActionKeys _golfActionKeys = new GolfActionKeys();
        private string _currentFilePath = null;
        private CustomGolfActionFile _currentFile = null;

        public CustomGolfActions()
        {
            InitializeComponent();
            UpdatePreview();
            UpdateSummary();
        }

        private void addItemBtn_Click(object sender, EventArgs e)
        {
            string selectedItem = comboBox1.SelectedItem?.ToString() ?? "";

            if (string.IsNullOrEmpty(selectedItem))
            {
                MessageBox.Show("Please select an action from the dropdown.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (int.TryParse(actionTimeTxtBox.Text, out int timeInMilliseconds) && timeInMilliseconds > 0)
            {
                // Use formatted display with meaning
                string displayText = FormatActionDisplay(selectedItem, timeInMilliseconds);
                actionItemsListBox.Items.Add(displayText);
                actionTimeTxtBox.Clear();
                UpdatePreview();
                UpdateSummary();
            }
            else
            {
                MessageBox.Show("Please enter a valid duration in milliseconds (must be greater than 0).", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Formats an action for display with meaningful context.
        /// </summary>
        private string FormatActionDisplay(string action, int durationMs)
        {
            string meaning = GolfDurationFormatter.FormatDuration(action, durationMs);
            return $"{action} - {meaning}";
        }

        /// <summary>
        /// Parses action name and duration from display format.
        /// </summary>
        private (string action, int duration) ParseActionDisplay(string displayText)
        {
            // Format: "ACTION NAME - 1800ms (72% power)" or "ACTION NAME - 100ms (small turn)"
            int dashIndex = displayText.IndexOf(" - ");
            if (dashIndex <= 0)
            {
                // Old format: "ACTION NAME (123 ms)"
                int parenIndex = displayText.LastIndexOf('(');
                if (parenIndex > 0)
                {
                    string actionName = displayText.Substring(0, parenIndex).Trim();
                    string durationStr = new string(displayText.Substring(parenIndex).Where(char.IsDigit).ToArray());
                    if (int.TryParse(durationStr, out int dur))
                        return (actionName, dur);
                }
                return (displayText.Trim(), 0);
            }

            string action = displayText.Substring(0, dashIndex).Trim();
            string rest = displayText.Substring(dashIndex + 3);

            // Extract just the milliseconds number
            string msStr = "";
            foreach (char c in rest)
            {
                if (char.IsDigit(c))
                    msStr += c;
                else if (msStr.Length > 0)
                    break; // Stop at first non-digit after we started collecting
            }

            int duration = 0;
            int.TryParse(msStr, out duration);

            return (action, duration);
        }

        private void removeItemBtn_Click(object sender, EventArgs e)
        {
            if (actionItemsListBox.SelectedItem != null)
            {
                actionItemsListBox.Items.Remove(actionItemsListBox.SelectedItem);
                UpdatePreview();
                UpdateSummary();
            }
        }

        private void updateSelectedActionItemBtn_Click(object sender, EventArgs e)
        {
            if (actionItemsListBox.SelectedItem == null)
            {
                MessageBox.Show("No item is selected to update.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedIndex = actionItemsListBox.SelectedIndex;
            string selectedItem = comboBox1.SelectedItem?.ToString() ?? "";

            if (string.IsNullOrEmpty(selectedItem))
            {
                MessageBox.Show("Please select an action from the dropdown.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (int.TryParse(actionTimeTxtBox.Text, out int timeInMilliseconds) && timeInMilliseconds > 0)
            {
                actionItemsListBox.Items[selectedIndex] = FormatActionDisplay(selectedItem, timeInMilliseconds);
                UpdatePreview();
                UpdateSummary();
            }
            else
            {
                MessageBox.Show("Please enter a valid duration in milliseconds (must be greater than 0).", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void loadActionItemBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON File|*.json",
                Title = "Open an Actions JSON File",
                InitialDirectory = (string)CoreFunctionality.ManageCustomActionsFolder("Golf", false)
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadActionsFromFile(openFileDialog.FileName);
            }
        }

        private void LoadActionsFromFile(string filePath)
        {
            var result = CustomGolfActionFileManager.Load(filePath);
            if (!result.Success)
            {
                MessageBox.Show($"Failed to load file: {result.ErrorMessage}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _currentFilePath = filePath;
            _currentFile = result.File;

            actionItemsListBox.Items.Clear();
            foreach (var action in result.File.Actions)
            {
                int duration = action.Duration > 0 ? action.Duration : 1000;
                actionItemsListBox.Items.Add(FormatActionDisplay(action.Action, duration));
            }

            UpdatePreview();
            UpdateSummary();
            MessageBox.Show($"Loaded {result.File.Actions.Count} actions.", "Load Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void saveActionItemBtn_Click(object sender, EventArgs e)
        {
            if (actionItemsListBox.Items.Count == 0)
            {
                MessageBox.Show("No actions to save. Please add some actions first.", "No Actions", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var actionsList = BuildActionsList();
            if (actionsList == null) return;

            // Build v2 file
            if (_currentFile == null)
                _currentFile = new CustomGolfActionFile();

            _currentFile.Actions = actionsList;

            SaveActionFile();
        }

        private List<GolfActionCommand> BuildActionsList()
        {
            List<GolfActionCommand> actionsList = new List<GolfActionCommand>();

            foreach (var item in actionItemsListBox.Items)
            {
                string actionText = item.ToString();
                var (actionName, duration) = ParseActionDisplay(actionText);

                if (duration <= 0)
                {
                    MessageBox.Show($"Invalid duration in: {actionText}", "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }

                // Validate action name (except DELAY TIME which doesn't need a key)
                if (actionName != "DELAY TIME" && !_golfActionKeys.ActionKeyMap.ContainsKey(actionName))
                {
                    MessageBox.Show($"Unknown action: {actionName}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }

                actionsList.Add(new GolfActionCommand
                {
                    Action = actionName,
                    Command = actionName,
                    Duration = duration
                });
            }

            return actionsList;
        }

        private void SaveActionFile()
        {
            string folderPath = (string)CoreFunctionality.ManageCustomActionsFolder("Golf", false);

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON File|*.json",
                Title = "Save an Actions JSON File",
                InitialDirectory = folderPath,
                FileName = !string.IsNullOrEmpty(_currentFilePath) ? Path.GetFileName(_currentFilePath) : ""
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                _currentFile.Name = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                bool success = CustomGolfActionFileManager.Save(_currentFile, saveFileDialog.FileName);
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

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedItem = comboBox1.SelectedItem?.ToString() ?? "";
            actionTimeTxtBox.Enabled = !string.IsNullOrEmpty(selectedItem);
            UpdateDurationDisplay();

            // Update help text and presets label
            switch (selectedItem)
            {
                case "SWING POWER":
                    helpLabel.Text = "Hold CTRL to charge power.\n\nLonger duration = more power.\n\n40% ≈ 1000ms\n60% ≈ 1500ms\n80% ≈ 2000ms\n100% ≈ 2500ms";
                    lblPresetsTitle.Text = "Power:";
                    break;
                case "TURN LEFT":
                case "TURN RIGHT":
                    helpLabel.Text = "Rotates aim direction.\n\nSmall values for fine adjustments:\n• 50ms = tiny turn\n• 100ms = small turn\n• 150ms = medium turn\n• 200ms = large turn";
                    lblPresetsTitle.Text = "Turns:";
                    break;
                case "MOVE TO LEFT TEE SPOT":
                case "MOVE TO RIGHT TEE SPOT":
                    helpLabel.Text = "Position on tee.\n\nNote: This is skipped during auto-play.\nPosition yourself manually before starting.";
                    lblPresetsTitle.Text = "Position:";
                    break;
                case "DELAY TIME":
                    helpLabel.Text = "Wait before next action.\n\nUse at start to wait for ball placement.\n\n• 5000ms = 5 seconds\n• 10000ms = 10 seconds\n• 15000ms = 15 seconds";
                    lblPresetsTitle.Text = "Delay:";
                    break;
                default:
                    helpLabel.Text = "Select an action to see help.";
                    lblPresetsTitle.Text = "Presets:";
                    break;
            }
        }

        private void actionTimeTxtBox_TextChanged(object sender, EventArgs e)
        {
            UpdateDurationDisplay();
        }

        private void UpdateDurationDisplay()
        {
            string selectedAction = comboBox1.SelectedItem?.ToString() ?? "";
            if (int.TryParse(actionTimeTxtBox.Text, out int duration) && duration > 0 && !string.IsNullOrEmpty(selectedAction))
            {
                // Show formatted meaning next to the text box
                if (selectedAction == "SWING POWER")
                {
                    int pct = GolfDurationFormatter.CalculatePowerPercentage(duration);
                    lblDurationDisplay.Text = $"= {pct}% power";
                }
                else if (selectedAction == "TURN LEFT" || selectedAction == "TURN RIGHT")
                {
                    string desc = duration switch
                    {
                        <= 50 => "tiny",
                        <= 100 => "small",
                        <= 150 => "medium",
                        <= 200 => "large",
                        _ => "very large"
                    };
                    lblDurationDisplay.Text = $"= {desc} turn";
                }
                else if (selectedAction == "DELAY TIME")
                {
                    lblDurationDisplay.Text = $"= {duration / 1000.0:F1}s";
                }
                else
                {
                    lblDurationDisplay.Text = "";
                }
            }
            else
            {
                lblDurationDisplay.Text = "";
            }
        }

        private void actionItemsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (actionItemsListBox.SelectedItem != null)
            {
                updateSelectedActionItemBtn.Enabled = true;
                string selectedItem = actionItemsListBox.SelectedItem.ToString();

                var (actionName, duration) = ParseActionDisplay(selectedItem);

                // Select the matching action in the combobox
                comboBox1.SelectedItem = actionName;
                actionTimeTxtBox.Text = duration.ToString();
                actionTimeTxtBox.Enabled = true;
            }
            else
            {
                updateSelectedActionItemBtn.Enabled = false;
            }
        }

        private void btnPreset_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                actionTimeTxtBox.Text = btn.Tag.ToString();
            }
        }

        /// <summary>
        /// Updates the sequence preview panel.
        /// </summary>
        private void UpdatePreview()
        {
            var actions = BuildActionsListSilent();
            lblSequencePreview.Text = GolfDurationFormatter.BuildSequencePreview(actions);
        }

        /// <summary>
        /// Updates the summary panel.
        /// </summary>
        private void UpdateSummary()
        {
            var actions = BuildActionsListSilent();

            if (actions == null || actions.Count == 0)
            {
                lblSummary.Text = "Actions: 0\nTotal Time: 0ms\nPower: -\nNet Turn: Center\nTee Position: Center";
                return;
            }

            var (totalMs, powerPct, netTurnMs, teePosition) = GolfDurationFormatter.GetSequenceSummary(actions);

            string turnDirection;
            if (netTurnMs < -50)
                turnDirection = $"Left ({Math.Abs(netTurnMs)}ms)";
            else if (netTurnMs > 50)
                turnDirection = $"Right ({netTurnMs}ms)";
            else
                turnDirection = "Center";

            string powerStr = powerPct > 0 ? $"{powerPct}%" : "-";
            double totalSec = totalMs / 1000.0;

            lblSummary.Text = $"Actions: {actions.Count}\n" +
                              $"Total Time: {totalMs}ms ({totalSec:F1}s)\n" +
                              $"Power: {powerStr}\n" +
                              $"Net Turn: {turnDirection}\n" +
                              $"Tee Position: {teePosition}";
        }

        /// <summary>
        /// Builds actions list without showing error messages (for preview/summary).
        /// </summary>
        private List<GolfActionCommand> BuildActionsListSilent()
        {
            List<GolfActionCommand> actionsList = new List<GolfActionCommand>();

            foreach (var item in actionItemsListBox.Items)
            {
                string actionText = item.ToString();
                var (actionName, duration) = ParseActionDisplay(actionText);

                if (duration > 0)
                {
                    actionsList.Add(new GolfActionCommand
                    {
                        Action = actionName,
                        Command = actionName,
                        Duration = duration
                    });
                }
            }

            return actionsList;
        }
    }
}
