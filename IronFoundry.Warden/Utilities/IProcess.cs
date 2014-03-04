using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Utilities
{
    public interface IProcess
    {
        int ExitCode { get; }
        bool HasExited { get; }
        int Id { get; }                
        TimeSpan TotalProcessorTime { get;  }

        event EventHandler Exited;

        void Kill();
    }
}
