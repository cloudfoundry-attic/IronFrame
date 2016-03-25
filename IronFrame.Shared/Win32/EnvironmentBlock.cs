using System;
using System.Runtime.InteropServices;

namespace IronFrame.Win32
{
    internal partial class NativeMethods
    {
        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/bb762270(v=vs.85).aspx
        /// </summary>
        [DllImport("userenv.dll", SetLastError = true)]
        public static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/bb762274%28v=vs.85%29.aspx
        /// </summary>
        [DllImport("userenv.dll", SetLastError = true)]
        public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);
    }
}
