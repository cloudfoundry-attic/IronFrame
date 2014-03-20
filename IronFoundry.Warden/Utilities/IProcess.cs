using System;

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

        void Kill();
        void WaitForExit();
        void WaitForExit(int milliseconds);
    }
}
