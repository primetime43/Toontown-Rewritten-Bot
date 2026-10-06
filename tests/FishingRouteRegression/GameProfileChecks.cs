using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using ToonTown_Rewritten_Bot;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Utilities;

internal static class GameProfileChecks
{
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly Type Profile = typeof(MainForm).Assembly.GetType("ToonTown_Rewritten_Bot.Utilities.GameProfile");
    private static readonly Type Kind = typeof(MainForm).Assembly.GetType("ToonTown_Rewritten_Bot.Utilities.GameKind");
    private static readonly Type Paths = typeof(MainForm).Assembly.GetType("ToonTown_Rewritten_Bot.Utilities.AppPaths");
    private static int passed;

    public static void Run()
    {
        string root = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        string selection = Path.Combine(root, "game_selection.json");
        var backup = new Dictionary<string, byte[]>();
        void Write(string path, string text)
        {
            if (!backup.ContainsKey(path)) backup[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
        }
        string Prefs(string game) => Path.Combine(DataDirectory(game, root), "user_preferences.json");
        try
        {
            Write(selection, "\"Rewritten\"");
            _ = Profile.GetProperty("Current").GetValue(null);
            Write(Prefs("Rewritten"), "{\"NumberOfCasts\":7,\"ControlForward\":\"W\"}");
            Write(Prefs("CorporateClash"), "{\"NumberOfCasts\":13,\"ControlForward\":\"I\"}");
            foreach (string game in new[] { "CorporateClash", "Rewritten" })
            {
                Write(selection, "\"" + game + "\"");
                var start = new ProcessStartInfo(Environment.ProcessPath)
                {
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                start.ArgumentList.Add("--profile-probe");
                start.ArgumentList.Add(game);
                using var child = Process.Start(start);
                var output = child.StandardOutput.ReadToEndAsync();
                var errors = child.StandardError.ReadToEndAsync();
                if (!child.WaitForExit(45000)) { child.Kill(); throw new Exception("Profile regression timed out"); }
                Console.Write(output.GetAwaiter().GetResult());
                if (child.ExitCode != 0) throw new Exception(errors.GetAwaiter().GetResult());
                Check((int)JObject.Parse(File.ReadAllText(Prefs(game)))["NumberOfCasts"] == 19,
                    game + " saves preferences to its own file");
                if (game == "CorporateClash")
                    Check((int)JObject.Parse(File.ReadAllText(Prefs("Rewritten")))["NumberOfCasts"] == 7,
                        "Saving Clash preferences leaves Rewritten untouched");
            }
            string invalidSelection = Path.Combine(root, "invalid-profile-selection.json");
            Write(invalidSelection, "\"unsupported\"");
            try { Profile.GetMethod("ReadSelection", Static).Invoke(null, new object[] { invalidSelection }); throw new Exception("Invalid profile accepted"); }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidDataException)
            { Check(true, "Invalid profile is rejected instead of targeting another game"); }
        }
        finally
        {
            foreach (var pair in backup)
                if (pair.Value == null) File.Delete(pair.Key); else File.WriteAllBytes(pair.Key, pair.Value);
        }
        Console.WriteLine($"{passed} cross-profile checks passed.");
    }

    public static int Probe(string game)
    {
        bool clash = game == "CorporateClash";
        object kind = Enum.Parse(Kind, game);
        string root = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        string data = DataDirectory(game, root);
        Check(Profile.GetProperty("Current").GetValue(null).ToString() == game, "Startup selects " + game);
        Check(Path.GetFullPath((string)Paths.GetProperty("GameDataDirectory").GetValue(null)).TrimEnd(Path.DirectorySeparatorChar) == data,
            game + " uses the expected data directory");
        Check(UserPreferences.Instance.NumberOfCasts == (clash ? 13 : 7), game + " loads only its own preferences");
        Check(UserPreferences.Instance.ControlForward == (clash ? "I" : "W"), game + " loads its own control bindings");
        Check(CoordinatesManager.GetCoordinatesFilePath() == Path.Combine(data, "UIElementCoordinates.json"), game + " isolates manual coordinates");
        Check(CustomFishingActionFileManager.GetCustomActionsFolder() == Path.Combine(data, "Custom Fishing Actions"), game + " isolates fishing routes");
        Check((string)CoreFunctionality.ManageCustomActionsFolder("Fishing", false) == CustomFishingActionFileManager.GetCustomActionsFolder(),
            "Route picker and route editor use the same folder");
        Check(CustomFishingActionFileManager.GetTemplatesFolder() == Path.Combine(data, "Templates", "CustomFishingTemplates"), game + " isolates route templates");
        Check((string)typeof(PondColorManager).GetField("PondColorsFile", Static).GetValue(null) == Path.Combine(data, "Templates", "PondColors.json"), game + " isolates pond calibration");
        Check((string)typeof(CustomScanAreaManager).GetField("CustomScanAreasFile", Static).GetValue(null) == Path.Combine(data, "Templates", "CustomScanAreas.json"), game + " isolates scan areas");
        string templates = (string)Paths.GetProperty("TemplatesDirectory").GetValue(null);
        Check(Path.GetDirectoryName(UIElementManager.Instance.GetTemplatePath("Red Fishing Button")) == templates,
            "Template matching uses the profile template folder");
        Check((string)typeof(UIElementManager).GetField("_dataFilePath", Private).GetValue(UIElementManager.Instance) == Path.Combine(templates, "UIElementCoordinates.json"),
            "Cached template coordinates stay beside their profile templates");
        Check((string)typeof(TemplateDefinitionManager).GetField("_definitionsFilePath", Private).GetValue(TemplateDefinitionManager.Instance) == Path.Combine(templates, "TemplateDefinitions.json"),
            "Template definitions and captures use the same folder");
        if (clash)
        {
            Check(templates == Path.Combine(data, "Templates"), "Clash cannot fall back to Rewritten source templates");
            CoreFunctionality.EnsureAllEmbeddedJsonFilesExist();
            Check(!File.Exists(Path.Combine(data, "Custom Fishing Actions", "EstateFishing Far Left Dock.json")), "Clash does not extract Rewritten routes");
            try
            {
                new FishingService().StartFishing(FishingLocationNames.EstateLeftDock, 1, 1, false, default).GetAwaiter().GetResult();
                throw new Exception("Rewritten route accepted in Clash");
            }
            catch (InvalidOperationException) { Check(true, "Service rejects a Rewritten preset before sending input"); }
        }
        var find = Profile.GetMethods(Static).Single(m => m.Name == "FindWindow" && m.GetParameters().Length == 2);
        Func<string, IntPtr> bothGames = title => title == "Toontown Rewritten" ? new IntPtr(11)
            : title == "Toontown: Corporate Clash" ? new IntPtr(22) : IntPtr.Zero;
        Check((IntPtr)find.Invoke(null, new object[] { kind, bothGames }) == new IntPtr(clash ? 22 : 11),
            "Both games open: resolver selects only " + game);
        Func<string, IntPtr> otherGameOnly = title => title == (clash ? "Toontown Rewritten" : "Toontown: Corporate Clash") ? new IntPtr(33) : IntPtr.Zero;
        Check((IntPtr)find.Invoke(null, new object[] { kind, otherGameOnly }) == IntPtr.Zero,
            "Missing selected game never falls back to the other game");

        var findProcess = Profile.GetMethods(Static).Single(m => m.Name == "FindWindow" && m.GetParameters().Length == 3);
        Func<string, IntPtr> clashProcess = name => name == "CorporateClash" ? new IntPtr(44) : IntPtr.Zero;
        Func<string, IntPtr> noTitleMatch = _ => IntPtr.Zero;
        Check((IntPtr)findProcess.Invoke(null, new object[] { kind, noTitleMatch, clashProcess }) == (clash ? new IntPtr(44) : IntPtr.Zero),
            "Versioned Clash window is found by process only in the Clash profile");
        Check((IntPtr)findProcess.Invoke(null, new object[] { kind, bothGames, clashProcess }) == new IntPtr(clash ? 44 : 11),
            "Clash process takes priority over title lookup without changing Rewritten selection");
        Func<string, IntPtr> noProcess = _ => IntPtr.Zero;
        Check((IntPtr)findProcess.Invoke(null, new object[] { kind, bothGames, noProcess }) == new IntPtr(clash ? 22 : 11),
            "Known titles remain a fallback when the process has no usable window");
        Check((IntPtr)findProcess.Invoke(null, new object[] { kind, otherGameOnly, noProcess }) == IntPtr.Zero,
            "Process lookup never introduces a fallback to the wrong game");

        using var form = (MainForm)Activator.CreateInstance(typeof(MainForm), Private, null, new object[] { false }, null);
        var tabs = (TabControl)typeof(MainForm).GetField("tabControl1", Private).GetValue(form);
        var locations = (ComboBox)typeof(MainForm).GetField("fishingLocationscomboBox", Private).GetValue(form);
        if (clash)
        {
            Check(locations.Items.Cast<string>().SequenceEqual(new[] { FishingLocationNames.FishAnywhere }), "Clash shows only Fish Anywhere presets");
            Check(tabs.TabPages.Cast<TabPage>().Select(t => t.Name).SequenceEqual(new[] { "Main", "Fishing", "CustomFishing", "Settings", "Dev" }),
                "Clash hides untested activity tabs and retains setup tools");
        }
        else Check(locations.Items.Count > 1 && tabs.TabPages.Count == 9, "Rewritten keeps its presets and activity tabs");
        form.ShowInTaskbar = false;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-20000, -20000);
        tabs.SelectedTab = (TabPage)typeof(MainForm).GetField("Settings", Private).GetValue(form);
        form.Show();
        Application.DoEvents();
        using (var image = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
            image.Save(Path.Combine(root, game + "-settings.png"));
        }
        UserPreferences.Instance.NumberOfCasts = 19;
        UserPreferences.Instance.Save();
        Profile.GetMethod("SaveSelection", Static).Invoke(null, new[] { Enum.Parse(Kind, clash ? "Rewritten" : "CorporateClash") });
        Check(Profile.GetProperty("Current").GetValue(null).ToString() == game, "Changing the next game cannot switch a running session");
        Console.WriteLine($"{passed} {game} profile checks passed.");
        return 0;
    }

