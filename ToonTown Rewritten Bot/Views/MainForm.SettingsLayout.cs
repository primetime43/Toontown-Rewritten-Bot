using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    public partial class MainForm
    {
        private void InitializeSettingsLayout()
        {
            Settings.SuspendLayout();
            var oldControls = Settings.Controls.Cast<Control>().ToArray();
            var (root, left, right) = CreateActivityColumns();

            preferencesListBox.Dock = DockStyle.Top;
            preferencesListBox.IntegralHeight = false;
            preferencesListBox.Height = 200;
            preferencesListBox.HorizontalScrollbar = true;
            preferencesListBox.Margin = new Padding(0, 6, 0, 6);
            ConfigureSettingsHint(labelSettingsInfo,
                "Saved automatically when the bot closes. Use Save now to save immediately.");
            StyleActivityButton(btnSavePreferencesNow, "Save now", true);
            StyleActivityButton(btnRefreshPreferences, "Refresh");
            StyleActivityButton(btnOpenPreferencesFile, "Open file");
            StyleActivityButton(btnResetPreferences, "Reset defaults");
            btnResetPreferences.ForeColor = UiColors.Danger;
            left.Controls.Add(CreateSettingsSection("Saved preferences", labelSettingsInfo, preferencesListBox,
                CreateControlRow(btnSavePreferencesNow, btnRefreshPreferences),
                CreateControlRow(btnOpenPreferencesFile, btnResetPreferences)));

            StyleActivityButton(btnGameControls, "Configure game controls…", true);
            StyleActivityButton(btnHotkeys, "Configure hotkeys…", true);
            foreach (var button in new[] { btnGameControls, btnHotkeys })
            {
                button.Dock = DockStyle.Top;
                button.Margin = new Padding(0, 5, 0, 3);
            }
            right.Controls.Add(CreateSettingsSection("Controls & shortcuts",
                ActivityHelp("Match the bot’s movement keys to TTR and customize pause and stop shortcuts."),
                btnGameControls, btnHotkeys));
            ConfigureSettingsHint(labelKeyboardShortcuts, labelKeyboardShortcuts.Text);
            right.Controls.Add(CreateSettingsSection("Current shortcuts", labelKeyboardShortcuts));
            FinishActivityLayout(Settings, root, oldControls);
        }

        private void InitializeDevLayout()
        {
            Dev.SuspendLayout();
            var oldControls = Dev.Controls.Cast<Control>().ToArray();
            var (root, left, right) = CreateActivityColumns();
            foreach (var combo in new[] { comboBoxTemplateItems, devCoordinatesComboBox })
            {
                combo.Dock = DockStyle.Top;
                combo.Margin = new Padding(0, 4, 0, 6);
                combo.DropDownWidth = 440;
            }

            StyleActivityButton(btnCaptureTemplate, "Capture", true);
            StyleActivityButton(btnViewTemplate, "View");
            StyleActivityButton(btnAddTemplateItem, "Add new");
            StyleActivityButton(btnEditTemplate, "Edit");
            StyleActivityButton(btnManageVariants, "Manage variants");
            StyleActivityButton(btnDeleteTemplate, "Delete");
            StyleActivityButton(btnOpenTemplateDefinitions, "Edit JSON file");
            btnDeleteTemplate.ForeColor = UiColors.Danger;
            ConfigureSettingsHint(labelTemplateStatus, labelTemplateStatus.Text);
            left.Controls.Add(CreateSettingsSection("UI element templates",
                ActivityHelp("Select an element to capture or manage its reference images."),
                comboBoxTemplateItems, labelTemplateStatus,
                CreateControlRow(btnCaptureTemplate, btnViewTemplate),
                CreateControlRow(btnAddTemplateItem, btnEditTemplate),
                CreateControlRow(btnManageVariants, btnDeleteTemplate),
                CreateControlRow(btnOpenTemplateDefinitions)));

            StyleActivityButton(devOpenDebugBtn, "Debug window", true);
            StyleActivityButton(devOpenLogViewerBtn, "Log viewer", true);
            StyleActivityButton(devDownloadOcrBtn, "Download OCR");
            StyleActivityButton(devResetCoordinatesBtn, "Reset state");
            devResetCoordinatesBtn.ForeColor = UiColors.Danger;
            right.Controls.Add(CreateSettingsSection("Debug & testing",
                ActivityHelp("Inspect image recognition, fish detection, OCR, and logs."),
                CreateControlRow(devOpenDebugBtn, devOpenLogViewerBtn),
                CreateControlRow(devDownloadOcrBtn, devResetCoordinatesBtn)));

            StyleActivityButton(devUpdateCoordinateBtn, "Update selected", true);
            StyleActivityButton(devOpenConfigBtn, "Open config");
            right.Controls.Add(CreateSettingsSection("Manual coordinates",
                ActivityHelp("Fallback positions for game controls."), devCoordinatesComboBox,
                CreateControlRow(devUpdateCoordinateBtn, devOpenConfigBtn)));
            FinishActivityLayout(Dev, root, oldControls);
        }

        private static void ConfigureSettingsHint(Label label, string text)
        {
            label.Text = text;
            label.AutoSize = true;
            label.UseMnemonic = false;
            label.MaximumSize = new Size(245, 0);
            label.Margin = new Padding(0, 4, 0, 4);
            label.ForeColor = UiColors.Text;
        }
    }
}
