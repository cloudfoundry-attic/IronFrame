using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using Microsoft.VisualBasic.ApplicationServices;
using NetFwTypeLib;

namespace IronFrame.Utilities
{
    internal interface IFirewallManager
    {
        void OpenPort(int port, string name);
        void ClosePort(string name);
        void RemoveAllFirewallRules(string userName);
        void CreateFirewallRule(string userName, FirewallRuleSpec firewallRuleSpec);
    }

    /// <summary>
    ///     http://www.shafqatahmed.com/2008/01/controlling-win.html
    ///     http://blogs.msdn.com/b/securitytools/archive/2009/08/21/automating-windows-firewall-settings-with-c.aspx
    /// </summary>
    internal class FirewallManager : IFirewallManager
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

        internal static string GetFormattedLocalUserSid(string windowsUsername)
        {
            var ntaccount = new NTAccount("", windowsUsername);
            var sid = ntaccount.Translate(typeof(SecurityIdentifier)).Value;
            return String.Format(CultureInfo.InvariantCulture, "D:(A;;CC;;;{0})", sid);
        }

        /// <summary>
        /// Remove all firewall rules that match the given name
        /// 
        /// The implementation is a bit hacky. There is no way determine 
        /// how many rules match a given name without iterating through 
        /// the entire collection of rules. This method seems cheaper 
        /// but uses exceptions to signal that no more rules match the given
        /// name.
        /// </summary>
        /// <param name="userName"></param>
        public void RemoveAllFirewallRules(string userName)
        {
            var firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID(NetFwPolicy2ProgID));
            var rules = firewallPolicy.Rules;
            try
            {
                // keep deleting until an exception is thrown
                while (true)
                {
                    // We need to call rules.Item() since rules.Remove() doesn't and silently return but 
                    // rules.Item() will throw an exception.
                    rules.Item(userName);
                    rules.Remove(userName);
                }
            }
            catch (FileNotFoundException)
            {
                // ignore the exception
            }
        }

        public void CreateFirewallRule(string windowsUserName, FirewallRuleSpec firewallRuleSpec)
        {
            var protocol = firewallRuleSpec.Protocol;
            if (protocol == Protocol.All)
            {
                _CreateFirewallRule(windowsUserName, Protocol.Udp, firewallRuleSpec);
                _CreateFirewallRule(windowsUserName, Protocol.Tcp, firewallRuleSpec);
            }
            else
            {
                _CreateFirewallRule(windowsUserName, protocol, firewallRuleSpec);    
            }
        }

        private void _CreateFirewallRule(string windowsUserName, Protocol proto, FirewallRuleSpec firewallRuleSpec)
        {
            var firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID(NetFwPolicy2ProgID));

            // This type is only avaible in Windows Server 2012
            var rule = ((INetFwRule3)Activator.CreateInstance(Type.GetTypeFromProgID(NetFwRuleProgID)));

            rule.Name = windowsUserName;
            switch (proto)
            {
                case Protocol.Tcp:
                    rule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
                    break;
                case Protocol.Udp:
                    rule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP;
                    break;
                default:
                    throw new Exception("Protocol " + firewallRuleSpec.Protocol + " is unknown");
            }
            rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
            rule.RemotePorts = firewallRuleSpec.RemotePorts;
            rule.RemoteAddresses = firewallRuleSpec.RemoteAddresses;
            rule.Enabled = true;

            string userSid = GetFormattedLocalUserSid(windowsUserName);
            rule.LocalUserAuthorizedList = userSid;
            firewallPolicy.Rules.Add(rule);
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