using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Manages custom user-defined pond and fish shadow colors per fishing location.
    /// Saves/loads from PondColors.json in the Templates folder.
    /// </summary>
    public static class PondColorManager
    {
        private static readonly string TemplatesFolder = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Templates");

        private static readonly string PondColorsFile = Path.Combine(
            TemplatesFolder, "PondColors.json");

        private static Dictionary<string, PondColorData> _pondColors;

        /// <summary>
        /// Data structure for storing pond color info per location.
        /// </summary>
        public class PondColorData
        {
            public int WaterR { get; set; }
            public int WaterG { get; set; }
            public int WaterB { get; set; }
            public int ShadowR { get; set; }
            public int ShadowG { get; set; }
            public int ShadowB { get; set; }
            public int ToleranceR { get; set; } = 15;
            public int ToleranceG { get; set; } = 15;
            public int ToleranceB { get; set; } = 15;
            public DateTime LastModified { get; set; }

            [JsonIgnore]
            public Color WaterColor => Color.FromArgb(WaterR, WaterG, WaterB);

            [JsonIgnore]
            public Color ShadowColor => Color.FromArgb(ShadowR, ShadowG, ShadowB);

            public PondColorData() { }

            public PondColorData(Color waterColor, Color shadowColor, int toleranceR = 15, int toleranceG = 15, int toleranceB = 15)
            {
                WaterR = waterColor.R;
                WaterG = waterColor.G;
                WaterB = waterColor.B;
                ShadowR = shadowColor.R;
                ShadowG = shadowColor.G;
                ShadowB = shadowColor.B;
                ToleranceR = toleranceR;
                ToleranceG = toleranceG;
                ToleranceB = toleranceB;
                LastModified = DateTime.Now;
            }
        }

        /// <summary>
        /// Loads pond colors from disk.
        /// </summary>
        private static void EnsureLoaded()
        {
            if (_pondColors != null) return;

            _pondColors = new Dictionary<string, PondColorData>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(PondColorsFile))
            {
                try
                {
                    string json = File.ReadAllText(PondColorsFile);
                    var loaded = JsonConvert.DeserializeObject<Dictionary<string, PondColorData>>(json);
                    if (loaded != null)
                    {
                        _pondColors = new Dictionary<string, PondColorData>(loaded, StringComparer.OrdinalIgnoreCase);
                    }
                    System.Diagnostics.Debug.WriteLine($"[PondColorManager] Loaded {_pondColors.Count} custom pond colors");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PondColorManager] Error loading: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Saves all pond colors to disk.
        /// </summary>
        private static void Save()
        {
            try
            {
                if (!Directory.Exists(TemplatesFolder))
                {
                    Directory.CreateDirectory(TemplatesFolder);
                }

                string json = JsonConvert.SerializeObject(_pondColors, Formatting.Indented);
                File.WriteAllText(PondColorsFile, json);
                System.Diagnostics.Debug.WriteLine($"[PondColorManager] Saved {_pondColors.Count} pond colors");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PondColorManager] Error saving: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the custom pond colors for a fishing location, if defined.
        /// </summary>
        public static PondColorData GetPondColors(string locationName)
        {
            EnsureLoaded();

            if (_pondColors.TryGetValue(locationName, out var data))
            {
                return data;
            }

            return null;
        }

        /// <summary>
        /// Checks if custom colors exist for the given location.
        /// </summary>
        public static bool HasCustomColors(string locationName)
        {
            EnsureLoaded();
            return _pondColors.ContainsKey(locationName);
        }

        /// <summary>
        /// Saves custom pond colors for a fishing location.
        /// </summary>
        public static void SetPondColors(string locationName, Color waterColor, Color shadowColor,
            int toleranceR = 15, int toleranceG = 15, int toleranceB = 15)
        {
            EnsureLoaded();

            _pondColors[locationName] = new PondColorData(waterColor, shadowColor, toleranceR, toleranceG, toleranceB);
            Save();

            System.Diagnostics.Debug.WriteLine($"[PondColorManager] Saved colors for '{locationName}': " +
                $"Water=({waterColor.R},{waterColor.G},{waterColor.B}), Shadow=({shadowColor.R},{shadowColor.G},{shadowColor.B})");
        }

        /// <summary>
        /// Removes custom colors for a location (reverts to default detection).
        /// </summary>
        public static void RemoveCustomColors(string locationName)
        {
            EnsureLoaded();

            if (_pondColors.Remove(locationName))
            {
                Save();
                System.Diagnostics.Debug.WriteLine($"[PondColorManager] Removed custom colors for '{locationName}'");
            }
        }

        /// <summary>
        /// Gets all locations that have custom colors defined.
        /// </summary>
        public static IEnumerable<string> GetCustomizedLocations()
        {
            EnsureLoaded();
            return _pondColors.Keys;
        }

        /// <summary>
        /// Reloads pond colors from disk.
        /// </summary>
        public static void Reload()
        {
            _pondColors = null;
            EnsureLoaded();
        }
    }
}
