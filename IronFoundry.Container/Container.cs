using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IronFoundry.Container.Utilities;
using IronFoundry.Warden.Containers;

namespace IronFoundry.Container
{
    public class ProcessSpec
    {
        public string ExecutablePath { get; set; }
        public string[] Arguments { get; set; }
        public Dictionary<string, string> Environment { get; set; }
        public string WorkingDirectory { get; set; }
        public bool Privileged { get; set; }
        public bool DisablePathMapping { get; set; }
    }

    public interface IProcessIO
    {
        TextWriter StandardOutput { get; }
        TextWriter StandardError { get; }
        TextReader StandardInput { get; }
    }

    public interface IContainer : IDisposable
    {
        string Id { get; }
        string Handle { get; }
        //ContainerState State { get; }
        IContainerDirectory Directory { get; }

        //void BindMounts(IEnumerable<BindMount> mounts);
        //void CreateTarFile(string sourcePath, string tarFilePath, bool compress);
        //void CopyFileIn(string sourceFilePath, string destinationFilePath);
        //void CopyFileOut(string sourceFilePath, string destinationFilePath);
        //void ExtractTarFile(string tarFilePath, string destinationPath, bool decompress);

        ContainerInfo GetInfo();

        void Stop(bool kill);

        int ReservePort(int requestedPort);
        ContainerProcess Run(ProcessSpec spec, IProcessIO io);

        void LimitMemory(ulong limitInBytes);

        //void Initialize(IContainerDirectory containerDirectory, ContainerHandle containerHandle, IContainerUser userInfo);
        //string ContainerDirectoryPath { get; }
        //string ContainerUserName { get; }
        //void Copy(string source, string destination);
    }

    public class Container : IContainer
    {
        const string DefaultWorkingDirectory = "/";

        readonly string id;
        readonly string handle;
        readonly IContainerUser user;
        readonly IContainerDirectory directory;
        readonly ILocalTcpPortManager tcpPortManager;
        readonly JobObject jobObject;
        readonly IProcessRunner processRunner;
        readonly IProcessRunner constrainedProcessRunner;
        readonly ProcessHelper processHelper;
        readonly Dictionary<string, string> defaultEnvironment;
        readonly List<int> reservedPorts = new List<int>();

        ContainerState currentState;

        public Container(
            string id,
            string handle,
            IContainerUser user,
            IContainerDirectory directory, 
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
            this.tcpPortManager = tcpPortManager;
            this.jobObject = jobObject;
            this.processRunner = processRunner;
            this.constrainedProcessRunner = constrainedProcessRunner;
            this.processHelper = processHelper;

            this.defaultEnvironment = defaultEnvironment ?? new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);

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

        public void Initialize()
        {
            // Start the 'host' process
            // Initialize the host (or wait for the host to initialize if it's implicit)
        }

        public int ReservePort(int requestedPort)
        {
            ThrowIfNotActive();

            var reservedPort = tcpPortManager.ReserveLocalPort(requestedPort, user.UserName);
            reservedPorts.Add(reservedPort);
            return reservedPort;
        }

        public ContainerProcess Run(ProcessSpec spec, IProcessIO io)
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

            var runSpec = new ProcessRunSpec
            {
                ExecutablePath = executablePath,
                Arguments = spec.Arguments,
                Environment = processEnvironment,
                WorkingDirectory = directory.MapUserPath(spec.WorkingDirectory ?? DefaultWorkingDirectory),
                OutputCallback = data => io.StandardOutput.Write(data),
                ErrorCallback = data => io.StandardError.Write(data),
            };

            var process = runner.Run(runSpec);

            return new ContainerProcess(process);
        }

        public void LimitMemory(ulong limitInBytes)
        {
            ThrowIfNotActive();

            this.jobObject.SetJobMemoryLimit(limitInBytes);
        }

        public void Destroy()
        {
            Stop(true);

            foreach (var port in reservedPorts)
            {
                tcpPortManager.ReleaseLocalPort(port, user.UserName);
            }

            // BR - Unmap the mounted directories (Removes user ACLs)
            // BR - Delete the container directory

            if (user != null)
                user.Delete();

            if (constrainedProcessRunner != null)
                constrainedProcessRunner.Dispose();

            if (processRunner != null)
                processRunner.Dispose();

            this.currentState = ContainerState.Destroyed;
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
                ContainerPath = directory.UserPath,
                State = this.currentState,
                CpuStat = GetCpuStat(),
                MemoryStat = GetMemoryStat(),
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
                constrainedProcessRunner.StopAll(kill);

            if (processRunner != null)
                processRunner.StopAll(kill);

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
    }
}