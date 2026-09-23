using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm
    {
        private Label customFishingStatusLabel;
        private Label customFishingShortcutsLabel;
        private CheckBox customQuickCasting, customBackgroundMode;
        private Control normalShadowWaitRow, customShadowWaitRow;

        private void InitializeFishingLayout()
        {
            SuspendLayout();
            ClientSize = new Size(Math.Max(ClientSize.Width, 740), Math.Max(ClientSize.Height, 460));
            customFishingStatusLabel = FishingLabel("Ready", "customFishingStatusLabel");
            customFishingShortcutsLabel = FishingLabel("", "customFishingShortcutsLabel");
            customQuickCasting = new CheckBox { Name = "customQuickCasting", Text = "Quick Casting", AutoSize = true };
            customBackgroundMode = new CheckBox { Name = "customBackgroundMode", Text = "Background Mode", AutoSize = true };
            toolTip1.SetToolTip(customQuickCasting, toolTip1.GetToolTip(quickCastingCheckBox));
            toolTip1.SetToolTip(customBackgroundMode, toolTip1.GetToolTip(backgroundModeCheckBox));

            BuildFishingTab(Fishing, false);
            BuildFishingTab(CustomFishing, true);

            // These two input settings already apply to both fishing modes.
            quickCastingCheckBox.CheckedChanged += (_, _) => customQuickCasting.Checked = quickCastingCheckBox.Checked;
            customQuickCasting.CheckedChanged += (_, _) => quickCastingCheckBox.Checked = customQuickCasting.Checked;
            backgroundModeCheckBox.CheckedChanged += (_, _) => customBackgroundMode.Checked = backgroundModeCheckBox.Checked;
            customBackgroundMode.CheckedChanged += (_, _) => backgroundModeCheckBox.Checked = customBackgroundMode.Checked;
            foreach (var checkbox in new[] { autoDetectFishCheckBox, waitForFishCheckBox, customAutoDetectFishCheckBox, customWaitForFishCheckBox })
                checkbox.CheckedChanged += (_, _) => UpdateFishingWaitControls();
            UpdateFishingWaitControls();
            SetFishingSessionActive(false);
            ResumeLayout(true);
        }

        private static Label FishingLabel(string text, string name = null) => new Label
        {
            Text = text, Name = name, AutoSize = true, UseMnemonic = false, Margin = new Padding(0, 4, 6, 4),
            ForeColor = Color.FromArgb(58, 68, 81)
        };

        private static FlowLayoutPanel FishingRow(params Control[] controls)
        {
            var row = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 2, 0, 2) };
            foreach (var control in controls)
            {
                control.Dock = DockStyle.None;
                control.Margin = new Padding(0, 3, 8, 3);
                if (control is Label label) { label.AutoSize = true; label.Margin = new Padding(0, 6, 8, 3); }
                if (control is NumericUpDown number) number.Width = 60;
                row.Controls.Add(control);
            }
            return row;
        }

        private static TableLayoutPanel FishingStack()
        {
            var panel = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, Margin = Padding.Empty };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return panel;
        }

        private static Control FishingSection(string title, params Control[] controls)
        {
            var section = FishingStack();
            section.BackColor = Color.White;
            section.Padding = new Padding(12, 8, 12, 8);
            section.Margin = new Padding(0, 0, 0, 10);
            section.Controls.Add(new Label
            {
                Text = title, AutoSize = true, UseMnemonic = false, Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Black, Margin = new Padding(0, 0, 0, 6)
            });
            foreach (var control in controls)
            {
                if (control is CheckBox check)
                {
                    check.AutoSize = true;
                    check.Margin = new Padding(0, 3, 0, 3);
                }
                section.Controls.Add(control);
            }
            return section;
        }

        private static void StyleFishingButton(Button button, bool primary = false)
        {
            button.AutoSize = true;
            button.MinimumSize = new Size(80, 32);
            button.Size = button.MinimumSize;
            button.Padding = new Padding(8, 0, 8, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(193, 205, 219);
            button.UseVisualStyleBackColor = false;
            button.BackColor = primary ? Color.FromArgb(35, 102, 185) : Color.White;
            button.ForeColor = primary ? Color.White : Color.FromArgb(36, 53, 74);
        }

        private void BuildFishingTab(TabPage tab, bool custom)
        {
            tab.SuspendLayout();
            var previousContainers = tab.Controls.Cast<Control>().ToArray();
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(10), BackColor = Color.FromArgb(242, 245, 249) };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var scrolling = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
            var columns = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, Margin = Padding.Empty };
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            var left = FishingStack();
            var right = FishingStack();
            left.Margin = new Padding(0, 0, 6, 0);
            right.Margin = new Padding(6, 0, 0, 0);
            columns.Controls.Add(left, 0, 0);
            columns.Controls.Add(right, 1, 0);
            scrolling.Controls.Add(columns);
            root.Controls.Add(scrolling, 0, 0);

            ComboBox location = custom ? customFishingFilesComboBox : fishingLocationscomboBox;
            location.Dock = DockStyle.Top;
            location.Margin = new Padding(0, 0, 0, 5);
            location.DropDownWidth = 420;
            Label castsLabel = custom ? labelCustomFishingCasts : labelCasts;
            Label sellsLabel = custom ? labelCustomFishingSells : labelSells;
            castsLabel.Text = "Casts:";
            sellsLabel.Text = "Sells:";
            int countLabelWidth = Math.Max(castsLabel.PreferredSize.Width, sellsLabel.PreferredSize.Width);
            castsLabel.MinimumSize = new Size(countLabelWidth, 0);
            sellsLabel.MinimumSize = new Size(countLabelWidth, 0);
            var casts = custom ? numericUpDownCustomCasts : numericUpDownCasts;
            var sells = custom ? numericUpDownCustomSells : numericUpDownSells;
            var setup = FishingSection(custom ? "Route & session" : "Location & session", location,
                FishingRow(castsLabel, casts), FishingRow(sellsLabel, sells));
            left.Controls.Add(setup);
            if (custom)
            {
                createCustomFishingActionsBtn.Text = "Edit route";
                wizardCustomFishingBtn.Text = "New route";
                StyleFishingButton(createCustomFishingActionsBtn);
                StyleFishingButton(wizardCustomFishingBtn);
                ((TableLayoutPanel)setup).Controls.Add(FishingRow(createCustomFishingActionsBtn, wizardCustomFishingBtn));
                var help = FishingLabel("Record the walk to the fisherman and back.\nStart fishing from the same dock.");
                help.MaximumSize = new Size(245, 0);
                ((TableLayoutPanel)setup).Controls.Add(help);
            }
            else
            {
                ((TableLayoutPanel)setup).Controls.Add(randomFishingCheckBox);
                randomFishingCheckBox.AutoSize = true;
                randomFishingCheckBox.Margin = new Padding(0, 4, 0, 4);
                fishingLocationDescLabel.AutoSize = true;
                fishingLocationDescLabel.Dock = DockStyle.Top;
                fishingLocationDescLabel.Margin = new Padding(0, 6, 0, 0);
                fishingLocationDescLabel.ForeColor = Color.DimGray;
                ((TableLayoutPanel)setup).Controls.Add(fishingLocationDescLabel);
                setup.SizeChanged += (_, _) => fishingLocationDescLabel.MaximumSize = new Size(Math.Max(1, setup.ClientSize.Width - setup.Padding.Horizontal), 0);
            }

            Button scan = custom ? new Button { Text = "Scan Area", Name = "customScanAreaButton" } : editScanAreaBtn;
            Button colors = custom ? new Button { Text = "Pond Colors", Name = "customPondColorsButton" } : calibrateColorsBtn;
            if (custom)
            {
                scan.Click += (_, _) => EditFishingScanArea("CUSTOM FISHING ACTION");
                colors.Click += (_, _) => EditFishingPondColors("CUSTOM FISHING ACTION");
                toolTip1.SetToolTip(scan, toolTip1.GetToolTip(editScanAreaBtn));
                toolTip1.SetToolTip(colors, toolTip1.GetToolTip(calibrateColorsBtn));
            }
            StyleFishingButton(scan);
            StyleFishingButton(colors);
            left.Controls.Add(FishingSection("Calibration", FishingRow(scan, colors)));

            var autoDetect = custom ? customAutoDetectFishCheckBox : autoDetectFishCheckBox;
            var wait = custom ? customWaitForFishCheckBox : waitForFishCheckBox;
            var waitNumber = custom ? customNumericUpDownWait : numericUpDownWaitAttempts;
            wait.Text = "Wait for shadow";
            var waitRow = FishingRow(FishingLabel("Wait limit:"), waitNumber, FishingLabel("sec"));
            if (custom) customShadowWaitRow = waitRow; else normalShadowWaitRow = waitRow;
            right.Controls.Add(FishingSection("Before casting", autoDetect, wait, waitRow));

            var timeout = custom ? customNumericUpDownBiteTimeout : numericUpDownBiteTimeout;
            right.Controls.Add(FishingSection("After casting", FishingRow(FishingLabel("Catch timeout:"), timeout, FishingLabel("sec"))));
            right.Controls.Add(FishingSection("Display & input",
                custom ? customShowOverlayCheckBox : showOverlayCheckBox,
                custom ? customQuickCasting : quickCastingCheckBox,
                custom ? customBackgroundMode : backgroundModeCheckBox));
            right.Controls[right.Controls.Count - 1].Margin = Padding.Empty;

            Button start = custom ? startCustomFishingBtn : startFishing;
            Button stop = custom ? stopCustomFishingBtn : stopFishingBtn;
            Label status = custom ? customFishingStatusLabel : fishingStatusLabel;
            start.Text = "Start fishing";
            stop.Text = "Stop";
            StyleFishingButton(start, true);
            StyleFishingButton(stop);
            status.AutoSize = true;
            status.Text = "Status: Idle";
            status.ForeColor = Color.DimGray;
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 6, 0, 0) };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footer.Controls.Add(FishingRow(start, stop, status), 0, 0);
            var shortcuts = custom ? customFishingShortcutsLabel : fishingShortcutsLabel;
            shortcuts.AutoSize = true;
            shortcuts.UseMnemonic = false;
            shortcuts.Margin = new Padding(0, 4, 6, 4);
            shortcuts.Text = "F11  Pause / Resume\nF12 / Esc  Stop";
            shortcuts.ForeColor = Color.DimGray;
            shortcuts.Anchor = AnchorStyles.Right;
            footer.Controls.Add(shortcuts, 1, 0);
            root.Controls.Add(footer, 0, 1);

            tab.Controls.Add(root);
            // Reused controls have moved to the new layout; dispose the obsolete containers.
            foreach (var container in previousContainers) { tab.Controls.Remove(container); container.Dispose(); }
            tab.ResumeLayout(true);
        }

        private void UpdateFishingWaitControls()
        {
            waitForFishCheckBox.Enabled = autoDetectFishCheckBox.Checked && !quickCastingCheckBox.Checked;
            customWaitForFishCheckBox.Enabled = customAutoDetectFishCheckBox.Checked && !quickCastingCheckBox.Checked;
            if (normalShadowWaitRow != null) normalShadowWaitRow.Enabled = waitForFishCheckBox.Enabled && waitForFishCheckBox.Checked;
            if (customShadowWaitRow != null) customShadowWaitRow.Enabled = customWaitForFishCheckBox.Enabled && customWaitForFishCheckBox.Checked;
        }
    }
}
