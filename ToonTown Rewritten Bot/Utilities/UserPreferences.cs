using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Stores and persists user preferences between sessions.
    /// </summary>
    public class UserPreferences
    {
        private static readonly string PreferencesFilePath;
        private static UserPreferences _instance;
        private static readonly object _lock = new object();

        static UserPreferences()
        {
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            PreferencesFilePath = Path.Combine(exePath, "user_preferences.json");
        }

        /// <summary>
        /// Gets the singleton instance of UserPreferences.
        /// </summary>
        public static UserPreferences Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = Load();
                        }
                    }
                }
                return _instance;
            }
        }

        // Fishing preferences
        public string FishingLocation { get; set; } = "";
        public int NumberOfCasts { get; set; } = 1;
        public int NumberOfSells { get; set; } = 1;
        public int BiteTimeoutSeconds { get; set; } = 30;
        public bool RandomVariance { get; set; } = false;
        public bool AutoDetectFish { get; set; } = false;
        public bool ShowFishingOverlay { get; set; } = false;
        public string CustomFishingFile { get; set; } = "";

        // Golf preferences
        public string GolfCourse { get; set; } = "";
        public bool ShowGolfOverlay { get; set; } = true;

        // Doodle preferences
        public string DoodleTrick { get; set; } = "None";
        public int NumberOfFeeds { get; set; } = 1;
        public int NumberOfScratches { get; set; } = 1;
        public bool UnlimitedTraining { get; set; } = false;
        public bool JustFeedDoodle { get; set; } = false;
        public bool JustScratchDoodle { get; set; } = false;

        // Gardening preferences
        public int WaterPlantCount { get; set; } = 2;
        public string FlowerBeanAmount { get; set; } = "";

        // Misc preferences
        public bool KeepProgramOnTop { get; set; } = false;
        public int KeepToonAwakeMinutes { get; set; } = 1;

        /// <summary>
        /// Saves current preferences to file.
        /// </summary>
        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(PreferencesFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserPreferences] Failed to save: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads preferences from file or creates new default preferences.
        /// </summary>
        private static UserPreferences Load()
        {
            try
            {
                if (File.Exists(PreferencesFilePath))
                {
                    string json = File.ReadAllText(PreferencesFilePath);
                    var prefs = JsonConvert.DeserializeObject<UserPreferences>(json);
                    if (prefs != null)
                    {
                        return prefs;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserPreferences] Failed to load: {ex.Message}");
            }

            return new UserPreferences();
        }

        /// <summary>
        /// Resets all preferences to defaults and saves.
        /// </summary>
        public void ResetToDefaults()
        {
            FishingLocation = "";
            NumberOfCasts = 1;
            NumberOfSells = 1;
            BiteTimeoutSeconds = 30;
            RandomVariance = false;
            AutoDetectFish = false;
            ShowFishingOverlay = false;
            CustomFishingFile = "";

            GolfCourse = "";
            ShowGolfOverlay = true;

            DoodleTrick = "None";
            NumberOfFeeds = 1;
            NumberOfScratches = 1;
            UnlimitedTraining = false;
            JustFeedDoodle = false;
            JustScratchDoodle = false;

            WaterPlantCount = 2;
            FlowerBeanAmount = "";

            KeepProgramOnTop = false;
            KeepToonAwakeMinutes = 1;

            Save();
        }
    }
}
