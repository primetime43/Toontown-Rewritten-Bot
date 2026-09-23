using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Utilities;
using ToonTown_Rewritten_Bot.Views;

internal static class Program
{
    private static int passed;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [STAThread]
    private static int Main()
    {
        try
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../ToonTown Rewritten Bot"));
            foreach (string path in Directory.GetFiles(Path.Combine(root, "Services/CustomFishingActions"), "*.json")
                .Concat(Directory.GetFiles(Path.Combine(root, "Templates/CustomFishingTemplates"), "*.json")))
            {
                var loaded = CustomFishingActionFileManager.Load(path);
                Check(loaded.Success, "Load " + Path.GetFileName(path));
                var steps = FishingRoute.Decode(loaded.File.Actions);
                Check(steps.SequenceEqual(FishingRoute.Decode(FishingRoute.Encode(steps))), "Lossless timed-step round trip: " + Path.GetFileName(path));
            }
            var legacy = FishingRoute.Decode(new List<FishingActionCommand>
            {
                new() { Action = "WALK FORWARDS", Command = "UP" }, new() { Action = "TIME", Command = "847)" },
                new() { Action = "SELL FISH", Command = "SELL" }, new() { Action = "TIME", Command = "0.5 seconds" },
                new() { Action = "WALK BACKWARDS", Command = "DOWN" }
            });
            Check(legacy[0].Milliseconds == 847 && legacy[2] == new FishingRouteStep("WAIT", 500) && legacy[3].Milliseconds == 500, "Legacy durations, explicit waits and default key holds");
            Check(FishingRoute.Validate(new[] { new FishingRouteStep("SELL") }) != null, "Incomplete routes have an actionable validation message");
            try
            {
                FishingRoute.Decode(new[] { new FishingActionCommand { Action = "WALK FORWARDS", Command = "UP" }, new FishingActionCommand { Action = "TIME", Command = "-2" } });
                throw new Exception("Invalid duration was accepted.");
            }
            catch (InvalidDataException) { Check(true, "Invalid durations cannot become stuck movement keys"); }
            var recorder = new FishingRouteRecorder();
            recorder.Change("UP", true, 0);
            recorder.Change("UP", true, 100);
            recorder.Change("LEFT", true, 200);
            recorder.Change("LEFT", false, 700);
            recorder.Change("UP", false, 800);
            recorder.Change("DOWN", true, 1500);
            recorder.ReleaseAll(1800);
            Check(recorder.Steps.SequenceEqual(new[] { new FishingRouteStep("UP", 200), new FishingRouteStep("UP+LEFT", 500), new FishingRouteStep("UP", 100), new FishingRouteStep("DOWN", 300) }), "Recording keeps simultaneous movement, ignores repeats and omits idle pauses");
            ReplayChecks().GetAwaiter().GetResult();
            EditorChecks();
            RouteNameChecks();
            Console.WriteLine($"{passed} custom fishing route checks passed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static async Task ReplayChecks()
    {
        var events = new List<string>();
        await FishingRoute.ReplayAsync(new[] { new FishingRouteStep("UP+RIGHT", 847), new FishingRouteStep("WAIT", 250), new FishingRouteStep("SELL") },
            key => events.Add("down:" + key), key => events.Add("up:" + key),
            _ => { events.Add("sell"); return Task.CompletedTask; }, CancellationToken.None,
            delay: (ms, _) => { events.Add("delay:" + ms); return Task.CompletedTask; });
        Check(events.SequenceEqual(new[] { "down:UP", "down:RIGHT", "delay:847", "up:UP", "up:RIGHT", "delay:250", "sell", "delay:3000" }), "Replay uses precise durations and runs wait/sell once");
        using var cancel = new CancellationTokenSource();
        events.Clear();
        try
        {
            await FishingRoute.ReplayAsync(new[] { new FishingRouteStep("UP+LEFT", 9000) },
                key => events.Add("down:" + key), key => events.Add("up:" + key), _ => Task.CompletedTask, cancel.Token,
                delay: (_, token) => { cancel.Cancel(); return Task.FromCanceled(token); });
            throw new Exception("Cancellation was not propagated.");
        }
        catch (OperationCanceledException) { }
        Check(events.SequenceEqual(new[] { "down:UP", "down:LEFT", "up:UP", "up:LEFT" }), "Cancelling playback releases every held key");
    }

    private static void RouteNameChecks()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "route-name-fixtures");
        Directory.CreateDirectory(folder);
        string namedPath = Path.Combine(folder, "Custom_20260923_0917.json");
        var route = new CustomFishingActionFile { Name = "Carnival Fishing" };
        Check(CustomFishingActionFileManager.Save(route, namedPath), "Create named route fixture");
        string legacyPath = Path.Combine(folder, "Legacy Dock.json");
        File.WriteAllText(legacyPath, "[]");
        string unnamedPath = Path.Combine(folder, "Unnamed Dock.json");
        CustomFishingActionFileManager.Save(new CustomFishingActionFile { Name = "  " }, unnamedPath);
        string brokenPath = Path.Combine(folder, "Repair Me.json");
        File.WriteAllText(brokenPath, "invalid JSON");
        var paths = new[] { namedPath, legacyPath, unnamedPath, brokenPath };
        var items = CustomFishingActionFileManager.GetRouteListItems(paths);
        using var combo = new ComboBox();
        combo.Items.AddRange(items.ToArray());
        combo.SelectedItem = items.Single(item => item.FilePath == namedPath);
        var selected = (CustomFishingRouteItem)combo.SelectedItem;
        Check(combo.GetItemText(selected) == "Carnival Fishing" && selected.FileName == "Custom_20260923_0917" && selected.FilePath == namedPath,
            "Dropdown displays the route name while retaining the original file for editing and execution");
        Check(items.Single(i => i.FilePath == legacyPath).DisplayName == "Legacy Dock" && items.Single(i => i.FilePath == unnamedPath).DisplayName == "Unnamed Dock",
            "Legacy and unnamed routes fall back to their filenames");
        Check(items.Single(i => i.FilePath == brokenPath).DisplayName == "Repair Me", "An unreadable route does not break the dropdown");
        route.Name = "Carnival — left dock";
        CustomFishingActionFileManager.Save(route, namedPath);
        var refreshed = CustomFishingActionFileManager.GetRouteListItems(paths);
        Check(refreshed.Single(i => i.FileName == selected.FileName).DisplayName == route.Name,
            "Renaming refreshes the displayed name without losing the saved file selection");
        string duplicatePath = Path.Combine(folder, "Other Dock.json");
        CustomFishingActionFileManager.Save(route, duplicatePath);
        var duplicates = CustomFishingActionFileManager.GetRouteListItems(new[] { namedPath, duplicatePath });
        Check(duplicates.Select(i => i.DisplayName).Distinct().Count() == 2 && duplicates.All(i => i.DisplayName.StartsWith(route.Name)),
            "Duplicate route names are distinguished by filename without changing file identity");
    }

