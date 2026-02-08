using System;
using System.Drawing;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Handles all color classification for fish shadow detection.
    /// Determines whether a pixel color matches a fish shadow via learned colors,
    /// custom user-defined colors, location-specific config, or general dark detection.
    /// </summary>
    internal class FishColorMatcher
    {
        private readonly FishingSpotConfig _spotConfig;
        private readonly string _locationName;
        private readonly Func<Color?> _getLearnedColor;

        public FishColorMatcher(FishingSpotConfig spotConfig, string locationName, Func<Color?> getLearnedColor)
        {
            _spotConfig = spotConfig;
            _locationName = locationName;
            _getLearnedColor = getLearnedColor;
        }

        /// <summary>
        /// Consolidated entry point for pixel-level fish shadow classification.
        /// Checks learned color, custom colors, config color, and general dark detection in priority order.
        /// </summary>
        /// <param name="color">The pixel color to test.</param>
        /// <param name="usingLearnedColor">Whether learned color mode is active.</param>
        /// <param name="darkThreshold">The dark threshold for general detection.</param>
        /// <returns>True if the pixel is likely a fish shadow.</returns>
        public bool IsPixelFishShadow(Color color, bool usingLearnedColor, int darkThreshold)
        {
            if (usingLearnedColor)
            {
                return MatchesLearnedColor(color);
            }
            else if (HasCustomPondColors())
            {
                return MatchesCustomShadowColor(color);
            }
            else
            {
                return MatchesConfigColor(color) || IsFishShadowColor(color, darkThreshold);
            }
        }

        /// <summary>
        /// Checks if a color could be a fish shadow (darker than water, teal-ish).
        /// Fish shadows are in water, so they should have a teal/cyan quality.
        /// Supports various water colors including TTC green/teal and other locations.
        /// </summary>
        public bool IsFishShadowColor(Color color, int darkThreshold)
        {
            int brightness = (color.R + color.G + color.B) / 3;

            // Must be reasonably dark (fish shadows are darker than surrounding water)
            // Increased max from 110 to 130 to catch lighter shadows in TTC green water
            if (brightness > Math.Min(darkThreshold, 130))
                return false;

            // Reject very dark pixels (black text, UI borders)
            // Fish shadows are dark but not pure black
            if (brightness < 12)
                return false;

            // Reject grayscale pixels (black text has R ~ G ~ B)
            int maxChannel = Math.Max(color.R, Math.Max(color.G, color.B));
            int minChannel = Math.Min(color.R, Math.Min(color.G, color.B));
            int colorRange = maxChannel - minChannel;

            // If all channels are very similar (grayscale) AND very dark, reject - likely text
            if (colorRange < 10 && brightness < 40)
                return false;

            // Reject obvious brown/wood (dock posts) - R much higher than G and B
            if (color.R > color.G + 25 && color.R > color.B + 25)
                return false;

            // Fish shadows should have some blue/teal/green quality
            // Either G or B should be >= R (not strictly greater, allow equal)
            if (color.R > color.G + 15 && color.R > color.B + 15)
                return false;

            // Distinguish from pure grass (G much higher than B)
            // But allow teal/cyan water (G and B both present)
            // TTC water is green-tinted so we need to be more lenient
            // Only reject if B is very low compared to G AND G is high (pure grass)
            if (color.G > 80 && color.B < color.G * 0.25)
                return false;

            return true;
        }

        /// <summary>
        /// Checks if a color matches the learned/confirmed fish shadow color.
        /// </summary>
        public bool MatchesLearnedColor(Color color)
        {
            var learned = _getLearnedColor();
            if (!learned.HasValue)
                return false;

            int tolerance = 35; // Allow more variation for different lighting/fish

            return Math.Abs(color.R - learned.Value.R) <= tolerance &&
                   Math.Abs(color.G - learned.Value.G) <= tolerance &&
                   Math.Abs(color.B - learned.Value.B) <= tolerance;
        }

        /// <summary>
        /// Checks if a color matches the location-specific configured fish shadow color.
        /// Uses a wider tolerance than the strict color matching since fish shadows vary.
        /// </summary>
        public bool MatchesConfigColor(Color color)
        {
            var target = _spotConfig.BubbleColor;
            var tolerance = _spotConfig.ColorTolerance;

            // Use wider tolerance (2x config tolerance) for more reliable detection
            int tolR = Math.Max(tolerance.R * 2, 20);
            int tolG = Math.Max(tolerance.G * 2, 20);
            int tolB = Math.Max(tolerance.B * 2, 20);

            return Math.Abs(color.R - target.R) <= tolR &&
                   Math.Abs(color.G - target.G) <= tolG &&
                   Math.Abs(color.B - target.B) <= tolB;
        }

        /// <summary>
        /// Checks if a color matches the user-defined custom shadow color for this location.
        /// Custom colors are set via the pond color calibration UI.
        /// </summary>
        public bool MatchesCustomShadowColor(Color color)
        {
            var customColors = PondColorManager.GetPondColors(_locationName);
            if (customColors == null)
                return false;

            var target = customColors.ShadowColor;
            int tolR = customColors.ToleranceR;
            int tolG = customColors.ToleranceG;
            int tolB = customColors.ToleranceB;

            return Math.Abs(color.R - target.R) <= tolR &&
                   Math.Abs(color.G - target.G) <= tolG &&
                   Math.Abs(color.B - target.B) <= tolB;
        }

        /// <summary>
        /// Checks if custom pond colors are defined for the current location.
        /// </summary>
        public bool HasCustomPondColors()
        {
            return PondColorManager.HasCustomColors(_locationName);
        }

        /// <summary>
        /// Checks if two colors match within a tolerance.
        /// </summary>
        public bool IsMatchingColor(Color actual, Color target, Tolerance tolerance)
        {
            return Math.Abs(actual.R - target.R) <= tolerance.R &&
                   Math.Abs(actual.G - target.G) <= tolerance.G &&
                   Math.Abs(actual.B - target.B) <= tolerance.B;
        }

        /// <summary>
        /// Checks if a color looks like water (teal/cyan/green tones).
        /// Water in TTR varies by location - from blue/cyan to green/teal.
        /// TTC has notably green-tinted water.
        /// </summary>
        public bool IsWaterColor(Color color)
        {
            int brightness = (color.R + color.G + color.B) / 3;

            // Water should be moderately bright (relaxed range for darker/lighter water)
            if (brightness < 35 || brightness > 210)
                return false;

            // Water has G and/or B higher than R
            // Relaxed: only need one of G or B to be notably higher
            if (color.G < color.R + 5 && color.B < color.R + 5)
                return false;

            // Distinguish water from pure grass
            // Water: B is reasonably present (teal/cyan/green-teal)
            // Pure grass: G is MUCH higher than B
            // TTC water is green-tinted, so allow lower B ratio
            // Only reject if B is very low compared to G
            if (color.B < color.G * 0.4)
                return false;

            // G should be reasonably present for water
            // Relaxed B requirement for green-tinted water like TTC
            if (color.G < 50)
                return false;

            // R should be lower than G for water (more lenient)
            if (color.R > 120)
                return false;

            return true;
        }
    }
}
