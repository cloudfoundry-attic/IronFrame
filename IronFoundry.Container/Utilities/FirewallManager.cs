using System;
using NetFwTypeLib;

namespace IronFoundry.Container.Utilities
{
    internal interface IFirewallManager
    {
        void OpenPort(int port, string name);
        void ClosePort(string name);
    }

    /// <summary>
    ///     http://www.shafqatahmed.com/2008/01/controlling-win.html
    ///     http://blogs.msdn.com/b/securitytools/archive/2009/08/21/automating-windows-firewall-settings-with-c.aspx
    /// </summary>
    internal sealed class FirewallManager : IFirewallManager
    {
        private const string NetFwPolicy2ProgID = "HNetCfg.FwPolicy2";
        private const string NetFwRuleProgID = "HNetCfg.FWRule";

        public void OpenPort(int port, string name)
        {
            if (port == default(ushort))
            {
                throw new ArgumentException("port");
            }

            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            var firewallRule = getComObject<INetFwRule2>(NetFwRuleProgID);
            firewallRule.Description = name;
            firewallRule.Name = name;
            firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            firewallRule.Enabled = true;
            firewallRule.InterfaceTypes = "All";
            firewallRule.Protocol = (int) NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
            firewallRule.LocalPorts = port.ToString();

            var firewallPolicy = getComObject<INetFwPolicy2>(NetFwPolicy2ProgID);
            firewallPolicy.Rules.Add(firewallRule);
        }

        public void ClosePort(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            var firewallPolicy = getComObject<INetFwPolicy2>(NetFwPolicy2ProgID);
            firewallPolicy.Rules.Remove(name);
        }

        private static T getComObject<T>(string progID)
        {
            Type t = Type.GetTypeFromProgID(progID, true);
            return (T) Activator.CreateInstance(t);
        }
    }
}