using IronFoundry.Container.Win32;
using Microsoft.Win32.SafeHandles;
using System;

namespace IronFoundry.Container.Utilities
{
    internal class SafeJobObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
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

    internal class JobObjectWaitHandle : System.Threading.WaitHandle
    {
        public JobObjectWaitHandle(SafeJobObjectHandle jobObject) 
        {
            SafeWaitHandle = new SafeWaitHandle(jobObject.DangerousGetHandle(), false);
        }
    }
}
