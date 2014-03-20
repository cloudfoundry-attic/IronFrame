using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Shared.Messaging;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Containers
{
    public class ProcessLauncher : IDisposable
    {
        string hostExe = "IronFoundry.Warden.ContainerHost.exe";
        Process hostProcess;
        MessageTransport messageTransport;
        MessagingClient messagingClient;

        public int HostProcessId
        {
            get { return hostProcess != null ? hostProcess.Id : 0; }
        }

        public virtual void Dispose()
        {
            if (hostProcess != null)
            {
                if (!hostProcess.HasExited)
                    hostProcess.SafeKill();

                hostProcess.Dispose();
                hostProcess = null;
            }
        }

        public virtual IProcess LaunchProcess(CreateProcessStartInfo si, JobObject jobObject)
        {
            if (hostProcess == null)
            {
                var hostFullPath = Path.Combine(Directory.GetCurrentDirectory(), hostExe);
                var hostStartInfo = new ProcessStartInfo(hostFullPath);
                hostStartInfo.RedirectStandardInput = true;
                hostStartInfo.RedirectStandardOutput = true;
                hostStartInfo.RedirectStandardError = true;
                hostStartInfo.UseShellExecute = false;

                hostProcess = Process.Start(hostStartInfo);

                messageTransport = new MessageTransport(hostProcess.StandardOutput, hostProcess.StandardInput);
                messagingClient = new MessagingClient(message => 
                {
                    messageTransport.PublishAsync(message).GetAwaiter().GetResult();
                });
                messageTransport.SubscribeResponse(message =>
                {
                    messagingClient.PublishResponse(message);
                    return Task.FromResult(0);
                });

                jobObject.AssignProcessToJob(hostProcess);
            }

            return RequestStartProcessAsync(si)
                .GetAwaiter()
                .GetResult();
        }

        private async Task<IProcess> RequestStartProcessAsync(CreateProcessStartInfo si)
        {
            CreateProcessRequest request = new CreateProcessRequest(si);
            CreateProcessResponse response = null;
            
            try
            {
                response = await messagingClient.SendMessageAsync<CreateProcessRequest, CreateProcessResponse>(request);
            }
            catch (MessagingException ex)
            {
                throw ProcessLauncherError(ex);
            }

            Process process = null;
            try
            {
                process = Process.GetProcessById(response.result.Id);
            }
            catch (ArgumentException)
            {
            }

            if (process == null)
            {
                // The process was unable to start or has died prematurely
                var exitInfo = await GetProcessExitInfoAsync(response.result.Id);
                if (exitInfo.HasExited && exitInfo.ExitCode == 0)
                {
                    return new ExitedProcess
                    {
                        Id = response.result.Id,
                        Handle = IntPtr.Zero,
                        HasExited = exitInfo.HasExited,
                        ExitCode = exitInfo.ExitCode,
                    };
                }
                else
                {
                    var builder = new StringBuilder();
                    builder.AppendLine(exitInfo.StandardError);
                    builder.AppendLine(exitInfo.StandardOutputTail);

                    var message = String.Format(
                        "Process was unable to start or died prematurely. Process exit code was {0}.\n{1}", 
                        exitInfo.ExitCode, 
                        builder.ToString());

                    throw ProcessLauncherError(message, exitInfo.ExitCode, builder.ToString());
                }
            }

            return new RealProcessWrapper(this, process);
        }

        private async Task<GetProcessExitInfoResult> GetProcessExitInfoAsync(int processId)
        {
            GetProcessExitInfoRequest request = new GetProcessExitInfoRequest(
                new GetProcessExitInfoParams
                {
                    Id = processId
                });
            GetProcessExitInfoResponse response = null;

            try
            {
                response = await messagingClient.SendMessageAsync<GetProcessExitInfoRequest, GetProcessExitInfoResponse>(request);
            }
            catch (MessagingException ex)
            {
                throw ProcessLauncherError(ex);
            }

            return response.result;
        }

        private ProcessLauncherException ProcessLauncherError(string message, int code, string remoteData, Exception innerException = null)
        {
            return new ProcessLauncherException(message, innerException)
            {
                Code = code,
                RemoteData = remoteData,
            };
        }

        private ProcessLauncherException ProcessLauncherError(MessagingException ex)
        {
            var errorInfo = ex.ErrorResponse.error;
            return ProcessLauncherError(errorInfo.Message, errorInfo.Code, errorInfo.Data, ex);
        }

        class ExitedProcess : IProcess
        {
            public int ExitCode { get; set; }
            public IntPtr Handle { get; set; }
            public bool HasExited { get; set; }
            public int Id { get; set; }
            
            public TimeSpan TotalProcessorTime
            {
                get { return TimeSpan.Zero; }
            }

            public TimeSpan TotalUserProcessorTime
            {
                get { return TimeSpan.Zero; }
            }

            public long PrivateMemoryBytes
            {
                get { return 0; }
            }

            public long PagedMemoryBytes
            {
                get { return 0; }
            }

            public long WorkingSet
            {
                get { return 0; }
            }

            EventHandler exited;
            public event EventHandler Exited
            {
                add { exited += value; }
                remove { exited -= value; }
            }

            public void Kill()
            {
            }

            public void WaitForExit()
            {
            }

            public void WaitForExit(int timeout)
            {
            }

            public void Dispose()
            {
            }
        }

        class RealProcessWrapper : IProcess
        {
            private readonly ProcessLauncher launcher;
            private readonly Process process;
            public event EventHandler Exited;

            public RealProcessWrapper(ProcessLauncher launcher, Process process)
            {
                this.launcher = launcher;
                this.process = process;
                Id = process.Id;
                process.Exited += (o, e) => this.OnExited();
            }

            public int Id { get; private set; }

            public int ExitCode
            {
                get
                {
                    // TODO: This is a hack that we added temporarily to unblock testing.
                    // Revisit how we get the exit code for the process!
                    var exitInfo = launcher.GetProcessExitInfoAsync(Id)
                        .GetAwaiter()
                        .GetResult();

                    if (!exitInfo.HasExited)
                        throw new InvalidOperationException("The process has not exited.");

                    return exitInfo.ExitCode;
                }
            }

            public IntPtr Handle
            {
                get { return process.Handle; }
            }

            public bool HasExited
            {
                get
                {
                    // TODO: This is a hack that we added temporarily to unblock testing.
                    // Revisit how we get the exit code for the process!
                    var exitInfo = launcher.GetProcessExitInfoAsync(Id)
                        .GetAwaiter()
                        .GetResult();

                    return exitInfo.HasExited;
                }
            }

            public TimeSpan TotalProcessorTime
            {
                get { return process.HasExited ? TimeSpan.Zero : process.TotalProcessorTime; }
            }

            public TimeSpan TotalUserProcessorTime
            {
                get { return process.HasExited ? TimeSpan.Zero : process.UserProcessorTime; }
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

            public long PagedMemoryBytes
            {
                get { return process.PagedMemorySize64; }
            }

            public long WorkingSet
            {
                get 
                { 
                    return process.HasExited ? 0 : process.WorkingSet64; 
                }
            }

            public void Dispose()
            {
                process.Dispose();
            }

            public void WaitForExit()
            {
                process.WaitForExit();
            }

            public void WaitForExit(int timeout)
            {
                process.WaitForExit(timeout);
            }
        }
    }

    [Serializable]
    public class ProcessLauncherException : Exception
    {
        public ProcessLauncherException() { }
        public ProcessLauncherException(string message) : base(message) { }
        public ProcessLauncherException(string message, Exception inner) : base(message, inner) { }
        protected ProcessLauncherException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public int Code { get; set; }
        public string RemoteData { get; set; }
    }
}
