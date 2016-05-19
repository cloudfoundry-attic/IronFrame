using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using IronFrame.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace IronFrame
{
    public class ImpersonationProcessRunnerTests : IDisposable
    {
        private LocalPrincipalManager userManager;
        private string username;
        private NetworkCredential creds;
        private ITestOutputHelper logger;

        public ImpersonationProcessRunnerTests(ITestOutputHelper helper)
        {
            logger = helper;
            username = GenerateRandomAlphaString();
            helper.WriteLine(username);
            userManager = new LocalPrincipalManager("IIS_IUSRS");
            userManager.DeleteUser(username);
            creds = userManager.CreateUser(username);
        }

        public void Dispose()
        {
            userManager.DeleteUser(username);
        }

        [Fact]
        public void TestStdout()
        {
            var runner = new ImpersonationProcessRunner();
            var spec = new ProcessRunSpec()
            {
                ExecutablePath = "whoami",
                BufferedInputOutput = true,
                Credentials = creds
            };
            var proc = runner.Run(spec);
            Assert.Matches(new Regex(username), proc.StandardOutput.ReadToEnd());
        }

        [Fact]
        public void TestStderr()
        {
            var runner = new ImpersonationProcessRunner();
            var spec = new ProcessRunSpec()
            {
                ExecutablePath = "powershell",
                Arguments = new[] { "-Command", "[Console]::Error.WriteLine('hi')" },
                BufferedInputOutput = true,
                Credentials = creds
            };
            var proc = runner.Run(spec);
            Assert.Equal("hi", proc.StandardError.ReadToEnd().Trim());
        }

        [Fact]
        public void TestStdin()
        {
            var runner = new ImpersonationProcessRunner();
            var spec = new ProcessRunSpec()
            {
                ExecutablePath = "powershell",
                Arguments = new[] { "-Command", "$x = [Console]::In.ReadLine(); [Console]::Out.WriteLine($x)" },
                BufferedInputOutput = true,
                Credentials = creds
            };
            var proc = runner.Run(spec);
            proc.StandardInput.WriteLine("hi");
            Assert.Equal("", proc.StandardError.ReadToEnd().Trim());
            Assert.Equal("hi", proc.StandardOutput.ReadToEnd().Trim());
        }

        [DllImport("kernel32.dll")]
        static extern int GetProcessId(IntPtr handle);

        [Fact]
        public void TestHandle()
        {
            var runner = new ImpersonationProcessRunner();
            var spec = new ProcessRunSpec()
            {
                ExecutablePath = "powershell",
                Arguments = new[] { "-Command", "$x = [Console]::In.ReadLine()" },
                BufferedInputOutput = true,
                Credentials = creds
            };
            var proc = runner.Run(spec);
            Assert.NotNull(proc.Handle);
            var pid = GetProcessId(proc.Handle);
            var realProc = Process.GetProcessById(pid);
            try
            {
                Assert.NotNull(realProc);
                Assert.Equal("powershell", realProc.ProcessName);
            }
            finally
            {
                realProc.Kill();
            }
        }

        [Fact]
        public void TestExited()
        {
            var exited = false;
            var runner = new ImpersonationProcessRunner();
            var spec = new ProcessRunSpec()
            {
                ExecutablePath = "whoami",
                BufferedInputOutput = true,
                Credentials = creds,
                ExitHandler = (sender, args) => exited = true
            };
            var process = runner.Run(spec);
            process.WaitForExit();
            Assert.True(exited);
        }
        private static string GenerateRandomAlphaString(int length = 8)
        {
            const string alphabet = "abcdefghijklmnopqrstuvwxyz";

            var r = RandomFactory.Create();
            var handle = "";
            for (var count = 0; count < length; count++)
            {
                var chosenCharIndex = r.Next(0, alphabet.Length);
                handle += alphabet[chosenCharIndex];
            }

            return handle;
        }
    }
}
