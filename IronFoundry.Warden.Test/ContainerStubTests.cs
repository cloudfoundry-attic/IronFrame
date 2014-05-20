using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Warden.PInvoke;
using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Test.TestSupport;
using IronFoundry.Warden.Utilities;
using NSubstitute;
using Xunit;

namespace IronFoundry.Warden.Test
{
    public class ContainerStubContext : IDisposable
    {
        protected readonly string testUserName = "TestUser";
        protected readonly string testUserPassword = "TestUserPassword";
        protected readonly string containerHandleString = "TestHandle";

        protected ContainerHandle containerHandle;
        protected JobObject jobObject;
        protected JobObjectLimits jobObjectLimits;
        protected ContainerStub containerStub;
        protected ICommandRunner commandRunner;
        protected string tempDirectory;
        protected IContainerDirectory containerDirectory;
        protected IContainerUser userInfo;
        protected ProcessHelper processHelper;
        protected ProcessMonitor processMonitor;

        public ContainerStubContext()
        {
            containerHandle = new ContainerHandle(containerHandleString);

            commandRunner = Substitute.For<ICommandRunner>();

            jobObject = Substitute.For<JobObject>();
            jobObjectLimits = Substitute.For<JobObjectLimits>(jobObject, TimeSpan.FromMilliseconds(10));
            processHelper = Substitute.For<ProcessHelper>();
            processMonitor = new ProcessMonitor();

            containerStub = new ContainerStub(jobObject, jobObjectLimits, commandRunner, processHelper, processMonitor);

            this.tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            this.containerDirectory = Substitute.For<IContainerDirectory>();
            this.containerDirectory.FullName.Returns(this.tempDirectory);

            this.userInfo = Substitute.For<IContainerUser>();
            this.userInfo.UserName.Returns(testUserName);
            this.userInfo.GetCredential().Returns(new System.Net.NetworkCredential(testUserName, testUserPassword));
        }

