using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Containers.Messages;
using IronFoundry.Warden.Shared.Data;
using IronFoundry.Warden.Shared.Messaging;
using IronFoundry.Warden.Tasks;
using IronFoundry.Warden.Test.TestSupport;
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
            protected IContainerHostLauncher launcher;
            protected string tempDirectory;
            protected IResourceHolder resourceHolder;

            public ProxyContainerContext()
            {
                this.launcher = Substitute.For<IContainerHostLauncher>();
                this.launcher.When(x => x.Start(null, null));

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

        public class BeforeInitialized : ProxyContainerContext
        {
            public BeforeInitialized()
            {
                launcher.IsActive.Returns(false);
            }

            [Fact]
            public async void GetStatisticsDoestNotCallHostStub()
            {                
                var response = await proxy.GetProcessStatisticsAsync();

                this.launcher.DidNotReceive(x => x.SendMessageAsync<ContainerStatisticsRequest, ContainerStatisticsResponse>(Arg.Any<ContainerStatisticsRequest>()));
            }

            [Fact]
            public async void GetStatisticsReturnsDefaultStats()
            {
                var response = await proxy.GetProcessStatisticsAsync();
                Assert.Equal(new ProcessStats(), response);
            }

            [Fact]
            public void RetrievingStateReturnsBorn()
            {
                ContainerState state = proxy.State;
                Assert.Equal(ContainerState.Born, state);
            }

            [Fact]
            public void RunCommandThrows()
            {
                var ex = ExceptionAssert.RecordThrowsAsync(async () => { await proxy.RunCommandAsync(new RemoteCommand(false, "blah")); });
                Assert.IsType<InvalidOperationException>(ex.Result);
            }

            [Fact]
            public async void DestroyDoesNotSendToStub()
            {
                await proxy.DestroyAsync();
                this.launcher.DidNotReceive(x => x.SendMessageAsync<ContainerDestroyRequest, ContainerDestroyResponse>(Arg.Any<ContainerDestroyRequest>()));
            }

            [Fact]
            public async void LimitMemoryDoesNotSendToStub()
            {
                await proxy.LimitMemoryAsync(1024);
                this.launcher.DidNotReceive(x => x.SendMessageAsync<LimitMemoryRequest, LimitMemoryResponse>(Arg.Any<LimitMemoryRequest>()));
            }
        }
        
        public class WhenInitialized : ProxyContainerContext
        {
            public WhenInitialized() : base()
            {
                launcher.IsActive.Returns(true);
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
            public async void RemovesResourcesOnDestroy()
            {
                await proxy.DestroyAsync();
                this.resourceHolder.Received(x => x.Destroy());
            }

            [Fact]
            public async void SendsDestroyMessageToStubOnDestroy()
            {
                await proxy.DestroyAsync();
                launcher.Received(x => x.SendMessageAsync<ContainerDestroyRequest, ContainerDestroyResponse>(Arg.Any<ContainerDestroyRequest>()));
            }

            [Fact]
            public async void DestroySetsStateToDestroy()
            {
                launcher.IsActive.Returns(false);
                await proxy.DestroyAsync();
                Assert.Equal(ContainerState.Destroyed, proxy.State);
            }

            [Fact]
            public async void StopSendsDestroyMessageToStubOnDestroy()
            {
                await proxy.StopAsync();
                launcher.Received(x => x.SendMessageAsync<ContainerDestroyRequest, ContainerDestroyResponse>(Arg.Any<ContainerDestroyRequest>()));
            }

            [Fact]
            public async void StopDestroysResourceHolder()
            {
                await proxy.StopAsync();
                resourceHolder.Received(x => x.Destroy());
            }

            [Fact]
            public async void StopDestroySetsStateToDestroy()
            {
                launcher.IsActive.Returns(false);
                await proxy.StopAsync();
                Assert.Equal(ContainerState.Destroyed, proxy.State);
            }

            [Fact]
            public async void GetStatisticsSendsMessageToHost()
            {
                this.launcher.SendMessageAsync<ContainerStatisticsRequest, ContainerStatisticsResponse>(Arg.Any<ContainerStatisticsRequest>()).ReturnsTask(new ContainerStatisticsResponse("", new Shared.Data.ProcessStats()));
                var response = await proxy.GetProcessStatisticsAsync();

                this.launcher.Received(x => x.SendMessageAsync<ContainerStatisticsRequest, ContainerStatisticsResponse>(Arg.Any<ContainerStatisticsRequest>()));
            }

            [Fact]
            public async void EnableLoggingAsyncSendsMessageToHost()
            {
                this.launcher.SendMessageAsync<EnableLoggingRequest, EnableLoggingResponse>(Arg.Any<EnableLoggingRequest>()).ReturnsTask(new EnableLoggingResponse(""));
                await proxy.EnableLoggingAsync(new InstanceLoggingInfo());

                this.launcher.Received(x => x.SendMessageAsync<EnableLoggingRequest, EnableLoggingResponse>(Arg.Any<EnableLoggingRequest>()));
            }

            [Fact]
            public async void LimitMemorySendsMessageToHost()
            {
                this.launcher.SendMessageAsync<LimitMemoryRequest, LimitMemoryResponse>(Arg.Any<LimitMemoryRequest>()).ReturnsTask(new LimitMemoryResponse(""));
                await proxy.LimitMemoryAsync(1024);

                this.launcher.Received(
                    1, 
                    x => x.SendMessageAsync<LimitMemoryRequest, LimitMemoryResponse>(
                        Arg.Is<LimitMemoryRequest>(
                            request => request.@params.LimitInBytes == 1024)));
            }
        }

        public class WhenLauncherExits : ProxyContainerContext
        {
            [Fact]
            public void OutOfMemoryExitResultsInEventEntry()
            {
                var exitCode =-2;
                this.launcher.HostStopped += Raise.Event<EventHandler<int>>(new object(), exitCode);

                Assert.Equal(new[] { "Application exceeded memory limits and was stopped." }, proxy.DrainEvents());
            }

            [Fact]
            public void ExitWithoutErrorResultsInNoEvents()
            {
                var exitCode = 0;
                this.launcher.HostStopped += Raise.Event<EventHandler<int>>(new object(), exitCode);

                Assert.Equal(new string[] { }, proxy.DrainEvents());
            }

            [Fact]
            public void ExitWithUnmappedCodeResultsInGenericMessage()
            {
                var exitCode = 9000;
                this.launcher.HostStopped += Raise.Event<EventHandler<int>>(new object(), exitCode);

                Assert.Equal(new[] { string.Format("Application's ContainerHost stopped with exit code: {0}.", exitCode) }, proxy.DrainEvents());
            }

            [Fact]
            public void EventsClearedAfterDrain()
            {
                var exitCode = 9000;
                this.launcher.HostStopped += Raise.Event<EventHandler<int>>(new object(), exitCode);

                Assert.Equal(1, proxy.DrainEvents().Count());

                Assert.Equal(0, proxy.DrainEvents().Count());
            }
        }

        public class WhenQueryingContainerState : ProxyContainerContext
        {
            public WhenQueryingContainerState()
            {
                launcher.IsActive.Returns(true);
            }

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
                launcher.IsActive.Returns(true);
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

        public class WhenLauncherEndsAfterInitialize : ProxyContainerContext
        {
            public WhenLauncherEndsAfterInitialize()
            {
                launcher.IsActive.Returns(true);
                
                launcher.SendMessageAsync<ContainerStateRequest, ContainerStateResponse>(Arg.Any<ContainerStateRequest>()).ReturnsTask(new ContainerStateResponse("foo", ContainerState.Active.ToString()));

                proxy.Initialize(resourceHolder);
            }

            [Fact]
            public void ShouldReportStoppedStatus()
            {
                var state = proxy.State;
                Assert.Equal(ContainerState.Active, state);

                launcher.IsActive.Returns(false);
                launcher.WasActive.Returns(true);

                state = proxy.State;
                Assert.Equal(ContainerState.Stopped, state);
            }

        }
        
        public class TestableContainerHostLauncher : ContainerHostLauncher
        {
            public void RaiseOnHostStopped(int exitCode)
            {
                this.OnHostStopped(exitCode);
            }
        }
    }
}
