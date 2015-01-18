using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using IronFoundry.Container.Win32;

namespace IronFoundry.Warden.Utilities
{
    // BR: Move this to IronFoundry.Container.Shared
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

        public static void SendSignal(int processId, bool kill)
        {
            var ctrlEvent = kill ? 
                NativeMethods.ConsoleControlEvent.ControlBreak : 
                NativeMethods.ConsoleControlEvent.ControlC;

            if (!NativeMethods.GenerateConsoleCtrlEvent(ctrlEvent, processId))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Error sending signal to process (id " + processId + ").");
        }

        public static IProcess WrapProcess(Process process)
        {
            return new RealProcessWrapper(process);
        }

        class RealProcessWrapper : IProcess
        {
            private readonly Process process;

            public RealProcessWrapper(Process process)
            {
                this.process = process;

                this.process.Exited += WrappedExited;
                this.process.OutputDataReceived += WrappedOutputDataReceived;
                this.process.ErrorDataReceived += WrappedErrorDataReceived;
            }

            public int Id
            {
                get { return process.Id; }
            }

            public int ExitCode
            {
                get { return process.ExitCode; }
            }

            public IntPtr Handle
            {
                get { return process.Handle; }
            }

            public long PrivateMemoryBytes
            {
                get { return process.PrivateMemorySize64; }
            }

            public void Dispose()
            {
                process.Dispose();
            }

            public void Kill()
            {
                if (process.HasExited) return;
                process.Kill();
            }

            public void RequestExit()
            {
                //ProcessHelper.SendSignal(process.Id, false);
            }

            public void WaitForExit()
            {
                process.WaitForExit();
            }

            public bool WaitForExit(int milliseconds)
            {
                return process.WaitForExit(milliseconds);
            }

            private void WrappedErrorDataReceived(object sender, DataReceivedEventArgs e)
            {
                OnErrorDataReceived(this, new ProcessDataReceivedEventArgs(e.Data));
            }

            private void WrappedOutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                OnOutputDataReceived(this, new ProcessDataReceivedEventArgs(e.Data));
            }

            private void WrappedExited(object sender, EventArgs e)
            {
                OnExited(sender, e);
            }

            public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
            protected virtual void OnOutputDataReceived(object sender, ProcessDataReceivedEventArgs e)
            {
                var handlers = OutputDataReceived;
                if (handlers != null)
                {
                    handlers(this, e);
                }
            }

            public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;
            protected virtual void OnErrorDataReceived(object sender, ProcessDataReceivedEventArgs e)
            {
                var handlers = ErrorDataReceived;
                if (handlers != null)
                {
                    handlers(this, e);
                }
            }

            public event EventHandler Exited;
            protected virtual void OnExited(object sender, EventArgs e)
            {
                var handlers = Exited;
                if (handlers != null)
                {
                    handlers(this, e);
                }

                process.ErrorDataReceived -= WrappedErrorDataReceived;
                process.OutputDataReceived -= WrappedOutputDataReceived;
            }
        }
    }
}
