using System.Collections.Generic;
using System.IO;
using System.Linq;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Utilities;
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

        public ContainerTests()
        {
            User = Substitute.For<IContainerUser>();
            User.UserName.Returns("container-username");
            
            Directory = Substitute.For<IContainerDirectory>();

            ProcessRunner = Substitute.For<IProcessRunner>();
            ConstrainedProcessRunner = Substitute.For<IProcessRunner>();
            TcpPortManager = Substitute.For<ILocalTcpPortManager>();
            JobObject = Substitute.For<JobObject>();

            Container = new Container("handle", User, Directory, TcpPortManager, JobObject, ProcessRunner, ConstrainedProcessRunner);
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

        public class Dispose : ContainerTests
        {
            [Fact]
            public void ReleasesPorts()
            {
                TcpPortManager.ReserveLocalPort(Arg.Any<int>(), Arg.Any<string>())
                    .Returns(c => c.Arg<int>());

                Container.ReservePort(100);
                Container.ReservePort(101);
                
                Container.Dispose();

                TcpPortManager.Received(1).ReleaseLocalPort(100, User.UserName);
                TcpPortManager.Received(1).ReleaseLocalPort(101, User.UserName);
            }

            [Fact]
            public void DeletesTheUser()
            {
                Container.Dispose();

                User.Received(1).Delete();
            }
        }
    }
}
