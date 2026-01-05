using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    public partial class ImageRecognitionDebugForm : Form
    {
        private Bitmap _currentScreenshot;
        private Bitmap _currentTemplate;
        private TextRecognition _ocr;
        private System.Windows.Forms.Timer _refreshTimer;
        private Rectangle _selectedRegion;
        private bool _isSelectingRegion;
        private Point _selectionStart;
        private List<ImageTemplateMatcher.MatchResult> _matchResults = new List<ImageTemplateMatcher.MatchResult>();
        private static readonly string TemplatesFolder = GetTemplatesFolder();
        private CancellationTokenSource _searchCancellation;

        // Fish detection debugging
        private List<Point> _fishBubbleResults = new List<Point>();
        private Rectangle _fishScanArea = Rectangle.Empty;
        private Color _fishBubbleColor = Color.Empty;
        private Point _calculatedRodPosition = Point.Empty;
        private Point _calculatedCastPosition = Point.Empty;
        private FishBubbleDetector _currentFishDetector;
        private Bitmap _lastFishScreenshot;

        /// <summary>
        /// Gets the Templates folder path. Tries to find the project source folder first (for persistence),
        /// falls back to the output directory.
        /// </summary>
        private static string GetTemplatesFolder()
        {
            // First, try to find the project source folder (for development)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Navigate up from bin/Debug/net10.0-windows to find the project folder
            DirectoryInfo dir = new DirectoryInfo(baseDir);
            while (dir != null && dir.Parent != null)
            {
                // Look for .csproj file to identify project root
                if (Directory.GetFiles(dir.FullName, "*.csproj").Length > 0)
                {
                    string projectTemplates = Path.Combine(dir.FullName, "Templates");
                    if (Directory.Exists(projectTemplates) || TryCreateDirectory(projectTemplates))
                    {
                        return projectTemplates;
                    }
                }
                dir = dir.Parent;
            }

            // Fall back to output directory
            return Path.Combine(baseDir, "Templates");
        }

        private static bool TryCreateDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public ImageRecognitionDebugForm()
        {
            InitializeComponent();
            InitializeOCR();
            SetupRefreshTimer();
            EnsureTemplatesFolderExists();
            LoadTemplateDefinitions();
        }

        private void LoadTemplateDefinitions()
        {
            templateDefinitionsComboBox.Items.Clear();
            var definitions = TemplateDefinitionManager.Instance.GetAllDefinitions();
            foreach (var def in definitions.OrderBy(d => d.Category).ThenBy(d => d.Name))
            {
                templateDefinitionsComboBox.Items.Add($"{def.Name}");
            }
            if (templateDefinitionsComboBox.Items.Count > 0)
                templateDefinitionsComboBox.SelectedIndex = 0;
        }

        private void TemplateDefinitionsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (templateDefinitionsComboBox.SelectedItem == null) return;

            string templateName = templateDefinitionsComboBox.SelectedItem.ToString();
            string templateFileName = GetTemplateFileName(templateName);
            string templatePath = Path.Combine(TemplatesFolder, templateFileName);

            if (File.Exists(templatePath))
            {
                try
                {
                    _currentTemplate?.Dispose();
                    _currentTemplate = new Bitmap(templatePath);
                    templatePreviewPictureBox.Image = _currentTemplate;
                    templatePathLabel.Text = templateFileName;
                    templatePathLabel.ForeColor = Color.LightGreen;
                    Log($"Loaded template: {templateName} ({_currentTemplate.Width}x{_currentTemplate.Height})");
                }
                catch (Exception ex)
                {
                    Log($"Failed to load template: {ex.Message}");
                    templatePathLabel.ForeColor = Color.Red;
                }
            }
            else
            {
                templatePreviewPictureBox.Image = null;
                templatePathLabel.Text = "Not captured";
                templatePathLabel.ForeColor = Color.Orange;
            }
        }

        private async void TestTemplateBtn_Click(object sender, EventArgs e)
        {
            if (_currentScreenshot == null)
            {
                // Capture a screenshot first
                CaptureScreenshot();
                if (_currentScreenshot == null)
                {
                    Log("Please capture a screenshot first.");
                    return;
                }
            }

            if (templateDefinitionsComboBox.SelectedItem == null)
            {
                Log("Please select a template from the dropdown.");
                return;
            }

            string templateName = templateDefinitionsComboBox.SelectedItem.ToString();
            string templateFileName = GetTemplateFileName(templateName);
            string templatePath = Path.Combine(TemplatesFolder, templateFileName);

            if (!File.Exists(templatePath))
            {
                Log($"Template image not found: {templateFileName}");
                Log($"Please capture this template first using the Dev tab.");
                return;
            }

            // Load the template if not already loaded
            if (_currentTemplate == null || templatePathLabel.Text != templateFileName)
            {
                try
                {
                    _currentTemplate?.Dispose();
                    _currentTemplate = new Bitmap(templatePath);
                    templatePreviewPictureBox.Image = _currentTemplate;
                    templatePathLabel.Text = templateFileName;
                }
                catch (Exception ex)
                {
                    Log($"Failed to load template: {ex.Message}");
                    return;
                }
            }

            Log($"Testing template: {templateName}...");

            // Use the existing find template logic
            FindTemplateBtn_Click(sender, e);
        }

        private string GetTemplateFileName(string templateName)
        {
            // Convert template name to filename format (same as UIElementManager)
            return templateName.Replace(" ", "_").Replace("/", "_") + ".png";
        }

        private void EnsureTemplatesFolderExists()
        {
            if (!Directory.Exists(TemplatesFolder))
            {
                Directory.CreateDirectory(TemplatesFolder);
                Log($"Created templates folder: {TemplatesFolder}");
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Image Recognition Debug";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(800, 500);

            // === MAIN LAYOUT: Left (Preview + Output) | Right (Controls) ===
            var mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 6
            };
            this.Controls.Add(mainSplit);

            this.Load += (s, e) =>
            {
                try { mainSplit.SplitterDistance = Math.Max(400, this.ClientSize.Width - 280); }
                catch { }
            };

            // === LEFT PANEL: Preview + Output ===
            var leftPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 75F));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            mainSplit.Panel1.Controls.Add(leftPanel);

            // Preview group
            var previewGroup = new GroupBox
            {
                Text = "Preview (Click & drag to select region)",
                Dock = DockStyle.Fill,
                Margin = new Padding(3)
            };
            leftPanel.Controls.Add(previewGroup, 0, 0);

            previewPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            previewPictureBox.MouseDown += PreviewPictureBox_MouseDown;
            previewPictureBox.MouseMove += PreviewPictureBox_MouseMove;
            previewPictureBox.MouseUp += PreviewPictureBox_MouseUp;
            previewPictureBox.Paint += PreviewPictureBox_Paint;
            previewGroup.Controls.Add(previewPictureBox);

            // Output group
            var outputGroup = new GroupBox
            {
                Text = "Output Log",
                Dock = DockStyle.Fill,
                Margin = new Padding(3)
            };
            leftPanel.Controls.Add(outputGroup, 0, 1);

            outputTextBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };
            outputGroup.Controls.Add(outputTextBox);

            // === RIGHT PANEL: All Controls ===
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(5)
            };
            mainSplit.Panel2.Controls.Add(rightPanel);

            var controlsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0)
            };
            rightPanel.Controls.Add(controlsFlow);

            // === CAPTURE GROUP ===
            var captureGroup = CreateGroupBox("Capture", 250);
            controlsFlow.Controls.Add(captureGroup);

            var captureInner = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, Padding = new Padding(8), WrapContents = false };
            captureGroup.Controls.Add(captureInner);

            var captureRow1 = CreateFlowRow();
            captureInner.Controls.Add(captureRow1);
            captureBtn = CreateButton("Capture", 80);
            captureBtn.Click += CaptureBtn_Click;
            captureRow1.Controls.Add(captureBtn);
            clearMatchesBtn = CreateButton("Clear", 60);
            clearMatchesBtn.Click += (s, e) => { _matchResults.Clear(); _selectedRegion = Rectangle.Empty; _fishBubbleResults.Clear(); _fishScanArea = Rectangle.Empty; _shadowBlobs.Clear(); previewPictureBox.Invalidate(); Log("Cleared."); };
            captureRow1.Controls.Add(clearMatchesBtn);

            var captureRow2 = CreateFlowRow();
            captureInner.Controls.Add(captureRow2);
            autoRefreshCheckBox = new CheckBox { Text = "Auto", AutoSize = true, Margin = new Padding(0, 4, 5, 0) };
            autoRefreshCheckBox.CheckedChanged += AutoRefreshCheckBox_CheckedChanged;
            captureRow2.Controls.Add(autoRefreshCheckBox);
            refreshIntervalNumeric = new NumericUpDown { Size = new Size(55, 24), Minimum = 100, Maximum = 5000, Value = 500, Increment = 100 };
            captureRow2.Controls.Add(refreshIntervalNumeric);
            captureRow2.Controls.Add(new Label { Text = "ms", AutoSize = true, Margin = new Padding(2, 5, 0, 0) });
            alwaysOnTopCheckBox = new CheckBox { Text = "On top", AutoSize = true, Margin = new Padding(10, 4, 0, 0) };
            alwaysOnTopCheckBox.CheckedChanged += AlwaysOnTopCheckBox_CheckedChanged;
            captureRow2.Controls.Add(alwaysOnTopCheckBox);

            // === TEMPLATE MATCHING GROUP ===
            var templateGroup = CreateGroupBox("Template Matching", 250);
            controlsFlow.Controls.Add(templateGroup);

            var templateInner = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, Padding = new Padding(8), WrapContents = false };
            templateGroup.Controls.Add(templateInner);

            templateDefinitionsComboBox = new ComboBox { Size = new Size(230, 24), DropDownStyle = ComboBoxStyle.DropDownList, DropDownHeight = 300, Margin = new Padding(0, 0, 0, 5) };
            templateDefinitionsComboBox.SelectedIndexChanged += TemplateDefinitionsComboBox_SelectedIndexChanged;
            templateInner.Controls.Add(templateDefinitionsComboBox);

            templatePreviewPictureBox = new PictureBox
            {
                Size = new Size(230, 50),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 5)
            };
            templateInner.Controls.Add(templatePreviewPictureBox);

            var templateRow1 = CreateFlowRow();
            templateInner.Controls.Add(templateRow1);
            findTemplateBtn = CreateButton("Find", 55);
            findTemplateBtn.Click += FindTemplateBtn_Click;
            templateRow1.Controls.Add(findTemplateBtn);
            findAllTemplatesBtn = CreateButton("Find All", 65);
            findAllTemplatesBtn.Click += FindAllTemplatesBtn_Click;
            templateRow1.Controls.Add(findAllTemplatesBtn);
            templatePathLabel = new Label { Text = "", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(5, 5, 0, 0), MaximumSize = new Size(100, 0) };
            templateRow1.Controls.Add(templatePathLabel);

            // === OCR GROUP ===
            var ocrGroup = CreateGroupBox("OCR (select region first)", 250);
            controlsFlow.Controls.Add(ocrGroup);

            var ocrInner = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, Padding = new Padding(8), WrapContents = false };
            ocrGroup.Controls.Add(ocrInner);

            var ocrRow1 = CreateFlowRow();
            ocrInner.Controls.Add(ocrRow1);
            readTextBtn = CreateButton("Read Text", 80);
            readTextBtn.Click += ReadTextBtn_Click;
            ocrRow1.Controls.Add(readTextBtn);
            readNumbersBtn = CreateButton("Numbers", 70);
            readNumbersBtn.Click += ReadNumbersBtn_Click;
            ocrRow1.Controls.Add(readNumbersBtn);
            readFullScreenBtn = CreateButton("Full", 50);
            readFullScreenBtn.Click += ReadFullScreenBtn_Click;
            ocrRow1.Controls.Add(readFullScreenBtn);

            // === FISH DETECTION GROUP ===
            var fishGroup = CreateGroupBox("Fish Detection", 250);
            controlsFlow.Controls.Add(fishGroup);

            var fishInner = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, Padding = new Padding(8), WrapContents = false };
            fishGroup.Controls.Add(fishInner);

            fishLocationComboBox = new ComboBox { Size = new Size(230, 24), DropDownStyle = ComboBoxStyle.DropDownList, DropDownHeight = 250, Margin = new Padding(0, 0, 0, 5) };
            fishLocationComboBox.Items.AddRange(new object[] { "TTC Punchline Place", "DDL Lullaby Lane", "Brrrgh Polar Place", "Brrrgh Walrus Way", "Brrrgh Sleet Street", "MML Tenor Terrace", "DD Lighthouse Lane", "DG Elm Street", "Fish Anywhere" });
            fishLocationComboBox.SelectedIndex = 0;
            fishInner.Controls.Add(fishLocationComboBox);

            var fishRow1 = CreateFlowRow();
            fishInner.Controls.Add(fishRow1);
            findFishBtn = CreateButton("Find Fish", 80);
            findFishBtn.Click += FindFishBtn_Click;
            fishRow1.Controls.Add(findFishBtn);
            resetCalibrationBtn = CreateButton("Reset Calibration", 110);
            resetCalibrationBtn.Click += ResetCalibrationBtn_Click;
            fishRow1.Controls.Add(resetCalibrationBtn);

            var fishRow2 = CreateFlowRow();
            fishInner.Controls.Add(fishRow2);
            confirmFishBtn = CreateButton("Confirm Fish", 90);
            confirmFishBtn.BackColor = Color.LightGreen;
            confirmFishBtn.Enabled = false;
            confirmFishBtn.Click += ConfirmFishBtn_Click;
            fishRow2.Controls.Add(confirmFishBtn);
            rejectFishBtn = CreateButton("Not Fish", 70);
            rejectFishBtn.BackColor = Color.LightCoral;
            rejectFishBtn.Enabled = false;
            rejectFishBtn.Click += RejectFishBtn_Click;
            fishRow2.Controls.Add(rejectFishBtn);

            // === STATUS BAR ===
            coordsLabel = new Label
            {
                Text = "Ready | Click preview to select region",
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                BorderStyle = BorderStyle.Fixed3D,
                Padding = new Padding(5, 0, 0, 0)
            };
            this.Controls.Add(coordsLabel);

            // Initialize dummy controls that are no longer in UI but referenced elsewhere
            thresholdNumeric = new NumericUpDown { Value = 85, Minimum = 50, Maximum = 100 };
            stopSearchBtn = new Button();
            testTemplateBtn = new Button();
            loadTemplateBtn = new Button();
            useSelectionBtn = new Button();
            saveTemplateBtn = new Button();
            openTemplatesFolderBtn = new Button();
            sampleColorBtn = new Button();
        }

        private GroupBox CreateGroupBox(string title, int width)
        {
            return new GroupBox
            {
                Text = title,
                Size = new Size(width, 0),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(3)
            };
        }

        private FlowLayoutPanel CreateFlowRow()
        {
            return new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 3)
            };
        }

        private Button CreateButton(string text, int width)
        {
            return new Button
            {
                Text = text,
                Size = new Size(width, 25),
                Margin = new Padding(0, 0, 5, 0)
            };
        }

        private void UseSelectionBtn_Click(object sender, EventArgs e)
        {
            if (_currentScreenshot == null)
            {
                Log("Please capture a screenshot first.");
                return;
            }

            if (_selectedRegion.IsEmpty || _selectedRegion.Width <= 5 || _selectedRegion.Height <= 5)
            {
                Log("Please select a region on the preview first (click and drag).");
                return;
            }

            var actualRegion = ConvertToImageCoordinates(_selectedRegion);

            if (actualRegion.Width <= 0 || actualRegion.Height <= 0)
            {
                Log("Invalid selection. Please try again.");
                return;
            }

            try
            {
                // Dispose old template
                _currentTemplate?.Dispose();

                // Clone the selected region as the new template
                _currentTemplate = _currentScreenshot.Clone(actualRegion, _currentScreenshot.PixelFormat);
                templatePreviewPictureBox.Image = _currentTemplate;
                templatePathLabel.Text = $"Selection ({_currentTemplate.Width}x{_currentTemplate.Height})";

                Log($"Template set from selection: {_currentTemplate.Width}x{_currentTemplate.Height}");
                Log($"  Now click 'Find' or 'Find All' to search for this template.");
            }
            catch (Exception ex)
            {
                Log($"Failed to create template: {ex.Message}");
            }
        }

        private void SaveTemplateBtn_Click(object sender, EventArgs e)
        {
            if (_currentScreenshot == null)
            {
                Log("Please capture a screenshot first.");
                return;
            }

            if (_selectedRegion.IsEmpty || _selectedRegion.Width <= 5 || _selectedRegion.Height <= 5)
            {
                Log("Please select a region on the preview first (click and drag).");
                Log("Tip: Select a button or icon you want to find later.");
                return;
            }

            var actualRegion = ConvertToImageCoordinates(_selectedRegion);

            if (actualRegion.Width <= 0 || actualRegion.Height <= 0)
            {
                Log("Invalid selection. Please try again.");
                return;
            }

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "PNG Image|*.png";
                saveDialog.Title = "Save Template Image";
                saveDialog.InitialDirectory = TemplatesFolder;
                saveDialog.FileName = $"template_{DateTime.Now:yyyyMMdd_HHmmss}.png";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (var templateBitmap = _currentScreenshot.Clone(actualRegion, _currentScreenshot.PixelFormat))
                        {
                            templateBitmap.Save(saveDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);

                            // Load it as the current template
                            _currentTemplate?.Dispose();
                            _currentTemplate = new Bitmap(saveDialog.FileName);
                            templatePreviewPictureBox.Image = _currentTemplate;
                            templatePathLabel.Text = Path.GetFileName(saveDialog.FileName);

                            Log($"Template saved: {saveDialog.FileName}");
                            Log($"  Size: {_currentTemplate.Width}x{_currentTemplate.Height}");
                            Log($"  Region: ({actualRegion.X}, {actualRegion.Y})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to save template: {ex.Message}");
                    }
                }
            }
        }

        private async void FindAllTemplatesBtn_Click(object sender, EventArgs e)
        {
            if (_currentScreenshot == null)
            {
                Log("Please capture a screenshot first.");
                return;
            }

            if (_currentTemplate == null)
            {
                Log("Please load a template image first.");
                return;
            }

            // Disable buttons during search
            SetSearchButtonsEnabled(false, isSearching: true);
            Log("Searching for all template matches...");

            // Clone bitmaps for thread safety - GDI+ bitmaps can't be used across threads
            Bitmap screenshotCopy = null;
            Bitmap templateCopy = null;

            // Create cancellation token
            _searchCancellation?.Dispose();
            _searchCancellation = new CancellationTokenSource();
            var token = _searchCancellation.Token;

            try
            {
                double threshold = (double)thresholdNumeric.Value / 100.0;

                // Create copies for the background thread
                screenshotCopy = (Bitmap)_currentScreenshot.Clone();
                templateCopy = (Bitmap)_currentTemplate.Clone();

                // Progress callback to update UI
                var progress = new Progress<int>(p =>
                {
                    if (!token.IsCancellationRequested)
                        findAllTemplatesBtn.Text = $"{p}%";
                });

                // Run template matching on background thread
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var results = await Task.Run(() => ImageTemplateMatcher.FindAllTemplates(
                    screenshotCopy, templateCopy, threshold, 10, token,
                    p => ((IProgress<int>)progress).Report(p)));
                stopwatch.Stop();

                if (token.IsCancellationRequested)
                {
                    Log($"Search cancelled after {stopwatch.ElapsedMilliseconds}ms. Found {results.Count} matches before cancellation.");
                    _matchResults = results;
                    previewPictureBox.Invalidate();
                    return;
                }

                _matchResults = results;

                if (_matchResults.Count > 0)
                {
                    Log($"FOUND {_matchResults.Count} matches in {stopwatch.ElapsedMilliseconds}ms:");
                    for (int i = 0; i < _matchResults.Count; i++)
                    {
                        var r = _matchResults[i];
                        Log($"  [{i + 1}] Location: ({r.Location.X}, {r.Location.Y}), Center: ({r.Center.X}, {r.Center.Y}), Confidence: {r.Confidence:P1}");
                    }
                    previewPictureBox.Invalidate();
                }
                else
                {
                    Log($"No templates found matching the threshold. Search took {stopwatch.ElapsedMilliseconds}ms.");
                }
            }
            catch (Exception ex)
            {
                Log($"Template matching error: {ex.Message}");
            }
            finally
            {
                screenshotCopy?.Dispose();
                templateCopy?.Dispose();
                SetSearchButtonsEnabled(true, isSearching: false);
            }
        }

        private async void InitializeOCR()
        {
            try
            {
                if (!TessdataDownloader.LanguageDataExists())
                {
                    Log("OCR data not found. Downloading automatically (this may take a moment)...");
                    bool downloaded = await TessdataDownloader.EnsureLanguageDataExistsAsync();

                    if (!downloaded)
                    {
                        Log("Failed to download OCR data. Please check your internet connection.");
                        return;
                    }
                    Log("OCR data downloaded successfully!");
                }

                _ocr = new TextRecognition();
                Log("OCR engine initialized successfully.");
            }
            catch (Exception ex)
            {
                Log($"OCR initialization failed: {ex.Message}");
                Log("OCR features will be disabled. Please restart the app to retry download.");
            }
        }

        private void SetupRefreshTimer()
        {
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _refreshTimer.Tick += (s, e) => CaptureScreenshot();
        }

        private void CaptureBtn_Click(object sender, EventArgs e)
        {
            CaptureScreenshot();
        }

        private void CaptureScreenshot()
        {
            try
            {
                _currentScreenshot?.Dispose();
                _currentScreenshot = (Bitmap)ImageRecognition.GetWindowScreenshot();
                previewPictureBox.Image = _currentScreenshot;

                // Clear previous matches when capturing new screenshot
                _matchResults.Clear();
                previewPictureBox.Invalidate();

                Log($"Screenshot captured: {_currentScreenshot.Width}x{_currentScreenshot.Height}");
            }
            catch (Exception ex)
            {
                Log($"Failed to capture: {ex.Message}");
                Log("Make sure Toontown Rewritten is running.");
            }
        }

        private void AutoRefreshCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (autoRefreshCheckBox.Checked)
            {
                _refreshTimer.Interval = (int)refreshIntervalNumeric.Value;
                _refreshTimer.Start();
                Log($"Auto-refresh enabled ({refreshIntervalNumeric.Value}ms)");
            }
            else
            {
                _refreshTimer.Stop();
                Log("Auto-refresh disabled");
            }
        }

        private void AlwaysOnTopCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = alwaysOnTopCheckBox.Checked;
        }

        private void LoadTemplateBtn_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Image files|*.png;*.jpg;*.bmp|All files|*.*";
                dialog.Title = "Select Template Image";
                dialog.InitialDirectory = Directory.Exists(TemplatesFolder) ? TemplatesFolder : "";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _currentTemplate?.Dispose();
                        _currentTemplate = new Bitmap(dialog.FileName);
                        templatePreviewPictureBox.Image = _currentTemplate;
                        templatePathLabel.Text = Path.GetFileName(dialog.FileName);
                        Log($"Template loaded: {Path.GetFileName(dialog.FileName)} ({_currentTemplate.Width}x{_currentTemplate.Height})");
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to load template: {ex.Message}");
                    }
                }
            }
        }

        private async void FindTemplateBtn_Click(object sender, EventArgs e)
        {
            if (_currentScreenshot == null)
            {
                Log("Please capture a screenshot first.");
                return;
            }

            if (_currentTemplate == null)
            {
                Log("Please load a template image first.");
                return;
            }

            // Disable buttons during search
            SetSearchButtonsEnabled(false, isSearching: true);
            Log("Searching for template...");

            // Clone bitmaps for thread safety - GDI+ bitmaps can't be used across threads
            Bitmap screenshotCopy = null;
            Bitmap templateCopy = null;

            // Create cancellation token
            _searchCancellation?.Dispose();
            _searchCancellation = new CancellationTokenSource();
            var token = _searchCancellation.Token;

            try
            {
                double threshold = (double)thresholdNumeric.Value / 100.0;

                // Create copies for the background thread
                screenshotCopy = (Bitmap)_currentScreenshot.Clone();
                templateCopy = (Bitmap)_currentTemplate.Clone();

                // Progress callback to update UI
                var progress = new Progress<int>(p =>
                {
                    if (!token.IsCancellationRequested)
                        findTemplateBtn.Text = $"{p}%";
                });

                // Run template matching on background thread
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await Task.Run(() => ImageTemplateMatcher.FindTemplate(
                    screenshotCopy, templateCopy, threshold, token,
                    p => ((IProgress<int>)progress).Report(p)));
                stopwatch.Stop();

                if (token.IsCancellationRequested)
                {
                    Log($"Search cancelled after {stopwatch.ElapsedMilliseconds}ms.");
                    return;
                }

                _matchResults.Clear();

                if (result.Found)
                {
                    _matchResults.Add(result);
                    Log($"FOUND in {stopwatch.ElapsedMilliseconds}ms!");
                    Log($"  Location: ({result.Location.X}, {result.Location.Y})");
                    Log($"  Center (for clicking): ({result.Center.X}, {result.Center.Y})");
                    Log($"  Size: {result.Bounds.Width}x{result.Bounds.Height}");
                    Log($"  Confidence: {result.Confidence:P1}");
                    previewPictureBox.Invalidate();
                }
                else
                {
                    Log($"Template NOT found. Search took {stopwatch.ElapsedMilliseconds}ms.");
                    Log($"  Best match confidence: {result.Confidence:P1}");
                    Log("  Try lowering the threshold or capturing a new template.");
                }
            }
            catch (Exception ex)
            {
                Log($"Template matching error: {ex.Message}");
            }
            finally
            {
                screenshotCopy?.Dispose();
                templateCopy?.Dispose();
                SetSearchButtonsEnabled(true, isSearching: false);
            }
        }

        private void StopSearchBtn_Click(object sender, EventArgs e)
        {
            if (_searchCancellation != null && !_searchCancellation.IsCancellationRequested)
            {
                _searchCancellation.Cancel();
                Log("Stopping search...");
                stopSearchBtn.Enabled = false;
            }
        }

        private void SetSearchButtonsEnabled(bool enabled, bool isSearching = false)
        {
            findTemplateBtn.Enabled = enabled;
            findAllTemplatesBtn.Enabled = enabled;
            stopSearchBtn.Enabled = isSearching;
            findTemplateBtn.Text = enabled ? "Find" : "0%";
            findAllTemplatesBtn.Text = enabled ? "Find All" : "0%";
        }

        private void ReadTextBtn_Click(object sender, EventArgs e)
        {
            ReadFromRegionAsync(readNumbers: false);
        }

        private void ReadNumbersBtn_Click(object sender, EventArgs e)
        {
            ReadFromRegionAsync(readNumbers: true);
        }

        private async void ReadFullScreenBtn_Click(object sender, EventArgs e)
        {
            if (_currentScreenshot == null)
            {
                Log("Please capture a screenshot first.");
                return;
            }

            if (_ocr == null)
            {
                Log("OCR not initialized. Please wait for download to complete.");
                return;
            }

            SetOcrButtonsEnabled(false);
            Log("Reading text from full screen...");

            // Clone bitmap for thread safety
            Bitmap screenshotCopy = null;

            try
            {
                screenshotCopy = (Bitmap)_currentScreenshot.Clone();
                var ocr = _ocr;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                string text = await Task.Run(() => ocr.ReadText(screenshotCopy));
                stopwatch.Stop();

                Log($"Full screen OCR completed in {stopwatch.ElapsedMilliseconds}ms:");
                Log($"{text}");
            }
            catch (Exception ex)
            {
                Log($"OCR error: {ex.Message}");
            }
            finally
            {
                screenshotCopy?.Dispose();
                SetOcrButtonsEnabled(true);
            }
        }

        private async void ReadFromRegionAsync(bool readNumbers)
        {
            if (_currentScreenshot == null)
            {
                Log("Please capture a screenshot first.");
                return;
            }

            if (_selectedRegion.IsEmpty || _selectedRegion.Width <= 0 || _selectedRegion.Height <= 0)
            {
                Log("Please select a region by clicking and dragging on the preview.");
                return;
            }

            if (_ocr == null)
            {
                Log("OCR not initialized. Please wait for download to complete.");
                return;
            }

            SetOcrButtonsEnabled(false);
            string type = readNumbers ? "numbers" : "text";
            Log($"Reading {type} from selected region...");

            // Clone bitmap for thread safety
            Bitmap screenshotCopy = null;

            try
            {
                var actualRegion = ConvertToImageCoordinates(_selectedRegion);
                screenshotCopy = (Bitmap)_currentScreenshot.Clone();
                var ocr = _ocr;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                string result = await Task.Run(() => readNumbers
                    ? ocr.ReadNumbersFromRegion(screenshotCopy, actualRegion)
                    : ocr.ReadTextFromRegion(screenshotCopy, actualRegion));
                stopwatch.Stop();

                string typeCapital = readNumbers ? "Numbers" : "Text";
                Log($"{typeCapital} from region ({actualRegion.X}, {actualRegion.Y}, {actualRegion.Width}x{actualRegion.Height}) - {stopwatch.ElapsedMilliseconds}ms:");
                Log($"  Result: \"{result}\"");
            }
            catch (Exception ex)
            {
                Log($"OCR error: {ex.Message}");
            }
            finally
            {
                screenshotCopy?.Dispose();
                SetOcrButtonsEnabled(true);
            }
        }

        private void SetOcrButtonsEnabled(bool enabled)
        {
            readTextBtn.Enabled = enabled;
            readNumbersBtn.Enabled = enabled;
            readFullScreenBtn.Enabled = enabled;
        }

        private Rectangle ConvertToImageCoordinates(Rectangle previewRect)
        {
            if (_currentScreenshot == null || previewPictureBox.Image == null)
                return previewRect;

            float imageAspect = (float)_currentScreenshot.Width / _currentScreenshot.Height;
            float boxAspect = (float)previewPictureBox.Width / previewPictureBox.Height;

            float scale;
            int offsetX = 0, offsetY = 0;

            if (imageAspect > boxAspect)
            {
                scale = (float)previewPictureBox.Width / _currentScreenshot.Width;
                offsetY = (int)((previewPictureBox.Height - _currentScreenshot.Height * scale) / 2);
            }
            else
            {
                scale = (float)previewPictureBox.Height / _currentScreenshot.Height;
                offsetX = (int)((previewPictureBox.Width - _currentScreenshot.Width * scale) / 2);
            }

            int x = (int)((previewRect.X - offsetX) / scale);
            int y = (int)((previewRect.Y - offsetY) / scale);
            int width = (int)(previewRect.Width / scale);
            int height = (int)(previewRect.Height / scale);

            x = Math.Max(0, Math.Min(x, _currentScreenshot.Width));
            y = Math.Max(0, Math.Min(y, _currentScreenshot.Height));
            width = Math.Min(width, _currentScreenshot.Width - x);
            height = Math.Min(height, _currentScreenshot.Height - y);

            return new Rectangle(x, y, width, height);
        }

        private Rectangle ConvertToPreviewCoordinates(Rectangle imageRect)
        {
            if (_currentScreenshot == null || previewPictureBox.Image == null)
                return imageRect;

            float imageAspect = (float)_currentScreenshot.Width / _currentScreenshot.Height;
            float boxAspect = (float)previewPictureBox.Width / previewPictureBox.Height;

            float scale;
            int offsetX = 0, offsetY = 0;

            if (imageAspect > boxAspect)
            {
                scale = (float)previewPictureBox.Width / _currentScreenshot.Width;
                offsetY = (int)((previewPictureBox.Height - _currentScreenshot.Height * scale) / 2);
            }
            else
            {
                scale = (float)previewPictureBox.Height / _currentScreenshot.Height;
                offsetX = (int)((previewPictureBox.Width - _currentScreenshot.Width * scale) / 2);
            }

            int x = (int)(imageRect.X * scale + offsetX);
            int y = (int)(imageRect.Y * scale + offsetY);
            int width = (int)(imageRect.Width * scale);
            int height = (int)(imageRect.Height * scale);

            return new Rectangle(x, y, width, height);
        }

        #region Preview Mouse Events

        private void PreviewPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isSelectingRegion = true;
                _selectionStart = e.Location;
                _selectedRegion = new Rectangle(e.Location, Size.Empty);
            }
        }

        private void PreviewPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentScreenshot != null)
            {
                var imgCoords = ConvertToImageCoordinates(new Rectangle(e.Location, Size.Empty));
                string selectionInfo = _selectedRegion.IsEmpty ? "none" :
                    $"{_selectedRegion.Width}x{_selectedRegion.Height}";
                coordsLabel.Text = $"Mouse: ({e.X}, {e.Y}) | Image: ({imgCoords.X}, {imgCoords.Y}) | Selection: {selectionInfo}";
            }

            if (_isSelectingRegion)
            {
                int x = Math.Min(_selectionStart.X, e.X);
                int y = Math.Min(_selectionStart.Y, e.Y);
                int width = Math.Abs(e.X - _selectionStart.X);
                int height = Math.Abs(e.Y - _selectionStart.Y);
                _selectedRegion = new Rectangle(x, y, width, height);
                previewPictureBox.Invalidate();
            }
        }

        private void PreviewPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isSelectingRegion)
            {
                _isSelectingRegion = false;
                if (_selectedRegion.Width > 5 && _selectedRegion.Height > 5)
                {
                    var actualRegion = ConvertToImageCoordinates(_selectedRegion);
                    Log($"Region selected: ({actualRegion.X}, {actualRegion.Y}) - Size: {actualRegion.Width}x{actualRegion.Height}");
                }
            }
        }

        private void PreviewPictureBox_Paint(object sender, PaintEventArgs e)
        {
            // Draw selection rectangle (blue)
            if (!_selectedRegion.IsEmpty && _selectedRegion.Width > 0 && _selectedRegion.Height > 0)
            {
                using (var pen = new Pen(Color.Blue, 2))
                using (var brush = new SolidBrush(Color.FromArgb(30, Color.Blue)))
                {
                    e.Graphics.FillRectangle(brush, _selectedRegion);
                    e.Graphics.DrawRectangle(pen, _selectedRegion);
                }
            }

            // Draw template match results (green for found)
            foreach (var match in _matchResults)
            {
                var previewBounds = ConvertToPreviewCoordinates(match.Bounds);
                using (var pen = new Pen(Color.LimeGreen, 3))
                using (var brush = new SolidBrush(Color.FromArgb(50, Color.LimeGreen)))
                {
                    e.Graphics.FillRectangle(brush, previewBounds);
                    e.Graphics.DrawRectangle(pen, previewBounds);

                    // Draw center crosshair
                    var center = ConvertToPreviewCoordinates(new Rectangle(match.Center.X - 5, match.Center.Y - 5, 10, 10));
                    e.Graphics.DrawLine(pen, center.X, center.Y - 10, center.X, center.Y + 10);
                    e.Graphics.DrawLine(pen, center.X - 10, center.Y, center.X + 10, center.Y);

                    // Draw confidence label
                    string label = $"{match.Confidence:P0}";
                    using (var font = new Font("Arial", 9, FontStyle.Bold))
                    using (var bgBrush = new SolidBrush(Color.LimeGreen))
                    {
                        var textSize = e.Graphics.MeasureString(label, font);
                        e.Graphics.FillRectangle(bgBrush, previewBounds.X, previewBounds.Y - textSize.Height - 2, textSize.Width + 4, textSize.Height);
                        e.Graphics.DrawString(label, font, Brushes.Black, previewBounds.X + 2, previewBounds.Y - textSize.Height - 1);
                    }
                }
            }

            // Draw fish scan area (orange dashed)
            if (!_fishScanArea.IsEmpty)
            {
                var previewScanArea = ConvertToPreviewCoordinates(_fishScanArea);
                using (var pen = new Pen(Color.Orange, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                using (var brush = new SolidBrush(Color.FromArgb(20, Color.Orange)))
                {
                    e.Graphics.FillRectangle(brush, previewScanArea);
                    e.Graphics.DrawRectangle(pen, previewScanArea);

                    // Label
                    using (var font = new Font("Arial", 8))
                    {
                        e.Graphics.DrawString("Scan Area", font, Brushes.Orange, previewScanArea.X + 2, previewScanArea.Y + 2);
                    }
                }
            }

            // Draw shadow blobs (GREEN - from computer vision detection)
            foreach (var blob in _shadowBlobs)
            {
                using (var brush = new SolidBrush(Color.FromArgb(80, Color.Lime)))
                {
                    foreach (var point in blob)
                    {
                        var previewPos = ConvertToPreviewCoordinates(new Rectangle(point.X, point.Y, 1, 1));
                        e.Graphics.FillRectangle(brush, previewPos.X - 2, previewPos.Y - 2, 5, 5);
                    }
                }
            }

            // Draw fish bubble detection results (magenta circles for best shadow)
            if (_fishBubbleResults.Count > 0)
            {
                // First result is from shadow detection (if found)
                var firstResult = _fishBubbleResults[0];
                var previewPos = ConvertToPreviewCoordinates(new Rectangle(firstResult.X, firstResult.Y, 1, 1));
                using (var pen = new Pen(Color.Lime, 3))
                using (var brush = new SolidBrush(Color.FromArgb(100, Color.Lime)))
                {
                    // Draw larger circle for best shadow detection
                    e.Graphics.FillEllipse(brush, previewPos.X - 15, previewPos.Y - 15, 30, 30);
                    e.Graphics.DrawEllipse(pen, previewPos.X - 15, previewPos.Y - 15, 30, 30);

                    // Draw crosshair
                    e.Graphics.DrawLine(pen, previewPos.X - 20, previewPos.Y, previewPos.X + 20, previewPos.Y);
                    e.Graphics.DrawLine(pen, previewPos.X, previewPos.Y - 20, previewPos.X, previewPos.Y + 20);
                }

                // Draw other results (color matches) in yellow
                for (int i = 1; i < _fishBubbleResults.Count; i++)
                {
                    var pos = ConvertToPreviewCoordinates(new Rectangle(_fishBubbleResults[i].X, _fishBubbleResults[i].Y, 1, 1));
                    using (var pen = new Pen(Color.Yellow, 2))
                    {
                        // Draw X mark for color matches
                        e.Graphics.DrawLine(pen, pos.X - 8, pos.Y - 8, pos.X + 8, pos.Y + 8);
                        e.Graphics.DrawLine(pen, pos.X + 8, pos.Y - 8, pos.X - 8, pos.Y + 8);
                        e.Graphics.DrawEllipse(pen, pos.X - 10, pos.Y - 10, 20, 20);
                    }
                }
            }

            // Draw calculated rod position (RED circle - where bot thinks Cast button is)
            if (!_calculatedRodPosition.IsEmpty)
            {
                var previewRod = ConvertToPreviewCoordinates(new Rectangle(_calculatedRodPosition.X, _calculatedRodPosition.Y, 1, 1));
                using (var pen = new Pen(Color.Red, 3))
                using (var brush = new SolidBrush(Color.FromArgb(100, Color.Red)))
                {
                    // Draw a large circle for the rod button position
                    e.Graphics.FillEllipse(brush, previewRod.X - 20, previewRod.Y - 20, 40, 40);
                    e.Graphics.DrawEllipse(pen, previewRod.X - 20, previewRod.Y - 20, 40, 40);

                    // Label
                    using (var font = new Font("Arial", 8, FontStyle.Bold))
                    {
                        e.Graphics.DrawString("ROD", font, Brushes.Red, previewRod.X - 12, previewRod.Y - 6);
                    }
                }
            }

            // Draw cast destination and line (CYAN)
            if (!_calculatedRodPosition.IsEmpty && !_calculatedCastPosition.IsEmpty)
            {
                var previewRod = ConvertToPreviewCoordinates(new Rectangle(_calculatedRodPosition.X, _calculatedRodPosition.Y, 1, 1));
                var previewCast = ConvertToPreviewCoordinates(new Rectangle(_calculatedCastPosition.X, _calculatedCastPosition.Y, 1, 1));

                using (var pen = new Pen(Color.Cyan, 3))
                using (var brush = new SolidBrush(Color.FromArgb(100, Color.Cyan)))
                {
                    // Draw line from rod to cast destination
                    e.Graphics.DrawLine(pen, previewRod.X, previewRod.Y, previewCast.X, previewCast.Y);

                    // Draw cast destination marker
                    e.Graphics.FillEllipse(brush, previewCast.X - 12, previewCast.Y - 12, 24, 24);
                    e.Graphics.DrawEllipse(pen, previewCast.X - 12, previewCast.Y - 12, 24, 24);

                    // Arrow head
                    float angle = (float)Math.Atan2(previewCast.Y - previewRod.Y, previewCast.X - previewRod.X);
                    float arrowSize = 15;
                    var arrowPoint1 = new PointF(
                        previewCast.X - arrowSize * (float)Math.Cos(angle - 0.4f),
                        previewCast.Y - arrowSize * (float)Math.Sin(angle - 0.4f));
                    var arrowPoint2 = new PointF(
                        previewCast.X - arrowSize * (float)Math.Cos(angle + 0.4f),
                        previewCast.Y - arrowSize * (float)Math.Sin(angle + 0.4f));
                    e.Graphics.DrawLine(pen, previewCast.X, previewCast.Y, arrowPoint1.X, arrowPoint1.Y);
                    e.Graphics.DrawLine(pen, previewCast.X, previewCast.Y, arrowPoint2.X, arrowPoint2.Y);

                    // Label
                    using (var font = new Font("Arial", 8, FontStyle.Bold))
                    {
                        e.Graphics.DrawString("CAST", font, Brushes.Cyan, previewCast.X - 15, previewCast.Y + 15);
                    }
                }
            }
        }

        #endregion

        #region Fish Detection

        private async void FindFishBtn_Click(object sender, EventArgs e)
        {
            if (_currentScreenshot == null)
            {
                Log("Please capture a screenshot first.");
                return;
            }

            string locationName = fishLocationComboBox.SelectedItem?.ToString() ?? "Fish Anywhere";
            Log($"=== Scanning for fish shadows at {locationName} ===");
            Log($"(Using shared FishBubbleDetector - same code as actual fishing)");

            findFishBtn.Enabled = false;
            confirmFishBtn.Enabled = false;
            rejectFishBtn.Enabled = false;

            try
            {
                // Use the shared FishBubbleDetector - same code as actual fishing
                _currentFishDetector = new FishBubbleDetector(locationName);
                var config = _currentFishDetector.GetCurrentConfig();

                _fishBubbleColor = config.BubbleColor;

                // Store screenshot for confirmation
                _lastFishScreenshot?.Dispose();
                _lastFishScreenshot = (Bitmap)_currentScreenshot.Clone();

                // Run detection using shared detector
                var result = await Task.Run(() => _currentFishDetector.DetectFromScreenshot(_lastFishScreenshot));

                // Store results for visualization
                _fishScanArea = result.ScanArea;
                _shadowBlobs = result.Blobs ?? new List<List<Point>>();
                _fishBubbleResults.Clear();

                // Show calibration status
                if (result.UsingLearnedColor && result.LearnedColor.HasValue)
                {
                    var lc = result.LearnedColor.Value;
                    Log($"CALIBRATED: Using learned color RGB({lc.R}, {lc.G}, {lc.B})");
                }
                else
                {
                    Log("NOT CALIBRATED: Using general dark pixel detection");
                    Log("  -> If a fish is detected, click 'Yes, Fish!' to calibrate");
                }

                string detectionMethod = result.UsedDynamicPondDetection ? "DYNAMIC (auto-detected pond)" : "CONFIG (predefined)";
                Log($"Scan area: ({result.ScanArea.X}, {result.ScanArea.Y}) - {result.ScanArea.Width}x{result.ScanArea.Height}");
                Log($"Detection method: {detectionMethod}");
                Log($"Target color: RGB({result.TargetBubbleColor.R}, {result.TargetBubbleColor.G}, {result.TargetBubbleColor.B})");
                Log($"Tolerance: R±{result.ColorTolerance.R}, G±{result.ColorTolerance.G}, B±{result.ColorTolerance.B}");
                Log("");
                Log("--- Shadow Detection Results ---");
                Log($"Avg brightness: {result.AvgBrightness}, Threshold: {result.DarkThreshold}");
                Log($"Dark pixels found: {result.DarkPixelCount}");
                Log($"Blobs found: {result.Blobs?.Count ?? 0} ({result.RejectedBlobCount} rejected)");
                Log($"Valid candidates: {result.CandidateCount} ({result.CandidatesWithBubbles} with bubbles above)");

                // Show all candidates with their cast power (same info actual fishing uses)
                if (result.AllCandidates != null && result.AllCandidates.Count > 0)
                {
                    Log("");
                    Log("--- All Candidates (sorted by ease) ---");
                    var sortedCandidates = result.AllCandidates.OrderBy(c => c.CastPower).ToList();
                    for (int i = 0; i < sortedCandidates.Count; i++)
                    {
                        var c = sortedCandidates[i];
                        string marker = i == 0 ? " ← SELECTED (easiest)" : "";
                        string bubbles = c.HasBubblesAbove ? " [BUBBLES]" : "";
                        Log($"  #{i + 1}: ({c.Position.X},{c.Position.Y}) CastPower={c.CastPower:F1}{bubbles}{marker}");
                    }
                }

                // USE SAME SELECTION LOGIC AS ACTUAL FISHING:
                // Pick the easiest candidate (lowest CastPower), not just BestShadowPosition
                Point? selectedFishPosition = null;
                if (result.AllCandidates != null && result.AllCandidates.Count > 0)
                {
                    var easiest = result.AllCandidates.OrderBy(c => c.CastPower).First();
                    selectedFishPosition = easiest.Position;
                    Log("");
                    Log($"=== FISH SELECTED (same as actual fishing) ===");
                    Log($"Position: ({easiest.Position.X}, {easiest.Position.Y})");
                    Log($"CastPower: {easiest.CastPower:F1} (lower = easier)");
                    Log($"Has bubbles: {(easiest.HasBubblesAbove ? "YES" : "no")}");
                }
                else if (result.BestShadowPosition.HasValue)
                {
                    selectedFishPosition = result.BestShadowPosition.Value;
                    Log("");
                    Log($"=== SHADOW DETECTED (fallback) ===");
                    Log($"Position: ({result.BestShadowPosition.Value.X}, {result.BestShadowPosition.Value.Y})");
                    string bubbleStatus = result.HasBubblesAbove ? "YES - CONFIRMED FISH!" : "no bubbles detected";
                    Log($"Shadow color: RGB({result.BestShadowColor.R}, {result.BestShadowColor.G}, {result.BestShadowColor.B})");
                    Log($"Bubbles above: {bubbleStatus}");
                }

                if (selectedFishPosition.HasValue)
                {
                    _fishBubbleResults.Add(selectedFishPosition.Value);

                    // Calculate cast using same method as actual fishing
                    var castResult = _currentFishDetector.CalculateCastFromPosition(
                        selectedFishPosition.Value.X, selectedFishPosition.Value.Y);

                    if (castResult != null)
                    {
                        _calculatedRodPosition = castResult.RodButtonPosition;
                        _calculatedCastPosition = castResult.CastDestination;

                        Log("");
                        Log("=== CAST CALCULATION (same as actual fishing) ===");
                        Log($"Rod position: ({_calculatedRodPosition.X}, {_calculatedRodPosition.Y})");
                        Log($"Cast to: ({_calculatedCastPosition.X}, {_calculatedCastPosition.Y})");
                    }
                    else if (result.RodButtonPosition.HasValue && result.CastDestination.HasValue)
                    {
                        _calculatedRodPosition = result.RodButtonPosition.Value;
                        _calculatedCastPosition = result.CastDestination.Value;

                        Log("");
                        Log("=== CAST CALCULATION ===");
                        Log($"Rod position: ({_calculatedRodPosition.X}, {_calculatedRodPosition.Y})");
                        Log($"Cast to: ({_calculatedCastPosition.X}, {_calculatedCastPosition.Y})");
                    }

                    // Enable confirm/reject buttons if we found something
                    confirmFishBtn.Enabled = true;
                    rejectFishBtn.Enabled = true;
                    Log("");
                    Log(">>> Is this a fish? Click 'Yes, Fish!' to learn this color, or 'Not Fish' to reject <<<");
                }
                else
                {
                    Log("No fish shadows detected.");
                    if (result.CandidateCount > 0)
                    {
                        Log($"  ({result.CandidateCount} candidates found but none passed validation)");
                    }
                    if (result.RejectedBlobCount > 0)
                    {
                        Log($"  ({result.RejectedBlobCount} blobs rejected - may be dock posts)");
                    }
                    _calculatedRodPosition = Point.Empty;
                    _calculatedCastPosition = Point.Empty;
                }

                Log("");
                Log("LEGEND: GREEN = shadow blobs, LIME circle = best shadow");
                Log("        RED circle = rod button, CYAN line = cast direction");

                previewPictureBox.Invalidate();
            }
            catch (Exception ex)
            {
                Log($"Error scanning for fish: {ex.Message}");
            }
            finally
            {
                findFishBtn.Enabled = true;
            }
        }

        // Shadow blobs for visualization (populated from FishBubbleDetector results)
        private List<List<Point>> _shadowBlobs = new List<List<Point>>();

        private void SampleColorBtn_Click(object sender, EventArgs e)
        {
            if (_currentScreenshot == null)
            {
                Log("Please capture a screenshot first.");
                return;
            }

            if (_selectedRegion.IsEmpty || _selectedRegion.Width <= 0 || _selectedRegion.Height <= 0)
            {
                Log("Please select a region on the preview (click on a fish shadow).");
                Log("Tip: Click on the dark shadow/bubble where a fish is visible.");
                return;
            }

            var actualRegion = ConvertToImageCoordinates(_selectedRegion);

            // Sample center pixel of selection
            int centerX = actualRegion.X + actualRegion.Width / 2;
            int centerY = actualRegion.Y + actualRegion.Height / 2;

            if (centerX >= 0 && centerX < _currentScreenshot.Width &&
                centerY >= 0 && centerY < _currentScreenshot.Height)
            {
                var color = _currentScreenshot.GetPixel(centerX, centerY);
                Log($"Sampled color at ({centerX}, {centerY}): RGB({color.R}, {color.G}, {color.B})");
                Log($"  Hex: #{color.R:X2}{color.G:X2}{color.B:X2}");

                // Also sample a small area and show average/range
                var colors = new List<Color>();
                for (int y = Math.Max(0, centerY - 5); y < Math.Min(_currentScreenshot.Height, centerY + 5); y++)
                {
                    for (int x = Math.Max(0, centerX - 5); x < Math.Min(_currentScreenshot.Width, centerX + 5); x++)
                    {
                        colors.Add(_currentScreenshot.GetPixel(x, y));
                    }
                }

                if (colors.Count > 0)
                {
                    int avgR = (int)colors.Average(c => c.R);
                    int avgG = (int)colors.Average(c => c.G);
                    int avgB = (int)colors.Average(c => c.B);
                    Log($"  Area average (10x10): RGB({avgR}, {avgG}, {avgB})");

                    // Compare to expected color for selected location using shared FishBubbleDetector
                    string locationName = fishLocationComboBox.SelectedItem?.ToString() ?? "Fish Anywhere";
                    var detector = new FishBubbleDetector(locationName);
                    var config = detector.GetCurrentConfig();

                    var expected = config.BubbleColor;
                    Log($"  Expected for {locationName}: RGB({expected.R}, {expected.G}, {expected.B})");
                    int diffR = Math.Abs(color.R - expected.R);
                    int diffG = Math.Abs(color.G - expected.G);
                    int diffB = Math.Abs(color.B - expected.B);
                    Log($"  Difference: R={diffR}, G={diffG}, B={diffB}");

                    if (diffR <= config.ColorTolerance.R &&
                        diffG <= config.ColorTolerance.G &&
                        diffB <= config.ColorTolerance.B)
                    {
                        Log("  This color MATCHES the expected fish bubble color!");
                    }
                    else
                    {
                        Log("  This color does NOT match the expected fish bubble color.");
                    }
                }
            }
            else
            {
                Log("Selection is outside the screenshot bounds.");
            }
        }

        private void ConfirmFishBtn_Click(object sender, EventArgs e)
        {
            if (_currentFishDetector == null || _lastFishScreenshot == null || _fishBubbleResults.Count == 0)
            {
                Log("No fish detection to confirm. Run 'Find Fish' first.");
                return;
            }

            var shadowPos = _fishBubbleResults[0];
            _currentFishDetector.ConfirmFishShadow(_lastFishScreenshot, shadowPos);

            var learnedColor = _currentFishDetector.GetLearnedShadowColor();
            if (learnedColor.HasValue)
            {
                var lc = learnedColor.Value;
                Log($"CALIBRATED! Learned fish shadow color: RGB({lc.R}, {lc.G}, {lc.B})");
                Log("Future scans will use this color for faster, more accurate detection.");
            }

            confirmFishBtn.Enabled = false;
            rejectFishBtn.Enabled = false;
        }

        private void RejectFishBtn_Click(object sender, EventArgs e)
        {
            if (_currentFishDetector == null)
            {
                Log("No fish detection to reject.");
                return;
            }

            _currentFishDetector.RejectFishShadow();
            Log("Detection rejected. Try 'Find Fish' again to find a different shadow.");

            confirmFishBtn.Enabled = false;
            rejectFishBtn.Enabled = false;
        }

        private void ResetCalibrationBtn_Click(object sender, EventArgs e)
        {
            if (_currentFishDetector != null)
            {
                _currentFishDetector.ResetLearnedColor();
            }
            Log("Calibration reset. Detection will use general dark pixel detection until you confirm a fish.");

            confirmFishBtn.Enabled = false;
            rejectFishBtn.Enabled = false;
        }

        #endregion

        private void Log(string message)
        {
            if (outputTextBox.InvokeRequired)
            {
                outputTextBox.Invoke(new Action(() => Log(message)));
                return;
            }

            outputTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            outputTextBox.ScrollToCaret();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _searchCancellation?.Cancel();
            _searchCancellation?.Dispose();
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _currentScreenshot?.Dispose();
            _currentTemplate?.Dispose();
            _lastFishScreenshot?.Dispose();
            _ocr?.Dispose();
            base.OnFormClosing(e);
        }

        #region Controls
        private PictureBox previewPictureBox;
        private PictureBox templatePreviewPictureBox;
        private Button captureBtn;
        private CheckBox autoRefreshCheckBox;
        private CheckBox alwaysOnTopCheckBox;
        private NumericUpDown refreshIntervalNumeric;
        private Button loadTemplateBtn;
        private Button useSelectionBtn;
        private Button saveTemplateBtn;
        private Button openTemplatesFolderBtn;
        private Label templatePathLabel;
        private Button findTemplateBtn;
        private Button findAllTemplatesBtn;
        private Button stopSearchBtn;
        private Button clearMatchesBtn;
        private NumericUpDown thresholdNumeric;
        private Button readTextBtn;
        private Button readNumbersBtn;
        private Button readFullScreenBtn;
        private TextBox outputTextBox;
        private Label coordsLabel;
        private ComboBox fishLocationComboBox;
        private Button findFishBtn;
        private Button sampleColorBtn;
        private Button confirmFishBtn;
        private Button rejectFishBtn;
        private Button resetCalibrationBtn;
        private ComboBox templateDefinitionsComboBox;
        private Button testTemplateBtn;
        #endregion
    }
}
