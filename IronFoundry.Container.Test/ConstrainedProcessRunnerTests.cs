using System;
using System.Collections.Generic;
using IronFoundry.Container.Messages;
using NSubstitute;
using Xunit;

namespace IronFoundry.Container
{
    public class ConstrainedProcessRunnerTests
    {
        IContainerHostClient Client { get; set; }

        public ConstrainedProcessRunnerTests()
        {
            Client = Substitute.For<IContainerHostClient>();
        }

        public class Run : ConstrainedProcessRunnerTests
        {
            [Fact]
            public void SendsCreateProcessMessage()
            {
                var runner = new ConstrainedProcessRunner(Client);

                var spec = new ProcessRunSpec
                {
                    ExecutablePath = "exe",
                    Arguments = new[] { "arg1", "arg2" },
                    WorkingDirectory = @"\WorkdirDir",
                    Environment = new Dictionary<string, string> { { "env1", "val1" } },
                };

                runner.Run(spec);

                Client.Received(1).CreateProcess(MatchCreateProcessData(spec));
            }

            [Fact]
            public void ReturnsProcessWithId()
            {
                var runner = new ConstrainedProcessRunner(Client);

                int expectedId = 123;
                Client.CreateProcess(Arg.Any<CreateProcessParams>()).Returns(
                new CreateProcessResult()
                {
                    id = expectedId
                }
                );

                var process = runner.Run(new ProcessRunSpec());

                Assert.Equal(expectedId, process.Id);
            }

            CreateProcessParams MatchCreateProcessData(ProcessRunSpec expected)
            {
                return Arg.Is<CreateProcessParams>(actual =>
                    actual.executablePath == expected.ExecutablePath &&
                    actual.arguments == expected.Arguments &&
                    actual.environment == expected.Environment &&
                    actual.workingDirectory == expected.WorkingDirectory &&
                    actual.key != Guid.Empty
                );
            }
        }

        public class Dispose : ConstrainedProcessRunnerTests
        {
            [Fact]
            public void DisposesHost()
            {
                var runner = new ConstrainedProcessRunner(Client);
                
                runner.Dispose();

                Client.Received(1).Dispose();
            }
        }
    }
}
