using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFrame.Utilities;
using Microsoft.VisualBasic.ApplicationServices;
using Xunit;

namespace IronFrame.Acceptance.Test
{
    public class GuardAcceptanceTests : IDisposable
    {
        private readonly string username = ContainerAcceptanceTests.GenerateRandomAlphaString();
        private JobObject jobObject;
        private LocalPrincipalManager userManager;
        private Process guardProcess;

        public GuardAcceptanceTests()
        {
           this.userManager = new LocalPrincipalManager();
           userManager.CreateUser(username);

           this.jobObject = new JobObject(username);
           var guardExePath = Path.Combine(Directory.GetCurrentDirectory(), "Guard.exe");

           this.guardProcess = Process.Start(new ProcessStartInfo()
           {
               FileName = guardExePath,
               Arguments = String.Format("{0} {1}", username, username),
           });
        }

        [Fact]
        public void GuardDoesNotRetainReferenceToParentJobObject()
        {
            var st = new Stopwatch();
            st.Start();
            while (st.ElapsedMilliseconds < 5 * 1000)
            {
                try
                {
                    using (var gJobObject = new JobObject("Global\\" + username + "-guard", true))
                    {
                        break;
                    }
                }
                catch
                {
                    // retry
                }
            }

            Assert.False(guardProcess.HasExited);

            // Act
            this.jobObject.Dispose();

            // Assert
            guardProcess.WaitForExit(1000);
            Assert.True(guardProcess.HasExited);
        }

        public void Dispose()
        {
            jobObject.Dispose();
            userManager.DeleteUser(username);
            try
            {
                guardProcess.Kill();
            }
            catch(System.InvalidOperationException)
            {
            }
        }
    }
}
