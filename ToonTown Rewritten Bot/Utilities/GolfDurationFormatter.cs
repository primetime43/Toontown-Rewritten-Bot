using System;
using System.Text;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Formats golf action durations with meaningful descriptions.
    /// </summary>
    public static class GolfDurationFormatter
    {
        // Power swing constants (CTRL key hold time)
        private const int MIN_POWER_MS = 0;
        private const int MAX_POWER_MS = 2500; // Full power at ~2.5 seconds

        /// <summary>
        /// Formats a duration with contextual meaning based on the action type.
        /// </summary>
        public static string FormatDuration(string action, int durationMs)
        {
            return action switch
            {
                "SWING POWER" => FormatPowerDuration(durationMs),
                "TURN LEFT" or "TURN RIGHT" => FormatTurnDuration(durationMs),
                "DELAY TIME" => FormatDelayDuration(durationMs),
                "AIM STRAIGHT" => $"{durationMs}ms",
                _ => $"{durationMs}ms"
            };
        }

        /// <summary>
        /// Formats power swing duration showing percentage.
        /// </summary>
        public static string FormatPowerDuration(int durationMs)
        {
            int percentage = CalculatePowerPercentage(durationMs);
            return $"{durationMs}ms ({percentage}% power)";
        }

        /// <summary>
        /// Calculates power percentage from duration.
        /// </summary>
        public static int CalculatePowerPercentage(int durationMs)
        {
            if (durationMs <= MIN_POWER_MS) return 0;
            if (durationMs >= MAX_POWER_MS) return 100;
            return (int)Math.Round((durationMs / (double)MAX_POWER_MS) * 100);
        }

        /// <summary>
        /// Calculates duration from power percentage.
        /// </summary>
        public static int CalculateDurationFromPercentage(int percentage)
        {
            if (percentage <= 0) return MIN_POWER_MS;
            if (percentage >= 100) return MAX_POWER_MS;
            return (int)Math.Round((percentage / 100.0) * MAX_POWER_MS);
        }

        /// <summary>
        /// Formats turn duration with description.
        /// </summary>
        public static string FormatTurnDuration(int durationMs)
        {
            string description = durationMs switch
            {
                <= 50 => "tiny",
                <= 100 => "small",
                <= 150 => "medium",
                <= 200 => "large",
                _ => "very large"
            };
            return $"{durationMs}ms ({description} turn)";
        }

        /// <summary>
        /// Formats delay duration in seconds.
        /// </summary>
        public static string FormatDelayDuration(int durationMs)
        {
            double seconds = durationMs / 1000.0;
            return $"{durationMs}ms ({seconds:F1}s delay)";
        }

        /// <summary>
        /// Gets a symbol/arrow for the action type.
        /// </summary>
        public static string GetActionSymbol(string action)
        {
            return action switch
            {
                "SWING POWER" => "⚡",
                "TURN LEFT" => "↶",
                "TURN RIGHT" => "↷",
                "AIM STRAIGHT" => "↑",
                "DELAY TIME" => "⏱",
                "MOVE TO LEFT TEE SPOT" => "◀",
                "MOVE TO RIGHT TEE SPOT" => "▶",
                _ => "•"
            };
        }

        /// <summary>
        /// Gets a short description for the action.
        /// </summary>
        public static string GetShortActionName(string action)
        {
            return action switch
            {
                "SWING POWER" => "Power",
                "TURN LEFT" => "Left",
                "TURN RIGHT" => "Right",
                "AIM STRAIGHT" => "Straight",
                "DELAY TIME" => "Wait",
                "MOVE TO LEFT TEE SPOT" => "Pos L",
                "MOVE TO RIGHT TEE SPOT" => "Pos R",
                _ => action
            };
        }

        /// <summary>
        /// Builds a visual preview string for a sequence of actions.
        /// Format: ↶ 100ms → ⚡ 72% → ↷ 50ms
        /// </summary>
        public static string BuildSequencePreview(System.Collections.Generic.List<ToonTown_Rewritten_Bot.Models.GolfActionCommand> actions)
        {
            if (actions == null || actions.Count == 0)
                return "(No actions)";

            var preview = new StringBuilder();
            bool first = true;

            foreach (var action in actions)
            {
                if (!first)
                    preview.Append(" → ");
                first = false;

                string symbol = GetActionSymbol(action.Action);

                if (action.Action == "SWING POWER")
                {
                    int pct = CalculatePowerPercentage(action.Duration);
                    preview.Append($"{symbol} {pct}%");
                }
                else if (action.Action == "DELAY TIME")
                {
                    double secs = action.Duration / 1000.0;
                    preview.Append($"{symbol} {secs:F1}s");
                }
                else if (action.Action == "MOVE TO LEFT TEE SPOT" || action.Action == "MOVE TO RIGHT TEE SPOT")
                {
                    preview.Append($"{symbol}");
                }
                else
                {
                    preview.Append($"{symbol} {action.Duration}ms");
                }
            }

            return preview.ToString();
        }

        /// <summary>
        /// Calculates total sequence duration.
        /// </summary>
        public static int CalculateTotalDuration(System.Collections.Generic.List<ToonTown_Rewritten_Bot.Models.GolfActionCommand> actions)
        {
            if (actions == null) return 0;

            int total = 0;
            foreach (var action in actions)
            {
                // Position moves are skipped during execution
                if (action.Action != "MOVE TO LEFT TEE SPOT" && action.Action != "MOVE TO RIGHT TEE SPOT")
                {
                    total += action.Duration;
                }
            }
            return total;
        }

        /// <summary>
        /// Gets summary statistics for a sequence.
        /// </summary>
        public static (int totalMs, int powerPct, int netTurnMs, string teePosition) GetSequenceSummary(
            System.Collections.Generic.List<ToonTown_Rewritten_Bot.Models.GolfActionCommand> actions)
        {
            if (actions == null || actions.Count == 0)
                return (0, 0, 0, "Center");

            int totalMs = 0;
            int powerPct = 0;
            int netTurnMs = 0;
            string teePosition = "Center";

            foreach (var action in actions)
            {
                switch (action.Action)
                {
                    case "SWING POWER":
                        powerPct = CalculatePowerPercentage(action.Duration);
                        totalMs += action.Duration;
                        break;
                    case "TURN LEFT":
                        netTurnMs -= action.Duration;
                        totalMs += action.Duration;
                        break;
                    case "TURN RIGHT":
                        netTurnMs += action.Duration;
                        totalMs += action.Duration;
                        break;
                    case "MOVE TO LEFT TEE SPOT":
                        teePosition = "Left";
                        break;
                    case "MOVE TO RIGHT TEE SPOT":
                        teePosition = "Right";
                        break;
                    case "DELAY TIME":
                        totalMs += action.Duration;
                        break;
                    default:
                        totalMs += action.Duration;
                        break;
                }
            }

            return (totalMs, powerPct, netTurnMs, teePosition);
        }
    }
}
