using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Container;
using IronFoundry.Warden.Protocol;
using IronFoundry.Warden.Utilities;
using NLog;

namespace IronFoundry.Warden.Tasks
{
    public abstract class ProcessCommand: RemoteCommand
    {
        public const string EnvLogRelativePath = @"logs\env.log";
        public const string PidLogRelativePath = "run.pid";

        private readonly StringBuilder stdout = new StringBuilder();
        private readonly StringBuilder stderr = new StringBuilder();

        private readonly Logger log = LogManager.GetCurrentClassLogger();

        protected override TaskCommandResult Invoke()
        {
            return DoExecute();
        }

        protected abstract TaskCommandResult DoExecute();

        protected TaskCommandResult RunProcess(string workingDirectory, string executable, string[] processArguments)
        {
            string exePath = this.Container.ConvertToUserPathWithin(executable);

            string workingDir = String.IsNullOrWhiteSpace(workingDirectory)
                ? this.Container.Directory.UserPath
                : this.Container.ConvertToUserPathWithin(workingDirectory);

            var environment = this.CommandArgs.Environment.ToDictionary(kv => kv.Key,
                kv => this.Container.ConvertToUserPathWithin(kv.Value));

            var privileged = this.CommandArgs.Privileged;

            var processArgs = processArguments ?? new string[0];
            log.Trace("Running process{0} (WorkingDir {1}): {2} Args: {3}",
                privileged ? " (privileged)" : " (non-privileged)",
                workingDir,
                exePath,
                string.Join(" ", processArgs)
                );

            var spec = new ProcessSpec
            {
                ExecutablePath = exePath,
                Arguments = processArguments,
                WorkingDirectory = workingDir,
                Environment = environment,
                DisablePathMapping = true,
                Privileged = privileged,
            };

            IContainerProcess process = null;

            process = this.Container.Run(spec, this.IO);

            log.Trace("Process ID: '{0}'", process.Id);

            // These aren't in a using block intentionally - we don't want to
            // delete the files because they might be useful for debugging
            // if the app crashes.
            CreatePidLog(process.Id);
            CreateProcessEnvLog(process.Environment);

            int exitCode = process.WaitForExit();

            log.Trace("Process ended with exit code: {0}", exitCode);

            return new TaskCommandResult(exitCode, null, null);
        }

        /// <summary>
        /// Create a log in the container holding the id of the process.
        /// </summary>
        /// <returns></returns>
        private TempFile CreatePidLog(int pid)
        {
            var tempFile = this.Container.FileInContainer(PidLogRelativePath);

            System.IO.File.WriteAllText(tempFile.FullName, pid.ToString(), Encoding.ASCII);

            return tempFile;
        }

        /// <summary>
        /// Create a log in the container with the environment variables used to start the process.
        /// </summary>
        private TempFile CreateProcessEnvLog(IReadOnlyDictionary<string, string> processEnv)
        {
            var tempFile = this.Container.FileInContainer(EnvLogRelativePath);

            var envLogBuilder = new StringBuilder();
            foreach (var kv in processEnv)
            {
                string line = string.Format("{0}={1}", kv.Key, kv.Value);
                envLogBuilder.AppendLine(line);
            }

            System.IO.File.WriteAllText(tempFile.FullName, envLogBuilder.ToString(), Encoding.UTF8);

            return tempFile;
        }
    }
}
