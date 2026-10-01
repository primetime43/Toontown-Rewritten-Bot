using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using ToonTown_Rewritten_Bot.Utilities;

internal static class FishTargetTrackerChecks
{
    // Replay screenshots sampled at the aiming loop's 500 ms interval. No game input.
    internal static void ReplayFrames(string directory)
    {
        var files = Directory.GetFiles(directory, "*.png").OrderBy(p => p, StringComparer.Ordinal).ToArray();
        if (files.Length == 0) throw new Exception("No replay frames found.");
        var detector = new FishBubbleDetector();
        Probe tracker = null;
        int frameNumber = 0;
        foreach (var path in files)
        {
            using var frame = new Bitmap(path);
            tracker ??= new Probe(frame.Size);
            var detection = detector.DetectFromScreenshot(frame);
            tracker.Update(detection);
            var cheapest = detection.AllCandidates.OrderBy(c => c.CastPower).FirstOrDefault();
            Console.WriteLine($"{Path.GetFileName(path)}: candidates={detection.AllCandidates.Count}, " +
                $"cheapest={cheapest?.Position}, locked={tracker.Position}, " +
                $"observed={tracker.Observed}, release={tracker.ReleaseAt(frameNumber++ * 0.5) ?? "waiting"}");
        }
    }

    internal static void Run(Action<bool, string> check)
    {
        // The shared aiming loop uses this tracker for both game profiles.
        foreach (var frame in new[] { new Size(1600, 1151), new Size(1920, 802), new Size(800, 600) })
        {
            Point P(int x, int y) => new Point((int)Math.Round(x * frame.Width / 1600.0),
                (int)Math.Round(y * frame.Height / 1151.0));
            var area = new Rectangle(P(100, 100), new Size(P(1000, 700)));
            FishCandidate Fish(int x, int y, double power = 1) => new FishCandidate
            { Position = P(x, y), CastPower = power };
            var tracker = new Probe(frame);
            tracker.Scan(area, Fish(500, 400), Fish(800, 400, 2));
            check(tracker.Position == P(500, 400), $"{frame}: acquire one initial shadow");
            tracker.Scan(area, Fish(800, 400, 0), Fish(505, 400, 3));
            check(tracker.Position == P(505, 400), $"{frame}: cheaper alternate shadow cannot steal target");
            tracker.Scan(area, Fish(510, 400, 3), Fish(800, 400, 0));
            check(tracker.Ready, $"{frame}: following the same shadow permits release");

            var movingTracker = new Probe(frame);
            movingTracker.Scan(area, Fish(500, 400));
            movingTracker.Scan(area, Fish(525, 420));
            movingTracker.Scan(area, Fish(550, 440));
            check(movingTracker.Ready && movingTracker.Position == P(550, 440),
                $"{frame}: a continuously moving tracked shadow permits release without stopping");

            tracker.Scan(area, Fish(800, 400, 0));
            check(tracker.Position == P(510, 400) && !tracker.Observed && !tracker.Ready,
                $"{frame}: missed detection holds aim and cannot count as stable");
            for (int i = 0; i < 8; i++) tracker.Scan(area, Fish(800, 400, 0));
            check(tracker.Position == P(510, 400), $"{frame}: prolonged interior loss does not switch targets");
            tracker.Scan(area, Fish(515, 400, 5), Fish(800, 400, 0));
            check(tracker.Position == P(515, 400) && tracker.Observed && !tracker.Ready,
                $"{frame}: reacquire original shadow without reusing stale stability");
            tracker.Scan(area, Fish(520, 400));
            tracker.Scan(area, Fish(525, 400));
            check(tracker.Ready, $"{frame}: release after fresh consecutive observations");
            tracker.NoFrame();
            check(tracker.Position == P(525, 400) && !tracker.Ready,
                $"{frame}: unavailable frame holds aim without a stale release");

            tracker = new Probe(frame);
            tracker.Scan(area, Fish(500, 400, 1), Fish(550, 400, 2));
            tracker.Scan(area, Fish(540, 400, 0), Fish(510, 400, 2));
            tracker.Scan(area, Fish(530, 400, 0), Fish(520, 400, 2));
            tracker.Scan(area, Fish(520, 400, 0), Fish(530, 400, 2));
            check(tracker.Position == P(530, 400), $"{frame}: motion prediction follows a shadow past its neighbor");

            // The target exits each edge; a different, distant shadow remains available.
            foreach (var path in new[]
            {
                new[] { P(1080, 400), P(1095, 400) },
                new[] { P(120, 400), P(105, 400) },
                new[] { P(500, 780), P(500, 795) },
                new[] { P(500, 120), P(500, 105) }
            })
            {
                tracker = new Probe(frame);
                tracker.Scan(area, new FishCandidate { Position = path[0] });
                tracker.Scan(area, new FishCandidate { Position = path[1] });
                tracker.Scan(area, Fish(750, 550));
                check(tracker.Position == P(750, 550) && !tracker.Ready,
                    $"{frame}: outward exit from {path[1]} permits a new target and resets stability");
            }

            tracker = new Probe(frame);
            tracker.Scan(area, Fish(105, 400));
            tracker.Scan(area, Fish(120, 400));
            tracker.Scan(area, Fish(750, 550));
            check(tracker.Position == P(120, 400), $"{frame}: loss near an edge while moving inward keeps the lock");
            tracker.Scan(new Rectangle(P(600, 100), new Size(P(500, 700))), Fish(750, 550));
            check(tracker.Position == P(750, 550) && !tracker.Ready,
                $"{frame}: a scan-area change excluding the target permits reselection");

            tracker = new Probe(frame);
            tracker.Scan(area, Fish(90, 400, 0), Fish(500, 400, 2));
            check(tracker.Position == P(500, 400), $"{frame}: never acquire outside the scan area");
            var newCast = new Probe(frame);
            newCast.Scan(area, Fish(500, 400, 2), Fish(800, 400, 0));
            check(newCast.Position == P(800, 400) && !newCast.Ready,
                $"{frame}: a new cast starts with a fresh target");

            var lost = new Probe(frame);
            lost.Scan(area, Fish(500, 400));
            check(lost.ReleaseAt(0) == null, $"{frame}: acquiring a shadow alone does not release immediately");
            lost.Scan(area, Fish(800, 400));
            check(lost.ReleaseAt(0.5) == null && lost.Position == P(500, 400),
                $"{frame}: brief dropout holds the original aim");
            lost.Scan(area, Fish(800, 400));
            check(lost.ReleaseAt(1.5)?.Contains("Shadow lost") == true && lost.Position == P(500, 400),
                $"{frame}: a lost lock releases at its last aim after 1.5 seconds without switching");

            var empty = new Probe(frame);
            empty.Scan(area);
            check(empty.ReleaseAt(3.99) == null && empty.ReleaseAt(4)?.Contains("4-second") == true,
                $"{frame}: no detections cannot keep the mouse held beyond the aiming deadline");
            empty.NoFrame();
            check(empty.ReleaseAt(5) != null, $"{frame}: missing screenshots still trigger the aiming deadline");

            var intermittent = new Probe(frame);
            string intermittentReason = null;
            for (int i = 0; i <= 8; i++)
            {
                if (i % 2 == 0) intermittent.Scan(area, Fish(500 + i, 400));
                else intermittent.Scan(area);
                intermittentReason = intermittent.ReleaseAt(i * 0.5);
                if (i < 8) check(intermittentReason == null,
                    $"{frame}: intermittent detection at {i * 0.5:F1}s preserves the lock without stale confirmation");
            }
            check(intermittentReason?.Contains("4-second") == true,
                $"{frame}: flickering observations cannot restart the overall aiming deadline");
        }

        // Actual moving-shadow positions from the reported TTR run, before it froze.
        var recorded = new Probe(new Size(1920, 1009));
        var recordedArea = new Rectangle(200, 100, 1500, 650);
        var recordedPositions = new[] { new Point(1406, 474), new Point(1386, 462), new Point(1355, 444) };
        string recordedReason = null;
        for (int i = 0; i < recordedPositions.Length; i++)
        {
            recorded.Scan(recordedArea, new FishCandidate { Position = recordedPositions[i] });
            recordedReason = recorded.ReleaseAt(i * 0.6);
        }
        check(recordedReason == "Tracked target confirmed",
            "Recorded TTR moving-shadow sequence releases instead of waiting for the fish to stop");
    }

