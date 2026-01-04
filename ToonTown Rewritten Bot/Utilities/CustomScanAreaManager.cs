using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Manages custom user-defined scan areas for fishing locations.
    /// Saves/loads from CustomScanAreas.json in the Templates folder.
    /// </summary>
    public static class CustomScanAreaManager
    {
        private static readonly string TemplatesFolder = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Templates");

        private static readonly string CustomScanAreasFile = Path.Combine(
            TemplatesFolder, "CustomScanAreas.json");

        private static Dictionary<string, ScanAreaData> _customScanAreas;

        /// <summary>
        /// Data structure for storing scan area info.
        /// Coordinates are stored as percentages (0-100) of window size for resolution independence.
        /// </summary>
        public class ScanAreaData
        {
            public float XPercent { get; set; }
            public float YPercent { get; set; }
            public float WidthPercent { get; set; }
            public float HeightPercent { get; set; }
            public DateTime LastModified { get; set; }

            public ScanAreaData() { }

            public ScanAreaData(Rectangle rect, int windowWidth, int windowHeight)
            {
                XPercent = (float)rect.X / windowWidth * 100f;
                YPercent = (float)rect.Y / windowHeight * 100f;
                WidthPercent = (float)rect.Width / windowWidth * 100f;
                HeightPercent = (float)rect.Height / windowHeight * 100f;
                LastModified = DateTime.Now;
            }

            /// <summary>
            /// Converts the percentage-based scan area to pixel coordinates for the given window size.
            /// </summary>
            public Rectangle ToRectangle(int windowWidth, int windowHeight)
            {
                return new Rectangle(
                    (int)(XPercent / 100f * windowWidth),
                    (int)(YPercent / 100f * windowHeight),
                    (int)(WidthPercent / 100f * windowWidth),
                    (int)(HeightPercent / 100f * windowHeight)
                );
            }
        }

        /// <summary>
        /// Loads custom scan areas from disk.
        /// </summary>
        private static void EnsureLoaded()
        {
            if (_customScanAreas != null) return;

            _customScanAreas = new Dictionary<string, ScanAreaData>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(CustomScanAreasFile))
            {
                try
                {
                    string json = File.ReadAllText(CustomScanAreasFile);
                    var loaded = JsonConvert.DeserializeObject<Dictionary<string, ScanAreaData>>(json);
                    if (loaded != null)
                    {
                        _customScanAreas = new Dictionary<string, ScanAreaData>(loaded, StringComparer.OrdinalIgnoreCase);
                    }
                    System.Diagnostics.Debug.WriteLine($"[CustomScanAreaManager] Loaded {_customScanAreas.Count} custom scan areas");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CustomScanAreaManager] Error loading: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Saves all custom scan areas to disk.
        /// </summary>
        private static void Save()
        {
            try
            {
                if (!Directory.Exists(TemplatesFolder))
                {
                    Directory.CreateDirectory(TemplatesFolder);
                }

                string json = JsonConvert.SerializeObject(_customScanAreas, Formatting.Indented);
                File.WriteAllText(CustomScanAreasFile, json);
                System.Diagnostics.Debug.WriteLine($"[CustomScanAreaManager] Saved {_customScanAreas.Count} custom scan areas");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomScanAreaManager] Error saving: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the custom scan area for a fishing location, if one exists.
        /// </summary>
        /// <param name="locationName">The fishing location name</param>
        /// <param name="windowWidth">Current game window width</param>
        /// <param name="windowHeight">Current game window height</param>
        /// <returns>The custom scan area rectangle, or null if none defined</returns>
        public static Rectangle? GetCustomScanArea(string locationName, int windowWidth, int windowHeight)
        {
            EnsureLoaded();

            if (_customScanAreas.TryGetValue(locationName, out var data))
            {
                return data.ToRectangle(windowWidth, windowHeight);
            }

            return null;
        }

        /// <summary>
        /// Checks if a custom scan area exists for the given location.
        /// </summary>
        public static bool HasCustomScanArea(string locationName)
        {
            EnsureLoaded();
            return _customScanAreas.ContainsKey(locationName);
        }

        /// <summary>
        /// Saves a custom scan area for a fishing location.
        /// </summary>
        /// <param name="locationName">The fishing location name</param>
        /// <param name="scanArea">The scan area rectangle in pixels</param>
        /// <param name="windowWidth">Current game window width</param>
        /// <param name="windowHeight">Current game window height</param>
        public static void SetCustomScanArea(string locationName, Rectangle scanArea, int windowWidth, int windowHeight)
        {
            EnsureLoaded();

            _customScanAreas[locationName] = new ScanAreaData(scanArea, windowWidth, windowHeight);
            Save();

            System.Diagnostics.Debug.WriteLine($"[CustomScanAreaManager] Saved custom scan area for '{locationName}': {scanArea}");
        }

        /// <summary>
        /// Removes the custom scan area for a location (reverts to default).
        /// </summary>
        public static void RemoveCustomScanArea(string locationName)
        {
            EnsureLoaded();

            if (_customScanAreas.Remove(locationName))
            {
                Save();
                System.Diagnostics.Debug.WriteLine($"[CustomScanAreaManager] Removed custom scan area for '{locationName}'");
            }
        }

        /// <summary>
        /// Gets all locations that have custom scan areas defined.
        /// </summary>
        public static IEnumerable<string> GetCustomizedLocations()
        {
            EnsureLoaded();
            return _customScanAreas.Keys;
        }

        /// <summary>
        /// Reloads custom scan areas from disk (useful if file was modified externally).
        /// </summary>
        public static void Reload()
        {
            _customScanAreas = null;
            EnsureLoaded();
        }
    }
}
