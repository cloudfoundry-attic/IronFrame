using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using IronFrame.Win32;
using NLog;

namespace IronFrame.Utilities
{
    internal class ProcessRunner : IProcessRunner
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        static readonly string[] EmptyArguments = new string[0];

        public void Dispose()
        {
        }

        public IProcess Run(ProcessRunSpec runSpec)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = runSpec.ExecutablePath,
                Arguments = String.Join(" ", runSpec.Arguments ?? EmptyArguments),
                WorkingDirectory = runSpec.WorkingDirectory,
                UseShellExecute = false,
                LoadUserProfile = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            };

            if (runSpec.Credentials != null)
            {
                startInfo.UserName = runSpec.Credentials.UserName;
                startInfo.Password = runSpec.Credentials.SecurePassword;
            }

            bool runSpecSpecifiesEnvironment = runSpec.Environment != null && runSpec.Environment.Count > 0;
            var environment = runSpecSpecifiesEnvironment
                ? runSpec.Environment
                : CreateDefaultProcessEnvironment(runSpec.Credentials);

            startInfo.EnvironmentVariables.Clear();
            foreach (var variable in environment)
            {
                startInfo.EnvironmentVariables[variable.Key] = variable.Value;
            }

            Process p = new Process
            {
                StartInfo = startInfo,
            };

            p.EnableRaisingEvents = true;
            
            var wrapped = ProcessHelper.WrapProcess(p);

            if (!runSpec.BufferedInputOutput)
            {
                if (runSpec.OutputCallback != null)
                {
                    p.OutputDataReceived += (sender, e) =>
                    {
                        runSpec.OutputCallback(e.Data);
                    };
                }

                if (runSpec.ErrorCallback != null)
                {
                    p.ErrorDataReceived += (sender, e) =>
                    {
                        runSpec.ErrorCallback(e.Data);
                    };
                }
            }

            log.Trace("Starting Process - FileName: {0} Arguments: {1} WorkingDirectory: {2}", 
                p.StartInfo.FileName,
                p.StartInfo.Arguments,
                p.StartInfo.WorkingDirectory);

            bool started = p.Start();
            Debug.Assert(started); // TODO: Should we throw an exception here? Fail fast?

            if (!runSpec.BufferedInputOutput)
            {
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }

            return wrapped;
        }

        public void StopAll(bool kill)
        {
        }

        /// <summary>
        /// Returns default environment for the process.
        /// If credentials are specified then the default environment is the default for that user.
        /// Otherwise the default is to inherit from this process.
        /// </summary>
        private Dictionary<string, string> CreateDefaultProcessEnvironment(NetworkCredential credential)
        {
            EnvironmentBlock envBlock = new EnvironmentBlock();

            if (credential == null)
            {
                envBlock = EnvironmentBlock.Create(Environment.GetEnvironmentVariables());
            }
            else
            {
                using (var safeUserToken = Utils.LogonAndGetUserPrimaryToken(credential))
                {
                    envBlock = EnvironmentBlock.CreateForUser(safeUserToken.DangerousGetHandle());
                }
            }

            return envBlock.ToDictionary();
        }
    }
}
