using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Container;
using IronFoundry.Container.Utilities;
using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Containers
{
    internal sealed class ContainerClient : IContainerClient
    {
        private const string RootPrefix = "@ROOT@";
        private readonly IronFoundry.Container.IContainerService containerService;
        private readonly FileSystemManager fileSystem;
        private readonly IContainer container;
        
        private ILogEmitter logEmitter;

        public ContainerClient(IContainerService containerService, IContainer container, FileSystemManager fileSystem)
        {
            this.containerService = containerService;
            this.container = container;
            this.fileSystem = fileSystem;

            this.logEmitter = new NullLogEmitter();
        }

        public string ContainerDirectoryPath
        {
            get { return this.container.Directory.UserPath;  }
        }

        public ContainerHandle Handle
        {
            get { return new ContainerHandle(container.Handle); }
        }

        public int? AssignedPort { get; private set; }

        public Task CopyAsync(string source, string destination)
        {
            Action copyAction = () =>
            {
                string mappedSource = this.container.ConvertToUserPathWithin(source);
                string mappedDestination = this.container.ConvertToUserPathWithin(destination);

                fileSystem.Copy(mappedSource, mappedDestination);
            };

            return Task.Run(copyAction);
        }

        public IEnumerable<string> DrainEvents()
        {
            throw new NotImplementedException();
        }

        public void EnableLogging(ILogEmitter logEmitter)
        {
            this.logEmitter = logEmitter;
        }

        public Task<ContainerInfo> GetInfoAsync()
        {
            return Task.Run(() => this.container.GetInfo());
        }

        public Task InitializeAsync(string baseDirectory, string handle, string userGroup)
        {
            return Task.Delay(0);
        }

        public Task LimitMemoryAsync(ulong bytes)
        {
            return Task.Run(() => this.container.LimitMemory(bytes));
        }

        public Task<int> ReservePortAsync(int port)
        {
            if (AssignedPort.HasValue)
                return Task.FromResult(AssignedPort.Value);

            Func<int> reservePort = () =>
            {
                this.AssignedPort = container.ReservePort(port);
                return this.AssignedPort.Value;
            };

            return Task<int>.Run(reservePort);
        }

        public async Task<CommandResult> RunCommandAsync(RemoteCommandArgs commandArgs)
        {
            var io = new ProcessIO(this.logEmitter);
            RemoteCommand remoteCommand = RemoteCommand.Create(this.container, io, commandArgs.Command, commandArgs);

            TaskCommandResult taskResult = await remoteCommand.InvokeAsync();
            
            var result = new CommandResult
            {
                ExitCode = taskResult.ExitCode,
                StdOut = taskResult.Stdout,
                StdErr = taskResult.Stderr,
            };

            return result;
        }

        public Task StopAsync(bool kill)
        {
            return Task.Run(() => this.container.Stop(kill));
        }

        public Task Destroy()
        {
            return Task.Run(() => containerService.DestroyContainer(this.container.Handle));
        }

        class ProcessIO : IProcessIO
        {
            private StringReader emptyReader = new StringReader(string.Empty);

            public ProcessIO(ILogEmitter emitter)
            {
                this.StandardOutput = new LogWriter(emitter, LogMessageType.STDOUT);
                this.StandardError = new LogWriter(emitter, LogMessageType.STDERR);
            }

            public TextWriter StandardOutput { get; private set; }

            public TextWriter StandardError { get; private set; }

            public TextReader StandardInput
            {
                get { return emptyReader; }
            }
        }

        class NullLogEmitter : ILogEmitter
        {
            public void EmitLogMessage(LogMessageType type, string message)
            {
            }
        }

        class LogWriter : TextWriter
        {
            private ILogEmitter emitter;
            private LogMessageType messageType;

            public LogWriter(ILogEmitter emitter, LogMessageType messageType)
            {
                this.emitter = emitter;
                this.messageType = messageType;
            }

            public override void Write(string value)
            {
                this.emitter.EmitLogMessage(this.messageType, value);
            }

            public override void WriteLine(string value)
            {
                this.emitter.EmitLogMessage(this.messageType, value + Environment.NewLine);
            }

            public override Encoding Encoding
            {
                get { throw new NotImplementedException(); }
            }
        }
    }
}
