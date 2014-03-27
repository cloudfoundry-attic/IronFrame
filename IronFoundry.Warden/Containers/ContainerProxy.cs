using IronFoundry.Warden.Shared.Data;
using IronFoundry.Warden.Shared.Messaging;
using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Containers
{
    public class ContainerProxy : IDisposable, IContainerClient
    {
        private ContainerHostLauncher launcher;
        private IResourceHolder containerResources;
        private ushort? assignedPort;

        public ContainerProxy(ContainerHostLauncher launcher)
        {
            this.launcher = launcher;
        }

        public string ContainerDirectoryPath
        {
            get { return containerResources.Directory.FullName; }
        }

        public ContainerHandle Handle
        {
            get { return containerResources.Handle; }
        }

        public string ContainerUserName
        {
            get { return containerResources.User.UserName; }
        }

        public ContainerState State
        {
            get
            {
                if (IsRemoteActive)
                    return GetRemoteContainerState().GetAwaiter().GetResult();
                return
                    ContainerState.Born;
            }
        }

        private bool IsRemoteActive
        {
            get { return launcher != null && launcher.IsActive; }
        }

        private async Task<string> GetRemoteContainerState()
        {
            var response = await launcher.SendMessageAsync<ContainerStateRequest, ContainerStateResponse>(new ContainerStateRequest());
            return response.result;
        }

        public async Task<CommandResult> RunCommandAsync(RemoteCommand command)
        {
            if (!IsRemoteActive) throw new InvalidOperationException();

            var response = await launcher.SendMessageAsync<RunCommandRequest, RunCommandResponse>(
                new RunCommandRequest(
                    new RunCommandData()
                    {
                        impersonate = command.ShouldImpersonate,
                        command = command.Command,
                        arguments = command.Arguments,
                    }));

            return new CommandResult()
            {
                ExitCode = response.result.exitCode,
                StdOut = response.result.stdOut,
                StdErr = response.result.stdErr,
            };
        }

        public async Task DestoryAsync()
        {
            if (IsRemoteActive)
            {
                var request = new ContainerDestroyRequest();
                var response = await launcher.SendMessageAsync<ContainerDestroyRequest, ContainerDestroyResponse>(request);
            }

            if (containerResources != null)
            {
                containerResources.Destroy();
            }
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

        public void Initialize(IResourceHolder resources)
        {
            this.containerResources = resources;
            launcher.Start(this.ContainerDirectoryPath, this.containerResources.Handle.ToString());

            InvokeRemoteInitialize();
        }

        private async void InvokeRemoteInitialize()
        {
            var request = new ContainerInitializeRequest(
                new ContainerInitializeParameters()
                {
                    containerDirectoryPath = ContainerDirectoryPath,
                    containerHandle = Handle.ToString(),
                    userName = this.containerResources.User.GetCredential().UserName,
                    userPassword = this.containerResources.User.GetCredential().SecurePassword
                });

            var response = await launcher.SendMessageAsync<ContainerInitializeRequest, ContainerInitializeResponse>(request);
        }

        public int ReservePort(int requestedPort)
        {
            if (!assignedPort.HasValue)
            {
                var localTcpPortManager = new LocalTcpPortManager((ushort)requestedPort, this.ContainerUserName);
                assignedPort = localTcpPortManager.ReserveLocalPort();
            }

            return assignedPort.Value;
        }

        public void Stop()
        {
            //bb: What should be done with stop?
        }

        public void Dispose()
        {
            launcher.Dispose();
        }

        public static IContainerClient Restore(string handle, ContainerState containerState)
        {
            throw new NotImplementedException();
        }

        internal static void CleanUp(string handle)
        {
            // bb: need to cleanup based on handle
        }
    }
}
