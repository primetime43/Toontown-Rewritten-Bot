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
            // Check if the action is one that can have a duration
            if (selectedItem == "DELAY TIME" || selectedItem == "SWING POWER" || selectedItem == "TURN LEFT" || selectedItem == "TURN RIGHT")
            {
                if (int.TryParse(actionTimeTxtBox.Text, out int timeInMilliseconds))
                {
                    actionItemsListBox.Items.Add($"{selectedItem} ({timeInMilliseconds} milliseconds)");
                    actionTimeTxtBox.Clear(); // Clear the TextBox after adding
                }
                else
                {
                    MessageBox.Show("Please enter a valid time in milliseconds.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (!string.IsNullOrEmpty(selectedItem))
            {
                // For other selections like "MOVE TO LEFT TEE SPOT" or "MOVE TO RIGHT TEE SPOT" that do not require duration
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

        private void updateSelectedActionItemBtn_Click(object sender, EventArgs e)
        {
            if (actionItemsListBox.SelectedItem == null)
            {
                MessageBox.Show("No item is selected to update.");
                return;
            }

            int selectedIndex = actionItemsListBox.SelectedIndex;
            string selectedItem = comboBox1.SelectedItem?.ToString() ?? "";

            if (selectedItem == "DELAY TIME" || selectedItem == "SWING POWER" || selectedItem == "TURN LEFT" || selectedItem == "TURN RIGHT")
            {
                if (int.TryParse(actionTimeTxtBox.Text, out int timeInMilliseconds))
                {
                    actionItemsListBox.Items[selectedIndex] = $"{selectedItem} ({timeInMilliseconds} milliseconds)";
                }
                else
                {
                    MessageBox.Show("Please enter a valid time in milliseconds.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                // No duration to update, just update the action
                actionItemsListBox.Items[selectedIndex] = selectedItem;
            }
        }

        private void loadActionItemBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON File|*.json",
                Title = "Open an Actions JSON File",
                InitialDirectory = CoreFunctionality.CreateCustomGolfActionsFolder()
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string json = File.ReadAllText(openFileDialog.FileName);
                var actionsList = JsonConvert.DeserializeObject<List<GolfActionCommand>>(json);

                actionItemsListBox.Items.Clear();
                foreach (var action in actionsList)
                {
                    string displayText;
                    // Check if the action is associated with a duration and is not "TIME"
                    if (action.Duration > 0 && action.Action != "TIME")
                    {
                        displayText = $"{action.Action} ({action.Duration} milliseconds)";
                    }
                    else if (action.Action == "TIME")
                    {
                        displayText = $"TIME ({action.Duration} milliseconds)";
                    }
                    else
                    {
                        displayText = action.Action; // For actions without a duration
                    }
                    actionItemsListBox.Items.Add(displayText);
                }
            }
        }

        private GolfActionKeys _golfActionKeys = new GolfActionKeys();
        private void saveActionItemBtn_Click(object sender, EventArgs e)
        {
            List<GolfActionCommand> actionsList = new List<GolfActionCommand>();

            foreach (var item in actionItemsListBox.Items)
            {
                string actionText = item.ToString();
                GolfActionCommand actionCommand = new GolfActionCommand();

                // Extract just the action name in case the text includes duration
                string actionName = actionText.Contains("(") ? actionText.Substring(0, actionText.IndexOf('(')).Trim() : actionText;

                // Check if the action text includes time specification and parse it
                if (actionText.Contains("milliseconds"))
                {
                    actionCommand.Action = actionName;
                    actionCommand.Command = actionName;  // Command is typically the action name or key command
                                                         // Parse the duration from the list item
                    actionCommand.Duration = int.Parse(actionText.Split('(')[1].Split(' ')[0].Replace("milliseconds", "").Trim());
                }
                else
                {
                    actionCommand.Action = actionName;
                    if (_golfActionKeys.ActionKeyMap.TryGetValue(actionName, out VirtualKeyCode keyCode))
                    {
                        actionCommand.Command = keyCode.ToString();
                        // Apply default duration for specific actions not specified as "milliseconds"
                        if (actionName == "MOVE TO LEFT TEE SPOT" || actionName == "MOVE TO RIGHT TEE SPOT" || actionName == "TURN LEFT" || actionName == "TURN RIGHT")
                        {
                            actionCommand.Duration = 500; // Set a specific duration for tee and turn actions
                        }
                        else
                        {
                            // If no duration is specified in the list item, apply a default duration or leave as is
                            // Consider providing a user interface to specify this default duration or set a logical default here
                            actionCommand.Duration = 1000; // Example: Default duration for unspecified actions
                        }
                    }
                    else
                    {
                        MessageBox.Show($"No key code found for the action: {actionName}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue; // Skip adding this action if the key code is not found
                    }
                }

                actionsList.Add(actionCommand);
            }

            string json = JsonConvert.SerializeObject(actionsList, Formatting.Indented);
            SaveToJsonFile(json);
        }

        private void SaveToJsonFile(string jsonContent)
        {
            string folderPath = CoreFunctionality.CreateCustomGolfActionsFolder();

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
            if (selectedItem == "DELAY TIME" || selectedItem == "SWING POWER" || selectedItem == "TURN LEFT" || selectedItem == "TURN RIGHT")
                actionTimeTxtBox.Enabled = true;
            else
                actionTimeTxtBox.Enabled = false;
        }

        private void actionItemsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (actionItemsListBox.SelectedItem != null)
            {
                updateSelectedActionItemBtn.Enabled = true;
                string selectedItem = actionItemsListBox.SelectedItem.ToString();

                if (selectedItem.Contains("TIME") || selectedItem.Contains("SWING POWER") || selectedItem.Contains("TURN LEFT") || selectedItem.Contains("TURN RIGHT"))
                {
                    comboBox1.SelectedItem = selectedItem.Split(' ')[0]; // Select the action type without duration
                    string timeValue = new String(selectedItem.Where(char.IsDigit).ToArray());
                    actionTimeTxtBox.Text = timeValue;
                    actionTimeTxtBox.Enabled = true;
                }
                else
                {
                    comboBox1.SelectedItem = selectedItem;
                    actionTimeTxtBox.Clear();
                    actionTimeTxtBox.Enabled = false;
                }
            }
        }
    }
}
