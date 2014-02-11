using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Protocol;
using IronFoundry.Warden.Utilities;
using NLog;

namespace IronFoundry.Warden.Tasks
{
    public abstract class ProcessCommand : TaskCommand
    {
        private readonly StringBuilder stdout = new StringBuilder();
        private readonly StringBuilder stderr = new StringBuilder();

        private readonly bool shouldImpersonate = false;
        private readonly ResourceLimits rlimits;

        private readonly Logger log = LogManager.GetCurrentClassLogger();
        public ProcessCommand(Container container, string[] arguments, bool shouldImpersonate, ResourceLimits rlimits)

            : base(container, arguments)
        {
            this.shouldImpersonate = shouldImpersonate;
            this.rlimits = rlimits;
        }

        public override TaskCommandResult Execute()
        {
            return DoExecute();
        }

        /*
         * Asynchronous execution
         */
        public event EventHandler<TaskCommandStatusEventArgs> StatusAvailable;

        public Task<TaskCommandResult> ExecuteAsync()
        {
            return Task.Run<TaskCommandResult>((Func<TaskCommandResult>)DoExecute);
        }

        protected abstract TaskCommandResult DoExecute();

        protected TaskCommandResult RunProcess(string workingDirectory, string executable, string processArguments)
        {
            log.Trace("Running process{0}: {1} {2}", shouldImpersonate ? " (as warden user)" : String.Empty,  executable, processArguments);

            using (var process = new BackgroundProcess(workingDirectory, executable, processArguments, GetImpersonatationCredential()))
            {
                process.ErrorDataReceived += process_ErrorDataReceived;
                process.OutputDataReceived += process_OutputDataReceived;

                Action<Process> postStartAction = (Process p) =>
                    {
                        log.Trace("Process ID: '{0}'", p.Id);
                        container.AddProcess(p, rlimits);
                    };
                process.StartAndWait(asyncOutput: true, postStartAction: postStartAction);

                process.ErrorDataReceived -= process_ErrorDataReceived;
                process.OutputDataReceived -= process_OutputDataReceived;

                string sout = stdout.ToString();
                string serr = stderr.ToString();

                log.Trace("Process ended with exit code: {0}", process.ExitCode);

                return new TaskCommandResult(process.ExitCode, sout, serr);
            }
        }

        private NetworkCredential GetImpersonatationCredential()
        {
            NetworkCredential impersonationCredential = null;
            if (shouldImpersonate)
            {
                impersonationCredential = container.GetCredential();
            }
            return impersonationCredential;
        }

        private void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                string outputLine = e.Data + '\n';
                stdout.Append(outputLine);
                OnStatusAvailable(new TaskCommandStatus(null, outputLine, null));
            }
        }

        private void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                string outputLine = e.Data + '\n';
                stderr.Append(outputLine);
                OnStatusAvailable(new TaskCommandStatus(null, null, outputLine));
            }
        }

        private void OnStatusAvailable(TaskCommandStatus status)
        {
            if (StatusAvailable != null)
            {
                StatusAvailable(this, new TaskCommandStatusEventArgs(status));
            }
        }
    }
}
