using System.Runtime.InteropServices;

namespace IronFrame.Win32
{
    partial class NativeMethods
    {
        [DllImport("userenv.dll", SetLastError = true)]
        public static extern bool DeleteProfile(string sidString, string profilePath, string computerName);

    }
}
