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
            this.Size = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(1000, 650);

            // Main vertical split - Preview on top, Controls on bottom
            var mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };
            this.Controls.Add(mainSplit);

            // Set splitter constraints after form is loaded to avoid constraint errors
            this.Load += (s, e) =>
            {
                try
                {
                    mainSplit.Panel1MinSize = 200;
                    mainSplit.Panel2MinSize = 180;
                    mainSplit.SplitterDistance = Math.Max(200, this.ClientSize.Height - 280);
                }
                catch { /* Ignore if constraints can't be satisfied */ }
            };

            // === TOP PANEL: Preview Areas ===
            var previewSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };
            mainSplit.Panel1.Controls.Add(previewSplit);

            // Set preview split constraints in Load event
            this.Load += (s, e) =>
            {
                try
                {
                    previewSplit.Panel2MinSize = 180;
                    previewSplit.SplitterDistance = Math.Max(100, this.ClientSize.Width - 250);
                }
                catch { }
            };

            // Main preview
            var previewGroup = new GroupBox
            {
                Text = "Game Window Preview (Click and drag to select region)",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            previewSplit.Panel1.Controls.Add(previewGroup);

            previewPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            previewPictureBox.MouseDown += PreviewPictureBox_MouseDown;
            previewPictureBox.MouseMove += PreviewPictureBox_MouseMove;
            previewPictureBox.MouseUp += PreviewPictureBox_MouseUp;
            previewPictureBox.Paint += PreviewPictureBox_Paint;
            previewGroup.Controls.Add(previewPictureBox);

            // Template preview panel
            var templatePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            templatePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            templatePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            previewSplit.Panel2.Controls.Add(templatePanel);

            var templatePreviewGroup = new GroupBox
            {
                Text = "Template",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 2)
            };
            templatePanel.Controls.Add(templatePreviewGroup, 0, 0);

            templatePreviewPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(60, 60, 65),
                BorderStyle = BorderStyle.FixedSingle
            };
            templatePreviewGroup.Controls.Add(templatePreviewPictureBox);

            // Output panel (small)
            var outputGroup = new GroupBox
            {
                Text = "Output",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 0)
            };
            templatePanel.Controls.Add(outputGroup, 0, 1);

            outputTextBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 8),
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGray
            };
            outputGroup.Controls.Add(outputTextBox);

            // === BOTTOM PANEL: All Controls ===
            var controlsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            mainSplit.Panel2.Controls.Add(controlsPanel);

            var controlsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1
            };
            controlsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170)); // Capture
            controlsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170)); // Create Template
            controlsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270)); // Test Templates
            controlsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180)); // OCR
            controlsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Fish
            controlsPanel.Controls.Add(controlsTable);

            // === GROUP 1: Capture ===
            var captureGroup = new GroupBox { Text = "Capture", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 3, 0) };
            controlsTable.Controls.Add(captureGroup, 0, 0);

            var captureFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(5) };
            captureGroup.Controls.Add(captureFlow);

            var captureRow1 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            captureFlow.Controls.Add(captureRow1);

            captureBtn = new Button { Text = "Capture", Size = new Size(75, 28), Margin = new Padding(0, 0, 5, 0) };
            captureBtn.Click += CaptureBtn_Click;
            captureRow1.Controls.Add(captureBtn);

            clearMatchesBtn = new Button { Text = "Clear", Size = new Size(55, 28) };
            clearMatchesBtn.Click += (s, e) => {
                _matchResults.Clear();
                _selectedRegion = Rectangle.Empty;
                previewPictureBox.Invalidate();
                Log("Cleared.");
            };
            captureRow1.Controls.Add(clearMatchesBtn);

            var captureRow2 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            captureFlow.Controls.Add(captureRow2);

            autoRefreshCheckBox = new CheckBox { Text = "Auto", AutoSize = true, Margin = new Padding(0, 3, 3, 0) };
            autoRefreshCheckBox.CheckedChanged += AutoRefreshCheckBox_CheckedChanged;
            captureRow2.Controls.Add(autoRefreshCheckBox);

            refreshIntervalNumeric = new NumericUpDown { Size = new Size(50, 22), Minimum = 100, Maximum = 5000, Value = 500, Increment = 100 };
            captureRow2.Controls.Add(refreshIntervalNumeric);

            captureRow2.Controls.Add(new Label { Text = "ms", AutoSize = true, Margin = new Padding(2, 4, 0, 0) });

            var captureRow3 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            captureFlow.Controls.Add(captureRow3);

            alwaysOnTopCheckBox = new CheckBox { Text = "Always On Top", AutoSize = true };
            alwaysOnTopCheckBox.CheckedChanged += AlwaysOnTopCheckBox_CheckedChanged;
            captureRow3.Controls.Add(alwaysOnTopCheckBox);

            // === GROUP 2: Create Template ===
            var templateGroup = new GroupBox { Text = "Create Template", Dock = DockStyle.Fill, Margin = new Padding(3, 0, 3, 0) };
            controlsTable.Controls.Add(templateGroup, 1, 0);

            var templateFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(5) };
            templateGroup.Controls.Add(templateFlow);

            var templateRow1 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            templateFlow.Controls.Add(templateRow1);

            useSelectionBtn = new Button { Text = "Use Selection", Size = new Size(95, 28), Margin = new Padding(0, 0, 5, 0) };
            useSelectionBtn.Click += UseSelectionBtn_Click;
            templateRow1.Controls.Add(useSelectionBtn);

            saveTemplateBtn = new Button { Text = "Save", Size = new Size(55, 28) };
            saveTemplateBtn.Click += SaveTemplateBtn_Click;
            templateRow1.Controls.Add(saveTemplateBtn);

            var templateRow2 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            templateFlow.Controls.Add(templateRow2);

            openTemplatesFolderBtn = new Button { Text = "Open Folder", Size = new Size(95, 28) };
            openTemplatesFolderBtn.Click += (s, e) => System.Diagnostics.Process.Start("explorer.exe", TemplatesFolder);
            templateRow2.Controls.Add(openTemplatesFolderBtn);

            // === GROUP 3: Test Templates ===
            var searchGroup = new GroupBox { Text = "Test Templates", Dock = DockStyle.Fill, Margin = new Padding(3, 0, 3, 0) };
            controlsTable.Controls.Add(searchGroup, 2, 0);

            var searchFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(5) };
            searchGroup.Controls.Add(searchFlow);

            // Template dropdown row
            var templateSelectRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            searchFlow.Controls.Add(templateSelectRow);

            templateDefinitionsComboBox = new ComboBox { Size = new Size(180, 24), DropDownStyle = ComboBoxStyle.DropDownList, DropDownHeight = 300 };
            templateDefinitionsComboBox.SelectedIndexChanged += TemplateDefinitionsComboBox_SelectedIndexChanged;
            templateSelectRow.Controls.Add(templateDefinitionsComboBox);

            testTemplateBtn = new Button { Text = "Test", Size = new Size(50, 26), Margin = new Padding(5, 0, 0, 0) };
            testTemplateBtn.Click += TestTemplateBtn_Click;
            templateSelectRow.Controls.Add(testTemplateBtn);

            // Load from file row
            var searchRow1 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            searchFlow.Controls.Add(searchRow1);

            loadTemplateBtn = new Button { Text = "Load File", Size = new Size(65, 26), Margin = new Padding(0, 0, 5, 0) };
            loadTemplateBtn.Click += LoadTemplateBtn_Click;
            searchRow1.Controls.Add(loadTemplateBtn);

            templatePathLabel = new Label { Text = "No template", AutoSize = true, Margin = new Padding(0, 5, 0, 0), ForeColor = Color.Gray, MaximumSize = new Size(120, 0) };
            searchRow1.Controls.Add(templatePathLabel);

            // Find buttons row
            var searchRow2 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            searchFlow.Controls.Add(searchRow2);

            findTemplateBtn = new Button { Text = "Find", Size = new Size(50, 26), Margin = new Padding(0, 0, 3, 0) };
            findTemplateBtn.Click += FindTemplateBtn_Click;
            searchRow2.Controls.Add(findTemplateBtn);

            findAllTemplatesBtn = new Button { Text = "Find All", Size = new Size(60, 26), Margin = new Padding(0, 0, 3, 0) };
            findAllTemplatesBtn.Click += FindAllTemplatesBtn_Click;
            searchRow2.Controls.Add(findAllTemplatesBtn);

            stopSearchBtn = new Button { Text = "Stop", Size = new Size(45, 26), Enabled = false, BackColor = Color.IndianRed, ForeColor = Color.White };
            stopSearchBtn.Click += StopSearchBtn_Click;
            searchRow2.Controls.Add(stopSearchBtn);

            // Threshold row
            var searchRow3 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            searchFlow.Controls.Add(searchRow3);

            searchRow3.Controls.Add(new Label { Text = "Threshold:", AutoSize = true, Margin = new Padding(0, 4, 3, 0) });
            thresholdNumeric = new NumericUpDown { Size = new Size(45, 22), Minimum = 50, Maximum = 100, Value = 85 };
            searchRow3.Controls.Add(thresholdNumeric);
            searchRow3.Controls.Add(new Label { Text = "%", AutoSize = true, Margin = new Padding(2, 4, 0, 0) });

            // === GROUP 4: OCR ===
            var ocrGroup = new GroupBox { Text = "OCR (Text Recognition)", Dock = DockStyle.Fill, Margin = new Padding(3, 0, 3, 0) };
            controlsTable.Controls.Add(ocrGroup, 3, 0);

            var ocrFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(5) };
            ocrGroup.Controls.Add(ocrFlow);

            var ocrRow1 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            ocrFlow.Controls.Add(ocrRow1);

            readTextBtn = new Button { Text = "Read Text", Size = new Size(80, 28), Margin = new Padding(0, 0, 5, 0) };
            readTextBtn.Click += ReadTextBtn_Click;
            ocrRow1.Controls.Add(readTextBtn);

            readNumbersBtn = new Button { Text = "Numbers", Size = new Size(70, 28) };
            readNumbersBtn.Click += ReadNumbersBtn_Click;
            ocrRow1.Controls.Add(readNumbersBtn);

            var ocrRow2 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            ocrFlow.Controls.Add(ocrRow2);

            readFullScreenBtn = new Button { Text = "Full Screen", Size = new Size(85, 28) };
            readFullScreenBtn.Click += ReadFullScreenBtn_Click;
            ocrRow2.Controls.Add(readFullScreenBtn);

            // === GROUP 5: Fish Detection ===
            var fishGroup = new GroupBox { Text = "Fish Detection", Dock = DockStyle.Fill, Margin = new Padding(3, 0, 0, 0) };
            controlsTable.Controls.Add(fishGroup, 4, 0);

            var fishFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(5) };
            fishGroup.Controls.Add(fishFlow);

            fishLocationComboBox = new ComboBox { Size = new Size(160, 24), DropDownStyle = ComboBoxStyle.DropDownList, DropDownHeight = 250 };
            fishLocationComboBox.Items.AddRange(new object[]
            {
                "TTC Punchline Place",
                "DDL Lullaby Lane",
                "Brrrgh Polar Place",
                "Brrrgh Walrus Way",
                "Brrrgh Sleet Street",
                "MML Tenor Terrace",
                "DD Lighthouse Lane",
                "DG Elm Street",
                "Fish Anywhere"
            });
            fishLocationComboBox.SelectedIndex = 0;
            fishFlow.Controls.Add(fishLocationComboBox);

            var fishRow2 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            fishFlow.Controls.Add(fishRow2);

            findFishBtn = new Button { Text = "Find Fish", Size = new Size(75, 28), Margin = new Padding(0, 0, 5, 0) };
            findFishBtn.Click += FindFishBtn_Click;
            fishRow2.Controls.Add(findFishBtn);

            sampleColorBtn = new Button { Text = "Sample", Size = new Size(65, 28) };
            sampleColorBtn.Click += SampleColorBtn_Click;
            fishRow2.Controls.Add(sampleColorBtn);

            // === STATUS BAR ===
            coordsLabel = new Label
            {
                Text = "Coordinates: (-, -) | Selection: none",
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.LightGray,
                Padding = new Padding(5, 0, 0, 0)
            };
            this.Controls.Add(coordsLabel);
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

            string locationName = fishLocationComboBox.SelectedItem?.ToString() ?? "FISH ANYWHERE";
            Log($"=== Scanning for fish shadows at {locationName} ===");

            findFishBtn.Enabled = false;

            try
            {
                var config = GetFishingSpotConfig(locationName);
                if (config == null)
                {
                    Log("Unknown fishing location.");
                    return;
                }

                _fishBubbleColor = config.BubbleColor;

                // Calculate scale factors based on screenshot size vs reference
                const int ReferenceWidth = 1600;
                const int ReferenceHeight = 1151;
                float scaleX = (float)_currentScreenshot.Width / ReferenceWidth;
                float scaleY = (float)_currentScreenshot.Height / ReferenceHeight;

                // Scale the scan area
                _fishScanArea = new Rectangle(
                    (int)(config.ScanArea.X * scaleX),
                    (int)(config.ScanArea.Y * scaleY),
                    (int)(config.ScanArea.Width * scaleX),
                    (int)(config.ScanArea.Height * scaleY)
                );

                Log($"Scan area: ({_fishScanArea.X}, {_fishScanArea.Y}) - {_fishScanArea.Width}x{_fishScanArea.Height}");

                _fishBubbleResults.Clear();
                _shadowBlobs.Clear();
                var screenshotCopy = (Bitmap)_currentScreenshot.Clone();

                // ============ METHOD 1: Computer Vision Shadow Detection ============
                Log("");
                Log("--- METHOD 1: Shadow Detection (Computer Vision) ---");

                var shadowResult = await Task.Run(() =>
                {
                    return DetectFishShadowsWithVisualization(screenshotCopy, _fishScanArea);
                });

                if (shadowResult.BestShadow.HasValue)
                {
                    Log($"SHADOW DETECTED at ({shadowResult.BestShadow.Value.X}, {shadowResult.BestShadow.Value.Y})");
                    Log($"  Shadow color: RGB({shadowResult.BestShadowColor.R}, {shadowResult.BestShadowColor.G}, {shadowResult.BestShadowColor.B})");
                    Log($"  Avg brightness: {shadowResult.AvgBrightness}, Threshold: {shadowResult.DarkThreshold}");
                    Log($"  Dark pixels found: {shadowResult.DarkPixelCount}");
                    Log($"  Blobs found: {shadowResult.Blobs.Count} ({shadowResult.RejectedBlobCount} rejected as non-fish)");

                    _shadowBlobs = shadowResult.Blobs;

                    // Add shadow center to fish results for visualization
                    _fishBubbleResults.Add(shadowResult.BestShadow.Value);
                }
                else
                {
                    Log($"No fish shadows detected.");
                    Log($"  Avg brightness: {shadowResult.AvgBrightness}, Threshold: {shadowResult.DarkThreshold}");
                    Log($"  Dark pixels found: {shadowResult.DarkPixelCount}");
                    Log($"  Blobs found: {shadowResult.Blobs.Count} ({shadowResult.RejectedBlobCount} rejected as non-fish colors)");
                    if (shadowResult.RejectedBlobCount > 0)
                    {
                        Log($"  (Rejected blobs may be dock posts or other non-fish objects)");
                    }
                }

                // ============ METHOD 2: Color Matching (Fallback) ============
                Log("");
                Log("--- METHOD 2: Color Matching (Original) ---");
                Log($"Target color: RGB({config.BubbleColor.R}, {config.BubbleColor.G}, {config.BubbleColor.B})");
                Log($"Tolerance: R±{config.ColorTolerance.R}, G±{config.ColorTolerance.G}, B±{config.ColorTolerance.B}");

                var colorResults = await Task.Run(() =>
                {
                    var found = new List<Point>();
                    int scaledStep = Math.Max(1, (int)(5 * Math.Min(scaleX, scaleY)));

                    for (int y = _fishScanArea.Y; y < _fishScanArea.Y + _fishScanArea.Height; y += scaledStep)
                    {
                        for (int x = _fishScanArea.X; x < _fishScanArea.X + _fishScanArea.Width; x += scaledStep)
                        {
                            if (x >= 0 && x < screenshotCopy.Width && y >= 0 && y < screenshotCopy.Height)
                            {
                                var color = screenshotCopy.GetPixel(x, y);
                                if (IsMatchingColor(color, config.BubbleColor, config.ColorTolerance))
                                {
                                    found.Add(new Point(x, y));
                                }
                            }
                        }
                    }
                    return found;
                });

                screenshotCopy.Dispose();

                if (colorResults.Count > 0)
                {
                    Log($"Color matches found: {colorResults.Count} pixels");
                    var clusters = ClusterPoints(colorResults, 30);
                    Log($"Clustered into {clusters.Count} location(s)");

                    foreach (var cluster in clusters)
                    {
                        _fishBubbleResults.Add(cluster);
                        Log($"  - Color match at ({cluster.X}, {cluster.Y})");
                    }
                }
                else
                {
                    Log("No color matches found.");
                }

                // ============ Calculate Cast if we found anything ============
                if (_fishBubbleResults.Count > 0)
                {
                    var bestFish = _fishBubbleResults[0]; // Prefer shadow detection result

                    const int RodButtonX = 800;
                    const int RodButtonY = 846;
                    const int BubbleRefX = 800;
                    const int BubbleRefY = 820;

                    float refFishX = bestFish.X / scaleX + 20;
                    float refFishY = bestFish.Y / scaleY + 20 + config.YAdjustment;

                    double factorX = 120.0 / 429.0;
                    double factorY = 220.0 / 428.0;
                    double yAdjustment = 0.75 + ((double)(BubbleRefY - refFishY) / BubbleRefY) * 0.38;
                    int destX = (int)(RodButtonX + factorX * (BubbleRefX - refFishX) * yAdjustment);
                    int destY = (int)(RodButtonY + factorY * (BubbleRefY - refFishY));

                    destX = Math.Max(100, Math.Min(destX, ReferenceWidth - 100));
                    destY = Math.Max(RodButtonY - 200, Math.Min(destY, 1009));

                    _calculatedRodPosition = new Point((int)(RodButtonX * scaleX), (int)(RodButtonY * scaleY));
                    _calculatedCastPosition = new Point((int)(destX * scaleX), (int)(destY * scaleY));

                    Log("");
                    Log("=== CAST CALCULATION ===");
                    Log($"Best fish at: ({bestFish.X}, {bestFish.Y})");
                    Log($"Rod position: ({_calculatedRodPosition.X}, {_calculatedRodPosition.Y})");
                    Log($"Cast to: ({_calculatedCastPosition.X}, {_calculatedCastPosition.Y})");
                    Log("");
                    Log("LEGEND: GREEN = shadow blobs, YELLOW = color matches");
                    Log("        RED circle = rod button, CYAN line = cast direction");
                }
                else
                {
                    Log("");
                    Log("No fish found by either method.");
                    _calculatedRodPosition = Point.Empty;
                    _calculatedCastPosition = Point.Empty;
                }

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

        /// <summary>
        /// Shadow detection result for visualization
        /// </summary>
        private class ShadowDetectionResult
        {
            public Point? BestShadow { get; set; }
            public Color BestShadowColor { get; set; } = Color.Empty;
            public int AvgBrightness { get; set; }
            public int DarkThreshold { get; set; }
            public int DarkPixelCount { get; set; }
            public int RejectedBlobCount { get; set; }
            public List<List<Point>> Blobs { get; set; } = new List<List<Point>>();
        }

        private List<List<Point>> _shadowBlobs = new List<List<Point>>();

        /// <summary>
        /// Detects fish shadows and returns detailed results for visualization.
        /// Uses color validation to reject non-fish blobs (like dock posts).
        /// </summary>
        private ShadowDetectionResult DetectFishShadowsWithVisualization(Bitmap screenshot, Rectangle scanArea)
        {
            var result = new ShadowDetectionResult();
            const int step = 3;
            const int minBlobSize = 50;
            const int maxBlobSize = 2000;

            int startX = scanArea.X;
            int startY = scanArea.Y;
            int endX = Math.Min(screenshot.Width, scanArea.X + scanArea.Width);
            int endY = Math.Min(screenshot.Height, scanArea.Y + scanArea.Height);

            // Calculate average brightness
            long totalBrightness = 0;
            int pixelCount = 0;

            for (int y = startY; y < endY; y += step * 2)
            {
                for (int x = startX; x < endX; x += step * 2)
                {
                    if (x >= 0 && x < screenshot.Width && y >= 0 && y < screenshot.Height)
                    {
                        var color = screenshot.GetPixel(x, y);
                        totalBrightness += (color.R + color.G + color.B) / 3;
                        pixelCount++;
                    }
                }
            }

            if (pixelCount == 0) return result;

            result.AvgBrightness = (int)(totalBrightness / pixelCount);
            result.DarkThreshold = Math.Max(10, result.AvgBrightness - 25);

            // Find dark pixels
            var darkPixels = new List<Point>();
            for (int y = startY; y < endY; y += step)
            {
                for (int x = startX; x < endX; x += step)
                {
                    if (x >= 0 && x < screenshot.Width && y >= 0 && y < screenshot.Height)
                    {
                        var color = screenshot.GetPixel(x, y);
                        int brightness = (color.R + color.G + color.B) / 3;

                        if (brightness < result.DarkThreshold)
                        {
                            darkPixels.Add(new Point(x, y));
                        }
                    }
                }
            }

            result.DarkPixelCount = darkPixels.Count;

            if (darkPixels.Count < minBlobSize / (step * step))
                return result;

            // Find blobs
            result.Blobs = FindBlobsClustering(darkPixels, step * 3);

            // Find best blob (with color validation)
            Point scanCenter = new Point((startX + endX) / 2, (startY + endY) / 2);
            Point? bestBlob = null;
            Color bestBlobColor = Color.Empty;
            double bestScore = double.MaxValue;
            int rejectedCount = 0;

            foreach (var blob in result.Blobs)
            {
                int blobSize = blob.Count * step * step;

                if (blobSize < minBlobSize || blobSize > maxBlobSize)
                    continue;

                int sumX = 0, sumY = 0;
                foreach (var p in blob)
                {
                    sumX += p.X;
                    sumY += p.Y;
                }
                Point blobCenter = new Point(sumX / blob.Count, sumY / blob.Count);

                // Sample the color at blob center
                Color blobColor = screenshot.GetPixel(blobCenter.X, blobCenter.Y);

                // Validate: fish shadows are teal/cyan (more green+blue than red)
                // Reject brown/wood colors (dock posts) where R > G
                if (!IsFishShadowColor(blobColor))
                {
                    rejectedCount++;
                    continue;
                }

                double distance = Math.Sqrt(Math.Pow(blobCenter.X - scanCenter.X, 2) +
                                           Math.Pow(blobCenter.Y - scanCenter.Y, 2));
                double score = distance - blobSize * 0.1;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestBlob = blobCenter;
                    bestBlobColor = blobColor;
                }
            }

            result.BestShadow = bestBlob;
            result.BestShadowColor = bestBlobColor;
            result.RejectedBlobCount = rejectedCount;
            return result;
        }

        /// <summary>
        /// Checks if a color looks like a fish shadow (teal/cyan tones, not brown/wood).
        /// Fish shadows typically have more green and blue than red.
        /// This is a lenient check to avoid rejecting valid fish.
        /// </summary>
        private bool IsFishShadowColor(Color color)
        {
            int brightness = (color.R + color.G + color.B) / 3;

            // Must be somewhat dark (allow up to 180 for lighter shadows)
            if (brightness > 180)
                return false;

            // Reject obvious brown/wood (dock posts) - R significantly greater than G and B
            // Only reject if clearly brown (R > G + 20 AND R > B + 20)
            if (color.R > color.G + 20 && color.R > color.B + 20)
                return false;

            // Accept most other dark colors - fish shadows can vary
            return true;
        }

        /// <summary>
        /// Blob clustering for visualization
        /// </summary>
        private List<List<Point>> FindBlobsClustering(List<Point> points, int maxDistance)
        {
            var blobs = new List<List<Point>>();
            var visited = new HashSet<int>();

            for (int i = 0; i < points.Count; i++)
            {
                if (visited.Contains(i)) continue;

                var blob = new List<Point>();
                var queue = new Queue<int>();
                queue.Enqueue(i);
                visited.Add(i);

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    blob.Add(points[current]);

                    for (int j = 0; j < points.Count; j++)
                    {
                        if (visited.Contains(j)) continue;

                        double dist = Math.Sqrt(Math.Pow(points[current].X - points[j].X, 2) +
                                               Math.Pow(points[current].Y - points[j].Y, 2));
                        if (dist <= maxDistance)
                        {
                            queue.Enqueue(j);
                            visited.Add(j);
                        }
                    }
                }

                if (blob.Count > 0)
                    blobs.Add(blob);
            }

            return blobs;
        }

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

                    // Compare to expected color for selected location
                    string locationName = fishLocationComboBox.SelectedItem?.ToString() ?? "FISH ANYWHERE";
                    var config = GetFishingSpotConfig(locationName);
                    if (config != null)
                    {
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
                            Log("  ✓ This color MATCHES the expected fish bubble color!");
                        }
                        else
                        {
                            Log("  ✗ This color does NOT match the expected fish bubble color.");
                        }
                    }
                }
            }
            else
            {
                Log("Selection is outside the screenshot bounds.");
            }
        }

        private FishingSpotConfig GetFishingSpotConfig(string locationName)
        {
            // Fishing spot configurations - supports both short and full location names
            return locationName switch
            {
                "TTC Punchline Place" or "TOONTOWN CENTRAL PUNCHLINE PLACE" => new FishingSpotConfig(
                    new Rectangle(260, 196, 1089, 430),
                    Color.FromArgb(20, 123, 114),
                    new Tolerance(8, 8, 8),
                    15),
                "DDL Lullaby Lane" or "DONALD DREAM LAND LULLABY LANE" => new FishingSpotConfig(
                    new Rectangle(248, 239, 1244, 421),
                    Color.FromArgb(55, 103, 116),
                    new Tolerance(8, 14, 11),
                    0),
                "Brrrgh Polar Place" or "Brrrgh Walrus Way" or "Brrrgh Sleet Street" or
                "BRRRGH POLAR PLACE" or "BRRRGH WALRUS WAY" or "BRRRGH SLEET STREET" => new FishingSpotConfig(
                    new Rectangle(153, 134, 1297, 569),
                    Color.FromArgb(25, 144, 148),
                    new Tolerance(10, 11, 11),
                    10),
                "MML Tenor Terrace" or "MINNIE'S MELODYLAND TENOR TERRACE" => new FishingSpotConfig(
                    new Rectangle(200, 150, 1292, 510),
                    Color.FromArgb(56, 129, 122),
                    new Tolerance(10, 10, 10),
                    20),
                "DD Lighthouse Lane" or "DONALD DOCK LIGHTHOUSE LANE" => new FishingSpotConfig(
                    new Rectangle(200, 150, 1292, 510),
                    Color.FromArgb(22, 140, 118),
                    new Tolerance(13, 13, 15),
                    15),
                "DG Elm Street" or "DAISY'S GARDEN ELM STREET" => new FishingSpotConfig(
                    new Rectangle(200, 80, 1230, 712),
                    Color.FromArgb(17, 102, 75),
                    new Tolerance(5, 4, 5),
                    35),
                _ => new FishingSpotConfig(
                    new Rectangle(200, 150, 1292, 510),
                    Color.FromArgb(56, 129, 122),
                    new Tolerance(7, 5, 5),
                    35)
            };
        }

        private bool IsMatchingColor(Color actual, Color target, Tolerance tolerance)
        {
            return Math.Abs(actual.R - target.R) <= tolerance.R &&
                   Math.Abs(actual.G - target.G) <= tolerance.G &&
                   Math.Abs(actual.B - target.B) <= tolerance.B;
        }

        private List<Point> ClusterPoints(List<Point> points, int clusterRadius)
        {
            var clusters = new List<Point>();
            var used = new HashSet<int>();

            for (int i = 0; i < points.Count; i++)
            {
                if (used.Contains(i)) continue;

                var cluster = new List<Point> { points[i] };
                used.Add(i);

                for (int j = i + 1; j < points.Count; j++)
                {
                    if (used.Contains(j)) continue;

                    if (Math.Abs(points[i].X - points[j].X) <= clusterRadius &&
                        Math.Abs(points[i].Y - points[j].Y) <= clusterRadius)
                    {
                        cluster.Add(points[j]);
                        used.Add(j);
                    }
                }

                // Calculate cluster center
                int avgX = (int)cluster.Average(p => p.X);
                int avgY = (int)cluster.Average(p => p.Y);
                clusters.Add(new Point(avgX, avgY));
            }

            return clusters;
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
        private ComboBox templateDefinitionsComboBox;
        private Button testTemplateBtn;
        #endregion
    }
}
