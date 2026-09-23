using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Services.FishingLocationsWalking;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>Shared route builder for creating and editing custom fishing routes.</summary>
    public class FishingRouteBuilderForm : Form
    {
        private const string LocationName = "CUSTOM FISHING ACTION";
        private readonly TextBox routeName = new() { Dock = DockStyle.Fill, PlaceholderText = "e.g. Estate — left dock" };
        private readonly TextBox description = new() { Dock = DockStyle.Fill, PlaceholderText = "Optional: starting position, landmark, or other notes" };
        private readonly ListView routeList = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = false };
        private readonly ComboBox action = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 244 };
        private readonly NumericUpDown seconds = new() { DecimalPlaces = 3, Increment = .05m, Minimum = .001m, Maximum = int.MaxValue / 1000m, Value = .5m, Width = 100 };
        private readonly Label status = new() { Dock = DockStyle.Fill, AutoSize = true, ForeColor = UiColors.Text, Padding = new Padding(0, 5, 0, 5) };
        private readonly Label calibrationStatus = new() { AutoSize = true, Margin = new Padding(10, 10, 0, 0) };
        private readonly Button stop = new() { Text = "Stop (F8)", AutoSize = true, Enabled = false };
        private readonly Button useTake = new() { Text = "Keep recording and continue", AutoSize = true, Visible = false,
            MinimumSize = new Size(0, 38), Padding = new Padding(12, 3, 12, 3) };
        private readonly Button discardTake = new() { Text = "Discard recording", AutoSize = true, Visible = false };
        private readonly Label reviewTitle = new() { AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
        private readonly Label reviewHelp = new() { AutoSize = true, MaximumSize = new Size(790, 0) };
        private Control routeGuide, reviewPanel, stepEditor, stepTools, calibrationPanel, testHelp;
        private FlowLayoutPanel footer;
        private Button closeButton, recordReturnButton, testButton;
        private readonly List<Control> idleControls = new();
        private Button applyButton, upButton, downButton, duplicateButton, removeButton, undoButton;
        private readonly Stack<List<FishingRouteStep>> undo = new();
        private List<FishingRouteStep> steps = new() { new("SELL") };
        private CustomFishingActionFile file = new();
        private string filePath;
        private bool dirty;
        private bool loading;
        private bool recording;
        private bool outbound;
        private bool busy;
        private List<FishingRouteStep> pendingTake;
        private FishingRouteRecorder recorder;
        private readonly Stopwatch recordingClock = new();
        private readonly GlobalKeyboardHook hook = new();
        private readonly System.Windows.Forms.Timer focusTimer = new() { Interval = 100 };
        private CancellationTokenSource operation;
        private Form recordingBanner;
        public string SavedFileName { get; private set; }

        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();

        public FishingRouteBuilderForm(string path = null)
        {
            Text = "Custom fishing route";
            Font = new Font("Segoe UI", 9.5f);
            BackColor = UiColors.Background;
            ForeColor = UiColors.Text;
            ClientSize = new Size(930, 780);
            MinimumSize = new Size(910, 790);
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            BuildLayout();
            hook.KeyPressed += KeyPressed;
            hook.KeyReleased += KeyReleased;
            focusTimer.Tick += (_, _) =>
            {
                if (recording && !GameFocused())
                {
                    recorder.ReleaseAll(recordingClock.ElapsedMilliseconds);
                    FinishRecording("Recording stopped when you switched away from Toontown.");
                }
                else if (busy && !recording && operation != null && recordingBanner != null && !GameFocused())
                    operation.Cancel();
            };
            routeName.TextChanged += (_, _) => { if (!loading) dirty = true; };
            description.TextChanged += (_, _) => { if (!loading) dirty = true; };
            routeList.SelectedIndexChanged += (_, _) => SelectStep();
            routeList.DoubleClick += (_, _) => { if (pendingTake == null && seconds.Enabled) { seconds.Focus(); seconds.Select(0, seconds.Text.Length); } };
            action.SelectedIndexChanged += (_, _) => seconds.Enabled = SelectedCommand != "SELL" && !busy && pendingTake == null;
            stop.Click += (_, _) => StopOperation();
            UiTheme.ApplyButtonColors(useTake, true);
            useTake.Click += (_, _) => AcceptTake();
            discardTake.Click += (_, _) => { pendingTake = null; RefreshRoute(); UpdateEnabled(); status.Text = "Recording discarded. Your route is unchanged."; };
            RefreshRoute();
            if (path != null) LoadRoute(path);
        }

        private Button Button(string text, Action clicked, bool primary = false)
        {
            var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(0, 32), Padding = new Padding(8, 1, 8, 1), FlatStyle = FlatStyle.Flat };
            UiTheme.ApplyButtonColors(button, primary);
            button.Click += (_, _) => clicked();
            idleControls.Add(button);
            return button;
        }

        private static FlowLayoutPanel Flow(params Control[] controls)
        {
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = Padding.Empty };
            panel.Controls.AddRange(controls);
            return panel;
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22, 16, 22, 16), ColumnCount = 1, RowCount = 10 };
            for (int i = 0; i < 10; i++) root.RowStyles.Add(new RowStyle(i == 3 ? SizeType.Percent : SizeType.AutoSize, i == 3 ? 100 : 0));
            Controls.Add(root);
            void FitStatusText() => status.MaximumSize = new Size(Math.Max(1, root.ClientSize.Width - root.Padding.Horizontal - status.Margin.Horizontal), 0);
            root.SizeChanged += (_, _) => FitStatusText();
            FitStatusText();
            root.Controls.Add(new Label { Text = "Build your fishing route", AutoSize = true, Font = new Font(Font.FontFamily, 18, FontStyle.Bold), Margin = new Padding(0, 0, 0, 12) }, 0, 0);
            var details = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 12) };
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            details.Controls.Add(new Label { Text = "Route name", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, 0, 0);
            details.Controls.Add(routeName, 1, 0);
            details.Controls.Add(new Label { Text = "Notes", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, 0, 1);
            details.Controls.Add(description, 1, 1);
            root.Controls.Add(details, 0, 1);

            var guide = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, BackColor = UiColors.InfoSurface, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 12) };
            routeGuide = guide;
            guide.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(820, 0), Text = "1  Leave fishing first. Start just off the dock, facing the same way as after exiting fishing.\n2  Record the walk to the fisherman, then the walk back onto the dock. Sell fish is added for you.\n3  Review the steps below, test from the same starting position, and save." });
            guide.Controls.Add(Flow(Button("Record to fisherman", () => StartRecording(true), true), recordReturnButton = Button("Record back to dock", () => StartRecording(false)), stop));
            guide.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(820, 0), Text = $"Use {GameControls.GetMovementBindingSummary()}. F8 finishes recording. Walking while turning is supported; pauses are omitted." });
            var review = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, BackColor = UiColors.InfoSurface, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 12), Visible = false };
            review.Controls.Add(reviewTitle);
            review.Controls.Add(reviewHelp);
            review.Controls.Add(Flow(useTake, discardTake));
            reviewPanel = review;
            var stage = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Margin = Padding.Empty };
            stage.Controls.Add(guide);
            stage.Controls.Add(review);
            root.Controls.Add(stage, 0, 2);

            routeList.Columns.Add("#", 40);
            routeList.Columns.Add("Route section", 165);
            routeList.Columns.Add("Action", 395);
            routeList.Columns.Add("Duration", 150);
            root.Controls.Add(routeList, 0, 3);
            foreach (var name in FishingRoute.Names) action.Items.Add(new ActionChoice(name.Key, name.Value));
            action.SelectedIndex = 0;
            var editor = Flow(action, seconds, new Label { Text = "seconds", AutoSize = true, Margin = new Padding(3, 8, 14, 0) },
                applyButton = Button("Apply to selected", ApplyStep), Button("Add before", () => AddStep(true)), Button("Add to end", () => AddStep(false)));
            editor.Margin = new Padding(0, 10, 0, 0);
            stepEditor = editor;
            root.Controls.Add(editor, 0, 4);
            stepTools = Flow(upButton = Button("Move up", () => MoveStep(-1)), downButton = Button("Move down", () => MoveStep(1)),
                duplicateButton = Button("Duplicate", DuplicateStep), removeButton = Button("Remove", RemoveStep), undoButton = Button("Undo", Undo));
            root.Controls.Add(stepTools, 0, 5);
            root.Controls.Add(status, 0, 6);
            var calibration = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 6, 0, 6) };
            calibrationPanel = calibration;
            calibration.Controls.Add(new Label { AutoSize = true, Text = "Optional: fish detection — enter fishing on the dock before calibrating." });
            calibration.Controls.Add(Flow(Button("Scan area…", CalibrateArea), Button("Pond colors…", CalibrateColors), calibrationStatus));
            root.Controls.Add(calibration, 0, 7);
            testHelp = new Label { AutoSize = true, Text = "Test route walks the entire path and sells fish in the game. Press F8 to stop.", ForeColor = UiColors.MutedText, Margin = new Padding(0, 6, 0, 8) };
            root.Controls.Add(testHelp, 0, 8);
            footer = Flow(Button("Open route…", OpenRoute), Button("Use template…", OpenTemplate), testButton = Button("Test route", TestRoute), Button("Save route", () => SaveRoute(false), true),
                Button("Save a copy…", () => SaveRoute(true)), closeButton = Button("Close", Close));
            root.Controls.Add(footer, 0, 9);
            idleControls.AddRange(new Control[] { routeName, description, action, seconds, routeList });
        }

        private record ActionChoice(string Command, string Name) { public override string ToString() => Name; }
        private string SelectedCommand => (action.SelectedItem as ActionChoice)?.Command ?? "UP";
        private int SelectedIndex => routeList.SelectedIndices.Count == 0 ? -1 : routeList.SelectedIndices[0];
        private FishingRouteStep EditedStep => new(SelectedCommand, SelectedCommand == "SELL" ? 0 : (int)(seconds.Value * 1000));

        private void SelectStep()
        {
            var visible = pendingTake ?? steps;
            int index = SelectedIndex;
            UpdateEnabled();
            if (index < 0 || index >= visible.Count) return;
            var step = visible[index];
            action.SelectedIndex = action.Items.Cast<ActionChoice>().ToList().FindIndex(a => a.Command == step.Command);
            if (step.Milliseconds > 0) seconds.Value = step.Milliseconds / 1000m;
        }

        private void RefreshRoute(int selected = -1)
        {
            routeList.BeginUpdate();
            routeList.Items.Clear();
            bool returning = false;
            var visible = pendingTake ?? steps;
            for (int i = 0; i < visible.Count; i++)
            {
                var step = visible[i];
                string section = pendingTake != null ? "Recording preview" : step.Command == "SELL" ? "At fisherman" : returning ? "Back to dock" : "To fisherman";
                var item = new ListViewItem(new[] { (i + 1).ToString(), section, step.DisplayName, step.Command == "SELL" ? "Automatic" : DurationFormatter.FormatSeconds(step.Milliseconds) });
                if (step.Command == "SELL") { item.BackColor = UiColors.SuccessSurface; returning = true; }
                routeList.Items.Add(item);
            }
            if (selected >= 0 && selected < visible.Count) { routeList.Items[selected].Selected = true; routeList.Items[selected].EnsureVisible(); }
            routeList.EndUpdate();
            string completionHint = FishingRoute.Validate(steps);
            status.Text = pendingTake != null ? "Your route has not changed yet. Keep this recording to continue, or discard it to try again."
                : completionHint != null ? $"Partial route — you can save now{(steps.Count > 0 ? " or test these steps" : "")}. {completionHint}"
                : $"Ready to test • {steps.Count} steps • {DurationFormatter.FormatSeconds(steps.Sum(s => (long)s.Milliseconds))} movement / waiting, plus selling";
            testHelp.Text = steps.Count == 0 ? "Save your progress at any time. Add a step to test it."
                : "Test replays only the listed steps, including Sell fish. It stops after the last step. F8 stops early.";
            UpdateCalibrationStatus();
            UpdateEnabled();
        }

        private void Remember() { undo.Push(steps.ToList()); dirty = true; }
        private void ApplyStep() { int i = SelectedIndex; if (i < 0) { status.Text = "Select a step to edit first."; return; } Remember(); steps[i] = EditedStep; RefreshRoute(i); }
        private void AddStep(bool before) { int i = before && SelectedIndex >= 0 ? SelectedIndex : steps.Count; Remember(); steps.Insert(i, EditedStep); RefreshRoute(i); }
        private void RemoveStep() { int i = SelectedIndex; if (i < 0) return; Remember(); steps.RemoveAt(i); RefreshRoute(Math.Min(i, steps.Count - 1)); }
        private void DuplicateStep() { int i = SelectedIndex; if (i < 0) return; Remember(); steps.Insert(i + 1, steps[i]); RefreshRoute(i + 1); }
        private void MoveStep(int offset) { int i = SelectedIndex, target = i + offset; if (i < 0 || target < 0 || target >= steps.Count) return; Remember(); (steps[i], steps[target]) = (steps[target], steps[i]); RefreshRoute(target); }
        private void Undo() { if (undo.Count == 0) return; steps = undo.Pop(); dirty = true; RefreshRoute(); }

        private bool GameFocused() { var game = CoreFunctionality.FindToontownWindow(); return game != IntPtr.Zero && GetForegroundWindow() == game; }
        private void UpdateEnabled()
        {
            foreach (var control in idleControls) control.Enabled = !busy && pendingTake == null;
            routeList.Enabled = !busy;
            stop.Enabled = busy;
            testButton.Enabled = !busy && pendingTake == null && steps.Count > 0;
            useTake.Visible = discardTake.Visible = pendingTake != null;
            bool reviewing = pendingTake != null;
            routeGuide.Visible = !reviewing;
            reviewPanel.Visible = reviewing;
            stepEditor.Visible = stepTools.Visible = calibrationPanel.Visible = testHelp.Visible = !reviewing;
            foreach (Control control in footer.Controls) control.Visible = !reviewing || control == closeButton;
            closeButton.Enabled = !busy;
            AcceptButton = reviewing ? useTake : null;
            if (reviewing)
            {
                reviewTitle.Text = $"Recording complete — {pendingTake.Count} steps {(outbound ? "to the fisherman" : "back to the dock")}";
                reviewHelp.Text = "Check your recorded movements below, then click Keep recording and continue.\n" +
                    $"This replaces only the {(outbound ? "outward" : "return")} path. You can adjust timings afterward, or use Undo to restore the old path.";
            }
            seconds.Enabled = !busy && pendingTake == null && SelectedCommand != "SELL";
            if (applyButton != null)
            {
                bool editable = !busy && pendingTake == null;
                int selected = SelectedIndex;
                applyButton.Enabled = removeButton.Enabled = duplicateButton.Enabled = editable && selected >= 0;
                upButton.Enabled = editable && selected > 0;
                downButton.Enabled = editable && selected >= 0 && selected < steps.Count - 1;
                undoButton.Enabled = editable && undo.Count > 0;
            }
        }

        private void ShowBanner(string message)
        {
            recordingBanner = new Form { FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false, TopMost = true, StartPosition = FormStartPosition.Manual, BackColor = UiColors.Banner, Size = new Size(580, 70) };
            var area = Screen.FromHandle(CoreFunctionality.FindToontownWindow()).WorkingArea;
            recordingBanner.Location = new Point(area.Left + (area.Width - recordingBanner.Width) / 2, area.Top + 12);
            recordingBanner.Controls.Add(new Label { Text = message, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = UiColors.OnPrimary, Font = new Font("Segoe UI", 12, FontStyle.Bold) });
            recordingBanner.Show();
        }

        private async Task<bool> PrepareOperation(string instructions)
        {
            if (busy || pendingTake != null) return false;
            if (CoreFunctionality.FindToontownWindow() == IntPtr.Zero) { status.Text = "Open Toontown Rewritten before recording or testing."; return false; }
            if (MessageBox.Show(this, instructions + "\n\nA three-second countdown will appear in the game. Press F8 to stop.", "Ready?", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK) return false;
            if (!hook.Start()) { status.Text = "Could not start the keyboard listener. Try reopening the route builder."; return false; }
            busy = true;
            operation = new CancellationTokenSource();
            UpdateEnabled();
            WindowState = FormWindowState.Minimized;
            ShowBanner("Starting in 3…  •  F8 to cancel");
            bool background = CoreFunctionality.UseBackgroundInput;
            try
            {
                CoreFunctionality.UseBackgroundInput = false;
                CoreFunctionality.FocusTTRWindow();
                for (int count = 3; count > 0; count--)
                {
                    recordingBanner.Controls[0].Text = $"Starting in {count}…  •  F8 to cancel";
                    await Task.Delay(1000, operation.Token);
                }
                if (!GameFocused()) throw new InvalidOperationException("Toontown must be the active window. Try again when the game is visible.");
                focusTimer.Start();
                return true;
            }
            catch (OperationCanceledException) { EndOperation(); status.Text = "Cancelled before starting. Your route is unchanged."; return false; }
            catch (Exception ex) { EndOperation(); status.Text = ex.Message; return false; }
            finally { CoreFunctionality.UseBackgroundInput = background; }
        }

        private async void StartRecording(bool toFisherman)
        {
            if (steps.Count(s => s.Command == "SELL") != 1) { status.Text = "Keep exactly one Sell fish step before recording either path."; return; }
            string instructions = toFisherman
                ? "Exit fishing. Start just off the dock, then walk to where the fisherman offers to buy fish. Press F8 before selling."
                : "Start beside the fisherman, after closing the sell dialog. Walk back onto the same dock until the Cast button appears, then press F8.";
            if (!await PrepareOperation(instructions)) return;
            outbound = toFisherman;
            recorder = new FishingRouteRecorder();
            recordingClock.Restart();
            recording = true;
            recordingBanner.Controls[0].Text = toFisherman ? "Recording → fisherman  •  F8 to finish" : "Recording → dock  •  F8 to finish";
        }

        private void KeyPressed(object sender, Keys key)
        {
            if (key == Keys.F8 && busy)
            {
                hook.SuppressKey = true;
                BeginInvoke(new Action(StopOperation));
                return;
            }
            if (!recording || !GameFocused()) return;
            string command = MovementCommand(key);
            if (command != null) recorder.Change(command, true, recordingClock.ElapsedMilliseconds);
        }

        private void KeyReleased(object sender, Keys key)
        {
            if (!recording) return;
            string command = MovementCommand(key);
            if (command != null) recorder.Change(command, false, recordingClock.ElapsedMilliseconds);
        }

        private static string MovementCommand(Keys key) => GameControls.GetMovementAction((int)key) switch
        {
            "WALK FORWARDS" => "UP", "WALK BACKWARDS" => "DOWN", "TURN LEFT" => "LEFT", "TURN RIGHT" => "RIGHT", _ => null
        };

        private void StopOperation()
        {
            if (recording) FinishRecording("Recording finished.");
            else operation?.Cancel();
        }

        private void FinishRecording(string message)
        {
            recorder.ReleaseAll(recordingClock.ElapsedMilliseconds);
            recordingClock.Stop();
            recording = false;
            pendingTake = recorder.Steps.Count > 0 ? recorder.Steps.ToList() : null;
            EndOperation();
            RefreshRoute();
            status.Text = pendingTake == null ? "No movement recorded. Your route is unchanged. Try again using the configured movement keys."
                : message + " Your route has not changed yet. Keep or discard this recording above.";
            if (pendingTake != null) useTake.Focus();
        }

        private void AcceptTake()
        {
            if (pendingTake == null) return;
            Remember();
            int sell = steps.FindIndex(s => s.Command == "SELL");
            steps = outbound ? pendingTake.Concat(steps.Skip(sell)).ToList() : steps.Take(sell + 1).Concat(pendingTake).ToList();
            pendingTake = null;
            RefreshRoute();
            UpdateEnabled();
            status.Text = outbound ? "Outward recording kept. Next: record the walk back to the dock, or test if your return path is already ready."
                : "Return recording kept. Next: test the route from your original starting position, then save.";
            if (outbound) recordReturnButton.Focus(); else testButton.Focus();
        }

        private void EndOperation()
        {
            focusTimer.Stop();
            hook.Stop();
            recordingBanner?.Dispose();
            recordingBanner = null;
            operation?.Dispose();
            operation = null;
            busy = false;
            UpdateEnabled();
            if (!IsDisposed) { WindowState = FormWindowState.Normal; Activate(); }
        }

        private async void TestRoute()
        {
            if (steps.Count == 0) { status.Text = "Add or record at least one step to test. You can still save this route."; return; }
            if (!await PrepareOperation(GetTestInstructions())) return;
            bool background = CoreFunctionality.UseBackgroundInput;
            try
            {
                CoreFunctionality.UseBackgroundInput = false;
                var progress = new Progress<int>(index =>
                {
                    if (!busy || recordingBanner == null) return;
                    recordingBanner.Controls[0].Text = $"Testing {index + 1}/{steps.Count}: {steps[index].DisplayName}  •  F8 to stop";
                });
                await new CustomActionsFishing(FishingRoute.Encode(steps)).ReplayRouteAsync(operation.Token, progress);
                status.Text = $"Test finished after {steps.Count} steps. Last action: {steps[^1].DisplayName}. Adjust the route, record more steps, or save.";
            }
            catch (OperationCanceledException) { status.Text = "Test stopped. Reposition your Toon before trying again."; }
            catch (Exception ex) { status.Text = "Test failed: " + ex.Message; }
            finally { CoreFunctionality.UseBackgroundInput = background; EndOperation(); }
        }

        private string GetTestInstructions()
        {
            string instructions = $"Position your Toon where the first listed step should begin, with the same facing direction used when recording.\n\nThis test runs only the {steps.Count} listed steps and stops after the last one.";
            if (steps.Any(s => s.Command == "SELL")) instructions += "\nThe Sell fish step WILL sell your fish in the game.";
            if (steps.Count > 0 && steps[^1].Command == "SELL") instructions += "\nThis route ends at the fisherman. Walk back yourself or record the return path afterward.";
            else if (FishingRoute.Validate(steps) != null) instructions += "\nThis is a partial route. Your Toon may finish away from the dock.";
            return instructions;
        }

        private bool ConfirmDiscard()
        {
            return (!dirty && pendingTake == null) || MessageBox.Show(this, "Discard the unsaved changes to this route?", "Unsaved route", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void OpenRoute()
        {
            if (!ConfirmDiscard()) return;
            using var dialog = new OpenFileDialog { Filter = "Fishing route (*.json)|*.json", InitialDirectory = CustomFishingActionFileManager.GetCustomActionsFolder() };
            if (dialog.ShowDialog(this) == DialogResult.OK) LoadRoute(dialog.FileName);
        }

        private void OpenTemplate()
        {
            if (!ConfirmDiscard()) return;
            using var dialog = new OpenFileDialog { Title = "Start from a fishing route template", Filter = "Fishing route (*.json)|*.json", InitialDirectory = Path.Combine(AppPaths.ExeDirectory, "Templates", "CustomFishingTemplates") };
            if (dialog.ShowDialog(this) != DialogResult.OK || !LoadRoute(dialog.FileName)) return;
            filePath = null;
            SavedFileName = null;
            file.CreatedAt = DateTime.Now;
            dirty = true;
            status.Text = "Template loaded. Adjust the route for your dock, then save it as a new route.";
        }

        private bool LoadRoute(string path)
        {
            var result = CustomFishingActionFileManager.Load(path);
            try
            {
                if (!result.Success) throw new InvalidDataException(result.ErrorMessage);
                var loadedSteps = FishingRoute.Decode(result.File.Actions);
                loading = true;
                file = result.File;
                steps = loadedSteps;
                filePath = path;
                SavedFileName = null;
                routeName.Text = string.IsNullOrWhiteSpace(file.Name) ? Path.GetFileNameWithoutExtension(path) : file.Name;
                description.Text = file.Description;
                undo.Clear();
                dirty = false;
                RefreshRoute();
                Text = "Custom fishing route — " + routeName.Text;
                return true;
            }
            catch (Exception ex) { status.Text = "Could not open route: " + ex.Message; return false; }
            finally { loading = false; }
        }

        private void SaveRoute(bool copy)
        {
            if (string.IsNullOrWhiteSpace(routeName.Text)) { status.Text = "Give this route a name before saving."; routeName.Focus(); return; }
            string target = filePath;
            if (copy || target == null)
            {
                string suggested = string.Concat(routeName.Text.Trim().Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                using var dialog = new SaveFileDialog { Filter = "Fishing route (*.json)|*.json", DefaultExt = "json", AddExtension = true, FileName = suggested + (copy ? " copy" : "") + ".json", InitialDirectory = CustomFishingActionFileManager.GetCustomActionsFolder(), OverwritePrompt = true };
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                target = dialog.FileName;
            }
            file.Name = routeName.Text.Trim();
            file.Description = description.Text.Trim();
            file.Actions = FishingRoute.Encode(steps);
            if (!CustomFishingActionFileManager.Save(file, target)) { status.Text = "Could not save this route. Check the folder permissions or use Save a copy."; return; }
            filePath = target;
            SavedFileName = Path.GetFileNameWithoutExtension(target);
            dirty = false;
            status.Text = (FishingRoute.Validate(steps) == null ? "Saved: " : "Saved partial route (you can finish it later): ") + Path.GetFileName(target);
            Text = "Custom fishing route — " + file.Name;
        }

        private void UpdateCalibrationStatus()
        {
            calibrationStatus.Text = $"Scan area: {(file.Calibration?.ScanArea != null ? "saved" : "global")}  •  Colors: {(file.Calibration?.PondColors != null ? "saved" : "global")}";
        }

        private void CalibrateArea()
        {
            try
            {
                var window = CoreFunctionality.GetGameWindowRect();
                if (window.IsEmpty) { status.Text = "Open Toontown and enter fishing before calibrating."; return; }
                var area = CustomScanAreaManager.GetCustomScanArea(LocationName, window.Width, window.Height) ?? new FishBubbleDetector(LocationName).GetDefaultScanArea();
                using var form = new ScanAreaCalibrationForm(LocationName, area);
                form.ShowDialog(this);
                if (!form.WasSaved) return;
                file.Calibration ??= new CalibrationData();
                file.Calibration.ScanArea = CustomFishingActionFileManager.CreateCalibrationFromGlobalSettings(LocationName, window.Width, window.Height)?.ScanArea;
                dirty = true;
                UpdateCalibrationStatus();
                status.Text = "Scan area updated. Save the route to keep its calibration.";
            }
            catch (Exception ex) { status.Text = "Could not calibrate: " + ex.Message; }
        }

        private void CalibrateColors()
        {
            try
            {
                using var form = new PondColorCalibrationForm(LocationName);
                form.ShowDialog(this);
                if (!form.WasSaved) return;
                var window = CoreFunctionality.GetGameWindowRect();
                file.Calibration ??= new CalibrationData();
                file.Calibration.PondColors = CustomFishingActionFileManager.CreateCalibrationFromGlobalSettings(LocationName, Math.Max(1, window.Width), Math.Max(1, window.Height))?.PondColors;
                dirty = true;
                UpdateCalibrationStatus();
                status.Text = "Pond colors updated. Save the route to keep its calibration.";
            }
            catch (Exception ex) { status.Text = "Could not calibrate: " + ex.Message; }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (busy) { StopOperation(); e.Cancel = true; }
            else if (!ConfirmDiscard()) e.Cancel = true;
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                operation?.Cancel();
                focusTimer.Dispose();
                hook.Dispose();
                recordingBanner?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
