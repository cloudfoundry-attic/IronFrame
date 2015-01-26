using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Container.Messaging;
using IronFoundry.Container.Utilities;
using NLog;

namespace IronFoundry.Container
{
    public interface IContainerHostService
    {
        IContainerHostClient StartContainerHost(string containerId, JobObject jobObject, NetworkCredential credentials);
    }

    public class ContainerHostService : IContainerHostService
    {
        const string HostExe = "IronFoundry.Container.Host.exe";
        static readonly TimeSpan HostProcessStartTimeout = TimeSpan.FromSeconds(5);

        readonly Logger log = LogManager.GetCurrentClassLogger();

        public IContainerHostClient StartContainerHost(string containerId, JobObject jobObject, NetworkCredential credentials)
        {
            var hostFullPath = Path.Combine(Directory.GetCurrentDirectory(), HostExe);
            var hostArguments = containerId;
            var hostStartInfo = new ProcessStartInfo(hostFullPath, hostArguments);

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

            WaitForProcessToStart(hostProcess, HostProcessStartTimeout);

            // Order here is important.
            // - Start the process and verify that it's running
            // - Add the process to the job object
            // - Start the RPC message pump
            //
            // We need to ensure that the host process cannot create any new processes before
            // it's added to the job object.
            jobObject.AssignProcessToJob(hostProcess);

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

            var containerHostClient = new ContainerHostClient(ProcessHelper.WrapProcess(hostProcess), messageTransport, messagingClient, jobObject);

            messageTransport.Start();

            return containerHostClient;
        }

        string ReadLineWithTimeout(TextReader reader, TimeSpan timeout)
        {
            var startTime = DateTimeOffset.UtcNow;
            var builder = new StringBuilder();
            do
            {
                if (reader.Peek() != 0)
                {
                    var ch = (char)reader.Read();
                    
                    if (ch == '\r')
                        continue;

                    if (ch == '\n')
                        return builder.ToString();

                    builder.Append(ch);
                }
            } 
            while (DateTimeOffset.UtcNow - startTime < timeout);

            return null;
        }

        void WaitForProcessToStart(Process hostProcess, TimeSpan timeout)
        {
            string status = ReadLineWithTimeout(hostProcess.StandardError, timeout);
            if (status == null)
            {
                throw new Exception(
                    String.Format(
                        "The container host process failed to start within the timeout period ({0:N}s).", 
                        timeout.TotalSeconds));
            }
            else if (status != "OK")
            {
                throw new Exception("The container host process failed to start with the error: " + status);
            }
        }
    }
}
