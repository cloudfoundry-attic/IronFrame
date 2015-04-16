using System;
using System.Runtime.InteropServices;

namespace IronFrame.Win32
{
    internal partial class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public class SecurityAttributes
        {
            public Int32 Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;

            public SecurityAttributes()
            {
                this.Length = Marshal.SizeOf(this); 
            }
        }
    }
}
