using System;
using System.Collections.Generic;
using System.Linq;
using IronFrame.Utilities;

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
        readonly ProcessHelper processHelper;
        readonly IContainerPropertyService propertyService;
        readonly Dictionary<string, string> defaultEnvironment;
        readonly List<int> reservedPorts = new List<int>();

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
            IProcessRunner processRunner,
            IProcessRunner constrainedProcessRunner,
            ProcessHelper processHelper,
            Dictionary<string, string> defaultEnvironment
            )
        {
            this.id = id;
            this.handle = handle;
            this.user = user;
            this.directory = directory;
            this.propertyService = propertyService;
            this.tcpPortManager = tcpPortManager;
            this.jobObject = jobObject;
            this.processRunner = processRunner;
            this.constrainedProcessRunner = constrainedProcessRunner;
            this.processHelper = processHelper;

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
            ThrowIfNotActive();

            var reservedPort = tcpPortManager.ReserveLocalPort(requestedPort, user.UserName);
            reservedPorts.Add(reservedPort);
            return reservedPort;
        }

        public IContainerProcess Run(ProcessSpec spec, IProcessIO io)
        {
            ThrowIfNotActive();

            var runner = spec.Privileged ?
                processRunner :
                constrainedProcessRunner;

            var executablePath = !spec.DisablePathMapping ?
                directory.MapUserPath(spec.ExecutablePath) :
                spec.ExecutablePath;

            var specEnvironment = spec.Environment ?? new Dictionary<string, string>();
            var processEnvironment = this.defaultEnvironment.Merge(specEnvironment);

            Action<string> stdOut = io == null || io.StandardOutput == null
                ? (Action<string>)null
                : data => io.StandardOutput.Write(data);

            Action<string> stdErr = io == null || io.StandardError == null
                ? (Action<string>)null
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

        public void LimitMemory(ulong limitInBytes)
        {
            ThrowIfNotActive();

            this.jobObject.SetJobMemoryLimit(limitInBytes);
        }

        public ulong CurrentMemoryLimit()
        {
            return jobObject.GetJobMemoryLimit();
        }

        public void Destroy()
        {
            Stop(true);

            foreach (var port in reservedPorts)
            {
                tcpPortManager.ReleaseLocalPort(port, user.UserName);
            }
            tcpPortManager.RemoveFirewallRules(user.UserName);

            // BR - Unmap the mounted directories (Removes user ACLs)
            // BR - Delete the container directory

            if (user != null)
                user.Delete();

            if (directory != null)
                directory.Destroy();

            this.currentState = ContainerState.Destroyed;
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

        public ContainerInfo GetInfo()
        {
            ThrowIfDestroyed();

            var ipAddress = IPUtilities.GetLocalIPAddress();
            var ipAddressString = ipAddress != null ? ipAddress.ToString() : "";

            return new ContainerInfo
            {
                HostIPAddress = ipAddressString,
                ContainerIPAddress = ipAddressString,
                ContainerPath = directory.RootPath,
                State = this.currentState,
                CpuStat = GetCpuStat(),
                MemoryStat = GetMemoryStat(),
                Properties = propertyService.GetProperties(this),
                ReservedPorts = new List<int>(reservedPorts),
            };
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
            var processIds = jobObject.GetProcessIds();

            var processes = processHelper.GetProcesses(processIds).ToList();

            ulong privateMemory = 0;

            foreach (var process in processes)
            {
                privateMemory += (ulong)process.PrivateMemoryBytes;
            }

            return new ContainerMemoryStat
            {
                PrivateBytes = privateMemory,
            };
        }

        public void Dispose()
        {
            // Should perform basic cleanup only.
        }

        public void Stop(bool kill)
        {
            ThrowIfDestroyed();

            if (constrainedProcessRunner != null)
            {
                constrainedProcessRunner.StopAll(kill);
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

        public void SetProperty(string name, string value)
        {
            propertyService.SetProperty(this, name, value);
        }

        public string GetProperty(string name)
        {
            return propertyService.GetProperty(this, name);
        }

        public Dictionary<string, string> GetProperties()
        {
            return propertyService.GetProperties(this);
        }

        public void RemoveProperty(string name)
        {
            propertyService.RemoveProperty(this, name);
        }

        public void LimitCpu(int i)
        {
            ThrowIfNotActive();
            jobObject.SetJobCpuLimit(i);
        }

        public int CurrentCpuLimit()
        {
            return jobObject.GetJobCpuLimit();
        }

        public void BlockAllOutBoundConnections()
        {
            tcpPortManager.BlockAllOutboundConnections(user.UserName);
        }
    }
}
