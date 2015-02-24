using System;
using System.Collections.Generic;
using IronFoundry.Container.Utilities;
using NSubstitute;
using Xunit;
using IronFoundry.Container.Internal;

namespace IronFoundry.Container
{
    public class ContainerTests
    {
        Internal.Container Container { get; set; }
        IProcessRunner ConstrainedProcessRunner { get; set; }
        Dictionary<string, string> ContainerEnvironment { get; set; }
        IContainerDirectory Directory { get; set; }
        JobObject JobObject { get; set; }
        ProcessHelper ProcessHelper { get; set; }
        IProcessRunner ProcessRunner { get; set; }
        IContainerPropertyService ContainerPropertiesService { get; set; }
        ILocalTcpPortManager TcpPortManager { get; set; }
        IContainerUser User { get; set; }

        public ContainerTests()
        {            
            ConstrainedProcessRunner = Substitute.For<IProcessRunner>();
            ContainerEnvironment = new Dictionary<string, string>() { { "Handle", "handle" } };

            Directory = Substitute.For<IContainerDirectory>();

            JobObject = Substitute.For<JobObject>();
            JobObject.GetCpuStatistics().Returns(new CpuStatistics
            {
                TotalKernelTime = TimeSpan.Zero,
                TotalUserTime = TimeSpan.Zero,
            });
            JobObject.GetProcessIds().Returns(new int[0]);

            ProcessHelper = Substitute.For<ProcessHelper>();
            ProcessRunner = Substitute.For<IProcessRunner>();
            ContainerPropertiesService = Substitute.For<IContainerPropertyService>();

            TcpPortManager = Substitute.For<ILocalTcpPortManager>();

            User = Substitute.For<IContainerUser>();
            User.UserName.Returns("container-username");

            Container = new Internal.Container(
                "id", 
                "handle", 
                User, 
                Directory, 
                ContainerPropertiesService, 
                TcpPortManager, 
                JobObject, 
                ProcessRunner, 
                ConstrainedProcessRunner, 
                ProcessHelper, 
                ContainerEnvironment);
        }

        public class GetProperty : ContainerTests
        {
            public GetProperty()
            {
                ContainerPropertiesService.GetProperty(Container, "Name").Returns("Value");
            }

            [Fact]
            public void ReturnsPropertyValue()
            {
                var value = Container.GetProperty("Name");

                Assert.Equal("Value", value);
                ContainerPropertiesService.Received(1).GetProperty(Container, "Name");
            }

            [Fact]
            public void WhenPropertyDoesNotExist_ReturnsNull()
            {
                ContainerPropertiesService.GetProperty(Container, "Unknown").Returns((string)null);

                var value = Container.GetProperty("Unknown");

                Assert.Null(value);
                ContainerPropertiesService.Received(1).GetProperty(Container, "Unknown");
            }
        }

        public class ReservePort : ContainerTests
        {
            [Fact]
            public void ReservesPortForContainerUser()
            {
                Container.ReservePort(3000);

                TcpPortManager.Received(1).ReserveLocalPort(3000, "container-username");
            }

            [Fact]
            public void ReturnsReservedPort()
            {
                TcpPortManager.ReserveLocalPort(3000, "container-username").Returns(5000);

                var port = Container.ReservePort(3000);

                Assert.Equal(5000, port);
            }

            [Fact]
            public void WhenContainerNotActive_Throws()
            {
                Container.Stop(false);
                Action action = () => Container.ReservePort(3000);
                Assert.Throws<InvalidOperationException>(action);
            }
        }

        public class Run : ContainerTests
        {
            ProcessSpec Spec  { get; set; }
            ProcessRunSpec ExpectedRunSpec { get; set; }

            public Run ()
            {
                Spec = new ProcessSpec
                {
                    ExecutablePath = "/.iishost/iishost.exe",
                    Arguments = new[] { "-p", "3000", "-r", @"/www" },
                };

                var containerUserPath = @"C:\Containers\handle\user\";
                ExpectedRunSpec = new ProcessRunSpec
                {
                    ExecutablePath = @"C:\Containers\handle\user\.iishost\iishost.exe",
                    Arguments = Spec.Arguments,
                    WorkingDirectory = containerUserPath,
                };

                Directory.MapUserPath("/.iishost/iishost.exe").Returns(ExpectedRunSpec.ExecutablePath);
                Directory.MapUserPath("/").Returns(containerUserPath);
            }

