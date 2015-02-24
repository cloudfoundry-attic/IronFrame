using System;
using System.Collections.Generic;
using IronFoundry.Container.Internal;
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
            Client.CreateProcess(null).ReturnsForAnyArgs(new CreateProcessResult());
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

            [Fact]
            public void ReturnsProcessWithEnvironment()
            {
                int expectedId = 123;
                Client.CreateProcess(Arg.Any<CreateProcessParams>()).Returns(
                new CreateProcessResult()
                {
                    id = expectedId
                }
                );

                var spec = new ProcessRunSpec
                {
                    Environment = new Dictionary<string, string> {{"FOO", "BAR"}}
                };

                var process = Runner.Run(spec);

                Assert.NotNull(process.Environment);
                Assert.True(process.Environment.Count > 0);
                Assert.Equal("BAR", process.Environment["FOO"]);
            }
        }

        public class StopAll : ConstrainedProcessRunnerTests
        {
            ConstrainedProcessRunner Runner { get; set; }

            public StopAll()
            {
                Runner = new ConstrainedProcessRunner(Client);
            }

            [Fact]
            public void WhenKillIsFalse_SendsStopAllProcessesMessageWithTimeout()
            {
                Runner.StopAll(kill: false);

                Client.Received(1).StopAllProcesses(10000);
            }

            [Fact]
            public void WhenKillIsTrue_SendsStopAllProcessesMessageWithNoTimeout()
            {
                Runner.StopAll(kill: true);

                Client.Received(1).StopAllProcesses(0);
            }
        }

        public class Dispose : ConstrainedProcessRunnerTests
        {
            [Fact]
            public void DisposesHostClient()
            {
                var runner = new ConstrainedProcessRunner(Client);
                
                runner.Dispose();

                Client.Received(1).Dispose();
            }
        }
    }
}
