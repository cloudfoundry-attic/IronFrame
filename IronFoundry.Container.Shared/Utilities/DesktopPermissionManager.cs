using IronFoundry.Container.Win32;

namespace IronFoundry.Container.Utilities
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;

    // BR: Move this to IronFoundry.Container
    public interface IDesktopPermissionManager
    {
        void AddDesktopPermission(string userName);
        void RemoveDesktopPermission(string userName);
    }

    // BR: Move this to IronFoundry.Container
    public class DesktopPermissionManager : IDesktopPermissionManager
    {
        // No need to close handle.
        // http://msdn.microsoft.com/en-us/library/windows/desktop/ms683225(v=vs.85).aspx
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetProcessWindowStation();

        // No need to close handle.
        // http://msdn.microsoft.com/en-us/library/windows/desktop/ms683232(v=vs.85).aspx
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetThreadDesktop(int dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetCurrentThreadId();

        /// <summary>
        /// Used to help provide SafeHandle conformance for
        /// API's that need SafeHanle but those handles don't need 
        /// actual releasing (e.g. GetProcessWindowStation)
        /// </summary>
        class NonReleasingSafeHandle : SafeHandle
        {
            public NonReleasingSafeHandle(IntPtr handle, bool ownsHandle)
                : base(handle, false)
            {
            }

            public override bool IsInvalid
            {
                get { return handle == IntPtr.Zero; }
            }

            protected override bool ReleaseHandle()
            {
                // This SafeHandle doe snot need releasing.
                return true;
            }
        }

        public void AddDesktopPermission(string userOrGroupName)
        {
            SafeHandle shWindowStation = new NonReleasingSafeHandle(GetProcessWindowStation(), false);
            var ws = new WindowStationSecurity(shWindowStation);
            ws.AddAccessRule(new AccessRule<NativeMethods.WindowStationRights>(userOrGroupName, NativeMethods.WindowStationRights.AllAccess, AccessControlType.Allow));
            ws.AcceptChanges();

            SafeHandle shDesktopThread = new NonReleasingSafeHandle(GetThreadDesktop(GetCurrentThreadId()), false);
            var ds = new DesktopSecurity(shDesktopThread);
            ds.AddAccessRule(new AccessRule<NativeMethods.DesktopRights>(userOrGroupName, NativeMethods.DesktopRights.AllAccess, AccessControlType.Allow));
            ds.AcceptChanges();
        }

        public void RemoveDesktopPermission(string userOrGroupName)
        {
            SafeHandle shWindowStation = new NonReleasingSafeHandle(GetProcessWindowStation(), false);
            var ws = new WindowStationSecurity(shWindowStation);
            ws.RemoveAccessRule(new AccessRule<NativeMethods.WindowStationRights>(userOrGroupName, NativeMethods.WindowStationRights.AllAccess, AccessControlType.Allow));
            ws.AcceptChanges();

            SafeHandle shDesktopThread = new NonReleasingSafeHandle(GetThreadDesktop(GetCurrentThreadId()), false);
            var ds = new DesktopSecurity(shDesktopThread);
            ds.RemoveAccessRule(new AccessRule<NativeMethods.DesktopRights>(userOrGroupName, NativeMethods.DesktopRights.AllAccess, AccessControlType.Allow));
            ds.AcceptChanges();
        }
    }
}
