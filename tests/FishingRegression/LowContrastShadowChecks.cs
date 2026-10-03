using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using ToonTown_Rewritten_Bot.Utilities;

internal static class LowContrastShadowChecks
{
    internal static void Run(Action<bool, string> check)
    {
        const BindingFlags fields = BindingFlags.Static | BindingFlags.NonPublic;
        var colors = typeof(PondColorManager).GetField("_pondColors", fields);
        var areas = typeof(CustomScanAreaManager).GetField("_customScanAreas", fields);
        object oldColors = colors.GetValue(null), oldAreas = areas.GetValue(null);
        try
        {
            colors.SetValue(null, new Dictionary<string, PondColorManager.PondColorData>
                { ["FISH ANYWHERE"] = new(Color.FromArgb(69, 85, 87), Color.FromArgb(61, 77, 81), 4, 4, 4) });
            areas.SetValue(null, new Dictionary<string, CustomScanAreaManager.ScanAreaData>
                { ["FISH ANYWHERE"] = new(new Rectangle(30, 20, 740, 440), 800, 600) });
            var detector = new FishBubbleDetector("FISH ANYWHERE");
            using var frame = new Bitmap(800, 600);
            using var graphics = Graphics.FromImage(frame);
            Color Water(int y) => Color.FromArgb(72 - y / 40, 88 - y / 40, 90 - y / 40);
            for (int y = 0; y < frame.Height; y++)
            {
                using var pen = new Pen(Water(y));
                graphics.DrawLine(pen, 0, y, frame.Width - 1, y);
            }
            var empty = detector.DetectFromScreenshot(frame);
            check(empty.UsedLocalContrastDetection && empty.AllCandidates.Count == 0,
                "Low-contrast calibration uses local contrast without mistaking the water gradient for a fish");
            foreach (var center in new[] { new Point(240, 180), new Point(560, 310) })
            {
                var water = Water(center.Y);
                using var shadow = new SolidBrush(Color.FromArgb(water.R - 7, water.G - 7, water.B - 6));
                graphics.FillEllipse(shadow, center.X - 35, center.Y - 17, 70, 34);
            }
            graphics.FillRectangle(Brushes.SaddleBrown, 40, 50, 80, 90);
            graphics.DrawLine(Pens.Black, 380, 80, 390, 400);
            graphics.FillEllipse(Brushes.White, 440, 230, 10, 10);
            var detected = detector.DetectFromScreenshot(frame);
            check(detected.AllCandidates.Count == 2 &&
                detected.AllCandidates.Any(c => Math.Abs(c.Position.X - 240) < 10 && Math.Abs(c.Position.Y - 180) < 10) &&
                detected.AllCandidates.Any(c => Math.Abs(c.Position.X - 560) < 10 && Math.Abs(c.Position.Y - 310) < 10),
                "Subtle shadows are detected while wood, a rod line and a bright bubble are rejected");
            var changed = detector.DetectFromScreenshot(frame);
            check(changed.AllCandidates.Count == detected.AllCandidates.Count,
                "Local-contrast scans do not change saved calibration or learn water as a shadow");
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            try { detector.DetectFromScreenshot(frame, cancellation.Token); throw new Exception("Stop ignored"); }
            catch (OperationCanceledException) { check(true, "Low-contrast detection observes Stop"); }
            foreach (var pond in new[] { Color.FromArgb(28, 39, 70), Color.FromArgb(76, 76, 76) })
            {
                var fish = Color.FromArgb(pond.R - 7, pond.G - 7, pond.B - 7);
                colors.SetValue(null, new Dictionary<string, PondColorManager.PondColorData>
                    { ["FISH ANYWHERE"] = new(pond, fish, 4, 4, 4) });
                graphics.Clear(pond);
                using var brush = new SolidBrush(fish);
                graphics.FillEllipse(brush, 300, 200, 70, 34);
                var dark = new FishBubbleDetector("FISH ANYWHERE").DetectFromScreenshot(frame);
                check(dark.UsedLocalContrastDetection && dark.AllCandidates.Count == 1 &&
                    Math.Abs(dark.AllCandidates[0].Position.X - 335) < 10,
                    $"Low-contrast shadows also work in water RGB({pond.R},{pond.G},{pond.B})");
            }
        }
        finally { colors.SetValue(null, oldColors); areas.SetValue(null, oldAreas); }
    }
}
