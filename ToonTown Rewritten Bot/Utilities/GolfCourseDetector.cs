using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Views;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Detects the current golf course by reading the course name from the game screen.
    /// </summary>
    public class GolfCourseDetector : IDisposable
    {
        private TextRecognition _ocr;
        private bool _disposed = false;
        private string _pencilButtonTemplatePath;

        /// <summary>
        /// Event raised when detection status changes.
        /// </summary>
        public event Action<string> StatusChanged;

        private void ReportStatus(string status)
        {
            Debug.WriteLine($"[GolfDetector] {status}");
            StatusChanged?.Invoke(status);
        }

        // Known golf course names mapped to their action file names
        private static readonly Dictionary<string, string> CourseNameToFile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // The keys are what we expect to read from the screen (course name)
            // The values are the file names (without .json extension)
            { "Afternoon Tee", "EASY - Afternoon Tee" },
            { "Down the Hatch", "EASY - Down the Hatch" },
            { "Hole In Fun", "EASY - Hole In Fun" },
            { "Hole on the Range", "EASY - Hole on the Range" },
            { "Holey Mackeral", "EASY - Holey Mackeral" },
            { "Holey Mackerel", "EASY - Holey Mackeral" }, // Alternative spelling
            { "Hot Links", "EASY - Hot Links" },
            { "One Little Birdie", "EASY - One Little Birdie" },
            { "Peanut Putter", "EASY - Peanut Putter" },
            { "Seeing Green", "EASY - Seeing green" },
            { "Swing Time", "EASY - Swing Time" },
            { "Swing-A-Long", "EASY - Swing-A-Long" },
            { "Swing A Long", "EASY - Swing-A-Long" }, // Alternative without hyphen
        };

        // Partial matches for fuzzy detection
        private static readonly string[] CourseKeywords = new[]
        {
            "Afternoon", "Hatch", "Hole", "Range", "Holey", "Mackeral", "Mackerel",
            "Hot Links", "Birdie", "Peanut", "Putter", "Seeing", "Green", "Swing"
        };

        public GolfCourseDetector(string pencilButtonTemplatePath = null)
        {
            _pencilButtonTemplatePath = pencilButtonTemplatePath;
        }

        /// <summary>
        /// Initializes the OCR engine. Call this before detecting.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_ocr == null)
            {
                _ocr = await TextRecognition.CreateAsync();
            }
        }

        /// <summary>
        /// Attempts to detect the golf course name from the game screen.
        /// First tries to read from scoreboard (if open), then tries the game screen.
        /// </summary>
        /// <returns>The detected course file name, or null if not found</returns>
        public string DetectCourse()
        {
            if (_ocr == null)
            {
                throw new InvalidOperationException("OCR not initialized. Call InitializeAsync first.");
            }

            try
            {
                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                {
                    if (screenshot == null) return null;

                    // First, check if the scoreboard is open by looking for its distinctive yellow background
                    // The scoreboard header contains the course name (e.g., "WALK IN THE PAR - AFTERNOON TEE")
                    string scoreboardCourse = TryReadScoreboardHeader(screenshot);
                    if (scoreboardCourse != null)
                    {
                        return scoreboardCourse;
                    }

                    // If scoreboard not open, scan the top portion of the screen
                    var regions = new[]
                    {
                        // Top center region (most likely)
                        new Rectangle(screenshot.Width / 4, 0, screenshot.Width / 2, screenshot.Height / 6),
                        // Full top strip
                        new Rectangle(0, 0, screenshot.Width, screenshot.Height / 8),
                        // Upper third center
                        new Rectangle(screenshot.Width / 4, screenshot.Height / 10, screenshot.Width / 2, screenshot.Height / 8),
                    };

                    foreach (var region in regions)
                    {
                        string text = _ocr.ReadTextFromRegion(screenshot, region);

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            System.Diagnostics.Debug.WriteLine($"[GolfDetector] Read text from region: {text}");

                            string matchedFile = MatchCourseName(text);
                            if (matchedFile != null)
                            {
                                return matchedFile;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GolfDetector] Error detecting course: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Tries to read the course name from the scoreboard header.
        /// The scoreboard shows "WALK IN THE PAR - [COURSE NAME]" at the top.
        /// </summary>
        private string TryReadScoreboardHeader(Bitmap screenshot)
        {
            try
            {
                // The scoreboard is a yellow/cream colored popup in the center of the screen
                // The header is at the top of the scoreboard with the course name
                // Look for the scoreboard in the center portion of the screen

                int centerX = screenshot.Width / 2;
                int centerY = screenshot.Height / 2;

                // Scoreboard header region - top portion of center area
                var headerRegion = new Rectangle(
                    screenshot.Width / 4,
                    screenshot.Height / 4,
                    screenshot.Width / 2,
                    screenshot.Height / 6
                );

                string headerText = _ocr.ReadTextFromRegion(screenshot, headerRegion);

                if (!string.IsNullOrWhiteSpace(headerText))
                {
                    System.Diagnostics.Debug.WriteLine($"[GolfDetector] Scoreboard header text: {headerText}");

                    // Look for "WALK IN THE PAR" which indicates the scoreboard
                    string lowerText = headerText.ToLower();
                    if (lowerText.Contains("walk") || lowerText.Contains("par") || lowerText.Contains("hole"))
                    {
                        // Extract course name - it's usually after a dash
                        string matchedFile = MatchCourseName(headerText);
                        if (matchedFile != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[GolfDetector] Found course from scoreboard: {matchedFile}");
                            return matchedFile;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GolfDetector] Error reading scoreboard: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Checks if the scoreboard is currently open by sampling multiple pixels.
        /// </summary>
        public bool IsScoreboardOpen()
        {
            try
            {
                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                {
                    if (screenshot == null) return false;

                    int centerX = screenshot.Width / 2;
                    int centerY = screenshot.Height / 2;

                    // Sample multiple points across the scoreboard area
                    var samplePoints = new[]
                    {
                        new Point(centerX, centerY),
                        new Point(centerX - 50, centerY),
                        new Point(centerX + 50, centerY),
                        new Point(centerX, centerY - 30),
                        new Point(centerX, centerY + 30),
                    };

                    int scoreboardColorCount = 0;
                    foreach (var point in samplePoints)
                    {
                        if (point.X < 0 || point.X >= screenshot.Width ||
                            point.Y < 0 || point.Y >= screenshot.Height)
                            continue;

                        Color pixel = screenshot.GetPixel(point.X, point.Y);

                        // Scoreboard has a cream/yellow/tan background
                        // RGB values are typically high (200+) with R >= G >= B pattern
                        bool isScoreboardColor = pixel.R > 180 && pixel.G > 160 && pixel.B > 100 &&
                                                  pixel.R >= pixel.G && pixel.G >= pixel.B;

                        if (isScoreboardColor)
                            scoreboardColorCount++;
                    }

                    // Require at least 3 out of 5 points to match
                    bool isOpen = scoreboardColorCount >= 3;
                    Debug.WriteLine($"[GolfDetector] IsScoreboardOpen: {scoreboardColorCount}/5 points matched = {isOpen}");
                    return isOpen;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GolfDetector] Error in IsScoreboardOpen: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Closes the scoreboard by clicking the red close button.
        /// Uses template matching to find the close button, with fallback positions.
        /// </summary>
        public async Task CloseScoreboardAsync()
        {
            const int maxAttempts = 3;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (!IsScoreboardOpen())
                {
                    Debug.WriteLine("[GolfDetector] Scoreboard already closed");
                    return;
                }

                try
                {
                    ReportStatus($"Closing scoreboard (attempt {attempt + 1})...");
                    bool clicked = false;

                    // Try template matching first
                    // Prompt for template if it doesn't exist (only on first attempt)
                    if (!HasCloseButtonTemplate() && attempt == 0)
                    {
                        PromptForCloseButtonTemplate();
                    }

                    string closeTemplatePath = GetCloseButtonTemplatePath();
                    if (HasCloseButtonTemplate())
                    {
                        using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                        using (var template = new Bitmap(closeTemplatePath))
                        {
                            if (screenshot != null)
                            {
                                var result = ImageTemplateMatcher.FindTemplate(screenshot, template, 0.70);

                                if (result.Found)
                                {
                                    var gameRect = CoreFunctionality.GetGameWindowRect();
                                    int clickX = gameRect.X + result.Center.X;
                                    int clickY = gameRect.Y + result.Center.Y;

                                    Debug.WriteLine($"[GolfDetector] Found close button at ({clickX}, {clickY})");
                                    var clickPoint = new Point(clickX, clickY);
                                    CoreFunctionality.DoMouseClickDown(clickPoint);
                                    await Task.Delay(50);
                                    CoreFunctionality.DoMouseClickUp(clickPoint);
                                    clicked = true;
                                }
                            }
                        }
                    }

                    // If template not found, try fallback positions
                    if (!clicked)
                    {
                        var gameRect = CoreFunctionality.GetGameWindowRect();

                        // Try different positions for the close button
                        // The close button is typically at the bottom-right of the scoreboard
                        var fallbackPositions = new[]
                        {
                            // Bottom center of scoreboard area
                            new Point(gameRect.X + gameRect.Width / 2, gameRect.Y + (int)(gameRect.Height * 0.68)),
                            // Slightly higher
                            new Point(gameRect.X + gameRect.Width / 2, gameRect.Y + (int)(gameRect.Height * 0.65)),
                            // Bottom right of center
                            new Point(gameRect.X + (int)(gameRect.Width * 0.6), gameRect.Y + (int)(gameRect.Height * 0.68)),
                        };

                        var fallbackPoint = fallbackPositions[attempt % fallbackPositions.Length];
                        Debug.WriteLine($"[GolfDetector] Using fallback position ({fallbackPoint.X}, {fallbackPoint.Y})");
                        CoreFunctionality.DoMouseClickDown(fallbackPoint);
                        await Task.Delay(50);
                        CoreFunctionality.DoMouseClickUp(fallbackPoint);
                    }

                    await Task.Delay(500);

                    // Verify it closed
                    if (!IsScoreboardOpen())
                    {
                        Debug.WriteLine("[GolfDetector] Scoreboard closed successfully");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[GolfDetector] Error closing scoreboard: {ex.Message}");
                }
            }

            Debug.WriteLine("[GolfDetector] Failed to close scoreboard after max attempts");
        }

        private const string CloseButtonTemplateName = "Golf_Close_Button";

        private static string GetCloseButtonTemplatePath()
        {
            return UIElementManager.Instance.GetTemplatePath(CloseButtonTemplateName);
        }

        private static bool HasCloseButtonTemplate()
        {
            return UIElementManager.Instance.HasTemplate(CloseButtonTemplateName);
        }

        /// <summary>
        /// Prompts the user to capture the close button template if it doesn't exist.
        /// Must be called on the UI thread.
        /// </summary>
        /// <returns>True if template exists or was captured successfully</returns>
        private bool PromptForCloseButtonTemplate()
        {
            if (HasCloseButtonTemplate())
            {
                return true;
            }

            Debug.WriteLine("[GolfDetector] Close button template not found, prompting user to capture...");
            ReportStatus("Close button template needed...");

            bool result = false;

            // Need to invoke on UI thread
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.Invoke(new Action(() =>
                    {
                        result = TemplateCaptureForm.CaptureTemplate(
                            CloseButtonTemplateName,
                            "Capture the red close button (X) on the golf scoreboard.\n" +
                            "Open the scoreboard in-game first, then capture the close button.");
                    }));
                }
                else
                {
                    result = TemplateCaptureForm.CaptureTemplate(
                        CloseButtonTemplateName,
                        "Capture the red close button (X) on the golf scoreboard.\n" +
                        "Open the scoreboard in-game first, then capture the close button.");
                }
            }

            return result;
        }

        private const string PencilButtonTemplateName = "Golf_Pencil_Button";

        private static bool HasPencilButtonTemplate()
        {
            return UIElementManager.Instance.HasTemplate(PencilButtonTemplateName);
        }

        private static string GetPencilButtonTemplatePath()
        {
            return UIElementManager.Instance.GetTemplatePath(PencilButtonTemplateName);
        }

        /// <summary>
        /// Prompts the user to capture the pencil button template if not configured.
        /// Must be called on the UI thread.
        /// </summary>
        /// <returns>The path to the template if exists or was captured, null otherwise</returns>
        private string PromptForPencilButtonTemplate()
        {
            // First check if already configured via constructor parameter
            if (!string.IsNullOrEmpty(_pencilButtonTemplatePath) && File.Exists(_pencilButtonTemplatePath))
            {
                return _pencilButtonTemplatePath;
            }

            // Check if template exists in UIElementManager
            if (HasPencilButtonTemplate())
            {
                _pencilButtonTemplatePath = GetPencilButtonTemplatePath();
                return _pencilButtonTemplatePath;
            }

            Debug.WriteLine("[GolfDetector] Pencil button template not found, prompting user to capture...");
            ReportStatus("Pencil button template needed...");

            bool result = false;

            // Need to invoke on UI thread
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.Invoke(new Action(() =>
                    {
                        result = TemplateCaptureForm.CaptureTemplate(
                            PencilButtonTemplateName,
                            "Capture the pencil button (opens the golf scoreboard).\n" +
                            "Make sure you're on a golf course where the pencil button is visible.");
                    }));
                }
                else
                {
                    result = TemplateCaptureForm.CaptureTemplate(
                        PencilButtonTemplateName,
                        "Capture the pencil button (opens the golf scoreboard).\n" +
                        "Make sure you're on a golf course where the pencil button is visible.");
                }
            }

            if (result && HasPencilButtonTemplate())
            {
                _pencilButtonTemplatePath = GetPencilButtonTemplatePath();
                return _pencilButtonTemplatePath;
            }

            return null;
        }

        /// <summary>
        /// Finds the pencil button on screen using template matching.
        /// </summary>
        /// <returns>The center point of the pencil button, or null if not found</returns>
        public Point? FindPencilButton()
        {
            // Try to get template path, prompting user if needed
            string templatePath = PromptForPencilButtonTemplate();

            if (string.IsNullOrEmpty(templatePath))
            {
                ReportStatus("No pencil template configured");
                return null;
            }

            if (!File.Exists(templatePath))
            {
                ReportStatus("Pencil template file not found");
                return null;
            }

            _pencilButtonTemplatePath = templatePath;

            ReportStatus("Searching for pencil button...");

            try
            {
                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                using (var template = new Bitmap(_pencilButtonTemplatePath))
                {
                    if (screenshot == null) return null;

                    var result = ImageTemplateMatcher.FindTemplate(screenshot, template, 0.8);

                    if (result.Found)
                    {
                        ReportStatus($"Found pencil button (confidence: {result.Confidence:P0})");
                        return result.Center;
                    }
                    else
                    {
                        ReportStatus("Pencil button not found on screen");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GolfDetector] Error finding pencil button: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Clicks the pencil button to open the scoreboard.
        /// Uses template matching to find the button location.
        /// </summary>
        public async Task<bool> ClickPencilButtonAsync()
        {
            try
            {
                // Find the pencil button using template matching
                var buttonPos = FindPencilButton();

                if (buttonPos == null)
                {
                    return false;
                }

                ReportStatus("Clicking pencil button...");

                // Convert to screen coordinates
                var gameRect = CoreFunctionality.GetGameWindowRect();
                int clickX = gameRect.X + buttonPos.Value.X;
                int clickY = gameRect.Y + buttonPos.Value.Y;

                var clickPoint = new Point(clickX, clickY);
                CoreFunctionality.DoMouseClickDown(clickPoint);
                await Task.Delay(50);
                CoreFunctionality.DoMouseClickUp(clickPoint);
                await Task.Delay(500); // Wait for scoreboard to open

                Debug.WriteLine($"[GolfDetector] Clicked pencil button at ({clickX}, {clickY})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GolfDetector] Error clicking pencil button: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to detect course by opening the scoreboard, reading it, and closing it.
        /// This is more reliable than reading from the game screen directly.
        /// </summary>
        /// <returns>The detected course file name, or null if not found</returns>
        public async Task<string> DetectCourseViaScoreboardAsync()
        {
            try
            {
                // Click pencil to open scoreboard
                bool clicked = await ClickPencilButtonAsync();

                if (!clicked)
                {
                    return null;
                }

                ReportStatus("Opening scoreboard...");
                await Task.Delay(500); // Wait for animation

                // Try to read the course from scoreboard
                ReportStatus("Reading course name...");
                string course = DetectCourse();

                // Close the scoreboard
                ReportStatus("Closing scoreboard...");
                await CloseScoreboardAsync();

                if (course != null)
                {
                    ReportStatus($"Detected: {course}");
                }

                return course;
            }
            catch (Exception ex)
            {
                ReportStatus($"Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Continuously scans for the golf course name until found or cancelled.
        /// Uses scoreboard detection (clicks pencil button to open scoreboard).
        /// After several failed attempts, prompts user to manually select the course.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="scanIntervalMs">Interval between scans in milliseconds</param>
        /// <returns>The detected course file name</returns>
        public async Task<string> WaitForCourseDetectionAsync(CancellationToken cancellationToken, int scanIntervalMs = 2000)
        {
            const int maxScoreboardAttempts = 3;
            int scoreboardAttempts = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                // Try scoreboard detection a limited number of times
                if (scoreboardAttempts < maxScoreboardAttempts)
                {
                    Debug.WriteLine($"[GolfDetector] Attempting scoreboard detection ({scoreboardAttempts + 1}/{maxScoreboardAttempts})...");
                    ReportStatus($"Reading scoreboard ({scoreboardAttempts + 1}/{maxScoreboardAttempts})...");

                    string course = await DetectCourseViaScoreboardAsync();

                    if (course != null)
                    {
                        Debug.WriteLine($"[GolfDetector] Course detected: {course}");
                        return course;
                    }

                    scoreboardAttempts++;
                    Debug.WriteLine("[GolfDetector] Course not detected from scoreboard");

                    if (scoreboardAttempts < maxScoreboardAttempts)
                    {
                        await Task.Delay(scanIntervalMs, cancellationToken);
                    }
                }
                else
                {
                    // After max attempts, prompt user to select course manually
                    ReportStatus("Could not read course - please select manually");
                    string manualCourse = await PromptForManualCourseSelectionAsync();

                    if (manualCourse != null)
                    {
                        Debug.WriteLine($"[GolfDetector] User selected course: {manualCourse}");
                        return manualCourse;
                    }

                    // User cancelled, reset and try again
                    scoreboardAttempts = 0;
                    await Task.Delay(scanIntervalMs, cancellationToken);
                }
            }

            return null;
        }

        /// <summary>
        /// Prompts the user to manually select the golf course from available action files.
        /// </summary>
        private async Task<string> PromptForManualCourseSelectionAsync()
        {
            var availableCourses = GetAvailableActionFiles();

            if (availableCourses.Count == 0)
            {
                Debug.WriteLine("[GolfDetector] No action files available for manual selection");
                return null;
            }

            string selectedCourse = null;

            // Need to invoke on UI thread
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                Action showDialog = () =>
                {
                    // Bring main form to front so user sees the dialog
                    mainForm.WindowState = FormWindowState.Normal;
                    mainForm.BringToFront();
                    mainForm.Activate();

                    using (var dialog = new Form())
                    {
                        dialog.Text = "Select Golf Course";
                        dialog.Size = new Size(350, 400);
                        dialog.StartPosition = FormStartPosition.CenterScreen;
                        dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                        dialog.MaximizeBox = false;
                        dialog.MinimizeBox = false;
                        dialog.TopMost = true;

                        var label = new Label
                        {
                            Text = "Could not detect course name.\nPlease select the current course:",
                            Location = new Point(10, 10),
                            Size = new Size(320, 40),
                            AutoSize = false
                        };
                        dialog.Controls.Add(label);

                        var listBox = new ListBox
                        {
                            Location = new Point(10, 55),
                            Size = new Size(315, 250)
                        };
                        foreach (var course in availableCourses)
                        {
                            listBox.Items.Add(course);
                        }
                        dialog.Controls.Add(listBox);

                        var okButton = new Button
                        {
                            Text = "Select",
                            DialogResult = DialogResult.OK,
                            Location = new Point(160, 320),
                            Size = new Size(75, 30)
                        };
                        dialog.Controls.Add(okButton);

                        var cancelButton = new Button
                        {
                            Text = "Retry",
                            DialogResult = DialogResult.Cancel,
                            Location = new Point(245, 320),
                            Size = new Size(75, 30)
                        };
                        dialog.Controls.Add(cancelButton);

                        dialog.AcceptButton = okButton;
                        dialog.CancelButton = cancelButton;

                        listBox.DoubleClick += (s, e) =>
                        {
                            if (listBox.SelectedItem != null)
                            {
                                dialog.DialogResult = DialogResult.OK;
                                dialog.Close();
                            }
                        };

                        if (dialog.ShowDialog() == DialogResult.OK && listBox.SelectedItem != null)
                        {
                            selectedCourse = listBox.SelectedItem.ToString();
                        }
                    }
                };

                if (mainForm.InvokeRequired)
                {
                    mainForm.Invoke(showDialog);
                }
                else
                {
                    showDialog();
                }
            }

            return selectedCourse;
        }

        private const string TurnTimerTemplateName = "Golf_Turn_Timer";

        /// <summary>
        /// Checks if the game is ready for the player to swing.
        /// Detects the orange countdown timer in the top-right corner that appears when it's your turn.
        /// </summary>
        public bool IsReadyToSwing()
        {
            try
            {
                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                {
                    if (screenshot == null) return false;

                    // Method 1: Template matching for the turn timer (most reliable if captured)
                    if (UIElementManager.Instance.HasTemplate(TurnTimerTemplateName))
                    {
                        string templatePath = UIElementManager.Instance.GetTemplatePath(TurnTimerTemplateName);
                        using (var template = new Bitmap(templatePath))
                        {
                            var result = ImageTemplateMatcher.FindTemplate(screenshot, template, 0.7);
                            if (result.Found)
                            {
                                Debug.WriteLine($"[GolfDetector] Ready to swing - timer template found (confidence: {result.Confidence:P0})");
                                return true;
                            }
                        }
                    }

                    // Method 2: Detect the orange countdown timer by color in the top-right corner
                    if (DetectTurnTimerByColor(screenshot))
                    {
                        Debug.WriteLine("[GolfDetector] Ready to swing - timer detected by color");
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GolfDetector] Error checking ready state: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Detects the orange countdown timer in the top-right corner.
        /// The timer is an orange/yellow circular clock that appears when it's your turn.
        /// </summary>
        private bool DetectTurnTimerByColor(Bitmap screenshot)
        {
            try
            {
                // The timer is in the top-right corner of the screen
                // Sample the area where the orange clock appears
                int timerCenterX = screenshot.Width - (int)(screenshot.Width * 0.05); // ~5% from right edge
                int timerCenterY = (int)(screenshot.Height * 0.07); // ~7% from top

                int searchRadius = Math.Min(screenshot.Width, screenshot.Height) / 15;
                int orangePixelCount = 0;
                int totalSamples = 0;

                // Sample pixels in the timer region
                for (int xOffset = -searchRadius; xOffset <= searchRadius; xOffset += 3)
                {
                    for (int yOffset = -searchRadius; yOffset <= searchRadius; yOffset += 3)
                    {
                        int x = timerCenterX + xOffset;
                        int y = timerCenterY + yOffset;

                        if (x < 0 || x >= screenshot.Width || y < 0 || y >= screenshot.Height)
                            continue;

                        Color pixel = screenshot.GetPixel(x, y);
                        totalSamples++;

                        // Orange color: high R, medium-high G, low B
                        // The timer is orange/gold colored
                        bool isOrange = pixel.R > 200 && pixel.G > 100 && pixel.G < 200 && pixel.B < 100;
                        // Also check for yellow/gold
                        bool isGold = pixel.R > 200 && pixel.G > 150 && pixel.B < 80;

                        if (isOrange || isGold)
                        {
                            orangePixelCount++;
                        }
                    }
                }

                // If we found enough orange pixels, the timer is visible
                float ratio = totalSamples > 0 ? (float)orangePixelCount / totalSamples : 0;
                bool detected = ratio > 0.20f; // At least 20% of sampled pixels are orange

                if (detected)
                {
                    Debug.WriteLine($"[GolfDetector] Turn timer color ratio: {ratio:P0}");
                }

                return detected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the tee position selection screen is showing.
        /// This appears before the swing prompt.
        /// </summary>
        public bool IsTeeSelectionScreen()
        {
            if (_ocr == null) return false;

            try
            {
                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                {
                    if (screenshot == null) return false;

                    // Scan the bottom portion where tee selection text appears
                    var bottomRegion = new Rectangle(
                        0,
                        screenshot.Height * 2 / 3,
                        screenshot.Width,
                        screenshot.Height / 3
                    );

                    string bottomText = _ocr.ReadTextFromRegion(screenshot, bottomRegion);
                    string lowerText = bottomText.ToLower();

                    // Check for tee selection text
                    return lowerText.Contains("tee") ||
                           lowerText.Contains("position") ||
                           lowerText.Contains("left") && lowerText.Contains("right");
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Waits until the game indicates it's ready to swing.
        /// </summary>
        public async Task WaitUntilReadyToSwingAsync(CancellationToken cancellationToken, int scanIntervalMs = 500)
        {
            int attempts = 0;
            const int promptAfterAttempts = 30; // Prompt for template after ~15 seconds of not detecting

            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsReadyToSwing())
                {
                    return;
                }

                attempts++;

                // If we've been waiting a while and don't have a timer template, offer to capture one
                if (attempts == promptAfterAttempts && !UIElementManager.Instance.HasTemplate(TurnTimerTemplateName))
                {
                    ReportStatus("Having trouble detecting turn - capture template?");
                    PromptForTurnTimerTemplate();
                }

                await Task.Delay(scanIntervalMs, cancellationToken);
            }
        }

        /// <summary>
        /// Prompts the user to capture the turn timer template.
        /// </summary>
        private void PromptForTurnTimerTemplate()
        {
            if (UIElementManager.Instance.HasTemplate(TurnTimerTemplateName))
            {
                return;
            }

            // Need to invoke on UI thread
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.Invoke(new Action(() =>
                    {
                        TemplateCaptureForm.CaptureTemplate(
                            TurnTimerTemplateName,
                            "Capture the orange countdown timer (clock) in the top-right corner.\n" +
                            "This appears when it's your turn to swing.");
                    }));
                }
                else
                {
                    TemplateCaptureForm.CaptureTemplate(
                        TurnTimerTemplateName,
                        "Capture the orange countdown timer (clock) in the top-right corner.\n" +
                        "This appears when it's your turn to swing.");
                }
            }
        }

        /// <summary>
        /// Matches OCR text against known course names.
        /// </summary>
        private string MatchCourseName(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // Clean up the text
            text = text.Replace("\n", " ").Replace("\r", " ").Trim();

            // First, try exact match
            foreach (var kvp in CourseNameToFile)
            {
                if (text.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[GolfDetector] Exact match found: {kvp.Key} -> {kvp.Value}");
                    return kvp.Value;
                }
            }

            // Second, try fuzzy matching with keywords
            foreach (var kvp in CourseNameToFile)
            {
                // Split course name into words and check if most words match
                string[] courseWords = kvp.Key.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
                int matchCount = courseWords.Count(word =>
                    text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);

                // If more than half the words match, consider it a match
                if (matchCount > courseWords.Length / 2)
                {
                    System.Diagnostics.Debug.WriteLine($"[GolfDetector] Fuzzy match found: {kvp.Key} -> {kvp.Value}");
                    return kvp.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets all available golf course action files.
        /// </summary>
        public static List<string> GetAvailableActionFiles()
        {
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string golfActionsPath = Path.Combine(exePath, "Custom Golf Actions");

            if (!Directory.Exists(golfActionsPath))
            {
                return new List<string>();
            }

            return Directory.GetFiles(golfActionsPath, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .ToList();
        }

        /// <summary>
        /// Checks if an action file exists for the given course.
        /// </summary>
        public static bool ActionFileExists(string courseFileName)
        {
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filePath = Path.Combine(exePath, "Custom Golf Actions", courseFileName + ".json");
            return File.Exists(filePath);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _ocr?.Dispose();
                _disposed = true;
            }
        }
    }
}
