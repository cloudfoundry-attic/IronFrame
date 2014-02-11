using System;
using System.Runtime.InteropServices;

namespace IronFoundry.Warden.PInvoke
{
    internal partial class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct TokenUser
        {
            public SidAndAttributes User;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SidAndAttributes
        {
            public IntPtr Sid;
            public int Attributes;
        }
    }
}
