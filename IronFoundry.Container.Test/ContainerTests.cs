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

            static ProcessRunSpec MatchProcessRunSpec(ProcessRunSpec expected)
            {
                var expectedEnvironmentKeys = new HashSet<string>(expected.Environment.Keys);

                return Arg.Is<ProcessRunSpec>(actual =>
                    actual.ExecutablePath == expected.ExecutablePath &&
                    actual.Arguments.SequenceEqual(expected.Arguments) &&
                    new HashSet<string>(actual.Environment.Keys).SetEquals(expectedEnvironmentKeys) &&
                    actual.WorkingDirectory == expected.WorkingDirectory
                );
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
                    ProcessRunner.Received(1).Run(MatchProcessRunSpec(ExpectedRunSpec));
                }

                [Fact]
                public void ProcessIoIsRedirected()
                {
                    var stdout = Substitute.For<TextWriter>();
                    var stderr = Substitute.For<TextWriter>();
                    var io = Substitute.For<IProcessIO>();
                    io.StandardOutput.Returns(stdout);
                    io.StandardError.Returns(stderr);
                    var localProcess = Substitute.For<IProcess>();
                    ProcessRunner.Run(Arg.Any<ProcessRunSpec>()).Returns(localProcess)
                        .AndDoes(call =>
                        {
                            var runSpec = call.Arg<ProcessRunSpec>();
                            runSpec.OutputCallback("This is STDOUT");
                            runSpec.ErrorCallback("This is STDERR");
                        });

                    Container.Run(Spec, io);

                    stdout.Received(1).Write("This is STDOUT");
                    stderr.Received(1).Write("This is STDERR");
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
                    ConstrainedProcessRunner.Received(1).Run(MatchProcessRunSpec(ExpectedRunSpec));
                }

                [Fact]
                public void ProcessIoIsRedirected()
                {
                    var stdout = Substitute.For<TextWriter>();
                    var stderr = Substitute.For<TextWriter>();
                    var io = Substitute.For<IProcessIO>();
                    io.StandardOutput.Returns(stdout);
                    io.StandardError.Returns(stderr);
                    var remoteProcess = Substitute.For<IProcess>();
                    ConstrainedProcessRunner.Run(Arg.Any<ProcessRunSpec>()).Returns(remoteProcess)
                        .AndDoes(call =>
                        {
                            var runSpec = call.Arg<ProcessRunSpec>();
                            runSpec.OutputCallback("This is STDOUT");
                            runSpec.ErrorCallback("This is STDERR");
                        });

                    Container.Run(Spec, io);

                    stdout.Received(1).Write("This is STDOUT");
                    stderr.Received(1).Write("This is STDERR");
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
