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

            public ProxyContainerContext()
            {
                this.launcher = Substitute.For<IContainerHostLauncher>();
                this.launcher.When(x => x.Start(null, null));

                this.tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(this.tempDirectory);

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
            public async void BindMountsThrows()
            {
                var ex = await ExceptionAssert.RecordThrowsAsync(async () => await proxy.BindMountsAsync(new BindMount[0]));
                Assert.IsType<InvalidOperationException>(ex);
            }

            [Fact]
            public async void GetInfoReturnsDefaultInfo()
            {
                var info = await proxy.GetInfoAsync();

                Assert.Equal(new ContainerInfo(), info);
            }

            [Fact]
            public async void GetInfoReturnsBornState()
            {
                var info = await proxy.GetInfoAsync();

                Assert.Equal(ContainerState.Born, info.State);
            }

            [Fact]
            public async void WhenContainerHasEvents_GetInfoReturnsEvents()
            {
                launcher.HostStopped += Raise.Event<EventHandler<int>>(this, 100);

                var info = await proxy.GetInfoAsync();

                Assert.Collection(info.Events,
                    x => Assert.Equal("Application's ContainerHost stopped with exit code: 100.", x));
            }

            [Fact]
            public async void RunCommandThrows()
            {
                var ex = await ExceptionAssert.RecordThrowsAsync(async () => { await proxy.RunCommandAsync(new RemoteCommand(false, "blah")); });
                Assert.IsType<InvalidOperationException>(ex);
            }

            [Fact]
            public async void StopDoesNotSendToStub()
            {
                await proxy.StopAsync(false);
                this.launcher.DidNotReceive(x => x.SendMessageAsync<StopRequest, StopResponse>(Arg.Any<StopRequest>()));
            }

            [Fact]
            public async void LimitMemoryDoesNotSendToStub()
            {
                await proxy.LimitMemoryAsync(1024);
                this.launcher.DidNotReceive(x => x.SendMessageAsync<LimitMemoryRequest, LimitMemoryResponse>(Arg.Any<LimitMemoryRequest>()));
            }
        }

        public class ContainerInitializedContext : ProxyContainerContext
        {
            public ContainerInitializedContext()
            {
                launcher.IsActive.Returns(true);
                launcher.SendMessageAsync<ContainerInitializeRequest, ContainerInitializeResponse>(Arg.Any<ContainerInitializeRequest>())
                    .ReturnsTask(new ContainerInitializeResponse("", tempDirectory));
            }

            protected virtual async Task CompleteInitializationAsync()
            {
                await proxy.InitializeAsync(tempDirectory, containerHandle);
            }
        }

        public class OnStop : ContainerInitializedContext
        {
           
            [Fact]
            public async void SendsStopMessageToStub()
            {
                await CompleteInitializationAsync();

                await proxy.StopAsync(false);

                launcher.Received(x => x.SendMessageAsync<StopRequest, StopResponse>(Arg.Any<StopRequest>()));
            }

            [Fact]
            public async void StopsLauncher()
            {
                await CompleteInitializationAsync();

                await proxy.StopAsync(false);

                launcher.Received(x => x.Stop());
            }
        }

        public class WhenInitialized : ContainerInitializedContext
        {
            [Fact]
            public async void LaunchesContainerHostProcess()
            {
                await CompleteInitializationAsync();

                launcher.Received().Start(Arg.Any<string>(), containerHandle);
            }

            [Fact]
            public async void LaunchesProcessWithContainerWorkingDirectory()
            {
                await CompleteInitializationAsync();

                launcher.Received().Start(tempDirectory, containerHandle);
            }

            [Fact]
            public async void SendsInitializationToStub()
            {
                await CompleteInitializationAsync();

                launcher.Received(x => x.SendMessageAsync<ContainerInitializeRequest, ContainerInitializeResponse>(Arg.Any<ContainerInitializeRequest>()));
            }

            [Fact]
            public async void SendContainerPathToStub()
            {
                ContainerInitializeParameters initializationParams = null;
                launcher.SendMessageAsync<ContainerInitializeRequest, ContainerInitializeResponse>(null).ReturnsTaskForAnyArgs(c =>
                {
                    var request = c.Arg<ContainerInitializeRequest>();
                    initializationParams = request.@params;
                    return new ContainerInitializeResponse("", tempDirectory);
                });

                await CompleteInitializationAsync();

                Assert.Equal(tempDirectory, initializationParams.containerBaseDirectoryPath);
            }

            [Fact]
            public async void SendContainerHandleToStub()
            {
                ContainerInitializeParameters initializationParams = null;
                launcher.SendMessageAsync<ContainerInitializeRequest, ContainerInitializeResponse>(null).ReturnsTaskForAnyArgs(c =>
                {
                    var request = c.Arg<ContainerInitializeRequest>();
                    initializationParams = request.@params;
                    return new ContainerInitializeResponse("", tempDirectory);
                });

                await CompleteInitializationAsync();

                Assert.Equal(containerHandle, initializationParams.containerHandle);
            }

            [Fact]
            public async void CacheContainerDirectory()
            {
                await CompleteInitializationAsync();

                Assert.Equal(tempDirectory, proxy.ContainerDirectoryPath);
            }

            [Fact]
            public async void CachesContainerHandle()
            {
                await CompleteInitializationAsync();

                Assert.Equal(containerHandle, proxy.Handle.ToString());
            }

            [Fact]
            public async void EnableLoggingAsyncSendsMessageToHost()
            {
                await CompleteInitializationAsync();
                this.launcher.SendMessageAsync<EnableLoggingRequest, EnableLoggingResponse>(Arg.Any<EnableLoggingRequest>()).ReturnsTask(new EnableLoggingResponse(0));

                await proxy.EnableLoggingAsync(new InstanceLoggingInfo());

                this.launcher.Received(x => x.SendMessageAsync<EnableLoggingRequest, EnableLoggingResponse>(Arg.Any<EnableLoggingRequest>()));
            }

            [Fact]
            public async void LimitMemorySendsMessageToHost()
            {
                await CompleteInitializationAsync();
                this.launcher.SendMessageAsync<LimitMemoryRequest, LimitMemoryResponse>(Arg.Any<LimitMemoryRequest>()).ReturnsTask(new LimitMemoryResponse(0));

                await proxy.LimitMemoryAsync(1024);

                this.launcher.Received(
                    1,
                    x => x.SendMessageAsync<LimitMemoryRequest, LimitMemoryResponse>(
                        Arg.Is<LimitMemoryRequest>(
                            request => request.@params.LimitInBytes == 1024)));
            }

            [Fact]
            public async void BindMountsSendsMessageToHost()
            {
                await CompleteInitializationAsync();
                var expectedBindMount = new BindMount
                {
                    SourcePath = @"C:\Global\Path",
                    DestinationPath = @"C:\Container\Path",
                    Access = FileAccess.Read,
                };
                this.launcher.SendMessageAsync<BindMountsRequest, BindMountsResponse>(Arg.Any<BindMountsRequest>()).ReturnsTask(new BindMountsResponse(0));

                await proxy.BindMountsAsync(new BindMount[] { expectedBindMount });

                this.launcher.Received(1,
                    x => x.SendMessageAsync<BindMountsRequest, BindMountsResponse>(
                        Arg.Is<BindMountsRequest>(r => r.@params.Mounts.Single() == expectedBindMount)
                    )
                );
            }

            [Fact]
            public async void GetInfoSendsMessageToHost()
            {
                await CompleteInitializationAsync();
                var expectedInfo = new ContainerInfo();
                var expectedResponse = new ContainerInfoResponse(0, expectedInfo);
                this.launcher.SendMessageAsync<ContainerInfoRequest, ContainerInfoResponse>(Arg.Any<ContainerInfoRequest>()).ReturnsTask(expectedResponse);

                var info = await proxy.GetInfoAsync();

                Assert.Same(expectedInfo, info);
            }

            [Fact]
            public async void WhenContainerProxyHasEvents_MergesIntoResponseEvents()
            {
                await CompleteInitializationAsync();

                launcher.HostStopped += Raise.Event<EventHandler<int>>(this, 100);

                var expectedInfo = new ContainerInfo();
                var expectedResponse = new ContainerInfoResponse(0, expectedInfo);
                this.launcher.SendMessageAsync<ContainerInfoRequest, ContainerInfoResponse>(Arg.Any<ContainerInfoRequest>()).ReturnsTask(expectedResponse);

                var info = await proxy.GetInfoAsync();

                Assert.Collection(info.Events,
                    x => Assert.Equal("Application's ContainerHost stopped with exit code: 100.", x));
            }
        }

        public class WhenLauncherExits : ProxyContainerContext
        {
            [Fact]
            public void OutOfMemoryExitResultsInEventEntry()
            {
                var exitCode = -2;
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

        public class WhenRunningCommand : ContainerInitializedContext
        {          
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
                this.launcher.SendMessageAsync<RunCommandRequest, RunCommandResponse>(Arg.Any<RunCommandRequest>()).ReturnsTask(new RunCommandResponse("", new RunCommandResponseData() { exitCode = 0, stdOut = "StdOutMessage" }));
                var command = new RemoteCommand(false, "tar", "foo.zip");

                var response = await proxy.RunCommandAsync(command);

                Assert.Equal("StdOutMessage", response.StdOut);
            }

            [Fact]
            public async void WhenRunningCommand_StdErrIsReturned()
            {
                this.launcher.SendMessageAsync<RunCommandRequest, RunCommandResponse>(Arg.Any<RunCommandRequest>()).ReturnsTask(new RunCommandResponse("", new RunCommandResponseData() { exitCode = 0, stdErr = "StdErrMessage" }));
                var command = new RemoteCommand(false, "tar", "foo.zip");

                var response = await proxy.RunCommandAsync(command);

                Assert.Equal("StdErrMessage", response.StdErr);
            }
        }

        public class WhenReservingPort : ContainerInitializedContext
        {
           
            [Fact]
            public async void ReturnsRespondedPort()
            {
                var request = new ReservePortRequest(100);

                launcher.SendMessageAsync<ReservePortRequest, ReservePortResponse>(Arg.Any<ReservePortRequest>())
                    .ReturnsTask(new ReservePortResponse("", 200));

                var reservedPort = await proxy.ReservePortAsync(100);

                Assert.Equal(200, reservedPort);
            }

            [Fact]
            public async void CachesFirstReservation()
            {
                var request = new ReservePortRequest(100);
                
                launcher.SendMessageAsync<ReservePortRequest, ReservePortResponse>(Arg.Any<ReservePortRequest>())
                   .ReturnsTask(new ReservePortResponse("", 100));

                var reservedPort = await proxy.ReservePortAsync(100);

                launcher.SendMessageAsync<ReservePortRequest, ReservePortResponse>(Arg.Any<ReservePortRequest>())
                   .ReturnsTask(new ReservePortResponse("", 200));

                var nextReservation = await proxy.ReservePortAsync(200);

                Assert.Equal(reservedPort, nextReservation);
            }

            [Fact]
            public void PriorToReservationPropertyReturnsNull()
            {
                Assert.Null(proxy.AssignedPort);    
            }

            [Fact]
            public async void ReservedPortAvailableFromProperty()
            {
                var request = new ReservePortRequest(100);

                launcher.SendMessageAsync<ReservePortRequest, ReservePortResponse>(Arg.Any<ReservePortRequest>())
                    .ReturnsTask(new ReservePortResponse("", 200));

                var reservedPort = await proxy.ReservePortAsync(100);

                Assert.Equal(200, proxy.AssignedPort);
            }

        }

        public class WhenLauncherEndsAfterInitialize : ContainerInitializedContext
        {
            public WhenLauncherEndsAfterInitialize()
            {
                launcher.SendMessageAsync<ContainerStateRequest, ContainerStateResponse>(Arg.Any<ContainerStateRequest>()).ReturnsTask(new ContainerStateResponse("foo", ContainerState.Active.ToString()));
            }

            [Fact]
            public async void GetInfoShouldReportStopped()
            {
                await CompleteInitializationAsync();

                launcher.IsActive.Returns(false);
                launcher.WasActive.Returns(true);

                var info = await proxy.GetInfoAsync();

                Assert.Equal(ContainerState.Stopped, info.State);
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
