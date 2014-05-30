using IronFoundry.Warden.Containers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IronFoundry.Warden.Test
{
    public class ContainerHostLauncherTests : IDisposable
    {
        private ContainerHostLauncher launcher;
        private string tempDirectory;

        public ContainerHostLauncherTests()
        {
            this.launcher = new ContainerHostLauncher();

            this.tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);
        }

        public void Dispose()
        {
            if (launcher != null && launcher.HostProcessId != 0)
            {
                using (var p = Process.GetProcessById(launcher.HostProcessId))
                {
                    p.Kill();
                    p.WaitForExit(3000);
                    launcher.Dispose();
                }
            }

            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory);
        }

        [Fact]
        public void CanLaunchHostProcess()
        {
            launcher.Start(tempDirectory, "JobObjectName");

            using (var p = Process.GetProcessById(launcher.HostProcessId))
            {
                Assert.False(p.HasExited);
            }
        }

        [Fact]
        public void WhenLaunchedReturnsActive()
        {
            launcher.Start(tempDirectory, "JobObjectName");

            using (var p = Process.GetProcessById(launcher.HostProcessId))
            {
                Assert.True(launcher.IsActive);
            }
        }

        [Fact]
        public void WhenStoppedReportsInactive()
        {
            using (var localLauncher = new ContainerHostLauncher())
            {
                localLauncher.Start(tempDirectory, "JobObjectName");

                using (var p = Process.GetProcessById(localLauncher.HostProcessId))
                {
                    p.Kill();
                    p.WaitForExit(3000);

                    Assert.False(launcher.IsActive);
                }
            }
        }
    }
}
