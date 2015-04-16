using System;
using System.Runtime.InteropServices;

namespace IronFrame.Win32
{
    using DWORD = UInt32;
    using PFN_AUTHZ_DYNAMIC_ACCESS_CHECK = IntPtr;
    using PFN_AUTHZ_COMPUTE_DYNAMIC_GROUPS = IntPtr;
    using PFN_AUTHZ_FREE_DYNAMIC_GROUPS = IntPtr;
    using PLARGE_INTEGER = IntPtr;
    using LPVOID = IntPtr;
    using AUTHZ_AUDIT_EVENT_HANDLE = IntPtr;
    using PSECURITY_DESCRIPTOR = IntPtr;
    using AUTHZ_ACCESS_CHECK_RESULTS_HANDLE = IntPtr;
    using POBJECT_TYPE_LIST = IntPtr;
    using PACCESS_MASK = IntPtr;
    using PDWORD = IntPtr;

    internal partial class NativeMethods
    {
        [DllImport(NativeDll.AUTHZ_DLL, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AuthzInitializeResourceManager(
            AuthzResourceManagerFlags flags,
            PFN_AUTHZ_DYNAMIC_ACCESS_CHECK pfnAccessCheck,
            PFN_AUTHZ_COMPUTE_DYNAMIC_GROUPS pfnComputeDynamicGroups,
            PFN_AUTHZ_FREE_DYNAMIC_GROUPS pfnFreeDynamicGroups,
            string szResourceManagerName,
            out SafeAuthzRMHandle phAuthzResourceManager);

        [DllImport(NativeDll.AUTHZ_DLL, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AuthzInitializeContextFromSid(
            AuthzInitFlags flags,
            byte[] rawUserSid,
            SafeAuthzRMHandle authzRM,
            PLARGE_INTEGER expirationTime,
            LUID Identifier,
            LPVOID DynamicGroupArgs,
            out SafeAuthzContextHandle authzClientContext);

        [DllImport(NativeDll.AUTHZ_DLL, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AuthzAccessCheck(
            AuthzACFlags flags,
            SafeAuthzContextHandle hAuthzClientContext,
            ref AUTHZ_ACCESS_REQUEST pRequest,
            AUTHZ_AUDIT_EVENT_HANDLE AuditEvent,
            byte[] rawSecurityDescriptor,
            PSECURITY_DESCRIPTOR[] OptionalSecurityDescriptorArray,
            DWORD OptionalSecurityDescriptorCount,
            ref AUTHZ_ACCESS_REPLY pReply,
            AUTHZ_ACCESS_CHECK_RESULTS_HANDLE cachedResults);

        [Flags]
        public enum AuthzResourceManagerFlags : uint
        {
            NO_AUDIT = 0x1
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct LUID
        {
            public uint LowPart;
            public uint HighPart;

            public static LUID NullLuid
            {
                get
                {
                    LUID Empty;
                    Empty.LowPart = 0;
                    Empty.HighPart = 0;

                    return Empty;
                }
            }
        }

        [Flags]
        public enum AuthzInitFlags : uint
        {
            Default = 0x0,
            SkipTokenGroups = 0x2,
            RequireS4ULogon = 0x4,
            ComputePrivileges = 0x8
        }

        public enum AuthzACFlags : uint // DWORD
        {
            None = 0,
            NoDeepCopySD
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AUTHZ_ACCESS_REQUEST
        {
            public ACCESS_MASK DesiredAccess;
            public byte[] PrincipalSelfSid;
            public POBJECT_TYPE_LIST ObjectTypeList;
            public int ObjectTypeListLength;
            public LPVOID OptionalArguments;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AUTHZ_ACCESS_REPLY
        {
            public int ResultListLength;
            public PACCESS_MASK GrantedAccessMask;
            public PDWORD SaclEvaluationResults;
            public PDWORD Error;
        }
    }
}