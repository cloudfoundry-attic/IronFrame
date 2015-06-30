using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using IronFrame.Utilities;
using NSubstitute;
using Xunit;

namespace IronFrame
{
    public class ContainerHostServiceTests
    {
        string ContainerId { get; set; }
        private ContainerHostDependencyHelper DependencyHelper { get; set; }
        IContainerDirectory Directory { get; set; }
        FileSystemManager FileSystem { get; set; }
        JobObject JobObject { get; set; }
        IProcess Process { get; set; }
        IProcessRunner ProcessRunner { get; set; }
        ContainerHostService Service { get; set; }
        
        public ContainerHostServiceTests()
        {
            ContainerId = Guid.NewGuid().ToString("N");

            Directory = Substitute.For<IContainerDirectory>();
            Directory.MapBinPath(null).ReturnsForAnyArgs(call => @"C:\Containers\handle\bin\" + call.Arg<string>());
            Directory.UserPath.Returns(@"C:\Containers\handle\user\");

            FileSystem = Substitute.For<FileSystemManager>();
            JobObject = Substitute.For<JobObject>();

            Process = Substitute.For<IProcess>();
            Process.Id.Returns(100);
            Process.Handle.Returns(new IntPtr(100));
            Process.StandardError.Returns(new StringReader("OK\n"));

            ProcessRunner = Substitute.For<IProcessRunner>();
            ProcessRunner.Run(null).ReturnsForAnyArgs(Process);

            DependencyHelper = Substitute.For<ContainerHostDependencyHelper>();
            DependencyHelper.ContainerHostExe.Returns("IronFrame.Host.exe");
            DependencyHelper.ContainerHostExePath.Returns(@"C:\Path\To\IronFrame.Host.exe");
            DependencyHelper.GetContainerHostDependencies().Returns(new [] { @"C:\Path\To\IronFrame.Shared.dll" });
            DependencyHelper.GuardExe.Returns("Guard.exe");
            DependencyHelper.GuardExePath.Returns(@"C:\Path\To\Guard.exe");

            FileSystem.FileExists(DependencyHelper.ContainerHostExeConfigPath).Returns(true);
            
            Service = new ContainerHostService(FileSystem, ProcessRunner, DependencyHelper);
        }

        [Fact]
        public void CopiesDependenciesToContainerBinDirectory()
        {
            IContainerHostClient client = null;
            try
            {
                client = Service.StartContainerHost(ContainerId, Directory, JobObject, null);
                
                FileSystem.Received(1).CopyFile(@"C:\Path\To\IronFrame.Host.exe", @"C:\Containers\handle\bin\IronFrame.Host.exe");
                FileSystem.Received(1).CopyFile(@"C:\Path\To\IronFrame.Host.exe.config", @"C:\Containers\handle\bin\IronFrame.Host.exe.config");
                FileSystem.Received(1).CopyFile(@"C:\Path\To\IronFrame.Shared.dll", @"C:\Containers\handle\bin\IronFrame.Shared.dll");

                FileSystem.Received(1).CopyFile(@"C:\Path\To\Guard.exe", @"C:\Containers\handle\bin\Guard.exe");
            }
            finally
            {
                if (client != null)
                    client.Shutdown();
            }
        }

        [Fact]
        public void CanStartContainerHost()
        {
            IContainerHostClient client = null;
            try
            {
                client = Service.StartContainerHost(ContainerId, Directory, JobObject, new NetworkCredential("username", "password"));

                ProcessRunner.Received(1).Run(
                    Arg.Is<ProcessRunSpec>(actual =>
                        actual.ExecutablePath == @"C:\Containers\handle\bin\IronFrame.Host.exe" &&
                        actual.Arguments.SequenceEqual(new[] { ContainerId }) &&
                        actual.WorkingDirectory == @"C:\Containers\handle\user\" &&
                        (actual.Credentials != null && actual.Credentials.UserName == "username" && actual.Credentials.Password == "password") &&
                        actual.BufferedInputOutput == true
                    ) 
                );
            }
            finally
            {
                if (client != null)
                    client.Shutdown();
            }
        }

        [Fact]
        public void WhenContainerHostFailsToStart_Throws()
        {
            Process.StandardError.Returns(new StringReader("Error message returned from IronFrame.Host.exe\n"));

            IContainerHostClient client = null;
            try
            {
                var ex = Record.Exception(() => client = Service.StartContainerHost("", Directory, JobObject, null));
                
                Assert.NotNull(ex);
                Assert.Contains("Error message returned from IronFrame.Host.exe", ex.Message);
            }
            finally
            {
                if (client != null)
                    client.Shutdown();
            }
        }

        [Fact]
        public void WhenContainerHostDoesNotStartWithinTimeout_Throws()
        {
        }

        [Fact]
        public void WhenCredentialsAreInvalid_Throws()
        {
            ProcessRunner.Run(null).ThrowsForAnyArgs(new SecurityException());

            IContainerHostClient client = null;
            try
            {
                var invalidCredentials = new NetworkCredential("InvalidUserName", "WrongPassword", Environment.MachineName);

                var ex = Record.Exception(() => client = Service.StartContainerHost(ContainerId, Directory, JobObject, invalidCredentials));

                Assert.IsAssignableFrom<SecurityException>(ex);
            }
            finally
            {
                if (client != null)
                    client.Shutdown();
            }
        }
    }
}
