﻿using System;
using IronFoundry.Container.Utilities;
using NLog;

namespace IronFoundry.Container.Utilities
{
    internal interface INetShRunner
    {
        bool AddRule(int port, string userName);
        bool DeleteRule(int port);
    }

    internal class NetShRunner : INetShRunner
    {
        private static readonly string workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        public bool AddRule(int port, string userName)
        {
            string arguments = String.Format("http add urlacl http://*:{0}/ user={1}", port, userName);
            return RunNetsh(arguments);
        }

        public bool DeleteRule(int port)
        {
            string arguments = String.Format("http delete urlacl http://*:{0}/", port);
            return RunNetsh(arguments);
        }

        private bool RunNetsh(string arguments)
        {
            log.Info("Running netsh.exe for {0}", arguments);

            bool success;
            using (ProcessRunner runner = new ProcessRunner())
            {
                var spec = new ProcessRunSpec
                {
                    ExecutablePath = "netsh.exe",
                    Arguments = new[] {arguments},
                    WorkingDirectory = workingDirectory
                };

                var process = runner.Run(spec);
                process.WaitForExit();
                success = process.ExitCode == 0;
            }

            return success;
        }
    }
}