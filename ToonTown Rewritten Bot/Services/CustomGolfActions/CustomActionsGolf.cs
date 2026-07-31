using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using WindowsInput;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Utilities;
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
        /// Gets the total number of executable actions (excludes position actions which are skipped).
        /// </summary>
        public int TotalActions => actions.FindAll(a =>
            a.Action != "MOVE TO LEFT TEE SPOT" &&
            a.Action != "MOVE TO RIGHT TEE SPOT").Count;

        public CustomActionsGolf(string filePath)
        {
            LoadActionsFromJson(filePath);
        }

        private void LoadActionsFromJson(string filePath)
        {
            var result = CustomGolfActionFileManager.Load(filePath);
            if (!result.Success)
            {
                throw new InvalidDataException(
                    $"Could not load golf action file: {result.ErrorMessage}");
            }

            actions = result.File?.Actions ?? new List<GolfActionCommand>();
            if (actions.Count == 0)
            {
                throw new InvalidDataException("The selected golf action file contains no actions.");
            }

            if (actions.Exists(action => action.Duration <= 0))
            {
                throw new InvalidDataException(
                    "Every golf action must have a duration greater than zero.");
            }

            if (!actions.Exists(action => action.Action == "SWING POWER"))
            {
                throw new InvalidDataException(
                    "The selected golf action file has no SWING POWER action, so it cannot shoot.");
            }
        }

        private void ReportProgress(int currentStep, string currentAction, string nextAction, int durationMs)
        {
            ProgressChanged?.Invoke(this, new GolfProgressEventArgs
            {
                CurrentAction = currentAction,
                NextAction = nextAction,
                CurrentStep = currentStep,
                TotalSteps = TotalActions,
                DurationMs = durationMs
            });
        }

        private void ReportStatus(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        public async Task PerformGolfActions(CancellationToken cancellationToken)
        {
            // Background mode is a fishing preference. Golf relies on foreground SendInput,
            // so temporarily disable it to ensure FocusTTRWindow and key delivery agree.
            bool previousBackgroundMode = CoreFunctionality.UseBackgroundInput;
            CoreFunctionality.UseBackgroundInput = false;

            try
            {
                CoreFunctionality.FocusTTRWindow();
                ReportStatus("Starting");
                await Task.Delay(1000, cancellationToken);
                GolfActionKeys keys = new GolfActionKeys();

                ReportStatus("Running");

                int executedStep = 0;

                for (int i = 0; i < actions.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var actionCommand = actions[i];

                    // Skip tee position actions - user positions themselves manually
                    if (actionCommand.Action == "MOVE TO RIGHT TEE SPOT" || actionCommand.Action == "MOVE TO LEFT TEE SPOT")
                    {
                        continue;
                    }

                    executedStep++;

                    // Find next executable action for display
                    string nextAction = "Done";
                    for (int j = i + 1; j < actions.Count; j++)
                    {
                        if (actions[j].Action != "MOVE TO RIGHT TEE SPOT" && actions[j].Action != "MOVE TO LEFT TEE SPOT")
                        {
                            nextAction = actions[j].Action;
                            break;
                        }
                    }

                    // Report progress
                    ReportProgress(executedStep, actionCommand.Action, nextAction, actionCommand.Duration);

                    // Handle delay time actions separately
                    if (actionCommand.Action == "DELAY TIME")
                    {
                        await Task.Delay(actionCommand.Duration, cancellationToken);
                        continue;
                    }

                    // Process other actions that should correspond to actual key presses
                    if (keys.ActionKeyMap.TryGetValue(actionCommand.Action, out VirtualKeyCode keyCode))
                    {
                        CoreFunctionality.SendKeyDown(keyCode);
                        try
                        {
                            await Task.Delay(actionCommand.Duration, cancellationToken);
                        }
                        finally
                        {
                            // Always release held keys, including when the user cancels mid-swing.
                            CoreFunctionality.SendKeyUp(keyCode);
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
            finally
            {
                CoreFunctionality.UseBackgroundInput = previousBackgroundMode;
            }
        }
    }
}
