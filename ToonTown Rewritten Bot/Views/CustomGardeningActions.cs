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
    public partial class CustomGardeningActions : Form
    {
        private GardeningActionKeys _gardeningActionKeys = new GardeningActionKeys();
        private string _currentFilePath = null;
        private CustomGardeningActionFile _currentFile = null;

        // Flower database (same as Plants.cs)
        private readonly Dictionary<string, string> _flowerDatabase = new Dictionary<string, string>
        {
            // 1 Bean
            { "Laff-o-dil", "g" },
            { "Dandy Pansy", "o" },
            { "What-in Carnation", "i" },
            { "School Daisy", "y" },
            { "Lily-of-the-Alley", "c" },
            // 2 Bean
            { "Daffy Dill", "gc" },
            { "Chim Pansy", "oc" },
            { "Instant Carnation", "iy" },
            { "Lazy Daisy", "yc" },
            { "Livered Lily", "cs" },
            // 3 Bean
            { "Tinted Daffodil", "goc" },
            { "Potsen Pansy", "oiy" },
            { "Hybrid Carnation", "iyc" },
            { "Freshasa Daisy", "ycs" },
            { "Tiger Lily", "cso" },
            // 4 Bean
            { "Corn Rose", "rcso" },
            { "Giraff-o-dil", "goiy" },
            { "Marzi Pansy", "oiyc" },
            { "Delishish Carnation", "iycs" },
            { "Whoopsie Daisy", "ycso" },
            // 5 Bean
            { "Time and a Half Rose", "rcsog" },
            { "Freshasa Daffodil", "goiyc" },
            { "Chili Pansy", "oiycs" },
            { "Stinking Carnation", "iycso" },
            { "Upsy Daisy", "ycsog" },
            // 6 Bean
            { "Onelip", "rcsogi" },
            { "Side Daisy", "rcsogy" },
            { "Summer's Last Rose", "rcsoiy" },
            { "Crazy Daisy", "ycsogu" },
            { "Tinted Rose", "icsogy" },
            // 7 Bean
            { "Twolip", "rcsogio" },
            { "Midsummer's Daisy", "rcsogiy" },
            { "Indubitab Rose", "rcsogyu" },
            { "Hazy Daisy", "ycsogub" },
            { "Car Petunia", "bcsogiy" },
            // 8 Bean
            { "Istilla Rose", "rbuubbib" },
            { "Threelip", "uyyuyouy" },
            { "Platoonia", "bcsogiyb" },
            { "Muddy Daisy", "ycsogubi" },
            { "Model Carnation", "iycsoiyc" }
        };

        public CustomGardeningActions()
        {
            InitializeComponent();
            PopulateActionComboBox();
            PopulateFlowerComboBox();
            UpdatePreview();
            UpdateSummary();
        }

        private void PopulateActionComboBox()
        {
            cmbAction.Items.Clear();
            cmbAction.Items.Add("WALK FORWARD");
            cmbAction.Items.Add("WALK BACKWARD");
            cmbAction.Items.Add("WALK LEFT");
            cmbAction.Items.Add("WALK RIGHT");
            cmbAction.Items.Add("TURN LEFT");
            cmbAction.Items.Add("TURN RIGHT");
            cmbAction.Items.Add("PLANT FLOWER");
            cmbAction.Items.Add("WATER PLANT");
            cmbAction.Items.Add("REMOVE PLANT");
            cmbAction.Items.Add("DELAY");
        }

        private void PopulateFlowerComboBox()
        {
            cmbFlower.Items.Clear();
            foreach (var flower in _flowerDatabase.Keys.OrderBy(f => _flowerDatabase[f].Length).ThenBy(f => f))
            {
                cmbFlower.Items.Add(flower);
            }
        }

        private void addItemBtn_Click(object sender, EventArgs e)
        {
            string selectedAction = cmbAction.SelectedItem?.ToString() ?? "";

            if (string.IsNullOrEmpty(selectedAction))
            {
                MessageBox.Show("Please select an action from the dropdown.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cmd = BuildCommandFromUI(selectedAction);
            if (cmd == null) return;

            string displayText = FormatActionDisplay(cmd);
            actionItemsListBox.Items.Add(displayText);
            UpdatePreview();
            UpdateSummary();
        }

        private GardeningActionCommand BuildCommandFromUI(string action)
        {
            var cmd = new GardeningActionCommand { Action = action };

            switch (action)
            {
                case "WALK FORWARD":
                case "WALK BACKWARD":
                case "WALK LEFT":
                case "WALK RIGHT":
                case "TURN LEFT":
                case "TURN RIGHT":
                case "DELAY":
                    if (numDuration.Value <= 0)
                    {
                        MessageBox.Show("Please enter a valid duration.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return null;
                    }
                    cmd.Duration = (int)numDuration.Value;
                    break;

                case "PLANT FLOWER":
                    string flower = cmbFlower.SelectedItem?.ToString() ?? "";
                    if (string.IsNullOrEmpty(flower))
                    {
                        MessageBox.Show("Please select a flower to plant.", "No Flower", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return null;
                    }
                    cmd.FlowerName = flower;
                    cmd.BeanSequence = _flowerDatabase.ContainsKey(flower) ? _flowerDatabase[flower] : "";
                    break;

                case "WATER PLANT":
                    if (numWaterCount.Value <= 0)
                    {
                        MessageBox.Show("Please enter a valid water count.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return null;
                    }
                    cmd.WaterCount = (int)numWaterCount.Value;
                    break;

                case "REMOVE PLANT":
                    // No additional params needed
                    break;
            }

            return cmd;
        }

        private string FormatActionDisplay(GardeningActionCommand cmd)
        {
            string formatted = GardeningDurationFormatter.FormatDuration(cmd);
            return $"{cmd.Action} - {formatted}";
        }

        private GardeningActionCommand ParseActionDisplay(string displayText)
        {
            // Format: "ACTION - details"
            int dashIndex = displayText.IndexOf(" - ");
            if (dashIndex <= 0)
                return new GardeningActionCommand { Action = displayText.Trim() };

            string action = displayText.Substring(0, dashIndex).Trim();
            string details = displayText.Substring(dashIndex + 3).Trim();

            var cmd = new GardeningActionCommand { Action = action };

            switch (action)
            {
                case "WALK FORWARD":
                case "WALK BACKWARD":
                case "WALK LEFT":
                case "WALK RIGHT":
                    // Parse "0.5s (short walk)" -> 500
                    if (details.Contains("s "))
                    {
                        string secStr = details.Split('s')[0].Trim();
                        if (double.TryParse(secStr, out double secs))
                            cmd.Duration = (int)(secs * 1000);
                    }
                    break;

                case "TURN LEFT":
                case "TURN RIGHT":
                    // Parse "100ms (small turn)" -> 100
                    string msStr = new string(details.TakeWhile(c => char.IsDigit(c)).ToArray());
                    if (int.TryParse(msStr, out int ms))
                        cmd.Duration = ms;
                    break;

                case "DELAY":
                    // Parse "5.0s delay" -> 5000
                    string delaySec = details.Replace("s delay", "").Trim();
                    if (double.TryParse(delaySec, out double delaySecs))
                        cmd.Duration = (int)(delaySecs * 1000);
                    break;

                case "PLANT FLOWER":
                    // Parse "Plant Daffy Dill" -> FlowerName
                    if (details.StartsWith("Plant "))
                    {
                        string flowerName = details.Substring(6).Trim();
                        cmd.FlowerName = flowerName;
                        if (_flowerDatabase.ContainsKey(flowerName))
                            cmd.BeanSequence = _flowerDatabase[flowerName];
                    }
                    break;

                case "WATER PLANT":
                    // Parse "Water 3x" -> 3
                    string waterStr = details.Replace("Water ", "").Replace("x", "").Trim();
                    if (int.TryParse(waterStr, out int wc))
                        cmd.WaterCount = wc;
                    break;

                case "REMOVE PLANT":
                    // No parsing needed
                    break;
            }

            return cmd;
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

            string selectedAction = cmbAction.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(selectedAction))
            {
                MessageBox.Show("Please select an action from the dropdown.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cmd = BuildCommandFromUI(selectedAction);
            if (cmd == null) return;

            int selectedIndex = actionItemsListBox.SelectedIndex;
            actionItemsListBox.Items[selectedIndex] = FormatActionDisplay(cmd);
            UpdatePreview();
            UpdateSummary();
        }

        private void loadActionItemBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON File|*.json",
                Title = "Open a Gardening Actions JSON File",
                InitialDirectory = (string)CoreFunctionality.ManageCustomActionsFolder("Gardening", false)
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadActionsFromFile(openFileDialog.FileName);
            }
        }

        private void LoadActionsFromFile(string filePath)
        {
            var result = CustomGardeningActionFileManager.Load(filePath);
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
                actionItemsListBox.Items.Add(FormatActionDisplay(action));
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

            if (_currentFile == null)
                _currentFile = new CustomGardeningActionFile();

            _currentFile.Actions = actionsList;
            SaveActionFile();
        }

        private List<GardeningActionCommand> BuildActionsList()
        {
            List<GardeningActionCommand> actionsList = new List<GardeningActionCommand>();

            foreach (var item in actionItemsListBox.Items)
            {
                string actionText = item.ToString();
                var cmd = ParseActionDisplay(actionText);
                actionsList.Add(cmd);
            }

            return actionsList;
        }

        private void SaveActionFile()
        {
            string folderPath = (string)CoreFunctionality.ManageCustomActionsFolder("Gardening", false);

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON File|*.json",
                Title = "Save a Gardening Actions JSON File",
                InitialDirectory = folderPath,
                FileName = !string.IsNullOrEmpty(_currentFilePath) ? Path.GetFileName(_currentFilePath) : ""
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                _currentFile.Name = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                bool success = CustomGardeningActionFileManager.Save(_currentFile, saveFileDialog.FileName);
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

        private void cmbAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedAction = cmbAction.SelectedItem?.ToString() ?? "";
            UpdateUIForAction(selectedAction);
        }

        private void UpdateUIForAction(string action)
        {
            // Show/hide controls based on action type
            bool showDuration = action == "WALK FORWARD" || action == "WALK BACKWARD" ||
                               action == "WALK LEFT" || action == "WALK RIGHT" ||
                               action == "TURN LEFT" || action == "TURN RIGHT" ||
                               action == "DELAY";
            bool showFlower = action == "PLANT FLOWER";
            bool showWater = action == "WATER PLANT";

            numDuration.Visible = showDuration;
            lblDuration.Visible = showDuration;
            groupBoxPresets.Visible = showDuration;

            cmbFlower.Visible = showFlower;
            lblFlower.Visible = showFlower;

            numWaterCount.Visible = showWater;
            lblWaterCount.Visible = showWater;

            // Update presets based on action
            UpdatePresets(action);

            // Update help text
            switch (action)
            {
                case "WALK FORWARD":
                case "WALK BACKWARD":
                case "WALK LEFT":
                case "WALK RIGHT":
                    lblHelp.Text = "Hold direction key to walk.\n\n• 500ms = short step\n• 1000ms = medium walk\n• 2000ms = long walk";
                    break;
                case "TURN LEFT":
                case "TURN RIGHT":
                    lblHelp.Text = "Rotate your toon.\n\n• 100ms = slight turn\n• 250ms = quarter turn\n• 500ms = half turn";
                    break;
                case "PLANT FLOWER":
                    lblHelp.Text = "Select a flower to plant.\nThe bot will click the jellybeans in sequence, then water 3x.";
                    break;
                case "WATER PLANT":
                    lblHelp.Text = "Water the current plant.\nEach water takes ~4 seconds.";
                    break;
                case "REMOVE PLANT":
                    lblHelp.Text = "Remove the plant from the current flower bed.";
                    break;
                case "DELAY":
                    lblHelp.Text = "Wait before next action.\n\n• 2000ms = 2 seconds\n• 5000ms = 5 seconds";
                    break;
                default:
                    lblHelp.Text = "Select an action to see help.";
                    break;
            }
        }

        private void UpdatePresets(string action)
        {
            groupBoxPresets.Controls.Clear();

            if (action == "WALK FORWARD" || action == "WALK BACKWARD" ||
                action == "WALK LEFT" || action == "WALK RIGHT")
            {
                var presets = new[] { ("0.5s", 500), ("1s", 1000), ("1.5s", 1500), ("2s", 2000) };
                AddPresetButtons(presets);
            }
            else if (action == "TURN LEFT" || action == "TURN RIGHT")
            {
                var presets = new[] { ("100", 100), ("250", 250), ("500", 500), ("1000", 1000) };
                AddPresetButtons(presets);
            }
            else if (action == "DELAY")
            {
                var presets = new[] { ("2s", 2000), ("5s", 5000), ("10s", 10000) };
                AddPresetButtons(presets);
            }
        }

        private void AddPresetButtons((string label, int value)[] presets)
        {
            int x = 10;
            foreach (var (label, value) in presets)
            {
                var btn = new Button
                {
                    Text = label,
                    Location = new Point(x, 18),
                    Size = new Size(55, 25),
                    Tag = value
                };
                btn.Click += (s, ev) =>
                {
                    if (s is Button b && b.Tag is int v)
                        numDuration.Value = v;
                };
                groupBoxPresets.Controls.Add(btn);
                x += 60;
            }
        }

        private void actionItemsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (actionItemsListBox.SelectedItem != null)
            {
                updateSelectedActionItemBtn.Enabled = true;
                string selectedItem = actionItemsListBox.SelectedItem.ToString();
                var cmd = ParseActionDisplay(selectedItem);

                // Select the matching action in the combobox
                cmbAction.SelectedItem = cmd.Action;

                // Populate fields based on action
                switch (cmd.Action)
                {
                    case "WALK FORWARD":
                    case "WALK BACKWARD":
                    case "WALK LEFT":
                    case "WALK RIGHT":
                    case "TURN LEFT":
                    case "TURN RIGHT":
                    case "DELAY":
                        numDuration.Value = cmd.Duration > 0 ? cmd.Duration : 1000;
                        break;
                    case "PLANT FLOWER":
                        cmbFlower.SelectedItem = cmd.FlowerName;
                        break;
                    case "WATER PLANT":
                        numWaterCount.Value = cmd.WaterCount > 0 ? cmd.WaterCount : 1;
                        break;
                }
            }
            else
            {
                updateSelectedActionItemBtn.Enabled = false;
            }
        }

        private void UpdatePreview()
        {
            var actions = BuildActionsListSilent();
            lblSequencePreview.Text = GardeningDurationFormatter.BuildSequencePreview(actions);
        }

        private void UpdateSummary()
        {
            var actions = BuildActionsListSilent();

            if (actions == null || actions.Count == 0)
            {
                lblSummary.Text = "Actions: 0\nPlants: 0\nWaters: 0\nEst. Time: 0s";
                return;
            }

            var (totalActions, plantCount, waterCount, removeCount, estimatedTimeMs) =
                GardeningDurationFormatter.GetSequenceSummary(actions);

            double estimatedSec = estimatedTimeMs / 1000.0;
            string timeStr = estimatedSec >= 60 ? $"{estimatedSec / 60:F1}min" : $"{estimatedSec:F0}s";

            lblSummary.Text = $"Actions: {totalActions}\n" +
                              $"Plants: {plantCount}\n" +
                              $"Waters: {waterCount}\n" +
                              $"Removes: {removeCount}\n" +
                              $"Est. Time: {timeStr}";
        }

        private List<GardeningActionCommand> BuildActionsListSilent()
        {
            List<GardeningActionCommand> actionsList = new List<GardeningActionCommand>();

            foreach (var item in actionItemsListBox.Items)
            {
                string actionText = item.ToString();
                var cmd = ParseActionDisplay(actionText);
                actionsList.Add(cmd);
            }

            return actionsList;
        }
    }
}
