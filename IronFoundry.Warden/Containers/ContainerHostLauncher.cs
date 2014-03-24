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
    public class ContainerHostLauncher : IDisposable
    {
        string hostExe = "IronFoundry.Warden.ContainerHost.exe";
        Process hostProcess;
        MessageTransport messageTransport;
        MessagingClient messagingClient;

        public int HostProcessId
        {
            get { return hostProcess != null ? hostProcess.Id : 0; }
        }

        public virtual void Dispose()
        {
            if (hostProcess != null)
            {
                if (!hostProcess.HasExited)
                    hostProcess.SafeKill();

                hostProcess.Dispose();
                hostProcess = null;
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

                hostProcess = Process.Start(hostStartInfo);

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

        public virtual async Task<TResult> SendMessageAsync<T, TResult>(T request)
            where T : JsonRpcRequest
            where TResult : JsonRpcResponse
        {
            return await messagingClient.SendMessageAsync<T, TResult>(request);
        }
    }       
}
