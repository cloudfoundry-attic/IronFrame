namespace IronFoundry.Warden.Utilities
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Containers;
    using IronFoundry.Warden.Shared.Messaging;
    using NLog;

    public class ProcessManager : IDisposable
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly JobObject jobObject;
        private readonly ProcessLauncher processLauncher;

        public ProcessManager(string handle) : this(new JobObject(handle), new ProcessLauncher())
        {
        }

        public ProcessManager(JobObject jobObject, ProcessLauncher processLauncher)
        {
            this.jobObject = jobObject;
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

        public virtual IProcess GetProcessById(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                return new RealProcessWrapper(process);
            }
            catch (ArgumentException)
            {
                return null;
            }
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

            var processes = processIds
                .Select(id => GetProcessById(id))
                .Where(p => p != null)
                .ToList();

            long privateMemory = 0;
            long pagedMemory = 0;
            long workingSet = 0;

            foreach (var process in processes)
            {
                privateMemory += process.PrivateMemoryBytes;
                pagedMemory += process.PagedMemoryBytes;
                workingSet += process.WorkingSet;
            }

            return new ProcessStats
            {
                TotalProcessorTime = cpuStatistics.TotalKernelTime + cpuStatistics.TotalUserTime,
                TotalUserProcessorTime = cpuStatistics.TotalUserTime,
                PrivateMemory = privateMemory,
                PagedMemory = pagedMemory,
                WorkingSet = workingSet,
            };
        }

        class RealProcessWrapper : IProcess
        {
            private readonly Process process;
            public event EventHandler Exited;

            public RealProcessWrapper(Process process)
            {
                this.process = process;
                Id = process.Id;
                process.Exited += (o, e) => this.OnExited();
            }

            public int Id { get; private set; }

            public int ExitCode
            {
                get { return process.ExitCode; }
            }

            public IntPtr Handle
            {
                get { return process.Handle; }
            }

            public bool HasExited
            {
                get { return process.HasExited; }
            }

            public TimeSpan TotalProcessorTime
            {
                get { return process.TotalProcessorTime; }
            }

            public TimeSpan TotalUserProcessorTime
            {
                get { return process.UserProcessorTime; }
            }

            public void Kill()
            {
                process.Kill();
            }

            protected virtual void OnExited()
            {
                var handlers = Exited;
                if (handlers != null)
                {
                    handlers.Invoke(this, EventArgs.Empty);
                }
            }

            public long PrivateMemoryBytes
            {
                get { return process.PrivateMemorySize64; }
            }

            public long PagedMemoryBytes
            {
                get { return process.PagedMemorySize64; }
            }

            public long WorkingSet
            {
                get { return process.WorkingSet64; }
            }

            public void Dispose()
            {
                process.Dispose();
            }

            public void WaitForExit()
            {
                process.WaitForExit();
            }
        }
    }

    public struct ProcessStats
    {
        public TimeSpan TotalProcessorTime { get; set; }
        public TimeSpan TotalUserProcessorTime { get; set; }
        public long PrivateMemory { get; set; }
        public long PagedMemory { get; set; }
        public long WorkingSet { get; set; }
    }
}
