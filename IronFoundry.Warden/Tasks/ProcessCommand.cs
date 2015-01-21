using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

namespace IronFoundry.Warden.Tasks
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Containers;
    using IronFoundry.Warden.Shared.Messaging;
    using NLog;
    using Protocol;
    using Utilities;
    using IronFoundry.Warden.Containers.Messages;

    public abstract class ProcessCommand : TaskCommand
    {
        public const string EnvLogRelativePath = @"logs\env.log";
        public const string PidLogRelativePath = "run.pid";

        private readonly StringBuilder stdout = new StringBuilder();
        private readonly StringBuilder stderr = new StringBuilder();

        private readonly bool privileged;
        private readonly ResourceLimits rlimits;
        private readonly IDictionary<string, string> environment; 

        private readonly Logger log = LogManager.GetCurrentClassLogger();

        public ProcessCommand(IContainer container, string[] arguments, bool privileged, IDictionary<string, string> environment, ResourceLimits rlimits)

            : base(container, arguments)
        {
            this.privileged = privileged;
            this.rlimits = rlimits;
            this.environment = PrepareEnvironment(environment);
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
            string exePath = container.ConvertToPathWithin(executable);
            
            string workingDir = workingDirectory.IsNullOrWhiteSpace()
                ? container.ContainerDirectoryPath
                : container.ConvertToPathWithin(workingDirectory);

            log.Trace("Running process{0} (WorkingDir {1}): {2} {3}", 
                privileged ? " (privileged)" : " (non-privileged)", 
                workingDir,
                exePath, 
                processArguments);

            var si = new CreateProcessStartInfo(exePath, processArguments)
            {
                WorkingDirectory = workingDir,
                EnvironmentVariables = environment
            };
            
            var process = container.CreateProcess(si, !privileged);
            
            log.Trace("Process ID: '{0}'", process.Id);

            // These aren't in a using block intentionally - we don't want to
            // delete the files because they might be useful for debugging
            // if the app crashes.
            CreatePidLog(process.Id);
            CreateProcessEnvLog(si.EnvironmentVariables);
            
            process.WaitForExit();

            int exitCode = process.ExitCode;
            log.Trace("Process ended with exit code: {0}", exitCode);

            return new TaskCommandResult(exitCode, null, null);
        }

        /// <summary>
        /// Create a log in the container holding the id of the process.
        /// </summary>
        /// <returns></returns>
        private TempFile CreatePidLog(int pid)
        {
            var tempFile = container.FileInContainer(PidLogRelativePath);

            System.IO.File.WriteAllText(tempFile.FullName, pid.ToString(), Encoding.ASCII);
            
            return tempFile;
        }

        /// <summary>
        /// Create a log in the container with the environment variables used to start the process.
        /// </summary>
        private TempFile CreateProcessEnvLog(IDictionary<string, string> processEnv)
        {
            var tempFile = container.FileInContainer(EnvLogRelativePath);
            
            var envLogBuilder = new StringBuilder();
            foreach (var kv in processEnv)
            {
                string line = string.Format("{0}={1}", kv.Key, kv.Value);
                envLogBuilder.AppendLine(line);
            }

            System.IO.File.WriteAllText(tempFile.FullName, envLogBuilder.ToString(), Encoding.UTF8);

            return tempFile;
        }

        /// <summary>
        /// Prepare the complete environment block that will be used based on the environment
        /// variables that were specified in the request.
        /// </summary>
        /// <remarks>
        /// When starting a process, you can let the process load the default environment or 
        /// you can specify the exact variables to use by setting ProcessStartInfo.EnvironmentVariables.
        /// 
        /// If you specify the value of any variable, then you have to specify all of them, no defaults
        /// will be provided
        /// 
        /// This method will generate the default environment variables to use and then upsert the ones
        /// specified.  
        /// </remarks>
        /// <remarks>
        /// For the variable values specified, any @ROOT@ prefixes found, will be replaced
        /// with the full path to the container root.
        /// </remarks>
        private IDictionary<string, string> PrepareEnvironment(IDictionary<string, string> env)
        {
            if (env.IsNullOrEmpty())
            {
                return new Dictionary<string, string>(0);
            }

            // Root the provided environment values
            var rootedEnv = env.ToDictionary(kv => kv.Key, kv => container.ConvertToPathWithin(kv.Value));

            // Generate a new environment containing all the default values.
            EnvironmentBlock defaultEnv = EnvironmentBlock.GenerateDefault();

            // Merge the default envs with the machine and user provided variables
            var envHash = defaultEnv.Merge(rootedEnv).ToDictionary();

            return envHash;
        }

        /// <summary>
        /// Converts all the paths starting with @ROO@ in the environment values and
        /// returns a new dictionary with the converted values.  @ROOT@ will be replaced
        /// with the full path of the container root.
        /// </summary>
        private IDictionary<string, string> ConvertEnvironmentPaths(IDictionary<string, string> env)
        {
            return env.ToDictionary(kv => kv.Key, kv => container.ConvertToPathWithin(kv.Value));
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
