using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Services;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Views
{
    public partial class CustomGolfActions : Form
    {
        public CustomGolfActions()
        {
            InitializeComponent();
        }

        private void addItemBtn_Click(object sender, EventArgs e)
        {
            string selectedItem = comboBox1.SelectedItem?.ToString() ?? "";

            if (string.IsNullOrEmpty(selectedItem))
            {
                MessageBox.Show("Please select an action from the dropdown.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // All actions now support duration
            if (int.TryParse(actionTimeTxtBox.Text, out int timeInMilliseconds) && timeInMilliseconds > 0)
            {
                actionItemsListBox.Items.Add($"{selectedItem} ({timeInMilliseconds} ms)");
                actionTimeTxtBox.Clear();
            }
            else
            {
                MessageBox.Show("Please enter a valid duration in milliseconds (must be greater than 0).", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void removeItemBtn_Click(object sender, EventArgs e)
        {
            actionItemsListBox.Items.Remove(actionItemsListBox.SelectedItem);
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
                actionItemsListBox.Items[selectedIndex] = $"{selectedItem} ({timeInMilliseconds} ms)";
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
                try
                {
                    string json = File.ReadAllText(openFileDialog.FileName);
                    var actionsList = JsonConvert.DeserializeObject<List<GolfActionCommand>>(json);

                    if (actionsList == null || actionsList.Count == 0)
                    {
                        MessageBox.Show("No actions found in the file.", "Empty File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    actionItemsListBox.Items.Clear();
                    foreach (var action in actionsList)
                    {
                        // All actions are displayed with duration in the new format
                        int duration = action.Duration > 0 ? action.Duration : 1000; // Default to 1000ms if not specified
                        string displayText = $"{action.Action} ({duration} ms)";
                        actionItemsListBox.Items.Add(displayText);
                    }

                    MessageBox.Show($"Loaded {actionsList.Count} actions.", "Load Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private GolfActionKeys _golfActionKeys = new GolfActionKeys();
        private void saveActionItemBtn_Click(object sender, EventArgs e)
        {
            if (actionItemsListBox.Items.Count == 0)
            {
                MessageBox.Show("No actions to save. Please add some actions first.", "No Actions", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<GolfActionCommand> actionsList = new List<GolfActionCommand>();

            foreach (var item in actionItemsListBox.Items)
            {
                string actionText = item.ToString();
                GolfActionCommand actionCommand = new GolfActionCommand();

                try
                {
                    // Parse action name and duration from format "ACTION NAME (123 ms)"
                    int parenIndex = actionText.LastIndexOf('(');
                    if (parenIndex <= 0)
                    {
                        MessageBox.Show($"Invalid action format: {actionText}\nExpected format: ACTION NAME (duration ms)", "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string actionName = actionText.Substring(0, parenIndex).Trim();
                    string durationPart = actionText.Substring(parenIndex);
                    string durationStr = new string(durationPart.Where(char.IsDigit).ToArray());

                    if (!int.TryParse(durationStr, out int duration) || duration <= 0)
                    {
                        MessageBox.Show($"Invalid duration in: {actionText}", "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    actionCommand.Action = actionName;
                    actionCommand.Command = actionName;
                    actionCommand.Duration = duration;

                    // Validate action name (except DELAY TIME which doesn't need a key)
                    if (actionName != "DELAY TIME" && !_golfActionKeys.ActionKeyMap.ContainsKey(actionName))
                    {
                        MessageBox.Show($"Unknown action: {actionName}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    actionsList.Add(actionCommand);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error parsing action: {actionText}\n{ex.Message}", "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            string json = JsonConvert.SerializeObject(actionsList, Formatting.Indented);
            SaveToJsonFile(json);
        }

        private void SaveToJsonFile(string jsonContent)
        {
            string folderPath = (string)CoreFunctionality.ManageCustomActionsFolder("Golf", false);  // Getting the folder path only

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

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedItem = comboBox1.SelectedItem?.ToString() ?? "";

            // All actions now require duration
            actionTimeTxtBox.Enabled = !string.IsNullOrEmpty(selectedItem);

            // Update help text based on selected action
            switch (selectedItem)
            {
                case "SWING POWER":
                    helpLabel.Text = "SWING POWER: Hold CTRL to charge. Duration = how long to charge (longer = more power). Try 1000-3000ms.";
                    break;
                case "TURN LEFT":
                    helpLabel.Text = "TURN LEFT: Rotates aim left. Duration = how long to turn. Small values (50-200ms) for fine adjustments.";
                    break;
                case "TURN RIGHT":
                    helpLabel.Text = "TURN RIGHT: Rotates aim right. Duration = how long to turn. Small values (50-200ms) for fine adjustments.";
                    break;
                case "MOVE TO LEFT TEE SPOT":
                    helpLabel.Text = "MOVE LEFT: Moves toon left on the tee. Duration = how long to walk (100-500ms typical).";
                    break;
                case "MOVE TO RIGHT TEE SPOT":
                    helpLabel.Text = "MOVE RIGHT: Moves toon right on the tee. Duration = how long to walk (100-500ms typical).";
                    break;
                case "DELAY TIME":
                    helpLabel.Text = "DELAY: Waits before next action. Use at start to wait for ball placement. Duration = wait time in ms.";
                    break;
                default:
                    helpLabel.Text = "Select an action to see help.";
                    break;
            }
        }

        private void actionItemsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (actionItemsListBox.SelectedItem != null)
            {
                updateSelectedActionItemBtn.Enabled = true;
                string selectedItem = actionItemsListBox.SelectedItem.ToString();

                // Parse the action name and duration from format "ACTION NAME (123 ms)"
                string actionName;
                string duration = "";

                int parenIndex = selectedItem.LastIndexOf('(');
                if (parenIndex > 0)
                {
                    actionName = selectedItem.Substring(0, parenIndex).Trim();
                    // Extract just the digits from the duration part
                    duration = new string(selectedItem.Substring(parenIndex).Where(char.IsDigit).ToArray());
                }
                else
                {
                    actionName = selectedItem.Trim();
                }

                // Select the matching action in the combobox
                comboBox1.SelectedItem = actionName;
                actionTimeTxtBox.Text = duration;
                actionTimeTxtBox.Enabled = true;
            }
            else
            {
                updateSelectedActionItemBtn.Enabled = false;
            }
        }
    }
}
