using System;
using System.Runtime.InteropServices;
using System.Security;

namespace IronFoundry.Container.Win32
{
    [SuppressUnmanagedCodeSecurity]
    public partial class NativeMethods
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlFillMemory")]
        public static extern void FillMemory(
            IntPtr ptr, 
            [MarshalAs(UnmanagedType.SysUInt)] IntPtr length, 
            byte value);
    }
}
