using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services.CustomGolfActions;
using ToonTown_Rewritten_Bot.Views;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services
{
    class Golf
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
                Console.WriteLine($"An error occurred: {ex.Message}");
                // Handle other exceptions, possibly related to JSON parsing or key lookup failures
            }
        }

        // GOLF- Afternoon Tee
        public static async void AfternoonTee()//works, finished
        {
            CoreFunctionality.maximizeAndFocus();
            await Task.Delay(15000);
            ToonLookAtHole();
            await Task.Delay(3000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            await Task.Delay(2120);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        // GOLF - Holey Mackeral
        public static async void HoleyMackeral()//works, finished
        {
            CoreFunctionality.maximizeAndFocus();
            await Task.Delay(15000);
            ToonLookAtHole();
            await Task.Delay(3000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            await Task.Delay(1000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        // GOLF - Hole on the Range
        public static async void HoleOnTheRange()//needs fixed? Not sure
        {
            CoreFunctionality.maximizeAndFocus();
            await Task.Delay(15000);
            ToonLookAtHole();
            await Task.Delay(3000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            await Task.Delay(1800); // 68%
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        // GOLF - Seeing green
        public static async void SeeingGreen()//works, finished
        {
            CoreFunctionality.maximizeAndFocus();
            await Task.Delay(15000);
            ToonLookAtHole();
            await Task.Delay(3000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            await Task.Delay(1790); // 67%
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        // GOLF - Swing Time
        public static async void SwingTime()//yellow, needs fixed (move to the right?)
        {
            CoreFunctionality.maximizeAndFocus();
            await Task.Delay(100);
            //move toon to the right location
            InputSimulator.SimulateKeyDown(VirtualKeyCode.RIGHT);
            await Task.Delay(50);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.RIGHT);
            await Task.Delay(100);
            await Task.Delay(15000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            await Task.Delay(2000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        // GOLF - Down the Hatch
        public static async void DownTheHatch()//yellow, needs fixed
        {
            CoreFunctionality.maximizeAndFocus();
            await Task.Delay(15000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            await Task.Delay(2340);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        //GOLF - Peanut Putter
        public static async void PeanutPutter()
        {
            CoreFunctionality.maximizeAndFocus();
            await Task.Delay(15000);
            ToonLookAtHole();
            await Task.Delay(3000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            await Task.Delay(1860); // 69-70% ?
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        //GOLF - Hot Links
        public static async void HotLinks()
        {
            CoreFunctionality.maximizeAndFocus();
            await Task.Delay(15000);
            ToonLookAtHole();
            await Task.Delay(3000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            await Task.Delay(1800); // 67%
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        //GOLF - Hole In Fun
        public static async void HoleInFun()
        {
            CoreFunctionality.maximizeAndFocus();
            await Task.Delay(15000);
            ToonLookAtHole();
            await Task.Delay(3000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            await Task.Delay(1300);// 52%
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        //GOLF - Swing-A-Long
        public static async void SwingALong()
        {
            CoreFunctionality.maximizeAndFocus();
            await Task.Delay(15000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            await Task.Delay(2340);// 82%
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        public static async void OneLittleBirdie()
        {
            CoreFunctionality.maximizeAndFocus();
            await Task.Delay(15000);
            //rotate the toon right
            InputSimulator.SimulateKeyDown(VirtualKeyCode.RIGHT);
            await Task.Delay(700);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.RIGHT);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            await Task.Delay(1870); // 69-70% ?
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        private static async void ConfirmLocation()
        {
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            await Task.Delay(50);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        private static async void ToonLookAtHole()//this is just to stop the timer
        {
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            await Task.Delay(50);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);
        }
    }
}
