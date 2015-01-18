using System;
using System.Threading.Tasks;
using IronFoundry.Warden.Configuration;
using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Warden.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace IronFoundry.Warden.Containers
{
    public class ContainerProxy : IDisposable, IContainerClient
    {
        private readonly IContainerHostLauncher launcher;
        
        private readonly List<string> events = new List<string>();
        private object eventLock = new object();
        private ILogEmitter logEmitter;

        private static readonly Dictionary<int, string> exitMessageMap = new Dictionary<int, string>()
        {
            { -2, "Application exceeded memory limits and was stopped." }
        };

        public ContainerProxy(IContainerHostLauncher launcher)
        {
            this.launcher = launcher;
            this.launcher.HostStopped += HostStoppedHandler;
            this.launcher.LogEvent += LogEventHandler;

            ContainerState = ContainerState.Born;
        }

        private bool IsRemoteActive
        {
            get { return launcher.IsActive; }
        }

        private bool RemoteHalted
        {
            get { return !launcher.IsActive && launcher.WasActive; }
        }

        private ContainerState ContainerState { get; set; }
        public string ContainerDirectoryPath { get; private set; }
        public ContainerHandle Handle { get; private set; }

        public int? AssignedPort { get; private set; }

        public async Task BindMountsAsync(IEnumerable<BindMount> mounts)
        {
            if (!IsRemoteActive) throw NotActiveError();

            await launcher.SendMessageAsync<BindMountsRequest, BindMountsResponse>(
                new BindMountsRequest(
                    new BindMountsParameters
                    {
                        Mounts = mounts.ToList(),
                    }));
        }

        public async Task CopyAsync(string source, string destination)
        {
            var request = new CopyRequest(new CopyInfo(source, destination));
            await launcher.SendMessageAsync<CopyRequest, CopyResponse>(request);
        }

        public void Dispose()
        {
            var disposable = launcher as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }

        public IEnumerable<string> DrainEvents()
        {
            lock (eventLock)
            {
                var clone = events.ToArray();
                events.Clear();
                return clone;
            }
        }

        public void EnableLogging(ILogEmitter logEmitter)
        {
            this.logEmitter = logEmitter;
        }

        public async Task<ContainerInfo> GetInfoAsync()
        {
            ContainerInfo info = null;
            if (IsRemoteActive)
            {
                var response = await launcher.SendMessageAsync<ContainerInfoRequest, ContainerInfoResponse>(new ContainerInfoRequest());
                info = response.result;

            }
            else
            {
                info = new ContainerInfo();

                if (RemoteHalted)
                {
                    ContainerState = ContainerState.Stopped;
                }

                info.State = ContainerState;
            }

            info.Events.AddRange(DrainEvents());

            return info;
        }

        private void HostStoppedHandler(object sender, int exitCode)
        {
            if (exitCode == 0) return;

            string msg = null;
            if (!exitMessageMap.TryGetValue(exitCode, out msg))
            {
                msg = string.Format("Application's ContainerHost stopped with exit code: {0}.", exitCode);
            }

            lock (eventLock)
            {
                events.Add(msg);
            }
        }

        private void LogEventHandler(object sender, LogEventArgs eventArgs)
        {
            if (logEmitter != null)
                logEmitter.EmitLogMessage(eventArgs.Type, eventArgs.Data);
        }

        public async Task InitializeAsync(string baseDirectory, string handle, string usersGroup)
        {
            this.Handle = new ContainerHandle(handle);
            launcher.Start(baseDirectory, handle);

            this.ContainerDirectoryPath = await InvokeRemoteInitializeAsync(baseDirectory, usersGroup);
        }
        
        private async Task<string> InvokeRemoteInitializeAsync(string baseDirectory, string usersGroup)
        {
            var request = new ContainerInitializeRequest(
                new ContainerInitializeParameters
                {
                    containerBaseDirectoryPath = baseDirectory,
                    containerHandle = Handle.ToString(),
                    wardenUserGroup = usersGroup,
                });

            var response = await launcher.SendMessageAsync<ContainerInitializeRequest, ContainerInitializeResponse>(request);

            return response.result.containerDirectoryPath;
        }

        public async Task LimitMemoryAsync(ulong bytes)
        {
            if (IsRemoteActive)
            {
                var info = new LimitMemoryInfo(bytes);

                await launcher.SendMessageAsync<LimitMemoryRequest, LimitMemoryResponse>(new LimitMemoryRequest(info));
            }
        }

        static Exception NotActiveError()
        {
            return new InvalidOperationException("The container proxy is not active.");
        }

        public async Task<int> ReservePortAsync(int requestedPort)
        {
            if (AssignedPort.HasValue)
                return AssignedPort.Value;

            var request = new ReservePortRequest(requestedPort);
            var response = await launcher.SendMessageAsync<ReservePortRequest, ReservePortResponse>(request);
            AssignedPort = response.result;
            return response.result;
        }
        
        public static IContainerClient Restore(string handle)
        {
            var container = new ContainerProxy(new ContainerHostLauncher());
            container.Handle = new ContainerHandle(handle);
            container.ContainerState = ContainerState.Stopped;

            return container;
        }

        public async Task<CommandResult> RunCommandAsync(RemoteCommand command)
        {
            if (!IsRemoteActive) throw NotActiveError();

            var response = await launcher.SendMessageAsync<RunCommandRequest, RunCommandResponse>(
                new RunCommandRequest(
                    new RunCommandData
                    {
                        privileged = command.Privileged,
                        command = command.Command,
                        arguments = command.Arguments,
                        environment = command.Environment,
                        working_dir = command.WorkingDirectory
                    }));

            return new CommandResult
            {
                ExitCode = response.result.exitCode,
                StdOut = response.result.stdOut,
                StdErr = response.result.stdErr,
            };
        }

        public async Task StopAsync(bool kill)
        {
            if (IsRemoteActive)
            {
                await launcher.SendMessageAsync<StopRequest, StopResponse>(new StopRequest(kill));
                launcher.Stop();
            }

            ContainerState = ContainerState.Stopped;
        }
    }
}