using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ToonTown_Rewritten_Bot.Models;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Manages loading, saving, and migrating custom fishing action files.
    /// Supports both v1 (simple array) and v2 (with embedded calibration) formats.
    /// </summary>
    public static class CustomFishingActionFileManager
    {
        private static readonly string CustomActionsFolder = Path.Combine(
            AppPaths.ExeDirectory, "Custom Fishing Actions");

        private static readonly string TemplatesFolder = Path.Combine(
            AppPaths.ExeDirectory, "Templates", "CustomFishingTemplates");

        /// <summary>
        /// Result of loading an action file.
        /// </summary>
        public class LoadResult
        {
            public bool Success { get; set; }
            public CustomFishingActionFile File { get; set; }
            public string ErrorMessage { get; set; }
            public bool WasV1Format { get; set; }
        }

        /// <summary>
        /// Loads a custom fishing action file, auto-detecting and migrating v1 format if needed.
        /// </summary>
        /// <param name="filePath">Full path to the JSON file</param>
        /// <returns>LoadResult containing the file or error information</returns>
        public static LoadResult Load(string filePath)
        {
            var result = new LoadResult();

            try
            {
                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = $"File not found: {filePath}";
                    return result;
                }

                string json = File.ReadAllText(filePath);

                // Try to parse as JToken to detect format
                var token = JToken.Parse(json);

                if (token.Type == JTokenType.Array)
                {
                    // V1 format: just an array of FishingActionCommand
                    result.WasV1Format = true;
                    var actions = token.ToObject<List<FishingActionCommand>>();

                    result.File = new CustomFishingActionFile
                    {
                        Version = 1, // Keep as v1 until explicitly saved
                        Name = Path.GetFileNameWithoutExtension(filePath),
                        Actions = actions ?? new List<FishingActionCommand>(),
                        Calibration = null // Will use global settings
                    };
                    result.Success = true;
                }
                else if (token.Type == JTokenType.Object)
                {
                    // V2 format: object with Version, Name, Calibration, Actions
                    var jObject = (JObject)token;

                    // Check if it has the Version property (v2) or is something else
                    if (jObject.ContainsKey("Version") && jObject.ContainsKey("Actions"))
                    {
                        result.File = jObject.ToObject<CustomFishingActionFile>();
                        result.WasV1Format = false;
                        result.Success = true;
                    }
                    else
                    {
                        // Unknown format
                        result.ErrorMessage = "Unrecognized file format. Expected v1 array or v2 object with Version and Actions.";
                    }
                }
                else
                {
                    result.ErrorMessage = "Invalid JSON format. Expected array or object.";
                }
            }
            catch (JsonException ex)
            {
                result.ErrorMessage = $"JSON parsing error: {ex.Message}";
                Debug.WriteLine($"[CustomFishingActionFileManager] Load error: {ex}");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Error loading file: {ex.Message}";
                Debug.WriteLine($"[CustomFishingActionFileManager] Load error: {ex}");
            }

            return result;
        }

        /// <summary>
        /// Saves a custom fishing action file in v2 format.
        /// </summary>
        /// <param name="file">The action file to save</param>
        /// <param name="filePath">Full path where to save</param>
        /// <returns>True if saved successfully</returns>
        public static bool Save(CustomFishingActionFile file, string filePath)
        {
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Update metadata
                file.Version = 2;
                file.ModifiedAt = DateTime.Now;
                if (file.CreatedAt == default)
                {
                    file.CreatedAt = DateTime.Now;
                }

                string json = JsonConvert.SerializeObject(file, Formatting.Indented);
                File.WriteAllText(filePath, json);

                Debug.WriteLine($"[CustomFishingActionFileManager] Saved: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomFishingActionFileManager] Save error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Saves actions in v1 format (for backward compatibility with existing workflows).
        /// </summary>
        public static bool SaveV1Format(List<FishingActionCommand> actions, string filePath)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(actions, Formatting.Indented);
                File.WriteAllText(filePath, json);

                Debug.WriteLine($"[CustomFishingActionFileManager] Saved v1 format: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomFishingActionFileManager] Save v1 error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Gets the embedded calibration from a file, or returns null to use global settings.
        /// </summary>
        public static CalibrationData GetEmbeddedCalibration(string filePath)
        {
            var result = Load(filePath);
            return result.Success ? result.File.Calibration : null;
        }

        /// <summary>
        /// Creates calibration data from current global settings for a location.
        /// </summary>
        public static CalibrationData CreateCalibrationFromGlobalSettings(string locationName, int windowWidth, int windowHeight)
        {
            var calibration = new CalibrationData();

            // Get scan area if defined
            var scanArea = CustomScanAreaManager.GetCustomScanArea(locationName, windowWidth, windowHeight);
            if (scanArea.HasValue)
            {
                var rect = scanArea.Value;
                calibration.ScanArea = new ScanAreaCalibration
                {
                    XPercent = (float)rect.X / windowWidth * 100f,
                    YPercent = (float)rect.Y / windowHeight * 100f,
                    WidthPercent = (float)rect.Width / windowWidth * 100f,
                    HeightPercent = (float)rect.Height / windowHeight * 100f
                };
            }

            // Get pond colors if defined
            var pondColors = PondColorManager.GetPondColors(locationName);
            if (pondColors != null)
            {
                calibration.PondColors = new PondColorCalibration
                {
                    WaterR = pondColors.WaterR,
                    WaterG = pondColors.WaterG,
                    WaterB = pondColors.WaterB,
                    ShadowR = pondColors.ShadowR,
                    ShadowG = pondColors.ShadowG,
                    ShadowB = pondColors.ShadowB,
                    ToleranceR = pondColors.ToleranceR,
                    ToleranceG = pondColors.ToleranceG,
                    ToleranceB = pondColors.ToleranceB
                };
            }

            // Return null if no calibration data was found
            if (calibration.ScanArea == null && calibration.PondColors == null)
            {
                return null;
            }

            return calibration;
        }

        /// <summary>
        /// Gets the scan area from embedded calibration as a Rectangle.
        /// </summary>
        public static Rectangle? GetScanAreaFromCalibration(CalibrationData calibration, int windowWidth, int windowHeight)
        {
            if (calibration?.ScanArea == null)
                return null;

            var sa = calibration.ScanArea;
            return new Rectangle(
                (int)(sa.XPercent / 100f * windowWidth),
                (int)(sa.YPercent / 100f * windowHeight),
                (int)(sa.WidthPercent / 100f * windowWidth),
                (int)(sa.HeightPercent / 100f * windowHeight)
            );
        }

        /// <summary>
        /// Gets a list of available template files.
        /// </summary>
        public static List<TemplateInfo> GetAvailableTemplates()
        {
            var templates = new List<TemplateInfo>();

            if (!Directory.Exists(TemplatesFolder))
            {
                return templates;
            }

            foreach (var file in Directory.GetFiles(TemplatesFolder, "*.json"))
            {
                var result = Load(file);
                if (result.Success)
                {
                    templates.Add(new TemplateInfo
                    {
                        FilePath = file,
                        Name = result.File.Name,
                        Description = result.File.Description
                    });
                }
            }

            return templates;
        }

        /// <summary>
        /// Copies a template to the custom actions folder with a new name.
        /// </summary>
        public static bool CopyTemplate(string templatePath, string newName)
        {
            try
            {
                var result = Load(templatePath);
                if (!result.Success)
                    return false;

                // Update the name
                result.File.Name = newName;
                result.File.CreatedAt = DateTime.Now;
                result.File.ModifiedAt = DateTime.Now;

                // Save to custom actions folder
                string destPath = Path.Combine(CustomActionsFolder, $"{newName}.json");
                return Save(result.File, destPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomFishingActionFileManager] CopyTemplate error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Gets the default custom actions folder path.
        /// </summary>
        public static string GetCustomActionsFolder()
        {
            if (!Directory.Exists(CustomActionsFolder))
            {
                Directory.CreateDirectory(CustomActionsFolder);
            }
            return CustomActionsFolder;
        }

        /// <summary>
        /// Gets the templates folder path.
        /// </summary>
        public static string GetTemplatesFolder()
        {
            if (!Directory.Exists(TemplatesFolder))
            {
                Directory.CreateDirectory(TemplatesFolder);
            }
            return TemplatesFolder;
        }
    }

    /// <summary>
    /// Information about a template file.
    /// </summary>
    public class TemplateInfo
    {
        public string FilePath { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
