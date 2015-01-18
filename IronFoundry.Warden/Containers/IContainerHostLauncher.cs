using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Container.Messaging;

namespace IronFoundry.Warden.Containers
{
    // BR: Move to IronFoundry.Container
    public enum LogMessageType
    {
        STDIN = 0,
        STDOUT = 1,
        STDERR = 2,
    }

    // BR: Move to IronFoundry.Container
    public class LogEventArgs : EventArgs
    {
        public LogMessageType Type { get; set; }
        public string Data { get; set; }
    }

    // BR: Move to IronFoundry.Container
    public interface IContainerHostLauncher
    {
        event EventHandler<int> HostStopped;
        event EventHandler<LogEventArgs> LogEvent;

        int HostProcessId { get; }
        bool IsActive { get; }
        bool WasActive { get; }
        int? LastExitCode { get; }
        void Start(string workingDirectory, string jobObjectName);
        void Stop();
        Task<TResult> SendMessageAsync<T, TResult>(T request)
            where T : JsonRpcRequest
            where TResult : JsonRpcResponse;
    }
}
