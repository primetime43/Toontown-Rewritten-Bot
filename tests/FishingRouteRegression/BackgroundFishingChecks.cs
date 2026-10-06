using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;

internal static class BackgroundFishingChecks
{
    private static int passed;
    private static readonly MethodInfo PostKey = typeof(CoreFunctionality).GetMethod(
        "PostBackgroundKeyMessage", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo SellTrip = typeof(FishingService).GetMethod(
        "RunSellTripAsync", BindingFlags.Static | BindingFlags.NonPublic);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    public static void Run()
    {
        bool originalMode = CoreFunctionality.UseBackgroundInput;
        IntPtr foreground = GetForegroundWindow();
        try
        {
            // Receive real posted messages in an invisible test window, never the game.
            using var receiver = new KeyReceiver();
            IntPtr hwnd = receiver.Handle;
            foreach (var entry in new[] { (Keys.Up, 0x48, true), (Keys.Left, 0x4B, true),
                (Keys.Down, 0x50, true), (Keys.Right, 0x4D, true), (Keys.Escape, 0x01, false),
                (Keys.Return, 0x1C, false) })
            {
                receiver.ReceivedKeys.Clear();
                PostKey.Invoke(null, new object[] { hwnd, (int)entry.Item1, false });
                PostKey.Invoke(null, new object[] { hwnd, (int)entry.Item1, true });
                Application.DoEvents();
                Check(receiver.ReceivedKeys.Count == 2, $"{entry.Item1}: down and up delivered to an unfocused window");
                for (int i = 0; i < 2; i++)
                {
                    var e = receiver.ReceivedKeys[i];
                    Check(e.Message == (i == 0 ? 0x100 : 0x101) && e.Key == (int)entry.Item1 &&
                        ((e.Data >> 16) & 0xFF) == entry.Item2 &&
                        ((e.Data & 0x1000000) != 0) == entry.Item3 &&
                        (e.Data & 0xFFFF) == 1 && (e.Data >> 30) == (i == 0 ? 0u : 3u),
                        $"{entry.Item1}: {(i == 0 ? "down" : "up")} has its scan code and Windows key-state flags (message={e.Message:X}, key={e.Key:X}, data={e.Data:X8})");
                }
            }
            foreach (bool background in new[] { true, false })
            {
                CoreFunctionality.UseBackgroundInput = background;
                RunTrip(async () =>
                {
                    Check(CoreFunctionality.UseBackgroundInput == background, "Sell trip keeps selected input mode");
                    await Task.Delay(10).ConfigureAwait(false);
                    Check(CoreFunctionality.UseBackgroundInput == background, "Walking retains mode across awaits");
                }).GetAwaiter().GetResult();
                try
                {
                    RunTrip(() => throw new OperationCanceledException()).GetAwaiter().GetResult();
                    throw new Exception("Cancellation swallowed");
                }
                catch (OperationCanceledException)
                {
                    Check(CoreFunctionality.UseBackgroundInput == background, "Cancelled sell trip preserves mode");
                }
            }
            Check(GetForegroundWindow() == foreground, "Background messages and sell trips do not change foreground window");
            Console.WriteLine($"Passed {passed} background fishing checks. Live game movement still requires verification.");
        }
        finally { CoreFunctionality.UseBackgroundInput = originalMode; }
    }

    private static Task RunTrip(Func<Task> trip) => (Task)SellTrip.Invoke(null, new object[] { trip });
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        passed++;
        Console.WriteLine("PASS " + name);
    }

    private sealed class KeyReceiver : Form
    {
        public readonly List<(int Message, int Key, uint Data)> ReceivedKeys = new();
        protected override void WndProc(ref Message message)
        {
            if (message.Msg is 0x100 or 0x101)
            {
                ReceivedKeys.Add((message.Msg, (int)message.WParam, unchecked((uint)message.LParam.ToInt64())));
                return;
            }
            base.WndProc(ref message);
        }
    }
}
