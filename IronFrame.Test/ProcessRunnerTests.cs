using System;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IronFrame.Utilities;
using Xunit;

namespace IronFrame
{
    public class ProcessRunnerTests
    {
        ProcessRunner Runner { get; set; }

        public ProcessRunnerTests()
        {
            Runner = new ProcessRunner();
        }

        public class Run : ProcessRunnerTests
        {
            static ProcessRunSpec CreateRunSpec(string exePath, string[] arguments = null)
            {
                return new ProcessRunSpec
                {
                    ExecutablePath = exePath,
                    Arguments = arguments,
                    BufferedInputOutput = false,
                };
            }

            static TempFile CreateTempFile()
            {
                return new TempFile(Path.GetTempPath());
            }

            static void WaitForGoodExit(IProcess process, int timeout = Int32.MaxValue)
            {
                Assert.True(process.WaitForExit(timeout), "Test failed because the process failed to exit within " + timeout + "ms");
                if (process.ExitCode != 0)
                    throw new Exception("Test failed because the process failed with exit code: " + process.ExitCode);
            }

            [Fact]
            public void SuppliedArgumentsAreEscaped()
            {
                using (var tempFile = CreateTempFile())
                {
                    File.WriteAllText(tempFile.FullName, "hello world");
                    var si = CreateRunSpec("findstr", new [] {"hello world", tempFile.FullName});
                    si.BufferedInputOutput = true;

                    using (var p = Runner.Run(si))
                    {
                        WaitForGoodExit(p);

                        var output = p.StandardError.ReadToEnd();
                        Assert.DoesNotContain("Cannot open world", output);
                    }
                };
            }

            [Fact]
            public void SuppliedArgumentsInStartupInfoIsPassedToProcess()
            {
                using (var tempFile = CreateTempFile())
                {
                    var si = CreateRunSpec("cmd.exe", new [] { "/C", String.Format("echo Boomerang > {0}", tempFile.FullName) });

                    using (var p = Runner.Run(si))
                    {
                        WaitForGoodExit(p);

                        var output = tempFile.ReadAllText();
                        Assert.Contains("Boomerang", output);
                    }
                }
            }

            [Fact]
            public void StartsProcessWithEnvironmentVariables()
            {
                using (var tempFile = CreateTempFile())
                {
                    var si = CreateRunSpec("cmd.exe", new[] { "/C", String.Format(@"set > {0}", tempFile.FullName) });
                    si.Environment["FOO"] = "BAR";
                    si.Environment["FOO2"] = "SNAFU";

                    using (var p = Runner.Run(si))
                    {
                        WaitForGoodExit(p);

                        var output = tempFile.ReadAllText();
                        Assert.Contains("BAR", output);
                        Assert.Contains("SNAFU", output);
                    }
                }
            }

            [Fact]
            public void StartsProcessWithSpecifiedWorkingDirectory()
            {
                using (var tempFile = CreateTempFile())
                {
                    var tempDirectory = tempFile.DirectoryName;
                    var si = CreateRunSpec("cmd.exe", new [] { "/C", String.Format(@"cd > {0}", tempFile.FullName) });
                    si.WorkingDirectory = tempDirectory;

                    using (var p = Runner.Run(si))
                    {
                        WaitForGoodExit(p);

                        var output = tempFile.ReadAllText();
                        Assert.Contains(tempDirectory.TrimEnd('\\'), output);
                    }
                }
            }

            [Fact]
            public void CanGetExitCodeFromCompletedProcess()
            {
                var si = CreateRunSpec("cmd.exe", new[] { "/S", "/C", "ping 127.0.0.1 -n 1 && exit" });
                si.BufferedInputOutput = true;

                using (var p = Runner.Run(si))
                {
                    WaitForGoodExit(p);
                    Assert.Equal(0, p.ExitCode);
                }
            }

            [Fact]
            public void WhenProcessExitsWithError_ExitCodeIsCorrect()
            {
                var si = CreateRunSpec("cmd.exe", new[] { "/c", "exit 10" });

                using (var p = Runner.Run(si))
                {
                    p.WaitForExit(2000);
                    Assert.Equal(10, p.ExitCode);
                    p.Kill();
                }
            }

