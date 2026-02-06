using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Manages the scan area for gardening jellybean detection.
    /// Saves/loads from GardeningScanArea.json in the Templates folder.
    /// </summary>
    public static class GardeningScanAreaManager
    {
        private static readonly string TemplatesFolder = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Templates");

        private static readonly string ScanAreaFile = Path.Combine(
            TemplatesFolder, "GardeningScanArea.json");

        private static GardeningScanAreaData _scanAreaData;

        /// <summary>
        /// Data structure for storing the jellybean panel scan area.
        /// Coordinates are stored as percentages (0-100) of window size for resolution independence.
        /// </summary>
        public class GardeningScanAreaData
        {
            /// <summary>
            /// Scan area for the jellybean selection panel.
            /// </summary>
            public ScanAreaPercent JellybeanPanel { get; set; } = new ScanAreaPercent
            {
                // Default: roughly center-left of screen where jellybeans appear
                XPercent = 20f,
                YPercent = 30f,
                WidthPercent = 25f,
                HeightPercent = 40f
            };

            public DateTime LastModified { get; set; } = DateTime.Now;
        }

        public class ScanAreaPercent
        {
            public float XPercent { get; set; }
            public float YPercent { get; set; }
            public float WidthPercent { get; set; }
            public float HeightPercent { get; set; }

            /// <summary>
            /// Converts the percentage-based scan area to pixel coordinates.
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

            /// <summary>
            /// Sets values from a pixel rectangle.
            /// </summary>
            public void FromRectangle(Rectangle rect, int windowWidth, int windowHeight)
            {
                XPercent = (float)rect.X / windowWidth * 100f;
                YPercent = (float)rect.Y / windowHeight * 100f;
                WidthPercent = (float)rect.Width / windowWidth * 100f;
                HeightPercent = (float)rect.Height / windowHeight * 100f;
            }
        }

        /// <summary>
        /// Loads the scan area data from disk.
        /// </summary>
        private static void EnsureLoaded()
        {
            if (_scanAreaData != null) return;

            _scanAreaData = new GardeningScanAreaData();

            if (File.Exists(ScanAreaFile))
            {
                try
                {
                    string json = File.ReadAllText(ScanAreaFile);
                    var loaded = JsonConvert.DeserializeObject<GardeningScanAreaData>(json);
                    if (loaded != null)
                    {
                        _scanAreaData = loaded;
                    }
                    System.Diagnostics.Debug.WriteLine("[GardeningScanAreaManager] Loaded scan area data");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GardeningScanAreaManager] Error loading: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Saves the scan area data to disk.
        /// </summary>
        private static void Save()
        {
            try
            {
                if (!Directory.Exists(TemplatesFolder))
                {
                    Directory.CreateDirectory(TemplatesFolder);
                }

                _scanAreaData.LastModified = DateTime.Now;
                string json = JsonConvert.SerializeObject(_scanAreaData, Formatting.Indented);
                File.WriteAllText(ScanAreaFile, json);
                System.Diagnostics.Debug.WriteLine("[GardeningScanAreaManager] Saved scan area data");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GardeningScanAreaManager] Error saving: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the jellybean panel scan area in pixels.
        /// </summary>
        public static Rectangle GetJellybeanPanelArea(int windowWidth, int windowHeight)
        {
            EnsureLoaded();
            return _scanAreaData.JellybeanPanel.ToRectangle(windowWidth, windowHeight);
        }

        /// <summary>
        /// Sets the jellybean panel scan area.
        /// </summary>
        public static void SetJellybeanPanelArea(Rectangle area, int windowWidth, int windowHeight)
        {
            EnsureLoaded();
            _scanAreaData.JellybeanPanel.FromRectangle(area, windowWidth, windowHeight);
            Save();
            System.Diagnostics.Debug.WriteLine($"[GardeningScanAreaManager] Set jellybean panel area: {area}");
        }

        /// <summary>
        /// Checks if a custom scan area has been configured.
        /// </summary>
        public static bool HasCustomScanArea()
        {
            return File.Exists(ScanAreaFile);
        }

        /// <summary>
        /// Resets to default scan area.
        /// </summary>
        public static void ResetToDefault()
        {
            _scanAreaData = new GardeningScanAreaData();
            if (File.Exists(ScanAreaFile))
            {
                try
                {
                    File.Delete(ScanAreaFile);
                }
                catch { }
            }
            System.Diagnostics.Debug.WriteLine("[GardeningScanAreaManager] Reset to default");
        }

        /// <summary>
        /// Reloads from disk.
        /// </summary>
        public static void Reload()
        {
            _scanAreaData = null;
            EnsureLoaded();
        }
    }
}
