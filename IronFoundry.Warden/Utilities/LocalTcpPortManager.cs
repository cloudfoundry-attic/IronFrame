using System;
using NLog;

namespace IronFoundry.Warden.Utilities
{
    public class LocalTcpPortManager
    {
        private static readonly string workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly ushort port;
        private readonly string userName;
        private readonly string firewallRuleName;

        public LocalTcpPortManager(ushort port, string userName)
        {
            this.port = port;
            if (this.port == default(ushort))
            {
                this.port = IPUtilities.RandomFreePort();
            }

            if (userName.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("userName");
            }
            this.userName = userName;

            this.firewallRuleName = String.Format("{0}-{1}", this.userName, this.port);
        }

        /// <summary>
        /// netsh http add urlacl http://*:8888/ user=warden_094850238
        /// </summary>
        public ushort ReserveLocalPort()
        {
            string arguments = String.Format("http add urlacl http://*:{0}/ user={1}", port, userName);
            if (RunNetsh(arguments))
            {
                try
                {
                    var firewallManager = new FirewallManager(port, firewallRuleName);
                    firewallManager.OpenPort();
                }
                catch (Exception ex)
                {
                    throw new WardenException(String.Format("Error adding firewall rule for port '{0}', user '{1}'", port, userName), ex);
                }
            }
            else
            {
                throw new WardenException("Error reserving port '{0}' for user '{1}'", port, userName);
            }

            return port;
        }

        /// <summary>
        /// netsh http delete urlacl http://*:8888/
        /// </summary>
        public void ReleaseLocalPort()
        {
            string arguments = String.Format("http delete urlacl http://*:{0}/", port);
            if (RunNetsh(arguments))
            {
                try
                {
                    var firewallManager = new FirewallManager(port, firewallRuleName);
                    firewallManager.ClosePort();
                }
                catch (Exception ex)
                {
                    throw new WardenException(String.Format("Error removing firewall rule for port '{0}', user '{1}'", port, userName), ex);
                }
            }
            else
            {
                throw new WardenException("Error removing reservation for port '{0}'", port);
            }
        }

        private bool RunNetsh(string arguments)
        {
            bool success = false;

            using (var process = new BackgroundProcess(workingDirectory, "netsh.exe", arguments))
            {
                process.StartAndWait(asyncOutput: false);
                success = process.ExitCode == 0;
            }

            return success;
        }
    }
}
