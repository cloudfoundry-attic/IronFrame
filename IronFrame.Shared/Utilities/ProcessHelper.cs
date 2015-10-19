using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using IronFrame.Win32;

namespace IronFrame.Utilities
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
            var ctrlEvent = kill
                ? NativeMethods.ConsoleControlEvent.ControlBreak
                : NativeMethods.ConsoleControlEvent.ControlC;

            if (!NativeMethods.GenerateConsoleCtrlEvent(ctrlEvent, processId))
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "Error sending signal to process (id " + processId + ").");
        }

        public static IProcess WrapProcess(Process process)
        {
            return new RealProcessWrapper(process);
        }

        private class RealProcessWrapper : IProcess
        {
            private readonly Process process;

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
                    var dictionary =
                        process.StartInfo.EnvironmentVariables.ToDictionary(StringComparer.OrdinalIgnoreCase);
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
                TerminateProcessTree(Handle, (uint) Id, -26);
            }

            ///<summary>
            /// Terminate a process tree
            ///</summary>
            ///<param name="hProcess">The handle of the process</param>
            ///<param name="processID">The ID of the process</param>
            ///<param name="exitCode">The exit code of the process</param>
            public static void TerminateProcessTree(IntPtr hProcess, uint processID, int exitCode)
            {
                // Retrieve all processes on the system
                Process[] processes = Process.GetProcesses();
                foreach (Process p in processes)
                {
                    // Get some basic information about the process
                    PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
                    try
                    {
                        uint bytesWritten;
                        NtQueryInformationProcess(p.Handle,
                            0, ref pbi, (uint) Marshal.SizeOf(pbi),
                            out bytesWritten); // == 0 is OK

                        // Is it a child process of the process we're trying to terminate?
                        if ((int) pbi.InheritedFromUniqueProcessId == processID)
                            // The terminate the child process and its child processes
                            TerminateProcessTree(p.Handle, (uint) pbi.UniqueProcessId, exitCode);
                    }
                    catch (Exception /* ex */)
                    {
                        // Ignore, most likely 'Access Denied'
                    }
                }

                // Finally, termine the process itself:
                TerminateProcess((uint) hProcess, exitCode);
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct PROCESS_BASIC_INFORMATION
            {
                public IntPtr ExitStatus;
                public IntPtr PebBaseAddress;
                public IntPtr AffinityMask;
                public IntPtr BasePriority;
                public IntPtr UniqueProcessId;
                public IntPtr InheritedFromUniqueProcessId;
            }

            [DllImport("kernel32.dll")]
            private static extern bool TerminateProcess(uint hProcess, int exitCode);

            [DllImport("ntdll.dll")]
            private static extern int NtQueryInformationProcess(
                IntPtr hProcess,
                int processInformationClass /* 0 */,
                ref PROCESS_BASIC_INFORMATION processBasicInformation,
                uint processInformationLength,
                out uint returnLength
                );

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