using System;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm
    {
        private ComboBox gameProfileComboBox;
        private Label gameProfileHint;

        private Control CreateGameProfileSection()
        {
            gameProfileComboBox = new ComboBox
            {
                Name = nameof(gameProfileComboBox), Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 4, 0, 4)
            };
            gameProfileComboBox.Items.AddRange(new object[] { "Toontown Rewritten", "Corporate Clash (experimental)" });
            gameProfileComboBox.SelectedIndex = (int)GameProfile.Current;
            gameProfileHint = ActivityHelp($"Current: {GameProfile.DisplayName}. Changing games automatically restarts the bot.");
            gameProfileComboBox.SelectionChangeCommitted += (_, _) =>
            {
                var selected = (GameKind)gameProfileComboBox.SelectedIndex;
                if (selected == GameProfile.Current) return;

                try
                {
                    GameProfile.SaveSelection(selected);
                }
                catch (Exception ex)
                {
                    gameProfileComboBox.SelectedIndex = (int)GameProfile.Current;
                    MessageBox.Show(this, $"Could not save the game choice: {ex.Message}", "Game selection",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                gameProfileComboBox.Enabled = false;
                gameProfileHint.Text = $"Restarting to use {GameProfile.GetDisplayName(selected)}...";
                try
                {
                    Logger.Info("Settings", $"Restarting to switch from {GameProfile.DisplayName} to {GameProfile.GetDisplayName(selected)}.");
                    // Normal form closing saves the current profile's preferences and releases resources.
                    Application.Restart();
                }
                catch (Exception ex)
                {
                    gameProfileComboBox.Enabled = true;
                    gameProfileHint.Text = $"Restart the bot to use {GameProfile.GetDisplayName(selected)}. Current: {GameProfile.DisplayName}.";
                    MessageBox.Show(this, $"The game choice was saved, but the bot could not restart automatically. Please restart it manually.\n\n{ex.Message}",
                        "Game selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            toolTip1.SetToolTip(gameProfileComboBox, "Each game has its own settings, templates, calibration, and routes. Changing games automatically restarts the bot.");
            return CreateSettingsSection("Game", gameProfileComboBox, gameProfileHint);
        }

        private void ApplyGameProfile()
        {
            if (!GameProfile.IsClash) return;

            Text = "Toontown Bot — Corporate Clash (experimental)";
            mainTitleLabel.Text = "Corporate Clash — experimental";
            gettingStartedLabel.Text = "• Fish Anywhere and custom routes\r\n\r\n" +
                "• Capture Clash templates in Dev\r\n\r\n" +
                "• Set Scan Area and Pond Colors\r\n   before using Auto Detect Fish\r\n\r\n" +
                "• Change games in Settings";
            fishingLocationscomboBox.Items.Clear();
            fishingLocationscomboBox.Items.Add(FishingLocationNames.FishAnywhere);
            fishingLocationscomboBox.SelectedIndex = 0;
            foreach (var tab in new[] { Gardening, Golf, Doodles, Misc })
                tabControl1.TabPages.Remove(tab);
        }

        private bool CheckClashFishingSetup()
        {
            if (!GameProfile.IsClash || UIElementManager.Instance.HasTemplate("FishPopupCloseButton")) return true;

            MessageBox.Show(this,
                "Catch one fish manually in Clash and leave its catch popup open.\n\n" +
                "In Dev, capture FishPopupCloseButton: select only the button that accepts or closes the catch. " +
                "Then return to Fishing and start again.\n\n" +
                "The bot will ask you to capture the cast, exit, and sell buttons when it needs them.",
                "Set up Clash fishing", MessageBoxButtons.OK, MessageBoxIcon.Information);
            tabControl1.SelectedTab = Dev;
            comboBoxTemplateItems.SelectedItem = "[Fishing] FishPopupCloseButton";
            return false;
        }
    }
}
