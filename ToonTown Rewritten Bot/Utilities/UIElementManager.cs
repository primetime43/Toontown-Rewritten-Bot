using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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

        private ConcurrentDictionary<string, UIElementData> _elements;
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
        public async Task<Point?> GetElementLocationAsync(string elementName, string description = null, bool forceSearch = false)
        {
            var (location, _) = await GetElementLocationWithSourceAsync(elementName, description, forceSearch);
            return location;
        }

        /// <summary>
        /// Same as GetElementLocationAsync, but also tells the caller where the value
        /// came from (cache hit, fresh template match, manual fallback) so logs and
        /// diagnostics can reflect what actually happened.
        /// </summary>
        public async Task<(Point? location, UIElementSource source)> GetElementLocationWithSourceAsync(string elementName, string description = null, bool forceSearch = false)
        {
            var element = GetOrCreateElement(elementName);

            // If no template exists, prompt user to capture one
            if (!HasTemplate(elementName))
            {
                Logger.Info("TemplateMatch", $"No template for '{elementName}', prompting capture...");

                bool captured = PromptForTemplateCapture(elementName, description ?? $"Please select the '{elementName}' on screen");

                if (!captured)
                {
                    // User cancelled - fall back to manual if available
                    if (element.ManualCoordinates.HasValue)
                    {
                        Logger.Info("TemplateMatch", $"Using manual coordinates for '{elementName}'");
                        return (element.ManualCoordinates, UIElementSource.Manual);
                    }
                    return (null, UIElementSource.None);
                }
            }

            // If we have cached coordinates, trust them without re-verifying.
            // UI elements like buttons don't move during a session, and re-verifying
            // via template matching on every call is unreliable (PrintWindow can return
            // partial frames from 3D-rendered games).
            if (!forceSearch && element.HasCachedCoordinates)
            {
                Logger.Debug("TemplateMatch", $"'{elementName}' using cached location");
                return (element.CachedCenter, UIElementSource.Cache);
            }

            // Now we have a template, try image recognition
            if (HasTemplate(elementName))
            {
                // Search for the element
                var result = await FindElementAsync(elementName);
                if (result.HasValue)
                {
                    // Update cache
                    element.CachedCenter = result.Value;
                    element.LastFoundTime = DateTime.Now;
                    SaveElementData();
                    return (result, UIElementSource.ImageRec);
                }
            }

            // Image rec failed — fall back to manual/cached coordinates silently
            // before interrupting the user with a recapture dialog
            if (element.ManualCoordinates.HasValue)
            {
                Logger.Info("TemplateMatch", $"Image rec failed, using manual coordinates for '{elementName}'");
                return (element.ManualCoordinates, UIElementSource.Manual);
            }

            // No fallback available — offer to recapture or add variant
            if (HasTemplate(elementName))
            {
                Logger.Info("TemplateMatch", $"Template exists for '{elementName}' but could not find it on screen");
                bool recaptured = PromptForVariantOrRecapture(elementName, description ?? $"Please select the '{elementName}' on screen");
                if (recaptured)
                {
                    // Try again with the new/updated template
                    var retryResult = await FindElementAsync(elementName);
                    if (retryResult.HasValue)
                    {
                        element.CachedCenter = retryResult.Value;
                        element.LastFoundTime = DateTime.Now;
                        SaveElementData();
                        return (retryResult, UIElementSource.ImageRec);
                    }
                }
            }

            Logger.Warning("TemplateMatch", $"Could not find '{elementName}'");
            return (null, UIElementSource.None);
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
                    // Bring bot window to front first so the capture dialog is visible
                    ToonTown_Rewritten_Bot.Services.CoreFunctionality.BringBotWindowToFront();
                    result = TemplateCaptureForm.CaptureTemplate(elementName, description);
                }));
                return result;
            }

            // Bring bot window to front first so the capture dialog is visible
            ToonTown_Rewritten_Bot.Services.CoreFunctionality.BringBotWindowToFront();
            return TemplateCaptureForm.CaptureTemplate(elementName, description);
        }

        /// <summary>
        /// Prompts user to replace the existing template or add a new variant when
        /// a template exists but can't be found on screen.
        /// </summary>
        /// <returns>True if a template was captured</returns>
        private bool PromptForVariantOrRecapture(string elementName, string description)
        {
            bool result = false;

            void ShowPrompt()
            {
                ToonTown_Rewritten_Bot.Services.CoreFunctionality.BringBotWindowToFront();

                int variantCount = GetVariantCount(elementName);

                using (var dialog = new Form())
                {
                    dialog.Text = "Element Not Found";
                    dialog.ClientSize = new System.Drawing.Size(400, 150);
                    dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    dialog.StartPosition = FormStartPosition.CenterScreen;
                    dialog.MaximizeBox = false;
                    dialog.MinimizeBox = false;

                    var label = new Label
                    {
                        Text = $"Could not find '{elementName}' on screen.\n\n" +
                               $"Current template has {variantCount} variant{(variantCount != 1 ? "s" : "")}.",
                        Location = new System.Drawing.Point(15, 15),
                        Size = new System.Drawing.Size(370, 60),
                        AutoSize = false
                    };

                    var btnReplace = new Button
                    {
                        Text = "Replace Template",
                        Location = new System.Drawing.Point(15, 100),
                        Size = new System.Drawing.Size(120, 30)
                    };
                    btnReplace.Click += (s, e) => { dialog.Tag = "replace"; dialog.Close(); };

                    var btnVariant = new Button
                    {
                        Text = "Add Variant",
                        Location = new System.Drawing.Point(145, 100),
                        Size = new System.Drawing.Size(120, 30)
                    };
                    btnVariant.Click += (s, e) => { dialog.Tag = "variant"; dialog.Close(); };

                    var btnSkip = new Button
                    {
                        Text = "Skip",
                        Location = new System.Drawing.Point(305, 100),
                        Size = new System.Drawing.Size(80, 30),
                        DialogResult = DialogResult.Cancel
                    };

                    dialog.Controls.AddRange(new Control[] { label, btnReplace, btnVariant, btnSkip });
                    dialog.CancelButton = btnSkip;
                    dialog.ShowDialog();

                    string choice = dialog.Tag as string;
                    if (choice == "replace")
                    {
                        result = TemplateCaptureForm.CaptureTemplate(elementName, description);
                    }
                    else if (choice == "variant")
                    {
                        result = TemplateCaptureForm.CaptureVariant(elementName, description);
                    }
                }
            }

            if (Application.OpenForms.Count > 0 && Application.OpenForms[0].InvokeRequired)
            {
                Application.OpenForms[0].Invoke(new Action(ShowPrompt));
            }
            else
            {
                ShowPrompt();
            }

            return result;
        }

        /// <summary>
        /// Finds an element on screen using its template variants.
        /// Tries each variant in order; returns the first match above threshold.
        /// If none match, returns null (best confidence is logged for diagnostics).
        /// </summary>
        public async Task<Point?> FindElementAsync(string elementName, CancellationToken cancellationToken = default)
        {
            var allPaths = GetAllTemplatePaths(elementName);
            if (allPaths.Count == 0)
                return null;

            try
            {
                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                {
                    double bestConfidence = 0;
                    string bestVariant = null;

                    for (int i = 0; i < allPaths.Count; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        using (var template = new Bitmap(allPaths[i]))
                        {
                            var result = await Task.Run(() =>
                                ImageTemplateMatcher.FindTemplate(screenshot, template, _defaultThreshold, cancellationToken));

                            if (result.Found)
                            {
                                Logger.Debug("TemplateMatch", $"'{elementName}' matched variant {i} ({Path.GetFileName(allPaths[i])}) at {result.Confidence:P1}");
                                return result.Center;
                            }

                            if (result.Confidence > bestConfidence)
                            {
                                bestConfidence = result.Confidence;
                                bestVariant = Path.GetFileName(allPaths[i]);
                            }
                        }
                    }

                    Logger.Debug("TemplateMatch", $"'{elementName}': no variant matched. Best was {bestVariant} at {bestConfidence:P1}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TemplateMatch", $"Error finding '{elementName}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Verifies if an element is at the expected location.
        /// Tries each variant; returns true if any matches.
        /// </summary>
        public async Task<bool> VerifyElementAtLocationAsync(string elementName, Point expectedCenter)
        {
            var allPaths = GetAllTemplatePaths(elementName);
            if (allPaths.Count == 0)
                return false;

            try
            {
                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                {
                    foreach (var templatePath in allPaths)
                    {
                        using (var template = new Bitmap(templatePath))
                        {
                            // Define a search region around the expected location
                            int margin = 50; // pixels of tolerance
                            int searchX = Math.Max(0, expectedCenter.X - template.Width / 2 - margin);
                            int searchY = Math.Max(0, expectedCenter.Y - template.Height / 2 - margin);
                            int searchWidth = Math.Min(template.Width + margin * 2, screenshot.Width - searchX);
                            int searchHeight = Math.Min(template.Height + margin * 2, screenshot.Height - searchY);

                            if (searchWidth <= template.Width || searchHeight <= template.Height)
                                continue;

                            Rectangle searchRegion = new Rectangle(searchX, searchY, searchWidth, searchHeight);

                            using (var regionBitmap = screenshot.Clone(searchRegion, screenshot.PixelFormat))
                            {
                                var result = await Task.Run(() =>
                                    ImageTemplateMatcher.FindTemplate(regionBitmap, template, _defaultThreshold));

                                if (result.Found)
                                    return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TemplateMatch", $"Error verifying '{elementName}': {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Checks if any template variant exists for the given element.
        /// </summary>
        public bool HasTemplate(string elementName)
        {
            return GetAllTemplatePaths(elementName).Count > 0;
        }

        /// <summary>
        /// Gets the primary template file path for an element (backward compat).
        /// </summary>
        public string GetTemplatePath(string elementName)
        {
            string safeName = MakeSafeFileName(elementName);
            return Path.Combine(_templatesFolder, $"{safeName}.png");
        }

        /// <summary>
        /// Gets the file path for a specific variant (0-based: 0=primary, 1=_v2, etc.)
        /// </summary>
        public string GetVariantPath(string elementName, int variantIndex)
        {
            string safeName = MakeSafeFileName(elementName);
            if (variantIndex == 0)
                return Path.Combine(_templatesFolder, $"{safeName}.png");
            return Path.Combine(_templatesFolder, $"{safeName}_v{variantIndex + 1}.png");
        }

        /// <summary>
        /// Gets absolute paths of all existing variant files for the element.
        /// </summary>
        public List<string> GetAllTemplatePaths(string elementName)
        {
            var paths = new List<string>();
            string primaryPath = GetTemplatePath(elementName);
            if (File.Exists(primaryPath))
                paths.Add(primaryPath);

            // Check for _v2, _v3, ... up to a reasonable limit
            string safeName = MakeSafeFileName(elementName);
            for (int i = 2; i <= 20; i++)
            {
                string variantPath = Path.Combine(_templatesFolder, $"{safeName}_v{i}.png");
                if (File.Exists(variantPath))
                    paths.Add(variantPath);
                else
                    break; // Stop at first gap
            }

            return paths;
        }

        /// <summary>
        /// Gets the number of variant files that exist on disk for the element.
        /// </summary>
        public int GetVariantCount(string elementName)
        {
            return GetAllTemplatePaths(elementName).Count;
        }

        /// <summary>
        /// Saves a template image for an element (overwrites the primary).
        /// </summary>
        public void SaveTemplate(string elementName, Bitmap templateImage)
        {
            string templatePath = GetTemplatePath(elementName);
            templateImage.Save(templatePath, System.Drawing.Imaging.ImageFormat.Png);

            // Store a relative path for portability (Templates/Name.png)
            string safeName = MakeSafeFileName(elementName);
            var element = GetOrCreateElement(elementName);
            element.TemplatePath = Path.Combine("Templates", $"{safeName}.png");

            // A newly captured template may match a different on-screen location
            // than the existing cache — drop the cache so the next lookup re-searches.
            element.CachedCenter = null;
            element.LastFoundTime = null;

            // Rebuild VariantPaths from disk
            RebuildVariantPaths(elementName);
            SaveElementData();

            Logger.Info("TemplateMatch", $"Saved template for '{elementName}' (cache cleared)");
        }

        /// <summary>
        /// Saves a template image as the next available variant.
        /// Returns the variant index that was saved (0-based).
        /// </summary>
        public int SaveTemplateVariant(string elementName, Bitmap templateImage)
        {
            // Find the next available variant number
            int variantIndex = 0;
            while (File.Exists(GetVariantPath(elementName, variantIndex)))
            {
                variantIndex++;
            }

            string variantPath = GetVariantPath(elementName, variantIndex);
            templateImage.Save(variantPath, System.Drawing.Imaging.ImageFormat.Png);

            // Drop the cache so the next lookup actually tries the new variant —
            // otherwise the cached center short-circuits the template search.
            var element = GetOrCreateElement(elementName);
            element.CachedCenter = null;
            element.LastFoundTime = null;

            // Rebuild VariantPaths from disk
            RebuildVariantPaths(elementName);
            SaveElementData();

            Logger.Info("TemplateMatch", $"Saved variant {variantIndex} for '{elementName}' (cache cleared)");
            return variantIndex;
        }

        /// <summary>
        /// Deletes a specific variant file and renumbers remaining variants.
        /// </summary>
        public void DeleteTemplateVariant(string elementName, int variantIndex)
        {
            string variantPath = GetVariantPath(elementName, variantIndex);
            if (File.Exists(variantPath))
            {
                File.Delete(variantPath);
                Logger.Debug("TemplateMatch", $"Deleted variant {variantIndex} for '{elementName}'");
            }

            // Renumber remaining variants to fill the gap
            RenumberVariants(elementName);
            RebuildVariantPaths(elementName);
            SaveElementData();
        }

        /// <summary>
        /// Sets manual fallback coordinates for an element.
        /// </summary>
        public void SetManualCoordinates(string elementName, Point coordinates)
        {
            var element = GetOrCreateElement(elementName);

            // CoordinatesManager calls this every lookup with the same value, so only
            // invalidate the cache when the user actually changed something — otherwise
            // we'd defeat caching entirely.
            if (element.ManualCoordinates != coordinates)
            {
                element.ManualCoordinates = coordinates;
                element.CachedCenter = null;
                element.LastFoundTime = null;
                Logger.Info("TemplateMatch", $"Manual coordinates updated for '{elementName}' (cache cleared)");
                SaveElementData();
            }
        }

        /// <summary>
        /// Clears a manual fallback for an element. Also clears any location cached while that
        /// fallback was active so the next lookup performs a real template search.
        /// </summary>
        public void ClearManualCoordinates(string elementName)
        {
            var element = GetOrCreateElement(elementName);
            if (element.ManualCoordinates.HasValue)
            {
                element.ManualCoordinates = null;
                element.CachedCenter = null;
                element.LastFoundTime = null;
                Logger.Info("TemplateMatch", $"Manual coordinates cleared for '{elementName}' (cache cleared)");
                SaveElementData();
            }
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
        /// Clears all manual fallbacks and cached locations. Used by Reset State so both the
        /// legacy coordinate file and image-recognition state are actually reset together.
        /// </summary>
        public void ClearAllPositionData()
        {
            foreach (var element in _elements.Values)
            {
                element.ManualCoordinates = null;
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
            return _elements.GetOrAdd(elementName, key => new UIElementData { Name = key });
        }

        private void LoadElementData()
        {
            _elements = new ConcurrentDictionary<string, UIElementData>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(_dataFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_dataFilePath);
                    var loaded = JsonConvert.DeserializeObject<Dictionary<string, UIElementData>>(json);
                    if (loaded != null)
                    {
                        _elements = new ConcurrentDictionary<string, UIElementData>(loaded, StringComparer.OrdinalIgnoreCase);

                        // Migration: if VariantPaths is null/empty but TemplatePath was set via old format,
                        // the TemplatePath setter already handles populating VariantPaths[0].
                        // No additional migration step needed.
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("TemplateMatch", $"Error loading element data: {ex.Message}");
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
                Logger.Error("TemplateMatch", $"Error saving element data: {ex.Message}");
            }
        }

        private string GetTemplatesFolder()
        {
            string baseDir = AppPaths.ExeDirectory;

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

        /// <summary>
        /// Rebuilds the VariantPaths list in UIElementData from files on disk.
        /// </summary>
        private void RebuildVariantPaths(string elementName)
        {
            var element = GetOrCreateElement(elementName);
            var allPaths = GetAllTemplatePaths(elementName);
            string safeName = MakeSafeFileName(elementName);

            element.VariantPaths = allPaths
                .Select(p => Path.Combine("Templates", Path.GetFileName(p)))
                .ToList();
        }

        /// <summary>
        /// Renumbers variant files to fill any gaps after a deletion.
        /// E.g., if _v2 is deleted and _v3 exists, _v3 becomes _v2.
        /// </summary>
        private void RenumberVariants(string elementName)
        {
            string safeName = MakeSafeFileName(elementName);

            // Collect all existing variant files
            var existingFiles = new List<string>();
            string primaryPath = Path.Combine(_templatesFolder, $"{safeName}.png");
            if (File.Exists(primaryPath))
                existingFiles.Add(primaryPath);

            for (int i = 2; i <= 20; i++)
            {
                string path = Path.Combine(_templatesFolder, $"{safeName}_v{i}.png");
                if (File.Exists(path))
                    existingFiles.Add(path);
            }

            // Rename them in order: primary, _v2, _v3, ...
            for (int i = 0; i < existingFiles.Count; i++)
            {
                string targetPath = GetVariantPath(elementName, i);
                if (existingFiles[i] != targetPath)
                {
                    try
                    {
                        File.Move(existingFiles[i], targetPath);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("TemplateMatch", $"Error renumbering variant: {ex.Message}");
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Data stored for each UI element.
    /// </summary>
    public class UIElementData
    {
        public string Name { get; set; }

        /// <summary>
        /// List of relative paths for all template variants.
        /// Index 0 is the primary template, index 1 is _v2, etc.
        /// </summary>
        public List<string> VariantPaths { get; set; }

        /// <summary>
        /// Primary template path (backward compat). Returns first item from VariantPaths.
        /// On set, initializes VariantPaths if needed and sets the first element.
        /// </summary>
        public string TemplatePath
        {
            get => VariantPaths != null && VariantPaths.Count > 0 ? VariantPaths[0] : null;
            set
            {
                if (VariantPaths == null)
                    VariantPaths = new List<string>();
                if (VariantPaths.Count == 0)
                    VariantPaths.Add(value);
                else
                    VariantPaths[0] = value;
            }
        }

        public Point? ManualCoordinates { get; set; }
        public Point? CachedCenter { get; set; }
        public DateTime? LastFoundTime { get; set; }

        [JsonIgnore]
        public bool HasCachedCoordinates => CachedCenter.HasValue;

        [JsonIgnore]
        public bool HasManualFallback => ManualCoordinates.HasValue;

        [JsonIgnore]
        public int VariantCount => VariantPaths?.Count ?? 0;
    }

    /// <summary>
    /// Event args for template capture requests.
    /// </summary>
    public class TemplateCaptureEventArgs : EventArgs
    {
        public string ElementName { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Where a resolved UI element location actually came from. Surfaced in logs so
    /// "image rec returned 958, 774" doesn't get blamed when the cache was the source.
    /// </summary>
    public enum UIElementSource
    {
        None,
        Cache,
        ImageRec,
        Manual,
    }
}
