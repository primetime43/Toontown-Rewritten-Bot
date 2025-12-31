using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Manages template definitions stored in a JSON file.
    /// This allows users to add new template items without modifying code.
    /// </summary>
    public class TemplateDefinitionManager
    {
        private static readonly Lazy<TemplateDefinitionManager> _instance =
            new Lazy<TemplateDefinitionManager>(() => new TemplateDefinitionManager());

        public static TemplateDefinitionManager Instance => _instance.Value;

        private readonly string _definitionsFilePath;
        private List<TemplateDefinition> _definitions;
        private int _nextKey = 1;

        private TemplateDefinitionManager()
        {
            _definitionsFilePath = ComputeDefinitionsFilePath();
            LoadDefinitions();
        }

        private static string ComputeDefinitionsFilePath()
        {
            // Try to find the project source folder first (for persistence)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo dir = new DirectoryInfo(baseDir);

            while (dir != null && dir.Parent != null)
            {
                if (Directory.GetFiles(dir.FullName, "*.csproj").Length > 0)
                {
                    string projectTemplates = Path.Combine(dir.FullName, "Templates");
                    if (!Directory.Exists(projectTemplates))
                        Directory.CreateDirectory(projectTemplates);
                    return Path.Combine(projectTemplates, "TemplateDefinitions.json");
                }
                dir = dir.Parent;
            }

            // Fall back to output directory
            string templatesDir = Path.Combine(baseDir, "Templates");
            if (!Directory.Exists(templatesDir))
                Directory.CreateDirectory(templatesDir);
            return Path.Combine(templatesDir, "TemplateDefinitions.json");
        }

        private void LoadDefinitions()
        {
            if (File.Exists(_definitionsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_definitionsFilePath);
                    _definitions = JsonSerializer.Deserialize<List<TemplateDefinition>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<TemplateDefinition>();

                    // Handle existing files without keys - assign keys to items that don't have them
                    bool needsSave = false;
                    int maxKey = _definitions.Where(d => d.Key > 0).Select(d => d.Key).DefaultIfEmpty(0).Max();
                    _nextKey = maxKey + 1;

                    foreach (var def in _definitions.Where(d => d.Key == 0))
                    {
                        def.Key = _nextKey++;
                        needsSave = true;
                    }

                    if (needsSave)
                        SaveDefinitions();
                }
                catch
                {
                    _definitions = new List<TemplateDefinition>();
                    CreateDefaultDefinitions();
                }
            }
            else
            {
                _definitions = new List<TemplateDefinition>();
                CreateDefaultDefinitions();
            }
        }

        private void CreateDefaultDefinitions()
        {
            // Add default definitions with keys matching the original CoordinateActions
            _definitions = new List<TemplateDefinition>
            {
                // Gardening (keys 1-14)
                new TemplateDefinition { Key = 1, Name = "Plant Flower/Remove Button", Category = "Gardening" },
                new TemplateDefinition { Key = 2, Name = "Red Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Key = 3, Name = "Green Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Key = 4, Name = "Orange Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Key = 5, Name = "Purple Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Key = 6, Name = "Blue Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Key = 7, Name = "Pink Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Key = 8, Name = "Yellow Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Key = 9, Name = "Cyan Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Key = 10, Name = "Silver Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Key = 11, Name = "Blue Plant Button", Category = "Gardening" },
                new TemplateDefinition { Key = 12, Name = "Blue Ok Button", Category = "Gardening" },
                new TemplateDefinition { Key = 13, Name = "Watering Can Button", Category = "Gardening" },
                new TemplateDefinition { Key = 14, Name = "Blue Yes Button", Category = "Gardening" },

                // Fishing (keys 15-17)
                new TemplateDefinition { Key = 15, Name = "Red Fishing Button", Category = "Fishing" },
                new TemplateDefinition { Key = 16, Name = "Exit Fishing Button", Category = "Fishing" },
                new TemplateDefinition { Key = 17, Name = "Blue Sell All Button", Category = "Fishing" },

                // Doodle Training (keys 18-29)
                new TemplateDefinition { Key = 18, Name = "Feed Doodle Button", Category = "Doodle Training" },
                new TemplateDefinition { Key = 19, Name = "Scratch Doodle Button", Category = "Doodle Training" },
                new TemplateDefinition { Key = 20, Name = "Green SpeedChat Button", Category = "Doodle Training" },
                new TemplateDefinition { Key = 21, Name = "Pets Tab in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Key = 22, Name = "Tricks Tab in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Key = 23, Name = "Jump Trick Option in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Key = 24, Name = "Beg Trick Option in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Key = 25, Name = "Play Dead Trick Option in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Key = 26, Name = "Rollover Trick Option in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Key = 27, Name = "Backflip Trick Option in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Key = 28, Name = "Dance Trick Option in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Key = 29, Name = "Speak Trick Option in SpeedChat", Category = "Doodle Training" }
            };

            _nextKey = 30;
            SaveDefinitions();
        }

        public void SaveDefinitions()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(_definitions, options);
                File.WriteAllText(_definitionsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save template definitions: {ex.Message}");
            }
        }

        public List<TemplateDefinition> GetAllDefinitions()
        {
            return _definitions.ToList();
        }

        public List<string> GetAllNames()
        {
            return _definitions.Select(d => d.Name).ToList();
        }

        public List<string> GetCategories()
        {
            return _definitions.Select(d => d.Category).Distinct().OrderBy(c => c).ToList();
        }

        public List<TemplateDefinition> GetDefinitionsByCategory(string category)
        {
            return _definitions.Where(d => d.Category == category).ToList();
        }

        public TemplateDefinition GetDefinition(string name)
        {
            return _definitions.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public bool AddDefinition(string name, string category)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Check if already exists
            if (_definitions.Any(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return false;

            _definitions.Add(new TemplateDefinition
            {
                Key = _nextKey++,
                Name = name,
                Category = category ?? "Custom"
            });

            SaveDefinitions();
            return true;
        }

        public bool RemoveDefinition(string name)
        {
            var definition = _definitions.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (definition != null)
            {
                _definitions.Remove(definition);
                SaveDefinitions();
                return true;
            }
            return false;
        }

        public void ReloadDefinitions()
        {
            LoadDefinitions();
        }

        public string GetDefinitionsFilePath() => _definitionsFilePath;

        // Key-based methods for backward compatibility with CoordinateActions
        public string GetDescriptionByKey(string key)
        {
            if (int.TryParse(key, out int keyInt))
            {
                var def = _definitions.FirstOrDefault(d => d.Key == keyInt);
                return def?.Name;
            }
            return null;
        }

        public string GetKeyByDescription(string description)
        {
            var def = _definitions.FirstOrDefault(d => d.Name.Equals(description, StringComparison.OrdinalIgnoreCase));
            return def?.Key.ToString();
        }

        public Dictionary<string, string> GetAllDescriptions()
        {
            return _definitions.ToDictionary(d => d.Key.ToString(), d => d.Name);
        }

        public TemplateDefinition GetDefinitionByKey(int key)
        {
            return _definitions.FirstOrDefault(d => d.Key == key);
        }
    }

    public class TemplateDefinition
    {
        public int Key { get; set; }
        public string Name { get; set; }
        public string Category { get; set; } = "Custom";
    }
}
