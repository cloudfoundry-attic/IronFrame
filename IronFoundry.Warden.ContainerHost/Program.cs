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
using IronFoundry.Warden.Shared.Messaging;
using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Utilities;

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

            container = new ContainerStub(jobObject, jobObjectLimits, BuildCommandRunner(), new ProcessHelper(), new ProcessMonitor(), new LocalTcpPortManager(), new FileSystemManager());

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
                    var containerUser = ContainerUser.CreateUser(containerHandle, new LocalPrincipalManager(new DesktopPermissionManager()));

                    var containerDirectory = new ContainerDirectory(containerHandle, containerUser, r.@params.containerBaseDirectoryPath, true);

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

                dispatcher.RegisterMethod<RunCommandRequest>(RunCommandRequest.MethodName, async (r) =>
                {
                    var remoteCommand = new RemoteCommand(r.@params.impersonate, r.@params.command, r.@params.arguments);
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

                dispatcher.RegisterMethod<EnableLoggingRequest>(EnableLoggingRequest.MethodName, (r) =>
                {
                    var containerEmitter = new ContainerLogEmitter(r.@params);
                    container.AttachEmitter(containerEmitter);

                    return Task.FromResult<object>(new EnableLoggingResponse(r.id));
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

                dispatcher.RegisterMethod<StopRequest>(StopRequest.MethodName, r =>
                {
                    container.Stop(r.@params.Kill);
                    return Task.FromResult<object>(new StopResponse(r.id));
                });

                transport.SubscribeRequest(
                    async (request) =>
                    {
                        var response = await dispatcher.DispatchAsync(request);
                        await transport.PublishAsync(response);
                    });

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

            commandRunner.RegisterCommand("exe", (shouldImpersonate, arguments) => { return new ExeCommand(container, arguments, shouldImpersonate, null); });
            commandRunner.RegisterCommand("mkdir", (shouldImpersonate, arguments) => { return new MkdirCommand(container, arguments); });
            commandRunner.RegisterCommand("iis", (shouldImpersonate, arguments) => { return new WebApplicationCommand(container, arguments, shouldImpersonate, null); });
            commandRunner.RegisterCommand("ps1", (shouldImpersonate, arguments) => { return new PowershellCommand(container, arguments, shouldImpersonate, null); });
            commandRunner.RegisterCommand("replace-tokens", (shouldImpersonate, arguments) => { return new ReplaceTokensCommand(container, arguments); });
            commandRunner.RegisterCommand("tar", (shouldImpersonate, arguments) => { return new TarCommand(container, arguments); });
            commandRunner.RegisterCommand("touch", (shouldImpersonate, arguments) => { return new TouchCommand(container, arguments); });
            commandRunner.RegisterCommand("unzip", (shouldImpersonate, arguments) => { return new UnzipCommand(container, arguments); });
            return commandRunner;
        }
    }
}