    public static int CheckLiveClashWindow()
    {
        object kind = Enum.Parse(Kind, "CorporateClash");
        var find = Profile.GetMethods(Static).Single(m => m.Name == "FindWindow" && m.GetParameters().Length == 1);
        var handle = (IntPtr)find.Invoke(null, new[] { kind });
        Check(handle != IntPtr.Zero, "Find the running Clash game with the production window resolver");
        GetWindowThreadProcessId(handle, out uint processId);
        using var process = Process.GetProcessById((int)processId);
        Check(process.ProcessName == "CorporateClash", "Resolved window belongs to CorporateClash.exe");
        var capture = typeof(MainForm).Assembly.GetType("ToonTown_Rewritten_Bot.Utilities.ImageRecognition");
        using var frame = (Bitmap)capture.GetMethod("CaptureGameClient", Static).Invoke(null, new object[] { handle, true });
        Check(frame.Width > 0 && frame.Height > 0, "Capture the live Clash game client without sending input");
        Console.WriteLine($"Live Clash verification: PID {processId}, client capture {frame.Width} x {frame.Height}.");
        return 0;
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    private static string DataDirectory(string game, string root) => game == "CorporateClash" ? Path.Combine(root, "Profiles", "CorporateClash") : root;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        passed++;
        Console.WriteLine("PASS " + message);
    }
}