            [Fact]
            public void CanGetEventedOutputFromProcess()
            {
                var spec = CreateRunSpec("cmd.exe", new[] { "/C", "echo This is STDOUT && echo This is STDERR >&2 && pause" });

                var output = new StringBuilder();
                var outputSignal = new ManualResetEvent(false);
                spec.OutputCallback = (data) => 
                {
                    output.Append(data);
                    if (!String.IsNullOrWhiteSpace(data))
                        outputSignal.Set();
                };

                var error = new StringBuilder();
                var errorSignal = new ManualResetEvent(false);
                spec.ErrorCallback = (data) =>
                {
                    error.Append(data);
                    if (!String.IsNullOrWhiteSpace(data))
                        errorSignal.Set();
                };

                using (var p = Runner.Run(spec))
                {
                    try
                    {
                        Assert.True(WaitHandle.WaitAll(new[] { outputSignal, errorSignal }, 2000));
                        Assert.Contains("This is STDOUT", output.ToString());
                        Assert.Contains("This is STDERR", error.ToString());
                    }
                    finally
                    {
                        p.Kill();
                    }
                }
            }

            [Fact]
            public void CanGetBufferedOutputFromProcess()
            {
                var spec = CreateRunSpec("cmd.exe", new[] { "/C", "echo This is STDOUT && echo This is STDERR >&2" });
                spec.BufferedInputOutput = true;

                using (var p = Runner.Run(spec))
                {
                    p.WaitForExit();
                    Assert.Contains("This is STDOUT", p.StandardOutput.ReadToEnd());
                    Assert.Contains("This is STDERR", p.StandardError.ReadToEnd());
                }
            }

            [Fact]
            public void WhenProcessFailsToStart_ThrowsException()
            {
                var si = CreateRunSpec("DoesNotExist.exe");

                var ex = Assert.Throws<System.ComponentModel.Win32Exception>(() => Runner.Run(si));
            }

            [FactAdminRequired]
            public async Task WhenCredentialsGiven_LoadsUserEnvironment()
            {
                var desktopPermissionManager = new DesktopPermissionManager();
                LocalPrincipalManager manager = new LocalPrincipalManager(desktopPermissionManager, "IIS_IUSRS");

                string userName = "Test_UserEnvironment";
                if (manager.FindUser(userName) != null)
                {
                    manager.DeleteUser(userName);
                }
                var user = manager.CreateUser(userName);
                desktopPermissionManager.AddDesktopPermission(userName);

                try
                {
                    var si = CreateRunSpec("cmd.exe", new[] {"/C", "set USERNAME"});
                    si.Credentials = user;
                    si.BufferedInputOutput = true;
                    si.WorkingDirectory = Environment.SystemDirectory;

                    using (var p = Runner.Run(si))
                    {
                        WaitForGoodExit(p, 1000);

                        var output = await p.StandardOutput.ReadToEndAsync();

                        string expectedUserName = string.Format("USERNAME={0}", user.UserName);
                        Assert.Contains(expectedUserName, output);
                    }
                }
                finally
                {
                    desktopPermissionManager.RemoveDesktopPermission(userName);
                    manager.DeleteUser(userName);
                }
            }

            [Fact]
            public void WhenCredentialsNotGiven_InheritsEnvironment()
            {
                var uniqueId = Guid.NewGuid().ToString("N");
                Environment.SetEnvironmentVariable(uniqueId, "FOO");

                using (var tempFile = CreateTempFile())
                {
                    var si = CreateRunSpec("cmd.exe", new[] { "/C", String.Format(@"set > {0}", tempFile.FullName) });

                    using (var p = Runner.Run(si))
                    {
                        WaitForGoodExit(p);

                        var output = tempFile.ReadAllText();

                        string expected= string.Format("{0}={1}", uniqueId, "FOO");
                        Assert.Contains(expected, output);
                    }
                }
            }

            [Fact]
            public void ReturnsProcessWithEnvironment()
            {
                var si = CreateRunSpec("cmd.exe", new[] { "/C" });

                var p = Runner.Run(si);

                Assert.NotNull(p.Environment);
                Assert.True(p.Environment.Count > 0);
                Assert.Equal(GetCurrentUserName(), p.Environment["USERNAME"]);
            }

            static string GetCurrentUserName()
            {
                // SYSTEM is a pseudo-user, the process is really running under the machine account 
                // and the username is MACHINENAME$
                var username = WindowsIdentity.GetCurrent().GetUserName();
                if (username.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase))
                    return Environment.MachineName + "$";

                return username;
            }
        }
    }
}
