using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm
    {
        private Label golfShortcutsLabel, doodleShortcutsLabel, awakeShortcutsLabel;

        private void InitializeActivityLayouts()
        {
            InitializeGolfLayout();
            InitializeDoodleLayout();
            InitializeMiscLayout();
        }

        private static (TableLayoutPanel Root, TableLayoutPanel Left, TableLayoutPanel Right) CreateActivityColumns()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                Padding = new Padding(10), BackColor = UiColors.Background
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var scrolling = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
            var columns = new TableLayoutPanel
            {
                Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = Padding.Empty
            };
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
            return (root, left, right);
        }

        private static void FinishActivityLayout(TabPage tab, TableLayoutPanel root, Control[] oldGroups)
        {
            tab.Padding = Padding.Empty;
            tab.Controls.Add(root);
            foreach (var group in oldGroups)
            {
                // Standalone controls may have been moved into a new section.
                if (group.Parent != tab) continue;
                tab.Controls.Remove(group);
                group.Dispose();
            }
            tab.ResumeLayout(true);
        }

        private static Label ActivityHelp(string text)
        {
            var label = CreateHintLabel(text);
            label.MaximumSize = new Size(245, 0);
            return label;
        }

        private static void StyleActivityButton(Button button, string text, bool primary = false)
        {
            button.Text = text;
            StyleActionButton(button, primary);
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        private static Control ActivityNumberRow(string text, NumericUpDown number, string unit = "")
        {
            var label = CreateHintLabel(text);
            label.MinimumSize = new Size(68, 0);
            return CreateControlRow(label, number, CreateHintLabel(unit));
        }

        private static void AddActivityFooter(TableLayoutPanel root, Label status, Label shortcuts, params Button[] buttons)
        {
            status.AutoSize = false;
            status.AutoEllipsis = true;
            status.Dock = DockStyle.Fill;
            status.TextAlign = ContentAlignment.MiddleLeft;
            status.ForeColor = UiColors.MutedText;
            status.Margin = new Padding(8, 0, 8, 0);
            status.Text = "Status: Idle";
            shortcuts.Anchor = AnchorStyles.Right;
            shortcuts.ForeColor = UiColors.MutedText;
            var footer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3, Margin = new Padding(0, 6, 0, 0)
            };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footer.Controls.Add(CreateControlRow(buttons), 0, 0);
            footer.Controls.Add(status, 1, 0);
            footer.Controls.Add(shortcuts, 2, 0);
            root.Controls.Add(footer, 0, 1);
        }

        private void InitializeGolfLayout()
        {
            Golf.SuspendLayout();
            var oldGroups = Golf.Controls.Cast<Control>().ToArray();
            var (root, left, right) = CreateActivityColumns();
            customGolfFilesComboBox.Dock = DockStyle.Top;
            customGolfFilesComboBox.Margin = new Padding(0, 0, 0, 8);
            golfActionsListBox.Dock = DockStyle.Top;
            golfActionsListBox.IntegralHeight = false;
            golfActionsListBox.Height = 156;
            golfActionsListBox.HorizontalScrollbar = true;
            golfActionsListBox.Margin = new Padding(0, 0, 0, 8);
            left.Controls.Add(CreateSettingsSection("Course & shot", customGolfFilesComboBox,
                CreateHintLabel("Actions preview"), golfActionsListBox, showGolfOverlayCheckBox));
            golfInstructionsLabel.AutoSize = true;
            golfInstructionsLabel.MaximumSize = new Size(245, 0);
            golfInstructionsLabel.Margin = new Padding(0, 4, 0, 4);
            golfInstructionsLabel.ForeColor = UiColors.Text;
            UpdateGolfInstructionsLabel();
            right.Controls.Add(CreateSettingsSection("Before you start", golfInstructionsLabel));
            StyleActivityButton(wizardCustomGolfBtn, "New routine", true);
            StyleActivityButton(createCustomGolfActionsBtn, "Edit actions");
            right.Controls.Add(CreateSettingsSection("Custom routines",
                ActivityHelp("Create a guided routine or open the action editor."),
                CreateControlRow(wizardCustomGolfBtn, createCustomGolfActionsBtn)));
            StyleActivityButton(startGolfBtn, "Start Golf", true);
            StyleActivityButton(startAutoGolfBtn, "Auto Golf", true);
            toolTip1.SetToolTip(startAutoGolfBtn, "Detect each hole and play continuously. Click again to stop.");
            golfShortcutsLabel = CreateHintLabel("", "golfShortcutsLabel");
            AddActivityFooter(root, autoGolfStatusLabel, golfShortcutsLabel, startGolfBtn, startAutoGolfBtn);
            FinishActivityLayout(Golf, root, oldGroups);
        }

        private void InitializeDoodleLayout()
        {
            Doodles.SuspendLayout();
            var oldGroups = Doodles.Controls.Cast<Control>().ToArray();
            var (root, left, right) = CreateActivityColumns();
            doodleTrickComboBox.Dock = DockStyle.Top;
            doodleTrickComboBox.Margin = new Padding(0, 0, 0, 6);
            left.Controls.Add(CreateSettingsSection("Training session", CreateHintLabel("Trick"), doodleTrickComboBox,
                ActivityNumberRow("Cycles:", numericUpDownMaxTricks),
                ActivityNumberRow("Feeds:", numberOfDoodleFeedsNumericUpDown, "per cycle"),
                ActivityNumberRow("Scratches:", numberOfDoodleScratchesNumericUpDown, "per cycle")));
            left.Controls.Add(CreateSettingsSection("Training options", unlimitedTrainingCheckBox,
                justFeedDoodleCheckBox, justScratchDoodleCheckBox));
            doodlePictureBox.Dock = DockStyle.Top;
            doodlePictureBox.Height = 120;
            doodlePictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            doodlePictureBox.Margin = new Padding(0, 0, 0, 8);
            doodleHelpRichTextBox.Dock = DockStyle.Top;
            doodleHelpRichTextBox.Height = 72;
            doodleHelpRichTextBox.BorderStyle = BorderStyle.None;
            doodleHelpRichTextBox.BackColor = UiColors.Surface;
            doodleHelpRichTextBox.ForeColor = UiColors.Text;
            doodleHelpRichTextBox.Font = Doodles.Font;
            doodleHelpRichTextBox.Margin = Padding.Empty;
            doodleHelpRichTextBox.Text = "Call your doodle and open its profile with the Feed and Scratch buttons before starting.\n\nEach trick is requested twice per cycle.";
            right.Controls.Add(CreateSettingsSection("Before you start", doodlePictureBox, doodleHelpRichTextBox));
            var display = CreateSettingsSection("Display & input", showDoodleOverlayCheckBox, doodleBackgroundModeCheckBox);
            display.Margin = Padding.Empty;
            right.Controls.Add(display);
            StyleActivityButton(startDoodleTrainingBtn, "Start training", true);
            StyleActivityButton(stopDoodleTrainingBtn, "Stop");
            doodleShortcutsLabel = CreateHintLabel("", "doodleShortcutsLabel");
            AddActivityFooter(root, doodleStatusLabel, doodleShortcutsLabel, startDoodleTrainingBtn, stopDoodleTrainingBtn);
            FinishActivityLayout(Doodles, root, oldGroups);
        }

        private void InitializeMiscLayout()
        {
            Misc.SuspendLayout();
            var oldGroups = Misc.Controls.Cast<Control>().ToArray();
            var (root, left, right) = CreateActivityColumns();
            messageToType.Dock = DockStyle.Top;
            messageToType.Margin = new Padding(0, 0, 0, 8);
            messageToType.PlaceholderText = "Message to send in Toontown";
            StyleActivityButton(startSpamButton, "Send message", true);
            miscSpamTimesLabel.Text = "times";
            var repeat = CreateControlRow(spamMessageCheckBox, numericUpDownSpamCount, miscSpamTimesLabel);
            left.Controls.Add(CreateSettingsSection("Send message", messageToType, repeat,
                ActivityHelp("Sends to the game chat. Hold Alt to stop repeating."), CreateControlRow(startSpamButton)));
            StyleActivityButton(startKeepToonAwakeButton, "Start", true);
            StyleActivityButton(stopKeepToonAwakeButton, "Stop");
            awakeCountdownLabel.ForeColor = UiColors.MutedText;
            awakeCountdownLabel.Margin = new Padding(0, 6, 0, 4);
            awakeShortcutsLabel = CreateHintLabel("", "awakeShortcutsLabel");
            awakeShortcutsLabel.MaximumSize = new Size(245, 0);
            right.Controls.Add(CreateSettingsSection("Keep toon awake",
                ActivityNumberRow("Duration:", numericUpDownAwakeMinutes, "min"),
                ActivityHelp("Keep your toon active for the selected duration."),
                CreateControlRow(startKeepToonAwakeButton, stopKeepToonAwakeButton),
                awakeCountdownLabel, awakeShortcutsLabel));
            keepOnTopCheckBox.Text = "Keep bot window on top";
            right.Controls.Add(CreateSettingsSection("Window", keepOnTopCheckBox));
            FinishActivityLayout(Misc, root, oldGroups);
        }
    }
}
