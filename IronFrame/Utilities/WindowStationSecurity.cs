using System.Runtime.InteropServices;
using System.Security.AccessControl;
using IronFrame.Win32;

namespace IronFrame.Utilities
{
    internal class WindowStationSecurity : ObjectSecurity<NativeMethods.WindowStationRights>
    {
        private SafeHandle hWindowStation;

        public WindowStationSecurity(SafeHandle hWindowStation)
            : base(false, ResourceType.WindowObject, hWindowStation, AccessControlSections.Access)
        {
            this.hWindowStation = hWindowStation;
        }

        public void AcceptChanges()
        {
            Persist(hWindowStation, AccessControlSections.Access);
        }
    }
}
