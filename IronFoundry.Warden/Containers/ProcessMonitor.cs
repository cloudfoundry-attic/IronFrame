using IronFoundry.Warden.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Containers
{
   
    /// <summary>
    /// Monitors supplied processes and aggregates their Error and Output received events.
    /// </summary>    
    public class ProcessMonitor
    {
        ConcurrentDictionary<int, IProcess> processes = new ConcurrentDictionary<int, IProcess>();

        public EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        public EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        public bool TryAdd(Utilities.IProcess process)
        {
            if (processes.TryAdd(process.GetHashCode(), process))
            {
                AddProcessEvents(process);
                return true;
            }

            return false;
        }

        public void Remove(IProcess process)
        {
            RemoveProcessEvents(process);

            IProcess removedProcess = null;
            processes.TryRemove(process.GetHashCode(), out removedProcess);
        }

        public bool HasProcess(IProcess process)
        {
            return processes.ContainsKey(process.GetHashCode());
        }

        protected virtual void OnOutputDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            var handlers = OutputDataReceived;
            if (handlers != null)
            {
                handlers(this, e);
            }
        }

        protected virtual void OnErrorDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            var handlers = ErrorDataReceived;
            if (handlers != null)
            {
                handlers(this, e);
            }
        }

        private void ProcessExited(object sender, EventArgs e)
        {
            IProcess p = sender as IProcess;
            if (p != null)
            {
                IProcess removedProcess = null;
                if (processes.TryRemove(p.GetHashCode(), out removedProcess))
                {
                    RemoveProcessEvents(removedProcess);
                }
            }
        }

        private void AddProcessEvents(IProcess process)
        {
            process.Exited += ProcessExited;
            process.OutputDataReceived += OnOutputDataReceived;
            process.ErrorDataReceived += OnErrorDataReceived;
        }

        private void RemoveProcessEvents(IProcess process)
        {
             process.Exited -= ProcessExited;
             process.OutputDataReceived -= OnOutputDataReceived ;
             process.ErrorDataReceived -= OnErrorDataReceived;
        }

    }
}
