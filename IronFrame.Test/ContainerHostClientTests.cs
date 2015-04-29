using System;
using System.Threading;
using System.Threading.Tasks;
using IronFrame.Messages;
using IronFrame.Messaging;
using IronFrame.Utilities;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace IronFrame
{
    public class ContainerHostClientTests
    {
        IProcess HostProcess { get; set; }
        IMessageTransport MessageTransport { get; set; }
        IMessagingClient MessagingClient { get; set; }
        JobObject JobObject { get; set; }
        Action<ProcessDataEvent> ProcessDataEventGenerator { get; set; }
        ContainerHostClient Client { get; set; }

        public ContainerHostClientTests()
        {
            HostProcess = Substitute.For<IProcess>();
            MessageTransport = Substitute.For<IMessageTransport>();

            MessagingClient = Substitute.For<IMessagingClient>();
            MessagingClient.WhenForAnyArgs(x => x.SubscribeEvent<ProcessDataEvent>("processData", null))
                .Do(call =>
                {
                    ProcessDataEventGenerator = call.Arg<Action<ProcessDataEvent>>();
                });

            JobObject = Substitute.For<JobObject>();

            Client = new ContainerHostClient(HostProcess, MessageTransport, MessagingClient, JobObject);
        }

        static Task<T> GetCompletedTask<T>(T result)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        public class CreateProcess : ContainerHostClientTests
        {
            CreateProcessResult ExpectedResult { get; set; }
            CreateProcessResponse ExpectedResponse { get; set; }

            public CreateProcess()
            {
                ExpectedResult = new CreateProcessResult();
                ExpectedResponse = new CreateProcessResponse(JToken.FromObject(1), ExpectedResult);

                MessagingClient.SendMessageAsync<CreateProcessRequest, CreateProcessResponse>(null)
                    .ReturnsForAnyArgs(GetCompletedTask(ExpectedResponse));
            }

            [Fact]
            public void SendsRequestWithParams()
            {
                var @params = new CreateProcessParams
                {
                    executablePath = "foo.exe",
                };

                Client.CreateProcess(@params);

                MessagingClient.Received(1).SendMessageAsync<CreateProcessRequest, CreateProcessResponse>(
                    Arg.Is<CreateProcessRequest>(request =>
                        request.@params == @params
                    )
                );
            }

            [Fact]
            public void ReturnsResultFromResponse()
            {
                var @params = new CreateProcessParams
                {
                    executablePath = "foo.exe",
                };

                var result = Client.CreateProcess(@params);

                Assert.Same(ExpectedResult, result);
            }
        }

        public class Ping : ContainerHostClientTests
        {
            PingResponse ExpectedResponse { get; set; }

            public Ping()
            {
                MessagingClient.SendMessageAsync<PingRequest, PingResponse>(null)
                    .ReturnsForAnyArgs(GetCompletedTask(ExpectedResponse));
            }

            [Fact]
            public void SendsRequest()
            {
                Client.Ping(TimeSpan.FromMilliseconds(0));

                MessagingClient.Received(1).SendMessageAsync<PingRequest, PingResponse>(
                    Arg.Any<PingRequest>());
            }

            [Fact]
            public void WhenTimeoutDoesNotOccur_ReturnsTrue()
            {
                Assert.True(Client.Ping(TimeSpan.FromMilliseconds(0)));
            }

            [Fact]
            public void WhenTimeoutOccurs_ReturnsFalse()
            {
                MessagingClient.SendMessageAsync<PingRequest, PingResponse>(null)
                    .ReturnsForAnyArgs(async (call) => {
                        await Task.Delay(20);
                        return ExpectedResponse;
                    });
                    
                Assert.False(Client.Ping(TimeSpan.FromMilliseconds(1)));
            }
        }

        public class Shutdown : ContainerHostClientTests
        {
            [Fact]
            public void DisconnectsMessaging()
            {
                Client.Shutdown();

                MessagingClient.Received(1).Dispose();
                MessageTransport.Received(1).Dispose();
            }

            [Fact]
            public void TerminatesProcessesInJobObject()
            {
                Client.Shutdown();
                
                JobObject.Received(1).TerminateProcessesAndWait(Timeout.Infinite);
            }
        }

        public class SubscribeToProcessData : ContainerHostClientTests
        {
            Guid KnownProcessKey { get; set; }
            Action<ProcessDataEvent> ProcessDataCallback { get; set; }
            
            public SubscribeToProcessData()
            {
                KnownProcessKey = Guid.NewGuid();
                ProcessDataCallback = delegate { };

                Client.SubscribeToProcessData(KnownProcessKey, (data) => ProcessDataCallback(data));
            }

            [Fact]
            public void DeliversProcessDataForAKnownProcess()
            {
                ProcessDataEvent actualProcessData = null;
                ProcessDataCallback = (data) => actualProcessData = data;

                ProcessDataEventGenerator(new ProcessDataEvent(KnownProcessKey, ProcessDataType.STDOUT, "This is STDOUT"));

                Assert.Equal(KnownProcessKey, actualProcessData.key);
                Assert.Equal(ProcessDataType.STDOUT, actualProcessData.dataType);
                Assert.Equal("This is STDOUT", actualProcessData.data);
            }

            [Fact]
            public void IgnoresProcessDataForUnknownProcesses()
            {
                ProcessDataEvent actualProcessData = null;
                ProcessDataCallback = (data) => actualProcessData = data;

                ProcessDataEventGenerator(new ProcessDataEvent(Guid.NewGuid(), ProcessDataType.STDOUT, "This is STDOUT"));

                Assert.Null(actualProcessData);
            }
        }

        public class FindProcessById : ContainerHostClientTests
        {
            private FindProcessByIdResult ExpectedResult { get; set; }
            private FindProcessByIdResponse ExpectedResponse { get; set; }

            public FindProcessById()
            {
                ExpectedResult = new FindProcessByIdResult();
                ExpectedResponse = new FindProcessByIdResponse(JToken.FromObject(1), ExpectedResult);

                MessagingClient.SendMessageAsync<FindProcessByIdRequest, FindProcessByIdResponse>(null)
                    .ReturnsForAnyArgs(GetCompletedTask(ExpectedResponse));
            }

            [Fact]
            public void SendsRequestWithParams()
            {
                var @params = new FindProcessByIdParams
                {
                    id = new Random().Next(10000),
                };

                Client.FindProcessById(@params);

                MessagingClient.Received(1).SendMessageAsync<FindProcessByIdRequest, FindProcessByIdResponse>(
                    Arg.Is<FindProcessByIdRequest>(request => request.@params == @params)
                );
            }
        }

        public class WaitForProcessExit : ContainerHostClientTests
        {
            WaitForProcessExitResult ExpectedResult { get; set; }
            WaitForProcessExitResponse ExpectedResponse { get; set; }

            public WaitForProcessExit()
            {
                ExpectedResult = new WaitForProcessExitResult();
                ExpectedResponse = new WaitForProcessExitResponse(JToken.FromObject(1), ExpectedResult);

                MessagingClient.SendMessageAsync<WaitForProcessExitRequest, WaitForProcessExitResponse>(null)
                    .ReturnsForAnyArgs(GetCompletedTask(ExpectedResponse));
            }

            [Fact]
            public void SendsRequestWithParams()
            {
                var @params = new WaitForProcessExitParams
                {
                    key = Guid.NewGuid(),
                    timeout = 5000,
                };

                Client.WaitForProcessExit(@params);

                MessagingClient.Received(1).SendMessageAsync<WaitForProcessExitRequest, WaitForProcessExitResponse>(
                    Arg.Is<WaitForProcessExitRequest>(request =>
                        request.@params == @params
                    )
                );
            }

            [Fact]
            public void ReturnsResultFromResponse()
            {
                var @params = new WaitForProcessExitParams
                {
                    key = Guid.NewGuid(),
                    timeout = 5000,
                };

                var result = Client.WaitForProcessExit(@params);

                Assert.Same(ExpectedResult, result);
            }
        }
    }
}
