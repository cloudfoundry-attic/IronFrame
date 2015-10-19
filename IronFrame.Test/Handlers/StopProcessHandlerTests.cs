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
        Guid Process3Key { get; set; }

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
            Process3Key = Guid.NewGuid();

            ProcessTracker = Substitute.For<IProcessTracker>();
            ProcessTracker.GetProcessByKey(Process1Key).Returns(process1);
            ProcessTracker.GetProcessByKey(Process2Key).Returns(process2);
            ProcessTracker.GetProcessByKey(Process3Key).Returns(x => null);

            Handler = new StopProcessHandler(ProcessTracker);
        }

        [Fact]
        public async void ItKillsTheProcess()
        {
            await Handler.ExecuteAsync(new StopProcessParams { key = Process1Key });

            Processes[0].Received(1).Kill();
        }

        [Fact]
        public async void WhenProcessDoesntExist()
        {
            try
            {
                await Handler.ExecuteAsync(new StopProcessParams { key = Process3Key, timeout = 1 });
            }
            catch (NullReferenceException e)
            {
                Assert.True(false, "NullReferenceException should not be thrown when process is null");
            }
        }
    }
}
