using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Services.FishingLocationsWalking;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>
    /// 4-step wizard for creating custom fishing action files with embedded calibration.
    /// Step 1: Record walk to fisherman
    /// Step 2: Calibrate detection (scan area + pond colors)
    /// Step 3: Record walk back to dock
    /// Step 4: Test & Save
    /// </summary>
    public partial class CustomFishingWizardForm : Form
    {
        // Current wizard step (1-4)
        private int _currentStep = 1;
        private const int TotalSteps = 4;

        // Recording state
        private GlobalKeyboardHook _keyboardHook;
        private bool _isRecording = false;
        private List<RecordedKeyPress> _recordedKeys = new List<RecordedKeyPress>();
        private Stopwatch _recordingStopwatch = new Stopwatch();
        private Keys? _currentKeyHeld = null;
        private long _keyDownTime = 0;

        // Recorded paths
        private List<FishingActionCommand> _walkToFisherman = new List<FishingActionCommand>();
        private List<FishingActionCommand> _walkBackToDock = new List<FishingActionCommand>();

        // Calibration data
        private CalibrationData _calibration = null;

        // File name
        private string _fileName = "";

        private class RecordedKeyPress
        {
            public string Action { get; set; }
            public long DurationMs { get; set; }
        }

        public CustomFishingWizardForm()
        {
            InitializeComponent();
            _keyboardHook = new GlobalKeyboardHook();
            _keyboardHook.KeyPressed += OnGlobalKeyPressed;
            _keyboardHook.KeyReleased += OnGlobalKeyReleased;
            UpdateUI();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomFishingActions));
            this.SuspendLayout();

            // Panel for step indicator
            panelStepIndicator = new Panel();
            panelStepIndicator.Location = new Point(10, 10);
            panelStepIndicator.Size = new Size(560, 50);
            panelStepIndicator.Paint += PanelStepIndicator_Paint;

            // Step title label
            lblStepTitle = new Label();
            lblStepTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblStepTitle.Location = new Point(10, 70);
            lblStepTitle.Size = new Size(560, 30);
            lblStepTitle.Text = "Step 1: Record Walk to Fisherman";

            // Step description label
            lblStepDescription = new Label();
            lblStepDescription.Location = new Point(10, 105);
            lblStepDescription.Size = new Size(560, 50);
            lblStepDescription.Text = "Stand on your fishing dock in TTR. Click 'Start Recording', then walk to the fisherman using arrow keys.";

            // Group box for step content
            groupBoxContent = new GroupBox();
            groupBoxContent.Location = new Point(10, 160);
            groupBoxContent.Size = new Size(560, 230);
            groupBoxContent.Text = "Recording";

            // Recording controls (Step 1 & 3)
            btnStartRecording = new Button();
            btnStartRecording.BackColor = Color.LightGreen;
            btnStartRecording.Location = new Point(20, 30);
            btnStartRecording.Size = new Size(150, 40);
            btnStartRecording.Text = "Start Recording";
            btnStartRecording.UseVisualStyleBackColor = false;
            btnStartRecording.Click += BtnStartRecording_Click;
            groupBoxContent.Controls.Add(btnStartRecording);

            btnStopRecording = new Button();
            btnStopRecording.BackColor = Color.LightCoral;
            btnStopRecording.Enabled = false;
            btnStopRecording.Location = new Point(180, 30);
            btnStopRecording.Size = new Size(150, 40);
            btnStopRecording.Text = "Stop Recording";
            btnStopRecording.UseVisualStyleBackColor = false;
            btnStopRecording.Click += BtnStopRecording_Click;
            groupBoxContent.Controls.Add(btnStopRecording);

            lblRecordingStatus = new Label();
            lblRecordingStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblRecordingStatus.ForeColor = Color.Gray;
            lblRecordingStatus.Location = new Point(20, 80);
            lblRecordingStatus.Size = new Size(520, 25);
            lblRecordingStatus.Text = "Status: Ready to record";
            groupBoxContent.Controls.Add(lblRecordingStatus);

            lblPathPreview = new Label();
            lblPathPreview.Font = new Font("Consolas", 10F);
            lblPathPreview.Location = new Point(20, 110);
            lblPathPreview.Size = new Size(520, 80);
            lblPathPreview.Text = "(Path will appear here as you record)";
            lblPathPreview.AutoEllipsis = true;
            groupBoxContent.Controls.Add(lblPathPreview);

            // Calibration controls (Step 2) - hidden initially
            btnCalibrrateScanArea = new Button();
            btnCalibrrateScanArea.Location = new Point(20, 30);
            btnCalibrrateScanArea.Size = new Size(180, 40);
            btnCalibrrateScanArea.Text = "Calibrate Scan Area";
            btnCalibrrateScanArea.Click += BtnCalibrateScanArea_Click;
            btnCalibrrateScanArea.Visible = false;
            groupBoxContent.Controls.Add(btnCalibrrateScanArea);

            btnCalibratePondColors = new Button();
            btnCalibratePondColors.Location = new Point(210, 30);
            btnCalibratePondColors.Size = new Size(180, 40);
            btnCalibratePondColors.Text = "Calibrate Pond Colors";
            btnCalibratePondColors.Click += BtnCalibratePondColors_Click;
            btnCalibratePondColors.Visible = false;
            groupBoxContent.Controls.Add(btnCalibratePondColors);

            lblCalibrationStatus = new Label();
            lblCalibrationStatus.Location = new Point(20, 80);
            lblCalibrationStatus.Size = new Size(520, 70);
            lblCalibrationStatus.Text = "Click buttons above to calibrate fish detection.\n\nScan Area: Not set\nPond Colors: Not set";
            lblCalibrationStatus.Visible = false;
            groupBoxContent.Controls.Add(lblCalibrationStatus);

            chkSkipCalibration = new CheckBox();
            chkSkipCalibration.Location = new Point(20, 160);
            chkSkipCalibration.Size = new Size(300, 25);
            chkSkipCalibration.Text = "Skip calibration (use global settings)";
            chkSkipCalibration.Visible = false;
            groupBoxContent.Controls.Add(chkSkipCalibration);

            // Test & Save controls (Step 4) - hidden initially
            lblTestInstructions = new Label();
            lblTestInstructions.Location = new Point(20, 25);
            lblTestInstructions.Size = new Size(520, 40);
            lblTestInstructions.Text = "Click 'Test Path' to walk the full path (fisherman → sell → dock). Make sure you're at the fishing dock!";
            lblTestInstructions.Visible = false;
            groupBoxContent.Controls.Add(lblTestInstructions);

            btnTestPath = new Button();
            btnTestPath.Location = new Point(20, 70);
            btnTestPath.Size = new Size(150, 40);
            btnTestPath.Text = "Test Path";
            btnTestPath.Click += BtnTestPath_Click;
            btnTestPath.Visible = false;
            groupBoxContent.Controls.Add(btnTestPath);

            lblFileName = new Label();
            lblFileName.Location = new Point(20, 120);
            lblFileName.Size = new Size(100, 23);
            lblFileName.Text = "File Name:";
            lblFileName.Visible = false;
            groupBoxContent.Controls.Add(lblFileName);

            txtFileName = new TextBox();
            txtFileName.Location = new Point(120, 117);
            txtFileName.Size = new Size(300, 23);
            txtFileName.Visible = false;
            groupBoxContent.Controls.Add(txtFileName);

            btnSave = new Button();
            btnSave.BackColor = Color.LightGreen;
            btnSave.Location = new Point(20, 150);
            btnSave.Size = new Size(150, 40);
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += BtnSave_Click;
            btnSave.Visible = false;
            groupBoxContent.Controls.Add(btnSave);

            // Navigation buttons
            btnPrevious = new Button();
            btnPrevious.Location = new Point(10, 405);
            btnPrevious.Size = new Size(120, 35);
            btnPrevious.Text = "← Previous";
            btnPrevious.Click += BtnPrevious_Click;
            btnPrevious.Enabled = false;

            btnNext = new Button();
            btnNext.Location = new Point(450, 405);
            btnNext.Size = new Size(120, 35);
            btnNext.Text = "Next →";
            btnNext.Click += BtnNext_Click;

            btnCancel = new Button();
            btnCancel.Location = new Point(320, 405);
            btnCancel.Size = new Size(120, 35);
            btnCancel.Text = "Cancel";
            btnCancel.Click += BtnCancel_Click;

            // Form settings
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(580, 455);
            this.Controls.Add(panelStepIndicator);
            this.Controls.Add(lblStepTitle);
            this.Controls.Add(lblStepDescription);
            this.Controls.Add(groupBoxContent);
            this.Controls.Add(btnPrevious);
            this.Controls.Add(btnNext);
            this.Controls.Add(btnCancel);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "CustomFishingWizardForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Custom Fishing Setup Wizard";
            try
            {
                this.Icon = (Icon)resources.GetObject("$this.Icon");
            }
            catch { }

            this.ResumeLayout(false);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            StopRecordingCleanup();
            if (_keyboardHook != null)
            {
                _keyboardHook.KeyPressed -= OnGlobalKeyPressed;
                _keyboardHook.KeyReleased -= OnGlobalKeyReleased;
                _keyboardHook.Dispose();
            }
        }

        private void PanelStepIndicator_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            string[] stepNames = { "Record To", "Calibrate", "Record Back", "Save" };
            int stepWidth = panelStepIndicator.Width / TotalSteps;

            for (int i = 0; i < TotalSteps; i++)
            {
                int x = i * stepWidth + stepWidth / 2;
                int y = 20;
                int radius = 15;

                // Draw circle
                Color circleColor = (i + 1 < _currentStep) ? Color.Green :
                                    (i + 1 == _currentStep) ? Color.Blue : Color.LightGray;
                using (var brush = new SolidBrush(circleColor))
                {
                    g.FillEllipse(brush, x - radius, y - radius, radius * 2, radius * 2);
                }

                // Draw number
                using (var brush = new SolidBrush(Color.White))
                using (var font = new Font("Segoe UI", 10F, FontStyle.Bold))
                {
                    var textSize = g.MeasureString((i + 1).ToString(), font);
                    g.DrawString((i + 1).ToString(), font, brush, x - textSize.Width / 2, y - textSize.Height / 2);
                }

                // Draw step name below
                using (var brush = new SolidBrush(Color.Black))
                using (var font = new Font("Segoe UI", 8F))
                {
                    var textSize = g.MeasureString(stepNames[i], font);
                    g.DrawString(stepNames[i], font, brush, x - textSize.Width / 2, y + radius + 2);
                }

                // Draw connecting line
                if (i < TotalSteps - 1)
                {
                    Color lineColor = (i + 1 < _currentStep) ? Color.Green : Color.LightGray;
                    using (var pen = new Pen(lineColor, 2))
                    {
                        g.DrawLine(pen, x + radius, y, x + stepWidth - radius, y);
                    }
                }
            }
        }

        private void UpdateUI()
        {
            // Update step indicator
            panelStepIndicator.Invalidate();

            // Update navigation buttons
            btnPrevious.Enabled = _currentStep > 1;
            btnNext.Text = _currentStep == TotalSteps ? "Finish" : "Next →";
            btnNext.Visible = _currentStep < TotalSteps;

            // Hide all step-specific controls first
            btnStartRecording.Visible = false;
            btnStopRecording.Visible = false;
            lblRecordingStatus.Visible = false;
            lblPathPreview.Visible = false;
            btnCalibrrateScanArea.Visible = false;
            btnCalibratePondColors.Visible = false;
            lblCalibrationStatus.Visible = false;
            chkSkipCalibration.Visible = false;
            lblTestInstructions.Visible = false;
            btnTestPath.Visible = false;
            lblFileName.Visible = false;
            txtFileName.Visible = false;
            btnSave.Visible = false;

            switch (_currentStep)
            {
                case 1:
                    lblStepTitle.Text = "Step 1: Record Walk to Fisherman";
                    lblStepDescription.Text = $"Stand on your fishing dock in TTR. Click 'Start Recording', then walk to the fisherman using your configured keys ({GameControls.GetMovementBindingSummary()}).";
                    groupBoxContent.Text = "Record Path to Fisherman";
                    btnStartRecording.Visible = true;
                    btnStopRecording.Visible = true;
                    lblRecordingStatus.Visible = true;
                    lblPathPreview.Visible = true;
                    UpdatePathPreviewLabel(_walkToFisherman);
                    break;

                case 2:
                    lblStepTitle.Text = "Step 2: Calibrate Fish Detection";
                    lblStepDescription.Text = "Set up the scan area and pond colors for fish detection at your custom location. You can skip this to use global settings.";
                    groupBoxContent.Text = "Calibration (Optional)";
                    btnCalibrrateScanArea.Visible = true;
                    btnCalibratePondColors.Visible = true;
                    lblCalibrationStatus.Visible = true;
                    chkSkipCalibration.Visible = true;
                    UpdateCalibrationStatus();
                    break;

                case 3:
                    lblStepTitle.Text = "Step 3: Record Walk Back to Dock";
                    lblStepDescription.Text = $"After selling, click 'Start Recording' and walk back using your configured keys ({GameControls.GetMovementBindingSummary()}).";
                    groupBoxContent.Text = "Record Path Back to Dock";
                    btnStartRecording.Visible = true;
                    btnStopRecording.Visible = true;
                    lblRecordingStatus.Visible = true;
                    lblPathPreview.Visible = true;
                    UpdatePathPreviewLabel(_walkBackToDock);
                    break;

                case 4:
                    lblStepTitle.Text = "Step 4: Test & Save";
                    lblStepDescription.Text = "Test your path to make sure it works correctly, then save your custom fishing action file.";
                    groupBoxContent.Text = "Test & Save";
                    lblTestInstructions.Visible = true;
                    btnTestPath.Visible = true;
                    lblFileName.Visible = true;
                    txtFileName.Visible = true;
                    btnSave.Visible = true;
                    if (string.IsNullOrEmpty(txtFileName.Text))
                    {
                        txtFileName.Text = $"Custom_{DateTime.Now:yyyyMMdd_HHmm}";
                    }
                    break;
            }
        }

        private void UpdatePathPreviewLabel(List<FishingActionCommand> actions)
        {
            if (actions == null || actions.Count == 0)
            {
                lblPathPreview.Text = "(Path will appear here as you record)";
                return;
            }

            var preview = new StringBuilder();
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                if (action.Action == "TIME")
                {
                    int ms = DurationFormatter.ParseToMilliseconds(action.Command);
                    preview.Append($"{DurationFormatter.FormatSeconds(ms)} ");
                }
                else if (action.Action == "SELL FISH")
                {
                    preview.Append("[SELL] ");
                }
                else
                {
                    string arrow = DurationFormatter.GetDirectionArrow(action.Action);
                    preview.Append($"{arrow} ");
                }
            }
            lblPathPreview.Text = preview.ToString().TrimEnd();
        }

        private void UpdateCalibrationStatus()
        {
            var sb = new StringBuilder("Click buttons above to calibrate fish detection.\n\n");

            if (_calibration?.ScanArea != null)
            {
                sb.AppendLine($"Scan Area: Set ({_calibration.ScanArea.WidthPercent:F0}% x {_calibration.ScanArea.HeightPercent:F0}%)");
            }
            else
            {
                sb.AppendLine("Scan Area: Not set (will use global settings)");
            }

            if (_calibration?.PondColors != null)
            {
                sb.AppendLine($"Pond Colors: Set (Shadow RGB: {_calibration.PondColors.ShadowR},{_calibration.PondColors.ShadowG},{_calibration.PondColors.ShadowB})");
            }
            else
            {
                sb.AppendLine("Pond Colors: Not set (will use global settings)");
            }

            lblCalibrationStatus.Text = sb.ToString();
        }

        #region Recording

        private void BtnStartRecording_Click(object sender, EventArgs e)
        {
            // Show countdown overlay
            lblRecordingStatus.Text = "Status: Starting countdown...";
            lblRecordingStatus.ForeColor = Color.Orange;

            this.WindowState = FormWindowState.Minimized;

            bool completed = CountdownOverlayForm.ShowCountdown(5);

            if (!completed)
            {
                this.WindowState = FormWindowState.Normal;
                lblRecordingStatus.Text = "Status: Recording cancelled";
                lblRecordingStatus.ForeColor = Color.Gray;
                return;
            }

            if (!_keyboardHook.Start())
            {
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
                lblRecordingStatus.Text = "Status: Could not start keyboard recording";
                lblRecordingStatus.ForeColor = Color.Red;
                MessageBox.Show(
                    this,
                    $"The keyboard recorder could not start (Windows error {_keyboardHook.LastErrorCode}).\n\n" +
                    "Restart the bot and make sure the bot and Toontown are running at the same administrator level.",
                    "Recording Unavailable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Clear previous recording
            _recordedKeys.Clear();
            _currentKeyHeld = null;
            _keyDownTime = 0;

            // Update UI
            btnStartRecording.Enabled = false;
            btnStopRecording.Enabled = true;
            lblRecordingStatus.Text = $"Status: Recording... Use {GameControls.GetMovementBindingSummary()}";
            lblRecordingStatus.ForeColor = Color.Green;

            // Start recording
            _isRecording = true;
            _recordingStopwatch.Restart();
        }

        private void BtnStopRecording_Click(object sender, EventArgs e)
        {
            FinalizeCurrentKey();
            StopRecordingCleanup();

            // Convert to actions
            var actions = ConvertRecordedKeysToActions();

            if (_currentStep == 1)
            {
                _walkToFisherman = actions;
            }
            else if (_currentStep == 3)
            {
                _walkBackToDock = actions;
            }

            UpdatePathPreviewLabel(actions);

            this.WindowState = FormWindowState.Normal;
            this.BringToFront();

            // Update UI
            if (_recordedKeys.Count == 0)
            {
                lblRecordingStatus.Text = "Status: No movements recorded";
                lblRecordingStatus.ForeColor = Color.Red;
                MessageBox.Show(
                    this,
                    $"No movement keys were recorded. Use the configured keys ({GameControls.GetMovementBindingSummary()}) and make sure the bot and Toontown run at the same administrator level.",
                    "Nothing Recorded",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else
            {
                lblRecordingStatus.Text = $"Status: Recorded {_recordedKeys.Count} movements";
                lblRecordingStatus.ForeColor = Color.Blue;
            }

        }

        private void OnGlobalKeyPressed(object sender, Keys key)
        {
            if (!_isRecording) return;

            string action = GetActionFromKey(key);
            if (action == null) return;

            long currentTime = _recordingStopwatch.ElapsedMilliseconds;

            if (_currentKeyHeld.HasValue && _currentKeyHeld.Value != key)
            {
                FinalizeCurrentKey();
            }

            if (!_currentKeyHeld.HasValue)
            {
                _currentKeyHeld = key;
                _keyDownTime = currentTime;
            }
        }

        private void OnGlobalKeyReleased(object sender, Keys key)
        {
            if (!_isRecording) return;

            if (_currentKeyHeld.HasValue && _currentKeyHeld.Value == key)
            {
                FinalizeCurrentKey();
                this.BeginInvoke(new Action(() =>
                {
                    lblRecordingStatus.Text = $"Status: Recording... {_recordedKeys.Count} movements";
                    UpdateLivePreview();
                }));
            }
        }

        private void FinalizeCurrentKey()
        {
            if (!_currentKeyHeld.HasValue) return;

            long currentTime = _recordingStopwatch.ElapsedMilliseconds;
            long duration = currentTime - _keyDownTime;

            if (duration >= 50)
            {
                string action = GetActionFromKey(_currentKeyHeld.Value);
                if (action != null)
                {
                    _recordedKeys.Add(new RecordedKeyPress
                    {
                        Action = action,
                        DurationMs = duration
                    });
                }
            }

            _currentKeyHeld = null;
            _keyDownTime = 0;
        }

        private string GetActionFromKey(Keys key)
        {
            return GameControls.GetMovementAction((int)(key & Keys.KeyCode));
        }

        private void StopRecordingCleanup()
        {
            _isRecording = false;
            _keyboardHook.Stop();
            _recordingStopwatch.Stop();

            btnStartRecording.Enabled = true;
            btnStopRecording.Enabled = false;
        }

        private List<FishingActionCommand> ConvertRecordedKeysToActions()
        {
            var actions = new List<FishingActionCommand>();
            var keys = new FishingActionKeys();

            foreach (var recorded in _recordedKeys)
            {
                // Add movement action
                actions.Add(new FishingActionCommand
                {
                    Action = recorded.Action,
                    Command = keys.GetKeyCodeString(recorded.Action)
                });

                // Add time action
                actions.Add(new FishingActionCommand
                {
                    Action = "TIME",
                    Command = recorded.DurationMs.ToString()
                });
            }

            return actions;
        }

        private void UpdateLivePreview()
        {
            if (_recordedKeys.Count == 0)
            {
                lblPathPreview.Text = "(Path will appear here as you record)";
                return;
            }

            var preview = new StringBuilder();
            int maxItems = 15;
            int startIndex = Math.Max(0, _recordedKeys.Count - maxItems);

            if (startIndex > 0) preview.Append("... ");

            for (int i = startIndex; i < _recordedKeys.Count; i++)
            {
                var recorded = _recordedKeys[i];
                string arrow = DurationFormatter.GetDirectionArrow(recorded.Action);
                string time = DurationFormatter.FormatSeconds(recorded.DurationMs);
                preview.Append($"{arrow} {time} ");
            }

            lblPathPreview.Text = preview.ToString().TrimEnd();
        }

        #endregion

        #region Calibration

        private void BtnCalibrateScanArea_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "This will open a fullscreen overlay on the game window where you can adjust the scan area.\n\n" +
                "Make sure Toontown is running and visible before continuing.",
                "Scan Area Calibration",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (result != DialogResult.OK) return;

            var detector = new FishBubbleDetector("CUSTOM FISHING ACTION");
            var gameRect = CoreFunctionality.GetGameWindowRect();
            var customScanArea = gameRect.IsEmpty ? null
                : CustomScanAreaManager.GetCustomScanArea("CUSTOM FISHING ACTION", gameRect.Width, gameRect.Height);
            var scanArea = customScanArea ?? detector.GetDefaultScanArea();

            if (scanArea.IsEmpty)
            {
                MessageBox.Show("No scan area defined. Make sure the game is running.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var calibrationForm = new ScanAreaCalibrationForm("CUSTOM FISHING ACTION", scanArea))
            {
                calibrationForm.ShowDialog();

                if (calibrationForm.WasSaved)
                {
                    // Get the window dimensions
                    var windowRect = CoreFunctionality.GetGameWindowRect();
                    int windowWidth = windowRect.Width;
                    int windowHeight = windowRect.Height;

                    // Store calibration
                    if (_calibration == null) _calibration = new CalibrationData();
                    var rect = calibrationForm.ResultScanArea;
                    _calibration.ScanArea = new ScanAreaCalibration
                    {
                        XPercent = (float)rect.X / windowWidth * 100f,
                        YPercent = (float)rect.Y / windowHeight * 100f,
                        WidthPercent = (float)rect.Width / windowWidth * 100f,
                        HeightPercent = (float)rect.Height / windowHeight * 100f
                    };

                    UpdateCalibrationStatus();
                }
            }
        }

        private void BtnCalibratePondColors_Click(object sender, EventArgs e)
        {
            using (var colorForm = new PondColorCalibrationForm("CUSTOM FISHING ACTION"))
            {
                colorForm.ShowDialog();

                if (colorForm.DialogResult == DialogResult.OK)
                {
                    // Get saved colors from manager
                    var colors = PondColorManager.GetPondColors("CUSTOM FISHING ACTION");
                    if (colors != null)
                    {
                        if (_calibration == null) _calibration = new CalibrationData();
                        _calibration.PondColors = new PondColorCalibration
                        {
                            WaterR = colors.WaterR,
                            WaterG = colors.WaterG,
                            WaterB = colors.WaterB,
                            ShadowR = colors.ShadowR,
                            ShadowG = colors.ShadowG,
                            ShadowB = colors.ShadowB,
                            ToleranceR = colors.ToleranceR,
                            ToleranceG = colors.ToleranceG,
                            ToleranceB = colors.ToleranceB
                        };
                    }

                    UpdateCalibrationStatus();
                }
            }
        }

        #endregion

        #region Test & Save

        private async void BtnTestPath_Click(object sender, EventArgs e)
        {
            if (_walkToFisherman.Count == 0)
            {
                MessageBox.Show("Please record the path to fisherman first (Step 1).",
                    "Missing Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_walkBackToDock.Count == 0)
            {
                MessageBox.Show("Please record the path back to dock first (Step 3).",
                    "Missing Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Make sure you're standing at your fishing dock!\n\nThe bot will walk to the fisherman, simulate selling, then walk back to the dock.",
                "Ready to Test?",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (result != DialogResult.OK) return;

            btnTestPath.Enabled = false;
            btnTestPath.Text = "Testing...";

            try
            {
                // Build combined actions
                var allActions = new List<FishingActionCommand>();
                allActions.AddRange(_walkToFisherman);
                allActions.Add(new FishingActionCommand { Action = "SELL FISH", Command = "SELL" });
                allActions.AddRange(_walkBackToDock);

                // Save to temp file for testing
                string tempPath = Path.Combine(AppPaths.ExeDirectory, "wizard_test.json");
                CustomFishingActionFileManager.SaveV1Format(allActions, tempPath);

                // Use the same method as "Test Walk Path Only" checkbox
                await FishingService.StartCustomFishingDebugging(tempPath, System.Threading.CancellationToken.None, showCompletionMessage: false);

                MessageBox.Show("Test completed! Did your toon return to the dock correctly?",
                    "Test Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                CoreFunctionality.BringBotWindowToFront();
                MessageBox.Show($"Test failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTestPath.Enabled = true;
                btnTestPath.Text = "Test Path";
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string fileName = txtFileName.Text.Trim();
            if (string.IsNullOrEmpty(fileName))
            {
                MessageBox.Show("Please enter a file name.", "Missing Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Remove invalid characters
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c.ToString(), "");
            }

            if (string.IsNullOrEmpty(fileName))
            {
                MessageBox.Show("Invalid file name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_walkToFisherman.Count == 0)
            {
                MessageBox.Show("Please record the path to fisherman first (Step 1).",
                    "Missing Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_walkBackToDock.Count == 0)
            {
                MessageBox.Show("Please record the path back to dock first (Step 3).",
                    "Missing Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Build the action file
            var actionFile = new CustomFishingActionFile
            {
                Version = 2,
                Name = fileName,
                Description = $"Created with wizard on {DateTime.Now:yyyy-MM-dd}",
                Calibration = chkSkipCalibration.Checked ? null : _calibration,
                Actions = new List<FishingActionCommand>()
            };

            // Add walk to fisherman
            actionFile.Actions.AddRange(_walkToFisherman);

            // Add sell fish
            actionFile.Actions.Add(new FishingActionCommand { Action = "SELL FISH", Command = "SELL" });

            // Add walk back to dock
            actionFile.Actions.AddRange(_walkBackToDock);

            // Save to file
            string folder = CustomFishingActionFileManager.GetCustomActionsFolder();
            string filePath = Path.Combine(folder, $"{fileName}.json");

            if (File.Exists(filePath))
            {
                var overwrite = MessageBox.Show(
                    $"A file named '{fileName}.json' already exists. Overwrite?",
                    "File Exists",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (overwrite != DialogResult.Yes) return;
            }

            if (CustomFishingActionFileManager.Save(actionFile, filePath))
            {
                MessageBox.Show($"Saved successfully!\n\nFile: {fileName}.json\nActions: {actionFile.Actions.Count}\nCalibration: {(actionFile.Calibration != null ? "Embedded" : "Global")}",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _fileName = fileName;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Failed to save file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Navigation

        private void BtnNext_Click(object sender, EventArgs e)
        {
            // Validate current step
            if (_currentStep == 1 && _walkToFisherman.Count == 0)
            {
                var result = MessageBox.Show("You haven't recorded a path to the fisherman yet. Continue anyway?",
                    "No Path Recorded", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes) return;
            }

            if (_currentStep == 3 && _walkBackToDock.Count == 0)
            {
                var result = MessageBox.Show("You haven't recorded a path back to the dock yet. Continue anyway?",
                    "No Path Recorded", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes) return;
            }

            if (_currentStep < TotalSteps)
            {
                _currentStep++;
                UpdateUI();
            }
        }

        private void BtnPrevious_Click(object sender, EventArgs e)
        {
            if (_currentStep > 1)
            {
                _currentStep--;
                UpdateUI();
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to cancel? All progress will be lost.",
                "Cancel Wizard", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        #endregion

        /// <summary>
        /// Gets the file name of the saved action file.
        /// </summary>
        public string SavedFileName => _fileName;

        // UI Controls
        private Panel panelStepIndicator;
        private Label lblStepTitle;
        private Label lblStepDescription;
        private GroupBox groupBoxContent;
        private Button btnStartRecording;
        private Button btnStopRecording;
        private Label lblRecordingStatus;
        private Label lblPathPreview;
        private Button btnCalibrrateScanArea;
        private Button btnCalibratePondColors;
        private Label lblCalibrationStatus;
        private CheckBox chkSkipCalibration;
        private Label lblTestInstructions;
        private Button btnTestPath;
        private Label lblFileName;
        private TextBox txtFileName;
        private Button btnSave;
        private Button btnPrevious;
        private Button btnNext;
        private Button btnCancel;
    }
}
