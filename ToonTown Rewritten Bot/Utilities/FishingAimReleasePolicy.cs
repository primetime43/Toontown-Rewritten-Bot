using System;

namespace ToonTown_Rewritten_Bot.Utilities
{
    // Time is supplied by the cast's Stopwatch so clock adjustments cannot prolong a hold.
    internal sealed class FishingAimReleasePolicy
    {
        private static readonly TimeSpan MaximumAimTime = TimeSpan.FromSeconds(4);
        private static readonly TimeSpan LostTargetGrace = TimeSpan.FromSeconds(1.5);
        private TimeSpan? _lastObservedAt;

        public string GetReleaseReason(FishTargetTracker tracker, TimeSpan elapsed)
        {
            if (tracker.ObservedThisFrame)
                _lastObservedAt = elapsed;

            if (tracker.ReadyToRelease)
                return "Tracked target confirmed";

            // Finish this cast at its last aim rather than switching to another fish
            // or waiting indefinitely for a lost interior shadow to reappear.
            if (tracker.Position.HasValue && !tracker.ObservedThisFrame &&
                _lastObservedAt.HasValue && elapsed - _lastObservedAt.Value >= LostTargetGrace)
                return "Shadow lost for 1.5 seconds; using last aim";

            if (elapsed >= MaximumAimTime)
                return "Aiming reached 4-second limit; using current aim";

            return null;
        }
    }
}
