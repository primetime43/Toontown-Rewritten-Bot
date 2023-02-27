using InputSimulatorEx;
using InputSimulatorEx.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToonTown_Rewritten_Bot
{
    public static class InputManager
    {
        public static InputSimulator InputSim = new InputSimulator();

        //Wrap InputSimulator functions in here to simplify this code InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP); to be shorter.
        public static void KeyDown(VirtualKeyCode keyCode)
        {
            InputSim.Keyboard.KeyDown(keyCode);
        }

        public static void KeyUp(VirtualKeyCode keyCode)
        {
            InputSim.Keyboard.KeyUp(keyCode);
        }
    }
}
