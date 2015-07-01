using System.DirectoryServices.ActiveDirectory;
using DiskQuotaTypeLibrary;
using IronFrame.Utilities;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Xunit;

namespace IronFrame
{
    public class ContainerTests
    {
        Container Container { get; set; }
        IProcessRunner ConstrainedProcessRunner { get; set; }
        Dictionary<string, string> ContainerEnvironment { get; set; }
        IContainerDirectory Directory { get; set; }
        JobObject JobObject { get; set; }
        ProcessHelper ProcessHelper { get; set; }
        IProcessRunner ProcessRunner { get; set; }
        IContainerPropertyService ContainerPropertiesService { get; set; }
        ILocalTcpPortManager TcpPortManager { get; set; }
        IContainerUser User { get; set; }
        DiskQuotaControl DiskQuotaControl { get; set; }
        ContainerHostDependencyHelper DependencyHelper { get; set; }
        private readonly string _containerUsername;

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
            _containerUsername = string.Concat("container-username-", Guid.NewGuid().ToString());
            User.UserName.Returns(_containerUsername);

            DiskQuotaControl = Substitute.For<DiskQuotaControl>();

            DependencyHelper = Substitute.For<ContainerHostDependencyHelper>();

            Container = new Container(
                string.Concat("id-", Guid.NewGuid()),
                string.Concat("handle-", Guid.NewGuid()),
                User,
                Directory,
                ContainerPropertiesService,
                TcpPortManager,
                JobObject,
                DiskQuotaControl,
                ProcessRunner,
                ConstrainedProcessRunner,
                ProcessHelper,
                ContainerEnvironment,
                DependencyHelper);
        }

        private EventWaitHandle CreateStopGuardEvent()
        {
            return new EventWaitHandle(false, EventResetMode.ManualReset, string.Concat(@"Global\discharge-", _containerUsername));
        }

        public class SetActiveProcessLimit : ContainerTests
        {
            [Fact]
            public void ProxiesToJobObject()
            {
                uint processLimit = 8765;
                this.Container.SetActiveProcessLimit(processLimit);
                JobObject.Received(1).SetActiveProcessLimit(processLimit);
            }
        }

        public class SetProcessPriority : ContainerTests
        {
            [Fact]
            public void ProxiesToJobObject()
            {
                var priority = ProcessPriorityClass.RealTime;
                this.Container.SetPriorityClass(priority);
                JobObject.Received(1).SetPriorityClass(priority);
            }
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

        public class GetProperties : ContainerTests
        {
            Dictionary<string, string> Properties { get; set; }

            public GetProperties()
            {
                Properties = new Dictionary<string, string>();

                ContainerPropertiesService.GetProperties(Container).Returns(Properties);
            }

            [Fact]
            public void ReturnsProperties()
            {
                Properties["Name"] = "Value";

                var properties = Container.GetProperties();

                Assert.Collection(properties,
                    x =>
                    {
                        Assert.Equal("Name", x.Key);
                        Assert.Equal("Value", x.Value);
                    }
                );
                ContainerPropertiesService.Received(1).GetProperties(Container);
            }

            [Fact]
            public void ThrowsInvalidOperationWhenIOExceptionThrownAndDestroyed()
            {
                Container.Destroy();
                ContainerPropertiesService.GetProperties(Container).Returns(x => { throw new IOException(); });

                Assert.Throws<InvalidOperationException>(() => Container.GetProperties());
            }

            [Fact]
            public void PassesThroughExceptionIfNotDestroyed()
            {
                ContainerPropertiesService.GetProperties(Container).Returns(x => { throw new IOException(); });

                Assert.Throws<IOException>(() => Container.GetProperties());

            }
        }

        public class ReservePort : ContainerTests
        {
            [Fact]
            public void ReservesPortForContainerUser()
            {
                Container.ReservePort(3000);

                TcpPortManager.Received(1).ReserveLocalPort(3000, _containerUsername);
            }

