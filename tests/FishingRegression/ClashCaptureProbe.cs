using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using ToonTown_Rewritten_Bot.Services.FishingLocationsWalking;

internal static class ClashCaptureProbe
{
    internal static void CaptureBackground(string outputPath)
    {
        var assembly = typeof(FishingStrategyBase).Assembly;
        var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var profile = assembly.GetType("ToonTown_Rewritten_Bot.Utilities.GameProfile");
        var kind = assembly.GetType("ToonTown_Rewritten_Bot.Utilities.GameKind");
        var find = profile.GetMethod("FindWindow", flags, null, new[] { kind }, null);
        var window = (IntPtr)find.Invoke(null, new[] { Enum.Parse(kind, "CorporateClash") });
        if (window == IntPtr.Zero) throw new Exception("Start Clash before capturing.");
        var geometry = assembly.GetType("ToonTown_Rewritten_Bot.Utilities.GameWindowGeometry");
        object[] readArgs = { window, null };
        if (!(bool)geometry.GetMethod("TryRead", flags).Invoke(null, readArgs))
            throw new Exception("Could not read Clash window bounds.");
        var capture = assembly.GetType("ToonTown_Rewritten_Bot.Utilities.GameGraphicsCapture");
        try
        {
            for (int i = 0; i < 3; i++)
            {
                var timer = Stopwatch.StartNew();
                using var frame = (Bitmap)assembly.GetType("ToonTown_Rewritten_Bot.Utilities.ImageRecognition")
                    .GetMethod("CaptureGameClient", flags).Invoke(null, new object[] { window, true });
                var client = (Rectangle)geometry.GetProperty("ClientBounds").GetValue(readArgs[1]);
                if (frame.Size != client.Size) throw new Exception("Capture did not match the client size.");
                string path = i == 0 ? outputPath : System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(outputPath),
                    System.IO.Path.GetFileNameWithoutExtension(outputPath) + $"-{i}.png");
                frame.Save(path);
                Console.WriteLine($"Frame {i}: {frame.Width}x{frame.Height}, {timer.ElapsedMilliseconds}ms, game focused={GetForegroundWindow() == window}, saved {path}");
                Thread.Sleep(500);
            }
        }
        finally { capture.GetMethod("Stop", flags).Invoke(null, null); }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    // Opt-in live diagnostic: captures pixels only and never sends game input.
    internal static void Watch(string templatePath, string outputPath)
    {
        var assembly = typeof(FishingStrategyBase).Assembly;
        var profile = assembly.GetType("ToonTown_Rewritten_Bot.Utilities.GameProfile");
        var kind = assembly.GetType("ToonTown_Rewritten_Bot.Utilities.GameKind");
        var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var findWindow = profile.GetMethod("FindWindow", flags, null, new[] { kind }, null);
        var capture = assembly.GetType("ToonTown_Rewritten_Bot.Utilities.ImageRecognition").GetMethod("CaptureGameClient", flags);
        var detect = assembly.GetType("ToonTown_Rewritten_Bot.Utilities.ClashFishingDetector").GetMethod("FindCatchButton", flags);
        Console.WriteLine("Watching Clash for a catch popup for 60 seconds. No input will be sent.");
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(60))
        {
            var handle = (IntPtr)findWindow.Invoke(null, new[] { Enum.Parse(kind, "CorporateClash") });
            if (handle == IntPtr.Zero) throw new Exception("Clash window is no longer available.");
            using var frame = (Bitmap)capture.Invoke(null, new object[] { handle, true });
            var result = (Point?)detect.Invoke(null, new object[] { frame, new[] { templatePath }, CancellationToken.None });
            if (result.HasValue)
            {
                frame.Save(outputPath);
                Console.WriteLine("Captured a live Clash catch popup at " + result.Value + ".");
                return;
            }
            Thread.Sleep(200);
        }
        throw new TimeoutException("No catch popup was visible during the capture window.");
    }
}
