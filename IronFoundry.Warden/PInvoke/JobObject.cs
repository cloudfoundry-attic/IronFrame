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

        [Flags]
        public enum JobObjectLimit : uint
        {
            // Basic Limits
            Workingset = 0x00000001,
            ProcessTime = 0x00000002,
            JobTime = 0x00000004,
            ActiveProcess = 0x00000008,
            Affinity = 0x00000010,
            PriorityClass = 0x00000020,
            PreserveJobTime = 0x00000040,
            SchedulingClass = 0x00000080,

            // Extended Limits
            ProcessMemory = 0x00000100,
            JobMemory = 0x00000200,
            DieOnUnhandledException = 0x00000400,
            BreakawayOk = 0x00000800,
            SilentBreakawayOk = 0x00001000,
            KillOnJobClose = 0x00002000,
            SubsetAffinity = 0x00004000,

            // Notification Limits
            JobReadBytes = 0x00010000,
            JobWriteBytes = 0x00020000,
            RateControl = 0x00040000,
        }

        public enum JobObjectInfoClass : uint
        {
            JobObjectBasicAccountingInformation = 1,                        
            BasicLimitInformation = 2,
            JobObjectBasicProcessIdList = 3,
            BasicUIRestrictions = 4,
            EndOfJobTimeInformation = 6,
            AssociateCompletionPortInformation = 7,
            ExtendedLimitInformation = 9,
            SecurityLimitInformation = 5,
            GroupInformation = 11
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
        public struct JobObjectBasicLimitInformation
        {
            public UInt64 PerProcessUserTimeLimit;
            public UInt64 PerJobUserTimeLimit;
            public JobObjectLimit LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public UInt32 ActiveProcessLimit;
            public Int64 Affinity;
            public UInt32 PriorityClass;
            public UInt32 SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JobObjectIoCounters
        {
            public UInt64 ReadOperationCount;
            public UInt64 WriteOperationCount;
            public UInt64 OtherOperationCount;
            public UInt64 ReadTransferCount;
            public UInt64 WriteTransferCount;
            public UInt64 OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JobObjectExtendedLimitInformation
        {
            public JobObjectBasicLimitInformation BasicLimitInformation;
            public JobObjectIoCounters IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
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
        public static extern bool SetInformationJobObject(
            SafeHandle hJob,
            [MarshalAs(UnmanagedType.U4)] JobObjectInfoClass infoType,
            IntPtr lpJobObjectInfo,
            [MarshalAs(UnmanagedType.U4)] uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateJobObject(SafeHandle jobHandle, uint exitCode);
    }
}
