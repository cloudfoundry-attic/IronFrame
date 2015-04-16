using System;

namespace IronFoundry.Container.Win32
{
    internal partial class NativeMethods
    {
        // From WinUser.h
        [Flags]
        public enum WindowStationRights
        {
            EnumDesktops = 0x0001,
            ReadAttributes = 0x0002,
            AccessClipboard = 0x0004,
            CreateDesktop = 0x0008,
            WriteAttributes = 0x0010,
            AccessGlobalAtoms = 0x0020,
            ExitWindows = 0x0040,
            Enumerate = 0x0100,
            ReadScreen = 0x0200,

            // Standard Rights
            Delete = 0x00010000,
            ReadPermissions = 0x00020000,
            WritePermissions = 0x00040000,
            TakeOwnership = 0x00080000,
            Synchronize = 0x00100000,

            Read = StandardRights.Read | EnumDesktops | ReadAttributes | Enumerate | ReadScreen,
            Write = StandardRights.Write | AccessClipboard | CreateDesktop | WriteAttributes,
            Execute = StandardRights.Read | AccessGlobalAtoms | ExitWindows,

            AllAccess = EnumDesktops | ReadAttributes | AccessClipboard | CreateDesktop |
                WriteAttributes | AccessGlobalAtoms | ExitWindows | Enumerate | ReadScreen |
                StandardRights.Required
        }
    }
}
