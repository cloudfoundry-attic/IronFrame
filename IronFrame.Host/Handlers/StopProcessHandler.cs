using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using IronFrame.Messages;
using IronFrame.Utilities;

namespace IronFrame.Host.Handlers
{
    internal class StopProcessHandler
    {
        private readonly IProcessTracker processTracker;

        public StopProcessHandler(IProcessTracker processTracker)
        {
            this.processTracker = processTracker;
        }

        public Task ExecuteAsync(StopProcessParams p)
        {
            var process = processTracker.GetProcessByKey(p.key);

            return StopProcessAsync(process, p.timeout);
        }

        public static Task StopProcessAsync(IProcess process, int timeout)
        {
            if (process == null)
                return Task.FromResult<object>(null);

            return Task.Run(
                () =>
                {
                    process.RequestExit();
                    if (!process.WaitForExit(timeout))
                        process.Kill();

                    TerminateProcessTree(process.Handle, (uint) process.Id, -26);
                }
            );
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
                      0, ref pbi, (uint)Marshal.SizeOf(pbi),
                      out bytesWritten); // == 0 is OK

                    // Is it a child process of the process we're trying to terminate?
                    if ((int)pbi.InheritedFromUniqueProcessId == processID)
                        // The terminate the child process and its child processes
                        TerminateProcessTree(p.Handle, (uint)pbi.UniqueProcessId, exitCode);
                }
                catch (Exception /* ex */)
                {
                    // Ignore, most likely 'Access Denied'
                }
            }

            // Finally, termine the process itself:
            TerminateProcess((uint)hProcess, exitCode);
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
        static extern bool TerminateProcess(uint hProcess, int exitCode);
        [DllImport("ntdll.dll")]
        static extern int NtQueryInformationProcess(
           IntPtr hProcess,
           int processInformationClass /* 0 */,
           ref PROCESS_BASIC_INFORMATION processBasicInformation,
           uint processInformationLength,
           out uint returnLength
        );
    }
}