using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>
    /// Semi-transparent fullscreen overlay that displays a countdown timer.
    /// Used to give users time to switch to TTR before recording starts.
    /// </summary>
    public partial class CountdownOverlayForm : Form
    {
        private int _currentCount;
        private readonly int _startCount;
        private Timer _countdownTimer;
        private bool _wasCancelled = false;

        /// <summary>
        /// Whether the countdown completed (vs was cancelled).
        /// </summary>
        public bool Completed { get; private set; } = false;

        /// <summary>
        /// Creates a countdown overlay starting from the specified number.
        /// </summary>
        /// <param name="startCount">Number to count down from (e.g., 5)</param>
        public CountdownOverlayForm(int startCount = 5)
        {
            _startCount = startCount;
            _currentCount = startCount;
            InitializeComponent();
            SetupForm();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Name = "CountdownOverlayForm";
            this.Text = "Countdown";
            this.ResumeLayout(false);
        }

        private void SetupForm()
        {
            // Make fullscreen and semi-transparent
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.BackColor = Color.Black;
            this.Opacity = 0.7;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0, 0);

            // Enable double buffering for smooth rendering
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            // Handle key press to cancel
            this.KeyPreview = true;
            this.KeyDown += CountdownOverlayForm_KeyDown;

            // Setup countdown timer
            _countdownTimer = new Timer();
            _countdownTimer.Interval = 1000; // 1 second
            _countdownTimer.Tick += CountdownTimer_Tick;
        }

        private void CountdownOverlayForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Allow ESC to cancel the countdown
            if (e.KeyCode == Keys.Escape)
            {
                _wasCancelled = true;
                _countdownTimer.Stop();
                this.Close();
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Start the countdown timer when form is shown
            _countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            _currentCount--;

            if (_currentCount <= 0)
            {
                _countdownTimer.Stop();
                Completed = !_wasCancelled;
                this.Close();
            }
            else
            {
                this.Invalidate(); // Trigger repaint
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            // Calculate center
            int centerX = this.ClientSize.Width / 2;
            int centerY = this.ClientSize.Height / 2;

            // Draw the countdown number
            string countText = _currentCount.ToString();
            using (var font = new Font("Segoe UI", 200, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            {
                var textSize = g.MeasureString(countText, font);
                float textX = centerX - textSize.Width / 2;
                float textY = centerY - textSize.Height / 2 - 50;
                g.DrawString(countText, font, brush, textX, textY);
            }

            // Draw instruction text
            string instructionText = "Switch to TTR now!";
            using (var font = new Font("Segoe UI", 36, FontStyle.Regular))
            using (var brush = new SolidBrush(Color.LightGray))
            {
                var textSize = g.MeasureString(instructionText, font);
                float textX = centerX - textSize.Width / 2;
                float textY = centerY + 100;
                g.DrawString(instructionText, font, brush, textX, textY);
            }

            // Draw cancel instruction
            string cancelText = "Press ESC to cancel";
            using (var font = new Font("Segoe UI", 18, FontStyle.Regular))
            using (var brush = new SolidBrush(Color.Gray))
            {
                var textSize = g.MeasureString(cancelText, font);
                float textX = centerX - textSize.Width / 2;
                float textY = this.ClientSize.Height - 80;
                g.DrawString(cancelText, font, brush, textX, textY);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _countdownTimer?.Dispose();
        }

        /// <summary>
        /// Shows the countdown overlay and waits for it to complete.
        /// </summary>
        /// <param name="countFrom">Number to count down from</param>
        /// <returns>True if countdown completed, false if cancelled</returns>
        public static bool ShowCountdown(int countFrom = 5)
        {
            using (var form = new CountdownOverlayForm(countFrom))
            {
                form.ShowDialog();
                return form.Completed;
            }
        }

        /// <summary>
        /// Shows the countdown overlay asynchronously.
        /// </summary>
        public static async Task<bool> ShowCountdownAsync(int countFrom = 5)
        {
            return await Task.Run(() => ShowCountdown(countFrom));
        }
    }
}
