using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>
    /// Wizard form for creating custom golf action files with guided steps.
    /// </summary>
    public partial class CustomGolfWizardForm : Form
    {
        private int _currentStep = 1;
        private const int TOTAL_STEPS = 3;

        // Action building
        private List<GolfActionCommand> _actions = new List<GolfActionCommand>();
        private CancellationTokenSource _testCts;

        // UI Controls
        private Label lblStepTitle;
        private Label lblStepDescription;
        private GroupBox groupBoxContent;
        private Button btnBack;
        private Button btnNext;
        private Button btnCancel;
        private ProgressBar progressBar;

        // Step 1: Setup controls
        private TextBox txtFileName;
        private ComboBox cmbDifficulty;
        private TextBox txtCourseName;
        private NumericUpDown numHoleNumber;

        // Step 2: Build sequence controls
        private ListBox lstActions;
        private ComboBox cmbAction;
        private NumericUpDown numDuration;
        private Button btnAddAction;
        private Button btnRemoveAction;
        private Label lblPreview;
        private Label lblSummary;
        private GroupBox groupBoxPresets;

        // Step 3: Test & Save controls
        private Button btnTest;
        private Label lblTestStatus;
        private CheckBox chkSaveAsV2;

        public CustomGolfWizardForm()
        {
            InitializeForm();
            ShowStep(1);
        }

        private void InitializeForm()
        {
            this.Text = "Custom Golf Action Wizard";
            this.Size = new Size(600, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            // Progress bar
            progressBar = new ProgressBar();
            progressBar.Location = new Point(20, 15);
            progressBar.Size = new Size(545, 20);
            progressBar.Minimum = 0;
            progressBar.Maximum = TOTAL_STEPS;
            progressBar.Value = 1;
            this.Controls.Add(progressBar);

            // Step title
            lblStepTitle = new Label();
            lblStepTitle.Location = new Point(20, 45);
            lblStepTitle.Size = new Size(545, 25);
            lblStepTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.Controls.Add(lblStepTitle);

            // Step description
            lblStepDescription = new Label();
            lblStepDescription.Location = new Point(20, 72);
            lblStepDescription.Size = new Size(545, 35);
            lblStepDescription.ForeColor = Color.DarkSlateGray;
            this.Controls.Add(lblStepDescription);

            // Content group box
            groupBoxContent = new GroupBox();
            groupBoxContent.Location = new Point(20, 110);
            groupBoxContent.Size = new Size(545, 290);
            this.Controls.Add(groupBoxContent);

            // Navigation buttons
            btnBack = new Button();
            btnBack.Text = "< Back";
            btnBack.Location = new Point(20, 415);
            btnBack.Size = new Size(100, 35);
            btnBack.Click += BtnBack_Click;
            this.Controls.Add(btnBack);

            btnNext = new Button();
            btnNext.Text = "Next >";
            btnNext.Location = new Point(350, 415);
            btnNext.Size = new Size(100, 35);
            btnNext.Click += BtnNext_Click;
            this.Controls.Add(btnNext);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(465, 415);
            btnCancel.Size = new Size(100, 35);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            // Initialize controls for each step
            InitializeStep1Controls();
            InitializeStep2Controls();
            InitializeStep3Controls();
        }

        #region Step 1: Setup

        private void InitializeStep1Controls()
        {
            // File name
            var lblFileName = new Label();
            lblFileName.Text = "Action File Name:";
            lblFileName.Location = new Point(20, 25);
            lblFileName.AutoSize = true;
            groupBoxContent.Controls.Add(lblFileName);

            txtFileName = new TextBox();
            txtFileName.Location = new Point(20, 45);
            txtFileName.Size = new Size(300, 23);
            txtFileName.Visible = false;
            groupBoxContent.Controls.Add(txtFileName);

            // Course name
            var lblCourseName = new Label();
            lblCourseName.Text = "Course Name (optional):";
            lblCourseName.Location = new Point(20, 80);
            lblCourseName.AutoSize = true;
            lblCourseName.Visible = false;
            groupBoxContent.Controls.Add(lblCourseName);

            txtCourseName = new TextBox();
            txtCourseName.Location = new Point(20, 100);
            txtCourseName.Size = new Size(300, 23);
            txtCourseName.Visible = false;
            groupBoxContent.Controls.Add(txtCourseName);

            // Difficulty
            var lblDifficulty = new Label();
            lblDifficulty.Text = "Difficulty:";
            lblDifficulty.Location = new Point(340, 25);
            lblDifficulty.AutoSize = true;
            lblDifficulty.Visible = false;
            groupBoxContent.Controls.Add(lblDifficulty);

            cmbDifficulty = new ComboBox();
            cmbDifficulty.Location = new Point(340, 45);
            cmbDifficulty.Size = new Size(150, 23);
            cmbDifficulty.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDifficulty.Items.AddRange(new object[] { "", "EASY", "MEDIUM", "HARD" });
            cmbDifficulty.SelectedIndex = 0;
            cmbDifficulty.Visible = false;
            groupBoxContent.Controls.Add(cmbDifficulty);

            // Hole number
            var lblHoleNumber = new Label();
            lblHoleNumber.Text = "Hole Number:";
            lblHoleNumber.Location = new Point(340, 80);
            lblHoleNumber.AutoSize = true;
            lblHoleNumber.Visible = false;
            groupBoxContent.Controls.Add(lblHoleNumber);

            numHoleNumber = new NumericUpDown();
            numHoleNumber.Location = new Point(340, 100);
            numHoleNumber.Size = new Size(80, 23);
            numHoleNumber.Minimum = 0;
            numHoleNumber.Maximum = 3;
            numHoleNumber.Value = 0;
            numHoleNumber.Visible = false;
            groupBoxContent.Controls.Add(numHoleNumber);

            // Tips
            var lblTips = new Label();
            lblTips.Text = "Tips:\n• Use a descriptive name like 'Afternoon Tee Easy Hole 1'\n• Difficulty helps organize your files\n• Hole number (1-3) is optional metadata";
            lblTips.Location = new Point(20, 150);
            lblTips.Size = new Size(500, 100);
            lblTips.ForeColor = Color.DarkSlateGray;
            lblTips.Visible = false;
            lblTips.Name = "lblStep1Tips";
            groupBoxContent.Controls.Add(lblTips);
        }

        #endregion

        #region Step 2: Build Sequence

        private void InitializeStep2Controls()
        {
            // Actions list
            lstActions = new ListBox();
            lstActions.Location = new Point(20, 25);
            lstActions.Size = new Size(250, 150);
            lstActions.Visible = false;
            groupBoxContent.Controls.Add(lstActions);

            // Action selection
            var lblAction = new Label();
            lblAction.Text = "Action:";
            lblAction.Location = new Point(285, 25);
            lblAction.AutoSize = true;
            lblAction.Visible = false;
            lblAction.Name = "lblStep2Action";
            groupBoxContent.Controls.Add(lblAction);

            cmbAction = new ComboBox();
            cmbAction.Location = new Point(285, 45);
            cmbAction.Size = new Size(180, 23);
            cmbAction.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAction.Items.AddRange(new object[] { "SWING POWER", "TURN LEFT", "TURN RIGHT", "DELAY TIME" });
            cmbAction.Visible = false;
            cmbAction.SelectedIndexChanged += CmbAction_SelectedIndexChanged;
            groupBoxContent.Controls.Add(cmbAction);

            // Duration
            var lblDuration = new Label();
            lblDuration.Text = "Duration (ms):";
            lblDuration.Location = new Point(285, 75);
            lblDuration.AutoSize = true;
            lblDuration.Visible = false;
            lblDuration.Name = "lblStep2Duration";
            groupBoxContent.Controls.Add(lblDuration);

            numDuration = new NumericUpDown();
            numDuration.Location = new Point(285, 95);
            numDuration.Size = new Size(100, 23);
            numDuration.Minimum = 1;
            numDuration.Maximum = 30000;
            numDuration.Value = 1000;
            numDuration.Visible = false;
            groupBoxContent.Controls.Add(numDuration);

            // Preset buttons group
            groupBoxPresets = new GroupBox();
            groupBoxPresets.Text = "Presets";
            groupBoxPresets.Location = new Point(285, 125);
            groupBoxPresets.Size = new Size(240, 55);
            groupBoxPresets.Visible = false;
            groupBoxContent.Controls.Add(groupBoxPresets);

            // Power presets
            var presetValues = new[] { ("40%", 1000), ("60%", 1500), ("80%", 2000), ("100%", 2500) };
            int x = 10;
            foreach (var (label, value) in presetValues)
            {
                var btn = new Button();
                btn.Text = label;
                btn.Location = new Point(x, 20);
                btn.Size = new Size(50, 25);
                btn.Tag = value;
                btn.Click += (s, e) => { if (s is Button b && b.Tag is int v) numDuration.Value = v; };
                groupBoxPresets.Controls.Add(btn);
                x += 55;
            }

            // Add/Remove buttons
            btnAddAction = new Button();
            btnAddAction.Text = "Add";
            btnAddAction.Location = new Point(400, 93);
            btnAddAction.Size = new Size(60, 25);
            btnAddAction.Visible = false;
            btnAddAction.Click += BtnAddAction_Click;
            groupBoxContent.Controls.Add(btnAddAction);

            btnRemoveAction = new Button();
            btnRemoveAction.Text = "Remove";
            btnRemoveAction.Location = new Point(465, 93);
            btnRemoveAction.Size = new Size(60, 25);
            btnRemoveAction.Visible = false;
            btnRemoveAction.Click += BtnRemoveAction_Click;
            groupBoxContent.Controls.Add(btnRemoveAction);

            // Preview
            lblPreview = new Label();
            lblPreview.Text = "(No actions)";
            lblPreview.Location = new Point(20, 185);
            lblPreview.Size = new Size(505, 25);
            lblPreview.Font = new Font("Consolas", 9F);
            lblPreview.Visible = false;
            groupBoxContent.Controls.Add(lblPreview);

            // Summary
            lblSummary = new Label();
            lblSummary.Text = "Actions: 0 | Power: - | Turn: Center";
            lblSummary.Location = new Point(20, 215);
            lblSummary.Size = new Size(505, 60);
            lblSummary.Visible = false;
            groupBoxContent.Controls.Add(lblSummary);
        }

        private void CmbAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            string action = cmbAction.SelectedItem?.ToString() ?? "";

            // Update presets based on action
            groupBoxPresets.Controls.Clear();

            if (action == "SWING POWER")
            {
                var presets = new[] { ("40%", 1000), ("60%", 1500), ("80%", 2000), ("100%", 2500) };
                int x = 10;
                foreach (var (label, value) in presets)
                {
                    var btn = new Button();
                    btn.Text = label;
                    btn.Location = new Point(x, 20);
                    btn.Size = new Size(50, 25);
                    btn.Tag = value;
                    btn.Click += (s, ev) => { if (s is Button b && b.Tag is int v) numDuration.Value = v; };
                    groupBoxPresets.Controls.Add(btn);
                    x += 55;
                }
                numDuration.Value = 1500;
            }
            else if (action == "TURN LEFT" || action == "TURN RIGHT")
            {
                var presets = new[] { ("50", 50), ("100", 100), ("150", 150), ("200", 200) };
                int x = 10;
                foreach (var (label, value) in presets)
                {
                    var btn = new Button();
                    btn.Text = label;
                    btn.Location = new Point(x, 20);
                    btn.Size = new Size(50, 25);
                    btn.Tag = value;
                    btn.Click += (s, ev) => { if (s is Button b && b.Tag is int v) numDuration.Value = v; };
                    groupBoxPresets.Controls.Add(btn);
                    x += 55;
                }
                numDuration.Value = 100;
            }
            else if (action == "DELAY TIME")
            {
                var presets = new[] { ("5s", 5000), ("10s", 10000), ("15s", 15000) };
                int x = 10;
                foreach (var (label, value) in presets)
                {
                    var btn = new Button();
                    btn.Text = label;
                    btn.Location = new Point(x, 20);
                    btn.Size = new Size(50, 25);
                    btn.Tag = value;
                    btn.Click += (s, ev) => { if (s is Button b && b.Tag is int v) numDuration.Value = v; };
                    groupBoxPresets.Controls.Add(btn);
                    x += 55;
                }
                numDuration.Value = 15000;
            }
        }

        private void BtnAddAction_Click(object sender, EventArgs e)
        {
            string action = cmbAction.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(action))
            {
                MessageBox.Show("Please select an action.", "No Action", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int duration = (int)numDuration.Value;
            var cmd = new GolfActionCommand
            {
                Action = action,
                Command = action,
                Duration = duration
            };

            _actions.Add(cmd);
            lstActions.Items.Add($"{action} - {GolfDurationFormatter.FormatDuration(action, duration)}");
            UpdateStep2Preview();
        }

        private void BtnRemoveAction_Click(object sender, EventArgs e)
        {
            if (lstActions.SelectedIndex >= 0)
            {
                int idx = lstActions.SelectedIndex;
                _actions.RemoveAt(idx);
                lstActions.Items.RemoveAt(idx);
                UpdateStep2Preview();
            }
        }

        private void UpdateStep2Preview()
        {
            lblPreview.Text = GolfDurationFormatter.BuildSequencePreview(_actions);

            var (totalMs, powerPct, netTurnMs, teePos) = GolfDurationFormatter.GetSequenceSummary(_actions);

            string turnStr = netTurnMs < -50 ? $"Left ({Math.Abs(netTurnMs)}ms)" :
                             netTurnMs > 50 ? $"Right ({netTurnMs}ms)" : "Center";
            string powerStr = powerPct > 0 ? $"{powerPct}%" : "-";

            lblSummary.Text = $"Actions: {_actions.Count} | Total: {totalMs}ms ({totalMs/1000.0:F1}s)\n" +
                              $"Power: {powerStr} | Net Turn: {turnStr}";
        }

        #endregion

        #region Step 3: Test & Save

        private void InitializeStep3Controls()
        {
            // Test button
            btnTest = new Button();
            btnTest.Text = "Test Sequence";
            btnTest.Location = new Point(20, 30);
            btnTest.Size = new Size(150, 40);
            btnTest.BackColor = Color.LightGreen;
            btnTest.Visible = false;
            btnTest.Click += BtnTest_Click;
            groupBoxContent.Controls.Add(btnTest);

            // Test status
            lblTestStatus = new Label();
            lblTestStatus.Text = "Click 'Test Sequence' to preview in game.\nMake sure Toontown is running and you're on the golf course.";
            lblTestStatus.Location = new Point(190, 30);
            lblTestStatus.Size = new Size(330, 50);
            lblTestStatus.Visible = false;
            groupBoxContent.Controls.Add(lblTestStatus);

            // Save options
            chkSaveAsV2 = new CheckBox();
            chkSaveAsV2.Text = "Save with metadata (v2 format)";
            chkSaveAsV2.Location = new Point(20, 100);
            chkSaveAsV2.Size = new Size(250, 25);
            chkSaveAsV2.Checked = true;
            chkSaveAsV2.Visible = false;
            groupBoxContent.Controls.Add(chkSaveAsV2);

            // Final summary
            var lblFinalSummary = new Label();
            lblFinalSummary.Text = "Summary will appear here...";
            lblFinalSummary.Location = new Point(20, 140);
            lblFinalSummary.Size = new Size(500, 120);
            lblFinalSummary.Visible = false;
            lblFinalSummary.Name = "lblFinalSummary";
            groupBoxContent.Controls.Add(lblFinalSummary);
        }

        private async void BtnTest_Click(object sender, EventArgs e)
        {
            if (_actions.Count == 0)
            {
                MessageBox.Show("No actions to test.", "Empty Sequence", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblTestStatus.Text = "Testing sequence... (Press ESC to cancel)";
            btnTest.Enabled = false;

            try
            {
                _testCts = new CancellationTokenSource();

                // Focus game window
                CoreFunctionality.FocusTTRWindow();
                await Task.Delay(1000, _testCts.Token);

                // Execute each action
                var golfKeys = new GolfActionKeys();
                foreach (var action in _actions)
                {
                    if (_testCts.Token.IsCancellationRequested) break;

                    if (action.Action == "DELAY TIME")
                    {
                        await Task.Delay(action.Duration, _testCts.Token);
                    }
                    else if (golfKeys.ActionKeyMap.TryGetValue(action.Action, out var keyCode))
                    {
                        WindowsInput.InputSimulator.SimulateKeyDown(keyCode);
                        await Task.Delay(action.Duration, _testCts.Token);
                        WindowsInput.InputSimulator.SimulateKeyUp(keyCode);
                    }
                }

                lblTestStatus.Text = "Test complete!";
            }
            catch (OperationCanceledException)
            {
                lblTestStatus.Text = "Test cancelled.";
            }
            catch (Exception ex)
            {
                lblTestStatus.Text = $"Test error: {ex.Message}";
            }
            finally
            {
                btnTest.Enabled = true;
                _testCts?.Dispose();
                _testCts = null;
                CoreFunctionality.BringBotWindowToFront();
            }
        }

        #endregion

        #region Navigation

        private void ShowStep(int step)
        {
            _currentStep = step;
            progressBar.Value = step;

            // Update button states
            btnBack.Enabled = step > 1;
            btnNext.Text = step == TOTAL_STEPS ? "Save" : "Next >";

            // Hide all step controls
            HideAllStepControls();

            // Show current step
            switch (step)
            {
                case 1:
                    ShowStep1();
                    break;
                case 2:
                    ShowStep2();
                    break;
                case 3:
                    ShowStep3();
                    break;
            }
        }

        private void HideAllStepControls()
        {
            foreach (Control ctrl in groupBoxContent.Controls)
            {
                ctrl.Visible = false;
            }
        }

        private void ShowStep1()
        {
            lblStepTitle.Text = "Step 1: Setup";
            lblStepDescription.Text = "Enter a name and optional metadata for your golf action file.";
            groupBoxContent.Text = "File Information";

            txtFileName.Visible = true;
            txtCourseName.Visible = true;
            cmbDifficulty.Visible = true;
            numHoleNumber.Visible = true;

            // Show all step 1 labels
            foreach (Control ctrl in groupBoxContent.Controls)
            {
                if (ctrl is Label lbl && (lbl.Text.Contains("Name") || lbl.Text.Contains("Difficulty") ||
                    lbl.Text.Contains("Hole") || lbl.Text.Contains("Course") || lbl.Name == "lblStep1Tips"))
                {
                    ctrl.Visible = true;
                }
            }
        }

        private void ShowStep2()
        {
            lblStepTitle.Text = "Step 2: Build Sequence";
            lblStepDescription.Text = "Add actions to create your golf shot sequence.";
            groupBoxContent.Text = "Action Sequence";

            lstActions.Visible = true;
            cmbAction.Visible = true;
            numDuration.Visible = true;
            btnAddAction.Visible = true;
            btnRemoveAction.Visible = true;
            lblPreview.Visible = true;
            lblSummary.Visible = true;
            groupBoxPresets.Visible = true;

            // Show step 2 labels
            foreach (Control ctrl in groupBoxContent.Controls)
            {
                if (ctrl is Label lbl && (lbl.Name == "lblStep2Action" || lbl.Name == "lblStep2Duration"))
                {
                    ctrl.Visible = true;
                }
            }

            if (cmbAction.SelectedIndex < 0)
                cmbAction.SelectedIndex = 0;

            UpdateStep2Preview();
        }

        private void ShowStep3()
        {
            lblStepTitle.Text = "Step 3: Test & Save";
            lblStepDescription.Text = "Test your sequence in-game, then save the file.";
            groupBoxContent.Text = "Finalize";

            btnTest.Visible = true;
            lblTestStatus.Visible = true;
            chkSaveAsV2.Visible = true;

            // Show final summary
            var lblFinal = groupBoxContent.Controls.Find("lblFinalSummary", false);
            if (lblFinal.Length > 0)
            {
                lblFinal[0].Visible = true;
                var (totalMs, powerPct, netTurnMs, _) = GolfDurationFormatter.GetSequenceSummary(_actions);
                string turnStr = netTurnMs < -50 ? $"Left" : netTurnMs > 50 ? "Right" : "Center";

                lblFinal[0].Text = $"File: {txtFileName.Text}\n" +
                                   $"Course: {(string.IsNullOrEmpty(txtCourseName.Text) ? "(not set)" : txtCourseName.Text)}\n" +
                                   $"Difficulty: {(cmbDifficulty.SelectedIndex > 0 ? cmbDifficulty.SelectedItem : "(not set)")}\n" +
                                   $"Hole: {(numHoleNumber.Value > 0 ? numHoleNumber.Value.ToString() : "(not set)")}\n\n" +
                                   $"Actions: {_actions.Count}\n" +
                                   $"Total Duration: {totalMs}ms ({totalMs/1000.0:F1}s)\n" +
                                   $"Power: {(powerPct > 0 ? $"{powerPct}%" : "-")}\n" +
                                   $"Turn Direction: {turnStr}";
            }
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (_currentStep > 1)
            {
                ShowStep(_currentStep - 1);
            }
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            // Validate current step
            if (!ValidateCurrentStep())
                return;

            if (_currentStep < TOTAL_STEPS)
            {
                ShowStep(_currentStep + 1);
            }
            else
            {
                // Save
                SaveFile();
            }
        }

        private bool ValidateCurrentStep()
        {
            switch (_currentStep)
            {
                case 1:
                    if (string.IsNullOrWhiteSpace(txtFileName.Text))
                    {
                        MessageBox.Show("Please enter a file name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    return true;

                case 2:
                    if (_actions.Count == 0)
                    {
                        MessageBox.Show("Please add at least one action.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    return true;

                default:
                    return true;
            }
        }

        private void SaveFile()
        {
            string folderPath = (string)CoreFunctionality.ManageCustomActionsFolder("Golf", false);
            string fileName = txtFileName.Text.Trim();

            // Remove invalid characters
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c.ToString(), "");
            }

            string filePath = Path.Combine(folderPath, fileName + ".json");

            if (chkSaveAsV2.Checked)
            {
                var file = new CustomGolfActionFile
                {
                    Name = fileName,
                    CourseName = txtCourseName.Text.Trim(),
                    Difficulty = cmbDifficulty.SelectedItem?.ToString() ?? "",
                    HoleNumber = (int)numHoleNumber.Value,
                    Actions = _actions
                };

                if (CustomGolfActionFileManager.Save(file, filePath))
                {
                    MessageBox.Show($"Saved to:\n{filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Failed to save file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                if (CustomGolfActionFileManager.SaveV1(_actions, filePath))
                {
                    MessageBox.Show($"Saved to:\n{filePath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Failed to save file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _testCts?.Cancel();
        }
    }
}
