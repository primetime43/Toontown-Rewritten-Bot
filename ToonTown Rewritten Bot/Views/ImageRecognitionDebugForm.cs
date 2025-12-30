using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        private static readonly string TemplatesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
        private CancellationTokenSource _searchCancellation;

        public ImageRecognitionDebugForm()
        {
            InitializeComponent();
            InitializeOCR();
            SetupRefreshTimer();
            EnsureTemplatesFolderExists();
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
            this.Size = new Size(1000, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(800, 600);

            // Main layout
            var mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 450
            };
            this.Controls.Add(mainSplit);

            // Top panel - Preview with template preview on the side
            var topSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 700
            };
            mainSplit.Panel1.Controls.Add(topSplit);

            // Left - Main preview
            var previewGroup = new GroupBox
            {
                Text = "Game Window Preview (Click and drag to select region)",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            topSplit.Panel1.Controls.Add(previewGroup);

            previewPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.DarkGray
            };
            previewPictureBox.MouseDown += PreviewPictureBox_MouseDown;
            previewPictureBox.MouseMove += PreviewPictureBox_MouseMove;
            previewPictureBox.MouseUp += PreviewPictureBox_MouseUp;
            previewPictureBox.Paint += PreviewPictureBox_Paint;
            previewGroup.Controls.Add(previewPictureBox);

            // Right - Template preview
            var templatePreviewGroup = new GroupBox
            {
                Text = "Current Template",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            topSplit.Panel2.Controls.Add(templatePreviewGroup);

            templatePreviewPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle
            };
            templatePreviewGroup.Controls.Add(templatePreviewPictureBox);

            // Bottom panel - Controls and Output
            var bottomSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 450
            };
            mainSplit.Panel2.Controls.Add(bottomSplit);

            // Left side - Controls
            var controlsPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            bottomSplit.Panel1.Controls.Add(controlsPanel);

            int yPos = 5;

            // Row 1: Capture and Create Template
            var captureGroup = new GroupBox
            {
                Text = "1. Capture",
                Location = new Point(5, yPos),
                Size = new Size(210, 55)
            };
            controlsPanel.Controls.Add(captureGroup);

            captureBtn = new Button
            {
                Text = "Capture",
                Location = new Point(10, 18),
                Size = new Size(65, 28)
            };
            captureBtn.Click += CaptureBtn_Click;
            captureGroup.Controls.Add(captureBtn);

            autoRefreshCheckBox = new CheckBox
            {
                Text = "Auto",
                Location = new Point(80, 22),
                AutoSize = true
            };
            autoRefreshCheckBox.CheckedChanged += AutoRefreshCheckBox_CheckedChanged;
            captureGroup.Controls.Add(autoRefreshCheckBox);

            refreshIntervalNumeric = new NumericUpDown
            {
                Location = new Point(130, 20),
                Size = new Size(50, 25),
                Minimum = 100,
                Maximum = 5000,
                Value = 500,
                Increment = 100
            };
            captureGroup.Controls.Add(refreshIntervalNumeric);

            var msLabel = new Label { Text = "ms", Location = new Point(182, 23), AutoSize = true };
            captureGroup.Controls.Add(msLabel);

            // Template Creation controls
            var createTemplateGroup = new GroupBox
            {
                Text = "2. Create Template (select region first)",
                Location = new Point(220, yPos),
                Size = new Size(220, 55)
            };
            controlsPanel.Controls.Add(createTemplateGroup);

            useSelectionBtn = new Button
            {
                Text = "Use Selection",
                Location = new Point(10, 18),
                Size = new Size(100, 28)
            };
            useSelectionBtn.Click += UseSelectionBtn_Click;
            createTemplateGroup.Controls.Add(useSelectionBtn);

            saveTemplateBtn = new Button
            {
                Text = "Save to File",
                Location = new Point(115, 18),
                Size = new Size(95, 28)
            };
            saveTemplateBtn.Click += SaveTemplateBtn_Click;
            createTemplateGroup.Controls.Add(saveTemplateBtn);

            yPos += 60;

            // Row 2: Template Matching controls
            var templateGroup = new GroupBox
            {
                Text = "3. Template Matching",
                Location = new Point(5, yPos),
                Size = new Size(435, 55)
            };
            controlsPanel.Controls.Add(templateGroup);

            loadTemplateBtn = new Button
            {
                Text = "Load",
                Location = new Point(10, 20),
                Size = new Size(55, 28)
            };
            loadTemplateBtn.Click += LoadTemplateBtn_Click;
            templateGroup.Controls.Add(loadTemplateBtn);

            templatePathLabel = new Label
            {
                Text = "No template loaded",
                Location = new Point(70, 25),
                Size = new Size(130, 20),
                AutoEllipsis = true
            };
            templateGroup.Controls.Add(templatePathLabel);

            findTemplateBtn = new Button
            {
                Text = "Find",
                Location = new Point(205, 20),
                Size = new Size(50, 28)
            };
            findTemplateBtn.Click += FindTemplateBtn_Click;
            templateGroup.Controls.Add(findTemplateBtn);

            findAllTemplatesBtn = new Button
            {
                Text = "Find All",
                Location = new Point(258, 20),
                Size = new Size(55, 28)
            };
            findAllTemplatesBtn.Click += FindAllTemplatesBtn_Click;
            templateGroup.Controls.Add(findAllTemplatesBtn);

            stopSearchBtn = new Button
            {
                Text = "Stop",
                Location = new Point(316, 20),
                Size = new Size(45, 28),
                Enabled = false,
                BackColor = Color.IndianRed,
                ForeColor = Color.White
            };
            stopSearchBtn.Click += StopSearchBtn_Click;
            templateGroup.Controls.Add(stopSearchBtn);

            thresholdNumeric = new NumericUpDown
            {
                Location = new Point(365, 20),
                Size = new Size(45, 25),
                Minimum = 50,
                Maximum = 100,
                Value = 85,
                DecimalPlaces = 0
            };
            templateGroup.Controls.Add(thresholdNumeric);

            var percentLabel = new Label { Text = "%", Location = new Point(412, 24), AutoSize = true };
            templateGroup.Controls.Add(percentLabel);

            yPos += 60;

            // Row 3: Template folder and clear button
            var templateFolderGroup = new GroupBox
            {
                Text = "Templates & Results",
                Location = new Point(5, yPos),
                Size = new Size(435, 55)
            };
            controlsPanel.Controls.Add(templateFolderGroup);

            openTemplatesFolderBtn = new Button
            {
                Text = "Open Templates Folder",
                Location = new Point(10, 20),
                Size = new Size(140, 28)
            };
            openTemplatesFolderBtn.Click += (s, e) => System.Diagnostics.Process.Start("explorer.exe", TemplatesFolder);
            templateFolderGroup.Controls.Add(openTemplatesFolderBtn);

            clearMatchesBtn = new Button
            {
                Text = "Clear Results",
                Location = new Point(160, 20),
                Size = new Size(100, 28)
            };
            clearMatchesBtn.Click += (s, e) => {
                _matchResults.Clear();
                _selectedRegion = Rectangle.Empty;
                previewPictureBox.Invalidate();
                Log("Cleared matches and selection.");
            };
            templateFolderGroup.Controls.Add(clearMatchesBtn);

            var folderHintLabel = new Label
            {
                Text = "Store templates for reuse",
                Location = new Point(270, 25),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            templateFolderGroup.Controls.Add(folderHintLabel);

            yPos += 60;

            // Row 4: OCR controls
            var ocrGroup = new GroupBox
            {
                Text = "4. OCR (Read Text/Numbers)",
                Location = new Point(5, yPos),
                Size = new Size(435, 55)
            };
            controlsPanel.Controls.Add(ocrGroup);

            readTextBtn = new Button
            {
                Text = "Read Text",
                Location = new Point(10, 20),
                Size = new Size(80, 28)
            };
            readTextBtn.Click += ReadTextBtn_Click;
            ocrGroup.Controls.Add(readTextBtn);

            readNumbersBtn = new Button
            {
                Text = "Read Numbers",
                Location = new Point(95, 20),
                Size = new Size(95, 28)
            };
            readNumbersBtn.Click += ReadNumbersBtn_Click;
            ocrGroup.Controls.Add(readNumbersBtn);

            readFullScreenBtn = new Button
            {
                Text = "Read Full Screen",
                Location = new Point(195, 20),
                Size = new Size(110, 28)
            };
            readFullScreenBtn.Click += ReadFullScreenBtn_Click;
            ocrGroup.Controls.Add(readFullScreenBtn);

            var ocrStatusLabel = new Label
            {
                Text = "(auto-downloads)",
                Location = new Point(310, 25),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            ocrGroup.Controls.Add(ocrStatusLabel);

            // Right side - Output
            var outputGroup = new GroupBox
            {
                Text = "Output / Results",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            bottomSplit.Panel2.Controls.Add(outputGroup);

            outputTextBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };
            outputGroup.Controls.Add(outputTextBox);

            var clearBtn = new Button
            {
                Text = "Clear Output",
                Dock = DockStyle.Bottom,
                Height = 28
            };
            clearBtn.Click += (s, e) => outputTextBox.Clear();
            outputGroup.Controls.Add(clearBtn);

            // Coordinate display label (status bar)
            coordsLabel = new Label
            {
                Text = "Coordinates: (-, -) | Selection: none",
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = SystemColors.Control,
                BorderStyle = BorderStyle.Fixed3D
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
        #endregion
    }
}