            public class WhenPrivileged : Run
            {
                public WhenPrivileged()
                {
                    Spec.Privileged = true;
                }

                [Fact]
                public void RunsTheProcessLocally()
                {
                    var io = Substitute.For<IProcessIO>();

                    var process = Container.Run(Spec, io);

                    Assert.NotNull(process);
                    var actual = ProcessRunner.Captured(x => x.Run(null)).Arg<ProcessRunSpec>();
                    Assert.Equal(ExpectedRunSpec.ExecutablePath, actual.ExecutablePath);
                    Assert.Equal(ExpectedRunSpec.Arguments, actual.Arguments);
                    Assert.Superset(
                        new HashSet<string>(ExpectedRunSpec.Environment.Keys),
                        new HashSet<string>(actual.Environment.Keys));
                    Assert.Equal(ExpectedRunSpec.WorkingDirectory, actual.WorkingDirectory);
                }

                [Fact]
                public void ProcessIoIsRedirected()
                {
                    var io = new TestProcessIO();
                    var localProcess = Substitute.For<IProcess>();
                    ProcessRunner.Run(Arg.Any<ProcessRunSpec>()).Returns(localProcess)
                        .AndDoes(call =>
                        {
                            var runSpec = call.Arg<ProcessRunSpec>();
                            runSpec.OutputCallback("This is STDOUT");
                            runSpec.ErrorCallback("This is STDERR");
                        });

                    Container.Run(Spec, io);

                    Assert.Equal("This is STDOUT", io.Output.ToString());
                    Assert.Equal("This is STDERR", io.Error.ToString());
                }

                [Fact]
                public void WhenPathMappingIsDisabled_DoesNotMapExecutablePath()
                {
                    var io = Substitute.For<IProcessIO>();
                    Spec.DisablePathMapping = true;
                    Spec.ExecutablePath = "cmd.exe";

                    var process = Container.Run(Spec, io);

                    Assert.NotNull(process);
                    var actual = ProcessRunner.Captured(x => x.Run(null)).Arg<ProcessRunSpec>();
                    Assert.Equal("cmd.exe", actual.ExecutablePath);
                }

                [Fact]
                public void WhenProcessSpecHasNoEnvironment()
                {
                    var io = Substitute.For<IProcessIO>();
                    var process = Container.Run(Spec, io);

                    var actualSpec = ProcessRunner.Captured(x => x.Run(null)).Arg<ProcessRunSpec>();

                    Assert.Equal(ContainerEnvironment, actualSpec.Environment);
                }

                [Fact]
                public void WhenProcessEnvironmentConflictsWithContainerEnvironment()
                {
                    Spec.Environment = new Dictionary<string, string>
                    {
                        { "Handle", "procHandle" },
                        { "ProcEnv", "ProcEnv" }
                    };

                    var io = Substitute.For<IProcessIO>();
                    var process = Container.Run(Spec, io);

                    var actualSpec = ProcessRunner.Captured(x => x.Run(null)).Arg<ProcessRunSpec>();

                    Assert.Equal(Spec.Environment, actualSpec.Environment);
                }
            }

            public class WhenNotPrivileged : Run
            {
                public WhenNotPrivileged()
                {
                    Spec.Privileged = false;
                }

                [Fact]
                public void RunsTheProcessRemotely()
                {
                    var io = Substitute.For<IProcessIO>();

                    var process = Container.Run(Spec, io);

                    Assert.NotNull(process);
                    var actual = ConstrainedProcessRunner.Captured(x => x.Run(null)).Arg<ProcessRunSpec>();
                    Assert.Equal(ExpectedRunSpec.ExecutablePath, actual.ExecutablePath);
                    Assert.Equal(ExpectedRunSpec.Arguments, actual.Arguments);
                    Assert.Superset(
                        new HashSet<string>(ExpectedRunSpec.Environment.Keys),
                        new HashSet<string>(actual.Environment.Keys));
                    Assert.Equal(ExpectedRunSpec.WorkingDirectory, actual.WorkingDirectory);
                }

