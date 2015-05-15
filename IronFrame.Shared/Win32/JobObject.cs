using System;
using System.Runtime.InteropServices;

namespace IronFrame.Win32
{
    internal partial class NativeMethods
    {
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

        [Flags]
        public enum UIRestrictions : uint
        {
            ReadClipboard = 0x00000002,
            WriteClipboard = 0x00000004,
        }

        public enum JobObjectNotification : uint
        {
            EndOfJobTime = 1,
            EndOfProcessTime = 2,
            ActiveProcessLimit = 3,
            ActiveProcessInfo = 4,
            NewProcess = 6,
            ExitProcess = 7,
            AbnormalExitProcess = 8,
            ProcessMemoryLimit = 9,
            JobMemoryLimit = 10,
            NotificationLimit = 11,
            JobCycleTimeLimit = 12,
        }

        public enum JobObjectInfoClass : uint
        {
            JobObjectBasicAccountingInformation = 1,
            BasicLimitInformation = 2,
            JobObjectBasicProcessIdList = 3,
            BasicUIRestrictions = 4,
            SecurityLimitInformation = 5,
            EndOfJobTimeInformation = 6,
            AssociateCompletionPortInformation = 7,
            ExtendedLimitInformation = 9,
            GroupInformation = 11,
            NotificationLimitInformation = 12,
            LimitViolationInformation = 13,
            CpuRateControlInformation = 15,
        }

        public enum JobObjectRateControlTolerance : uint
        {
            Unspecified = 0,
            Low = 1,
            Medium = 2,
            High = 3,
        }

        public enum JobObjectRateControlToleranceInterval : uint
        {
            Unspecified = 0,
            Short = 1,
            Medium = 2,
            Long = 3,
        }

        [Flags]
        public enum JobObjectCpuRateControl : uint
        {
            Enable = 1U,
            WeightBased = 2U,
            HardCap = 4U,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JobObjectAssociateCompletionPort
        {
            public IntPtr CompletionKey;
            public IntPtr CompletionPortHandle;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct JobObjectCpuRateControlInformation
        {
            [MarshalAs(UnmanagedType.U4)] [FieldOffset(0)] public UInt32 ControlFlags;
            [MarshalAs(UnmanagedType.U4)] [FieldOffset(4)] public UInt32 CpuRate;
            [MarshalAs(UnmanagedType.U4)] [FieldOffset(4)] public UInt32 Weight;
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
            public UIntPtr Affinity;
            public UInt32 PriorityClass;
            public UInt32 SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JobObjectUIRestrictions
        {
            public UIRestrictions UIRestrictionsClass;
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

        [StructLayout(LayoutKind.Sequential)]
        public struct JobObjectNotificationLimitInformation
        {
            public ulong IoReadBytesLimit;
            public ulong IoWriteBytesLimit;
            public long PerJobUserTimeLimit;
            public ulong JobMemoryLimit;
            public JobObjectRateControlTolerance RateControlTolerance;
            public JobObjectRateControlToleranceInterval RateControlToleranceInterval;
            public JobObjectLimit LimitFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JobObjectLimitViolationInformation
        {
            public JobObjectLimit LimitFlags;
            public JobObjectLimit ViolationLimitFlags;
            public ulong IoReadBytes;
            public ulong IoReadBytesLimit;
            public ulong IoWriteBytes;
            public ulong IoWriteBytesLimit;
            public long PerJobUserTime;
            public long PerJobUserTimeLimit;
            public ulong JobMemory;
            public ulong JobMemoryLimit;
            public JobObjectRateControlTolerance RateControlTolerance;
            public JobObjectRateControlTolerance RateControlToleranceLimit;
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
            [MarshalAs(UnmanagedType.U4)] int cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateJobObject(SafeHandle jobHandle, uint exitCode);
    }
}
