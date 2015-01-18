using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Warden.Protocol;
using IronFoundry.Warden.Utilities;
using NSubstitute;
using Xunit;

namespace IronFoundry.Warden.Tasks.Test
{
    public class ProcessCommandTests : IDisposable
    {
        private readonly string tempDir;
        private ContainerHandle handle;
        private IContainerUser user;
        private IContainerDirectory directory;
        private IProcess process;
        private IContainer container;
        
        public ProcessCommandTests()
        {
            // Create a temporary directory to use as the container root
            tempDir = CreateTempDir();
 
            handle = new ContainerHandle("TestContainerHandle");
            user = Substitute.For<IContainerUser>();
            user.UserName.Returns("TestUser");
            user.GetCredential().ReturnsForAnyArgs(new System.Net.NetworkCredential("TestUser", "TestUserPassword"));

            directory = Substitute.For<IContainerDirectory>();
            process = Substitute.For<IProcess>();
            container = Substitute.For<IContainer>();

            container.ContainerDirectoryPath.Returns(tempDir);
        }

        public void Dispose()
        {
            Directory.Delete(tempDir, true);
        }


        [Fact]
        public void WhenProcessCommandExcutes_DelegatesCreationToContainer()
        {
            container.CreateProcess(null).ReturnsForAnyArgs(process);
            var cmd = new TestableProcessCommand(container, tempDir, "some.exe", "some args");

            cmd.Execute();

            container.Received().CreateProcess(Arg.Any<CreateProcessStartInfo>());
        }

        [Fact]
        public void WhenNotPrivileged_ShouldImpersonateRestrictedUser()
        {
            CreateProcessStartInfo si  = null;
            container.CreateProcess(null).ReturnsForAnyArgs( c => 
            {                
                si = c.Arg<CreateProcessStartInfo>();
                return process;
            });

            var cmd = new TestableProcessCommand(container, tempDir, "some.exe", "some args", false);
            cmd.Execute();

            container.Received().CreateProcess(Arg.Any<CreateProcessStartInfo>(), true);
        }

        [Fact]
        public void AwaitsProcessExit()
        {
            container.CreateProcess(null).ReturnsForAnyArgs(process);
            
            var cmd = new TestableProcessCommand(container, tempDir, "some.exe", "some args");
            cmd.Execute();

            process.Received().WaitForExit();
        }

        [Fact]
        public void ReturnsResultWithExitCode()
        {
            container.CreateProcess(null).ReturnsForAnyArgs(process);
            process.ExitCode.Returns(100);

            var cmd = new TestableProcessCommand(container, tempDir, "some.exe", "some args");
            var result = cmd.Execute();

            Assert.Equal(100, result.ExitCode);
        }

        [Fact]
        public void SetsTheWorkingDirectoryInStartInfo()
        {
            CreateProcessStartInfo si = null;
            container.CreateProcess(null).ReturnsForAnyArgs(c =>
            {
                si = c.Arg<CreateProcessStartInfo>();
                return process;
            });

            var cmd = new TestableProcessCommand(container, tempDir, "some.exe", "some args", true);
            cmd.Execute();

            Assert.Equal(tempDir, si.WorkingDirectory);
        }

        /// <summary>
        /// Validate that the env log is created while the process is running.
        /// </summary>
        [Fact]
        public void CreatesEnvLog()
        {
            string envLogContent = InvokeCommandAndGetLogContent(ProcessCommand.EnvLogRelativePath);

            envLogContent.Should().Contain("env1")
                .And.Contain("val1")
                .And.Contain("val2");
        }

        /// <summary>
        /// Validate that the pid log is created while the process is running.
        /// </summary>
        [Fact]
        public void CreatesPidLog()
        {
            string pidLogContent = InvokeCommandAndGetLogContent(ProcessCommand.PidLogRelativePath);

            pidLogContent.Should().Contain("100");
        }

        /// <summary>
        /// Validate that the ProcessCommand translates expands environment values starting with @ROOT@
        /// replacing it with the root of the container.
        /// </summary>
        [Fact]
        public void RootsEnvironmentValues()
        {
            string content = InvokeCommandAndGetLogContent(ProcessCommand.EnvLogRelativePath);

            string expectedPath = Path.Combine(tempDir, "sub\\dir");
            content.Should().Contain(expectedPath);
        }

        private string InvokeCommandAndGetLogContent(string logRelativePath)
        {
            string content = null;

            // When Wait for exit is called, get the content of the env log.
            string expectedEnvPath = Path.Combine(tempDir, logRelativePath);
            process.When(p => p.WaitForExit()).Do(c => content = File.ReadAllText(expectedEnvPath));
            process.Id.Returns(100);

            container.ContainerDirectoryPath.Returns(tempDir);
            container.CreateProcess(null).ReturnsForAnyArgs(process);

            var env = new Dictionary<string, string> { { "env1", "val1" }, { "env2", "val2" }, { "rootedenv", "@ROOT@\\sub\\dir"} };
            var cmd = new TestableProcessCommand(container, tempDir, "some.exe", "some args", environment: env);

            cmd.Execute();

            return content;
        }

        private string CreateTempDir()
        {
            string tempPath = Path.GetTempFileName();
            File.Delete(tempPath);
            Directory.CreateDirectory(tempPath);

            return tempPath;
        }

        private class TestableProcessCommand : ProcessCommand
        {
            private string workingDirectory;
            private string exePath;
            private string exeArguments;

            public TestableProcessCommand(IContainer container, string workingDirectory, string exePath, string exeArguments, bool privileged = true, IDictionary<string, string> environment = null) 
                : base(container, null, privileged, environment, null)
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
