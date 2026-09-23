using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using ToonTown_Rewritten_Bot.Utilities;

internal static class ShadowScanChecks
{
    public static void Run()
    {
        var type = typeof(FishBubbleDetector).Assembly.GetType("ToonTown_Rewritten_Bot.Utilities.FishShadowAnalyzer");
        var analyzer = Activator.CreateInstance(type, new object[] { null });
        var method = type.GetMethod("FindBlobs");
        List<List<Point>> Cluster(List<Point> points, int distance, CancellationToken token = default)
        {
            try { return (List<List<Point>>)method.Invoke(analyzer, new object[] { points, distance, token }); }
            catch (TargetInvocationException ex) when (ex.InnerException is OperationCanceledException)
            { throw ex.InnerException; }
        }

        var edgeCases = new List<Point> { new Point(-9, -9), new Point(-9, -9), new Point(-6, -5),
            new Point(0, 0), new Point(3, 4), new Point(6, 8), new Point(30, 30), new Point(31, 31) };
        foreach (int radius in new[] { 0, 1, 5, 9 })
            Require(Canonical(Cluster(edgeCases, radius)) == Canonical(ReferenceCluster(edgeCases, radius)),
                $"Blob grouping preserves duplicates, negative coordinates and distance boundaries (radius {radius})");
        Require(Cluster(new List<Point>(), 9).Count == 0, "Empty scan returns no blobs");
        for (int seed = 0; seed < 8; seed++)
        {
            var random = new Random(seed);
            var points = Enumerable.Range(0, 250).Select(_ => new Point(random.Next(-80, 80), random.Next(-80, 80))).ToList();
            Require(Canonical(Cluster(points, 9)) == Canonical(ReferenceCluster(points, 9)),
                $"Spatial search preserves existing blob membership (seed {seed})");
        }

        var small = Grid(100, 60);
        var watch = Stopwatch.StartNew();
        ReferenceCluster(small, 9);
        var oldMs = watch.Elapsed.TotalMilliseconds;
        watch.Restart();
        Cluster(small, 9);
        Console.WriteLine($"6,000-pixel comparison: previous all-pairs algorithm {oldMs:F0} ms; spatial search {watch.Elapsed.TotalMilliseconds:F0} ms.");

        var dense = Grid(600, 200);
        watch.Restart();
        var blobs = Cluster(dense, 9);
        Require(blobs.Count == 1 && blobs[0].Count == dense.Count, "A 120,000-pixel pond remains one complete connected region");
        Require(watch.Elapsed < TimeSpan.FromSeconds(5), $"Dense pond scan completes promptly ({watch.Elapsed.TotalMilliseconds:F0} ms)");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        try { Cluster(dense, 9, cancellation.Token); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { Console.WriteLine("PASS: Blob analysis observes Stop"); }
        try { new FishBubbleDetector("FISH ANYWHERE").DetectFromScreenshot(null, cancellation.Token); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { Console.WriteLine("PASS: Screenshot analysis observes Stop before doing any work"); }

        using var activeCancellation = new CancellationTokenSource();
        activeCancellation.CancelAfter(1);
        watch.Restart();
        try { Cluster(dense, 9, activeCancellation.Token); throw new Exception("Active scan ignored cancellation"); }
        catch (OperationCanceledException)
        { Require(watch.Elapsed < TimeSpan.FromSeconds(2), "Stop interrupts an active dense scan promptly"); }
        CheckFullFrame();
    }

    private static void CheckFullFrame()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Templates", "PondColors.json");
        byte[] previous = File.Exists(path) ? File.ReadAllBytes(path) : null;
        try
        {
            // The user's saved water and shadow colors overlap at tolerance 10.
            Require(PondColorManager.SetPondColors("Startup regression", Color.FromArgb(28, 39, 70), Color.FromArgb(20, 33, 64), 10, 10, 10),
                "Overlapping calibration fixture saved in the test directory");
            using var frame = new Bitmap(1920, 1080);
            using (var graphics = Graphics.FromImage(frame)) graphics.Clear(Color.FromArgb(28, 39, 70));
            var stopwatch = Stopwatch.StartNew();
            var result = new FishBubbleDetector("Startup regression").DetectFromScreenshot(frame);
            Require(result.DarkPixelCount > 70000 && stopwatch.Elapsed < TimeSpan.FromSeconds(5),
                $"Full frame with overlapping pond colors completes ({result.DarkPixelCount:N0} pixels in {stopwatch.Elapsed.TotalMilliseconds:F0} ms)");
        }
        finally
        {
            if (previous == null) File.Delete(path);
            else File.WriteAllBytes(path, previous);
            PondColorManager.Reload();
        }
    }

    private static List<Point> Grid(int width, int height)
    {
        var points = new List<Point>(width * height);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++) points.Add(new Point(x * 3, y * 3));
        return points;
    }

    private static string Canonical(List<List<Point>> blobs) => string.Join(";", blobs
        .Select(blob => string.Join("|", blob.OrderBy(p => p.X).ThenBy(p => p.Y).Select(p => $"{p.X},{p.Y}")))
        .OrderBy(s => s, StringComparer.Ordinal));

    // Previous algorithm retained only as a reference for connectivity and performance checks.
    private static List<List<Point>> ReferenceCluster(List<Point> points, int radius)
    {
        var blobs = new List<List<Point>>();
        var visited = new HashSet<int>();
        for (int i = 0; i < points.Count; i++)
        {
            if (visited.Contains(i)) continue;
            var blob = new List<Point>();
            var queue = new Queue<int>();
            queue.Enqueue(i);
            visited.Add(i);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                blob.Add(points[current]);
                for (int j = 0; j < points.Count; j++)
                {
                    if (visited.Contains(j)) continue;
                    double distance = Math.Sqrt(Math.Pow(points[current].X - points[j].X, 2) + Math.Pow(points[current].Y - points[j].Y, 2));
                    if (distance <= radius) { queue.Enqueue(j); visited.Add(j); }
                }
            }
            blobs.Add(blob);
        }
        return blobs;
    }

    private static void Require(bool condition, string description)
    {
        if (!condition) throw new Exception("FAILED: " + description);
        Console.WriteLine("PASS: " + description);
    }
}
