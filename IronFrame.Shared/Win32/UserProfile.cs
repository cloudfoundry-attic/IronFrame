using System.Runtime.InteropServices;
using System.Text;

namespace IronFrame.Win32
{
    partial class NativeMethods
    {
        [DllImport("userenv.dll", SetLastError = true)]
        public static extern bool DeleteProfile(string sidString, string profilePath, string computerName);

        [DllImport("userenv.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int CreateProfile(
          [MarshalAs(UnmanagedType.LPWStr)] string pszUserSid,
          [MarshalAs(UnmanagedType.LPWStr)] string pszUserName,
          [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszProfilePath,
          uint cchProfilePath);
    }
}
