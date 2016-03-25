using System;

namespace IronFrame.Win32
{
    internal partial class NativeMethods
    {
        [Flags]
        public enum LogonType
        {
            LOGON32_LOGON_INTERACTIVE       = 2,
            LOGON32_LOGON_NETWORK           = 3,
            LOGON32_LOGON_BATCH             = 4,
            LOGON32_LOGON_SERVICE           = 5,
            LOGON32_LOGON_UNLOCK            = 7,
            LOGON32_LOGON_NETWORK_CLEARTEXT = 8,
            LOGON32_LOGON_NEW_CREDENTIALS   = 9
        }

        [Flags]
        public enum LogonProvider
        {
            LOGON32_PROVIDER_DEFAULT = 0,
            LOGON32_PROVIDER_WINNT35,
            LOGON32_PROVIDER_WINNT40,
            LOGON32_PROVIDER_WINNT50
        }

        public enum SecurityImpersonationLevel
        {
            SecurityAnonymous      = 0,
            SecurityIdentification = 1,
            SecurityImpersonation  = 2,
            SecurityDelegation     = 3
        }

        public enum TokenType
        {
            TokenPrimary       = 1,
            TokenImpersonation = 2
        }

        [Flags]
        public enum CreateProcessFlags
        {
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_NO_WINDOW = 0x08000000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            CREATE_SUSPENDED = 0x00000004,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            DEBUG_PROCESS = 0x00000001,
            DETACHED_PROCESS = 0x00000008,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            INHERIT_PARENT_AFFINITY = 0x00010000
        }

    }
}
