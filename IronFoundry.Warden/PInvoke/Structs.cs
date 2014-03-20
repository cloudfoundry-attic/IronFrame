namespace IronFoundry.Warden.PInvoke
{
    using System;
    using System.Runtime.InteropServices;

    public partial class NativeMethods
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
