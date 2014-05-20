using System;
using System.Threading.Tasks;
using IronFoundry.Warden.Configuration;
using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Warden.Shared.Data;
using IronFoundry.Warden.Tasks;
using System.Collections.Generic;
using System.Linq;

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

        private string ContainerUserName
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
                    cachedContainerState = ContainerState.Stopped;
                }

                info.State = cachedContainerState;
            }

            info.Events.AddRange(DrainEvents());
            
            return info;
        }
        
        public async Task<CommandResult> RunCommandAsync(RemoteCommand command)
        {
            if (!IsRemoteActive) throw NotActiveError();

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

        public async Task EnableLoggingAsync(InstanceLoggingInfo loggingInfo)
        {
            if (IsRemoteActive)
            {
                var enableResponse = await launcher.SendMessageAsync<EnableLoggingRequest, EnableLoggingResponse>(new EnableLoggingRequest { @params = loggingInfo });
            }
        }

        public Task InitializeAsync(IResourceHolder resources)
        {
            containerResources = resources;
            launcher.Start(ContainerDirectoryPath, containerResources.Handle.ToString());

            return InvokeRemoteInitializeAsync();
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

        public int ReservePort(int requestedPort)
        {
            if (!containerResources.AssignedPort.HasValue)
            {
                containerResources.AssignedPort = containerResources.LocalTcpPortManager.ReserveLocalPort((ushort)requestedPort, ContainerUserName);
            }

            return containerResources.AssignedPort.Value;
        }

        public async Task StopAsync(bool kill)
        {
            if (IsRemoteActive)
                await launcher.SendMessageAsync<StopRequest, StopResponse>(new StopRequest(kill));

            cachedContainerState = ContainerState.Stopped;
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

        private async Task InvokeRemoteInitializeAsync()
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