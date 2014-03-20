using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Shared.Messaging;
using IronFoundry.Warden.Utilities;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IronFoundry.Warden.Test
{
    public class ContainerTests
    {
        private IProcess returnedProcess;
        private ProcessManager processManager;
        private Container container;
        private string containerDirectoryPath = "c:\\somepath\\app";

        public ContainerTests()
        {
            var containerHandle = new ContainerHandle("TestContainerHandle");
            var containerUser = Substitute.For<IContainerUser>();
            var containerDirectory = Substitute.For<IContainerDirectory>();
            containerDirectory.FullName.Returns(containerDirectoryPath);

            returnedProcess = Substitute.For<IProcess>();
            processManager = Substitute.For<ProcessManager>("TestHandle");
            container = new Container(containerHandle, containerUser, containerDirectory, processManager);
        }

        [Fact]
        public void WhenCreatingProcess_ReturnsProcess()
        {
            CreateProcessStartInfo capturedStartInfo = null;
            processManager.CreateProcess(null).ReturnsForAnyArgs(c =>
            {
                capturedStartInfo = c.Arg<CreateProcessStartInfo>();
                return returnedProcess;
            });

            var si = new CreateProcessStartInfo("some.ext", "some arguments");
            var process = container.CreateProcess(si);

            Assert.Same(returnedProcess, process);
        }

        [Fact]
        public void WhenCreatingProcess_ForwardsExeAndArguments()
        {
            CreateProcessStartInfo capturedStartInfo = null;
            processManager.CreateProcess(null).ReturnsForAnyArgs(c =>
            {
                capturedStartInfo = c.Arg<CreateProcessStartInfo>();
                return returnedProcess;
            });

            var si = new CreateProcessStartInfo("some.ext", "some arguments");
            container.CreateProcess(si);

            Assert.Equal(si.FileName, capturedStartInfo.FileName);
            Assert.Equal(si.Arguments, capturedStartInfo.Arguments);
        }

        [Fact]
        public void WhenCreatingProcessWithoutImpersonation_DoesNotReplaceEnvironment()
        {
            CreateProcessStartInfo capturedStartInfo = null;
            processManager.CreateProcess(null).ReturnsForAnyArgs(c =>
            {
                capturedStartInfo = c.Arg<CreateProcessStartInfo>();
                return returnedProcess;
            });

            var si = new CreateProcessStartInfo("some.ext", "some arguments");
            si.EnvironmentVariables["One"] = "Alpha";

            container.CreateProcess(si);

            Assert.Equal("Alpha", capturedStartInfo.EnvironmentVariables["One"]);
        }

        [Fact]
        public void WhenCreatingProcessWithImpersonation_ClearsExistingEnvironment()
        {
            CreateProcessStartInfo capturedStartInfo = null;
            processManager.CreateProcess(null).ReturnsForAnyArgs(c =>
            {
                capturedStartInfo = c.Arg<CreateProcessStartInfo>();
                return returnedProcess;
            });

            var si = new CreateProcessStartInfo("some.ext", "some arguments");
            si.UserName = "TestUser";
            si.Password = "TestUserPassword".ToSecureString();

            si.EnvironmentVariables["One"] = "Alpha";

            container.CreateProcess(si);

            Assert.False(capturedStartInfo.EnvironmentVariables.ContainsKey("One"));
        }

        [Theory]
        [InlineData("Path")]
        [InlineData("SystemRoot")]
        [InlineData("SystemDrive")]
        [InlineData("windir")]
        [InlineData("PSModulePath")]
        [InlineData("ProgramData")]
        [InlineData("PATHEXT")]
        public void WhenCreatingProcessWithImpersonation_CopiesDefaultEnvironmentVariables(string environmentKey)
        {
            CreateProcessStartInfo capturedStartInfo = null;
            processManager.CreateProcess(null).ReturnsForAnyArgs(c =>
            {
                capturedStartInfo = c.Arg<CreateProcessStartInfo>();
                return returnedProcess;
            });

            var si = new CreateProcessStartInfo("some.ext", "some arguments");
            si.UserName = "TestUser";
            si.Password = "TestUserPassword".ToSecureString();

            container.CreateProcess(si);

            Assert.Equal(Environment.GetEnvironmentVariables()[environmentKey], capturedStartInfo.EnvironmentVariables[environmentKey]);
        }


        [Theory]
        [InlineData("APPDATA")]
        [InlineData("LOCALAPPDATA")]
        [InlineData("USERPROFILE")]
        [InlineData("TMP")]
        [InlineData("TEMP")]
        public void WhenCreatingProcessWithImpersonation_SetsTempDirectoryUnderContainerDirectory(string environmentKey)
        {            
            CreateProcessStartInfo capturedStartInfo = null;
            processManager.CreateProcess(null).ReturnsForAnyArgs(c =>
            {
                capturedStartInfo = c.Arg<CreateProcessStartInfo>();
                return returnedProcess;
            });

            var si = new CreateProcessStartInfo("some.ext", "some arguments");
            si.UserName = "TestUser";
            si.Password = "TestUserPassword".ToSecureString();

            container.CreateProcess(si);

            var expectedPath = System.IO.Path.Combine(containerDirectoryPath, "tmp");

            Assert.Equal(expectedPath, capturedStartInfo.EnvironmentVariables[environmentKey]);
        }

        [Fact]
        public void WhenDestroyingContainer_TerminatesRunningProcesses()
        {
            processManager.When(x => x.StopProcesses()).DoNotCallBase();
            processManager.When(x => x.Dispose()).DoNotCallBase();

            container.Destroy();

            processManager.Received().StopProcesses();
        }

        [Fact]
        public void WhenDestoryingContainer_DisposesProcessManager()
        {
            processManager.When(x => x.StopProcesses()).DoNotCallBase();
            processManager.When(x => x.Dispose()).DoNotCallBase();

            container.Destroy();

            processManager.Received().Dispose();
        }
    }
}
