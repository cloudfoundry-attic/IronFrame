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
    public class ProcessHostLauncherTests : IDisposable
    {
        private ContainerHostLauncher launcher;
        private string tempDirectory;
        
        public ProcessHostLauncherTests()
        {
            this.launcher = new ContainerHostLauncher();

            this.tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);
        }

        public void Dispose()
        {
            using (var p = Process.GetProcessById(launcher.HostProcessId))
            {
                p.Kill();
                p.WaitForExit(3000);
                launcher.Dispose();
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
    }
}
