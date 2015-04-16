﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Container.Messaging
{
    internal interface IMessageTransport : IDisposable
    {
        void Start();
        void Stop();

        void SubscribeResponse(Func<JObject, Task> callback);
        void SubscribeRequest(Func<JObject, Task> callback);

        Task PublishResponseAsync(JObject message);
        Task PublishRequestAsync(JObject message);

        void SubscribeEvent(Func<JObject, Task> callback);
        Task PublishEventAsync<T>(string eventTopic, T @event);
    }

    internal class MessageTransport : IMessageTransport
    {
        private TextReader reader;
        private TextWriter writer;
        private List<Func<JObject, Task>> requestCallbacks = new List<Func<JObject, Task>>();
        private List<Func<JObject, Task>> responseCallbacks = new List<Func<JObject, Task>>();
        private List<Func<JObject, Task>> eventSubscribers = new List<Func<JObject, Task>>();

        private List<Action<Exception>> errorSubscribers = new List<Action<Exception>>();
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private volatile Task readTask;
        private readonly AsyncLock writeLock = new AsyncLock();

        enum ContentType
        {
            Unknown,
            Request,
            Response,
            Event
        }

        internal MessageTransport(TextReader reader, TextWriter writer)
        {
            this.reader = reader;
            this.writer = writer;
        }

        public static IMessageTransport Create(TextReader reader, TextWriter writer)
        {
            return new MessageTransport(reader, writer);
        }

        public void Dispose()
        {
            Stop();
        }

        private async Task ReadLineAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                string line = await reader.ReadLineAsync();
                if (token.IsCancellationRequested)
                    return;

                if (line == null)
                    return;

                if (line.Length == 0)
                    continue;

                var unused = Task.Run(() => InvokeCallbackAsync(line));

                if (token.IsCancellationRequested)
                    return;
            }
        }

        private async Task InvokeCallbackAsync(string stringMessage)
        {
            JObject message = null;
            try
            {
                message = JObject.Parse(stringMessage);
            }
            catch (Exception e)
            {
                InvokeErrors(e);
                return;
            }

            ContentType contentType;
            JToken body = null;

            if (!TryUnwrap(message, out contentType, out body))
                InvokeErrors(new InvalidOperationException("Received invalidly formatted message"));

            switch (contentType)
            {
                case ContentType.Request:
                    await InvokeRequestCallbackAsync((JObject)body);
                    break;
                case ContentType.Response:
                    await InvokeResponseCallbackAsync((JObject)body);
                    break;
                case ContentType.Event:
                    await InvokeEventCallbackAsync((JObject)body);
                    break;
                default:
                    InvokeErrors(new InvalidOperationException("Received currently unsupported content-type"));
                    break;
            }
        }

        private bool TryUnwrap(JObject wrappedMessage, out ContentType contentType, out JToken body)
        {
            contentType = ContentType.Unknown;
            body = null;

            JToken contentTypeToken = null;
            if (!wrappedMessage.TryGetValue("content_type", out contentTypeToken))
                return false;

            if (!Enum.TryParse<ContentType>(contentTypeToken.Value<string>(), true, out contentType))
                return false;

            if (!wrappedMessage.TryGetValue("body", out body))
                return false;

            return true;
        }
        
        private async Task InvokeRequestCallbackAsync(JObject message)
        {
            List<Func<JObject, Task>> callbacks = new List<Func<JObject, Task>>();

            lock (requestCallbacks)
            {
                callbacks.AddRange(requestCallbacks);
            }

            foreach (var callback in callbacks)
            {
                await callback(message);
            }
        }

        private async Task InvokeResponseCallbackAsync(JObject message)
        {
            List<Func<JObject, Task>> callbacks = new List<Func<JObject,Task>>();

            lock (responseCallbacks)
            {
                callbacks.AddRange(responseCallbacks);
            }

            foreach (var callback in callbacks)
            {
                await callback(message);
            }
        }

        private async Task InvokeEventCallbackAsync(JObject message)
        {
            List<Func<JObject, Task>> subscribers = new List<Func<JObject, Task>>();

            lock (eventSubscribers)
            {
                subscribers.AddRange(eventSubscribers);
            }

            foreach (var subscriber in subscribers)
            {
                await subscriber(message);
            }
        }

        private void InvokeErrors(Exception e)
        {
            lock (errorSubscribers)
            {
                foreach (var callback in errorSubscribers)
                {
                    try
                    {
                        callback(e);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private async Task PublishAsync(JObject message)
        {
            string text = message.ToString(Formatting.None);
            using(var releaser = await writeLock.LockAsync())
            {
                writer.WriteLine(text);
            }
        }

        public Task PublishRequestAsync(JObject message)
        {
            var request = WrapMessage(ContentType.Request, message);
            return PublishAsync(request);
        }

        public Task PublishResponseAsync(JObject message)
        {
            var response = WrapMessage(ContentType.Response, message);
            return PublishAsync(response);
        }

        public Task PublishEventAsync(JObject message)
        {
            var @event = WrapMessage(ContentType.Event, message);
            return PublishAsync(@event);
        }

        public Task PublishEventAsync<T>(string eventTopic, T @event)
        {
            var jsonEvent = JObject.FromObject(@event);
            jsonEvent["EventTopic"] = eventTopic;
            return PublishEventAsync(jsonEvent);
        }

        public void Start()
        {
            if (!tokenSource.IsCancellationRequested)
                tokenSource.Cancel();

            tokenSource = new CancellationTokenSource();

            readTask = Task.Run(() => ReadLineAsync(tokenSource.Token));
        }
        
        public void Stop()
        {
            tokenSource.Cancel();
            readTask = null;
        }

        public void SubscribeRequest(Func<JObject, Task> callback)
        {
            lock (requestCallbacks)
            {
                requestCallbacks.Add(callback);
            }
        }

        public void SubscribeResponse(Func<JObject, Task> callback)
        {
            lock (responseCallbacks)
            {
                responseCallbacks.Add(callback);
            }
        }

        public void SubscribeEvent(Func<JObject, Task> callback)
        {
            lock (eventSubscribers)
            {
                eventSubscribers.Add(callback);
            }
        }

        public void SubscribeError(Action<Exception> callback)
        {
            lock (errorSubscribers)
            {
                errorSubscribers.Add(callback);
            }
        }

        private static JObject WrapMessage(ContentType contentType, JObject body)
        {
            return new JObject(
                new JProperty("content_type", contentType.ToString()),
                new JProperty("body", body)
                );
        }
    }
}
