using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;
using System.Threading;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    class Misc
    {
        public static Boolean sendMessage(String message, int spamCount, bool spam, NumericUpDown upDown)
        {
            DialogResult confirmation;
            Console.WriteLine("Spam? : " + spam);
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
                            send(message);
                            spamCount--;
                            if (spamCount != 0)
                                upDown.Value = spamCount;
                        }
                    }
                    else if (!spam || spamCount == 1)
                        send(message);
                }
                else if (confirmation.Equals(DialogResult.No) || confirmation.Equals(DialogResult.Cancel))
                    return false;
            }
            else
                MessageBox.Show("You must enter a message to send!");
            return false;
        }
        private static void send(String text)
        {
            BotFunctions.DoMouseClick();
            Thread.Sleep(500);
            InputSimulator.SimulateTextEntry(text);
            Thread.Sleep(500);
            SendKeys.SendWait("{ENTER}");
        }

        public static Boolean keepToonAwake(int min)
        {
            DateTime endTime = DateTime.Now.AddMinutes(min);
            BotFunctions.DoMouseClick();
            while (endTime > DateTime.Now)
            {
                SendKeys.SendWait("^");
                if (Control.ModifierKeys == Keys.Alt)//break out of loop
                    return true;
            }
            return false;
        }
    }
}
