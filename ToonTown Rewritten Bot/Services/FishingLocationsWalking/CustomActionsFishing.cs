using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using ToonTown_Rewritten_Bot.Services.FishingLocations;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services.FishingLocationsWalking
{
    public class CustomActionsFishing : FishingStrategyBase
    {
        private Dictionary<string, string> actions = new Dictionary<string, string>();

        public CustomActionsFishing()
        {

        }
        
        public CustomActionsFishing(string filePath)
        {
            LoadActionsFromJson("path_to_your_json_file.json");
        }

        private void LoadActionsFromJson(string filePath)
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                actions = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
        }

        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            foreach (var action in actions)
            {
                if (action.Value.StartsWith("TIME"))
                {
                    // Extract the number of seconds from the action.Value, which is like "TIME X"
                    if (int.TryParse(action.Value.Split(' ')[1], out int seconds))
                    {
                        await Task.Delay(seconds * 1000, cancellationToken); // Convert seconds to milliseconds
                    }
                }
                else
                {
                    // Convert action string to VirtualKeyCode and simulate key press
                    if (Enum.TryParse<VirtualKeyCode>(action.Value, out var keyCode))
                    {
                        InputSimulator.SimulateKeyDown(keyCode);
                        await Task.Delay(500, cancellationToken); // Assuming a default delay for key press
                        InputSimulator.SimulateKeyUp(keyCode);
                    }
                }
            }

            await SellFishAsync(cancellationToken);
        }
    }
}