using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>
    /// A transparent, click-through overlay that displays fish detection results
    /// on top of the game window.
    /// </summary>
    public class FishingOverlayForm : Form
    {
        // Win32 constants for click-through transparency
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        // Detection data to display
        private Rectangle _scanArea;
        private List<FishCandidate> _candidates = new List<FishCandidate>();
        private List<List<Point>> _blobs = new List<List<Point>>();  // Raw detection blobs
        private Point? _targetFish;
        private Point? _castDestination;
        private string _statusText = "";
        private int _darkPixelCount = 0;

        // Action status display
        private string _currentAction = "";
        private string _nextAction = "";
        private string _fishingStatus = "Ready";
        private int _fishCaught = 0;
        private int _castCount = 0;
        private string _location = "";

        // Timer for repositioning over game window
        private Timer _repositionTimer;

        public FishingOverlayForm()
        {
            InitializeOverlay();
        }

        private void InitializeOverlay()
        {
            // Form settings for transparency
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;
            this.StartPosition = FormStartPosition.Manual;
            this.DoubleBuffered = true;

            // Set initial size (will be repositioned by timer)
            this.Size = new Size(800, 600);
            this.Location = new Point(100, 100);

            // Start repositioning timer - will position over game window
            _repositionTimer = new Timer();
            _repositionTimer.Interval = 100; // Update position every 100ms
            _repositionTimer.Tick += RepositionTimer_Tick;
            _repositionTimer.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Make the form click-through
            MakeClickThrough();
        }

        private void MakeClickThrough()
        {
            int extendedStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        private void RepositionTimer_Tick(object sender, EventArgs e)
        {
            RepositionOverGameWindow();
        }

        private void RepositionOverGameWindow()
        {
            var gameRect = CoreFunctionality.GetGameWindowRect();
            if (!gameRect.IsEmpty)
            {
                if (this.Location.X != gameRect.X || this.Location.Y != gameRect.Y ||
                    this.Width != gameRect.Width || this.Height != gameRect.Height)
                {
                    this.Location = new Point(gameRect.X, gameRect.Y);
                    this.Size = new Size(gameRect.Width, gameRect.Height);
                }
            }
        }

        /// <summary>
        /// Updates the overlay with new detection results.
        /// </summary>
        public void UpdateDetection(FishDetectionDebugResult result, Point? targetFish = null, string status = "")
        {
            if (result == null) return;

            _scanArea = result.ScanArea;
            _candidates = result.AllCandidates ?? new List<FishCandidate>();
            _blobs = result.Blobs ?? new List<List<Point>>();
            _targetFish = targetFish ?? result.BestShadowPosition;
            _castDestination = result.CastDestination;
            _statusText = status;
            _darkPixelCount = result.DarkPixelCount;

            // Force redraw
            this.Invalidate();
        }

        /// <summary>
        /// Clears the overlay display.
        /// </summary>
        public void ClearOverlay()
        {
            _candidates.Clear();
            _blobs.Clear();
            _targetFish = null;
            _castDestination = null;
            _statusText = "";
            _darkPixelCount = 0;
            _currentAction = "";
            _nextAction = "";
            _fishingStatus = "Ready";
            _fishCaught = 0;
            _castCount = 0;
            _location = "";
            this.Invalidate();
        }

        /// <summary>
        /// Sets the status text displayed on the overlay.
        /// </summary>
        public void SetStatus(string status)
        {
            _statusText = status;
            this.Invalidate();
        }

        /// <summary>
        /// Updates the fishing action status panel.
        /// </summary>
        public void UpdateActionStatus(string currentAction, string nextAction, string status)
        {
            _currentAction = currentAction ?? "";
            _nextAction = nextAction ?? "";
            _fishingStatus = status ?? "Ready";
            this.Invalidate();
        }

        /// <summary>
        /// Updates the fishing statistics.
        /// </summary>
        public void UpdateStats(int fishCaught, int castCount)
        {
            _fishCaught = fishCaught;
            _castCount = castCount;
            this.Invalidate();
        }

        /// <summary>
        /// Sets the current fishing location name.
        /// </summary>
        public void SetLocation(string location)
        {
            _location = location ?? "";
            this.Invalidate();
        }

        /// <summary>
        /// Increments the fish caught counter.
        /// </summary>
        public void IncrementFishCaught()
        {
            _fishCaught++;
            this.Invalidate();
        }

        /// <summary>
        /// Increments the cast counter.
        /// </summary>
        public void IncrementCastCount()
        {
            _castCount++;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Get game window offset for coordinate translation
            var gameRect = CoreFunctionality.GetGameWindowRect();
            int offsetX = gameRect.IsEmpty ? 0 : -gameRect.X;
            int offsetY = gameRect.IsEmpty ? 0 : -gameRect.Y;

            // Draw scan area boundary
            if (!_scanArea.IsEmpty)
            {
                using (var pen = new Pen(Color.FromArgb(100, Color.Yellow), 2))
                {
                    pen.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(pen, _scanArea);
                }
            }

            // Draw all raw detection blobs as green dots (like debug view)
            using (var brush = new SolidBrush(Color.FromArgb(150, Color.LimeGreen)))
            {
                foreach (var blob in _blobs)
                {
                    foreach (var point in blob)
                    {
                        // Draw small rectangles for each detected pixel
                        g.FillRectangle(brush, point.X - 1, point.Y - 1, 3, 3);
                    }
                }
            }

            // Draw all detected fish candidates
            foreach (var candidate in _candidates)
            {
                bool isTarget = _targetFish.HasValue &&
                    Math.Abs(candidate.Position.X - _targetFish.Value.X) < 10 &&
                    Math.Abs(candidate.Position.Y - _targetFish.Value.Y) < 10;

                // Different colors for target vs other fish
                Color circleColor = isTarget ? Color.Lime : Color.Cyan;
                int circleSize = isTarget ? 40 : 30;
                int penWidth = isTarget ? 3 : 2;

                // Draw circle around fish
                using (var pen = new Pen(circleColor, penWidth))
                {
                    int x = candidate.Position.X - circleSize / 2;
                    int y = candidate.Position.Y - circleSize / 2;
                    g.DrawEllipse(pen, x, y, circleSize, circleSize);
                }

                // Draw bubble indicator if fish has bubbles
                if (candidate.HasBubblesAbove)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(180, Color.White)))
                    {
                        int bubbleY = candidate.Position.Y - circleSize / 2 - 15;
                        g.FillEllipse(brush, candidate.Position.X - 5, bubbleY, 10, 10);
                        g.FillEllipse(brush, candidate.Position.X - 10, bubbleY - 8, 7, 7);
                        g.FillEllipse(brush, candidate.Position.X + 5, bubbleY - 5, 6, 6);
                    }
                }

                // Draw crosshair on target fish
                if (isTarget)
                {
                    using (var pen = new Pen(Color.Red, 2))
                    {
                        int cx = candidate.Position.X;
                        int cy = candidate.Position.Y;
                        g.DrawLine(pen, cx - 15, cy, cx + 15, cy);
                        g.DrawLine(pen, cx, cy - 15, cx, cy + 15);
                    }
                }
            }

            // Draw cast destination line if available
            if (_targetFish.HasValue && _castDestination.HasValue)
            {
                using (var pen = new Pen(Color.FromArgb(150, Color.Orange), 2))
                {
                    pen.DashStyle = DashStyle.Dot;
                    g.DrawLine(pen, _targetFish.Value, _castDestination.Value);
                }

                // Draw cast destination marker
                using (var brush = new SolidBrush(Color.FromArgb(150, Color.Orange)))
                {
                    g.FillEllipse(brush,
                        _castDestination.Value.X - 8,
                        _castDestination.Value.Y - 8,
                        16, 16);
                }
            }

            // Draw status text (centered at top)
            if (!string.IsNullOrEmpty(_statusText))
            {
                using (var font = new Font("Arial", 12, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                using (var shadowBrush = new SolidBrush(Color.Black))
                {
                    // Measure text to center it
                    var textSize = g.MeasureString(_statusText, font);
                    float centerX = (this.Width - textSize.Width) / 2;

                    // Draw shadow
                    g.DrawString(_statusText, font, shadowBrush, centerX + 2, 12);
                    // Draw text
                    g.DrawString(_statusText, font, brush, centerX, 10);
                }
            }

            // Draw detection stats (centered)
            int yOffset = 35;
            using (var font = new Font("Arial", 10, FontStyle.Regular))
            using (var shadowBrush = new SolidBrush(Color.Black))
            {
                // Dark pixels found
                if (_darkPixelCount > 0 || _blobs.Count > 0)
                {
                    string blobText = $"Blobs: {_blobs.Count} | Dark pixels: {_darkPixelCount}";
                    var textSize = g.MeasureString(blobText, font);
                    float centerX = (this.Width - textSize.Width) / 2;
                    using (var brush = new SolidBrush(Color.LimeGreen))
                    {
                        g.DrawString(blobText, font, shadowBrush, centerX + 2, yOffset + 2);
                        g.DrawString(blobText, font, brush, centerX, yOffset);
                    }
                    yOffset += 18;
                }

                // Fish candidates
                if (_candidates.Count > 0)
                {
                    string countText = $"Fish candidates: {_candidates.Count}";
                    var textSize = g.MeasureString(countText, font);
                    float centerX = (this.Width - textSize.Width) / 2;
                    using (var brush = new SolidBrush(Color.Cyan))
                    {
                        g.DrawString(countText, font, shadowBrush, centerX + 2, yOffset + 2);
                        g.DrawString(countText, font, brush, centerX, yOffset);
                    }
                }
            }

            // Draw action status panel at bottom-left (to avoid covering scan area)
            DrawActionStatusPanel(g);
        }

        private void DrawActionStatusPanel(Graphics g)
        {
            // Panel dimensions and position (bottom-left)
            int panelWidth = 280;
            int panelHeight = 140;
            int panelX = 15;
            int panelY = this.Height - panelHeight - 15;

            // Draw panel background
            using (var bgBrush = new SolidBrush(Color.FromArgb(200, 20, 20, 20)))
            using (var borderPen = new Pen(Color.FromArgb(200, 70, 130, 180), 2))
            {
                var panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);

                // Rounded rectangle
                using (var path = CreateRoundedRectangle(panelRect, 10))
                {
                    g.FillPath(bgBrush, path);
                    g.DrawPath(borderPen, path);
                }
            }

            int textX = panelX + 12;
            int textY = panelY + 10;

            // Title
            using (var titleFont = new Font("Segoe UI", 11, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(255, 70, 180, 255)))
            {
                g.DrawString("Fishing", titleFont, titleBrush, textX, textY);
            }

            // Status indicator
            Color statusColor = _fishingStatus switch
            {
                "Fishing" => Color.LimeGreen,
                "Casting" => Color.Yellow,
                "Waiting" => Color.Orange,
                "Selling" => Color.Cyan,
                "Walking" => Color.MediumPurple,
                "Complete" => Color.Cyan,
                "Stopped" => Color.Gray,
                _ => Color.Gray
            };

            using (var statusFont = new Font("Segoe UI", 9))
            using (var statusBrush = new SolidBrush(statusColor))
            {
                string statusDisplay = $"[{_fishingStatus}]";
                var statusSize = g.MeasureString(statusDisplay, statusFont);
                g.DrawString(statusDisplay, statusFont, statusBrush, panelX + panelWidth - statusSize.Width - 12, textY + 2);
            }

            textY += 24;

            // Location
            if (!string.IsNullOrEmpty(_location))
            {
                using (var labelFont = new Font("Segoe UI", 9))
                using (var valueFont = new Font("Segoe UI", 9, FontStyle.Bold))
                using (var labelBrush = new SolidBrush(Color.LightGray))
                using (var valueBrush = new SolidBrush(Color.FromArgb(255, 100, 255, 150)))
                {
                    g.DrawString("Location:", labelFont, labelBrush, textX, textY);
                    g.DrawString(_location, valueFont, valueBrush, textX + 62, textY);
                }
                textY += 18;
            }

            // Stats (Fish caught / Casts)
            using (var font = new Font("Segoe UI", 9))
            using (var labelBrush = new SolidBrush(Color.LightGray))
            using (var valueBrush = new SolidBrush(Color.White))
            {
                string statsText = $"Fish: {_fishCaught}  |  Casts: {_castCount}";
                g.DrawString(statsText, font, valueBrush, textX, textY);
            }

            textY += 22;

            // Current action
            using (var labelFont = new Font("Segoe UI", 9))
            using (var actionFont = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var labelBrush = new SolidBrush(Color.LightGray))
            using (var actionBrush = new SolidBrush(Color.Yellow))
            {
                g.DrawString("Current:", labelFont, labelBrush, textX, textY);
                string actionDisplay = string.IsNullOrEmpty(_currentAction) ? "-" : _currentAction;
                g.DrawString(actionDisplay, actionFont, actionBrush, textX + 58, textY - 1);
            }

            textY += 20;

            // Next action
            using (var labelFont = new Font("Segoe UI", 9))
            using (var nextFont = new Font("Segoe UI", 9))
            using (var labelBrush = new SolidBrush(Color.LightGray))
            using (var nextBrush = new SolidBrush(Color.FromArgb(255, 180, 180, 180)))
            {
                g.DrawString("Next:", labelFont, labelBrush, textX, textY);
                string nextDisplay = string.IsNullOrEmpty(_nextAction) ? "-" : _nextAction;
                g.DrawString(nextDisplay, nextFont, nextBrush, textX + 58, textY);
            }
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _repositionTimer?.Stop();
            _repositionTimer?.Dispose();
            base.OnFormClosing(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
                return cp;
            }
        }
    }
}
