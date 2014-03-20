namespace IronFoundry.Warden.Containers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Security.AccessControl;
    using IronFoundry.Warden.PInvoke;
    using IronFoundry.Warden.Shared.Messaging;
    using IronFoundry.Warden.Test;
    using IronFoundry.Warden.Test.TestSupport;
    using Xunit;

    public class ProcessLauncherTest : IDisposable
    {
        JobObject jobObject = new JobObject();
        ProcessLauncher launcher = new ProcessLauncher();
        string tempDirectory;

        public ProcessLauncherTest()
        {
            tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);
        }

        public void Dispose()
        {
            jobObject.TerminateProcesses();
            jobObject.Dispose();

            launcher.Dispose();

            Directory.Delete(tempDirectory, true);
        }

        [Fact]
        public void StartedProcessLaunchUnderJobObject()
        {
            var si = new CreateProcessStartInfo("cmd.exe");

            using (var p = launcher.LaunchProcess(si, jobObject))
            {
                bool isInJob = false;

                NativeMethods.IsProcessInJob(p.Handle, jobObject.Handle, out isInJob);
                Assert.True(isInJob);
            }
        }

        [Fact]
        public void WhenProcessExitsImmediately_ReturnsProcess()
        {
            var si = new CreateProcessStartInfo("cmd.exe", "/C exit 0");

            using (var p = launcher.LaunchProcess(si, jobObject))
            {
                Assert.NotEqual(0, p.Id);
                Assert.True(p.HasExited);
                Assert.Equal(0, p.ExitCode);
            }
        }

        [Fact]
        public void WhenProcessFailsToStart_ReturnsProcessExitStatus()
        {
            var si = new CreateProcessStartInfo("cmd.exe", "/C exit 10");

            var ex = Assert.Throws<ProcessLauncherException>(() => launcher.LaunchProcess(si, jobObject));

            Assert.Equal(10, ex.Code);
        }

        [Fact]
        public void WhenProcessFailsToStart_ReturnsStandardOutputTail()
        {
            var si = new CreateProcessStartInfo("cmd.exe", "/C echo Failed to start && exit 10");

            var ex = Assert.Throws<ProcessLauncherException>(() => launcher.LaunchProcess(si, jobObject));

            Assert.Contains("Failed to start", ex.RemoteData);
        }

        [Fact]
        public void SuppliedArgumentsInStartupInfoIsPassedToRemoteProcess()
        {
            var tempFile = Path.Combine(tempDirectory, Guid.NewGuid().ToString());

            var si = new CreateProcessStartInfo("cmd.exe", string.Format(@"/K echo Boomerang > {0}", tempFile));

            using (var p = launcher.LaunchProcess(si, jobObject))
            {
                var output = File.ReadAllText(tempFile);
                Assert.Contains("Boomerang", output);
            }
        }

        [Fact]
        public void StartsProcessWithEnvironmentVariables()
        {
            var tempFile = Path.Combine(tempDirectory, Guid.NewGuid().ToString());

            var si = new CreateProcessStartInfo("cmd.exe", string.Format(@"/K echo %FOO% > {0}", tempFile));
            si.EnvironmentVariables["FOO"] = "BAR";

            using (var p = launcher.LaunchProcess(si, jobObject))
            {
                var output = File.ReadAllText(tempFile);
                Assert.Contains("BAR", output);
            }
        }

        [Fact]
        public void StartsProcessWithSpecifiedWorkingDirectory()
        {
            var tempFile = Path.Combine(tempDirectory, Guid.NewGuid().ToString());

            var si = new CreateProcessStartInfo("cmd.exe", string.Format(@"/K cd > {0}", tempFile));
            si.WorkingDirectory = tempDirectory;

            using (var p = launcher.LaunchProcess(si, jobObject))
            {
                var output = File.ReadAllText(tempFile);
                Assert.Contains(tempDirectory, output);

                // Need to kill process and wait for it's exit before allowing
                // cleanup of temporary directories, otherwise the temp directory
                // may still be in use.
                p.Kill();
                p.WaitForExit();
            }
        }

        [Fact]
        public void CanGetExitCodeFromCompletedProcess()
        {
            var si = new CreateProcessStartInfo("cmd.exe", @"/S /C ""ping 127.0.0.1 -n 1 && exit""");
            si.WorkingDirectory = tempDirectory;

            using (var p = launcher.LaunchProcess(si, jobObject))
            {
                p.WaitForExit();
                Assert.Equal(0, p.ExitCode);
            }
        }

        [Fact]
        public void ProcessLaunchFailures_ThrowsAnException()
        {
            var si = new CreateProcessStartInfo("DoesNotExist.exe");

            Assert.Throws<ProcessLauncherException>(() => launcher.LaunchProcess(si, jobObject));
        }

        [Fact]
        public void ProcessLaunchFailures_ThrownExceptionIncludesErrorDetails()
        {
            var si = new CreateProcessStartInfo("DoesNotExist.exe");

            var ex = Record.Exception(() => launcher.LaunchProcess(si, jobObject));
            ProcessLauncherException processException = (ProcessLauncherException)ex;

            Assert.Equal(-32603, processException.Code);
            Assert.Contains("CreateProcessHandler", processException.RemoteData);
        }

        [Fact]
        public void ProcessLaunchFailures_ThrownExceptionIncludesRemoteStack()
        {
            var si = new CreateProcessStartInfo("DoesNotExist.exe");

            var ex = Record.Exception(() => launcher.LaunchProcess(si, jobObject));
            ProcessLauncherException processException = (ProcessLauncherException)ex;

            Assert.Contains("CreateProcessHandler", processException.RemoteData);
        }

        [FactAdminRequired]
        public void CanLaunchProcessAsAlternateUser()
        {
            string shortId = this.GetType().GetHashCode().ToString();
            string testUserName = "IFTest_" + shortId;

            using (var testUser = TestUserHolder.CreateUser(testUserName))
            {
                AddFileSecurity(tempDirectory, testUser.Principal.Name, FileSystemRights.FullControl, AccessControlType.Allow);

                var tempFile = Path.Combine(tempDirectory, Guid.NewGuid().ToString());

                var si = new CreateProcessStartInfo("cmd.exe", string.Format(@"/K echo %USERNAME% > {0}", tempFile))
                {
                    UserName = testUserName,
                    Password = testUser.Password.ToSecureString()
                };

                using (var p = launcher.LaunchProcess(si, jobObject))
                {
                    var output = File.ReadAllText(tempFile);
                    Assert.Contains(testUserName, output);
                }
            }
        }

        [Fact]
        public void WhenHostProcessIsNotRunning_ReturnsInvalidProcessId()
        {
            Assert.Equal(0, launcher.HostProcessId);
        }

        [Fact]
        public void AfterLaunchingProcess_ReturnsHostProcessId()
        {
            var si = new CreateProcessStartInfo("cmd.exe");

            using (var p = launcher.LaunchProcess(si, jobObject))
            {
                Assert.NotEqual(0, launcher.HostProcessId);
                Assert.NotNull(Process.GetProcessById(launcher.HostProcessId));
            }
        }

        [Fact]
        public void DisposeShouldShutdownHostProcess()
        {
            var si = new CreateProcessStartInfo("cmd.exe");
            var p = launcher.LaunchProcess(si, jobObject);
            p.Dispose();

            var hostProcess = Process.GetProcessById(launcher.HostProcessId);

            launcher.Dispose();

            Assert.True(hostProcess.HasExited);
        }

        [Fact]
        public void DisposeShouldNotThrowWhenHostProcessDies()
        {
            var si = new CreateProcessStartInfo("cmd.exe");
            var p = launcher.LaunchProcess(si, jobObject);
            p.Dispose();

            var hostProcess = Process.GetProcessById(launcher.HostProcessId);
            hostProcess.Kill();

            var ex = Record.Exception(() => launcher.Dispose());

            Assert.Null(ex);
        }

        private void AddFileSecurity(string file, string account, FileSystemRights rights, AccessControlType access)
        {
            var fileSecurity = File.GetAccessControl(file);
            fileSecurity.AddAccessRule(new FileSystemAccessRule(account, rights, access));
            File.SetAccessControl(file, fileSecurity);
        }
    }
}
