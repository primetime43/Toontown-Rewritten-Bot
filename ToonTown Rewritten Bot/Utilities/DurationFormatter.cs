using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Utility class for formatting durations in a user-friendly way.
    /// </summary>
    public static class DurationFormatter
    {
        /// <summary>
        /// Formats milliseconds as a compact string (e.g., "0.8s", "1.2s", "2s").
        /// </summary>
        /// <param name="milliseconds">Duration in milliseconds</param>
        /// <returns>Formatted string with 's' suffix</returns>
        public static string FormatSeconds(long milliseconds)
        {
            double seconds = milliseconds / 1000.0;
            return seconds.ToString("0.###", CultureInfo.InvariantCulture) + "s";
        }

        /// <summary>
        /// Formats milliseconds as a compact string (e.g., "0.8s", "1.2s", "2s").
        /// </summary>
        public static string FormatSeconds(int milliseconds)
        {
            return FormatSeconds((long)milliseconds);
        }

        /// <summary>
        /// Formats milliseconds with full unit name (e.g., "800 ms", "1.2 seconds").
        /// </summary>
        public static string FormatFull(long milliseconds)
        {
            if (milliseconds < 1000)
            {
                return $"{milliseconds} ms";
            }
            else
            {
                double seconds = milliseconds / 1000.0;
                return $"{seconds:0.#} seconds";
            }
        }

        /// <summary>
        /// Parses a time string that may be in various formats and returns milliseconds.
        /// Handles: "500", "500 ms", "0.5s", "0.5 seconds", "TIME (500 milliseconds)", etc.
        /// </summary>
        /// <param name="input">Time string to parse</param>
        /// <returns>Duration in milliseconds, or 0 if parsing fails</returns>
        public static int ParseToMilliseconds(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 0;

            Match numericMatch = Regex.Match(input, @"[+-]?(?:\d+(?:[.,]\d+)?|[.,]\d+)");
            if (!numericMatch.Success)
                return 0;

            string numericValue = numericMatch.Value.Replace(',', '.');
            if (!double.TryParse(numericValue,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out double value))
            {
                return 0;
            }

            string lower = input.ToLowerInvariant();
            bool hasMilliseconds = Regex.IsMatch(lower,
                @"(?<![a-z])(?:ms|msec|msecs|millisecond|milliseconds)(?![a-z])");
            bool hasSeconds = !hasMilliseconds && Regex.IsMatch(lower,
                @"(?<![a-z])(?:s|sec|secs|second|seconds)(?![a-z])");

            double milliseconds = hasSeconds ? value * 1000.0 : value;
            if (milliseconds < 0 || milliseconds > int.MaxValue ||
                double.IsNaN(milliseconds) || double.IsInfinity(milliseconds))
            {
                return 0;
            }

            return (int)Math.Round(milliseconds, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Gets an arrow symbol for a movement direction.
        /// </summary>
        public static string GetDirectionArrow(string action)
        {
            return action?.ToUpperInvariant() switch
            {
                "WALK FORWARDS" => "↑",
                "WALK BACKWARDS" => "↓",
                "TURN LEFT" => "←",
                "TURN RIGHT" => "→",
                "UP" => "↑",
                "DOWN" => "↓",
                "LEFT" => "←",
                "RIGHT" => "→",
                _ => "?"
            };
        }

        /// <summary>
        /// Gets a display name for a movement action.
        /// </summary>
        public static string GetActionDisplayName(string action)
        {
            return action?.ToUpperInvariant() switch
            {
                "WALK FORWARDS" => "Forward",
                "WALK BACKWARDS" => "Back",
                "TURN LEFT" => "Left",
                "TURN RIGHT" => "Right",
                "SELL FISH" => "SELL",
                "TIME" => "Wait",
                _ => action ?? ""
            };
        }
    }
}
