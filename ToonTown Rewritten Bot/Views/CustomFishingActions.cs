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

namespace ToonTown_Rewritten_Bot.Views
{
    public partial class CustomFishingActions : Form
    {
        public CustomFishingActions()
        {
            InitializeComponent();
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

        private ActionKeys _actionKeys = new ActionKeys();
        private void saveActonItemBtn_Click(object sender, EventArgs e)
        {
            List<ActionCommand> actionsList = new List<ActionCommand>();

            foreach (var item in actionItemsListBox.Items)
            {
                string actionText = item.ToString();
                string action, command;

                if (actionText.StartsWith("TIME"))
                {
                    action = "TIME";
                    command = actionText.Split('(')[1].Split(' ')[0]; // Extracts "XXXX milliseconds" from "TIME (XXXX milliseconds)"
                }
                else
                {
                    action = actionText;
                    command = _actionKeys.GetKeyCodeString(actionText);
                    if (string.IsNullOrEmpty(command))
                    {
                        command = "UNKNOWN"; // Handle the case where no key code is found for the action
                    }
                }

                actionsList.Add(new ActionCommand { Action = action, Command = command });
            }

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(actionsList, Newtonsoft.Json.Formatting.Indented);
            SaveToJsonFile(json);
        }

        private void SaveToJsonFile(string jsonContent)
        {
            string folderPath = CoreFunctionality.CreateCustomFishingActionsFolder();

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

        private void loadActonItemBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON File|*.json",
                Title = "Open an Actions JSON File",
                InitialDirectory = CoreFunctionality.CreateCustomFishingActionsFolder()
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string json = File.ReadAllText(openFileDialog.FileName);
                var actionsList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ActionCommand>>(json);

                actionItemsListBox.Items.Clear();
                foreach (var actionCommand in actionsList)
                {
                    string displayText = actionCommand.Action == "TIME"
                        ? $"TIME ({actionCommand.Command})"
                        : actionCommand.Action;
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
    }
}
