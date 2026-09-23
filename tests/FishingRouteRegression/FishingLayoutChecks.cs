using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot;
using ToonTown_Rewritten_Bot.Models;

internal static class FishingLayoutChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int passed;

    public static void Run()
    {
        using var form = (MainForm)Activator.CreateInstance(typeof(MainForm), Private, null, new object[] { false }, null);
        form.ShowInTaskbar = false;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-20000, -20000);
        var tabs = Field<TabControl>(form, "tabControl1");
        var fishing = Field<TabPage>(form, "Fishing");
        var custom = Field<TabPage>(form, "CustomFishing");
        Field<ComboBox>(form, "fishingLocationscomboBox").SelectedItem = "FISH ANYWHERE";
        Field<ComboBox>(form, "customFishingFilesComboBox").Items.Add(new CustomFishingRouteItem("example.json", "Carnival Fishing (middle dock)"));
        Field<ComboBox>(form, "customFishingFilesComboBox").SelectedIndex = 0;
        tabs.SelectedTab = fishing;
        form.Show();
        Layout(form);
        var defaultSize = form.Size;
        var auto = Field<CheckBox>(form, "autoDetectFishCheckBox");
        var wait = Field<CheckBox>(form, "waitForFishCheckBox");
        var time = Field<NumericUpDown>(form, "numericUpDownWaitAttempts");
        auto.Checked = true;
        wait.Checked = false;
        Check(!time.Enabled, "Unchecked shadow wait disables its time field");
        wait.Checked = true;
        Check(time.Enabled, "Enabled shadow waiting unlocks its time field");
        auto.Checked = false;
        Check(!wait.Enabled && !time.Enabled, "Disabling detection disables shadow waiting");
        auto.Checked = true;
        Check(wait.Enabled && time.Enabled, "Re-enabling detection preserves shadow wait selection");
        var customAuto = Field<CheckBox>(form, "customAutoDetectFishCheckBox");
        var customWait = Field<CheckBox>(form, "customWaitForFishCheckBox");
        var customTime = Field<NumericUpDown>(form, "customNumericUpDownWait");
        customAuto.Checked = true;
        customWait.Checked = true;
        Check(customTime.Enabled, "Custom fishing uses the same shadow-wait interaction");
        Field<CheckBox>(form, "customQuickCasting").Checked = true;
        Check(Field<CheckBox>(form, "quickCastingCheckBox").Checked && !wait.Enabled && !customWait.Enabled && !time.Enabled && !customTime.Enabled,
            "Quick Casting is synchronized and disables both shadow waits");
        Field<CheckBox>(form, "quickCastingCheckBox").Checked = false;
        Check(!Field<CheckBox>(form, "customQuickCasting").Checked && wait.Enabled && customWait.Enabled,
            "Changing shared options on either tab stays synchronized");
        wait.Checked = false;
        customWait.Checked = false;
        Check(Field<NumericUpDown>(form, "numericUpDownBiteTimeout").Enabled && Field<NumericUpDown>(form, "customNumericUpDownBiteTimeout").Enabled,
            "Catch timeout stays available independently of shadow waiting");
        Call(form, "SetFishingSessionActive", true);
        Check(!Field<Button>(form, "startFishing").Enabled && !Field<Button>(form, "startCustomFishingBtn").Enabled && Field<Button>(form, "stopCustomFishingBtn").Enabled,
            "Session bar prevents overlapping starts and enables Stop");
        Call(form, "SetFishingSessionActive", false);
        Check(!Field<Button>(form, "stopFishingBtn").Enabled && !Field<Button>(form, "stopCustomFishingBtn").Enabled,
            "Both session bars disable Stop when idle");
        Hotkeys.Pause = Keys.F9;
        Hotkeys.Stop = Keys.F10;
        Hotkeys.AllowEscToStop = false;
        Call(form, "UpdateShortcutLabels");
        Check(Field<Label>(form, "fishingShortcutsLabel").Text == Field<Label>(form, "customFishingShortcutsLabel").Text &&
            Field<Label>(form, "customFishingShortcutsLabel").Text.Contains("F9") &&
            !Field<Label>(form, "customFishingShortcutsLabel").Text.Contains("Esc"),
            "Both footers display configured shortcuts");
        Hotkeys.Pause = Keys.F11;
        Hotkeys.Stop = Keys.F12;
        Hotkeys.AllowEscToStop = true;
        Call(form, "UpdateShortcutLabels");
        Render(form, "fishing-tab.png");
        CheckVisibleInSettings(Field<CheckBox>(form, "backgroundModeCheckBox"));
        CheckVisibleInSettings(Field<Button>(form, "calibrateColorsBtn"));
        tabs.SelectedTab = custom;
        Layout(form);
        Render(form, "custom-fishing-tab.png");
        CheckVisibleInSettings(Field<CheckBox>(form, "customBackgroundMode"));
        Check(custom.Controls.Find("customScanAreaButton", true).Length == 1 && custom.Controls.Find("customPondColorsButton", true).Length == 1,
            "Custom fishing exposes its calibration actions beside setup");
        foreach (var page in new[] { fishing, custom })
        {
            tabs.SelectedTab = page;
            form.Size = form.MinimumSize;
            Layout(form);
            foreach (string name in page == fishing ? new[] { "startFishing", "stopFishingBtn", "fishingStatusLabel" }
                : new[] { "startCustomFishingBtn", "stopCustomFishingBtn", "customFishingStatusLabel" })
            {
                var control = Field<Control>(form, name);
                Check(form.ClientRectangle.Contains(form.RectangleToClient(control.RectangleToScreen(control.ClientRectangle))),
                    "Minimum window keeps session control visible: " + name);
            }
            Render(form, page == fishing ? "fishing-tab-compact.png" : "custom-fishing-tab-compact.png");
        }
        tabs.SelectedTab = Field<TabPage>(form, "Main");
        foreach (var size in new[] { defaultSize, form.MinimumSize, new Size(900, 650) })
        {
            form.Size = size;
            Layout(form);
            var page = tabs.SelectedTab;
            var logo = Field<PictureBox>(form, "pictureBox1");
            var left = Field<GroupBox>(form, "gettingStartedGroup");
            var right = Field<GroupBox>(form, "infoGroup");
            var logoBounds = page.RectangleToClient(logo.RectangleToScreen(logo.ClientRectangle));
            Check(Math.Abs(logoBounds.Left + logoBounds.Width / 2 - page.ClientSize.Width / 2) <= 2,
                "Home logo remains centered at width " + size.Width);
            Check(Math.Abs(left.Width - right.Width) <= 1 && left.Top == right.Top && left.Bottom == right.Bottom,
                "Home panels share width and vertical alignment at width " + size.Width);
            foreach (var name in new[] { "mainTitleLabel", "mainVersionLabel", "gettingStartedGroup", "infoGroup", "githubLinkLabel", "aboutBtn" })
            {
                var control = Field<Control>(form, name);
                Check(page.ClientRectangle.Contains(page.RectangleToClient(control.RectangleToScreen(control.ClientRectangle))),
                    "Home control stays on page: " + name);
            }
            var instructions = Field<Label>(form, "gettingStartedLabel");
            var textSize = TextRenderer.MeasureText(instructions.Text, instructions.Font, new Size(instructions.Width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            Check(textSize.Height <= instructions.Height, "Getting Started text fits at width " + size.Width);
            Render(form, size == defaultSize ? "home-tab.png" : size == form.MinimumSize ? "home-tab-compact.png" : "home-tab-wide.png");
        }
        Console.WriteLine($"{passed} layout checks passed.");
    }

    private static void Layout(Control c) { c.PerformLayout(); foreach (Control child in c.Controls) Layout(child); }
    private static void CheckVisibleInSettings(Control control)
    {
        var parent = control.Parent;
        while (parent is not Panel { AutoScroll: true }) parent = parent.Parent;
        Check(parent.ClientRectangle.Contains(parent.RectangleToClient(control.RectangleToScreen(control.ClientRectangle))),
            "Default window shows setting without scrolling: " + control.Text);
    }
    private static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, Private).GetValue(obj);
    private static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Private).Invoke(obj, args);
    private static void Render(Form form, string filename) { using var image = new Bitmap(form.Width, form.Height); form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size)); image.Save(Path.Combine(AppContext.BaseDirectory, filename)); }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); passed++; Console.WriteLine("PASS " + message); }
}
