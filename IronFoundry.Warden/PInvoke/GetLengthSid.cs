using System;
using System.Runtime.InteropServices;

namespace IronFoundry.Warden.PInvoke
{
    internal partial class NativeMethods
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern uint GetLengthSid(IntPtr pSid);
    }
}