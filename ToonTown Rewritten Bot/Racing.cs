using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

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
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(6000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            //turn left
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(500);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(500);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            //turn left more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(500);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(500);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            //turn left more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(500);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(800);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            //turn left more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(500);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(1000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            //short turn left
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(500);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(1000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            //short turn left
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(450);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);

            //straight away
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(7000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);
        }

        private static void makeSecondBendScrewball()
        {
            //straight away
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(6000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            //turn left
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(800);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(500);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            //turn left more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(800);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(500);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            //turn left more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(600);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(800);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            //turn left more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(500);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(1000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            /*//short turn left
            robot.keyPress(KeyEvent.VK_LEFT);
            robot.delay(500);
            robot.keyRelease(KeyEvent.VK_LEFT);*/

            //go straight more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(500);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            //short turn left
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(100);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);

            //straight away
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(7000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);
        }

        private static void makeThirdBendScrewball()
        {
            Thread.Sleep(1300);
            //short turn left
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(700);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);

            //straight away
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(1000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            //turn left
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(700);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);

            //go straight more
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(2000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);
        }
    }
}
