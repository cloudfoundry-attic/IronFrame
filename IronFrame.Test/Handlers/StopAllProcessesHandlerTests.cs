using IronFrame.Host;
using IronFrame.Host.Handlers;
using IronFrame.Messages;
using IronFrame.Utilities;
using NSubstitute;
using Xunit;

namespace IronFrame.Handlers
{
    public class StopAllProcessesHandlerTests
    {
        IProcess[] Processes { get; set; }
        IProcessTracker ProcessTracker { get; set; }
        StopAllProcessesHandler Handler { get; set; }

        public StopAllProcessesHandlerTests()
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

            ProcessTracker = Substitute.For<IProcessTracker>();
            ProcessTracker.GetAllChildProcesses().Returns(Processes);

            Handler = new StopAllProcessesHandler(ProcessTracker);
        }

        [Fact]
        public async void ItKillsProcess()
        {
            await Handler.ExecuteAsync(new StopAllProcessesParams());

            Processes[0].Received(1).Kill();
            Processes[1].Received(1).Kill();
        }
    }
}
