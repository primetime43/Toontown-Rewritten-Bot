using System;
using System.Drawing;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>
    /// A form that shows the detected fish shadow area for user confirmation.
    /// </summary>
    public class FishCalibrationForm : Form
    {
        private PictureBox pictureBox;
        private Label infoLabel;
        private Button yesButton;
        private Button noButton;
        private Button skipAllButton;

        public bool UserConfirmed { get; private set; } = false;
        public bool SkipAll { get; private set; } = false;

        public FishCalibrationForm(Bitmap screenshot, Point detectionPoint, Color detectionColor, int candidateNumber, int totalCandidates)
        {
            InitializeComponent();
            SetupImage(screenshot, detectionPoint, detectionColor, candidateNumber, totalCandidates);
        }

        private void InitializeComponent()
        {
            this.Text = "Is this a fish shadow?";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.TopMost = true;

            // Picture box for the cropped screenshot
            pictureBox = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(465, 300),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pictureBox);

            // Info label
            infoLabel = new Label
            {
                Location = new Point(10, 320),
                Size = new Size(465, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(infoLabel);

            // Yes button
            yesButton = new Button
            {
                Text = "YES - This is a fish!",
                Location = new Point(10, 370),
                Size = new Size(150, 35),
                BackColor = Color.DarkGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            yesButton.Click += (s, e) => { UserConfirmed = true; this.Close(); };
            this.Controls.Add(yesButton);

            // No button
            noButton = new Button
            {
                Text = "NO - Try next",
                Location = new Point(170, 370),
                Size = new Size(150, 35),
                BackColor = Color.DarkOrange,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            noButton.Click += (s, e) => { UserConfirmed = false; this.Close(); };
            this.Controls.Add(noButton);

            // Skip all button
            skipAllButton = new Button
            {
                Text = "Skip calibration",
                Location = new Point(330, 370),
                Size = new Size(145, 35),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            skipAllButton.Click += (s, e) => { SkipAll = true; this.Close(); };
            this.Controls.Add(skipAllButton);
        }

        private void SetupImage(Bitmap fullScreenshot, Point detectionPoint, Color detectionColor, int candidateNumber, int totalCandidates)
        {
            // Create a cropped view centered on the detection point
            int cropSize = 200;
            int cropX = Math.Max(0, detectionPoint.X - cropSize / 2);
            int cropY = Math.Max(0, detectionPoint.Y - cropSize / 2);

            // Ensure we don't go out of bounds
            if (cropX + cropSize > fullScreenshot.Width)
                cropX = Math.Max(0, fullScreenshot.Width - cropSize);
            if (cropY + cropSize > fullScreenshot.Height)
                cropY = Math.Max(0, fullScreenshot.Height - cropSize);

            int actualWidth = Math.Min(cropSize, fullScreenshot.Width - cropX);
            int actualHeight = Math.Min(cropSize, fullScreenshot.Height - cropY);

            // Create cropped image
            var croppedImage = new Bitmap(actualWidth, actualHeight);
            using (var g = Graphics.FromImage(croppedImage))
            {
                g.DrawImage(fullScreenshot,
                    new Rectangle(0, 0, actualWidth, actualHeight),
                    new Rectangle(cropX, cropY, actualWidth, actualHeight),
                    GraphicsUnit.Pixel);

                // Draw a circle around the detection point
                int relativeX = detectionPoint.X - cropX;
                int relativeY = detectionPoint.Y - cropY;

                using (var pen = new Pen(Color.Lime, 3))
                {
                    g.DrawEllipse(pen, relativeX - 25, relativeY - 25, 50, 50);
                }

                // Draw crosshair
                using (var pen = new Pen(Color.Red, 2))
                {
                    g.DrawLine(pen, relativeX - 15, relativeY, relativeX + 15, relativeY);
                    g.DrawLine(pen, relativeX, relativeY - 15, relativeX, relativeY + 15);
                }
            }

            pictureBox.Image = croppedImage;

            // Update info label
            infoLabel.Text = $"Candidate {candidateNumber} of {totalCandidates}\n" +
                           $"Position: ({detectionPoint.X}, {detectionPoint.Y}) - Color: RGB({detectionColor.R}, {detectionColor.G}, {detectionColor.B})";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            pictureBox.Image?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
