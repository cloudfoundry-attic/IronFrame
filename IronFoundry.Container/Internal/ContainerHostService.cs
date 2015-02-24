using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Container.Messaging;
using IronFoundry.Container.Utilities;
using NLog;

namespace IronFoundry.Container.Internal
{
    internal interface IContainerHostService
    {
        IContainerHostClient StartContainerHost(string containerId, IContainerDirectory directory, JobObject jobObject, NetworkCredential credentials);
    }

    internal sealed class ContainerHostService : IContainerHostService
    {
        static readonly TimeSpan HostProcessStartTimeout = TimeSpan.FromSeconds(5);

        readonly ContainerHostDependencyHelper dependencyHelper;
        readonly FileSystemManager fileSystem;
        readonly Logger log = LogManager.GetCurrentClassLogger();
        readonly IProcessRunner processRunner;

        internal ContainerHostService(FileSystemManager fileSystem, IProcessRunner processRunner, ContainerHostDependencyHelper dependencyHelper)
        {
            this.fileSystem = fileSystem;
            this.processRunner = processRunner;
            this.dependencyHelper = dependencyHelper;
        }

        public ContainerHostService()
            : this(
                new FileSystemManager(),
                new ProcessRunner(),
                new ContainerHostDependencyHelper()
            )
        {
        }

        private void CopyHostToContainer(IContainerDirectory directory)
        {
            fileSystem.CopyFile(
                dependencyHelper.ContainerHostExePath,
                directory.MapBinPath(dependencyHelper.ContainerHostExe));

            // This check is here for the acceptance tests.
            // They get ContainerHost.exe by referencing the project.  However, msbuild does
            // not copy .config files of referenced assemblies.  Thus when running the acceptance
            // tests, the ContainerHost.exe used does not have an app.config.
            if (fileSystem.FileExists(dependencyHelper.ContainerHostExeConfigPath))
            {
                fileSystem.CopyFile(
                    dependencyHelper.ContainerHostExeConfigPath,
                    directory.MapBinPath(dependencyHelper.ContainerHostExeConfig));
            }

            foreach (var dependencyFilePath in dependencyHelper.GetContainerHostDependencies())
            {
                var targetFilePath = directory.MapBinPath(Path.GetFileName(dependencyFilePath));
                fileSystem.CopyFile(dependencyFilePath, targetFilePath);
            }
        }

        public IContainerHostClient StartContainerHost(string containerId, IContainerDirectory directory, JobObject jobObject, NetworkCredential credentials)
        {
            CopyHostToContainer(directory);

            var hostRunSpec = new ProcessRunSpec
            {
                ExecutablePath = directory.MapBinPath(dependencyHelper.ContainerHostExe),
                Arguments = new[] { containerId },
                BufferedInputOutput = true,
                WorkingDirectory = directory.UserPath,
                Credentials = credentials,
            };

            var hostProcess = processRunner.Run(hostRunSpec);

            WaitForProcessToStart(hostProcess, HostProcessStartTimeout);

            // Order here is important.
            // - Start the process and verify that it's running
            // - Add the process to the job object
            // - Start the RPC message pump
            //
            // We need to ensure that the host process cannot create any new processes before
            // it's added to the job object.
            jobObject.AssignProcessToJob(hostProcess.Handle);

            var messageTransport = MessageTransport.Create(hostProcess.StandardOutput, hostProcess.StandardInput);
            var messagingClient = MessagingClient.Create(async message =>
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
                    log.Log(LogLevel.Error, e.ToString(), e);
                }
                return Task.FromResult(0);
            });

            var containerHostClient = new ContainerHostClient(hostProcess, messageTransport, messagingClient, jobObject);

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

        void WaitForProcessToStart(IProcess hostProcess, TimeSpan timeout)
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
