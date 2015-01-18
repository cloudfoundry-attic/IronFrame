using System;
using System.Threading;
using IronFoundry.Container.Messages;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Container
{
    public class ConstrainedProcess : IProcess
    {
        readonly IContainerHostClient hostClient;
        readonly Guid key;
        readonly int id;
        readonly Action<string> outputCallback;
        readonly Action<string> errorCallback;

        int? exitCode = 0;

        public ConstrainedProcess(
            IContainerHostClient hostClient, 
            Guid key, 
            int id, 
            Action<string> outputCallback, 
            Action<string> errorCallback)
        {
            this.hostClient = hostClient;
            this.key = key;
            this.id = id;
            this.outputCallback = outputCallback;
            this.errorCallback = errorCallback;

            hostClient.SubscribeToProcessData(key, HandleProcessData);
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

        void HandleProcessData(ProcessDataEvent processData)
        {
            switch (processData.dataType)
            {
                case ProcessDataType.STDOUT:
                    if (outputCallback != null)
                        outputCallback(processData.data);
                    break;

                case ProcessDataType.STDERR:
                    if (errorCallback != null)
                        errorCallback(processData.data);
                    break;
            }
        }
    }
}
