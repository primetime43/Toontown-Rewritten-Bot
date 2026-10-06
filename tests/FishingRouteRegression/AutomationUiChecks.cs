using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot;
using ToonTown_Rewritten_Bot.Services.FishingLocationsWalking;
using ToonTown_Rewritten_Bot.Utilities;

internal static class AutomationUiChecks
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    internal static void Run()
    {
        using var form = (MainForm)Activator.CreateInstance(typeof(MainForm), Hidden, null, new object[] { false }, null);
        Control Field(string name) => (Control)typeof(MainForm).GetField(name, Hidden).GetValue(form);
        object Call(string name, params object[] args) => typeof(MainForm).GetMethod(name, Hidden).Invoke(form, args);
        var cts = (CancellationTokenSource)typeof(MainForm).GetField("_cancellationTokenSource", Hidden).GetValue(form);
        using var hook = new GlobalKeyboardHook(); // No native hook installed; invoke only the bot's handler.
        typeof(MainForm).GetField("_globalKeyboardHook", Hidden).SetValue(form, hook);
        FishingStrategyBase.ResetPause();
        Call("GlobalKeyboardHook_KeyPressed", hook, Keys.F12);
        Check(!cts.IsCancellationRequested && !hook.SuppressKey, "Idle bot leaves the global Stop key alone");
        Call("GlobalKeyboardHook_KeyPressed", hook, Keys.F11);
        Check(!FishingStrategyBase.IsPaused, "Idle bot does not toggle fishing pause");

        Field("Dev").Enabled = false;
        var activity = (IDisposable)Call("TryBeginAutomation", "Fishing");
        try
        {
            Check(!Field("backgroundModeCheckBox").Enabled && !Field("customBackgroundMode").Enabled,
                "Both background-mode controls are locked while an activity runs");
            Check(!Field("Settings").Enabled && !Field("createCustomGolfActionsBtn").Enabled &&
                !Field("editCustomGardeningBtn").Enabled, "Settings and route-test entry points cannot interfere with a running activity");
            Call("UpdateGardeningControls");
            Check(!Field("wizardCustomGardeningBtn").Enabled && !Field("stopPlantingBtn").Enabled,
                "Changing gardening selections cannot enable another route test or a misleading Stop button");
            Call("SetFishingSessionActive", true);
            Check(Field("stopFishingBtn").Enabled && Field("stopCustomFishingBtn").Enabled,
                "Stop remains available on both fishing tabs");
            Call("GlobalKeyboardHook_KeyPressed", hook, Keys.F12);
            Check(cts.IsCancellationRequested && hook.SuppressKey, "Global Stop still cancels an active session");
            Check(!Field("backgroundModeCheckBox").Enabled, "Requesting Stop keeps input settings locked until cleanup finishes");
            Call("SetFishingSessionActive", false);
        }
        finally { activity.Dispose(); }
        Check(Field("backgroundModeCheckBox").Enabled && Field("customBackgroundMode").Enabled && Field("Settings").Enabled,
            "Cleanup restores background-mode controls and settings");
        Check(!Field("Dev").Enabled, "Cleanup preserves controls that were already disabled");
        Console.WriteLine("10 automation UI checks passed.");
    }

    private static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception(name);
        Console.WriteLine("PASS " + name);
    }
}