    private static void EditorChecks()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "route-regression.json");
        var original = new CustomFishingActionFile
        {
            Name = "Estate — left dock", Description = "Start facing the pond, just off the dock.", CreatedAt = new DateTime(2025, 1, 2),
            Calibration = new CalibrationData { PondColors = new PondColorCalibration { WaterR = 88 } },
            Actions = FishingRoute.Encode(new[] { new FishingRouteStep("DOWN", 847), new FishingRouteStep("LEFT", 1024), new FishingRouteStep("UP", 740), new FishingRouteStep("SELL"), new FishingRouteStep("DOWN", 710), new FishingRouteStep("RIGHT", 1024), new FishingRouteStep("UP", 860) })
        };
        Check(CustomFishingActionFileManager.Save(original, path), "Create isolated route fixture");
        using var form = new FishingRouteBuilderForm(path);
        form.ShowInTaskbar = false;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-20000, -20000);
        form.Show();
        Layout(form);
        Check(Field<TextBox>(form, "routeName").Text == original.Name, "Editing loads the selected route and its metadata");
        var rows = Field<ListView>(form, "routeList");
        Check(rows.Items.Count == 7 && rows.Items[0].SubItems[3].Text == "0.847s", "Editor combines movements with durations without rounding");
        rows.Items[0].Selected = true;
        Call(form, "DuplicateStep");
        Check(Field<List<FishingRouteStep>>(form, "steps").Count == 8, "Duplicate edits route");
        Call(form, "Undo");
        Check(Field<List<FishingRouteStep>>(form, "steps").Count == 7, "Undo restores route");
        Set(form, "pendingTake", new List<FishingRouteStep> { new("UP+LEFT", 1234) });
        Set(form, "outbound", true);
        Call(form, "AcceptTake");
        var replaced = Field<List<FishingRouteStep>>(form, "steps");
        Check(replaced.Count == 5 && replaced[0].Command == "UP+LEFT" && replaced[2].Milliseconds == 710, "Accepting an outward recording preserves sell and the return path");
        Call(form, "SaveRoute", false);
        var saved = CustomFishingActionFileManager.Load(path).File;
        Check(saved.Calibration.PondColors.WaterR == 88 && saved.CreatedAt == original.CreatedAt && saved.Description == original.Description, "Editing and saving preserves calibration and metadata");
        Check(form.SavedFileName == "route-regression", "Saved filename available to main selection");
        Call(form, "Undo");
        Set(form, "pendingTake", new List<FishingRouteStep> { new("DOWN+RIGHT", 600) });
        Set(form, "outbound", false);
        Call(form, "AcceptTake");
        replaced = Field<List<FishingRouteStep>>(form, "steps");
        Check(replaced.Count == 5 && replaced[0].Milliseconds == 847 && replaced[3].Command == "SELL" && replaced[4].Command == "DOWN+RIGHT", "Accepting a return recording preserves the outward path and sell");
        Call(form, "Undo");
        Check(!(bool)Call(form, "LoadRoute", path + ".missing") && Field<List<FishingRouteStep>>(form, "steps").Count == 7, "Failed loads retain the working route");
        Call(form, "RefreshRoute", -1);
        Render(form, "route-builder-preview.png");
        form.Size = form.MinimumSize;
        Layout(form);
        Check(rows.Height >= 120 && rows.Width > 700, "Route list remains usable at minimum size");
        foreach (var button in Descendants(form).OfType<Button>().Where(b => b.Visible))
        {
            var bounds = form.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
            Check(form.ClientRectangle.Contains(bounds), "Button remains on screen: " + button.Text);
        }
        Render(form, "route-builder-compact.png");
        Set(form, "pendingTake", new List<FishingRouteStep> { new("UP", 500), new("UP+RIGHT", 300) });
        Call(form, "RefreshRoute", -1);
        Call(form, "UpdateEnabled");
        Check(Field<Button>(form, "useTake").Visible && !Field<TextBox>(form, "routeName").Enabled, "Recording preview requires use or discard before editing");
        Check(Field<Control>(form, "reviewPanel").Visible && !Field<Control>(form, "routeGuide").Visible &&
            !Field<Control>(form, "stepEditor").Visible && !Field<Button>(form, "testButton").Visible,
            "Recording review replaces unrelated controls with a clear next step");
        Check(form.AcceptButton == Field<Button>(form, "useTake") && Field<Button>(form, "closeButton").Enabled,
            "Enter keeps the recording and Close remains available during review");
        Check(Field<Label>(form, "status").Text.Contains("Keep this recording"), "Review explicitly explains why the route is waiting");
        Render(form, "route-builder-recording.png");
        Field<Button>(form, "discardTake").PerformClick();
        Check(Field<List<FishingRouteStep>>(form, "steps").Count == 7 && Field<List<FishingRouteStep>>(form, "pendingTake") == null, "Discarding a take leaves the route unchanged");
        Check(!Field<Control>(form, "reviewPanel").Visible && Field<Control>(form, "routeGuide").Visible &&
            Field<Control>(form, "stepEditor").Visible && form.AcceptButton == null, "Leaving review restores route controls");
        Set(form, "pendingTake", new List<FishingRouteStep> { new("UP", 900) });
        Set(form, "outbound", true);
        Call(form, "RefreshRoute", -1);
        Field<Button>(form, "useTake").PerformClick();
        Check(Field<Label>(form, "status").Text.StartsWith("Outward recording kept.") &&
            Field<Button>(form, "recordReturnButton").Enabled && Field<Control>(form, "stepEditor").Visible,
            "Keeping a recording unlocks the editor and explains the return-path step");
        // Reproduce removing every step after selling, then saving and testing the remaining path.
        var partial = new List<FishingRouteStep> { new("DOWN", 959), new("RIGHT", 1437), new("UP", 1636), new("SELL") };
        Set(form, "steps", partial);
        Call(form, "RefreshRoute", -1);
        Check(Field<Button>(form, "testButton").Enabled && Field<Label>(form, "status").Text.Contains("you can save now or test"),
            "Removing the return path keeps saving and testing available");
        string instructions = (string)Call(form, "GetTestInstructions");
        Check(instructions.Contains("4 listed steps") && instructions.Contains("ends at the fisherman") && instructions.Contains("WILL sell"),
            "Partial test explains exactly what will run and where it ends");
        Call(form, "SaveRoute", false);
        Check(FishingRoute.Decode(CustomFishingActionFileManager.Load(path).File.Actions).SequenceEqual(partial) &&
            !Field<bool>(form, "dirty") && Field<Label>(form, "status").Text.StartsWith("Saved partial route"),
            "Partial route is actually saved and marked saved");
        Render(form, "route-builder-partial.png");
        Set(form, "steps", new List<FishingRouteStep> { new("UP", 500) });
        Call(form, "RefreshRoute", -1);
        Check(Field<Button>(form, "testButton").Enabled && !((string)Call(form, "GetTestInstructions")).Contains("WILL sell"),
            "Movement-only tests do not require or promise selling");
        Set(form, "steps", new List<FishingRouteStep>());
        Call(form, "RefreshRoute", -1);
        Call(form, "SaveRoute", false);
        Check(CustomFishingActionFileManager.Load(path).File.Actions.Count == 0 && !Field<Button>(form, "testButton").Enabled,
            "Empty drafts can be saved while empty tests are disabled");
    }

    private static IEnumerable<Control> Descendants(Control control) => control.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static void Layout(Control c) { c.CreateControl(); c.PerformLayout(); foreach (Control child in c.Controls) Layout(child); }
    private static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, Private).GetValue(obj);
    private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Private).SetValue(obj, value);
    private static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Private).Invoke(obj, args);
    private static void Render(Form form, string filename) { using var image = new Bitmap(form.Width, form.Height); form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size)); image.Save(Path.Combine(AppContext.BaseDirectory, filename)); }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); passed++; Console.WriteLine("PASS " + message); }
}
