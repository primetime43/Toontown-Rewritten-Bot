using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using WindowsInput;
using ToonTown_Rewritten_Bot.Models;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot.Services.CustomGolfActions
{
    /// <summary>
    /// Event args for golf action progress updates.
    /// </summary>
    public class GolfProgressEventArgs : EventArgs
    {
        public string CurrentAction { get; set; }
        public string NextAction { get; set; }
        public int CurrentStep { get; set; }
        public int TotalSteps { get; set; }
        public int DurationMs { get; set; }
    }

    public class CustomActionsGolf
    {
        private List<GolfActionCommand> actions = new List<GolfActionCommand>();

        /// <summary>
        /// Event raised when action progress changes.
        /// </summary>
        public event EventHandler<GolfProgressEventArgs> ProgressChanged;

        /// <summary>
        /// Event raised when golf actions complete or are cancelled.
        /// </summary>
        public event EventHandler<string> StatusChanged;

        /// <summary>
        /// Gets the total number of actions loaded.
        /// </summary>
        public int TotalActions => actions.Count;

        public CustomActionsGolf(string filePath)
        {
            LoadActionsFromJson(filePath);
        }

        private void LoadActionsFromJson(string filePath)
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                actions = JsonConvert.DeserializeObject<List<GolfActionCommand>>(json);
            }
        }

        private async Task PrepareToHitBall()
        {
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            await Task.Delay(50);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);
        }

        private void ReportProgress(int currentStep, string currentAction, string nextAction, int durationMs)
        {
            ProgressChanged?.Invoke(this, new GolfProgressEventArgs
            {
                CurrentAction = currentAction,
                NextAction = nextAction,
                CurrentStep = currentStep,
                TotalSteps = actions.Count,
                DurationMs = durationMs
            });
        }

        private void ReportStatus(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        public async Task PerformGolfActions(CancellationToken cancellationToken)
        {
            CoreFunctionality.FocusTTRWindow();
            ReportStatus("Starting");
            await Task.Delay(1000, cancellationToken);
            GolfActionKeys keys = new GolfActionKeys();

            try
            {
                ReportStatus("Running");

                for (int i = 0; i < actions.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var actionCommand = actions[i];
                    string nextAction = (i + 1 < actions.Count) ? actions[i + 1].Action : "Done";

                    // Report progress
                    ReportProgress(i + 1, actionCommand.Action, nextAction, actionCommand.Duration);

                    // Handle delay time actions separately
                    if (actionCommand.Action == "DELAY TIME")
                    {
                        await Task.Delay(actionCommand.Duration, cancellationToken);
                        continue;
                    }

                    // Process other actions that should correspond to actual key presses
                    if (keys.ActionKeyMap.TryGetValue(actionCommand.Action, out VirtualKeyCode keyCode))
                    {
                        // Tee movements use the duration to wait after the movement
                        if (actionCommand.Action == "MOVE TO RIGHT TEE SPOT" || actionCommand.Action == "MOVE TO LEFT TEE SPOT")
                        {
                            InputSimulator.SimulateKeyDown(keyCode);
                            await Task.Delay(actionCommand.Duration, cancellationToken);
                            InputSimulator.SimulateKeyUp(keyCode);
                            await PrepareToHitBall();
                        }
                        else
                        {
                            InputSimulator.SimulateKeyDown(keyCode);
                            await Task.Delay(actionCommand.Duration, cancellationToken);
                            InputSimulator.SimulateKeyUp(keyCode);
                        }
                    }
                    else
                    {
                        CoreFunctionality.BringBotWindowToFront();
                        MessageBox.Show($"Unsupported action: {actionCommand.Action}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                ReportStatus("Completed");
            }
            catch (OperationCanceledException)
            {
                ReportStatus("Cancelled");
                throw;
            }
        }
    }
}