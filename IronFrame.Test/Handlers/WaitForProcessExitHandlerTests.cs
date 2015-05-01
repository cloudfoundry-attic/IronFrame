using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFrame.Host;
using IronFrame.Host.Handlers;
using IronFrame.Messages;
using IronFrame.Utilities;
using NSubstitute;
using Xunit;

namespace IronFrame.Handlers
{
    public class WaitForProcessExitHandlerTests
    {
        IProcessTracker ProcessTracker { get; set; }
        WaitForProcessExitHandler Handler { get; set; }
        WaitForProcessExitParams ExpectedParams { get; set; }
        IProcess ExpectedProcess { get; set; }

        public WaitForProcessExitHandlerTests()
        {
            ProcessTracker = Substitute.For<IProcessTracker>();

            Handler = new WaitForProcessExitHandler(ProcessTracker);

            ExpectedParams = new WaitForProcessExitParams
            {
                key = Guid.NewGuid(),
                timeout = 100,
            };

            ExpectedProcess = Substitute.For<IProcess>();

            ProcessTracker.GetProcessByKey(ExpectedParams.key).Returns(ExpectedProcess);
        }

        [Fact]
        public async void WhenProcessIsNotTracked_Throws()
        {
            ExpectedParams.key = Guid.NewGuid();
            ProcessTracker.GetProcessByKey(ExpectedParams.key).Returns((IProcess)null);

            var ex = await Record.ExceptionAsync(async () => await Handler.ExecuteAsync(ExpectedParams));

            Assert.Equal(
                String.Format("A process with key '{0}' is not being tracked.", ExpectedParams.key), 
                ex.Message);
        }

        [Fact]
        public async void WaitsForProcessToExit()
        {
            await Handler.ExecuteAsync(ExpectedParams);

            ExpectedProcess.Received(1).WaitForExit(ExpectedParams.timeout);
        }

        [Fact]
        public async void WhenProcessDoesNotExitWithinTimeoutPeriod_ReturnsCorrectResult()
        {
            ExpectedProcess.WaitForExit(ExpectedParams.timeout).Returns(false);

            var result = await Handler.ExecuteAsync(ExpectedParams);

            Assert.False(result.exited);
            Assert.Equal(0, result.exitCode);
        }

        [Fact]
        public async void WhenProcessDoesNotExitWithinTimeoutPeriod_DoesNotRemoveFromTracker()
        {
            ExpectedProcess.WaitForExit(ExpectedParams.timeout).Returns(false);

            await Handler.ExecuteAsync(ExpectedParams);

            ProcessTracker.DidNotReceive().RemoveProcess(Arg.Any<Guid>());
        }

        [Fact]
        public async void WhenProcessExits_ReturnsExitCode()
        {
            ExpectedProcess.WaitForExit(ExpectedParams.timeout).Returns(true);
            ExpectedProcess.ExitCode.Returns(100);

            var result = await Handler.ExecuteAsync(ExpectedParams);

            Assert.True(result.exited);
            Assert.Equal(100, result.exitCode);
        }

        [Fact]
        public async void WhenProcessExits_RemovesProcessFromTracker()
        {
            ExpectedProcess.WaitForExit(ExpectedParams.timeout).Returns(true);
            ExpectedProcess.ExitCode.Returns(100);

            await Handler.ExecuteAsync(ExpectedParams);
            ProcessTracker.Received().RemoveProcess(ExpectedParams.key);
        }
    }
}
