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
            ConstrainedProcessRunner Runner { get; set; }

            public Run()
            {
                Runner = new ConstrainedProcessRunner(Client);
            }

            [Fact]
            public void SendsCreateProcessMessage()
            {
                var spec = new ProcessRunSpec
                {
                    ExecutablePath = "exe",
                    Arguments = new[] { "arg1", "arg2" },
                    WorkingDirectory = @"\WorkdirDir",
                    Environment = new Dictionary<string, string> { { "env1", "val1" } },
                };

                Runner.Run(spec);

                Client.Received(1).CreateProcess(
                    Arg.Is<CreateProcessParams>(actual =>
                        actual.executablePath == spec.ExecutablePath &&
                        actual.arguments == spec.Arguments &&
                        actual.environment.ContainsKey("env1") &&
                        actual.workingDirectory == spec.WorkingDirectory &&
                        actual.key != Guid.Empty
                    )
                );
            }

            [Fact]
            public void SetsDefaultEnvironmentBlock()
            {
                var spec = new ProcessRunSpec
                {
                    ExecutablePath = "exe",
                    Arguments = new[] { "arg1", "arg2" },
                    WorkingDirectory = @"\WorkdirDir",
                    Environment = new Dictionary<string, string>(),
                };

                Runner.Run(spec);

                Client.Received(1).CreateProcess(
                    Arg.Is<CreateProcessParams>(actual =>
                        actual.environment.ContainsKey("TEMP") &&
                        actual.environment.ContainsKey("PATH")
                    )
                );
            }

            [Fact]
            public void MergesEnvironmentWithDefaultEnvironmentBlock()
            {
                var spec = new ProcessRunSpec
                {
                    ExecutablePath = "exe",
                    Arguments = new[] { "arg1", "arg2" },
                    WorkingDirectory = @"\WorkdirDir",
                    Environment = new Dictionary<string, string> { { "env1", "val1" } },
                };

                Runner.Run(spec);

                Client.Received(1).CreateProcess(
                    Arg.Is<CreateProcessParams>(actual =>
                        actual.environment.ContainsKey("TEMP") &&
                        actual.environment.ContainsKey("PATH") &&
                        actual.environment["env1"] == "val1"
                    )
                );
            }

            [Fact]
            public void ReturnsProcessWithId()
            {
                int expectedId = 123;
                Client.CreateProcess(Arg.Any<CreateProcessParams>()).Returns(
                new CreateProcessResult()
                {
                    id = expectedId
                }
                );

                var process = Runner.Run(new ProcessRunSpec());

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
