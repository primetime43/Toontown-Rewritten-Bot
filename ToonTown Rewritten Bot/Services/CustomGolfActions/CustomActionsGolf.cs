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

        private async void PrepareToHitBall()
        {
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            await Task.Delay(50);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);
        }

        public async Task PerformGolfActions(CancellationToken cancellationToken)
        {
            CoreFunctionality.maximizeAndFocus();
            await Task.Delay(1000, cancellationToken); // Initial delay before starting actions
            GolfActionKeys keys = new GolfActionKeys();

            foreach (var actionCommand in actions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (actionCommand.Action == "TIME")
                {
                    // Directly delay for the specified duration without any key press
                    await Task.Delay(actionCommand.Duration, cancellationToken);
                }
                else if (keys.ActionKeyMap.TryGetValue(actionCommand.Action, out VirtualKeyCode keyCode))
                {
                    // Tee movements use the duration to wait after the movement, but do not apply delay during key press.
                    if (actionCommand.Action == "MOVE TO RIGHT TEE SPOT" || actionCommand.Action == "MOVE TO LEFT TEE SPOT")
                    {
                        InputSimulator.SimulateKeyDown(keyCode);
                        // Press and release key immediately for tee spot moves
                        InputSimulator.SimulateKeyUp(keyCode);
                        PrepareToHitBall(); // Assuming this function correctly prepares for the next golf swing
                        await Task.Delay(actionCommand.Duration, cancellationToken); // Use specified duration to delay after moving to tee spot
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
                    MessageBox.Show($"Unsupported action: {actionCommand.Action}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}