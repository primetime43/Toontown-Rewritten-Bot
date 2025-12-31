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
            // Add default definitions from the original CoordinateActions
            _definitions = new List<TemplateDefinition>
            {
                // Gardening
                new TemplateDefinition { Name = "Plant Flower/Remove Button", Category = "Gardening" },
                new TemplateDefinition { Name = "Red Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Name = "Green Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Name = "Orange Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Name = "Purple Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Name = "Blue Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Name = "Pink Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Name = "Yellow Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Name = "Cyan Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Name = "Silver Jellybean Button", Category = "Gardening" },
                new TemplateDefinition { Name = "Blue Plant Button", Category = "Gardening" },
                new TemplateDefinition { Name = "Blue Ok Button", Category = "Gardening" },
                new TemplateDefinition { Name = "Watering Can Button", Category = "Gardening" },
                new TemplateDefinition { Name = "Blue Yes Button", Category = "Gardening" },

                // Fishing
                new TemplateDefinition { Name = "Red Fishing Button", Category = "Fishing" },
                new TemplateDefinition { Name = "Exit Fishing Button", Category = "Fishing" },
                new TemplateDefinition { Name = "Blue Sell All Button", Category = "Fishing" },

                // Doodle Training
                new TemplateDefinition { Name = "Feed Doodle Button", Category = "Doodle Training" },
                new TemplateDefinition { Name = "Scratch Doodle Button", Category = "Doodle Training" },
                new TemplateDefinition { Name = "Green SpeedChat Button", Category = "Doodle Training" },
                new TemplateDefinition { Name = "Pets Tab in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Name = "Tricks Tab in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Name = "Jump Trick Option in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Name = "Beg Trick Option in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Name = "Play Dead Trick Option in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Name = "Rollover Trick Option in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Name = "Backflip Trick Option in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Name = "Dance Trick Option in SpeedChat", Category = "Doodle Training" },
                new TemplateDefinition { Name = "Speak Trick Option in SpeedChat", Category = "Doodle Training" }
            };

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
    }

    public class TemplateDefinition
    {
        public string Name { get; set; }
        public string Category { get; set; } = "Custom";
    }
}
