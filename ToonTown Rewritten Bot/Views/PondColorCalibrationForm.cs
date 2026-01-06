using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>
    /// Form that allows users to calibrate pond water color and fish shadow color
    /// by clicking on the game screenshot.
    /// </summary>
    public class PondColorCalibrationForm : Form
    {
        private string _locationName;
        private Bitmap _screenshot;
        private Rectangle _gameWindowRect;

        private Color? _waterColor = null;
        private Color? _shadowColor = null;
        private Point? _waterClickPoint = null;
        private Point? _shadowClickPoint = null;

        private CalibrationStep _currentStep = CalibrationStep.SampleWater;

        private Panel _colorPreviewPanel;
        private Label _waterColorLabel;
        private Label _shadowColorLabel;
        private Panel _waterColorPreview;
        private Panel _shadowColorPreview;
        private Label _instructionsLabel;
        private Button _saveButton;
        private Button _cancelButton;
        private Button _resetButton;
        private PictureBox _screenshotBox;
        private NumericUpDown _toleranceNumeric;

        public bool WasSaved { get; private set; } = false;

        private enum CalibrationStep
        {
            SampleWater,
            SampleShadow,
            Ready
        }

        public PondColorCalibrationForm(string locationName)
        {
            _locationName = locationName;
            _gameWindowRect = CoreFunctionality.GetGameWindowRect();

            if (_gameWindowRect.IsEmpty)
            {
                MessageBox.Show("Game window not found. Please make sure Toontown is running.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Load += (s, e) => Close();
                return;
            }

            // Capture screenshot
            try
            {
                _screenshot = ImageRecognition.GetWindowScreenshot() as Bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to capture screenshot: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Load += (s, e) => Close();
                return;
            }

            // Load existing colors if any
            var existing = PondColorManager.GetPondColors(locationName);
            if (existing != null)
            {
                _waterColor = existing.WaterColor;
                _shadowColor = existing.ShadowColor;
                _currentStep = CalibrationStep.Ready;
            }

            InitializeForm();
        }

        private void InitializeForm()
        {
            this.Text = $"Pond Color Calibration - {_locationName}";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);

            // Instructions panel at top
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(10)
            };
            this.Controls.Add(topPanel);

            _instructionsLabel = new Label
            {
                Text = GetInstructionText(),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            topPanel.Controls.Add(_instructionsLabel);

            // Screenshot display
            _screenshotBox = new PictureBox
            {
                Location = new Point(10, 90),
                Size = new Size(600, 450),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = _screenshot,
                Cursor = Cursors.Cross
            };
            _screenshotBox.MouseClick += ScreenshotBox_MouseClick;
            _screenshotBox.Paint += ScreenshotBox_Paint;
            this.Controls.Add(_screenshotBox);

            // Right panel for color previews
            _colorPreviewPanel = new Panel
            {
                Location = new Point(620, 90),
                Size = new Size(260, 450),
                BackColor = Color.FromArgb(45, 45, 48)
            };
            this.Controls.Add(_colorPreviewPanel);

            // Water color section
            var waterLabel = new Label
            {
                Text = "1. POND WATER COLOR",
                Location = new Point(10, 10),
                Size = new Size(240, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.LightBlue
            };
            _colorPreviewPanel.Controls.Add(waterLabel);

            _waterColorPreview = new Panel
            {
                Location = new Point(10, 35),
                Size = new Size(100, 60),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = _waterColor ?? Color.Gray
            };
            _colorPreviewPanel.Controls.Add(_waterColorPreview);

            _waterColorLabel = new Label
            {
                Text = _waterColor.HasValue ? $"RGB({_waterColor.Value.R}, {_waterColor.Value.G}, {_waterColor.Value.B})" : "Not set - click on water",
                Location = new Point(120, 35),
                Size = new Size(130, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8)
            };
            _colorPreviewPanel.Controls.Add(_waterColorLabel);

            // Shadow color section
            var shadowLabel = new Label
            {
                Text = "2. FISH SHADOW COLOR",
                Location = new Point(10, 110),
                Size = new Size(240, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Orange
            };
            _colorPreviewPanel.Controls.Add(shadowLabel);

            _shadowColorPreview = new Panel
            {
                Location = new Point(10, 135),
                Size = new Size(100, 60),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = _shadowColor ?? Color.Gray
            };
            _colorPreviewPanel.Controls.Add(_shadowColorPreview);

            _shadowColorLabel = new Label
            {
                Text = _shadowColor.HasValue ? $"RGB({_shadowColor.Value.R}, {_shadowColor.Value.G}, {_shadowColor.Value.B})" : "Not set - click on fish shadow",
                Location = new Point(120, 135),
                Size = new Size(130, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8)
            };
            _colorPreviewPanel.Controls.Add(_shadowColorLabel);

            // Color difference info
            var diffLabel = new Label
            {
                Text = "COLOR DIFFERENCE",
                Location = new Point(10, 210),
                Size = new Size(240, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.LightGreen
            };
            _colorPreviewPanel.Controls.Add(diffLabel);

            var diffInfoLabel = new Label
            {
                Name = "diffInfoLabel",
                Text = GetColorDifferenceText(),
                Location = new Point(10, 235),
                Size = new Size(240, 60),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 8)
            };
            _colorPreviewPanel.Controls.Add(diffInfoLabel);

            // Tolerance setting
            var toleranceLabel = new Label
            {
                Text = "Detection Tolerance:",
                Location = new Point(10, 310),
                Size = new Size(130, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            _colorPreviewPanel.Controls.Add(toleranceLabel);

            _toleranceNumeric = new NumericUpDown
            {
                Location = new Point(140, 308),
                Size = new Size(60, 25),
                Minimum = 5,
                Maximum = 50,
                Value = 15,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            _colorPreviewPanel.Controls.Add(_toleranceNumeric);

            // Tips
            var tipsLabel = new Label
            {
                Text = "Tips:\n" +
                       "• Click on clear pond water (not near edges)\n" +
                       "• Click on the dark part of a fish shadow\n" +
                       "• Shadow should be darker than water\n" +
                       "• Higher tolerance = more detections",
                Location = new Point(10, 350),
                Size = new Size(240, 90),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8)
            };
            _colorPreviewPanel.Controls.Add(tipsLabel);

            // Bottom buttons
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            this.Controls.Add(bottomPanel);

            _resetButton = new Button
            {
                Text = "Reset",
                Size = new Size(80, 30),
                Location = new Point(10, 10),
                BackColor = Color.DarkOrange,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _resetButton.Click += ResetButton_Click;
            bottomPanel.Controls.Add(_resetButton);

            _saveButton = new Button
            {
                Text = "Save Colors",
                Size = new Size(100, 30),
                Location = new Point(this.Width - 230, 10),
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = _waterColor.HasValue && _shadowColor.HasValue
            };
            _saveButton.Click += SaveButton_Click;
            bottomPanel.Controls.Add(_saveButton);

            _cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(80, 30),
                Location = new Point(this.Width - 110, 10),
                BackColor = Color.DarkRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _cancelButton.Click += (s, e) => Close();
            bottomPanel.Controls.Add(_cancelButton);
        }

        private string GetInstructionText()
        {
            return _currentStep switch
            {
                CalibrationStep.SampleWater => "Step 1: Click on the POND WATER (the normal water color, not a shadow)",
                CalibrationStep.SampleShadow => "Step 2: Click on a FISH SHADOW (the darker area where a fish is)",
                CalibrationStep.Ready => "Colors calibrated! Click Save to apply, or click to re-sample colors.",
                _ => ""
            };
        }

        private string GetColorDifferenceText()
        {
            if (!_waterColor.HasValue || !_shadowColor.HasValue)
                return "Sample both colors to see difference";

            int diffR = _waterColor.Value.R - _shadowColor.Value.R;
            int diffG = _waterColor.Value.G - _shadowColor.Value.G;
            int diffB = _waterColor.Value.B - _shadowColor.Value.B;

            string quality;
            int totalDiff = Math.Abs(diffR) + Math.Abs(diffG) + Math.Abs(diffB);
            if (totalDiff > 60) quality = "Excellent contrast!";
            else if (totalDiff > 30) quality = "Good contrast";
            else if (totalDiff > 15) quality = "Low contrast - may need higher tolerance";
            else quality = "Very low contrast - detection may be difficult";

            return $"R: {diffR:+#;-#;0}, G: {diffG:+#;-#;0}, B: {diffB:+#;-#;0}\n" +
                   $"Total: {totalDiff}\n{quality}";
        }

        private void ScreenshotBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (_screenshot == null) return;

            // Convert click position to screenshot coordinates
            float scaleX = (float)_screenshot.Width / _screenshotBox.Width;
            float scaleY = (float)_screenshot.Height / _screenshotBox.Height;

            // Account for PictureBox zoom mode
            float imageRatio = (float)_screenshot.Width / _screenshot.Height;
            float boxRatio = (float)_screenshotBox.Width / _screenshotBox.Height;

            int actualWidth, actualHeight, offsetX, offsetY;

            if (imageRatio > boxRatio)
            {
                // Image is wider - letterboxing on top/bottom
                actualWidth = _screenshotBox.Width;
                actualHeight = (int)(_screenshotBox.Width / imageRatio);
                offsetX = 0;
                offsetY = (_screenshotBox.Height - actualHeight) / 2;
            }
            else
            {
                // Image is taller - letterboxing on sides
                actualHeight = _screenshotBox.Height;
                actualWidth = (int)(_screenshotBox.Height * imageRatio);
                offsetX = (_screenshotBox.Width - actualWidth) / 2;
                offsetY = 0;
            }

            // Check if click is within the image bounds
            if (e.X < offsetX || e.X > offsetX + actualWidth ||
                e.Y < offsetY || e.Y > offsetY + actualHeight)
                return;

            int imgX = (int)((e.X - offsetX) * _screenshot.Width / actualWidth);
            int imgY = (int)((e.Y - offsetY) * _screenshot.Height / actualHeight);

            // Clamp to valid range
            imgX = Math.Max(0, Math.Min(_screenshot.Width - 1, imgX));
            imgY = Math.Max(0, Math.Min(_screenshot.Height - 1, imgY));

            // Sample a small area and average the colors for better accuracy
            Color sampledColor = SampleAreaColor(imgX, imgY, 3);

            // Store based on current step or allow re-sampling
            if (_currentStep == CalibrationStep.SampleWater || !_waterColor.HasValue ||
                (Control.ModifierKeys == Keys.Shift))
            {
                // Sample water (or Shift+click to re-sample water)
                _waterColor = sampledColor;
                _waterClickPoint = new Point(imgX, imgY);
                _waterColorPreview.BackColor = sampledColor;
                _waterColorLabel.Text = $"RGB({sampledColor.R}, {sampledColor.G}, {sampledColor.B})";

                if (_currentStep == CalibrationStep.SampleWater)
                    _currentStep = CalibrationStep.SampleShadow;
            }
            else
            {
                // Sample shadow
                _shadowColor = sampledColor;
                _shadowClickPoint = new Point(imgX, imgY);
                _shadowColorPreview.BackColor = sampledColor;
                _shadowColorLabel.Text = $"RGB({sampledColor.R}, {sampledColor.G}, {sampledColor.B})";
                _currentStep = CalibrationStep.Ready;
            }

            // Update UI
            _instructionsLabel.Text = GetInstructionText();
            _saveButton.Enabled = _waterColor.HasValue && _shadowColor.HasValue;

            // Update difference label
            var diffLabel = _colorPreviewPanel.Controls["diffInfoLabel"] as Label;
            if (diffLabel != null)
                diffLabel.Text = GetColorDifferenceText();

            _screenshotBox.Invalidate();
        }

        private Color SampleAreaColor(int centerX, int centerY, int radius)
        {
            int totalR = 0, totalG = 0, totalB = 0, count = 0;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x >= 0 && x < _screenshot.Width && y >= 0 && y < _screenshot.Height)
                    {
                        var pixel = _screenshot.GetPixel(x, y);
                        totalR += pixel.R;
                        totalG += pixel.G;
                        totalB += pixel.B;
                        count++;
                    }
                }
            }

            if (count == 0) return Color.Gray;

            return Color.FromArgb(totalR / count, totalG / count, totalB / count);
        }

        private void ScreenshotBox_Paint(object sender, PaintEventArgs e)
        {
            if (_screenshot == null) return;

            // Calculate image display bounds (same as in MouseClick)
            float imageRatio = (float)_screenshot.Width / _screenshot.Height;
            float boxRatio = (float)_screenshotBox.Width / _screenshotBox.Height;

            int actualWidth, actualHeight, offsetX, offsetY;

            if (imageRatio > boxRatio)
            {
                actualWidth = _screenshotBox.Width;
                actualHeight = (int)(_screenshotBox.Width / imageRatio);
                offsetX = 0;
                offsetY = (_screenshotBox.Height - actualHeight) / 2;
            }
            else
            {
                actualHeight = _screenshotBox.Height;
                actualWidth = (int)(_screenshotBox.Height * imageRatio);
                offsetX = (_screenshotBox.Width - actualWidth) / 2;
                offsetY = 0;
            }

            // Draw markers for sampled points
            if (_waterClickPoint.HasValue)
            {
                int dispX = offsetX + (int)(_waterClickPoint.Value.X * actualWidth / _screenshot.Width);
                int dispY = offsetY + (int)(_waterClickPoint.Value.Y * actualHeight / _screenshot.Height);

                using (var pen = new Pen(Color.Cyan, 2))
                {
                    e.Graphics.DrawEllipse(pen, dispX - 10, dispY - 10, 20, 20);
                    e.Graphics.DrawLine(pen, dispX - 15, dispY, dispX + 15, dispY);
                    e.Graphics.DrawLine(pen, dispX, dispY - 15, dispX, dispY + 15);
                }

                using (var font = new Font("Arial", 8, FontStyle.Bold))
                {
                    e.Graphics.DrawString("WATER", font, Brushes.Cyan, dispX + 12, dispY - 8);
                }
            }

            if (_shadowClickPoint.HasValue)
            {
                int dispX = offsetX + (int)(_shadowClickPoint.Value.X * actualWidth / _screenshot.Width);
                int dispY = offsetY + (int)(_shadowClickPoint.Value.Y * actualHeight / _screenshot.Height);

                using (var pen = new Pen(Color.Orange, 2))
                {
                    e.Graphics.DrawEllipse(pen, dispX - 10, dispY - 10, 20, 20);
                    e.Graphics.DrawLine(pen, dispX - 15, dispY, dispX + 15, dispY);
                    e.Graphics.DrawLine(pen, dispX, dispY - 15, dispX, dispY + 15);
                }

                using (var font = new Font("Arial", 8, FontStyle.Bold))
                {
                    e.Graphics.DrawString("SHADOW", font, Brushes.Orange, dispX + 12, dispY - 8);
                }
            }
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            _waterColor = null;
            _shadowColor = null;
            _waterClickPoint = null;
            _shadowClickPoint = null;
            _currentStep = CalibrationStep.SampleWater;

            _waterColorPreview.BackColor = Color.Gray;
            _shadowColorPreview.BackColor = Color.Gray;
            _waterColorLabel.Text = "Not set - click on water";
            _shadowColorLabel.Text = "Not set - click on fish shadow";
            _instructionsLabel.Text = GetInstructionText();
            _saveButton.Enabled = false;

            var diffLabel = _colorPreviewPanel.Controls["diffInfoLabel"] as Label;
            if (diffLabel != null)
                diffLabel.Text = GetColorDifferenceText();

            _screenshotBox.Invalidate();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (!_waterColor.HasValue || !_shadowColor.HasValue)
            {
                MessageBox.Show("Please sample both water and shadow colors first.",
                    "Incomplete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int tolerance = (int)_toleranceNumeric.Value;
            PondColorManager.SetPondColors(_locationName, _waterColor.Value, _shadowColor.Value,
                tolerance, tolerance, tolerance);

            WasSaved = true;
            MessageBox.Show($"Colors saved for '{_locationName}'!\n\n" +
                $"Water: RGB({_waterColor.Value.R}, {_waterColor.Value.G}, {_waterColor.Value.B})\n" +
                $"Shadow: RGB({_shadowColor.Value.R}, {_shadowColor.Value.G}, {_shadowColor.Value.B})\n" +
                $"Tolerance: {tolerance}",
                "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _screenshot?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
