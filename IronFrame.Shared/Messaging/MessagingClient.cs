using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace IronFrame.Messaging
{
    internal interface IMessagingClient : IDisposable
    {
        Task<TResult> SendMessageAsync<T, TResult>(T request)
            where T : JsonRpcRequest
            where TResult : JsonRpcResponse;

        void SubscribeEvent<T>(string eventTopic, Action<T> callback)
            where T: class, new();

        void PublishResponse(JObject response);

        void PublishEvent(JObject @event);
    }

    internal class MessagingClient : IMessagingClient
    {
        private Action<JObject> transportHandler;
        private ConcurrentDictionary<JToken, ResponsePublisher> awaitingResponse =
            new ConcurrentDictionary<JToken, ResponsePublisher>();
        private ConcurrentDictionary<string, EventPublisher> eventSubscribers =
            new ConcurrentDictionary<string, EventPublisher>();

        internal MessagingClient(Action<JObject> transportHandler)
        {
            this.transportHandler = transportHandler;
        }

        public static IMessagingClient Create(Action<JObject> transportHandler)
        {
            return new MessagingClient(transportHandler);
        }

        public void Dispose()
        {
            foreach (var key in awaitingResponse.Keys.ToArray())
            {
                ResponsePublisher publisher;
                if (awaitingResponse.TryRemove(key, out publisher))
                {
                    var disposable = publisher as IDisposable;
                    if (disposable != null) disposable.Dispose();
                }
            }
        }

        public void PublishResponse(JObject response)
        {
            string id = response["id"].ToString();
            ResponsePublisher publisher;
            if (awaitingResponse.TryRemove(id, out publisher))
            {
                publisher.Publish(response);
            }
            else
            {
                throw new MessagingException("No one waiting for response " + id);
            }
        }

        public Task<JsonRpcResponse> SendMessageAsync(JsonRpcRequest request)
        {
            var publisher = new DefaultResponsePublisher();
            if (!awaitingResponse.TryAdd(request.id, publisher))
            {
                throw new MessagingException(String.Format("A message with the id '{0}' is already pending.", request.id));
            }

            // TODO: Wire up error handling to raise an error on the publisher.Task when the transport fails to send the message.
            transportHandler(JObject.FromObject(request));
            return publisher.Task;
        }

        public Task<TResult> SendMessageAsync<T, TResult>(T request)
            where T : JsonRpcRequest
            where TResult : JsonRpcResponse
        {
            var publisher = new StronglyTypedResponsePublisher<TResult>();
            if (!awaitingResponse.TryAdd(request.id, publisher))
            {
                throw new MessagingException(String.Format("A message with the id '{0}' is already pending.", request.id));
            }

            // TODO: Wire up error handling to raise an error on the publisher.Task when the transport fails to send the message.
            transportHandler(JObject.FromObject(request));
            return publisher.Task;
        }

        public void SubscribeEvent(string eventTopic, Action<JObject> callback)
        {

            eventSubscribers.TryAdd(eventTopic, new DefaultEventPublisher(callback));
        }

        public void SubscribeEvent<T>(string eventTopic, Action<T> callback)
            where T: class, new()
        {
            eventSubscribers.TryAdd(eventTopic, new StronglyTypedEventPublisher<T>(callback));
        }

        public void PublishEvent(JObject @event)
        {
            JToken topic;
            if (!@event.TryGetValue("EventTopic", out topic))
            {
                throw new ArgumentException("Event does not contain topic");
            }

            EventPublisher publisher;
            if (eventSubscribers.TryGetValue(topic.Value<string>(), out publisher))
            {
                publisher.Publish(@event);
            }
        }

        private abstract class ResponsePublisher
        {
            abstract public void Publish(JObject response);

            protected bool IsErrorResponse(JObject response)
            {
                return (response["error"] != null);
            }

            protected JsonRpcErrorResponse BuildErrorResponse(JObject error)
            {
                var errorResponse = new JsonRpcErrorResponse(error["id"].ToString());
                errorResponse.error.Code = (int)error["error"]["code"];
                errorResponse.error.Message = error["error"]["message"].ToString();
                errorResponse.error.Data = error["error"]["data"] == null ? null : error["error"]["data"].ToString();

                return errorResponse;
            }
        }

        private class DefaultResponsePublisher : ResponsePublisher, IDisposable
        {
            TaskCompletionSource<JsonRpcResponse> tcs = new TaskCompletionSource<JsonRpcResponse>();

            public DefaultResponsePublisher()
            {
            }

            public override void Publish(JObject arg)
            {
                if (IsErrorResponse(arg))
                {
                    tcs.SetResult(BuildErrorResponse(arg));
                }
                else
                {
                    var rpcResponse = new JsonRpcResponse<string>(arg["id"].ToString(), arg["result"].ToString());
                    tcs.SetResult(rpcResponse);
                }
            }

            public Task<JsonRpcResponse> Task
            {
                get
                {
                    return tcs.Task;
                }
            }

            public void Dispose()
            {
                tcs.TrySetException(new OperationCanceledException());
            }
        }

        private class StronglyTypedResponsePublisher<TResponse> : ResponsePublisher, IDisposable
            where TResponse : JsonRpcResponse
        {
            private TaskCompletionSource<TResponse> tcs = new TaskCompletionSource<TResponse>();

            public StronglyTypedResponsePublisher()
            {
            }

            override public void Publish(JObject response)
            {
                if (IsErrorResponse(response))
                {
                    var error = BuildErrorResponse(response);
                    tcs.SetException(new MessagingException(error.error.Message) { ErrorResponse = error });
                }
                else
                {
                    tcs.SetResult(response.ToObject<TResponse>());
                }
            }

            public Task<TResponse> Task
            {
                get
                {
                    return tcs.Task;
                }
            }

            public void Dispose()
            {
                tcs.TrySetException(new OperationCanceledException());
            }
        }

        private abstract class EventPublisher
        {
            public abstract void Publish(JObject message);
        }

        private class DefaultEventPublisher: EventPublisher
        {
            private Action<JObject> callback;
            public DefaultEventPublisher(Action<JObject> callback)
            {
                this.callback = callback;
            }

            public override void Publish(JObject message)
            {
                this.callback(message);
            }
        }

        private class StronglyTypedEventPublisher<T> : EventPublisher
        {
            private Action<T> callback;

            public StronglyTypedEventPublisher(Action<T> callback) 
            {
                this.callback = callback;
            }

            public override void Publish(JObject message)
            {
                T eventMessage = message.ToObject<T>();
                this.callback(eventMessage);
            }
        }
    }
}
