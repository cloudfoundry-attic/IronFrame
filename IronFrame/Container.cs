using DiskQuotaTypeLibrary;
using IronFrame.Utilities;
using SimpleImpersonation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace IronFrame
{
    internal class Container : IContainer
    {
        const string DefaultWorkingDirectory = "/";

        readonly string id;
        readonly string handle;
        readonly IContainerUser user;
        readonly IContainerDirectory directory;
        readonly ILocalTcpPortManager tcpPortManager;
        readonly JobObject jobObject;
        readonly DiskQuotaControl diskQuotaControl;
        readonly ProcessHelper processHelper;
        readonly IContainerPropertyService propertyService;
        readonly Dictionary<string, string> defaultEnvironment;
        readonly List<int> reservedPorts = new List<int>();
        readonly ContainerHostDependencyHelper dependencyHelper;
        private readonly object _ioLock = new object();

        IProcessRunner processRunner;
        IProcessRunner constrainedProcessRunner;
        ContainerState currentState;

        public Container(
            string id,
            string handle,
            IContainerUser user,
            IContainerDirectory directory,
            IContainerPropertyService propertyService,
            ILocalTcpPortManager tcpPortManager,
            JobObject jobObject,
            DiskQuotaControl diskQuotaControl,
            IProcessRunner processRunner,
            IProcessRunner constrainedProcessRunner,
            ProcessHelper processHelper,
            Dictionary<string, string> defaultEnvironment,
            ContainerHostDependencyHelper dependencyHelper
            )
        {
            this.id = id;
            this.handle = handle;
            this.user = user;
            this.directory = directory;
            this.propertyService = propertyService;
            this.tcpPortManager = tcpPortManager;
            this.jobObject = jobObject;
            this.diskQuotaControl = diskQuotaControl;
            this.processRunner = processRunner;
            this.constrainedProcessRunner = constrainedProcessRunner;
            this.processHelper = processHelper;
            this.dependencyHelper = dependencyHelper;
            this.defaultEnvironment = defaultEnvironment ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            this.currentState = ContainerState.Active;
        }

        public string Id
        {
            get { return id; }
        }

        public string Handle
        {
            get { return handle; }
        }

        public IContainerDirectory Directory
        {
            get { return directory; }
        }

        public int ReservePort(int requestedPort)
        {
            lock (_ioLock)
            {
                ThrowIfNotActive();

                var reservedPort = tcpPortManager.ReserveLocalPort(requestedPort, user.UserName);
                reservedPorts.Add(reservedPort);
                return reservedPort;
            }
        }

        public IContainerProcess Run(ProcessSpec spec, IProcessIO io)
        {
            lock (_ioLock)
            {
                ThrowIfNotActive();

                var runner = spec.Privileged
                    ? processRunner
                    : constrainedProcessRunner;

                var executablePath = !spec.DisablePathMapping
                    ? directory.MapUserPath(spec.ExecutablePath)
                    : spec.ExecutablePath;

                var specEnvironment = spec.Environment ?? new Dictionary<string, string>();
                var processEnvironment = this.defaultEnvironment.Merge(specEnvironment);

                Action<string> stdOut = io == null || io.StandardOutput == null
                    ? (Action<string>) null
                    : data => io.StandardOutput.Write(data);

                Action<string> stdErr = io == null || io.StandardError == null
                    ? (Action<string>) null
                    : data => io.StandardError.Write(data);

                var runSpec = new ProcessRunSpec
                {
                    ExecutablePath = executablePath,
                    Arguments = spec.Arguments,
                    Environment = processEnvironment,
                    WorkingDirectory = directory.MapUserPath(spec.WorkingDirectory ?? DefaultWorkingDirectory),
                    OutputCallback = stdOut,
                    ErrorCallback = stdErr,
                };

                var process = runner.Run(runSpec);

                return new ContainerProcess(process);
            }
        }

        public void LimitMemory(ulong limitInBytes)
        {
            lock (_ioLock)
            {
                ThrowIfNotActive();
                ThrowIfGuardActive();

                this.jobObject.SetJobMemoryLimit(limitInBytes);
            }
        }

        public ulong CurrentMemoryLimit()
        {
            return jobObject.GetJobMemoryLimit();
        }

        public void Destroy()
        {
            lock (_ioLock)
            {
                Stop(true);
                StopGuardAndWait(new TimeSpan(0, 0, 0, 10));


                foreach (var port in reservedPorts)
                {
                    tcpPortManager.ReleaseLocalPort(port, user.UserName);
                }
                tcpPortManager.RemoveFirewallRules(user.UserName);

                // BR - Unmap the mounted directories (Removes user ACLs)
                try
                {
                    jobObject.TerminateProcessesAndWait();
                }
                catch (ObjectDisposedException)
                {
                }
                jobObject.Dispose();

                if (directory != null)
                    directory.Destroy();

                deleteUserDiskQuota();

                if (user != null)
                    user.Delete();

                this.currentState = ContainerState.Destroyed;
            }
        }

        private void deleteUserDiskQuota()
        {
            try
            {
                var dskuser = diskQuotaControl.FindUser(user.SID);
                diskQuotaControl.DeleteUser(dskuser);
            }
            catch (COMException)
            {
                // we can't determine if a disk quota exists for a given user
                // so we just have to try to delete it and catch the exception
            }
        }

        public IContainerProcess FindProcessById(int id)
        {
            var process = constrainedProcessRunner.FindProcessById(id);
            if (process == null)
            {
                return null;
            }
            return new ContainerProcess(process);
        }

        public void CreateOutboundFirewallRule(FirewallRuleSpec firewallRuleSpec)
        {
            lock (_ioLock)
            {
                tcpPortManager.CreateOutboundFirewallRule(user.UserName, firewallRuleSpec);
            }
        }

        public ulong CurrentDiskLimit()
        {
            lock (_ioLock)
            {
                return (ulong) diskQuotaControl.FindUser(user.SID).QuotaLimit;
            }
        }

        public ulong CurrentDiskUsage()
        {
            lock (_ioLock)
            {
                try
                {
                    return (ulong)diskQuotaControl.FindUser(user.SID).QuotaUsed;
                }
                catch (COMException) {
                    return 0;
                }
            }
        }

        public ContainerInfo GetInfo()
        {
            lock (_ioLock)
            {
                ThrowIfDestroyed();

                return new ContainerInfo
                {
                    State = this.currentState,
                    ReservedPorts = new List<int>(reservedPorts),
                    Properties = propertyService.GetProperties(this),
                };
            }
        }

        public ContainerMetrics GetMetrics()
        {
            lock (_ioLock)
            {
                ThrowIfDestroyed();

                return new ContainerMetrics
                {
                    CpuStat = GetCpuStat(),
                    MemoryStat = GetMemoryStat(),
                };
            }
        }

        private ContainerCpuStat GetCpuStat()
        {
            var cpuStatistics = jobObject.GetCpuStatistics();
            return new ContainerCpuStat
            {
                TotalProcessorTime = cpuStatistics.TotalKernelTime + cpuStatistics.TotalUserTime,
            };
        }

        private ContainerMemoryStat GetMemoryStat()
        {
            lock (_ioLock)
            {
                var processIds = jobObject.GetProcessIds();

                var processes = processHelper.GetProcesses(processIds).ToList();

                ulong privateMemory = 0;

                foreach (var process in processes)
                {
                    try
                    {
                        privateMemory += (ulong) process.PrivateMemoryBytes;
                    }
                    catch (InvalidOperationException)
                    {
                        // skip the error, the process has exited 
                    }
                }

                return new ContainerMemoryStat
                {
                    PrivateBytes = privateMemory,
                };
            }
        }

        public void Dispose()
        {
            // Should perform basic cleanup only.
        }

        public void Stop(bool kill)
        {
            lock (_ioLock)
            {
                ThrowIfDestroyed();

                if (constrainedProcessRunner != null)
                {
                    try
                    {
                        constrainedProcessRunner.StopAll(kill);
                    }
                    catch (TimeoutException)
                    {
                        jobObject.TerminateProcessesAndWait();
                    }
                    constrainedProcessRunner.Dispose();
                    constrainedProcessRunner = null;
                }

                if (processRunner != null)
                {
                    processRunner.StopAll(kill);
                    processRunner.Dispose();
                    processRunner = null;
                }

                this.currentState = ContainerState.Stopped;
            }
        }

        private void ThrowIfNotActive()
        {
            if (currentState != ContainerState.Active)
            {
                throw new InvalidOperationException("Container must be active for this operation.");
            }
        }

        private void ThrowIfDestroyed()
        {
            if (currentState == ContainerState.Destroyed)
            {
                throw new InvalidOperationException("The container has been destroyed.");
            }
        }
        private void ThrowIfGuardActive()
        {
            if (IsGuardRunning())
            {
                throw new InvalidOperationException("Memory Limits can only be changed before first process is run.");
            }
        }

        public void LimitDisk(ulong limitInBytes)
        {
            lock (_ioLock)
            {
                ThrowIfNotActive();
                var dskuser = diskQuotaControl.FindUser(user.SID);
                dskuser.QuotaLimit = (double) limitInBytes;
            }
        }

        public void SetProperty(string name, string value)
        {
            lock (_ioLock)
            {
                ThrowIfDestroyed();
                propertyService.SetProperty(this, name, value);
            }
        }

        public string GetProperty(string name)
        {
            lock (_ioLock)
            {
                ThrowIfDestroyed();
                return propertyService.GetProperty(this, name);
            }
        }

        public Dictionary<string, string> GetProperties()
        {
            lock (_ioLock)
            {
                ThrowIfDestroyed();
                return propertyService.GetProperties(this);
            }
        }

        public void RemoveProperty(string name)
        {
            lock (_ioLock)
            {
                ThrowIfDestroyed();
                propertyService.RemoveProperty(this, name);
            }
        }

        public void LimitCpu(int i)
        {
            lock (_ioLock)
            {
                ThrowIfNotActive();
                jobObject.SetJobCpuLimit(i);
            }
        }

        public int CurrentCpuLimit()
        {
            return jobObject.GetJobCpuLimit();
        }

        public void SetActiveProcessLimit(uint processLimit)
        {
            jobObject.SetActiveProcessLimit(processLimit);
        }

        public void SetPriorityClass(ProcessPriorityClass priority)
        {
            jobObject.SetPriorityClass(priority);
        }

        public void ImpersonateContainerUser(Action f)
        {
            using (Impersonation.LogonUser("", user.UserName, user.GetCredential().Password, LogonType.Interactive))
            {
                lock (_ioLock)
                {
                    f();
                }
            }
        }

        public void StartGuard()
        {
            lock (_ioLock)
            {
                if (IsGuardRunning())
                    return;

                processRunner.Run(new ProcessRunSpec
                {
                    ExecutablePath = dependencyHelper.GuardExePath,
                    Arguments = new string[]
                    {
                        user.UserName,
                        Id
                    },
                    WorkingDirectory = directory.MapUserPath("/")
                });
            }
        }

        public void StopGuard()
        {
            lock (_ioLock)
            {
                using (var dischargeEvent = GuardEventWaitHandle())
                {
                    if (dischargeEvent != null)
                    {
                        dischargeEvent.Set();
                    }
                }
            }
        }

        private EventWaitHandle GuardEventWaitHandle()
        {
            EventWaitHandle dischargeEvent = null;
            EventWaitHandle.TryOpenExisting(string.Concat("Global\\", "discharge-", user.UserName), out dischargeEvent);
            return dischargeEvent;
        }

        private bool IsGuardRunning()
        {
            using (var dischargeEvent = GuardEventWaitHandle())
            {
                return (dischargeEvent != null);
            }
        }

        private void StopGuardAndWait(TimeSpan timeout)
        {
            var st = new Stopwatch();
            st.Start();

            while (IsGuardRunning() && st.Elapsed < timeout)
            {
                StopGuard();
                Thread.Sleep(1);
            }
        }
    }
}
