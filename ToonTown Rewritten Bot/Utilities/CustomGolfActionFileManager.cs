using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ToonTown_Rewritten_Bot.Models;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Manages loading, saving, and migrating custom golf action files.
    /// Supports both v1 (simple array) and v2 (with metadata) formats.
    /// </summary>
    public static class CustomGolfActionFileManager
    {
        /// <summary>
        /// Result of loading an action file.
        /// </summary>
        public class LoadResult
        {
            public bool Success { get; set; }
            public CustomGolfActionFile File { get; set; }
            public string ErrorMessage { get; set; }
            public bool WasV1Format { get; set; }
        }

        /// <summary>
        /// Loads a custom golf action file, auto-detecting and migrating v1 format if needed.
        /// </summary>
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
                var token = JToken.Parse(json);

                if (token.Type == JTokenType.Array)
                {
                    // V1 format: just an array of GolfActionCommand
                    result.WasV1Format = true;
                    var actions = token.ToObject<List<GolfActionCommand>>();

                    result.File = new CustomGolfActionFile
                    {
                        Version = 1,
                        Name = Path.GetFileNameWithoutExtension(filePath),
                        Actions = actions ?? new List<GolfActionCommand>()
                    };
                    result.Success = true;
                }
                else if (token.Type == JTokenType.Object)
                {
                    var jObject = (JObject)token;

                    if (jObject.ContainsKey("Version") && jObject.ContainsKey("Actions"))
                    {
                        result.File = jObject.ToObject<CustomGolfActionFile>();
                        result.WasV1Format = false;
                        result.Success = true;
                    }
                    else
                    {
                        result.ErrorMessage = "Unrecognized file format.";
                    }
                }
                else
                {
                    result.ErrorMessage = "Invalid JSON format.";
                }
            }
            catch (JsonException ex)
            {
                result.ErrorMessage = $"JSON parsing error: {ex.Message}";
                Debug.WriteLine($"[CustomGolfActionFileManager] Load error: {ex}");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Error loading file: {ex.Message}";
                Debug.WriteLine($"[CustomGolfActionFileManager] Load error: {ex}");
            }

            return result;
        }

        /// <summary>
        /// Saves a custom golf action file in v2 format.
        /// </summary>
        public static bool Save(CustomGolfActionFile file, string filePath)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                file.Version = 2;
                file.ModifiedAt = DateTime.Now;
                if (file.CreatedAt == default)
                {
                    file.CreatedAt = DateTime.Now;
                }

                string json = JsonConvert.SerializeObject(file, Formatting.Indented);
                File.WriteAllText(filePath, json);

                Debug.WriteLine($"[CustomGolfActionFileManager] Saved: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomGolfActionFileManager] Save error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Saves actions in v1 format (for backward compatibility).
        /// </summary>
        public static bool SaveV1(List<GolfActionCommand> actions, string filePath)
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
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomGolfActionFileManager] SaveV1 error: {ex}");
                return false;
            }
        }
    }
}
