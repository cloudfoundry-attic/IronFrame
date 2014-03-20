using System;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.PInvoke;
using IronFoundry.Warden.Shared.Messaging;
using IronFoundry.Warden.Utilities;
using Xunit;

namespace IronFoundry.Warden.Test
{
    public class ProcessManagerTests
    {
        [Fact]
        public void StoppingProcessManager_StopsProcesses()
        {
            var launcher = new ProcessLauncher();
            var manager = new ProcessManager(new JobObject(), new ProcessHelper(), launcher);

            var si = new CreateProcessStartInfo("cmd.exe");
            using (var process = manager.CreateProcess(si))
            {
                IntPtr pInt = process.Handle;
                manager.StopProcesses();
                
                uint exitCode = 0;
                NativeMethods.GetExitCodeProcess(pInt, out exitCode);

                Assert.NotEqual(
                    (uint)NativeMethods.ProcessExitCode.StillActive, 
                    exitCode);
            }
        }

        [Fact]
        public void ProcessManagerCreatesNamedJobObject()
        {
            var manager = new ProcessManager("SomeHandle");
            var jobHandle = new SafeJobObjectHandle(NativeMethods.OpenJobObject(NativeMethods.JobObjectAccessRights.AllAccess, false, "SomeHandle"));
            Assert.False(jobHandle.IsInvalid);
        }
    }
}
