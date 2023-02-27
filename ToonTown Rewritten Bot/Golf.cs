using System.Threading;
using System.Windows.Forms;
using InputSimulatorEx.Native;

namespace ToonTown_Rewritten_Bot
{
    class Golf
    {
        // GOLF- Afternoon Tee
        public static void afternoonTee()//works, finished
        {
            BotFunctions.maximizeAndFocus();
            Thread.Sleep(15000);
            toonLookAtHole();
            Thread.Sleep(3000);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(2120);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        }

        // GOLF - Holey Mackeral
        public static void holeyMackeral()//works, finished
        {
            BotFunctions.maximizeAndFocus();
            Thread.Sleep(15000);
            toonLookAtHole();
            Thread.Sleep(3000);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(1000);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        }

        // GOLF - Hole on the Range
        public static void holeOnTheRange()//needs fixed? Not sure
        {
            BotFunctions.maximizeAndFocus();
            Thread.Sleep(15000);
            toonLookAtHole();
            Thread.Sleep(3000);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(1800); // 68%
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        }

        // GOLF - Seeing green
        public static void seeingGreen()//works, finished
        {
            BotFunctions.maximizeAndFocus();
            Thread.Sleep(15000);
            toonLookAtHole();
            Thread.Sleep(3000);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(1790); // 67%
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        }

        // GOLF - Swing Time
        public static void swingTime()//yellow, needs fixed (move to the right?)
        {
            BotFunctions.maximizeAndFocus();
            Thread.Sleep(100);
            //move toon to the right location
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.RIGHT);
            Thread.Sleep(50);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.RIGHT);
            Thread.Sleep(100);
            Thread.Sleep(15000);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(2000);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        }

        // GOLF - Down the Hatch
        public static void downTheHatch()//yellow, needs fixed
        {
            BotFunctions.maximizeAndFocus();
            Thread.Sleep(15000);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(2340);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        }

        //GOLF - Peanut Putter
        public static void peanutPutter()
        {
            BotFunctions.maximizeAndFocus();
            Thread.Sleep(15000);
            toonLookAtHole();
            Thread.Sleep(3000);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(1860); // 69-70% ?
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        }

        //GOLF - Hot Links
        public static void hotLinks()
        {
            BotFunctions.maximizeAndFocus();
            Thread.Sleep(15000);
            toonLookAtHole();
            Thread.Sleep(3000);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(1800); // 67%
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        }

        //GOLF - Hole In Fun
        public static void holeInFun()
        {
            BotFunctions.maximizeAndFocus();
            Thread.Sleep(15000);
            toonLookAtHole();
            Thread.Sleep(3000);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(1300);// 52%
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        }

        //GOLF - Swing-A-Long
        public static void swingALong()
        {
            BotFunctions.maximizeAndFocus();
            Thread.Sleep(15000);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(2340);// 82%
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        }

        public static void oneLittleBirdie()
        {
            BotFunctions.maximizeAndFocus();
            Thread.Sleep(15000);
            //rotate the toon right
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.RIGHT);
            Thread.Sleep(700);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.RIGHT);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(1870); // 69-70% ?
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        }

        private static void confirmLocation()
        {
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(50);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        }

        private static void toonLookAtHole()//this is just to stop the timer
        {
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(50);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
        }
    }
}
