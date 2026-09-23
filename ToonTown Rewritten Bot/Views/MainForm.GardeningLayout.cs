using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm
    {
        private bool _gardeningTaskActive;
        private Label gardeningSelectionHint, gardeningRoutineHint, gardeningShortcutsLabel;

        private void InitializeGardeningLayout()
        {
            Gardening.SuspendLayout();
            var oldGroups = Gardening.Controls.Cast<Control>().ToArray();
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(10), BackColor = UiColors.Background };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var scrolling = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
            var columns = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = Padding.Empty };
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            var left = CreateLayoutStack();
            var right = CreateLayoutStack();
            left.Margin = new Padding(0, 0, 6, 0);
            right.Margin = new Padding(6, 0, 0, 0);
            columns.Controls.Add(left, 0, 0);
            columns.Controls.Add(right, 1, 0);
            scrolling.Controls.Add(columns);
            root.Controls.Add(scrolling, 0, 0);

            foreach (var combo in new[] { beanCountComboBox, flowerComboBox, customGardeningFilesComboBox })
            {
                combo.Dock = DockStyle.Top;
                combo.Margin = new Padding(0, 0, 0, 6);
                combo.DropDownWidth = 320;
            }
            foreach (var button in new[] { plantFlowerBtn, waterPlantBtn, removePlantBtn, startCustomGardeningBtn, wizardCustomGardeningBtn, editCustomGardeningBtn, calibrateGardeningBtn, stopPlantingBtn })
            {
                StyleActionButton(button, button == plantFlowerBtn || button == startCustomGardeningBtn);
                button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            }
            plantFlowerBtn.Text = "Plant flower";
            waterPlantBtn.Text = "Water now";
            removePlantBtn.Text = "Remove plant";
            removePlantBtn.ForeColor = UiColors.Danger;
            wizardCustomGardeningBtn.Text = "New routine";
            editCustomGardeningBtn.Text = "Edit selected";
            startCustomGardeningBtn.Text = "Start routine";
            calibrateGardeningBtn.Text = "Jellybean detection…";
            stopPlantingBtn.Text = "Stop";

            gardeningSelectionHint = CreateHintLabel("Choose a bean count, then a flower.", "gardeningSelectionHint");
            gardeningSelectionHint.MaximumSize = new Size(245, 0);
            beanSequencePanel.Dock = DockStyle.Top;
            beanSequencePanel.Height = 24;
            beanSequencePanel.Margin = Padding.Empty;
            var flowerSelectors = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Margin = Padding.Empty };
            flowerSelectors.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            flowerSelectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            flowerSelectors.Controls.Add(CreateHintLabel("Bean count:"), 0, 0);
            flowerSelectors.Controls.Add(beanCountComboBox, 1, 0);
            flowerSelectors.Controls.Add(CreateHintLabel("Flower:"), 0, 1);
            flowerSelectors.Controls.Add(flowerComboBox, 1, 1);
            left.Controls.Add(CreateSettingsSection("Plant a flower",
                flowerSelectors,
                gardeningSelectionHint, beanSequencePanel,
                CreateControlRow(plantFlowerBtn)));
            var waterHelp = CreateHintLabel("Also used after planting. Set 0 to skip.");
            waterHelp.MaximumSize = new Size(245, 0);
            var care = CreateSettingsSection("Plant care",
                CreateControlRow(CreateHintLabel("Water:"), waterPlantNumericUpDown, CreateHintLabel("times")),
                waterHelp, CreateControlRow(waterPlantBtn, removePlantBtn));
            care.Margin = Padding.Empty;
            left.Controls.Add(care);

            gardeningRoutineHint = CreateHintLabel("Choose a saved routine or create a new one.", "gardeningRoutineHint");
            gardeningRoutineHint.MaximumSize = new Size(245, 0);
            right.Controls.Add(CreateSettingsSection("Saved routines", customGardeningFilesComboBox,
                gardeningRoutineHint, CreateControlRow(wizardCustomGardeningBtn, editCustomGardeningBtn),
                CreateControlRow(startCustomGardeningBtn)));
            var calibrationHelp = CreateHintLabel("Open a flower bed so its jellybean buttons are visible, then calibrate their area.");
            calibrationHelp.MaximumSize = new Size(245, 0);
            right.Controls.Add(CreateSettingsSection("Calibration", calibrationHelp, CreateControlRow(calibrateGardeningBtn)));

            plantStatusLabel.Text = "Ready";
            plantStatusLabel.Font = Gardening.Font;
            plantStatusLabel.Dock = DockStyle.Fill;
            plantStatusLabel.Margin = new Padding(8, 0, 8, 0);
            plantStatusLabel.AutoEllipsis = true;
            plantStatusLabel.ForeColor = UiColors.MutedText;
            gardeningShortcutsLabel = CreateHintLabel("", "gardeningShortcutsLabel");
            gardeningShortcutsLabel.Anchor = AnchorStyles.Right;
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 3, Margin = new Padding(0, 6, 0, 0) };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footer.Controls.Add(stopPlantingBtn, 0, 0);
            footer.Controls.Add(plantStatusLabel, 1, 0);
            footer.Controls.Add(gardeningShortcutsLabel, 2, 0);
            root.Controls.Add(footer, 0, 1);

            Gardening.Controls.Add(root);
            foreach (var group in oldGroups) { Gardening.Controls.Remove(group); group.Dispose(); }
            beanCountComboBox.SelectedIndexChanged += (_, _) => UpdateGardeningControls();
            flowerComboBox.SelectedIndexChanged += (_, _) => UpdateGardeningControls();
            customGardeningFilesComboBox.SelectedIndexChanged += (_, _) => UpdateGardeningControls();
            waterPlantNumericUpDown.ValueChanged += (_, _) => UpdateGardeningControls();
            UpdateGardeningControls();
            Gardening.ResumeLayout(true);
        }

        private void SetGardeningTaskActive(bool active)
        {
            _gardeningTaskActive = active;
            UpdateGardeningControls();
        }

        private void UpdateGardeningControls()
        {
            bool idle = !_gardeningTaskActive;
            bool flowerSelected = flowerComboBox.SelectedItem != null;
            bool routineSelected = customGardeningFilesComboBox.SelectedItem != null;
            beanCountComboBox.Enabled = idle;
            flowerComboBox.Enabled = idle && beanCountComboBox.SelectedIndex >= 0;
            plantFlowerBtn.Enabled = idle && flowerSelected;
            waterPlantNumericUpDown.Enabled = idle;
            waterPlantBtn.Enabled = idle && waterPlantNumericUpDown.Value > 0;
            removePlantBtn.Enabled = idle;
            customGardeningFilesComboBox.Enabled = idle;
            startCustomGardeningBtn.Enabled = idle && routineSelected;
            editCustomGardeningBtn.Enabled = idle && routineSelected;
            wizardCustomGardeningBtn.Enabled = calibrateGardeningBtn.Enabled = idle;
            stopPlantingBtn.Enabled = !idle;
            if (gardeningSelectionHint != null)
            {
                gardeningSelectionHint.Text = flowerSelected ? "Jellybeans in planting order:" : beanCountComboBox.SelectedIndex < 0
                    ? "Choose a bean count, then a flower." : "Choose the flower you want to plant.";
                beanSequencePanel.Visible = flowerSelected;
                gardeningRoutineHint.Text = routineSelected ? "Start from the first flower bed in your routine."
                    : "Choose a saved routine or create a new one.";
            }
        }
    }
}
