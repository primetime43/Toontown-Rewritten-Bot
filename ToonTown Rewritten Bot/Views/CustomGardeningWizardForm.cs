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
    /// Wizard form for creating custom gardening action files with guided steps.
    /// </summary>
    public partial class CustomGardeningWizardForm : Form
    {
        private int _currentStep = 1;
        private const int TOTAL_STEPS = 4;

        // Action building
        private List<GardeningActionCommand> _actions = new List<GardeningActionCommand>();
        private CancellationTokenSource _testCts;
        private GardeningActionKeys _gardeningKeys = new GardeningActionKeys();

        // Flower database
        private readonly Dictionary<string, string> _flowerDatabase = new Dictionary<string, string>
        {
            { "Laff-o-dil", "g" }, { "Dandy Pansy", "o" }, { "What-in Carnation", "i" },
            { "School Daisy", "y" }, { "Lily-of-the-Alley", "c" },
            { "Daffy Dill", "gc" }, { "Chim Pansy", "oc" }, { "Instant Carnation", "iy" },
            { "Lazy Daisy", "yc" }, { "Livered Lily", "cs" },
            { "Tinted Daffodil", "goc" }, { "Potsen Pansy", "oiy" }, { "Hybrid Carnation", "iyc" },
            { "Freshasa Daisy", "ycs" }, { "Tiger Lily", "cso" },
            { "Corn Rose", "rcso" }, { "Giraff-o-dil", "goiy" }, { "Marzi Pansy", "oiyc" },
            { "Delishish Carnation", "iycs" }, { "Whoopsie Daisy", "ycso" },
            { "Time and a Half Rose", "rcsog" }, { "Freshasa Daffodil", "goiyc" },
            { "Chili Pansy", "oiycs" }, { "Stinking Carnation", "iycso" }, { "Upsy Daisy", "ycsog" },
            { "Onelip", "rcsogi" }, { "Side Daisy", "rcsogy" }, { "Summer's Last Rose", "rcsoiy" },
            { "Crazy Daisy", "ycsogu" }, { "Tinted Rose", "icsogy" },
            { "Twolip", "rcsogio" }, { "Midsummer's Daisy", "rcsogiy" }, { "Indubitab Rose", "rcsogyu" },
            { "Hazy Daisy", "ycsogub" }, { "Car Petunia", "bcsogiy" },
            { "Istilla Rose", "rbuubbib" }, { "Threelip", "uyyuyouy" }, { "Platoonia", "bcsogiyb" },
            { "Muddy Daisy", "ycsogubi" }, { "Model Carnation", "iycsoiyc" }
        };

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
        private ComboBox cmbLocation;
        private TextBox txtDescription;

        // Step 2: Add flower beds
        private ListBox lstFlowerBeds;
        private ComboBox cmbFlower;
        private NumericUpDown numWaterCount;
        private Button btnAddFlowerBed;
        private Button btnRemoveFlowerBed;
        private CheckBox chkRemoveFirst;

        // Step 3: Add walking between beds
        private ListBox lstActions;
        private ComboBox cmbWalkAction;
        private NumericUpDown numWalkDuration;
        private Button btnAddWalk;
        private Button btnRemoveAction;
        private Label lblPreview;

        // Step 4: Test & Save
        private Button btnTest;
        private Label lblTestStatus;
        private CheckBox chkSaveAsV2;
        private Label lblFinalSummary;

        public CustomGardeningWizardForm()
        {
            InitializeForm();
            ShowStep(1);
        }

        private void InitializeForm()
        {
            this.Text = "Custom Gardening Wizard";
            this.Size = new Size(650, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            // Progress bar
            progressBar = new ProgressBar();
            progressBar.Location = new Point(20, 15);
            progressBar.Size = new Size(595, 20);
            progressBar.Minimum = 0;
            progressBar.Maximum = TOTAL_STEPS;
            progressBar.Value = 1;
            this.Controls.Add(progressBar);

            // Step title
            lblStepTitle = new Label();
            lblStepTitle.Location = new Point(20, 45);
            lblStepTitle.Size = new Size(595, 25);
            lblStepTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.Controls.Add(lblStepTitle);

            // Step description
            lblStepDescription = new Label();
            lblStepDescription.Location = new Point(20, 72);
            lblStepDescription.Size = new Size(595, 35);
            lblStepDescription.ForeColor = Color.DarkSlateGray;
            this.Controls.Add(lblStepDescription);

            // Content group box
            groupBoxContent = new GroupBox();
            groupBoxContent.Location = new Point(20, 110);
            groupBoxContent.Size = new Size(595, 310);
            this.Controls.Add(groupBoxContent);

            // Navigation buttons
            btnBack = new Button();
            btnBack.Text = "< Back";
            btnBack.Location = new Point(20, 435);
            btnBack.Size = new Size(100, 35);
            btnBack.Click += BtnBack_Click;
            this.Controls.Add(btnBack);

            btnNext = new Button();
            btnNext.Text = "Next >";
            btnNext.Location = new Point(400, 435);
            btnNext.Size = new Size(100, 35);
            btnNext.Click += BtnNext_Click;
            this.Controls.Add(btnNext);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(515, 435);
            btnCancel.Size = new Size(100, 35);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            // Initialize controls for each step
            InitializeStep1Controls();
            InitializeStep2Controls();
            InitializeStep3Controls();
            InitializeStep4Controls();
        }

        #region Step 1: Setup

        private void InitializeStep1Controls()
        {
            // File name
            var lblFileName = new Label();
            lblFileName.Text = "Routine Name:";
            lblFileName.Location = new Point(20, 25);
            lblFileName.AutoSize = true;
            lblFileName.Name = "lblStep1FileName";
            groupBoxContent.Controls.Add(lblFileName);

            txtFileName = new TextBox();
            txtFileName.Location = new Point(20, 45);
            txtFileName.Size = new Size(300, 23);
            txtFileName.Visible = false;
            groupBoxContent.Controls.Add(txtFileName);

            // Location
            var lblLocation = new Label();
            lblLocation.Text = "Location (optional):";
            lblLocation.Location = new Point(340, 25);
            lblLocation.AutoSize = true;
            lblLocation.Name = "lblStep1Location";
            groupBoxContent.Controls.Add(lblLocation);

            cmbLocation = new ComboBox();
            cmbLocation.Location = new Point(340, 45);
            cmbLocation.Size = new Size(200, 23);
            cmbLocation.DropDownStyle = ComboBoxStyle.DropDown;
            cmbLocation.Items.AddRange(new object[] { "", "Front Yard", "Back Yard", "Left Side", "Right Side" });
            cmbLocation.Visible = false;
            groupBoxContent.Controls.Add(cmbLocation);

            // Description
            var lblDesc = new Label();
            lblDesc.Text = "Description (optional):";
            lblDesc.Location = new Point(20, 85);
            lblDesc.AutoSize = true;
            lblDesc.Name = "lblStep1Desc";
            groupBoxContent.Controls.Add(lblDesc);

            txtDescription = new TextBox();
            txtDescription.Location = new Point(20, 105);
            txtDescription.Size = new Size(520, 60);
            txtDescription.Multiline = true;
            txtDescription.Visible = false;
            groupBoxContent.Controls.Add(txtDescription);

            // Tips
            var lblTips = new Label();
            lblTips.Text = "Tips:\n" +
                           "• Use a descriptive name like 'Front Yard 4 Beds'\n" +
                           "• Location helps organize your routines\n" +
                           "• Description can include notes about positioning";
            lblTips.Location = new Point(20, 180);
            lblTips.Size = new Size(520, 100);
            lblTips.ForeColor = Color.DarkSlateGray;
            lblTips.Name = "lblStep1Tips";
            groupBoxContent.Controls.Add(lblTips);
        }

        #endregion

        #region Step 2: Flower Beds

        private void InitializeStep2Controls()
        {
            // Flower beds list
            lstFlowerBeds = new ListBox();
            lstFlowerBeds.Location = new Point(20, 25);
            lstFlowerBeds.Size = new Size(280, 150);
            lstFlowerBeds.Visible = false;
            groupBoxContent.Controls.Add(lstFlowerBeds);

            // Flower selection
            var lblFlower = new Label();
            lblFlower.Text = "Flower:";
            lblFlower.Location = new Point(320, 25);
            lblFlower.AutoSize = true;
            lblFlower.Name = "lblStep2Flower";
            groupBoxContent.Controls.Add(lblFlower);

            cmbFlower = new ComboBox();
            cmbFlower.Location = new Point(320, 45);
            cmbFlower.Size = new Size(200, 23);
            cmbFlower.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var flower in _flowerDatabase.Keys)
            {
                cmbFlower.Items.Add(flower);
            }
            cmbFlower.Visible = false;
            groupBoxContent.Controls.Add(cmbFlower);

            // Water count
            var lblWater = new Label();
            lblWater.Text = "Water count:";
            lblWater.Location = new Point(320, 80);
            lblWater.AutoSize = true;
            lblWater.Name = "lblStep2Water";
            groupBoxContent.Controls.Add(lblWater);

            numWaterCount = new NumericUpDown();
            numWaterCount.Location = new Point(320, 100);
            numWaterCount.Size = new Size(80, 23);
            numWaterCount.Minimum = 1;
            numWaterCount.Maximum = 10;
            numWaterCount.Value = 3;
            numWaterCount.Visible = false;
            groupBoxContent.Controls.Add(numWaterCount);

            // Remove first checkbox
            chkRemoveFirst = new CheckBox();
            chkRemoveFirst.Text = "Remove existing plant first";
            chkRemoveFirst.Location = new Point(320, 130);
            chkRemoveFirst.Size = new Size(200, 25);
            chkRemoveFirst.Visible = false;
            groupBoxContent.Controls.Add(chkRemoveFirst);

            // Add/Remove buttons
            btnAddFlowerBed = new Button();
            btnAddFlowerBed.Text = "Add Flower Bed";
            btnAddFlowerBed.Location = new Point(320, 165);
            btnAddFlowerBed.Size = new Size(120, 30);
            btnAddFlowerBed.Click += BtnAddFlowerBed_Click;
            btnAddFlowerBed.Visible = false;
            groupBoxContent.Controls.Add(btnAddFlowerBed);

            btnRemoveFlowerBed = new Button();
            btnRemoveFlowerBed.Text = "Remove";
            btnRemoveFlowerBed.Location = new Point(450, 165);
            btnRemoveFlowerBed.Size = new Size(80, 30);
            btnRemoveFlowerBed.Click += BtnRemoveFlowerBed_Click;
            btnRemoveFlowerBed.Visible = false;
            groupBoxContent.Controls.Add(btnRemoveFlowerBed);

            // Tips
            var lblStep2Tips = new Label();
            lblStep2Tips.Text = "Add each flower bed you want to tend.\n" +
                                "The wizard will ask you to add walking between beds in the next step.";
            lblStep2Tips.Location = new Point(20, 210);
            lblStep2Tips.Size = new Size(520, 50);
            lblStep2Tips.ForeColor = Color.DarkSlateGray;
            lblStep2Tips.Name = "lblStep2Tips";
            groupBoxContent.Controls.Add(lblStep2Tips);
        }

        private void BtnAddFlowerBed_Click(object sender, EventArgs e)
        {
            string flower = cmbFlower.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(flower))
            {
                MessageBox.Show("Please select a flower.", "No Flower", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int waterCount = (int)numWaterCount.Value;
            bool removeFirst = chkRemoveFirst.Checked;

            string display = removeFirst
                ? $"[Remove] → Plant {flower} → Water {waterCount}x"
                : $"Plant {flower} → Water {waterCount}x";

            lstFlowerBeds.Items.Add(display);
        }

        private void BtnRemoveFlowerBed_Click(object sender, EventArgs e)
        {
            if (lstFlowerBeds.SelectedIndex >= 0)
            {
                lstFlowerBeds.Items.RemoveAt(lstFlowerBeds.SelectedIndex);
            }
        }

        #endregion

        #region Step 3: Walking/Actions

        private void InitializeStep3Controls()
        {
            // Full actions list
            lstActions = new ListBox();
            lstActions.Location = new Point(20, 25);
            lstActions.Size = new Size(340, 180);
            lstActions.Visible = false;
            groupBoxContent.Controls.Add(lstActions);

            // Walk action
            var lblWalk = new Label();
            lblWalk.Text = "Action:";
            lblWalk.Location = new Point(380, 25);
            lblWalk.AutoSize = true;
            lblWalk.Name = "lblStep3Walk";
            groupBoxContent.Controls.Add(lblWalk);

            cmbWalkAction = new ComboBox();
            cmbWalkAction.Location = new Point(380, 45);
            cmbWalkAction.Size = new Size(150, 23);
            cmbWalkAction.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbWalkAction.Items.AddRange(new object[] {
                "WALK FORWARD", "WALK BACKWARD", "WALK LEFT", "WALK RIGHT",
                "TURN LEFT", "TURN RIGHT", "DELAY"
            });
            cmbWalkAction.Visible = false;
            groupBoxContent.Controls.Add(cmbWalkAction);

            // Duration
            var lblDur = new Label();
            lblDur.Text = "Duration (ms):";
            lblDur.Location = new Point(380, 80);
            lblDur.AutoSize = true;
            lblDur.Name = "lblStep3Duration";
            groupBoxContent.Controls.Add(lblDur);

            numWalkDuration = new NumericUpDown();
            numWalkDuration.Location = new Point(380, 100);
            numWalkDuration.Size = new Size(100, 23);
            numWalkDuration.Minimum = 100;
            numWalkDuration.Maximum = 30000;
            numWalkDuration.Value = 1000;
            numWalkDuration.Visible = false;
            groupBoxContent.Controls.Add(numWalkDuration);

            // Add/Remove buttons
            btnAddWalk = new Button();
            btnAddWalk.Text = "Insert Walk";
            btnAddWalk.Location = new Point(380, 135);
            btnAddWalk.Size = new Size(100, 30);
            btnAddWalk.Click += BtnAddWalk_Click;
            btnAddWalk.Visible = false;
            groupBoxContent.Controls.Add(btnAddWalk);

            btnRemoveAction = new Button();
            btnRemoveAction.Text = "Remove";
            btnRemoveAction.Location = new Point(490, 135);
            btnRemoveAction.Size = new Size(80, 30);
            btnRemoveAction.Click += BtnRemoveAction_Click;
            btnRemoveAction.Visible = false;
            groupBoxContent.Controls.Add(btnRemoveAction);

            // Preview
            lblPreview = new Label();
            lblPreview.Text = "(Preview will appear here)";
            lblPreview.Location = new Point(20, 215);
            lblPreview.Size = new Size(550, 60);
            lblPreview.Font = new Font("Consolas", 9F);
            lblPreview.Name = "lblStep3Preview";
            groupBoxContent.Controls.Add(lblPreview);
        }

        private void BtnAddWalk_Click(object sender, EventArgs e)
        {
            string action = cmbWalkAction.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(action))
            {
                MessageBox.Show("Please select a walk action.", "No Action", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int duration = (int)numWalkDuration.Value;

            var cmd = new GardeningActionCommand
            {
                Action = action,
                Duration = duration
            };

            // Insert at selected position or at end
            int insertIndex = lstActions.SelectedIndex >= 0 ? lstActions.SelectedIndex + 1 : lstActions.Items.Count;
            _actions.Insert(insertIndex, cmd);

            // Refresh list
            RefreshActionsList();
        }

        private void BtnRemoveAction_Click(object sender, EventArgs e)
        {
            if (lstActions.SelectedIndex >= 0)
            {
                int idx = lstActions.SelectedIndex;
                if (idx < _actions.Count)
                {
                    _actions.RemoveAt(idx);
                    RefreshActionsList();
                }
            }
        }

        private void RefreshActionsList()
        {
            lstActions.Items.Clear();
            foreach (var action in _actions)
            {
                string display = $"{action.Action} - {GardeningDurationFormatter.FormatDuration(action)}";
                lstActions.Items.Add(display);
            }

            lblPreview.Text = GardeningDurationFormatter.BuildSequencePreview(_actions);
        }

        #endregion

        #region Step 4: Test & Save

        private void InitializeStep4Controls()
        {
            // Test button
            btnTest = new Button();
            btnTest.Text = "Test Routine";
            btnTest.Location = new Point(20, 30);
            btnTest.Size = new Size(150, 40);
            btnTest.BackColor = Color.LightGreen;
            btnTest.Click += BtnTest_Click;
            btnTest.Visible = false;
            groupBoxContent.Controls.Add(btnTest);

            // Test status
            lblTestStatus = new Label();
            lblTestStatus.Text = "Click 'Test Routine' to run in game.\n" +
                                 "Make sure Toontown is running and you're at a flower bed.";
            lblTestStatus.Location = new Point(190, 30);
            lblTestStatus.Size = new Size(370, 50);
            lblTestStatus.Name = "lblStep4Status";
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
            lblFinalSummary = new Label();
            lblFinalSummary.Text = "Summary will appear here...";
            lblFinalSummary.Location = new Point(20, 140);
            lblFinalSummary.Size = new Size(540, 140);
            lblFinalSummary.Name = "lblStep4Summary";
            groupBoxContent.Controls.Add(lblFinalSummary);
        }

        private async void BtnTest_Click(object sender, EventArgs e)
        {
            if (_actions.Count == 0)
            {
                MessageBox.Show("No actions to test.", "Empty Routine", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblTestStatus.Text = "Testing routine... (Close TTR window to cancel)";
            btnTest.Enabled = false;

            try
            {
                _testCts = new CancellationTokenSource();

                // Focus game window
                CoreFunctionality.FocusTTRWindow();
                await Task.Delay(1000, _testCts.Token);

                // Execute each action
                foreach (var action in _actions)
                {
                    if (_testCts.Token.IsCancellationRequested) break;

                    switch (action.Action)
                    {
                        case "WALK FORWARD":
                        case "WALK BACKWARD":
                        case "WALK LEFT":
                        case "WALK RIGHT":
                        case "TURN LEFT":
                        case "TURN RIGHT":
                            if (_gardeningKeys.ActionKeyMap.TryGetValue(action.Action, out var keyCode))
                            {
                                WindowsInput.InputSimulator.SimulateKeyDown(keyCode);
                                await Task.Delay(action.Duration, _testCts.Token);
                                WindowsInput.InputSimulator.SimulateKeyUp(keyCode);
                            }
                            break;

                        case "DELAY":
                            await Task.Delay(action.Duration, _testCts.Token);
                            break;

                        case "PLANT FLOWER":
                            // For testing, just show a message
                            lblTestStatus.Text = $"Would plant: {action.FlowerName}";
                            await Task.Delay(2000, _testCts.Token);
                            break;

                        case "WATER PLANT":
                            lblTestStatus.Text = $"Would water: {action.WaterCount}x";
                            await Task.Delay(1000, _testCts.Token);
                            break;

                        case "REMOVE PLANT":
                            lblTestStatus.Text = "Would remove plant";
                            await Task.Delay(1000, _testCts.Token);
                            break;
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

            btnBack.Enabled = step > 1;
            btnNext.Text = step == TOTAL_STEPS ? "Save" : "Next >";

            HideAllStepControls();

            switch (step)
            {
                case 1: ShowStep1(); break;
                case 2: ShowStep2(); break;
                case 3: ShowStep3(); break;
                case 4: ShowStep4(); break;
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
            lblStepDescription.Text = "Enter a name and description for your gardening routine.";
            groupBoxContent.Text = "Routine Information";

            txtFileName.Visible = true;
            cmbLocation.Visible = true;
            txtDescription.Visible = true;

            foreach (Control ctrl in groupBoxContent.Controls)
            {
                if (ctrl is Label lbl && lbl.Name.StartsWith("lblStep1"))
                    ctrl.Visible = true;
            }
        }

        private void ShowStep2()
        {
            lblStepTitle.Text = "Step 2: Add Flower Beds";
            lblStepDescription.Text = "Define what flowers you want to plant and how to tend each bed.";
            groupBoxContent.Text = "Flower Bed Setup";

            lstFlowerBeds.Visible = true;
            cmbFlower.Visible = true;
            numWaterCount.Visible = true;
            chkRemoveFirst.Visible = true;
            btnAddFlowerBed.Visible = true;
            btnRemoveFlowerBed.Visible = true;

            foreach (Control ctrl in groupBoxContent.Controls)
            {
                if (ctrl is Label lbl && lbl.Name.StartsWith("lblStep2"))
                    ctrl.Visible = true;
            }
        }

        private void ShowStep3()
        {
            lblStepTitle.Text = "Step 3: Add Walking Between Beds";
            lblStepDescription.Text = "Insert walk/turn actions between flower bed operations.";
            groupBoxContent.Text = "Build Full Sequence";

            // Convert flower beds to actions if not done yet
            if (_actions.Count == 0 && lstFlowerBeds.Items.Count > 0)
            {
                BuildActionsFromFlowerBeds();
            }

            lstActions.Visible = true;
            cmbWalkAction.Visible = true;
            numWalkDuration.Visible = true;
            btnAddWalk.Visible = true;
            btnRemoveAction.Visible = true;
            lblPreview.Visible = true;

            foreach (Control ctrl in groupBoxContent.Controls)
            {
                if (ctrl is Label lbl && lbl.Name.StartsWith("lblStep3"))
                    ctrl.Visible = true;
            }

            RefreshActionsList();
        }

        private void BuildActionsFromFlowerBeds()
        {
            _actions.Clear();

            foreach (var item in lstFlowerBeds.Items)
            {
                string text = item.ToString();

                // Parse the flower bed entry
                bool removeFirst = text.Contains("[Remove]");
                string flowerName = "";

                // Extract flower name from "Plant FlowerName →"
                int plantIdx = text.IndexOf("Plant ");
                if (plantIdx >= 0)
                {
                    int arrowIdx = text.IndexOf(" →", plantIdx);
                    if (arrowIdx > plantIdx)
                    {
                        flowerName = text.Substring(plantIdx + 6, arrowIdx - plantIdx - 6).Trim();
                    }
                }

                // Extract water count from "Water Nx"
                int waterCount = 3;
                int waterIdx = text.IndexOf("Water ");
                if (waterIdx >= 0)
                {
                    string waterStr = text.Substring(waterIdx + 6).Replace("x", "").Trim();
                    int.TryParse(waterStr, out waterCount);
                }

                // Add remove action if needed
                if (removeFirst)
                {
                    _actions.Add(new GardeningActionCommand { Action = "REMOVE PLANT" });
                }

                // Add plant action
                _actions.Add(new GardeningActionCommand
                {
                    Action = "PLANT FLOWER",
                    FlowerName = flowerName,
                    BeanSequence = _flowerDatabase.ContainsKey(flowerName) ? _flowerDatabase[flowerName] : ""
                });

                // Add water action
                _actions.Add(new GardeningActionCommand
                {
                    Action = "WATER PLANT",
                    WaterCount = waterCount
                });
            }
        }

        private void ShowStep4()
        {
            lblStepTitle.Text = "Step 4: Test & Save";
            lblStepDescription.Text = "Test your routine in-game, then save the file.";
            groupBoxContent.Text = "Finalize";

            btnTest.Visible = true;
            lblTestStatus.Visible = true;
            chkSaveAsV2.Visible = true;
            lblFinalSummary.Visible = true;

            // Build summary
            var (totalActions, plantCount, waterCount, removeCount, estimatedTimeMs) =
                GardeningDurationFormatter.GetSequenceSummary(_actions);

            double estimatedSec = estimatedTimeMs / 1000.0;
            string timeStr = estimatedSec >= 60 ? $"{estimatedSec / 60:F1} minutes" : $"{estimatedSec:F0} seconds";

            lblFinalSummary.Text = $"Routine: {txtFileName.Text}\n" +
                                   $"Location: {(string.IsNullOrEmpty(cmbLocation.Text) ? "(not set)" : cmbLocation.Text)}\n\n" +
                                   $"Total Actions: {totalActions}\n" +
                                   $"Flowers to Plant: {plantCount}\n" +
                                   $"Total Waters: {waterCount}\n" +
                                   $"Plants to Remove: {removeCount}\n" +
                                   $"Estimated Time: {timeStr}";
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
            if (!ValidateCurrentStep())
                return;

            if (_currentStep < TOTAL_STEPS)
            {
                ShowStep(_currentStep + 1);
            }
            else
            {
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
                        MessageBox.Show("Please enter a routine name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    return true;

                case 2:
                    if (lstFlowerBeds.Items.Count == 0)
                    {
                        MessageBox.Show("Please add at least one flower bed.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    return true;

                default:
                    return true;
            }
        }

        private void SaveFile()
        {
            string folderPath = (string)CoreFunctionality.ManageCustomActionsFolder("Gardening", false);
            string fileName = txtFileName.Text.Trim();

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c.ToString(), "");
            }

            string filePath = Path.Combine(folderPath, fileName + ".json");

            if (chkSaveAsV2.Checked)
            {
                var file = new CustomGardeningActionFile
                {
                    Name = fileName,
                    Location = cmbLocation.Text.Trim(),
                    Description = txtDescription.Text.Trim(),
                    FlowerBedCount = lstFlowerBeds.Items.Count,
                    Actions = _actions
                };

                if (CustomGardeningActionFileManager.Save(file, filePath))
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
                if (CustomGardeningActionFileManager.SaveV1(_actions, filePath))
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
