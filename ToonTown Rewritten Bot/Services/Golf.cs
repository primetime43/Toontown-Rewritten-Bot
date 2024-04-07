﻿using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Views;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services
{
    class Golf
    {
        // GOLF- Afternoon Tee
        public static void afternoonTee()//works, finished
        {
            Debug.WriteLine("1");
            CommonFunctionality.maximizeAndFocus();
            Thread.Sleep(15000);
            Debug.WriteLine("2");
            toonLookAtHole();
            Thread.Sleep(3000);
            Debug.WriteLine("3");
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(2120);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
            Debug.WriteLine("4");
        }

        // GOLF - Holey Mackeral
        public static void holeyMackeral()//works, finished
        {
            CommonFunctionality.maximizeAndFocus();
            Thread.Sleep(15000);
            toonLookAtHole();
            Thread.Sleep(3000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(1000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        // GOLF - Hole on the Range
        public static void holeOnTheRange()//needs fixed? Not sure
        {
            CommonFunctionality.maximizeAndFocus();
            Thread.Sleep(15000);
            toonLookAtHole();
            Thread.Sleep(3000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(1800); // 68%
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        // GOLF - Seeing green
        public static void seeingGreen()//works, finished
        {
            CommonFunctionality.maximizeAndFocus();
            Thread.Sleep(15000);
            toonLookAtHole();
            Thread.Sleep(3000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(1790); // 67%
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        // GOLF - Swing Time
        public static void swingTime()//yellow, needs fixed (move to the right?)
        {
            CommonFunctionality.maximizeAndFocus();
            Thread.Sleep(100);
            //move toon to the right location
            InputSimulator.SimulateKeyDown(VirtualKeyCode.RIGHT);
            Thread.Sleep(50);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.RIGHT);
            Thread.Sleep(100);
            Thread.Sleep(15000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(2000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        // GOLF - Down the Hatch
        public static void downTheHatch()//yellow, needs fixed
        {
            CommonFunctionality.maximizeAndFocus();
            Thread.Sleep(15000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(2340);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        //GOLF - Peanut Putter
        public static void peanutPutter()
        {
            CommonFunctionality.maximizeAndFocus();
            Thread.Sleep(15000);
            toonLookAtHole();
            Thread.Sleep(3000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(1860); // 69-70% ?
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        //GOLF - Hot Links
        public static void hotLinks()
        {
            CommonFunctionality.maximizeAndFocus();
            Thread.Sleep(15000);
            toonLookAtHole();
            Thread.Sleep(3000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(1800); // 67%
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        //GOLF - Hole In Fun
        public static void holeInFun()
        {
            CommonFunctionality.maximizeAndFocus();
            Thread.Sleep(15000);
            toonLookAtHole();
            Thread.Sleep(3000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(1300);// 52%
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        //GOLF - Swing-A-Long
        public static void swingALong()
        {
            CommonFunctionality.maximizeAndFocus();
            Thread.Sleep(15000);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(2340);// 82%
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        public static void oneLittleBirdie()
        {
            CommonFunctionality.maximizeAndFocus();
            Thread.Sleep(15000);
            //rotate the toon right
            InputSimulator.SimulateKeyDown(VirtualKeyCode.RIGHT);
            Thread.Sleep(700);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.RIGHT);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(1870); // 69-70% ?
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        private static void confirmLocation()
        {
            InputSimulator.SimulateKeyDown(VirtualKeyCode.CONTROL);
            Thread.Sleep(50);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
        }

        private static void toonLookAtHole()//this is just to stop the timer
        {
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            Thread.Sleep(50);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);
        }
    }
}
