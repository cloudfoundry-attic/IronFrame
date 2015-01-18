using System;
using System.Diagnostics;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Container
{
    // BR: Move this to IronFoundry.Container.Shared
    public class ContainerProcess
    {
        readonly IProcess process;

        public ContainerProcess(IProcess process)
        {
            this.process = process;
        }

        public int Id
        {
            get { return process.Id; }
        }

        public void Kill()
        {
            process.Kill();
        }

        public int WaitForExit()
        {
            process.WaitForExit();
            return process.ExitCode;
        }

        /// <summary>
        /// Wait for the process to exit up to the specified time
        /// </summary>
        /// <returns>
        /// True if the process exited
        /// </returns>
        /// <param name="milliseconds">The max time for the process to exit</param>
        /// <param name="exitCode">Populated with the exit code of the process if it exited</param>
        public bool TryWaitForExit(int milliseconds, out int exitCode)
        {
            bool exited = false;
            exitCode = 0;

            if (process.WaitForExit(milliseconds))
            {
                exitCode = process.ExitCode;
                exited = true;
            }

            return exited;
        }
    }
}
