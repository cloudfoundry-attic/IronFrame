namespace IronFoundry.Warden.PInvoke
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;

    public partial class NativeMethods
    {
        public const uint ERROR_MORE_DATA = 0x000000EA;

        [Flags]
        public enum JobObjectAccessRights : uint
        {
            // Standard Access Rights
            Delete = 0x00010000, 
            ReadControl= 0x00020000,
            Synchronize = 0x00100000, 
            WriteDac= 0x00040000, 
            WriteOwner= 0x00080000,

            // Job Specific Rights
            AllAccess = 0x1F001F,
            AssignProcess = 0x0001,
            Query = 0x0004,
            SetAttributes = 0x0002,
            // Note: SetSecurityAttributes is obsolete
            // SetSecurityAttributes = 0x0010,
            Termiante = 0x0008,
        }

        public enum JobObjectInfoClass : uint
        {
            JobObjectBasicAccountingInformation = 1,
            JobObjectBasicProcessIdList = 3,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JobObjectBasicAccountingInformation
        {
            public long TotalUserTime;
            public long TotalKernelTime;
            public long ThisPeriodTotalUserTime;
            public long ThisPeriodTotalKernelTime;
            public uint TotalPageFaultCount;
            public uint TotalProcesses;
            public uint ActiveProcesses;
            public uint TotalTerminatedProcesses;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JobObjectBasicProcessIdList
        {
            // NOTE: Do not rename these fields or otherwise modify this struct without updating custom marshalling code!
            public uint NumberOfAssignedProcesses;
            public uint NumberOfProcessIdsInList;
            public IntPtr FirstProcessId;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AssignProcessToJobObject(SafeHandle jobHandle, IntPtr processHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateJobObject(IntPtr securityAttributes, string name);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenJobObject(JobObjectAccessRights desiredAccess, bool inheritHandles, string name);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsProcessInJob(IntPtr processHandle, SafeHandle jobHandle, [MarshalAs(UnmanagedType.Bool)]out bool result);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryInformationJobObject(
            SafeHandle jobHandle,
            [MarshalAs(UnmanagedType.U4)] JobObjectInfoClass infoClass,
            IntPtr info,
            [MarshalAs(UnmanagedType.U4)] int infoLength,
            IntPtr returnedInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateJobObject(SafeHandle jobHandle, uint exitCode);
    }
}
