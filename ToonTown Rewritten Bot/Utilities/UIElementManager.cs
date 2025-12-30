using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using ToonTown_Rewritten_Bot.Views;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Manages UI element detection using image recognition as primary method
    /// with cached coordinates and manual fallback.
    /// </summary>
    public class UIElementManager
    {
        private static UIElementManager _instance;
        private static readonly object _lock = new object();

        private Dictionary<string, UIElementData> _elements;
        private readonly string _dataFilePath;
        private readonly string _templatesFolder;
        private double _defaultThreshold = 0.85;

        /// <summary>
        /// Event raised when a template needs to be captured for an element.
        /// </summary>
        public event EventHandler<TemplateCaptureEventArgs> TemplateCaptureRequired;

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static UIElementManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new UIElementManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private UIElementManager()
        {
            _templatesFolder = GetTemplatesFolder();
            _dataFilePath = Path.Combine(_templatesFolder, "UIElementCoordinates.json");
            LoadElementData();
        }

        /// <summary>
        /// Gets the location of a UI element, using image recognition to find or verify.
        /// Will prompt user to capture template if none exists.
        /// </summary>
        /// <param name="elementName">Unique name for the UI element</param>
        /// <param name="description">Description to show user when capturing template</param>
        /// <param name="forceSearch">If true, always search instead of using cache</param>
        /// <returns>The center point of the element, or null if not found</returns>
        public async Task<Point?> GetElementLocationAsync(string elementName, string description = null, bool forceSearch = false)
        {
            var element = GetOrCreateElement(elementName);

            // If no template exists, prompt user to capture one
            if (!HasTemplate(elementName))
            {
                System.Diagnostics.Debug.WriteLine($"[UIElementManager] No template for {elementName}, prompting capture...");

                bool captured = PromptForTemplateCapture(elementName, description ?? $"Please select the '{elementName}' on screen");

                if (!captured)
                {
                    // User cancelled - fall back to manual if available
                    if (element.ManualCoordinates.HasValue)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UIElementManager] Using manual coordinates for {elementName}");
                        return element.ManualCoordinates;
                    }
                    return null;
                }
            }

            // Now we have a template, try image recognition
            if (HasTemplate(elementName))
            {
                // If we have cached coordinates and not forcing search, verify first
                if (!forceSearch && element.HasCachedCoordinates)
                {
                    // Quick verify at cached location
                    bool stillThere = await VerifyElementAtLocationAsync(elementName, element.CachedCenter.Value);
                    if (stillThere)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UIElementManager] {elementName} found at cached location");
                        return element.CachedCenter;
                    }
                    System.Diagnostics.Debug.WriteLine($"[UIElementManager] {elementName} not at cached location, searching...");
                }

                // Search for the element
                var result = await FindElementAsync(elementName);
                if (result.HasValue)
                {
                    // Update cache
                    element.CachedCenter = result.Value;
                    element.LastFoundTime = DateTime.Now;
                    SaveElementData();
                    return result;
                }
            }

            // Image rec failed - check if we have manual coordinates as fallback
            if (element.ManualCoordinates.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"[UIElementManager] Image rec failed, using manual coordinates for {elementName}");
                return element.ManualCoordinates;
            }

            System.Diagnostics.Debug.WriteLine($"[UIElementManager] Could not find {elementName}");
            return null;
        }

        /// <summary>
        /// Prompts the user to capture a template for the specified element.
        /// </summary>
        /// <returns>True if template was captured successfully</returns>
        public bool PromptForTemplateCapture(string elementName, string description = null)
        {
            // Must run on UI thread
            if (Application.OpenForms.Count > 0 && Application.OpenForms[0].InvokeRequired)
            {
                bool result = false;
                Application.OpenForms[0].Invoke(new Action(() =>
                {
                    result = TemplateCaptureForm.CaptureTemplate(elementName, description);
                }));
                return result;
            }

            return TemplateCaptureForm.CaptureTemplate(elementName, description);
        }

        /// <summary>
        /// Finds an element on screen using its template.
        /// </summary>
        public async Task<Point?> FindElementAsync(string elementName, CancellationToken cancellationToken = default)
        {
            string templatePath = GetTemplatePath(elementName);
            if (!File.Exists(templatePath))
                return null;

            try
            {
                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                using (var template = new Bitmap(templatePath))
                {
                    var result = await Task.Run(() =>
                        ImageTemplateMatcher.FindTemplate(screenshot, template, _defaultThreshold, cancellationToken));

                    if (result.Found)
                    {
                        return result.Center;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UIElementManager] Error finding {elementName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Verifies if an element is at the expected location.
        /// </summary>
        public async Task<bool> VerifyElementAtLocationAsync(string elementName, Point expectedCenter)
        {
            string templatePath = GetTemplatePath(elementName);
            if (!File.Exists(templatePath))
                return false;

            try
            {
                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                using (var template = new Bitmap(templatePath))
                {
                    // Define a search region around the expected location
                    int margin = 50; // pixels of tolerance
                    int searchX = Math.Max(0, expectedCenter.X - template.Width / 2 - margin);
                    int searchY = Math.Max(0, expectedCenter.Y - template.Height / 2 - margin);
                    int searchWidth = Math.Min(template.Width + margin * 2, screenshot.Width - searchX);
                    int searchHeight = Math.Min(template.Height + margin * 2, screenshot.Height - searchY);

                    if (searchWidth <= template.Width || searchHeight <= template.Height)
                        return false;

                    Rectangle searchRegion = new Rectangle(searchX, searchY, searchWidth, searchHeight);

                    using (var regionBitmap = screenshot.Clone(searchRegion, screenshot.PixelFormat))
                    {
                        var result = await Task.Run(() =>
                            ImageTemplateMatcher.FindTemplate(regionBitmap, template, _defaultThreshold));

                        return result.Found;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UIElementManager] Error verifying {elementName}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Checks if a template exists for the given element.
        /// </summary>
        public bool HasTemplate(string elementName)
        {
            return File.Exists(GetTemplatePath(elementName));
        }

        /// <summary>
        /// Gets the template file path for an element.
        /// </summary>
        public string GetTemplatePath(string elementName)
        {
            string safeName = MakeSafeFileName(elementName);
            return Path.Combine(_templatesFolder, $"{safeName}.png");
        }

        /// <summary>
        /// Saves a template image for an element.
        /// </summary>
        public void SaveTemplate(string elementName, Bitmap templateImage)
        {
            string templatePath = GetTemplatePath(elementName);
            templateImage.Save(templatePath, System.Drawing.Imaging.ImageFormat.Png);

            var element = GetOrCreateElement(elementName);
            element.TemplatePath = templatePath;
            SaveElementData();

            System.Diagnostics.Debug.WriteLine($"[UIElementManager] Saved template for {elementName}");
        }

        /// <summary>
        /// Sets manual fallback coordinates for an element.
        /// </summary>
        public void SetManualCoordinates(string elementName, Point coordinates)
        {
            var element = GetOrCreateElement(elementName);
            element.ManualCoordinates = coordinates;
            SaveElementData();
        }

        /// <summary>
        /// Gets manual fallback coordinates for an element.
        /// </summary>
        public Point? GetManualCoordinates(string elementName)
        {
            if (_elements.TryGetValue(elementName, out var element))
            {
                return element.ManualCoordinates;
            }
            return null;
        }

        /// <summary>
        /// Clears cached coordinates for an element (forces re-search on next use).
        /// </summary>
        public void ClearCache(string elementName)
        {
            if (_elements.TryGetValue(elementName, out var element))
            {
                element.CachedCenter = null;
                element.LastFoundTime = null;
                SaveElementData();
            }
        }

        /// <summary>
        /// Clears all cached coordinates.
        /// </summary>
        public void ClearAllCache()
        {
            foreach (var element in _elements.Values)
            {
                element.CachedCenter = null;
                element.LastFoundTime = null;
            }
            SaveElementData();
        }

        /// <summary>
        /// Gets all registered element names.
        /// </summary>
        public IEnumerable<string> GetAllElementNames()
        {
            return _elements.Keys;
        }

        /// <summary>
        /// Gets element data for debugging/display.
        /// </summary>
        public UIElementData GetElementData(string elementName)
        {
            _elements.TryGetValue(elementName, out var element);
            return element;
        }

        /// <summary>
        /// Requests template capture from the user.
        /// </summary>
        public void RequestTemplateCapture(string elementName, string description = null)
        {
            TemplateCaptureRequired?.Invoke(this, new TemplateCaptureEventArgs
            {
                ElementName = elementName,
                Description = description ?? $"Please capture the template for: {elementName}"
            });
        }

        #region Private Methods

        private UIElementData GetOrCreateElement(string elementName)
        {
            if (!_elements.TryGetValue(elementName, out var element))
            {
                element = new UIElementData { Name = elementName };
                _elements[elementName] = element;
            }
            return element;
        }

        private void LoadElementData()
        {
            _elements = new Dictionary<string, UIElementData>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(_dataFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_dataFilePath);
                    var loaded = JsonConvert.DeserializeObject<Dictionary<string, UIElementData>>(json);
                    if (loaded != null)
                    {
                        _elements = new Dictionary<string, UIElementData>(loaded, StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UIElementManager] Error loading data: {ex.Message}");
                }
            }
        }

        private void SaveElementData()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_elements, Formatting.Indented);
                File.WriteAllText(_dataFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UIElementManager] Error saving data: {ex.Message}");
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

        private string MakeSafeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in invalid)
            {
                name = name.Replace(c, '_');
            }
            return name.Replace(' ', '_');
        }

        #endregion
    }

    /// <summary>
    /// Data stored for each UI element.
    /// </summary>
    public class UIElementData
    {
        public string Name { get; set; }
        public string TemplatePath { get; set; }
        public Point? ManualCoordinates { get; set; }
        public Point? CachedCenter { get; set; }
        public DateTime? LastFoundTime { get; set; }

        [JsonIgnore]
        public bool HasCachedCoordinates => CachedCenter.HasValue;

        [JsonIgnore]
        public bool HasManualFallback => ManualCoordinates.HasValue;
    }

    /// <summary>
    /// Event args for template capture requests.
    /// </summary>
    public class TemplateCaptureEventArgs : EventArgs
    {
        public string ElementName { get; set; }
        public string Description { get; set; }
    }
}
