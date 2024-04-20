using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;
using System.Threading;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Views;
using ToonTown_Rewritten_Bot.Models;

namespace ToonTown_Rewritten_Bot.Services
{
    public class BotFunctions
    {
        private static Dictionary<string, string> _dataFileMap = new Dictionary<string, string>();
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
            CoreFunctionality.maximizeAndFocus(); // Ensure the game window is focused
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

        /*public static Dictionary<string, string> GetDataFileMap()
        {
            return _dataFileMap;
        }*/

        public static void CreateItemsDataFileMap()
        {
            //Gardening Coords
            _dataFileMap.Add(((int)Coordinates.GardeningCoordinatesEnum.PlantFlowerRemoveButton).ToString(), "Plant Flower/Remove Button");
            _dataFileMap.Add(((int)Coordinates.GardeningCoordinatesEnum.RedJellybeanButton).ToString(), "Red Jellybean Button");
            _dataFileMap.Add(((int)Coordinates.GardeningCoordinatesEnum.GreenJellybeanButton).ToString(), "Green Jellybean Button");
            _dataFileMap.Add(((int)Coordinates.GardeningCoordinatesEnum.OrangeJellybeanButton).ToString(), "Orange Jellybean Button");
            _dataFileMap.Add(((int)Coordinates.GardeningCoordinatesEnum.PurpleJellybeanButton).ToString(), "Purple Jellybean Button");
            _dataFileMap.Add(((int)Coordinates.GardeningCoordinatesEnum.BlueJellybeanButton).ToString(), "Blue Jellybean Button");
            _dataFileMap.Add(((int)Coordinates.GardeningCoordinatesEnum.PinkJellybeanButton).ToString(), "Pink Jellybean Button");
            _dataFileMap.Add(((int)Coordinates.GardeningCoordinatesEnum.YellowJellybeanButton).ToString(), "Yellow Jellybean Button");
            _dataFileMap.Add(((int)Coordinates.GardeningCoordinatesEnum.CyanJellybeanButton).ToString(), "Cyan Jellybean Button");
            _dataFileMap.Add(((int)Coordinates.GardeningCoordinatesEnum.SilverJellybeanButton).ToString(), "Silver Jellybean Button");
            _dataFileMap.Add(((int)Coordinates.GardeningCoordinatesEnum.BluePlantButton).ToString(), "Blue Plant Button");
            _dataFileMap.Add(((int)Coordinates.GardeningCoordinatesEnum.BlueOkButton).ToString(), "Blue Ok Button");
            _dataFileMap.Add(((int)Coordinates.GardeningCoordinatesEnum.WateringCanButton).ToString(), "Watering Can Button");
            _dataFileMap.Add(((int)Coordinates.GardeningCoordinatesEnum.BlueYesButton).ToString(), "Blue Yes Button");
            //Fishing Coords
            _dataFileMap.Add(((int)Coordinates.FishingCoordinatesEnum.RedFishingButton).ToString(), "Red Fishing Button");
            _dataFileMap.Add(((int)Coordinates.FishingCoordinatesEnum.ExitFishingButton).ToString(), "Exit Fishing Button");
            _dataFileMap.Add(((int)Coordinates.FishingCoordinatesEnum.BlueSellAllButton).ToString(), "Blue Sell All Button");
            //Racing Coords
            //Doodle Training Coords
            _dataFileMap.Add(((int)Coordinates.DoodleTrainingCoordinatesEnum.FeedDoodleButton).ToString(), "Feed Doodle Button");
            _dataFileMap.Add(((int)Coordinates.DoodleTrainingCoordinatesEnum.ScratchDoodleButton).ToString(), "Scratch Doodle Button");
            _dataFileMap.Add(((int)Coordinates.DoodleTrainingCoordinatesEnum.GreenSpeedChatButton).ToString(), "Green SpeedChat Button");
            _dataFileMap.Add(((int)Coordinates.DoodleTrainingCoordinatesEnum.PetsTabInSpeedChat).ToString(), "Pets Tab in SpeedChat");
            _dataFileMap.Add(((int)Coordinates.DoodleTrainingCoordinatesEnum.TricksTabInSpeedChat).ToString(), "Tricks Tab in SpeedChat");
            _dataFileMap.Add(((int)Coordinates.DoodleTrainingCoordinatesEnum.JumpTrickOptionInSpeedChat).ToString(), "Jump Trick Option in SpeedChat");
            _dataFileMap.Add(((int)Coordinates.DoodleTrainingCoordinatesEnum.BegTrickOptionInSpeedChat).ToString(), "Beg Trick Option in SpeedChat");
            _dataFileMap.Add(((int)Coordinates.DoodleTrainingCoordinatesEnum.PlayDeadTrickOptionInSpeedChat).ToString(), "Play Dead Trick Option in SpeedChat");
            _dataFileMap.Add(((int)Coordinates.DoodleTrainingCoordinatesEnum.RolloverTrickOptionInSpeedChat).ToString(), "Rollover Trick Option in SpeedChat");
            _dataFileMap.Add(((int)Coordinates.DoodleTrainingCoordinatesEnum.BackflipTrickOptionInSpeedChat).ToString(), "Backflip Trick Option in SpeedChat");
            _dataFileMap.Add(((int)Coordinates.DoodleTrainingCoordinatesEnum.DanceTrickOptionInSpeedChat).ToString(), "Dance Trick Option in SpeedChat");
            _dataFileMap.Add(((int)Coordinates.DoodleTrainingCoordinatesEnum.SpeakTrickOptionInSpeedChat).ToString(), "Speak Trick Option in SpeedChat");
        }
    }
}
