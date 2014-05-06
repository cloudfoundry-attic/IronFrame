using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Shared.Messaging;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Containers
{
    public interface IContainerHostLauncher
    {
        event EventHandler<int> HostStopped;

        int HostProcessId { get; }
        bool IsActive { get; }
        bool WasActive { get; }
        int? LastExitCode { get; }
        void Start(string workingDirectory, string jobObjectName);
        Task<TResult> SendMessageAsync<T, TResult>(T request)
            where T : JsonRpcRequest
            where TResult : JsonRpcResponse;
    }

    public class ContainerHostLauncher : IDisposable, IContainerHostLauncher
    {
        string hostExe = "IronFoundry.Warden.ContainerHost.exe";
        Process hostProcess;
        MessageTransport messageTransport;
        MessagingClient messagingClient;

        public event EventHandler<int> HostStopped;

        public int HostProcessId
        {
            get { return hostProcess != null ? hostProcess.Id : 0; }
        }

        public virtual bool IsActive
        {
            get { return hostProcess != null && hostProcess.HasExited == false; }
        }

        public virtual bool WasActive
        {
            get { return hostProcess != null; }
        }

        public virtual void Dispose()
        {
            DisposeMessageHandling();

            if (hostProcess != null)
            {
                if (!hostProcess.HasExited)
                    hostProcess.SafeKill();

                hostProcess.Dispose();
                hostProcess = null;
            }
        }

        public virtual int? LastExitCode
        {
            get
            {
                if (hostProcess != null && hostProcess.HasExited)
                    return hostProcess.ExitCode;

                return null;
            }
        }

        public virtual void Start(string workingDirectory, string jobObjectName)
        {
            if (hostProcess == null)
            {
                var hostFullPath = Path.Combine(Directory.GetCurrentDirectory(), hostExe);
                var hostStartInfo = new ProcessStartInfo(hostFullPath, jobObjectName);

                hostStartInfo.RedirectStandardInput = true;
                hostStartInfo.RedirectStandardOutput = true;
                hostStartInfo.RedirectStandardError = true;
                hostStartInfo.UseShellExecute = false;

                hostProcess = new Process();
                hostProcess.StartInfo = hostStartInfo;
                hostProcess.EnableRaisingEvents = true;
                hostProcess.Exited += (o, e) => 
                    {
                        OnHostStopped(hostProcess != null && hostProcess.HasExited ? hostProcess.ExitCode : 0);
                        DisposeMessageHandling(); 
                    };

                hostProcess.Start();

                messageTransport = new MessageTransport(hostProcess.StandardOutput, hostProcess.StandardInput);
                messagingClient = new MessagingClient(message =>
                {
                    messageTransport.PublishAsync(message).GetAwaiter().GetResult();
                });
                messageTransport.SubscribeResponse(message =>
                {
                    messagingClient.PublishResponse(message);
                    return Task.FromResult(0);
                });
            }
        }

        protected virtual void OnHostStopped(int exitCode)
        {
            var handlers = HostStopped;
            if (handlers != null)
            {
                handlers(this, exitCode);
            }
        }

        private void DisposeMessageHandling()
        {
            if (this.messagingClient != null)
            {
                this.messagingClient.Dispose();
                this.messagingClient = null;
            }

            if (this.messageTransport != null)
            {
                this.messageTransport.Dispose();
                this.messageTransport = null;
            }
        }

        public virtual async Task<TResult> SendMessageAsync<T, TResult>(T request)
            where T : JsonRpcRequest
            where TResult : JsonRpcResponse
        {
            return await messagingClient.SendMessageAsync<T, TResult>(request);
        }
    }
}
