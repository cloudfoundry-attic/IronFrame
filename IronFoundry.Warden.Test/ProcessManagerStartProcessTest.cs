using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Shared.Messaging;
using IronFoundry.Warden.Utilities;
using NSubstitute;
using Xunit;

namespace IronFoundry.Warden.Utilities
{
    public class ProcessManagerStartProcessTest : IDisposable
    {
        ProcessLauncher launcher;
        ProcessManager manager;
        IProcess process;
        JobObject jobObject;

        public ProcessManagerStartProcessTest()
        {
            launcher = Substitute.For<ProcessLauncher>();
            process = Substitute.For<IProcess>();
            process.Id.Returns(100);

            jobObject = Substitute.For<JobObject>();
            jobObject.GetProcessIds().Returns(new[] { 100 });
            
            launcher.LaunchProcess(null, null).ReturnsForAnyArgs(process);
            launcher.When(x => x.Dispose()).DoNotCallBase();

            manager = new ProcessManager(jobObject, launcher);
        }

        public void Dispose()
        {
            manager.Dispose();
        }

        [Fact]
        public void StartsProcessInJobObject()
        {
            var si = new CreateProcessStartInfo("cmd.exe");
            var createdProcess = manager.CreateProcess(si);

            launcher.Received().LaunchProcess(si, Arg.Is<JobObject>(x => x != null));
            Assert.NotNull(createdProcess);
        }

        [Fact]
        public void TracksStartedProcesses()
        {
            var si = new CreateProcessStartInfo("cmd.exe");
            var createdProcess = manager.CreateProcess(si);

            Assert.True(manager.ContainsProcess(createdProcess.Id));
        }

        [Fact]
        public void WhenTheProcessIsNotContained_ContainsProcessReturnsFalse()
        {
            Assert.False(manager.ContainsProcess(1));
        }

        [Fact]
        public void DisposesLauncher()
        {
            manager.Dispose();

            launcher.Received().Dispose();
        }
    }
}
