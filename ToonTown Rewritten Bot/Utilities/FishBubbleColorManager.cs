using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Manages fish bubble color configurations per fishing location.
    /// Colors are captured by the user instead of being hardcoded.
    /// </summary>
    public class FishBubbleColorManager
    {
        private static FishBubbleColorManager _instance;
        private static readonly object _lock = new object();

        private Dictionary<string, FishBubbleColorConfig> _colorConfigs;
        private readonly string _dataFilePath;

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static FishBubbleColorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new FishBubbleColorManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private FishBubbleColorManager()
        {
            string templatesFolder = GetTemplatesFolder();
            _dataFilePath = Path.Combine(templatesFolder, "FishBubbleColors.json");
            LoadColorConfigs();
        }

        /// <summary>
        /// Gets the color config for a fishing location, prompting user to capture if none exists.
        /// </summary>
        /// <param name="locationName">The fishing location name</param>
        /// <returns>The color config, or null if user cancelled</returns>
        public FishBubbleColorConfig GetColorConfig(string locationName)
        {
            string normalizedName = NormalizeLocationName(locationName);

            if (_colorConfigs.TryGetValue(normalizedName, out var config))
            {
                return config;
            }

            // No config exists - prompt user to capture
            System.Diagnostics.Debug.WriteLine($"[FishBubbleColorManager] No color config for {locationName}, prompting capture...");

            if (PromptForColorCapture(locationName))
            {
                if (_colorConfigs.TryGetValue(normalizedName, out config))
                {
                    return config;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a color config exists for a location.
        /// </summary>
        public bool HasColorConfig(string locationName)
        {
            string normalizedName = NormalizeLocationName(locationName);
            return _colorConfigs.ContainsKey(normalizedName);
        }

        /// <summary>
        /// Prompts the user to capture a fish bubble color.
        /// </summary>
        /// <returns>True if color was captured successfully</returns>
        public bool PromptForColorCapture(string locationName)
        {
            // Must run on UI thread
            if (System.Windows.Forms.Application.OpenForms.Count > 0 &&
                System.Windows.Forms.Application.OpenForms[0].InvokeRequired)
            {
                bool result = false;
                System.Windows.Forms.Application.OpenForms[0].Invoke(new Action(() =>
                {
                    ToonTown_Rewritten_Bot.Services.CoreFunctionality.BringBotWindowToFront();
                    result = ShowCaptureForm(locationName);
                }));
                return result;
            }

            ToonTown_Rewritten_Bot.Services.CoreFunctionality.BringBotWindowToFront();
            return ShowCaptureForm(locationName);
        }

        private bool ShowCaptureForm(string locationName)
        {
            using (var form = new Views.FishBubbleColorCaptureForm(locationName))
            {
                var result = form.ShowDialog();
                return result == System.Windows.Forms.DialogResult.OK && form.ColorCaptured;
            }
        }

        /// <summary>
        /// Saves a color config for a location.
        /// </summary>
        public void SaveColorConfig(string locationName, Color bubbleColor, Tolerance tolerance, Rectangle scanArea, int yAdjustment = 0)
        {
            string normalizedName = NormalizeLocationName(locationName);

            var config = new FishBubbleColorConfig
            {
                LocationName = locationName,
                BubbleColorR = bubbleColor.R,
                BubbleColorG = bubbleColor.G,
                BubbleColorB = bubbleColor.B,
                ToleranceR = tolerance.R,
                ToleranceG = tolerance.G,
                ToleranceB = tolerance.B,
                ScanAreaX = scanArea.X,
                ScanAreaY = scanArea.Y,
                ScanAreaWidth = scanArea.Width,
                ScanAreaHeight = scanArea.Height,
                YAdjustment = yAdjustment,
                CapturedAt = DateTime.Now
            };

            _colorConfigs[normalizedName] = config;
            SaveColorConfigs();

            System.Diagnostics.Debug.WriteLine($"[FishBubbleColorManager] Saved color config for {locationName}: " +
                $"Color=({bubbleColor.R},{bubbleColor.G},{bubbleColor.B}), Tolerance=({tolerance.R},{tolerance.G},{tolerance.B})");
        }

        /// <summary>
        /// Deletes the color config for a location.
        /// </summary>
        public void DeleteColorConfig(string locationName)
        {
            string normalizedName = NormalizeLocationName(locationName);
            if (_colorConfigs.Remove(normalizedName))
            {
                SaveColorConfigs();
            }
        }

        /// <summary>
        /// Gets all location names with saved configs.
        /// </summary>
        public IEnumerable<string> GetAllLocationNames()
        {
            return _colorConfigs.Keys;
        }

        private void LoadColorConfigs()
        {
            _colorConfigs = new Dictionary<string, FishBubbleColorConfig>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(_dataFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_dataFilePath);
                    var loaded = JsonConvert.DeserializeObject<Dictionary<string, FishBubbleColorConfig>>(json);
                    if (loaded != null)
                    {
                        _colorConfigs = new Dictionary<string, FishBubbleColorConfig>(loaded, StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FishBubbleColorManager] Error loading data: {ex.Message}");
                }
            }
        }

        private void SaveColorConfigs()
        {
            try
            {
                string directory = Path.GetDirectoryName(_dataFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(_colorConfigs, Formatting.Indented);
                File.WriteAllText(_dataFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FishBubbleColorManager] Error saving data: {ex.Message}");
            }
        }

        private string GetTemplatesFolder()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Navigate up from bin/Debug/net10.0-windows to find the project folder
            DirectoryInfo dir = new DirectoryInfo(baseDir);
            while (dir != null && dir.Parent != null)
            {
                if (Directory.GetFiles(dir.FullName, "*.csproj").Length > 0)
                {
                    string projectTemplates = Path.Combine(dir.FullName, "Templates");
                    if (!Directory.Exists(projectTemplates))
                        Directory.CreateDirectory(projectTemplates);
                    return projectTemplates;
                }
                dir = dir.Parent;
            }

            // Fall back to output directory
            string fallback = Path.Combine(baseDir, "Templates");
            if (!Directory.Exists(fallback))
                Directory.CreateDirectory(fallback);
            return fallback;
        }

        private string NormalizeLocationName(string locationName)
        {
            return locationName?.ToUpperInvariant().Trim() ?? "";
        }
    }

    /// <summary>
    /// Stored configuration for fish bubble detection at a location.
    /// </summary>
    public class FishBubbleColorConfig
    {
        public string LocationName { get; set; }

        // Bubble color RGB
        public int BubbleColorR { get; set; }
        public int BubbleColorG { get; set; }
        public int BubbleColorB { get; set; }

        // Color tolerance RGB
        public int ToleranceR { get; set; }
        public int ToleranceG { get; set; }
        public int ToleranceB { get; set; }

        // Scan area (reference coordinates)
        public int ScanAreaX { get; set; }
        public int ScanAreaY { get; set; }
        public int ScanAreaWidth { get; set; }
        public int ScanAreaHeight { get; set; }

        // Y adjustment for casting calculation
        public int YAdjustment { get; set; }

        public DateTime CapturedAt { get; set; }

        /// <summary>
        /// Gets the bubble color as a Color object.
        /// </summary>
        [JsonIgnore]
        public Color BubbleColor => Color.FromArgb(BubbleColorR, BubbleColorG, BubbleColorB);

        /// <summary>
        /// Gets the tolerance as a Tolerance object.
        /// </summary>
        [JsonIgnore]
        public Tolerance ColorTolerance => new Tolerance(ToleranceR, ToleranceG, ToleranceB);

        /// <summary>
        /// Gets the scan area as a Rectangle.
        /// </summary>
        [JsonIgnore]
        public Rectangle ScanArea => new Rectangle(ScanAreaX, ScanAreaY, ScanAreaWidth, ScanAreaHeight);
    }
}