                [Fact]
                public void ProcessIoIsRedirected()
                {
                    var io = new TestProcessIO();
                    var remoteProcess = Substitute.For<IProcess>();
                    ConstrainedProcessRunner.Run(Arg.Any<ProcessRunSpec>()).Returns(remoteProcess)
                        .AndDoes(call =>
                        {
                            var runSpec = call.Arg<ProcessRunSpec>();
                            runSpec.OutputCallback("This is STDOUT");
                            runSpec.ErrorCallback("This is STDERR");
                        });

                    Container.Run(Spec, io);

                    Assert.Equal("This is STDOUT", io.Output.ToString());
                    Assert.Equal("This is STDERR", io.Error.ToString());
                }

                [Fact]
                public void ProcessIoCanBeNull()
                {
                    var io = new TestProcessIO();
                    io.Output = null;
                    io.Error = null;

                    Container.Run(Spec, io);

                    var proc = ConstrainedProcessRunner.Captured(x => x.Run(null)).Arg<ProcessRunSpec>();

                    Assert.Equal(null, proc.OutputCallback);
                    Assert.Equal(null, proc.ErrorCallback);
                }


                [Fact]
                public void WhenPathMappingIsDisabled_DoesNotMapExecutablePath()
                {
                    var io = Substitute.For<IProcessIO>();
                    Spec.DisablePathMapping = true;
                    Spec.ExecutablePath = "cmd.exe";

                    var process = Container.Run(Spec, io);

                    Assert.NotNull(process);
                    var actual = ConstrainedProcessRunner.Captured(x => x.Run(null)).Arg<ProcessRunSpec>();
                    Assert.Equal("cmd.exe", actual.ExecutablePath);
                }
            }

            [Fact]
            public void WhenContainerNotActive_Throws()
            {
                var io = Substitute.For<IProcessIO>();
                Container.Stop(false);
                Action action = () => Container.Run(Spec, io);
                Assert.Throws<InvalidOperationException>(action);
            }
        }

        public class Destroy : ContainerTests
        {
            [Fact]
            public void KillsProcesses()
            {
                Container.Destroy();

                ProcessRunner.Received(1).StopAll(true);
                ConstrainedProcessRunner.Received(1).StopAll(true);
            }

            [Fact]
            public void ReleasesPorts()
            {
                TcpPortManager.ReserveLocalPort(Arg.Any<int>(), Arg.Any<string>())
                    .Returns(c => c.Arg<int>());

                Container.ReservePort(100);
                Container.ReservePort(101);

                Container.Destroy();

                TcpPortManager.Received(1).ReleaseLocalPort(100, User.UserName);
                TcpPortManager.Received(1).ReleaseLocalPort(101, User.UserName);
            }

            [Fact]
            public void DeletesUser()
            {
                Container.Destroy();

                User.Received(1).Delete();
            }

            [Fact]
            public void DisposesRunners()
            {
                Container.Destroy();

                ProcessRunner.Received(1).Dispose();
                ConstrainedProcessRunner.Received(1).Dispose();
            }


            [Fact]
            public void DeletesContainerDirectory()
            {
                Container.Destroy();

                this.Directory.Received(1).Destroy();
            }

            [Fact]
            public void WhenContainerStopped_Runs()
            {
                Container.Stop(false);
                Container.Destroy();

                ProcessRunner.Received(1).Dispose();
            }
        }

        public class GetInfo : ContainerTests
        {
            [Fact]
            public void ReturnsListOfReservedPorts()
            {
                TcpPortManager.ReserveLocalPort(1000, Arg.Any<string>()).Returns(1000);
                TcpPortManager.ReserveLocalPort(1001, Arg.Any<string>()).Returns(1001);

                Container.ReservePort(1000);
                Container.ReservePort(1001);

                var info = Container.GetInfo();

                Assert.Collection(info.ReservedPorts,
                    x => Assert.Equal(1000, x),
                    x => Assert.Equal(1001, x)
                );
            }

            [Fact]
            public void ReturnsProperties()
            {
                var properties = new Dictionary<string, string>()
                {
                    { "name1", "value1" },
                    { "name2", "value2" },
                };
                ContainerPropertiesService.GetProperties(Container).Returns(properties);

                var info = Container.GetInfo();

                Assert.Equal(
                    new HashSet<string>(properties.Keys),
                    new HashSet<string>(info.Properties.Keys)
                );
            }

