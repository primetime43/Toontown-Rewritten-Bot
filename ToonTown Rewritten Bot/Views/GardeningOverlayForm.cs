using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>
    /// A transparent, click-through overlay that displays gardening planting progress
    /// on top of the game window.
    /// </summary>
    public class GardeningOverlayForm : Form
    {
        // Win32 constants for click-through transparency
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        // Bean char to display color mapping
        private static readonly Dictionary<char, Color> BeanColors = new Dictionary<char, Color>
        {
            { 'r', Color.Red },
            { 'g', Color.Green },
            { 'o', Color.Orange },
            { 'u', Color.Purple },
            { 'b', Color.Blue },
            { 'i', Color.Pink },
            { 'y', Color.Yellow },
            { 'c', Color.Cyan },
            { 's', Color.Silver },
        };

        private static readonly Dictionary<char, string> BeanNames = new Dictionary<char, string>
        {
            { 'r', "Red" },
            { 'g', "Green" },
            { 'o', "Orange" },
            { 'u', "Purple" },
            { 'b', "Blue" },
            { 'i', "Pink" },
            { 'y', "Yellow" },
            { 'c', "Cyan" },
            { 's', "Silver" },
        };

        // Gardening action data to display
        private string _currentAction = "";
        private string _nextAction = "";
        private int _currentStep = 0;
        private int _totalSteps = 0;
        private int _currentDuration = 0;
        private int _elapsedTime = 0;
        private string _statusText = "Ready";
        private string _flowerName = "";
        private char[] _beanSequence = Array.Empty<char>();
        private int _currentBeanIndex = -1;
        private bool _isRunning = false;

        // Timer for repositioning over game window
        private Timer _repositionTimer;

        // Progress animation timer
        private Timer _progressTimer;
        private DateTime _actionStartTime;

        public GardeningOverlayForm()
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
        /// Sets the flower name and bean sequence for visualization.
        /// </summary>
        public void SetFlowerInfo(string flowerName, string beanSequence)
        {
            _flowerName = flowerName ?? "";
            _beanSequence = (beanSequence ?? "").ToCharArray();
            _currentBeanIndex = -1;
            this.Invalidate();
        }

        /// <summary>
        /// Highlights the current bean in the sequence visualization.
        /// </summary>
        public void SetCurrentBean(int beanIndex)
        {
            _currentBeanIndex = beanIndex;
            this.Invalidate();
        }

        /// <summary>
        /// Sets the status text (e.g., "Running", "Completed", "Cancelled").
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
            _flowerName = "";
            _beanSequence = Array.Empty<char>();
            _currentBeanIndex = -1;
            _isRunning = false;
            _progressTimer.Stop();
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Draw panel background (semi-transparent) - positioned on middle right
            // (gardening UI is on the left side of the game window)
            int panelWidth = 340;
            int panelHeight = 260;
            int panelX = this.Width - panelWidth - 15;
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
                g.DrawString("Gardening", titleFont, titleBrush, textX, textY);
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

            // Flower name
            using (var labelFont = new Font("Segoe UI", 9))
            using (var flowerFont = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var labelBrush = new SolidBrush(Color.LightGray))
            using (var flowerBrush = new SolidBrush(Color.FromArgb(255, 100, 255, 150)))
            {
                g.DrawString("Flower:", labelFont, labelBrush, textX, textY);
                string flowerDisplay = string.IsNullOrEmpty(_flowerName) ? "..." : _flowerName;
                g.DrawString(flowerDisplay, flowerFont, flowerBrush, textX + 55, textY - 1);
            }

            textY += 24;

            // Bean sequence visualization
            if (_beanSequence.Length > 0)
            {
                DrawBeanSequence(g, textX, textY, panelWidth - 30);
            }

            textY += 34;

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

        private void DrawBeanSequence(Graphics g, int x, int y, int maxWidth)
        {
            int circleSize = 20;
            int currentCircleSize = 26;
            int spacing = 6;
            int totalWidth = _beanSequence.Length * (circleSize + spacing) - spacing;

            // Center the sequence if it fits, otherwise start at x
            int startX = x;
            if (totalWidth < maxWidth)
                startX = x + (maxWidth - totalWidth) / 2;

            for (int i = 0; i < _beanSequence.Length; i++)
            {
                char bean = _beanSequence[i];
                Color beanColor = BeanColors.TryGetValue(bean, out var c) ? c : Color.White;

                bool isCompleted = _currentBeanIndex >= 0 && i < _currentBeanIndex;
                bool isCurrent = i == _currentBeanIndex;
                bool isPending = _currentBeanIndex < 0 || i > _currentBeanIndex;

                int drawX = startX + i * (circleSize + spacing);
                int drawY = y;
                int size = circleSize;

                if (isCurrent)
                {
                    // Current bean: larger, bright with glow
                    size = currentCircleSize;
                    drawX -= (currentCircleSize - circleSize) / 2;
                    drawY -= (currentCircleSize - circleSize) / 2;

                    // Glow ring
                    using (var glowPen = new Pen(Color.FromArgb(120, beanColor), 3))
                    {
                        g.DrawEllipse(glowPen, drawX - 2, drawY - 2, size + 4, size + 4);
                    }

                    // Filled circle
                    using (var brush = new SolidBrush(beanColor))
                    {
                        g.FillEllipse(brush, drawX, drawY, size, size);
                    }

                    // White border
                    using (var pen = new Pen(Color.White, 2))
                    {
                        g.DrawEllipse(pen, drawX, drawY, size, size);
                    }
                }
                else if (isCompleted)
                {
                    // Completed: filled but dimmed
                    using (var brush = new SolidBrush(Color.FromArgb(100, beanColor)))
                    {
                        g.FillEllipse(brush, drawX, drawY, size, size);
                    }
                    using (var pen = new Pen(Color.FromArgb(80, 150, 150, 150), 1))
                    {
                        g.DrawEllipse(pen, drawX, drawY, size, size);
                    }

                    // Checkmark
                    using (var checkPen = new Pen(Color.FromArgb(180, Color.White), 2))
                    {
                        int cx = drawX + size / 2;
                        int cy = drawY + size / 2;
                        g.DrawLine(checkPen, cx - 4, cy, cx - 1, cy + 3);
                        g.DrawLine(checkPen, cx - 1, cy + 3, cx + 4, cy - 3);
                    }
                }
                else
                {
                    // Pending: outlined only
                    using (var pen = new Pen(beanColor, 2))
                    {
                        g.DrawEllipse(pen, drawX, drawY, size, size);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the display name for a bean character.
        /// </summary>
        public static string GetBeanName(char beanChar)
        {
            return BeanNames.TryGetValue(beanChar, out var name) ? name : "Unknown";
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
