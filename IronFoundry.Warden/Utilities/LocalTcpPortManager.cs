using System;
using NLog;

namespace IronFoundry.Warden.Utilities
{
    public class LocalTcpPortManager : ILocalTcpPortManager
    {
        private readonly IFirewallManager firewallManager;
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly INetShRunner netShRunner;

        public LocalTcpPortManager()
            : this(new FirewallManager(), new NetShRunner())
        { }

        public LocalTcpPortManager(IFirewallManager firewallManager, INetShRunner netShRunner)
        {
            this.netShRunner = netShRunner;
            this.firewallManager = firewallManager;
        }

        /// <summary>
        ///     netsh http add urlacl http://*:8888/ user=warden_094850238
        /// </summary>
        public ushort ReserveLocalPort(ushort port, string userName)
        {
            if (userName.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("userName");
            }

            if (port == default(ushort))
            {
                port = IPUtilities.RandomFreePort();
            }

            log.Info("Reserving port {0} for user {1}", port, userName);

            //string arguments = String.Format("http add urlacl http://*:{0}/ user={1}", port, userName);
            if (netShRunner.AddRule(port, userName))
            {
                try
                {
                    firewallManager.OpenPort(port, userName);
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
        ///     netsh http delete urlacl http://*:8888/
        /// </summary>
        public void ReleaseLocalPort(ushort? port, string userName)
        {
            if (userName.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("userName");
            }

            log.Info("Releasing port {0} for user {1}", port, userName);


            if (port.HasValue)
            {
                if (!netShRunner.DeleteRule(port.Value))
                    throw new WardenException("Error removing reservation for port '{0}'", port);
            }

            try
            {
                firewallManager.ClosePort(userName);
            }
            catch (Exception ex)
            {
                throw new WardenException(String.Format("Error removing firewall rule for port '{0}', user '{1}'", port, userName), ex);
            }

        }
    }
}