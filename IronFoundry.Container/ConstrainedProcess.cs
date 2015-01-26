using System;
using System.Threading;
using IronFoundry.Container.Messages;
using IronFoundry.Container.Utilities;

namespace IronFoundry.Container
{
    public class ConstrainedProcess : IProcess
    {
        readonly IContainerHostClient hostClient;
        readonly Guid key;
        readonly int id;

        int? exitCode = 0;

        public ConstrainedProcess(
            IContainerHostClient hostClient, 
            Guid key, 
            int id)
        {
            this.hostClient = hostClient;
            this.key = key;
            this.id = id;
        }

        public int ExitCode
        {
            get
            {
                if (!exitCode.HasValue)
                    throw new InvalidOperationException("The process has not exited.");

                return exitCode.Value;
            }
        }

        public IntPtr Handle
        {
            get { throw new NotImplementedException(); }
        }

        public int Id
        {
            get { return id; }
        }

        public long PrivateMemoryBytes
        {
            get { throw new NotImplementedException(); }
        }

#pragma warning disable 0067
        public event EventHandler Exited;

        public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;

        public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;
#pragma warning restore

        public void Kill()
        {
            throw new NotImplementedException();
        }

        public void WaitForExit()
        {
            WaitForExit(Timeout.Infinite);
        }

        public bool WaitForExit(int milliseconds)
        {
            var @params = new WaitForProcessExitParams
            {
                key = key,
                timeout = milliseconds,
            };

            var result = hostClient.WaitForProcessExit(@params);
            if (result.exited)
                exitCode = result.exitCode;

            return result.exited;
        }

        public void RequestExit()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }


        public System.IO.TextReader StandardOutput
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public System.IO.TextReader StandardError
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public System.IO.TextWriter StandardInput
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
