using System;
using System.Drawing;
using System.Linq;

namespace ToonTown_Rewritten_Bot.Utilities
{
    // Created for each cast. Prefer a nearby continuation only when the alternatives
    // have similar cast power; never retain a position absent from the current scan.
    internal sealed class FishTargetSelector
    {
        private Point? _previous;

        public Point? Select(FishDetectionDebugResult detection, Size frameSize)
        {
            if (detection == null)
                return _previous = null;

            var candidates = detection.AllCandidates;
            var easiest = candidates.OrderBy(c => c.CastPower).FirstOrDefault();
            if (easiest == null)
                return _previous = detection.BestShadowPosition;

            var selected = easiest;
            if (_previous is Point previous)
            {
                double scaleX = Math.Max(1, frameSize.Width) / 1600.0;
                double scaleY = Math.Max(1, frameSize.Height) / 1151.0;
                var nearby = candidates.Select(c => new
                    {
                        Candidate = c,
                        Distance = Math.Pow((c.Position.X - previous.X) / scaleX, 2)
                            + Math.Pow((c.Position.Y - previous.Y) / scaleY, 2)
                    })
                    .Where(c => c.Distance <= 60 * 60)
                    .OrderBy(c => c.Distance)
                    .FirstOrDefault();

                // Small ranking changes should not move the aim to a different fish.
                // A substantially easier cast or a missing target can switch immediately.
                if (nearby != null && easiest.CastPower >= nearby.Candidate.CastPower * 0.8)
                    selected = nearby.Candidate;
            }

            return _previous = selected.Position;
        }
    }
}
