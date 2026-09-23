using System.Drawing;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm
    {
        private void InitializeHomeLayout()
        {
            Main.SuspendLayout();
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4,
                Padding = new Padding(12, 3, 12, 12), Margin = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, pictureBox1.Height));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            pictureBox1.Anchor = AnchorStyles.None;
            pictureBox1.Margin = Padding.Empty;
            layout.Controls.Add(pictureBox1, 0, 0);
            layout.SetColumnSpan(pictureBox1, 2);

            mainTitleLabel.Anchor = AnchorStyles.Left;
            mainTitleLabel.Margin = new Padding(0, 7, 0, 3);
            layout.Controls.Add(mainTitleLabel, 0, 1);
            mainVersionLabel.Anchor = AnchorStyles.Right;
            mainVersionLabel.Margin = new Padding(0, 7, 0, 3);
            layout.Controls.Add(mainVersionLabel, 1, 1);
            mainAuthorLabel.Anchor = AnchorStyles.Left;
            mainAuthorLabel.Margin = new Padding(0, 0, 0, 10);
            layout.Controls.Add(mainAuthorLabel, 0, 2);
            layout.SetColumnSpan(mainAuthorLabel, 2);

            gettingStartedGroup.Dock = DockStyle.Fill;
            gettingStartedGroup.Margin = new Padding(0, 0, 7, 0);
            gettingStartedGroup.Padding = new Padding(12, 8, 12, 12);
            gettingStartedLabel.Dock = DockStyle.Fill;
            gettingStartedLabel.Text = gettingStartedLabel.Text.Replace("with\r\n   templates", "with templates");
            layout.Controls.Add(gettingStartedGroup, 0, 3);

            infoGroup.Dock = DockStyle.Fill;
            infoGroup.Margin = new Padding(7, 0, 0, 0);
            var info = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3,
                Padding = new Padding(9, 6, 9, 9), Margin = Padding.Empty
            };
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            info.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            info.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            info.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shortcutsTitleLabel.Margin = new Padding(0, 0, 0, 8);
            info.Controls.Add(shortcutsTitleLabel, 0, 0);
            info.SetColumnSpan(shortcutsTitleLabel, 2);
            shortcutsLabel.Dock = DockStyle.Fill;
            shortcutsLabel.Margin = Padding.Empty;
            info.Controls.Add(shortcutsLabel, 0, 1);
            info.SetColumnSpan(shortcutsLabel, 2);
            githubLinkLabel.Anchor = AnchorStyles.Left;
            githubLinkLabel.Margin = Padding.Empty;
            aboutBtn.Anchor = AnchorStyles.Right;
            aboutBtn.Margin = Padding.Empty;
            info.Controls.Add(githubLinkLabel, 0, 2);
            info.Controls.Add(aboutBtn, 1, 2);
            infoGroup.Controls.Add(info);
            layout.Controls.Add(infoGroup, 1, 3);

            Main.Controls.Add(layout);
            Main.ResumeLayout(true);
        }
    }
}
