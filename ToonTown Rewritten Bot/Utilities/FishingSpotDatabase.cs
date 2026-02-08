using System.Collections.Generic;
using System.Drawing;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Static registry of fishing spot configurations keyed by location name.
    /// Location-specific fishing spot data from MouseClickSimulator project.
    /// Supports both full location names and short debug UI names.
    /// </summary>
    internal static class FishingSpotDatabase
    {
        private static readonly Dictionary<string, FishingSpotConfig> FishingSpots = new()
        {
            // Toontown Central - Original working values (darker than general water)
            ["TOONTOWN CENTRAL PUNCHLINE PLACE"] = new FishingSpotConfig(
                new Rectangle(260, 196, 1089, 430),
                Color.FromArgb(20, 123, 114),
                new Tolerance(8, 8, 8),
                15
            ),
            ["TTC Punchline Place"] = new FishingSpotConfig(
                new Rectangle(260, 196, 1089, 430),
                Color.FromArgb(20, 123, 114),
                new Tolerance(8, 8, 8),
                15
            ),

            // Donald's Dreamland
            ["DONALD DREAM LAND LULLABY LANE"] = new FishingSpotConfig(
                new Rectangle(248, 239, 1244, 421),
                Color.FromArgb(55, 103, 116),
                new Tolerance(8, 14, 11),
                0
            ),
            ["DDL Lullaby Lane"] = new FishingSpotConfig(
                new Rectangle(248, 239, 1244, 421),
                Color.FromArgb(55, 103, 116),
                new Tolerance(8, 14, 11),
                0
            ),

            // The Brrrgh
            ["BRRRGH POLAR PLACE"] = new FishingSpotConfig(
                new Rectangle(153, 134, 1297, 569),
                Color.FromArgb(25, 144, 148),
                new Tolerance(10, 11, 11),
                10
            ),
            ["Brrrgh Polar Place"] = new FishingSpotConfig(
                new Rectangle(153, 134, 1297, 569),
                Color.FromArgb(25, 144, 148),
                new Tolerance(10, 11, 11),
                10
            ),
            ["BRRRGH WALRUS WAY"] = new FishingSpotConfig(
                new Rectangle(153, 134, 1297, 569),
                Color.FromArgb(25, 144, 148),
                new Tolerance(10, 11, 11),
                10
            ),
            ["Brrrgh Walrus Way"] = new FishingSpotConfig(
                new Rectangle(153, 134, 1297, 569),
                Color.FromArgb(25, 144, 148),
                new Tolerance(10, 11, 11),
                10
            ),
            ["BRRRGH SLEET STREET"] = new FishingSpotConfig(
                new Rectangle(153, 134, 1297, 569),
                Color.FromArgb(25, 144, 148),
                new Tolerance(10, 11, 11),
                10
            ),
            ["Brrrgh Sleet Street"] = new FishingSpotConfig(
                new Rectangle(153, 134, 1297, 569),
                Color.FromArgb(25, 144, 148),
                new Tolerance(10, 11, 11),
                10
            ),

            // Minnie's Melodyland
            ["MINNIE'S MELODYLAND TENOR TERRACE"] = new FishingSpotConfig(
                new Rectangle(200, 150, 1292, 510),
                Color.FromArgb(56, 129, 122),
                new Tolerance(10, 10, 10),
                20
            ),
            ["MML Tenor Terrace"] = new FishingSpotConfig(
                new Rectangle(200, 150, 1292, 510),
                Color.FromArgb(56, 129, 122),
                new Tolerance(10, 10, 10),
                20
            ),

            // Donald's Dock
            ["DONALD DOCK LIGHTHOUSE LANE"] = new FishingSpotConfig(
                new Rectangle(200, 150, 1292, 510),
                Color.FromArgb(22, 140, 118),
                new Tolerance(13, 13, 15),
                15
            ),
            ["DD Lighthouse Lane"] = new FishingSpotConfig(
                new Rectangle(200, 150, 1292, 510),
                Color.FromArgb(22, 140, 118),
                new Tolerance(13, 13, 15),
                15
            ),

            // Daisy Gardens - From MouseClickSimulator
            ["DAISY'S GARDEN ELM STREET"] = new FishingSpotConfig(
                new Rectangle(200, 80, 1230, 712),
                Color.FromArgb(17, 102, 75),
                new Tolerance(5, 4, 5),
                35
            ),
            ["DG Elm Street"] = new FishingSpotConfig(
                new Rectangle(200, 80, 1230, 712),
                Color.FromArgb(17, 102, 75),
                new Tolerance(5, 4, 5),
                35
            ),

            // Estate - Far Left Dock
            ["ESTATE (FAR LEFT DOCK)"] = new FishingSpotConfig(
                new Rectangle(200, 150, 1292, 510),
                Color.FromArgb(56, 129, 122),
                new Tolerance(7, 5, 5),
                35
            ),
            ["Estate Far Left Dock"] = new FishingSpotConfig(
                new Rectangle(200, 150, 1292, 510),
                Color.FromArgb(56, 129, 122),
                new Tolerance(7, 5, 5),
                35
            ),

            // Estate (default for Fish Anywhere)
            ["FISH ANYWHERE"] = new FishingSpotConfig(
                new Rectangle(200, 150, 1292, 510),
                Color.FromArgb(56, 129, 122),
                new Tolerance(7, 5, 5),
                35
            ),
            ["Fish Anywhere"] = new FishingSpotConfig(
                new Rectangle(200, 150, 1292, 510),
                Color.FromArgb(56, 129, 122),
                new Tolerance(7, 5, 5),
                35
            ),

            // Custom fishing uses Estate settings as default
            ["CUSTOM FISHING ACTION"] = new FishingSpotConfig(
                new Rectangle(200, 150, 1292, 510),
                Color.FromArgb(56, 129, 122),
                new Tolerance(7, 5, 5),
                35
            )
        };

        /// <summary>
        /// Gets the fishing spot configuration for the given location name.
        /// Returns the "FISH ANYWHERE" default if the location is not found.
        /// </summary>
        public static FishingSpotConfig GetConfig(string locationName)
        {
            if (FishingSpots.TryGetValue(locationName, out var config))
                return config;

            return FishingSpots["FISH ANYWHERE"];
        }

        /// <summary>
        /// Normalizes a location name to a consistent key for learned color storage.
        /// </summary>
        public static string NormalizeLocationName(string locationName)
        {
            return locationName?.ToUpperInvariant()?.Trim() ?? "FISH ANYWHERE";
        }
    }
}
