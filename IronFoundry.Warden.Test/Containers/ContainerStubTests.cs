using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using IronFoundry.Container;
using IronFoundry.Container.Utilities;
using IronFoundry.Container.Win32;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Test.TestSupport;
using IronFoundry.Warden.Utilities;
using NSubstitute;
using Xunit;
// Temporary type aliases
using IContainerDirectory = IronFoundry.Warden.Containers.IContainerDirectory;

namespace IronFoundry.Warden.Test
{
    public class ContainerStubContext : IDisposable
    {
        protected readonly string testUserName = "TestUser";
        protected readonly string testUserPassword = "TestUserPassword";
        protected readonly string containerHandleString = "TestHandle";
        protected readonly int owningProcessId = 255;

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
        protected ILocalTcpPortManager portManager;
        protected FileSystemManager fileSystemManager;

        public ContainerStubContext()
        {
            containerHandle = new ContainerHandle(containerHandleString);

            commandRunner = Substitute.For<ICommandRunner>();

            portManager = Substitute.For<ILocalTcpPortManager>();
            fileSystemManager = Substitute.For<FileSystemManager>();

            jobObject = Substitute.ForPartsOf<JobObject>();
            jobObjectLimits = Substitute.For<JobObjectLimits>(jobObject, TimeSpan.FromMilliseconds(10));
            processHelper = Substitute.For<ProcessHelper>();
            processMonitor = new ProcessMonitor();

            containerStub = new ContainerStub(jobObject, jobObjectLimits, commandRunner, processHelper, processMonitor, owningProcessId, portManager, fileSystemManager);

            this.tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            this.containerDirectory = Substitute.For<IContainerDirectory>();
            this.containerDirectory.FullName.Returns(this.tempDirectory);

            this.userInfo = Substitute.For<IContainerUser>();
            this.userInfo.UserName.Returns(testUserName);
            this.userInfo.GetCredential().Returns(new NetworkCredential(testUserName, testUserPassword));
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

    public class ContainerInitializedContext : ContainerStubContext
    {
        public ContainerInitializedContext()
        {
            containerStub.Initialize(containerDirectory, containerHandle, userInfo);
        }
    }

    public class ContainerStubTests
    {
        public class AfterInitialization : ContainerInitializedContext
        {

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
        }

        public class BeforeInitialized : ContainerStubContext
        {
            public BeforeInitialized()
            {
                containerStub = new ContainerStub(null, new JobObjectLimits(jobObject), null, null, new ProcessMonitor(), null, null);
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
            public void ReservePortThrows()
            {
                Assert.Throws<InvalidOperationException>(() => containerStub.ReservePort(100));
            }

            [Fact]
            public void CopyThrows()
            {
                Assert.Throws<InvalidOperationException>(() => containerStub.Copy("source", "destination"));
            }

            [Fact]
            public void CopyFileInThrows()
            {
                Assert.Throws<InvalidOperationException>(() => containerStub.CopyFileIn("source", "destination"));
            }

            [Fact]
            public void CopyFileOutThrows()
            {
                Assert.Throws<InvalidOperationException>(() => containerStub.CopyFileOut("source", "destination"));
            }

            [Fact]
            public void StopThrows()
            {
                Assert.Throws<InvalidOperationException>(() => containerStub.Stop(false));
            }
        }

        public class CreateProcess : ContainerInitializedContext
        {
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

                var si = new CreateProcessStartInfo("cmd.exe", string.Format(@"/C set > {0}", tempFile));
                si.EnvironmentVariables["FOO"] = "BAR";
                si.EnvironmentVariables["FOO2"] = "SNAFU";

                using (var p = containerStub.CreateProcess(si))
                {
                    WaitForGoodExit(p);

                    var output = File.ReadAllText(tempFile);
                    Assert.Contains("BAR", output);
                    Assert.Contains("SNAFU", output);
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
        }

        public class GetInfo : ContainerInitializedContext
        {
            protected ContainerInfo Info { get; private set; }
            protected CpuStatistics CpuStatistics { get; private set; }
            protected IProcess[] Processes { get; private set; }

            public GetInfo()
                : base()
            {
                CpuStatistics = new CpuStatistics
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

        public class LimitingMemory : ContainerInitializedContext
        {
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
        }

        public class ReservePort : ContainerInitializedContext
        {
            [Fact]
            public void ReturnsPortReturnedfromResourceManager()
            {
                portManager.ReserveLocalPort(50000, testUserName).Returns((ushort)10000);

                int requestedPort = 50000;
                var reservedPort = containerStub.ReservePort(requestedPort);

                Assert.Equal(10000, reservedPort);
            }
        }

        public class CopyingFiles : ContainerInitializedContext
        {
            [Fact]
            public void EmptySourceThrows()
            {
                var except = Record.Exception(() => containerStub.Copy(string.Empty, "destination"));

                Assert.IsType<InvalidOperationException>(except);
            }

            [Fact]
            public void EmptyDestinationThrows()
            {
                var except = Record.Exception(() => containerStub.Copy("source", string.Empty));

                Assert.IsType<InvalidOperationException>(except);
            }

            [Fact]
            public void CopiesFiles()
            {
                containerStub.Copy("source", "destination");

                fileSystemManager.Received(x => x.Copy("source", "destination"));
            }

            [Fact]
            public void ReplacesContainerPathPlaceholders()
            {
                containerStub.Copy(@"@ROOT@/source", @"@ROOT@/destination");

                fileSystemManager.Received(x => x.Copy(Path.Combine(containerDirectory.FullName, "source"), Path.Combine(containerDirectory.FullName, "destination")));
            }
        }

        public class CopyFileIn : ContainerInitializedContext
        {
            [Fact]
            public void EmptySourceThrows()
            {
                var ex = Record.Exception(() => containerStub.CopyFileIn("", "/destination"));

                Assert.IsType<InvalidOperationException>(ex);
            }

            [Fact]
            public void EmptyDestinationThrows()
            {
                var ex = Record.Exception(() => containerStub.CopyFileIn("source", ""));

                Assert.IsType<InvalidOperationException>(ex);
            }

            [Fact]
            public void TranslatesDestinationPath()
            {
                containerStub.CopyFileIn("source", "/destination");

                fileSystemManager.Received(x => x.CopyFile("source", Path.Combine(containerDirectory.FullName, "root", "destination")));
            }

            [Fact]
            public void CopiesFile()
            {
                containerStub.CopyFileIn("source", "/destination");

                fileSystemManager.Received(x => x.CopyFile("source", Path.Combine(containerDirectory.FullName, "root", "destination")));
            }

            [Fact]
            public void WhenDestinationPathIsNotRooted_Throws()
            {
                var ex = Record.Exception(() => containerStub.CopyFileIn("source", "destination"));

                Assert.IsType<ArgumentException>(ex);
            }
        }

        public class CopyFileOut : ContainerInitializedContext
        {
            [Fact]
            public void EmptySourceThrows()
            {
                var ex = Record.Exception(() => containerStub.CopyFileOut("", "destination"));

                Assert.IsType<InvalidOperationException>(ex);
            }

            [Fact]
            public void EmptyDestinationThrows()
            {
                var ex = Record.Exception(() => containerStub.CopyFileOut("/source", ""));

                Assert.IsType<InvalidOperationException>(ex);
            }

            [Fact]
            public void TranslatesSourcePath()
            {
                containerStub.CopyFileOut("/source", "destination");

                fileSystemManager.Received(x => x.CopyFile(Path.Combine(containerDirectory.FullName, "root", "source"), "destination"));
            }

            [Fact]
            public void CopiesFile()
            {
                containerStub.CopyFileOut("/source", "destination");

                fileSystemManager.Received(x => x.CopyFile(Path.Combine(containerDirectory.FullName, "root", "source"), "destination"));
            }

            [Fact]
            public void WhenSourcePathIsNotRooted_Throws()
            {
                var ex = Record.Exception(() => containerStub.CopyFileOut("source", "destination"));

                Assert.IsType<ArgumentException>(ex);
            }
        }

        public class CreateTarFile : ContainerInitializedContext
        {
            [Fact]
            public void EmptySourcePathThrows()
            {
                var ex = Record.Exception(() => containerStub.ExtractTarFile("", @"C:\destination.tar", false));
                Assert.IsType<InvalidOperationException>(ex);
            }

            [Fact]
            public void EmptyTarFilePathThrows()
            {
                var ex = Record.Exception(() => containerStub.CreateTarFile("/source", "", false));
                Assert.IsType<InvalidOperationException>(ex);
            }

            [Fact]
            public void TranslatesSourcePath()
            {
                containerStub.CreateTarFile("/source", @"C:\destination.tar", false);

                fileSystemManager.Received(x => x.CreateTarFile(Path.Combine(containerDirectory.FullName, "root", "source"), @"C:\destination.tar", false));
            }

            [Fact]
            public void DoesNotTranslateTarFilePath()
            {
                containerStub.CreateTarFile("/source", @"C:\destination.tar", false);

                fileSystemManager.Received(x => x.CreateTarFile(Path.Combine(containerDirectory.FullName, "root", "source"), @"C:\destination.tar", false));
            }

            [Fact]
            public void CreatesTarFile()
            {
                containerStub.CreateTarFile("/source", @"C:\destination.tar", false);

                fileSystemManager.Received(x => x.CreateTarFile(Path.Combine(containerDirectory.FullName, "root", "source"), @"C:\destination.tar", false));
            }
        }

        public class ExtractTarFile : ContainerInitializedContext
        {
            [Fact]
            public void EmptyTarFilePathThrows()
            {
                var ex = Record.Exception(() => containerStub.ExtractTarFile("", "/destination", false));
                Assert.IsType<InvalidOperationException>(ex);
            }

            [Fact]
            public void EmptyDestinationPathThrows()
            {
                var ex = Record.Exception(() => containerStub.ExtractTarFile(@"C:\source.tar", "", false));
                Assert.IsType<InvalidOperationException>(ex);
            }

            [Fact]
            public void TranslatesTarFilePath()
            {
                containerStub.ExtractTarFile(@"C:\source.tar", "/destination", false);

                fileSystemManager.Received(x => x.ExtractTarFile(@"C:\source.tar", Path.Combine(containerDirectory.FullName, "root", "destination"), false));
            }

            [Fact]
            public void TranslatesDestinationPath()
            {
                containerStub.ExtractTarFile(@"C:\source.tar", "/destination", false);

                fileSystemManager.Received(x => x.ExtractTarFile(@"C:\source.tar", Path.Combine(containerDirectory.FullName, "root", "destination"), false));
            }

            [Fact]
            public void ExtractsTarFile()
            {
                containerStub.ExtractTarFile(@"C:\source.tar", "/destination", false);

                fileSystemManager.Received(x => x.ExtractTarFile(@"C:\source.tar", Path.Combine(containerDirectory.FullName, "root", "destination"), false));
            }
        }

        public class RequestingBindMounts : ContainerInitializedContext
        {
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
        }

        public class RunCommand : ContainerInitializedContext
        {

            [Fact]
            public async void ShouldDispatchToCommandRunner()
            {
                commandRunner.RunCommandAsync(null, null).ReturnsTaskForAnyArgs(new TaskCommandResult(0, null, null));

                var result = await containerStub.RunCommandAsync(new RemoteCommand(true, "tar", new [] { @"c:\temp" }));

                commandRunner.Received(x => x.RunCommandAsync(Arg.Is<string>(y => y == "tar"),  Arg.Is<IRemoteCommandArgs>(y => y.Arguments[0] == @"c:\temp")));
            }
        }

        public class StoppingContainer : ContainerInitializedContext
        {
            protected IProcess[] Processes { get; private set; }

            public StoppingContainer()
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

            [Fact]
            public void IgnoresOwningProcessId()
            {
                jobObject.GetProcessIds().Returns(new int[] { 1, 2, owningProcessId });

                containerStub.Stop(false);

                Processes[0].Received(1, x => x.RequestExit());
                Processes[1].Received(1, x => x.RequestExit());
            }

            IProcess CreateProcess(int processId)
            {
                var process = Substitute.For<IProcess>();
                process.Id.Returns(processId);
                return process;
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

        internal static void WaitForGoodExit(IProcess p)
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
