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
                string state = GetRemoteContainerState().GetAwaiter().GetResult();
                return state;
            }
        }

        private async Task<string> GetRemoteContainerState()
        {
            var response = await launcher.SendMessageAsync<ContainerStateRequest, ContainerStateResponse>(new ContainerStateRequest());
            return response.result;
        }

        public async Task<CommandResult> RunCommandAsync(RemoteCommand command)
        {
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

        public async void Destroy()
        {
            var request = new ContainerDestroyRequest();
            var response = await launcher.SendMessageAsync<ContainerDestroyRequest, ContainerDestroyResponse>(request);

            containerResources.Destroy();
        }

        public System.Security.Principal.WindowsImpersonationContext GetExecutionContext(bool shouldImpersonate = false)
        {
            throw new NotImplementedException();
        }

        public Utilities.ProcessStats GetProcessStatistics()
        {
            return new ProcessStats();
        }

        // Deprecating
        public void Initialize()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
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
