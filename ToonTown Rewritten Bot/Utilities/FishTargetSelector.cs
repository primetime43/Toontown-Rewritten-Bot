using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ToonTown_Rewritten_Bot.Utilities
{
    // Created for each cast. Prefer shadows that have stayed still across three scans,
    // then use cast power and continuity. Always return a position from the current scan.
    internal sealed class FishTargetSelector
    {
        private Point? _previous;
        private Point? _stationaryAnchor;
        private readonly Queue<Point[]> _recentScans = new();
        private Size _frameSize;
        private const double StillRadius = 8;

        public Point? Select(FishDetectionDebugResult detection, Size frameSize)
        {
            if (_frameSize != frameSize)
            {
                _recentScans.Clear();
                _previous = null;
                _stationaryAnchor = null;
                _frameSize = frameSize;
            }
            if (detection == null)
            {
                _recentScans.Clear();
                _stationaryAnchor = null;
                return _previous = null;
            }

            var candidates = detection.AllCandidates;
            if (candidates.Count == 0)
            {
                _recentScans.Clear();
                _stationaryAnchor = null;
                return _previous = detection.BestShadowPosition;
            }

            double scaleX = Math.Max(1, frameSize.Width) / 1600.0;
            double scaleY = Math.Max(1, frameSize.Height) / 1151.0;
            double Distance(Point a, Point b) => Math.Pow((a.X - b.X) / scaleX, 2)
                + Math.Pow((a.Y - b.Y) / scaleY, 2);

            bool StayedStill(FishCandidate candidate)
            {
                if (_recentScans.Count < 2) return false;
                foreach (var scan in _recentScans)
                {
                    var matches = scan.Where(p => Distance(p, candidate.Position) <= StillRadius * StillRadius).ToArray();
                    // Nearby overlapping shadows cannot provide reliable motion evidence.
                    if (matches.Length != 1 || candidates.Count(c =>
                        Distance(c.Position, matches[0]) <= StillRadius * StillRadius) != 1)
                        return false;
                }
                return true;
            }

            // Keep a confirmed stationary target through ranking changes or a nearby
            // crossing. Anchor to its original position so small steps cannot drag the
            // preference across the pond. A missing target is released immediately.
            FishCandidate retained = _stationaryAnchor is Point anchor
                ? candidates.Where(c => Distance(c.Position, anchor) <= StillRadius * StillRadius)
                    .OrderBy(c => Distance(c.Position, anchor)).FirstOrDefault()
                : null;
            if (retained == null && _stationaryAnchor.HasValue)
            {
                _stationaryAnchor = null;
                // Do not immediately relabel a slowly drifting target as stationary
                // using the same observations that just lost its original anchor.
                _recentScans.Clear();
            }

            var stationary = candidates.Where(StayedStill).ToList();
            var eligible = stationary.Count > 0 ? stationary : candidates;
            var easiest = eligible.OrderBy(c => c.CastPower).First();

            var selected = easiest;
            if (retained != null)
                selected = retained;
            else if (_previous is Point previous)
            {
                var nearby = eligible.Select(c => new
                    {
                        Candidate = c,
                        Distance = Distance(c.Position, previous)
                    })
                    .Where(c => c.Distance <= 60 * 60)
                    .OrderBy(c => c.Distance)
                    .FirstOrDefault();

                // Small ranking changes should not move the aim to a different fish.
                // A substantially easier cast or a missing target can switch immediately.
                if (nearby != null && easiest.CastPower >= nearby.Candidate.CastPower * 0.8)
                    selected = nearby.Candidate;
            }

            if (_recentScans.Count == 2) _recentScans.Dequeue();
            _recentScans.Enqueue(candidates.Select(c => c.Position).ToArray());
            if (!_stationaryAnchor.HasValue && stationary.Contains(selected))
            {
                _stationaryAnchor = selected.Position;
                Logger.Info("FishDetect", $"Keeping stationary shadow at ({selected.Position.X},{selected.Position.Y}) while it remains detected there.");
            }
            return _previous = selected.Position;
        }
    }
}
