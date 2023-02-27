using InputSimulatorEx.Native;
using System.Threading;

namespace ToonTown_Rewritten_Bot
{
    class Racing
    {
        public static void startRacing()
        {
            makeFirstBendScrewball();//first bend

            makeSecondBendScrewball();//second bend

            makeThirdBendScrewball();//third bend
        }

        private static void makeFirstBendScrewball()
        {
            //straight away
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(6000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);

            //turn left
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(500);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(500);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);

            //turn left more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(500);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(500);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);

            //turn left more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(500);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(800);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);

            //turn left more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(500);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(1000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);

            //short turn left
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(500);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(1000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);

            //short turn left
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(450);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);

            //straight away
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(7000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
        }

        private static void makeSecondBendScrewball()
        {
            //straight away
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(6000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);

            //turn left
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(800);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(500);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);

            //turn left more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(800);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(500);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);

            //turn left more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(600);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(800);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);

            //turn left more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(500);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(1000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);

            /*//short turn left
            robot.keyPress(KeyEvent.VK_LEFT);
            robot.delay(500);
            robot.keyRelease(KeyEvent.VK_LEFT);*/

            //go straight more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(500);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);

            //short turn left
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(100);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);

            //straight away
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(7000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
        }

        private static void makeThirdBendScrewball()
        {
            Thread.Sleep(1300);
            //short turn left
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(700);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);

            //straight away
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(1000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);

            //turn left
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(700);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(2000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
        }
    }
}
