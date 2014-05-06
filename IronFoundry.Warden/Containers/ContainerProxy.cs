using System;
using System.Threading.Tasks;
using IronFoundry.Warden.Configuration;
using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Warden.Shared.Data;
using IronFoundry.Warden.Tasks;
using System.Collections.Generic;

namespace IronFoundry.Warden.Containers
{
    public class ContainerProxy : IDisposable, IContainerClient
    {
        private readonly IContainerHostLauncher launcher;
        private ContainerState cachedContainerState;
        private IResourceHolder containerResources;
        private readonly List<string> events = new List<string>();
        private object eventLock = new object();

        private static readonly Dictionary<int, string> exitMessageMap = new Dictionary<int, string>()
        {
            { -2, "Application exceeded memory limits and was stopped." }
        };

        public ContainerProxy(IContainerHostLauncher launcher)
        {
            this.launcher = launcher;
            this.launcher.HostStopped += HostStoppedHandler;

            cachedContainerState = ContainerState.Born;
        }

        public string ContainerUserName
        {
            get { return containerResources.User.UserName; }
        }

        private bool IsRemoteActive
        {
            get { return launcher.IsActive; }
        }

        private bool RemoteHalted
        {
            get { return !launcher.IsActive && launcher.WasActive; }
        }

        public string ContainerDirectoryPath
        {
            get { return containerResources.Directory.FullName; }
        }

        public ContainerHandle Handle
        {
            get { return containerResources.Handle; }
        }

        public ContainerState State
        {
            get
            {
                if (RemoteHalted)
                {
                    cachedContainerState = ContainerState.Stopped;
                }
                else if (IsRemoteActive)
                {
                    cachedContainerState = GetRemoteContainerState().GetAwaiter().GetResult();                
                }

                return cachedContainerState;
            }
        }

        public async Task<CommandResult> RunCommandAsync(RemoteCommand command)
        {
            if (!IsRemoteActive) throw new InvalidOperationException();

            var response = await launcher.SendMessageAsync<RunCommandRequest, RunCommandResponse>(
                new RunCommandRequest(
                    new RunCommandData
                    {
                        impersonate = command.ShouldImpersonate,
                        command = command.Command,
                        arguments = command.Arguments,
                    }));

            return new CommandResult
                   {
                       ExitCode = response.result.exitCode,
                       StdOut = response.result.stdOut,
                       StdErr = response.result.stdErr,
                   };
        }

        public async Task DestroyAsync()
        {
            if (IsRemoteActive)
            {
                var request = new ContainerDestroyRequest();
                var response = await launcher.SendMessageAsync<ContainerDestroyRequest, ContainerDestroyResponse>(request);
            }

            if (cachedContainerState != ContainerState.Destroyed && containerResources != null)
            {
                containerResources.Destroy();
            }

            cachedContainerState = ContainerState.Destroyed;
        }

        public async Task<ProcessStats> GetProcessStatisticsAsync()
        {
            if (IsRemoteActive)
            {
                var statsResponse = await launcher.SendMessageAsync<ContainerStatisticsRequest, ContainerStatisticsResponse>(new ContainerStatisticsRequest());
                return statsResponse.result;
            }

            return new ProcessStats();
        }

        public async Task EnableLoggingAsync(InstanceLoggingInfo loggingInfo)
        {
            if (IsRemoteActive)
            {
                var enableResponse = await launcher.SendMessageAsync<EnableLoggingRequest, EnableLoggingResponse>(new EnableLoggingRequest { @params = loggingInfo });
            }
        }

        public void Initialize(IResourceHolder resources)
        {
            containerResources = resources;
            launcher.Start(ContainerDirectoryPath, containerResources.Handle.ToString());

            InvokeRemoteInitialize();
        }

        public async Task LimitMemoryAsync(ulong bytes)
        {
            if (IsRemoteActive)
            {
                var info = new LimitMemoryInfo(bytes);

                await launcher.SendMessageAsync<LimitMemoryRequest, LimitMemoryResponse>(new LimitMemoryRequest(info));
            }
        }

        public int ReservePort(int requestedPort)
        {
            if (!containerResources.AssignedPort.HasValue)
            {
                containerResources.AssignedPort = containerResources.LocalTcpPortManager.ReserveLocalPort((ushort)requestedPort, ContainerUserName);
            }

            return containerResources.AssignedPort.Value;
        }

        public async Task StopAsync()
        {
            await DestroyAsync();
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

        public void Dispose()
        {
            var disposable = launcher as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }

        private async Task<string> GetRemoteContainerState()
        {
            var response = await launcher.SendMessageAsync<ContainerStateRequest, ContainerStateResponse>(new ContainerStateRequest());
            return response.result;
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

        private async void InvokeRemoteInitialize()
        {
            var request = new ContainerInitializeRequest(
                new ContainerInitializeParameters
                {
                    containerDirectoryPath = ContainerDirectoryPath,
                    containerHandle = Handle.ToString(),
                    userName = containerResources.User.GetCredential().UserName,
                    userPassword = containerResources.User.GetCredential().SecurePassword
                });

            var response = await launcher.SendMessageAsync<ContainerInitializeRequest, ContainerInitializeResponse>(request);
        }

        public static IContainerClient Restore(string handle, ContainerState containerState)
        {
            throw new NotImplementedException();
        }

        internal static void CleanUp(string handle)
        {
            // this creates a temporary set of resources to make sure we clean up
            var holder = ContainerResourceHolder.Create(new WardenConfig(), new ContainerHandle(handle));
            holder.Destroy();
        }



   
    }
}