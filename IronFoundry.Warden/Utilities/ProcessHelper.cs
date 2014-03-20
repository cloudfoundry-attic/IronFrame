using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IronFoundry.Warden.Utilities
{
    public class ProcessHelper
    {
        public virtual IProcess GetProcessById(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                return new RealProcessWrapper(process);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public virtual IEnumerable<IProcess> GetProcesses(IEnumerable<int> processIds)
        {
            return processIds
                .Select(id => GetProcessById(id))
                .Where(p => p != null);
        }

        class RealProcessWrapper : IProcess
        {
            private readonly Process process;
            public event EventHandler Exited;

            public RealProcessWrapper(Process process)
            {
                this.process = process;
                Id = process.Id;
                process.Exited += (o, e) => this.OnExited();
            }

            public int Id { get; private set; }

            public int ExitCode
            {
                get { return process.ExitCode; }
            }

            public IntPtr Handle
            {
                get { return process.Handle; }
            }

            public bool HasExited
            {
                get { return process.HasExited; }
            }

            public TimeSpan TotalProcessorTime
            {
                get { return process.TotalProcessorTime; }
            }

            public TimeSpan TotalUserProcessorTime
            {
                get { return process.UserProcessorTime; }
            }

            public void Kill()
            {
                process.Kill();
            }

            protected virtual void OnExited()
            {
                var handlers = Exited;
                if (handlers != null)
                {
                    handlers.Invoke(this, EventArgs.Empty);
                }
            }

            public long PrivateMemoryBytes
            {
                get { return process.PrivateMemorySize64; }
            }

            public long WorkingSet
            {
                get { return process.WorkingSet64; }
            }

            public void Dispose()
            {
                process.Dispose();
            }

            public void WaitForExit()
            {
                process.WaitForExit();
            }
        }
    }
}
