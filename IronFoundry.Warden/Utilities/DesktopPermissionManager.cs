namespace IronFoundry.Warden.Utilities
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using Asprosys.Security.AccessControl;

    public interface IDesktopPermissionManager
    {
        void AddDesktopPermission(string userName);
        void RemoveDesktopPermission(string userName);
    }

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

        public DesktopPermissionManager()
        {
        }

        public void AddDesktopPermission(string userName)
        {
            IntPtr hWindowStation = GetProcessWindowStation();
            var ws = new WindowStationSecurity(hWindowStation, AccessControlSections.Access);
            ws.AddAccessRule(new WindowStationAccessRule(userName, WindowStationRights.AllAccess, AccessControlType.Allow));
            ws.AcceptChanges();

            IntPtr hDesktopThread = GetThreadDesktop(GetCurrentThreadId());
            var ds = new DesktopSecurity(hDesktopThread, AccessControlSections.Access);
            ds.AddAccessRule(new DesktopAccessRule(userName, DesktopRights.AllAccess, AccessControlType.Allow));
            ds.AcceptChanges();
        }

        public void RemoveDesktopPermission(string userName)
        {
            IntPtr hWindowStation = GetProcessWindowStation();
            var ws = new WindowStationSecurity(hWindowStation, AccessControlSections.Access);
            ws.RemoveAccessRule(new WindowStationAccessRule(userName, WindowStationRights.AllAccess, AccessControlType.Allow));
            ws.AcceptChanges();

            IntPtr hDesktopThread = GetThreadDesktop(GetCurrentThreadId());
            var ds = new DesktopSecurity(hDesktopThread, AccessControlSections.Access);
            ds.RemoveAccessRule(new DesktopAccessRule(userName, DesktopRights.AllAccess, AccessControlType.Allow));
            ds.AcceptChanges();
        }
    }
}
