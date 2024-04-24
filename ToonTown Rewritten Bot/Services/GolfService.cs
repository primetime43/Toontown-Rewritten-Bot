using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Services.CustomGolfActions;
using ToonTown_Rewritten_Bot.Views;
using WindowsInput;
using static System.Windows.Forms.Design.AxImporter;

namespace ToonTown_Rewritten_Bot.Services
{
    class GolfService
    {
        public static async Task StartCustomGolfAction(string filePath, CancellationToken cancellationToken)
        {
            // Initialize the CustomActionsGolf with the file path
            CustomActionsGolf customGolfActions = new CustomActionsGolf(filePath);

            try
            {
                // Perform the actions read from the JSON file
                await customGolfActions.PerformGolfActions(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation was canceled by the user.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        public static GolfActionCommand[] GetCustomGolfActions(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.WriteLine("File does not exist: " + filePath);
                return new GolfActionCommand[0]; // Return an empty array if the file doesn't exist
            }

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<GolfActionCommand[]>(json);
        }

        public static string GetCustomGolfActionFilePath(string fileName)
        {
            // Get the directory where the executable is running.
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // Combine the executable path with the "Custom Golf Actions" folder name and the file name.
            return Path.Combine(exePath, "Custom Golf Actions", fileName + ".json");
        }
    }
}
