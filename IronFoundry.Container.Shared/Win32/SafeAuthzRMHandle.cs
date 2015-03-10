using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace IronFoundry.Container.Win32
{
    internal class SafeAuthzRMHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeAuthzRMHandle()
            : base(true)
        { }

        public SafeAuthzRMHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
        }

        /// <summary>
        /// Release the resource manager handle held by this instance
        /// </summary>
        /// <returns>true if the release was successful. false otherwise.</returns>        
        protected override bool ReleaseHandle()
        {
            return AuthzFreeResourceManager(handle);
        }

        [DllImport(NativeDll.AUTHZ_DLL, SetLastError = true),
         SuppressUnmanagedCodeSecurity,
         ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AuthzFreeResourceManager(IntPtr handle);
    }
}
