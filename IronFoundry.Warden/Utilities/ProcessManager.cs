namespace IronFoundry.Warden.Utilities
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Containers;
    using IronFoundry.Warden.PInvoke;
    using IronFoundry.Warden.Shared.Messaging;
    using NLog;

    public class ProcessManager : IDisposable
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly JobObject jobObject;
        private readonly ProcessHelper processHelper;
        private readonly ProcessLauncher processLauncher;

        public ProcessManager(string handle) : this(new JobObject(handle), new ProcessHelper(), new ProcessLauncher())
        {
        }

        public ProcessManager(JobObject jobObject, ProcessHelper processHelper, ProcessLauncher processLauncher)
        {
            this.jobObject = jobObject;
            this.processHelper = processHelper;
            this.processLauncher = processLauncher;
        }

        public bool HasProcesses
        {
            get { return jobObject.GetProcessIds().Count() > 0; }
        }

        public bool ContainsProcess(int processId)
        {
            return jobObject.GetProcessIds().Contains(processId);
        }

        public virtual IProcess CreateProcess(CreateProcessStartInfo startInfo)
        {
            var process = processLauncher.LaunchProcess(startInfo, jobObject);
            return process;
        }

        public virtual void Dispose()
        {
            jobObject.Dispose();
            processLauncher.Dispose();
        }

        public void RestoreProcesses()
        {
           // Should restore just by using same named JobObject, if there are any.
        }

        public virtual void StopProcesses()
        {
            jobObject.TerminateProcesses();
        }

        public ProcessStats GetProcessStats()
        {
            var cpuStatistics = jobObject.GetCpuStatistics();
            var processIds = jobObject.GetProcessIds();

            var processes = processHelper.GetProcesses(processIds).ToList();

            long privateMemory = 0;
            long workingSet = 0;

            foreach (var process in processes)
            {
                privateMemory += process.PrivateMemoryBytes;
                workingSet += process.WorkingSet;
            }

            return new ProcessStats
            {
                TotalProcessorTime = cpuStatistics.TotalKernelTime + cpuStatistics.TotalUserTime,
                TotalUserProcessorTime = cpuStatistics.TotalUserTime,
                PrivateMemory = privateMemory,
                WorkingSet = workingSet,
            };
        }
    }

    public struct ProcessStats
    {
        public TimeSpan TotalProcessorTime { get; set; }
        public TimeSpan TotalUserProcessorTime { get; set; }
        public long PrivateMemory { get; set; }
        public long WorkingSet { get; set; }
    }
}
