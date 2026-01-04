using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>
    /// Overlay form that allows users to visually adjust the fish scan area
    /// by dragging the edges/corners of the rectangle.
    /// </summary>
    public class ScanAreaCalibrationForm : Form
    {
        private string _locationName;
        private Rectangle _scanArea;
        private Rectangle _gameWindowRect;
        private Bitmap _backgroundScreenshot;

        // Drag state
        private bool _isDragging = false;
        private DragHandle _activeHandle = DragHandle.None;
        private Point _dragStartMouse;
        private Rectangle _dragStartRect;

        // Handle hit detection size
        private const int HandleSize = 12;
        private const int EdgeHitSize = 8;

        private enum DragHandle
        {
            None,
            TopLeft, Top, TopRight,
            Left, Center, Right,
            BottomLeft, Bottom, BottomRight
        }

        public Rectangle ResultScanArea => _scanArea;
        public bool WasSaved { get; private set; } = false;

        public ScanAreaCalibrationForm(string locationName, Rectangle defaultScanArea)
        {
            _locationName = locationName;
            _gameWindowRect = CoreFunctionality.GetGameWindowRect();

            if (_gameWindowRect.IsEmpty)
            {
                MessageBox.Show("Game window not found. Please make sure Toontown is running.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            // Check for existing custom scan area
            var customArea = CustomScanAreaManager.GetCustomScanArea(
                locationName, _gameWindowRect.Width, _gameWindowRect.Height);

            _scanArea = customArea ?? defaultScanArea;

            // Capture screenshot for background
            try
            {
                _backgroundScreenshot = ImageRecognition.GetWindowScreenshot() as Bitmap;
            }
            catch { }

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
            this.Opacity = 0.85;
            this.ShowInTaskbar = false;
            this.KeyPreview = true;

            // Instructions label
            var instructionsLabel = new Label
            {
                Text = $"Drag edges/corners to resize scan area for: {_locationName}\n" +
                       "Press ENTER to save, ESC to cancel, R to reset to default",
                AutoSize = true,
                BackColor = Color.FromArgb(200, 0, 0, 0),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Padding = new Padding(10),
                Location = new Point(10, 10)
            };
            this.Controls.Add(instructionsLabel);

            // Save button
            var saveButton = new Button
            {
                Text = "Save",
                Size = new Size(80, 30),
                Location = new Point(this.Width - 200, this.Height - 50),
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            saveButton.Click += (s, e) => SaveAndClose();
            this.Controls.Add(saveButton);

            // Cancel button
            var cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(80, 30),
                Location = new Point(this.Width - 100, this.Height - 50),
                BackColor = Color.DarkRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cancelButton.Click += (s, e) => Close();
            this.Controls.Add(cancelButton);

            // Reset button
            var resetButton = new Button
            {
                Text = "Reset Default",
                Size = new Size(100, 30),
                Location = new Point(this.Width - 320, this.Height - 50),
                BackColor = Color.DarkOrange,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            resetButton.Click += (s, e) => ResetToDefault();
            this.Controls.Add(resetButton);

            // Event handlers
            this.Paint += OnPaint;
            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
            this.KeyDown += OnKeyDown;
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw background screenshot if available
            if (_backgroundScreenshot != null)
            {
                g.DrawImage(_backgroundScreenshot, 0, 0, this.Width, this.Height);

                // Darken areas outside scan area
                using (var darkBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                {
                    // Top
                    g.FillRectangle(darkBrush, 0, 0, this.Width, _scanArea.Y);
                    // Bottom
                    g.FillRectangle(darkBrush, 0, _scanArea.Bottom, this.Width, this.Height - _scanArea.Bottom);
                    // Left
                    g.FillRectangle(darkBrush, 0, _scanArea.Y, _scanArea.X, _scanArea.Height);
                    // Right
                    g.FillRectangle(darkBrush, _scanArea.Right, _scanArea.Y, this.Width - _scanArea.Right, _scanArea.Height);
                }
            }

            // Draw scan area rectangle
            using (var pen = new Pen(Color.Lime, 3))
            {
                g.DrawRectangle(pen, _scanArea);
            }

            // Draw dashed inner line
            using (var pen = new Pen(Color.Yellow, 1) { DashStyle = DashStyle.Dash })
            {
                g.DrawRectangle(pen, _scanArea.X + 2, _scanArea.Y + 2, _scanArea.Width - 4, _scanArea.Height - 4);
            }

            // Draw resize handles
            DrawHandle(g, GetHandleRect(DragHandle.TopLeft));
            DrawHandle(g, GetHandleRect(DragHandle.Top));
            DrawHandle(g, GetHandleRect(DragHandle.TopRight));
            DrawHandle(g, GetHandleRect(DragHandle.Left));
            DrawHandle(g, GetHandleRect(DragHandle.Right));
            DrawHandle(g, GetHandleRect(DragHandle.BottomLeft));
            DrawHandle(g, GetHandleRect(DragHandle.Bottom));
            DrawHandle(g, GetHandleRect(DragHandle.BottomRight));

            // Draw center move handle (larger)
            var centerRect = GetHandleRect(DragHandle.Center);
            using (var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 0)))
            {
                g.FillEllipse(brush, centerRect);
            }
            using (var pen = new Pen(Color.Black, 2))
            {
                g.DrawEllipse(pen, centerRect);
            }

            // Draw dimensions label
            string dimensions = $"{_scanArea.Width} x {_scanArea.Height}";
            using (var font = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            using (var bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            {
                var textSize = g.MeasureString(dimensions, font);
                var textRect = new RectangleF(
                    _scanArea.X + (_scanArea.Width - textSize.Width) / 2,
                    _scanArea.Bottom + 5,
                    textSize.Width + 10,
                    textSize.Height + 4
                );
                g.FillRectangle(bgBrush, textRect);
                g.DrawString(dimensions, font, brush, textRect.X + 5, textRect.Y + 2);
            }
        }

        private void DrawHandle(Graphics g, Rectangle rect)
        {
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillRectangle(brush, rect);
            }
            using (var pen = new Pen(Color.Black, 1))
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
                DragHandle.Center => new Rectangle(_scanArea.X + _scanArea.Width / 2 - 15, _scanArea.Y + _scanArea.Height / 2 - 15, 30, 30),
                DragHandle.Right => new Rectangle(_scanArea.Right - half, _scanArea.Y + _scanArea.Height / 2 - half, hs, hs),
                DragHandle.BottomLeft => new Rectangle(_scanArea.X - half, _scanArea.Bottom - half, hs, hs),
                DragHandle.Bottom => new Rectangle(_scanArea.X + _scanArea.Width / 2 - half, _scanArea.Bottom - half, hs, hs),
                DragHandle.BottomRight => new Rectangle(_scanArea.Right - half, _scanArea.Bottom - half, hs, hs),
                _ => Rectangle.Empty
            };
        }

        private DragHandle HitTest(Point p)
        {
            // Check corner handles first (they have priority)
            if (GetHandleRect(DragHandle.TopLeft).Contains(p)) return DragHandle.TopLeft;
            if (GetHandleRect(DragHandle.TopRight).Contains(p)) return DragHandle.TopRight;
            if (GetHandleRect(DragHandle.BottomLeft).Contains(p)) return DragHandle.BottomLeft;
            if (GetHandleRect(DragHandle.BottomRight).Contains(p)) return DragHandle.BottomRight;

            // Check edge handles
            if (GetHandleRect(DragHandle.Top).Contains(p)) return DragHandle.Top;
            if (GetHandleRect(DragHandle.Bottom).Contains(p)) return DragHandle.Bottom;
            if (GetHandleRect(DragHandle.Left).Contains(p)) return DragHandle.Left;
            if (GetHandleRect(DragHandle.Right).Contains(p)) return DragHandle.Right;

            // Check center handle
            if (GetHandleRect(DragHandle.Center).Contains(p)) return DragHandle.Center;

            // Check if on edges
            var expanded = _scanArea;
            expanded.Inflate(EdgeHitSize, EdgeHitSize);
            var inner = _scanArea;
            inner.Inflate(-EdgeHitSize, -EdgeHitSize);

            if (expanded.Contains(p) && !inner.Contains(p))
            {
                // On edge - determine which edge
                bool nearTop = Math.Abs(p.Y - _scanArea.Y) < EdgeHitSize;
                bool nearBottom = Math.Abs(p.Y - _scanArea.Bottom) < EdgeHitSize;
                bool nearLeft = Math.Abs(p.X - _scanArea.X) < EdgeHitSize;
                bool nearRight = Math.Abs(p.X - _scanArea.Right) < EdgeHitSize;

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

            _activeHandle = HitTest(e.Location);
            if (_activeHandle != DragHandle.None)
            {
                _isDragging = true;
                _dragStartMouse = e.Location;
                _dragStartRect = _scanArea;
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
                if (newRect.Width < 50) newRect.Width = 50;
                if (newRect.Height < 50) newRect.Height = 50;

                // Keep within bounds
                if (newRect.X < 0) newRect.X = 0;
                if (newRect.Y < 0) newRect.Y = 0;
                if (newRect.Right > this.Width) newRect.X = this.Width - newRect.Width;
                if (newRect.Bottom > this.Height) newRect.Y = this.Height - newRect.Height;

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
            _isDragging = false;
            _activeHandle = DragHandle.None;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
            {
                SaveAndClose();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
            else if (e.KeyCode == Keys.R)
            {
                ResetToDefault();
            }
        }

        private void SaveAndClose()
        {
            CustomScanAreaManager.SetCustomScanArea(
                _locationName, _scanArea, _gameWindowRect.Width, _gameWindowRect.Height);
            WasSaved = true;
            Close();
        }

        private void ResetToDefault()
        {
            CustomScanAreaManager.RemoveCustomScanArea(_locationName);
            MessageBox.Show("Custom scan area removed. Will use default on next scan.",
                "Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _backgroundScreenshot?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
