using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>
    /// A transparent, click-through overlay that displays golf action progress
    /// on top of the game window.
    /// </summary>
    public class GolfOverlayForm : Form
    {
        // Win32 constants for click-through transparency
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        // Golf action data to display
        private string _currentAction = "";
        private string _nextAction = "";
        private int _currentStep = 0;
        private int _totalSteps = 0;
        private int _currentDuration = 0;
        private int _elapsedTime = 0;
        private string _statusText = "Ready";
        private string _courseName = "";
        private bool _isRunning = false;

        // Timer for repositioning over game window
        private Timer _repositionTimer;

        // Progress animation timer
        private Timer _progressTimer;
        private DateTime _actionStartTime;

        public GolfOverlayForm()
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

            // Start repositioning timer
            _repositionTimer = new Timer();
            _repositionTimer.Interval = 100;
            _repositionTimer.Tick += RepositionTimer_Tick;
            _repositionTimer.Start();

            // Progress timer for smooth countdown display
            _progressTimer = new Timer();
            _progressTimer.Interval = 50;
            _progressTimer.Tick += ProgressTimer_Tick;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
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

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (_isRunning && _currentDuration > 0)
            {
                _elapsedTime = (int)(DateTime.Now - _actionStartTime).TotalMilliseconds;
                this.Invalidate();
            }
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
        /// Updates the overlay with current action information.
        /// </summary>
        public void UpdateAction(string currentAction, string nextAction, int currentStep, int totalSteps, int durationMs)
        {
            _currentAction = currentAction ?? "";
            _nextAction = nextAction ?? "";
            _currentStep = currentStep;
            _totalSteps = totalSteps;
            _currentDuration = durationMs;
            _elapsedTime = 0;
            _actionStartTime = DateTime.Now;
            _isRunning = true;
            _statusText = "Running";

            _progressTimer.Start();
            this.Invalidate();
        }

        /// <summary>
        /// Sets the status text (e.g., "Starting", "Completed", "Cancelled").
        /// </summary>
        public void SetStatus(string status)
        {
            _statusText = status;
            if (status == "Completed" || status == "Cancelled" || status == "Ready")
            {
                _isRunning = false;
                _progressTimer.Stop();
            }
            this.Invalidate();
        }

        /// <summary>
        /// Sets the detected course name.
        /// </summary>
        public void SetCourseName(string courseName)
        {
            _courseName = courseName ?? "";
            this.Invalidate();
        }

        /// <summary>
        /// Clears the overlay display.
        /// </summary>
        public void ClearOverlay()
        {
            _currentAction = "";
            _nextAction = "";
            _currentStep = 0;
            _totalSteps = 0;
            _currentDuration = 0;
            _elapsedTime = 0;
            _statusText = "Ready";
            _courseName = "";
            _isRunning = false;
            _progressTimer.Stop();
            this.Invalidate();
        }

        /// <summary>
        /// Sets total steps for initial display.
        /// </summary>
        public void SetTotalSteps(int total)
        {
            _totalSteps = total;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Draw panel background (semi-transparent) - positioned on middle left
            int panelWidth = 320;
            int panelHeight = 165;
            int panelX = 15;
            int panelY = (this.Height - panelHeight) / 2;

            using (var bgBrush = new SolidBrush(Color.FromArgb(200, 20, 20, 20)))
            using (var borderPen = new Pen(Color.FromArgb(200, 100, 100, 100), 2))
            {
                var panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);

                // Rounded rectangle
                using (var path = CreateRoundedRectangle(panelRect, 10))
                {
                    g.FillPath(bgBrush, path);
                    g.DrawPath(borderPen, path);
                }
            }

            int textX = panelX + 15;
            int textY = panelY + 12;

            // Title
            using (var titleFont = new Font("Segoe UI", 11, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(255, 100, 200, 255)))
            {
                g.DrawString("Golf Actions", titleFont, titleBrush, textX, textY);
            }

            // Status indicator
            Color statusColor = _statusText switch
            {
                "Running" => Color.LimeGreen,
                "Completed" => Color.Cyan,
                "Cancelled" => Color.Orange,
                _ => Color.Gray
            };

            using (var statusFont = new Font("Segoe UI", 9))
            using (var statusBrush = new SolidBrush(statusColor))
            {
                string statusDisplay = $"[{_statusText}]";
                var statusSize = g.MeasureString(statusDisplay, statusFont);
                g.DrawString(statusDisplay, statusFont, statusBrush, panelX + panelWidth - statusSize.Width - 15, textY + 2);
            }

            textY += 24;

            // Course name
            using (var labelFont = new Font("Segoe UI", 9))
            using (var courseFont = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var labelBrush = new SolidBrush(Color.LightGray))
            using (var courseBrush = new SolidBrush(Color.FromArgb(255, 100, 255, 150)))
            {
                g.DrawString("Course:", labelFont, labelBrush, textX, textY);
                string courseDisplay = string.IsNullOrEmpty(_courseName) ? "Detecting..." : _courseName;
                g.DrawString(courseDisplay, courseFont, courseBrush, textX + 55, textY - 1);
            }

            textY += 22;

            // Progress (Step X of Y)
            using (var font = new Font("Segoe UI", 10))
            using (var brush = new SolidBrush(Color.White))
            {
                string progressText = _totalSteps > 0
                    ? $"Step {_currentStep} of {_totalSteps}"
                    : "Waiting...";
                g.DrawString(progressText, font, brush, textX, textY);
            }

            textY += 24;

            // Current action
            using (var labelFont = new Font("Segoe UI", 9))
            using (var actionFont = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var labelBrush = new SolidBrush(Color.LightGray))
            using (var actionBrush = new SolidBrush(Color.Yellow))
            {
                g.DrawString("Current:", labelFont, labelBrush, textX, textY);
                string actionDisplay = string.IsNullOrEmpty(_currentAction) ? "-" : _currentAction;
                g.DrawString(actionDisplay, actionFont, actionBrush, textX + 60, textY - 1);
            }

            textY += 22;

            // Next action
            using (var labelFont = new Font("Segoe UI", 9))
            using (var nextFont = new Font("Segoe UI", 9))
            using (var labelBrush = new SolidBrush(Color.LightGray))
            using (var nextBrush = new SolidBrush(Color.FromArgb(255, 180, 180, 180)))
            {
                g.DrawString("Next:", labelFont, labelBrush, textX, textY);
                string nextDisplay = string.IsNullOrEmpty(_nextAction) ? "-" : _nextAction;
                g.DrawString(nextDisplay, nextFont, nextBrush, textX + 60, textY);
            }

            textY += 24;

            // Progress bar for current action duration
            if (_currentDuration > 0 && _isRunning)
            {
                int barWidth = panelWidth - 30;
                int barHeight = 12;
                int barX = textX;

                // Background
                using (var bgBrush = new SolidBrush(Color.FromArgb(100, 50, 50, 50)))
                {
                    g.FillRectangle(bgBrush, barX, textY, barWidth, barHeight);
                }

                // Progress fill
                float progress = Math.Min(1.0f, (float)_elapsedTime / _currentDuration);
                int fillWidth = (int)(barWidth * progress);

                using (var fillBrush = new LinearGradientBrush(
                    new Rectangle(barX, textY, barWidth, barHeight),
                    Color.FromArgb(255, 100, 200, 100),
                    Color.FromArgb(255, 50, 150, 50),
                    LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(fillBrush, barX, textY, fillWidth, barHeight);
                }

                // Border
                using (var borderPen = new Pen(Color.FromArgb(150, 100, 100, 100), 1))
                {
                    g.DrawRectangle(borderPen, barX, textY, barWidth, barHeight);
                }

                // Time remaining text
                int remainingMs = Math.Max(0, _currentDuration - _elapsedTime);
                string timeText = $"{remainingMs}ms";
                using (var timeFont = new Font("Segoe UI", 8))
                using (var timeBrush = new SolidBrush(Color.White))
                {
                    var timeSize = g.MeasureString(timeText, timeFont);
                    g.DrawString(timeText, timeFont, timeBrush,
                        barX + (barWidth - timeSize.Width) / 2,
                        textY + (barHeight - timeSize.Height) / 2);
                }
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
            _progressTimer?.Stop();
            _progressTimer?.Dispose();
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
