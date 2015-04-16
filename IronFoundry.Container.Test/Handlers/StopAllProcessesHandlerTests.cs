using IronFoundry.Container.Host;
using IronFoundry.Container.Host.Handlers;
using IronFoundry.Container.Messages;
using IronFoundry.Container.Utilities;
using NSubstitute;
using Xunit;

namespace IronFoundry.Container.Handlers
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
        public async void RequestsProcessesToExit()
        {
            await Handler.ExecuteAsync(new StopAllProcessesParams { timeout = 1 });

            Processes[0].Received(1).RequestExit();
            Processes[1].Received(1).RequestExit();
        }

        [Fact]
        public async void WaitsForProcessesToExit()
        {
            await Handler.ExecuteAsync(new StopAllProcessesParams { timeout = 1 });

            Processes[0].Received(1).WaitForExit(1);
            Processes[1].Received(1).WaitForExit(1);
        }

        [Fact]
        public async void WhenProcessDoesNotExitWithinTimeout_KillsProcess()
        {
            Processes[0].WaitForExit(1).Returns(false);
            Processes[1].WaitForExit(1).Returns(true);

            await Handler.ExecuteAsync(new StopAllProcessesParams { timeout = 1 });

            Processes[0].Received(1).Kill();
            Processes[1].DidNotReceive().Kill();
        }
    }
}
