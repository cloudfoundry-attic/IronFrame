using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace IronFrame.Win32
{
    internal partial class NativeMethods
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean CreateProcessWithTokenW
        (
            SafeFileHandle hToken,
            uint dwLogonFlags,
            String lpApplicationName,
            String lpCommandLine,
            CreateProcessFlags dwCreationFlags,
            IntPtr lpEnvironment,
            String lpCurrentDirectory,
            ref StartupInfo lpStartupInfo,
            out ProcessInformation lpProcessInformation
        );
    }
}
