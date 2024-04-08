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

        private void button1_Click(object sender, EventArgs e)
        {
            string selectedItem = comboBox1.SelectedItem?.ToString() ?? "";
            if (selectedItem == "TIME")
            {
                // Validate the time input if "TIME" is selected
                if (int.TryParse(textBox1.Text, out int timeInSeconds))
                {
                    // Add the time in seconds to the ListBox
                    listBox1.Items.Add($"{selectedItem} ({timeInSeconds} seconds)");
                    textBox1.Clear(); // Optionally clear the TextBox after adding
                }
                else
                {
                    MessageBox.Show("Please enter a valid time in seconds.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (!string.IsNullOrEmpty(selectedItem))
            {
                // For other selections, just add the selected item directly
                listBox1.Items.Add(selectedItem);
            }
            else
            {
                MessageBox.Show("Please select an item from the ComboBox.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            listBox1.Items.Remove(listBox1.SelectedItem);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem.ToString() == "TIME")
                textBox1.Enabled = true;
            else
                textBox1.Enabled = false;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private ActionKeys _actionKeys = new ActionKeys();
        private void button3_Click(object sender, EventArgs e)
        {
            var actions = new Dictionary<string, string>();

            foreach (var item in listBox1.Items)
            {
                string action = item.ToString();
                if (action.StartsWith("TIME"))
                {
                    // Extract the numeric value (time in seconds) from the action string
                    string timeValue = new String(action.Where(Char.IsDigit).ToArray());
                    // Combine "TIME" with the extracted time value
                    actions[action] = $"TIME {timeValue}";
                }
                else
                {
                    // Use the ActionKeys instance to retrieve the VirtualKeyCode string representation
                    string keyCodeString = _actionKeys.GetKeyCodeString(action);
                    if (!string.IsNullOrEmpty(keyCodeString))
                    {
                        actions[action] = keyCodeString;
                    }
                    else
                    {
                        // Optionally handle the case where no key code is found for the action
                        actions[action] = "UNKNOWN";
                    }
                }
            }

            // Convert actions dictionary to JSON
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(actions, Newtonsoft.Json.Formatting.Indented);

            // Save JSON to file
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
    }
}
