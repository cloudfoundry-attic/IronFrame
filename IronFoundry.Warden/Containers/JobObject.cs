namespace IronFoundry.Warden.Containers
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using IronFoundry.Warden.PInvoke;
    using Microsoft.Win32.SafeHandles;

    public class CpuStatistics
    {
        public TimeSpan TotalKernelTime { get; set; }
        public TimeSpan TotalUserTime { get; set; }
    }

    public class JobObject : IDisposable
    {
        SafeJobObjectHandle handle;
        volatile SafeFileHandle completionPortHandle;

        EventHandler memoryLimitedEvent;

        public JobObject()
            : this(null)
        {
        }

        public JobObject(string name)
            : this(name, false)
        {
        }

        public JobObject(string name, bool openExisting, bool terminateOnLastHandleClose = true)
        {
            if (openExisting)
            {
                handle = new SafeJobObjectHandle(NativeMethods.OpenJobObject(NativeMethods.JobObjectAccessRights.AllAccess, false, name));
            }
            else
            {
                handle = new SafeJobObjectHandle(NativeMethods.CreateJobObject(IntPtr.Zero, name));
                SetJobLimits(NativeMethods.JobObjectLimit.KillOnJobClose);
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

        public event EventHandler MemoryLimited
        {
            add
            {
                EnsureNotifications();
                memoryLimitedEvent += value; 
            }
            remove { memoryLimitedEvent -= value; }
        }

        public virtual void Dispose()
        {
            if (completionPortHandle != null)
            {
                completionPortHandle.Dispose();
                completionPortHandle = null;
            }

            if (handle != null)
            {
                handle.Dispose();
                handle = null;
            }
        }

        public void AssignProcessToJob(IntPtr processHandle)
        {
            NativeMethods.AssignProcessToJobObject(handle, processHandle);
        }

        public void AssignProcessToJob(Process process)
        {
            AssignProcessToJob(process.Handle);
        }

        void EnsureNotifications()
        {
            if (completionPortHandle == null)
            {
                completionPortHandle = NativeMethods.CreateIoCompletionPort(NativeMethods.Constants.INVALID_HANDLE_VALUE, IntPtr.Zero, IntPtr.Zero, 0);
                if (completionPortHandle.IsInvalid)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to create IO completion port for JobObject.");

                SetCompletionPort(completionPortHandle);
                ThreadPool.QueueUserWorkItem(ProcessNotifications);
            }
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
                    if (error != NativeMethods.Constants.ERROR_MORE_DATA)
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

        private NativeMethods.JobObjectExtendedLimitInformation GetJobLimits()
        {
            int length = Marshal.SizeOf(typeof(NativeMethods.JobObjectExtendedLimitInformation));
            IntPtr extendedInfoPtr = IntPtr.Zero;
            try
            {
                extendedInfoPtr = Marshal.AllocHGlobal(length);

                if (!NativeMethods.QueryInformationJobObject(
                        handle,
                        NativeMethods.JobObjectInfoClass.ExtendedLimitInformation,
                        extendedInfoPtr,
                        length,
                        IntPtr.Zero))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var extendedLimit = (NativeMethods.JobObjectExtendedLimitInformation)Marshal.PtrToStructure(extendedInfoPtr, typeof(NativeMethods.JobObjectExtendedLimitInformation));

                return extendedLimit;
            }
            finally
            {
                if (extendedInfoPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(extendedInfoPtr);
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
                        if (error == NativeMethods.Constants.ERROR_MORE_DATA)
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

        void OnMemoryLimited()
        {
            EventHandler handler = memoryLimitedEvent;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        void ProcessNotifications(object state)
        {
            // This method is called unpredictably in the ThreadPool, therefore it's possible that it will be called after the JobObject has been Disposed.
            SafeFileHandle cachedCompletionPortHandle = completionPortHandle;
            if (cachedCompletionPortHandle == null)
                return;

            uint numberOfBytes = 0;
            IntPtr completionKey = IntPtr.Zero;
            IntPtr overlappedPtr = IntPtr.Zero;
            uint timeoutInMS = 200;

            try
            {
                //
                // Process as many JobObject notifications as we can, then queue this method to run again.
                //
                while (!cachedCompletionPortHandle.IsInvalid &&
                       NativeMethods.GetQueuedCompletionStatus(cachedCompletionPortHandle, ref numberOfBytes, ref completionKey, ref overlappedPtr, timeoutInMS))
                {
                    // TODO: Consider using another ThreadPool thread (via QueueUserWorkItem) to raise the notification events, so that misbehaving event handlers
                    // can't delay the processing of notifications from the JobObject.  Doing so may cause events to be processed out of order though.
                    switch (numberOfBytes)
                    {
                        case (uint)NativeMethods.JobObjectNotification.JobMemoryLimit:
                            OnMemoryLimited();
                            break;
                    }
                }

                ThreadPool.QueueUserWorkItem(ProcessNotifications);
            }
            catch (ObjectDisposedException)
            {
                // The SafeHandle has been closed, so stop processing notifications.
            }
        }

        private void SetCompletionPort(SafeFileHandle completionPortHandle)
        {
            int length = Marshal.SizeOf(typeof(NativeMethods.JobObjectAssociateCompletionPort));
            IntPtr completionPortPtr = IntPtr.Zero;
            try
            {
                var completionPort = new NativeMethods.JobObjectAssociateCompletionPort
                {
                    CompletionKey = IntPtr.Zero,
                    CompletionPortHandle = completionPortHandle.DangerousGetHandle(),
                };

                completionPortPtr = Marshal.AllocHGlobal(length);

                Marshal.StructureToPtr(completionPort, completionPortPtr, false);

                if (!NativeMethods.SetInformationJobObject(handle, NativeMethods.JobObjectInfoClass.AssociateCompletionPortInformation, completionPortPtr, (uint)length))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                if (completionPortPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(completionPortPtr);
            }
        }

        private void SetJobLimits(NativeMethods.JobObjectLimit limitFlag)
        {
            var extendedLimit = GetJobLimits();
            extendedLimit.BasicLimitInformation.LimitFlags |= limitFlag;
            SetJobLimits(extendedLimit);
        }

        private void SetJobLimits(NativeMethods.JobObjectExtendedLimitInformation extendedLimit)
        {
            int length = Marshal.SizeOf(typeof(NativeMethods.JobObjectExtendedLimitInformation));
            IntPtr extendedInfoPtr = IntPtr.Zero;
            try
            {
                extendedInfoPtr = Marshal.AllocHGlobal(length);

                Marshal.StructureToPtr(extendedLimit, extendedInfoPtr, false);

                if (!NativeMethods.SetInformationJobObject(handle, NativeMethods.JobObjectInfoClass.ExtendedLimitInformation, extendedInfoPtr, (uint)length))
                {
                    throw new Exception(string.Format("Unable to set information.  Error: {0}", Marshal.GetLastWin32Error()));
                }
            }
            finally
            {
                if (extendedInfoPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(extendedInfoPtr);
            }
        }

        public virtual void SetMemoryLimit(ulong jobMemoryLimitInBytes)
        {
            var extendedLimit = new NativeMethods.JobObjectExtendedLimitInformation();

            extendedLimit.BasicLimitInformation.LimitFlags = NativeMethods.JobObjectLimit.JobMemory;
            extendedLimit.JobMemoryLimit = new UIntPtr(jobMemoryLimitInBytes);

            SetJobLimits(extendedLimit);
        }

        public virtual void TerminateProcesses()
        {
            if (handle == null) { throw new ObjectDisposedException("JobObject"); }
            NativeMethods.TerminateJobObject(handle, 0);
        }

        public virtual void TerminateProcessesAndWait(int milliseconds = System.Threading.Timeout.Infinite)
        {
            TerminateProcesses();
            using (var waitHandle = new JobObjectWaitHandle(handle))
            {
                waitHandle.WaitOne(milliseconds);
            }
        }
    }
}
