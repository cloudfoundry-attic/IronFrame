using System.Runtime.InteropServices;
using System.Security.AccessControl;
using IronFoundry.Container.Win32;

namespace IronFoundry.Container.Utilities
{
    // BR: Move this to IronFoundry.Container
    class WindowStationSecurity : ObjectSecurity<NativeMethods.WindowStationRights>
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
