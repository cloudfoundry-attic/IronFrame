using IronFoundry.Container.Utilities;
using NSubstitute;
using Xunit;

namespace IronFoundry.Container
{
    public class ContainerProcessTests
    {
        IProcess Process { get; set; }
        ContainerProcess ContainerProcess { get; set; }

        public ContainerProcessTests()
        {
            Process = Substitute.For<IProcess>();

            ContainerProcess = new ContainerProcess(Process);
        }

        public class Id : ContainerProcessTests
        {
            [Fact]
            public void ReturnsProcessId()
            {
                Process.Id.Returns(100);

                Assert.Equal(100, ContainerProcess.Id);
            }
        }

        public class Kill : ContainerProcessTests
        {
            [Fact]
            public void KillsProcess()
            {
                ContainerProcess.Kill();

                Process.Received(1).Kill();
            }
        }

        public class WaitForExit : ContainerProcessTests
        {
            [Fact]
            public void WaitsForProcessToExitAndReturnsExitCode()
            {
                Process.ExitCode.Returns(1);

                var exitCode = ContainerProcess.WaitForExit();

                Assert.Equal(1, exitCode);
                Process.Received(1).WaitForExit();
            } 
        }

        public class WaitForExitWithTimeout : ContainerProcessTests
        {
            [Fact]
            public void WaitsForProcessAndReturnsExitCode()
            {
                Process.ExitCode.Returns(1);
                Process.WaitForExit(100).Returns(true);

                int exitCode = 0;
                bool exited = ContainerProcess.TryWaitForExit(100, out exitCode);

                Assert.Equal(1, exitCode);
                Assert.True(exited);
                Process.Received(1).WaitForExit(100);
            }

            [Fact]
            public void ReturnsFalseIfNotExited()
            {
                Process.WaitForExit(100).Returns(false);

                int exitCode = 0;
                bool exited = ContainerProcess.TryWaitForExit(100, out exitCode);

                Assert.False(exited);
                Process.Received(1).WaitForExit(100);
            }
        }
    }
}
