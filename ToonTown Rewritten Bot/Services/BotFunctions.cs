using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services
{
    public class BotFunctions
    {
        public static bool SendMessage(string message, int spamCount, bool spam, NumericUpDown upDown)
        {
            DialogResult confirmation;
            if (!message.Equals(""))
            {
                confirmation = MessageBox.Show("Send Message?", "Continue...", MessageBoxButtons.YesNo);
                if (confirmation.Equals(DialogResult.Yes))
                {
                    if (spam && spamCount > 1)//spam checkbox check
                    {
                        while (spamCount >= 1)
                        {
                            if (Control.ModifierKeys == Keys.Alt)//break out of loop
                            {
                                upDown.Value = 1;
                                return true;
                            }
                            Send(message);
                            spamCount--;
                            if (spamCount != 0)
                                upDown.Value = spamCount;
                        }
                    }
                    else if (!spam || spamCount == 1)
                        Send(message);
                }
                else if (confirmation.Equals(DialogResult.No) || confirmation.Equals(DialogResult.Cancel))
                    return false;
            }
            else
                MessageBox.Show("You must enter a message to Send!");
            return false;
        }
        private static void Send(string text)
        {
            CoreFunctionality.DoMouseClick();
            Thread.Sleep(500);
            InputSimulator.SimulateTextEntry(text);
            Thread.Sleep(500);
            SendKeys.SendWait("{ENTER}");
        }

        public static async Task KeepToonAwake(int timeInSeconds, CancellationToken cancellationToken)
        {
            CoreFunctionality.FocusTTRWindow(); // Ensure the game window is focused
            DateTime endTime = DateTime.Now.AddSeconds(timeInSeconds); // Calculate the end time based on seconds
            CoreFunctionality.DoMouseClick(); // Initial action to "keep awake"

            try
            {
                while (endTime > DateTime.Now)
                {
                    cancellationToken.ThrowIfCancellationRequested(); // Check for cancellation
                    SendKeys.SendWait("^"); // Simulate key press to keep the toon awake
                    await Task.Delay(1000, cancellationToken); // Wait for a second before the next key press
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
