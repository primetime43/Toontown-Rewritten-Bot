using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using WindowsInput;
using System.Diagnostics;
using ToonTown_Rewritten_Bot.Models;

namespace ToonTown_Rewritten_Bot.Services.FishingLocationsWalking
{
    public class CustomActionsFishing : FishingStrategyBase
    {
        private List<FishingActionCommand> actions = new List<FishingActionCommand>();

        public CustomActionsFishing(string filePath)
        {
            LoadActionsFromJson(filePath);
        }

        private void LoadActionsFromJson(string filePath)
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                actions = JsonConvert.DeserializeObject<List<FishingActionCommand>>(json);
            }
        }

        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            foreach (var actionCommand in actions)
            {
                Debug.WriteLine($"Executing action: {actionCommand.Action}");

                if (!actionCommand.Command.StartsWith("TIME"))
                {
                    if (actionCommand.Command == "SELL")
                    {
                        await SellFishAsync(cancellationToken); // Handle selling fish
                        await Task.Delay(3000, cancellationToken); // Delay to ensure the selling action is complete
                    }
                    else if (Enum.TryParse<VirtualKeyCode>(actionCommand.Command, out var keyCode))
                    {
                        InputSimulator.SimulateKeyDown(keyCode);
                        // Find the next action
                        int currentIndex = actions.IndexOf(actionCommand);
                        if (currentIndex + 1 < actions.Count && actions[currentIndex + 1].Action == "TIME")
                        {
                            var nextAction = actions[currentIndex + 1];
                            if (int.TryParse(nextAction.Command.Split(' ')[0], out int milliseconds))
                            {
                                await Task.Delay(milliseconds, cancellationToken);
                                InputSimulator.SimulateKeyUp(keyCode);
                            }
                        }
                        else
                        {
                            await Task.Delay(500, cancellationToken); // Default press duration for keys without a specified time
                            InputSimulator.SimulateKeyUp(keyCode);
                        }
                    }
                }
                else
                {
                    if (int.TryParse(actionCommand.Command.Split(' ')[0], out int milliseconds))
                    {
                        await Task.Delay(milliseconds, cancellationToken); // Wait for the specified time in milliseconds
                    }
                }
            }
        }
    }
}