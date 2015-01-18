using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Container.Win32
{
    // BR: Move this to IronFoundry.Container
    class DesktopSecurity : ObjectSecurity<NativeMethods.DesktopRights>
    {
        private SafeHandle hDesktopHandle;

        public DesktopSecurity(SafeHandle hDesktopHandle)
            : base(false, ResourceType.WindowObject, hDesktopHandle, AccessControlSections.Access)
        {
            this.hDesktopHandle = hDesktopHandle;
        }

        public void AcceptChanges()
        {
            Persist(hDesktopHandle, AccessControlSections.Access);
        }
    }
}
