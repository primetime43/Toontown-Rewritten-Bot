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

            // Start repositioning timer
            _repositionTimer = new Timer();
            _repositionTimer.Interval = 100; // Update position every 100ms
            _repositionTimer.Tick += RepositionTimer_Tick;
            _repositionTimer.Start();

            // Initial position
            RepositionOverGameWindow();
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

            // Draw status text
            if (!string.IsNullOrEmpty(_statusText))
            {
                using (var font = new Font("Arial", 12, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                using (var shadowBrush = new SolidBrush(Color.Black))
                {
                    // Draw shadow
                    g.DrawString(_statusText, font, shadowBrush, 12, 12);
                    // Draw text
                    g.DrawString(_statusText, font, brush, 10, 10);
                }
            }

            // Draw detection stats
            int yOffset = 35;
            using (var font = new Font("Arial", 10, FontStyle.Regular))
            using (var shadowBrush = new SolidBrush(Color.Black))
            {
                // Dark pixels found
                if (_darkPixelCount > 0 || _blobs.Count > 0)
                {
                    string blobText = $"Blobs: {_blobs.Count} | Dark pixels: {_darkPixelCount}";
                    using (var brush = new SolidBrush(Color.LimeGreen))
                    {
                        g.DrawString(blobText, font, shadowBrush, 12, yOffset + 2);
                        g.DrawString(blobText, font, brush, 10, yOffset);
                    }
                    yOffset += 18;
                }

                // Fish candidates
                if (_candidates.Count > 0)
                {
                    string countText = $"Fish candidates: {_candidates.Count}";
                    using (var brush = new SolidBrush(Color.Cyan))
                    {
                        g.DrawString(countText, font, shadowBrush, 12, yOffset + 2);
                        g.DrawString(countText, font, brush, 10, yOffset);
                    }
                }
            }
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
