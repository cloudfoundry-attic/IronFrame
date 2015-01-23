using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading.Tasks;
using IronFoundry.Container.Messages;
using IronFoundry.Container.Messaging;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Container
{
    public interface IContainerHostClient : IDisposable
    {
        CreateProcessResult CreateProcess(CreateProcessParams @params);
        bool Ping(TimeSpan timeout);
        void Shutdown();
        void SubscribeToProcessData(Guid processKey, Action<ProcessDataEvent> callback);
        WaitForProcessExitResult WaitForProcessExit(WaitForProcessExitParams @params);
    }

    public class ContainerHostClient : IContainerHostClient
    {
        IProcess hostProcess;
        IMessageTransport messageTransport;
        IMessagingClient messagingClient;

        readonly ConcurrentDictionary<Guid, Action<ProcessDataEvent>> subscribers = new ConcurrentDictionary<Guid,Action<ProcessDataEvent>>();

        public ContainerHostClient(IProcess hostProcess, IMessageTransport messageTransport, IMessagingClient messagingClient)
        {
            this.hostProcess = hostProcess;
            this.messageTransport = messageTransport;
            this.messagingClient = messagingClient;

            this.hostProcess.Exited += (o, e) =>
            {
                // TODO: If the host process dies (or is killed) we should shutdown the container.

                //OnHostStopped(hostProcess != null && hostProcess.HasExited ? hostProcess.ExitCode : 0);
                DisposeMessageHandling();
            };

            this.messagingClient.SubscribeEvent<ProcessDataEvent>("processData", HandleProcessData);
        }

        public CreateProcessResult CreateProcess(CreateProcessParams @params)
        {
            var request = new CreateProcessRequest(@params);
            var responseTask = messagingClient.SendMessageAsync<CreateProcessRequest, CreateProcessResponse>(request);
            return responseTask.GetAwaiter().GetResult().result;
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
            var request = new PingRequest();
            var responseTask = messagingClient.SendMessageAsync<PingRequest, PingResponse>(request);

            // This looks weird, but it's the only want to detect a timeout with Tasks and awaiters.
            // Using Task.Wait(timeout) is dangerous in an async/await world!
            var timeoutTask = Task.Delay(timeout);
            var completedTask = Task.WhenAny(responseTask, timeoutTask).GetAwaiter().GetResult();

            if (Object.ReferenceEquals(completedTask, responseTask))
            {
                responseTask.GetAwaiter().GetResult();
                return true;
            }

            return false;
        }

        static void SafeKill(IProcess process)
        {
            try
            {
                process.Kill();
            }
            catch (Win32Exception)
            {
                // If the process is terminating and kill is invoked again you will get a Win32Exception for AccessDenied.
                // http://msdn.microsoft.com/en-us/library/system.diagnostics.process.kill(v=vs.110).aspx
            }
        }

        public void Shutdown()
        {
            var hostCapture = hostProcess;
            hostProcess = null;

            DisposeMessageHandling();

            if (hostCapture != null)
            {
                SafeKill(hostCapture);

                hostCapture.Dispose();
            }
        }

        public void SubscribeToProcessData(Guid processKey, Action<ProcessDataEvent> callback)
        {
            subscribers.AddOrUpdate(processKey, callback, (key, existingValue) => callback);
        }

        public WaitForProcessExitResult WaitForProcessExit(WaitForProcessExitParams @params)
        {
            var request = new WaitForProcessExitRequest(@params);
            var responseTask = messagingClient.SendMessageAsync<WaitForProcessExitRequest, WaitForProcessExitResponse>(request);
            return responseTask.GetAwaiter().GetResult().result;
        }
    }
}
