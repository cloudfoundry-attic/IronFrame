using System;
using System.Threading;
using IronFoundry.Container.Messages;
using NSubstitute;
using Xunit;

namespace IronFoundry.Container
{
    public class ConstrainedProcessTests
    {
        Guid ProcessKey { get; set; }
        IContainerHostClient HostClient { get; set; }
        ConstrainedProcess Process { get; set; }
        Action<string> OutputCallback { get; set; }
        Action<string> ErrorCallback { get; set; }

        public ConstrainedProcessTests()
        {
            ProcessKey = Guid.NewGuid();

            HostClient = Substitute.For<IContainerHostClient>();

            OutputCallback = delegate { };
            ErrorCallback = delegate { };

            Process = new ConstrainedProcess(HostClient, ProcessKey, 100);
        }

        public class WaitForExit : ConstrainedProcessTests
        {
            [Fact]
            public void ForwardsToHostClient()
            {
                Process.WaitForExit(100);

                HostClient.Received(1).WaitForProcessExit(
                    Arg.Is<WaitForProcessExitParams>(actual =>
                        actual.key == ProcessKey && actual.timeout == 100
                    )
                );
            }

            [Fact]
            public void DefaultTimeoutIsInfinite()
            {
                Process.WaitForExit();

                HostClient.Received(1).WaitForProcessExit(
                    Arg.Is<WaitForProcessExitParams>(actual =>
                        actual.key == ProcessKey && actual.timeout == Timeout.Infinite
                    )
                );
            }

            [Fact]
            public void WhenTimeoutOccurs_ReturnsFalse()
            {
                HostClient.WaitForProcessExit(null)
                    .ReturnsForAnyArgs(new WaitForProcessExitResult { exited = false });

                var success = Process.WaitForExit(0);

                Assert.False(success);
            }

            [Fact]
            public void WhenProcessExits_ReturnsTrue()
            {
                HostClient.WaitForProcessExit(null)
                    .ReturnsForAnyArgs(new WaitForProcessExitResult { exited = true });

                var success = Process.WaitForExit(1);

                Assert.True(success);
            }

            [Fact]
            public void WhenProcessExits_SetsExitCode()
            {
                HostClient.WaitForProcessExit(null)
                    .ReturnsForAnyArgs(new WaitForProcessExitResult { exited = true, exitCode = 100 });

                Process.WaitForExit();

                Assert.Equal(100, Process.ExitCode);
            }
        }
    }
}
