using System;
using System.Diagnostics;

namespace IronFoundry.Warden.Utilities
{
    public interface IProcess : IDisposable
    {
        int ExitCode { get; }
        IntPtr Handle { get; }
        bool HasExited { get; }
        int Id { get; }
        
        TimeSpan TotalProcessorTime { get;  }
        TimeSpan TotalUserProcessorTime { get; }
        long WorkingSet { get; }
        long PrivateMemoryBytes { get; }

        event EventHandler Exited;
        event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        void Kill();
        void WaitForExit();
        void WaitForExit(int milliseconds);
    }
}
