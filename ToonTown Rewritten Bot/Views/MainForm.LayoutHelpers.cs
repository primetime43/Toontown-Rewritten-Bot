using System.Drawing;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm
    {
        private static Label CreateHintLabel(string text, string name = null) => new Label
        {
            Text = text, Name = name, AutoSize = true, UseMnemonic = false, Margin = new Padding(0, 4, 6, 4),
            ForeColor = UiColors.Text
        };

        private static FlowLayoutPanel CreateControlRow(params Control[] controls)
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

        private static TableLayoutPanel CreateLayoutStack()
        {
            var panel = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, Margin = Padding.Empty };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return panel;
        }

        private static Control CreateSettingsSection(string title, params Control[] controls)
        {
            var section = CreateLayoutStack();
            section.BackColor = UiColors.Surface;
            section.Padding = new Padding(12, 8, 12, 8);
            section.Margin = new Padding(0, 0, 0, 10);
            section.Controls.Add(new Label
            {
                Text = title, AutoSize = true, UseMnemonic = false, Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = UiColors.Heading, Margin = new Padding(0, 0, 0, 6)
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

        private static void StyleActionButton(Button button, bool primary = false)
        {
            button.AutoSize = true;
            button.MinimumSize = new Size(80, 32);
            button.Size = button.MinimumSize;
            button.Padding = new Padding(8, 0, 8, 0);
            UiTheme.ApplyButtonColors(button, primary);
        }

    }
}
