using System.Collections.Generic;
using System.Text;
using ToonTown_Rewritten_Bot.Models;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Formats gardening action durations with meaningful descriptions.
    /// </summary>
    public static class GardeningDurationFormatter
    {
        /// <summary>
        /// Formats a duration with contextual meaning based on the action type.
        /// </summary>
        public static string FormatDuration(GardeningActionCommand action)
        {
            return action.Action switch
            {
                "WALK FORWARD" or "WALK BACKWARD" or "WALK LEFT" or "WALK RIGHT" =>
                    FormatWalkDuration(action.Duration),
                "TURN LEFT" or "TURN RIGHT" =>
                    FormatTurnDuration(action.Duration),
                "DELAY" =>
                    FormatDelayDuration(action.Duration),
                "PLANT FLOWER" =>
                    FormatPlantFlower(action),
                "WATER PLANT" =>
                    FormatWaterPlant(action),
                "REMOVE PLANT" =>
                    "Remove current plant",
                _ => $"{action.Duration}ms"
            };
        }

        /// <summary>
        /// Formats walk duration with distance estimate.
        /// </summary>
        public static string FormatWalkDuration(int durationMs)
        {
            string description = durationMs switch
            {
                <= 300 => "very short",
                <= 600 => "short",
                <= 1000 => "medium",
                <= 2000 => "long",
                _ => "very long"
            };
            double seconds = durationMs / 1000.0;
            return $"{seconds:F1}s ({description} walk)";
        }

        /// <summary>
        /// Formats turn duration with turn size.
        /// </summary>
        public static string FormatTurnDuration(int durationMs)
        {
            string description = durationMs switch
            {
                <= 100 => "slight",
                <= 250 => "small",
                <= 500 => "medium",
                <= 1000 => "large",
                _ => "full"
            };
            return $"{durationMs}ms ({description} turn)";
        }

        /// <summary>
        /// Formats delay duration in seconds.
        /// </summary>
        public static string FormatDelayDuration(int durationMs)
        {
            double seconds = durationMs / 1000.0;
            return $"{seconds:F1}s delay";
        }

        /// <summary>
        /// Formats plant flower action.
        /// </summary>
        public static string FormatPlantFlower(GardeningActionCommand action)
        {
            if (!string.IsNullOrEmpty(action.FlowerName))
                return $"Plant {action.FlowerName}";
            if (!string.IsNullOrEmpty(action.BeanSequence))
                return $"Plant ({action.BeanSequence.Length} beans: {action.BeanSequence})";
            return "Plant flower";
        }

        /// <summary>
        /// Formats water plant action.
        /// </summary>
        public static string FormatWaterPlant(GardeningActionCommand action)
        {
            int count = action.WaterCount > 0 ? action.WaterCount : 1;
            return $"Water {count}x";
        }

        /// <summary>
        /// Gets a symbol/arrow for the action type.
        /// </summary>
        public static string GetActionSymbol(string action)
        {
            return action switch
            {
                "WALK FORWARD" => "↑",
                "WALK BACKWARD" => "↓",
                "WALK LEFT" => "←",
                "WALK RIGHT" => "→",
                "TURN LEFT" => "↶",
                "TURN RIGHT" => "↷",
                "PLANT FLOWER" => "🌸",
                "WATER PLANT" => "💧",
                "REMOVE PLANT" => "✂",
                "DELAY" => "⏱",
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
                "WALK FORWARD" => "Fwd",
                "WALK BACKWARD" => "Back",
                "WALK LEFT" => "Left",
                "WALK RIGHT" => "Right",
                "TURN LEFT" => "TurnL",
                "TURN RIGHT" => "TurnR",
                "PLANT FLOWER" => "Plant",
                "WATER PLANT" => "Water",
                "REMOVE PLANT" => "Remove",
                "DELAY" => "Wait",
                _ => action
            };
        }

        /// <summary>
        /// Builds a visual preview string for a sequence of actions.
        /// </summary>
        public static string BuildSequencePreview(List<GardeningActionCommand> actions)
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

                switch (action.Action)
                {
                    case "WALK FORWARD":
                    case "WALK BACKWARD":
                    case "WALK LEFT":
                    case "WALK RIGHT":
                        double secs = action.Duration / 1000.0;
                        preview.Append($"{symbol} {secs:F1}s");
                        break;
                    case "TURN LEFT":
                    case "TURN RIGHT":
                        preview.Append($"{symbol} {action.Duration}ms");
                        break;
                    case "PLANT FLOWER":
                        if (!string.IsNullOrEmpty(action.FlowerName))
                            preview.Append($"{symbol} {action.FlowerName}");
                        else
                            preview.Append($"{symbol}");
                        break;
                    case "WATER PLANT":
                        int count = action.WaterCount > 0 ? action.WaterCount : 1;
                        preview.Append($"{symbol}{count}x");
                        break;
                    case "REMOVE PLANT":
                        preview.Append($"{symbol}");
                        break;
                    case "DELAY":
                        double delaySecs = action.Duration / 1000.0;
                        preview.Append($"{symbol} {delaySecs:F1}s");
                        break;
                    default:
                        preview.Append($"{symbol}");
                        break;
                }
            }

            return preview.ToString();
        }

        /// <summary>
        /// Gets summary statistics for a sequence.
        /// </summary>
        public static (int totalActions, int plantCount, int waterCount, int removeCount, int estimatedTimeMs) GetSequenceSummary(
            List<GardeningActionCommand> actions)
        {
            if (actions == null || actions.Count == 0)
                return (0, 0, 0, 0, 0);

            int plantCount = 0;
            int waterCount = 0;
            int removeCount = 0;
            int estimatedTimeMs = 0;

            foreach (var action in actions)
            {
                switch (action.Action)
                {
                    case "WALK FORWARD":
                    case "WALK BACKWARD":
                    case "WALK LEFT":
                    case "WALK RIGHT":
                    case "TURN LEFT":
                    case "TURN RIGHT":
                    case "DELAY":
                        estimatedTimeMs += action.Duration;
                        break;
                    case "PLANT FLOWER":
                        plantCount++;
                        // Estimate: 2s per bean + 8s plant animation + 3x water (12s)
                        int beanCount = action.BeanSequence?.Length ?? 1;
                        estimatedTimeMs += (beanCount * 2000) + 8000 + 12000;
                        break;
                    case "WATER PLANT":
                        int wCount = action.WaterCount > 0 ? action.WaterCount : 1;
                        waterCount += wCount;
                        estimatedTimeMs += wCount * 4000; // 4s per water
                        break;
                    case "REMOVE PLANT":
                        removeCount++;
                        estimatedTimeMs += 3000; // ~3s for removal
                        break;
                }
            }

            return (actions.Count, plantCount, waterCount, removeCount, estimatedTimeMs);
        }
    }
}
