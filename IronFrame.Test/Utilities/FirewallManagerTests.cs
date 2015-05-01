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

        [FactAdminRequired]
        public void BlocksAllConnections()
        {
            try
            {
                manager.BlockAllOutboundConnections(Username);
                var rule = (INetFwRule3)firewallPolicy.Rules.Item(Username);
                Assert.NotNull(rule);
                Assert.Equal(rule.Action, NET_FW_ACTION_.NET_FW_ACTION_BLOCK);
                Assert.Equal(rule.LocalUserAuthorizedList, FirewallManager.GetFormattedLocalUserSid(Username));
            }
            finally
            {
                firewallPolicy.Rules.Remove(Username);
                Assert.Throws<FileNotFoundException>(() => firewallPolicy.Rules.Item(Username));
            }
        }

        [FactAdminRequired]
        public void RemoveFirewallRules()
        {
            manager.BlockAllOutboundConnections(Username);
            // windows allow more than one rule to be added with the same name
            manager.BlockAllOutboundConnections(Username);
            manager.RemoveAllFirewallRules(Username);
            Assert.Throws<FileNotFoundException>(() => firewallPolicy.Rules.Item(Username));
        }
    }
}
