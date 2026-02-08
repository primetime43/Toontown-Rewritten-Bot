using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using WindowsInput;
using System.Diagnostics;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Services.FishingLocationsWalking
{
    public class CustomActionsFishing : FishingStrategyBase
    {
        private List<FishingActionCommand> actions = new List<FishingActionCommand>();
        private FishingActionKeys _actionKeys = new FishingActionKeys();

        /// <summary>
        /// Embedded calibration data from v2 format files.
        /// Null if file was v1 format or had no calibration data.
        /// </summary>
        public CalibrationData EmbeddedCalibration { get; private set; }

        public CustomActionsFishing(string filePath)
        {
            LoadActionsFromJson(filePath);
        }

        private void LoadActionsFromJson(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"[CustomActionsFishing] File not found: {filePath}");
                return;
            }

            var result = CustomFishingActionFileManager.Load(filePath);
            if (result.Success)
            {
                actions = result.File.Actions ?? new List<FishingActionCommand>();
                EmbeddedCalibration = result.File.Calibration;

                if (result.WasV1Format)
                {
                    Debug.WriteLine($"[CustomActionsFishing] Loaded v1 format: {actions.Count} actions");
                }
                else
                {
                    Debug.WriteLine($"[CustomActionsFishing] Loaded v2 format: {actions.Count} actions, " +
                        $"Calibration: {(EmbeddedCalibration != null ? "Yes" : "No")}");
                }
            }
            else
            {
                Debug.WriteLine($"[CustomActionsFishing] Failed to load: {result.ErrorMessage}");
            }
        }

        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            foreach (var actionCommand in actions)
            {
                // Check for cancellation at the start of each action
                cancellationToken.ThrowIfCancellationRequested();

                Debug.WriteLine($"Executing action: {actionCommand.Action}");

                if (!actionCommand.Command.StartsWith("TIME"))
                {
                    if (actionCommand.Command == "SELL")
                    {
                        await SellFishAsync(cancellationToken).ConfigureAwait(false); // Handle selling fish
                        await Task.Delay(3000, cancellationToken).ConfigureAwait(false); // Delay to ensure the selling action is complete
                    }
                    else
                    {
                        // Look up the VirtualKeyCode from our mapping
                        var keyCode = _actionKeys.GetKeyCodeFromString(actionCommand.Command);
                        if (keyCode.HasValue)
                        {
                            InputSimulator.SimulateKeyDown(keyCode.Value);
                            Debug.WriteLine($"Key down: {keyCode.Value} (from command '{actionCommand.Command}')");

                            // Find the next action to determine hold duration
                            int currentIndex = actions.IndexOf(actionCommand);
                            int delayMs = 500; // Default press duration

                            if (currentIndex + 1 < actions.Count && actions[currentIndex + 1].Action == "TIME")
                            {
                                var nextAction = actions[currentIndex + 1];
                                // Extract just digits from the command to handle malformed values like "847)"
                                string timeDigits = new string(nextAction.Command.Where(char.IsDigit).ToArray());
                                if (int.TryParse(timeDigits, out int milliseconds))
                                {
                                    delayMs = milliseconds;
                                }
                                Debug.WriteLine($"TIME action found, delay: {delayMs}ms");
                            }

                            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                            InputSimulator.SimulateKeyUp(keyCode.Value);
                            Debug.WriteLine($"Key up: {keyCode.Value}");
                        }
                        else
                        {
                            Debug.WriteLine($"WARNING: Unknown command '{actionCommand.Command}' - not a recognized key");
                        }
                    }
                }
                else
                {
                    // TIME action - extract just digits to handle malformed values
                    string timeDigits = new string(actionCommand.Command.Where(char.IsDigit).ToArray());
                    if (int.TryParse(timeDigits, out int milliseconds))
                    {
                        Debug.WriteLine($"Standalone TIME delay: {milliseconds}ms");
                        await Task.Delay(milliseconds, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}