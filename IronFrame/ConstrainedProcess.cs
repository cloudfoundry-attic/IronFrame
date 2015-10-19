using System;
using System.Collections.Generic;
using System.Threading;
using IronFrame.Messages;
using IronFrame.Utilities;

namespace IronFrame
{
    internal class ConstrainedProcess : IProcess
    {
        readonly IContainerHostClient hostClient;
        readonly Guid key;
        readonly int id;

        int? exitCode = 0;

        public ConstrainedProcess(
            IContainerHostClient hostClient, 
            Guid key, 
            int id,
            Dictionary<string,string> environment)
        {
            this.hostClient = hostClient;
            this.key = key;
            this.id = id;
            this.Environment = environment;
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

        public IReadOnlyDictionary<string, string> Environment { get; private set; }

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
            hostClient.StopProcess(key, ConstrainedProcessRunner.DefaultStopTimeout);
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
