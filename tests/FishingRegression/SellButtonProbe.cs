using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading;
using ToonTown_Rewritten_Bot.Services.FishingLocationsWalking;
using ToonTown_Rewritten_Bot.Utilities;

internal static class SellButtonProbe
{
    internal static void Watch(string templatePath, string outputPath)
    {
        var assembly = typeof(FishingStrategyBase).Assembly;
        var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var kind = assembly.GetType("ToonTown_Rewritten_Bot.Utilities.GameKind");
        var find = assembly.GetType("ToonTown_Rewritten_Bot.Utilities.GameProfile")
            .GetMethod("FindWindow", flags, null, new[] { kind }, null);
        var capture = assembly.GetType("ToonTown_Rewritten_Bot.Utilities.ImageRecognition")
            .GetMethod("CaptureGameClient", flags);
        using var template = new Bitmap(templatePath);
        var timer = Stopwatch.StartNew();
        try
        {
            while (timer.Elapsed < TimeSpan.FromSeconds(60))
            {
                var window = (IntPtr)find.Invoke(null, new[] { Enum.Parse(kind, "Rewritten") });
                using var frame = (Bitmap)capture.Invoke(null, new object[] { window, true });
                var match = ImageTemplateMatcher.FindTemplate(frame, template, 0.85);
                if (match.Found)
                {
                    frame.Save(outputPath);
                    Console.WriteLine($"Sell button found at client {match.Center}, confidence={match.Confidence:P1}. Saved {outputPath}. No input sent.");
                    return;
                }
                Thread.Sleep(200);
            }
            throw new TimeoutException("Sell button did not match within 60 seconds.");
        }
        finally
        {
            assembly.GetType("ToonTown_Rewritten_Bot.Utilities.GameGraphicsCapture")
                .GetMethod("Stop", flags).Invoke(null, null);
        }
    }
}
