using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;

namespace ToonTown_Rewritten_Bot.Views
{
    public class DoodleOverlayForm : Form
    {
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private string _status = "Ready";
        private string _currentAction = "";
        private string _nextAction = "";
        private string _trick = "";
        private int _feedsRemaining = 0;
        private int _scratchesRemaining = 0;
        private bool _unlimited = false;
        private int _totalFeeds = 0;
        private int _totalScratches = 0;
        private int _totalTricks = 0;
        private int _totalCycles = 0;

        private Timer _repositionTimer;

        public DoodleOverlayForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;
            this.StartPosition = FormStartPosition.Manual;
            this.DoubleBuffered = true;
            this.Size = new Size(800, 600);
            this.Location = new Point(100, 100);

            _repositionTimer = new Timer();
            _repositionTimer.Interval = 100;
            _repositionTimer.Tick += (s, e) => RepositionOverGameWindow();
            _repositionTimer.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            int extendedStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
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

        public void UpdateStatus(string status, string currentAction, string nextAction)
        {
            _status = status ?? "Ready";
            _currentAction = currentAction ?? "";
            _nextAction = nextAction ?? "";
            Invalidate();
        }

        public void UpdateProgress(int feedsPerCycle, int scratchesPerCycle, bool unlimited, string trick,
            int totalFeeds = 0, int totalScratches = 0, int totalTricks = 0, int totalCycles = 0)
        {
            _feedsRemaining = feedsPerCycle;
            _scratchesRemaining = scratchesPerCycle;
            _unlimited = unlimited;
            _trick = trick ?? "";
            _totalFeeds = totalFeeds;
            _totalScratches = totalScratches;
            _totalTricks = totalTricks;
            _totalCycles = totalCycles;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            DrawStatusPanel(g);
        }

        private void DrawStatusPanel(Graphics g)
        {
            int panelWidth = 260;
            int panelHeight = 200;
            int panelX = this.Width - panelWidth - 15;
            int panelY = this.Height - panelHeight - 15;

            using (var bgBrush = new SolidBrush(Color.FromArgb(200, 20, 20, 20)))
            using (var borderPen = new Pen(Color.FromArgb(200, 180, 130, 70), 2))
            {
                var panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);
                using (var path = CreateRoundedRectangle(panelRect, 10))
                {
                    g.FillPath(bgBrush, path);
                    g.DrawPath(borderPen, path);
                }
            }

            int textX = panelX + 12;
            int textY = panelY + 10;

            // Title + status
            using (var titleFont = new Font("Segoe UI", 11, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(255, 255, 180, 70)))
            {
                g.DrawString("Doodle Training", titleFont, titleBrush, textX, textY);
            }

            Color statusColor = _status switch
            {
                "Training" => Color.LimeGreen,
                "Feeding" => Color.Orange,
                "Scratching" => Color.Cyan,
                "Trick" => Color.Yellow,
                "Opening Menu" => Color.MediumPurple,
                "Complete" => Color.Cyan,
                "Stopped" => Color.Gray,
                _ => Color.Gray
            };

            using (var statusFont = new Font("Segoe UI", 9))
            using (var statusBrush = new SolidBrush(statusColor))
            {
                string statusDisplay = $"[{_status}]";
                var statusSize = g.MeasureString(statusDisplay, statusFont);
                g.DrawString(statusDisplay, statusFont, statusBrush, panelX + panelWidth - statusSize.Width - 12, textY + 2);
            }

            textY += 26;

            // Trick name
            if (!string.IsNullOrEmpty(_trick) && _trick != "None")
            {
                using (var labelFont = new Font("Segoe UI", 9))
                using (var valueFont = new Font("Segoe UI", 9, FontStyle.Bold))
                using (var labelBrush = new SolidBrush(Color.LightGray))
                using (var valueBrush = new SolidBrush(Color.FromArgb(255, 255, 200, 100)))
                {
                    g.DrawString("Trick:", labelFont, labelBrush, textX, textY);
                    g.DrawString(_trick, valueFont, valueBrush, textX + 45, textY);
                }
                textY += 18;
            }

            // Cycle info
            using (var labelFont = new Font("Segoe UI", 9))
            using (var valueBrush = new SolidBrush(Color.White))
            {
                string cycleInfo = _unlimited ? "Unlimited cycles" : $"Cycles: {_totalTricks}/{_totalCycles}";
                string perCycle = $"  ({_feedsRemaining}F / {_scratchesRemaining}S per cycle)";
                g.DrawString(cycleInfo + perCycle, labelFont, valueBrush, textX, textY);
            }

            textY += 20;

            // Session stats
            if (_totalFeeds > 0 || _totalScratches > 0 || _totalTricks > 0)
            {
                using (var labelFont = new Font("Segoe UI", 8))
                using (var statsBrush = new SolidBrush(Color.FromArgb(255, 150, 220, 150)))
                {
                    string stats = $"Done:  {_totalFeeds} fed  |  {_totalScratches} scratched  |  {_totalTricks} tricks";
                    g.DrawString(stats, labelFont, statsBrush, textX, textY);
                }
                textY += 18;
            }

            textY += 4;

            // Current action (wraps within the panel so long status text isn't clipped)
            using (var labelFont = new Font("Segoe UI", 9))
            using (var actionFont = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var labelBrush = new SolidBrush(Color.LightGray))
            using (var actionBrush = new SolidBrush(Color.Yellow))
            using (var wrapFormat = new StringFormat())
            {
                g.DrawString("Current:", labelFont, labelBrush, textX, textY);
                string actionDisplay = string.IsNullOrEmpty(_currentAction) ? "-" : _currentAction;

                int actionX = textX + 58;
                int actionWidth = panelX + panelWidth - 12 - actionX;
                var actionSize = g.MeasureString(actionDisplay, actionFont, actionWidth, wrapFormat);
                var actionRect = new RectangleF(actionX, textY - 1, actionWidth, actionSize.Height);
                g.DrawString(actionDisplay, actionFont, actionBrush, actionRect, wrapFormat);

                // Advance past however many lines the action wrapped to (at least the original spacing).
                textY += Math.Max(20, (int)Math.Ceiling(actionSize.Height));
            }

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _repositionTimer?.Stop();
                _repositionTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
