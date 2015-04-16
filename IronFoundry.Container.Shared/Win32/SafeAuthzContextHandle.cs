using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace IronFoundry.Container.Win32
{
    internal class SafeAuthzContextHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeAuthzContextHandle()
            : base(true)
        {
        }

        public SafeAuthzContextHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
        }

        /// <summary>
        ///     Release the resource manager handle held by this instance
        /// </summary>
        /// <returns>true if the release was successful. false otherwise.</returns>
        protected override bool ReleaseHandle()
        {
            return AuthzFreeContext(handle);
        }

        [DllImport(NativeDll.AUTHZ_DLL, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AuthzFreeContext(IntPtr handle);
    }
}