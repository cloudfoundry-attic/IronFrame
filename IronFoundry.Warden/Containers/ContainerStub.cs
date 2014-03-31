using IronFoundry.Warden.Shared.Data;
using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Containers
{
    public class ContainerStub : IContainer, IDisposable
    {
        private JobObject jobObject;
        private ContainerState currentState;
        private string containerDirectory;
        private ContainerHandle containerHandle;
        private System.Net.NetworkCredential user;
        private readonly ICommandRunner commandRunner;
        private ProcessHelper processHelper;
        private ILogEmitter logEmitter;
        private ProcessMonitor processMonitor;

        public ContainerStub(
            JobObject jobObject,
            ICommandRunner commandRunner,
            ProcessHelper processHelper,
            ProcessMonitor processMonitor)
        {
            this.jobObject = jobObject;
            this.currentState = ContainerState.Born;
            this.commandRunner = commandRunner;
            this.processHelper = processHelper;
            this.processMonitor = processMonitor;

            this.processMonitor.OutputDataReceived += LogOutputData;
            this.processMonitor.ErrorDataReceived += LogErrorData;
        }

        public string ContainerDirectoryPath
        {
            get { return containerDirectory; }
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

        public Utilities.IProcess CreateProcess(Shared.Messaging.CreateProcessStartInfo si, bool impersonate = false)
        {
            ThrowIfNotActive();

            Process p = new Process()
            {
                StartInfo = ToProcessStartInfo(si, impersonate),
            };

            p.EnableRaisingEvents = true;

            var wrapped = new RealProcessWrapper(p);
            processMonitor.TryAdd(wrapped);

            bool started = p.Start();
            Debug.Assert(started);

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            jobObject.AssignProcessToJob(p);

            return wrapped;
        }

        public async Task<CommandResult> RunCommandAsync(RemoteCommand remoteCommand)
        {
            var result = await commandRunner.RunCommandAsync(remoteCommand.ShouldImpersonate, remoteCommand.Command, remoteCommand.Arguments);
            return new CommandResult { ExitCode = result.ExitCode };
        }

        private void LogErrorData(object sender, ProcessDataReceivedEventArgs e)
        {
            if (logEmitter != null)
            {
                logEmitter.EmitLogMessage(logmessage.LogMessage.MessageType.ERR, e.Data);
            }
        }

        private void LogOutputData(object sender, ProcessDataReceivedEventArgs e)
        {
            if (logEmitter != null)
            {
                logEmitter.EmitLogMessage(logmessage.LogMessage.MessageType.OUT, e.Data);
            }
        }

        private void ThrowIfNotActive()
        {
            if (currentState != ContainerState.Active)
            {
                throw new InvalidOperationException("Container is not in an active state.");
            }
        }

        private ProcessStartInfo ToProcessStartInfo(Shared.Messaging.CreateProcessStartInfo createProcessStartInfo, bool impersonate)
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

            if (createProcessStartInfo.EnvironmentVariables.Count > 0)
            {
                si.EnvironmentVariables.Clear();
                foreach (string key in createProcessStartInfo.EnvironmentVariables.Keys)
                {
                    si.EnvironmentVariables[key] = createProcessStartInfo.EnvironmentVariables[key];
                }
            }

            return si;
        }

        public void Destroy()
        {
            this.currentState = ContainerState.Destroyed;
        }

        public System.Security.Principal.WindowsImpersonationContext GetExecutionContext(bool shouldImpersonate = false)
        {
            return Impersonator.GetContext(user, shouldImpersonate);
        }

        public ProcessStats GetProcessStatistics()
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

        public void Initialize(string containerDirectory, string containerHandle, IContainerUser userInfo)
        {
            this.user = userInfo.GetCredential();
            this.currentState = ContainerState.Active;
            this.containerDirectory = containerDirectory;
            this.containerHandle = new ContainerHandle(containerHandle);
        }

        public int ReservePort(int requestedPort)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            jobObject.Dispose();
        }

        class RealProcessWrapper : IProcess
        {
            private Process wrappedProcess;

            public RealProcessWrapper(Process process)
            {
                this.wrappedProcess = process;

                this.wrappedProcess.Exited += (o, e) => { this.OnExited(o, e); };
                this.wrappedProcess.OutputDataReceived += WrappedOutputDataReceived;
                this.wrappedProcess.ErrorDataReceived += WrappedErrorDataRecevied;
            }

            public int ExitCode
            {
                get { return this.wrappedProcess.ExitCode; }
            }

            public IntPtr Handle
            {
                get { return this.wrappedProcess.Handle; }
            }

            public bool HasExited
            {
                get { return this.wrappedProcess.HasExited; }
            }

            public int Id
            {
                get { return this.wrappedProcess.Id; }
            }

            public TimeSpan TotalProcessorTime
            {
                get { return this.wrappedProcess.TotalProcessorTime; }
            }

            public TimeSpan TotalUserProcessorTime
            {
                get { return this.wrappedProcess.UserProcessorTime; }
            }

            public long WorkingSet
            {
                get { return this.wrappedProcess.WorkingSet64; }
            }

            public long PrivateMemoryBytes
            {
                get { return this.wrappedProcess.PrivateMemorySize64; }
            }

            public event EventHandler Exited;

            protected virtual void OnExited(object sender, EventArgs eventArgs)
            {
                var handlers = Exited;
                if (handlers != null)
                {
                    handlers(this, eventArgs);
                }

                this.wrappedProcess.ErrorDataReceived -= WrappedErrorDataRecevied;
                this.wrappedProcess.OutputDataReceived -= WrappedOutputDataReceived;
            }

            public void Kill()
            {
                if (this.wrappedProcess.HasExited) return;
                this.wrappedProcess.Kill();
            }

            public void WaitForExit()
            {
                this.wrappedProcess.WaitForExit();
            }

            public void WaitForExit(int milliseconds)
            {
                this.wrappedProcess.WaitForExit(milliseconds);
            }

            public void Dispose()
            {
                this.wrappedProcess.Dispose();
            }

            private void WrappedErrorDataRecevied(object sender, DataReceivedEventArgs e)
            {
                OnErrorDataReceived(this, new ProcessDataReceivedEventArgs(e.Data));
            }

            private void WrappedOutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                OnOutputDataReceived(this, new ProcessDataReceivedEventArgs(e.Data));
            }

            public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
            protected virtual void OnOutputDataReceived(object sender, ProcessDataReceivedEventArgs e)
            {
                var handlers = OutputDataReceived;
                if (handlers != null)
                {
                    handlers(this, e);
                }
            }

            public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;
            protected virtual void OnErrorDataReceived(object sender, ProcessDataReceivedEventArgs e)
            {
                var handlers = ErrorDataReceived;
                if (handlers != null)
                {
                    handlers(this, e);
                }
            }

        }

        public void AttachEmitter(ILogEmitter emitter)
        {
            this.logEmitter = emitter;
        }
    }
}