            [Fact]
            public void ReturnsReservedPort()
            {
                TcpPortManager.ReserveLocalPort(3000, _containerUsername).Returns(5000);

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
            ProcessSpec Spec { get; set; }
            ProcessRunSpec ExpectedRunSpec { get; set; }

            public Run()
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

                [Fact]
                public void CanFindProcessByPid()
                {
                    var pid = 9123;
                    var process = Substitute.For<IProcess>();
                    process.Id.Returns(pid);
                    ConstrainedProcessRunner.FindProcessById(pid).Returns(process);

                    var actualProcess = Container.FindProcessById(pid);
                    Assert.Equal(actualProcess.Id, pid);
                }

                [Fact]
                public void ReturnsNullWhenProcessNotFound()
                {
                    ConstrainedProcessRunner.FindProcessById(-1).Returns(null as IProcess);

                    var actualProcess = Container.FindProcessById(-1);
                    Assert.Null(actualProcess);
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

        public class StartGuard : ContainerTests, IDisposable
        {
            private JobObject guardJobObject;

            public StartGuard()
            {
                DependencyHelper.GuardExePath.Returns(@"C:\Containers\handle\bin\Guard.exe");
                const string containerUserPath = @"C:\Containers\handle\user\";
                Directory.MapUserPath("/").Returns(containerUserPath);

                var guardJobObjectName = String.Format("if:{0}:guard", Container.Id);
                guardJobObject = new JobObject(guardJobObjectName, false, true);
            }

            public void Dispose()
            {
                guardJobObject.Dispose();
            }

            [Fact]
            public void StartsExeWithCorrectOptions()
            {
                JobObject.GetJobMemoryLimit().Returns(6789UL);
                Container.StartGuard();

                var actual = ProcessRunner.Captured(x => x.Run(null)).Arg<ProcessRunSpec>();
                Assert.Equal(@"C:\Containers\handle\bin\Guard.exe", actual.ExecutablePath);
                Assert.Equal(3, actual.Arguments.Length);
                Assert.Equal(_containerUsername, actual.Arguments[0]);
                Assert.Equal("6789", actual.Arguments[1]);
                Assert.Equal(Container.Id, actual.Arguments[2]);
                Assert.Equal(@"C:\Containers\handle\user\", actual.WorkingDirectory);
                Assert.Null(actual.Credentials);
            }

            [Fact]
            public void DoesNotStartGuardIfAlreadyRunning()
            {
                using (CreateStopGuardEvent())
                {
                    JobObject.GetJobMemoryLimit().Returns(6789UL);
                    Container.StartGuard();

                    ProcessRunner.Received(0).Run(Arg.Any<ProcessRunSpec>());
                }
            }
        }

        public class StopGuard : ContainerTests
        {
            [Fact]
            public void WhenSomeoneListening_SetsEventWaitObject()
            {
                using (var stopEvent = CreateStopGuardEvent())
                {
                    Assert.False(stopEvent.WaitOne(0));
                    Container.StopGuard();
                    Assert.True(stopEvent.WaitOne(0));
                }
            }

            [Fact]
            public void WhenNooneListening_DoesNotFail()
            {
                Container.StopGuard();
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
            public void DisposesJobObject_ThisEnsuresWeCanDeleteTheDirectory()
            {
                Container.Destroy();

                JobObject.Received(1).TerminateProcessesAndWait();
                JobObject.Received(1).Dispose();
            }


            [Fact]
            public void DeletesContainerDirectory()
            {
                Container.Destroy();

                this.Directory.Received(1).Destroy();
            }

            [Fact]
            public void TransitionsToDeletedEvenIfDirectoryDeletionFails()
            {
                Directory.When(x => x.Destroy()).Do(x => { throw new Exception(); });
                try { Container.Destroy(); }
                catch { }

                Assert.Throws<InvalidOperationException>(() => Container.GetInfo());
            }

            [Fact]
            public void WhenContainerStopped_Runs()
            {
                Container.Stop(false);
                Container.Destroy();

                ProcessRunner.Received(1).Dispose();
            }

            [Fact]
            public void DeletesFirewallRules()
            {
                Container.Destroy();
                TcpPortManager.Received(1).RemoveFirewallRules(User.UserName);
            }

            [Fact]
            public void DeletesDiskQuota()
            {
                var dskuser = Substitute.For<DIDiskQuotaUser>();
                DiskQuotaControl.FindUser(User.UserName).Returns(dskuser);

                Container.Destroy();

                DiskQuotaControl.Received(1).DeleteUser(dskuser);
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

                Assert.Equal(expectedTotalKernelTime + expectedTotalUserTime, info.CpuStat.TotalProcessorTime);
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

            [Fact]
            public void WhenGuardIsRunning_Throws()
            {
                using (CreateStopGuardEvent())
                {
                    Action action = () => Container.LimitMemory(3000);

                    Assert.Throws<InvalidOperationException>(action);
                }
            }

            [Fact]
            public void ReturnsMemoryLimit()
            {
                ulong limitInBytes = 2048;
                JobObject.GetJobMemoryLimit().Returns(limitInBytes);
                Assert.Equal(limitInBytes, Container.CurrentMemoryLimit());
            }
        }

        public class LimitCpu : ContainerTests
        {
            [Fact]
            public void SetsJobCpuLimit()
            {
                Container.LimitCpu(5);

                JobObject.Received(1).SetJobCpuLimit(5);
            }

            [Fact]
            public void WhenContainerNotActive_Throws()
            {
                Container.Stop(false);
                Action action = () => Container.LimitCpu(3000);

                Assert.Throws<InvalidOperationException>(action);
            }

            [Fact]
            public void ReturnsCpuLimit()
            {
                int weight = 7;
                JobObject.GetJobCpuLimit().Returns(weight);
                Assert.Equal(weight, Container.CurrentCpuLimit());
            }
        }

        public class LimitDisk : ContainerTests
        {
            [Fact]
            public void SetsUserDiskLimit()
            {
                var quota = Substitute.For<DIDiskQuotaUser>();
                this.DiskQuotaControl.AddUser(User.UserName).Returns(quota);

                Container.LimitDisk(5);

                Assert.Equal(5, quota.QuotaLimit);
            }

            [Fact]
            public void WhenContainerNotActive_Throws()
            {
                Container.Stop(false);
                Action action = () => Container.LimitDisk(5);

                Assert.Throws<InvalidOperationException>(action);
            }


            [Fact]
            public void ReturnsDiskLimit()
            {
                ulong limitInBytes = 2048;
                var quota = Substitute.For<DIDiskQuotaUser>();
                quota.QuotaLimit = limitInBytes;
                this.DiskQuotaControl.FindUser(User.SID).Returns(quota);

                Assert.Equal(limitInBytes, Container.CurrentDiskLimit());
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
    }
}
