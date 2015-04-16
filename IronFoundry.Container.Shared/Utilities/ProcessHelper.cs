﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using IronFoundry.Container.Win32;

namespace IronFoundry.Container.Utilities
{
    internal class ProcessHelper
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
            readonly Process process;

            public RealProcessWrapper(Process process)
            {
                this.process = process;

                this.process.Exited += WrappedExited;

                this.process.OutputDataReceived += WrappedOutputDataReceived;
                this.process.ErrorDataReceived += WrappedErrorDataReceived;

                //this.Environment = this.process.StartInfo.EnvironmentVariables.
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

            public IReadOnlyDictionary<string, string> Environment
            {
                get
                {
                    var dictionary = process.StartInfo.EnvironmentVariables.ToDictionary(StringComparer.OrdinalIgnoreCase);
                    return new ReadOnlyDictionary<string, string>(dictionary);
                }
            }


            public long PrivateMemoryBytes
            {
                get { return process.PrivateMemorySize64; }
            }

            public TextReader StandardOutput
            {
                get { return process.StandardOutput; }
            }

            public TextReader StandardError
            {
                get { return process.StandardError; }
            }

            public TextWriter StandardInput
            {
                get { return process.StandardInput; }
            }

            public void Dispose()
            {
                process.Dispose();
            }

            public void Kill()
            {
                // FROM: https://msdn.microsoft.com/en-us/library/windows/desktop/ms681382%28v=vs.85%29.aspx
                const int accessDeniedErrorCode = 0x5;

                if (process.HasExited) return;
                try
                {
                    process.Kill();
                }
                catch (Win32Exception ex)
                {
                    // Access Denied can be thrown if we attempt to kill a process while it is exiting.
                    // See notes at: https://msdn.microsoft.com/en-us/library/system.diagnostics.process.kill%28v=vs.110%29.aspx
                    if (ex.ErrorCode != accessDeniedErrorCode)
                    {
                        throw;
                    }
                }
            }

            public void RequestExit()
            {
                // MO: We can't do this anymore.  RequestForExit as currently implemented uses
                // GenerateConsoleCtrlEvent which will send the request to the whole console
                // group.  At the moment that console group includes the ContainerHost.  We don't
                // want to kill the ContainerHost.
                //
                //ProcessHelper.SendSignal(process.Id, false);
                Kill();
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
