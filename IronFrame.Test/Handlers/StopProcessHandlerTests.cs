using System;
using IronFrame.Host;
using IronFrame.Host.Handlers;
using IronFrame.Messages;
using IronFrame.Utilities;
using NSubstitute;
using Xunit;

namespace IronFrame.Handlers
{
    public class StopProcessHandlerTests
    {
        IProcess[] Processes { get; set; }
        IProcessTracker ProcessTracker { get; set; }
        StopProcessHandler Handler { get; set; }
        Guid Process1Key { get; set; }
        Guid Process2Key { get; set; }

        public StopProcessHandlerTests()
        {
            var process1 = Substitute.For<IProcess>();
            process1.Id.Returns(1);

            var process2 = Substitute.For<IProcess>();
            process2.Id.Returns(2);

            Processes = new[]
            {
                process1,
                process2,
            };

            Process1Key = Guid.NewGuid();
            Process2Key = Guid.NewGuid();

            ProcessTracker = Substitute.For<IProcessTracker>();
            ProcessTracker.GetProcessByKey(Process1Key).Returns(process1);
            ProcessTracker.GetProcessByKey(Process2Key).Returns(process2);

            Handler = new StopProcessHandler(ProcessTracker);
        }

        [Fact]
        public async void RequestsProcessToExit()
        {
            await Handler.ExecuteAsync(new StopProcessParams() { key =  Process1Key, timeout = 1 });

            Processes[0].Received(1).RequestExit();
            Processes[1].Received(0).RequestExit();
        }

        [Fact]
        public async void WaitsForProcessToExit()
        {
            await Handler.ExecuteAsync(new StopProcessParams { key = Process2Key, timeout = 1 });

            Processes[0].Received(0).WaitForExit(1);
            Processes[1].Received(1).WaitForExit(1);
        }

        [Fact]
        public async void WhenProcessDoesNotExitWithinTimeout_KillsProcess()
        {
            Processes[0].WaitForExit(1).Returns(false);

            await Handler.ExecuteAsync(new StopProcessParams { key = Process1Key, timeout = 1 });

            Processes[0].Received(1).Kill();
        }
    }
}
