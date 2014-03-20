using IronFoundry.Warden.PInvoke;
using Microsoft.Win32.SafeHandles;
using System;

namespace IronFoundry.Warden.Containers
{
    public class SafeJobObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeJobObjectHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(handle);
        }
    }
}
