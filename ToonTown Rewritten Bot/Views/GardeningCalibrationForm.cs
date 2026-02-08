using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>
    /// Overlay form that allows users to calibrate the jellybean detection area
    /// and shows real-time detection of jellybeans using template matching.
    /// </summary>
    public class GardeningCalibrationForm : Form
    {
        private Rectangle _scanArea;
        private Rectangle _gameWindowRect;
        private Bitmap _backgroundScreenshot;
        private Dictionary<string, Point> _detectedBeans = new Dictionary<string, Point>();

        /// <summary>
        /// Jellybean template definitions - maps bean type char to the template element name
        /// used by UIElementManager (matches TemplateDefinitionManager names).
        /// </summary>
        public static readonly Dictionary<char, string> JellybeanTemplates = new Dictionary<char, string>
        {
            { 'r', "Red Jellybean Button" },
            { 'g', "Green Jellybean Button" },
            { 'o', "Orange Jellybean Button" },
            { 'u', "Purple Jellybean Button" },
            { 'b', "Blue Jellybean Button" },
            { 'i', "Pink Jellybean Button" },
            { 'y', "Yellow Jellybean Button" },
            { 'c', "Cyan Jellybean Button" },
            { 's', "Silver Jellybean Button" },
        };

        /// <summary>
        /// Display colors for each jellybean type (for visualization only).
        /// </summary>
        private static readonly Dictionary<string, Color> JellybeanDisplayColors = new Dictionary<string, Color>
        {
            { "Red Jellybean Button", Color.Red },
            { "Green Jellybean Button", Color.Green },
            { "Orange Jellybean Button", Color.Orange },
            { "Purple Jellybean Button", Color.Purple },
            { "Blue Jellybean Button", Color.Blue },
            { "Pink Jellybean Button", Color.Pink },
            { "Yellow Jellybean Button", Color.Yellow },
            { "Cyan Jellybean Button", Color.Cyan },
            { "Silver Jellybean Button", Color.Silver },
        };

        // Drag state
        private bool _isDragging = false;
        private DragHandle _activeHandle = DragHandle.None;
        private Point _dragStartMouse;
        private Rectangle _dragStartRect;

        private const int HandleSize = 14;
        private const int EdgeHitSize = 10;

        // UI Controls positioned at bottom
        private Panel _bottomPanel;
        private Label _instructionsLabel;
        private Label _statusLabel;

        private enum DragHandle
        {
            None,
            TopLeft, Top, TopRight,
            Left, Center, Right,
            BottomLeft, Bottom, BottomRight
        }

        public bool WasSaved { get; private set; } = false;

        public GardeningCalibrationForm()
        {
            _gameWindowRect = CoreFunctionality.GetGameWindowRect();

            if (_gameWindowRect.IsEmpty)
            {
                MessageBox.Show("Game window not found. Please make sure Toontown is running.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Load += (s, e) => Close();
                return;
            }

            // Get existing or default scan area
            _scanArea = GardeningScanAreaManager.GetJellybeanPanelArea(
                _gameWindowRect.Width, _gameWindowRect.Height);

            InitializeForm();
        }

        private void InitializeForm()
        {
            // Position over the game window
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(_gameWindowRect.X, _gameWindowRect.Y);
            this.Size = new Size(_gameWindowRect.Width, _gameWindowRect.Height);
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;
            this.ShowInTaskbar = false;
            this.KeyPreview = true;
            this.Text = "Gardening Calibration";

            // Bottom panel for controls - positioned at the very bottom
            _bottomPanel = new Panel
            {
                BackColor = Color.FromArgb(240, 30, 30, 30),
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(10)
            };
            this.Controls.Add(_bottomPanel);

            // Instructions label
            _instructionsLabel = new Label
            {
                Text = "Drag the GREEN box to cover the jellybean buttons. Click 'Detect' to test, then Save.",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(10, 8),
                AutoSize = true
            };
            _bottomPanel.Controls.Add(_instructionsLabel);

            // Status label
            _statusLabel = new Label
            {
                Text = "Click 'Detect' to test",
                ForeColor = Color.Yellow,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(10, 32),
                AutoSize = true
            };
            _bottomPanel.Controls.Add(_statusLabel);

            // Capture Templates button
            var captureButton = new Button
            {
                Text = "Capture Templates",
                Size = new Size(130, 40),
                Location = new Point(this.Width - 510, 10),
                BackColor = Color.DarkOrange,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            captureButton.Click += (s, e) => CaptureJellybeanTemplates();
            _bottomPanel.Controls.Add(captureButton);

            // Detect button
            var detectButton = new Button
            {
                Text = "Detect",
                Size = new Size(100, 40),
                Location = new Point(this.Width - 360, 10),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            detectButton.Click += (s, e) => RefreshDetection();
            _bottomPanel.Controls.Add(detectButton);

            // Save button
            var saveButton = new Button
            {
                Text = "Save (Enter)",
                Size = new Size(100, 40),
                Location = new Point(this.Width - 240, 10),
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            saveButton.Click += (s, e) => SaveAndClose();
            _bottomPanel.Controls.Add(saveButton);

            // Cancel button
            var cancelButton = new Button
            {
                Text = "Cancel (Esc)",
                Size = new Size(100, 40),
                Location = new Point(this.Width - 120, 10),
                BackColor = Color.DarkRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            cancelButton.Click += (s, e) => Close();
            _bottomPanel.Controls.Add(cancelButton);

            // Event handlers - attach to form directly
            this.Paint += OnPaint;
            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
            this.KeyDown += OnKeyDown;
            this.Shown += (s, e) => { this.Activate(); this.Focus(); CaptureInitialScreenshot(); };
        }

        private void CaptureInitialScreenshot()
        {
            try
            {
                _backgroundScreenshot?.Dispose();
                _backgroundScreenshot = ImageRecognition.GetWindowScreenshot() as Bitmap;
                Invalidate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GardeningCalibration] Initial capture error: {ex.Message}");
            }
        }

        private void RefreshDetection()
        {
            try
            {
                _statusLabel.Text = "Detecting...";
                _statusLabel.ForeColor = Color.White;
                _statusLabel.Refresh();

                // Capture fresh screenshot
                _backgroundScreenshot?.Dispose();
                _backgroundScreenshot = ImageRecognition.GetWindowScreenshot() as Bitmap;

                if (_backgroundScreenshot != null)
                {
                    var allBeans = new Dictionary<string, Point>();
                    int missingTemplates = 0;

                    // Extract scan area region for faster searching
                    Rectangle clampedScanArea = _scanArea;
                    clampedScanArea.Intersect(new Rectangle(0, 0, _backgroundScreenshot.Width, _backgroundScreenshot.Height));

                    if (clampedScanArea.Width > 10 && clampedScanArea.Height > 10)
                    {
                        using (var scanRegion = _backgroundScreenshot.Clone(clampedScanArea, _backgroundScreenshot.PixelFormat))
                        {
                            foreach (var kvp in JellybeanTemplates)
                            {
                                string templatePath = UIElementManager.Instance.GetTemplatePath(kvp.Value);

                                if (!File.Exists(templatePath))
                                {
                                    missingTemplates++;
                                    continue;
                                }

                                try
                                {
                                    using (var template = new Bitmap(templatePath))
                                    {
                                        var result = ImageTemplateMatcher.FindTemplate(scanRegion, template, 0.85);
                                        if (result.Found)
                                        {
                                            // Adjust coordinates back to full screenshot coordinates
                                            Point fullScreenCoord = new Point(
                                                clampedScanArea.X + result.Center.X,
                                                clampedScanArea.Y + result.Center.Y);
                                            allBeans[kvp.Value] = fullScreenCoord;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[GardeningCalibration] Error matching {kvp.Value}: {ex.Message}");
                                }
                            }
                        }
                    }

                    _detectedBeans = allBeans;

                    int count = _detectedBeans.Count;
                    string statusText = $"Detected: {count} beans";

                    if (missingTemplates > 0)
                    {
                        statusText += $" ({missingTemplates} templates missing - click 'Capture Templates')";
                        _statusLabel.ForeColor = Color.Orange;
                    }
                    else if (count >= 5)
                    {
                        statusText += " (Good!)";
                        _statusLabel.ForeColor = Color.Lime;
                    }
                    else if (count > 0)
                    {
                        statusText += " (Adjust area)";
                        _statusLabel.ForeColor = Color.Yellow;
                    }
                    else
                    {
                        statusText += " (No beans found)";
                        _statusLabel.ForeColor = Color.Red;
                    }

                    _statusLabel.Text = statusText;
                }

                Invalidate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GardeningCalibration] Refresh error: {ex.Message}");
                _statusLabel.Text = "Detection failed";
                _statusLabel.ForeColor = Color.Red;
            }
        }

        /// <summary>
        /// Opens the template capture tool for each missing jellybean template.
        /// </summary>
        private void CaptureJellybeanTemplates()
        {
            var missingTemplates = new List<KeyValuePair<char, string>>();

            foreach (var kvp in JellybeanTemplates)
            {
                if (!UIElementManager.Instance.HasTemplate(kvp.Value))
                {
                    missingTemplates.Add(kvp);
                }
            }

            if (missingTemplates.Count == 0)
            {
                MessageBox.Show("All jellybean templates are already captured!", "Templates Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Hide this overlay temporarily
            this.Hide();

            try
            {
                int captured = 0;
                foreach (var kvp in missingTemplates)
                {
                    string beanName = kvp.Value.Replace(" Jellybean Button", "");
                    string description = $"Select the {beanName.ToUpper()} jellybean button.\n\n" +
                                       "Make sure the jellybean selection panel is visible in the game, " +
                                       $"then click and drag to select just the {beanName} jellybean icon.";

                    bool result = TemplateCaptureForm.CaptureTemplate(kvp.Value, description);
                    if (result)
                    {
                        captured++;
                    }
                    else
                    {
                        var continueResult = MessageBox.Show(
                            $"Skipped {beanName}. Continue capturing other jellybean templates?",
                            "Continue?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (continueResult == DialogResult.No)
                            break;
                    }
                }

                MessageBox.Show($"Captured {captured} of {missingTemplates.Count} templates.\n\n" +
                              "Click 'Detect' to test the detection.", "Capture Complete",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                this.Show();
                this.BringToFront();
                CaptureInitialScreenshot();
            }
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw background screenshot
            if (_backgroundScreenshot != null)
            {
                g.DrawImage(_backgroundScreenshot, 0, 0, this.Width, this.Height - _bottomPanel.Height);
            }

            // Darken areas outside scan area
            using (var darkBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
            {
                // Top
                if (_scanArea.Y > 0)
                    g.FillRectangle(darkBrush, 0, 0, this.Width, _scanArea.Y);
                // Bottom (above the panel)
                if (_scanArea.Bottom < this.Height - _bottomPanel.Height)
                    g.FillRectangle(darkBrush, 0, _scanArea.Bottom, this.Width, this.Height - _bottomPanel.Height - _scanArea.Bottom);
                // Left
                if (_scanArea.X > 0)
                    g.FillRectangle(darkBrush, 0, _scanArea.Y, _scanArea.X, _scanArea.Height);
                // Right
                if (_scanArea.Right < this.Width)
                    g.FillRectangle(darkBrush, _scanArea.Right, _scanArea.Y, this.Width - _scanArea.Right, _scanArea.Height);
            }

            // Draw scan area rectangle with thick border
            using (var pen = new Pen(Color.Lime, 4))
            {
                g.DrawRectangle(pen, _scanArea);
            }

            // Draw inner dashed line
            using (var pen = new Pen(Color.White, 1) { DashStyle = DashStyle.Dash })
            {
                g.DrawRectangle(pen, _scanArea.X + 3, _scanArea.Y + 3, _scanArea.Width - 6, _scanArea.Height - 6);
            }

            // Draw "JELLYBEAN PANEL" label above the box
            using (var font = new Font("Segoe UI", 11, FontStyle.Bold))
            using (var bgBrush = new SolidBrush(Color.FromArgb(200, 0, 100, 0)))
            {
                string label = "JELLYBEAN PANEL - Drag to resize";
                var textSize = g.MeasureString(label, font);
                float labelX = _scanArea.X;
                float labelY = _scanArea.Y - textSize.Height - 5;
                if (labelY < 0) labelY = _scanArea.Bottom + 5;

                g.FillRectangle(bgBrush, labelX, labelY, textSize.Width + 10, textSize.Height + 4);
                g.DrawString(label, font, Brushes.White, labelX + 5, labelY + 2);
            }

            // Draw detected beans
            foreach (var bean in _detectedBeans)
            {
                Color displayColor = JellybeanDisplayColors.TryGetValue(bean.Key, out var c) ? c : Color.White;
                string displayName = bean.Key.Replace(" Jellybean Button", "");

                using (var pen = new Pen(displayColor, 3))
                using (var brush = new SolidBrush(Color.FromArgb(180, displayColor)))
                {
                    g.DrawEllipse(pen, bean.Value.X - 18, bean.Value.Y - 18, 36, 36);
                    g.FillEllipse(brush, bean.Value.X - 6, bean.Value.Y - 6, 12, 12);

                    // Draw label
                    using (var labelFont = new Font("Segoe UI", 8, FontStyle.Bold))
                    using (var bgBrush = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
                    {
                        var textSize = g.MeasureString(displayName, labelFont);
                        g.FillRectangle(bgBrush, bean.Value.X + 20, bean.Value.Y - 8, textSize.Width + 4, textSize.Height);
                        g.DrawString(displayName, labelFont, Brushes.White, bean.Value.X + 22, bean.Value.Y - 6);
                    }
                }
            }

            // Draw resize handles (larger and more visible)
            DrawHandle(g, GetHandleRect(DragHandle.TopLeft));
            DrawHandle(g, GetHandleRect(DragHandle.Top));
            DrawHandle(g, GetHandleRect(DragHandle.TopRight));
            DrawHandle(g, GetHandleRect(DragHandle.Left));
            DrawHandle(g, GetHandleRect(DragHandle.Right));
            DrawHandle(g, GetHandleRect(DragHandle.BottomLeft));
            DrawHandle(g, GetHandleRect(DragHandle.Bottom));
            DrawHandle(g, GetHandleRect(DragHandle.BottomRight));

            // Draw center move handle (larger circle)
            var centerRect = GetHandleRect(DragHandle.Center);
            using (var brush = new SolidBrush(Color.FromArgb(220, 255, 255, 0)))
            {
                g.FillEllipse(brush, centerRect);
            }
            using (var pen = new Pen(Color.Black, 2))
            {
                g.DrawEllipse(pen, centerRect);
            }
            // Draw move arrows in center
            using (var pen = new Pen(Color.Black, 2))
            {
                int cx = centerRect.X + centerRect.Width / 2;
                int cy = centerRect.Y + centerRect.Height / 2;
                g.DrawLine(pen, cx - 8, cy, cx + 8, cy);
                g.DrawLine(pen, cx, cy - 8, cx, cy + 8);
            }

            // Draw dimensions
            string dimensions = $"{_scanArea.Width} x {_scanArea.Height}";
            using (var font = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var bgBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
            {
                var textSize = g.MeasureString(dimensions, font);
                float textX = _scanArea.X + (_scanArea.Width - textSize.Width) / 2;
                float textY = _scanArea.Bottom + 30;
                if (textY > this.Height - _bottomPanel.Height - 30)
                    textY = _scanArea.Y - 50;

                g.FillRectangle(bgBrush, textX - 5, textY - 2, textSize.Width + 10, textSize.Height + 4);
                g.DrawString(dimensions, font, Brushes.White, textX, textY);
            }
        }

        private void DrawHandle(Graphics g, Rectangle rect)
        {
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillRectangle(brush, rect);
            }
            using (var pen = new Pen(Color.Black, 2))
            {
                g.DrawRectangle(pen, rect);
            }
        }

        private Rectangle GetHandleRect(DragHandle handle)
        {
            int hs = HandleSize;
            int half = hs / 2;

            return handle switch
            {
                DragHandle.TopLeft => new Rectangle(_scanArea.X - half, _scanArea.Y - half, hs, hs),
                DragHandle.Top => new Rectangle(_scanArea.X + _scanArea.Width / 2 - half, _scanArea.Y - half, hs, hs),
                DragHandle.TopRight => new Rectangle(_scanArea.Right - half, _scanArea.Y - half, hs, hs),
                DragHandle.Left => new Rectangle(_scanArea.X - half, _scanArea.Y + _scanArea.Height / 2 - half, hs, hs),
                DragHandle.Center => new Rectangle(_scanArea.X + _scanArea.Width / 2 - 18, _scanArea.Y + _scanArea.Height / 2 - 18, 36, 36),
                DragHandle.Right => new Rectangle(_scanArea.Right - half, _scanArea.Y + _scanArea.Height / 2 - half, hs, hs),
                DragHandle.BottomLeft => new Rectangle(_scanArea.X - half, _scanArea.Bottom - half, hs, hs),
                DragHandle.Bottom => new Rectangle(_scanArea.X + _scanArea.Width / 2 - half, _scanArea.Bottom - half, hs, hs),
                DragHandle.BottomRight => new Rectangle(_scanArea.Right - half, _scanArea.Bottom - half, hs, hs),
                _ => Rectangle.Empty
            };
        }

        private DragHandle HitTest(Point p)
        {
            // Don't respond to clicks in the bottom panel area
            if (p.Y >= this.Height - _bottomPanel.Height)
                return DragHandle.None;

            // Check handles first (enlarged hit areas)
            int hitMargin = 5;

            var tl = GetHandleRect(DragHandle.TopLeft); tl.Inflate(hitMargin, hitMargin);
            if (tl.Contains(p)) return DragHandle.TopLeft;

            var tr = GetHandleRect(DragHandle.TopRight); tr.Inflate(hitMargin, hitMargin);
            if (tr.Contains(p)) return DragHandle.TopRight;

            var bl = GetHandleRect(DragHandle.BottomLeft); bl.Inflate(hitMargin, hitMargin);
            if (bl.Contains(p)) return DragHandle.BottomLeft;

            var br = GetHandleRect(DragHandle.BottomRight); br.Inflate(hitMargin, hitMargin);
            if (br.Contains(p)) return DragHandle.BottomRight;

            var t = GetHandleRect(DragHandle.Top); t.Inflate(hitMargin, hitMargin);
            if (t.Contains(p)) return DragHandle.Top;

            var b = GetHandleRect(DragHandle.Bottom); b.Inflate(hitMargin, hitMargin);
            if (b.Contains(p)) return DragHandle.Bottom;

            var l = GetHandleRect(DragHandle.Left); l.Inflate(hitMargin, hitMargin);
            if (l.Contains(p)) return DragHandle.Left;

            var r = GetHandleRect(DragHandle.Right); r.Inflate(hitMargin, hitMargin);
            if (r.Contains(p)) return DragHandle.Right;

            var c = GetHandleRect(DragHandle.Center); c.Inflate(hitMargin, hitMargin);
            if (c.Contains(p)) return DragHandle.Center;

            // Check if clicking on the edge of the rectangle
            var edgeRect = _scanArea;
            edgeRect.Inflate(EdgeHitSize, EdgeHitSize);
            var innerRect = _scanArea;
            innerRect.Inflate(-EdgeHitSize, -EdgeHitSize);

            if (edgeRect.Contains(p) && !innerRect.Contains(p))
            {
                // On an edge
                bool nearTop = Math.Abs(p.Y - _scanArea.Y) <= EdgeHitSize;
                bool nearBottom = Math.Abs(p.Y - _scanArea.Bottom) <= EdgeHitSize;
                bool nearLeft = Math.Abs(p.X - _scanArea.X) <= EdgeHitSize;
                bool nearRight = Math.Abs(p.X - _scanArea.Right) <= EdgeHitSize;

                if (nearTop && nearLeft) return DragHandle.TopLeft;
                if (nearTop && nearRight) return DragHandle.TopRight;
                if (nearBottom && nearLeft) return DragHandle.BottomLeft;
                if (nearBottom && nearRight) return DragHandle.BottomRight;
                if (nearTop) return DragHandle.Top;
                if (nearBottom) return DragHandle.Bottom;
                if (nearLeft) return DragHandle.Left;
                if (nearRight) return DragHandle.Right;
            }

            // Inside the rectangle = move
            if (_scanArea.Contains(p)) return DragHandle.Center;

            return DragHandle.None;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            var handle = HitTest(e.Location);
            if (handle != DragHandle.None)
            {
                _isDragging = true;
                _activeHandle = handle;
                _dragStartMouse = e.Location;
                _dragStartRect = _scanArea;
                this.Capture = true;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                int dx = e.X - _dragStartMouse.X;
                int dy = e.Y - _dragStartMouse.Y;

                Rectangle newRect = _dragStartRect;

                switch (_activeHandle)
                {
                    case DragHandle.TopLeft:
                        newRect.X = _dragStartRect.X + dx;
                        newRect.Y = _dragStartRect.Y + dy;
                        newRect.Width = _dragStartRect.Width - dx;
                        newRect.Height = _dragStartRect.Height - dy;
                        break;
                    case DragHandle.Top:
                        newRect.Y = _dragStartRect.Y + dy;
                        newRect.Height = _dragStartRect.Height - dy;
                        break;
                    case DragHandle.TopRight:
                        newRect.Y = _dragStartRect.Y + dy;
                        newRect.Width = _dragStartRect.Width + dx;
                        newRect.Height = _dragStartRect.Height - dy;
                        break;
                    case DragHandle.Left:
                        newRect.X = _dragStartRect.X + dx;
                        newRect.Width = _dragStartRect.Width - dx;
                        break;
                    case DragHandle.Center:
                        newRect.X = _dragStartRect.X + dx;
                        newRect.Y = _dragStartRect.Y + dy;
                        break;
                    case DragHandle.Right:
                        newRect.Width = _dragStartRect.Width + dx;
                        break;
                    case DragHandle.BottomLeft:
                        newRect.X = _dragStartRect.X + dx;
                        newRect.Width = _dragStartRect.Width - dx;
                        newRect.Height = _dragStartRect.Height + dy;
                        break;
                    case DragHandle.Bottom:
                        newRect.Height = _dragStartRect.Height + dy;
                        break;
                    case DragHandle.BottomRight:
                        newRect.Width = _dragStartRect.Width + dx;
                        newRect.Height = _dragStartRect.Height + dy;
                        break;
                }

                // Enforce minimum size
                if (newRect.Width < 100) newRect.Width = 100;
                if (newRect.Height < 100) newRect.Height = 100;

                // Keep within bounds (above the bottom panel)
                int maxY = this.Height - _bottomPanel.Height - 20;
                if (newRect.X < 0) newRect.X = 0;
                if (newRect.Y < 0) newRect.Y = 0;
                if (newRect.Right > this.Width) newRect.X = this.Width - newRect.Width;
                if (newRect.Bottom > maxY) newRect.Y = maxY - newRect.Height;

                _scanArea = newRect;
                Invalidate();
            }
            else
            {
                // Update cursor based on hit test
                var handle = HitTest(e.Location);
                this.Cursor = handle switch
                {
                    DragHandle.TopLeft => Cursors.SizeNWSE,
                    DragHandle.TopRight => Cursors.SizeNESW,
                    DragHandle.BottomLeft => Cursors.SizeNESW,
                    DragHandle.BottomRight => Cursors.SizeNWSE,
                    DragHandle.Top => Cursors.SizeNS,
                    DragHandle.Bottom => Cursors.SizeNS,
                    DragHandle.Left => Cursors.SizeWE,
                    DragHandle.Right => Cursors.SizeWE,
                    DragHandle.Center => Cursors.SizeAll,
                    _ => Cursors.Default
                };
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _activeHandle = DragHandle.None;
                this.Capture = false;
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
            {
                SaveAndClose();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void SaveAndClose()
        {
            GardeningScanAreaManager.SetJellybeanPanelArea(
                _scanArea, _gameWindowRect.Width, _gameWindowRect.Height);
            WasSaved = true;
            MessageBox.Show($"Scan area saved!\n\nDetected {_detectedBeans.Count} jellybeans in the selected area.\n\nThe bot will now search for jellybeans only in this area.",
                "Calibration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _backgroundScreenshot?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
