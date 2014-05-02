namespace IronFoundry.Warden.PInvoke
{
    using System;

    public partial class NativeMethods
    {
        public class Constants
        {
            public const Int32  GENERIC_ALL_ACCESS = 0x10000000;
            public const UInt32 INFINITE = 0xFFFFFFFF;
            public const UInt32 WAIT_FAILED = 0xFFFFFFFF;

            public const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000u;
            public const uint STANDARD_RIGHTS_READ = 0x00020000u;
            public const uint TOKEN_ASSIGN_PRIMARY = 0x0001u;
            public const uint TOKEN_DUPLICATE = 0x0002u;
            public const uint TOKEN_IMPERSONATE = 0x0004u;
            public const uint TOKEN_QUERY = 0x0008u;
            public const uint TOKEN_QUERY_SOURCE = 0x0010u;
            public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020u;
            public const uint TOKEN_ADJUST_GROUPS = 0x0040u;
            public const uint TOKEN_ADJUST_DEFAULT = 0x0080u;
            public const uint TOKEN_ADJUST_SESSIONID = 0x0100u;
            public const uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
            public const uint TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
                TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
                TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
                TOKEN_ADJUST_SESSIONID);

            public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            public const uint ERROR_ACCESS_DENIED = 5u;
            public const uint ERROR_INSUFFICIENT_BUFFER = 122u;
            public const uint ERROR_MORE_DATA = 0x000000EA;
        }
    }
}
