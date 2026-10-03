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
                stayedWithMovingFish &= probe.Scan(Fish(800 + i * 12, 400, otherPower), Fish(400 + i * 10, 400, 100))
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

            var motion = new Probe(size);
            motion.Scan(Fish(400, 400, 50), Fish(800, 400, 150));
            check(motion.Scan(Fish(800, 400, 150), Fish(430, 400, 50)) == P(430, 400),
                $"{size}: one still interval does not prematurely switch targets");
            check(motion.Scan(Fish(460, 400, 50), Fish(800, 400, 150)) == P(800, 400),
                $"{size}: a stationary fish takes priority over an easier moving fish");
            check(motion.Scan(Fish(802, 399, 150), Fish(490, 400, 50)) == P(802, 399),
                $"{size}: small detection jitter preserves the stationary preference and uses fresh coordinates");
            check(motion.Scan(Fish(520, 400, 50), Fish(840, 400, 150)) == P(520, 400),
                $"{size}: a fish that starts moving loses its stationary preference immediately");
            motion.Scan(Fish(550, 400, 50), Fish(840, 400, 150));
            check(motion.Scan(Fish(580, 400, 50), Fish(840, 400, 150)) == P(840, 400),
                $"{size}: a fish that stops can become preferred again");
            check(motion.Scan(Fish(610, 400, 50)) == P(610, 400),
                $"{size}: a stationary fish leaving the scan never keeps the cast locked");

            var slow = new Probe(size);
            slow.Scan(Fish(400, 400, 50), Fish(800, 400, 150));
            slow.Scan(Fish(405, 400, 50), Fish(800, 400, 150));
            check(slow.Scan(Fish(410, 400, 50), Fish(800, 400, 150)) == P(800, 400),
                $"{size}: accumulated slow movement is not mistaken for a stationary fish");
            slow.NoFrame();
            check(slow.Scan(Fish(420, 400, 50), Fish(800, 400, 150)) == P(420, 400),
                $"{size}: missing frames discard stationary history");
            slow.Scan(Fish(440, 400, 50), Fish(800, 400, 150));
            slow.Scan();
            check(slow.Scan(Fish(460, 400, 50), Fish(800, 400, 150)) == P(460, 400),
                $"{size}: empty scans discard stationary history");

            var anotherCast = new Probe(size);
            check(anotherCast.Scan(Fish(640, 400, 50), Fish(840, 400, 150)) == P(640, 400),
                $"{size}: stationary preference does not survive into the next cast");

            var twoStill = new Probe(size);
            twoStill.Scan(Fish(400, 400, 100), Fish(800, 400, 105));
            twoStill.Scan(Fish(800, 400, 95), Fish(400, 400, 100));
            check(twoStill.Scan(Fish(800, 400, 95), Fish(400, 400, 100)) == P(400, 400),
                $"{size}: two stationary fish retain the existing preference when power rankings fluctuate");
            check(twoStill.Scan(Fish(800, 400, 20), Fish(400, 400, 100)) == P(400, 400),
                $"{size}: a much easier alternative cannot take over a confirmed stationary target");
            check(twoStill.Scan(Fish(401, 400, 100), Fish(404, 400, 20)) == P(401, 400),
                $"{size}: a crossing fish does not steal the confirmed stationary target");
            check(twoStill.Scan(Fish(425, 400, 20), Fish(400, 400, 100)) == P(400, 400),
                $"{size}: stationary target survives ambiguous history after the crossing");
            check(twoStill.Scan(Fish(450, 400, 20)) == P(450, 400),
                $"{size}: a missing stationary target releases its preference immediately");

            var drifting = new Probe(size);
            drifting.Scan(Fish(800, 400, 150));
            drifting.Scan(Fish(800, 400, 150));
            drifting.Scan(Fish(800, 400, 150));
            drifting.Scan(Fish(804, 400, 150), Fish(500, 400, 50));
            drifting.Scan(Fish(808, 400, 150), Fish(530, 400, 50));
            check(drifting.Scan(Fish(812, 400, 150), Fish(560, 400, 50)) == P(560, 400),
                $"{size}: small successive movements cannot drag a stationary preference across the pond");

            var overlap = new Probe(size);
            overlap.Scan(Fish(400, 400, 50), Fish(800, 400, 150));
            overlap.Scan(Fish(430, 400, 50), Fish(800, 400, 150));
            check(overlap.Scan(Fish(460, 400, 50), Fish(797, 400, 150), Fish(803, 400, 150)) == P(460, 400),
                $"{size}: ambiguous overlapping shadows do not inherit stationary priority");
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
