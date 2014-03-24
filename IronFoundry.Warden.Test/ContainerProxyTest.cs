using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Shared.Messaging;
using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Utilities;
using NSubstitute;
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
    public class ContainerProxyTest
    {
        public class ProxyContainerContext : IDisposable
        {
            protected readonly string testUserName = "TestUser";
            protected readonly string testUserPassword = "TestUserPassword";
            protected readonly string containerHandle = "ContainerHandle";

            protected ContainerProxy proxy;
            protected ContainerHostLauncher launcher;
            protected string tempDirectory;
            protected IResourceHolder resourceHolder;

            public ProxyContainerContext()
            {
                this.launcher = Substitute.For<ContainerHostLauncher>();
                this.launcher.When(x => x.Start(null, null)).DoNotCallBase();

                var userInfo = Substitute.For<IContainerUser>();
                userInfo.UserName.ReturnsForAnyArgs(testUserName);
                userInfo.GetCredential().ReturnsForAnyArgs(new System.Net.NetworkCredential(testUserName, testUserPassword));

                this.tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(this.tempDirectory);

                var containerDirectory = Substitute.For<IContainerDirectory>();
                containerDirectory.FullName.Returns(this.tempDirectory);

                var jobObject = Substitute.For<JobObject>();

                this.resourceHolder = Substitute.For<IResourceHolder>();
                this.resourceHolder.User.Returns(userInfo);
                this.resourceHolder.Directory.Returns(containerDirectory);
                this.resourceHolder.JobObject.Returns(jobObject);

                this.resourceHolder.Handle.Returns(new ContainerHandle(containerHandle));

                this.proxy = new ContainerProxy(launcher);
            }

            public virtual void Dispose()
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, true);
            }
        }

        public class WhenInitialized : ProxyContainerContext
        {
            public WhenInitialized() : base()
            {
                proxy.Initialize(resourceHolder);
            }

            [Fact]
            public void LaunchesContainerHostProcess()
            {
                launcher.Received().Start(Arg.Any<string>(), containerHandle);
            }

            [Fact]
            public void LaunchesProcessWithContainerWorkingDirectory()
            {
                launcher.Received().Start(tempDirectory, containerHandle);
            }

            [Fact]
            public void SendsInitializationToStub()
            {
                launcher.Received(x => x.SendMessageAsync<ContainerInitializeRequest, ContainerInitializeResponse>(Arg.Any<ContainerInitializeRequest>()));
            }

            [Fact]
            public void SendContainerPathToStub()
            {
                ContainerInitializeParameters initializationParams = null;
                launcher.SendMessageAsync<ContainerInitializeRequest, ContainerInitializeResponse>(null).ReturnsForAnyArgs(c =>
                {
                    var request = c.Arg<ContainerInitializeRequest>();
                    initializationParams = request.@params;
                    return Task.FromResult<ContainerInitializeResponse>(null);
                });

                proxy.Initialize(resourceHolder);

                Assert.Equal(tempDirectory, initializationParams.containerDirectoryPath);

            }

            [Fact]
            public void SendContainerHandleToStub()
            {
                ContainerInitializeParameters initializationParams = null;
                launcher.SendMessageAsync<ContainerInitializeRequest, ContainerInitializeResponse>(null).ReturnsForAnyArgs(c =>
                {
                    var request = c.Arg<ContainerInitializeRequest>();
                    initializationParams = request.@params;
                    return Task.FromResult<ContainerInitializeResponse>(null);
                });
                proxy.Initialize(resourceHolder);

                Assert.Equal(containerHandle, initializationParams.containerHandle);
            }

            [Fact]
            public void SendsUserInfoToStub()
            {
                ContainerInitializeParameters initializationParams = null;
                launcher.SendMessageAsync<ContainerInitializeRequest, ContainerInitializeResponse>(null).ReturnsForAnyArgs(c =>
                {
                    var request = c.Arg<ContainerInitializeRequest>();
                    initializationParams = request.@params;
                    return Task.FromResult<ContainerInitializeResponse>(null);
                });
                proxy.Initialize(resourceHolder);

                Assert.Equal(testUserName, initializationParams.userName);
                Assert.Equal(testUserPassword, initializationParams.userPassword.ToUnsecureString());
            }

            [Fact]
            public void CacheContainerDirectory()
            {
                proxy.Initialize(resourceHolder);
                Assert.Equal(tempDirectory, proxy.ContainerDirectoryPath);
            }

            [Fact]
            public void CachesUserInfo()
            {
                proxy.Initialize(resourceHolder);

                Assert.Equal(testUserName, proxy.ContainerUserName);
            }

            [Fact]
            public void CachesContainerHandle()
            {
                Assert.Equal(containerHandle, proxy.Handle.ToString());
            }

            [Fact]
            public void RemovesResourcesOnDestroy()
            {
                proxy.Destroy();
                this.resourceHolder.Received(x => x.Destroy());
            }

            [Fact]
            public void SendsDestroyMessageToStubOnDestroy()
            {
                proxy.Destroy();
                launcher.Received(x => x.SendMessageAsync<ContainerDestroyRequest, ContainerDestroyResponse>(Arg.Any<ContainerDestroyRequest>()));
            }
        }

        public class WhenQueryingContainerState : ProxyContainerContext
        {
            [Fact]
            public void WhenQueryingContainerState_ShouldQueryContainerHost()
            {
                this.launcher.SendMessageAsync<ContainerStateRequest, ContainerStateResponse>(Arg.Any<ContainerStateRequest>()).ReturnsTask(new ContainerStateResponse("",""));
                ContainerState state = proxy.State;

                this.launcher.Received(x => x.SendMessageAsync<ContainerStateRequest, ContainerStateResponse>(Arg.Any<ContainerStateRequest>()));
            }

            [Fact]
            public void WhenQueryingContainerSate_ShouldReturnMatchingValueFromContainerHost()
            {
                this.launcher.SendMessageAsync<ContainerStateRequest, ContainerStateResponse>(Arg.Any<ContainerStateRequest>())
                    .ReturnsTask(new ContainerStateResponse("", "Active"));
                ContainerState state = proxy.State;

                Assert.Equal(ContainerState.Active, state);
            }

        }

        public class WhenRunningCommand : ProxyContainerContext
        {
            public WhenRunningCommand()
            {
            }

            [Fact]
            public async void WhenRunningCommand_ShouldSendRunCommandRequestToHost()
            {
                this.launcher.SendMessageAsync<RunCommandRequest, RunCommandResponse>(Arg.Any<RunCommandRequest>()).ReturnsTask(new RunCommandResponse("", new RunCommandResponseData()));

                var response = await proxy.RunCommandAsync(new RemoteCommand(false, "test", "test"));

                this.launcher.Received(x => x.SendMessageAsync<RunCommandRequest, RunCommandResponse>(Arg.Any<RunCommandRequest>()));
            }

            [Fact]
            public async void WhenRunningCommand_ShouldSendRunCommandRequestWithCommandToHost()
            {
                this.launcher.SendMessageAsync<RunCommandRequest, RunCommandResponse>(Arg.Any<RunCommandRequest>()).ReturnsTask(new RunCommandResponse("", new RunCommandResponseData()));
                var command = new RemoteCommand(false, "tar", "foo.zip");

                var response = await proxy.RunCommandAsync(command);

                this.launcher.Received(x => x.SendMessageAsync<RunCommandRequest, RunCommandResponse>(Arg.Is<RunCommandRequest>(y => y.@params.command == command.Command && y.@params.arguments == command.Arguments)));
            }

            [Fact]
            public async void WhenRunningCommand_ExitCodeShouldBeReturned()
            {
                this.launcher.SendMessageAsync<RunCommandRequest, RunCommandResponse>(Arg.Any<RunCommandRequest>()).ReturnsTask(new RunCommandResponse("", new RunCommandResponseData() { exitCode = 10 }));
                var command = new RemoteCommand(false, "tar", "foo.zip");

                var response = await proxy.RunCommandAsync(command);

                Assert.Equal(10, response.ExitCode);
            }

            [Fact]
            public async void WhenRunningCommand_StdOutIsReturned()
            {
                this.launcher.SendMessageAsync<RunCommandRequest, RunCommandResponse>(Arg.Any<RunCommandRequest>()).ReturnsTask(new RunCommandResponse("", new RunCommandResponseData(){ exitCode = 0, stdOut = "StdOutMessage" }));
                var command = new RemoteCommand(false, "tar", "foo.zip");

                var response = await proxy.RunCommandAsync(command);

                Assert.Equal("StdOutMessage", response.StdOut);
            }

            [Fact]
            public async void WhenRunningCommand_StdErrIsReturned()
            {
                this.launcher.SendMessageAsync<RunCommandRequest, RunCommandResponse>(Arg.Any<RunCommandRequest>()).ReturnsTask(new RunCommandResponse("", new RunCommandResponseData(){ exitCode = 0, stdErr = "StdErrMessage" }));
                var command = new RemoteCommand(false, "tar", "foo.zip");

                var response = await proxy.RunCommandAsync(command);

                Assert.Equal("StdErrMessage", response.StdErr);
            }
        }

        public class WhenDisposed : ProxyContainerContext
        {
            [Fact]
            public void WhenDisposed_DisposesLauncher()
            {
                proxy.Dispose();
                launcher.Received().Dispose();
            }
        
        }
    }
}