            [Fact]
            public void WhenManagingNoProcess()
            {
                JobObject.GetCpuStatistics().Returns(new CpuStatistics
                {
                    TotalKernelTime = TimeSpan.Zero,
                    TotalUserTime = TimeSpan.Zero,
                });
                JobObject.GetProcessIds().Returns(new int[0]);


                var info = Container.GetInfo();

                Assert.Equal(TimeSpan.Zero, info.CpuStat.TotalProcessorTime);
                Assert.Equal(0ul, info.MemoryStat.PrivateBytes);
            }

            [Fact]
            public void WhenManagingMultipleProcesses()
            {
                const long oneProcessPrivateMemory = 1024;
                TimeSpan expectedTotalKernelTime = TimeSpan.FromSeconds(2);
                TimeSpan expectedTotalUserTime = TimeSpan.FromSeconds(2);

                var expectedCpuStats = new CpuStatistics
                {
                    TotalKernelTime = expectedTotalKernelTime,
                    TotalUserTime = expectedTotalUserTime,
                };

                var firstProcess = Substitute.For<IProcess>();
                firstProcess.Id.Returns(1);
                firstProcess.PrivateMemoryBytes.Returns(oneProcessPrivateMemory);
                
                var secondProcess = Substitute.For<IProcess>();
                secondProcess.Id.Returns(2);
                secondProcess.PrivateMemoryBytes.Returns(oneProcessPrivateMemory);
                
                JobObject.GetCpuStatistics().Returns(expectedCpuStats);
                JobObject.GetProcessIds().Returns(new int[] { 1, 2 });

                ProcessHelper.GetProcesses(null).ReturnsForAnyArgs(new[] { firstProcess, secondProcess });

                var info = Container.GetInfo();

                Assert.Equal(expectedTotalKernelTime + expectedTotalUserTime,info.CpuStat.TotalProcessorTime);
                Assert.Equal((ulong)firstProcess.PrivateMemoryBytes + (ulong)secondProcess.PrivateMemoryBytes, info.MemoryStat.PrivateBytes);
            }

            [Fact]
            public void WhenContainerStopped_Runs()
            {
                JobObject.GetCpuStatistics().Returns(new CpuStatistics
                {
                    TotalKernelTime = TimeSpan.Zero,
                    TotalUserTime = TimeSpan.Zero,
                });
                JobObject.GetProcessIds().Returns(new int[0]);

                Container.Stop(false);
                var info = Container.GetInfo();

                Assert.NotNull(info);
            }

            [Fact]
            public void WhenContainerDestroyed_Throws()
            {
                Container.Destroy();
                Action action = () => Container.GetInfo();

                Assert.Throws<InvalidOperationException>(action);
            }
        }

        public class LimitMemory : ContainerTests
        {
            [Fact]
            public void SetsJobMemoryLimit()
            {
                Container.LimitMemory(2048);

                JobObject.Received(1).SetJobMemoryLimit(2048);
            }

            [Fact]
            public void WhenContainerNotActive_Throws()
            {
                Container.Stop(false);
                Action action = () => Container.LimitMemory(3000);

                Assert.Throws<InvalidOperationException>(action);
            }
        }

        public class RemoveProperty : ContainerTests
        {
            [Fact]
            public void RemovesProperty()
            {
                Container.RemoveProperty("Name");

                ContainerPropertiesService.Received(1).RemoveProperty(Container, "Name");
            }
        }

        public class SetProperty : ContainerTests
        {
            [Fact]
            public void SetsProperty()
            {                
                Container.SetProperty("Name", "Value");

                ContainerPropertiesService.Received(1).SetProperty(Container, "Name", "Value");
            }
        }

        public class Stop : ContainerTests
        {
            [Fact]
            public void DisposesProcessRunners()
            {
                Container.Stop(false);

                ProcessRunner.Received(1).Dispose();
                ConstrainedProcessRunner.Received(1).Dispose();
            }

            [Fact]
            public void WhenContainerDestroyed_Throws()
            {
                Container.Destroy();

                Action action = () => Container.Stop(false);

                Assert.Throws<InvalidOperationException>(action);
            }

            [Fact]
            public void ChangesStateToStopped()
            {
                Container.Stop(false);
                var info = Container.GetInfo();

                Assert.Equal(ContainerState.Stopped, info.State);
            }
        }

        public class Dispose : ContainerTests
        {
        }
    }
}
