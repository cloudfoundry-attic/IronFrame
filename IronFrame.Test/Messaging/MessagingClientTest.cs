using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IronFrame.Messaging
{
    public class MessagingClientTest
    {
        [Fact]
        public void InvokesSenderToSendMessage()
        {
            bool invoked = false;
            MessagingClient client = null;
            JsonRpcRequest r = new JsonRpcRequest("TestMethod");

            client = new MessagingClient(m =>
            {
                invoked = true;
            });

            client.SendMessageAsync(r);

            Assert.True(invoked);
        }

        [Fact]
        public async void CorrelatesResponseWithRequest()
        {
            MessagingClient client = null;
            JsonRpcRequest r = new JsonRpcRequest("TestMethod");

            client = new MessagingClient(m =>
            {
                client.PublishResponse(new JObject(
                    new JProperty("jsonrpc", "2.0"),
                    new JProperty("id", r.id),
                    new JProperty("result", "0")
                    ));
            });

            var response = await client.SendMessageAsync(r);

            Assert.Equal(r.id, response.id);
        }

        [Fact]
        public async void CorrelatesErrorResponseWithRequest()
        {
            MessagingClient client = null;
            JsonRpcRequest r = new JsonRpcRequest("TestMethod");

            client = new MessagingClient(m =>
            {
                client.PublishResponse(
                    new JObject(
                        new JProperty("jsonrpc", "2.0"),
                        new JProperty("id", r.id),
                        new JProperty("error",
                            new JObject(
                                new JProperty("code", 1),
                                new JProperty("message", "Error Message")
                                )
                            )
                        )
                    );
            });

            var response = await client.SendMessageAsync(r);

            Assert.IsType<JsonRpcErrorResponse>(response);
        }

        [Fact]
        public async void ErrorResponseIncludesErrorData()
        {
            MessagingClient client = null;
            JsonRpcRequest r = new JsonRpcRequest("TestMethod");

            client = new MessagingClient(m =>
            {
                client.PublishResponse(
                    new JObject(
                        new JProperty("jsonrpc", "2.0"),
                        new JProperty("id", r.id),
                        new JProperty("error",
                            new JObject(
                                new JProperty("code", 1),
                                new JProperty("message", "Error Message"),
                                new JProperty("data", "Error Data")
                                )
                            )
                        )
                    );
            });

            var response = (JsonRpcErrorResponse)await client.SendMessageAsync(r);

            Assert.Equal(1, response.error.Code);
            Assert.Equal("Error Message", response.error.Message);
            Assert.Equal("Error Data", response.error.Data);
        }


        [Fact]
        public async void StronglyTypedRequestsReturnsStronglyTypedResponse()
        {
            MessagingClient client = null;
            var r = new CustomRequest();

            client = new MessagingClient(m =>
            {
                client.PublishResponse(new JObject(
                    new JProperty("jsonrpc", "2.0"),
                    new JProperty("id", r.id),
                    new JProperty("result", "ResultData")
                    ));
            });

            CustomResponse response = await client.SendMessageAsync<CustomRequest, CustomResponse>(r);
            Assert.NotNull(response);
        }

        [Fact]
        public async void StronglyTypedResponseContainsProperResults()
        {
            MessagingClient client = null;
            var r = new CustomRequest();

            client = new MessagingClient(m =>
            {
                client.PublishResponse(new JObject(
                    new JProperty("jsonrpc", "2.0"),
                    new JProperty("id", r.id),
                    new JProperty("result", "ResultData")
                    ));
            });

            CustomResponse response = await client.SendMessageAsync<CustomRequest, CustomResponse>(r);
            Assert.Equal("ResultData", response.result);
        }

        [Fact]
        public void StronglyTypedRequestErrorsThrowsProperExceptionType()
        {
            MessagingClient client = null;
            var r = new CustomRequest();

            client = new MessagingClient(m =>
            {
                client.PublishResponse(
                    new JObject(
                        new JProperty("jsonrpc", "2.0"),
                        new JProperty("id", r.id),
                        new JProperty("error",
                            new JObject(
                                new JProperty("code", 1),
                                new JProperty("message", "Error Message"),
                                new JProperty("data", "Error Data")
                                )
                            )
                        )
                    );
            });

            Exception recordedExeption = Record.Exception(() =>
            {
                var responseTask = client.SendMessageAsync<CustomRequest, CustomResponse>(r);
                var result = responseTask.Result;
            });

            Assert.IsType<MessagingException>(((AggregateException)recordedExeption).InnerExceptions[0]);
        }

        [Fact]
        public async void ThrowsWhenReceivingDuplicateRequest()
        {
            MessagingClient client = null;
            JsonRpcRequest r = new JsonRpcRequest("TestMethod");

            client = new MessagingClient(m => { });
            client.SendMessageAsync(r).Forget();

            var exception = await Record.ExceptionAsync(() => client.SendMessageAsync(r));

            Assert.IsType<MessagingException>(exception);
        }

        [Fact]
        public async void ThrowsWhenReceivingDuplicateStronglyTypedRequest()
        {
            MessagingClient client = null;
            var r = new CustomRequest();

            client = new MessagingClient(m => { });
            client.SendMessageAsync<CustomRequest, CustomResponse>(r).Forget();

            var exception = await Record.ExceptionAsync(() => client.SendMessageAsync<CustomRequest, CustomResponse>(r));

            Assert.IsType<MessagingException>(exception);
        }

        [Fact]
        public void ThrowsWhenReceivingAnUncorrelatableResponse()
        {
            MessagingClient client = new MessagingClient(s => { });
            var r = new JsonRpcRequest("TestMethod");

            var exception = Record.Exception(() =>
            {
                client.PublishResponse(
                    new JObject(
                       new JProperty("jsonrpc", "2.0"),
                       new JProperty("id", r.id + "_notit"),
                       new JProperty("result", "0")
                       ));
            });

            Assert.IsType<MessagingException>(exception);
        }

        [Fact]
        public async void ThrowsWhenReceivingDuplicateResponse()
        {
            MessagingClient client = null;
            JsonRpcRequest r = new JsonRpcRequest("TestMethod");

            client = new MessagingClient(m =>
            {
                client.PublishResponse(
                    new JObject(
                        new JProperty("jsonrpc", "2.0"),
                        new JProperty("id", r.id),
                        new JProperty("result", "0")
                        ));
            });

            await client.SendMessageAsync(r);

            var exception = Record.Exception(() =>
            {
                client.PublishResponse(
                    new JObject(
                        new JProperty("jsonrpc", "2.0"),
                        new JProperty("id", r.id),
                        new JProperty("result", "0")
                        ));
            });

            Assert.IsType<MessagingException>(exception);
        }

        [Fact]
        public void DisposingDisposesExistingAwaiters()
        {
            MessagingClient client = null;
            JsonRpcRequest r = new JsonRpcRequest("TestMethod");

            client = new MessagingClient(m => { });

            var response = client.SendMessageAsync(r);

            client.Dispose();

            var exception = Record.Exception(() => { response.Wait(1000); });

            Assert.IsType<OperationCanceledException>(((AggregateException)exception).InnerExceptions[0]);
        }

        [Fact]
        public void PublishesEventToListener()
        {
            MessagingClient client = new MessagingClient(s => { });

            bool invoked = false;
            client.SubscribeEvent("someTopic", e => { invoked = true; });

            var @event = new JObject(
                new JProperty("EventTopic", "someTopic")
                );
            
            client.PublishEvent(@event);

            Assert.True(invoked);
        }

        [Fact]
        public void IgnoresEventsWithNoListeners()
        {
            MessagingClient client = new MessagingClient(s => { });
            
            var @event = new JObject(
              new JProperty("EventTopic", "someTopic")
              );

            client.PublishEvent(@event);

            // Should not error or throw on publish
        }

        [Fact]
        public void ThrowsOnMalformedEvents()
        {
            MessagingClient client = new MessagingClient(s => { });
            var @event = new JObject(
                new JProperty("no_event_topic", "not here")
            );

            var except = Record.Exception(() => { client.PublishEvent(@event); });

            Assert.IsType<ArgumentException>(except);
        }

        [Fact]
        public void StronglyTypedSubscriptionsAreInvoked()
        {
            MessagingClient client = new MessagingClient(s => { });

            TestEvent testEvent = null;
            client.SubscribeEvent<TestEvent>("TestEvent", e => { testEvent = e; });

            client.PublishEvent(new JObject(
                new JProperty("EventTopic", "TestEvent"),
                new JProperty("SomeEventProperty", "PropertyValue")
                ));

            Assert.NotNull(testEvent);
            Assert.Equal("PropertyValue", testEvent.SomeEventProperty);
        }

        class CustomRequest : JsonRpcRequest
        {
            public CustomRequest()
                : base("CustomRequestMethod")
            {
            }
        }

        class CustomResponse : JsonRpcResponse<string>
        {
            public CustomResponse()
                : base("", "")
            {

            }
        }

        class TestEvent 
        {
            public string EventTopic { get; set; }
            public string SomeEventProperty { get; set; }
        }

    }
}