    private sealed class Probe
    {
        private static readonly Type TrackerType = typeof(FishBubbleDetector).Assembly
            .GetType("ToonTown_Rewritten_Bot.Utilities.FishTargetTracker", throwOnError: true);
        private readonly object _tracker = Activator.CreateInstance(TrackerType, nonPublic: true);
        private static readonly Type ReleaseType = TrackerType.Assembly
            .GetType("ToonTown_Rewritten_Bot.Utilities.FishingAimReleasePolicy", throwOnError: true);
        private readonly object _releasePolicy = Activator.CreateInstance(ReleaseType, nonPublic: true);
        private readonly Size _frame;
        public Probe(Size frame) => _frame = frame;
        public Point? Position => (Point?)TrackerType.GetProperty("Position").GetValue(_tracker);
        public bool Observed => (bool)TrackerType.GetProperty("ObservedThisFrame").GetValue(_tracker);
        public bool Ready => (bool)TrackerType.GetProperty("ReadyToRelease").GetValue(_tracker);
        public string ReleaseAt(double seconds) => (string)ReleaseType.GetMethod("GetReleaseReason")
            .Invoke(_releasePolicy, new object[] { _tracker, TimeSpan.FromSeconds(seconds) });
        public void NoFrame() => Update(null);
        public void Scan(Rectangle area, params FishCandidate[] candidates)
        {
            var detection = new FishDetectionDebugResult { ScanArea = area };
            detection.AllCandidates.AddRange(candidates);
            Update(detection);
        }
        public void Update(FishDetectionDebugResult detection) =>
            TrackerType.GetMethod("Update").Invoke(_tracker, new object[] { detection, _frame });
    }
}
