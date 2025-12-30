using System;
using System.Drawing;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>
    /// Form for capturing a fish bubble color sample from the game.
    /// User selects a region containing the fish shadow/bubble to extract the color.
    /// </summary>
    public class FishBubbleColorCaptureForm : Form
    {
        private readonly string _locationName;

        private PictureBox _previewPictureBox;
        private PictureBox _colorPreviewBox;
        private Button _captureBtn;
        private Button _saveBtn;
        private Button _cancelBtn;
        private Label _instructionLabel;
        private Label _statusLabel;
        private Label _colorInfoLabel;

        private Bitmap _currentScreenshot;
        private Rectangle _selectedRegion;
        private bool _isSelectingRegion;
        private Point _selectionStart;

        private Color _capturedColor;
        private Tolerance _capturedTolerance;

        // Default scan area (reference coordinates 1600x1151)
        private Rectangle _defaultScanArea = new Rectangle(200, 150, 1200, 500);

        /// <summary>
        /// Gets whether a color was successfully captured.
        /// </summary>
        public bool ColorCaptured { get; private set; }

        public FishBubbleColorCaptureForm(string locationName)
        {
            _locationName = locationName;
            InitializeComponent();

            this.TopMost = true;
            this.Load += (s, e) =>
            {
                this.BringToFront();
                this.Activate();
                this.BeginInvoke(new Action(() => this.TopMost = false));
            };
        }

        private void InitializeComponent()
        {
            this.Text = $"Capture Fish Bubble Color: {_locationName}";
            this.Size = new Size(950, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(750, 550);

            // Main layout
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 2,
                Padding = new Padding(10)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Instructions
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Preview
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            this.Controls.Add(mainPanel);

            // Instructions
            _instructionLabel = new Label
            {
                Text = $"Capture fish bubble color for: {_locationName}\n\n" +
                       "1. Make sure a fish shadow/bubble is visible in the game\n" +
                       "2. Click 'Capture' to take a screenshot\n" +
                       "3. Click and drag to select ONLY the fish shadow (darker area)\n" +
                       "4. The color will be extracted from your selection\n" +
                       "5. Click 'Save Color' when the preview looks correct",
                AutoSize = true,
                Padding = new Padding(5),
                Font = new Font(Font.FontFamily, 10)
            };
            mainPanel.Controls.Add(_instructionLabel, 0, 0);
            mainPanel.SetColumnSpan(_instructionLabel, 2);

            // Game preview
            var previewGroup = new GroupBox
            {
                Text = "Game Window - Select the fish shadow/bubble",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            mainPanel.Controls.Add(previewGroup, 0, 1);

            _previewPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.DarkGray
            };
            _previewPictureBox.MouseDown += PreviewPictureBox_MouseDown;
            _previewPictureBox.MouseMove += PreviewPictureBox_MouseMove;
            _previewPictureBox.MouseUp += PreviewPictureBox_MouseUp;
            _previewPictureBox.Paint += PreviewPictureBox_Paint;
            previewGroup.Controls.Add(_previewPictureBox);

            // Color preview panel
            var colorPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };
            colorPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            colorPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            colorPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            mainPanel.Controls.Add(colorPanel, 1, 1);

            // Color preview box
            var colorGroup = new GroupBox
            {
                Text = "Captured Color",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            colorPanel.Controls.Add(colorGroup, 0, 0);

            _colorPreviewBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle
            };
            colorGroup.Controls.Add(_colorPreviewBox);

            // Color info
            var infoGroup = new GroupBox
            {
                Text = "Color Information",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            colorPanel.Controls.Add(infoGroup, 0, 1);

            _colorInfoLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "No color captured yet.\n\nSelect a fish shadow region\nto capture its color.",
                Font = new Font(Font.FontFamily, 9),
                TextAlign = ContentAlignment.TopLeft
            };
            infoGroup.Controls.Add(_colorInfoLabel);

            // Buttons panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5)
            };
            mainPanel.Controls.Add(buttonPanel, 0, 2);
            mainPanel.SetColumnSpan(buttonPanel, 2);

            _captureBtn = new Button
            {
                Text = "Capture Game Window",
                Size = new Size(150, 35),
                Margin = new Padding(5)
            };
            _captureBtn.Click += CaptureBtn_Click;
            buttonPanel.Controls.Add(_captureBtn);

            _saveBtn = new Button
            {
                Text = "Save Color",
                Size = new Size(100, 35),
                Margin = new Padding(5),
                Enabled = false
            };
            _saveBtn.Click += SaveBtn_Click;
            buttonPanel.Controls.Add(_saveBtn);

            _statusLabel = new Label
            {
                Text = "Click 'Capture Game Window' to begin",
                AutoSize = true,
                Margin = new Padding(10, 12, 5, 5),
                ForeColor = Color.Gray
            };
            buttonPanel.Controls.Add(_statusLabel);

            // Spacer
            buttonPanel.Controls.Add(new Panel { Width = 150 });

            _cancelBtn = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 35),
                Margin = new Padding(5)
            };
            _cancelBtn.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            buttonPanel.Controls.Add(_cancelBtn);
        }

        private void CaptureBtn_Click(object sender, EventArgs e)
        {
            try
            {
                _currentScreenshot?.Dispose();
                _currentScreenshot = (Bitmap)ImageRecognition.GetWindowScreenshot();
                _previewPictureBox.Image = _currentScreenshot;
                _selectedRegion = Rectangle.Empty;
                _colorPreviewBox.BackColor = Color.LightGray;
                _saveBtn.Enabled = false;
                _statusLabel.Text = "Screenshot captured. Select a fish shadow.";
                _statusLabel.ForeColor = Color.Black;
                _colorInfoLabel.Text = "Select a region containing\nthe fish shadow/bubble.";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error: {ex.Message}";
                _statusLabel.ForeColor = Color.Red;
                MessageBox.Show(
                    "Could not capture game window. Make sure Toontown Rewritten is running.",
                    "Capture Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            if (_capturedColor.IsEmpty || _capturedColor == Color.LightGray)
            {
                MessageBox.Show("Please capture a screenshot and select a fish shadow region first.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Save the color config
                FishBubbleColorManager.Instance.SaveColorConfig(
                    _locationName,
                    _capturedColor,
                    _capturedTolerance,
                    _defaultScanArea,
                    15 // Default Y adjustment
                );

                ColorCaptured = true;
                _statusLabel.Text = "Color saved successfully!";
                _statusLabel.ForeColor = Color.Green;

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving color: {ex.Message}",
                    "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region Mouse Events

        private void PreviewPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _currentScreenshot != null)
            {
                _isSelectingRegion = true;
                _selectionStart = e.Location;
                _selectedRegion = new Rectangle(e.Location, Size.Empty);
            }
        }

        private void PreviewPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelectingRegion)
            {
                int x = Math.Min(_selectionStart.X, e.X);
                int y = Math.Min(_selectionStart.Y, e.Y);
                int width = Math.Abs(e.X - _selectionStart.X);
                int height = Math.Abs(e.Y - _selectionStart.Y);
                _selectedRegion = new Rectangle(x, y, width, height);
                _previewPictureBox.Invalidate();
            }
        }

        private void PreviewPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isSelectingRegion)
            {
                _isSelectingRegion = false;

                if (_selectedRegion.Width > 3 && _selectedRegion.Height > 3)
                {
                    ExtractColorFromSelection();
                }
            }
        }

        private void PreviewPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (!_selectedRegion.IsEmpty && _selectedRegion.Width > 0 && _selectedRegion.Height > 0)
            {
                using (var pen = new Pen(Color.Red, 2))
                using (var brush = new SolidBrush(Color.FromArgb(30, Color.Red)))
                {
                    e.Graphics.FillRectangle(brush, _selectedRegion);
                    e.Graphics.DrawRectangle(pen, _selectedRegion);
                }
            }
        }

        #endregion

        #region Color Extraction

        private void ExtractColorFromSelection()
        {
            if (_currentScreenshot == null || _selectedRegion.IsEmpty)
                return;

            var actualRegion = ConvertToImageCoordinates(_selectedRegion);

            if (actualRegion.Width <= 0 || actualRegion.Height <= 0)
                return;

            // Clamp to image bounds
            actualRegion = Rectangle.Intersect(actualRegion,
                new Rectangle(0, 0, _currentScreenshot.Width, _currentScreenshot.Height));

            if (actualRegion.Width <= 0 || actualRegion.Height <= 0)
                return;

            try
            {
                // Calculate average color and variance from the selected region
                long totalR = 0, totalG = 0, totalB = 0;
                int minR = 255, minG = 255, minB = 255;
                int maxR = 0, maxG = 0, maxB = 0;
                int pixelCount = 0;

                for (int y = actualRegion.Y; y < actualRegion.Y + actualRegion.Height; y++)
                {
                    for (int x = actualRegion.X; x < actualRegion.X + actualRegion.Width; x++)
                    {
                        Color pixel = _currentScreenshot.GetPixel(x, y);
                        totalR += pixel.R;
                        totalG += pixel.G;
                        totalB += pixel.B;

                        minR = Math.Min(minR, pixel.R);
                        minG = Math.Min(minG, pixel.G);
                        minB = Math.Min(minB, pixel.B);

                        maxR = Math.Max(maxR, pixel.R);
                        maxG = Math.Max(maxG, pixel.G);
                        maxB = Math.Max(maxB, pixel.B);

                        pixelCount++;
                    }
                }

                if (pixelCount > 0)
                {
                    // Calculate average color
                    int avgR = (int)(totalR / pixelCount);
                    int avgG = (int)(totalG / pixelCount);
                    int avgB = (int)(totalB / pixelCount);
                    _capturedColor = Color.FromArgb(avgR, avgG, avgB);

                    // Calculate tolerance based on color range in selection, with minimum values
                    int tolR = Math.Max(10, (maxR - minR) / 2 + 5);
                    int tolG = Math.Max(10, (maxG - minG) / 2 + 5);
                    int tolB = Math.Max(10, (maxB - minB) / 2 + 5);
                    _capturedTolerance = new Tolerance(tolR, tolG, tolB);

                    // Update UI
                    _colorPreviewBox.BackColor = _capturedColor;
                    _colorInfoLabel.Text = $"Average Color:\n" +
                        $"  R: {avgR}\n" +
                        $"  G: {avgG}\n" +
                        $"  B: {avgB}\n\n" +
                        $"Tolerance:\n" +
                        $"  R: +/-{tolR}\n" +
                        $"  G: +/-{tolG}\n" +
                        $"  B: +/-{tolB}\n\n" +
                        $"Pixels sampled: {pixelCount}";

                    _saveBtn.Enabled = true;
                    _statusLabel.Text = $"Color captured! Click 'Save Color' to confirm.";
                    _statusLabel.ForeColor = Color.DarkGreen;
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error extracting color: {ex.Message}";
                _statusLabel.ForeColor = Color.Red;
            }
        }

        private Rectangle ConvertToImageCoordinates(Rectangle previewRect)
        {
            if (_currentScreenshot == null || _previewPictureBox.Image == null)
                return previewRect;

            float imageAspect = (float)_currentScreenshot.Width / _currentScreenshot.Height;
            float boxAspect = (float)_previewPictureBox.Width / _previewPictureBox.Height;

            float scale;
            int offsetX = 0, offsetY = 0;

            if (imageAspect > boxAspect)
            {
                scale = (float)_previewPictureBox.Width / _currentScreenshot.Width;
                offsetY = (int)((_previewPictureBox.Height - _currentScreenshot.Height * scale) / 2);
            }
            else
            {
                scale = (float)_previewPictureBox.Height / _currentScreenshot.Height;
                offsetX = (int)((_previewPictureBox.Width - _currentScreenshot.Width * scale) / 2);
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

        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _currentScreenshot?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
