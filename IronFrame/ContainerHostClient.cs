﻿using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IronFrame.Messages;
using IronFrame.Messaging;
using IronFrame.Utilities;

namespace IronFrame
{
    internal interface IContainerHostClient : IDisposable
    {
        CreateProcessResult CreateProcess(CreateProcessParams @params);
        bool Ping(TimeSpan timeout);
        void Shutdown();
        void StopProcess(Guid key, int timeout);
        void StopAllProcesses(int timeout);
        void SubscribeToProcessData(Guid processKey, Action<ProcessDataEvent> callback);
        WaitForProcessExitResult WaitForProcessExit(WaitForProcessExitParams @params);
        event EventHandler Exited;
        FindProcessByIdResult FindProcessById(FindProcessByIdParams @params);
    }

    internal class ContainerHostClient : IContainerHostClient
    {
        private const int DefaultCreateProcessTimeout = 15 * 1000;

        JobObject containerJobObject;
        IProcess hostProcess;
        IMessageTransport messageTransport;
        IMessagingClient messagingClient;
        public event EventHandler Exited;

        readonly ConcurrentDictionary<Guid, Action<ProcessDataEvent>> subscribers = new ConcurrentDictionary<Guid,Action<ProcessDataEvent>>();

        public ContainerHostClient(IProcess hostProcess, IMessageTransport messageTransport, IMessagingClient messagingClient, JobObject containerJobObject)
        {
            this.hostProcess = hostProcess;
            this.messageTransport = messageTransport;
            this.messagingClient = messagingClient;
            this.containerJobObject = containerJobObject;

            this.hostProcess.Exited += (o, e) =>
            {
                Exited.Invoke(this, e);

                // TODO: If the host process dies (or is killed) we should shutdown the container.

                //OnHostStopped(hostProcess != null && hostProcess.HasExited ? hostProcess.ExitCode : 0);
                DisposeMessageHandling();
            };

            this.messagingClient.SubscribeEvent<ProcessDataEvent>("processData", HandleProcessData);
        }

        public CreateProcessResult CreateProcess(CreateProcessParams @params)
        {
            CreateProcessResponse response;

            if (!TrySendMessage<CreateProcessRequest, CreateProcessResponse>(new CreateProcessRequest(@params), new TimeSpan(0, 0, 0, 0, DefaultCreateProcessTimeout), out response))
            {
                throw new TimeoutException("CreateProcess timed out");
            }

            return response.result;
        }

        public void Dispose()
        {
            Shutdown();
        }

        void DisposeMessageHandling()
        {
            if (messagingClient != null)
            {
                messagingClient.Dispose();
                messagingClient = null;
            }

            if (messageTransport != null)
            {
                messageTransport.Dispose();
                messageTransport = null;
            }
        }

        void HandleProcessData(ProcessDataEvent processData)
        {
            Action<ProcessDataEvent> callback;
            if (subscribers.TryGetValue(processData.key, out callback))
                callback(processData);
        }

        public bool Ping(TimeSpan timeout)
        {
            PingResponse response;
            return TrySendMessage<PingRequest, PingResponse>(new PingRequest(), timeout, out response);
        }

        TResponse SendMessage<TRequest, TResponse>(TRequest request)
            where TRequest : JsonRpcRequest
            where TResponse : JsonRpcResponse
        {
            var task = messagingClient.SendMessageAsync<TRequest, TResponse>(request);
            return task.GetAwaiter().GetResult();
        }

        bool TrySendMessage<TRequest, TResponse>(TRequest request, TimeSpan timeout, out TResponse response)
            where TRequest : JsonRpcRequest
            where TResponse : JsonRpcResponse
        {
            var task = messagingClient.SendMessageAsync<TRequest, TResponse>(request);
            // This looks weird, but it's the only want to detect a timeout with Tasks and awaiters.
            // Using Task.Wait(timeout) is dangerous in an async/await world!
            var timeoutTask = Task.Delay(timeout);
            var completedTask = Task.WhenAny(task, timeoutTask).GetAwaiter().GetResult();

            if (Object.ReferenceEquals(completedTask, task))
            {
                response = task.GetAwaiter().GetResult();
                return true;
            }

            response = null;
            return false;
        }

        public void Shutdown()
        {
            DisposeMessageHandling();

            if (containerJobObject != null)
                containerJobObject.TerminateProcessesAndWait(Timeout.Infinite);

            hostProcess = null;
            containerJobObject = null;
        }

        public void StopProcess(Guid key, int timeout)
        {
            var @params = new StopProcessParams
            {
                key = key,
                timeout = timeout
            };

            messagingClient.SendMessageAsync<StopProcessRequest, StopAllProcessesResponse>(new StopProcessRequest(@params));
        }

        public void StopAllProcesses(int timeout)
        {
            var @params = new StopAllProcessesParams
            {
                timeout = timeout,
            };

            StopAllProcessesResponse response;
            if (!TrySendMessage<StopAllProcessesRequest, StopAllProcessesResponse>(new StopAllProcessesRequest(@params), new TimeSpan(0, 0, 0, 0, timeout), out response))
            {
                throw new TimeoutException("Sending Stop timedout");
            }
        }

        public void SubscribeToProcessData(Guid processKey, Action<ProcessDataEvent> callback)
        {
            subscribers.AddOrUpdate(processKey, callback, (key, existingValue) => callback);
        }

        public WaitForProcessExitResult WaitForProcessExit(WaitForProcessExitParams @params)
        {
            var response = SendMessage<WaitForProcessExitRequest, WaitForProcessExitResponse>(new WaitForProcessExitRequest(@params));
            return response.result;
        }

        public FindProcessByIdResult FindProcessById(FindProcessByIdParams @params)
        {
            var response =
                SendMessage<FindProcessByIdRequest, FindProcessByIdResponse>(new FindProcessByIdRequest(@params));
            return response.result;
        }
    }
}
