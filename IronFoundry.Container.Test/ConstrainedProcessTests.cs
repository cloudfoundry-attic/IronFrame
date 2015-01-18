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
        TestableHostClient HostClient { get; set; }
        ConstrainedProcess Process { get; set; }
        Action<string> OutputCallback { get; set; }
        Action<string> ErrorCallback { get; set; }

        public ConstrainedProcessTests()
        {
            ProcessKey = Guid.NewGuid();

            HostClient = Substitute.For<TestableHostClient>();

            OutputCallback = delegate { };
            ErrorCallback = delegate { };

            Process = new ConstrainedProcess(HostClient, ProcessKey, 100, (data) => OutputCallback(data), (data) => ErrorCallback(data));
        }

        public class Constructor : ConstrainedProcessTests
        {
            [Fact]
            public void SubscribesToProcessDataEvents()
            {
                Assert.Equal(ProcessKey, HostClient.ActualProcessKey);
                Assert.NotNull(HostClient.ActualProcessDataCallback);
            }
        }

        public class HandleProcessData : ConstrainedProcessTests
        {
            [Fact]
            public void BubblesUpOutputData()
            {
                string actualOutputData = null;
                OutputCallback = (data) => actualOutputData = data;

                HostClient.ActualProcessDataCallback(new ProcessDataEvent(ProcessKey, ProcessDataType.STDOUT, "This is STDOUT"));

                Assert.Equal("This is STDOUT", actualOutputData);
            }

            [Fact]
            public void BubblesUpErrorData()
            {
                string actualErrorData = null;
                ErrorCallback = (data) => actualErrorData = data;

                HostClient.ActualProcessDataCallback(new ProcessDataEvent(ProcessKey, ProcessDataType.STDERR, "This is STDERR"));

                Assert.Equal("This is STDERR", actualErrorData);
            }
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

        public class TestableHostClient : IContainerHostClient
        {
            public Guid ActualProcessKey { get; set; }
            public Action<ProcessDataEvent> ActualProcessDataCallback { get; set; }

            public virtual CreateProcessResult CreateProcess(CreateProcessParams @params)
            {
                throw new NotImplementedException();
            }

            public bool Ping(TimeSpan timeout)
            {
                throw new NotImplementedException();
            }

            public void Shutdown()
            {
                throw new NotImplementedException();
            }

            public void SubscribeToProcessData(Guid processKey, Action<ProcessDataEvent> callback)
            {
                ActualProcessKey = processKey;
                ActualProcessDataCallback = callback;
            }

            public virtual WaitForProcessExitResult WaitForProcessExit(WaitForProcessExitParams @params)
            {
                throw new NotImplementedException();
            }
        }
    }
}
