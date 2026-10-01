using System;
using System.Drawing;
using System.Linq;

namespace ToonTown_Rewritten_Bot.Utilities
{
    // One instance per cast. Positions and scan bounds are game-client coordinates.
    internal sealed class FishTargetTracker
    {
        public Point? Position { get; private set; }
        public bool ObservedThisFrame { get; private set; }
        public bool ReadyToRelease => ObservedThisFrame && _targetMatches >= 2;

        private PointF _velocity;
        private int _missedFrames;
        private int _targetMatches;

        public void Update(FishDetectionDebugResult detection, Size frameSize)
        {
            bool previouslyObserved = ObservedThisFrame;
            ObservedThisFrame = false;
            if (detection == null)
            {
                HoldTarget();
                return;
            }

            var scanArea = Rectangle.Intersect(detection.ScanArea, new Rectangle(Point.Empty, frameSize));
            if (Position.HasValue && !scanArea.Contains(Position.Value))
                ClearTarget();

            var candidates = detection.AllCandidates
                .Where(c => scanArea.Contains(c.Position)).ToList();
            if (candidates.Count == 0 && detection.BestShadowPosition is Point fallback && scanArea.Contains(fallback))
                candidates.Add(new FishCandidate { Position = fallback });

            if (Position is Point previous)
            {
                // Predict only two scans ahead. A long detection gap must not make the
                // search radius grow until it starts matching an unrelated shadow.
                int predictionSteps = Math.Min(_missedFrames + 1, 2);
                var predicted = new PointF(previous.X + _velocity.X * predictionSteps,
                    previous.Y + _velocity.Y * predictionSteps);
                double scaleX = Math.Max(1, frameSize.Width) / 1600.0;
                double scaleY = Math.Max(1, frameSize.Height) / 1151.0;
                var match = candidates
                    .Select(c => new
                    {
                        Candidate = c,
                        Distance = Math.Pow((c.Position.X - predicted.X) / scaleX, 2)
                            + Math.Pow((c.Position.Y - predicted.Y) / scaleY, 2)
                    })
                    .Where(c => c.Distance <= 40 * 40)
                    .OrderBy(c => c.Distance)
                    .FirstOrDefault();

                if (match != null)
                {
                    Point current = match.Candidate.Position;
                    // Matching the same moving shadow confirms the target. Requiring
                    // it to become stationary can prevent release for an entire cast.
                    _targetMatches = previouslyObserved ? _targetMatches + 1 : 0;
                    _velocity = new PointF((current.X - previous.X) / (float)(_missedFrames + 1),
                        (current.Y - previous.Y) / (float)(_missedFrames + 1));
                    _missedFrames = 0;
                    Position = current;
                    ObservedThisFrame = true;
                    return;
                }

                // The detector cannot see outside its scan rectangle. Treat a missing
                // shadow moving across that boundary as an exit, not a missed interior scan.
                if (!((RectangleF)scanArea).Contains(predicted))
                {
                    Logger.Info("Fishing", "Tracked shadow left the scan area; selecting another target.");
                    ClearTarget();
                }
                else
                {
                    HoldTarget();
                    return;
                }
            }

            var selected = candidates.OrderBy(c => c.CastPower).FirstOrDefault();
            if (selected == null) return;
            _missedFrames = 0;
            _targetMatches = 0;
            _velocity = PointF.Empty;
            Position = selected.Position;
            ObservedThisFrame = true;
            Logger.Info("Fishing", $"Locked fishing target at ({Position.Value.X},{Position.Value.Y}).");
        }

        private void HoldTarget()
        {
            if (Position.HasValue && _missedFrames == 0)
                Logger.Debug("Fishing", "Tracked shadow temporarily missing; holding its last aim.");
            _missedFrames++;
            _targetMatches = 0;
        }

        private void ClearTarget()
        {
            Position = null;
            _velocity = PointF.Empty;
            _missedFrames = 0;
            _targetMatches = 0;
        }
    }
}
