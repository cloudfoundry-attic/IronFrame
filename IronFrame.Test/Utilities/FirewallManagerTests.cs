using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

        public void CheckCommonRuleProperties(INetFwRule3 rule)
        {
            Assert.NotNull(rule);
            Assert.Equal(NET_FW_ACTION_.NET_FW_ACTION_ALLOW, rule.Action);
            Assert.Equal(NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT, rule.Direction);
            Assert.Equal(true, rule.Enabled);
            Assert.Equal(FirewallManager.GetFormattedLocalUserSid(Username), rule.LocalUserAuthorizedList);
        }

        [Collection("Firewall Test Collection")]
        public class TcpTest : FirewallManagerTests
        {
            [FactAdminRequired]
            public void CreateFirewallRuleWithEmptySpec()
            {
                try
                {
                    manager.CreateOutboundFirewallRule(Username, new FirewallRuleSpec
                    {
                        Protocol =  Protocol.Tcp,
                    });
                    var rule = (INetFwRule3)firewallPolicy.Rules.Item(Username);
                    CheckCommonRuleProperties(rule);
                    Assert.Equal(rule.Protocol, (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);
                    Assert.Equal("*", rule.RemoteAddresses);
                    Assert.Equal("*", rule.RemotePorts);
                }
                finally
                {
                    manager.RemoveAllFirewallRules(Username);
                }
            }

            [FactAdminRequired]
            public void CreateFirewallRuleWithNetworks()
            {
                try
                {
                    var firewallRuleSpec = new FirewallRuleSpec
                    {
                        Protocol = Protocol.Tcp,
                        Networks = new List<IPRange>
                        {
                            new IPRange {Start = "10.1.1.1", End = "10.1.10.10"}
                        }
                    };
                    manager.CreateOutboundFirewallRule(Username, firewallRuleSpec);
                    var rule = (INetFwRule3)firewallPolicy.Rules.Item(Username);
                    CheckCommonRuleProperties(rule);
                    Assert.Equal(rule.Protocol, (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);
                    Assert.Equal(firewallRuleSpec.RemoteAddresses, rule.RemoteAddresses);
                    Assert.Equal("*", rule.RemotePorts);
                }
                finally
                {
                    manager.RemoveAllFirewallRules(Username);
                }
            }

            [FactAdminRequired]
            public void CreateFirewallRuleWithPorts()
            {
                var firewallSpec = new FirewallRuleSpec
                {
                    Protocol = Protocol.Tcp,
                    Ports = new List<PortRange>
                    {
                        new PortRange {Start = 8080, End = 8090},
                    }
                };
                try
                {
                    manager.CreateOutboundFirewallRule(Username, firewallSpec);
                    var rule = (INetFwRule3)firewallPolicy.Rules.Item(Username);
                    CheckCommonRuleProperties(rule);
                    Assert.Equal(rule.Protocol, (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);
                    Assert.Equal("*", rule.RemoteAddresses);
                    Assert.Equal(firewallSpec.RemotePorts, rule.RemotePorts);
                }
                finally
                {
                    manager.RemoveAllFirewallRules(Username);
                }
            }

            [FactAdminRequired]
            public void CreateFirewallRuleWithNetworksAndPorts()
            {
                try
                {
                    var firewallSpec = new FirewallRuleSpec
                    {
                        Protocol = Protocol.Tcp,
                        Ports = new List<PortRange>
                        {
                            new PortRange {Start = 8080, End = 8090},
                        },
                        Networks = new List<IPRange>
                        {
                            new IPRange() {Start = "10.1.1.1", End = "10.1.1.100"},
                        }

                    };
                    manager.CreateOutboundFirewallRule(Username, firewallSpec);
                    var rule = (INetFwRule3)firewallPolicy.Rules.Item(Username);
                    CheckCommonRuleProperties(rule);
                    Assert.Equal(rule.Protocol, (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);
                    Assert.Equal(firewallSpec.RemoteAddresses, rule.RemoteAddresses);
                    Assert.Equal(firewallSpec.RemotePorts, rule.RemotePorts);
                }
                finally
                {
                    manager.RemoveAllFirewallRules(Username);
                }
            }
        }

        [Collection("Firewall Test Collection")]
        public class AllProtocolsTest : FirewallManagerTests
        {
            [FactAdminRequired]
            public void TestAllProtocolsFirewallRule()
            {
                try
                {
                    var firewallSpec = new FirewallRuleSpec
                    {
                        Protocol = Protocol.All,
                        Ports = new List<PortRange>
                        {
                            new PortRange {Start = 8080, End = 8090},
                        },
                    };
                    manager.CreateOutboundFirewallRule(Username, firewallSpec);

                    // On windows we have to create two rules one for tcp and another for udp
                    var rule = (INetFwRule3)firewallPolicy.Rules.Item(Username);
                    CheckCommonRuleProperties(rule);
                    Assert.Equal(rule.Protocol, (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);
                    Assert.Equal(firewallSpec.RemoteAddresses, rule.RemoteAddresses);
                    Assert.Equal(firewallSpec.RemotePorts, rule.RemotePorts);
                    firewallPolicy.Rules.Remove(Username);

                    rule = (INetFwRule3)firewallPolicy.Rules.Item(Username);
                    CheckCommonRuleProperties(rule);
                    Assert.Equal(rule.Protocol, (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP);
                    Assert.Equal(firewallSpec.RemoteAddresses, rule.RemoteAddresses);
                    Assert.Equal(firewallSpec.RemotePorts, rule.RemotePorts);

                }
                finally
                {
                    manager.RemoveAllFirewallRules(Username);
                }
            }
        };

        [Collection("Firewall Test Collection")]
        public class UdpTest : FirewallManagerTests
        {
            [FactAdminRequired]
            public void TestUdpProtocol()
            {
                try
                {
                    var firewallSpec = new FirewallRuleSpec
                    {
                        Protocol = Protocol.Udp,
                        Ports = new List<PortRange>
                        {
                            new PortRange {Start = 8080, End = 8090},
                        },
                        Networks = new List<IPRange>
                        {
                            new IPRange() {Start = "10.1.1.1", End = "10.1.1.100"},
                        }

                    };
                    manager.CreateOutboundFirewallRule(Username, firewallSpec);
                    var rule = (INetFwRule3)firewallPolicy.Rules.Item(Username);
                    CheckCommonRuleProperties(rule);
                    Assert.Equal(rule.Protocol, (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP);
                    Assert.Equal(firewallSpec.RemoteAddresses, rule.RemoteAddresses);
                    Assert.Equal(firewallSpec.RemotePorts, rule.RemotePorts);
                }
                finally
                {
                    manager.RemoveAllFirewallRules(Username);
                }
            }
        };

        [Collection("Firewall Test Collection")]
        public class RemoveFirewallRulesTest : FirewallManagerTests
        {
            [FactAdminRequired]
            public void RemoveFirewallRules()
            {
                manager.CreateOutboundFirewallRule(Username, new FirewallRuleSpec());
                manager.RemoveAllFirewallRules(Username);
                Assert.Throws<FileNotFoundException>(() => firewallPolicy.Rules.Item(Username));
            }
        }
    }
}
