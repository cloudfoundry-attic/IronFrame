using System;
using System.Runtime.InteropServices;

namespace IronFrame.Win32
{
    internal partial class NativeMethods
    {
        public enum ConsoleControlEvent : int
        {
            ControlC = 0,
            ControlBreak = 1,
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GenerateConsoleCtrlEvent(ConsoleControlEvent ctrlEvent, int processId);
    }
}
