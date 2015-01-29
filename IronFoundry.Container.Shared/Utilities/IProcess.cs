using System;
using System.IO;

namespace IronFoundry.Container.Utilities
{
    // BR: Move this to IronFoundry.Container.Shared
    // BR: Get rid of RequestExit(). Add Stop(timeout).
    public interface IProcess : IDisposable
    {
        int ExitCode { get; }
        IntPtr Handle { get; }
        int Id { get; }
        
        long PrivateMemoryBytes { get; }

        event EventHandler Exited;
        event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        TextReader StandardOutput { get; }
        TextReader StandardError { get; }
        TextWriter StandardInput { get; }

        void Kill();
        void WaitForExit();
        bool WaitForExit(int milliseconds);

        void RequestExit();
    }
}
