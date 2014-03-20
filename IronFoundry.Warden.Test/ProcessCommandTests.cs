using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Shared.Messaging;
using IronFoundry.Warden.Protocol;
using NSubstitute;
using Xunit;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Tasks.Test
{
    public class ProcessCommandTests
    {
        private ContainerHandle handle;
        private IContainerUser user;
        private IContainerDirectory directory;
        private IProcess process;
        private Container container;
        
        public ProcessCommandTests()
        {
            handle = new ContainerHandle("TestContainerHandle");
            user = Substitute.For<IContainerUser>();
            user.UserName.Returns("TestUser");

            directory = Substitute.For<IContainerDirectory>();
            process = Substitute.For<IProcess>();
            container = Substitute.For<Container>(handle, user, directory, new ProcessManager(handle));
        }

        [Fact]
        public void WhenProcessCommandExcutes_DelegatesCreationToContainer()
        {
            container.CreateProcess(null).ReturnsForAnyArgs(process);
            var cmd = new TestableProcessCommand(container, "C:\\temp", "some.exe", "some args");

            cmd.Execute();

            container.Received().CreateProcess(Arg.Any<CreateProcessStartInfo>());
        }

        [Fact]
        public void WhenImpersonating_AssignsCredentials()
        {
            container.GetCredential().Returns(new System.Net.NetworkCredential("TestUser", "TestUserPassword"));

            CreateProcessStartInfo si  = null;
            container.CreateProcess(null).ReturnsForAnyArgs( c => 
            {
                si = c.Arg<CreateProcessStartInfo>();
                return process;
            });

            var cmd = new TestableProcessCommand(container, "C:\\temp", "some.exe", "some args", true);
            cmd.Execute();

            Assert.Equal("TestUser", si.UserName);
            Assert.Equal("TestUserPassword", si.Password.ToUnsecureString());
        }

        [Fact]
        public void AwaitsProcessExit()
        {
            container.CreateProcess(null).ReturnsForAnyArgs(process);
            
            var cmd = new TestableProcessCommand(container, "C:\\temp", "some.exe", "some args");
            cmd.Execute();

            process.Received().WaitForExit();            
        }

        [Fact]
        public void ReturnsResultWithExitCode()
        {
            container.CreateProcess(null).ReturnsForAnyArgs(process);
            process.ExitCode.Returns(100);

            var cmd = new TestableProcessCommand(container, "C:\\temp", "some.exe", "some args");
            var result = cmd.Execute();

            Assert.Equal(100, result.ExitCode);
        }

        [Fact]
        public void SetsTheWorkingDirectoryInStartInfo()
        {
            container.GetCredential().Returns(new System.Net.NetworkCredential("TestUser", "TestUserPassword"));

            CreateProcessStartInfo si = null;
            container.CreateProcess(null).ReturnsForAnyArgs(c =>
            {
                si = c.Arg<CreateProcessStartInfo>();
                return process;
            });

            var cmd = new TestableProcessCommand(container, "C:\\temp", "some.exe", "some args", true);
            cmd.Execute();

            Assert.Equal("C:\\temp", si.WorkingDirectory);
        }

        private class TestableProcessCommand : ProcessCommand
        {
            private string workingDirectory;
            private string exePath;
            private string exeArguments;

            public TestableProcessCommand(Container container, string workingDirectory, string exePath, string exeArguments, bool impersonate = false) : base(container, null, impersonate, null)
            {
                this.workingDirectory = workingDirectory;
                this.exePath = exePath;
                this.exeArguments = exeArguments;
            }
            protected override TaskCommandResult DoExecute()
            {
                return base.RunProcess(workingDirectory, exePath, exeArguments);
            }
        }
    }
}
