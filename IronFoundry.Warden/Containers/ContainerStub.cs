using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IronFoundry.Container;
using IronFoundry.Container.Win32;
using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Containers
{
    // BR: Move to IronFoundry.Container.Shared
    // BR: Might make sense to split the functionality between the library and the host
    public class ContainerStub : IContainer, IDisposable
    {
        const int ExitTimeout = 10000;

        private readonly JobObject jobObject;
        private readonly JobObjectLimits jobObjectLimits;
        private ContainerState currentState;
        private IContainerDirectory containerDirectory;
        private ContainerHandle containerHandle;
        private System.Net.NetworkCredential user;
        private readonly ICommandRunner commandRunner;
        private readonly ProcessHelper processHelper;
        private EventHandler outOfMemoryHandler;
        private readonly ProcessMonitor processMonitor;
        private readonly int owningProcessId;
        private readonly ILocalTcpPortManager portManager;
        private readonly FileSystemManager fileSystemManager;

        public ContainerStub(
            JobObject jobObject,
            JobObjectLimits jobObjectLimits,
            ICommandRunner commandRunner,
            ProcessHelper processHelper,
            ProcessMonitor processMonitor,
            ILocalTcpPortManager portManager,
            FileSystemManager fileSystemManager)
            : this(
                     jobObject,
                     jobObjectLimits,
                     commandRunner,
                     processHelper,
                     processMonitor,
                     Process.GetCurrentProcess().Id,
                     portManager,
                     fileSystemManager)
        {
        }

        public ContainerStub(
            JobObject jobObject,
            JobObjectLimits jobObjectLimits,
            ICommandRunner commandRunner,
            ProcessHelper processHelper,
            ProcessMonitor processMonitor,
            int owningProcessId,
            ILocalTcpPortManager portManager,
            FileSystemManager fileSystemManager)
        {
            this.jobObject = jobObject;
            this.jobObjectLimits = jobObjectLimits;
            this.currentState = ContainerState.Born;
            this.commandRunner = commandRunner;
            this.processHelper = processHelper;
            this.processMonitor = processMonitor;
            this.owningProcessId = owningProcessId;
            this.portManager = portManager;
            this.fileSystemManager = fileSystemManager;

            this.jobObjectLimits.MemoryLimitReached += MemoryLimitReached;
        }

        public string ContainerDirectoryPath
        {
            get { return containerDirectory.FullName; }
        }

        public string ContainerUserName
        {
            get { return user.UserName; }
        }

        public ContainerHandle Handle
        {
            get { return this.containerHandle; }
        }

        public ContainerState State
        {
            get { return this.currentState; }
        }

        public event EventHandler OutOfMemory
        {
            add { outOfMemoryHandler += value; }
            remove { outOfMemoryHandler -= value; }
        }

        public void BindMounts(IEnumerable<BindMount> mounts)
        {
            ThrowIfNotActive();

            containerDirectory.BindMounts(mounts);
        }

        public void CreateTarFile(string sourcePath, string tarFilePath, bool compress)
        {
            if (String.IsNullOrWhiteSpace(sourcePath))
                throw new InvalidOperationException("The source path is empty.");

            if (String.IsNullOrWhiteSpace(tarFilePath))
                throw new InvalidOperationException("The tar file path is empty.");

            var containerSourcePath = ConvertToContainerPath(sourcePath);

            // NOTE: tarFilePath should not be contained within the container.
            fileSystemManager.CreateTarFile(containerSourcePath, tarFilePath, compress);
        }

        string ConvertToContainerPath(string path)
        {
            // Expect the incoming path to be a unix style path.  The root is relative to the root of the container.
            if (!path.StartsWith("/"))
                throw new ArgumentException("The container path is invalid. The path must be rooted.", "path");

            // Trim the leading '/' from the path so that we can combine it with the container's root.
            string relativePath = path.Substring(1);

            // Combine and normalize the paths (GetFullPath() will convert slashes to Windows backslashes).
            return Path.GetFullPath(Path.Combine(containerDirectory.FullName, "root", relativePath));
        }

        public void Copy(string source, string destination)
        {
            ThrowIfNotActive();

            if (string.IsNullOrWhiteSpace(source))
                throw new InvalidOperationException("Source file or directory is empty");

            if (string.IsNullOrWhiteSpace(destination))
                throw new InvalidOperationException("Destination file or directory is empty");

            var convertedSource = this.ConvertToPathWithin(source);
            var convertedDestination = this.ConvertToPathWithin(destination);

            fileSystemManager.Copy(convertedSource, convertedDestination);
        }

        public void CopyFileIn(string sourceFilePath, string destinationFilePath)
        {
            ThrowIfNotActive();

            if (string.IsNullOrWhiteSpace(sourceFilePath))
                throw new InvalidOperationException("Source file is empty");

            if (string.IsNullOrWhiteSpace(destinationFilePath))
                throw new InvalidOperationException("Destination file is empty");

            var containerDestinationPath = ConvertToContainerPath(destinationFilePath);

            fileSystemManager.CopyFile(sourceFilePath, containerDestinationPath);
        }

        public void CopyFileOut(string sourceFilePath, string destinationFilePath)
        {
            ThrowIfNotActive();

            if (String.IsNullOrWhiteSpace(sourceFilePath))
                throw new InvalidOperationException("Source file is empty");

            if (String.IsNullOrWhiteSpace(destinationFilePath))
                throw new InvalidOperationException("Destination file is empty");

            var containerSourcePath = ConvertToContainerPath(sourceFilePath);

            fileSystemManager.CopyFile(containerSourcePath, destinationFilePath);
        }

        public Utilities.IProcess CreateProcess(CreateProcessStartInfo si, bool impersonate = false)
        {
            ThrowIfNotActive();

            Process p = new Process()
            {
                StartInfo = ToProcessStartInfo(si, impersonate),
            };

            p.EnableRaisingEvents = true;

            var wrapped = ProcessHelper.WrapProcess(p);
            processMonitor.TryAdd(wrapped);

            bool started = p.Start();
            Debug.Assert(started);

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            jobObject.AssignProcessToJob(p);

            return wrapped;
        }

        public void ExtractTarFile(string tarFilePath, string destinationPath, bool decompress)
        {
            if (String.IsNullOrWhiteSpace(tarFilePath))
                throw new InvalidOperationException("The tar file path is empty.");

            if (String.IsNullOrWhiteSpace(destinationPath))
                throw new InvalidOperationException("The destination path is empty.");

            var containerDestinationPath = ConvertToContainerPath(destinationPath);

            fileSystemManager.ExtractTarFile(tarFilePath, containerDestinationPath, decompress);
        }

        public async Task<CommandResult> RunCommandAsync(RemoteCommand remoteCommand)
        {
            var result = await commandRunner.RunCommandAsync(remoteCommand.Command, remoteCommand);
            return new CommandResult { ExitCode = result.ExitCode };
        }

        private void ThrowIfNotActive()
        {
            if (currentState != ContainerState.Active)
            {
                throw new InvalidOperationException("Container is not in an active state.");
            }
        }

        private ProcessStartInfo ToProcessStartInfo(CreateProcessStartInfo createProcessStartInfo, bool impersonate)
        {
            var si = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                LoadUserProfile = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                WorkingDirectory = createProcessStartInfo.WorkingDirectory,
                FileName = createProcessStartInfo.FileName,
                Arguments = createProcessStartInfo.Arguments,
                UserName = impersonate ? user.UserName : createProcessStartInfo.UserName,
                Password = impersonate ? user.SecurePassword : createProcessStartInfo.Password
            };

            if (!createProcessStartInfo.EnvironmentVariables.IsNullOrEmpty())
            {
                si.EnvironmentVariables.Clear();

                foreach (string key in createProcessStartInfo.EnvironmentVariables.Keys)
                {
                    si.EnvironmentVariables[key] = createProcessStartInfo.EnvironmentVariables[key];
                }
            }

            return si;
        }

        public System.Security.Principal.WindowsImpersonationContext GetExecutionContext(bool shouldImpersonate = false)
        {
            return Impersonator.GetContext(user, shouldImpersonate);
        }

        private ContainerCpuStat GetCpuStat()
        {
            var cpuStatistics = jobObject.GetCpuStatistics();
            return new ContainerCpuStat
            {
                TotalProcessorTime = cpuStatistics.TotalKernelTime + cpuStatistics.TotalUserTime,
            };
        }

        public ContainerInfo GetInfo()
        {
            ThrowIfNotActive();

            var ipAddress = IPUtilities.GetLocalIPAddress();
            var ipAddressString = ipAddress != null ? ipAddress.ToString() : "";

            return new ContainerInfo
            {
                HostIPAddress = ipAddressString,
                ContainerIPAddress = ipAddressString,
                ContainerPath = containerDirectory.FullName,
                State = currentState,
                CpuStat = GetCpuStat(),
                MemoryStat = GetMemoryStat(),
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

        public void Initialize(IContainerDirectory containerDirectory, ContainerHandle containerHandle, IContainerUser userInfo)
        {
            this.user = userInfo.GetCredential();
            this.currentState = ContainerState.Active;
            this.containerDirectory = containerDirectory;
            this.containerHandle = containerHandle;
        }

        public void LimitMemory(LimitMemoryInfo info)
        {
            jobObjectLimits.LimitMemory(info.LimitInBytes);
        }

        private void MemoryLimitReached(object sender, EventArgs e)
        {
            var handler = outOfMemoryHandler;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public int ReservePort(int requestedPort)
        {
            ThrowIfNotActive();

            return portManager.ReserveLocalPort((ushort)requestedPort, user.UserName);
        }

        public void Stop(bool kill)
        {
            ThrowIfNotActive();

            // Sends "term" signal to processes
            var processIds = jobObject.GetProcessIds().Where(x => x != owningProcessId);
            var processes = processHelper.GetProcesses(processIds);

            var processTasks = processes.Select(p =>
                Task.Run(() =>
                {
                    try
                    {
                        try
                        {
                            if (!kill)
                            {
                                p.RequestExit();
                                p.WaitForExit(ExitTimeout);
                            }
                        }
                        catch
                        {
                            // TODO: We should probably log any exceptions for debugging purposes.
                        }

                        p.Kill();
                    }
                    catch
                    {
                        // TODO: We should probably log any exceptions for debugging purposes.
                    }
                }))
                .ToArray();

            Task.WaitAll(processTasks);

            // Set state to Stopped
            currentState = ContainerState.Stopped;
        }

        public void Dispose()
        {
            jobObject.Dispose();
        }
    }
}
