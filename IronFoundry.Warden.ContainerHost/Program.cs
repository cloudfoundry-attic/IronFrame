using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IronFoundry.Warden.Configuration;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Container.Messaging;
using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Utilities;
using logmessage;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.ContainerHost
{
    class ProcessContext
    {
        public ProcessContext()
        {
            StandardError = new StringBuilder();
            StandardOutputTail = new Queue<string>();
        }

        public bool HasExited { get; set; }
        public int ExitCode { get; set; }
        public StringBuilder StandardError { get; set; }
        public Queue<string> StandardOutputTail { get; set; }

        public void HandleErrorData(object sender, DataReceivedEventArgs e)
        {
            StandardError.AppendLine(e.Data);
        }

        public void HandleOutputData(object sender, DataReceivedEventArgs e)
        {
            while (StandardOutputTail.Count > 100)
                StandardOutputTail.Dequeue();

            StandardOutputTail.Enqueue(e.Data);
        }

        public void HandleProcessExit(object sender, EventArgs e)
        {
            var process = (Process)sender;

            HasExited = true;
            ExitCode = process.ExitCode;
        }
    }

    class ContainerHostConfig : IWardenConfig
    {
        public string ContainerBasePath { get; set; }
        public ushort TcpPort { get; set; }
        public bool DeleteContainerDirectories { get; set; }
        public string WardenUsersGroup { get; set; }
    }

    class Program
    {
        const int OutOfMemoryExitCode = -2;

        static ManualResetEvent exitEvent = new ManualResetEvent(false);
        static ConcurrentDictionary<int, ProcessContext> processContexts = new ConcurrentDictionary<int, ProcessContext>();
        static ContainerStub container;

        static void HandleOutOfMemory(object sender, EventArgs e)
        {
            Environment.Exit(OutOfMemoryExitCode);
        }

        static void Main(string[] args)
        {
            //Debugger.Launch();

            if (args.IsNullOrEmpty())
            {
                Console.Error.WriteLine("There must be a start or destroy command supplied");
                Environment.Exit(10);
            }

            var argumentQueue = new Queue<string>(args);

            var command = argumentQueue.Dequeue();

            switch (command.ToLowerInvariant())
            {
                case "start":
                    Start(argumentQueue);
                    break;

                case "destroy":
                    DestroyContainer(argumentQueue);
                    break;

                default:
                    throw new InvalidOperationException(string.Format("Unrecognized command {0}", command));
            }
        }

        private static void Start(IEnumerable<string> args)
        {
            //Debugger.Launch();

            var input = Console.In;
            var output = Console.Out;
            string handle = null;

            var options = new NDesk.Options.OptionSet {
                { "handle=", v => handle = v },
            };

            options.Parse(args);

            if (String.IsNullOrWhiteSpace(handle))
                ExitWithError("Missing --handle option for starting container", 10);

            var jobObject = new JobObject(handle);
            var jobObjectLimits = new JobObjectLimits(jobObject);
            var hostProcess = System.Diagnostics.Process.GetCurrentProcess();
            jobObject.AssignProcessToJob(hostProcess);
            var processMonitor = new ProcessMonitor();
            container = new ContainerStub(jobObject, jobObjectLimits, BuildCommandRunner(), new ProcessHelper(), processMonitor, new LocalTcpPortManager(), new FileSystemManager());

            container.OutOfMemory += HandleOutOfMemory;

            using (var transport = new MessageTransport(input, output))
            {
                var dispatcher = new MessageDispatcher();

                dispatcher.RegisterMethod<BindMountsRequest>(BindMountsRequest.MethodName, r =>
                {
                    container.BindMounts(r.@params.Mounts);

                    return Task.FromResult<object>(new BindMountsResponse(r.id));
                });

                dispatcher.RegisterMethod<ContainerInfoRequest>(ContainerInfoRequest.MethodName, r =>
                {
                    var info = container.GetInfo();

                    return Task.FromResult<object>(new ContainerInfoResponse(r.id, info));
                });

                dispatcher.RegisterMethod<ContainerInitializeRequest>(ContainerInitializeRequest.MethodName, (r) =>
                {
                    var containerHandle = new ContainerHandle(r.@params.containerHandle);
                    var containerUser = ContainerUser.CreateUser(containerHandle, new LocalPrincipalManager(new DesktopPermissionManager(), r.@params.wardenUserGroup));

                    var containerDirectory = new ContainerDirectory(
                        containerHandle, 
                        containerUser, 
                        new FileSystemManager(), 
                        r.@params.containerBaseDirectoryPath, 
                        true);

                    container.Initialize(
                        containerDirectory,
                        containerHandle,
                        containerUser);

                    return Task.FromResult<object>(new ContainerInitializeResponse(r.id, containerDirectory.FullName));
                });

                dispatcher.RegisterMethod<ContainerStateRequest>(ContainerStateRequest.MethodName, (r) =>
                {
                    return Task.FromResult<object>(
                        new ContainerStateResponse(r.id, container.State.ToString()));
                });

                dispatcher.RegisterMethod<CopyRequest>(CopyRequest.MethodName, r =>
                {
                    container.Copy(r.@params.Source, r.@params.Destination);
                    return Task.FromResult<object>(new CopyResponse(r.id));
                });

                dispatcher.RegisterMethod<CopyFileInRequest>(CopyFileInRequest.MethodName, r =>
                {
                    
                    container.CopyFileIn(r.@params.SourceFilePath, r.@params.DestinationFilePath);
                    return Task.FromResult<object>(new CopyFileResponse(r.id));
                });

                dispatcher.RegisterMethod<CopyFileOutRequest>(CopyFileOutRequest.MethodName, r =>
                {
                    container.CopyFileOut(r.@params.SourceFilePath, r.@params.DestinationFilePath);
                    return Task.FromResult<object>(new CopyFileResponse(r.id));
                });

                dispatcher.RegisterMethod<LimitMemoryRequest>(LimitMemoryRequest.MethodName, (r) =>
                {
                    container.LimitMemory(r.@params);
                    return Task.FromResult<object>(new LimitMemoryResponse(r.id));
                });

                dispatcher.RegisterMethod<ReservePortRequest>(ReservePortRequest.MethodName, r =>
                {
                    var reservedPort = container.ReservePort(r.@params);
                    return Task.FromResult<object>(new ReservePortResponse(r.id, reservedPort));
                });

                dispatcher.RegisterMethod<RunCommandRequest>(RunCommandRequest.MethodName, async (r) =>
                {
                    var remoteCommand = new RemoteCommand(r.@params.privileged, r.@params.command, r.@params.arguments, r.@params.environment, r.@params.working_dir);
                    var result = await container.RunCommandAsync(remoteCommand);

                    return new RunCommandResponse(
                        r.id,
                        new RunCommandResponseData()
                        {
                            exitCode = result.ExitCode,
                            stdErr = result.StdErr,
                            stdOut = result.StdOut,
                        });

                });

                dispatcher.RegisterMethod<StopRequest>(StopRequest.MethodName, r =>
                {
                    container.Stop(r.@params.Kill);
                    return Task.FromResult<object>(new StopResponse(r.id));
                });

                transport.SubscribeRequest(
                    async (request) =>
                    {
                        var response = await dispatcher.DispatchAsync(request);
                        await transport.PublishResponseAsync(response);
                    });

                processMonitor.ErrorDataReceived += (o,e) =>
                {
                    var jsonLogEvent = JObject.FromObject(new LogEvent() { MessageType = LogMessage.MessageType.ERR, LogData = e.Data });
                    transport.PublishEventAsync(jsonLogEvent);
                };

                processMonitor.OutputDataReceived += (o, e) =>
                {
                    var jsonLogEvent = JObject.FromObject(new LogEvent() { MessageType = LogMessage.MessageType.OUT, LogData = e.Data });
                    transport.PublishEventAsync(jsonLogEvent);
                };

                exitEvent.WaitOne();
            }
        }

        private static void DestroyContainer(IEnumerable<string> args)
        {
            string handle = null;
            string containerBasePath = null;
            string tcpPort = null;
            bool deleteDirectories = true;
            ushort? containerPort = null;

            var options = new NDesk.Options.OptionSet {
                { "handle=", v => handle = v },
                { "containerBasePath=", v => containerBasePath = v},
                { "tcpPort=", v => tcpPort = v },
                { "containerPort=", v => containerPort = ushort.Parse(v) },
                { "deleteDirectories", v => { if (v != null)  deleteDirectories = true; } },
            };

            options.Parse(args);

            if (string.IsNullOrWhiteSpace(handle))
                ExitWithError("Missing --handle option for destroying container", 10);

            if (string.IsNullOrWhiteSpace(containerBasePath))
                ExitWithError("Missing --containerBasePath option", 10);

            if (string.IsNullOrWhiteSpace(tcpPort))
                ExitWithError("Missing --tcpPort option", 10);

            var config = new ContainerHostConfig()
            {
                ContainerBasePath = containerBasePath,
                TcpPort = ushort.Parse(tcpPort),
                DeleteContainerDirectories = deleteDirectories
            };

            var holder = ContainerResourceHolder.CreateForDestroy(config, new ContainerHandle(handle), containerPort);
            holder.Destroy();
        }

        private static void ExitWithError(string message, int exitCode)
        {
            Console.Error.WriteLine(message);
            Environment.Exit(exitCode);
        }

        private static CommandRunner BuildCommandRunner()
        {
            var commandRunner = new CommandRunner();

            commandRunner.RegisterCommand("exe", (rcArgs) => { return new ExeCommand(container, rcArgs, null); });
            commandRunner.RegisterCommand("mkdir", (rcArgs) => { return new MkdirCommand(container, rcArgs.Arguments); });
            commandRunner.RegisterCommand("iis", (rcArgs) => { return new WebApplicationCommand(container, rcArgs, null); });
            commandRunner.RegisterCommand("ps1", (rcArgs) => { return new PowershellCommand(container, rcArgs, null); });
            commandRunner.RegisterCommand("replace-tokens", (rcArgs) => { return new ReplaceTokensCommand(container, rcArgs.Arguments); });
            commandRunner.RegisterCommand("tar", (rcArgs) => { return new TarCommand(container, rcArgs.Arguments); });
            commandRunner.RegisterCommand("touch", (rcArgs) => { return new TouchCommand(container, rcArgs.Arguments); });
            commandRunner.RegisterCommand("unzip", (rcArgs) => { return new UnzipCommand(container, rcArgs.Arguments); });

            return commandRunner;
        }
    }
}
