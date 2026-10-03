using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using ToonTown_Rewritten_Bot.Utilities;

internal static class FallbackShadowChecks
{
    internal static void Run(Action<bool, string> check)
    {
        const BindingFlags fields = BindingFlags.Static | BindingFlags.NonPublic;
        var colors = typeof(PondColorManager).GetField("_pondColors", fields);
        var areas = typeof(CustomScanAreaManager).GetField("_customScanAreas", fields);
        object oldColors = colors.GetValue(null), oldAreas = areas.GetValue(null);
        var water = Color.FromArgb(43, 154, 99);
        var shadow = Color.FromArgb(29, 125, 90);
        try
        {
            colors.SetValue(null, new Dictionary<string, PondColorManager.PondColorData>
                { ["FISH ANYWHERE"] = new(water, shadow, 12, 12, 12) });
            areas.SetValue(null, new Dictionary<string, CustomScanAreaManager.ScanAreaData>
                { ["FISH ANYWHERE"] = new() { WidthPercent = 100, HeightPercent = 70 } });
            var detector = new FishBubbleDetector("FISH ANYWHERE");
            var selectorType = typeof(FishBubbleDetector).Assembly.GetType("ToonTown_Rewritten_Bot.Utilities.FishTargetSelector");
            var selector = Activator.CreateInstance(selectorType, nonPublic: true);
            using var frame = new Bitmap(1600, 1151);
            using var graphics = Graphics.FromImage(frame);
            using var brush = new SolidBrush(shadow);
            for (int scan = 0; scan < 5; scan++)
            {
                graphics.Clear(water);
                graphics.FillEllipse(brush, 1000 - 60, 400 - 25, 120, 50);
                int movingWidth = scan == 0 ? 100 : 150;
                int movingX = 400 + scan * 50;
                graphics.FillEllipse(brush, movingX - movingWidth / 2, 300 - 25, movingWidth, 50);
                var result = detector.DetectFromScreenshot(frame);
                check(result.CandidateCount == 0 && result.AllCandidates.Count == 2,
                    $"Fallback scan {scan} keeps both shadows even when their size ranking changes");
                var point = (Point?)selectorType.GetMethod("Select").Invoke(selector, new object[] { result, frame.Size });
                if (scan >= 2)
                    check(point.HasValue && Math.Abs(point.Value.X - 1000) < 4 && Math.Abs(point.Value.Y - 400) < 4,
                        $"Fallback scan {scan} retains the stationary fish after the moving blob grows larger");
            }
            graphics.Clear(water);
            var empty = detector.DetectFromScreenshot(frame);
            check(selectorType.GetMethod("Select").Invoke(selector, new object[] { empty, frame.Size }) == null,
                "Empty pond cannot preserve the last stationary fallback position");
        }
        finally { colors.SetValue(null, oldColors); areas.SetValue(null, oldAreas); }
    }
}
