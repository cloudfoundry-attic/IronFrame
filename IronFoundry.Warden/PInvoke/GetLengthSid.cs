namespace IronFoundry.Warden.PInvoke
{
    using System;
    using System.Runtime.InteropServices;

    internal partial class NativeMethods
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern uint GetLengthSid(IntPtr pSid);
    }
}