using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetFwTypeLib;
using Xunit;

namespace IronFrame.Utilities
{
    public class FirewallManagerTests
    {
        private const string Username = "Administrator";
        readonly FirewallManager manager = new FirewallManager();
        readonly INetFwPolicy2 firewallPolicy =
               (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
        readonly FirewallRuleSpec firewallRuleSpec = new FirewallRuleSpec
        {
            Networks = new List<IPRange>
            {
                new IPRange { Start = "10.1.1.1", End = "10.1.10.10" }
            }
        };

        [FactAdminRequired]
        public void CreateFirewallRule()
        {
            try
            {
                manager.CreateFirewallRule(Username, firewallRuleSpec);
                var rule = (INetFwRule3)firewallPolicy.Rules.Item(Username);
                Assert.NotNull(rule);
                Assert.Equal(rule.Action, NET_FW_ACTION_.NET_FW_ACTION_ALLOW);
                Assert.Equal(rule.Direction, NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT);
                Assert.Equal(rule.Enabled, true);
                Assert.Equal(rule.LocalUserAuthorizedList, FirewallManager.GetFormattedLocalUserSid(Username));
                Assert.Equal(rule.RemoteAddresses, firewallRuleSpec.RemoteAddresses);
            }
            finally
            {
                firewallPolicy.Rules.Remove(Username);
            }
        }


        [FactAdminRequired]
        public void RemoveFirewallRules()
        {
            manager.CreateFirewallRule(Username, firewallRuleSpec);
            manager.RemoveAllFirewallRules(Username);
            Assert.Throws<FileNotFoundException>(() => firewallPolicy.Rules.Item(Username));
        }
    }
}
