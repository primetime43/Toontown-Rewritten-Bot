using System;
using System.Drawing;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>
    /// Form for capturing a template image for a specific UI element.
    /// Shows the game window and lets the user select the region to capture.
    /// </summary>
    public class TemplateCaptureForm : Form
    {
        private readonly string _elementName;
        private readonly string _description;

        private PictureBox _previewPictureBox;
        private PictureBox _selectionPreviewPictureBox;
        private Button _captureBtn;
        private Button _saveBtn;
        private Button _cancelBtn;
        private Label _instructionLabel;
        private Label _statusLabel;

        private Bitmap _currentScreenshot;
        private Rectangle _selectedRegion;
        private bool _isSelectingRegion;
        private Point _selectionStart;

        /// <summary>
        /// Gets whether a template was successfully captured.
        /// </summary>
        public bool TemplateCaptured { get; private set; }

        /// <summary>
        /// Gets the captured template image.
        /// </summary>
        public Bitmap CapturedTemplate { get; private set; }

        /// <summary>
        /// Gets the location where the template was found/captured.
        /// </summary>
        public Point CapturedLocation { get; private set; }

        public TemplateCaptureForm(string elementName, string description = null)
        {
            _elementName = elementName;
            _description = description ?? $"Capture template for: {elementName}";
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = $"Capture Template: {_elementName}";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(700, 500);

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
                Text = $"{_description}\n\n" +
                       "1. Click 'Capture' to take a screenshot of the game window\n" +
                       "2. Click and drag on the preview to select the UI element\n" +
                       "3. Click 'Save Template' to save",
                AutoSize = true,
                Padding = new Padding(5),
                Font = new Font(Font.FontFamily, 10)
            };
            mainPanel.Controls.Add(_instructionLabel, 0, 0);
            mainPanel.SetColumnSpan(_instructionLabel, 2);

            // Game preview
            var previewGroup = new GroupBox
            {
                Text = "Game Window (click and drag to select)",
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

            // Selection preview
            var selectionGroup = new GroupBox
            {
                Text = "Selected Region",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            mainPanel.Controls.Add(selectionGroup, 1, 1);

            _selectionPreviewPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle
            };
            selectionGroup.Controls.Add(_selectionPreviewPictureBox);

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
                Text = "Save Template",
                Size = new Size(120, 35),
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

            // Add spacer
            buttonPanel.Controls.Add(new Panel { Width = 200 });

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
                _selectionPreviewPictureBox.Image = null;
                _saveBtn.Enabled = false;
                _statusLabel.Text = "Screenshot captured. Select the UI element.";
                _statusLabel.ForeColor = Color.Black;
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
            if (_currentScreenshot == null || _selectedRegion.IsEmpty)
            {
                MessageBox.Show("Please capture a screenshot and select a region first.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var actualRegion = ConvertToImageCoordinates(_selectedRegion);

            if (actualRegion.Width < 10 || actualRegion.Height < 10)
            {
                MessageBox.Show("Selection is too small. Please select a larger region.",
                    "Selection Too Small", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Create the template
                CapturedTemplate = _currentScreenshot.Clone(actualRegion, _currentScreenshot.PixelFormat);
                CapturedLocation = new Point(
                    actualRegion.X + actualRegion.Width / 2,
                    actualRegion.Y + actualRegion.Height / 2);

                // Save to UIElementManager
                UIElementManager.Instance.SaveTemplate(_elementName, CapturedTemplate);

                TemplateCaptured = true;
                _statusLabel.Text = $"Template saved! Size: {CapturedTemplate.Width}x{CapturedTemplate.Height}";
                _statusLabel.ForeColor = Color.Green;

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving template: {ex.Message}",
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

                if (_selectedRegion.Width > 5 && _selectedRegion.Height > 5)
                {
                    UpdateSelectionPreview();
                    _saveBtn.Enabled = true;
                    var actualRegion = ConvertToImageCoordinates(_selectedRegion);
                    _statusLabel.Text = $"Selected: {actualRegion.Width}x{actualRegion.Height} at ({actualRegion.X}, {actualRegion.Y})";
                    _statusLabel.ForeColor = Color.Black;
                }
            }
        }

        private void PreviewPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (!_selectedRegion.IsEmpty && _selectedRegion.Width > 0 && _selectedRegion.Height > 0)
            {
                using (var pen = new Pen(Color.Blue, 2))
                using (var brush = new SolidBrush(Color.FromArgb(30, Color.Blue)))
                {
                    e.Graphics.FillRectangle(brush, _selectedRegion);
                    e.Graphics.DrawRectangle(pen, _selectedRegion);
                }
            }
        }

        #endregion

        #region Helper Methods

        private void UpdateSelectionPreview()
        {
            if (_currentScreenshot == null || _selectedRegion.IsEmpty)
                return;

            var actualRegion = ConvertToImageCoordinates(_selectedRegion);

            if (actualRegion.Width <= 0 || actualRegion.Height <= 0)
                return;

            try
            {
                _selectionPreviewPictureBox.Image?.Dispose();
                _selectionPreviewPictureBox.Image = _currentScreenshot.Clone(actualRegion, _currentScreenshot.PixelFormat);
            }
            catch { }
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
            if (!TemplateCaptured)
            {
                CapturedTemplate?.Dispose();
            }
            base.OnFormClosing(e);
        }

        /// <summary>
        /// Shows a dialog to capture a template for the specified element.
        /// </summary>
        /// <returns>True if template was captured successfully</returns>
        public static bool CaptureTemplate(string elementName, string description = null)
        {
            using (var form = new TemplateCaptureForm(elementName, description))
            {
                var result = form.ShowDialog();
                return result == DialogResult.OK && form.TemplateCaptured;
            }
        }

        /// <summary>
        /// Shows a dialog to capture a template, with callback for the captured location.
        /// </summary>
        public static bool CaptureTemplate(string elementName, string description, out Point? capturedLocation)
        {
            capturedLocation = null;
            using (var form = new TemplateCaptureForm(elementName, description))
            {
                var result = form.ShowDialog();
                if (result == DialogResult.OK && form.TemplateCaptured)
                {
                    capturedLocation = form.CapturedLocation;
                    return true;
                }
                return false;
            }
        }
    }
}
