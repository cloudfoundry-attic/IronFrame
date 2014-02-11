using System;
using System.Runtime.InteropServices;

namespace IronFoundry.Warden.PInvoke
{
    internal partial class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetProcessWindowStation();
    }
}
