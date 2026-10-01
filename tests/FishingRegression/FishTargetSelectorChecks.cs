using System;
using System.Drawing;
using ToonTown_Rewritten_Bot.Utilities;

internal static class FishTargetSelectorChecks
{
    internal static void Run(Action<bool, string> check)
    {
        foreach (var size in new[] { new Size(1600, 1151), new Size(1920, 802), new Size(800, 600) })
        {
            Point P(int x, int y) => new Point((int)Math.Round(x * size.Width / 1600.0),
                (int)Math.Round(y * size.Height / 1151.0));
            FishCandidate Fish(int x, int y, double power) => new FishCandidate
                { Position = P(x, y), CastPower = power };
            var probe = new Probe(size);
            check(probe.Scan(Fish(400, 400, 100), Fish(800, 400, 110)) == P(400, 400),
                $"{size}: each cast initially chooses the easiest fish");
            bool stayedWithMovingFish = true;
            for (int i = 1; i <= 6; i++)
            {
                double otherPower = i % 2 == 0 ? 105 : 95;
                stayedWithMovingFish &= probe.Scan(Fish(800, 400, otherPower), Fish(400 + i * 10, 400, 100))
                    == P(400 + i * 10, 400);
            }
            check(stayedWithMovingFish, $"{size}: small ranking changes do not bounce aim between moving shadows");
            check(probe.Scan(Fish(480, 400, 100), Fish(800, 400, 70)) == P(800, 400),
                $"{size}: a substantially easier fish can take over immediately");
            check(probe.Scan(Fish(490, 400, 100)) == P(490, 400),
                $"{size}: missing preferred fish immediately allows the other fish");
            check(probe.Scan() == null, $"{size}: no detections never return an old shadow position");
            check(probe.Scan(Fish(500, 400, 100), Fish(800, 400, 95)) == P(800, 400),
                $"{size}: detection resumes with a fresh choice after an empty scan");
            check(probe.NoFrame() == null, $"{size}: missing screenshot clears the preference");
            check(probe.Scan(Fish(800, 400, 100), Fish(500, 400, 95)) == P(500, 400),
                $"{size}: missing screenshot cannot leave an old target locked");

            var nextCast = new Probe(size);
            check(nextCast.Scan(Fish(500, 400, 100), Fish(800, 400, 95)) == P(800, 400),
                $"{size}: next cast can choose a different fish even when powers are close");
            check(nextCast.Fallback(P(600, 350)) == P(600, 350),
                $"{size}: original best-shadow fallback is preserved");
        }
    }

    private sealed class Probe
    {
        private static readonly Type SelectorType = typeof(FishBubbleDetector).Assembly
            .GetType("ToonTown_Rewritten_Bot.Utilities.FishTargetSelector", throwOnError: true);
        private readonly object _selector = Activator.CreateInstance(SelectorType, nonPublic: true);
        private readonly Size _size;
        internal Probe(Size size) => _size = size;
        internal Point? Scan(params FishCandidate[] candidates)
        {
            var detection = new FishDetectionDebugResult();
            detection.AllCandidates.AddRange(candidates);
            return Select(detection);
        }
        internal Point? Fallback(Point point) => Select(new FishDetectionDebugResult { BestShadowPosition = point });
        internal Point? NoFrame() => Select(null);
        private Point? Select(FishDetectionDebugResult detection) => (Point?)SelectorType.GetMethod("Select")
            .Invoke(_selector, new object[] { detection, _size });
    }
}
