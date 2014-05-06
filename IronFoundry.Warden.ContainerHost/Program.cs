using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IronFoundry.Warden.Shared.Messaging;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Utilities;
using IronFoundry.Warden.Containers.Messages;

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
            var input = Console.In;
            var output = Console.Out;

            if (args.Length < 1)
                throw new InvalidOperationException("Cannot start host, missing JobObject name.");

            var jobObject = new JobObject(args[0]);
            var jobObjectLimits = new JobObjectLimits(jobObject);
            var hostProcess = System.Diagnostics.Process.GetCurrentProcess();
            jobObject.AssignProcessToJob(hostProcess);

            container = new ContainerStub(jobObject, jobObjectLimits, BuildCommandRunner(), new ProcessHelper(), new ProcessMonitor());

            container.OutOfMemory += HandleOutOfMemory;

            using (var transport = new MessageTransport(input, output))
            {
                var dispatcher = new MessageDispatcher();

                dispatcher.RegisterMethod<ContainerInitializeRequest>(ContainerInitializeRequest.MethodName, (r) => 
                {
                    var containerUser = new ContainerUser(r.@params.userName, r.@params.userPassword);

                    container.Initialize(
                        r.@params.containerDirectoryPath,
                        r.@params.containerHandle,
                        containerUser);

                    return Task.FromResult<object>(new ContainerInitializeResponse(r.id));
                });

                dispatcher.RegisterMethod<ContainerStateRequest>(ContainerStateRequest.MethodName, (r) => 
                {
                    return Task.FromResult<object>(
                        new ContainerStateResponse(r.id, container.State.ToString()));
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

                dispatcher.RegisterMethod<ContainerStatisticsRequest>(ContainerStatisticsRequest.MethodName, (r) =>
                {
                    return Task.FromResult<object>(new ContainerStatisticsResponse(r.id, container.GetProcessStatistics()));
                });

                dispatcher.RegisterMethod<ContainerDestroyRequest>(ContainerDestroyRequest.MethodName, (r) =>
                {
                    container.Destroy();
                    container = null;

                    return Task.FromResult<object>(new ContainerDestroyResponse(r.id));
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

                transport.SubscribeRequest(
                    async (request) =>
                    {
                        var response = await dispatcher.DispatchAsync(request);
                        await transport.PublishAsync(response);
                    });

                exitEvent.WaitOne();
            }
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
