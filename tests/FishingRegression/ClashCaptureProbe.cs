using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading;
using ToonTown_Rewritten_Bot.Services.FishingLocationsWalking;

internal static class ClashCaptureProbe
{
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
