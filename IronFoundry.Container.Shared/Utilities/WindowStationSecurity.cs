using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Container.Win32;

namespace IronFoundry.Warden.Utilities
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
