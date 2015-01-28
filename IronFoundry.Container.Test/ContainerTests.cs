using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IronFoundry.Warden.Containers;
using IronFoundry.Container.Utilities;
using NSubstitute;
using Xunit;

namespace IronFoundry.Container
{
    public class ContainerTests
    {
        Container Container { get; set; }
        IContainerUser User { get; set; }
        IContainerDirectory Directory { get; set; }
        JobObject JobObject { get; set; }
        IProcessRunner ProcessRunner { get; set; }
        IProcessRunner ConstrainedProcessRunner { get; set; }
        ILocalTcpPortManager TcpPortManager { get; set; }
        Dictionary<string, string> ContainerEnvironment { get; set; }
        ProcessHelper ProcessHelper { get; set; }

        public ContainerTests()
        {
            User = Substitute.For<IContainerUser>();
            User.UserName.Returns("container-username");
            
            Directory = Substitute.For<IContainerDirectory>();

            ProcessRunner = Substitute.For<IProcessRunner>();
            ConstrainedProcessRunner = Substitute.For<IProcessRunner>();
            TcpPortManager = Substitute.For<ILocalTcpPortManager>();
            JobObject = Substitute.For<JobObject>();
            ContainerEnvironment = new Dictionary<string, string>() { { "Handle", "handle" } };
            ProcessHelper = Substitute.For<ProcessHelper>();

            Container = new Container("id", "handle", User, Directory, TcpPortManager, JobObject, ProcessRunner, ConstrainedProcessRunner, ProcessHelper, ContainerEnvironment);
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
        }

        public class GetInfo : ContainerTests
        {
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
        }

        public class LimitMemory : ContainerTests
        {
            [Fact]
            public void SetsJobMemoryLimit()
            {
                Container.LimitMemory(2048);

                JobObject.Received(1).SetJobMemoryLimit(2048);
            }
        }

        public class Dispose : ContainerTests
        {
        }
    }
}
