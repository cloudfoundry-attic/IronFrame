using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IronFoundry.Warden.Shared.Concurrency;

namespace IronFoundry.Warden.Shared.Messaging
{
    public class MessageTransport : IDisposable
    {
        private TextReader reader;
        private TextWriter writer;
        private List<Func<JObject, Task>> requestCallbacks = new List<Func<JObject, Task>>();
        private List<Func<JObject, Task>> responseCallbacks = new List<Func<JObject, Task>>();
        private List<Action<Exception>> errorSubscribers = new List<Action<Exception>>();
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private volatile Task readTask;
        private readonly AsyncLock writeLock = new AsyncLock();

        public MessageTransport(TextReader reader, TextWriter writer)
        {
            this.reader = reader;
            this.writer = writer;

            Start();
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

            if (IsResponseMessage(message))
                await InvokeResponseCallbackAsync(message);
            else
                await InvokeRequestCallbackAsync(message);
        }

        private bool IsResponseMessage(JObject message)
        {
            return (message["result"] != null || message["error"] != null);
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

        public async Task PublishAsync(JObject message)
        {
            string text = message.ToString(Formatting.None);
            using(var releaser = await writeLock.LockAsync())
            {
                writer.WriteLine(text);
            }
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

        public void SubscribeError(Action<Exception> callback)
        {
            lock (errorSubscribers)
            {
                errorSubscribers.Add(callback);
            }
        }
    }
}
