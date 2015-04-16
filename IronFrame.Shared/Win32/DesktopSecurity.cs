using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace IronFrame.Win32
{
    // BR: Move this to IronFrame
    internal class DesktopSecurity : ObjectSecurity<NativeMethods.DesktopRights>
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
