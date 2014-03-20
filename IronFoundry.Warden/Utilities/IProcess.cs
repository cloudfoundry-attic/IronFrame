using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        long PagedMemoryBytes { get; }

        event EventHandler Exited;

        void Kill();
        void WaitForExit();
    }
}
