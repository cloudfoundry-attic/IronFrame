using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Container.Messaging;
using IronFoundry.Warden.Utilities;
using NLog;

namespace IronFoundry.Container
{
    public interface IContainerHostService
    {
        IContainerHostClient StartContainerHost(string jobObjectName, NetworkCredential credentials);
    }

    public class ContainerHostService : IContainerHostService
    {
        const string HostExe = "IronFoundry.Container.Host.exe";
        readonly Logger log = LogManager.GetCurrentClassLogger();

        public IContainerHostClient StartContainerHost(string jobObjectName, NetworkCredential credentials)
        {
            var argumentBuilder = new StringBuilder();
            argumentBuilder.AppendFormat("--jobObject {0}", jobObjectName);

            var hostFullPath = Path.Combine(Directory.GetCurrentDirectory(), HostExe);
            var hostStartInfo = new ProcessStartInfo(hostFullPath, argumentBuilder.ToString());

            hostStartInfo.RedirectStandardInput = true;
            hostStartInfo.RedirectStandardOutput = true;
            hostStartInfo.RedirectStandardError = true;
            hostStartInfo.UseShellExecute = false;

            if (credentials != null)
            {
                hostStartInfo.UserName = credentials.UserName;
                hostStartInfo.Password = credentials.SecurePassword;
                hostStartInfo.LoadUserProfile = false;
            }

            var hostProcess = new Process();
            hostProcess.StartInfo = hostStartInfo;
            hostProcess.EnableRaisingEvents = true;

            hostProcess.Start();

            string status = hostProcess.StandardError.ReadLine();
            ThrowIfFailedToStart(status);

            var messageTransport = new MessageTransport(hostProcess.StandardOutput, hostProcess.StandardInput);
            var messagingClient = new MessagingClient(async message =>
            {
                await messageTransport.PublishRequestAsync(message);
            });

            messageTransport.SubscribeResponse(message =>
            {
                messagingClient.PublishResponse(message);
                return Task.FromResult(0);
            });

            messageTransport.SubscribeEvent(@event =>
            {
                try
                {
                    messagingClient.PublishEvent(@event);
                }
                catch (Exception e)
                {
                    log.LogException(LogLevel.Error, e.ToString(), e);
                }
                return Task.FromResult(0);
            });

            var containerHostClient = new ContainerHostClient(ProcessHelper.WrapProcess(hostProcess), messageTransport, messagingClient);

            messageTransport.Start();

            return containerHostClient;
        }

        void ThrowIfFailedToStart(string status)
        {
            if (status != "OK")
            {
                throw new Exception("The container host process failed to start with the error: " + status);
            }
        }
    }
}
