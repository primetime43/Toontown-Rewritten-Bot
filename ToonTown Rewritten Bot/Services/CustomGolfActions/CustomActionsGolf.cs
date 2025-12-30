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
    public class CustomActionsGolf
    {
        private List<GolfActionCommand> actions = new List<GolfActionCommand>();

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

        public async Task PerformGolfActions(CancellationToken cancellationToken)
        {
            CoreFunctionality.MaximizeAndFocusTTRWindow();
            await Task.Delay(1000, cancellationToken); // Initial delay before starting actions
            GolfActionKeys keys = new GolfActionKeys();

            foreach (var actionCommand in actions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Handle delay time actions separately
                if (actionCommand.Action == "DELAY TIME")
                {
                    await Task.Delay(actionCommand.Duration, cancellationToken);
                    continue; // Skip the rest of the loop for delay actions
                }

                // Process other actions that should correspond to actual key presses
                if (keys.ActionKeyMap.TryGetValue(actionCommand.Action, out VirtualKeyCode keyCode))
                {
                    // Tee movements use the duration to wait after the movement, but do not apply delay during key press.
                    if (actionCommand.Action == "MOVE TO RIGHT TEE SPOT" || actionCommand.Action == "MOVE TO LEFT TEE SPOT")
                    {
                        // Hold the key for the specified duration to move to the tee spot
                        InputSimulator.SimulateKeyDown(keyCode);
                        await Task.Delay(actionCommand.Duration, cancellationToken);
                        InputSimulator.SimulateKeyUp(keyCode);
                        await PrepareToHitBall(); // Prepare for the next golf swing
                    }
                    else
                    {
                        // For all other actions, hold the key for the duration specified, then release
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
        }
    }
}