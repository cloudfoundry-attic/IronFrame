using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using IronFoundry.Container.Host.Handlers;
using IronFoundry.Container.Host;
using IronFoundry.Container.Messages;
using IronFoundry.Container.Utilities;

namespace IronFoundry.Container.Handlers
{
    public class CreateProcessHandlerTests
    {
        IProcessRunner ProcessRunner { get; set; }
        IProcessTracker ProcessTracker { get; set; }
        CreateProcessHandler Handler { get; set; }
        CreateProcessParams ExpectedParams { get; set; }
        IProcess ExpectedProcess { get; set; }

        public CreateProcessHandlerTests()
        {
            ProcessRunner = Substitute.For<IProcessRunner>();
            ProcessTracker = Substitute.For<IProcessTracker>();

            Handler = new CreateProcessHandler(ProcessRunner, ProcessTracker);

            ExpectedParams = new CreateProcessParams
            {
                executablePath = "cmd.exe",
                arguments = new[] { "/C", "echo Hello" },
                key = Guid.NewGuid(),
                workingDirectory = @"C:\Containers\handle\user",
            };

            ExpectedProcess = Substitute.For<IProcess>();
            ProcessRunner.Run(null).ReturnsForAnyArgs(ExpectedProcess);
        }

        [Fact]
        public async void RunsProcess()
        {
            await Handler.ExecuteAsync(ExpectedParams);

            ProcessRunner.Received(1).Run(
                Arg.Is<ProcessRunSpec>(actual =>
                    actual.ExecutablePath == ExpectedParams.executablePath &&
                    actual.Arguments.SequenceEqual(ExpectedParams.arguments) &&
                    actual.WorkingDirectory == ExpectedParams.workingDirectory
                )
            );
        }

        [Fact]
        public async void WiresUpStandardIo()
        {
            Action<string> outputCallback = null;
            Action<string> errorCallback = null;
            ProcessRunner.Run(null).ReturnsForAnyArgs(ExpectedProcess)
                .AndDoes(call =>
                {
                    var spec = call.Arg<ProcessRunSpec>();
                    outputCallback = spec.OutputCallback;
                    errorCallback = spec.ErrorCallback;
                });

            await Handler.ExecuteAsync(ExpectedParams);

            Assert.NotNull(outputCallback);
            Assert.NotNull(errorCallback);

            outputCallback("This is STDOUT");
            errorCallback("This is STDERR");

            ProcessTracker.Received(1).HandleProcessData(ExpectedParams.key, ProcessDataType.STDOUT, "This is STDOUT");
            ProcessTracker.Received(1).HandleProcessData(ExpectedParams.key, ProcessDataType.STDERR, "This is STDERR");
        }

        [Fact]
        public async void AddsProcessToTracker()
        {
            await Handler.ExecuteAsync(ExpectedParams);

            ProcessTracker.Received(1).TrackProcess(ExpectedParams.key, ExpectedProcess);
        }

        [Fact]
        public async void ReturnsProcessId()
        {
            ExpectedProcess.Id.Returns(100);

            var result = await Handler.ExecuteAsync(ExpectedParams);

            Assert.Equal(100, result.id);
        }
    }
}
