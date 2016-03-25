using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IronFrame.Win32
{
    internal partial class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UInt32 WaitForSingleObject (IntPtr hHandle, UInt32 dwMilliseconds);
    }
}
