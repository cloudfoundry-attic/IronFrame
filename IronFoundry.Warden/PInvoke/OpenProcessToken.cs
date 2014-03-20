namespace IronFoundry.Warden.PInvoke
{
    using System;
    using System.Runtime.InteropServices;

    public partial class NativeMethods
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetExitCodeProcess(IntPtr Handle, out uint exitCode);
    }
}