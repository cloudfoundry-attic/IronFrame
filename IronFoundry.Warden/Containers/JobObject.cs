namespace IronFoundry.Warden.Containers
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using IronFoundry.Warden.PInvoke;

    public class CpuStatistics
    {
        public TimeSpan TotalKernelTime { get; set; }
        public TimeSpan TotalUserTime { get; set; }
    }

    public class JobObject : IDisposable
    {
        SafeJobObjectHandle handle;

        public JobObject() : this(null)
        {
        }

        public JobObject(string name) : this(name, false)
        {
        }

        public JobObject(string name, bool openExisting)
        {
            if (openExisting)
            {
                handle = new SafeJobObjectHandle(NativeMethods.OpenJobObject(NativeMethods.JobObjectAccessRights.AllAccess, false, name));
            }
            else
            {
                handle = new SafeJobObjectHandle(NativeMethods.CreateJobObject(IntPtr.Zero, name));
            }

            if (handle.IsInvalid)
            {
                throw new Exception("Unable to create job object.");
            }
        }

        public SafeJobObjectHandle Handle
        {
            get { return handle; }
        }

        public void Dispose()
        {
            if (handle == null) { return; }
            handle.Dispose();
            handle = null;
        }

        public void AssignProcessToJob(IntPtr processHandle)
        {
            NativeMethods.AssignProcessToJobObject(handle, processHandle);
        }

        public void AssignProcessToJob(Process process)
        {
            AssignProcessToJob(process.Handle);
        }

        public virtual CpuStatistics GetCpuStatistics()
        {
            if (handle == null) { throw new ObjectDisposedException("JobObject"); }

            var info = GetJobObjectBasicAccountingInformation(handle);

            return new CpuStatistics
            {
                TotalKernelTime = new TimeSpan(info.TotalKernelTime),
                TotalUserTime = new TimeSpan(info.TotalUserTime),
            };
        }

        public virtual int[] GetProcessIds()
        {
            return GetJobObjectProcessIds(handle);
        }

        static NativeMethods.JobObjectBasicAccountingInformation GetJobObjectBasicAccountingInformation(SafeJobObjectHandle handle)
        {
            int infoSize = Marshal.SizeOf(typeof(NativeMethods.JobObjectBasicAccountingInformation));
            IntPtr infoPtr = IntPtr.Zero;
            try
            {
                infoPtr = Marshal.AllocHGlobal(infoSize);

                if (!NativeMethods.QueryInformationJobObject(
                    handle,
                    NativeMethods.JobObjectInfoClass.JobObjectBasicAccountingInformation,
                    infoPtr,
                    infoSize,
                    IntPtr.Zero))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error != NativeMethods.ERROR_MORE_DATA)
                        throw new Win32Exception(error);
                }

                return (NativeMethods.JobObjectBasicAccountingInformation)Marshal.PtrToStructure(infoPtr, typeof(NativeMethods.JobObjectBasicAccountingInformation));
            }
            finally
            {
                if (infoPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(infoPtr);
            }
        }

        static int[] GetJobObjectProcessIds(SafeJobObjectHandle handle)
        {
            const int JobCountIncrement = 5;

            int numberOfAssignedProcessesOffset = Marshal.OffsetOf(typeof(NativeMethods.JobObjectBasicProcessIdList), "NumberOfAssignedProcesses").ToInt32();
            int numberOfProcessIdsInListOffset = Marshal.OffsetOf(typeof(NativeMethods.JobObjectBasicProcessIdList), "NumberOfProcessIdsInList").ToInt32();
            int firstProcessIdOffset = Marshal.OffsetOf(typeof(NativeMethods.JobObjectBasicProcessIdList), "FirstProcessId").ToInt32();

            int numberOfProcessesInJob = JobCountIncrement;
            do
            {
                int infoSize = firstProcessIdOffset + (IntPtr.Size * numberOfProcessesInJob);
                IntPtr infoPtr = IntPtr.Zero;
                try
                {
                    infoPtr = Marshal.AllocHGlobal(infoSize);
                    NativeMethods.FillMemory(infoPtr, (IntPtr)infoSize, 0);

                    Marshal.WriteInt32(infoPtr, numberOfAssignedProcessesOffset, numberOfProcessesInJob);
                    Marshal.WriteInt32(infoPtr, numberOfProcessIdsInListOffset, 0);
                    

                    if (!NativeMethods.QueryInformationJobObject(
                        handle,
                        NativeMethods.JobObjectInfoClass.JobObjectBasicProcessIdList,
                        infoPtr,
                        infoSize,
                        IntPtr.Zero))
                    {
                        var error = Marshal.GetLastWin32Error();
                        if (error == NativeMethods.ERROR_MORE_DATA)
                        {
                            numberOfProcessesInJob += JobCountIncrement;
                            continue;
                        }

                        throw new Win32Exception(error);
                    }

                    int count = Marshal.ReadInt32(infoPtr, numberOfProcessIdsInListOffset);
                    if (count == 0)
                        return new int[0];

                    IntPtr[] ids = new IntPtr[count];

                    Marshal.Copy(infoPtr + firstProcessIdOffset, ids, 0, count);

                    return ids.Select(id => id.ToInt32()).ToArray();
                }
                finally
                {
                    if (infoPtr != IntPtr.Zero)
                        Marshal.FreeHGlobal(infoPtr);
                }

            } while (true);
        }

        public void TerminateProcesses()
        {
            if (handle == null) { throw new ObjectDisposedException("JobObject"); }
            NativeMethods.TerminateJobObject(handle, 0);
        }
    }
}
