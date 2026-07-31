using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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
        private bool _turnTimerTemplateMismatch;

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
            // EASY courses (Walk in the Par)
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
            // MEDIUM courses (Hole-some Fun)
            { "At the Drive In", "MEDIUM - At the Drive In" },
            { "Bogey Nights-2", "MEDIUM - Bogey Nights-2" },
            { "Down the Hatch-2", "MEDIUM - Down the Hatch-2" },
            { "Hole in Fun-2", "MEDIUM - Hole in Fun-2" },
            { "Holey Mackerel-2", "MEDIUM - Holey Mackerel-2" },
            { "Holey Mackeral-2", "MEDIUM - Holey Mackerel-2" }, // Alternative spelling
            { "Hot Links-2", "MEDIUM - Hot Links-2" },
            { "No Putts About It", "MEDIUM - No Putts About It" },
            { "Rock and Roll In", "MEDIUM - Rock and Roll In" },
            { "Rock and Roll In-2", "MEDIUM - Rock and Roll In-2" },
            { "Second Wind", "MEDIUM - Second Wind" },
            { "Swing Time-2", "MEDIUM - Swing Time-2" },
            { "Tea Off Time", "MEDIUM - Tea Off Time" },
            // HARD courses (The Hole Kit and Caboodle)
            { "Afternoon Tee-2", "HARD - Afternoon Tee-2" },
            { "At the Drive In-2", "HARD - At the Drive In-2" },
            { "Hole on the Range-2", "HARD - Hole on the Range-2" },
            { "No Putts About It-2", "HARD - No Putts About It-2" },
            { "One Little Birdie-2", "HARD - One Little Birdie-2" },
            { "Peanut Putter-2", "HARD - Peanut Putter-2" },
            { "Second Wind-2", "HARD - Second Wind-2" },
            { "Seeing Green-2", "HARD - Seeing Green-2" },
            { "Swing-A-Long-2", "HARD - Swing-A-Long-2" },
            { "Swing A Long-2", "HARD - Swing-A-Long-2" }, // Alternative without hyphen
            { "Tea Off Time-2", "HARD - Tea Off Time-2" },
            { "Whole in Won", "HARD - Whole in Won" },
            { "Whole in Won-2", "HARD - Whole in Won-2" },
        };

        // Partial matches for fuzzy detection
        private static readonly string[] CourseKeywords = new[]
        {
            "Afternoon", "Hatch", "Hole", "Range", "Holey", "Mackeral", "Mackerel",
            "Hot Links", "Birdie", "Peanut", "Putter", "Seeing", "Green", "Swing",
            "Drive In", "Putts", "Rock", "Roll", "Second", "Wind", "Tea Off", "Whole", "Won",
            "Bogey", "Nights", "-2"
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
            catch (WindowCaptureException)
            {
                throw;
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
            catch (WindowCaptureException)
            {
                throw;
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

                    // If template didn't match, offer recapture on first attempt
                    if (!clicked && HasCloseButtonTemplate() && attempt == 0)
                    {
                        Debug.WriteLine("[GolfDetector] Close button template exists but didn't match, offering recapture...");
                        bool recaptured = PromptForCloseButtonTemplate_Recapture();
                        if (recaptured)
                        {
                            // Retry with the new template
                            using (var retryScreenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                            using (var retryTemplate = new Bitmap(GetCloseButtonTemplatePath()))
                            {
                                if (retryScreenshot != null)
                                {
                                    var retryResult = ImageTemplateMatcher.FindTemplate(retryScreenshot, retryTemplate, 0.70);
                                    if (retryResult.Found)
                                    {
                                        var retryRect = CoreFunctionality.GetGameWindowRect();
                                        var retryPoint = new Point(retryRect.X + retryResult.Center.X, retryRect.Y + retryResult.Center.Y);
                                        Debug.WriteLine($"[GolfDetector] Found close button after recapture at ({retryPoint.X}, {retryPoint.Y})");
                                        CoreFunctionality.DoMouseClickDown(retryPoint);
                                        await Task.Delay(50);
                                        CoreFunctionality.DoMouseClickUp(retryPoint);
                                        clicked = true;
                                    }
                                }
                            }
                        }
                    }

                    // If still not found, try fallback positions
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
                catch (WindowCaptureException)
                {
                    throw;
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

        /// <summary>
        /// Prompts the user to recapture the close button template when it exists but didn't match.
        /// </summary>
        /// <returns>True if template was recaptured successfully</returns>
        private bool PromptForCloseButtonTemplate_Recapture()
        {
            Debug.WriteLine("[GolfDetector] Close button template didn't match, prompting user to recapture...");

            return UIElementManager.Instance.PromptForTemplateCapture(
                CloseButtonTemplateName,
                "The close button template didn't match. Please recapture the red close button (X) on the golf scoreboard.");
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
            catch (WindowCaptureException)
            {
                throw;
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
            catch (WindowCaptureException)
            {
                throw;
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
            catch (WindowCaptureException)
            {
                throw;
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
        private const double TurnTimerTemplateThreshold = 0.80;
        private const int RequiredConsecutiveTurnDetections = 2;
        private const int MissingTemplatePromptDelayMs = 15000;
        private const int ExistingTemplatePromptDelayMs = 30000;
        private const int WaitingStatusIntervalMs = 10000;

        /// <summary>
        /// Checks if the game is ready for the player to swing.
        /// Detects the orange countdown timer in the top-right corner that appears when it's your turn.
        /// </summary>
        public bool IsReadyToSwing()
        {
            _turnTimerTemplateMismatch = false;

            try
            {
                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                {
                    if (screenshot == null) return false;

                    Rectangle timerRegion = GetTurnTimerSearchRegion(screenshot.Size);

                    // Search only where the timer can appear. Full-window matching was both slow and
                    // susceptible to finding a similar orange graphic elsewhere on the course.
                    using (var timerImage = screenshot.Clone(timerRegion, screenshot.PixelFormat))
                    {
                        bool checkedUsableTemplate = false;

                        // Try every captured variant so users can save timer appearances from
                        // different resolutions or countdown frames.
                        foreach (string templatePath in UIElementManager.Instance.GetAllTemplatePaths(TurnTimerTemplateName))
                        {
                            try
                            {
                                using (var template = new Bitmap(templatePath))
                                {
                                    if (template.Width > timerImage.Width || template.Height > timerImage.Height)
                                    {
                                        Debug.WriteLine($"[GolfDetector] Skipping oversized timer template: {Path.GetFileName(templatePath)}");
                                        continue;
                                    }

                                    checkedUsableTemplate = true;
                                    var result = ImageTemplateMatcher.FindTemplate(
                                        timerImage,
                                        template,
                                        TurnTimerTemplateThreshold);
                                    if (result.Found)
                                    {
                                        Debug.WriteLine(
                                            $"[GolfDetector] Timer template found in expected region " +
                                            $"({Path.GetFileName(templatePath)}, confidence: {result.Confidence:P0})");
                                        return true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // A corrupt or obsolete variant should not disable the color fallback
                                // or prevent another valid variant from being checked.
                                Debug.WriteLine($"[GolfDetector] Could not check timer template '{templatePath}': {ex.Message}");
                            }
                        }

                        bool colorDetected = DetectTurnTimerByColor(timerImage);

                        // Once the user has a usable template, treat it as authoritative. Falling
                        // through to the looser color heuristic after a template miss caused random
                        // starts on orange course graphics. If color suggests the real timer is now
                        // visible, use that moment to offer recapture instead of starting the shot.
                        if (checkedUsableTemplate && colorDetected)
                        {
                            _turnTimerTemplateMismatch = true;
                            Debug.WriteLine("[GolfDetector] Orange timer candidate found, but saved templates did not match");
                        }
                        else if (colorDetected)
                        {
                            Debug.WriteLine("[GolfDetector] Timer detected by color in expected region");
                            return true;
                        }
                    }

                    return false;
                }
            }
            catch (WindowCaptureException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GolfDetector] Error checking ready state: {ex.Message}");
                return false;
            }
        }

        private static Rectangle GetTurnTimerSearchRegion(Size screenshotSize)
        {
            // The countdown clock is near the upper-right corner. Keep enough padding for
            // different window sizes while excluding most course graphics from consideration.
            int width = Math.Max(1, (int)Math.Ceiling(screenshotSize.Width * 0.25));
            int height = Math.Max(1, (int)Math.Ceiling(screenshotSize.Height * 0.22));
            return new Rectangle(screenshotSize.Width - width, 0, width, height);
        }

        /// <summary>
        /// Detects the orange countdown timer in the top-right corner.
        /// The timer is an orange/yellow circular clock that appears when it's your turn.
        /// </summary>
        private bool DetectTurnTimerByColor(Bitmap timerImage)
        {
            try
            {
                // Coordinates are relative to the cropped top-right search region. The clock's
                // expected full-window position (~95% across, ~7% down) lands near here.
                int timerCenterX = (int)(timerImage.Width * 0.80);
                int timerCenterY = (int)(timerImage.Height * 0.32);

                int searchRadius = Math.Max(4, (int)(timerImage.Height * 0.30));
                int orangePixelCount = 0;
                int totalSamples = 0;

                // Sample pixels in the timer region
                for (int xOffset = -searchRadius; xOffset <= searchRadius; xOffset += 3)
                {
                    for (int yOffset = -searchRadius; yOffset <= searchRadius; yOffset += 3)
                    {
                        int x = timerCenterX + xOffset;
                        int y = timerCenterY + yOffset;

                        if (x < 0 || x >= timerImage.Width || y < 0 || y >= timerImage.Height)
                            continue;

                        Color pixel = timerImage.GetPixel(x, y);
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
            catch (WindowCaptureException)
            {
                throw;
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
            if (scanIntervalMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scanIntervalMs), "Scan interval must be greater than zero.");
            }

            int consecutiveDetections = 0;
            int nextStatusAtMs = WaitingStatusIntervalMs;
            bool recoveryOffered = false;
            var waitTimer = Stopwatch.StartNew();

            ReportStatus("Waiting for your turn - watching for the orange timer...");

            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsReadyToSwing())
                {
                    consecutiveDetections++;
                    if (consecutiveDetections >= RequiredConsecutiveTurnDetections)
                    {
                        ReportStatus("Turn confirmed - starting shot...");
                        return;
                    }

                    ReportStatus("Turn timer found - confirming...");
                }
                else
                {
                    // A single-frame result can be a transient course graphic. Require the timer
                    // to survive the next scan before allowing the macro to start.
                    if (consecutiveDetections > 0)
                    {
                        ReportStatus("Timer signal was brief - still waiting for your turn...");
                    }
                    consecutiveDetections = 0;
                }

                int elapsedMs = (int)Math.Min(int.MaxValue, waitTimer.ElapsedMilliseconds);

                if (elapsedMs >= nextStatusAtMs)
                {
                    int elapsedSeconds = elapsedMs / 1000;
                    ReportStatus($"Still waiting for your turn ({elapsedSeconds}s) - looking for the orange timer...");
                    nextStatusAtMs += WaitingStatusIntervalMs;
                }

                bool hasTemplate = UIElementManager.Instance.HasTemplate(TurnTimerTemplateName);
                int recoveryDelayMs = hasTemplate ? ExistingTemplatePromptDelayMs : MissingTemplatePromptDelayMs;
                bool templateMismatchNowVisible = hasTemplate && _turnTimerTemplateMismatch;
                if (!recoveryOffered && (templateMismatchNowVisible || elapsedMs >= recoveryDelayMs))
                {
                    recoveryOffered = true;
                    ReportStatus(templateMismatchNowVisible
                        ? "Orange timer found, but the saved template is outdated - recapture help opened"
                        : hasTemplate
                            ? "Turn timer not detected - recapture help opened"
                            : "Turn timer template needed - capture help opened");

                    bool captured = PromptForTurnTimerTemplate(
                        allowRecapture: hasTemplate,
                        timerCandidateVisible: templateMismatchNowVisible);
                    ReportStatus(captured
                        ? "Timer template saved - resuming turn detection..."
                        : "Continuing to wait for the orange turn timer...");

                    // Do not count a frame from before a modal capture dialog toward confirmation.
                    consecutiveDetections = 0;
                }

                await Task.Delay(scanIntervalMs, cancellationToken);
            }
        }

        /// <summary>
        /// Prompts the user to capture the turn timer template.
        /// </summary>
        private bool PromptForTurnTimerTemplate(bool allowRecapture, bool timerCandidateVisible)
        {
            bool hasTemplate = UIElementManager.Instance.HasTemplate(TurnTimerTemplateName);
            if (hasTemplate && !allowRecapture)
            {
                return false;
            }

            // Need to invoke on UI thread
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                Func<bool> showCapture = () =>
                {
                    if (hasTemplate)
                    {
                        var choice = MessageBox.Show(
                            (timerCandidateVisible
                                ? "Auto Golf can see the orange timer, but your saved template did not match.\n\n"
                                : "Auto Golf has not detected your turn timer for 30 seconds.\n\n") +
                            "If the orange countdown clock is visible now, choose Yes and capture only the clock.\n" +
                            "If another player is taking a turn, choose No and Auto Golf will keep waiting.",
                            "Turn Timer Not Detected",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);
                        if (choice != DialogResult.Yes)
                        {
                            return false;
                        }
                    }

                    return TemplateCaptureForm.CaptureTemplate(
                        TurnTimerTemplateName,
                        "Wait until the orange countdown clock is visible, then capture only the clock.\n" +
                        "Avoid including the course background or other UI so turn detection stays reliable.");
                };

                if (mainForm.InvokeRequired)
                {
                    return (bool)mainForm.Invoke(showCapture);
                }

                return showCapture();
            }

            return false;
        }

        /// <summary>
        /// Matches OCR text against known course names.
        /// </summary>
        private string MatchCourseName(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // Clean up the text
            text = text.Replace("\n", " ").Replace("\r", " ").Trim();

            // Iterate longest-name-first so more specific entries claim the match
            // before shorter substrings of them can. Without this, "Whole in Won"
            // wins when the OCR text is actually "Whole in Won-2" because the
            // shorter key is a substring of the longer.
            var candidates = CourseNameToFile.OrderByDescending(kvp => kvp.Key.Length);

            // First, try exact match (substring containment)
            foreach (var kvp in candidates)
            {
                if (text.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[GolfDetector] Exact match found: {kvp.Key} -> {kvp.Value}");
                    return kvp.Value;
                }
            }

            // Second, try fuzzy matching with keywords
            foreach (var kvp in candidates)
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
            string exePath = AppPaths.ExeDirectory;
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
            string exePath = AppPaths.ExeDirectory;
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
