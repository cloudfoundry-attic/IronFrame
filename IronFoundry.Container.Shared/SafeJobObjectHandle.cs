using IronFoundry.Container.Win32;
using Microsoft.Win32.SafeHandles;
using System;

namespace IronFoundry.Warden.Containers
{
    // BR: Move this to IronFoundry.Container.Shared
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

    // BR: Move this to IronFoundry.Container.Shared
    public class JobObjectWaitHandle : System.Threading.WaitHandle
    {
        public JobObjectWaitHandle(SafeJobObjectHandle jobObject) 
        {
            SafeWaitHandle = new SafeWaitHandle(jobObject.DangerousGetHandle(), false);
        }
    }
}
