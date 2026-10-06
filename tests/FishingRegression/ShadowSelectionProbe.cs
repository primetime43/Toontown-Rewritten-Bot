using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using ToonTown_Rewritten_Bot.Utilities;

internal static class ShadowSelectionProbe
{
    // Load calibration into this diagnostic process only; never write the user's settings.
    internal static void Analyze(string templatesFolder, string[] paths)
    {
        const BindingFlags fields = BindingFlags.Static | BindingFlags.NonPublic;
        var colors = typeof(PondColorManager).GetField("_pondColors", fields);
        var areas = typeof(CustomScanAreaManager).GetField("_customScanAreas", fields);
        object oldColors = colors.GetValue(null), oldAreas = areas.GetValue(null);
        try
        {
            colors.SetValue(null, JsonConvert.DeserializeObject<Dictionary<string, PondColorManager.PondColorData>>(
                File.ReadAllText(Path.Combine(templatesFolder, "PondColors.json"))));
            areas.SetValue(null, JsonConvert.DeserializeObject(File.ReadAllText(
                Path.Combine(templatesFolder, "CustomScanAreas.json")), areas.FieldType));
            var detector = new FishBubbleDetector("FISH ANYWHERE");
            var selectorType = typeof(FishBubbleDetector).Assembly.GetType("ToonTown_Rewritten_Bot.Utilities.FishTargetSelector");
            var selector = Activator.CreateInstance(selectorType, nonPublic: true);
            foreach (string path in paths)
            {
                using var frame = new Bitmap(path);
                var result = detector.DetectFromScreenshot(frame);
                var selected = selectorType.GetMethod("Select").Invoke(selector, new object[] { result, frame.Size });
                Console.WriteLine($"{Path.GetFileName(path)}: local contrast={result.UsedLocalContrastDetection}, matched pixels={result.DarkPixelCount}, blobs={result.Blobs.Count}, rejected={result.RejectedBlobCount}, filtered candidates={result.CandidateCount}, selectable={result.AllCandidates.Count}, selected={selected}");
                foreach (var blob in result.Blobs.Where(b => b.Count >= 3))
                    Console.WriteLine($"  Blob center=({blob.Average(p => p.X):F0},{blob.Average(p => p.Y):F0}), samples={blob.Count}");
                foreach (var candidate in result.AllCandidates)
                    Console.WriteLine($"  Candidate={candidate.Position}, power={candidate.CastPower:F1}");
            }
        }
        finally { colors.SetValue(null, oldColors); areas.SetValue(null, oldAreas); }
    }
}
