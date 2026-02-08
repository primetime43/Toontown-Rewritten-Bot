using System;
using System.Linq;
using System.Text;

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

            if (seconds < 10)
            {
                // Show one decimal place for shorter durations
                return $"{seconds:0.#}s";
            }
            else
            {
                // Show as whole seconds for longer durations
                return $"{Math.Round(seconds)}s";
            }
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

            // First, try to extract just digits (handles "TIME (500 milliseconds)" format)
            string digitsOnly = new string(input.Where(char.IsDigit).ToArray());
            if (int.TryParse(digitsOnly, out int msValue))
            {
                // If the input contains "s" or "sec" but NOT "ms" or "milliseconds",
                // treat the number as seconds
                string lower = input.ToLowerInvariant();
                bool hasSeconds = lower.Contains("sec") || (lower.Contains("s") && !lower.Contains("ms") && !lower.Contains("millisec"));

                if (hasSeconds)
                {
                    // Number is in seconds, convert to milliseconds
                    // But first check if there's a decimal point we might have missed
                    return msValue * 1000;
                }

                return msValue;
            }

            // Try parsing as a decimal for formats like "0.5s"
            // Extract the numeric part including decimal point
            var numericPart = new StringBuilder();
            bool foundDecimal = false;
            foreach (char c in input)
            {
                if (char.IsDigit(c))
                {
                    numericPart.Append(c);
                }
                else if (c == '.' && !foundDecimal)
                {
                    numericPart.Append(c);
                    foundDecimal = true;
                }
            }

            if (double.TryParse(numericPart.ToString(), out double value))
            {
                string lower = input.ToLowerInvariant();
                if (lower.Contains("s") && !lower.Contains("ms"))
                {
                    // Seconds - convert to milliseconds
                    return (int)(value * 1000);
                }
                else
                {
                    // Assume milliseconds
                    return (int)value;
                }
            }

            return 0;
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
