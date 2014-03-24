using IronFoundry.Warden.Containers;
using IronFoundry.Warden.PInvoke;
using IronFoundry.Warden.Shared.Messaging;
using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Test.TestSupport;
using IronFoundry.Warden.Utilities;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IronFoundry.Warden.Test
{
    public class ContainerStubContext : IDisposable
    {
        protected readonly string testUserName = "TestUser";
        protected readonly string testUserPassword = "TestUserPassword";
        protected readonly string containerHandle = "TestHandle";

        protected JobObject jobObject;
        protected ContainerStub containerStub;
        protected ICommandRunner commandRunner;
        protected string tempDirectory;
        protected IContainerUser userInfo;
        protected ProcessHelper processHelper;

        public ContainerStubContext()
        {
            commandRunner = Substitute.For<ICommandRunner>();

            jobObject = Substitute.For<JobObject>();
            processHelper = Substitute.For<ProcessHelper>();

            containerStub = new ContainerStub(jobObject, commandRunner, processHelper);

            this.tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

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
        public class WhenCreated : ContainerStubContext
        {
            [Fact]
            public void StateIsBorn()
            {
                var container = new ContainerStub(null, null, null);
                Assert.Equal(ContainerState.Born, container.State);
            }

            [Fact]
            public void CannotLaunchProcessIfContainerIsNonActive()
            {
                var containerStub = new ContainerStub(null, null, null);
                var si = new CreateProcessStartInfo("cmd.exe");

                // Not initialized ==> not active
                Assert.Throws<InvalidOperationException>(() => containerStub.CreateProcess(si, false));
            }

            [Fact]
            public void ReservePortThrowsNotImplemented()
            {
                Assert.Throws<NotImplementedException>(() => containerStub.ReservePort(100));
            }
        }


        public class WhenInitialzed : ContainerStubContext
        {
            public WhenInitialzed()
            {
                containerStub.Initialize(tempDirectory, containerHandle, userInfo);
            }

            [Fact]
            public void StateIsActive()
            {
                containerStub.Initialize(tempDirectory, "TestContainerHandle", userInfo);
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

            [FactAdminRequired(Skip = "Runs unreliably on build server, review build server setup.")]            
            public void CanLaunchProcessAsAlternateUser()
            {
                string shortId = this.GetType().GetHashCode().ToString();
                string testUserName = "IFTest_" + shortId;

                using (var testUser = TestUserHolder.CreateUser(testUserName))
                {
                    AddFileSecurity(tempDirectory, testUser.Principal.Name, FileSystemRights.FullControl, AccessControlType.Allow);

                    var tempFile = Path.Combine(tempDirectory, Guid.NewGuid().ToString());

                    var si = new CreateProcessStartInfo("cmd.exe", string.Format(@"/C echo %USERNAME% > {0}", tempFile))
                    {
                        UserName = testUserName,
                        Password = testUser.Password.ToSecureString()
                    };

                    using (var p = containerStub.CreateProcess(si))
                    {
                        WaitForGoodExit(p);

                        var output = File.ReadAllText(tempFile);
                        Assert.Contains(testUserName, output);
                    }
                }
            }

            [Fact]
            public async void WhenRecievingRunCommand_ShouldDispatchToCommandRunner()
            {
                commandRunner.RunCommandAsync(false, null, null).ReturnsTaskForAnyArgs(new TaskCommandResult(0, null, null));

                var result = await containerStub.RunCommandAsync(new RemoteCommand(false, "tar", "c:\temp"));

                commandRunner.Received(x => x.RunCommandAsync(Arg.Any<bool>(), Arg.Is<string>(y => y == "tar"), Arg.Is<string[]>(y => y[0] == "c:\temp")));
            }

            [Fact]
            public void DisposeShouldTerminateProcessUnderJob()
            {
                var si = new CreateProcessStartInfo("cmd.exe");
                var p = containerStub.CreateProcess(si);

                containerStub.Dispose();

                p.WaitForExit(3000);
                Assert.True(p.HasExited);
            }

            [Fact]
            public void ProcessesAreHalted()
            {
                var si = new CreateProcessStartInfo("cmd.exe");

                var p = containerStub.CreateProcess(si, false);

                containerStub.Destroy();

                p.WaitForExit(3000);
                Assert.True(p.HasExited);
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