        public virtual void Dispose()
        {
            if (jobObject.Handle != null)
            {
                jobObject.TerminateProcesses();
            }
            jobObject.Dispose();

            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);
        }
    }

    public class ContainerStubTests
    {
        public class BeforeInitialized : ContainerStubContext
        {
            public BeforeInitialized()
            {
                containerStub = new ContainerStub(null, new JobObjectLimits(jobObject), null, null, new ProcessMonitor());
            }

            [Fact]
            public void StateIsBorn()
            {
                Assert.Equal(ContainerState.Born, containerStub.State);
            }

            [Fact]
            public void CannotLaunchProcessIfContainerIsNonActive()
            {
                var si = new CreateProcessStartInfo("cmd.exe");

                // Not initialized ==> not active
                Assert.Throws<InvalidOperationException>(() => containerStub.CreateProcess(si, false));
            }

            [Fact]
            public void BindMountsThrows()
            {
                var mounts = new BindMount[]
                {
                    new BindMount
                    {
                        SourcePath = @"C:\Global\Path",
                        DestinationPath = @"C:\Container\Path",
                        Access = FileAccess.Read,
                    },
                };

                Assert.Throws<InvalidOperationException>(() => containerStub.BindMounts(mounts));
            }

            [Fact]
            public void GetInfoThrows()
            {
                Assert.Throws<InvalidOperationException>(() => containerStub.GetInfo());
            }

            [Fact]
            public void ReservePortThrowsNotImplemented()
            {
                Assert.Throws<NotImplementedException>(() => containerStub.ReservePort(100));
            }

            [Fact]
            public void StopThrows()
            {
                Assert.Throws<InvalidOperationException>(() => containerStub.Stop(false));
            }
        }

        public class WhenInitialized : ContainerStubContext
        {
            public WhenInitialized()
            {
                containerStub.Initialize(containerDirectory, containerHandle, userInfo);
            }

            [Fact]
            public void StateIsActive()
            {
                Assert.Equal(ContainerState.Active, containerStub.State);
            }

            [Fact]
            public void CanReturnDirectoryPath()
            {
                Assert.Equal(tempDirectory, containerStub.ContainerDirectoryPath);
            }

            [Fact]
            public void CachesUserInformation()
            {
                Assert.NotNull(containerStub.ContainerUserName);
            }

            [Fact]
            public void ReturnsContainerHandle()
            {
                Assert.Equal("TestHandle", containerStub.Handle.ToString());
            }

            [Fact]
            public void StartedProcessLaunchUnderJobObject()
            {
                var si = new CreateProcessStartInfo("cmd.exe");

                using (var p = containerStub.
                    CreateProcess(si, false))
                {
                    bool isInJob = false;

                    NativeMethods.IsProcessInJob(p.Handle, jobObject.Handle, out isInJob);
                    Assert.True(isInJob);
                }
            }

            [Fact]
            public void SuppliedArgumentsInStartupInfoIsPassedToProcess()
            {
                var tempFile = Path.Combine(tempDirectory, Guid.NewGuid().ToString());

                var si = new CreateProcessStartInfo("cmd.exe", string.Format(@"/C echo Boomerang > {0}", tempFile));

                using (var p = containerStub.CreateProcess(si))
                {
                    WaitForGoodExit(p);

                    var output = File.ReadAllText(tempFile);
                    Assert.Contains("Boomerang", output);
                }
            }

            [Fact]
            public void StartsProcessWithEnvironmentVariables()
            {
                var tempFile = Path.Combine(tempDirectory, Guid.NewGuid().ToString());

                var si = new CreateProcessStartInfo("cmd.exe", string.Format(@"/C echo %FOO% > {0}", tempFile));
                si.EnvironmentVariables["FOO"] = "BAR";

                using (var p = containerStub.CreateProcess(si))
                {
                    WaitForGoodExit(p);

                    var output = File.ReadAllText(tempFile);
                    Assert.Contains("BAR", output);
                }
            }

            [Fact]
            public void StartsProcessWithSpecifiedWorkingDirectory()
            {
                var tempFile = Path.Combine(tempDirectory, Guid.NewGuid().ToString());

                var si = new CreateProcessStartInfo("cmd.exe", string.Format(@"/C cd > {0}", tempFile))
                {
                    WorkingDirectory = tempDirectory
                };

                using (var p = containerStub.CreateProcess(si, false))
                {
                    WaitForGoodExit(p);

                    var output = File.ReadAllText(tempFile);
                    Assert.Contains(tempDirectory, output);
                }
            }

            [Fact]
            public void CanGetExitCodeFromCompletedProcess()
            {
                var si = new CreateProcessStartInfo("cmd.exe", @"/S /C ""ping 127.0.0.1 -n 1 && exit""");
                si.WorkingDirectory = tempDirectory;

                using (var p = containerStub.CreateProcess(si))
                {
                    WaitForGoodExit(p);
                    Assert.Equal(0, p.ExitCode);
                }
            }

            [Fact]
            public void WhenProcessExitsWithError_ExitCodeIsCorrect()
            {
                var si = new CreateProcessStartInfo("cmd.exe", "/c exit 10");

                using (var p = containerStub.CreateProcess(si))
                {
                    p.WaitForExit(2000);
                    Assert.Equal(10, p.ExitCode);
                    p.Kill();
                }
            }

            [Fact]
            public void WhenProcessFailsToStart_ThrowsException()
            {
                var si = new CreateProcessStartInfo("DoesNotExist.exe");

                var ex = Assert.Throws<System.ComponentModel.Win32Exception>(() => containerStub.CreateProcess(si));
            }

            [Fact]
            public async void WhenReceivingRunCommand_ShouldDispatchToCommandRunner()
            {
                commandRunner.RunCommandAsync(false, null, null).ReturnsTaskForAnyArgs(new TaskCommandResult(0, null, null));

                var result = await containerStub.RunCommandAsync(new RemoteCommand(false, "tar", "c:\temp"));

                commandRunner.Received(x => x.RunCommandAsync(Arg.Any<bool>(), Arg.Is<string>(y => y == "tar"), Arg.Is<string[]>(y => y[0] == "c:\temp")));
            }

            [Fact(Skip="Unreliable on build server, investigate")]
            public void WhenAttachingLogEmitter_ForwardsOutputToEmitter()
            {
                var emitter = Substitute.For<ILogEmitter>();
                containerStub.AttachEmitter(emitter);

                var si = new CreateProcessStartInfo("cmd.exe", @"/C echo Boomerang");

                using (var p = containerStub.CreateProcess(si))
                {
                    WaitForGoodExit(p);
                    emitter.Received().EmitLogMessage(logmessage.LogMessage.MessageType.OUT, "Boomerang");
                }
            }

            [Fact(Skip="Unreliable on build server, investigate")]
            public void WhenAttachingLogEmitter_ForwardsErrorsToEmitter()
            {
                var emitter = Substitute.For<ILogEmitter>();
                containerStub.AttachEmitter(emitter);

                var si = new CreateProcessStartInfo("cmd.exe", @"/C echo Boomerang>&2");

                using (var p = containerStub.CreateProcess(si))
                {
                    WaitForGoodExit(p);
                    emitter.Received().EmitLogMessage(logmessage.LogMessage.MessageType.ERR, "Boomerang");
                }
            }

            [Fact]
            public void WhenLimitingMemory_SetsJobObjectMemoryLimit()
            {
                containerStub.LimitMemory(new LimitMemoryInfo(1024));

                jobObjectLimits.Received(1, x => x.LimitMemory(1024));
            }

            [Fact]
            public void WhenMemoryLimitIsReached_RaisesOutOfMemory()
            {
                bool eventRaised = false;
                containerStub.OutOfMemory += (sender, e) =>
                {
                    eventRaised = true;
                };

                jobObjectLimits.MemoryLimitReached += Raise.Event();

                Assert.True(eventRaised);
            }

            [Fact]
            public void BindMountsDelegatesToContainerDirectory()
            {
                var mounts = new BindMount[]
                {
                    new BindMount
                    {
                        SourcePath = @"C:\Global\Path",
                        DestinationPath = @"C:\Container\Path",
                        Access = FileAccess.Read,
                    }
                };

                containerStub.BindMounts(mounts);

                containerDirectory.Received(1, x => x.BindMounts(mounts));
            }

            public class GetInfo : WhenInitialized
            {
                protected ContainerInfo Info { get; private set; }
                protected CpuStatistics CpuStatistics { get; private set; }
                protected IProcess[] Processes { get; private set; }

                public GetInfo() : base()
                {
                    CpuStatistics = new Containers.CpuStatistics
                    {
                        TotalKernelTime = TimeSpan.FromSeconds(1),
                        TotalUserTime = TimeSpan.FromSeconds(2),
                    };
                    jobObject.GetCpuStatistics().Returns(CpuStatistics);

                    Processes = new IProcess[]
                    {
                        CreateProcess(1, 1024),
                        CreateProcess(2, 4096),
                    };
                    jobObject.GetProcessIds().Returns(new int[] { 1, 2 });
                    processHelper.GetProcesses(ArgMatchers.IsSequence(1, 2)).Returns(Processes);

                    Info = containerStub.GetInfo();
                }

                [Fact]
                public void ReturnsHostIPAddress()
                {
                    var localIPAddress = GetLocalIPAddress();
                    Assert.Equal(localIPAddress.ToString(), Info.HostIPAddress);
                }

                [Fact]
                public void ReturnsContainerIPAddress()
                {
                    var localIPAddress = GetLocalIPAddress();
                    Assert.Equal(localIPAddress.ToString(), Info.ContainerIPAddress);
                }

                [Fact]
                public void ReturnsContainerPath()
                {
                    Assert.Equal(tempDirectory, Info.ContainerPath);
                }

                [Fact]
                public void ReturnsEvents()
                {
                    Assert.Empty(Info.Events);
                }

                [Fact]
                public void ReturnsState()
                {
                    Assert.Equal(containerStub.State, Info.State);
                }

                [Fact]
                public void ReturnsCpuStat()
                {
                    Assert.Equal(CpuStatistics.TotalKernelTime + CpuStatistics.TotalUserTime, Info.CpuStat.TotalProcessorTime);
                }

                [Fact]
                public void ReturnsMemoryStat()
                {
                    Assert.Equal(1024UL + 4096UL, Info.MemoryStat.PrivateBytes);
                }

                private IPAddress GetLocalIPAddress()
                {
                    var address = Dns.GetHostAddresses(Dns.GetHostName());
                    return address.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                }

                static IProcess CreateProcess(int processId, long privateMemoryBytes)
                {
                    var process = Substitute.For<IProcess>();
                    process.Id.Returns(processId);
                    process.PrivateMemoryBytes.Returns(privateMemoryBytes);
                    return process;
                }
            }

            public class Stop : WhenInitialized
            {
                protected IProcess[] Processes { get; private set; }

                public Stop()
                {
                    Processes = new IProcess[]
                    {
                        CreateProcess(1),
                        CreateProcess(2),
                    };
                    jobObject.GetProcessIds().Returns(new int[] { 1, 2 });

                    processHelper.GetProcesses(ArgMatchers.IsSequence(1, 2)).Returns(Processes);
                }

                [Fact]
                public void WhenKillIsFalse_SendsSignalToProcesses()
                {
                    containerStub.Stop(false);

                    Processes[0].Received(1, x => x.RequestExit());
                    Processes[1].Received(1, x => x.RequestExit());
                }

                [Fact]
                public void WhenKillIsFalse_GivesProcessAChanceToExit()
                {
                    containerStub.Stop(false);

                    Processes[0].Received(1, x => x.WaitForExit(10000));
                    Processes[1].Received(1, x => x.WaitForExit(10000));
                }

                [Fact]
                public void WhenKillIsTrue_DoesNotSendSignalToProcesses()
                {
                    containerStub.Stop(true);

                    Processes[0].DidNotReceive(x => x.RequestExit());
                    Processes[1].DidNotReceive(x => x.RequestExit());
                }

                [Fact]
                public void WhenRequestExitThrows_DoesNotPreventOtherProcessesFromReceivedRequestExit()
                {
                    Processes[0].Throws(x => x.RequestExit(), new InvalidTimeZoneException());

                    containerStub.Stop(false);

                    Processes[1].Received(1, x => x.RequestExit());
                }

                [Fact]
                public void WhenKillIsFalse_ProcessKillInvoked()
                {
                    containerStub.Stop(false);

                    Processes[0].Received(1, x => x.Kill());
                    Processes[1].Received(1, x => x.Kill());
                }

                [Fact]
                public void WhenKillIsTrue_ProcessKillInvoked()
                {
                    containerStub.Stop(true);

                    Processes[0].Received(1, x => x.Kill());
                    Processes[1].Received(1, x => x.Kill());
                }

                [Fact]
                public void SetsStateToStopped()
                {
                    containerStub.Stop(false);

                    Assert.Equal(ContainerState.Stopped, containerStub.State);
                }

                IProcess CreateProcess(int processId)
                {
                    var process = Substitute.For<IProcess>();
                    process.Id.Returns(processId);
                    return process;
                }
            }
        }

        public class WhenInitializedWithTestUserAccount : ContainerStubContext
        {
            protected string shortUserName;
            protected TestUserHolder userHolder;
            private string tempFilePath;

            public WhenInitializedWithTestUserAccount()
            {
                this.tempFilePath = Path.Combine(tempDirectory, Guid.NewGuid().ToString());
                
                this.shortUserName = "IF_" + this.GetHashCode().ToString();
                this.userHolder = TestUserHolder.CreateUser(shortUserName);

                userInfo.GetCredential().Returns(new System.Net.NetworkCredential(userHolder.UserName, userHolder.Password));
                AddFileSecurity(tempDirectory, userHolder.Principal.Name, FileSystemRights.FullControl, AccessControlType.Allow);

                containerStub.Initialize(containerDirectory, containerHandle, userInfo);
            }

            public override void Dispose()
            {
                userHolder.Dispose();
                base.Dispose();
            }

            [FactAdminRequired(Skip = "Unreliable on build server, review build server settings")]
            public void WhenImpersonationRequested_LaunchesProcessImpersonated()
            {
                var si = new CreateProcessStartInfo("cmd.exe", string.Format(@"/C echo %USERNAME% > {0}", tempFilePath));

                using (var p = containerStub.CreateProcess(si, true))
                {
                    WaitForGoodExit(p);

                    var output = File.ReadAllText(tempFilePath);
                    Assert.Contains(userHolder.UserName, output);
                } 
            }

            [FactAdminRequired(Skip = "Unreliable on build server, review build server settings")]
            public void CanLaunchProcessAsAlternateUser()
            {
                var si = new CreateProcessStartInfo("cmd.exe", string.Format(@"/C echo %USERNAME% > {0}", tempFilePath))
                {
                    UserName = userHolder.UserName,
                    Password = userHolder.Password.ToSecureString()
                };

                using (var p = containerStub.CreateProcess(si))
                {
                    WaitForGoodExit(p);

                    var output = File.ReadAllText(tempFilePath);
                    Assert.Contains(userHolder.UserName, output);
                }
            }
        }

        public class WhenDestroyed : ContainerStubContext
        {
            [Fact]
            public void StateIsDestroyed()
            {
                containerStub.Destroy();
                Assert.Equal(ContainerState.Destroyed, containerStub.State);
            }
        }

        public class WhenDisposed : ContainerStubContext
        {
            public WhenDisposed()
            {
                containerStub.Initialize(containerDirectory, containerHandle, userInfo);
            }

            [Fact]
            public void DisposesJobObject()
            {
                containerStub.Dispose();
                jobObject.Received().Dispose();
            }
        }

        internal static void WaitForGoodExit(Utilities.IProcess p)
        {
            p.WaitForExit(2000);
            Assert.Equal(0, p.ExitCode);
            p.Kill();
        }

        internal static void AddFileSecurity(string file, string account, FileSystemRights rights, AccessControlType access)
        {
            var fileSecurity = File.GetAccessControl(file);
            fileSecurity.AddAccessRule(new FileSystemAccessRule(account, rights, access));
            File.SetAccessControl(file, fileSecurity);
        }
    }
}
