using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace IronFrame.Win32
{
    internal partial class NativeMethods
    {
        public enum LogonFlags
        {
            WithProfile = 1,
            NetCredentialsOnly = 2
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean CreateProcessWithTokenW
        (
            SafeFileHandle hToken,
            LogonFlags dwLogonFlags,
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
