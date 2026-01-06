using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Services.CustomGolfActions;
using ToonTown_Rewritten_Bot.Utilities;
using ToonTown_Rewritten_Bot.Views;

namespace ToonTown_Rewritten_Bot.Services
{
    /// <summary>
    /// Event args for auto-golf status updates.
    /// </summary>
    public class AutoGolfStatusEventArgs : EventArgs
    {
        public string Status { get; set; }
        public string DetectedCourse { get; set; }
        public int HoleNumber { get; set; }
        public int TotalHoles { get; set; }
    }

    class GolfService
    {
        private static GolfOverlayForm _overlay;

        /// <summary>
        /// Event raised when auto-golf status changes.
        /// </summary>
        public static event EventHandler<AutoGolfStatusEventArgs> AutoGolfStatusChanged;

        public static async Task StartCustomGolfAction(string filePath, CancellationToken cancellationToken, bool showOverlay = true)
        {
            CustomActionsGolf customGolfActions = new CustomActionsGolf(filePath);

            // Create and show overlay if requested
            if (showOverlay)
            {
                ShowOverlay();
                _overlay?.SetTotalSteps(customGolfActions.TotalActions);

                // Subscribe to progress events
                customGolfActions.ProgressChanged += (s, e) =>
                {
                    _overlay?.BeginInvoke(new Action(() =>
                    {
                        _overlay?.UpdateAction(e.CurrentAction, e.NextAction, e.CurrentStep, e.TotalSteps, e.DurationMs);
                    }));
                };

                customGolfActions.StatusChanged += (s, status) =>
                {
                    _overlay?.BeginInvoke(new Action(() =>
                    {
                        _overlay?.SetStatus(status);
                    }));
                };
            }

            try
            {
                await customGolfActions.PerformGolfActions(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Golf operation was canceled by the user.");
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts automatic golf - detects course, waits for ready, then executes actions.
        /// Continues for all holes in the course.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="showOverlay">Whether to show the overlay</param>
        /// <param name="totalHoles">Number of holes to play (default 3)</param>
        public static async Task StartAutoGolfAsync(CancellationToken cancellationToken, bool showOverlay = true, int totalHoles = 3)
        {
            using (var detector = new GolfCourseDetector())
            {
                await detector.InitializeAsync();

                RaiseStatusChanged("Waiting for course...", null, 0, totalHoles);

                if (showOverlay)
                {
                    ShowOverlay();
                    _overlay?.SetStatus("Detecting course...");
                }

                for (int hole = 1; hole <= totalHoles; hole++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Wait for course detection
                    RaiseStatusChanged($"Detecting course (Hole {hole})...", null, hole, totalHoles);
                    UpdateOverlayStatus($"Hole {hole}/{totalHoles} - Detecting course...");

                    string courseFile = await detector.WaitForCourseDetectionAsync(cancellationToken);

                    if (courseFile == null)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        continue;
                    }

                    // Check if action file exists
                    if (!GolfCourseDetector.ActionFileExists(courseFile))
                    {
                        RaiseStatusChanged($"No action file for: {courseFile}", courseFile, hole, totalHoles);
                        UpdateOverlayStatus($"Missing: {courseFile}");
                        await Task.Delay(2000, cancellationToken);
                        continue;
                    }

                    RaiseStatusChanged($"Detected: {courseFile}", courseFile, hole, totalHoles);
                    UpdateOverlayStatus($"Hole {hole}/{totalHoles} - {courseFile}");

                    // Wait for ready to swing
                    RaiseStatusChanged("Waiting for turn...", courseFile, hole, totalHoles);
                    UpdateOverlayStatus($"Waiting for turn...");

                    await detector.WaitUntilReadyToSwingAsync(cancellationToken);

                    // Delay before starting to ensure game is fully ready
                    UpdateOverlayStatus("Get ready...");
                    await Task.Delay(1500, cancellationToken);

                    // Execute golf actions
                    string filePath = GetCustomGolfActionFilePath(courseFile);
                    RaiseStatusChanged($"Executing: {courseFile}", courseFile, hole, totalHoles);

                    await StartCustomGolfAction(filePath, cancellationToken, showOverlay);

                    // Wait a bit between holes
                    if (hole < totalHoles)
                    {
                        RaiseStatusChanged("Waiting for next hole...", courseFile, hole, totalHoles);
                        UpdateOverlayStatus("Waiting for next hole...");
                        await Task.Delay(3000, cancellationToken);
                    }
                }

                RaiseStatusChanged("Auto-golf complete!", null, totalHoles, totalHoles);
                UpdateOverlayStatus("Complete!");
            }
        }

        /// <summary>
        /// Starts automatic golf that plays all 3 holes in a round.
        /// Detects each hole and plays it automatically, then stops.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="showOverlay">Whether to show the overlay</param>
        /// <param name="pencilButtonTemplatePath">Path to the pencil button template image</param>
        public static async Task StartContinuousAutoGolfAsync(CancellationToken cancellationToken, bool showOverlay = true, string pencilButtonTemplatePath = null)
        {
            const int holesPerRound = 3;

            using (var detector = new GolfCourseDetector(pencilButtonTemplatePath))
            {
                await detector.InitializeAsync();

                // Subscribe to detector status changes for overlay updates
                detector.StatusChanged += (status) =>
                {
                    UpdateOverlayStatus(status);
                };

                RaiseStatusChanged("Auto-golf started - waiting for course...", null, 0, holesPerRound);

                if (showOverlay)
                {
                    ShowOverlay();
                    UpdateOverlayStatus("Auto-golf active");
                }

                int holesPlayed = 0;
                string lastPlayedCourse = null;

                while (!cancellationToken.IsCancellationRequested && holesPlayed < holesPerRound)
                {
                    // Wait for course detection
                    RaiseStatusChanged($"Scanning for course ({holesPlayed + 1}/{holesPerRound})...", null, holesPlayed, holesPerRound);
                    UpdateOverlayStatus($"Scanning for hole {holesPlayed + 1}/{holesPerRound}...");

                    string courseFile = await detector.WaitForCourseDetectionAsync(cancellationToken, 1500);

                    if (courseFile == null)
                    {
                        continue;
                    }

                    // Skip if this is the same course we just played (detected too soon)
                    if (courseFile == lastPlayedCourse)
                    {
                        Debug.WriteLine($"[AutoGolf] Same course as last hole ({courseFile}), waiting for next course...");
                        UpdateOverlayStatus("Waiting for next hole to load...");
                        await Task.Delay(2000, cancellationToken);
                        continue;
                    }

                    // Check if action file exists
                    if (!GolfCourseDetector.ActionFileExists(courseFile))
                    {
                        Debug.WriteLine($"[AutoGolf] No action file for: {courseFile}");
                        await Task.Delay(2000, cancellationToken);
                        continue;
                    }

                    RaiseStatusChanged($"Detected: {courseFile}", courseFile, holesPlayed + 1, holesPerRound);
                    UpdateOverlayStatus($"Hole {holesPlayed + 1}/{holesPerRound}: {courseFile}");
                    UpdateOverlayCourseName(courseFile);

                    // Wait for ready to swing
                    RaiseStatusChanged("Waiting for turn...", courseFile, holesPlayed + 1, holesPerRound);
                    UpdateOverlayStatus("Waiting for turn...");

                    await detector.WaitUntilReadyToSwingAsync(cancellationToken);

                    // Delay before starting to ensure game is fully ready
                    UpdateOverlayStatus("Get ready...");
                    await Task.Delay(1500, cancellationToken);

                    // Execute golf actions
                    string filePath = GetCustomGolfActionFilePath(courseFile);
                    holesPlayed++;
                    RaiseStatusChanged($"Playing hole {holesPlayed}/{holesPerRound}: {courseFile}", courseFile, holesPlayed, holesPerRound);

                    try
                    {
                        await StartCustomGolfAction(filePath, cancellationToken, showOverlay);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }

                    // Remember this course so we don't replay it if detected again too soon
                    lastPlayedCourse = courseFile;

                    // Wait before scanning for next hole (if not the last one)
                    if (holesPlayed < holesPerRound)
                    {
                        RaiseStatusChanged("Waiting for next hole...", courseFile, holesPlayed, holesPerRound);
                        UpdateOverlayStatus("Waiting for next hole...");
                        UpdateOverlayCourseName(""); // Clear for next detection
                        await Task.Delay(3000, cancellationToken);
                    }
                }

                // Round complete
                RaiseStatusChanged("Round complete!", null, holesPlayed, holesPerRound);
                UpdateOverlayStatus("Round complete!");
                Debug.WriteLine($"[AutoGolf] Completed {holesPlayed} holes");
            }
        }

        private static void RaiseStatusChanged(string status, string course, int hole, int total)
        {
            AutoGolfStatusChanged?.Invoke(null, new AutoGolfStatusEventArgs
            {
                Status = status,
                DetectedCourse = course,
                HoleNumber = hole,
                TotalHoles = total
            });
        }

        private static void UpdateOverlayStatus(string status)
        {
            if (_overlay != null && !_overlay.IsDisposed)
            {
                _overlay.BeginInvoke(new Action(() => _overlay.SetStatus(status)));
            }
        }

        private static void UpdateOverlayCourseName(string courseName)
        {
            if (_overlay != null && !_overlay.IsDisposed)
            {
                _overlay.BeginInvoke(new Action(() => _overlay.SetCourseName(courseName)));
            }
        }

        /// <summary>
        /// Shows the golf overlay. Thread-safe.
        /// </summary>
        public static void ShowOverlay()
        {
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.BeginInvoke(new Action(ShowOverlayInternal));
                    return;
                }
            }
            ShowOverlayInternal();
        }

        private static void ShowOverlayInternal()
        {
            if (_overlay == null || _overlay.IsDisposed)
            {
                _overlay = new GolfOverlayForm();
                _overlay.Show();
            }
        }

        /// <summary>
        /// Hides and disposes the golf overlay. Thread-safe.
        /// </summary>
        public static void HideOverlay()
        {
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.BeginInvoke(new Action(HideOverlayInternal));
                    return;
                }
            }
            HideOverlayInternal();
        }

        private static void HideOverlayInternal()
        {
            if (_overlay != null && !_overlay.IsDisposed)
            {
                _overlay.Close();
                _overlay.Dispose();
                _overlay = null;
            }
        }

        /// <summary>
        /// Gets whether the overlay is currently visible.
        /// </summary>
        public static bool IsOverlayVisible => _overlay != null && !_overlay.IsDisposed && _overlay.Visible;

        public static GolfActionCommand[] GetCustomGolfActions(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.WriteLine("File does not exist: " + filePath);
                return Array.Empty<GolfActionCommand>();
            }

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<GolfActionCommand[]>(json);
        }

        public static string GetCustomGolfActionFilePath(string fileName)
        {
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(exePath, "Custom Golf Actions", fileName + ".json");
        }
    }
}
