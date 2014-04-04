using System;
using NLog;

namespace IronFoundry.Warden.Utilities
{
    public class NetShRunner : INetShRunner
    {
        private static readonly string workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        public bool AddRule(ushort port, string userName)
        {
            string arguments = String.Format("http add urlacl http://*:{0}/ user={1}", port, userName);
            return RunNetsh(arguments);
        }

        public bool DeleteRule(ushort port)
        {
            string arguments = String.Format("http delete urlacl http://*:{0}/", port);
            return RunNetsh(arguments);
        }

        private bool RunNetsh(string arguments)
        {
            log.Info("Running netsh.exe for {0}", arguments);

            bool success;
            using (var process = new BackgroundProcess(workingDirectory, "netsh.exe", arguments))
            {
                process.StartAndWait(false);
                success = process.ExitCode == 0;
            }

            return success;
        }
    }
}