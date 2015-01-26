using System;

namespace IronFoundry.Container.Win32
{
    public partial class NativeMethods
    {
        // From WinUser.h
        [Flags]
        public enum DesktopRights
        {
            ReadObjects = 0x0001,
            CreateWindow = 0x0002,
            CreateMenu = 0x0004,
            HookControl = 0x0008,
            JournalRecord = 0x0010,
            JournalPlayback = 0x0020,
            Enumerate = 0x0040,
            WriteObjects = 0x0080,
            SwitchDesktop = 0x0100,

            // Standard Rights
            Delete = 0x00010000,
            ReadPermissions = 0x00020000,
            WritePermissions = 0x00040000,
            TakeOwnership = 0x00080000,
            Synchronize = 0x00100000,


            Read = ReadObjects | Enumerate | StandardRights.Read,
            Write = WriteObjects | CreateWindow | CreateMenu | HookControl | JournalRecord |
                JournalPlayback | StandardRights.Write,
            Execute = SwitchDesktop | StandardRights.Execute,

            AllAccess = ReadObjects | CreateWindow | CreateMenu | HookControl |
                JournalRecord | JournalPlayback | Enumerate | WriteObjects | SwitchDesktop |
                StandardRights.Required
        }
    }

}
